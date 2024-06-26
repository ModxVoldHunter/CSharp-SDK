using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace System.Reflection;

[Serializable]
[TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
public sealed class TargetInvocationException : ApplicationException
{
	public TargetInvocationException(Exception? inner)
		: base(SR.Arg_TargetInvocationException, inner)
	{
		base.HResult = -2146232828;
	}

	public TargetInvocationException(string? message, Exception? inner)
		: base(message, inner)
	{
		base.HResult = -2146232828;
	}

	[Obsolete("This API supports obsolete formatter-based serialization. It should not be called or extended by application code.", DiagnosticId = "SYSLIB0051", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
	private TargetInvocationException(SerializationInfo info, StreamingContext context)
		: base(info, context)
	{
	}
}
