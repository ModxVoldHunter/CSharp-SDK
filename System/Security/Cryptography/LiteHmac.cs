using Microsoft.Win32.SafeHandles;

namespace System.Security.Cryptography;

internal readonly struct LiteHmac : ILiteHash, IDisposable
{
	private readonly SafeBCryptHashHandle _hashHandle;

	private readonly int _hashSizeInBytes;

	public int HashSizeInBytes => _hashSizeInBytes;

	internal LiteHmac(string algorithm, ReadOnlySpan<byte> key)
	{
		global::Interop.BCrypt.BCryptOpenAlgorithmProviderFlags flags = global::Interop.BCrypt.BCryptOpenAlgorithmProviderFlags.BCRYPT_ALG_HANDLE_HMAC_FLAG;
		SafeBCryptAlgorithmHandle cachedBCryptAlgorithmHandle = global::Interop.BCrypt.BCryptAlgorithmCache.GetCachedBCryptAlgorithmHandle(algorithm, flags, out _hashSizeInBytes);
		SafeBCryptHashHandle phHash;
		global::Interop.BCrypt.NTSTATUS nTSTATUS = global::Interop.BCrypt.BCryptCreateHash(cachedBCryptAlgorithmHandle, out phHash, IntPtr.Zero, 0, key, key.Length, global::Interop.BCrypt.BCryptCreateHashFlags.None);
		if (nTSTATUS != 0)
		{
			phHash.Dispose();
			throw global::Interop.BCrypt.CreateCryptographicException(nTSTATUS);
		}
		_hashHandle = phHash;
	}

	public void Append(ReadOnlySpan<byte> data)
	{
		if (!data.IsEmpty)
		{
			global::Interop.BCrypt.NTSTATUS nTSTATUS = global::Interop.BCrypt.BCryptHashData(_hashHandle, data, data.Length, 0);
			if (nTSTATUS != 0)
			{
				throw global::Interop.BCrypt.CreateCryptographicException(nTSTATUS);
			}
		}
	}

	public int Finalize(Span<byte> destination)
	{
		global::Interop.BCrypt.NTSTATUS nTSTATUS = global::Interop.BCrypt.BCryptFinishHash(_hashHandle, destination, _hashSizeInBytes, 0);
		if (nTSTATUS != 0)
		{
			throw global::Interop.BCrypt.CreateCryptographicException(nTSTATUS);
		}
		return _hashSizeInBytes;
	}

	public void Dispose()
	{
		_hashHandle.Dispose();
	}
}
