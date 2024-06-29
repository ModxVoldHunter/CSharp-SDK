using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Metrics;
using System.IO;
using System.Net.Http.Metrics;
using System.Net.Security;
using System.Runtime.Versioning;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Http;

[UnsupportedOSPlatform("browser")]
public sealed class SocketsHttpHandler : HttpMessageHandler
{
	private readonly HttpConnectionSettings _settings = new HttpConnectionSettings();

	private HttpMessageHandlerStage _handler;

	private Func<HttpConnectionSettings, HttpMessageHandlerStage, HttpMessageHandlerStage> _decompressionHandlerFactory;

	private bool _disposed;

	[UnsupportedOSPlatformGuard("browser")]
	public static bool IsSupported => !OperatingSystem.IsBrowser();

	public bool UseCookies
	{
		get
		{
			return _settings._useCookies;
		}
		set
		{
			CheckDisposedOrStarted();
			_settings._useCookies = value;
		}
	}

	public CookieContainer CookieContainer
	{
		get
		{
			HttpConnectionSettings settings = _settings;
			return settings._cookieContainer ?? (settings._cookieContainer = new CookieContainer());
		}
		[param: AllowNull]
		set
		{
			CheckDisposedOrStarted();
			_settings._cookieContainer = value;
		}
	}

	public DecompressionMethods AutomaticDecompression
	{
		get
		{
			return _settings._automaticDecompression;
		}
		set
		{
			CheckDisposedOrStarted();
			EnsureDecompressionHandlerFactory();
			_settings._automaticDecompression = value;
		}
	}

	public bool UseProxy
	{
		get
		{
			return _settings._useProxy;
		}
		set
		{
			CheckDisposedOrStarted();
			_settings._useProxy = value;
		}
	}

	public IWebProxy? Proxy
	{
		get
		{
			return _settings._proxy;
		}
		set
		{
			CheckDisposedOrStarted();
			_settings._proxy = value;
		}
	}

	public ICredentials? DefaultProxyCredentials
	{
		get
		{
			return _settings._defaultProxyCredentials;
		}
		set
		{
			CheckDisposedOrStarted();
			_settings._defaultProxyCredentials = value;
		}
	}

	public bool PreAuthenticate
	{
		get
		{
			return _settings._preAuthenticate;
		}
		set
		{
			CheckDisposedOrStarted();
			_settings._preAuthenticate = value;
		}
	}

	public ICredentials? Credentials
	{
		get
		{
			return _settings._credentials;
		}
		set
		{
			CheckDisposedOrStarted();
			_settings._credentials = value;
		}
	}

	public bool AllowAutoRedirect
	{
		get
		{
			return _settings._allowAutoRedirect;
		}
		set
		{
			CheckDisposedOrStarted();
			_settings._allowAutoRedirect = value;
		}
	}

	public int MaxAutomaticRedirections
	{
		get
		{
			return _settings._maxAutomaticRedirections;
		}
		set
		{
			ArgumentOutOfRangeException.ThrowIfNegativeOrZero(value, "value");
			CheckDisposedOrStarted();
			_settings._maxAutomaticRedirections = value;
		}
	}

	public int MaxConnectionsPerServer
	{
		get
		{
			return _settings._maxConnectionsPerServer;
		}
		set
		{
			ArgumentOutOfRangeException.ThrowIfNegativeOrZero(value, "value");
			CheckDisposedOrStarted();
			_settings._maxConnectionsPerServer = value;
		}
	}

	public int MaxResponseDrainSize
	{
		get
		{
			return _settings._maxResponseDrainSize;
		}
		set
		{
			ArgumentOutOfRangeException.ThrowIfNegative(value, "value");
			CheckDisposedOrStarted();
			_settings._maxResponseDrainSize = value;
		}
	}

	public TimeSpan ResponseDrainTimeout
	{
		get
		{
			return _settings._maxResponseDrainTime;
		}
		set
		{
			if ((value < TimeSpan.Zero && value != Timeout.InfiniteTimeSpan) || value.TotalMilliseconds > 2147483647.0)
			{
				throw new ArgumentOutOfRangeException("value");
			}
			CheckDisposedOrStarted();
			_settings._maxResponseDrainTime = value;
		}
	}

