using System.ComponentModel;
using System.Runtime.Versioning;

namespace System.Runtime.InteropServices.JavaScript;

[SupportedOSPlatform("browser")]
[CLSCompliant(false)]
[EditorBrowsable(EditorBrowsableState.Never)]
public sealed class JSFunctionBinding
{
	internal JSFunctionBinding()
	{
		throw new PlatformNotSupportedException(System.SR.SystemRuntimeInteropServicesJavaScript_PlatformNotSupported);
	}

	public static void InvokeJS(JSFunctionBinding signature, Span<JSMarshalerArgument> arguments)
	{
		throw new PlatformNotSupportedException(System.SR.SystemRuntimeInteropServicesJavaScript_PlatformNotSupported);
	}

	public static JSFunctionBinding BindJSFunction(string functionName, string moduleName, ReadOnlySpan<JSMarshalerType> signatures)
	{
		throw new PlatformNotSupportedException(System.SR.SystemRuntimeInteropServicesJavaScript_PlatformNotSupported);
	}

	public static JSFunctionBinding BindManagedFunction(string fullyQualifiedName, int signatureHash, ReadOnlySpan<JSMarshalerType> signatures)
	{
		throw new PlatformNotSupportedException(System.SR.SystemRuntimeInteropServicesJavaScript_PlatformNotSupported);
	}
}
