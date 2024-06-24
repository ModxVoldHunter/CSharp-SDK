using System.Runtime.Versioning;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Http;

public class HttpMessageInvoker : IDisposable
{
	private volatile bool _disposed;

	private readonly bool _disposeHandler;

	private readonly HttpMessageHandler _handler;

	public HttpMessageInvoker(HttpMessageHandler handler)
		: this(handler, disposeHandler: true)
	{
	}

	public HttpMessageInvoker(HttpMessageHandler handler, bool disposeHandler)
	{
		ArgumentNullException.ThrowIfNull(handler, "handler");
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			System.Net.NetEventSource.Associate(this, handler, ".ctor");
		}
		_handler = handler;
		_disposeHandler = disposeHandler;
	}

	[UnsupportedOSPlatform("browser")]
	public virtual HttpResponseMessage Send(HttpRequestMessage request, CancellationToken cancellationToken)
	{
		ArgumentNullException.ThrowIfNull(request, "request");
		ObjectDisposedException.ThrowIf(_disposed, this);
		if (ShouldSendWithTelemetry(request))
		{
			HttpTelemetry.Log.RequestStart(request);
			HttpResponseMessage httpResponseMessage = null;
			try
			{
				httpResponseMessage = _handler.Send(request, cancellationToken);
				return httpResponseMessage;
			}
			catch (Exception exception) when (LogRequestFailed(exception, telemetryStarted: true))
			{
				throw;
			}
			finally
			{
				HttpTelemetry.Log.RequestStop(httpResponseMessage);
			}
		}
		return _handler.Send(request, cancellationToken);
	}

	public virtual Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
	{
		ArgumentNullException.ThrowIfNull(request, "request");
		ObjectDisposedException.ThrowIf(_disposed, this);
		if (ShouldSendWithTelemetry(request))
		{
			return SendAsyncWithTelemetry(_handler, request, cancellationToken);
		}
		return _handler.SendAsync(request, cancellationToken);
		static async Task<HttpResponseMessage> SendAsyncWithTelemetry(HttpMessageHandler handler, HttpRequestMessage request, CancellationToken cancellationToken)
		{
			HttpTelemetry.Log.RequestStart(request);
			HttpResponseMessage response = null;
			try
			{
				response = await handler.SendAsync(request, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
				return response;
			}
			catch (Exception exception) when (LogRequestFailed(exception, telemetryStarted: true))
			{
				throw;
			}
			finally
			{
				HttpTelemetry.Log.RequestStop(response);
			}
		}
	}

	private static bool ShouldSendWithTelemetry(HttpRequestMessage request)
	{
		if (HttpTelemetry.Log.IsEnabled() && !request.WasSentByHttpClient())
		{
			Uri requestUri = request.RequestUri;
			if ((object)requestUri != null)
			{
				return requestUri.IsAbsoluteUri;
			}
		}
		return false;
	}

	internal static bool LogRequestFailed(Exception exception, bool telemetryStarted)
	{
		if (HttpTelemetry.Log.IsEnabled() && telemetryStarted)
		{
			HttpTelemetry.Log.RequestFailed(exception);
		}
		return false;
	}

	public void Dispose()
	{
		Dispose(disposing: true);
		GC.SuppressFinalize(this);
	}

	protected virtual void Dispose(bool disposing)
	{
		if (disposing && !_disposed)
		{
			_disposed = true;
			if (_disposeHandler)
			{
				_handler.Dispose();
			}
		}
	}
}
