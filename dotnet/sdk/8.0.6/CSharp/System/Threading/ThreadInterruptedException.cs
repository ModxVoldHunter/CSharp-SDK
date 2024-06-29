using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace System.Threading;

[Serializable]
[TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
public class ThreadInterruptedException : SystemException
{
	public ThreadInterruptedException()
		: base(Exception.GetMessageFromNativeResources(ExceptionMessageKind.ThreadInterrupted))
	{
		base.HResult = -2146233063;
	}

	public ThreadInterruptedException(string? message)
		: base(message)
	{
		base.HResult = -2146233063;
	}

	public ThreadInterruptedException(string? message, Exception? innerException)
		: base(message, innerException)
	{
		base.HResult = -2146233063;
	}

	[Obsolete("This API supports obsolete formatter-based serialization. It should not be called or extended by application code.", DiagnosticId = "SYSLIB0051", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
	[EditorBrowsable(EditorBrowsableState.Never)]
	protected ThreadInterruptedException(SerializationInfo info, StreamingContext context)
		: base(info, context)
	{
	}
}
