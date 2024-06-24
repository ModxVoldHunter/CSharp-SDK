namespace System.Security.Cryptography;

public class DSASignatureFormatter : AsymmetricSignatureFormatter
{
	private DSA _dsaKey;

	public DSASignatureFormatter()
	{
	}

	public DSASignatureFormatter(AsymmetricAlgorithm key)
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

	public override byte[] CreateSignature(byte[] rgbHash)
	{
		ArgumentNullException.ThrowIfNull(rgbHash, "rgbHash");
		if (_dsaKey == null)
		{
			throw new CryptographicUnexpectedOperationException(System.SR.Cryptography_FormatterMissingKey);
		}
		return _dsaKey.CreateSignature(rgbHash);
	}
}
