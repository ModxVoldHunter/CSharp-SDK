using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace System.Threading;

[Serializable]
[TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
public sealed class ThreadAbortException : SystemException
{
	public object? ExceptionState => null;

	internal ThreadAbortException()
	{
		base.HResult = -2146233040;
	}

	[Obsolete("This API supports obsolete formatter-based serialization. It should not be called or extended by application code.", DiagnosticId = "SYSLIB0051", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
	private ThreadAbortException(SerializationInfo info, StreamingContext context)
		: base(info, context)
	{
	}
}
