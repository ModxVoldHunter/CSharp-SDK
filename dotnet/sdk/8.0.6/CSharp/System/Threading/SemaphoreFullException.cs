using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace System.Threading;

[Serializable]
[TypeForwardedFrom("System, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
public class SemaphoreFullException : SystemException
{
	public SemaphoreFullException()
		: base(SR.Threading_SemaphoreFullException)
	{
	}

	public SemaphoreFullException(string? message)
		: base(message)
	{
	}

	public SemaphoreFullException(string? message, Exception? innerException)
		: base(message, innerException)
	{
	}

	[Obsolete("This API supports obsolete formatter-based serialization. It should not be called or extended by application code.", DiagnosticId = "SYSLIB0051", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
	[EditorBrowsable(EditorBrowsableState.Never)]
	protected SemaphoreFullException(SerializationInfo info, StreamingContext context)
		: base(info, context)
	{
	}
}