	public int MaxResponseHeadersLength
	{
		get
		{
			return _settings._maxResponseHeadersLength;
		}
		set
		{
			ArgumentOutOfRangeException.ThrowIfNegativeOrZero(value, "value");
			CheckDisposedOrStarted();
			_settings._maxResponseHeadersLength = value;
		}
	}

	public SslClientAuthenticationOptions SslOptions
	{
		get
		{
			HttpConnectionSettings settings = _settings;
			return settings._sslOptions ?? (settings._sslOptions = new SslClientAuthenticationOptions());
		}
		[param: AllowNull]
		set
		{
			CheckDisposedOrStarted();
			_settings._sslOptions = value;
		}
	}

	public TimeSpan PooledConnectionLifetime
	{
		get
		{
			return _settings._pooledConnectionLifetime;
		}
		set
		{
			if (value < TimeSpan.Zero && value != Timeout.InfiniteTimeSpan)
			{
				throw new ArgumentOutOfRangeException("value");
			}
			CheckDisposedOrStarted();
			_settings._pooledConnectionLifetime = value;
		}
	}

	public TimeSpan PooledConnectionIdleTimeout
	{
		get
		{
			return _settings._pooledConnectionIdleTimeout;
		}
		set
		{
			if (value < TimeSpan.Zero && value != Timeout.InfiniteTimeSpan)
			{
				throw new ArgumentOutOfRangeException("value");
			}
			CheckDisposedOrStarted();
			_settings._pooledConnectionIdleTimeout = value;
		}
	}

	public TimeSpan ConnectTimeout
	{
		get
		{
			return _settings._connectTimeout;
		}
		set
		{
			if ((value <= TimeSpan.Zero && value != Timeout.InfiniteTimeSpan) || value.TotalMilliseconds > 2147483647.0)
			{
				throw new ArgumentOutOfRangeException("value");
			}
			CheckDisposedOrStarted();
			_settings._connectTimeout = value;
		}
	}

	public TimeSpan Expect100ContinueTimeout
	{
		get
		{
			return _settings._expect100ContinueTimeout;
		}
		set
		{
			if ((value < TimeSpan.Zero && value != Timeout.InfiniteTimeSpan) || value.TotalMilliseconds > 2147483647.0)
			{
				throw new ArgumentOutOfRangeException("value");
			}
			CheckDisposedOrStarted();
			_settings._expect100ContinueTimeout = value;
		}
	}

	public int InitialHttp2StreamWindowSize
	{
		get
		{
			return _settings._initialHttp2StreamWindowSize;
		}
		set
		{
			if (value < 65535 || value > GlobalHttpSettings.SocketsHttpHandler.MaxHttp2StreamWindowSize)
			{
				string message = System.SR.Format(System.SR.net_http_http2_invalidinitialstreamwindowsize, 65535, GlobalHttpSettings.SocketsHttpHandler.MaxHttp2StreamWindowSize);
				throw new ArgumentOutOfRangeException("InitialHttp2StreamWindowSize", message);
			}
			CheckDisposedOrStarted();
			_settings._initialHttp2StreamWindowSize = value;
		}
	}

	public TimeSpan KeepAlivePingDelay
	{
		get
		{
			return _settings._keepAlivePingDelay;
		}
		set
		{
			if (value.Ticks < 10000000 && value != Timeout.InfiniteTimeSpan)
			{
				throw new ArgumentOutOfRangeException("value", value, System.SR.Format(System.SR.net_http_value_must_be_greater_than_or_equal, value, TimeSpan.FromSeconds(1.0)));
			}
			CheckDisposedOrStarted();
			_settings._keepAlivePingDelay = value;
		}
	}

	public TimeSpan KeepAlivePingTimeout
	{
		get
		{
			return _settings._keepAlivePingTimeout;
		}
		set
		{
			if (value.Ticks < 10000000 && value != Timeout.InfiniteTimeSpan)
			{
				throw new ArgumentOutOfRangeException("value", value, System.SR.Format(System.SR.net_http_value_must_be_greater_than_or_equal, value, TimeSpan.FromSeconds(1.0)));
			}
			CheckDisposedOrStarted();
			_settings._keepAlivePingTimeout = value;
		}
	}

