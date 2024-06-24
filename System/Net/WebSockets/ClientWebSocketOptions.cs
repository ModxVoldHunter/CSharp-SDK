using System.Collections.Generic;
using System.Net.Http;
using System.Net.Security;
using System.Runtime.Versioning;
using System.Security.Cryptography.X509Certificates;
using System.Threading;

namespace System.Net.WebSockets;

public sealed class ClientWebSocketOptions
{
	private bool _isReadOnly;

	private TimeSpan _keepAliveInterval = WebSocket.DefaultKeepAliveInterval;

	private bool _useDefaultCredentials;

	private ICredentials _credentials;

	private IWebProxy _proxy;

	private CookieContainer _cookies;

	private int _receiveBufferSize = 4096;

	private ArraySegment<byte>? _buffer;

	private RemoteCertificateValidationCallback _remoteCertificateValidationCallback;

	internal X509CertificateCollection _clientCertificates;

	internal WebHeaderCollection _requestHeaders;

	internal List<string> _requestedSubProtocols;

	private Version _version = System.Net.HttpVersion.Version11;

	private HttpVersionPolicy _versionPolicy;

	private bool _collectHttpResponseDetails;

	public Version HttpVersion
	{
		get
		{
			return _version;
		}
		[UnsupportedOSPlatform("browser")]
		set
		{
			ThrowIfReadOnly();
			ArgumentNullException.ThrowIfNull(value, "value");
			_version = value;
		}
	}

	public HttpVersionPolicy HttpVersionPolicy
	{
		get
		{
			return _versionPolicy;
		}
		[UnsupportedOSPlatform("browser")]
		set
		{
			ThrowIfReadOnly();
			_versionPolicy = value;
		}
	}

	internal WebHeaderCollection RequestHeaders => _requestHeaders ?? (_requestHeaders = new WebHeaderCollection());

	internal List<string> RequestedSubProtocols => _requestedSubProtocols ?? (_requestedSubProtocols = new List<string>());

	[UnsupportedOSPlatform("browser")]
	public bool UseDefaultCredentials
	{
		get
		{
			return _useDefaultCredentials;
		}
		set
		{
			ThrowIfReadOnly();
			_useDefaultCredentials = value;
		}
	}

	[UnsupportedOSPlatform("browser")]
	public ICredentials? Credentials
	{
		get
		{
			return _credentials;
		}
		set
		{
			ThrowIfReadOnly();
			_credentials = value;
		}
	}

	[UnsupportedOSPlatform("browser")]
	public IWebProxy? Proxy
	{
		get
		{
			return _proxy;
		}
		set
		{
			ThrowIfReadOnly();
			_proxy = value;
		}
	}

	[UnsupportedOSPlatform("browser")]
	public X509CertificateCollection ClientCertificates
	{
		get
		{
			return _clientCertificates ?? (_clientCertificates = new X509CertificateCollection());
		}
		set
		{
			ThrowIfReadOnly();
			ArgumentNullException.ThrowIfNull(value, "value");
			_clientCertificates = value;
		}
	}

	[UnsupportedOSPlatform("browser")]
	public RemoteCertificateValidationCallback? RemoteCertificateValidationCallback
	{
		get
		{
			return _remoteCertificateValidationCallback;
		}
		set
		{
			ThrowIfReadOnly();
			_remoteCertificateValidationCallback = value;
		}
	}

	[UnsupportedOSPlatform("browser")]
	public CookieContainer? Cookies
	{
		get
		{
			return _cookies;
		}
		set
		{
			ThrowIfReadOnly();
			_cookies = value;
		}
	}

	[UnsupportedOSPlatform("browser")]
	public TimeSpan KeepAliveInterval
	{
		get
		{
			return _keepAliveInterval;
		}
		set
		{
			ThrowIfReadOnly();
			if (value != Timeout.InfiniteTimeSpan && value < TimeSpan.Zero)
			{
				throw new ArgumentOutOfRangeException("value", value, System.SR.Format(System.SR.net_WebSockets_ArgumentOutOfRange_TooSmall, Timeout.InfiniteTimeSpan.ToString()));
			}
			_keepAliveInterval = value;
		}
	}

	[UnsupportedOSPlatform("browser")]
	public WebSocketDeflateOptions? DangerousDeflateOptions { get; set; }

	[UnsupportedOSPlatform("browser")]
	public bool CollectHttpResponseDetails
	{
		get
		{
			return _collectHttpResponseDetails;
		}
		set
		{
			ThrowIfReadOnly();
			_collectHttpResponseDetails = value;
		}
	}

	internal bool AreCompatibleWithCustomInvoker()
	{
		if (!UseDefaultCredentials && Credentials == null)
		{
			X509CertificateCollection clientCertificates = _clientCertificates;
			if ((clientCertificates == null || clientCertificates.Count == 0) && RemoteCertificateValidationCallback == null && Cookies == null)
			{
				if (Proxy != null)
				{
					return Proxy == WebSocketHandle.DefaultWebProxy.Instance;
				}
				return true;
			}
		}
		return false;
	}

	internal ClientWebSocketOptions()
	{
	}

	[UnsupportedOSPlatform("browser")]
	public void SetRequestHeader(string headerName, string? headerValue)
	{
		ThrowIfReadOnly();
		RequestHeaders.Set(headerName, headerValue);
	}

	public void AddSubProtocol(string subProtocol)
	{
		ThrowIfReadOnly();
		System.Net.WebSockets.WebSocketValidate.ValidateSubprotocol(subProtocol);
		List<string> requestedSubProtocols = RequestedSubProtocols;
		foreach (string item in requestedSubProtocols)
		{
			if (string.Equals(item, subProtocol, StringComparison.OrdinalIgnoreCase))
			{
				throw new ArgumentException(System.SR.Format(System.SR.net_WebSockets_NoDuplicateProtocol, subProtocol), "subProtocol");
			}
		}
		requestedSubProtocols.Add(subProtocol);
	}

	[UnsupportedOSPlatform("browser")]
	public void SetBuffer(int receiveBufferSize, int sendBufferSize)
	{
		ThrowIfReadOnly();
		ArgumentOutOfRangeException.ThrowIfNegativeOrZero(receiveBufferSize, "receiveBufferSize");
		ArgumentOutOfRangeException.ThrowIfNegativeOrZero(sendBufferSize, "sendBufferSize");
		_receiveBufferSize = receiveBufferSize;
		_buffer = null;
	}

	[UnsupportedOSPlatform("browser")]
	public void SetBuffer(int receiveBufferSize, int sendBufferSize, ArraySegment<byte> buffer)
	{
		ThrowIfReadOnly();
		ArgumentOutOfRangeException.ThrowIfNegativeOrZero(receiveBufferSize, "receiveBufferSize");
		ArgumentOutOfRangeException.ThrowIfNegativeOrZero(sendBufferSize, "sendBufferSize");
		System.Net.WebSockets.WebSocketValidate.ValidateArraySegment(buffer, "buffer");
		ArgumentOutOfRangeException.ThrowIfZero(buffer.Count, "buffer");
		_receiveBufferSize = receiveBufferSize;
		_buffer = buffer;
	}

	internal void SetToReadOnly()
	{
		_isReadOnly = true;
	}

	private void ThrowIfReadOnly()
	{
		if (_isReadOnly)
		{
			throw new InvalidOperationException(System.SR.net_WebSockets_AlreadyStarted);
		}
	}
}
