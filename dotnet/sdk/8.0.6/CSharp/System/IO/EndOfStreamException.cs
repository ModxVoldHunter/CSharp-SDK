using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace System.IO;

[Serializable]
[TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
public class EndOfStreamException : IOException
{
	public EndOfStreamException()
		: base(SR.Arg_EndOfStreamException)
	{
		base.HResult = -2147024858;
	}

	public EndOfStreamException(string? message)
		: base(message)
	{
		base.HResult = -2147024858;
	}

	public EndOfStreamException(string? message, Exception? innerException)
		: base(message, innerException)
	{
		base.HResult = -2147024858;
	}

	[Obsolete("This API supports obsolete formatter-based serialization. It should not be called or extended by application code.", DiagnosticId = "SYSLIB0051", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
	[EditorBrowsable(EditorBrowsableState.Never)]
	protected EndOfStreamException(SerializationInfo info, StreamingContext context)
		: base(info, context)
	{
	}
}
