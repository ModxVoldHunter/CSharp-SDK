using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace System.Security.Cryptography;

public sealed class SafeEvpPKeyHandle : SafeHandle
{
	[UnsupportedOSPlatform("android")]
	[UnsupportedOSPlatform("browser")]
	[UnsupportedOSPlatform("ios")]
	[UnsupportedOSPlatform("tvos")]
	[UnsupportedOSPlatform("windows")]
	public static long OpenSslVersion
	{
		get
		{
			throw new PlatformNotSupportedException(System.SR.PlatformNotSupported_CryptographyOpenSSL);
		}
	}

	public override bool IsInvalid => true;

	[UnsupportedOSPlatform("android")]
	[UnsupportedOSPlatform("browser")]
	[UnsupportedOSPlatform("ios")]
	[UnsupportedOSPlatform("tvos")]
	[UnsupportedOSPlatform("windows")]
	public SafeEvpPKeyHandle()
		: base(IntPtr.Zero, ownsHandle: false)
	{
		throw new PlatformNotSupportedException(System.SR.PlatformNotSupported_CryptographyOpenSSL);
	}

	[UnsupportedOSPlatform("android")]
	[UnsupportedOSPlatform("browser")]
	[UnsupportedOSPlatform("ios")]
	[UnsupportedOSPlatform("tvos")]
	[UnsupportedOSPlatform("windows")]
	public SafeEvpPKeyHandle(nint handle, bool ownsHandle)
		: base(handle, ownsHandle)
	{
		throw new PlatformNotSupportedException(System.SR.PlatformNotSupported_CryptographyOpenSSL);
	}

	[UnsupportedOSPlatform("android")]
	[UnsupportedOSPlatform("browser")]
	[UnsupportedOSPlatform("ios")]
	[UnsupportedOSPlatform("tvos")]
	[UnsupportedOSPlatform("windows")]
	public static SafeEvpPKeyHandle OpenPrivateKeyFromEngine(string engineName, string keyId)
	{
		throw new PlatformNotSupportedException(System.SR.PlatformNotSupported_CryptographyOpenSSL);
	}

	[UnsupportedOSPlatform("android")]
	[UnsupportedOSPlatform("browser")]
	[UnsupportedOSPlatform("ios")]
	[UnsupportedOSPlatform("tvos")]
	[UnsupportedOSPlatform("windows")]
	public static SafeEvpPKeyHandle OpenPublicKeyFromEngine(string engineName, string keyId)
	{
		throw new PlatformNotSupportedException(System.SR.PlatformNotSupported_CryptographyOpenSSL);
	}

	public SafeEvpPKeyHandle DuplicateHandle()
	{
		return null;
	}

	protected override bool ReleaseHandle()
	{
		return false;
	}
}
