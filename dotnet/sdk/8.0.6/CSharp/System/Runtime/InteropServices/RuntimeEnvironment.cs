using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Reflection;

namespace System.Runtime.InteropServices;

public static class RuntimeEnvironment
{
	[Obsolete("RuntimeEnvironment members SystemConfigurationFile, GetRuntimeInterfaceAsIntPtr, and GetRuntimeInterfaceAsObject are not supported and throw PlatformNotSupportedException.", DiagnosticId = "SYSLIB0019", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
	public static string SystemConfigurationFile
	{
		get
		{
			throw new PlatformNotSupportedException();
		}
	}

	public static bool FromGlobalAccessCache(Assembly a)
	{
		return false;
	}

	[UnconditionalSuppressMessage("SingleFile", "IL3000: Avoid accessing Assembly file path when publishing as a single file", Justification = "This call is fine because the code handles the Assembly.Location equals null by calling AppDomain.CurrentDomain.BaseDirectory")]
	public static string GetRuntimeDirectory()
	{
		string path = typeof(object).Assembly.Location;
		if (!Path.IsPathRooted(path))
		{
			path = AppDomain.CurrentDomain.BaseDirectory;
		}
		char reference = Path.DirectorySeparatorChar;
		return Path.GetDirectoryName(path) + new ReadOnlySpan<char>(ref reference);
	}

	[Obsolete("RuntimeEnvironment members SystemConfigurationFile, GetRuntimeInterfaceAsIntPtr, and GetRuntimeInterfaceAsObject are not supported and throw PlatformNotSupportedException.", DiagnosticId = "SYSLIB0019", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
	public static nint GetRuntimeInterfaceAsIntPtr(Guid clsid, Guid riid)
	{
		throw new PlatformNotSupportedException();
	}

	[Obsolete("RuntimeEnvironment members SystemConfigurationFile, GetRuntimeInterfaceAsIntPtr, and GetRuntimeInterfaceAsObject are not supported and throw PlatformNotSupportedException.", DiagnosticId = "SYSLIB0019", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
	public static object GetRuntimeInterfaceAsObject(Guid clsid, Guid riid)
	{
		throw new PlatformNotSupportedException();
	}

	public static string GetSystemVersion()
	{
		return $"v{Environment.Version}";
	}
}
