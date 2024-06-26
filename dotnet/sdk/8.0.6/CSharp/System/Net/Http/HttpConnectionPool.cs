using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Net.Http.Headers;
using System.Net.Http.HPack;
using System.Net.Http.QPack;
using System.Net.Quic;
using System.Net.Security;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Runtime.Versioning;
using System.Security.Authentication;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Http;

internal sealed class HttpConnectionPool : IDisposable
{
	private struct RequestQueue<T> where T : HttpConnectionBase
	{
		public struct QueueItem
		{
			public HttpRequestMessage Request;

			public HttpConnectionWaiter<T> Waiter;
		}

		private QueueItem[] _array;

		private int _head;

		private int _tail;

		private int _size;

		private int _attemptedConnectionsOffset;

		public int Count => _size;

		public int RequestsWithoutAConnectionAttempt => _size - _attemptedConnectionsOffset;

		public RequestQueue()
		{
			_array = Array.Empty<QueueItem>();
			_head = 0;
			_tail = 0;
			_size = 0;
			_attemptedConnectionsOffset = 0;
		}

		private void Enqueue(QueueItem queueItem)
		{
			if (_size == _array.Length)
			{
				Grow();
			}
			_array[_tail] = queueItem;
			MoveNext(ref _tail);
			_size++;
		}

		private QueueItem Dequeue()
		{
			int head = _head;
			QueueItem[] array = _array;
			QueueItem result = array[head];
			array[head] = default(QueueItem);
			MoveNext(ref _head);
			if (_attemptedConnectionsOffset > 0)
			{
				_attemptedConnectionsOffset--;
			}
			_size--;
			return result;
		}

		private bool TryPeek(out QueueItem queueItem)
		{
			if (_size == 0)
			{
				queueItem = default(QueueItem);
				return false;
			}
			queueItem = _array[_head];
			return true;
		}

		private void MoveNext(ref int index)
		{
			int num = index + 1;
			if (num == _array.Length)
			{
				num = 0;
			}
			index = num;
		}

		private void Grow()
		{
			QueueItem[] array = new QueueItem[Math.Max(4, _array.Length * 2)];
			if (_size != 0)
			{
				if (_head < _tail)
				{
					Array.Copy(_array, _head, array, 0, _size);
				}
				else
				{
					Array.Copy(_array, _head, array, 0, _array.Length - _head);
					Array.Copy(_array, 0, array, _array.Length - _head, _tail);
				}
			}
			_array = array;
			_head = 0;
			_tail = _size;
		}

		public HttpConnectionWaiter<T> EnqueueRequest(HttpRequestMessage request)
		{
			HttpConnectionWaiter<T> httpConnectionWaiter = new HttpConnectionWaiter<T>();
			Enqueue(new QueueItem
			{
				Request = request,
				Waiter = httpConnectionWaiter
			});
			return httpConnectionWaiter;
		}

		public void PruneCompletedRequestsFromHeadOfQueue(HttpConnectionPool pool)
		{
			QueueItem queueItem;
			while (TryPeek(out queueItem) && queueItem.Waiter.Task.IsCompleted)
			{
				if (System.Net.NetEventSource.Log.IsEnabled())
				{
					pool.Trace(queueItem.Waiter.Task.IsCanceled ? "Discarding canceled request from queue." : "Discarding signaled request waiter from queue.", "PruneCompletedRequestsFromHeadOfQueue");
				}
				Dequeue();
			}
		}

		public bool TryDequeueWaiter(HttpConnectionPool pool, [MaybeNullWhen(false)] out HttpConnectionWaiter<T> waiter)
		{
			PruneCompletedRequestsFromHeadOfQueue(pool);
			if (Count != 0)
			{
				waiter = Dequeue().Waiter;
				return true;
			}
			waiter = null;
			return false;
		}

		public void TryDequeueSpecificWaiter(HttpConnectionWaiter<T> waiter)
		{
			if (TryPeek(out var queueItem) && queueItem.Waiter == waiter)
			{
				Dequeue();
			}
		}

		public QueueItem PeekNextRequestForConnectionAttempt()
		{
			int num = _head + _attemptedConnectionsOffset;
			_attemptedConnectionsOffset++;
			if (num >= _array.Length)
			{
				num -= _array.Length;
			}
			return _array[num];
		}
	}

	private sealed class HttpConnectionWaiter<T> : TaskCompletionSourceWithCancellation<T> where T : HttpConnectionBase
	{
		public CancellationTokenSource ConnectionCancellationTokenSource;

		public bool CancelledByOriginatingRequestCompletion { get; set; }

		public ValueTask<T> WaitForConnectionAsync(HttpRequestMessage request, HttpConnectionPool pool, bool async, CancellationToken requestCancellationToken)
		{
			if (!HttpTelemetry.Log.IsEnabled() && !pool.Settings._metrics.RequestsQueueDuration.Enabled)
			{
				return WaitWithCancellationAsync(async, requestCancellationToken);
			}
			return WaitForConnectionWithTelemetryAsync(request, pool, async, requestCancellationToken);
		}

		private async ValueTask<T> WaitForConnectionWithTelemetryAsync(HttpRequestMessage request, HttpConnectionPool pool, bool async, CancellationToken requestCancellationToken)
		{
			long startingTimestamp = Stopwatch.GetTimestamp();
			try
			{
				return await WaitWithCancellationAsync(async, requestCancellationToken).ConfigureAwait(continueOnCapturedContext: false);
			}
			finally
			{
				TimeSpan elapsedTime = Stopwatch.GetElapsedTime(startingTimestamp);
				int versionMajor = ((typeof(T) == typeof(HttpConnection)) ? 1 : 2);
				pool.Settings._metrics.RequestLeftQueue(request, pool, elapsedTime, versionMajor);
				if (HttpTelemetry.Log.IsEnabled())
				{
					HttpTelemetry.Log.RequestLeftQueue(versionMajor, elapsedTime);
				}
			}
		}
	}

	private static readonly bool s_isWindows7Or2008R2 = GetIsWindows7Or2008R2();

	private readonly HttpConnectionPoolManager _poolManager;

	private readonly HttpConnectionKind _kind;

	private readonly Uri _proxyUri;

	private readonly HttpAuthority _originAuthority;

	private volatile HttpAuthority _http3Authority;

	private Timer _authorityExpireTimer;

	private bool _persistAuthority;

	private string _connectTunnelUserAgent;

	private volatile Dictionary<HttpAuthority, Exception> _altSvcBlocklist;

	private CancellationTokenSource _altSvcBlocklistTimerCancellation;

	private volatile bool _altSvcEnabled = true;

	private readonly List<HttpConnection> _availableHttp11Connections = new List<HttpConnection>();

	private readonly int _maxHttp11Connections;

	private int _associatedHttp11ConnectionCount;

	private int _pendingHttp11ConnectionCount;

	private RequestQueue<HttpConnection> _http11RequestQueue;

	private List<Http2Connection> _availableHttp2Connections;

	private int _associatedHttp2ConnectionCount;

	private bool _pendingHttp2Connection;

	private RequestQueue<Http2Connection> _http2RequestQueue;

	private bool _http2Enabled;

	private byte[] _http2AltSvcOriginUri;

	internal readonly byte[] _http2EncodedAuthorityHostHeader;

	private bool _http3Enabled;

	private Http3Connection _http3Connection;

	private SemaphoreSlim _http3ConnectionCreateLock;

	internal readonly byte[] _http3EncodedAuthorityHostHeader;

	internal uint _lastSeenHttp2MaxHeaderListSize;

	internal uint _lastSeenHttp3MaxHeaderListSize;

	private readonly byte[] _hostHeaderLineBytes;

	private readonly SslClientAuthenticationOptions _sslOptionsHttp11;

	private readonly SslClientAuthenticationOptions _sslOptionsHttp2;

	private readonly SslClientAuthenticationOptions _sslOptionsHttp2Only;

	private SslClientAuthenticationOptions _sslOptionsHttp3;

	private SslClientAuthenticationOptions _sslOptionsProxy;

	private bool _usedSinceLastCleanup = true;

	private bool _disposed;

	private static readonly List<SslApplicationProtocol> s_http3ApplicationProtocols = new List<SslApplicationProtocol> { SslApplicationProtocol.Http3 };

	private static readonly List<SslApplicationProtocol> s_http2ApplicationProtocols = new List<SslApplicationProtocol>
	{
		SslApplicationProtocol.Http2,
		SslApplicationProtocol.Http11
	};

	private static readonly List<SslApplicationProtocol> s_http2OnlyApplicationProtocols = new List<SslApplicationProtocol> { SslApplicationProtocol.Http2 };

	public HttpAuthority OriginAuthority => _originAuthority;

	public HttpConnectionSettings Settings => _poolManager.Settings;

	public HttpConnectionKind Kind => _kind;

	public bool IsSecure
	{
		get
		{
			if (_kind != HttpConnectionKind.Https && _kind != HttpConnectionKind.SslProxyTunnel)
			{
				return _kind == HttpConnectionKind.SslSocksTunnel;
			}
			return true;
		}
	}

	public Uri ProxyUri => _proxyUri;

	public ICredentials ProxyCredentials => _poolManager.ProxyCredentials;

	public byte[] HostHeaderLineBytes => _hostHeaderLineBytes;

