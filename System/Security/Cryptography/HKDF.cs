namespace System.Security.Cryptography;

public static class HKDF
{
	public static byte[] Extract(HashAlgorithmName hashAlgorithmName, byte[] ikm, byte[]? salt = null)
	{
		ArgumentNullException.ThrowIfNull(ikm, "ikm");
		int num = HashLength(hashAlgorithmName);
		byte[] array = new byte[num];
		Extract(hashAlgorithmName, num, ikm, salt, array);
		return array;
	}

	public static int Extract(HashAlgorithmName hashAlgorithmName, ReadOnlySpan<byte> ikm, ReadOnlySpan<byte> salt, Span<byte> prk)
	{
		int num = HashLength(hashAlgorithmName);
		if (prk.Length < num)
		{
			throw new ArgumentException(System.SR.Format(System.SR.Cryptography_Prk_TooSmall, num), "prk");
		}
		if (prk.Length > num)
		{
			prk = prk.Slice(0, num);
		}
		Extract(hashAlgorithmName, num, ikm, salt, prk);
		return num;
	}

	private static void Extract(HashAlgorithmName hashAlgorithmName, int hashLength, ReadOnlySpan<byte> ikm, ReadOnlySpan<byte> salt, Span<byte> prk)
	{
		int num = HashOneShotHelpers.MacData(hashAlgorithmName, salt, ikm, prk);
	}

	public static byte[] Expand(HashAlgorithmName hashAlgorithmName, byte[] prk, int outputLength, byte[]? info = null)
	{
		ArgumentNullException.ThrowIfNull(prk, "prk");
		ArgumentOutOfRangeException.ThrowIfNegativeOrZero(outputLength, "outputLength");
		int num = HashLength(hashAlgorithmName);
		int num2 = 255 * num;
		if (outputLength <= 0 || outputLength > num2)
		{
			throw new ArgumentOutOfRangeException("outputLength", System.SR.Format(System.SR.Cryptography_Okm_TooLarge, num2));
		}
		byte[] array = new byte[outputLength];
		Expand(hashAlgorithmName, num, prk, array, info);
		return array;
	}

	public static void Expand(HashAlgorithmName hashAlgorithmName, ReadOnlySpan<byte> prk, Span<byte> output, ReadOnlySpan<byte> info)
	{
		int num = HashLength(hashAlgorithmName);
		if (output.Length == 0)
		{
			throw new ArgumentException(System.SR.Argument_DestinationTooShort, "output");
		}
		int num2 = 255 * num;
		if (output.Length > num2)
		{
			throw new ArgumentException(System.SR.Format(System.SR.Cryptography_Okm_TooLarge, num2), "output");
		}
		Expand(hashAlgorithmName, num, prk, output, info);
	}

	private static void Expand(HashAlgorithmName hashAlgorithmName, int hashLength, ReadOnlySpan<byte> prk, Span<byte> output, ReadOnlySpan<byte> info)
	{
		if (prk.Length < hashLength)
		{
			throw new ArgumentException(System.SR.Format(System.SR.Cryptography_Prk_TooSmall, hashLength), "prk");
		}
		byte reference = 0;
		Span<byte> span = new Span<byte>(ref reference);
		Span<byte> span2 = Span<byte>.Empty;
		Span<byte> destination = output;
		Span<byte> span3 = stackalloc byte[64];
		byte[] array = null;
		ReadOnlySpan<byte> data;
		if (output.Overlaps(info))
		{
			if (info.Length > 64)
			{
				array = System.Security.Cryptography.CryptoPool.Rent(info.Length);
				span3 = array;
			}
			span3 = span3.Slice(0, info.Length);
			info.CopyTo(span3);
			data = span3;
		}
		else
		{
			data = info;
		}
		using (IncrementalHash incrementalHash = IncrementalHash.CreateHMAC(hashAlgorithmName, prk))
		{
			int num = 1;
			while (true)
			{
				incrementalHash.AppendData(span2);
				incrementalHash.AppendData(data);
				reference = (byte)num;
				incrementalHash.AppendData(span);
				if (destination.Length < hashLength)
				{
					break;
				}
				span2 = destination.Slice(0, hashLength);
				destination = destination.Slice(hashLength);
				GetHashAndReset(incrementalHash, span2);
				num++;
			}
			if (destination.Length > 0)
			{
				Span<byte> output2 = stackalloc byte[hashLength];
				GetHashAndReset(incrementalHash, output2);
				output2.Slice(0, destination.Length).CopyTo(destination);
			}
		}
		if (array != null)
		{
			System.Security.Cryptography.CryptoPool.Return(array, info.Length);
		}
	}

	public static byte[] DeriveKey(HashAlgorithmName hashAlgorithmName, byte[] ikm, int outputLength, byte[]? salt = null, byte[]? info = null)
	{
		ArgumentNullException.ThrowIfNull(ikm, "ikm");
		ArgumentOutOfRangeException.ThrowIfNegativeOrZero(outputLength, "outputLength");
		int num = HashLength(hashAlgorithmName);
		int num2 = 255 * num;
		if (outputLength > num2)
		{
			throw new ArgumentOutOfRangeException("outputLength", System.SR.Format(System.SR.Cryptography_Okm_TooLarge, num2));
		}
		Span<byte> span = stackalloc byte[num];
		Extract(hashAlgorithmName, num, ikm, salt, span);
		byte[] array = new byte[outputLength];
		Expand(hashAlgorithmName, num, span, array, info);
		return array;
	}

	public static void DeriveKey(HashAlgorithmName hashAlgorithmName, ReadOnlySpan<byte> ikm, Span<byte> output, ReadOnlySpan<byte> salt, ReadOnlySpan<byte> info)
	{
		int num = HashLength(hashAlgorithmName);
		if (output.Length == 0)
		{
			throw new ArgumentException(System.SR.Argument_DestinationTooShort, "output");
		}
		int num2 = 255 * num;
		if (output.Length > num2)
		{
			throw new ArgumentException(System.SR.Format(System.SR.Cryptography_Okm_TooLarge, num2), "output");
		}
		Span<byte> span = stackalloc byte[num];
		Extract(hashAlgorithmName, num, ikm, salt, span);
		Expand(hashAlgorithmName, num, span, output, info);
	}

	private static void GetHashAndReset(IncrementalHash hmac, Span<byte> output)
	{
		if (!hmac.TryGetHashAndReset(output, out var _))
		{
			throw new CryptographicException(System.SR.Arg_CryptographyException);
		}
	}

	private static int HashLength(HashAlgorithmName hashAlgorithmName)
	{
		if (hashAlgorithmName == HashAlgorithmName.SHA1)
		{
			return 20;
		}
		if (hashAlgorithmName == HashAlgorithmName.SHA256)
		{
			return 32;
		}
		if (hashAlgorithmName == HashAlgorithmName.SHA384)
		{
			return 48;
		}
		if (hashAlgorithmName == HashAlgorithmName.SHA512)
		{
			return 64;
		}
		if (hashAlgorithmName == HashAlgorithmName.SHA3_256)
		{
			return 32;
		}
		if (hashAlgorithmName == HashAlgorithmName.SHA3_384)
		{
			return 48;
		}
		if (hashAlgorithmName == HashAlgorithmName.SHA3_512)
		{
			return 64;
		}
		if (hashAlgorithmName == HashAlgorithmName.MD5)
		{
			return 16;
		}
		throw new ArgumentOutOfRangeException("hashAlgorithmName");
	}
}
