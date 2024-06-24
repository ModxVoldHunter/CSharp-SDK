using System.Collections.Generic;

namespace System.Net.Http.Headers;

public sealed class HttpContentHeaders : HttpHeaders
{
	private readonly HttpContent _parent;

	private bool _contentLengthSet;

	private HttpHeaderValueCollection<string> _allow;

	private HttpHeaderValueCollection<string> _contentEncoding;

	private HttpHeaderValueCollection<string> _contentLanguage;

	public ICollection<string> Allow => _allow ?? (_allow = new HttpHeaderValueCollection<string>(KnownHeaders.Allow.Descriptor, this));

	public ContentDispositionHeaderValue? ContentDisposition
	{
		get
		{
			return (ContentDispositionHeaderValue)GetSingleParsedValue(KnownHeaders.ContentDisposition.Descriptor);
		}
		set
		{
			SetOrRemoveParsedValue(KnownHeaders.ContentDisposition.Descriptor, value);
		}
	}

	public ICollection<string> ContentEncoding => _contentEncoding ?? (_contentEncoding = new HttpHeaderValueCollection<string>(KnownHeaders.ContentEncoding.Descriptor, this));

	public ICollection<string> ContentLanguage => _contentLanguage ?? (_contentLanguage = new HttpHeaderValueCollection<string>(KnownHeaders.ContentLanguage.Descriptor, this));

	public long? ContentLength
	{
		get
		{
			object singleParsedValue = GetSingleParsedValue(KnownHeaders.ContentLength.Descriptor);
			if (!_contentLengthSet && singleParsedValue == null)
			{
				long? computedOrBufferLength = _parent.GetComputedOrBufferLength();
				if (computedOrBufferLength.HasValue)
				{
					SetParsedValue(KnownHeaders.ContentLength.Descriptor, computedOrBufferLength.Value);
				}
				return computedOrBufferLength;
			}
			if (singleParsedValue == null)
			{
				return null;
			}
			return (long)singleParsedValue;
		}
		set
		{
			SetOrRemoveParsedValue(KnownHeaders.ContentLength.Descriptor, value);
			_contentLengthSet = true;
		}
	}

	public Uri? ContentLocation
	{
		get
		{
			return (Uri)GetSingleParsedValue(KnownHeaders.ContentLocation.Descriptor);
		}
		set
		{
			SetOrRemoveParsedValue(KnownHeaders.ContentLocation.Descriptor, value);
		}
	}

	public byte[]? ContentMD5
	{
		get
		{
			return (byte[])GetSingleParsedValue(KnownHeaders.ContentMD5.Descriptor);
		}
		set
		{
			SetOrRemoveParsedValue(KnownHeaders.ContentMD5.Descriptor, value);
		}
	}

	public ContentRangeHeaderValue? ContentRange
	{
		get
		{
			return (ContentRangeHeaderValue)GetSingleParsedValue(KnownHeaders.ContentRange.Descriptor);
		}
		set
		{
			SetOrRemoveParsedValue(KnownHeaders.ContentRange.Descriptor, value);
		}
	}

	public MediaTypeHeaderValue? ContentType
	{
		get
		{
			return (MediaTypeHeaderValue)GetSingleParsedValue(KnownHeaders.ContentType.Descriptor);
		}
		set
		{
			SetOrRemoveParsedValue(KnownHeaders.ContentType.Descriptor, value);
		}
	}

	public DateTimeOffset? Expires
	{
		get
		{
			return HeaderUtilities.GetDateTimeOffsetValue(KnownHeaders.Expires.Descriptor, this, DateTimeOffset.MinValue);
		}
		set
		{
			SetOrRemoveParsedValue(KnownHeaders.Expires.Descriptor, value);
		}
	}

	public DateTimeOffset? LastModified
	{
		get
		{
			return HeaderUtilities.GetDateTimeOffsetValue(KnownHeaders.LastModified.Descriptor, this);
		}
		set
		{
			SetOrRemoveParsedValue(KnownHeaders.LastModified.Descriptor, value);
		}
	}

	internal HttpContentHeaders(HttpContent parent)
		: base(HttpHeaderType.Content | HttpHeaderType.Custom, HttpHeaderType.None)
	{
		_parent = parent;
	}
}
