using System.Runtime.Versioning;

namespace System.Security.Cryptography;

[UnsupportedOSPlatform("browser")]
public class RSAPKCS1SignatureDeformatter : AsymmetricSignatureDeformatter
{
	private RSA _rsaKey;

	private string _algName;

	public RSAPKCS1SignatureDeformatter()
	{
	}

	public RSAPKCS1SignatureDeformatter(AsymmetricAlgorithm key)
	{
		ArgumentNullException.ThrowIfNull(key, "key");
		_rsaKey = (RSA)key;
	}

	public override void SetKey(AsymmetricAlgorithm key)
	{
		ArgumentNullException.ThrowIfNull(key, "key");
		_rsaKey = (RSA)key;
	}

	public override void SetHashAlgorithm(string strName)
	{
		if (CryptoConfig.MapNameToOID(strName) != null)
		{
			_algName = HashAlgorithmNames.ToUpper(strName);
		}
		else
		{
			_algName = null;
		}
	}

	public override bool VerifySignature(byte[] rgbHash, byte[] rgbSignature)
	{
		ArgumentNullException.ThrowIfNull(rgbHash, "rgbHash");
		ArgumentNullException.ThrowIfNull(rgbSignature, "rgbSignature");
		if (_algName == null)
		{
			throw new CryptographicUnexpectedOperationException(System.SR.Cryptography_FormatterMissingAlgorithm);
		}
		if (_rsaKey == null)
		{
			throw new CryptographicUnexpectedOperationException(System.SR.Cryptography_FormatterMissingKey);
		}
		return _rsaKey.VerifyHash(rgbHash, rgbSignature, new HashAlgorithmName(_algName), RSASignaturePadding.Pkcs1);
	}
}
