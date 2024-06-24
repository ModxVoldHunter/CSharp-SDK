using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace System;

[Serializable]
[TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
public class InvalidCastException : SystemException
{
	public InvalidCastException()
		: base(SR.Arg_InvalidCastException)
	{
		base.HResult = -2147467262;
	}

	public InvalidCastException(string? message)
		: base(message)
	{
		base.HResult = -2147467262;
	}

	public InvalidCastException(string? message, Exception? innerException)
		: base(message, innerException)
	{
		base.HResult = -2147467262;
	}

	public InvalidCastException(string? message, int errorCode)
		: base(message)
	{
		base.HResult = errorCode;
	}

	[Obsolete("This API supports obsolete formatter-based serialization. It should not be called or extended by application code.", DiagnosticId = "SYSLIB0051", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
	[EditorBrowsable(EditorBrowsableState.Never)]
	protected InvalidCastException(SerializationInfo info, StreamingContext context)
		: base(info, context)
	{
	}
}
