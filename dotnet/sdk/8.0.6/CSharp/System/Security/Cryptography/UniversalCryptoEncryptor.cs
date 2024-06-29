namespace System.Security.Cryptography;

internal sealed class UniversalCryptoEncryptor : UniversalCryptoTransform
{
	public UniversalCryptoEncryptor(PaddingMode paddingMode, BasicSymmetricCipher basicSymmetricCipher)
		: base(paddingMode, basicSymmetricCipher)
	{
	}

	protected override int UncheckedTransformBlock(ReadOnlySpan<byte> inputBuffer, Span<byte> outputBuffer)
	{
		return base.BasicSymmetricCipher.Transform(inputBuffer, outputBuffer);
	}

	protected override int UncheckedTransformFinalBlock(ReadOnlySpan<byte> inputBuffer, Span<byte> outputBuffer)
	{
		int length = SymmetricPadding.PadBlock(inputBuffer, outputBuffer, base.PaddingSizeBytes, base.PaddingMode);
		return base.BasicSymmetricCipher.TransformFinal(outputBuffer.Slice(0, length), outputBuffer);
	}

	protected override byte[] UncheckedTransformFinalBlock(byte[] inputBuffer, int inputOffset, int inputCount)
	{
		int ciphertextLength = SymmetricPadding.GetCiphertextLength(inputCount, base.PaddingSizeBytes, base.PaddingMode);
		byte[] array = GC.AllocateUninitializedArray<byte>(ciphertextLength);
		int num = UncheckedTransformFinalBlock(inputBuffer.AsSpan(inputOffset, inputCount), array);
		return array;
	}
}
