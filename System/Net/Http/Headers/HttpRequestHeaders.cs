namespace System.Net.Http.Headers;

public sealed class HttpRequestHeaders : HttpHeaders
{
	private object[] _specialCollectionsSlots;

	private HttpGeneralHeaders _generalHeaders;

	private bool _expectContinueSet;

	public HttpHeaderValueCollection<MediaTypeWithQualityHeaderValue> Accept => GetSpecializedCollection(0, (HttpRequestHeaders thisRef) => new HttpHeaderValueCollection<MediaTypeWithQualityHeaderValue>(KnownHeaders.Accept.Descriptor, thisRef));

	public HttpHeaderValueCollection<StringWithQualityHeaderValue> AcceptCharset => GetSpecializedCollection(1, (HttpRequestHeaders thisRef) => new HttpHeaderValueCollection<StringWithQualityHeaderValue>(KnownHeaders.AcceptCharset.Descriptor, thisRef));

	public HttpHeaderValueCollection<StringWithQualityHeaderValue> AcceptEncoding => GetSpecializedCollection(2, (HttpRequestHeaders thisRef) => new HttpHeaderValueCollection<StringWithQualityHeaderValue>(KnownHeaders.AcceptEncoding.Descriptor, thisRef));

	public HttpHeaderValueCollection<StringWithQualityHeaderValue> AcceptLanguage => GetSpecializedCollection(3, (HttpRequestHeaders thisRef) => new HttpHeaderValueCollection<StringWithQualityHeaderValue>(KnownHeaders.AcceptLanguage.Descriptor, thisRef));

	public AuthenticationHeaderValue? Authorization
	{
		get
		{
			return (AuthenticationHeaderValue)GetSingleParsedValue(KnownHeaders.Authorization.Descriptor);
		}
		set
		{
			SetOrRemoveParsedValue(KnownHeaders.Authorization.Descriptor, value);
		}
	}

	public bool? ExpectContinue
	{
		get
		{
			if (ContainsParsedValue(KnownHeaders.Expect.Descriptor, HeaderUtilities.ExpectContinue))
			{
				return true;
			}
			if (_expectContinueSet)
			{
				return false;
			}
			return null;
		}
		set
		{
			if (value == true)
			{
				_expectContinueSet = true;
				if (!ContainsParsedValue(KnownHeaders.Expect.Descriptor, HeaderUtilities.ExpectContinue))
				{
					AddParsedValue(KnownHeaders.Expect.Descriptor, HeaderUtilities.ExpectContinue);
				}
			}
			else
			{
				_expectContinueSet = value.HasValue;
				RemoveParsedValue(KnownHeaders.Expect.Descriptor, HeaderUtilities.ExpectContinue);
			}
		}
	}

	public string? From
	{
		get
		{
			return (string)GetSingleParsedValue(KnownHeaders.From.Descriptor);
		}
		set
		{
			if (value == string.Empty)
			{
				value = null;
			}
			HttpHeaders.CheckContainsNewLine(value);
			SetOrRemoveParsedValue(KnownHeaders.From.Descriptor, value);
		}
	}

	public string? Host
	{
		get
		{
			return (string)GetSingleParsedValue(KnownHeaders.Host.Descriptor);
		}
		set
		{
			if (value == string.Empty)
			{
				value = null;
			}
			if (value != null && HttpRuleParser.GetHostLength(value, 0, allowToken: false) != value.Length)
			{
				throw new FormatException(System.SR.net_http_headers_invalid_host_header);
			}
			SetOrRemoveParsedValue(KnownHeaders.Host.Descriptor, value);
		}
	}

	public HttpHeaderValueCollection<EntityTagHeaderValue> IfMatch => GetSpecializedCollection(4, (HttpRequestHeaders thisRef) => new HttpHeaderValueCollection<EntityTagHeaderValue>(KnownHeaders.IfMatch.Descriptor, thisRef));

	public DateTimeOffset? IfModifiedSince
	{
		get
		{
			return HeaderUtilities.GetDateTimeOffsetValue(KnownHeaders.IfModifiedSince.Descriptor, this);
		}
		set
		{
			SetOrRemoveParsedValue(KnownHeaders.IfModifiedSince.Descriptor, value);
		}
	}

	public HttpHeaderValueCollection<EntityTagHeaderValue> IfNoneMatch => GetSpecializedCollection(5, (HttpRequestHeaders thisRef) => new HttpHeaderValueCollection<EntityTagHeaderValue>(KnownHeaders.IfNoneMatch.Descriptor, thisRef));

	public RangeConditionHeaderValue? IfRange
	{
		get
		{
			return (RangeConditionHeaderValue)GetSingleParsedValue(KnownHeaders.IfRange.Descriptor);
		}
		set
		{
			SetOrRemoveParsedValue(KnownHeaders.IfRange.Descriptor, value);
		}
	}

	public DateTimeOffset? IfUnmodifiedSince
	{
		get
		{
			return HeaderUtilities.GetDateTimeOffsetValue(KnownHeaders.IfUnmodifiedSince.Descriptor, this);
		}
		set
		{
			SetOrRemoveParsedValue(KnownHeaders.IfUnmodifiedSince.Descriptor, value);
		}
	}

