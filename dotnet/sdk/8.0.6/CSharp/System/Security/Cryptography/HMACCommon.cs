using System.Diagnostics.CodeAnalysis;
using Internal.Cryptography;

namespace System.Security.Cryptography;

internal sealed class HMACCommon
{
	private readonly string _hashAlgorithmId;

	private HashProvider _hMacProvider;

	private readonly int _blockSize;

	public int HashSizeInBits => _hMacProvider.HashSizeInBytes * 8;

	public int HashSizeInBytes => _hMacProvider.HashSizeInBytes;

	public byte[] ActualKey { get; private set; }

	public HMACCommon(string hashAlgorithmId, byte[] key, int blockSize)
		: this(hashAlgorithmId, (ReadOnlySpan<byte>)key, blockSize)
	{
		if (ActualKey == null)
		{
			byte[] array2 = (ActualKey = key);
		}
	}

	internal HMACCommon(string hashAlgorithmId, ReadOnlySpan<byte> key, int blockSize)
	{
		_hashAlgorithmId = hashAlgorithmId;
		_blockSize = blockSize;
		ActualKey = ChangeKeyImpl(key);
	}

	public void ChangeKey(byte[] key)
	{
		ActualKey = ChangeKeyImpl(key) ?? key;
	}

	[MemberNotNull("_hMacProvider")]
	private byte[] ChangeKeyImpl(ReadOnlySpan<byte> key)
	{
		byte[] result = null;
		if (key.Length > _blockSize && _blockSize > 0)
		{
			switch (_hashAlgorithmId)
			{
			case "SHA256":
				result = SHA256.HashData(key);
				break;
			case "SHA384":
				result = SHA384.HashData(key);
				break;
			case "SHA512":
				result = SHA512.HashData(key);
				break;
			case "SHA3-256":
				result = SHA3_256.HashData(key);
				break;
			case "SHA3-384":
				result = SHA3_384.HashData(key);
				break;
			case "SHA3-512":
				result = SHA3_512.HashData(key);
				break;
			case "SHA1":
				result = SHA1.HashData(key);
				break;
			case "MD5":
				if (Helpers.HasMD5)
				{
					result = MD5.HashData(key);
					break;
				}
				goto default;
			default:
				throw new CryptographicException(System.SR.Format(System.SR.Cryptography_UnknownHashAlgorithm, _hashAlgorithmId));
			}
		}
		HashProvider hMacProvider = _hMacProvider;
		_hMacProvider = null;
		hMacProvider?.Dispose(disposing: true);
		_hMacProvider = HashProviderDispenser.CreateMacProvider(_hashAlgorithmId, key);
		return result;
	}

	public void AppendHashData(byte[] data, int offset, int count)
	{
		_hMacProvider.AppendHashData(data, offset, count);
	}

	public void AppendHashData(ReadOnlySpan<byte> source)
	{
		_hMacProvider.AppendHashData(source);
	}

	public byte[] FinalizeHashAndReset()
	{
		return _hMacProvider.FinalizeHashAndReset();
	}

	public int FinalizeHashAndReset(Span<byte> destination)
	{
		return _hMacProvider.FinalizeHashAndReset(destination);
	}

	public bool TryFinalizeHashAndReset(Span<byte> destination, out int bytesWritten)
	{
		return _hMacProvider.TryFinalizeHashAndReset(destination, out bytesWritten);
	}

	public int GetCurrentHash(Span<byte> destination)
	{
		return _hMacProvider.GetCurrentHash(destination);
	}

	public void Reset()
	{
		_hMacProvider.Reset();
	}

	public void Dispose(bool disposing)
	{
		if (disposing)
		{
			_hMacProvider?.Dispose(disposing: true);
			_hMacProvider = null;
		}
	}
}
