using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace System.Runtime.Serialization;

[Serializable]
[TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
public class SerializationException : SystemException
{
	public SerializationException()
		: base(SR.SerializationException)
	{
		base.HResult = -2146233076;
	}

	public SerializationException(string? message)
		: base(message)
	{
		base.HResult = -2146233076;
	}

	public SerializationException(string? message, Exception? innerException)
		: base(message, innerException)
	{
		base.HResult = -2146233076;
	}

	[Obsolete("This API supports obsolete formatter-based serialization. It should not be called or extended by application code.", DiagnosticId = "SYSLIB0051", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
	[EditorBrowsable(EditorBrowsableState.Never)]
	protected SerializationException(SerializationInfo info, StreamingContext context)
		: base(info, context)
	{
	}
}
