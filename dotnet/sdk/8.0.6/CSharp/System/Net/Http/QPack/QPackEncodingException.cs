using System.Runtime.Serialization;

namespace System.Net.Http.QPack;

[Serializable]
internal sealed class QPackEncodingException : Exception
{
	public QPackEncodingException(string message)
		: base(message)
	{
	}

	[Obsolete("This API supports obsolete formatter-based serialization. It should not be called or extended by application code.", DiagnosticId = "SYSLIB0051", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
	private QPackEncodingException(SerializationInfo info, StreamingContext context)
		: base(info, context)
	{
	}
}
