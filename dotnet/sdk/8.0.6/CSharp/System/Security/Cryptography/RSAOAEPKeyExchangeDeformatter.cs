namespace System.Security.Cryptography;

public class RSAOAEPKeyExchangeDeformatter : AsymmetricKeyExchangeDeformatter
{
	private RSA _rsaKey;

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

	public RSAOAEPKeyExchangeDeformatter()
	{
	}

	public RSAOAEPKeyExchangeDeformatter(AsymmetricAlgorithm key)
	{
		ArgumentNullException.ThrowIfNull(key, "key");
		_rsaKey = (RSA)key;
	}

	public override byte[] DecryptKeyExchange(byte[] rgbData)
	{
		if (_rsaKey == null)
		{
			throw new CryptographicUnexpectedOperationException(System.SR.Cryptography_FormatterMissingKey);
		}
		return _rsaKey.Decrypt(rgbData, RSAEncryptionPadding.OaepSHA1);
	}

	public override void SetKey(AsymmetricAlgorithm key)
	{
		ArgumentNullException.ThrowIfNull(key, "key");
		_rsaKey = (RSA)key;
	}
}
