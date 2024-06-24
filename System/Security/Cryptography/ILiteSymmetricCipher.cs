namespace System.Security.Cryptography;

internal interface ILiteSymmetricCipher : IDisposable
{
	int BlockSizeInBytes { get; }

	int PaddingSizeInBytes { get; }

	int TransformFinal(ReadOnlySpan<byte> input, Span<byte> output);

	int Transform(ReadOnlySpan<byte> input, Span<byte> output);

	void Reset(ReadOnlySpan<byte> iv);
}
