using System.Collections.Specialized;
using System.Net.Mail;
using System.Text;

namespace System.Net.Mime;

internal class MimeBasePart
{
	internal sealed class MimePartAsyncResult : System.Net.LazyAsyncResult
	{
		internal MimePartAsyncResult(MimeBasePart part, object state, AsyncCallback callback)
			: base(part, state, callback)
		{
		}
	}

	protected ContentType _contentType;

	protected ContentDisposition _contentDisposition;

	private HeaderCollection _headers;

	private static readonly char[] s_headerValueSplitChars = new char[3] { '\r', '\n', ' ' };

	internal string ContentID
	{
		get
		{
			return Headers[MailHeaderInfo.GetString(MailHeaderID.ContentID)];
		}
		set
		{
			if (string.IsNullOrEmpty(value))
			{
				Headers.Remove(MailHeaderInfo.GetString(MailHeaderID.ContentID));
			}
			else
			{
				Headers[MailHeaderInfo.GetString(MailHeaderID.ContentID)] = value;
			}
		}
	}

	internal string ContentLocation
	{
		get
		{
			return Headers[MailHeaderInfo.GetString(MailHeaderID.ContentLocation)];
		}
		set
		{
			if (string.IsNullOrEmpty(value))
			{
				Headers.Remove(MailHeaderInfo.GetString(MailHeaderID.ContentLocation));
			}
			else
			{
				Headers[MailHeaderInfo.GetString(MailHeaderID.ContentLocation)] = value;
			}
		}
	}

	internal NameValueCollection Headers
	{
		get
		{
			if (_headers == null)
			{
				_headers = new HeaderCollection();
			}
			if (_contentType == null)
			{
				_contentType = new ContentType();
			}
			_contentType.PersistIfNeeded(_headers, forcePersist: false);
			_contentDisposition?.PersistIfNeeded(_headers, forcePersist: false);
			return _headers;
		}
	}

	internal ContentType ContentType
	{
		get
		{
			return _contentType ?? (_contentType = new ContentType());
		}
		set
		{
			ArgumentNullException.ThrowIfNull(value, "value");
			_contentType = value;
			_contentType.PersistIfNeeded((HeaderCollection)Headers, forcePersist: true);
		}
	}

	internal MimeBasePart()
	{
	}

	internal static bool ShouldUseBase64Encoding(Encoding encoding)
	{
		if (encoding != Encoding.Unicode && encoding != Encoding.UTF8 && encoding != Encoding.UTF32)
		{
			return encoding == Encoding.BigEndianUnicode;
		}
		return true;
	}

	internal static string EncodeHeaderValue(string value, Encoding encoding, bool base64Encoding)
	{
		return EncodeHeaderValue(value, encoding, base64Encoding, 0);
	}

	internal static string EncodeHeaderValue(string value, Encoding encoding, bool base64Encoding, int headerLength)
	{
		if (IsAscii(value, permitCROrLF: false))
		{
			return value;
		}
		if (encoding == null)
		{
			encoding = Encoding.GetEncoding("utf-8");
		}
		IEncodableStream encoderForHeader = EncodedStreamFactory.GetEncoderForHeader(encoding, base64Encoding, headerLength);
		encoderForHeader.EncodeString(value, encoding);
		return encoderForHeader.GetEncodedString();
	}

	internal static string DecodeHeaderValue(string value)
	{
		if (string.IsNullOrEmpty(value))
		{
			return string.Empty;
		}
		string text = string.Empty;
		string[] array = value.Split(s_headerValueSplitChars, StringSplitOptions.RemoveEmptyEntries);
		string[] array2 = array;
		foreach (string text2 in array2)
		{
			string[] array3 = text2.Split('?');
			if (array3.Length != 5 || array3[0] != "=" || array3[4] != "=")
			{
				return value;
			}
			string name = array3[1];
			bool useBase64Encoding = array3[2] == "B";
			byte[] bytes = Encoding.ASCII.GetBytes(array3[3]);
			IEncodableStream encoderForHeader = EncodedStreamFactory.GetEncoderForHeader(Encoding.GetEncoding(name), useBase64Encoding, 0);
			int count = encoderForHeader.DecodeBytes(bytes, 0, bytes.Length);
			Encoding encoding = Encoding.GetEncoding(name);
			text += encoding.GetString(bytes, 0, count);
		}
		return text;
	}

	internal static Encoding DecodeEncoding(string value)
	{
		if (string.IsNullOrEmpty(value))
		{
			return null;
		}
		ReadOnlySpan<char> source = value;
		Span<Range> destination = stackalloc Range[6];
		if (source.SplitAny(destination, "?\r\n") >= 5)
		{
			Range range = destination[0];
			if (source[range.Start..range.End].SequenceEqual("=".AsSpan()))
			{
				range = destination[4];
				if (source[range.Start..range.End].SequenceEqual("=".AsSpan()))
				{
					range = destination[1];
					return Encoding.GetEncoding(value[range.Start..range.End]);
				}
			}
		}
		return null;
	}

	internal static bool IsAscii(string value, bool permitCROrLF)
	{
		ArgumentNullException.ThrowIfNull(value, "value");
		if (Ascii.IsValid(value))
		{
			if (!permitCROrLF)
			{
				return !value.AsSpan().ContainsAny('\r', '\n');
			}
			return true;
		}
		return false;
	}

	internal void PrepareHeaders(bool allowUnicode)
	{
		_contentType.PersistIfNeeded((HeaderCollection)Headers, forcePersist: false);
		_headers.InternalSet(MailHeaderInfo.GetString(MailHeaderID.ContentType), _contentType.Encode(allowUnicode));
		if (_contentDisposition != null)
		{
			_contentDisposition.PersistIfNeeded((HeaderCollection)Headers, forcePersist: false);
			_headers.InternalSet(MailHeaderInfo.GetString(MailHeaderID.ContentDisposition), _contentDisposition.Encode(allowUnicode));
		}
	}

	internal virtual void Send(BaseWriter writer, bool allowUnicode)
	{
		throw new NotImplementedException();
	}

	internal virtual IAsyncResult BeginSend(BaseWriter writer, AsyncCallback callback, bool allowUnicode, object state)
	{
		throw new NotImplementedException();
	}

	internal void EndSend(IAsyncResult asyncResult)
	{
		ArgumentNullException.ThrowIfNull(asyncResult, "asyncResult");
		System.Net.LazyAsyncResult lazyAsyncResult = asyncResult as MimePartAsyncResult;
		if (lazyAsyncResult == null || lazyAsyncResult.AsyncObject != this)
		{
			throw new ArgumentException(System.SR.net_io_invalidasyncresult, "asyncResult");
		}
		if (lazyAsyncResult.EndCalled)
		{
			throw new InvalidOperationException(System.SR.Format(System.SR.net_io_invalidendcall, "EndSend"));
		}
		lazyAsyncResult.InternalWaitForCompletion();
		lazyAsyncResult.EndCalled = true;
		if (lazyAsyncResult.Result is Exception)
		{
			throw (Exception)lazyAsyncResult.Result;
		}
	}
}
