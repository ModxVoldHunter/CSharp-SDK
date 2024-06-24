using System.Runtime.CompilerServices;
using System.Threading;
using Microsoft.Win32.SafeHandles;

namespace System.Security.Cryptography;

internal static class Pbkdf2Implementation
{
	private static readonly bool s_useKeyDerivation = OperatingSystem.IsWindowsVersionAtLeast(6, 2);

	private static SafeBCryptAlgorithmHandle s_pbkdf2AlgorithmHandle;

	public static void Fill(ReadOnlySpan<byte> password, ReadOnlySpan<byte> salt, int iterations, HashAlgorithmName hashAlgorithmName, Span<byte> destination)
	{
		if (s_useKeyDerivation)
		{
			FillKeyDerivation(password, salt, iterations, hashAlgorithmName.Name, destination);
		}
		else
		{
			FillDeriveKeyPBKDF2(password, salt, iterations, hashAlgorithmName.Name, destination);
		}
	}

	private unsafe static void FillKeyDerivation(ReadOnlySpan<byte> password, ReadOnlySpan<byte> salt, int iterations, string hashAlgorithmName, Span<byte> destination)
	{
		int hashBlockSize = GetHashBlockSize(hashAlgorithmName);
		ReadOnlySpan<byte> readOnlySpan;
		int cbSecret;
		Span<byte> span2;
		if (password.IsEmpty)
		{
			Span<byte> span = stackalloc byte[1];
			readOnlySpan = span;
			cbSecret = 0;
			span2 = default(Span<byte>);
		}
		else if (password.Length <= hashBlockSize)
		{
			readOnlySpan = password;
			cbSecret = password.Length;
			span2 = default(Span<byte>);
		}
		else
		{
			Span<byte> destination2 = stackalloc byte[64];
			int num = hashAlgorithmName switch
			{
				"SHA1" => SHA1.HashData(password, destination2), 
				"SHA256" => SHA256.HashData(password, destination2), 
				"SHA384" => SHA384.HashData(password, destination2), 
				"SHA512" => SHA512.HashData(password, destination2), 
				"SHA3-256" => SHA3_256.HashData(password, destination2), 
				"SHA3-384" => SHA3_384.HashData(password, destination2), 
				"SHA3-512" => SHA3_512.HashData(password, destination2), 
				_ => throw new CryptographicException(), 
			};
			span2 = destination2.Slice(0, num);
			readOnlySpan = span2;
			cbSecret = num;
		}
		global::Interop.BCrypt.NTSTATUS nTSTATUS;
		SafeBCryptKeyHandle phKey;
		if (global::Interop.BCrypt.PseudoHandlesSupported)
		{
			fixed (byte* pbSecret = readOnlySpan)
			{
				nTSTATUS = global::Interop.BCrypt.BCryptGenerateSymmetricKey(817u, out phKey, IntPtr.Zero, 0, pbSecret, cbSecret, 0u);
			}
		}
		else
		{
			if (s_pbkdf2AlgorithmHandle == null)
			{
				SafeBCryptAlgorithmHandle phAlgorithm;
				global::Interop.BCrypt.NTSTATUS nTSTATUS2 = global::Interop.BCrypt.BCryptOpenAlgorithmProvider(out phAlgorithm, "PBKDF2", null, global::Interop.BCrypt.BCryptOpenAlgorithmProviderFlags.None);
				if (nTSTATUS2 != 0)
				{
					phAlgorithm.Dispose();
					CryptographicOperations.ZeroMemory(span2);
					throw global::Interop.BCrypt.CreateCryptographicException(nTSTATUS2);
				}
				Interlocked.CompareExchange(ref s_pbkdf2AlgorithmHandle, phAlgorithm, null);
			}
			fixed (byte* pbSecret2 = readOnlySpan)
			{
				nTSTATUS = global::Interop.BCrypt.BCryptGenerateSymmetricKey(s_pbkdf2AlgorithmHandle, out phKey, IntPtr.Zero, 0, pbSecret2, cbSecret, 0u);
			}
		}
		CryptographicOperations.ZeroMemory(span2);
		if (nTSTATUS != 0)
		{
			phKey.Dispose();
			throw global::Interop.BCrypt.CreateCryptographicException(nTSTATUS);
		}
		ulong num2 = (ulong)iterations;
		using (phKey)
		{
			fixed (char* pvBuffer2 = hashAlgorithmName)
			{
				fixed (byte* pvBuffer = salt)
				{
					fixed (byte* pbDerivedKey = destination)
					{
						Span<global::Interop.BCrypt.BCryptBuffer> span3 = stackalloc global::Interop.BCrypt.BCryptBuffer[3];
						span3[0].BufferType = global::Interop.BCrypt.CngBufferDescriptors.KDF_ITERATION_COUNT;
						span3[0].pvBuffer = (nint)(&num2);
						span3[0].cbBuffer = 8;
						span3[1].BufferType = global::Interop.BCrypt.CngBufferDescriptors.KDF_SALT;
						span3[1].pvBuffer = (nint)pvBuffer;
						span3[1].cbBuffer = salt.Length;
						span3[2].BufferType = global::Interop.BCrypt.CngBufferDescriptors.KDF_HASH_ALGORITHM;
						span3[2].pvBuffer = (nint)pvBuffer2;
						span3[2].cbBuffer = checked((hashAlgorithmName.Length + 1) * 2);
						fixed (global::Interop.BCrypt.BCryptBuffer* pBuffers = span3)
						{
							Unsafe.SkipInit(out global::Interop.BCrypt.BCryptBufferDesc bCryptBufferDesc);
							bCryptBufferDesc.ulVersion = 0;
							bCryptBufferDesc.cBuffers = span3.Length;
							bCryptBufferDesc.pBuffers = (nint)pBuffers;
							uint pcbResult;
							global::Interop.BCrypt.NTSTATUS nTSTATUS3 = global::Interop.BCrypt.BCryptKeyDerivation(phKey, &bCryptBufferDesc, pbDerivedKey, destination.Length, out pcbResult, 0);
							if (nTSTATUS3 != 0)
							{
								throw global::Interop.BCrypt.CreateCryptographicException(nTSTATUS3);
							}
							if (destination.Length != pcbResult)
							{
								throw new CryptographicException();
							}
						}
					}
				}
			}
		}
	}

