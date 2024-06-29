using Internal.Cryptography;
using Internal.NativeCrypto;

namespace System.Security.Cryptography;

internal struct CngSymmetricAlgorithmCore
{
	private readonly ICngSymmetricAlgorithm _outer;

	private string _keyName;

	private readonly CngProvider _provider;

	private readonly CngKeyOpenOptions _optionOptions;

	private bool KeyInPlainText => _keyName == null;

	public CngSymmetricAlgorithmCore(ICngSymmetricAlgorithm outer)
	{
		_outer = outer;
		_keyName = null;
		_provider = null;
		_optionOptions = CngKeyOpenOptions.None;
	}

	public CngSymmetricAlgorithmCore(ICngSymmetricAlgorithm outer, string keyName, CngProvider provider, CngKeyOpenOptions openOptions)
	{
		ArgumentNullException.ThrowIfNull(keyName, "keyName");
		ArgumentNullException.ThrowIfNull(provider, "provider");
		_outer = outer;
		_keyName = keyName;
		_provider = provider;
		_optionOptions = openOptions;
		using CngKey cngKey = ProduceCngKey();
		CngAlgorithm algorithm = cngKey.Algorithm;
		string nCryptAlgorithmIdentifier = _outer.GetNCryptAlgorithmIdentifier();
		if (nCryptAlgorithmIdentifier != algorithm.Algorithm)
		{
			throw new CryptographicException(System.SR.Format(System.SR.Cryptography_CngKeyWrongAlgorithm, algorithm.Algorithm, nCryptAlgorithmIdentifier));
		}
		_outer.BaseKeySize = cngKey.KeySize;
	}

	public byte[] GetKeyIfExportable()
	{
		if (KeyInPlainText)
		{
			return _outer.BaseKey;
		}
		using CngKey cngKey = ProduceCngKey();
		return cngKey.GetSymmetricKeyDataIfExportable(_outer.GetNCryptAlgorithmIdentifier());
	}

	public void SetKey(byte[] key)
	{
		_outer.BaseKey = key;
		_keyName = null;
	}

	public void SetKeySize(int keySize, ICngSymmetricAlgorithm outer)
	{
		outer.BaseKeySize = keySize;
		_keyName = null;
	}

	public void GenerateKey()
	{
		byte[] bytes = RandomNumberGenerator.GetBytes(AsymmetricAlgorithmHelpers.BitsToBytes(_outer.BaseKeySize));
		SetKey(bytes);
	}

	public void GenerateIV()
	{
		byte[] bytes = RandomNumberGenerator.GetBytes(AsymmetricAlgorithmHelpers.BitsToBytes(_outer.BlockSize));
		_outer.IV = bytes;
	}

	public ICryptoTransform CreateEncryptor()
	{
		return CreateCryptoTransform(encrypting: true);
	}

	public ICryptoTransform CreateDecryptor()
	{
		return CreateCryptoTransform(encrypting: false);
	}

	public ICryptoTransform CreateEncryptor(byte[] rgbKey, byte[] rgbIV)
	{
		return CreateCryptoTransform(rgbKey, rgbIV, encrypting: true, _outer.Padding, _outer.Mode, _outer.FeedbackSize);
	}

	public ICryptoTransform CreateDecryptor(byte[] rgbKey, byte[] rgbIV)
	{
		return CreateCryptoTransform(rgbKey, rgbIV, encrypting: false, _outer.Padding, _outer.Mode, _outer.FeedbackSize);
	}

	private UniversalCryptoTransform CreateCryptoTransform(bool encrypting)
	{
		if (KeyInPlainText)
		{
			return CreateCryptoTransform(_outer.BaseKey, _outer.IV, encrypting, _outer.Padding, _outer.Mode, _outer.FeedbackSize);
		}
		return CreatePersistedCryptoTransformCore(ProduceCngKey, _outer.IV, encrypting, _outer.Padding, _outer.Mode, _outer.FeedbackSize);
	}

	public ILiteSymmetricCipher CreateLiteSymmetricCipher(ReadOnlySpan<byte> iv, bool encrypting, CipherMode mode, int feedbackSizeInBits)
	{
		if (KeyInPlainText)
		{
			return CreateLiteSymmetricCipher(_outer.BaseKey, iv, encrypting, mode, feedbackSizeInBits);
		}
		return CreatePersistedLiteSymmetricCipher(ProduceCngKey, iv, encrypting, mode, feedbackSizeInBits);
	}

	private BasicSymmetricCipherLiteBCrypt CreateLiteSymmetricCipher(ReadOnlySpan<byte> key, ReadOnlySpan<byte> iv, bool encrypting, CipherMode mode, int feedbackSizeInBits)
	{
		ValidateFeedbackSize(mode, feedbackSizeInBits);
		if (!iv.IsEmpty && iv.Length != AsymmetricAlgorithmHelpers.BitsToBytes(_outer.BlockSize))
		{
			throw new ArgumentException(System.SR.Cryptography_InvalidIVSize, "iv");
		}
		if (mode.UsesIv() && iv.IsEmpty)
		{
			throw new CryptographicException(System.SR.Cryptography_MissingIV);
		}
		byte[] array = CopyAndValidateKey(key);
		int blockSizeInBytes = AsymmetricAlgorithmHelpers.BitsToBytes(_outer.BlockSize);
		SafeAlgorithmHandle ephemeralModeHandle = _outer.GetEphemeralModeHandle(mode, feedbackSizeInBits);
		return new BasicSymmetricCipherLiteBCrypt(ephemeralModeHandle, blockSizeInBytes, _outer.GetPaddingSize(mode, feedbackSizeInBits), array, ownsParentHandle: false, iv, encrypting);
	}

