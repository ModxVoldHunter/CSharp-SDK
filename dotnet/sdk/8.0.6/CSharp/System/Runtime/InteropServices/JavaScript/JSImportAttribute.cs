using System.Runtime.Versioning;

namespace System.Runtime.InteropServices.JavaScript;

[AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
[SupportedOSPlatform("browser")]
public sealed class JSImportAttribute : Attribute
{
	public string FunctionName
	{
		get
		{
			throw new PlatformNotSupportedException(System.SR.SystemRuntimeInteropServicesJavaScript_PlatformNotSupported);
		}
	}

	public string? ModuleName
	{
		get
		{
			throw new PlatformNotSupportedException(System.SR.SystemRuntimeInteropServicesJavaScript_PlatformNotSupported);
		}
	}

	public JSImportAttribute(string functionName)
	{
		throw new PlatformNotSupportedException(System.SR.SystemRuntimeInteropServicesJavaScript_PlatformNotSupported);
	}

	public JSImportAttribute(string functionName, string moduleName)
	{
		throw new PlatformNotSupportedException(System.SR.SystemRuntimeInteropServicesJavaScript_PlatformNotSupported);
	}
}
