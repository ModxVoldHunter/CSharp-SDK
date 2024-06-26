using System.Buffers.Binary;

namespace System.Security.Cryptography;

internal static class RsaPaddingProcessor
{
	private static ReadOnlySpan<byte> DigestInfoMD5 => new byte[18]
	{
		48, 32, 48, 12, 6, 8, 42, 134, 72, 134,
		247, 13, 2, 5, 5, 0, 4, 16
	};

	private static ReadOnlySpan<byte> DigestInfoSha1 => new byte[15]
	{
		48, 33, 48, 9, 6, 5, 43, 14, 3, 2,
		26, 5, 0, 4, 20
	};

	private static ReadOnlySpan<byte> DigestInfoSha256 => new byte[19]
	{
		48, 49, 48, 13, 6, 9, 96, 134, 72, 1,
		101, 3, 4, 2, 1, 5, 0, 4, 32
	};

	private static ReadOnlySpan<byte> DigestInfoSha384 => new byte[19]
	{
		48, 65, 48, 13, 6, 9, 96, 134, 72, 1,
		101, 3, 4, 2, 2, 5, 0, 4, 48
	};

	private static ReadOnlySpan<byte> DigestInfoSha512 => new byte[19]
	{
		48, 81, 48, 13, 6, 9, 96, 134, 72, 1,
		101, 3, 4, 2, 3, 5, 0, 4, 64
	};

	private static ReadOnlySpan<byte> DigestInfoSha3_256 => new byte[19]
	{
		48, 49, 48, 13, 6, 9, 96, 134, 72, 1,
		101, 3, 4, 2, 8, 5, 0, 4, 32
	};

	private static ReadOnlySpan<byte> DigestInfoSha3_384 => new byte[19]
	{
		48, 65, 48, 13, 6, 9, 96, 134, 72, 1,
		101, 3, 4, 2, 9, 5, 0, 4, 48
	};

	private static ReadOnlySpan<byte> DigestInfoSha3_512 => new byte[19]
	{
		48, 81, 48, 13, 6, 9, 96, 134, 72, 1,
		101, 3, 4, 2, 10, 5, 0, 4, 64
	};

	private static ReadOnlySpan<byte> GetDigestInfoForAlgorithm(HashAlgorithmName hashAlgorithmName, out int digestLengthInBytes)
	{
		switch (hashAlgorithmName.Name)
		{
		case "MD5":
			digestLengthInBytes = 16;
			return DigestInfoMD5;
		case "SHA1":
			digestLengthInBytes = 20;
			return DigestInfoSha1;
		case "SHA256":
			digestLengthInBytes = 32;
			return DigestInfoSha256;
		case "SHA384":
			digestLengthInBytes = 48;
			return DigestInfoSha384;
		case "SHA512":
			digestLengthInBytes = 64;
			return DigestInfoSha512;
		case "SHA3-256":
			digestLengthInBytes = 32;
			return DigestInfoSha3_256;
		case "SHA3-384":
			digestLengthInBytes = 48;
			return DigestInfoSha3_384;
		case "SHA3-512":
			digestLengthInBytes = 64;
			return DigestInfoSha3_512;
		default:
			throw new CryptographicException();
		}
	}

	internal static int BytesRequiredForBitCount(int keySizeInBits)
	{
		return (int)((uint)(keySizeInBits + 7) / 8u);
	}

	internal static int HashLength(HashAlgorithmName hashAlgorithmName)
	{
		GetDigestInfoForAlgorithm(hashAlgorithmName, out var digestLengthInBytes);
		return digestLengthInBytes;
	}

	internal static void PadPkcs1Encryption(ReadOnlySpan<byte> source, Span<byte> destination)
	{
		int length = source.Length;
		int length2 = destination.Length;
		if (length > length2 - 11)
		{
			throw new CryptographicException(System.SR.Cryptography_KeyTooSmall);
		}
		Span<byte> destination2 = destination.Slice(destination.Length - source.Length);
		Span<byte> data = destination.Slice(2, destination.Length - source.Length - 3);
		destination[0] = 0;
		destination[1] = 2;
		destination[data.Length + 2] = 0;
		RandomNumberGeneratorImplementation.FillNonZeroBytes(data);
		source.CopyTo(destination2);
	}

	internal static void PadOaep(HashAlgorithmName hashAlgorithmName, ReadOnlySpan<byte> source, Span<byte> destination)
	{
		int num = HashLength(hashAlgorithmName);
		byte[] array = null;
		Span<byte> span = Span<byte>.Empty;
		try
		{
			int num2 = checked(destination.Length - num - num - 2);
			if (source.Length > num2)
			{
				throw new CryptographicException(System.SR.Format(System.SR.Cryptography_Encryption_MessageTooLong, num2));
			}
			Span<byte> span2 = destination.Slice(1, num);
			Span<byte> span3 = destination.Slice(1 + num);
			using IncrementalHash incrementalHash = IncrementalHash.CreateHash(hashAlgorithmName);
			Span<byte> destination2 = span3.Slice(0, num);
			Span<byte> destination3 = span3.Slice(span3.Length - source.Length);
			Span<byte> span4 = span3.Slice(num, span3.Length - num - 1 - destination3.Length);
			Span<byte> span5 = span3.Slice(num + span4.Length, 1);
			if (!incrementalHash.TryGetHashAndReset(destination2, out var bytesWritten) || bytesWritten != num)
			{
				throw new CryptographicException();
			}
			span4.Clear();
			span5[0] = 1;
			source.CopyTo(destination3);
			RandomNumberGenerator.Fill(span2);
			array = System.Security.Cryptography.CryptoPool.Rent(span3.Length);
			span = new Span<byte>(array, 0, span3.Length);
			Mgf1(incrementalHash, span2, span);
			Xor(span3, span);
			Span<byte> span6 = stackalloc byte[num];
			Mgf1(incrementalHash, span3, span6);
			Xor(span2, span6);
			destination[0] = 0;
		}
		catch (Exception ex) when (!(ex is CryptographicException))
		{
			throw new CryptographicException();
		}
		finally
		{
			if (array != null)
			{
				CryptographicOperations.ZeroMemory(span);
				System.Security.Cryptography.CryptoPool.Return(array, 0);
			}
		}
	}

	private static void Mgf1(IncrementalHash hasher, ReadOnlySpan<byte> mgfSeed, Span<byte> mask)
	{
		int hashLengthInBytes = hasher.HashLengthInBytes;
		Span<byte> destination = mask;
		int num = 0;
		Span<byte> span = stackalloc byte[4];
		while (destination.Length > 0)
		{
			hasher.AppendData(mgfSeed);
			BinaryPrimitives.WriteInt32BigEndian(span, num);
			hasher.AppendData(span);
			if (destination.Length >= hashLengthInBytes)
			{
				if (!hasher.TryGetHashAndReset(destination, out var bytesWritten))
				{
					throw new CryptographicException();
				}
				destination = destination.Slice(bytesWritten);
				num++;
				continue;
			}
			Span<byte> destination2 = stackalloc byte[hashLengthInBytes];
			if (!hasher.TryGetHashAndReset(destination2, out var _))
			{
				throw new CryptographicException();
			}
			destination2.Slice(0, destination.Length).CopyTo(destination);
			break;
		}
	}

	private static void Xor(Span<byte> a, ReadOnlySpan<byte> b)
	{
		if (a.Length != b.Length)
		{
			throw new InvalidOperationException();
		}
		for (int i = 0; i < b.Length; i++)
		{
			a[i] ^= b[i];
		}
	}
}
