using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace System;

[Serializable]
[TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
public class EntryPointNotFoundException : TypeLoadException
{
	public EntryPointNotFoundException()
		: base(SR.Arg_EntryPointNotFoundException)
	{
		base.HResult = -2146233053;
	}

	public EntryPointNotFoundException(string? message)
		: base(message)
	{
		base.HResult = -2146233053;
	}

	public EntryPointNotFoundException(string? message, Exception? inner)
		: base(message, inner)
	{
		base.HResult = -2146233053;
	}

	[Obsolete("This API supports obsolete formatter-based serialization. It should not be called or extended by application code.", DiagnosticId = "SYSLIB0051", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
	[EditorBrowsable(EditorBrowsableState.Never)]
	protected EntryPointNotFoundException(SerializationInfo info, StreamingContext context)
		: base(info, context)
	{
	}
}
