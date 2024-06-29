using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System.Security.Cryptography;

public abstract class RandomNumberGenerator : IDisposable
{
	public static RandomNumberGenerator Create()
	{
		return RandomNumberGeneratorImplementation.s_singleton;
	}

	[Obsolete("Cryptographic factory methods accepting an algorithm name are obsolete. Use the parameterless Create factory method on the algorithm type instead.", DiagnosticId = "SYSLIB0045", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
	[RequiresUnreferencedCode("The default algorithm implementations might be removed, use strong type references like 'RSA.Create()' instead.")]
	public static RandomNumberGenerator? Create(string rngName)
	{
		return (RandomNumberGenerator)CryptoConfig.CreateFromName(rngName);
	}

	public void Dispose()
	{
		Dispose(disposing: true);
		GC.SuppressFinalize(this);
	}

	protected virtual void Dispose(bool disposing)
	{
	}

	public abstract void GetBytes(byte[] data);

	public virtual void GetBytes(byte[] data, int offset, int count)
	{
		VerifyGetBytes(data, offset, count);
		if (count > 0)
		{
			if (offset == 0 && count == data.Length)
			{
				GetBytes(data);
				return;
			}
			byte[] array = new byte[count];
			GetBytes(array);
			Buffer.BlockCopy(array, 0, data, offset, count);
		}
	}

	public virtual void GetBytes(Span<byte> data)
	{
		byte[] array = ArrayPool<byte>.Shared.Rent(data.Length);
		try
		{
			GetBytes(array, 0, data.Length);
			new ReadOnlySpan<byte>(array, 0, data.Length).CopyTo(data);
		}
		finally
		{
			Array.Clear(array, 0, data.Length);
			ArrayPool<byte>.Shared.Return(array);
		}
	}

	public virtual void GetNonZeroBytes(byte[] data)
	{
		throw new NotImplementedException();
	}

	public virtual void GetNonZeroBytes(Span<byte> data)
	{
		byte[] array = ArrayPool<byte>.Shared.Rent(data.Length);
		try
		{
			GetNonZeroBytes(array);
			new ReadOnlySpan<byte>(array, 0, data.Length).CopyTo(data);
		}
		finally
		{
			Array.Clear(array, 0, data.Length);
			ArrayPool<byte>.Shared.Return(array);
		}
	}

	public static void Fill(Span<byte> data)
	{
		RandomNumberGeneratorImplementation.FillSpan(data);
	}

	public static int GetInt32(int fromInclusive, int toExclusive)
	{
		if (fromInclusive >= toExclusive)
		{
			throw new ArgumentException(System.SR.Argument_InvalidRandomRange);
		}
		uint num = (uint)(toExclusive - fromInclusive - 1);
		if (num == 0)
		{
			return fromInclusive;
		}
		uint num2 = num;
		num2 |= num2 >> 1;
		num2 |= num2 >> 2;
		num2 |= num2 >> 4;
		num2 |= num2 >> 8;
		num2 |= num2 >> 16;
		uint reference = 0u;
		Span<byte> data = MemoryMarshal.AsBytes(new Span<uint>(ref reference));
		uint num3;
		do
		{
			RandomNumberGeneratorImplementation.FillSpan(data);
			num3 = num2 & reference;
		}
		while (num3 > num);
		return (int)num3 + fromInclusive;
	}

	public static int GetInt32(int toExclusive)
	{
		ArgumentOutOfRangeException.ThrowIfNegativeOrZero(toExclusive, "toExclusive");
		return GetInt32(0, toExclusive);
	}

	public static byte[] GetBytes(int count)
	{
		ArgumentOutOfRangeException.ThrowIfNegative(count, "count");
		byte[] array = new byte[count];
		RandomNumberGeneratorImplementation.FillSpan(array);
		return array;
	}

	public static void GetItems<T>(ReadOnlySpan<T> choices, Span<T> destination)
	{
		if (choices.IsEmpty)
		{
			throw new ArgumentException(System.SR.Arg_EmptySpan, "choices");
		}
		GetItemsCore(choices, destination);
	}

	public static T[] GetItems<T>(ReadOnlySpan<T> choices, int length)
	{
		if (choices.IsEmpty)
		{
			throw new ArgumentException(System.SR.Arg_EmptySpan, "choices");
		}
		ArgumentOutOfRangeException.ThrowIfNegative(length, "length");
		T[] array = new T[length];
		GetItemsCore(choices, array);
		return array;
	}

	public unsafe static string GetString(ReadOnlySpan<char> choices, int length)
	{
		if (choices.IsEmpty)
		{
			throw new ArgumentException(System.SR.Arg_EmptySpan, "choices");
		}
		ArgumentOutOfRangeException.ThrowIfNegative(length, "length");
		return string.Create(length, (nint)(&choices), delegate(Span<char> destination, nint state)
		{
			GetItemsCore(Unsafe.Read<ReadOnlySpan<char>>((void*)state), destination);
		});
	}

	public static void GetHexString(Span<char> destination, bool lowercase = false)
	{
		if (!destination.IsEmpty)
		{
			GetHexStringCore(destination, lowercase);
		}
	}

	public static string GetHexString(int stringLength, bool lowercase = false)
	{
		if (stringLength == 0)
		{
			return string.Empty;
		}
		return string.Create(stringLength, lowercase, GetHexStringCore);
	}

	public static void Shuffle<T>(Span<T> values)
	{
		int length = values.Length;
		for (int i = 0; i < length - 1; i++)
		{
			int @int = GetInt32(i, length);
			if (i != @int)
			{
				T val = values[i];
				values[i] = values[@int];
				values[@int] = val;
			}
		}
	}

	private static void GetHexStringCore(Span<char> destination, bool lowercase)
	{
		Span<byte> span = stackalloc byte[64];
		System.HexConverter.Casing casing = (lowercase ? System.HexConverter.Casing.Lower : System.HexConverter.Casing.Upper);
		int val = (destination.Length + 1) / 2;
		Span<byte> span2 = span.Slice(0, Math.Min(64, val));
		Fill(span2);
		if (destination.Length % 2 != 0)
		{
			destination[0] = (lowercase ? System.HexConverter.ToCharLower(span2[0]) : System.HexConverter.ToCharUpper(span2[0]));
			destination = destination.Slice(1);
			span2 = span2.Slice(1);
		}
		while (!destination.IsEmpty)
		{
			val = destination.Length / 2;
			if (span2.IsEmpty)
			{
				span2 = span.Slice(0, Math.Min(64, val));
				Fill(span2);
			}
			System.HexConverter.EncodeToUtf16(span2, destination, casing);
			destination = destination.Slice(span2.Length * 2);
			span2 = default(Span<byte>);
		}
	}

	private static void GetItemsCore<T>(ReadOnlySpan<T> choices, Span<T> destination)
	{
		for (int i = 0; i < destination.Length; i++)
		{
			destination[i] = choices[GetInt32(choices.Length)];
		}
	}

	internal static void VerifyGetBytes(byte[] data, int offset, int count)
	{
		ArgumentNullException.ThrowIfNull(data, "data");
		ArgumentOutOfRangeException.ThrowIfNegative(offset, "offset");
		ArgumentOutOfRangeException.ThrowIfNegative(count, "count");
		if (count > data.Length - offset)
		{
			throw new ArgumentException(System.SR.Argument_InvalidOffLen);
		}
	}
}
