using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace System.Runtime;

[Serializable]
[TypeForwardedFrom("System.Runtime, Version=4.2.1.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
public sealed class AmbiguousImplementationException : Exception
{
	public AmbiguousImplementationException()
		: base(SR.Arg_AmbiguousImplementationException_NoMessage)
	{
		base.HResult = -2146234262;
	}

	public AmbiguousImplementationException(string? message)
		: base(message)
	{
		base.HResult = -2146234262;
	}

	public AmbiguousImplementationException(string? message, Exception? innerException)
		: base(message, innerException)
	{
		base.HResult = -2146234262;
	}

	[Obsolete("This API supports obsolete formatter-based serialization. It should not be called or extended by application code.", DiagnosticId = "SYSLIB0051", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
	private AmbiguousImplementationException(SerializationInfo info, StreamingContext context)
		: base(info, context)
	{
	}
}
