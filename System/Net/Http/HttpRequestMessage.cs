using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;

namespace System.Net.Http;

public class HttpRequestMessage : IDisposable
{
	private int _sendStatus;

	private HttpMethod _method;

	private Uri _requestUri;

	private HttpRequestHeaders _headers;

	private Version _version;

	private HttpVersionPolicy _versionPolicy;

	private HttpContent _content;

	internal HttpRequestOptions _options;

	internal static Version DefaultRequestVersion => HttpVersion.Version11;

	public Version Version
	{
		get
		{
			return _version;
		}
		set
		{
			ArgumentNullException.ThrowIfNull(value, "value");
			CheckDisposed();
			_version = value;
		}
	}

	public HttpVersionPolicy VersionPolicy
	{
		get
		{
			return _versionPolicy;
		}
		set
		{
			CheckDisposed();
			_versionPolicy = value;
		}
	}

	public HttpContent? Content
	{
		get
		{
			return _content;
		}
		set
		{
			CheckDisposed();
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				if (value == null)
				{
					System.Net.NetEventSource.ContentNull(this);
				}
				else
				{
					System.Net.NetEventSource.Associate(this, value, "Content");
				}
			}
			_content = value;
		}
	}

	public HttpMethod Method
	{
		get
		{
			return _method;
		}
		set
		{
			ArgumentNullException.ThrowIfNull(value, "value");
			CheckDisposed();
			_method = value;
		}
	}

	public Uri? RequestUri
	{
		get
		{
			return _requestUri;
		}
		set
		{
			CheckDisposed();
			_requestUri = value;
		}
	}

	public HttpRequestHeaders Headers => _headers ?? (_headers = new HttpRequestHeaders());

	internal bool HasHeaders => _headers != null;

	[Obsolete("HttpRequestMessage.Properties has been deprecated. Use Options instead.")]
	public IDictionary<string, object?> Properties => Options;

	public HttpRequestOptions Options => _options ?? (_options = new HttpRequestOptions());

	private bool Disposed
	{
		get
		{
			return (_sendStatus & 4) != 0;
		}
		set
		{
			_sendStatus |= 4;
		}
	}

	internal bool IsExtendedConnectRequest
	{
		get
		{
			if (Method == HttpMethod.Connect)
			{
				return _headers?.Protocol != null;
			}
			return false;
		}
	}

	public HttpRequestMessage()
		: this(HttpMethod.Get, (Uri?)null)
	{
	}

	public HttpRequestMessage(HttpMethod method, Uri? requestUri)
	{
		ArgumentNullException.ThrowIfNull(method, "method");
		_method = method;
		_requestUri = requestUri;
		_version = DefaultRequestVersion;
		_versionPolicy = HttpVersionPolicy.RequestVersionOrLower;
	}

	public HttpRequestMessage(HttpMethod method, [StringSyntax("Uri")] string? requestUri)
		: this(method, string.IsNullOrEmpty(requestUri) ? null : new Uri(requestUri, UriKind.RelativeOrAbsolute))
	{
	}

	public override string ToString()
	{
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.Append("Method: ");
		stringBuilder.Append(_method);
		stringBuilder.Append(", RequestUri: '");
		if ((object)_requestUri == null)
		{
			stringBuilder.Append("<null>");
		}
		else
		{
			StringBuilder stringBuilder2 = stringBuilder;
			StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(0, 1, stringBuilder2);
			handler.AppendFormatted(_requestUri);
			stringBuilder2.Append(ref handler);
		}
		stringBuilder.Append("', Version: ");
		stringBuilder.Append(_version);
		stringBuilder.Append(", Content: ");
		stringBuilder.Append((_content == null) ? "<null>" : _content.GetType().ToString());
		stringBuilder.AppendLine(", Headers:");
		HeaderUtilities.DumpHeaders(stringBuilder, _headers, _content?.Headers);
		return stringBuilder.ToString();
	}

	internal bool MarkAsSent()
	{
		return Interlocked.CompareExchange(ref _sendStatus, 1, 0) == 0;
	}

	internal bool WasSentByHttpClient()
	{
		return (_sendStatus & 1) != 0;
	}

	internal void MarkAsRedirected()
	{
		_sendStatus |= 2;
	}

	internal bool WasRedirected()
	{
		return (_sendStatus & 2) != 0;
	}

	protected virtual void Dispose(bool disposing)
	{
		if (disposing && !Disposed)
		{
			Disposed = true;
			_content?.Dispose();
		}
	}

	public void Dispose()
	{
		Dispose(disposing: true);
		GC.SuppressFinalize(this);
	}

	private void CheckDisposed()
	{
		ObjectDisposedException.ThrowIf(Disposed, this);
	}
}
