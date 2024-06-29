using System.Runtime.Versioning;

namespace System.Runtime.InteropServices.JavaScript;

[SupportedOSPlatform("browser")]
public sealed class JSException : Exception
{
	public JSException(string msg)
	{
		throw new PlatformNotSupportedException(System.SR.SystemRuntimeInteropServicesJavaScript_PlatformNotSupported);
	}
}
