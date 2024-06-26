using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace System.Runtime.Serialization;

[Serializable]
[TypeForwardedFrom("System.Runtime.Serialization, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
public class InvalidDataContractException : Exception
{
	public InvalidDataContractException()
	{
	}

	public InvalidDataContractException(string? message)
		: base(message)
	{
	}

	public InvalidDataContractException(string? message, Exception? innerException)
		: base(message, innerException)
	{
	}

	[Obsolete("This API supports obsolete formatter-based serialization. It should not be called or extended by application code.", DiagnosticId = "SYSLIB0051", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
	[EditorBrowsable(EditorBrowsableState.Never)]
	protected InvalidDataContractException(SerializationInfo info, StreamingContext context)
		: base(info, context)
	{
	}
}