	private unsafe static void FillDeriveKeyPBKDF2(ReadOnlySpan<byte> password, ReadOnlySpan<byte> salt, int iterations, string hashAlgorithmName, Span<byte> destination)
	{
		int hashSizeInBytes;
		SafeBCryptAlgorithmHandle cachedBCryptAlgorithmHandle = global::Interop.BCrypt.BCryptAlgorithmCache.GetCachedBCryptAlgorithmHandle(hashAlgorithmName, global::Interop.BCrypt.BCryptOpenAlgorithmProviderFlags.BCRYPT_ALG_HANDLE_HMAC_FLAG, out hashSizeInBytes);
		fixed (byte* pbPassword = password)
		{
			fixed (byte* pbSalt = salt)
			{
				fixed (byte* pbDerivedKey = destination)
				{
					global::Interop.BCrypt.NTSTATUS nTSTATUS = global::Interop.BCrypt.BCryptDeriveKeyPBKDF2(cachedBCryptAlgorithmHandle, pbPassword, password.Length, pbSalt, salt.Length, (ulong)iterations, pbDerivedKey, destination.Length, 0u);
					if (nTSTATUS != 0)
					{
						throw global::Interop.BCrypt.CreateCryptographicException(nTSTATUS);
					}
				}
			}
		}
	}

	private static int GetHashBlockSize(string hashAlgorithmName)
	{
		switch (hashAlgorithmName)
		{
		case "SHA256":
		case "SHA1":
			return 64;
		case "SHA384":
		case "SHA512":
			return 128;
		case "SHA3-256":
			return 136;
		case "SHA3-384":
			return 104;
		case "SHA3-512":
			return 72;
		default:
			throw new CryptographicException();
		}
	}
}
