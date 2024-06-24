using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace System.IO;

[Serializable]
[TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
public class IOException : SystemException
{
	public IOException()
		: base(SR.Arg_IOException)
	{
		base.HResult = -2146232800;
	}

	public IOException(string? message)
		: base(message)
	{
		base.HResult = -2146232800;
	}

	public IOException(string? message, int hresult)
		: base(message)
	{
		base.HResult = hresult;
	}

	public IOException(string? message, Exception? innerException)
		: base(message, innerException)
	{
		base.HResult = -2146232800;
	}

	[Obsolete("This API supports obsolete formatter-based serialization. It should not be called or extended by application code.", DiagnosticId = "SYSLIB0051", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
	[EditorBrowsable(EditorBrowsableState.Never)]
	protected IOException(SerializationInfo info, StreamingContext context)
		: base(info, context)
	{
	}
}
