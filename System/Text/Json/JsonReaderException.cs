using System.Runtime.Serialization;

namespace System.Text.Json;

[Serializable]
internal sealed class JsonReaderException : JsonException
{
	public JsonReaderException(string message, long lineNumber, long bytePositionInLine)
		: base(message, null, lineNumber, bytePositionInLine)
	{
	}

	[Obsolete("This API supports obsolete formatter-based serialization. It should not be called or extended by application code.", DiagnosticId = "SYSLIB0051", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
	private JsonReaderException(SerializationInfo info, StreamingContext context)
		: base(info, context)
	{
	}
}
