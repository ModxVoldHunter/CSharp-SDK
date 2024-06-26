namespace System.Security.Cryptography;

internal sealed class BasicSymmetricCipherNCrypt : BasicSymmetricCipher
{
	private BasicSymmetricCipherLiteNCrypt _cipher;

	public BasicSymmetricCipherNCrypt(Func<CngKey> cngKeyFactory, CipherMode cipherMode, int blockSizeInBytes, byte[] iv, bool encrypting, int paddingSizeInBytes)
		: base(iv, blockSizeInBytes, paddingSizeInBytes)
	{
		_cipher = new BasicSymmetricCipherLiteNCrypt(cngKeyFactory, cipherMode, blockSizeInBytes, iv, encrypting, paddingSizeInBytes);
	}

	public sealed override int Transform(ReadOnlySpan<byte> input, Span<byte> output)
	{
		return _cipher.Transform(input, output);
	}

	public sealed override int TransformFinal(ReadOnlySpan<byte> input, Span<byte> output)
	{
		int result = _cipher.TransformFinal(input, output);
		Reset();
		return result;
	}

	protected sealed override void Dispose(bool disposing)
	{
		if (disposing)
		{
			_cipher?.Dispose();
			_cipher = null;
		}
		base.Dispose(disposing);
	}

	private void Reset()
	{
		if (base.IV != null)
		{
			_cipher.Reset(base.IV);
		}
	}
}
