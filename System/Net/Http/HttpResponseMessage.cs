using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Net.Http.Headers;
using System.Text;

namespace System.Net.Http;

public class HttpResponseMessage : IDisposable
{
	private HttpStatusCode _statusCode;

	private HttpResponseHeaders _headers;

	private HttpResponseHeaders _trailingHeaders;

	private string _reasonPhrase;

	private HttpRequestMessage _requestMessage;

	private Version _version;

	private HttpContent _content;

	private bool _disposed;

	private static Version DefaultResponseVersion => HttpVersion.Version11;

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

	public HttpContent Content
	{
		get
		{
			return _content ?? (_content = new EmptyContent());
		}
		[param: AllowNull]
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

	public HttpStatusCode StatusCode
	{
		get
		{
			return _statusCode;
		}
		set
		{
			ArgumentOutOfRangeException.ThrowIfNegative((int)value, "value");
			ArgumentOutOfRangeException.ThrowIfGreaterThan((int)value, 999, "value");
			CheckDisposed();
			_statusCode = value;
		}
	}

	public string? ReasonPhrase
	{
		get
		{
			if (_reasonPhrase != null)
			{
				return _reasonPhrase;
			}
			return HttpStatusDescription.Get(StatusCode);
		}
		set
		{
			if (value != null && HttpRuleParser.ContainsNewLine(value))
			{
				throw new FormatException(System.SR.net_http_reasonphrase_format_error);
			}
			CheckDisposed();
			_reasonPhrase = value;
		}
	}

	public HttpResponseHeaders Headers => _headers ?? (_headers = new HttpResponseHeaders());

	public HttpResponseHeaders TrailingHeaders => _trailingHeaders ?? (_trailingHeaders = new HttpResponseHeaders(containsTrailingHeaders: true));

	public HttpRequestMessage? RequestMessage
	{
		get
		{
			return _requestMessage;
		}
		set
		{
			CheckDisposed();
			if (value != null && System.Net.NetEventSource.Log.IsEnabled())
			{
				System.Net.NetEventSource.Associate(this, value, "RequestMessage");
			}
			_requestMessage = value;
		}
	}

	public bool IsSuccessStatusCode
	{
		get
		{
			if (_statusCode >= HttpStatusCode.OK)
			{
				return _statusCode <= (HttpStatusCode)299;
			}
			return false;
		}
	}

	internal void SetVersionWithoutValidation(Version value)
	{
		_version = value;
	}

	internal void SetStatusCodeWithoutValidation(HttpStatusCode value)
	{
		_statusCode = value;
	}

	internal void SetReasonPhraseWithoutValidation(string value)
	{
		_reasonPhrase = value;
	}

	internal void StoreReceivedTrailingHeaders(HttpResponseHeaders headers)
	{
		if (_trailingHeaders == null)
		{
			_trailingHeaders = headers;
		}
		else
		{
			_trailingHeaders.AddHeaders(headers);
		}
	}

	public HttpResponseMessage()
		: this(HttpStatusCode.OK)
	{
	}

	public HttpResponseMessage(HttpStatusCode statusCode)
	{
		ArgumentOutOfRangeException.ThrowIfNegative((int)statusCode, "statusCode");
		ArgumentOutOfRangeException.ThrowIfGreaterThan((int)statusCode, 999, "statusCode");
		_statusCode = statusCode;
		_version = DefaultResponseVersion;
	}

	public HttpResponseMessage EnsureSuccessStatusCode()
	{
		if (!IsSuccessStatusCode)
		{
			throw new HttpRequestException(System.SR.Format(CultureInfo.InvariantCulture, string.IsNullOrWhiteSpace(ReasonPhrase) ? System.SR.net_http_message_not_success_statuscode : System.SR.net_http_message_not_success_statuscode_reason, (int)_statusCode, ReasonPhrase), null, _statusCode);
		}
		return this;
	}

	public override string ToString()
	{
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.Append("StatusCode: ");
		stringBuilder.Append((int)_statusCode);
		stringBuilder.Append(", ReasonPhrase: '");
		stringBuilder.Append(ReasonPhrase ?? "<null>");
		stringBuilder.Append("', Version: ");
		stringBuilder.Append(_version);
		stringBuilder.Append(", Content: ");
		stringBuilder.Append((_content == null) ? "<null>" : _content.GetType().ToString());
		stringBuilder.AppendLine(", Headers:");
		HeaderUtilities.DumpHeaders(stringBuilder, _headers, _content?.Headers);
		if (_trailingHeaders != null)
		{
			stringBuilder.AppendLine(", Trailing Headers:");
			HeaderUtilities.DumpHeaders(stringBuilder, _trailingHeaders);
		}
		return stringBuilder.ToString();
	}

	protected virtual void Dispose(bool disposing)
	{
		if (disposing && !_disposed)
		{
			_disposed = true;
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
		ObjectDisposedException.ThrowIf(_disposed, this);
	}
}
