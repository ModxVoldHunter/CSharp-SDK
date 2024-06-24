using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace System.Reflection;

[Serializable]
[TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
public class InvalidFilterCriteriaException : ApplicationException
{
	public InvalidFilterCriteriaException()
		: this(SR.Arg_InvalidFilterCriteriaException)
	{
	}

	public InvalidFilterCriteriaException(string? message)
		: this(message, null)
	{
	}

	public InvalidFilterCriteriaException(string? message, Exception? inner)
		: base(message, inner)
	{
		base.HResult = -2146232831;
	}

	[Obsolete("This API supports obsolete formatter-based serialization. It should not be called or extended by application code.", DiagnosticId = "SYSLIB0051", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
	[EditorBrowsable(EditorBrowsableState.Never)]
	protected InvalidFilterCriteriaException(SerializationInfo info, StreamingContext context)
		: base(info, context)
	{
	}
}
