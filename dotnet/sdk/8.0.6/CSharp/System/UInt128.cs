using System.Buffers.Binary;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System;

[CLSCompliant(false)]
[Intrinsic]
public readonly struct UInt128 : IBinaryInteger<UInt128>, IBinaryNumber<UInt128>, IBitwiseOperators<UInt128, UInt128, UInt128>, INumber<UInt128>, IComparable, IComparable<UInt128>, IComparisonOperators<UInt128, UInt128, bool>, IEqualityOperators<UInt128, UInt128, bool>, IModulusOperators<UInt128, UInt128, UInt128>, INumberBase<UInt128>, IAdditionOperators<UInt128, UInt128, UInt128>, IAdditiveIdentity<UInt128, UInt128>, IDecrementOperators<UInt128>, IDivisionOperators<UInt128, UInt128, UInt128>, IEquatable<UInt128>, IIncrementOperators<UInt128>, IMultiplicativeIdentity<UInt128, UInt128>, IMultiplyOperators<UInt128, UInt128, UInt128>, ISpanFormattable, IFormattable, ISpanParsable<UInt128>, IParsable<UInt128>, ISubtractionOperators<UInt128, UInt128, UInt128>, IUnaryPlusOperators<UInt128, UInt128>, IUnaryNegationOperators<UInt128, UInt128>, IUtf8SpanFormattable, IUtf8SpanParsable<UInt128>, IShiftOperators<UInt128, int, UInt128>, IMinMaxValue<UInt128>, IUnsignedNumber<UInt128>, IBinaryIntegerParseAndFormatInfo<UInt128>
{
	private readonly ulong _lower;

	private readonly ulong _upper;

	internal ulong Lower => _lower;

	internal ulong Upper => _upper;

	static UInt128 IAdditiveIdentity<UInt128, UInt128>.AdditiveIdentity => default(UInt128);

	static UInt128 IBinaryNumber<UInt128>.AllBitsSet => new UInt128(ulong.MaxValue, ulong.MaxValue);

	public static UInt128 MinValue => new UInt128(0uL, 0uL);

	public static UInt128 MaxValue => new UInt128(ulong.MaxValue, ulong.MaxValue);

	static UInt128 IMultiplicativeIdentity<UInt128, UInt128>.MultiplicativeIdentity => One;

	public static UInt128 One => new UInt128(0uL, 1uL);

	static int INumberBase<UInt128>.Radix => 2;

	public static UInt128 Zero => default(UInt128);

	static bool IBinaryIntegerParseAndFormatInfo<UInt128>.IsSigned => false;

	static int IBinaryIntegerParseAndFormatInfo<UInt128>.MaxDigitCount => 39;

	static int IBinaryIntegerParseAndFormatInfo<UInt128>.MaxHexDigitCount => 32;

	static UInt128 IBinaryIntegerParseAndFormatInfo<UInt128>.MaxValueDiv10 => new UInt128(1844674407370955161uL, 11068046444225730969uL);

	static string IBinaryIntegerParseAndFormatInfo<UInt128>.OverflowMessage => SR.Overflow_UInt128;

	[CLSCompliant(false)]
	public UInt128(ulong upper, ulong lower)
	{
		_lower = lower;
		_upper = upper;
	}

	public int CompareTo(object? value)
	{
		if (value is UInt128 value2)
		{
			return CompareTo(value2);
		}
		if (value == null)
		{
			return 1;
		}
		throw new ArgumentException(SR.Arg_MustBeUInt128);
	}

	public int CompareTo(UInt128 value)
	{
		if (this < value)
		{
			return -1;
		}
		if (this > value)
		{
			return 1;
		}
		return 0;
	}

	public override bool Equals([NotNullWhen(true)] object? obj)
	{
		if (obj is UInt128 other)
		{
			return Equals(other);
		}
		return false;
	}

	public bool Equals(UInt128 other)
	{
		return this == other;
	}

	public override int GetHashCode()
	{
		return HashCode.Combine(_lower, _upper);
	}

	public override string ToString()
	{
		return Number.UInt128ToDecStr(this);
	}

	public string ToString(IFormatProvider? provider)
	{
		return Number.FormatUInt128(this, null, provider);
	}

	public string ToString([StringSyntax("NumericFormat")] string? format)
	{
		return Number.FormatUInt128(this, format, null);
	}

	public string ToString([StringSyntax("NumericFormat")] string? format, IFormatProvider? provider)
	{
		return Number.FormatUInt128(this, format, provider);
	}

	public bool TryFormat(Span<char> destination, out int charsWritten, [StringSyntax("NumericFormat")] ReadOnlySpan<char> format = default(ReadOnlySpan<char>), IFormatProvider? provider = null)
	{
		return Number.TryFormatUInt128(this, format, provider, destination, out charsWritten);
	}

	public bool TryFormat(Span<byte> utf8Destination, out int bytesWritten, [StringSyntax("NumericFormat")] ReadOnlySpan<char> format = default(ReadOnlySpan<char>), IFormatProvider? provider = null)
	{
		return Number.TryFormatUInt128(this, format, provider, utf8Destination, out bytesWritten);
	}

	public static UInt128 Parse(string s)
	{
		return Parse(s, NumberStyles.Integer, null);
	}

	public static UInt128 Parse(string s, NumberStyles style)
	{
		return Parse(s, style, null);
	}

	public static UInt128 Parse(string s, IFormatProvider? provider)
	{
		return Parse(s, NumberStyles.Integer, provider);
	}

	public static UInt128 Parse(string s, NumberStyles style, IFormatProvider? provider)
	{
		if (s == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.s);
		}
		return Parse(s.AsSpan(), style, provider);
	}

	public static UInt128 Parse(ReadOnlySpan<char> s, NumberStyles style = NumberStyles.Integer, IFormatProvider? provider = null)
	{
		NumberFormatInfo.ValidateParseStyleInteger(style);
		return Number.ParseBinaryInteger<char, UInt128>(s, style, NumberFormatInfo.GetInstance(provider));
	}

	public static bool TryParse([NotNullWhen(true)] string? s, out UInt128 result)
	{
		return TryParse(s, NumberStyles.Integer, null, out result);
	}

	public static bool TryParse(ReadOnlySpan<char> s, out UInt128 result)
	{
		return TryParse(s, NumberStyles.Integer, null, out result);
	}

	public static bool TryParse(ReadOnlySpan<byte> utf8Text, out UInt128 result)
	{
		return TryParse(utf8Text, NumberStyles.Integer, null, out result);
	}

	public static bool TryParse([NotNullWhen(true)] string? s, NumberStyles style, IFormatProvider? provider, out UInt128 result)
	{
		NumberFormatInfo.ValidateParseStyleInteger(style);
		if (s == null)
		{
			result = (byte)0;
			return false;
		}
		return Number.TryParseBinaryInteger<char, UInt128>(s.AsSpan(), style, NumberFormatInfo.GetInstance(provider), out result) == Number.ParsingStatus.OK;
	}

	public static bool TryParse(ReadOnlySpan<char> s, NumberStyles style, IFormatProvider? provider, out UInt128 result)
	{
		NumberFormatInfo.ValidateParseStyleInteger(style);
		return Number.TryParseBinaryInteger<char, UInt128>(s, style, NumberFormatInfo.GetInstance(provider), out result) == Number.ParsingStatus.OK;
	}

	public static explicit operator byte(UInt128 value)
	{
		return (byte)value._lower;
	}

	public static explicit operator checked byte(UInt128 value)
	{
		if (value._upper != 0L)
		{
			ThrowHelper.ThrowOverflowException();
		}
		return checked((byte)value._lower);
	}

	public static explicit operator char(UInt128 value)
	{
		return (char)value._lower;
	}

	public static explicit operator checked char(UInt128 value)
	{
		if (value._upper != 0L)
		{
			ThrowHelper.ThrowOverflowException();
		}
		return (char)checked((ushort)value._lower);
	}

	public static explicit operator decimal(UInt128 value)
	{
		ulong lower = value._lower;
		if (value._upper > uint.MaxValue)
		{
			Number.ThrowOverflowException(SR.Overflow_Decimal);
		}
		uint hi = (uint)value._upper;
		return new decimal((int)lower, (int)(lower >> 32), (int)hi, isNegative: false, 0);
	}

	public static explicit operator double(UInt128 value)
	{
		if (value._upper == 0L)
		{
			return value._lower;
		}
		if (value._upper >> 24 == 0L)
		{
			double num = BitConverter.UInt64BitsToDouble(0x4330000000000000uL | (value._lower << 12 >> 12)) - 4503599627370496.0;
			double num2 = BitConverter.UInt64BitsToDouble(0x4670000000000000uL | (ulong)(value >> 52)) - 2.028240960365167E+31;
			return num + num2;
		}
		double num3 = BitConverter.UInt64BitsToDouble(0x44B0000000000000uL | ((ulong)(value >> 12) >> 12) | (value._lower & 0xFFFFFF)) - 7.555786372591432E+22;
		double num4 = BitConverter.UInt64BitsToDouble(0x47F0000000000000uL | (ulong)(value >> 76)) - 3.402823669209385E+38;
		return num3 + num4;
	}

	public static explicit operator Half(UInt128 value)
	{
		return (Half)(double)value;
	}

	public static explicit operator short(UInt128 value)
	{
		return (short)value._lower;
	}

	public static explicit operator checked short(UInt128 value)
	{
		if (value._upper != 0L)
		{
			ThrowHelper.ThrowOverflowException();
		}
		return checked((short)value._lower);
	}

	public static explicit operator int(UInt128 value)
	{
		return (int)value._lower;
	}

	public static explicit operator checked int(UInt128 value)
	{
		if (value._upper != 0L)
		{
			ThrowHelper.ThrowOverflowException();
		}
		return checked((int)value._lower);
	}

	public static explicit operator long(UInt128 value)
	{
		return (long)value._lower;
	}

	public static explicit operator checked long(UInt128 value)
	{
		if (value._upper != 0L)
		{
			ThrowHelper.ThrowOverflowException();
		}
		return checked((long)value._lower);
	}

	[CLSCompliant(false)]
	public static explicit operator Int128(UInt128 value)
	{
		return new Int128(value._upper, value._lower);
	}

	[CLSCompliant(false)]
	public static explicit operator checked Int128(UInt128 value)
	{
		if ((long)value._upper < 0L)
		{
			ThrowHelper.ThrowOverflowException();
		}
		return new Int128(value._upper, value._lower);
	}

	public static explicit operator nint(UInt128 value)
	{
		return (nint)value._lower;
	}

	public static explicit operator checked nint(UInt128 value)
	{
		if (value._upper != 0L)
		{
			ThrowHelper.ThrowOverflowException();
		}
		return checked((nint)value._lower);
	}

	[CLSCompliant(false)]
	public static explicit operator sbyte(UInt128 value)
	{
		return (sbyte)value._lower;
	}

	[CLSCompliant(false)]
	public static explicit operator checked sbyte(UInt128 value)
	{
		if (value._upper != 0L)
		{
			ThrowHelper.ThrowOverflowException();
		}
		return checked((sbyte)value._lower);
	}

	public static explicit operator float(UInt128 value)
	{
		return (float)(double)value;
	}

	[CLSCompliant(false)]
	public static explicit operator ushort(UInt128 value)
	{
		return (ushort)value._lower;
	}

	[CLSCompliant(false)]
	public static explicit operator checked ushort(UInt128 value)
	{
		if (value._upper != 0L)
		{
			ThrowHelper.ThrowOverflowException();
		}
		return checked((ushort)value._lower);
	}

	[CLSCompliant(false)]
	public static explicit operator uint(UInt128 value)
	{
		return (uint)value._lower;
	}

	[CLSCompliant(false)]
	public static explicit operator checked uint(UInt128 value)
	{
		if (value._upper != 0L)
		{
			ThrowHelper.ThrowOverflowException();
		}
		return checked((uint)value._lower);
	}

	[CLSCompliant(false)]
	public static explicit operator ulong(UInt128 value)
	{
		return value._lower;
	}

	[CLSCompliant(false)]
	public static explicit operator checked ulong(UInt128 value)
	{
		if (value._upper != 0L)
		{
			ThrowHelper.ThrowOverflowException();
		}
		return value._lower;
	}

	[CLSCompliant(false)]
	public static explicit operator nuint(UInt128 value)
	{
		return (nuint)value._lower;
	}

	[CLSCompliant(false)]
	public static explicit operator checked nuint(UInt128 value)
	{
		if (value._upper != 0L)
		{
			ThrowHelper.ThrowOverflowException();
		}
		return checked((nuint)value._lower);
	}

	public static explicit operator UInt128(decimal value)
	{
		value = decimal.Truncate(value);
		if (value < 0.0m)
		{
			ThrowHelper.ThrowOverflowException();
		}
		return new UInt128(value.High, value.Low64);
	}

	public static explicit operator UInt128(double value)
	{
		if (double.IsNegative(value) || double.IsNaN(value))
		{
			return MinValue;
		}
		if (value >= 3.402823669209385E+38)
		{
			return MaxValue;
		}
		return ToUInt128(value);
	}

	public static explicit operator checked UInt128(double value)
	{
		if (value < 0.0 || double.IsNaN(value) || value >= 3.402823669209385E+38)
		{
			ThrowHelper.ThrowOverflowException();
		}
		return ToUInt128(value);
	}

	internal static UInt128 ToUInt128(double value)
	{
		if (value >= 1.0)
		{
			ulong num = BitConverter.DoubleToUInt64Bits(value);
			UInt128 uInt = new UInt128((num << 12 >> 1) | 0x8000000000000000uL, 0uL);
			return uInt >> 1150 - (int)(num >> 52);
		}
		return MinValue;
	}

	public static explicit operator UInt128(short value)
	{
		long num = value;
		return new UInt128((ulong)(num >> 63), (ulong)num);
	}

	public static explicit operator checked UInt128(short value)
	{
		if (value < 0)
		{
			ThrowHelper.ThrowOverflowException();
		}
		return new UInt128(0uL, (ushort)value);
	}

	public static explicit operator UInt128(int value)
	{
		long num = value;
		return new UInt128((ulong)(num >> 63), (ulong)num);
	}

	public static explicit operator checked UInt128(int value)
	{
		if (value < 0)
		{
			ThrowHelper.ThrowOverflowException();
		}
		return new UInt128(0uL, (uint)value);
	}

	public static explicit operator UInt128(long value)
	{
		return new UInt128((ulong)(value >> 63), (ulong)value);
	}

	public static explicit operator checked UInt128(long value)
	{
		if (value < 0)
		{
			ThrowHelper.ThrowOverflowException();
		}
		return new UInt128(0uL, (ulong)value);
	}

	public static explicit operator UInt128(nint value)
	{
		long num = value;
		return new UInt128((ulong)(num >> 63), (ulong)num);
	}

	public static explicit operator checked UInt128(nint value)
	{
		if (value < 0)
		{
			ThrowHelper.ThrowOverflowException();
		}
		return new UInt128(0uL, (nuint)value);
	}

	[CLSCompliant(false)]
	public static explicit operator UInt128(sbyte value)
	{
		long num = value;
		return new UInt128((ulong)(num >> 63), (ulong)num);
	}

	[CLSCompliant(false)]
	public static explicit operator checked UInt128(sbyte value)
	{
		if (value < 0)
		{
			ThrowHelper.ThrowOverflowException();
		}
		return new UInt128(0uL, (byte)value);
	}

	public static explicit operator UInt128(float value)
	{
		return (UInt128)(double)value;
	}

	public static explicit operator checked UInt128(float value)
	{
		return checked((UInt128)(double)value);
	}

	public static implicit operator UInt128(byte value)
	{
		return new UInt128(0uL, value);
	}

	public static implicit operator UInt128(char value)
	{
		return new UInt128(0uL, value);
	}

	[CLSCompliant(false)]
	public static implicit operator UInt128(ushort value)
	{
		return new UInt128(0uL, value);
	}

	[CLSCompliant(false)]
	public static implicit operator UInt128(uint value)
	{
		return new UInt128(0uL, value);
	}

	[CLSCompliant(false)]
	public static implicit operator UInt128(ulong value)
	{
		return new UInt128(0uL, value);
	}

	[CLSCompliant(false)]
	public static implicit operator UInt128(nuint value)
	{
		return new UInt128(0uL, value);
	}

	private void WriteLittleEndianUnsafe(Span<byte> destination)
	{
		ulong lower = _lower;
		ulong upper = _upper;
		if (!BitConverter.IsLittleEndian)
		{
		}
		ref byte reference = ref MemoryMarshal.GetReference(destination);
		Unsafe.WriteUnaligned(ref reference, lower);
		Unsafe.WriteUnaligned(ref Unsafe.AddByteOffset(ref reference, (nint)8), upper);
	}

	public static UInt128 operator +(UInt128 left, UInt128 right)
	{
		ulong num = left._lower + right._lower;
		ulong num2 = (ulong)((num < left._lower) ? 1 : 0);
		ulong upper = left._upper + right._upper + num2;
		return new UInt128(upper, num);
	}

	public static UInt128 operator checked +(UInt128 left, UInt128 right)
	{
		ulong num = left._lower + right._lower;
		ulong num2 = (ulong)((num < left._lower) ? 1 : 0);
		ulong upper = checked(left._upper + right._upper + num2);
		return new UInt128(upper, num);
	}

	public static (UInt128 Quotient, UInt128 Remainder) DivRem(UInt128 left, UInt128 right)
	{
		UInt128 uInt = left / right;
		return (Quotient: uInt, Remainder: left - uInt * right);
	}

	public static UInt128 LeadingZeroCount(UInt128 value)
	{
		if (value._upper == 0L)
		{
			return 64 + ulong.LeadingZeroCount(value._lower);
		}
		return ulong.LeadingZeroCount(value._upper);
	}

	public static UInt128 PopCount(UInt128 value)
	{
		return ulong.PopCount(value._lower) + ulong.PopCount(value._upper);
	}

	public static UInt128 RotateLeft(UInt128 value, int rotateAmount)
	{
		return (value << rotateAmount) | (value >>> 128 - rotateAmount);
	}

	public static UInt128 RotateRight(UInt128 value, int rotateAmount)
	{
		return (value >>> rotateAmount) | (value << 128 - rotateAmount);
	}

	public static UInt128 TrailingZeroCount(UInt128 value)
	{
		if (value._lower == 0L)
		{
			return 64 + ulong.TrailingZeroCount(value._upper);
		}
		return ulong.TrailingZeroCount(value._lower);
	}

	static bool IBinaryInteger<UInt128>.TryReadBigEndian(ReadOnlySpan<byte> source, bool isUnsigned, out UInt128 value)
	{
		UInt128 uInt = default(UInt128);
		if (source.Length != 0)
		{
			if (!isUnsigned && sbyte.IsNegative((sbyte)source[0]))
			{
				value = uInt;
				return false;
			}
			if (source.Length > 16)
			{
				if (source.Slice(0, source.Length - 16).ContainsAnyExcept<byte>(0))
				{
					value = uInt;
					return false;
				}
			}
			ref byte reference = ref MemoryMarshal.GetReference(source);
			if (source.Length >= 16)
			{
				uInt = Unsafe.ReadUnaligned<UInt128>(ref Unsafe.Add(ref reference, source.Length - 16));
				_ = BitConverter.IsLittleEndian;
				uInt = BinaryPrimitives.ReverseEndianness(uInt);
			}
			else
			{
				for (int i = 0; i < source.Length; i++)
				{
					uInt <<= 8;
					uInt |= (UInt128)Unsafe.Add(ref reference, i);
				}
			}
		}
		value = uInt;
		return true;
	}

	static bool IBinaryInteger<UInt128>.TryReadLittleEndian(ReadOnlySpan<byte> source, bool isUnsigned, out UInt128 value)
	{
		UInt128 uInt = default(UInt128);
		if (source.Length != 0)
		{
			if (!isUnsigned)
			{
				if (sbyte.IsNegative((sbyte)source[source.Length - 1]))
				{
					value = uInt;
					return false;
				}
			}
			if (source.Length > 16)
			{
				if (source.Slice(16, source.Length - 16).ContainsAnyExcept<byte>(0))
				{
					value = uInt;
					return false;
				}
			}
			ref byte reference = ref MemoryMarshal.GetReference(source);
			if (source.Length >= 16)
			{
				uInt = Unsafe.ReadUnaligned<UInt128>(ref reference);
				if (BitConverter.IsLittleEndian)
				{
					goto IL_00be;
				}
			}
			for (int i = 0; i < source.Length; i++)
			{
				UInt128 uInt2 = Unsafe.Add(ref reference, i);
				uInt2 <<= i * 8;
				uInt |= uInt2;
			}
		}
		goto IL_00be;
		IL_00be:
		value = uInt;
		return true;
	}

	int IBinaryInteger<UInt128>.GetShortestBitLength()
	{
		UInt128 value = this;
		return 128 - BitOperations.LeadingZeroCount(value);
	}

	int IBinaryInteger<UInt128>.GetByteCount()
	{
		return 16;
	}

	bool IBinaryInteger<UInt128>.TryWriteBigEndian(Span<byte> destination, out int bytesWritten)
	{
		if (destination.Length >= 16)
		{
			ulong lower = _lower;
			ulong upper = _upper;
			_ = BitConverter.IsLittleEndian;
			lower = BinaryPrimitives.ReverseEndianness(lower);
			upper = BinaryPrimitives.ReverseEndianness(upper);
			ref byte reference = ref MemoryMarshal.GetReference(destination);
			Unsafe.WriteUnaligned(ref reference, upper);
			Unsafe.WriteUnaligned(ref Unsafe.AddByteOffset(ref reference, (nint)8), lower);
			bytesWritten = 16;
			return true;
		}
		bytesWritten = 0;
		return false;
	}

	bool IBinaryInteger<UInt128>.TryWriteLittleEndian(Span<byte> destination, out int bytesWritten)
	{
		if (destination.Length >= 16)
		{
			WriteLittleEndianUnsafe(destination);
			bytesWritten = 16;
			return true;
		}
		bytesWritten = 0;
		return false;
	}

	public static bool IsPow2(UInt128 value)
	{
		return PopCount(value) == 1u;
	}

	public static UInt128 Log2(UInt128 value)
	{
		if (value._upper == 0L)
		{
			return ulong.Log2(value._lower);
		}
		return 64 + ulong.Log2(value._upper);
	}

	public static UInt128 operator &(UInt128 left, UInt128 right)
	{
		return new UInt128(left._upper & right._upper, left._lower & right._lower);
	}

	public static UInt128 operator |(UInt128 left, UInt128 right)
	{
		return new UInt128(left._upper | right._upper, left._lower | right._lower);
	}

	public static UInt128 operator ^(UInt128 left, UInt128 right)
	{
		return new UInt128(left._upper ^ right._upper, left._lower ^ right._lower);
	}

	public static UInt128 operator ~(UInt128 value)
	{
		return new UInt128(~value._upper, ~value._lower);
	}

	public static bool operator <(UInt128 left, UInt128 right)
	{
		if (left._upper >= right._upper)
		{
			if (left._upper == right._upper)
			{
				return left._lower < right._lower;
			}
			return false;
		}
		return true;
	}

	public static bool operator <=(UInt128 left, UInt128 right)
	{
		if (left._upper >= right._upper)
		{
			if (left._upper == right._upper)
			{
				return left._lower <= right._lower;
			}
			return false;
		}
		return true;
	}

	public static bool operator >(UInt128 left, UInt128 right)
	{
		if (left._upper <= right._upper)
		{
			if (left._upper == right._upper)
			{
				return left._lower > right._lower;
			}
			return false;
		}
		return true;
	}

	public static bool operator >=(UInt128 left, UInt128 right)
	{
		if (left._upper <= right._upper)
		{
			if (left._upper == right._upper)
			{
				return left._lower >= right._lower;
			}
			return false;
		}
		return true;
	}

	public static UInt128 operator --(UInt128 value)
	{
		return value - One;
	}

	public static UInt128 operator checked --(UInt128 value)
	{
		return checked(value - One);
	}

	public static UInt128 operator /(UInt128 left, UInt128 right)
	{
		if (right._upper == 0L && left._upper == 0L)
		{
			return left._lower / right._lower;
		}
		if (right >= left)
		{
			if (!(right == left))
			{
				return Zero;
			}
			return One;
		}
		return DivideSlow(left, right);
		static uint AddDivisor(Span<uint> left, ReadOnlySpan<uint> right)
		{
			ulong num = 0uL;
			for (int i = 0; i < right.Length; i++)
			{
				ref uint reference = ref left[i];
				ulong num2 = reference + num + right[i];
				reference = (uint)num2;
				num = num2 >> 32;
			}
			return (uint)num;
		}
		static bool DivideGuessTooBig(ulong q, ulong valHi, uint valLo, uint divHi, uint divLo)
		{
			ulong num5 = divHi * q;
			ulong num6 = divLo * q;
			num5 += num6 >> 32;
			num6 = (uint)num6;
			if (num5 <= valHi)
			{
				if (num5 == valHi)
				{
					return num6 > valLo;
				}
				return false;
			}
			return true;
		}
		unsafe static UInt128 DivideSlow(UInt128 quotient, UInt128 divisor)
		{
			uint* ptr = stackalloc uint[4];
			Unsafe.WriteUnaligned(ref *(byte*)ptr, (uint)quotient._lower);
			Unsafe.WriteUnaligned(ref *(byte*)(ptr + 1), (uint)(quotient._lower >> 32));
			Unsafe.WriteUnaligned(ref *(byte*)(ptr + 2), (uint)quotient._upper);
			Unsafe.WriteUnaligned(ref *(byte*)(ptr + 3), (uint)(quotient._upper >> 32));
			Span<uint> span = new Span<uint>(ptr, 4 - BitOperations.LeadingZeroCount(quotient) / 32);
			uint* ptr2 = stackalloc uint[4];
			Unsafe.WriteUnaligned(ref *(byte*)ptr2, (uint)divisor._lower);
			Unsafe.WriteUnaligned(ref *(byte*)(ptr2 + 1), (uint)(divisor._lower >> 32));
			Unsafe.WriteUnaligned(ref *(byte*)(ptr2 + 2), (uint)divisor._upper);
			Unsafe.WriteUnaligned(ref *(byte*)(ptr2 + 3), (uint)(divisor._upper >> 32));
			Span<uint> span2 = new Span<uint>(ptr2, 4 - BitOperations.LeadingZeroCount(divisor) / 32);
			Span<uint> span3 = stackalloc uint[4];
			span3.Clear();
			Span<uint> span4 = span3.Slice(0, span.Length - span2.Length + 1);
			uint num7 = span2[span2.Length - 1];
			uint num8 = ((span2.Length > 1) ? span2[span2.Length - 2] : 0u);
			int num9 = BitOperations.LeadingZeroCount(num7);
			int num10 = 32 - num9;
			if (num9 > 0)
			{
				uint num11 = ((span2.Length > 2) ? span2[span2.Length - 3] : 0u);
				num7 = (num7 << num9) | (num8 >> num10);
				num8 = (num8 << num9) | (num11 >> num10);
			}
			for (int num12 = span.Length; num12 >= span2.Length; num12--)
			{
				int num13 = num12 - span2.Length;
				uint num14 = (((uint)num12 < (uint)span.Length) ? span[num12] : 0u);
				ulong num15 = ((ulong)num14 << 32) | span[num12 - 1];
				uint num16 = ((num12 > 1) ? span[num12 - 2] : 0u);
				if (num9 > 0)
				{
					uint num17 = ((num12 > 2) ? span[num12 - 3] : 0u);
					num15 = (num15 << num9) | (num16 >> num10);
					num16 = (num16 << num9) | (num17 >> num10);
				}
				ulong num18 = num15 / num7;
				if (num18 > uint.MaxValue)
				{
					num18 = 4294967295uL;
				}
				while (DivideGuessTooBig(num18, num15, num16, num7, num8))
				{
					num18--;
				}
				if (num18 != 0)
				{
					uint num19 = SubtractDivisor(span.Slice(num13), span2, num18);
					if (num19 != num14)
					{
						num19 = AddDivisor(span.Slice(num13), span2);
						num18--;
					}
				}
				if ((uint)num13 < (uint)span4.Length)
				{
					span4[num13] = (uint)num18;
				}
				if ((uint)num12 < (uint)span.Length)
				{
					span[num12] = 0u;
				}
			}
			return new UInt128(((ulong)span3[3] << 32) | span3[2], ((ulong)span3[1] << 32) | span3[0]);
		}
		static uint SubtractDivisor(Span<uint> left, ReadOnlySpan<uint> right, ulong q)
		{
			ulong num3 = 0uL;
			for (int j = 0; j < right.Length; j++)
			{
				num3 += right[j] * q;
				uint num4 = (uint)num3;
				num3 >>= 32;
				ref uint reference2 = ref left[j];
				if (reference2 < num4)
				{
					num3++;
				}
				reference2 -= num4;
			}
			return (uint)num3;
		}
	}

	public static UInt128 operator checked /(UInt128 left, UInt128 right)
	{
		return left / right;
	}

	public static bool operator ==(UInt128 left, UInt128 right)
	{
		if (left._lower == right._lower)
		{
			return left._upper == right._upper;
		}
		return false;
	}

	public static bool operator !=(UInt128 left, UInt128 right)
	{
		if (left._lower == right._lower)
		{
			return left._upper != right._upper;
		}
		return true;
	}

	public static UInt128 operator ++(UInt128 value)
	{
		return value + One;
	}

	public static UInt128 operator checked ++(UInt128 value)
	{
		return checked(value + One);
	}

	public static UInt128 operator %(UInt128 left, UInt128 right)
	{
		UInt128 uInt = left / right;
		return left - uInt * right;
	}

	public static UInt128 operator *(UInt128 left, UInt128 right)
	{
		ulong num = Math.BigMul(left._lower, right._lower, out var low);
		num += left._upper * right._lower + left._lower * right._upper;
		return new UInt128(num, low);
	}

	public static UInt128 operator checked *(UInt128 left, UInt128 right)
	{
		UInt128 lower;
		UInt128 uInt = BigMul(left, right, out lower);
		if (uInt != 0u)
		{
			ThrowHelper.ThrowOverflowException();
		}
		return lower;
	}

	internal static UInt128 BigMul(UInt128 left, UInt128 right, out UInt128 lower)
	{
		UInt128 uInt = left._lower;
		UInt128 uInt2 = left._upper;
		UInt128 uInt3 = right._lower;
		UInt128 uInt4 = right._upper;
		UInt128 uInt5 = uInt * uInt3;
		UInt128 uInt6 = uInt2 * uInt3 + uInt5._upper;
		UInt128 uInt7 = uInt * uInt4 + uInt6._lower;
		lower = new UInt128(uInt7._lower, uInt5._lower);
		return uInt2 * uInt4 + uInt6._upper + uInt7._upper;
	}

	public static UInt128 Clamp(UInt128 value, UInt128 min, UInt128 max)
	{
		if (min > max)
		{
			Math.ThrowMinMaxException(min, max);
		}
		if (value < min)
		{
			return min;
		}
		if (value > max)
		{
			return max;
		}
		return value;
	}

	static UInt128 INumber<UInt128>.CopySign(UInt128 value, UInt128 sign)
	{
		return value;
	}

	public static UInt128 Max(UInt128 x, UInt128 y)
	{
		if (!(x >= y))
		{
			return y;
		}
		return x;
	}

	static UInt128 INumber<UInt128>.MaxNumber(UInt128 x, UInt128 y)
	{
		return Max(x, y);
	}

	public static UInt128 Min(UInt128 x, UInt128 y)
	{
		if (!(x <= y))
		{
			return y;
		}
		return x;
	}

	static UInt128 INumber<UInt128>.MinNumber(UInt128 x, UInt128 y)
	{
		return Min(x, y);
	}

	public static int Sign(UInt128 value)
	{
		return (!(value == 0u)) ? 1 : 0;
	}

	static UInt128 INumberBase<UInt128>.Abs(UInt128 value)
	{
		return value;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static UInt128 CreateChecked<TOther>(TOther value) where TOther : INumberBase<TOther>
	{
		if (typeof(TOther) == typeof(UInt128))
		{
			return (UInt128)(object)value;
		}
		if (!TryConvertFromChecked(value, out var result) && !TOther.TryConvertToChecked<UInt128>(value, out result))
		{
			ThrowHelper.ThrowNotSupportedException();
		}
		return result;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static UInt128 CreateSaturating<TOther>(TOther value) where TOther : INumberBase<TOther>
	{
		if (typeof(TOther) == typeof(UInt128))
		{
			return (UInt128)(object)value;
		}
		if (!TryConvertFromSaturating(value, out var result) && !TOther.TryConvertToSaturating<UInt128>(value, out result))
		{
			ThrowHelper.ThrowNotSupportedException();
		}
		return result;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static UInt128 CreateTruncating<TOther>(TOther value) where TOther : INumberBase<TOther>
	{
		if (typeof(TOther) == typeof(UInt128))
		{
			return (UInt128)(object)value;
		}
		if (!TryConvertFromTruncating(value, out var result) && !TOther.TryConvertToTruncating<UInt128>(value, out result))
		{
			ThrowHelper.ThrowNotSupportedException();
		}
		return result;
	}

	static bool INumberBase<UInt128>.IsCanonical(UInt128 value)
	{
		return true;
	}

	static bool INumberBase<UInt128>.IsComplexNumber(UInt128 value)
	{
		return false;
	}

	public static bool IsEvenInteger(UInt128 value)
	{
		return (value._lower & 1) == 0;
	}

	static bool INumberBase<UInt128>.IsFinite(UInt128 value)
	{
		return true;
	}

	static bool INumberBase<UInt128>.IsImaginaryNumber(UInt128 value)
	{
		return false;
	}

	static bool INumberBase<UInt128>.IsInfinity(UInt128 value)
	{
		return false;
	}

	static bool INumberBase<UInt128>.IsInteger(UInt128 value)
	{
		return true;
	}

	static bool INumberBase<UInt128>.IsNaN(UInt128 value)
	{
		return false;
	}

	static bool INumberBase<UInt128>.IsNegative(UInt128 value)
	{
		return false;
	}

	static bool INumberBase<UInt128>.IsNegativeInfinity(UInt128 value)
	{
		return false;
	}

	static bool INumberBase<UInt128>.IsNormal(UInt128 value)
	{
		return value != 0u;
	}

	public static bool IsOddInteger(UInt128 value)
	{
		return (value._lower & 1) != 0;
	}

	static bool INumberBase<UInt128>.IsPositive(UInt128 value)
	{
		return true;
	}

	static bool INumberBase<UInt128>.IsPositiveInfinity(UInt128 value)
	{
		return false;
	}

	static bool INumberBase<UInt128>.IsRealNumber(UInt128 value)
	{
		return true;
	}

	static bool INumberBase<UInt128>.IsSubnormal(UInt128 value)
	{
		return false;
	}

	static bool INumberBase<UInt128>.IsZero(UInt128 value)
	{
		return value == 0u;
	}

	static UInt128 INumberBase<UInt128>.MaxMagnitude(UInt128 x, UInt128 y)
	{
		return Max(x, y);
	}

	static UInt128 INumberBase<UInt128>.MaxMagnitudeNumber(UInt128 x, UInt128 y)
	{
		return Max(x, y);
	}

	static UInt128 INumberBase<UInt128>.MinMagnitude(UInt128 x, UInt128 y)
	{
		return Min(x, y);
	}

	static UInt128 INumberBase<UInt128>.MinMagnitudeNumber(UInt128 x, UInt128 y)
	{
		return Min(x, y);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	static bool INumberBase<UInt128>.TryConvertFromChecked<TOther>(TOther value, out UInt128 result)
	{
		return TryConvertFromChecked(value, out result);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static bool TryConvertFromChecked<TOther>(TOther value, out UInt128 result) where TOther : INumberBase<TOther>
	{
		if (typeof(TOther) == typeof(byte))
		{
			byte b = (byte)(object)value;
			result = b;
			return true;
		}
		if (typeof(TOther) == typeof(char))
		{
			char c = (char)(object)value;
			result = c;
			return true;
		}
		if (typeof(TOther) == typeof(decimal))
		{
			decimal num = (decimal)(object)value;
			result = (UInt128)num;
			return true;
		}
		if (typeof(TOther) == typeof(ushort))
		{
			ushort num2 = (ushort)(object)value;
			result = num2;
			return true;
		}
		if (typeof(TOther) == typeof(uint))
		{
			uint num3 = (uint)(object)value;
			result = num3;
			return true;
		}
		if (typeof(TOther) == typeof(ulong))
		{
			ulong num4 = (ulong)(object)value;
			result = num4;
			return true;
		}
		if (typeof(TOther) == typeof(nuint))
		{
			nuint num5 = (nuint)(object)value;
			result = num5;
			return true;
		}
		result = default(UInt128);
		return false;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	static bool INumberBase<UInt128>.TryConvertFromSaturating<TOther>(TOther value, out UInt128 result)
	{
		return TryConvertFromSaturating(value, out result);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static bool TryConvertFromSaturating<TOther>(TOther value, out UInt128 result) where TOther : INumberBase<TOther>
	{
		if (typeof(TOther) == typeof(byte))
		{
			byte b = (byte)(object)value;
			result = b;
			return true;
		}
		if (typeof(TOther) == typeof(char))
		{
			char c = (char)(object)value;
			result = c;
			return true;
		}
		if (typeof(TOther) == typeof(decimal))
		{
			decimal num = (decimal)(object)value;
			result = ((num < 0m) ? MinValue : ((UInt128)num));
			return true;
		}
		if (typeof(TOther) == typeof(ushort))
		{
			ushort num2 = (ushort)(object)value;
			result = num2;
			return true;
		}
		if (typeof(TOther) == typeof(uint))
		{
			uint num3 = (uint)(object)value;
			result = num3;
			return true;
		}
		if (typeof(TOther) == typeof(ulong))
		{
			ulong num4 = (ulong)(object)value;
			result = num4;
			return true;
		}
		if (typeof(TOther) == typeof(nuint))
		{
			nuint num5 = (nuint)(object)value;
			result = num5;
			return true;
		}
		result = default(UInt128);
		return false;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	static bool INumberBase<UInt128>.TryConvertFromTruncating<TOther>(TOther value, out UInt128 result)
	{
		return TryConvertFromTruncating(value, out result);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static bool TryConvertFromTruncating<TOther>(TOther value, out UInt128 result) where TOther : INumberBase<TOther>
	{
		if (typeof(TOther) == typeof(byte))
		{
			byte b = (byte)(object)value;
			result = b;
			return true;
		}
		if (typeof(TOther) == typeof(char))
		{
			char c = (char)(object)value;
			result = c;
			return true;
		}
		if (typeof(TOther) == typeof(decimal))
		{
			decimal num = (decimal)(object)value;
			result = ((num < 0m) ? MinValue : ((UInt128)num));
			return true;
		}
		if (typeof(TOther) == typeof(ushort))
		{
			ushort num2 = (ushort)(object)value;
			result = num2;
			return true;
		}
		if (typeof(TOther) == typeof(uint))
		{
			uint num3 = (uint)(object)value;
			result = num3;
			return true;
		}
		if (typeof(TOther) == typeof(ulong))
		{
			ulong num4 = (ulong)(object)value;
			result = num4;
			return true;
		}
		if (typeof(TOther) == typeof(nuint))
		{
			nuint num5 = (nuint)(object)value;
			result = num5;
			return true;
		}
		result = default(UInt128);
		return false;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	static bool INumberBase<UInt128>.TryConvertToChecked<TOther>(UInt128 value, [MaybeNullWhen(false)] out TOther result)
	{
		if (typeof(TOther) == typeof(double))
		{
			double num = (double)value;
			result = (TOther)(object)num;
			return true;
		}
		if (typeof(TOther) == typeof(Half))
		{
			Half half = (Half)value;
			result = (TOther)(object)half;
			return true;
		}
		checked
		{
			if (typeof(TOther) == typeof(short))
			{
				short num2 = (short)value;
				result = (TOther)(object)num2;
				return true;
			}
			if (typeof(TOther) == typeof(int))
			{
				int num3 = (int)value;
				result = (TOther)(object)num3;
				return true;
			}
			if (typeof(TOther) == typeof(long))
			{
				long num4 = (long)value;
				result = (TOther)(object)num4;
				return true;
			}
			if (typeof(TOther) == typeof(Int128))
			{
				Int128 @int = (Int128)value;
				result = (TOther)(object)@int;
				return true;
			}
			if (typeof(TOther) == typeof(nint))
			{
				nint num5 = (nint)value;
				result = (TOther)(object)num5;
				return true;
			}
			if (typeof(TOther) == typeof(sbyte))
			{
				sbyte b = (sbyte)value;
				result = (TOther)(object)b;
				return true;
			}
		}
		if (typeof(TOther) == typeof(float))
		{
			float num6 = (float)value;
			result = (TOther)(object)num6;
			return true;
		}
		result = default(TOther);
		return false;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	static bool INumberBase<UInt128>.TryConvertToSaturating<TOther>(UInt128 value, [MaybeNullWhen(false)] out TOther result)
	{
		if (typeof(TOther) == typeof(double))
		{
			double num = (double)value;
			result = (TOther)(object)num;
			return true;
		}
		if (typeof(TOther) == typeof(Half))
		{
			Half half = (Half)value;
			result = (TOther)(object)half;
			return true;
		}
		if (typeof(TOther) == typeof(short))
		{
			short num2 = ((value >= new UInt128(0uL, 32767uL)) ? short.MaxValue : ((short)value));
			result = (TOther)(object)num2;
			return true;
		}
		if (typeof(TOther) == typeof(int))
		{
			int num3 = ((value >= new UInt128(0uL, 2147483647uL)) ? int.MaxValue : ((int)value));
			result = (TOther)(object)num3;
			return true;
		}
		if (typeof(TOther) == typeof(long))
		{
			long num4 = ((value >= new UInt128(0uL, 9223372036854775807uL)) ? long.MaxValue : ((long)value));
			result = (TOther)(object)num4;
			return true;
		}
		if (typeof(TOther) == typeof(Int128))
		{
			Int128 @int = ((value >= new UInt128(9223372036854775807uL, ulong.MaxValue)) ? Int128.MaxValue : ((Int128)value));
			result = (TOther)(object)@int;
			return true;
		}
		if (typeof(TOther) == typeof(nint))
		{
			nint num5 = ((value >= new UInt128(0uL, 9223372036854775807uL)) ? IntPtr.MaxValue : ((nint)value));
			result = (TOther)(object)num5;
			return true;
		}
		if (typeof(TOther) == typeof(sbyte))
		{
			sbyte b = ((value >= new UInt128(0uL, 127uL)) ? sbyte.MaxValue : ((sbyte)value));
			result = (TOther)(object)b;
			return true;
		}
		if (typeof(TOther) == typeof(float))
		{
			float num6 = (float)value;
			result = (TOther)(object)num6;
			return true;
		}
		result = default(TOther);
		return false;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	static bool INumberBase<UInt128>.TryConvertToTruncating<TOther>(UInt128 value, [MaybeNullWhen(false)] out TOther result)
	{
		if (typeof(TOther) == typeof(double))
		{
			double num = (double)value;
			result = (TOther)(object)num;
			return true;
		}
		if (typeof(TOther) == typeof(Half))
		{
			Half half = (Half)value;
			result = (TOther)(object)half;
			return true;
		}
		if (typeof(TOther) == typeof(short))
		{
			short num2 = (short)value;
			result = (TOther)(object)num2;
			return true;
		}
		if (typeof(TOther) == typeof(int))
		{
			int num3 = (int)value;
			result = (TOther)(object)num3;
			return true;
		}
		if (typeof(TOther) == typeof(long))
		{
			long num4 = (long)value;
			result = (TOther)(object)num4;
			return true;
		}
		if (typeof(TOther) == typeof(Int128))
		{
			Int128 @int = (Int128)value;
			result = (TOther)(object)@int;
			return true;
		}
		if (typeof(TOther) == typeof(nint))
		{
			nint num5 = (nint)value;
			result = (TOther)(object)num5;
			return true;
		}
		if (typeof(TOther) == typeof(sbyte))
		{
			sbyte b = (sbyte)value;
			result = (TOther)(object)b;
			return true;
		}
		if (typeof(TOther) == typeof(float))
		{
			float num6 = (float)value;
			result = (TOther)(object)num6;
			return true;
		}
		result = default(TOther);
		return false;
	}

	public static bool TryParse([NotNullWhen(true)] string? s, IFormatProvider? provider, out UInt128 result)
	{
		return TryParse(s, NumberStyles.Integer, provider, out result);
	}

	public static UInt128 operator <<(UInt128 value, int shiftAmount)
	{
		shiftAmount &= 0x7F;
		if (((uint)shiftAmount & 0x40u) != 0)
		{
			ulong upper = value._lower << shiftAmount;
			return new UInt128(upper, 0uL);
		}
		if (shiftAmount != 0)
		{
			ulong lower = value._lower << shiftAmount;
			ulong upper2 = (value._upper << shiftAmount) | (value._lower >> 64 - shiftAmount);
			return new UInt128(upper2, lower);
		}
		return value;
	}

	public static UInt128 operator >>(UInt128 value, int shiftAmount)
	{
		return value >>> shiftAmount;
	}

	public static UInt128 operator >>>(UInt128 value, int shiftAmount)
	{
		shiftAmount &= 0x7F;
		if (((uint)shiftAmount & 0x40u) != 0)
		{
			ulong lower = value._upper >> shiftAmount;
			return new UInt128(0uL, lower);
		}
		if (shiftAmount != 0)
		{
			ulong lower2 = (value._lower >> shiftAmount) | (value._upper << 64 - shiftAmount);
			ulong upper = value._upper >> shiftAmount;
			return new UInt128(upper, lower2);
		}
		return value;
	}

	public static UInt128 Parse(ReadOnlySpan<char> s, IFormatProvider? provider)
	{
		return Parse(s, NumberStyles.Integer, provider);
	}

	public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, out UInt128 result)
	{
		return TryParse(s, NumberStyles.Integer, provider, out result);
	}

	public static UInt128 operator -(UInt128 left, UInt128 right)
	{
		ulong num = left._lower - right._lower;
		ulong num2 = (ulong)((num > left._lower) ? 1 : 0);
		ulong upper = left._upper - right._upper - num2;
		return new UInt128(upper, num);
	}

	public static UInt128 operator checked -(UInt128 left, UInt128 right)
	{
		ulong num = left._lower - right._lower;
		ulong num2 = (ulong)((num > left._lower) ? 1 : 0);
		ulong upper = checked(left._upper - right._upper - num2);
		return new UInt128(upper, num);
	}

	public static UInt128 operator -(UInt128 value)
	{
		return Zero - value;
	}

	public static UInt128 operator checked -(UInt128 value)
	{
		return checked(Zero - value);
	}

	public static UInt128 operator +(UInt128 value)
	{
		return value;
	}

	public static UInt128 Parse(ReadOnlySpan<byte> utf8Text, NumberStyles style = NumberStyles.Integer, IFormatProvider? provider = null)
	{
		NumberFormatInfo.ValidateParseStyleInteger(style);
		return Number.ParseBinaryInteger<byte, UInt128>(utf8Text, style, NumberFormatInfo.GetInstance(provider));
	}

	public static bool TryParse(ReadOnlySpan<byte> utf8Text, NumberStyles style, IFormatProvider? provider, out UInt128 result)
	{
		NumberFormatInfo.ValidateParseStyleInteger(style);
		return Number.TryParseBinaryInteger<byte, UInt128>(utf8Text, style, NumberFormatInfo.GetInstance(provider), out result) == Number.ParsingStatus.OK;
	}

	public static UInt128 Parse(ReadOnlySpan<byte> utf8Text, IFormatProvider? provider)
	{
		return Parse(utf8Text, NumberStyles.Integer, provider);
	}

	public static bool TryParse(ReadOnlySpan<byte> utf8Text, IFormatProvider? provider, out UInt128 result)
	{
		return TryParse(utf8Text, NumberStyles.Integer, provider, out result);
	}

	static bool IBinaryIntegerParseAndFormatInfo<UInt128>.IsGreaterThanAsUnsigned(UInt128 left, UInt128 right)
	{
		return left > right;
	}

	static UInt128 IBinaryIntegerParseAndFormatInfo<UInt128>.MultiplyBy10(UInt128 value)
	{
		return value * (byte)10;
	}

	static UInt128 IBinaryIntegerParseAndFormatInfo<UInt128>.MultiplyBy16(UInt128 value)
	{
		return value * (byte)16;
	}
}
