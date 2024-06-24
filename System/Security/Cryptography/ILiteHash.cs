namespace System.Security.Cryptography;

internal interface ILiteHash : IDisposable
{
	int HashSizeInBytes { get; }

	void Append(ReadOnlySpan<byte> data);

	int Finalize(Span<byte> destination);
}