	public CredentialCache PreAuthCredentials { get; }

	public bool IsDefaultPort => OriginAuthority.Port == (IsSecure ? 443 : 80);

	public byte[] Http2AltSvcOriginUri
	{
		get
		{
			if (_http2AltSvcOriginUri == null)
			{
				StringBuilder stringBuilder = new StringBuilder();
				stringBuilder.Append(IsSecure ? "https://" : "http://").Append(_originAuthority.IdnHost);
				if (!IsDefaultPort)
				{
					StringBuilder stringBuilder2 = stringBuilder;
					IFormatProvider invariantCulture = CultureInfo.InvariantCulture;
					StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(1, 1, stringBuilder2, invariantCulture);
					handler.AppendLiteral(":");
					handler.AppendFormatted(_originAuthority.Port);
					stringBuilder2.Append(invariantCulture, ref handler);
				}
				_http2AltSvcOriginUri = Encoding.ASCII.GetBytes(stringBuilder.ToString());
			}
			return _http2AltSvcOriginUri;
		}
	}

	private bool EnableMultipleHttp2Connections => _poolManager.Settings.EnableMultipleHttp2Connections;

	private object SyncObj => _availableHttp11Connections;

	private bool DoProxyAuth
	{
		get
		{
			if (_kind != HttpConnectionKind.Proxy)
			{
				return _kind == HttpConnectionKind.ProxyConnect;
			}
			return true;
		}
	}

