using System.IO;

namespace System.Net.Http;

public class HttpIOException : IOException
{
	public HttpRequestError HttpRequestError { get; }

	public override string Message => $"{base.Message} ({HttpRequestError})";

	public HttpIOException(HttpRequestError httpRequestError, string? message = null, Exception? innerException = null)
		: base(message, innerException)
	{
		HttpRequestError = httpRequestError;
	}
}
