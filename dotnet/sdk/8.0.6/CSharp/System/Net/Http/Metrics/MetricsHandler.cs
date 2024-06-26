using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Http.Metrics;

internal sealed class MetricsHandler : HttpMessageHandlerStage
{
	private sealed class SharedMeter : Meter
	{
		public static Meter Instance { get; } = new SharedMeter();


		private SharedMeter()
			: base("System.Net.Http")
		{
		}

		protected override void Dispose(bool disposing)
		{
		}
	}

	private readonly HttpMessageHandler _innerHandler;

	private readonly UpDownCounter<long> _activeRequests;

	private readonly Histogram<double> _requestsDuration;

	private static object[] s_boxedStatusCodes;

	private static string[] s_statusCodeStrings;

	public MetricsHandler(HttpMessageHandler innerHandler, IMeterFactory meterFactory, out Meter meter)
	{
		_innerHandler = innerHandler;
		meter = meterFactory?.Create("System.Net.Http") ?? SharedMeter.Instance;
		_activeRequests = meter.CreateUpDownCounter<long>("http.client.active_requests", "{request}", "Number of outbound HTTP requests that are currently active on the client.");
		_requestsDuration = meter.CreateHistogram<double>("http.client.request.duration", "s", "The duration of outbound HTTP requests.");
	}

	internal override ValueTask<HttpResponseMessage> SendAsync(HttpRequestMessage request, bool async, CancellationToken cancellationToken)
	{
		if (_activeRequests.Enabled || _requestsDuration.Enabled)
		{
			return SendAsyncWithMetrics(request, async, cancellationToken);
		}
		if (!async)
		{
			return new ValueTask<HttpResponseMessage>(_innerHandler.Send(request, cancellationToken));
		}
		return new ValueTask<HttpResponseMessage>(_innerHandler.SendAsync(request, cancellationToken));
	}

	private async ValueTask<HttpResponseMessage> SendAsyncWithMetrics(HttpRequestMessage request, bool async, CancellationToken cancellationToken)
	{
		(long, bool) tuple = RequestStart(request);
		long startTimestamp = tuple.Item1;
		bool recordCurrentRequests = tuple.Item2;
		HttpResponseMessage response = null;
		Exception exception = null;
		try
		{
			HttpResponseMessage httpResponseMessage = ((!async) ? _innerHandler.Send(request, cancellationToken) : (await _innerHandler.SendAsync(request, cancellationToken).ConfigureAwait(continueOnCapturedContext: false)));
			response = httpResponseMessage;
			return response;
		}
		catch (Exception ex)
		{
			exception = ex;
			throw;
		}
		finally
		{
			RequestStop(request, response, exception, startTimestamp, recordCurrentRequests);
		}
	}

	protected override void Dispose(bool disposing)
	{
		if (disposing)
		{
			_innerHandler.Dispose();
		}
		base.Dispose(disposing);
	}

	private (long StartTimestamp, bool RecordCurrentRequests) RequestStart(HttpRequestMessage request)
	{
		bool enabled = _activeRequests.Enabled;
		long timestamp = Stopwatch.GetTimestamp();
		if (enabled)
		{
			TagList tagList = InitializeCommonTags(request);
			_activeRequests.Add(1L, in tagList);
		}
		return (StartTimestamp: timestamp, RecordCurrentRequests: enabled);
	}

	private void RequestStop(HttpRequestMessage request, HttpResponseMessage response, Exception exception, long startTimestamp, bool recordCurrentRequests)
	{
		TagList tagList = InitializeCommonTags(request);
		if (recordCurrentRequests)
		{
			_activeRequests.Add(-1L, in tagList);
		}
		if (_requestsDuration.Enabled)
		{
			if (response != null)
			{
				tagList.Add("http.response.status_code", GetBoxedStatusCode((int)response.StatusCode));
				tagList.Add("network.protocol.version", GetProtocolVersionString(response.Version));
			}
			if (TryGetErrorType(response, exception, out var errorType))
			{
				tagList.Add("error.type", errorType);
			}
			TimeSpan elapsedTime = Stopwatch.GetElapsedTime(startTimestamp, Stopwatch.GetTimestamp());
			HttpMetricsEnrichmentContext enrichmentContextForRequest = HttpMetricsEnrichmentContext.GetEnrichmentContextForRequest(request);
			if (enrichmentContextForRequest == null)
			{
				_requestsDuration.Record(elapsedTime.TotalSeconds, in tagList);
			}
			else
			{
				enrichmentContextForRequest.RecordDurationWithEnrichment(request, response, exception, elapsedTime, in tagList, _requestsDuration);
			}
		}
	}

