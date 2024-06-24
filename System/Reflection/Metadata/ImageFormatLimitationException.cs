using System.ComponentModel;
using System.Runtime.Serialization;

namespace System.Reflection.Metadata;

[Serializable]
public class ImageFormatLimitationException : Exception
{
	public ImageFormatLimitationException()
	{
	}

	public ImageFormatLimitationException(string? message)
		: base(message)
	{
	}

	public ImageFormatLimitationException(string? message, Exception? innerException)
		: base(message, innerException)
	{
	}

	[Obsolete("This API supports obsolete formatter-based serialization. It should not be called or extended by application code.", DiagnosticId = "SYSLIB0051", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
	[EditorBrowsable(EditorBrowsableState.Never)]
	protected ImageFormatLimitationException(SerializationInfo info, StreamingContext context)
		: base(info, context)
	{
	}
}