	public int? MaxForwards
	{
		get
		{
			object singleParsedValue = GetSingleParsedValue(KnownHeaders.MaxForwards.Descriptor);
			if (singleParsedValue != null)
			{
				return (int)singleParsedValue;
			}
			return null;
		}
		set
		{
			SetOrRemoveParsedValue(KnownHeaders.MaxForwards.Descriptor, value);
		}
	}

	public string? Protocol
	{
		get
		{
			if (_specialCollectionsSlots != null)
			{
				return (string)_specialCollectionsSlots[9];
			}
			return null;
		}
		set
		{
			HttpHeaders.CheckContainsNewLine(value);
			if (_specialCollectionsSlots == null)
			{
				_specialCollectionsSlots = new object[10];
			}
			_specialCollectionsSlots[9] = value;
		}
	}

	public AuthenticationHeaderValue? ProxyAuthorization
	{
		get
		{
			return (AuthenticationHeaderValue)GetSingleParsedValue(KnownHeaders.ProxyAuthorization.Descriptor);
		}
		set
		{
			SetOrRemoveParsedValue(KnownHeaders.ProxyAuthorization.Descriptor, value);
		}
	}

	public RangeHeaderValue? Range
	{
		get
		{
			return (RangeHeaderValue)GetSingleParsedValue(KnownHeaders.Range.Descriptor);
		}
		set
		{
			SetOrRemoveParsedValue(KnownHeaders.Range.Descriptor, value);
		}
	}

	public Uri? Referrer
	{
		get
		{
			return (Uri)GetSingleParsedValue(KnownHeaders.Referer.Descriptor);
		}
		set
		{
			SetOrRemoveParsedValue(KnownHeaders.Referer.Descriptor, value);
		}
	}

	public HttpHeaderValueCollection<TransferCodingWithQualityHeaderValue> TE => GetSpecializedCollection(6, (HttpRequestHeaders thisRef) => new HttpHeaderValueCollection<TransferCodingWithQualityHeaderValue>(KnownHeaders.TE.Descriptor, thisRef));

	public HttpHeaderValueCollection<ProductInfoHeaderValue> UserAgent => GetSpecializedCollection(7, (HttpRequestHeaders thisRef) => new HttpHeaderValueCollection<ProductInfoHeaderValue>(KnownHeaders.UserAgent.Descriptor, thisRef));

	public HttpHeaderValueCollection<NameValueWithParametersHeaderValue> Expect => GetSpecializedCollection(8, (HttpRequestHeaders thisRef) => new HttpHeaderValueCollection<NameValueWithParametersHeaderValue>(KnownHeaders.Expect.Descriptor, thisRef));

	public CacheControlHeaderValue? CacheControl
	{
		get
		{
			return GeneralHeaders.CacheControl;
		}
		set
		{
			GeneralHeaders.CacheControl = value;
		}
	}

	public HttpHeaderValueCollection<string> Connection => GeneralHeaders.Connection;

	public bool? ConnectionClose
	{
		get
		{
			return HttpGeneralHeaders.GetConnectionClose(this, _generalHeaders);
		}
		set
		{
			GeneralHeaders.ConnectionClose = value;
		}
	}

	public DateTimeOffset? Date
	{
		get
		{
			return GeneralHeaders.Date;
		}
		set
		{
			GeneralHeaders.Date = value;
		}
	}

	public HttpHeaderValueCollection<NameValueHeaderValue> Pragma => GeneralHeaders.Pragma;

	public HttpHeaderValueCollection<string> Trailer => GeneralHeaders.Trailer;

	public HttpHeaderValueCollection<TransferCodingHeaderValue> TransferEncoding => GeneralHeaders.TransferEncoding;

	public bool? TransferEncodingChunked
	{
		get
		{
			return HttpGeneralHeaders.GetTransferEncodingChunked(this, _generalHeaders);
		}
		set
		{
			GeneralHeaders.TransferEncodingChunked = value;
		}
	}

	public HttpHeaderValueCollection<ProductHeaderValue> Upgrade => GeneralHeaders.Upgrade;

	public HttpHeaderValueCollection<ViaHeaderValue> Via => GeneralHeaders.Via;

	public HttpHeaderValueCollection<WarningHeaderValue> Warning => GeneralHeaders.Warning;

	private HttpGeneralHeaders GeneralHeaders => _generalHeaders ?? (_generalHeaders = new HttpGeneralHeaders(this));

	private T GetSpecializedCollection<T>(int slot, Func<HttpRequestHeaders, T> creationFunc)
	{
		if (_specialCollectionsSlots == null)
		{
			_specialCollectionsSlots = new object[10];
		}
		object[] specialCollectionsSlots = _specialCollectionsSlots;
		return (T)(specialCollectionsSlots[slot] ?? (specialCollectionsSlots[slot] = creationFunc(this)));
	}

	internal HttpRequestHeaders()
		: base(HttpHeaderType.General | HttpHeaderType.Request | HttpHeaderType.Custom, HttpHeaderType.Response)
	{
	}

	internal override void AddHeaders(HttpHeaders sourceHeaders)
	{
		base.AddHeaders(sourceHeaders);
		HttpRequestHeaders httpRequestHeaders = sourceHeaders as HttpRequestHeaders;
		if (httpRequestHeaders._generalHeaders != null)
		{
			GeneralHeaders.AddSpecialsFrom(httpRequestHeaders._generalHeaders);
		}
		if (!ExpectContinue.HasValue)
		{
			ExpectContinue = httpRequestHeaders.ExpectContinue;
		}
	}
}
