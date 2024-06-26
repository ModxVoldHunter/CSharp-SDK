using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Net.Http.Headers;
using System.Net.Http.Metrics;
using System.Net.Security;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Http;

internal abstract class HttpConnectionBase : IDisposable, IHttpTrace
{
	private static long s_connectionCounter = -1L;

	private readonly ConnectionMetrics _connectionMetrics;

	private readonly bool _httpTelemetryMarkedConnectionAsOpened;

	private readonly long _creationTickCount = Environment.TickCount64;

	private long _idleSinceTickCount;

	private string _lastDateHeaderValue;

	private string _lastServerHeaderValue;

	public long Id { get; } = Interlocked.Increment(ref s_connectionCounter);


	public HttpConnectionBase(HttpConnectionPool pool, IPEndPoint remoteEndPoint)
	{
		SocketsHttpHandlerMetrics metrics = pool.Settings._metrics;
		if (metrics.OpenConnections.Enabled || metrics.ConnectionDuration.Enabled)
		{
			_connectionMetrics = new ConnectionMetrics(metrics, (this is HttpConnection) ? "1.1" : ((this is Http2Connection) ? "2" : "3"), pool.IsSecure ? "https" : "http", pool.OriginAuthority.HostValue, pool.IsDefaultPort ? null : new int?(pool.OriginAuthority.Port), remoteEndPoint?.Address?.ToString());
			_connectionMetrics.ConnectionEstablished();
		}
		_idleSinceTickCount = _creationTickCount;
		if (HttpTelemetry.Log.IsEnabled())
		{
			_httpTelemetryMarkedConnectionAsOpened = true;
			string scheme = (pool.IsSecure ? "https" : "http");
			string hostValue = pool.OriginAuthority.HostValue;
			int port = pool.OriginAuthority.Port;
			if (this is HttpConnection)
			{
				HttpTelemetry.Log.Http11ConnectionEstablished(Id, scheme, hostValue, port, remoteEndPoint);
			}
			else if (this is Http2Connection)
			{
				HttpTelemetry.Log.Http20ConnectionEstablished(Id, scheme, hostValue, port, remoteEndPoint);
			}
			else
			{
				HttpTelemetry.Log.Http30ConnectionEstablished(Id, scheme, hostValue, port, remoteEndPoint);
			}
		}
	}

	public void MarkConnectionAsClosed()
	{
		_connectionMetrics?.ConnectionClosed(Environment.TickCount64 - _creationTickCount);
		if (HttpTelemetry.Log.IsEnabled() && _httpTelemetryMarkedConnectionAsOpened)
		{
			if (this is HttpConnection)
			{
				HttpTelemetry.Log.Http11ConnectionClosed(Id);
			}
			else if (this is Http2Connection)
			{
				HttpTelemetry.Log.Http20ConnectionClosed(Id);
			}
			else
			{
				HttpTelemetry.Log.Http30ConnectionClosed(Id);
			}
		}
	}

	public void MarkConnectionAsIdle()
	{
		_idleSinceTickCount = Environment.TickCount64;
		_connectionMetrics?.IdleStateChanged(idle: true);
	}

	public void MarkConnectionAsNotIdle()
	{
		_connectionMetrics?.IdleStateChanged(idle: false);
	}

	public string GetResponseHeaderValueWithCaching(HeaderDescriptor descriptor, ReadOnlySpan<byte> value, Encoding valueEncoding)
	{
		if (!descriptor.Equals(KnownHeaders.Date))
		{
			if (!descriptor.Equals(KnownHeaders.Server))
			{
				return descriptor.GetHeaderValue(value, valueEncoding);
			}
			return GetOrAddCachedValue(ref _lastServerHeaderValue, descriptor, value, valueEncoding);
		}
		return GetOrAddCachedValue(ref _lastDateHeaderValue, descriptor, value, valueEncoding);
		static string GetOrAddCachedValue([NotNull] ref string cache, HeaderDescriptor descriptor, ReadOnlySpan<byte> value, Encoding encoding)
		{
			string text = cache;
			if (text == null || !Ascii.Equals(value, text))
			{
				text = (cache = descriptor.GetHeaderValue(value, encoding));
			}
			return text;
		}
	}

	public abstract void Trace(string message, [CallerMemberName] string memberName = null);

	protected void TraceConnection(Stream stream)
	{
		if (stream is SslStream sslStream)
		{
			Trace($"{this}. Id:{Id}, SslProtocol:{sslStream.SslProtocol}, NegotiatedApplicationProtocol:{sslStream.NegotiatedApplicationProtocol}, NegotiatedCipherSuite:{sslStream.NegotiatedCipherSuite}, CipherAlgorithm:{sslStream.CipherAlgorithm}, CipherStrength:{sslStream.CipherStrength}, HashAlgorithm:{sslStream.HashAlgorithm}, HashStrength:{sslStream.HashStrength}, KeyExchangeAlgorithm:{sslStream.KeyExchangeAlgorithm}, KeyExchangeStrength:{sslStream.KeyExchangeStrength}, LocalCertificate:{sslStream.LocalCertificate}, RemoteCertificate:{sslStream.RemoteCertificate}", "TraceConnection");
		}
		else
		{
			Trace($"{this}. Id:{Id}", "TraceConnection");
		}
	}

	public long GetLifetimeTicks(long nowTicks)
	{
		return nowTicks - _creationTickCount;
	}

	public virtual long GetIdleTicks(long nowTicks)
	{
		return nowTicks - _idleSinceTickCount;
	}

	public virtual bool CheckUsabilityOnScavenge()
	{
		return true;
	}

	internal static bool IsDigit(byte c)
	{
		return (uint)(c - 48) <= 9u;
	}

	internal static int ParseStatusCode(ReadOnlySpan<byte> value)
	{
		byte b;
		byte b2;
		byte b3;
		if (value.Length != 3 || !IsDigit(b = value[0]) || !IsDigit(b2 = value[1]) || !IsDigit(b3 = value[2]))
		{
			throw new HttpRequestException(HttpRequestError.InvalidResponse, System.SR.Format(System.SR.net_http_invalid_response_status_code, Encoding.ASCII.GetString(value)));
		}
		return 100 * (b - 48) + 10 * (b2 - 48) + (b3 - 48);
	}

	internal void LogExceptions(Task task)
	{
		if (task.IsCompleted)
		{
			if (task.IsFaulted)
			{
				LogFaulted(this, task);
			}
		}
		else
		{
			task.ContinueWith(delegate(Task t, object state)
			{
				LogFaulted((HttpConnectionBase)state, t);
			}, this, CancellationToken.None, TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
		}
		static void LogFaulted(HttpConnectionBase connection, Task task)
		{
			Exception innerException = task.Exception.InnerException;
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				connection.Trace($"Exception from asynchronous processing: {innerException}", "LogExceptions");
			}
		}
	}

	public abstract void Dispose();
}
