using System.Runtime.InteropServices;
using Internal.Cryptography;
using Microsoft.Win32.SafeHandles;

namespace System.Security.Cryptography;

internal static class HashProviderDispenser
{
	public static class OneShotHashProvider
	{
		public static int MacData(string hashAlgorithmId, ReadOnlySpan<byte> key, ReadOnlySpan<byte> source, Span<byte> destination)
		{
			int hashSizeInBytes;
			if (global::Interop.BCrypt.PseudoHandlesSupported)
			{
				HashDataUsingPseudoHandle(hashAlgorithmId, source, key, isHmac: true, destination, out hashSizeInBytes);
				return hashSizeInBytes;
			}
			SafeBCryptAlgorithmHandle cachedBCryptAlgorithmHandle = global::Interop.BCrypt.BCryptAlgorithmCache.GetCachedBCryptAlgorithmHandle(hashAlgorithmId, global::Interop.BCrypt.BCryptOpenAlgorithmProviderFlags.BCRYPT_ALG_HANDLE_HMAC_FLAG, out hashSizeInBytes);
			if (destination.Length < hashSizeInBytes)
			{
				throw new CryptographicException();
			}
			HashUpdateAndFinish(cachedBCryptAlgorithmHandle, hashSizeInBytes, key, source, destination);
			return hashSizeInBytes;
		}

		public static void HashDataXof(string hashAlgorithmId, ReadOnlySpan<byte> source, Span<byte> destination)
		{
			HashDataUsingPseudoHandle(hashAlgorithmId, source, default(ReadOnlySpan<byte>), isHmac: false, destination, out var _);
		}

		public static int HashData(string hashAlgorithmId, ReadOnlySpan<byte> source, Span<byte> destination)
		{
			int hashSizeInBytes;
			if (global::Interop.BCrypt.PseudoHandlesSupported)
			{
				HashDataUsingPseudoHandle(hashAlgorithmId, source, default(ReadOnlySpan<byte>), isHmac: false, destination, out hashSizeInBytes);
				return hashSizeInBytes;
			}
			SafeBCryptAlgorithmHandle cachedBCryptAlgorithmHandle = global::Interop.BCrypt.BCryptAlgorithmCache.GetCachedBCryptAlgorithmHandle(hashAlgorithmId, global::Interop.BCrypt.BCryptOpenAlgorithmProviderFlags.None, out hashSizeInBytes);
			if (destination.Length < hashSizeInBytes)
			{
				throw new CryptographicException();
			}
			HashUpdateAndFinish(cachedBCryptAlgorithmHandle, hashSizeInBytes, default(ReadOnlySpan<byte>), source, destination);
			return hashSizeInBytes;
		}

		private unsafe static void HashDataUsingPseudoHandle(string hashAlgorithmId, ReadOnlySpan<byte> source, ReadOnlySpan<byte> key, bool isHmac, Span<byte> destination, out int hashSize)
		{
			hashSize = 0;
			global::Interop.BCrypt.BCryptAlgPseudoHandle bCryptAlgPseudoHandle;
			int num;
			switch (hashAlgorithmId)
			{
			case "MD5":
				bCryptAlgPseudoHandle = (isHmac ? global::Interop.BCrypt.BCryptAlgPseudoHandle.BCRYPT_HMAC_MD5_ALG_HANDLE : global::Interop.BCrypt.BCryptAlgPseudoHandle.BCRYPT_MD5_ALG_HANDLE);
				num = 16;
				break;
			case "SHA1":
				bCryptAlgPseudoHandle = (isHmac ? global::Interop.BCrypt.BCryptAlgPseudoHandle.BCRYPT_HMAC_SHA1_ALG_HANDLE : global::Interop.BCrypt.BCryptAlgPseudoHandle.BCRYPT_SHA1_ALG_HANDLE);
				num = 20;
				break;
			case "SHA256":
				bCryptAlgPseudoHandle = (isHmac ? global::Interop.BCrypt.BCryptAlgPseudoHandle.BCRYPT_HMAC_SHA256_ALG_HANDLE : global::Interop.BCrypt.BCryptAlgPseudoHandle.BCRYPT_SHA256_ALG_HANDLE);
				num = 32;
				break;
			case "SHA384":
				bCryptAlgPseudoHandle = (isHmac ? global::Interop.BCrypt.BCryptAlgPseudoHandle.BCRYPT_HMAC_SHA384_ALG_HANDLE : global::Interop.BCrypt.BCryptAlgPseudoHandle.BCRYPT_SHA384_ALG_HANDLE);
				num = 48;
				break;
			case "SHA512":
				bCryptAlgPseudoHandle = (isHmac ? global::Interop.BCrypt.BCryptAlgPseudoHandle.BCRYPT_HMAC_SHA512_ALG_HANDLE : global::Interop.BCrypt.BCryptAlgPseudoHandle.BCRYPT_SHA512_ALG_HANDLE);
				num = 64;
				break;
			case "SHA3-256":
				bCryptAlgPseudoHandle = (isHmac ? global::Interop.BCrypt.BCryptAlgPseudoHandle.BCRYPT_HMAC_SHA3_256_ALG_HANDLE : global::Interop.BCrypt.BCryptAlgPseudoHandle.BCRYPT_SHA3_256_ALG_HANDLE);
				num = 32;
				break;
			case "SHA3-384":
				bCryptAlgPseudoHandle = (isHmac ? global::Interop.BCrypt.BCryptAlgPseudoHandle.BCRYPT_HMAC_SHA3_384_ALG_HANDLE : global::Interop.BCrypt.BCryptAlgPseudoHandle.BCRYPT_SHA3_384_ALG_HANDLE);
				num = 48;
				break;
			case "SHA3-512":
				bCryptAlgPseudoHandle = (isHmac ? global::Interop.BCrypt.BCryptAlgPseudoHandle.BCRYPT_HMAC_SHA3_512_ALG_HANDLE : global::Interop.BCrypt.BCryptAlgPseudoHandle.BCRYPT_SHA3_512_ALG_HANDLE);
				num = 64;
				break;
			case "CSHAKE128":
				bCryptAlgPseudoHandle = global::Interop.BCrypt.BCryptAlgPseudoHandle.BCRYPT_CSHAKE128_ALG_HANDLE;
				num = destination.Length;
				break;
			case "CSHAKE256":
				bCryptAlgPseudoHandle = global::Interop.BCrypt.BCryptAlgPseudoHandle.BCRYPT_CSHAKE256_ALG_HANDLE;
				num = destination.Length;
				break;
			default:
				throw new CryptographicException();
			}
			if (destination.Length < num)
			{
				throw new CryptographicException();
			}
			fixed (byte* pbSecret = &MemoryMarshal.GetReference(key))
			{
				fixed (byte* pbInput = &MemoryMarshal.GetReference(source))
				{
					fixed (byte* pbOutput = &Helpers.GetNonNullPinnableReference(destination))
					{
						global::Interop.BCrypt.NTSTATUS nTSTATUS = global::Interop.BCrypt.BCryptHash((nuint)bCryptAlgPseudoHandle, pbSecret, key.Length, pbInput, source.Length, pbOutput, num);
						if (nTSTATUS != 0)
						{
							throw global::Interop.BCrypt.CreateCryptographicException(nTSTATUS);
						}
					}
				}
			}
			hashSize = num;
		}

