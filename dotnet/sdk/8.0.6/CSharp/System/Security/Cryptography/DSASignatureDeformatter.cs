namespace System.Security.Cryptography;

public class DSASignatureDeformatter : AsymmetricSignatureDeformatter
{
	private DSA _dsaKey;

	public DSASignatureDeformatter()
	{
	}

	public DSASignatureDeformatter(AsymmetricAlgorithm key)
		: this()
	{
		ArgumentNullException.ThrowIfNull(key, "key");
		_dsaKey = (DSA)key;
	}

	public override void SetKey(AsymmetricAlgorithm key)
	{
		ArgumentNullException.ThrowIfNull(key, "key");
		_dsaKey = (DSA)key;
	}

	public override void SetHashAlgorithm(string strName)
	{
		if (!strName.Equals("SHA1", StringComparison.InvariantCultureIgnoreCase))
		{
			throw new CryptographicUnexpectedOperationException(System.SR.Cryptography_InvalidOperation);
		}
	}

	public override bool VerifySignature(byte[] rgbHash, byte[] rgbSignature)
	{
		ArgumentNullException.ThrowIfNull(rgbHash, "rgbHash");
		ArgumentNullException.ThrowIfNull(rgbSignature, "rgbSignature");
		if (_dsaKey == null)
		{
			throw new CryptographicUnexpectedOperationException(System.SR.Cryptography_FormatterMissingKey);
		}
		return _dsaKey.VerifySignature(rgbHash, rgbSignature);
	}
}
