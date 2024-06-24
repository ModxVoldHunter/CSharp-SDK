namespace System.Security.Cryptography;

public class RSAPKCS1KeyExchangeFormatter : AsymmetricKeyExchangeFormatter
{
	private RSA _rsaKey;

	private RandomNumberGenerator RngValue;

	public override string Parameters => "<enc:KeyEncryptionMethod enc:Algorithm=\"http://www.microsoft.com/xml/security/algorithm/PKCS1-v1.5-KeyEx\" xmlns:enc=\"http://www.microsoft.com/xml/security/encryption/v1.0\" />";

	public RandomNumberGenerator? Rng
	{
		get
		{
			return RngValue;
		}
		set
		{
			RngValue = value;
		}
	}

	public RSAPKCS1KeyExchangeFormatter()
	{
	}

	public RSAPKCS1KeyExchangeFormatter(AsymmetricAlgorithm key)
	{
		ArgumentNullException.ThrowIfNull(key, "key");
		_rsaKey = (RSA)key;
	}

	public override void SetKey(AsymmetricAlgorithm key)
	{
		ArgumentNullException.ThrowIfNull(key, "key");
		_rsaKey = (RSA)key;
	}

	public override byte[] CreateKeyExchange(byte[] rgbData, Type? symAlgType)
	{
		return CreateKeyExchange(rgbData);
	}

	public override byte[] CreateKeyExchange(byte[] rgbData)
	{
		if (_rsaKey == null)
		{
			throw new CryptographicUnexpectedOperationException(System.SR.Cryptography_FormatterMissingKey);
		}
		return _rsaKey.Encrypt(rgbData, RSAEncryptionPadding.Pkcs1);
	}
}
