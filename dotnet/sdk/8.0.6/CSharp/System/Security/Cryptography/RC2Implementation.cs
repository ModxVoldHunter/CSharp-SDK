using Internal.Cryptography;
using Internal.NativeCrypto;

namespace System.Security.Cryptography;

internal sealed class RC2Implementation : RC2
{
	public override int EffectiveKeySize
	{
		get
		{
			return KeySizeValue;
		}
		set
		{
			if (value != KeySizeValue)
			{
				throw new CryptographicUnexpectedOperationException(System.SR.Cryptography_RC2_EKSKS2);
			}
		}
	}

	public override ICryptoTransform CreateDecryptor()
	{
		return CreateTransform(Key, IV, encrypting: false);
	}

	public override ICryptoTransform CreateDecryptor(byte[] rgbKey, byte[] rgbIV)
	{
		return CreateTransform(rgbKey, rgbIV.CloneByteArray(), encrypting: false);
	}

	public override ICryptoTransform CreateEncryptor()
	{
		return CreateTransform(Key, IV, encrypting: true);
	}

	public override ICryptoTransform CreateEncryptor(byte[] rgbKey, byte[] rgbIV)
	{
		return CreateTransform(rgbKey, rgbIV.CloneByteArray(), encrypting: true);
	}

	public override void GenerateIV()
	{
		IV = RandomNumberGenerator.GetBytes(BlockSize / 8);
	}

	public sealed override void GenerateKey()
	{
		Key = RandomNumberGenerator.GetBytes(KeySize / 8);
	}

	private UniversalCryptoTransform CreateTransform(byte[] rgbKey, byte[] rgbIV, bool encrypting)
	{
		ArgumentNullException.ThrowIfNull(rgbKey, "rgbKey");
		if (!ValidKeySize(rgbKey.Length))
		{
			throw new ArgumentException(System.SR.Cryptography_InvalidKeySize, "rgbKey");
		}
		if (rgbIV != null)
		{
			long num = (long)rgbIV.Length * 8L;
			if (num != BlockSize)
			{
				throw new ArgumentException(System.SR.Cryptography_InvalidIVSize, "rgbIV");
			}
		}
		if (Mode == CipherMode.CFB)
		{
			ValidateCFBFeedbackSize(FeedbackSize);
		}
		return CreateTransformCore(Mode, Padding, rgbKey, rgbIV, BlockSize / 8, FeedbackSize / 8, GetPaddingSize(), encrypting);
	}

	protected override bool TryDecryptEcbCore(ReadOnlySpan<byte> ciphertext, Span<byte> destination, PaddingMode paddingMode, out int bytesWritten)
	{
		if (!ValidKeySize(Key.Length))
		{
			throw new InvalidOperationException(System.SR.Cryptography_InvalidKeySize);
		}
		ILiteSymmetricCipher liteSymmetricCipher = CreateLiteCipher(CipherMode.ECB, Key, null, BlockSize / 8, BlockSize / 8, encrypting: false);
		using (liteSymmetricCipher)
		{
			return UniversalCryptoOneShot.OneShotDecrypt(liteSymmetricCipher, paddingMode, ciphertext, destination, out bytesWritten);
		}
	}

	protected override bool TryEncryptEcbCore(ReadOnlySpan<byte> plaintext, Span<byte> destination, PaddingMode paddingMode, out int bytesWritten)
	{
		if (!ValidKeySize(Key.Length))
		{
			throw new InvalidOperationException(System.SR.Cryptography_InvalidKeySize);
		}
		ILiteSymmetricCipher liteSymmetricCipher = CreateLiteCipher(CipherMode.ECB, Key, default(ReadOnlySpan<byte>), BlockSize / 8, BlockSize / 8, encrypting: true);
		using (liteSymmetricCipher)
		{
			return UniversalCryptoOneShot.OneShotEncrypt(liteSymmetricCipher, paddingMode, plaintext, destination, out bytesWritten);
		}
	}

