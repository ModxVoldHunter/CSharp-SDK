using System.Runtime.Serialization;

namespace System.Net.Http.QPack;

[Serializable]
internal sealed class QPackDecodingException : Exception
{
	public QPackDecodingException(string message)
		: base(message)
	{
	}

	public QPackDecodingException(string message, Exception innerException)
		: base(message, innerException)
	{
	}

	[Obsolete("This API supports obsolete formatter-based serialization. It should not be called or extended by application code.", DiagnosticId = "SYSLIB0051", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
	private QPackDecodingException(SerializationInfo info, StreamingContext context)
		: base(info, context)
	{
	}
}