		private static void HashUpdateAndFinish(SafeBCryptAlgorithmHandle algHandle, int hashSize, ReadOnlySpan<byte> key, ReadOnlySpan<byte> source, Span<byte> destination)
		{
			global::Interop.BCrypt.NTSTATUS nTSTATUS = global::Interop.BCrypt.BCryptCreateHash(algHandle, out var phHash, IntPtr.Zero, 0, key, key.Length, global::Interop.BCrypt.BCryptCreateHashFlags.None);
			if (nTSTATUS != 0)
			{
				phHash.Dispose();
				throw global::Interop.BCrypt.CreateCryptographicException(nTSTATUS);
			}
			using (phHash)
			{
				nTSTATUS = global::Interop.BCrypt.BCryptHashData(phHash, source, source.Length, 0);
				if (nTSTATUS != 0)
				{
					throw global::Interop.BCrypt.CreateCryptographicException(nTSTATUS);
				}
				global::Interop.BCrypt.BCryptFinishHash(phHash, destination, hashSize, 0);
			}
		}
	}

	public static HashProvider CreateHashProvider(string hashAlgorithmId)
	{
		return new HashProviderCng(hashAlgorithmId, null);
	}

	public static HashProvider CreateMacProvider(string hashAlgorithmId, ReadOnlySpan<byte> key)
	{
		return new HashProviderCng(hashAlgorithmId, key, isHmac: true);
	}

	internal static bool HashSupported(string hashAlgorithmId)
	{
		switch (hashAlgorithmId)
		{
		case "SHA256":
		case "SHA384":
		case "SHA512":
		case "MD5":
		case "SHA1":
			return true;
		case "SHA3-256":
		case "SHA3-384":
		case "SHA3-512":
		case "CSHAKE128":
		case "CSHAKE256":
			return global::Interop.BCrypt.BCryptAlgorithmCache.IsBCryptAlgorithmSupported(hashAlgorithmId, global::Interop.BCrypt.BCryptOpenAlgorithmProviderFlags.None);
		default:
			return false;
		}
	}

	internal static bool MacSupported(string hashAlgorithmId)
	{
		switch (hashAlgorithmId)
		{
		case "SHA256":
		case "SHA384":
		case "SHA512":
		case "MD5":
		case "SHA1":
			return true;
		case "SHA3-256":
		case "SHA3-384":
		case "SHA3-512":
			return global::Interop.BCrypt.BCryptAlgorithmCache.IsBCryptAlgorithmSupported(hashAlgorithmId, global::Interop.BCrypt.BCryptOpenAlgorithmProviderFlags.BCRYPT_ALG_HANDLE_HMAC_FLAG);
		default:
			return false;
		}
	}
}