	protected override bool TryEncryptCbcCore(ReadOnlySpan<byte> plaintext, ReadOnlySpan<byte> iv, Span<byte> destination, PaddingMode paddingMode, out int bytesWritten)
	{
		if (!ValidKeySize(Key.Length))
		{
			throw new InvalidOperationException(System.SR.Cryptography_InvalidKeySize);
		}
		ILiteSymmetricCipher liteSymmetricCipher = CreateLiteCipher(CipherMode.CBC, Key, iv, BlockSize / 8, BlockSize / 8, encrypting: true);
		using (liteSymmetricCipher)
		{
			return UniversalCryptoOneShot.OneShotEncrypt(liteSymmetricCipher, paddingMode, plaintext, destination, out bytesWritten);
		}
	}

	protected override bool TryDecryptCbcCore(ReadOnlySpan<byte> ciphertext, ReadOnlySpan<byte> iv, Span<byte> destination, PaddingMode paddingMode, out int bytesWritten)
	{
		if (!ValidKeySize(Key.Length))
		{
			throw new InvalidOperationException(System.SR.Cryptography_InvalidKeySize);
		}
		ILiteSymmetricCipher liteSymmetricCipher = CreateLiteCipher(CipherMode.CBC, Key, iv, BlockSize / 8, BlockSize / 8, encrypting: false);
		using (liteSymmetricCipher)
		{
			return UniversalCryptoOneShot.OneShotDecrypt(liteSymmetricCipher, paddingMode, ciphertext, destination, out bytesWritten);
		}
	}

	protected override bool TryDecryptCfbCore(ReadOnlySpan<byte> ciphertext, ReadOnlySpan<byte> iv, Span<byte> destination, PaddingMode paddingMode, int feedbackSizeInBits, out int bytesWritten)
	{
		throw new CryptographicException(System.SR.Format(System.SR.Cryptography_CipherModeNotSupported, CipherMode.CFB));
	}

	protected override bool TryEncryptCfbCore(ReadOnlySpan<byte> plaintext, ReadOnlySpan<byte> iv, Span<byte> destination, PaddingMode paddingMode, int feedbackSizeInBits, out int bytesWritten)
	{
		throw new CryptographicException(System.SR.Format(System.SR.Cryptography_CipherModeNotSupported, CipherMode.CFB));
	}

	private static void ValidateCFBFeedbackSize(int feedback)
	{
		throw new CryptographicException(System.SR.Format(System.SR.Cryptography_CipherModeFeedbackNotSupported, feedback, CipherMode.CFB));
	}

	private int GetPaddingSize()
	{
		return BlockSize / 8;
	}

	private new bool ValidKeySize(int keySizeBytes)
	{
		if (keySizeBytes > 268435455)
		{
			return false;
		}
		int size = keySizeBytes << 3;
		return size.IsLegalSize(LegalKeySizes);
	}

	private static UniversalCryptoTransform CreateTransformCore(CipherMode cipherMode, PaddingMode paddingMode, byte[] key, byte[] iv, int blockSize, int _, int paddingSize, bool encrypting)
	{
		using SafeAlgorithmHandle algorithm = RC2BCryptModes.GetHandle(cipherMode, key.Length * 8);
		BasicSymmetricCipher cipher = new BasicSymmetricCipherBCrypt(algorithm, cipherMode, blockSize, paddingSize, key, ownsParentHandle: true, iv, encrypting);
		return UniversalCryptoTransform.Create(paddingMode, cipher, encrypting);
	}

	private static BasicSymmetricCipherLiteBCrypt CreateLiteCipher(CipherMode cipherMode, ReadOnlySpan<byte> key, ReadOnlySpan<byte> iv, int blockSize, int paddingSize, bool encrypting)
	{
		using SafeAlgorithmHandle algorithm = RC2BCryptModes.GetHandle(cipherMode, key.Length * 8);
		return new BasicSymmetricCipherLiteBCrypt(algorithm, blockSize, paddingSize, key, ownsParentHandle: true, iv, encrypting);
	}
}
