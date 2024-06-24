using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace System.ComponentModel;

[Serializable]
[TypeForwardedFrom("System, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
public class InvalidEnumArgumentException : ArgumentException
{
	public InvalidEnumArgumentException()
		: this(null)
	{
	}

	public InvalidEnumArgumentException(string? message)
		: base(message)
	{
	}

	public InvalidEnumArgumentException(string? message, Exception? innerException)
		: base(message, innerException)
	{
	}

	public InvalidEnumArgumentException(string? argumentName, int invalidValue, Type enumClass)
		: base(System.SR.Format(System.SR.InvalidEnumArgument, argumentName, invalidValue, enumClass?.Name), argumentName)
	{
		ArgumentNullException.ThrowIfNull(enumClass, "enumClass");
	}

	[Obsolete("This API supports obsolete formatter-based serialization. It should not be called or extended by application code.", DiagnosticId = "SYSLIB0051", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
	[EditorBrowsable(EditorBrowsableState.Never)]
	protected InvalidEnumArgumentException(SerializationInfo info, StreamingContext context)
		: base(info, context)
	{
	}
}
