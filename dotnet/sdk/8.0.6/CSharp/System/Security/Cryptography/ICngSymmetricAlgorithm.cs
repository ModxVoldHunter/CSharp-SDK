using Internal.NativeCrypto;

namespace System.Security.Cryptography;

internal interface ICngSymmetricAlgorithm
{
	int BlockSize { get; }

	int FeedbackSize { get; }

	CipherMode Mode { get; }

	PaddingMode Padding { get; }

	byte[] IV { get; set; }

	KeySizes[] LegalKeySizes { get; }

	byte[] BaseKey { get; set; }

	int BaseKeySize { get; set; }

	bool IsWeakKey(byte[] key);

	SafeAlgorithmHandle GetEphemeralModeHandle(CipherMode mode, int feedbackSizeInBits);

	string GetNCryptAlgorithmIdentifier();

	byte[] PreprocessKey(byte[] key);

	int GetPaddingSize(CipherMode mode, int feedbackSizeBits);

	bool IsValidEphemeralFeedbackSize(int feedbackSizeInBits);
}
