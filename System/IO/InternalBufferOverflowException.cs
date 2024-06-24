using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace System.IO;

[Serializable]
[TypeForwardedFrom("System, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
public class InternalBufferOverflowException : SystemException
{
	public InternalBufferOverflowException()
	{
		base.HResult = -2146232059;
	}

	public InternalBufferOverflowException(string? message)
		: base(message)
	{
		base.HResult = -2146232059;
	}

	public InternalBufferOverflowException(string? message, Exception? inner)
		: base(message, inner)
	{
		base.HResult = -2146232059;
	}

	[Obsolete("This API supports obsolete formatter-based serialization. It should not be called or extended by application code.", DiagnosticId = "SYSLIB0051", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
	[EditorBrowsable(EditorBrowsableState.Never)]
	protected InternalBufferOverflowException(SerializationInfo info, StreamingContext context)
		: base(info, context)
	{
	}
}
