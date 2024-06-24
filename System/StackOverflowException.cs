using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace System;

[Serializable]
[TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
public sealed class StackOverflowException : SystemException
{
	public StackOverflowException()
		: base(SR.Arg_StackOverflowException)
	{
		base.HResult = -2147023895;
	}

	public StackOverflowException(string? message)
		: base(message)
	{
		base.HResult = -2147023895;
	}

	public StackOverflowException(string? message, Exception? innerException)
		: base(message, innerException)
	{
		base.HResult = -2147023895;
	}

	[Obsolete("This API supports obsolete formatter-based serialization. It should not be called or extended by application code.", DiagnosticId = "SYSLIB0051", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
	private StackOverflowException(SerializationInfo info, StreamingContext context)
		: base(info, context)
	{
	}
}
