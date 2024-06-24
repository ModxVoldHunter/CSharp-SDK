using Internal.Cryptography;
using Internal.NativeCrypto;

namespace System.Security.Cryptography;

internal sealed class TripleDesImplementation : TripleDES
{
	public TripleDesImplementation()
	{
		FeedbackSizeValue = 8;
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
		long num = (long)rgbKey.Length * 8L;
		if (num > int.MaxValue || !((int)num).IsLegalSize(LegalKeySizes))
		{
			throw new ArgumentException(System.SR.Cryptography_InvalidKeySize, "rgbKey");
		}
		if (rgbIV != null)
		{
			long num2 = (long)rgbIV.Length * 8L;
			if (num2 != BlockSize)
			{
				throw new ArgumentException(System.SR.Cryptography_InvalidIVSize, "rgbIV");
			}
		}
		if (rgbKey.Length == 16)
		{
			byte[] array = new byte[24];
			Array.Copy(rgbKey, array, 16);
			Array.Copy(rgbKey, 0, array, 16, 8);
			rgbKey = array;
		}
		if (Mode == CipherMode.CFB)
		{
			ValidateCFBFeedbackSize(FeedbackSize);
		}
		return CreateTransformCore(Mode, Padding, rgbKey, rgbIV, BlockSize / 8, this.GetPaddingSize(Mode, FeedbackSize), FeedbackSize / 8, encrypting);
	}

	protected override bool TryDecryptEcbCore(ReadOnlySpan<byte> ciphertext, Span<byte> destination, PaddingMode paddingMode, out int bytesWritten)
	{
		ILiteSymmetricCipher liteSymmetricCipher = CreateLiteCipher(CipherMode.ECB, Key, default(ReadOnlySpan<byte>), BlockSize / 8, BlockSize / 8, 0, encrypting: false);
		using (liteSymmetricCipher)
		{
			return UniversalCryptoOneShot.OneShotDecrypt(liteSymmetricCipher, paddingMode, ciphertext, destination, out bytesWritten);
		}
	}

	protected override bool TryEncryptEcbCore(ReadOnlySpan<byte> plaintext, Span<byte> destination, PaddingMode paddingMode, out int bytesWritten)
	{
		ILiteSymmetricCipher liteSymmetricCipher = CreateLiteCipher(CipherMode.ECB, Key, default(ReadOnlySpan<byte>), BlockSize / 8, BlockSize / 8, 0, encrypting: true);
		using (liteSymmetricCipher)
		{
			return UniversalCryptoOneShot.OneShotEncrypt(liteSymmetricCipher, paddingMode, plaintext, destination, out bytesWritten);
		}
	}

	protected override bool TryEncryptCbcCore(ReadOnlySpan<byte> plaintext, ReadOnlySpan<byte> iv, Span<byte> destination, PaddingMode paddingMode, out int bytesWritten)
	{
		ILiteSymmetricCipher liteSymmetricCipher = CreateLiteCipher(CipherMode.CBC, Key, iv, BlockSize / 8, BlockSize / 8, 0, encrypting: true);
		using (liteSymmetricCipher)
		{
			return UniversalCryptoOneShot.OneShotEncrypt(liteSymmetricCipher, paddingMode, plaintext, destination, out bytesWritten);
		}
	}

	protected override bool TryDecryptCbcCore(ReadOnlySpan<byte> ciphertext, ReadOnlySpan<byte> iv, Span<byte> destination, PaddingMode paddingMode, out int bytesWritten)
	{
		ILiteSymmetricCipher liteSymmetricCipher = CreateLiteCipher(CipherMode.CBC, Key, iv, BlockSize / 8, BlockSize / 8, 0, encrypting: false);
		using (liteSymmetricCipher)
		{
			return UniversalCryptoOneShot.OneShotDecrypt(liteSymmetricCipher, paddingMode, ciphertext, destination, out bytesWritten);
		}
	}

	protected override bool TryDecryptCfbCore(ReadOnlySpan<byte> ciphertext, ReadOnlySpan<byte> iv, Span<byte> destination, PaddingMode paddingMode, int feedbackSizeInBits, out int bytesWritten)
	{
		ValidateCFBFeedbackSize(feedbackSizeInBits);
		ILiteSymmetricCipher liteSymmetricCipher = CreateLiteCipher(CipherMode.CFB, Key, iv, BlockSize / 8, feedbackSizeInBits / 8, feedbackSizeInBits / 8, encrypting: false);
		using (liteSymmetricCipher)
		{
			return UniversalCryptoOneShot.OneShotDecrypt(liteSymmetricCipher, paddingMode, ciphertext, destination, out bytesWritten);
		}
	}

	protected override bool TryEncryptCfbCore(ReadOnlySpan<byte> plaintext, ReadOnlySpan<byte> iv, Span<byte> destination, PaddingMode paddingMode, int feedbackSizeInBits, out int bytesWritten)
	{
		ValidateCFBFeedbackSize(feedbackSizeInBits);
		ILiteSymmetricCipher liteSymmetricCipher = CreateLiteCipher(CipherMode.CFB, Key, iv, BlockSize / 8, feedbackSizeInBits / 8, feedbackSizeInBits / 8, encrypting: true);
		using (liteSymmetricCipher)
		{
			return UniversalCryptoOneShot.OneShotEncrypt(liteSymmetricCipher, paddingMode, plaintext, destination, out bytesWritten);
		}
	}

	private static void ValidateCFBFeedbackSize(int feedback)
	{
		if (feedback != 8 && feedback != 64)
		{
			throw new CryptographicException(System.SR.Format(System.SR.Cryptography_CipherModeFeedbackNotSupported, feedback, CipherMode.CFB));
		}
	}

	private static UniversalCryptoTransform CreateTransformCore(CipherMode cipherMode, PaddingMode paddingMode, byte[] key, byte[] iv, int blockSize, int paddingSize, int feedbackSize, bool encrypting)
	{
		SafeAlgorithmHandle sharedHandle = TripleDesBCryptModes.GetSharedHandle(cipherMode, feedbackSize);
		BasicSymmetricCipher cipher = new BasicSymmetricCipherBCrypt(sharedHandle, cipherMode, blockSize, paddingSize, key, ownsParentHandle: false, iv, encrypting);
		return UniversalCryptoTransform.Create(paddingMode, cipher, encrypting);
	}

	private static BasicSymmetricCipherLiteBCrypt CreateLiteCipher(CipherMode cipherMode, ReadOnlySpan<byte> key, ReadOnlySpan<byte> iv, int blockSize, int paddingSize, int feedbackSize, bool encrypting)
	{
		SafeAlgorithmHandle sharedHandle = TripleDesBCryptModes.GetSharedHandle(cipherMode, feedbackSize);
		return new BasicSymmetricCipherLiteBCrypt(sharedHandle, blockSize, paddingSize, key, ownsParentHandle: false, iv, encrypting);
	}
}
