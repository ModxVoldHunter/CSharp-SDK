using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Runtime.Versioning;

namespace System;

public sealed class OperatingSystem : ISerializable, ICloneable
{
	private readonly Version _version;

	private readonly PlatformID _platform;

	private readonly string _servicePack;

	private string _versionString;

	public PlatformID Platform => _platform;

	public string ServicePack => _servicePack ?? string.Empty;

	public Version Version => _version;

	public string VersionString
	{
		get
		{
			if (_versionString == null)
			{
				string value = _platform switch
				{
					PlatformID.Win32S => "Microsoft Win32S ", 
					PlatformID.Win32Windows => (_version.Major > 4 || (_version.Major == 4 && _version.Minor > 0)) ? "Microsoft Windows 98 " : "Microsoft Windows 95 ", 
					PlatformID.Win32NT => "Microsoft Windows NT ", 
					PlatformID.WinCE => "Microsoft Windows CE ", 
					PlatformID.Unix => "Unix ", 
					PlatformID.Xbox => "Xbox ", 
					PlatformID.MacOSX => "Mac OS X ", 
					PlatformID.Other => "Other ", 
					_ => "<unknown> ", 
				};
				Span<char> span = stackalloc char[128];
				string versionString;
				if (!string.IsNullOrEmpty(_servicePack))
				{
					IFormatProvider formatProvider = null;
					IFormatProvider provider = formatProvider;
					Span<char> span2 = span;
					Span<char> initialBuffer = span2;
					DefaultInterpolatedStringHandler handler = new DefaultInterpolatedStringHandler(1, 3, formatProvider, span2);
					handler.AppendFormatted(value);
					handler.AppendFormatted(_version.ToString(3));
					handler.AppendLiteral(" ");
					handler.AppendFormatted(_servicePack);
					versionString = string.Create(provider, initialBuffer, ref handler);
				}
				else
				{
					IFormatProvider formatProvider = null;
					IFormatProvider provider2 = formatProvider;
					Span<char> span2 = span;
					Span<char> initialBuffer2 = span2;
					DefaultInterpolatedStringHandler handler2 = new DefaultInterpolatedStringHandler(0, 2, formatProvider, span2);
					handler2.AppendFormatted(value);
					handler2.AppendFormatted(_version);
					versionString = string.Create(provider2, initialBuffer2, ref handler2);
				}
				_versionString = versionString;
			}
			return _versionString;
		}
	}

	public OperatingSystem(PlatformID platform, Version version)
		: this(platform, version, null)
	{
	}

	internal OperatingSystem(PlatformID platform, Version version, string servicePack)
	{
		if (platform < PlatformID.Win32S || platform > PlatformID.Other)
		{
			throw new ArgumentOutOfRangeException("platform", platform, SR.Format(SR.Arg_EnumIllegalVal, platform));
		}
		ArgumentNullException.ThrowIfNull(version, "version");
		_platform = platform;
		_version = version;
		_servicePack = servicePack;
	}

	[Obsolete("This API supports obsolete formatter-based serialization. It should not be called or extended by application code.", DiagnosticId = "SYSLIB0051", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
	[EditorBrowsable(EditorBrowsableState.Never)]
	public void GetObjectData(SerializationInfo info, StreamingContext context)
	{
		throw new PlatformNotSupportedException();
	}

	public object Clone()
	{
		return new OperatingSystem(_platform, _version, _servicePack);
	}

	public override string ToString()
	{
		return VersionString;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool IsOSPlatform(string platform)
	{
		ArgumentNullException.ThrowIfNull(platform, "platform");
		return platform.Equals("WINDOWS", StringComparison.OrdinalIgnoreCase);
	}

	public static bool IsOSPlatformVersionAtLeast(string platform, int major, int minor = 0, int build = 0, int revision = 0)
	{
		if (IsOSPlatform(platform))
		{
			return IsOSVersionAtLeast(major, minor, build, revision);
		}
		return false;
	}

	[NonVersionable]
	public static bool IsBrowser()
	{
		return false;
	}

	[NonVersionable]
	public static bool IsWasi()
	{
		return false;
	}

	[NonVersionable]
	public static bool IsLinux()
	{
		return false;
	}

	[NonVersionable]
	public static bool IsFreeBSD()
	{
		return false;
	}

	public static bool IsFreeBSDVersionAtLeast(int major, int minor = 0, int build = 0, int revision = 0)
	{
		if (false)
		{
		}
		return false;
	}

	[NonVersionable]
	public static bool IsAndroid()
	{
		return false;
	}

	public static bool IsAndroidVersionAtLeast(int major, int minor = 0, int build = 0, int revision = 0)
	{
		if (false)
		{
		}
		return false;
	}

	[SupportedOSPlatformGuard("maccatalyst")]
	[NonVersionable]
	public static bool IsIOS()
	{
		return false;
	}

	[SupportedOSPlatformGuard("maccatalyst")]
	[NonVersionable]
	public static bool IsIOSVersionAtLeast(int major, int minor = 0, int build = 0)
	{
		if (false)
		{
		}
		return false;
	}

	[NonVersionable]
	public static bool IsMacOS()
	{
		return false;
	}

	public static bool IsMacOSVersionAtLeast(int major, int minor = 0, int build = 0)
	{
		if (false)
		{
		}
		return false;
	}

	[NonVersionable]
	public static bool IsMacCatalyst()
	{
		return false;
	}

	public static bool IsMacCatalystVersionAtLeast(int major, int minor = 0, int build = 0)
	{
		if (false)
		{
		}
		return false;
	}

	[NonVersionable]
	public static bool IsTvOS()
	{
		return false;
	}

	public static bool IsTvOSVersionAtLeast(int major, int minor = 0, int build = 0)
	{
		if (false)
		{
		}
		return false;
	}

	[NonVersionable]
	public static bool IsWatchOS()
	{
		return false;
	}

	public static bool IsWatchOSVersionAtLeast(int major, int minor = 0, int build = 0)
	{
		if (false)
		{
		}
		return false;
	}

	[NonVersionable]
	public static bool IsWindows()
	{
		return true;
	}

	public static bool IsWindowsVersionAtLeast(int major, int minor = 0, int build = 0, int revision = 0)
	{
		_ = 1;
		return IsOSVersionAtLeast(major, minor, build, revision);
	}

	private static bool IsOSVersionAtLeast(int major, int minor, int build, int revision)
	{
		Version version = Environment.OSVersion.Version;
		if (version.Major != major)
		{
			return version.Major > major;
		}
		if (version.Minor != minor)
		{
			return version.Minor > minor;
		}
		if (version.Build != build)
		{
			return version.Build > build;
		}
		if (version.Revision < revision)
		{
			if (version.Revision == -1)
			{
				return revision == 0;
			}
			return false;
		}
		return true;
	}
}
