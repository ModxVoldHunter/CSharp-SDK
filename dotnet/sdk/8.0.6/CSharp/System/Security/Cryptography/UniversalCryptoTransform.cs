namespace System.Security.Cryptography;

internal abstract class UniversalCryptoTransform : ICryptoTransform, IDisposable
{
	public bool CanReuseTransform => true;

	public bool CanTransformMultipleBlocks => true;

	protected int PaddingSizeBytes => BasicSymmetricCipher.PaddingSizeInBytes;

	public int InputBlockSize => BasicSymmetricCipher.BlockSizeInBytes;

	public int OutputBlockSize => BasicSymmetricCipher.BlockSizeInBytes;

	protected PaddingMode PaddingMode { get; private set; }

	protected BasicSymmetricCipher BasicSymmetricCipher { get; private set; }

	public static UniversalCryptoTransform Create(PaddingMode paddingMode, BasicSymmetricCipher cipher, bool encrypting)
	{
		if (encrypting)
		{
			return new UniversalCryptoEncryptor(paddingMode, cipher);
		}
		return new UniversalCryptoDecryptor(paddingMode, cipher);
	}

	protected UniversalCryptoTransform(PaddingMode paddingMode, BasicSymmetricCipher basicSymmetricCipher)
	{
		PaddingMode = paddingMode;
		BasicSymmetricCipher = basicSymmetricCipher;
	}

	public void Dispose()
	{
		Dispose(disposing: true);
		GC.SuppressFinalize(this);
	}

	public int TransformBlock(byte[] inputBuffer, int inputOffset, int inputCount, byte[] outputBuffer, int outputOffset)
	{
		ArgumentNullException.ThrowIfNull(inputBuffer, "inputBuffer");
		ArgumentOutOfRangeException.ThrowIfNegative(inputOffset, "inputOffset");
		ArgumentOutOfRangeException.ThrowIfGreaterThan(inputOffset, inputBuffer.Length, "inputOffset");
		ArgumentOutOfRangeException.ThrowIfNegativeOrZero(inputCount, "inputCount");
		if (inputCount % InputBlockSize != 0)
		{
			throw new ArgumentOutOfRangeException("inputCount", System.SR.Cryptography_MustTransformWholeBlock);
		}
		if (inputCount > inputBuffer.Length - inputOffset)
		{
			throw new ArgumentOutOfRangeException("inputCount", System.SR.Argument_InvalidOffLen);
		}
		ArgumentNullException.ThrowIfNull(outputBuffer, "outputBuffer");
		ArgumentOutOfRangeException.ThrowIfGreaterThan(outputOffset, outputBuffer.Length, "outputOffset");
		if (inputCount > outputBuffer.Length - outputOffset)
		{
			throw new ArgumentOutOfRangeException("outputOffset", System.SR.Argument_InvalidOffLen);
		}
		return UncheckedTransformBlock(inputBuffer, inputOffset, inputCount, outputBuffer, outputOffset);
	}

	public byte[] TransformFinalBlock(byte[] inputBuffer, int inputOffset, int inputCount)
	{
		ArgumentNullException.ThrowIfNull(inputBuffer, "inputBuffer");
		ArgumentOutOfRangeException.ThrowIfNegative(inputOffset, "inputOffset");
		ArgumentOutOfRangeException.ThrowIfNegative(inputCount, "inputCount");
		ArgumentOutOfRangeException.ThrowIfGreaterThan(inputOffset, inputBuffer.Length, "inputOffset");
		if (inputCount > inputBuffer.Length - inputOffset)
		{
			throw new ArgumentOutOfRangeException("inputCount", System.SR.Argument_InvalidOffLen);
		}
		return UncheckedTransformFinalBlock(inputBuffer, inputOffset, inputCount);
	}

	protected virtual void Dispose(bool disposing)
	{
		if (disposing)
		{
			BasicSymmetricCipher.Dispose();
		}
	}

	protected int UncheckedTransformBlock(byte[] inputBuffer, int inputOffset, int inputCount, byte[] outputBuffer, int outputOffset)
	{
		return UncheckedTransformBlock(inputBuffer.AsSpan(inputOffset, inputCount), outputBuffer.AsSpan(outputOffset));
	}

	protected abstract int UncheckedTransformBlock(ReadOnlySpan<byte> inputBuffer, Span<byte> outputBuffer);

	protected abstract byte[] UncheckedTransformFinalBlock(byte[] inputBuffer, int inputOffset, int inputCount);

	protected abstract int UncheckedTransformFinalBlock(ReadOnlySpan<byte> inputBuffer, Span<byte> outputBuffer);
}
