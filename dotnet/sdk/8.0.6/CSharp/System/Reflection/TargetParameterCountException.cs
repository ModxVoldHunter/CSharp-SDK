using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace System.Reflection;

[Serializable]
[TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
public sealed class TargetParameterCountException : ApplicationException
{
	public TargetParameterCountException()
		: base(SR.Arg_TargetParameterCountException)
	{
		base.HResult = -2147352562;
	}

	public TargetParameterCountException(string? message)
		: base(message)
	{
		base.HResult = -2147352562;
	}

	public TargetParameterCountException(string? message, Exception? inner)
		: base(message, inner)
	{
		base.HResult = -2147352562;
	}

	[Obsolete("This API supports obsolete formatter-based serialization. It should not be called or extended by application code.", DiagnosticId = "SYSLIB0051", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
	private TargetParameterCountException(SerializationInfo info, StreamingContext context)
		: base(info, context)
	{
	}
}