	private UniversalCryptoTransform CreateCryptoTransform(byte[] rgbKey, byte[] rgbIV, bool encrypting, PaddingMode padding, CipherMode mode, int feedbackSizeInBits)
	{
		ArgumentNullException.ThrowIfNull(rgbKey, "rgbKey");
		ValidateFeedbackSize(mode, feedbackSizeInBits);
		byte[] key = CopyAndValidateKey(rgbKey);
		if (rgbIV != null && rgbIV.Length != AsymmetricAlgorithmHelpers.BitsToBytes(_outer.BlockSize))
		{
			throw new ArgumentException(System.SR.Cryptography_InvalidIVSize, "rgbIV");
		}
		byte[] iv = mode.GetCipherIv(rgbIV).CloneByteArray();
		return CreateEphemeralCryptoTransformCore(key, iv, encrypting, padding, mode, feedbackSizeInBits);
	}

	private UniversalCryptoTransform CreateEphemeralCryptoTransformCore(byte[] key, byte[] iv, bool encrypting, PaddingMode padding, CipherMode mode, int feedbackSizeInBits)
	{
		int blockSizeInBytes = AsymmetricAlgorithmHelpers.BitsToBytes(_outer.BlockSize);
		SafeAlgorithmHandle ephemeralModeHandle = _outer.GetEphemeralModeHandle(mode, feedbackSizeInBits);
		BasicSymmetricCipher cipher = new BasicSymmetricCipherBCrypt(ephemeralModeHandle, mode, blockSizeInBytes, _outer.GetPaddingSize(mode, feedbackSizeInBits), key, ownsParentHandle: false, iv, encrypting);
		return UniversalCryptoTransform.Create(padding, cipher, encrypting);
	}

	private BasicSymmetricCipherLiteNCrypt CreatePersistedLiteSymmetricCipher(Func<CngKey> cngKeyFactory, ReadOnlySpan<byte> iv, bool encrypting, CipherMode mode, int feedbackSizeInBits)
	{
		ValidateFeedbackSize(mode, feedbackSizeInBits);
		int blockSizeInBytes = AsymmetricAlgorithmHelpers.BitsToBytes(_outer.BlockSize);
		return new BasicSymmetricCipherLiteNCrypt(cngKeyFactory, mode, blockSizeInBytes, iv, encrypting, _outer.GetPaddingSize(mode, feedbackSizeInBits));
	}

	private UniversalCryptoTransform CreatePersistedCryptoTransformCore(Func<CngKey> cngKeyFactory, byte[] iv, bool encrypting, PaddingMode padding, CipherMode mode, int feedbackSizeInBits)
	{
		ValidateFeedbackSize(mode, feedbackSizeInBits);
		int blockSizeInBytes = AsymmetricAlgorithmHelpers.BitsToBytes(_outer.BlockSize);
		BasicSymmetricCipher cipher = new BasicSymmetricCipherNCrypt(cngKeyFactory, mode, blockSizeInBytes, iv, encrypting, _outer.GetPaddingSize(mode, feedbackSizeInBits));
		return UniversalCryptoTransform.Create(padding, cipher, encrypting);
	}

	private CngKey ProduceCngKey()
	{
		return CngKey.Open(_keyName, _provider, _optionOptions);
	}

	private void ValidateFeedbackSize(CipherMode mode, int feedbackSizeInBits)
	{
		if (mode != CipherMode.CFB)
		{
			return;
		}
		if (KeyInPlainText)
		{
			if (!_outer.IsValidEphemeralFeedbackSize(feedbackSizeInBits))
			{
				throw new CryptographicException(System.SR.Format(System.SR.Cryptography_CipherModeFeedbackNotSupported, feedbackSizeInBits, CipherMode.CFB));
			}
		}
		else if (feedbackSizeInBits != 8)
		{
			throw new CryptographicException(System.SR.Format(System.SR.Cryptography_CipherModeFeedbackNotSupported, feedbackSizeInBits, CipherMode.CFB));
		}
	}

	private byte[] CopyAndValidateKey(ReadOnlySpan<byte> rgbKey)
	{
		long num = (long)rgbKey.Length * 8L;
		if (num > int.MaxValue || !((int)num).IsLegalSize(_outer.LegalKeySizes))
		{
			throw new ArgumentException(System.SR.Cryptography_InvalidKeySize, "rgbKey");
		}
		byte[] key = rgbKey.ToArray();
		if (_outer.IsWeakKey(key))
		{
			throw new CryptographicException(System.SR.Format(System.SR.Cryptography_InvalidKey_Weak, _outer.GetNCryptAlgorithmIdentifier()));
		}
		return _outer.PreprocessKey(key);
	}
}
