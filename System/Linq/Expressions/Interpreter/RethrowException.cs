using System.Runtime.Serialization;

namespace System.Linq.Expressions.Interpreter;

[Serializable]
internal sealed class RethrowException : Exception
{
	public RethrowException()
	{
	}

	[Obsolete("This API supports obsolete formatter-based serialization. It should not be called or extended by application code.", DiagnosticId = "SYSLIB0051", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
	internal RethrowException(SerializationInfo info, StreamingContext context)
		: base(info, context)
	{
	}
}