	public HttpKeepAlivePingPolicy KeepAlivePingPolicy
	{
		get
		{
			return _settings._keepAlivePingPolicy;
		}
		set
		{
			CheckDisposedOrStarted();
			_settings._keepAlivePingPolicy = value;
		}
	}

	public bool EnableMultipleHttp2Connections
	{
		get
		{
			return _settings._enableMultipleHttp2Connections;
		}
		set
		{
			CheckDisposedOrStarted();
			_settings._enableMultipleHttp2Connections = value;
		}
	}

	public Func<SocketsHttpConnectionContext, CancellationToken, ValueTask<Stream>>? ConnectCallback
	{
		get
		{
			return _settings._connectCallback;
		}
		set
		{
			CheckDisposedOrStarted();
			_settings._connectCallback = value;
		}
	}

	public Func<SocketsHttpPlaintextStreamFilterContext, CancellationToken, ValueTask<Stream>>? PlaintextStreamFilter
	{
		get
		{
			return _settings._plaintextStreamFilter;
		}
		set
		{
			CheckDisposedOrStarted();
			_settings._plaintextStreamFilter = value;
		}
	}

	public IDictionary<string, object?> Properties
	{
		get
		{
			HttpConnectionSettings settings = _settings;
			return settings._properties ?? (settings._properties = new Dictionary<string, object>());
		}
	}

	public HeaderEncodingSelector<HttpRequestMessage>? RequestHeaderEncodingSelector
	{
		get
		{
			return _settings._requestHeaderEncodingSelector;
		}
		set
		{
			CheckDisposedOrStarted();
			_settings._requestHeaderEncodingSelector = value;
		}
	}

	public HeaderEncodingSelector<HttpRequestMessage>? ResponseHeaderEncodingSelector
	{
		get
		{
			return _settings._responseHeaderEncodingSelector;
		}
		set
		{
			CheckDisposedOrStarted();
			_settings._responseHeaderEncodingSelector = value;
		}
	}

	[CLSCompliant(false)]
	public DistributedContextPropagator? ActivityHeadersPropagator
	{
		get
		{
			return _settings._activityHeadersPropagator;
		}
		set
		{
			CheckDisposedOrStarted();
			_settings._activityHeadersPropagator = value;
		}
	}

	[CLSCompliant(false)]
	public IMeterFactory? MeterFactory
	{
		get
		{
			return _settings._meterFactory;
		}
		set
		{
			CheckDisposedOrStarted();
			_settings._meterFactory = value;
		}
	}

	internal ClientCertificateOption ClientCertificateOptions
	{
		get
		{
			return _settings._clientCertificateOptions;
		}
		set
		{
			CheckDisposedOrStarted();
			_settings._clientCertificateOptions = value;
		}
	}

	private void CheckDisposedOrStarted()
	{
		ObjectDisposedException.ThrowIf(_disposed, this);
		if (_handler != null)
		{
			throw new InvalidOperationException(System.SR.net_http_operation_started);
		}
	}

	protected override void Dispose(bool disposing)
	{
		if (disposing && !_disposed)
		{
			_disposed = true;
			_handler?.Dispose();
		}
		base.Dispose(disposing);
	}

	private HttpMessageHandlerStage SetupHandlerChain()
	{
		HttpConnectionSettings httpConnectionSettings = _settings.CloneAndNormalize();
		HttpConnectionPoolManager poolManager = new HttpConnectionPoolManager(httpConnectionSettings);
		HttpMessageHandlerStage innerHandler = ((httpConnectionSettings._credentials != null) ? ((HttpMessageHandlerStage)new HttpAuthenticatedConnectionHandler(poolManager)) : ((HttpMessageHandlerStage)new HttpConnectionHandler(poolManager)));
		if (DiagnosticsHandler.IsGloballyEnabled())
		{
			DistributedContextPropagator activityHeadersPropagator = httpConnectionSettings._activityHeadersPropagator;
			if (activityHeadersPropagator != null)
			{
				innerHandler = new DiagnosticsHandler(innerHandler, activityHeadersPropagator, httpConnectionSettings._allowAutoRedirect);
			}
		}
		innerHandler = new MetricsHandler(innerHandler, httpConnectionSettings._meterFactory, out var meter);
		httpConnectionSettings._metrics = new SocketsHttpHandlerMetrics(meter);
		if (httpConnectionSettings._allowAutoRedirect)
		{
			HttpMessageHandlerStage redirectInnerHandler = ((httpConnectionSettings._credentials == null || httpConnectionSettings._credentials is CredentialCache) ? innerHandler : new HttpConnectionHandler(poolManager));
			innerHandler = new RedirectHandler(httpConnectionSettings._maxAutomaticRedirections, innerHandler, redirectInnerHandler);
		}
		if (httpConnectionSettings._automaticDecompression != 0)
		{
			innerHandler = _decompressionHandlerFactory(httpConnectionSettings, innerHandler);
		}
		if (Interlocked.CompareExchange(ref _handler, innerHandler, null) != null)
		{
			innerHandler.Dispose();
		}
		return _handler;
	}

