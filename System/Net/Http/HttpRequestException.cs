namespace System.Net.Http;

public class HttpRequestException : Exception
{
	internal RequestRetryType AllowRetry { get; }

	public HttpRequestError HttpRequestError { get; }

	public HttpStatusCode? StatusCode { get; }

	public HttpRequestException()
	{
	}

	public HttpRequestException(string? message)
		: base(message)
	{
	}

	public HttpRequestException(string? message, Exception? inner)
		: base(message, inner)
	{
		if (inner != null)
		{
			base.HResult = inner.HResult;
		}
	}

	public HttpRequestException(string? message, Exception? inner, HttpStatusCode? statusCode)
		: this(message, inner)
	{
		StatusCode = statusCode;
	}

	public HttpRequestException(HttpRequestError httpRequestError, string? message = null, Exception? inner = null, HttpStatusCode? statusCode = null)
		: this(message, inner, statusCode)
	{
		HttpRequestError = httpRequestError;
	}

	internal HttpRequestException(HttpRequestError httpRequestError, string message, Exception inner, RequestRetryType allowRetry)
		: this(httpRequestError, message, inner)
	{
		AllowRetry = allowRetry;
	}
}
