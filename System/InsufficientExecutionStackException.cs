using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace System;

[Serializable]
[TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
public sealed class InsufficientExecutionStackException : SystemException
{
	public InsufficientExecutionStackException()
		: base(SR.Arg_InsufficientExecutionStackException)
	{
		base.HResult = -2146232968;
	}

	public InsufficientExecutionStackException(string? message)
		: base(message)
	{
		base.HResult = -2146232968;
	}

	public InsufficientExecutionStackException(string? message, Exception? innerException)
		: base(message, innerException)
	{
		base.HResult = -2146232968;
	}

	[Obsolete("This API supports obsolete formatter-based serialization. It should not be called or extended by application code.", DiagnosticId = "SYSLIB0051", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
	private InsufficientExecutionStackException(SerializationInfo info, StreamingContext context)
		: base(info, context)
	{
	}
}
