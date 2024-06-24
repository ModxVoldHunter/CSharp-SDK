namespace System.Security.Cryptography;

public sealed class PbeParameters
{
	public PbeEncryptionAlgorithm EncryptionAlgorithm { get; }

	public HashAlgorithmName HashAlgorithm { get; }

	public int IterationCount { get; }

	public PbeParameters(PbeEncryptionAlgorithm encryptionAlgorithm, HashAlgorithmName hashAlgorithm, int iterationCount)
	{
		ArgumentOutOfRangeException.ThrowIfNegativeOrZero(iterationCount, "iterationCount");
		EncryptionAlgorithm = encryptionAlgorithm;
		HashAlgorithm = hashAlgorithm;
		IterationCount = iterationCount;
	}
}
