using System.ComponentModel;
using System.Runtime.Serialization;

namespace System.Diagnostics.Tracing;

[Serializable]
public class EventSourceException : Exception
{
	public EventSourceException()
		: base(SR.EventSource_ListenerWriteFailure)
	{
	}

	public EventSourceException(string? message)
		: base(message)
	{
	}

	public EventSourceException(string? message, Exception? innerException)
		: base(message, innerException)
	{
	}

	[Obsolete("This API supports obsolete formatter-based serialization. It should not be called or extended by application code.", DiagnosticId = "SYSLIB0051", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
	[EditorBrowsable(EditorBrowsableState.Never)]
	protected EventSourceException(SerializationInfo info, StreamingContext context)
		: base(info, context)
	{
	}

	internal EventSourceException(Exception innerException)
		: base(SR.EventSource_ListenerWriteFailure, innerException)
	{
	}
}
