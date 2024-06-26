using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace System.Data;

[Serializable]
[TypeForwardedFrom("System.Data, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
public class StrongTypingException : DataException
{
	[Obsolete("This API supports obsolete formatter-based serialization. It should not be called or extended by application code.", DiagnosticId = "SYSLIB0051", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
	[EditorBrowsable(EditorBrowsableState.Never)]
	protected StrongTypingException(SerializationInfo info, StreamingContext context)
		: base(info, context)
	{
	}

	public StrongTypingException()
	{
		base.HResult = -2146232021;
	}

	public StrongTypingException(string? message)
		: base(message)
	{
		base.HResult = -2146232021;
	}

	public StrongTypingException(string? s, Exception? innerException)
		: base(s, innerException)
	{
		base.HResult = -2146232021;
	}
}
