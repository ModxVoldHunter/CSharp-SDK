using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace System;

[Serializable]
[TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
public class AccessViolationException : SystemException
{
	private nint _ip;

	private nint _target;

	private int _accessType;

	public AccessViolationException()
		: base(SR.Arg_AccessViolationException)
	{
		base.HResult = -2147467261;
	}

	public AccessViolationException(string? message)
		: base(message)
	{
		base.HResult = -2147467261;
	}

	public AccessViolationException(string? message, Exception? innerException)
		: base(message, innerException)
	{
		base.HResult = -2147467261;
	}

	[Obsolete("This API supports obsolete formatter-based serialization. It should not be called or extended by application code.", DiagnosticId = "SYSLIB0051", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
	[EditorBrowsable(EditorBrowsableState.Never)]
	protected AccessViolationException(SerializationInfo info, StreamingContext context)
		: base(info, context)
	{
	}
}
