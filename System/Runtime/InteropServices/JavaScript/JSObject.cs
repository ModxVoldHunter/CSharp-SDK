using System.Runtime.Versioning;

namespace System.Runtime.InteropServices.JavaScript;

[SupportedOSPlatform("browser")]
public class JSObject : IDisposable
{
	public bool IsDisposed
	{
		get
		{
			throw new PlatformNotSupportedException(System.SR.SystemRuntimeInteropServicesJavaScript_PlatformNotSupported);
		}
	}

	internal JSObject()
	{
		throw new PlatformNotSupportedException(System.SR.SystemRuntimeInteropServicesJavaScript_PlatformNotSupported);
	}

	public void Dispose()
	{
	}

	public bool HasProperty(string propertyName)
	{
		throw new PlatformNotSupportedException(System.SR.SystemRuntimeInteropServicesJavaScript_PlatformNotSupported);
	}

	public string GetTypeOfProperty(string propertyName)
	{
		throw new PlatformNotSupportedException(System.SR.SystemRuntimeInteropServicesJavaScript_PlatformNotSupported);
	}

	public bool GetPropertyAsBoolean(string propertyName)
	{
		throw new PlatformNotSupportedException(System.SR.SystemRuntimeInteropServicesJavaScript_PlatformNotSupported);
	}

	public int GetPropertyAsInt32(string propertyName)
	{
		throw new PlatformNotSupportedException(System.SR.SystemRuntimeInteropServicesJavaScript_PlatformNotSupported);
	}

	public double GetPropertyAsDouble(string propertyName)
	{
		throw new PlatformNotSupportedException(System.SR.SystemRuntimeInteropServicesJavaScript_PlatformNotSupported);
	}

	public string? GetPropertyAsString(string propertyName)
	{
		throw new PlatformNotSupportedException(System.SR.SystemRuntimeInteropServicesJavaScript_PlatformNotSupported);
	}

	public JSObject? GetPropertyAsJSObject(string propertyName)
	{
		throw new PlatformNotSupportedException(System.SR.SystemRuntimeInteropServicesJavaScript_PlatformNotSupported);
	}

	public byte[]? GetPropertyAsByteArray(string propertyName)
	{
		throw new PlatformNotSupportedException(System.SR.SystemRuntimeInteropServicesJavaScript_PlatformNotSupported);
	}

	public void SetProperty(string propertyName, bool value)
	{
		throw new PlatformNotSupportedException(System.SR.SystemRuntimeInteropServicesJavaScript_PlatformNotSupported);
	}

	public void SetProperty(string propertyName, int value)
	{
		throw new PlatformNotSupportedException(System.SR.SystemRuntimeInteropServicesJavaScript_PlatformNotSupported);
	}

	public void SetProperty(string propertyName, double value)
	{
		throw new PlatformNotSupportedException(System.SR.SystemRuntimeInteropServicesJavaScript_PlatformNotSupported);
	}

	public void SetProperty(string propertyName, string? value)
	{
		throw new PlatformNotSupportedException(System.SR.SystemRuntimeInteropServicesJavaScript_PlatformNotSupported);
	}

	public void SetProperty(string propertyName, JSObject? value)
	{
		throw new PlatformNotSupportedException(System.SR.SystemRuntimeInteropServicesJavaScript_PlatformNotSupported);
	}

	public void SetProperty(string propertyName, byte[]? value)
	{
		throw new PlatformNotSupportedException(System.SR.SystemRuntimeInteropServicesJavaScript_PlatformNotSupported);
	}
}
