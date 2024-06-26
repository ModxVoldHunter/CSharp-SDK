using System.ComponentModel;
using System.Runtime.Serialization;

namespace System.Net.Http.HPack;

[Serializable]
internal sealed class HPackDecodingException : Exception
{
	public HPackDecodingException(string message)
		: base(message)
	{
	}

	public HPackDecodingException(string message, Exception innerException)
		: base(message, innerException)
	{
	}

	[Obsolete("This API supports obsolete formatter-based serialization. It should not be called or extended by application code.", DiagnosticId = "SYSLIB0051", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
	[EditorBrowsable(EditorBrowsableState.Never)]
	public HPackDecodingException(SerializationInfo info, StreamingContext context)
		: base(info, context)
	{
	}
}