	public HttpConnectionPool(HttpConnectionPoolManager poolManager, HttpConnectionKind kind, string host, int port, string sslHostName, Uri proxyUri)
	{
		_poolManager = poolManager;
		_kind = kind;
		_proxyUri = proxyUri;
		_maxHttp11Connections = Settings._maxConnectionsPerServer;
		_originAuthority = new HttpAuthority(host ?? proxyUri.IdnHost, port);
		_http2Enabled = _poolManager.Settings._maxHttpVersion >= HttpVersion.Version20;
		if (IsHttp3Supported())
		{
			_http3Enabled = _poolManager.Settings._maxHttpVersion >= HttpVersion.Version30;
		}
		switch (kind)
		{
		case HttpConnectionKind.Http:
			_http3Enabled = false;
			break;
		case HttpConnectionKind.Proxy:
			_http2Enabled = false;
			_http3Enabled = false;
			break;
		case HttpConnectionKind.ProxyTunnel:
			_http2Enabled = false;
			_http3Enabled = false;
			break;
		case HttpConnectionKind.SslProxyTunnel:
			_http3Enabled = false;
			break;
		case HttpConnectionKind.ProxyConnect:
			_maxHttp11Connections = int.MaxValue;
			_http2Enabled = false;
			_http3Enabled = false;
			break;
		case HttpConnectionKind.SocksTunnel:
		case HttpConnectionKind.SslSocksTunnel:
			_http3Enabled = false;
			break;
		}
		if (!_http3Enabled)
		{
			_altSvcEnabled = false;
		}
		string text = null;
		if (host != null)
		{
			text = (IsDefaultPort ? _originAuthority.HostValue : $"{_originAuthority.HostValue}:{_originAuthority.Port}");
			byte[] array = new byte[6 + text.Length + 2];
			"Host: "u8.CopyTo(array);
			Encoding.ASCII.GetBytes(text, array.AsSpan(6));
			array[^2] = 13;
			array[^1] = 10;
			_hostHeaderLineBytes = array;
			if (sslHostName == null)
			{
				_http2EncodedAuthorityHostHeader = HPackEncoder.EncodeLiteralHeaderFieldWithoutIndexingToAllocatedArray(1, text);
				_http3EncodedAuthorityHostHeader = QPackEncoder.EncodeLiteralHeaderFieldWithStaticNameReferenceToArray(0, text);
			}
		}
		if (sslHostName != null)
		{
			_sslOptionsHttp11 = ConstructSslOptions(poolManager, sslHostName);
			_sslOptionsHttp11.ApplicationProtocols = null;
			if (_http2Enabled)
			{
				_sslOptionsHttp2 = ConstructSslOptions(poolManager, sslHostName);
				_sslOptionsHttp2.ApplicationProtocols = s_http2ApplicationProtocols;
				_sslOptionsHttp2Only = ConstructSslOptions(poolManager, sslHostName);
				_sslOptionsHttp2Only.ApplicationProtocols = s_http2OnlyApplicationProtocols;
				_http2EncodedAuthorityHostHeader = HPackEncoder.EncodeLiteralHeaderFieldWithoutIndexingToAllocatedArray(1, text);
				_http3EncodedAuthorityHostHeader = QPackEncoder.EncodeLiteralHeaderFieldWithStaticNameReferenceToArray(0, text);
			}
		}
		if (_poolManager.Settings._preAuthenticate)
		{
			PreAuthCredentials = new CredentialCache();
		}
		_http11RequestQueue = new RequestQueue<HttpConnection>();
		if (_http2Enabled)
		{
			_http2RequestQueue = new RequestQueue<Http2Connection>();
		}
		if (_proxyUri != null && HttpUtilities.IsSupportedSecureScheme(_proxyUri.Scheme))
		{
			_sslOptionsProxy = ConstructSslOptions(poolManager, _proxyUri.IdnHost);
			_sslOptionsProxy.ApplicationProtocols = null;
		}
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			Trace($"{this}", ".ctor");
		}
	}

	[SupportedOSPlatformGuard("linux")]
	[SupportedOSPlatformGuard("macOS")]
	[SupportedOSPlatformGuard("Windows")]
	internal static bool IsHttp3Supported()
	{
		if ((!OperatingSystem.IsLinux() || OperatingSystem.IsAndroid()) && !OperatingSystem.IsWindows())
		{
			return OperatingSystem.IsMacOS();
		}
		return true;
	}

	private static SslClientAuthenticationOptions ConstructSslOptions(HttpConnectionPoolManager poolManager, string sslHostName)
	{
		SslClientAuthenticationOptions sslClientAuthenticationOptions = poolManager.Settings._sslOptions?.ShallowClone() ?? new SslClientAuthenticationOptions();
		if (poolManager.Settings._clientCertificateOptions == ClientCertificateOption.Manual && sslClientAuthenticationOptions.LocalCertificateSelectionCallback != null && (sslClientAuthenticationOptions.ClientCertificates == null || sslClientAuthenticationOptions.ClientCertificates.Count == 0))
		{
			sslClientAuthenticationOptions.LocalCertificateSelectionCallback = null;
		}
		sslClientAuthenticationOptions.TargetHost = sslHostName;
		if (s_isWindows7Or2008R2 && sslClientAuthenticationOptions.EnabledSslProtocols == SslProtocols.None)
		{
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				System.Net.NetEventSource.Info(poolManager, $"Win7OrWin2K8R2 platform, Changing default TLS protocols to {SslProtocols.Tls | SslProtocols.Tls11 | SslProtocols.Tls12 | SslProtocols.Tls13}", "ConstructSslOptions");
			}
			sslClientAuthenticationOptions.EnabledSslProtocols = SslProtocols.Tls | SslProtocols.Tls11 | SslProtocols.Tls12 | SslProtocols.Tls13;
		}
		return sslClientAuthenticationOptions;
	}

	[DoesNotReturn]
	private static void ThrowGetVersionException(HttpRequestMessage request, int desiredVersion, Exception inner = null)
	{
		HttpRequestException ex = new HttpRequestException(HttpRequestError.VersionNegotiationError, System.SR.Format(System.SR.net_http_requested_version_cannot_establish, request.Version, request.VersionPolicy, desiredVersion), inner);
		if (request.IsExtendedConnectRequest && desiredVersion == 2)
		{
			ex.Data["HTTP2_ENABLED"] = false;
		}
		throw ex;
	}

	private bool CheckExpirationOnGet(HttpConnectionBase connection)
	{
		TimeSpan pooledConnectionLifetime = _poolManager.Settings._pooledConnectionLifetime;
		if (pooledConnectionLifetime != Timeout.InfiniteTimeSpan)
		{
			return (double)connection.GetLifetimeTicks(Environment.TickCount64) > pooledConnectionLifetime.TotalMilliseconds;
		}
		return false;
	}

	private static Exception CreateConnectTimeoutException(OperationCanceledException oce)
	{
		TimeoutException innerException = new TimeoutException(System.SR.net_http_connect_timedout, oce.InnerException);
		Exception ex = CancellationHelper.CreateOperationCanceledException(innerException, oce.CancellationToken);
		ExceptionDispatchInfo.SetCurrentStackTrace(ex);
		return ex;
	}

	private async Task AddHttp11ConnectionAsync(RequestQueue<HttpConnection>.QueueItem queueItem)
	{
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			Trace("Creating new HTTP/1.1 connection for pool.", "AddHttp11ConnectionAsync");
		}
		await Task.CompletedTask.ConfigureAwait(ConfigureAwaitOptions.ForceYielding);
		HttpConnectionWaiter<HttpConnection> waiter = queueItem.Waiter;
		HttpConnection connection = null;
		Exception connectionException = null;
		CancellationTokenSource cts = (waiter.ConnectionCancellationTokenSource = GetConnectTimeoutCancellationTokenSource());
		try
		{
			connection = await CreateHttp11ConnectionAsync(queueItem.Request, async: true, cts.Token).ConfigureAwait(continueOnCapturedContext: false);
		}
		catch (Exception ex)
		{
			connectionException = ((ex is OperationCanceledException ex2 && ex2.CancellationToken == cts.Token && !waiter.CancelledByOriginatingRequestCompletion) ? CreateConnectTimeoutException(ex2) : ex);
		}
		finally
		{
			lock (waiter)
			{
				waiter.ConnectionCancellationTokenSource = null;
				cts.Dispose();
			}
		}
		if (connection != null)
		{
			ReturnHttp11Connection(connection, isNewConnection: true, queueItem.Waiter);
		}
		else
		{
			HandleHttp11ConnectionFailure(waiter, connectionException);
		}
	}

	private void CheckForHttp11ConnectionInjection()
	{
		_http11RequestQueue.PruneCompletedRequestsFromHeadOfQueue(this);
		bool flag = _availableHttp11Connections.Count == 0 && _http11RequestQueue.Count > _pendingHttp11ConnectionCount && _associatedHttp11ConnectionCount < _maxHttp11Connections && _http11RequestQueue.RequestsWithoutAConnectionAttempt > 0;
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			Trace($"Available HTTP/1.1 connections: {_availableHttp11Connections.Count}, Requests in the queue: {_http11RequestQueue.Count}, Requests without a connection attempt: {_http11RequestQueue.RequestsWithoutAConnectionAttempt}, Pending HTTP/1.1 connections: {_pendingHttp11ConnectionCount}, Total associated HTTP/1.1 connections: {_associatedHttp11ConnectionCount}, Max HTTP/1.1 connection limit: {_maxHttp11Connections}, Will inject connection: {flag}.", "CheckForHttp11ConnectionInjection");
		}
		if (flag)
		{
			_associatedHttp11ConnectionCount++;
			_pendingHttp11ConnectionCount++;
			RequestQueue<HttpConnection>.QueueItem queueItem = _http11RequestQueue.PeekNextRequestForConnectionAttempt();
			AddHttp11ConnectionAsync(queueItem);
		}
	}

	private bool TryGetPooledHttp11Connection(HttpRequestMessage request, bool async, [NotNullWhen(true)] out HttpConnection connection, [NotNullWhen(false)] out HttpConnectionWaiter<HttpConnection> waiter)
	{
		while (true)
		{
			lock (SyncObj)
			{
				_usedSinceLastCleanup = true;
				int count = _availableHttp11Connections.Count;
				if (count <= 0)
				{
					waiter = _http11RequestQueue.EnqueueRequest(request);
					CheckForHttp11ConnectionInjection();
					if (System.Net.NetEventSource.Log.IsEnabled())
					{
						Trace("No available HTTP/1.1 connections; request queued.", "TryGetPooledHttp11Connection");
					}
					connection = null;
					return false;
				}
				connection = _availableHttp11Connections[count - 1];
				_availableHttp11Connections.RemoveAt(count - 1);
			}
			if (CheckExpirationOnGet(connection))
			{
				if (System.Net.NetEventSource.Log.IsEnabled())
				{
					connection.Trace("Found expired HTTP/1.1 connection in pool.", "TryGetPooledHttp11Connection");
				}
				connection.Dispose();
				continue;
			}
			if (connection.PrepareForReuse(async))
			{
				break;
			}
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				connection.Trace("Found invalid HTTP/1.1 connection in pool.", "TryGetPooledHttp11Connection");
			}
			connection.Dispose();
		}
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			connection.Trace("Found usable HTTP/1.1 connection in pool.", "TryGetPooledHttp11Connection");
		}
		waiter = null;
		return true;
	}

	private async Task HandleHttp11Downgrade(HttpRequestMessage request, Stream stream, TransportContext transportContext, IPEndPoint remoteEndPoint, CancellationToken cancellationToken)
	{
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			Trace("Server does not support HTTP2; disabling HTTP2 use and proceeding with HTTP/1.1 connection", "HandleHttp11Downgrade");
		}
		bool flag = true;
		HttpConnectionWaiter<Http2Connection> waiter = null;
		lock (SyncObj)
		{
			_http2Enabled = false;
			_associatedHttp2ConnectionCount--;
			_pendingHttp2Connection = false;
			if (_associatedHttp11ConnectionCount < _maxHttp11Connections)
			{
				_associatedHttp11ConnectionCount++;
				_pendingHttp11ConnectionCount++;
			}
			else
			{
				flag = false;
			}
			_http2RequestQueue.TryDequeueWaiter(this, out waiter);
		}
		while (waiter != null)
		{
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				Trace("Downgrading queued HTTP2 request to HTTP/1.1", "HandleHttp11Downgrade");
			}
			Volatile.Write(ref waiter.ConnectionCancellationTokenSource, null);
			waiter.TrySetResult(null);
			lock (SyncObj)
			{
				_http2RequestQueue.TryDequeueWaiter(this, out waiter);
			}
		}
		if (!flag)
		{
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				Trace("Discarding downgraded HTTP/1.1 connection because HTTP/1.1 connection limit is exceeded", "HandleHttp11Downgrade");
			}
			stream.Dispose();
		}
		HttpConnection connection;
		try
		{
			connection = await ConstructHttp11ConnectionAsync(async: true, stream, transportContext, request, remoteEndPoint, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
		}
		catch (OperationCanceledException ex) when (ex.CancellationToken == cancellationToken)
		{
			HandleHttp11ConnectionFailure(null, CreateConnectTimeoutException(ex));
			return;
		}
		catch (Exception e)
		{
			HandleHttp11ConnectionFailure(null, e);
			return;
		}
		ReturnHttp11Connection(connection, isNewConnection: true);
	}

	private async Task AddHttp2ConnectionAsync(RequestQueue<Http2Connection>.QueueItem queueItem)
	{
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			Trace("Creating new HTTP/2 connection for pool.", "AddHttp2ConnectionAsync");
		}
		await Task.CompletedTask.ConfigureAwait(ConfigureAwaitOptions.ForceYielding);
		Http2Connection connection = null;
		Exception connectionException = null;
		HttpConnectionWaiter<Http2Connection> waiter = queueItem.Waiter;
		CancellationTokenSource cts = (waiter.ConnectionCancellationTokenSource = GetConnectTimeoutCancellationTokenSource());
		try
		{
			var (stream, transportContext, remoteEndPoint) = await ConnectAsync(queueItem.Request, async: true, cts.Token).ConfigureAwait(continueOnCapturedContext: false);
			if (IsSecure)
			{
				SslStream sslStream = (SslStream)stream;
				if (!(sslStream.NegotiatedApplicationProtocol == SslApplicationProtocol.Http2))
				{
					await HandleHttp11Downgrade(queueItem.Request, stream, transportContext, remoteEndPoint, cts.Token).ConfigureAwait(continueOnCapturedContext: false);
					return;
				}
				if (sslStream.SslProtocol < SslProtocols.Tls12)
				{
					stream.Dispose();
					connectionException = new HttpRequestException(System.SR.Format(System.SR.net_ssl_http2_requires_tls12, sslStream.SslProtocol));
				}
				else
				{
					connection = await ConstructHttp2ConnectionAsync(stream, queueItem.Request, remoteEndPoint, cts.Token).ConfigureAwait(continueOnCapturedContext: false);
				}
			}
			else
			{
				connection = await ConstructHttp2ConnectionAsync(stream, queueItem.Request, remoteEndPoint, cts.Token).ConfigureAwait(continueOnCapturedContext: false);
			}
		}
		catch (Exception ex)
		{
			connectionException = ((ex is OperationCanceledException ex2 && ex2.CancellationToken == cts.Token && !waiter.CancelledByOriginatingRequestCompletion) ? CreateConnectTimeoutException(ex2) : ex);
		}
		finally
		{
			lock (waiter)
			{
				waiter.ConnectionCancellationTokenSource = null;
				cts.Dispose();
			}
		}
		if (connection != null)
		{
			ReturnHttp2Connection(connection, isNewConnection: true, queueItem.Waiter);
		}
		else
		{
			HandleHttp2ConnectionFailure(waiter, connectionException);
		}
	}

	private void CheckForHttp2ConnectionInjection()
	{
		_http2RequestQueue.PruneCompletedRequestsFromHeadOfQueue(this);
		int num = _availableHttp2Connections?.Count ?? 0;
		bool flag = num == 0 && !_pendingHttp2Connection && _http2RequestQueue.Count > 0 && (_associatedHttp2ConnectionCount == 0 || EnableMultipleHttp2Connections) && _http2RequestQueue.RequestsWithoutAConnectionAttempt > 0;
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			Trace($"Available HTTP/2.0 connections: {num}, Pending HTTP/2.0 connection: {_pendingHttp2Connection}Requests in the queue: {_http2RequestQueue.Count}, Requests without a connection attempt: {_http2RequestQueue.RequestsWithoutAConnectionAttempt}, Total associated HTTP/2.0 connections: {_associatedHttp2ConnectionCount}, Will inject connection: {flag}.", "CheckForHttp2ConnectionInjection");
		}
		if (flag)
		{
			_associatedHttp2ConnectionCount++;
			_pendingHttp2Connection = true;
			RequestQueue<Http2Connection>.QueueItem queueItem = _http2RequestQueue.PeekNextRequestForConnectionAttempt();
			AddHttp2ConnectionAsync(queueItem);
		}
	}

	private bool TryGetPooledHttp2Connection(HttpRequestMessage request, [NotNullWhen(true)] out Http2Connection connection, out HttpConnectionWaiter<Http2Connection> waiter)
	{
		while (true)
		{
			lock (SyncObj)
			{
				_usedSinceLastCleanup = true;
				if (!_http2Enabled)
				{
					waiter = null;
					connection = null;
					return false;
				}
				int num = _availableHttp2Connections?.Count ?? 0;
				if (num <= 0)
				{
					waiter = _http2RequestQueue.EnqueueRequest(request);
					CheckForHttp2ConnectionInjection();
					if (System.Net.NetEventSource.Log.IsEnabled())
					{
						Trace("No available HTTP/2 connections; request queued.", "TryGetPooledHttp2Connection");
					}
					connection = null;
					return false;
				}
				connection = _availableHttp2Connections[num - 1];
			}
			if (CheckExpirationOnGet(connection))
			{
				if (System.Net.NetEventSource.Log.IsEnabled())
				{
					connection.Trace("Found expired HTTP/2 connection in pool.", "TryGetPooledHttp2Connection");
				}
				InvalidateHttp2Connection(connection);
				continue;
			}
			if (connection.TryReserveStream())
			{
				break;
			}
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				connection.Trace("Found HTTP/2 connection in pool without available streams.", "TryGetPooledHttp2Connection");
			}
			bool flag = false;
			lock (SyncObj)
			{
				int num2 = _availableHttp2Connections.IndexOf(connection);
				if (num2 != -1)
				{
					flag = true;
					_availableHttp2Connections.RemoveAt(num2);
				}
			}
			if (flag)
			{
				DisableHttp2Connection(connection);
			}
		}
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			connection.Trace("Found usable HTTP/2 connection in pool.", "TryGetPooledHttp2Connection");
		}
		waiter = null;
		return true;
	}

	[SupportedOSPlatform("windows")]
	[SupportedOSPlatform("linux")]
	[SupportedOSPlatform("macos")]
	private async ValueTask<Http3Connection> GetHttp3ConnectionAsync(HttpRequestMessage request, HttpAuthority authority, CancellationToken cancellationToken)
	{
		Http3Connection http3Connection = Volatile.Read(ref _http3Connection);
		if (http3Connection != null)
		{
			if (!CheckExpirationOnGet(http3Connection) && http3Connection.Authority == authority)
			{
				if (System.Net.NetEventSource.Log.IsEnabled())
				{
					Trace("Using existing HTTP3 connection.", "GetHttp3ConnectionAsync");
				}
				_usedSinceLastCleanup = true;
				return http3Connection;
			}
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				http3Connection.Trace("Found expired HTTP3 connection.", "GetHttp3ConnectionAsync");
			}
			http3Connection.Dispose();
			InvalidateHttp3Connection(http3Connection);
		}
		if (_http3ConnectionCreateLock == null)
		{
			lock (SyncObj)
			{
				if (_http3ConnectionCreateLock == null)
				{
					_http3ConnectionCreateLock = new SemaphoreSlim(1);
				}
			}
		}
		await _http3ConnectionCreateLock.WaitAsync(cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
		try
		{
			if (_http3Connection != null)
			{
				if (System.Net.NetEventSource.Log.IsEnabled())
				{
					Trace("Using existing HTTP3 connection.", "GetHttp3ConnectionAsync");
				}
				return _http3Connection;
			}
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				Trace("Attempting new HTTP3 connection.", "GetHttp3ConnectionAsync");
			}
			QuicConnection connection;
			try
			{
				connection = await ConnectHelper.ConnectQuicAsync(request, new DnsEndPoint(authority.IdnHost, authority.Port), _poolManager.Settings._pooledConnectionIdleTimeout, _sslOptionsHttp3, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
			}
			catch (Exception ex)
			{
				if (System.Net.NetEventSource.Log.IsEnabled())
				{
					Trace($"QUIC connection failed: {ex}", "GetHttp3ConnectionAsync");
				}
				if (!(ex is OperationCanceledException ex2) || !cancellationToken.IsCancellationRequested || ex2.CancellationToken != cancellationToken)
				{
					BlocklistAuthority(authority, ex);
				}
				throw;
			}
			http3Connection = (_http3Connection = new Http3Connection(this, authority, connection, _http3Authority == authority));
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				Trace("New HTTP3 connection established.", "GetHttp3ConnectionAsync");
			}
			return http3Connection;
		}
		finally
		{
			_http3ConnectionCreateLock.Release();
		}
	}

	[SupportedOSPlatform("windows")]
	[SupportedOSPlatform("linux")]
	[SupportedOSPlatform("macos")]
	private async ValueTask<HttpResponseMessage> TrySendUsingHttp3Async(HttpRequestMessage request, CancellationToken cancellationToken)
	{
		HttpResponseMessage httpResponseMessage;
		while (true)
		{
			HttpAuthority httpAuthority = _http3Authority;
			if (request.Version.Major >= 3 && request.VersionPolicy != 0 && httpAuthority == null)
			{
				httpAuthority = _originAuthority;
			}
			if (httpAuthority == null)
			{
				return null;
			}
			if (IsAltSvcBlocked(httpAuthority, out var reasonException))
			{
				ThrowGetVersionException(request, 3, reasonException);
			}
			long queueStartingTimestamp = ((HttpTelemetry.Log.IsEnabled() || Settings._metrics.RequestsQueueDuration.Enabled) ? Stopwatch.GetTimestamp() : 0);
			Http3Connection connection = await GetHttp3ConnectionAsync(request, httpAuthority, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
			httpResponseMessage = await connection.SendAsync(request, queueStartingTimestamp, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
			if (httpResponseMessage.StatusCode != HttpStatusCode.MisdirectedRequest || connection.Authority == _originAuthority)
			{
				break;
			}
			httpResponseMessage.Dispose();
			BlocklistAuthority(connection.Authority);
		}
		return httpResponseMessage;
	}

	private void ProcessAltSvc(HttpResponseMessage response)
	{
		if (_altSvcEnabled && response.Headers.TryGetValues(KnownHeaders.AltSvc.Descriptor, out var values))
		{
			HandleAltSvc(values, response.Headers.Age);
		}
	}

	public async ValueTask<HttpResponseMessage> SendWithVersionDetectionAndRetryAsync(HttpRequestMessage request, bool async, bool doRequestAuth, CancellationToken cancellationToken)
	{
		int retryCount = 0;
		while (true)
		{
			HttpConnectionWaiter<HttpConnection> http11ConnectionWaiter = null;
			HttpConnectionWaiter<Http2Connection> http2ConnectionWaiter = null;
			try
			{
				HttpResponseMessage response = null;
				if (IsHttp3Supported() && _http3Enabled && (request.Version.Major >= 3 || (request.VersionPolicy == HttpVersionPolicy.RequestVersionOrHigher && IsSecure)) && !request.IsExtendedConnectRequest)
				{
					if (QuicConnection.IsSupported)
					{
						if (_sslOptionsHttp3 == null)
						{
							SslClientAuthenticationOptions sslClientAuthenticationOptions = ConstructSslOptions(_poolManager, _sslOptionsHttp11.TargetHost);
							sslClientAuthenticationOptions.ApplicationProtocols = s_http3ApplicationProtocols;
							Interlocked.CompareExchange(ref _sslOptionsHttp3, sslClientAuthenticationOptions, null);
						}
						response = await TrySendUsingHttp3Async(request, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
					}
					else
					{
						_altSvcEnabled = false;
						_http3Enabled = false;
					}
				}
				if (response == null)
				{
					if (request.Version.Major >= 3 && request.VersionPolicy != 0)
					{
						ThrowGetVersionException(request, 3);
					}
					if (_http2Enabled && (request.Version.Major >= 2 || (request.VersionPolicy == HttpVersionPolicy.RequestVersionOrHigher && IsSecure)) && (request.VersionPolicy != 0 || IsSecure))
					{
						if (!TryGetPooledHttp2Connection(request, out var connection, out http2ConnectionWaiter) && http2ConnectionWaiter != null)
						{
							connection = await http2ConnectionWaiter.WaitForConnectionAsync(request, this, async, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
						}
						if (connection != null)
						{
							if (request.IsExtendedConnectRequest)
							{
								await connection.InitialSettingsReceived.WaitWithCancellationAsync(cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
								if (!connection.IsConnectEnabled)
								{
									HttpRequestException ex = new HttpRequestException(HttpRequestError.ExtendedConnectNotSupported, System.SR.net_unsupported_extended_connect);
									ex.Data["SETTINGS_ENABLE_CONNECT_PROTOCOL"] = false;
									throw ex;
								}
							}
							response = await connection.SendAsync(request, async, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
						}
						connection = null;
					}
					if (response == null)
					{
						if (request.Version.Major >= 2 && request.VersionPolicy != 0)
						{
							ThrowGetVersionException(request, 2);
						}
						if (!TryGetPooledHttp11Connection(request, async, out var connection, out http11ConnectionWaiter))
						{
							connection = await http11ConnectionWaiter.WaitForConnectionAsync(request, this, async, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
						}
						connection.Acquire();
						try
						{
							response = await SendWithNtConnectionAuthAsync(connection, request, async, doRequestAuth, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
						}
						finally
						{
							connection.Release();
						}
						connection = null;
					}
				}
				ProcessAltSvc(response);
				return response;
			}
			catch (HttpRequestException ex2) when (ex2.AllowRetry == RequestRetryType.RetryOnConnectionFailure)
			{
				if (retryCount == 3)
				{
					if (System.Net.NetEventSource.Log.IsEnabled())
					{
						Trace($"MaxConnectionFailureRetries limit of {3} hit. Retryable request will not be retried. Exception: {ex2}", "SendWithVersionDetectionAndRetryAsync");
					}
					throw;
				}
				retryCount++;
				if (System.Net.NetEventSource.Log.IsEnabled())
				{
					Trace($"Retry attempt {retryCount} after connection failure. Connection exception: {ex2}", "SendWithVersionDetectionAndRetryAsync");
				}
			}
			catch (HttpRequestException ex3) when (ex3.AllowRetry == RequestRetryType.RetryOnLowerHttpVersion)
			{
				if (request.VersionPolicy != 0)
				{
					throw new HttpRequestException(HttpRequestError.VersionNegotiationError, System.SR.Format(System.SR.net_http_requested_version_server_refused, request.Version, request.VersionPolicy), ex3);
				}
				if (System.Net.NetEventSource.Log.IsEnabled())
				{
					Trace($"Retrying request because server requested version fallback: {ex3}", "SendWithVersionDetectionAndRetryAsync");
				}
				request.Version = HttpVersion.Version11;
			}
			catch (HttpRequestException ex4) when (ex4.AllowRetry == RequestRetryType.RetryOnStreamLimitReached)
			{
				if (System.Net.NetEventSource.Log.IsEnabled())
				{
					Trace($"Retrying request on another HTTP/2 connection after active streams limit is reached on existing one: {ex4}", "SendWithVersionDetectionAndRetryAsync");
				}
			}
			finally
			{
				CancelIfNecessary(http11ConnectionWaiter, cancellationToken.IsCancellationRequested);
				CancelIfNecessary(http2ConnectionWaiter, cancellationToken.IsCancellationRequested);
			}
		}
	}

	private void CancelIfNecessary<T>(HttpConnectionWaiter<T> waiter, bool requestCancelled) where T : HttpConnectionBase
	{
		int pendingConnectionTimeoutOnRequestCompletion = GlobalHttpSettings.SocketsHttpHandler.PendingConnectionTimeoutOnRequestCompletion;
		if (waiter?.ConnectionCancellationTokenSource == null || pendingConnectionTimeoutOnRequestCompletion == -1 || (Settings._connectTimeout != Timeout.InfiniteTimeSpan && pendingConnectionTimeoutOnRequestCompletion > (int)Settings._connectTimeout.TotalMilliseconds))
		{
			return;
		}
		lock (waiter)
		{
			if (waiter.ConnectionCancellationTokenSource != null)
			{
				if (System.Net.NetEventSource.Log.IsEnabled())
				{
					Trace($"Initiating cancellation of a pending connection attempt with delay of {pendingConnectionTimeoutOnRequestCompletion} ms, Reason: {(requestCancelled ? "Request cancelled" : "Request served by another connection")}.", "CancelIfNecessary");
				}
				waiter.CancelledByOriginatingRequestCompletion = true;
				if (pendingConnectionTimeoutOnRequestCompletion > 0)
				{
					waiter.ConnectionCancellationTokenSource.CancelAfter(pendingConnectionTimeoutOnRequestCompletion);
				}
				else
				{
					waiter.ConnectionCancellationTokenSource.Cancel();
				}
			}
		}
	}

	internal void HandleAltSvc(IEnumerable<string> altSvcHeaderValues, TimeSpan? responseAge)
	{
		HttpAuthority httpAuthority = null;
		TimeSpan dueTime = default(TimeSpan);
		bool flag = false;
		foreach (string altSvcHeaderValue2 in altSvcHeaderValues)
		{
			int index = 0;
			if (!AltSvcHeaderParser.Parser.TryParseValue(altSvcHeaderValue2, null, ref index, out var parsedValue))
			{
				continue;
			}
			AltSvcHeaderValue altSvcHeaderValue = (AltSvcHeaderValue)parsedValue;
			if (altSvcHeaderValue == AltSvcHeaderValue.Clear)
			{
				lock (SyncObj)
				{
					ExpireAltSvcAuthority();
					_authorityExpireTimer?.Change(-1, -1);
				}
				break;
			}
			if (httpAuthority != null || altSvcHeaderValue == null || !(altSvcHeaderValue.AlpnProtocolName == "h3"))
			{
				continue;
			}
			HttpAuthority httpAuthority2 = new HttpAuthority(altSvcHeaderValue.Host ?? _originAuthority.IdnHost, altSvcHeaderValue.Port);
			if (!IsAltSvcBlocked(httpAuthority2, out var _))
			{
				TimeSpan maxAge = altSvcHeaderValue.MaxAge;
				if (responseAge.HasValue)
				{
					maxAge -= responseAge.GetValueOrDefault();
				}
				if (maxAge > TimeSpan.Zero)
				{
					httpAuthority = httpAuthority2;
					dueTime = maxAge;
					flag = altSvcHeaderValue.Persist;
				}
			}
		}
		if (httpAuthority == null || httpAuthority.Equals(_http3Authority))
		{
			return;
		}
		if (dueTime.Ticks > 25920000000000L)
		{
			dueTime = TimeSpan.FromTicks(25920000000000L);
		}
		lock (SyncObj)
		{
			if (_disposed)
			{
				return;
			}
			if (_authorityExpireTimer == null)
			{
				WeakReference<HttpConnectionPool> state = new WeakReference<HttpConnectionPool>(this);
				using (ExecutionContext.SuppressFlow())
				{
					_authorityExpireTimer = new Timer(delegate(object o)
					{
						WeakReference<HttpConnectionPool> weakReference = (WeakReference<HttpConnectionPool>)o;
						if (weakReference.TryGetTarget(out var target))
						{
							target.ExpireAltSvcAuthority();
						}
					}, state, dueTime, Timeout.InfiniteTimeSpan);
				}
			}
			else
			{
				_authorityExpireTimer.Change(dueTime, Timeout.InfiniteTimeSpan);
			}
			_http3Authority = httpAuthority;
			_persistAuthority = flag;
		}
		if (!flag)
		{
			_poolManager.StartMonitoringNetworkChanges();
		}
	}

	private void ExpireAltSvcAuthority()
	{
		_http3Authority = null;
	}

	private bool IsAltSvcBlocked(HttpAuthority authority, out Exception reasonException)
	{
		if (_altSvcBlocklist != null)
		{
			lock (_altSvcBlocklist)
			{
				return _altSvcBlocklist.TryGetValue(authority, out reasonException);
			}
		}
		reasonException = null;
		return false;
	}

	internal void BlocklistAuthority(HttpAuthority badAuthority, Exception exception = null)
	{
		Dictionary<HttpAuthority, Exception> altSvcBlocklist = _altSvcBlocklist;
		if (altSvcBlocklist == null)
		{
			lock (SyncObj)
			{
				if (_disposed)
				{
					return;
				}
				altSvcBlocklist = _altSvcBlocklist;
				if (altSvcBlocklist == null)
				{
					altSvcBlocklist = new Dictionary<HttpAuthority, Exception>();
					_altSvcBlocklistTimerCancellation = new CancellationTokenSource();
					_altSvcBlocklist = altSvcBlocklist;
				}
			}
		}
		bool flag = false;
		bool flag2;
		lock (altSvcBlocklist)
		{
			flag2 = altSvcBlocklist.TryAdd(badAuthority, exception);
			if (flag2 && altSvcBlocklist.Count >= 8 && _altSvcEnabled)
			{
				_altSvcEnabled = false;
				flag = true;
			}
		}
		CancellationToken token;
		lock (SyncObj)
		{
			if (_disposed)
			{
				return;
			}
			if (_http3Authority == badAuthority)
			{
				ExpireAltSvcAuthority();
				_authorityExpireTimer.Change(-1, -1);
			}
			token = _altSvcBlocklistTimerCancellation.Token;
		}
		if (flag2)
		{
			Task.Delay(600000, token).ContinueWith(delegate
			{
				lock (altSvcBlocklist)
				{
					altSvcBlocklist.Remove(badAuthority);
				}
			}, token, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
		}
		if (flag)
		{
			Task.Delay(600000, token).ContinueWith(delegate
			{
				_altSvcEnabled = true;
			}, token, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
		}
	}

	public void OnNetworkChanged()
	{
		lock (SyncObj)
		{
			if (_http3Authority != null && !_persistAuthority)
			{
				ExpireAltSvcAuthority();
				_authorityExpireTimer?.Change(-1, -1);
			}
		}
	}

	public Task<HttpResponseMessage> SendWithNtConnectionAuthAsync(HttpConnection connection, HttpRequestMessage request, bool async, bool doRequestAuth, CancellationToken cancellationToken)
	{
		if (doRequestAuth && Settings._credentials != null)
		{
			return AuthenticationHelper.SendWithNtConnectionAuthAsync(request, async, Settings._credentials, connection, this, cancellationToken);
		}
		return SendWithNtProxyAuthAsync(connection, request, async, cancellationToken);
	}

	public Task<HttpResponseMessage> SendWithNtProxyAuthAsync(HttpConnection connection, HttpRequestMessage request, bool async, CancellationToken cancellationToken)
	{
		if (DoProxyAuth && ProxyCredentials != null)
		{
			return AuthenticationHelper.SendWithNtProxyAuthAsync(request, ProxyUri, async, ProxyCredentials, connection, this, cancellationToken);
		}
		return connection.SendAsync(request, async, cancellationToken);
	}

	public ValueTask<HttpResponseMessage> SendWithProxyAuthAsync(HttpRequestMessage request, bool async, bool doRequestAuth, CancellationToken cancellationToken)
	{
		if (DoProxyAuth && ProxyCredentials != null)
		{
			return AuthenticationHelper.SendWithProxyAuthAsync(request, _proxyUri, async, ProxyCredentials, doRequestAuth, this, cancellationToken);
		}
		return SendWithVersionDetectionAndRetryAsync(request, async, doRequestAuth, cancellationToken);
	}

	public ValueTask<HttpResponseMessage> SendAsync(HttpRequestMessage request, bool async, bool doRequestAuth, CancellationToken cancellationToken)
	{
		HttpConnectionKind kind = Kind;
		bool flag = kind - 3 <= HttpConnectionKind.Https;
		if (flag && request.HasHeaders && request.Headers.NonValidated.TryGetValues("User-Agent", out var values))
		{
			_connectTunnelUserAgent = values.ToString();
		}
		if (doRequestAuth && Settings._credentials != null)
		{
			return AuthenticationHelper.SendWithRequestAuthAsync(request, async, Settings._credentials, Settings._preAuthenticate, this, cancellationToken);
		}
		return SendWithProxyAuthAsync(request, async, doRequestAuth, cancellationToken);
	}

	private CancellationTokenSource GetConnectTimeoutCancellationTokenSource()
	{
		return new CancellationTokenSource(Settings._connectTimeout);
	}

	private async ValueTask<(Stream, TransportContext, IPEndPoint)> ConnectAsync(HttpRequestMessage request, bool async, CancellationToken cancellationToken)
	{
		Stream stream2 = null;
		IPEndPoint remoteEndPoint = null;
		switch (_kind)
		{
		case HttpConnectionKind.Http:
		case HttpConnectionKind.Https:
		case HttpConnectionKind.ProxyConnect:
			stream2 = await ConnectToTcpHostAsync(_originAuthority.IdnHost, _originAuthority.Port, request, async, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
			remoteEndPoint = GetRemoteEndPoint(stream2);
			if (_kind == HttpConnectionKind.ProxyConnect && _sslOptionsProxy != null)
			{
				stream2 = await ConnectHelper.EstablishSslConnectionAsync(_sslOptionsProxy, request, async, stream2, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
			}
			break;
		case HttpConnectionKind.Proxy:
			stream2 = await ConnectToTcpHostAsync(_proxyUri.IdnHost, _proxyUri.Port, request, async, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
			remoteEndPoint = GetRemoteEndPoint(stream2);
			if (_sslOptionsProxy != null)
			{
				stream2 = await ConnectHelper.EstablishSslConnectionAsync(_sslOptionsProxy, request, async, stream2, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
			}
			break;
		case HttpConnectionKind.ProxyTunnel:
		case HttpConnectionKind.SslProxyTunnel:
			stream2 = await EstablishProxyTunnelAsync(async, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
			if (stream2 is HttpContentStream httpContentStream)
			{
				Stream stream3 = httpContentStream._connection?._stream;
				if (stream3 != null)
				{
					remoteEndPoint = GetRemoteEndPoint(stream3);
				}
			}
			break;
		case HttpConnectionKind.SocksTunnel:
		case HttpConnectionKind.SslSocksTunnel:
			stream2 = await EstablishSocksTunnel(request, async, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
			remoteEndPoint = GetRemoteEndPoint(stream2);
			break;
		}
		TransportContext item = null;
		if (IsSecure)
		{
			SslStream sslStream = stream2 as SslStream;
			if (sslStream == null)
			{
				sslStream = await ConnectHelper.EstablishSslConnectionAsync(GetSslOptionsForRequest(request), request, async, stream2, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
			}
			else if (System.Net.NetEventSource.Log.IsEnabled())
			{
				Trace($"Connected with custom SslStream: alpn='${sslStream.NegotiatedApplicationProtocol}'", "ConnectAsync");
			}
			item = sslStream.TransportContext;
			stream2 = sslStream;
		}
		return (stream2, item, remoteEndPoint);
		static IPEndPoint GetRemoteEndPoint(Stream stream)
		{
			return (stream as NetworkStream)?.Socket?.RemoteEndPoint as IPEndPoint;
		}
	}

	private async ValueTask<Stream> ConnectToTcpHostAsync(string host, int port, HttpRequestMessage initialRequest, bool async, CancellationToken cancellationToken)
	{
		cancellationToken.ThrowIfCancellationRequested();
		DnsEndPoint endPoint = new DnsEndPoint(host, port);
		try
		{
			Stream result;
			if (Settings._connectCallback != null)
			{
				ValueTask<Stream> valueTask = Settings._connectCallback(new SocketsHttpConnectionContext(endPoint, initialRequest), cancellationToken);
				if (!async && !valueTask.IsCompleted)
				{
					Trace("ConnectCallback completing asynchronously for a synchronous request.", "ConnectToTcpHostAsync");
				}
				result = (await valueTask.ConfigureAwait(continueOnCapturedContext: false)) ?? throw new HttpRequestException(System.SR.net_http_null_from_connect_callback);
			}
			else
			{
				Socket socket = new Socket(SocketType.Stream, ProtocolType.Tcp)
				{
					NoDelay = true
				};
				try
				{
					if (async)
					{
						await socket.ConnectAsync(endPoint, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
					}
					else
					{
						using (cancellationToken.UnsafeRegister(delegate(object s)
						{
							((Socket)s).Dispose();
						}, socket))
						{
							socket.Connect(endPoint);
						}
					}
					result = new NetworkStream(socket, ownsSocket: true);
				}
				catch
				{
					socket.Dispose();
					throw;
				}
			}
			return result;
		}
		catch (Exception ex)
		{
			throw (ex is OperationCanceledException ex2 && ex2.CancellationToken == cancellationToken) ? CancellationHelper.CreateOperationCanceledException(null, cancellationToken) : ConnectHelper.CreateWrappedException(ex, endPoint.Host, endPoint.Port, cancellationToken);
		}
	}

	internal async ValueTask<HttpConnection> CreateHttp11ConnectionAsync(HttpRequestMessage request, bool async, CancellationToken cancellationToken)
	{
		var (stream, transportContext, remoteEndPoint) = await ConnectAsync(request, async, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
		return await ConstructHttp11ConnectionAsync(async, stream, transportContext, request, remoteEndPoint, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
	}

	private SslClientAuthenticationOptions GetSslOptionsForRequest(HttpRequestMessage request)
	{
		if (_http2Enabled)
		{
			if (request.Version.Major >= 2 && request.VersionPolicy != 0)
			{
				return _sslOptionsHttp2Only;
			}
			if (request.Version.Major >= 2 || request.VersionPolicy == HttpVersionPolicy.RequestVersionOrHigher)
			{
				return _sslOptionsHttp2;
			}
		}
		return _sslOptionsHttp11;
	}

	private async ValueTask<Stream> ApplyPlaintextFilterAsync(bool async, Stream stream, Version httpVersion, HttpRequestMessage request, CancellationToken cancellationToken)
	{
		if (Settings._plaintextStreamFilter == null)
		{
			return stream;
		}
		Stream stream2;
		try
		{
			ValueTask<Stream> valueTask = Settings._plaintextStreamFilter(new SocketsHttpPlaintextStreamFilterContext(stream, httpVersion, request), cancellationToken);
			if (!async && !valueTask.IsCompleted)
			{
				Trace("PlaintextStreamFilter completing asynchronously for a synchronous request.", "ApplyPlaintextFilterAsync");
			}
			stream2 = await valueTask.ConfigureAwait(continueOnCapturedContext: false);
		}
		catch (OperationCanceledException ex) when (ex.CancellationToken == cancellationToken)
		{
			stream.Dispose();
			throw;
		}
		catch (Exception inner)
		{
			stream.Dispose();
			throw new HttpRequestException(System.SR.net_http_exception_during_plaintext_filter, inner);
		}
		if (stream2 == null)
		{
			stream.Dispose();
			throw new HttpRequestException(System.SR.net_http_null_from_plaintext_filter);
		}
		return stream2;
	}

	private async ValueTask<HttpConnection> ConstructHttp11ConnectionAsync(bool async, Stream stream, TransportContext transportContext, HttpRequestMessage request, IPEndPoint remoteEndPoint, CancellationToken cancellationToken)
	{
		return new HttpConnection(this, await ApplyPlaintextFilterAsync(async, stream, HttpVersion.Version11, request, cancellationToken).ConfigureAwait(continueOnCapturedContext: false), transportContext, remoteEndPoint);
	}

	private async ValueTask<Http2Connection> ConstructHttp2ConnectionAsync(Stream stream, HttpRequestMessage request, IPEndPoint remoteEndPoint, CancellationToken cancellationToken)
	{
		stream = await ApplyPlaintextFilterAsync(async: true, stream, HttpVersion.Version20, request, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
		Http2Connection http2Connection = new Http2Connection(this, stream, remoteEndPoint);
		try
		{
			await http2Connection.SetupAsync(cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
			return http2Connection;
		}
		catch (Exception ex)
		{
			if (ex is OperationCanceledException ex2 && ex2.CancellationToken == cancellationToken)
			{
				throw;
			}
			throw new HttpRequestException(System.SR.net_http_client_execution_error, ex);
		}
	}

	private async ValueTask<Stream> EstablishProxyTunnelAsync(bool async, CancellationToken cancellationToken)
	{
		HttpRequestMessage httpRequestMessage = new HttpRequestMessage(HttpMethod.Connect, _proxyUri);
		httpRequestMessage.Headers.Host = $"{_originAuthority.IdnHost}:{_originAuthority.Port}";
		if (_connectTunnelUserAgent != null)
		{
			httpRequestMessage.Headers.TryAddWithoutValidation(KnownHeaders.UserAgent.Descriptor, _connectTunnelUserAgent);
		}
		HttpResponseMessage httpResponseMessage = await _poolManager.SendProxyConnectAsync(httpRequestMessage, _proxyUri, async, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
		if (httpResponseMessage.StatusCode != HttpStatusCode.OK)
		{
			httpResponseMessage.Dispose();
			throw new HttpRequestException(HttpRequestError.ProxyTunnelError, System.SR.Format(System.SR.net_http_proxy_tunnel_returned_failure_status_code, _proxyUri, (int)httpResponseMessage.StatusCode));
		}
		try
		{
			return httpResponseMessage.Content.ReadAsStream(cancellationToken);
		}
		catch
		{
			httpResponseMessage.Dispose();
			throw;
		}
	}

	private async ValueTask<Stream> EstablishSocksTunnel(HttpRequestMessage request, bool async, CancellationToken cancellationToken)
	{
		Stream stream = await ConnectToTcpHostAsync(_proxyUri.IdnHost, _proxyUri.Port, request, async, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
		try
		{
			await SocksHelper.EstablishSocksTunnelAsync(stream, _originAuthority.IdnHost, _originAuthority.Port, _proxyUri, ProxyCredentials, async, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
			return stream;
		}
		catch (Exception ex) when (!(ex is OperationCanceledException))
		{
			throw new HttpRequestException(HttpRequestError.ProxyTunnelError, System.SR.net_http_proxy_tunnel_error, ex);
		}
	}

	private void HandleHttp11ConnectionFailure(HttpConnectionWaiter<HttpConnection> requestWaiter, Exception e)
	{
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			Trace($"HTTP/1.1 connection failed: {e}", "HandleHttp11ConnectionFailure");
		}
		requestWaiter?.TrySetException(e);
		lock (SyncObj)
		{
			_associatedHttp11ConnectionCount--;
			_pendingHttp11ConnectionCount--;
			CheckForHttp11ConnectionInjection();
		}
	}

	private void HandleHttp2ConnectionFailure(HttpConnectionWaiter<Http2Connection> requestWaiter, Exception e)
	{
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			Trace($"HTTP2 connection failed: {e}", "HandleHttp2ConnectionFailure");
		}
		requestWaiter.TrySetException(e);
		lock (SyncObj)
		{
			_associatedHttp2ConnectionCount--;
			_pendingHttp2Connection = false;
			CheckForHttp2ConnectionInjection();
		}
	}

	public void InvalidateHttp11Connection(HttpConnection connection, bool disposing = true)
	{
		lock (SyncObj)
		{
			_associatedHttp11ConnectionCount--;
			CheckForHttp11ConnectionInjection();
		}
	}

	public void InvalidateHttp2Connection(Http2Connection connection)
	{
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			connection.Trace("", "InvalidateHttp2Connection");
		}
		bool flag = false;
		lock (SyncObj)
		{
			if (_availableHttp2Connections != null)
			{
				int num = _availableHttp2Connections.IndexOf(connection);
				if (num != -1)
				{
					flag = true;
					_availableHttp2Connections.RemoveAt(num);
					_associatedHttp2ConnectionCount--;
				}
			}
			CheckForHttp2ConnectionInjection();
		}
		if (flag)
		{
			connection.Dispose();
		}
	}

	private bool CheckExpirationOnReturn(HttpConnectionBase connection)
	{
		TimeSpan pooledConnectionLifetime = _poolManager.Settings._pooledConnectionLifetime;
		if (pooledConnectionLifetime != Timeout.InfiniteTimeSpan)
		{
			if (!(pooledConnectionLifetime == TimeSpan.Zero))
			{
				return (double)connection.GetLifetimeTicks(Environment.TickCount64) > pooledConnectionLifetime.TotalMilliseconds;
			}
			return true;
		}
		return false;
	}

	public void RecycleHttp11Connection(HttpConnection connection)
	{
		ReturnHttp11Connection(connection, isNewConnection: false);
	}

	private void ReturnHttp11Connection(HttpConnection connection, bool isNewConnection, HttpConnectionWaiter<HttpConnection> initialRequestWaiter = null)
	{
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			connection.Trace($"{"isNewConnection"}={isNewConnection}", "ReturnHttp11Connection");
		}
		if (!isNewConnection && CheckExpirationOnReturn(connection))
		{
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				connection.Trace("Disposing HTTP/1.1 connection return to pool. Connection lifetime expired.", "ReturnHttp11Connection");
			}
			connection.Dispose();
			return;
		}
		bool flag;
		while (true)
		{
			HttpConnectionWaiter<HttpConnection> waiter = null;
			flag = false;
			lock (SyncObj)
			{
				if (isNewConnection)
				{
					_pendingHttp11ConnectionCount--;
					isNewConnection = false;
				}
				if (initialRequestWaiter != null)
				{
					waiter = initialRequestWaiter;
					initialRequestWaiter = null;
					_http11RequestQueue.TryDequeueSpecificWaiter(waiter);
				}
				else if (!_http11RequestQueue.TryDequeueWaiter(this, out waiter) && !_disposed)
				{
					flag = true;
					connection.MarkConnectionAsIdle();
					_availableHttp11Connections.Add(connection);
				}
			}
			if (waiter == null)
			{
				break;
			}
			if (waiter.TrySetResult(connection))
			{
				if (System.Net.NetEventSource.Log.IsEnabled())
				{
					connection.Trace("Dequeued waiting HTTP/1.1 request.", "ReturnHttp11Connection");
				}
				return;
			}
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				Trace(waiter.Task.IsCanceled ? "Discarding canceled HTTP/1.1 request from queue." : "Discarding signaled HTTP/1.1 request waiter from queue.", "ReturnHttp11Connection");
			}
		}
		if (flag)
		{
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				connection.Trace("Put HTTP/1.1 connection in pool.", "ReturnHttp11Connection");
			}
			return;
		}
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			connection.Trace("Disposing HTTP/1.1 connection returned to pool. Pool was disposed.", "ReturnHttp11Connection");
		}
		connection.Dispose();
	}

	private void ReturnHttp2Connection(Http2Connection connection, bool isNewConnection, HttpConnectionWaiter<Http2Connection> initialRequestWaiter = null)
	{
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			connection.Trace($"{"isNewConnection"}={isNewConnection}", "ReturnHttp2Connection");
		}
		if (!isNewConnection && CheckExpirationOnReturn(connection))
		{
			lock (SyncObj)
			{
				_associatedHttp2ConnectionCount--;
			}
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				connection.Trace("Disposing HTTP2 connection return to pool. Connection lifetime expired.", "ReturnHttp2Connection");
			}
			connection.Dispose();
			return;
		}
		while (connection.TryReserveStream())
		{
			while (true)
			{
				HttpConnectionWaiter<Http2Connection> waiter = null;
				bool flag = false;
				lock (SyncObj)
				{
					if (isNewConnection)
					{
						_pendingHttp2Connection = false;
						isNewConnection = false;
					}
					if (initialRequestWaiter != null)
					{
						waiter = initialRequestWaiter;
						initialRequestWaiter = null;
						_http2RequestQueue.TryDequeueSpecificWaiter(waiter);
					}
					else if (!_http2RequestQueue.TryDequeueWaiter(this, out waiter))
					{
						if (_disposed)
						{
							_associatedHttp2ConnectionCount--;
						}
						else
						{
							flag = true;
							if (_availableHttp2Connections == null)
							{
								_availableHttp2Connections = new List<Http2Connection>();
							}
							_availableHttp2Connections.Add(connection);
						}
					}
				}
				if (waiter != null)
				{
					if (waiter.TrySetResult(connection))
					{
						break;
					}
					if (System.Net.NetEventSource.Log.IsEnabled())
					{
						Trace(waiter.Task.IsCanceled ? "Discarding canceled HTTP/2 request from queue." : "Discarding signaled HTTP/2 request waiter from queue.", "ReturnHttp2Connection");
					}
					continue;
				}
				connection.ReleaseStream();
				if (flag)
				{
					if (System.Net.NetEventSource.Log.IsEnabled())
					{
						connection.Trace("Put HTTP2 connection in pool.", "ReturnHttp2Connection");
					}
					return;
				}
				if (System.Net.NetEventSource.Log.IsEnabled())
				{
					connection.Trace("Disposing HTTP2 connection returned to pool. Pool was disposed.", "ReturnHttp2Connection");
				}
				connection.Dispose();
				return;
			}
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				connection.Trace("Dequeued waiting HTTP2 request.", "ReturnHttp2Connection");
			}
		}
		if (isNewConnection)
		{
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				connection.Trace("New HTTP2 connection is unusable due to no available streams.", "ReturnHttp2Connection");
			}
			connection.Dispose();
			HttpRequestException ex = new HttpRequestException(System.SR.net_http_http2_connection_not_established);
			ExceptionDispatchInfo.SetCurrentStackTrace(ex);
			HandleHttp2ConnectionFailure(initialRequestWaiter, ex);
		}
		else
		{
			lock (SyncObj)
			{
				CheckForHttp2ConnectionInjection();
			}
			DisableHttp2Connection(connection);
		}
	}

	private void DisableHttp2Connection(Http2Connection connection)
	{
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			connection.Trace("", "DisableHttp2Connection");
		}
		DisableHttp2ConnectionAsync(connection);
		async Task DisableHttp2ConnectionAsync(Http2Connection connection)
		{
			bool flag = await connection.WaitForAvailableStreamsAsync().ConfigureAwait(ConfigureAwaitOptions.ForceYielding);
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				connection.Trace($"{"WaitForAvailableStreamsAsync"} completed, {"usable"}={flag}", "DisableHttp2Connection");
			}
			if (flag)
			{
				ReturnHttp2Connection(connection, isNewConnection: false);
			}
			else
			{
				lock (SyncObj)
				{
					_associatedHttp2ConnectionCount--;
					CheckForHttp2ConnectionInjection();
				}
				if (System.Net.NetEventSource.Log.IsEnabled())
				{
					connection.Trace("HTTP2 connection no longer usable", "DisableHttp2Connection");
				}
				connection.Dispose();
			}
		}
	}

	public void InvalidateHttp3Connection(Http3Connection connection)
	{
		lock (SyncObj)
		{
			if (_http3Connection == connection)
			{
				_http3Connection = null;
			}
		}
	}

	public void Dispose()
	{
		List<HttpConnectionBase> list = null;
		lock (SyncObj)
		{
			if (!_disposed)
			{
				if (System.Net.NetEventSource.Log.IsEnabled())
				{
					Trace("Disposing pool.", "Dispose");
				}
				_disposed = true;
				list = new List<HttpConnectionBase>(_availableHttp11Connections.Count + (_availableHttp2Connections?.Count ?? 0));
				list.AddRange(_availableHttp11Connections);
				if (_availableHttp2Connections != null)
				{
					list.AddRange(_availableHttp2Connections);
				}
				_availableHttp11Connections.Clear();
				_associatedHttp2ConnectionCount -= _availableHttp2Connections?.Count ?? 0;
				_availableHttp2Connections?.Clear();
				if (_http3Connection != null)
				{
					list.Add(_http3Connection);
					_http3Connection = null;
				}
				if (_authorityExpireTimer != null)
				{
					_authorityExpireTimer.Dispose();
					_authorityExpireTimer = null;
				}
				if (_altSvcBlocklistTimerCancellation != null)
				{
					_altSvcBlocklistTimerCancellation.Cancel();
					_altSvcBlocklistTimerCancellation.Dispose();
					_altSvcBlocklistTimerCancellation = null;
				}
			}
		}
		list?.ForEach(delegate(HttpConnectionBase c)
		{
			c.Dispose();
		});
	}

	public bool CleanCacheAndDisposeIfUnused()
	{
		TimeSpan pooledConnectionLifetime2 = _poolManager.Settings._pooledConnectionLifetime;
		TimeSpan pooledConnectionIdleTimeout2 = _poolManager.Settings._pooledConnectionIdleTimeout;
		long tickCount = Environment.TickCount64;
		List<HttpConnectionBase> toDispose2 = null;
		lock (SyncObj)
		{
			if (!_usedSinceLastCleanup && _associatedHttp11ConnectionCount == 0 && _associatedHttp2ConnectionCount == 0)
			{
				_disposed = true;
				return true;
			}
			_usedSinceLastCleanup = false;
			ScavengeConnectionList<HttpConnection>(_availableHttp11Connections, ref toDispose2, tickCount, pooledConnectionLifetime2, pooledConnectionIdleTimeout2);
			if (_availableHttp2Connections != null)
			{
				int num = ScavengeConnectionList<Http2Connection>(_availableHttp2Connections, ref toDispose2, tickCount, pooledConnectionLifetime2, pooledConnectionIdleTimeout2);
				_associatedHttp2ConnectionCount -= num;
			}
		}
		if (toDispose2 != null)
		{
			Task.Factory.StartNew(delegate(object s)
			{
				((List<HttpConnectionBase>)s).ForEach(delegate(HttpConnectionBase c)
				{
					c.Dispose();
				});
			}, toDispose2, CancellationToken.None, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
		}
		return false;
		static bool IsUsableConnection(HttpConnectionBase connection, long nowTicks, TimeSpan pooledConnectionLifetime, TimeSpan pooledConnectionIdleTimeout)
		{
			if (pooledConnectionIdleTimeout != Timeout.InfiniteTimeSpan)
			{
				long idleTicks = connection.GetIdleTicks(nowTicks);
				if ((double)idleTicks > pooledConnectionIdleTimeout.TotalMilliseconds)
				{
					if (System.Net.NetEventSource.Log.IsEnabled())
					{
						connection.Trace($"Scavenging connection. Idle {TimeSpan.FromMilliseconds(idleTicks)} > {pooledConnectionIdleTimeout}.", "CleanCacheAndDisposeIfUnused");
					}
					return false;
				}
			}
			if (pooledConnectionLifetime != Timeout.InfiniteTimeSpan)
			{
				long lifetimeTicks = connection.GetLifetimeTicks(nowTicks);
				if ((double)lifetimeTicks > pooledConnectionLifetime.TotalMilliseconds)
				{
					if (System.Net.NetEventSource.Log.IsEnabled())
					{
						connection.Trace($"Scavenging connection. Lifetime {TimeSpan.FromMilliseconds(lifetimeTicks)} > {pooledConnectionLifetime}.", "CleanCacheAndDisposeIfUnused");
					}
					return false;
				}
			}
			if (!connection.CheckUsabilityOnScavenge())
			{
				if (System.Net.NetEventSource.Log.IsEnabled())
				{
					connection.Trace("Scavenging connection. Keep-Alive timeout exceeded, unexpected data or EOF received.", "CleanCacheAndDisposeIfUnused");
				}
				return false;
			}
			return true;
		}
		static int ScavengeConnectionList<T>(List<T> list, ref List<HttpConnectionBase> toDispose, long nowTicks, TimeSpan pooledConnectionLifetime, TimeSpan pooledConnectionIdleTimeout) where T : HttpConnectionBase
		{
			int i;
			for (i = 0; i < list.Count && IsUsableConnection(list[i], nowTicks, pooledConnectionLifetime, pooledConnectionIdleTimeout); i++)
			{
			}
			int num2 = 0;
			if (i < list.Count)
			{
				if (toDispose == null)
				{
					toDispose = new List<HttpConnectionBase>();
				}
				toDispose.Add(list[i]);
				int j = i + 1;
				while (j < list.Count)
				{
					for (; j < list.Count && !IsUsableConnection(list[j], nowTicks, pooledConnectionLifetime, pooledConnectionIdleTimeout); j++)
					{
						toDispose.Add(list[j]);
					}
					if (j < list.Count)
					{
						list[i++] = list[j++];
					}
				}
				num2 = list.Count - i;
				list.RemoveRange(i, num2);
			}
			return num2;
		}
	}

	private static bool GetIsWindows7Or2008R2()
	{
		OperatingSystem oSVersion = Environment.OSVersion;
		if (oSVersion.Platform == PlatformID.Win32NT)
		{
			Version version = oSVersion.Version;
			if (version.Major == 6)
			{
				return version.Minor == 1;
			}
			return false;
		}
		return false;
	}

	internal void HeartBeat()
	{
		Http2Connection[] array;
		lock (SyncObj)
		{
			array = _availableHttp2Connections?.ToArray();
		}
		if (array != null)
		{
			Http2Connection[] array2 = array;
			foreach (Http2Connection http2Connection in array2)
			{
				http2Connection.HeartBeat();
			}
		}
	}

	public override string ToString()
	{
		return "HttpConnectionPool " + ((!(_proxyUri == null)) ? ((_sslOptionsHttp11 == null) ? $"Proxy {_proxyUri}" : ($"https://{_originAuthority}/ tunnelled via Proxy {_proxyUri}" + ((_sslOptionsHttp11.TargetHost != _originAuthority.IdnHost) ? (", SSL TargetHost=" + _sslOptionsHttp11.TargetHost) : null))) : ((_sslOptionsHttp11 == null) ? $"http://{_originAuthority}" : ($"https://{_originAuthority}" + ((_sslOptionsHttp11.TargetHost != _originAuthority.IdnHost) ? (", SSL TargetHost=" + _sslOptionsHttp11.TargetHost) : null))));
	}

	private void Trace(string message, [CallerMemberName] string memberName = null)
	{
		System.Net.NetEventSource.Log.HandlerMessage(GetHashCode(), 0, 0, memberName, message);
	}
}
