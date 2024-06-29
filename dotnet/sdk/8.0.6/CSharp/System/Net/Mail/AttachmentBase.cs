using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Net.Mime;
using System.Text;

namespace System.Net.Mail;

public abstract class AttachmentBase : IDisposable
{
	internal bool disposed;

	private readonly MimePart _part = new MimePart();

	public Stream ContentStream
	{
		get
		{
			ObjectDisposedException.ThrowIf(disposed, this);
			return _part.Stream;
		}
	}

	public string ContentId
	{
		get
		{
			string contentID = _part.ContentID;
			if (string.IsNullOrEmpty(contentID))
			{
				return ContentId = Guid.NewGuid().ToString();
			}
			if (contentID.StartsWith('<') && contentID.EndsWith('>'))
			{
				return contentID.Substring(1, contentID.Length - 2);
			}
			return contentID;
		}
		[param: AllowNull]
		set
		{
			if (string.IsNullOrEmpty(value))
			{
				_part.ContentID = null;
				return;
			}
			if (value.AsSpan().ContainsAny('<', '>'))
			{
				throw new ArgumentException(System.SR.MailHeaderInvalidCID, "value");
			}
			_part.ContentID = "<" + value + ">";
		}
	}

	public ContentType ContentType
	{
		get
		{
			return _part.ContentType;
		}
		set
		{
			_part.ContentType = value;
		}
	}

	public TransferEncoding TransferEncoding
	{
		get
		{
			return _part.TransferEncoding;
		}
		set
		{
			_part.TransferEncoding = value;
		}
	}

	internal Uri? ContentLocation
	{
		get
		{
			if (!Uri.TryCreate(_part.ContentLocation, UriKind.RelativeOrAbsolute, out Uri result))
			{
				return null;
			}
			return result;
		}
		set
		{
			_part.ContentLocation = ((value == null) ? null : (value.IsAbsoluteUri ? value.AbsoluteUri : value.OriginalString));
		}
	}

	internal MimePart MimePart => _part;

	internal AttachmentBase()
	{
	}

	protected AttachmentBase(string fileName)
	{
		SetContentFromFile(fileName, string.Empty);
	}

	protected AttachmentBase(string fileName, string? mediaType)
	{
		SetContentFromFile(fileName, mediaType);
	}

	protected AttachmentBase(string fileName, ContentType? contentType)
	{
		SetContentFromFile(fileName, contentType);
	}

	protected AttachmentBase(Stream contentStream)
	{
		_part.SetContent(contentStream);
	}

	protected AttachmentBase(Stream contentStream, string? mediaType)
	{
		_part.SetContent(contentStream, null, mediaType);
	}

	internal AttachmentBase(Stream contentStream, string name, string mediaType)
	{
		_part.SetContent(contentStream, name, mediaType);
	}

	protected AttachmentBase(Stream contentStream, ContentType? contentType)
	{
		_part.SetContent(contentStream, contentType);
	}

	public void Dispose()
	{
		Dispose(disposing: true);
	}

	protected virtual void Dispose(bool disposing)
	{
		if (disposing && !disposed)
		{
			disposed = true;
			_part.Dispose();
		}
	}

	internal void SetContentFromFile(string fileName, ContentType contentType)
	{
		ArgumentException.ThrowIfNullOrEmpty(fileName, "fileName");
		Stream stream = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.Read);
		_part.SetContent(stream, contentType);
	}

	internal void SetContentFromFile(string fileName, string mediaType)
	{
		ArgumentException.ThrowIfNullOrEmpty(fileName, "fileName");
		Stream stream = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.Read);
		_part.SetContent(stream, null, mediaType);
	}

	internal void SetContentFromString(string content, ContentType contentType)
	{
		ArgumentNullException.ThrowIfNull(content, "content");
		_part.Stream?.Close();
		Encoding encoding = ((contentType != null && contentType.CharSet != null) ? Encoding.GetEncoding(contentType.CharSet) : ((!MimeBasePart.IsAscii(content, permitCROrLF: false)) ? Encoding.GetEncoding("utf-8") : Encoding.ASCII));
		byte[] bytes = encoding.GetBytes(content);
		_part.SetContent(new MemoryStream(bytes), contentType);
		if (MimeBasePart.ShouldUseBase64Encoding(encoding))
		{
			_part.TransferEncoding = TransferEncoding.Base64;
		}
		else
		{
			_part.TransferEncoding = TransferEncoding.QuotedPrintable;
		}
	}

	internal void SetContentFromString(string content, Encoding encoding, string mediaType)
	{
		ArgumentNullException.ThrowIfNull(content, "content");
		_part.Stream?.Close();
		if (string.IsNullOrEmpty(mediaType))
		{
			mediaType = "text/plain";
		}
		int offset = 0;
		try
		{
			string text = MailBnfHelper.ReadToken(mediaType, ref offset);
			if (text.Length == 0 || offset >= mediaType.Length || mediaType[offset++] != '/')
			{
				throw new ArgumentException(System.SR.MediaTypeInvalid, "mediaType");
			}
			text = MailBnfHelper.ReadToken(mediaType, ref offset);
			if (text.Length == 0 || offset < mediaType.Length)
			{
				throw new ArgumentException(System.SR.MediaTypeInvalid, "mediaType");
			}
		}
		catch (FormatException)
		{
			throw new ArgumentException(System.SR.MediaTypeInvalid, "mediaType");
		}
		ContentType contentType = new ContentType(mediaType);
		if (encoding == null)
		{
			encoding = ((!MimeBasePart.IsAscii(content, permitCROrLF: false)) ? Encoding.GetEncoding("utf-8") : Encoding.ASCII);
		}
		contentType.CharSet = encoding.BodyName;
		byte[] bytes = encoding.GetBytes(content);
		_part.SetContent(new MemoryStream(bytes), contentType);
		if (MimeBasePart.ShouldUseBase64Encoding(encoding))
		{
			_part.TransferEncoding = TransferEncoding.Base64;
		}
		else
		{
			_part.TransferEncoding = TransferEncoding.QuotedPrintable;
		}
	}

	internal virtual void PrepareForSending(bool allowUnicode)
	{
		_part.ResetStream();
	}
}
