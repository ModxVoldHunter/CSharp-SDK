namespace System.Security.Cryptography;

public class RSAPKCS1KeyExchangeDeformatter : AsymmetricKeyExchangeDeformatter
{
	private RSA _rsaKey;

	private RandomNumberGenerator RngValue;

	public RandomNumberGenerator? RNG
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

	public override string? Parameters
	{
		get
		{
			return null;
		}
		set
		{
		}
	}

	public RSAPKCS1KeyExchangeDeformatter()
	{
	}

	public RSAPKCS1KeyExchangeDeformatter(AsymmetricAlgorithm key)
	{
		ArgumentNullException.ThrowIfNull(key, "key");
		_rsaKey = (RSA)key;
	}

	public override byte[] DecryptKeyExchange(byte[] rgbIn)
	{
		if (_rsaKey == null)
		{
			throw new CryptographicUnexpectedOperationException(System.SR.Cryptography_FormatterMissingKey);
		}
		return _rsaKey.Decrypt(rgbIn, RSAEncryptionPadding.Pkcs1);
	}

	public override void SetKey(AsymmetricAlgorithm key)
	{
		ArgumentNullException.ThrowIfNull(key, "key");
		_rsaKey = (RSA)key;
	}
}
