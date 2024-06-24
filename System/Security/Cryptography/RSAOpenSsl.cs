using System.Runtime.Versioning;

namespace System.Security.Cryptography;

public sealed class RSAOpenSsl : RSA
{
	[UnsupportedOSPlatform("android")]
	[UnsupportedOSPlatform("browser")]
	[UnsupportedOSPlatform("ios")]
	[UnsupportedOSPlatform("tvos")]
	[UnsupportedOSPlatform("windows")]
	public RSAOpenSsl()
	{
		throw new PlatformNotSupportedException(System.SR.PlatformNotSupported_CryptographyOpenSSL);
	}

	[UnsupportedOSPlatform("android")]
	[UnsupportedOSPlatform("browser")]
	[UnsupportedOSPlatform("ios")]
	[UnsupportedOSPlatform("tvos")]
	[UnsupportedOSPlatform("windows")]
	public RSAOpenSsl(int keySize)
	{
		throw new PlatformNotSupportedException(System.SR.PlatformNotSupported_CryptographyOpenSSL);
	}

	[UnsupportedOSPlatform("android")]
	[UnsupportedOSPlatform("browser")]
	[UnsupportedOSPlatform("ios")]
	[UnsupportedOSPlatform("tvos")]
	[UnsupportedOSPlatform("windows")]
	public RSAOpenSsl(nint handle)
	{
		throw new PlatformNotSupportedException(System.SR.PlatformNotSupported_CryptographyOpenSSL);
	}

	[UnsupportedOSPlatform("android")]
	[UnsupportedOSPlatform("browser")]
	[UnsupportedOSPlatform("ios")]
	[UnsupportedOSPlatform("tvos")]
	[UnsupportedOSPlatform("windows")]
	public RSAOpenSsl(RSAParameters parameters)
	{
		throw new PlatformNotSupportedException(System.SR.PlatformNotSupported_CryptographyOpenSSL);
	}

	[UnsupportedOSPlatform("android")]
	[UnsupportedOSPlatform("browser")]
	[UnsupportedOSPlatform("ios")]
	[UnsupportedOSPlatform("tvos")]
	[UnsupportedOSPlatform("windows")]
	public RSAOpenSsl(SafeEvpPKeyHandle pkeyHandle)
	{
		throw new PlatformNotSupportedException(System.SR.PlatformNotSupported_CryptographyOpenSSL);
	}

	public SafeEvpPKeyHandle DuplicateKeyHandle()
	{
		return null;
	}

	public override RSAParameters ExportParameters(bool includePrivateParameters)
	{
		return default(RSAParameters);
	}

	public override void ImportParameters(RSAParameters parameters)
	{
	}
}
