using System.IO;
using Internal.Cryptography;

namespace System.Security.Cryptography;

internal static class HashOneShotHelpers
{
	internal static byte[] HashData(HashAlgorithmName hashAlgorithm, ReadOnlySpan<byte> source)
	{
		switch (hashAlgorithm.Name)
		{
		case "SHA256":
			return SHA256.HashData(source);
		case "SHA1":
			return SHA1.HashData(source);
		case "SHA512":
			return SHA512.HashData(source);
		case "SHA384":
			return SHA384.HashData(source);
		case "SHA3-256":
			return SHA3_256.HashData(source);
		case "SHA3-384":
			return SHA3_384.HashData(source);
		case "SHA3-512":
			return SHA3_512.HashData(source);
		case "MD5":
			if (Helpers.HasMD5)
			{
				return MD5.HashData(source);
			}
			break;
		}
		throw new CryptographicException(System.SR.Format(System.SR.Cryptography_UnknownHashAlgorithm, hashAlgorithm.Name));
	}

	internal static bool TryHashData(HashAlgorithmName hashAlgorithm, ReadOnlySpan<byte> source, Span<byte> destination, out int bytesWritten)
	{
		switch (hashAlgorithm.Name)
		{
		case "SHA256":
			return SHA256.TryHashData(source, destination, out bytesWritten);
		case "SHA1":
			return SHA1.TryHashData(source, destination, out bytesWritten);
		case "SHA512":
			return SHA512.TryHashData(source, destination, out bytesWritten);
		case "SHA384":
			return SHA384.TryHashData(source, destination, out bytesWritten);
		case "SHA3-256":
			return SHA3_256.TryHashData(source, destination, out bytesWritten);
		case "SHA3-384":
			return SHA3_384.TryHashData(source, destination, out bytesWritten);
		case "SHA3-512":
			return SHA3_512.TryHashData(source, destination, out bytesWritten);
		case "MD5":
			if (Helpers.HasMD5)
			{
				return MD5.TryHashData(source, destination, out bytesWritten);
			}
			break;
		}
		throw new CryptographicException(System.SR.Format(System.SR.Cryptography_UnknownHashAlgorithm, hashAlgorithm.Name));
	}

	internal static byte[] HashData(HashAlgorithmName hashAlgorithm, Stream source)
	{
		switch (hashAlgorithm.Name)
		{
		case "SHA256":
			return SHA256.HashData(source);
		case "SHA1":
			return SHA1.HashData(source);
		case "SHA512":
			return SHA512.HashData(source);
		case "SHA384":
			return SHA384.HashData(source);
		case "SHA3-256":
			return SHA3_256.HashData(source);
		case "SHA3-384":
			return SHA3_384.HashData(source);
		case "SHA3-512":
			return SHA3_512.HashData(source);
		case "MD5":
			if (Helpers.HasMD5)
			{
				return MD5.HashData(source);
			}
			break;
		}
		throw new CryptographicException(System.SR.Format(System.SR.Cryptography_UnknownHashAlgorithm, hashAlgorithm.Name));
	}

	internal static int MacData(HashAlgorithmName hashAlgorithm, ReadOnlySpan<byte> key, ReadOnlySpan<byte> source, Span<byte> destination)
	{
		switch (hashAlgorithm.Name)
		{
		case "SHA256":
			return HMACSHA256.HashData(key, source, destination);
		case "SHA1":
			return HMACSHA1.HashData(key, source, destination);
		case "SHA512":
			return HMACSHA512.HashData(key, source, destination);
		case "SHA384":
			return HMACSHA384.HashData(key, source, destination);
		case "SHA3-256":
			return HMACSHA3_256.HashData(key, source, destination);
		case "SHA3-384":
			return HMACSHA3_384.HashData(key, source, destination);
		case "SHA3-512":
			return HMACSHA3_512.HashData(key, source, destination);
		case "MD5":
			if (Helpers.HasMD5)
			{
				return HMACMD5.HashData(key, source, destination);
			}
			break;
		}
		throw new CryptographicException(System.SR.Format(System.SR.Cryptography_UnknownHashAlgorithm, hashAlgorithm.Name));
	}
}
