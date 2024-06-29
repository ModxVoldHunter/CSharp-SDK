using System.Globalization;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System.Buffers.Text;

internal static class FormattingHelpers
{
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static int CountDigits(UInt128 value)
	{
		ulong upper = value.Upper;
		if (upper == 0L)
		{
			return CountDigits(value.Lower);
		}
		int num = 20;
		switch (upper)
		{
		default:
			value /= new UInt128(5uL, 7766279631452241920uL);
			num += CountDigits(value.Lower);
			break;
		case 5uL:
			if (value.Lower >= 7766279631452241920L)
			{
				num++;
			}
			break;
		case 0uL:
		case 1uL:
		case 2uL:
		case 3uL:
		case 4uL:
			break;
		}
		return num;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static int CountDigits(ulong value)
	{
		ReadOnlySpan<byte> span = new byte[64]
		{
			1, 1, 1, 2, 2, 2, 3, 3, 3, 4,
			4, 4, 4, 5, 5, 5, 6, 6, 6, 7,
			7, 7, 7, 8, 8, 8, 9, 9, 9, 10,
			10, 10, 10, 11, 11, 11, 12, 12, 12, 13,
			13, 13, 13, 14, 14, 14, 15, 15, 15, 16,
			16, 16, 16, 17, 17, 17, 18, 18, 18, 19,
			19, 19, 19, 20
		};
		uint num = Unsafe.Add(ref MemoryMarshal.GetReference(span), BitOperations.Log2(value));
		ReadOnlySpan<ulong> span2 = RuntimeHelpers.CreateSpan<ulong>((RuntimeFieldHandle)/*OpCode not supported: LdMemberToken*/);
		ulong num2 = Unsafe.Add(ref MemoryMarshal.GetReference(span2), num);
		bool source = value < num2;
		return (int)(num - Unsafe.As<bool, byte>(ref source));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static int CountDigits(uint value)
	{
		ReadOnlySpan<long> span = RuntimeHelpers.CreateSpan<long>((RuntimeFieldHandle)/*OpCode not supported: LdMemberToken*/);
		long num = Unsafe.Add(ref MemoryMarshal.GetReference(span), uint.Log2(value));
		return (int)(value + num >> 32);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static int CountHexDigits(UInt128 value)
	{
		return ((int)UInt128.Log2(value) >> 2) + 1;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static int CountHexDigits(ulong value)
	{
		return (BitOperations.Log2(value) >> 2) + 1;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static int CountDecimalTrailingZeros(uint value, out uint valueWithoutTrailingZeros)
	{
		int num = 0;
		if (value != 0)
		{
			while (true)
			{
				uint num2 = value / 10;
				if (value != num2 * 10)
				{
					break;
				}
				value = num2;
				num++;
			}
		}
		valueWithoutTrailingZeros = value;
		return num;
	}

	public static bool TryFormat<T>(T value, Span<byte> utf8Destination, out int bytesWritten, StandardFormat format) where T : IUtf8SpanFormattable
	{
		Span<char> span = default(Span<char>);
		if (!format.IsDefault)
		{
			Span<char> destination = stackalloc char[3];
			span = format.Format(destination);
		}
		ref T reference = ref value;
		T val = default(T);
		if (val == null)
		{
			val = reference;
			reference = ref val;
		}
		return reference.TryFormat(utf8Destination, out bytesWritten, span, CultureInfo.InvariantCulture);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static char GetSymbolOrDefault(in StandardFormat format, char defaultSymbol)
	{
		char c = format.Symbol;
		if (c == '\0' && format.Precision == 0)
		{
			c = defaultSymbol;
		}
		return c;
	}
}
