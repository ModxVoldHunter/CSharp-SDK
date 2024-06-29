using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace System.IO;

[Serializable]
[TypeForwardedFrom("System, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
public sealed class InvalidDataException : SystemException
{
	public InvalidDataException()
		: base(SR.GenericInvalidData)
	{
	}

	public InvalidDataException(string? message)
		: base(message)
	{
	}

	public InvalidDataException(string? message, Exception? innerException)
		: base(message, innerException)
	{
	}

	[Obsolete("This API supports obsolete formatter-based serialization. It should not be called or extended by application code.", DiagnosticId = "SYSLIB0051", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
	private InvalidDataException(SerializationInfo info, StreamingContext context)
		: base(info, context)
	{
	}
}
