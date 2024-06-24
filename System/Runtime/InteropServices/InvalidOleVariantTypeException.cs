using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace System.Runtime.InteropServices;

[Serializable]
[TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
public class InvalidOleVariantTypeException : SystemException
{
	public InvalidOleVariantTypeException()
		: base(SR.Arg_InvalidOleVariantTypeException)
	{
		base.HResult = -2146233039;
	}

	public InvalidOleVariantTypeException(string? message)
		: base(message)
	{
		base.HResult = -2146233039;
	}

	public InvalidOleVariantTypeException(string? message, Exception? inner)
		: base(message, inner)
	{
		base.HResult = -2146233039;
	}

	[Obsolete("This API supports obsolete formatter-based serialization. It should not be called or extended by application code.", DiagnosticId = "SYSLIB0051", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
	[EditorBrowsable(EditorBrowsableState.Never)]
	protected InvalidOleVariantTypeException(SerializationInfo info, StreamingContext context)
		: base(info, context)
	{
	}
}
