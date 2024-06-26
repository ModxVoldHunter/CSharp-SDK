using System.ComponentModel;
using System.Runtime.Serialization;

namespace System.Formats.Asn1;

[Serializable]
public class AsnContentException : Exception
{
	public AsnContentException()
		: base(System.SR.ContentException_DefaultMessage)
	{
	}

	public AsnContentException(string? message)
		: base(message ?? System.SR.ContentException_DefaultMessage)
	{
	}

	public AsnContentException(string? message, Exception? inner)
		: base(message ?? System.SR.ContentException_DefaultMessage, inner)
	{
	}

	[Obsolete("This API supports obsolete formatter-based serialization. It should not be called or extended by application code.", DiagnosticId = "SYSLIB0051", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
	[EditorBrowsable(EditorBrowsableState.Never)]
	protected AsnContentException(SerializationInfo info, StreamingContext context)
		: base(info, context)
	{
	}
}
