using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace System.Runtime.InteropServices;

[Serializable]
[TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
public class SEHException : ExternalException
{
	public SEHException()
	{
		base.HResult = -2147467259;
	}

	public SEHException(string? message)
		: base(message)
	{
		base.HResult = -2147467259;
	}

	public SEHException(string? message, Exception? inner)
		: base(message, inner)
	{
		base.HResult = -2147467259;
	}

	[Obsolete("This API supports obsolete formatter-based serialization. It should not be called or extended by application code.", DiagnosticId = "SYSLIB0051", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
	[EditorBrowsable(EditorBrowsableState.Never)]
	protected SEHException(SerializationInfo info, StreamingContext context)
		: base(info, context)
	{
	}

	public virtual bool CanResume()
	{
		return false;
	}
}
