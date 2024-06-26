using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;

namespace System.ComponentModel.Design;

[Serializable]
[TypeForwardedFrom("System, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
public class CheckoutException : ExternalException
{
	public static readonly CheckoutException Canceled = new CheckoutException(System.SR.CHECKOUTCanceled, -2147467260);

	public CheckoutException()
	{
	}

	public CheckoutException(string? message)
		: base(message)
	{
	}

	public CheckoutException(string? message, int errorCode)
		: base(message, errorCode)
	{
	}

	[Obsolete("This API supports obsolete formatter-based serialization. It should not be called or extended by application code.", DiagnosticId = "SYSLIB0051", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
	[EditorBrowsable(EditorBrowsableState.Never)]
	protected CheckoutException(SerializationInfo info, StreamingContext context)
		: base(info, context)
	{
	}

	public CheckoutException(string? message, Exception? innerException)
		: base(message, innerException)
	{
	}
}
