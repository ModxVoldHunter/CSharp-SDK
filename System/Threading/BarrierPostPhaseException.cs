using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace System.Threading;

[Serializable]
[TypeForwardedFrom("System, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
public class BarrierPostPhaseException : Exception
{
	public BarrierPostPhaseException()
		: this((string?)null)
	{
	}

	public BarrierPostPhaseException(Exception? innerException)
		: this(null, innerException)
	{
	}

	public BarrierPostPhaseException(string? message)
		: this(message, null)
	{
	}

	public BarrierPostPhaseException(string? message, Exception? innerException)
		: base((message == null) ? System.SR.BarrierPostPhaseException : message, innerException)
	{
	}

	[Obsolete("This API supports obsolete formatter-based serialization. It should not be called or extended by application code.", DiagnosticId = "SYSLIB0051", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
	[EditorBrowsable(EditorBrowsableState.Never)]
	protected BarrierPostPhaseException(SerializationInfo info, StreamingContext context)
		: base(info, context)
	{
	}
}
