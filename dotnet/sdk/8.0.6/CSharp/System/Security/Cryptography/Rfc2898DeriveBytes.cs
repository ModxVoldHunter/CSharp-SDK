using System.Buffers.Binary;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace System.Security.Cryptography;

public class Rfc2898DeriveBytes : DeriveBytes
{
	private byte[] _salt;

	private uint _iterations;

	private IncrementalHash _hmac;

	private readonly int _blockSize;

	private byte[] _buffer;

	private uint _block;

	private int _startIndex;

	private int _endIndex;

	private static readonly UTF8Encoding s_throwingUtf8Encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);

	public HashAlgorithmName HashAlgorithm { get; }

	public int IterationCount
	{
		get
		{
			return (int)_iterations;
		}
		set
		{
			ArgumentOutOfRangeException.ThrowIfNegativeOrZero(value, "value");
			_iterations = (uint)value;
			Initialize();
		}
	}

	public byte[] Salt
	{
		get
		{
			return _salt.AsSpan(0, _salt.Length - 4).ToArray();
		}
		set
		{
			ArgumentNullException.ThrowIfNull(value, "value");
			_salt = new byte[value.Length + 4];
			value.AsSpan().CopyTo(_salt);
			Initialize();
		}
	}

	[Obsolete("The default hash algorithm and iteration counts in Rfc2898DeriveBytes constructors are outdated and insecure. Use a constructor that accepts the hash algorithm and the number of iterations.", DiagnosticId = "SYSLIB0041", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
	public Rfc2898DeriveBytes(byte[] password, byte[] salt, int iterations)
		: this(password, salt, iterations, HashAlgorithmName.SHA1)
	{
	}

	public Rfc2898DeriveBytes(byte[] password, byte[] salt, int iterations, HashAlgorithmName hashAlgorithm)
		: this(password, salt, iterations, hashAlgorithm, clearPassword: false)
	{
	}

	[Obsolete("The default hash algorithm and iteration counts in Rfc2898DeriveBytes constructors are outdated and insecure. Use a constructor that accepts the hash algorithm and the number of iterations.", DiagnosticId = "SYSLIB0041", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
	public Rfc2898DeriveBytes(string password, byte[] salt)
		: this(password, salt, 1000)
	{
	}

	[Obsolete("The default hash algorithm and iteration counts in Rfc2898DeriveBytes constructors are outdated and insecure. Use a constructor that accepts the hash algorithm and the number of iterations.", DiagnosticId = "SYSLIB0041", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
	public Rfc2898DeriveBytes(string password, byte[] salt, int iterations)
		: this(password, salt, iterations, HashAlgorithmName.SHA1)
	{
	}

	public Rfc2898DeriveBytes(string password, byte[] salt, int iterations, HashAlgorithmName hashAlgorithm)
		: this(Encoding.UTF8.GetBytes(password ?? throw new ArgumentNullException("password")), salt, iterations, hashAlgorithm, clearPassword: true)
	{
	}

	[Obsolete("The default hash algorithm and iteration counts in Rfc2898DeriveBytes constructors are outdated and insecure. Use a constructor that accepts the hash algorithm and the number of iterations.", DiagnosticId = "SYSLIB0041", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
	public Rfc2898DeriveBytes(string password, int saltSize)
		: this(password, saltSize, 1000)
	{
	}

	[Obsolete("The default hash algorithm and iteration counts in Rfc2898DeriveBytes constructors are outdated and insecure. Use a constructor that accepts the hash algorithm and the number of iterations.", DiagnosticId = "SYSLIB0041", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
	public Rfc2898DeriveBytes(string password, int saltSize, int iterations)
		: this(password, saltSize, iterations, HashAlgorithmName.SHA1)
	{
	}

	public Rfc2898DeriveBytes(string password, int saltSize, int iterations, HashAlgorithmName hashAlgorithm)
	{
		ArgumentOutOfRangeException.ThrowIfNegative(saltSize, "saltSize");
		ArgumentOutOfRangeException.ThrowIfNegativeOrZero(iterations, "iterations");
		_salt = new byte[saltSize + 4];
		RandomNumberGenerator.Fill(_salt.AsSpan(0, saltSize));
		_iterations = (uint)iterations;
		byte[] bytes = Encoding.UTF8.GetBytes(password);
		HashAlgorithm = hashAlgorithm;
		_hmac = OpenHmac(bytes);
		CryptographicOperations.ZeroMemory(bytes);
		_blockSize = _hmac.HashLengthInBytes;
		Initialize();
	}

	internal Rfc2898DeriveBytes(byte[] password, byte[] salt, int iterations, HashAlgorithmName hashAlgorithm, bool clearPassword)
		: this(new ReadOnlySpan<byte>(password ?? throw new ArgumentNullException("password")), new ReadOnlySpan<byte>(salt ?? throw new ArgumentNullException("salt")), iterations, hashAlgorithm)
	{
		if (clearPassword)
		{
			CryptographicOperations.ZeroMemory(password);
		}
	}

	internal Rfc2898DeriveBytes(ReadOnlySpan<byte> password, ReadOnlySpan<byte> salt, int iterations, HashAlgorithmName hashAlgorithm)
	{
		ArgumentOutOfRangeException.ThrowIfNegativeOrZero(iterations, "iterations");
		_salt = new byte[salt.Length + 4];
		salt.CopyTo(_salt);
		_iterations = (uint)iterations;
		HashAlgorithm = hashAlgorithm;
		_hmac = OpenHmac(password);
		_blockSize = _hmac.HashLengthInBytes;
		Initialize();
	}

	protected override void Dispose(bool disposing)
	{
		if (disposing)
		{
			if (_hmac != null)
			{
				_hmac.Dispose();
				_hmac = null;
			}
			if (_buffer != null)
			{
				Array.Clear(_buffer);
			}
			if (_salt != null)
			{
				Array.Clear(_salt);
			}
		}
		base.Dispose(disposing);
	}

	public override byte[] GetBytes(int cb)
	{
		ArgumentOutOfRangeException.ThrowIfNegativeOrZero(cb, "cb");
		byte[] array = new byte[cb];
		GetBytes(array);
		return array;
	}

	internal void GetBytes(Span<byte> destination)
	{
		int length = destination.Length;
		int i = 0;
		int num = _endIndex - _startIndex;
		ReadOnlySpan<byte> readOnlySpan = _buffer;
		if (num > 0)
		{
			if (length < num)
			{
				readOnlySpan.Slice(_startIndex, length).CopyTo(destination);
				_startIndex += length;
				return;
			}
			readOnlySpan.Slice(_startIndex, num).CopyTo(destination);
			_startIndex = (_endIndex = 0);
			i += num;
		}
		for (; i < length; i += _blockSize)
		{
			Func();
			int num2 = length - i;
			if (num2 >= _blockSize)
			{
				readOnlySpan.Slice(0, _blockSize).CopyTo(destination.Slice(i));
				continue;
			}
			readOnlySpan.Slice(0, num2).CopyTo(destination.Slice(i));
			_startIndex = num2;
			_endIndex = _buffer.Length;
			break;
		}
	}

	[Obsolete("Rfc2898DeriveBytes.CryptDeriveKey is obsolete and is not supported. Use PasswordDeriveBytes.CryptDeriveKey instead.", DiagnosticId = "SYSLIB0033", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
	public byte[] CryptDeriveKey(string algname, string alghashname, int keySize, byte[] rgbIV)
	{
		throw new PlatformNotSupportedException();
	}

	public override void Reset()
	{
		Initialize();
	}

	private IncrementalHash OpenHmac(ReadOnlySpan<byte> password)
	{
		HashAlgorithmName hashAlgorithm = HashAlgorithm;
		if (string.IsNullOrEmpty(hashAlgorithm.Name))
		{
			throw new CryptographicException(System.SR.Cryptography_HashAlgorithmNameNullOrEmpty);
		}
		if (hashAlgorithm != HashAlgorithmName.SHA1 && hashAlgorithm != HashAlgorithmName.SHA256 && hashAlgorithm != HashAlgorithmName.SHA384 && hashAlgorithm != HashAlgorithmName.SHA512 && hashAlgorithm != HashAlgorithmName.SHA3_256 && hashAlgorithm != HashAlgorithmName.SHA3_384 && hashAlgorithm != HashAlgorithmName.SHA3_512)
		{
			throw new CryptographicException(System.SR.Format(System.SR.Cryptography_UnknownHashAlgorithm, hashAlgorithm.Name));
		}
		return IncrementalHash.CreateHMAC(hashAlgorithm, password);
	}

	[MemberNotNull("_buffer")]
	private void Initialize()
	{
		if (_buffer != null)
		{
			Array.Clear(_buffer);
		}
		_buffer = new byte[_blockSize];
		_block = 0u;
		_startIndex = (_endIndex = 0);
	}

	private void Func()
	{
		if (_block == uint.MaxValue)
		{
			throw new CryptographicException(System.SR.Cryptography_ExceedKdfExtractLimit);
		}
		BinaryPrimitives.WriteUInt32BigEndian(_salt.AsSpan(_salt.Length - 4), _block + 1);
		Span<byte> span = stackalloc byte[64];
		span = span.Slice(0, _blockSize);
		_hmac.AppendData(_salt);
		int hashAndReset = _hmac.GetHashAndReset(span);
		span.CopyTo(_buffer);
		for (int i = 2; i <= _iterations; i++)
		{
			_hmac.AppendData(span);
			hashAndReset = _hmac.GetHashAndReset(span);
			for (int num = _buffer.Length - 1; num >= 0; num--)
			{
				_buffer[num] ^= span[num];
			}
		}
		_block++;
	}

	public static byte[] Pbkdf2(byte[] password, byte[] salt, int iterations, HashAlgorithmName hashAlgorithm, int outputLength)
	{
		ArgumentNullException.ThrowIfNull(password, "password");
		ArgumentNullException.ThrowIfNull(salt, "salt");
		return Pbkdf2(new ReadOnlySpan<byte>(password), new ReadOnlySpan<byte>(salt), iterations, hashAlgorithm, outputLength);
	}

	public static byte[] Pbkdf2(ReadOnlySpan<byte> password, ReadOnlySpan<byte> salt, int iterations, HashAlgorithmName hashAlgorithm, int outputLength)
	{
		ArgumentOutOfRangeException.ThrowIfNegativeOrZero(iterations, "iterations");
		ArgumentOutOfRangeException.ThrowIfNegative(outputLength, "outputLength");
		ValidateHashAlgorithm(hashAlgorithm);
		byte[] array = new byte[outputLength];
		Pbkdf2Core(password, salt, array, iterations, hashAlgorithm);
		return array;
	}

	public static void Pbkdf2(ReadOnlySpan<byte> password, ReadOnlySpan<byte> salt, Span<byte> destination, int iterations, HashAlgorithmName hashAlgorithm)
	{
		ArgumentOutOfRangeException.ThrowIfNegativeOrZero(iterations, "iterations");
		ValidateHashAlgorithm(hashAlgorithm);
		Pbkdf2Core(password, salt, destination, iterations, hashAlgorithm);
	}

	public static byte[] Pbkdf2(string password, byte[] salt, int iterations, HashAlgorithmName hashAlgorithm, int outputLength)
	{
		ArgumentNullException.ThrowIfNull(password, "password");
		ArgumentNullException.ThrowIfNull(salt, "salt");
		return Pbkdf2(password.AsSpan(), new ReadOnlySpan<byte>(salt), iterations, hashAlgorithm, outputLength);
	}

	public static byte[] Pbkdf2(ReadOnlySpan<char> password, ReadOnlySpan<byte> salt, int iterations, HashAlgorithmName hashAlgorithm, int outputLength)
	{
		ArgumentOutOfRangeException.ThrowIfNegative(outputLength, "outputLength");
		ArgumentOutOfRangeException.ThrowIfNegativeOrZero(iterations, "iterations");
		ValidateHashAlgorithm(hashAlgorithm);
		byte[] array = new byte[outputLength];
		Pbkdf2Core(password, salt, array, iterations, hashAlgorithm);
		return array;
	}

	public static void Pbkdf2(ReadOnlySpan<char> password, ReadOnlySpan<byte> salt, Span<byte> destination, int iterations, HashAlgorithmName hashAlgorithm)
	{
		ArgumentOutOfRangeException.ThrowIfNegativeOrZero(iterations, "iterations");
		ValidateHashAlgorithm(hashAlgorithm);
		Pbkdf2Core(password, salt, destination, iterations, hashAlgorithm);
	}

	private static void Pbkdf2Core(ReadOnlySpan<char> password, ReadOnlySpan<byte> salt, Span<byte> destination, int iterations, HashAlgorithmName hashAlgorithm)
	{
		if (!destination.IsEmpty)
		{
			byte[] array = null;
			int maxByteCount = s_throwingUtf8Encoding.GetMaxByteCount(password.Length);
			Span<byte> span = ((maxByteCount <= 256) ? stackalloc byte[256] : ((Span<byte>)(array = System.Security.Cryptography.CryptoPool.Rent(maxByteCount))));
			Span<byte> bytes = span;
			Span<byte> span2 = bytes[..s_throwingUtf8Encoding.GetBytes(password, bytes)];
			try
			{
				Pbkdf2Implementation.Fill(span2, salt, iterations, hashAlgorithm, destination);
			}
			finally
			{
				CryptographicOperations.ZeroMemory(span2);
			}
			if (array != null)
			{
				System.Security.Cryptography.CryptoPool.Return(array, 0);
			}
		}
	}

	private static void Pbkdf2Core(ReadOnlySpan<byte> password, ReadOnlySpan<byte> salt, Span<byte> destination, int iterations, HashAlgorithmName hashAlgorithm)
	{
		if (!destination.IsEmpty)
		{
			Pbkdf2Implementation.Fill(password, salt, iterations, hashAlgorithm, destination);
		}
	}

	private static void ValidateHashAlgorithm(HashAlgorithmName hashAlgorithm)
	{
		string name = hashAlgorithm.Name;
		ArgumentException.ThrowIfNullOrEmpty(name, "hashAlgorithm");
		if (!(name == HashAlgorithmName.SHA1.Name) && !(name == HashAlgorithmName.SHA256.Name) && !(name == HashAlgorithmName.SHA384.Name) && !(name == HashAlgorithmName.SHA512.Name))
		{
			if (!(name == HashAlgorithmName.SHA3_256.Name) && !(name == HashAlgorithmName.SHA3_384.Name) && !(name == HashAlgorithmName.SHA3_512.Name))
			{
				throw new CryptographicException(System.SR.Format(System.SR.Cryptography_UnknownHashAlgorithm, name));
			}
			if (!HMACSHA3_256.IsSupported)
			{
				throw new PlatformNotSupportedException();
			}
		}
	}
}
