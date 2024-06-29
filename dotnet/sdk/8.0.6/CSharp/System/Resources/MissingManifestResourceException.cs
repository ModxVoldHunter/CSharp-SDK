using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace System.Resources;

[Serializable]
[TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
[EditorBrowsable(EditorBrowsableState.Never)]
public class MissingManifestResourceException : SystemException
{
	public MissingManifestResourceException()
		: base(SR.Arg_MissingManifestResourceException)
	{
		base.HResult = -2146233038;
	}

	public MissingManifestResourceException(string? message)
		: base(message)
	{
		base.HResult = -2146233038;
	}

	public MissingManifestResourceException(string? message, Exception? inner)
		: base(message, inner)
	{
		base.HResult = -2146233038;
	}

	[Obsolete("This API supports obsolete formatter-based serialization. It should not be called or extended by application code.", DiagnosticId = "SYSLIB0051", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
	protected MissingManifestResourceException(SerializationInfo info, StreamingContext context)
		: base(info, context)
	{
	}
}
