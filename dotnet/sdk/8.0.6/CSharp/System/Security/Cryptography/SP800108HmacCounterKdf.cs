namespace System.Security.Cryptography;

public sealed class SP800108HmacCounterKdf : IDisposable
{
	private readonly SP800108HmacCounterKdfImplementationBase _implementation;

	private static readonly bool s_isWindows8OrGreater = OperatingSystem.IsWindowsVersionAtLeast(6, 2);

	private static SP800108HmacCounterKdfImplementationBase CreateImplementation(ReadOnlySpan<byte> key, HashAlgorithmName hashAlgorithm)
	{
		if (s_isWindows8OrGreater)
		{
			return new SP800108HmacCounterKdfImplementationCng(key, hashAlgorithm);
		}
		return new SP800108HmacCounterKdfImplementationManaged(key, hashAlgorithm);
	}

	public SP800108HmacCounterKdf(ReadOnlySpan<byte> key, HashAlgorithmName hashAlgorithm)
	{
		CheckHashAlgorithm(hashAlgorithm);
		_implementation = CreateImplementation(key, hashAlgorithm);
	}

	public SP800108HmacCounterKdf(byte[] key, HashAlgorithmName hashAlgorithm)
	{
		if (key == null)
		{
			throw new ArgumentNullException("key");
		}
		CheckHashAlgorithm(hashAlgorithm);
		_implementation = CreateImplementation(key, hashAlgorithm);
	}

	public static byte[] DeriveBytes(byte[] key, HashAlgorithmName hashAlgorithm, byte[] label, byte[] context, int derivedKeyLengthInBytes)
	{
		if (key == null)
		{
			throw new ArgumentNullException("key");
		}
		if (label == null)
		{
			throw new ArgumentNullException("label");
		}
		if (context == null)
		{
			throw new ArgumentNullException("context");
		}
		CheckPrfOutputLength(derivedKeyLengthInBytes, "derivedKeyLengthInBytes");
		CheckHashAlgorithm(hashAlgorithm);
		return DeriveBytesCore(key, hashAlgorithm, label, context, derivedKeyLengthInBytes);
	}

	public static byte[] DeriveBytes(byte[] key, HashAlgorithmName hashAlgorithm, string label, string context, int derivedKeyLengthInBytes)
	{
		if (key == null)
		{
			throw new ArgumentNullException("key");
		}
		if (label == null)
		{
			throw new ArgumentNullException("label");
		}
		if (context == null)
		{
			throw new ArgumentNullException("context");
		}
		CheckPrfOutputLength(derivedKeyLengthInBytes, "derivedKeyLengthInBytes");
		CheckHashAlgorithm(hashAlgorithm);
		byte[] array = new byte[derivedKeyLengthInBytes];
		DeriveBytesCore(key, hashAlgorithm, label.AsSpan(), context.AsSpan(), array);
		return array;
	}

	public static byte[] DeriveBytes(ReadOnlySpan<byte> key, HashAlgorithmName hashAlgorithm, ReadOnlySpan<byte> label, ReadOnlySpan<byte> context, int derivedKeyLengthInBytes)
	{
		CheckPrfOutputLength(derivedKeyLengthInBytes, "derivedKeyLengthInBytes");
		byte[] array = new byte[derivedKeyLengthInBytes];
		DeriveBytes(key, hashAlgorithm, label, context, array);
		return array;
	}

	public static void DeriveBytes(ReadOnlySpan<byte> key, HashAlgorithmName hashAlgorithm, ReadOnlySpan<byte> label, ReadOnlySpan<byte> context, Span<byte> destination)
	{
		CheckHashAlgorithm(hashAlgorithm);
		CheckPrfOutputLength(destination.Length, "destination");
		DeriveBytesCore(key, hashAlgorithm, label, context, destination);
	}

	public static byte[] DeriveBytes(ReadOnlySpan<byte> key, HashAlgorithmName hashAlgorithm, ReadOnlySpan<char> label, ReadOnlySpan<char> context, int derivedKeyLengthInBytes)
	{
		CheckPrfOutputLength(derivedKeyLengthInBytes, "derivedKeyLengthInBytes");
		byte[] array = new byte[derivedKeyLengthInBytes];
		DeriveBytes(key, hashAlgorithm, label, context, array);
		return array;
	}

	public static void DeriveBytes(ReadOnlySpan<byte> key, HashAlgorithmName hashAlgorithm, ReadOnlySpan<char> label, ReadOnlySpan<char> context, Span<byte> destination)
	{
		CheckHashAlgorithm(hashAlgorithm);
		CheckPrfOutputLength(destination.Length, "destination");
		DeriveBytesCore(key, hashAlgorithm, label, context, destination);
	}

	public byte[] DeriveKey(byte[] label, byte[] context, int derivedKeyLengthInBytes)
	{
		if (label == null)
		{
			throw new ArgumentNullException("label");
		}
		if (context == null)
		{
			throw new ArgumentNullException("context");
		}
		CheckPrfOutputLength(derivedKeyLengthInBytes, "derivedKeyLengthInBytes");
		byte[] array = new byte[derivedKeyLengthInBytes];
		DeriveKeyCore(label, context, array);
		return array;
	}

