using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace System.Reflection;

[Serializable]
[TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
public sealed class AmbiguousMatchException : SystemException
{
	public AmbiguousMatchException()
		: base(SR.Arg_AmbiguousMatchException_NoMessage)
	{
		base.HResult = -2147475171;
	}

	public AmbiguousMatchException(string? message)
		: base(message)
	{
		base.HResult = -2147475171;
	}

	public AmbiguousMatchException(string? message, Exception? inner)
		: base(message, inner)
	{
		base.HResult = -2147475171;
	}

	[Obsolete("This API supports obsolete formatter-based serialization. It should not be called or extended by application code.", DiagnosticId = "SYSLIB0051", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
	private AmbiguousMatchException(SerializationInfo info, StreamingContext context)
		: base(info, context)
	{
	}
}
