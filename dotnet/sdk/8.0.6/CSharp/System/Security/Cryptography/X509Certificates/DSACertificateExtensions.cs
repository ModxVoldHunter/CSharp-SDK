using System.Runtime.Versioning;
using Internal.Cryptography;

namespace System.Security.Cryptography.X509Certificates;

[UnsupportedOSPlatform("ios")]
[UnsupportedOSPlatform("tvos")]
public static class DSACertificateExtensions
{
	public static DSA? GetDSAPublicKey(this X509Certificate2 certificate)
	{
		return certificate.GetPublicKey<DSA>();
	}

	public static DSA? GetDSAPrivateKey(this X509Certificate2 certificate)
	{
		return certificate.GetPrivateKey<DSA>();
	}

	public static X509Certificate2 CopyWithPrivateKey(this X509Certificate2 certificate, DSA privateKey)
	{
		ArgumentNullException.ThrowIfNull(certificate, "certificate");
		ArgumentNullException.ThrowIfNull(privateKey, "privateKey");
		if (certificate.HasPrivateKey)
		{
			throw new InvalidOperationException(System.SR.Cryptography_Cert_AlreadyHasPrivateKey);
		}
		using (DSA dSA = certificate.GetDSAPublicKey())
		{
			if (dSA == null)
			{
				throw new ArgumentException(System.SR.Cryptography_PrivateKey_WrongAlgorithm);
			}
			DSAParameters dSAParameters = dSA.ExportParameters(includePrivateParameters: false);
			DSAParameters dSAParameters2 = privateKey.ExportParameters(includePrivateParameters: false);
			if (!dSAParameters.G.ContentsEqual(dSAParameters2.G) || !dSAParameters.P.ContentsEqual(dSAParameters2.P) || !dSAParameters.Q.ContentsEqual(dSAParameters2.Q) || !dSAParameters.Y.ContentsEqual(dSAParameters2.Y))
			{
				throw new ArgumentException(System.SR.Cryptography_PrivateKey_DoesNotMatch, "privateKey");
			}
		}
		ICertificatePal pal = certificate.Pal.CopyWithPrivateKey(privateKey);
		return new X509Certificate2(pal);
	}
}
