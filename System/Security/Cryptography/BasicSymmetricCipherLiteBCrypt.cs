using Internal.NativeCrypto;

namespace System.Security.Cryptography;

internal sealed class BasicSymmetricCipherLiteBCrypt : ILiteSymmetricCipher, IDisposable
{
	private readonly bool _encrypting;

	private readonly byte[] _currentIv;

	private SafeKeyHandle _hKey;

	public int BlockSizeInBytes { get; }

	public int PaddingSizeInBytes { get; }

	public BasicSymmetricCipherLiteBCrypt(SafeAlgorithmHandle algorithm, int blockSizeInBytes, int paddingSizeInBytes, ReadOnlySpan<byte> key, bool ownsParentHandle, ReadOnlySpan<byte> iv, bool encrypting)
	{
		if (!iv.IsEmpty)
		{
			_currentIv = iv.ToArray();
		}
		BlockSizeInBytes = blockSizeInBytes;
		PaddingSizeInBytes = paddingSizeInBytes;
		_encrypting = encrypting;
		_hKey = global::Interop.BCrypt.BCryptImportKey(algorithm, key);
		if (ownsParentHandle)
		{
			_hKey.SetParentHandle(algorithm);
		}
	}

	public int Transform(ReadOnlySpan<byte> input, Span<byte> output)
	{
		int num = 0;
		if (input.Overlaps(output, out var elementOffset) && elementOffset != 0)
		{
			byte[] array = System.Security.Cryptography.CryptoPool.Rent(output.Length);
			try
			{
				num = BCryptTransform(input, array);
				array.AsSpan(0, num).CopyTo(output);
			}
			finally
			{
				System.Security.Cryptography.CryptoPool.Return(array, num);
			}
		}
		else
		{
			num = BCryptTransform(input, output);
		}
		if (num != input.Length)
		{
			throw new CryptographicException(System.SR.Cryptography_UnexpectedTransformTruncation);
		}
		return num;
		int BCryptTransform(ReadOnlySpan<byte> input, Span<byte> output)
		{
			if (!_encrypting)
			{
				return global::Interop.BCrypt.BCryptDecrypt(_hKey, input, _currentIv, output);
			}
			return global::Interop.BCrypt.BCryptEncrypt(_hKey, input, _currentIv, output);
		}
	}

	public void Reset(ReadOnlySpan<byte> iv)
	{
		if (_currentIv != null)
		{
			iv.CopyTo(_currentIv);
		}
	}

	public int TransformFinal(ReadOnlySpan<byte> input, Span<byte> output)
	{
		int result = 0;
		if (input.Length != 0)
		{
			result = Transform(input, output);
		}
		return result;
	}

	public void Dispose()
	{
		if (_currentIv != null)
		{
			CryptographicOperations.ZeroMemory(_currentIv);
		}
		_hKey?.Dispose();
		_hKey = null;
	}
}
