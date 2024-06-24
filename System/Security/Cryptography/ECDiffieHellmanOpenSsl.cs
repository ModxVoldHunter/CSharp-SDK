using System.Runtime.Versioning;

namespace System.Security.Cryptography;

public sealed class ECDiffieHellmanOpenSsl : ECDiffieHellman
{
	public override ECDiffieHellmanPublicKey PublicKey => null;

	[UnsupportedOSPlatform("android")]
	[UnsupportedOSPlatform("browser")]
	[UnsupportedOSPlatform("ios")]
	[UnsupportedOSPlatform("tvos")]
	[UnsupportedOSPlatform("windows")]
	public ECDiffieHellmanOpenSsl()
	{
		throw new PlatformNotSupportedException(System.SR.PlatformNotSupported_CryptographyOpenSSL);
	}

	[UnsupportedOSPlatform("android")]
	[UnsupportedOSPlatform("browser")]
	[UnsupportedOSPlatform("ios")]
	[UnsupportedOSPlatform("tvos")]
	[UnsupportedOSPlatform("windows")]
	public ECDiffieHellmanOpenSsl(int keySize)
	{
		throw new PlatformNotSupportedException(System.SR.PlatformNotSupported_CryptographyOpenSSL);
	}

	[UnsupportedOSPlatform("android")]
	[UnsupportedOSPlatform("browser")]
	[UnsupportedOSPlatform("ios")]
	[UnsupportedOSPlatform("tvos")]
	[UnsupportedOSPlatform("windows")]
	public ECDiffieHellmanOpenSsl(nint handle)
	{
		throw new PlatformNotSupportedException(System.SR.PlatformNotSupported_CryptographyOpenSSL);
	}

	[UnsupportedOSPlatform("android")]
	[UnsupportedOSPlatform("browser")]
	[UnsupportedOSPlatform("ios")]
	[UnsupportedOSPlatform("tvos")]
	[UnsupportedOSPlatform("windows")]
	public ECDiffieHellmanOpenSsl(ECCurve curve)
	{
		throw new PlatformNotSupportedException(System.SR.PlatformNotSupported_CryptographyOpenSSL);
	}

	[UnsupportedOSPlatform("android")]
	[UnsupportedOSPlatform("browser")]
	[UnsupportedOSPlatform("ios")]
	[UnsupportedOSPlatform("tvos")]
	[UnsupportedOSPlatform("windows")]
	public ECDiffieHellmanOpenSsl(SafeEvpPKeyHandle pkeyHandle)
	{
		throw new PlatformNotSupportedException(System.SR.PlatformNotSupported_CryptographyOpenSSL);
	}

	public SafeEvpPKeyHandle DuplicateKeyHandle()
	{
		return null;
	}

	public override ECParameters ExportParameters(bool includePrivateParameters)
	{
		return default(ECParameters);
	}

	public override void ImportParameters(ECParameters parameters)
	{
	}
}
