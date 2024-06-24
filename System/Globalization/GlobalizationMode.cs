using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace System.Globalization;

internal static class GlobalizationMode
{
	private static class Settings
	{
		internal static bool Invariant { get; } = AppContextConfigHelper.GetBooleanConfig("System.Globalization.Invariant", "DOTNET_SYSTEM_GLOBALIZATION_INVARIANT");


		internal static bool PredefinedCulturesOnly { get; } = AppContextConfigHelper.GetBooleanConfig("System.Globalization.PredefinedCulturesOnly", "DOTNET_SYSTEM_GLOBALIZATION_PREDEFINED_CULTURES_ONLY", GlobalizationMode.Invariant);

	}

	internal static bool Invariant => Settings.Invariant;

	internal static bool PredefinedCulturesOnly => Settings.PredefinedCulturesOnly;

	internal static bool UseNls { get; } = !Invariant && (AppContextConfigHelper.GetBooleanConfig("System.Globalization.UseNls", "DOTNET_SYSTEM_GLOBALIZATION_USENLS") || !LoadIcu());


	private static bool TryGetAppLocalIcuSwitchValue([NotNullWhen(true)] out string value)
	{
		return TryGetStringValue("System.Globalization.AppLocalIcu", "DOTNET_SYSTEM_GLOBALIZATION_APPLOCALICU", out value);
	}

	private static bool TryGetStringValue(string switchName, string envVariable, [NotNullWhen(true)] out string value)
	{
		value = AppContext.GetData(switchName) as string;
		if (string.IsNullOrEmpty(value))
		{
			value = Environment.GetEnvironmentVariable(envVariable);
			if (string.IsNullOrEmpty(value))
			{
				return false;
			}
		}
		return true;
	}

	private static void LoadAppLocalIcu(string icuSuffixAndVersion)
	{
		ReadOnlySpan<char> suffix = default(ReadOnlySpan<char>);
		int num = icuSuffixAndVersion.IndexOf(':');
		ReadOnlySpan<char> version;
		if (num >= 0)
		{
			suffix = icuSuffixAndVersion.AsSpan(0, num);
			version = icuSuffixAndVersion.AsSpan(suffix.Length + 1);
		}
		else
		{
			version = icuSuffixAndVersion;
		}
		LoadAppLocalIcuCore(version, suffix);
	}

	private static string CreateLibraryName(ReadOnlySpan<char> baseName, ReadOnlySpan<char> suffix, ReadOnlySpan<char> extension, ReadOnlySpan<char> version, bool versionAtEnd = false)
	{
		if (!versionAtEnd)
		{
			return string.Concat(baseName, suffix, version, extension);
		}
		return string.Concat(baseName, suffix, extension, version);
	}

	private static nint LoadLibrary(string library, bool failOnLoadFailure)
	{
		if (!NativeLibrary.TryLoad(library, typeof(object).Assembly, DllImportSearchPath.ApplicationDirectory | DllImportSearchPath.System32, out var handle) && failOnLoadFailure)
		{
			Environment.FailFast("Failed to load app-local ICU: " + library);
		}
		return handle;
	}

	private static bool LoadIcu()
	{
		if (!TryGetAppLocalIcuSwitchValue(out var value))
		{
			return Interop.Globalization.LoadICU() != 0;
		}
		LoadAppLocalIcu(value);
		return true;
	}

	private static void LoadAppLocalIcuCore(ReadOnlySpan<char> version, ReadOnlySpan<char> suffix)
	{
		nint num = IntPtr.Zero;
		nint num2 = IntPtr.Zero;
		int num3 = version.IndexOf('.');
		if (num3 > 0)
		{
			ReadOnlySpan<char> version2 = version.Slice(0, num3);
			num = LoadLibrary(CreateLibraryName("icuuc", suffix, ".dll", version2), failOnLoadFailure: false);
			if (num != IntPtr.Zero)
			{
				num2 = LoadLibrary(CreateLibraryName("icuin", suffix, ".dll", version2), failOnLoadFailure: false);
			}
		}
		if (num == IntPtr.Zero)
		{
			num = LoadLibrary(CreateLibraryName("icuuc", suffix, ".dll", version), failOnLoadFailure: true);
		}
		if (num2 == IntPtr.Zero)
		{
			num2 = LoadLibrary(CreateLibraryName("icuin", suffix, ".dll", version), failOnLoadFailure: true);
		}
		Interop.Globalization.InitICUFunctions(num, num2, version, suffix);
	}
}
