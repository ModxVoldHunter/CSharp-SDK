using Internal.Cryptography;
using Internal.NativeCrypto;

namespace System.Security.Cryptography;

internal sealed class BasicSymmetricCipherBCrypt : BasicSymmetricCipher
{
	private readonly BasicSymmetricCipherLiteBCrypt _cipherLite;

	public BasicSymmetricCipherBCrypt(SafeAlgorithmHandle algorithm, CipherMode cipherMode, int blockSizeInBytes, int paddingSizeInBytes, ReadOnlySpan<byte> key, bool ownsParentHandle, byte[] iv, bool encrypting)
		: base(cipherMode.GetCipherIv(iv), blockSizeInBytes, paddingSizeInBytes)
	{
		_cipherLite = new BasicSymmetricCipherLiteBCrypt(algorithm, blockSizeInBytes, paddingSizeInBytes, key, ownsParentHandle, base.IV, encrypting);
	}

	protected override void Dispose(bool disposing)
	{
		if (disposing)
		{
			_cipherLite.Dispose();
		}
		base.Dispose(disposing);
	}

	public override int Transform(ReadOnlySpan<byte> input, Span<byte> output)
	{
		return _cipherLite.Transform(input, output);
	}

	public override int TransformFinal(ReadOnlySpan<byte> input, Span<byte> output)
	{
		int result = _cipherLite.TransformFinal(input, output);
		_cipherLite.Reset(base.IV);
		return result;
	}
}
