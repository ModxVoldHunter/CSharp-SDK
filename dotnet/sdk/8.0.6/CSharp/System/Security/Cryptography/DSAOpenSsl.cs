using System.Runtime.Versioning;

namespace System.Security.Cryptography;

public sealed class DSAOpenSsl : DSA
{
	[UnsupportedOSPlatform("android")]
	[UnsupportedOSPlatform("browser")]
	[UnsupportedOSPlatform("ios")]
	[UnsupportedOSPlatform("tvos")]
	[UnsupportedOSPlatform("windows")]
	public DSAOpenSsl()
	{
		throw new PlatformNotSupportedException(System.SR.PlatformNotSupported_CryptographyOpenSSL);
	}

	[UnsupportedOSPlatform("android")]
	[UnsupportedOSPlatform("browser")]
	[UnsupportedOSPlatform("ios")]
	[UnsupportedOSPlatform("tvos")]
	[UnsupportedOSPlatform("windows")]
	public DSAOpenSsl(int keySize)
	{
		throw new PlatformNotSupportedException(System.SR.PlatformNotSupported_CryptographyOpenSSL);
	}

	[UnsupportedOSPlatform("android")]
	[UnsupportedOSPlatform("browser")]
	[UnsupportedOSPlatform("ios")]
	[UnsupportedOSPlatform("tvos")]
	[UnsupportedOSPlatform("windows")]
	public DSAOpenSsl(nint handle)
	{
		throw new PlatformNotSupportedException(System.SR.PlatformNotSupported_CryptographyOpenSSL);
	}

	[UnsupportedOSPlatform("android")]
	[UnsupportedOSPlatform("browser")]
	[UnsupportedOSPlatform("ios")]
	[UnsupportedOSPlatform("tvos")]
	[UnsupportedOSPlatform("windows")]
	public DSAOpenSsl(DSAParameters parameters)
	{
		throw new PlatformNotSupportedException(System.SR.PlatformNotSupported_CryptographyOpenSSL);
	}

	[UnsupportedOSPlatform("android")]
	[UnsupportedOSPlatform("browser")]
	[UnsupportedOSPlatform("ios")]
	[UnsupportedOSPlatform("tvos")]
	[UnsupportedOSPlatform("windows")]
	public DSAOpenSsl(SafeEvpPKeyHandle pkeyHandle)
	{
		throw new PlatformNotSupportedException(System.SR.PlatformNotSupported_CryptographyOpenSSL);
	}

	public SafeEvpPKeyHandle DuplicateKeyHandle()
	{
		return null;
	}

	public override byte[] CreateSignature(byte[] rgbHash)
	{
		return null;
	}

	public override DSAParameters ExportParameters(bool includePrivateParameters)
	{
		return default(DSAParameters);
	}

	public override void ImportParameters(DSAParameters parameters)
	{
	}

	public override bool VerifySignature(byte[] rgbHash, byte[] rgbSignature)
	{
		return false;
	}
}