	private static bool TryGetErrorType(HttpResponseMessage response, Exception exception, out string errorType)
	{
		if (response != null)
		{
			int statusCode = (int)response.StatusCode;
			if (statusCode >= 400 && statusCode <= 599)
			{
				errorType = GetErrorStatusCodeString(statusCode);
				return true;
			}
		}
		if (exception == null)
		{
			errorType = null;
			return false;
		}
		errorType = (exception as HttpRequestException)?.HttpRequestError switch
		{
			HttpRequestError.NameResolutionError => "name_resolution_error", 
			HttpRequestError.ConnectionError => "connection_error", 
			HttpRequestError.SecureConnectionError => "secure_connection_error", 
			HttpRequestError.HttpProtocolError => "http_protocol_error", 
			HttpRequestError.ExtendedConnectNotSupported => "extended_connect_not_supported", 
			HttpRequestError.VersionNegotiationError => "version_negotiation_error", 
			HttpRequestError.UserAuthenticationError => "user_authentication_error", 
			HttpRequestError.ProxyTunnelError => "proxy_tunnel_error", 
			HttpRequestError.InvalidResponse => "invalid_response", 
			HttpRequestError.ResponseEnded => "response_ended", 
			HttpRequestError.ConfigurationLimitExceeded => "configuration_limit_exceeded", 
			_ => exception.GetType().FullName, 
		};
		return true;
	}

	private static string GetProtocolVersionString(Version httpVersion)
	{
		int major = httpVersion.Major;
		int minor = httpVersion.Minor;
		switch (major)
		{
		case 1:
			switch (minor)
			{
			case 0:
				return "1.0";
			case 1:
				return "1.1";
			}
			break;
		case 2:
			if (minor != 0)
			{
				break;
			}
			return "2";
		case 3:
			if (minor != 0)
			{
				break;
			}
			return "3";
		}
		return httpVersion.ToString();
	}

	private static TagList InitializeCommonTags(HttpRequestMessage request)
	{
		TagList result = default(TagList);
		Uri requestUri = request.RequestUri;
		if ((object)requestUri != null && requestUri.IsAbsoluteUri)
		{
			result.Add("url.scheme", requestUri.Scheme);
			result.Add("server.address", requestUri.Host);
			if (!requestUri.IsDefaultPort)
			{
				result.Add("server.port", requestUri.Port);
			}
		}
		result.Add(GetMethodTag(request.Method));
		return result;
	}

	internal static KeyValuePair<string, object> GetMethodTag(HttpMethod method)
	{
		return new KeyValuePair<string, object>("http.request.method", HttpMethod.GetKnownMethod(method.Method)?.Method ?? "_OTHER");
	}

	private static object GetBoxedStatusCode(int statusCode)
	{
		object[] array = LazyInitializer.EnsureInitialized(ref s_boxedStatusCodes, () => new object[512]);
		if ((uint)statusCode >= (uint)array.Length)
		{
			return statusCode;
		}
		object[] array2 = array;
		return array2[statusCode] ?? (array2[statusCode] = statusCode);
	}

	private static string GetErrorStatusCodeString(int statusCode)
	{
		string[] array = LazyInitializer.EnsureInitialized(ref s_statusCodeStrings, () => new string[200]);
		int num = statusCode - 400;
		if ((uint)num >= (uint)array.Length)
		{
			return statusCode.ToString();
		}
		ref string reference = ref array[num];
		return reference ?? (reference = statusCode.ToString());
	}
}
