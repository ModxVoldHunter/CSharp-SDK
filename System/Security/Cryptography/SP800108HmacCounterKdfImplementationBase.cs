namespace System.Security.Cryptography;

internal abstract class SP800108HmacCounterKdfImplementationBase : IDisposable
{
	internal abstract void DeriveBytes(ReadOnlySpan<byte> label, ReadOnlySpan<byte> context, Span<byte> destination);

	internal abstract void DeriveBytes(ReadOnlySpan<char> label, ReadOnlySpan<char> context, Span<byte> destination);

	public abstract void Dispose();
}
