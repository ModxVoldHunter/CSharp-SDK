namespace System.Net.Http.Headers;

internal sealed class HttpGeneralHeaders
{
	private HttpHeaderValueCollection<string> _connection;

	private HttpHeaderValueCollection<string> _trailer;

	private HttpHeaderValueCollection<TransferCodingHeaderValue> _transferEncoding;

	private HttpHeaderValueCollection<ProductHeaderValue> _upgrade;

	private HttpHeaderValueCollection<ViaHeaderValue> _via;

	private HttpHeaderValueCollection<WarningHeaderValue> _warning;

	private HttpHeaderValueCollection<NameValueHeaderValue> _pragma;

	private readonly HttpHeaders _parent;

	private bool _transferEncodingChunkedSet;

	private bool _connectionCloseSet;

	public CacheControlHeaderValue CacheControl
	{
		get
		{
			return (CacheControlHeaderValue)_parent.GetSingleParsedValue(KnownHeaders.CacheControl.Descriptor);
		}
		set
		{
			_parent.SetOrRemoveParsedValue(KnownHeaders.CacheControl.Descriptor, value);
		}
	}

	public bool? ConnectionClose
	{
		get
		{
			return GetConnectionClose(_parent, this);
		}
		set
		{
			if (value == true)
			{
				_connectionCloseSet = true;
				if (!_parent.ContainsParsedValue(KnownHeaders.Connection.Descriptor, "close"))
				{
					_parent.AddParsedValue(KnownHeaders.Connection.Descriptor, "close");
				}
			}
			else
			{
				_connectionCloseSet = value.HasValue;
				_parent.RemoveParsedValue(KnownHeaders.Connection.Descriptor, "close");
			}
		}
	}

	public DateTimeOffset? Date
	{
		get
		{
			return HeaderUtilities.GetDateTimeOffsetValue(KnownHeaders.Date.Descriptor, _parent);
		}
		set
		{
			_parent.SetOrRemoveParsedValue(KnownHeaders.Date.Descriptor, value);
		}
	}

	public HttpHeaderValueCollection<NameValueHeaderValue> Pragma => _pragma ?? (_pragma = new HttpHeaderValueCollection<NameValueHeaderValue>(KnownHeaders.Pragma.Descriptor, _parent));

	public HttpHeaderValueCollection<string> Trailer => _trailer ?? (_trailer = new HttpHeaderValueCollection<string>(KnownHeaders.Trailer.Descriptor, _parent));

	public bool? TransferEncodingChunked
	{
		get
		{
			return GetTransferEncodingChunked(_parent, this);
		}
		set
		{
			if (value == true)
			{
				_transferEncodingChunkedSet = true;
				if (!_parent.ContainsParsedValue(KnownHeaders.TransferEncoding.Descriptor, HeaderUtilities.TransferEncodingChunked))
				{
					_parent.AddParsedValue(KnownHeaders.TransferEncoding.Descriptor, HeaderUtilities.TransferEncodingChunked);
				}
			}
			else
			{
				_transferEncodingChunkedSet = value.HasValue;
				_parent.RemoveParsedValue(KnownHeaders.TransferEncoding.Descriptor, HeaderUtilities.TransferEncodingChunked);
			}
		}
	}

	public HttpHeaderValueCollection<ProductHeaderValue> Upgrade => _upgrade ?? (_upgrade = new HttpHeaderValueCollection<ProductHeaderValue>(KnownHeaders.Upgrade.Descriptor, _parent));

	public HttpHeaderValueCollection<ViaHeaderValue> Via => _via ?? (_via = new HttpHeaderValueCollection<ViaHeaderValue>(KnownHeaders.Via.Descriptor, _parent));

	public HttpHeaderValueCollection<WarningHeaderValue> Warning => _warning ?? (_warning = new HttpHeaderValueCollection<WarningHeaderValue>(KnownHeaders.Warning.Descriptor, _parent));

	public HttpHeaderValueCollection<string> Connection => _connection ?? (_connection = new HttpHeaderValueCollection<string>(KnownHeaders.Connection.Descriptor, _parent));

	public HttpHeaderValueCollection<TransferCodingHeaderValue> TransferEncoding => _transferEncoding ?? (_transferEncoding = new HttpHeaderValueCollection<TransferCodingHeaderValue>(KnownHeaders.TransferEncoding.Descriptor, _parent));

	internal static bool? GetConnectionClose(HttpHeaders parent, HttpGeneralHeaders headers)
	{
		if (parent.ContainsParsedValue(KnownHeaders.Connection.Descriptor, "close"))
		{
			return true;
		}
		if (headers != null && headers._connectionCloseSet)
		{
			return false;
		}
		return null;
	}

	internal static bool? GetTransferEncodingChunked(HttpHeaders parent, HttpGeneralHeaders headers)
	{
		if (parent.TryGetHeaderValue(KnownHeaders.TransferEncoding.Descriptor, out var value))
		{
			if (value is string text && text.Equals("chunked", StringComparison.OrdinalIgnoreCase))
			{
				return true;
			}
			if (parent.ContainsParsedValue(KnownHeaders.TransferEncoding.Descriptor, HeaderUtilities.TransferEncodingChunked))
			{
				return true;
			}
		}
		if (headers != null && headers._transferEncodingChunkedSet)
		{
			return false;
		}
		return null;
	}

	internal HttpGeneralHeaders(HttpHeaders parent)
	{
		_parent = parent;
	}

	internal void AddSpecialsFrom(HttpGeneralHeaders sourceHeaders)
	{
		if (!TransferEncodingChunked.HasValue)
		{
			TransferEncodingChunked = sourceHeaders.TransferEncodingChunked;
		}
		if (!ConnectionClose.HasValue)
		{
			ConnectionClose = sourceHeaders.ConnectionClose;
		}
	}
}
