using System.ComponentModel;
using System.Runtime.Serialization;

namespace System.Threading.Channels;

[Serializable]
public class ChannelClosedException : InvalidOperationException
{
	public ChannelClosedException()
		: base(System.SR.ChannelClosedException_DefaultMessage)
	{
	}

	public ChannelClosedException(string? message)
		: base(message)
	{
	}

	public ChannelClosedException(Exception? innerException)
		: base(System.SR.ChannelClosedException_DefaultMessage, innerException)
	{
	}

	public ChannelClosedException(string? message, Exception? innerException)
		: base(message, innerException)
	{
	}

	[Obsolete("This API supports obsolete formatter-based serialization. It should not be called or extended by application code.", DiagnosticId = "SYSLIB0051", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
	[EditorBrowsable(EditorBrowsableState.Never)]
	protected ChannelClosedException(SerializationInfo info, StreamingContext context)
		: base(info, context)
	{
	}
}
