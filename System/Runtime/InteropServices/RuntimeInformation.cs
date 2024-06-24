using System.Reflection;
using System.Runtime.CompilerServices;

namespace System.Runtime.InteropServices;

public static class RuntimeInformation
{
	private static string s_frameworkDescription;

	private static string s_runtimeIdentifier;

	private static string s_osDescription;

	private static volatile int s_osArchPlusOne;

	public static string FrameworkDescription
	{
		get
		{
			if (s_frameworkDescription == null)
			{
				ReadOnlySpan<char> readOnlySpan = typeof(object).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
				int num = readOnlySpan.IndexOf('+');
				if (num >= 0)
				{
					readOnlySpan = readOnlySpan.Slice(0, num);
				}
				s_frameworkDescription = ((!readOnlySpan.Trim().IsEmpty) ? $"{".NET"} {readOnlySpan}" : ".NET");
			}
			return s_frameworkDescription;
		}
	}

	public static string RuntimeIdentifier
	{
		get
		{
			object obj = s_runtimeIdentifier;
			if (obj == null)
			{
				obj = (AppContext.GetData("RUNTIME_IDENTIFIER") as string) ?? "unknown";
				s_runtimeIdentifier = (string)obj;
			}
			return (string)obj;
		}
	}

	public static Architecture ProcessArchitecture => Architecture.X64;

	public static string OSDescription
	{
		get
		{
			string text = s_osDescription;
			if (text == null)
			{
				OperatingSystem oSVersion = Environment.OSVersion;
				Version version = oSVersion.Version;
				Span<char> span = stackalloc char[256];
				string text2;
				if (!string.IsNullOrEmpty(oSVersion.ServicePack))
				{
					IFormatProvider formatProvider = null;
					IFormatProvider provider = formatProvider;
					Span<char> span2 = span;
					Span<char> initialBuffer = span2;
					DefaultInterpolatedStringHandler handler = new DefaultInterpolatedStringHandler(4, 5, formatProvider, span2);
					handler.AppendFormatted("Microsoft Windows");
					handler.AppendLiteral(" ");
					handler.AppendFormatted((uint)version.Major);
					handler.AppendLiteral(".");
					handler.AppendFormatted((uint)version.Minor);
					handler.AppendLiteral(".");
					handler.AppendFormatted((uint)version.Build);
					handler.AppendLiteral(" ");
					handler.AppendFormatted(oSVersion.ServicePack);
					text2 = string.Create(provider, initialBuffer, ref handler);
				}
				else
				{
					IFormatProvider formatProvider = null;
					IFormatProvider provider2 = formatProvider;
					Span<char> span2 = span;
					Span<char> initialBuffer2 = span2;
					DefaultInterpolatedStringHandler handler2 = new DefaultInterpolatedStringHandler(3, 4, formatProvider, span2);
					handler2.AppendFormatted("Microsoft Windows");
					handler2.AppendLiteral(" ");
					handler2.AppendFormatted((uint)version.Major);
					handler2.AppendLiteral(".");
					handler2.AppendFormatted((uint)version.Minor);
					handler2.AppendLiteral(".");
					handler2.AppendFormatted((uint)version.Build);
					text2 = string.Create(provider2, initialBuffer2, ref handler2);
				}
				text = text2;
				s_osDescription = text2;
			}
			return text;
		}
	}

	public unsafe static Architecture OSArchitecture
	{
		get
		{
			int num = s_osArchPlusOne - 1;
			if (num < 0)
			{
				nint handle = Interop.Kernel32.LoadLibraryEx("kernel32.dll", 0, 2048);
				if (NativeLibrary.TryGetExport(handle, "IsWow64Process2", out var address))
				{
					Unsafe.SkipInit(out ushort num2);
					Unsafe.SkipInit(out ushort processMachine);
					num = (int)((((delegate* unmanaged<nint, ushort*, ushort*, int>)address)(Interop.Kernel32.GetCurrentProcess(), &num2, &processMachine) == 0) ? Architecture.X64 : MapMachineConstant(processMachine));
				}
				else
				{
					Unsafe.SkipInit(out Interop.Kernel32.SYSTEM_INFO sYSTEM_INFO);
					Interop.Kernel32.GetNativeSystemInfo(&sYSTEM_INFO);
					num = (int)Map(sYSTEM_INFO.wProcessorArchitecture);
				}
				s_osArchPlusOne = num + 1;
			}
			return (Architecture)num;
		}
	}

	public static bool IsOSPlatform(OSPlatform osPlatform)
	{
		return OperatingSystem.IsOSPlatform(osPlatform.Name);
	}

	private static Architecture Map(int processorArchitecture)
	{
		return processorArchitecture switch
		{
			12 => Architecture.Arm64, 
			5 => Architecture.Arm, 
			9 => Architecture.X64, 
			_ => Architecture.X86, 
		};
	}

	private static Architecture MapMachineConstant(ushort processMachine)
	{
		return processMachine switch
		{
			452 => Architecture.Arm, 
			34404 => Architecture.X64, 
			43620 => Architecture.Arm64, 
			332 => Architecture.X86, 
			_ => Architecture.X64, 
		};
	}
}
