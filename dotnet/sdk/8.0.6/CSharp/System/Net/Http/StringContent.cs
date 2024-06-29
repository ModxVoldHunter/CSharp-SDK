using System.IO;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Http;

public class StringContent : ByteArrayContent
{
	public StringContent(string content)
		: this(content, HttpContent.DefaultStringEncoding, "text/plain")
	{
	}

	public StringContent(string content, MediaTypeHeaderValue mediaType)
		: this(content, HttpContent.DefaultStringEncoding, mediaType)
	{
	}

	public StringContent(string content, Encoding? encoding)
		: this(content, encoding, "text/plain")
	{
	}

	public StringContent(string content, Encoding? encoding, string mediaType)
		: this(content, encoding, new MediaTypeHeaderValue(mediaType ?? "text/plain", (encoding ?? HttpContent.DefaultStringEncoding).WebName))
	{
	}

	public StringContent(string content, Encoding? encoding, MediaTypeHeaderValue mediaType)
		: base(GetContentByteArray(content, encoding))
	{
		base.Headers.ContentType = mediaType;
	}

	private static byte[] GetContentByteArray(string content, Encoding encoding)
	{
		ArgumentNullException.ThrowIfNull(content, "content");
		return (encoding ?? HttpContent.DefaultStringEncoding).GetBytes(content);
	}

	protected override Task SerializeToStreamAsync(Stream stream, TransportContext? context, CancellationToken cancellationToken)
	{
		if (!(GetType() == typeof(StringContent)))
		{
			return base.SerializeToStreamAsync(stream, context, cancellationToken);
		}
		return SerializeToStreamAsyncCore(stream, cancellationToken);
	}

	internal override Stream TryCreateContentReadStream()
	{
		if (!(GetType() == typeof(StringContent)))
		{
			return null;
		}
		return CreateMemoryStreamForByteArray();
	}
}