	public byte[] DeriveKey(ReadOnlySpan<byte> label, ReadOnlySpan<byte> context, int derivedKeyLengthInBytes)
	{
		CheckPrfOutputLength(derivedKeyLengthInBytes, "derivedKeyLengthInBytes");
		byte[] array = new byte[derivedKeyLengthInBytes];
		DeriveKey(label, context, array);
		return array;
	}

	public void DeriveKey(ReadOnlySpan<byte> label, ReadOnlySpan<byte> context, Span<byte> destination)
	{
		CheckPrfOutputLength(destination.Length, "destination");
		DeriveKeyCore(label, context, destination);
	}

	public byte[] DeriveKey(ReadOnlySpan<char> label, ReadOnlySpan<char> context, int derivedKeyLengthInBytes)
	{
		CheckPrfOutputLength(derivedKeyLengthInBytes, "derivedKeyLengthInBytes");
		byte[] array = new byte[derivedKeyLengthInBytes];
		DeriveKeyCore(label, context, array);
		return array;
	}

	public void DeriveKey(ReadOnlySpan<char> label, ReadOnlySpan<char> context, Span<byte> destination)
	{
		CheckPrfOutputLength(destination.Length, "destination");
		DeriveKeyCore(label, context, destination);
	}

	public byte[] DeriveKey(string label, string context, int derivedKeyLengthInBytes)
	{
		if (label == null)
		{
			throw new ArgumentNullException("label");
		}
		if (context == null)
		{
			throw new ArgumentNullException("context");
		}
		return DeriveKey(label.AsSpan(), context.AsSpan(), derivedKeyLengthInBytes);
	}

	public void Dispose()
	{
		_implementation.Dispose();
	}

	private static void CheckHashAlgorithm(HashAlgorithmName hashAlgorithm)
	{
		string name = hashAlgorithm.Name;
		switch (name)
		{
		case null:
			throw new ArgumentNullException("hashAlgorithm");
		case "":
			throw new ArgumentException(System.SR.Argument_EmptyString, "hashAlgorithm");
		case "SHA3-256":
			if (!HMACSHA3_256.IsSupported)
			{
				throw new PlatformNotSupportedException();
			}
			break;
		case "SHA3-384":
			if (!HMACSHA3_384.IsSupported)
			{
				throw new PlatformNotSupportedException();
			}
			break;
		case "SHA3-512":
			if (!HMACSHA3_512.IsSupported)
			{
				throw new PlatformNotSupportedException();
			}
			break;
		default:
			throw new CryptographicException(System.SR.Format(System.SR.Cryptography_UnknownHashAlgorithm, name));
		case "SHA256":
			break;
		case "SHA384":
			break;
		case "SHA512":
			break;
		case "SHA1":
			break;
		}
	}

	private static byte[] DeriveBytesCore(byte[] key, HashAlgorithmName hashAlgorithm, byte[] label, byte[] context, int derivedKeyLengthInBytes)
	{
		byte[] array = new byte[derivedKeyLengthInBytes];
		if (s_isWindows8OrGreater)
		{
			SP800108HmacCounterKdfImplementationCng.DeriveBytesOneShot(key, hashAlgorithm, label, context, array);
		}
		else
		{
			SP800108HmacCounterKdfImplementationManaged.DeriveBytesOneShot(key, hashAlgorithm, label, context, array);
		}
		return array;
	}

	private static void DeriveBytesCore(ReadOnlySpan<byte> key, HashAlgorithmName hashAlgorithm, ReadOnlySpan<byte> label, ReadOnlySpan<byte> context, Span<byte> destination)
	{
		if (s_isWindows8OrGreater)
		{
			SP800108HmacCounterKdfImplementationCng.DeriveBytesOneShot(key, hashAlgorithm, label, context, destination);
		}
		else
		{
			SP800108HmacCounterKdfImplementationManaged.DeriveBytesOneShot(key, hashAlgorithm, label, context, destination);
		}
	}

	private static void DeriveBytesCore(ReadOnlySpan<byte> key, HashAlgorithmName hashAlgorithm, ReadOnlySpan<char> label, ReadOnlySpan<char> context, Span<byte> destination)
	{
		if (s_isWindows8OrGreater)
		{
			SP800108HmacCounterKdfImplementationCng.DeriveBytesOneShot(key, hashAlgorithm, label, context, destination);
		}
		else
		{
			SP800108HmacCounterKdfImplementationManaged.DeriveBytesOneShot(key, hashAlgorithm, label, context, destination);
		}
	}

	private void DeriveKeyCore(ReadOnlySpan<byte> label, ReadOnlySpan<byte> context, Span<byte> destination)
	{
		_implementation.DeriveBytes(label, context, destination);
	}

	private void DeriveKeyCore(ReadOnlySpan<char> label, ReadOnlySpan<char> context, Span<byte> destination)
	{
		_implementation.DeriveBytes(label, context, destination);
	}

	private static void CheckPrfOutputLength(int length, string paramName)
	{
		if (length > 536870911)
		{
			throw new ArgumentOutOfRangeException(paramName, System.SR.ArgumentOutOfRange_KOut_Too_Large);
		}
		if (length < 0)
		{
			throw new ArgumentOutOfRangeException(paramName, System.SR.ArgumentOutOfRange_NeedNonNegNum);
		}
	}
}