	private void EnsureDecompressionHandlerFactory()
	{
		if (_decompressionHandlerFactory == null)
		{
			_decompressionHandlerFactory = (HttpConnectionSettings settings, HttpMessageHandlerStage handler) => new DecompressionHandler(settings._automaticDecompression, handler);
		}
	}

	protected internal override HttpResponseMessage Send(HttpRequestMessage request, CancellationToken cancellationToken)
	{
		ArgumentNullException.ThrowIfNull(request, "request");
		if (request.Version.Major >= 2)
		{
			throw new NotSupportedException(System.SR.Format(System.SR.net_http_http2_sync_not_supported, GetType()));
		}
		if (request.VersionPolicy == HttpVersionPolicy.RequestVersionOrHigher)
		{
			throw new NotSupportedException(System.SR.Format(System.SR.net_http_upgrade_not_enabled_sync, "Send", request.VersionPolicy));
		}
		ObjectDisposedException.ThrowIf(_disposed, this);
		cancellationToken.ThrowIfCancellationRequested();
		HttpMessageHandlerStage httpMessageHandlerStage = _handler ?? SetupHandlerChain();
		Exception ex = ValidateAndNormalizeRequest(request);
		if (ex != null)
		{
			throw ex;
		}
		return httpMessageHandlerStage.Send(request, cancellationToken);
	}

	protected internal override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
	{
		ArgumentNullException.ThrowIfNull(request, "request");
		ObjectDisposedException.ThrowIf(_disposed, this);
		if (cancellationToken.IsCancellationRequested)
		{
			return Task.FromCanceled<HttpResponseMessage>(cancellationToken);
		}
		HttpMessageHandlerStage httpMessageHandlerStage = _handler ?? SetupHandlerChain();
		Exception ex = ValidateAndNormalizeRequest(request);
		if (ex != null)
		{
			return Task.FromException<HttpResponseMessage>(ex);
		}
		return httpMessageHandlerStage.SendAsync(request, cancellationToken);
	}

	private static Exception ValidateAndNormalizeRequest(HttpRequestMessage request)
	{
		if (request.Version.Major == 0)
		{
			return new NotSupportedException(System.SR.net_http_unsupported_version);
		}
		if (request.HasHeaders && request.Headers.TransferEncodingChunked.GetValueOrDefault())
		{
			if (request.Content == null)
			{
				return new HttpRequestException(System.SR.net_http_client_execution_error, new InvalidOperationException(System.SR.net_http_chunked_not_allowed_with_empty_content));
			}
			request.Content.Headers.ContentLength = null;
		}
		else if (request.Content != null && !request.Content.Headers.ContentLength.HasValue)
		{
			request.Headers.TransferEncodingChunked = true;
		}
		if (request.Version.Minor == 0 && request.Version.Major == 1 && request.HasHeaders)
		{
			if (request.Headers.TransferEncodingChunked == true)
			{
				return new NotSupportedException(System.SR.net_http_unsupported_chunking);
			}
			if (request.Headers.ExpectContinue == true)
			{
				request.Headers.ExpectContinue = false;
			}
		}
		Uri requestUri = request.RequestUri;
		if ((object)requestUri == null || !requestUri.IsAbsoluteUri)
		{
			return new InvalidOperationException(System.SR.net_http_client_invalid_requesturi);
		}
		if (!HttpUtilities.IsSupportedScheme(requestUri.Scheme))
		{
			return new NotSupportedException(System.SR.Format(System.SR.net_http_unsupported_requesturi_scheme, requestUri.Scheme));
		}
		return null;
	}
}
