using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace System;

[Serializable]
[TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
public class CannotUnloadAppDomainException : SystemException
{
	public CannotUnloadAppDomainException()
		: base(SR.Arg_CannotUnloadAppDomainException)
	{
		base.HResult = -2146234347;
	}

	public CannotUnloadAppDomainException(string? message)
		: base(message)
	{
		base.HResult = -2146234347;
	}

	public CannotUnloadAppDomainException(string? message, Exception? innerException)
		: base(message, innerException)
	{
		base.HResult = -2146234347;
	}

	[Obsolete("This API supports obsolete formatter-based serialization. It should not be called or extended by application code.", DiagnosticId = "SYSLIB0051", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
	[EditorBrowsable(EditorBrowsableState.Never)]
	protected CannotUnloadAppDomainException(SerializationInfo info, StreamingContext context)
		: base(info, context)
	{
	}
}
