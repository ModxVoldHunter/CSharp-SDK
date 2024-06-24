using System.Text;

namespace System.Security.Cryptography;

internal readonly ref struct Utf8DataEncoding
{
	private readonly byte[] _rented;

	private readonly Span<byte> _buffer;

	internal static Encoding ThrowingUtf8Encoding { get; } = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);


	internal ReadOnlySpan<byte> Utf8Bytes => _buffer;

	internal Utf8DataEncoding(ReadOnlySpan<char> data, Span<byte> stackBuffer)
	{
		_rented = null;
		int maxByteCount = ThrowingUtf8Encoding.GetMaxByteCount(data.Length);
		_buffer = (((uint)maxByteCount <= stackBuffer.Length) ? stackBuffer : ((Span<byte>)(_rented = System.Security.Cryptography.CryptoPool.Rent(maxByteCount))));
		int bytes = ThrowingUtf8Encoding.GetBytes(data, _buffer);
		_buffer = _buffer.Slice(0, bytes);
	}

	internal void Dispose()
	{
		CryptographicOperations.ZeroMemory(_buffer);
		if (_rented != null)
		{
			System.Security.Cryptography.CryptoPool.Return(_rented, 0);
		}
	}
}
