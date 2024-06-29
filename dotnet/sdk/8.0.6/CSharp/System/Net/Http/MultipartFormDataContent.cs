using System.IO;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Http;

public class MultipartFormDataContent : MultipartContent
{
	public MultipartFormDataContent()
		: base("form-data")
	{
	}

	public MultipartFormDataContent(string boundary)
		: base("form-data", boundary)
	{
	}

	public override void Add(HttpContent content)
	{
		ArgumentNullException.ThrowIfNull(content, "content");
		HttpContentHeaders headers = content.Headers;
		if (headers.ContentDisposition == null)
		{
			ContentDispositionHeaderValue contentDispositionHeaderValue2 = (headers.ContentDisposition = new ContentDispositionHeaderValue("form-data"));
		}
		base.Add(content);
	}

	public void Add(HttpContent content, string name)
	{
		ArgumentNullException.ThrowIfNull(content, "content");
		ArgumentException.ThrowIfNullOrWhiteSpace(name, "name");
		AddInternal(content, name, null);
	}

	public void Add(HttpContent content, string name, string fileName)
	{
		ArgumentNullException.ThrowIfNull(content, "content");
		ArgumentException.ThrowIfNullOrWhiteSpace(name, "name");
		ArgumentException.ThrowIfNullOrWhiteSpace(fileName, "fileName");
		AddInternal(content, name, fileName);
	}

	private void AddInternal(HttpContent content, string name, string fileName)
	{
		if (content.Headers.ContentDisposition == null)
		{
			ContentDispositionHeaderValue contentDispositionHeaderValue = new ContentDispositionHeaderValue("form-data");
			contentDispositionHeaderValue.Name = name;
			contentDispositionHeaderValue.FileName = fileName;
			contentDispositionHeaderValue.FileNameStar = fileName;
			content.Headers.ContentDisposition = contentDispositionHeaderValue;
		}
		base.Add(content);
	}

	protected override Task SerializeToStreamAsync(Stream stream, TransportContext? context, CancellationToken cancellationToken)
	{
		if (!(GetType() == typeof(MultipartFormDataContent)))
		{
			return base.SerializeToStreamAsync(stream, context, cancellationToken);
		}
		return SerializeToStreamAsyncCore(stream, context, cancellationToken);
	}
}
