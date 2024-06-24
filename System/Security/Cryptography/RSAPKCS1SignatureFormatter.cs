using System.Runtime.Versioning;

namespace System.Security.Cryptography;

[UnsupportedOSPlatform("browser")]
public class RSAPKCS1SignatureFormatter : AsymmetricSignatureFormatter
{
	private RSA _rsaKey;

	private string _algName;

	public RSAPKCS1SignatureFormatter()
	{
	}

	public RSAPKCS1SignatureFormatter(AsymmetricAlgorithm key)
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

	public override byte[] CreateSignature(byte[] rgbHash)
	{
		ArgumentNullException.ThrowIfNull(rgbHash, "rgbHash");
		if (_algName == null)
		{
			throw new CryptographicUnexpectedOperationException(System.SR.Cryptography_FormatterMissingAlgorithm);
		}
		if (_rsaKey == null)
		{
			throw new CryptographicUnexpectedOperationException(System.SR.Cryptography_FormatterMissingKey);
		}
		return _rsaKey.SignHash(rgbHash, new HashAlgorithmName(_algName), RSASignaturePadding.Pkcs1);
	}
}
