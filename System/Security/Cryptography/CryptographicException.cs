using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace System.Security.Cryptography;

[Serializable]
[TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
public class CryptographicException : SystemException
{
	public CryptographicException()
		: base(SR.Arg_CryptographyException)
	{
	}

	public CryptographicException(int hr)
		: base(SR.Arg_CryptographyException)
	{
		base.HResult = hr;
	}

	public CryptographicException(string? message)
		: base(message)
	{
	}

	public CryptographicException(string? message, Exception? inner)
		: base(message, inner)
	{
	}

	public CryptographicException([StringSyntax("CompositeFormat")] string format, string? insert)
		: base(string.Format(format, insert))
	{
	}

	[Obsolete("This API supports obsolete formatter-based serialization. It should not be called or extended by application code.", DiagnosticId = "SYSLIB0051", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
	[EditorBrowsable(EditorBrowsableState.Never)]
	protected CryptographicException(SerializationInfo info, StreamingContext context)
		: base(info, context)
	{
	}
}
