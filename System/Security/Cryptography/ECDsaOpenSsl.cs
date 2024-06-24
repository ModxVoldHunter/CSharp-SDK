using System.Runtime.Versioning;

namespace System.Security.Cryptography;

public sealed class ECDsaOpenSsl : ECDsa
{
	[UnsupportedOSPlatform("android")]
	[UnsupportedOSPlatform("browser")]
	[UnsupportedOSPlatform("ios")]
	[UnsupportedOSPlatform("tvos")]
	[UnsupportedOSPlatform("windows")]
	public ECDsaOpenSsl()
	{
		throw new PlatformNotSupportedException(System.SR.PlatformNotSupported_CryptographyOpenSSL);
	}

	[UnsupportedOSPlatform("android")]
	[UnsupportedOSPlatform("browser")]
	[UnsupportedOSPlatform("ios")]
	[UnsupportedOSPlatform("tvos")]
	[UnsupportedOSPlatform("windows")]
	public ECDsaOpenSsl(int keySize)
	{
		throw new PlatformNotSupportedException(System.SR.PlatformNotSupported_CryptographyOpenSSL);
	}

	[UnsupportedOSPlatform("android")]
	[UnsupportedOSPlatform("browser")]
	[UnsupportedOSPlatform("ios")]
	[UnsupportedOSPlatform("tvos")]
	[UnsupportedOSPlatform("windows")]
	public ECDsaOpenSsl(nint handle)
	{
		throw new PlatformNotSupportedException(System.SR.PlatformNotSupported_CryptographyOpenSSL);
	}

	[UnsupportedOSPlatform("android")]
	[UnsupportedOSPlatform("browser")]
	[UnsupportedOSPlatform("ios")]
	[UnsupportedOSPlatform("tvos")]
	[UnsupportedOSPlatform("windows")]
	public ECDsaOpenSsl(ECCurve curve)
	{
		throw new PlatformNotSupportedException(System.SR.PlatformNotSupported_CryptographyOpenSSL);
	}

	[UnsupportedOSPlatform("android")]
	[UnsupportedOSPlatform("browser")]
	[UnsupportedOSPlatform("ios")]
	[UnsupportedOSPlatform("tvos")]
	[UnsupportedOSPlatform("windows")]
	public ECDsaOpenSsl(SafeEvpPKeyHandle pkeyHandle)
	{
		throw new PlatformNotSupportedException(System.SR.PlatformNotSupported_CryptographyOpenSSL);
	}

	public SafeEvpPKeyHandle DuplicateKeyHandle()
	{
		return null;
	}

	public override byte[] SignHash(byte[] hash)
	{
		return null;
	}

	public override bool VerifyHash(byte[] hash, byte[] signature)
	{
		return false;
	}
}
