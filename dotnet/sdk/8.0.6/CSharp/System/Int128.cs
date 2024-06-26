using System.Buffers.Binary;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System;

[Intrinsic]
public readonly struct Int128 : IBinaryInteger<Int128>, IBinaryNumber<Int128>, IBitwiseOperators<Int128, Int128, Int128>, INumber<Int128>, IComparable, IComparable<Int128>, IComparisonOperators<Int128, Int128, bool>, IEqualityOperators<Int128, Int128, bool>, IModulusOperators<Int128, Int128, Int128>, INumberBase<Int128>, IAdditionOperators<Int128, Int128, Int128>, IAdditiveIdentity<Int128, Int128>, IDecrementOperators<Int128>, IDivisionOperators<Int128, Int128, Int128>, IEquatable<Int128>, IIncrementOperators<Int128>, IMultiplicativeIdentity<Int128, Int128>, IMultiplyOperators<Int128, Int128, Int128>, ISpanFormattable, IFormattable, ISpanParsable<Int128>, IParsable<Int128>, ISubtractionOperators<Int128, Int128, Int128>, IUnaryPlusOperators<Int128, Int128>, IUnaryNegationOperators<Int128, Int128>, IUtf8SpanFormattable, IUtf8SpanParsable<Int128>, IShiftOperators<Int128, int, Int128>, IMinMaxValue<Int128>, ISignedNumber<Int128>, IBinaryIntegerParseAndFormatInfo<Int128>
{
	private readonly ulong _lower;

	private readonly ulong _upper;

	internal ulong Lower => _lower;

	internal ulong Upper => _upper;

	static Int128 IAdditiveIdentity<Int128, Int128>.AdditiveIdentity => default(Int128);

	static Int128 IBinaryNumber<Int128>.AllBitsSet => new Int128(ulong.MaxValue, ulong.MaxValue);

	public static Int128 MinValue => new Int128(9223372036854775808uL, 0uL);

	public static Int128 MaxValue => new Int128(9223372036854775807uL, ulong.MaxValue);

	static Int128 IMultiplicativeIdentity<Int128, Int128>.MultiplicativeIdentity => One;

	public static Int128 One => new Int128(0uL, 1uL);

	static int INumberBase<Int128>.Radix => 2;

	public static Int128 Zero => default(Int128);

	public static Int128 NegativeOne => new Int128(ulong.MaxValue, ulong.MaxValue);

	static bool IBinaryIntegerParseAndFormatInfo<Int128>.IsSigned => true;

	static int IBinaryIntegerParseAndFormatInfo<Int128>.MaxDigitCount => 39;

	static int IBinaryIntegerParseAndFormatInfo<Int128>.MaxHexDigitCount => 32;

	static Int128 IBinaryIntegerParseAndFormatInfo<Int128>.MaxValueDiv10 => new Int128(922337203685477580uL, 14757395258967641292uL);

	static string IBinaryIntegerParseAndFormatInfo<Int128>.OverflowMessage => SR.Overflow_Int128;

	[CLSCompliant(false)]
	public Int128(ulong upper, ulong lower)
	{
		_lower = lower;
		_upper = upper;
	}

	public int CompareTo(object? value)
	{
		if (value is Int128 value2)
		{
			return CompareTo(value2);
		}
		if (value == null)
		{
			return 1;
		}
		throw new ArgumentException(SR.Arg_MustBeInt128);
	}

	public int CompareTo(Int128 value)
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
		if (obj is Int128 other)
		{
			return Equals(other);
		}
		return false;
	}

	public bool Equals(Int128 other)
	{
		return this == other;
	}

	public override int GetHashCode()
	{
		return HashCode.Combine(_lower, _upper);
	}

	public override string ToString()
	{
		return Number.Int128ToDecStr(this);
	}

	public string ToString(IFormatProvider? provider)
	{
		return Number.FormatInt128(this, null, provider);
	}

	public string ToString([StringSyntax("NumericFormat")] string? format)
	{
		return Number.FormatInt128(this, format, null);
	}

	public string ToString([StringSyntax("NumericFormat")] string? format, IFormatProvider? provider)
	{
		return Number.FormatInt128(this, format, provider);
	}

	public bool TryFormat(Span<char> destination, out int charsWritten, [StringSyntax("NumericFormat")] ReadOnlySpan<char> format = default(ReadOnlySpan<char>), IFormatProvider? provider = null)
	{
		return Number.TryFormatInt128(this, format, provider, destination, out charsWritten);
	}

	public bool TryFormat(Span<byte> utf8Destination, out int bytesWritten, [StringSyntax("NumericFormat")] ReadOnlySpan<char> format = default(ReadOnlySpan<char>), IFormatProvider? provider = null)
	{
		return Number.TryFormatInt128(this, format, provider, utf8Destination, out bytesWritten);
	}

	public static Int128 Parse(string s)
	{
		return Parse(s, NumberStyles.Integer, null);
	}

	public static Int128 Parse(string s, NumberStyles style)
	{
		return Parse(s, style, null);
	}

	public static Int128 Parse(string s, IFormatProvider? provider)
	{
		return Parse(s, NumberStyles.Integer, provider);
	}

	public static Int128 Parse(string s, NumberStyles style, IFormatProvider? provider)
	{
		if (s == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.s);
		}
		return Parse(s.AsSpan(), style, provider);
	}

	public static Int128 Parse(ReadOnlySpan<char> s, NumberStyles style = NumberStyles.Integer, IFormatProvider? provider = null)
	{
		NumberFormatInfo.ValidateParseStyleInteger(style);
		return Number.ParseBinaryInteger<char, Int128>(s, style, NumberFormatInfo.GetInstance(provider));
	}

	public static bool TryParse([NotNullWhen(true)] string? s, out Int128 result)
	{
		return TryParse(s, NumberStyles.Integer, null, out result);
	}

	public static bool TryParse(ReadOnlySpan<char> s, out Int128 result)
	{
		return TryParse(s, NumberStyles.Integer, null, out result);
	}

	public static bool TryParse(ReadOnlySpan<byte> utf8Text, out Int128 result)
	{
		return TryParse(utf8Text, NumberStyles.Integer, null, out result);
	}

	public static bool TryParse([NotNullWhen(true)] string? s, NumberStyles style, IFormatProvider? provider, out Int128 result)
	{
		NumberFormatInfo.ValidateParseStyleInteger(style);
		if (s == null)
		{
			result = 0;
			return false;
		}
		return Number.TryParseBinaryInteger<char, Int128>(s.AsSpan(), style, NumberFormatInfo.GetInstance(provider), out result) == Number.ParsingStatus.OK;
	}

	public static bool TryParse(ReadOnlySpan<char> s, NumberStyles style, IFormatProvider? provider, out Int128 result)
	{
		NumberFormatInfo.ValidateParseStyleInteger(style);
		return Number.TryParseBinaryInteger<char, Int128>(s, style, NumberFormatInfo.GetInstance(provider), out result) == Number.ParsingStatus.OK;
	}

	public static explicit operator byte(Int128 value)
	{
		return (byte)value._lower;
	}

	public static explicit operator checked byte(Int128 value)
	{
		if (value._upper != 0L)
		{
			ThrowHelper.ThrowOverflowException();
		}
		return checked((byte)value._lower);
	}

	public static explicit operator char(Int128 value)
	{
		return (char)value._lower;
	}

	public static explicit operator checked char(Int128 value)
	{
		if (value._upper != 0L)
		{
			ThrowHelper.ThrowOverflowException();
		}
		return (char)checked((ushort)value._lower);
	}

	public static explicit operator decimal(Int128 value)
	{
		if (IsNegative(value))
		{
			value = -value;
			return -(decimal)(UInt128)value;
		}
		return (decimal)(UInt128)value;
	}

	public static explicit operator double(Int128 value)
	{
		if (IsNegative(value))
		{
			value = -value;
			return 0.0 - (double)(UInt128)value;
		}
		return (double)(UInt128)value;
	}

	public static explicit operator Half(Int128 value)
	{
		if (IsNegative(value))
		{
			value = -value;
			return -(Half)(UInt128)value;
		}
		return (Half)(UInt128)value;
	}

	public static explicit operator short(Int128 value)
	{
		return (short)value._lower;
	}

	public static explicit operator checked short(Int128 value)
	{
		if (~value._upper == 0L)
		{
			long lower = (long)value._lower;
			return checked((short)lower);
		}
		if (value._upper != 0L)
		{
			ThrowHelper.ThrowOverflowException();
		}
		return checked((short)value._lower);
	}

	public static explicit operator int(Int128 value)
	{
		return (int)value._lower;
	}

	public static explicit operator checked int(Int128 value)
	{
		if (~value._upper == 0L)
		{
			long lower = (long)value._lower;
			return checked((int)lower);
		}
		if (value._upper != 0L)
		{
			ThrowHelper.ThrowOverflowException();
		}
		return checked((int)value._lower);
	}

	public static explicit operator long(Int128 value)
	{
		return (long)value._lower;
	}

	public static explicit operator checked long(Int128 value)
	{
		if (~value._upper == 0L)
		{
			return (long)value._lower;
		}
		if (value._upper != 0L)
		{
			ThrowHelper.ThrowOverflowException();
		}
		return checked((long)value._lower);
	}

	public static explicit operator nint(Int128 value)
	{
		return (nint)value._lower;
	}

	public static explicit operator checked nint(Int128 value)
	{
		if (~value._upper == 0L)
		{
			long lower = (long)value._lower;
			return checked((nint)lower);
		}
		if (value._upper != 0L)
		{
			ThrowHelper.ThrowOverflowException();
		}
		return checked((nint)value._lower);
	}

	[CLSCompliant(false)]
	public static explicit operator sbyte(Int128 value)
	{
		return (sbyte)value._lower;
	}

	[CLSCompliant(false)]
	public static explicit operator checked sbyte(Int128 value)
	{
		if (~value._upper == 0L)
		{
			long lower = (long)value._lower;
			return checked((sbyte)lower);
		}
		if (value._upper != 0L)
		{
			ThrowHelper.ThrowOverflowException();
		}
		return checked((sbyte)value._lower);
	}

	public static explicit operator float(Int128 value)
	{
		if (IsNegative(value))
		{
			value = -value;
			return 0f - (float)(UInt128)value;
		}
		return (float)(UInt128)value;
	}

	[CLSCompliant(false)]
	public static explicit operator ushort(Int128 value)
	{
		return (ushort)value._lower;
	}

	[CLSCompliant(false)]
	public static explicit operator checked ushort(Int128 value)
	{
		if (value._upper != 0L)
		{
			ThrowHelper.ThrowOverflowException();
		}
		return checked((ushort)value._lower);
	}

	[CLSCompliant(false)]
	public static explicit operator uint(Int128 value)
	{
		return (uint)value._lower;
	}

	[CLSCompliant(false)]
	public static explicit operator checked uint(Int128 value)
	{
		if (value._upper != 0L)
		{
			ThrowHelper.ThrowOverflowException();
		}
		return checked((uint)value._lower);
	}

	[CLSCompliant(false)]
	public static explicit operator ulong(Int128 value)
	{
		return value._lower;
	}

	[CLSCompliant(false)]
	public static explicit operator checked ulong(Int128 value)
	{
		if (value._upper != 0L)
		{
			ThrowHelper.ThrowOverflowException();
		}
		return value._lower;
	}

	[CLSCompliant(false)]
	public static explicit operator UInt128(Int128 value)
	{
		return new UInt128(value._upper, value._lower);
	}

	[CLSCompliant(false)]
	public static explicit operator checked UInt128(Int128 value)
	{
		if ((long)value._upper < 0L)
		{
			ThrowHelper.ThrowOverflowException();
		}
		return new UInt128(value._upper, value._lower);
	}

	[CLSCompliant(false)]
	public static explicit operator nuint(Int128 value)
	{
		return (nuint)value._lower;
	}

	[CLSCompliant(false)]
	public static explicit operator checked nuint(Int128 value)
	{
		if (value._upper != 0L)
		{
			ThrowHelper.ThrowOverflowException();
		}
		return checked((nuint)value._lower);
	}

	public static explicit operator Int128(decimal value)
	{
		value = decimal.Truncate(value);
		Int128 @int = new Int128(value.High, value.Low64);
		if (decimal.IsNegative(value))
		{
			return -@int;
		}
		return @int;
	}

	public static explicit operator Int128(double value)
	{
		if (value <= -1.7014118346046923E+38)
		{
			return MinValue;
		}
		if (double.IsNaN(value))
		{
			return 0;
		}
		if (value >= 1.7014118346046923E+38)
		{
			return MaxValue;
		}
		return ToInt128(value);
	}

	public static explicit operator checked Int128(double value)
	{
		if (value < -1.7014118346046923E+38 || double.IsNaN(value) || value >= 1.7014118346046923E+38)
		{
			ThrowHelper.ThrowOverflowException();
		}
		return ToInt128(value);
	}

	internal static Int128 ToInt128(double value)
	{
		bool flag = double.IsNegative(value);
		if (flag)
		{
			value = 0.0 - value;
		}
		if (value >= 1.0)
		{
			ulong num = BitConverter.DoubleToUInt64Bits(value);
			Int128 @int = new Int128((num << 12 >> 1) | 0x8000000000000000uL, 0uL);
			@int >>>= 1150 - (int)(num >> 52);
			if (flag)
			{
				@int = -@int;
			}
			return @int;
		}
		return 0;
	}

	public static explicit operator Int128(float value)
	{
		return (Int128)(double)value;
	}

	public static explicit operator checked Int128(float value)
	{
		return checked((Int128)(double)value);
	}

	public static implicit operator Int128(byte value)
	{
		return new Int128(0uL, value);
	}

	public static implicit operator Int128(char value)
	{
		return new Int128(0uL, value);
	}

	public static implicit operator Int128(short value)
	{
		long num = value;
		return new Int128((ulong)(num >> 63), (ulong)num);
	}

	public static implicit operator Int128(int value)
	{
		long num = value;
		return new Int128((ulong)(num >> 63), (ulong)num);
	}

	public static implicit operator Int128(long value)
	{
		return new Int128((ulong)(value >> 63), (ulong)value);
	}

	public static implicit operator Int128(nint value)
	{
		long num = value;
		return new Int128((ulong)(num >> 63), (ulong)num);
	}

	[CLSCompliant(false)]
	public static implicit operator Int128(sbyte value)
	{
		long num = value;
		return new Int128((ulong)(num >> 63), (ulong)num);
	}

	[CLSCompliant(false)]
	public static implicit operator Int128(ushort value)
	{
		return new Int128(0uL, value);
	}

	[CLSCompliant(false)]
	public static implicit operator Int128(uint value)
	{
		return new Int128(0uL, value);
	}

	[CLSCompliant(false)]
	public static implicit operator Int128(ulong value)
	{
		return new Int128(0uL, value);
	}

	[CLSCompliant(false)]
	public static implicit operator Int128(nuint value)
	{
		return new Int128(0uL, value);
	}

	public static Int128 operator +(Int128 left, Int128 right)
	{
		ulong num = left._lower + right._lower;
		ulong num2 = (ulong)((num < left._lower) ? 1 : 0);
		ulong upper = left._upper + right._upper + num2;
		return new Int128(upper, num);
	}

	public static Int128 operator checked +(Int128 left, Int128 right)
	{
		Int128 result = left + right;
		uint num = (uint)(left._upper >> 63);
		if (num == (uint)(right._upper >> 63) && num != (uint)(result._upper >> 63))
		{
			ThrowHelper.ThrowOverflowException();
		}
		return result;
	}

	public static (Int128 Quotient, Int128 Remainder) DivRem(Int128 left, Int128 right)
	{
		Int128 @int = left / right;
		return (Quotient: @int, Remainder: left - @int * right);
	}

	public static Int128 LeadingZeroCount(Int128 value)
	{
		if (value._upper == 0L)
		{
			return 64 + ulong.LeadingZeroCount(value._lower);
		}
		return ulong.LeadingZeroCount(value._upper);
	}

	public static Int128 PopCount(Int128 value)
	{
		return ulong.PopCount(value._lower) + ulong.PopCount(value._upper);
	}

	public static Int128 RotateLeft(Int128 value, int rotateAmount)
	{
		return (value << rotateAmount) | (value >>> 128 - rotateAmount);
	}

	public static Int128 RotateRight(Int128 value, int rotateAmount)
	{
		return (value >>> rotateAmount) | (value << 128 - rotateAmount);
	}

	public static Int128 TrailingZeroCount(Int128 value)
	{
		if (value._lower == 0L)
		{
			return 64 + ulong.TrailingZeroCount(value._upper);
		}
		return ulong.TrailingZeroCount(value._lower);
	}

	static bool IBinaryInteger<Int128>.TryReadBigEndian(ReadOnlySpan<byte> source, bool isUnsigned, out Int128 value)
	{
		Int128 @int = default(Int128);
		if (source.Length != 0)
		{
			sbyte b = (sbyte)source[0];
			b >>= 31;
			isUnsigned = isUnsigned || b == 0;
			if (isUnsigned && sbyte.IsNegative(b) && source.Length >= 16)
			{
				value = @int;
				return false;
			}
			if (source.Length > 16)
			{
				if (source.Slice(0, source.Length - 16).ContainsAnyExcept((byte)b))
				{
					value = @int;
					return false;
				}
				if (isUnsigned == sbyte.IsNegative((sbyte)source[source.Length - 16]))
				{
					value = @int;
					return false;
				}
			}
			ref byte reference = ref MemoryMarshal.GetReference(source);
			if (source.Length >= 16)
			{
				@int = Unsafe.ReadUnaligned<Int128>(ref Unsafe.Add(ref reference, source.Length - 16));
				_ = BitConverter.IsLittleEndian;
				@int = BinaryPrimitives.ReverseEndianness(@int);
			}
			else
			{
				for (int i = 0; i < source.Length; i++)
				{
					@int <<= 8;
					@int |= (Int128)Unsafe.Add(ref reference, i);
				}
				if (!isUnsigned)
				{
					@int |= One << 127 >> (16 - source.Length) * 8 - 1;
				}
			}
		}
		value = @int;
		return true;
	}

	static bool IBinaryInteger<Int128>.TryReadLittleEndian(ReadOnlySpan<byte> source, bool isUnsigned, out Int128 value)
	{
		Int128 @int = default(Int128);
		if (source.Length != 0)
		{
			sbyte b = (sbyte)source[source.Length - 1];
			b >>= 31;
			isUnsigned = isUnsigned || b == 0;
			if (isUnsigned && sbyte.IsNegative(b) && source.Length >= 16)
			{
				value = @int;
				return false;
			}
			if (source.Length > 16)
			{
				if (source.Slice(16, source.Length - 16).ContainsAnyExcept((byte)b))
				{
					value = @int;
					return false;
				}
				if (isUnsigned == sbyte.IsNegative((sbyte)source[15]))
				{
					value = @int;
					return false;
				}
			}
			ref byte reference = ref MemoryMarshal.GetReference(source);
			if (source.Length >= 16)
			{
				@int = Unsafe.ReadUnaligned<Int128>(ref reference);
				if (BitConverter.IsLittleEndian)
				{
					goto IL_0136;
				}
			}
			for (int i = 0; i < source.Length; i++)
			{
				@int <<= 8;
				@int |= (Int128)Unsafe.Add(ref reference, i);
			}
			@int <<= (16 - source.Length) * 8;
			@int = BinaryPrimitives.ReverseEndianness(@int);
			if (!isUnsigned)
			{
				@int |= One << 127 >> (16 - source.Length) * 8 - 1;
			}
		}
		goto IL_0136;
		IL_0136:
		value = @int;
		return true;
	}

	int IBinaryInteger<Int128>.GetShortestBitLength()
	{
		Int128 @int = this;
		if (IsPositive(@int))
		{
			return 128 - BitOperations.LeadingZeroCount(@int);
		}
		return 129 - BitOperations.LeadingZeroCount(~@int);
	}

	int IBinaryInteger<Int128>.GetByteCount()
	{
		return 16;
	}

	bool IBinaryInteger<Int128>.TryWriteBigEndian(Span<byte> destination, out int bytesWritten)
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

	bool IBinaryInteger<Int128>.TryWriteLittleEndian(Span<byte> destination, out int bytesWritten)
	{
		if (destination.Length >= 16)
		{
			ulong lower = _lower;
			ulong upper = _upper;
			if (!BitConverter.IsLittleEndian)
			{
			}
			ref byte reference = ref MemoryMarshal.GetReference(destination);
			Unsafe.WriteUnaligned(ref reference, lower);
			Unsafe.WriteUnaligned(ref Unsafe.AddByteOffset(ref reference, (nint)8), upper);
			bytesWritten = 16;
			return true;
		}
		bytesWritten = 0;
		return false;
	}

	public static bool IsPow2(Int128 value)
	{
		if (PopCount(value) == 1u)
		{
			return IsPositive(value);
		}
		return false;
	}

	public static Int128 Log2(Int128 value)
	{
		if (IsNegative(value))
		{
			ThrowHelper.ThrowValueArgumentOutOfRange_NeedNonNegNumException();
		}
		if (value._upper == 0L)
		{
			return ulong.Log2(value._lower);
		}
		return 64 + ulong.Log2(value._upper);
	}

	public static Int128 operator &(Int128 left, Int128 right)
	{
		return new Int128(left._upper & right._upper, left._lower & right._lower);
	}

	public static Int128 operator |(Int128 left, Int128 right)
	{
		return new Int128(left._upper | right._upper, left._lower | right._lower);
	}

	public static Int128 operator ^(Int128 left, Int128 right)
	{
		return new Int128(left._upper ^ right._upper, left._lower ^ right._lower);
	}

	public static Int128 operator ~(Int128 value)
	{
		return new Int128(~value._upper, ~value._lower);
	}

	public static bool operator <(Int128 left, Int128 right)
	{
		if (IsNegative(left) == IsNegative(right))
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
		return IsNegative(left);
	}

	public static bool operator <=(Int128 left, Int128 right)
	{
		if (IsNegative(left) == IsNegative(right))
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
		return IsNegative(left);
	}

	public static bool operator >(Int128 left, Int128 right)
	{
		if (IsNegative(left) == IsNegative(right))
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
		return IsNegative(right);
	}

	public static bool operator >=(Int128 left, Int128 right)
	{
		if (IsNegative(left) == IsNegative(right))
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
		return IsNegative(right);
	}

	public static Int128 operator --(Int128 value)
	{
		return value - One;
	}

	public static Int128 operator checked --(Int128 value)
	{
		return checked(value - One);
	}

	public static Int128 operator /(Int128 left, Int128 right)
	{
		if (right == -1 && left._upper == 9223372036854775808uL && left._lower == 0L)
		{
			ThrowHelper.ThrowOverflowException();
		}
		ulong num = (left._upper ^ right._upper) & 0x8000000000000000uL;
		if (IsNegative(left))
		{
			left = ~left + 1u;
		}
		if (IsNegative(right))
		{
			right = ~right + 1u;
		}
		UInt128 uInt = (UInt128)left / (UInt128)right;
		if (num != 0L)
		{
			uInt = ~uInt + 1u;
		}
		return new Int128(uInt.Upper, uInt.Lower);
	}

	public static Int128 operator checked /(Int128 left, Int128 right)
	{
		return left / right;
	}

	public static bool operator ==(Int128 left, Int128 right)
	{
		if (left._lower == right._lower)
		{
			return left._upper == right._upper;
		}
		return false;
	}

	public static bool operator !=(Int128 left, Int128 right)
	{
		if (left._lower == right._lower)
		{
			return left._upper != right._upper;
		}
		return true;
	}

	public static Int128 operator ++(Int128 value)
	{
		return value + One;
	}

	public static Int128 operator checked ++(Int128 value)
	{
		return checked(value + One);
	}

	public static Int128 operator %(Int128 left, Int128 right)
	{
		Int128 @int = left / right;
		return left - @int * right;
	}

	public static Int128 operator *(Int128 left, Int128 right)
	{
		return (Int128)((UInt128)left * (UInt128)right);
	}

	public static Int128 operator checked *(Int128 left, Int128 right)
	{
		Int128 lower;
		Int128 @int = BigMul(left, right, out lower);
		if ((@int != 0 || lower < 0) && (~@int != 0 || lower >= 0))
		{
			ThrowHelper.ThrowOverflowException();
		}
		return lower;
	}

	internal static Int128 BigMul(Int128 left, Int128 right, out Int128 lower)
	{
		UInt128 lower2;
		UInt128 uInt = UInt128.BigMul((UInt128)left, (UInt128)right, out lower2);
		lower = (Int128)lower2;
		return (Int128)uInt - ((left >> 127) & right) - ((right >> 127) & left);
	}

	public static Int128 Clamp(Int128 value, Int128 min, Int128 max)
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

	public static Int128 CopySign(Int128 value, Int128 sign)
	{
		Int128 @int = value;
		if (IsNegative(@int))
		{
			@int = -@int;
		}
		if (IsPositive(sign))
		{
			if (IsNegative(@int))
			{
				Math.ThrowNegateTwosCompOverflow();
			}
			return @int;
		}
		return -@int;
	}

	public static Int128 Max(Int128 x, Int128 y)
	{
		if (!(x >= y))
		{
			return y;
		}
		return x;
	}

	static Int128 INumber<Int128>.MaxNumber(Int128 x, Int128 y)
	{
		return Max(x, y);
	}

	public static Int128 Min(Int128 x, Int128 y)
	{
		if (!(x <= y))
		{
			return y;
		}
		return x;
	}

	static Int128 INumber<Int128>.MinNumber(Int128 x, Int128 y)
	{
		return Min(x, y);
	}

	public static int Sign(Int128 value)
	{
		if (IsNegative(value))
		{
			return -1;
		}
		if (value != default(Int128))
		{
			return 1;
		}
		return 0;
	}

	public static Int128 Abs(Int128 value)
	{
		if (IsNegative(value))
		{
			value = -value;
			if (IsNegative(value))
			{
				Math.ThrowNegateTwosCompOverflow();
			}
		}
		return value;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Int128 CreateChecked<TOther>(TOther value) where TOther : INumberBase<TOther>
	{
		if (typeof(TOther) == typeof(Int128))
		{
			return (Int128)(object)value;
		}
		if (!TryConvertFromChecked(value, out var result) && !TOther.TryConvertToChecked<Int128>(value, out result))
		{
			ThrowHelper.ThrowNotSupportedException();
		}
		return result;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Int128 CreateSaturating<TOther>(TOther value) where TOther : INumberBase<TOther>
	{
		if (typeof(TOther) == typeof(Int128))
		{
			return (Int128)(object)value;
		}
		if (!TryConvertFromSaturating(value, out var result) && !TOther.TryConvertToSaturating<Int128>(value, out result))
		{
			ThrowHelper.ThrowNotSupportedException();
		}
		return result;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Int128 CreateTruncating<TOther>(TOther value) where TOther : INumberBase<TOther>
	{
		if (typeof(TOther) == typeof(Int128))
		{
			return (Int128)(object)value;
		}
		if (!TryConvertFromTruncating(value, out var result) && !TOther.TryConvertToTruncating<Int128>(value, out result))
		{
			ThrowHelper.ThrowNotSupportedException();
		}
		return result;
	}

	static bool INumberBase<Int128>.IsCanonical(Int128 value)
	{
		return true;
	}

	static bool INumberBase<Int128>.IsComplexNumber(Int128 value)
	{
		return false;
	}

	public static bool IsEvenInteger(Int128 value)
	{
		return (value._lower & 1) == 0;
	}

	static bool INumberBase<Int128>.IsFinite(Int128 value)
	{
		return true;
	}

	static bool INumberBase<Int128>.IsImaginaryNumber(Int128 value)
	{
		return false;
	}

	static bool INumberBase<Int128>.IsInfinity(Int128 value)
	{
		return false;
	}

	static bool INumberBase<Int128>.IsInteger(Int128 value)
	{
		return true;
	}

	static bool INumberBase<Int128>.IsNaN(Int128 value)
	{
		return false;
	}

	public static bool IsNegative(Int128 value)
	{
		return (long)value._upper < 0L;
	}

	static bool INumberBase<Int128>.IsNegativeInfinity(Int128 value)
	{
		return false;
	}

	static bool INumberBase<Int128>.IsNormal(Int128 value)
	{
		return value != 0;
	}

	public static bool IsOddInteger(Int128 value)
	{
		return (value._lower & 1) != 0;
	}

	public static bool IsPositive(Int128 value)
	{
		return (long)value._upper >= 0L;
	}

	static bool INumberBase<Int128>.IsPositiveInfinity(Int128 value)
	{
		return false;
	}

	static bool INumberBase<Int128>.IsRealNumber(Int128 value)
	{
		return true;
	}

	static bool INumberBase<Int128>.IsSubnormal(Int128 value)
	{
		return false;
	}

	static bool INumberBase<Int128>.IsZero(Int128 value)
	{
		return value == 0;
	}

	public static Int128 MaxMagnitude(Int128 x, Int128 y)
	{
		Int128 @int = x;
		if (IsNegative(@int))
		{
			@int = -@int;
			if (IsNegative(@int))
			{
				return x;
			}
		}
		Int128 int2 = y;
		if (IsNegative(int2))
		{
			int2 = -int2;
			if (IsNegative(int2))
			{
				return y;
			}
		}
		if (@int > int2)
		{
			return x;
		}
		if (@int == int2)
		{
			if (!IsNegative(x))
			{
				return x;
			}
			return y;
		}
		return y;
	}

	static Int128 INumberBase<Int128>.MaxMagnitudeNumber(Int128 x, Int128 y)
	{
		return MaxMagnitude(x, y);
	}

	public static Int128 MinMagnitude(Int128 x, Int128 y)
	{
		Int128 @int = x;
		if (IsNegative(@int))
		{
			@int = -@int;
			if (IsNegative(@int))
			{
				return y;
			}
		}
		Int128 int2 = y;
		if (IsNegative(int2))
		{
			int2 = -int2;
			if (IsNegative(int2))
			{
				return x;
			}
		}
		if (@int < int2)
		{
			return x;
		}
		if (@int == int2)
		{
			if (!IsNegative(x))
			{
				return y;
			}
			return x;
		}
		return y;
	}

	static Int128 INumberBase<Int128>.MinMagnitudeNumber(Int128 x, Int128 y)
	{
		return MinMagnitude(x, y);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	static bool INumberBase<Int128>.TryConvertFromChecked<TOther>(TOther value, out Int128 result)
	{
		return TryConvertFromChecked(value, out result);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static bool TryConvertFromChecked<TOther>(TOther value, out Int128 result) where TOther : INumberBase<TOther>
	{
		checked
		{
			if (typeof(TOther) == typeof(double))
			{
				double num = (double)(object)value;
				result = (Int128)num;
				return true;
			}
			if (typeof(TOther) == typeof(Half))
			{
				Half half = (Half)(object)value;
				result = (Int128)half;
				return true;
			}
			if (typeof(TOther) == typeof(short))
			{
				short num2 = (short)(object)value;
				result = num2;
				return true;
			}
			if (typeof(TOther) == typeof(int))
			{
				int num3 = (int)(object)value;
				result = num3;
				return true;
			}
			if (typeof(TOther) == typeof(long))
			{
				long num4 = (long)(object)value;
				result = num4;
				return true;
			}
			if (typeof(TOther) == typeof(nint))
			{
				nint num5 = (nint)(object)value;
				result = num5;
				return true;
			}
			if (typeof(TOther) == typeof(sbyte))
			{
				sbyte b = (sbyte)(object)value;
				result = b;
				return true;
			}
			if (typeof(TOther) == typeof(float))
			{
				float num6 = (float)(object)value;
				result = (Int128)num6;
				return true;
			}
			result = default(Int128);
			return false;
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	static bool INumberBase<Int128>.TryConvertFromSaturating<TOther>(TOther value, out Int128 result)
	{
		return TryConvertFromSaturating(value, out result);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static bool TryConvertFromSaturating<TOther>(TOther value, out Int128 result) where TOther : INumberBase<TOther>
	{
		if (typeof(TOther) == typeof(double))
		{
			double num = (double)(object)value;
			result = ((num >= 1.7014118346046923E+38) ? MaxValue : ((num <= -1.7014118346046923E+38) ? MinValue : ((Int128)num)));
			return true;
		}
		if (typeof(TOther) == typeof(Half))
		{
			Half half = (Half)(object)value;
			result = ((half == Half.PositiveInfinity) ? MaxValue : ((half == Half.NegativeInfinity) ? MinValue : ((Int128)half)));
			return true;
		}
		if (typeof(TOther) == typeof(short))
		{
			short num2 = (short)(object)value;
			result = num2;
			return true;
		}
		if (typeof(TOther) == typeof(int))
		{
			int num3 = (int)(object)value;
			result = num3;
			return true;
		}
		if (typeof(TOther) == typeof(long))
		{
			long num4 = (long)(object)value;
			result = num4;
			return true;
		}
		if (typeof(TOther) == typeof(nint))
		{
			nint num5 = (nint)(object)value;
			result = num5;
			return true;
		}
		if (typeof(TOther) == typeof(sbyte))
		{
			sbyte b = (sbyte)(object)value;
			result = b;
			return true;
		}
		if (typeof(TOther) == typeof(float))
		{
			float num6 = (float)(object)value;
			result = ((num6 >= 1.7014118E+38f) ? MaxValue : ((num6 <= -1.7014118E+38f) ? MinValue : ((Int128)num6)));
			return true;
		}
		result = default(Int128);
		return false;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	static bool INumberBase<Int128>.TryConvertFromTruncating<TOther>(TOther value, out Int128 result)
	{
		return TryConvertFromTruncating(value, out result);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static bool TryConvertFromTruncating<TOther>(TOther value, out Int128 result) where TOther : INumberBase<TOther>
	{
		if (typeof(TOther) == typeof(double))
		{
			double num = (double)(object)value;
			result = ((num >= 1.7014118346046923E+38) ? MaxValue : ((num <= -1.7014118346046923E+38) ? MinValue : ((Int128)num)));
			return true;
		}
		if (typeof(TOther) == typeof(Half))
		{
			Half half = (Half)(object)value;
			result = ((half == Half.PositiveInfinity) ? MaxValue : ((half == Half.NegativeInfinity) ? MinValue : ((Int128)half)));
			return true;
		}
		if (typeof(TOther) == typeof(short))
		{
			short num2 = (short)(object)value;
			result = num2;
			return true;
		}
		if (typeof(TOther) == typeof(int))
		{
			int num3 = (int)(object)value;
			result = num3;
			return true;
		}
		if (typeof(TOther) == typeof(long))
		{
			long num4 = (long)(object)value;
			result = num4;
			return true;
		}
		if (typeof(TOther) == typeof(nint))
		{
			nint num5 = (nint)(object)value;
			result = num5;
			return true;
		}
		if (typeof(TOther) == typeof(sbyte))
		{
			sbyte b = (sbyte)(object)value;
			result = b;
			return true;
		}
		if (typeof(TOther) == typeof(float))
		{
			float num6 = (float)(object)value;
			result = ((num6 >= 1.7014118E+38f) ? MaxValue : ((num6 <= -1.7014118E+38f) ? MinValue : ((Int128)num6)));
			return true;
		}
		result = default(Int128);
		return false;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	static bool INumberBase<Int128>.TryConvertToChecked<TOther>(Int128 value, [MaybeNullWhen(false)] out TOther result)
	{
		checked
		{
			if (typeof(TOther) == typeof(byte))
			{
				byte b = (byte)value;
				result = (TOther)(object)b;
				return true;
			}
			if (typeof(TOther) == typeof(char))
			{
				char c = (char)value;
				result = (TOther)(object)c;
				return true;
			}
			if (typeof(TOther) == typeof(decimal))
			{
				decimal num = unchecked((decimal)value);
				result = (TOther)(object)num;
				return true;
			}
			if (typeof(TOther) == typeof(ushort))
			{
				ushort num2 = (ushort)value;
				result = (TOther)(object)num2;
				return true;
			}
			if (typeof(TOther) == typeof(uint))
			{
				uint num3 = (uint)value;
				result = (TOther)(object)num3;
				return true;
			}
			if (typeof(TOther) == typeof(ulong))
			{
				ulong num4 = (ulong)value;
				result = (TOther)(object)num4;
				return true;
			}
			if (typeof(TOther) == typeof(UInt128))
			{
				UInt128 uInt = (UInt128)value;
				result = (TOther)(object)uInt;
				return true;
			}
			if (typeof(TOther) == typeof(nuint))
			{
				nuint num5 = (nuint)value;
				result = (TOther)(object)num5;
				return true;
			}
			result = default(TOther);
			return false;
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	static bool INumberBase<Int128>.TryConvertToSaturating<TOther>(Int128 value, [MaybeNullWhen(false)] out TOther result)
	{
		if (typeof(TOther) == typeof(byte))
		{
			byte b = (byte)((value >= byte.MaxValue) ? byte.MaxValue : ((!(value <= (byte)0)) ? ((byte)value) : 0));
			result = (TOther)(object)b;
			return true;
		}
		if (typeof(TOther) == typeof(char))
		{
			char c = ((value >= '\uffff') ? '\uffff' : ((!(value <= '\0')) ? ((char)value) : '\0'));
			result = (TOther)(object)c;
			return true;
		}
		if (typeof(TOther) == typeof(decimal))
		{
			decimal num = ((value >= new Int128(4294967295uL, ulong.MaxValue)) ? decimal.MaxValue : ((value <= new Int128(18446744069414584320uL, 1uL)) ? decimal.MinValue : ((decimal)value)));
			result = (TOther)(object)num;
			return true;
		}
		if (typeof(TOther) == typeof(ushort))
		{
			ushort num2 = (ushort)((value >= ushort.MaxValue) ? ushort.MaxValue : ((!(value <= (ushort)0)) ? ((ushort)value) : 0));
			result = (TOther)(object)num2;
			return true;
		}
		if (typeof(TOther) == typeof(uint))
		{
			uint num3 = ((value >= uint.MaxValue) ? uint.MaxValue : ((!(value <= 0u)) ? ((uint)value) : 0u));
			result = (TOther)(object)num3;
			return true;
		}
		if (typeof(TOther) == typeof(ulong))
		{
			ulong num4 = ((value >= ulong.MaxValue) ? ulong.MaxValue : ((value <= 0uL) ? 0 : ((ulong)value)));
			result = (TOther)(object)num4;
			return true;
		}
		if (typeof(TOther) == typeof(UInt128))
		{
			UInt128 uInt = ((value <= 0) ? UInt128.MinValue : ((UInt128)value));
			result = (TOther)(object)uInt;
			return true;
		}
		if (typeof(TOther) == typeof(nuint))
		{
			nuint num5 = ((value >= UIntPtr.MaxValue) ? UIntPtr.MaxValue : ((value <= UIntPtr.MinValue) ? UIntPtr.MinValue : ((nuint)value)));
			result = (TOther)(object)num5;
			return true;
		}
		result = default(TOther);
		return false;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	static bool INumberBase<Int128>.TryConvertToTruncating<TOther>(Int128 value, [MaybeNullWhen(false)] out TOther result)
	{
		if (typeof(TOther) == typeof(byte))
		{
			byte b = (byte)value;
			result = (TOther)(object)b;
			return true;
		}
		if (typeof(TOther) == typeof(char))
		{
			char c = (char)value;
			result = (TOther)(object)c;
			return true;
		}
		if (typeof(TOther) == typeof(decimal))
		{
			decimal num = ((value >= new Int128(4294967295uL, ulong.MaxValue)) ? decimal.MaxValue : ((value <= new Int128(18446744069414584320uL, 1uL)) ? decimal.MinValue : ((decimal)value)));
			result = (TOther)(object)num;
			return true;
		}
		if (typeof(TOther) == typeof(ushort))
		{
			ushort num2 = (ushort)value;
			result = (TOther)(object)num2;
			return true;
		}
		if (typeof(TOther) == typeof(uint))
		{
			uint num3 = (uint)value;
			result = (TOther)(object)num3;
			return true;
		}
		if (typeof(TOther) == typeof(ulong))
		{
			ulong num4 = (ulong)value;
			result = (TOther)(object)num4;
			return true;
		}
		if (typeof(TOther) == typeof(UInt128))
		{
			UInt128 uInt = (UInt128)value;
			result = (TOther)(object)uInt;
			return true;
		}
		if (typeof(TOther) == typeof(nuint))
		{
			nuint num5 = (nuint)value;
			result = (TOther)(object)num5;
			return true;
		}
		result = default(TOther);
		return false;
	}

	public static bool TryParse([NotNullWhen(true)] string? s, IFormatProvider? provider, out Int128 result)
	{
		return TryParse(s, NumberStyles.Integer, provider, out result);
	}

	public static Int128 operator <<(Int128 value, int shiftAmount)
	{
		shiftAmount &= 0x7F;
		if (((uint)shiftAmount & 0x40u) != 0)
		{
			ulong upper = value._lower << shiftAmount;
			return new Int128(upper, 0uL);
		}
		if (shiftAmount != 0)
		{
			ulong lower = value._lower << shiftAmount;
			ulong upper2 = (value._upper << shiftAmount) | (value._lower >> 64 - shiftAmount);
			return new Int128(upper2, lower);
		}
		return value;
	}

	public static Int128 operator >>(Int128 value, int shiftAmount)
	{
		shiftAmount &= 0x7F;
		if (((uint)shiftAmount & 0x40u) != 0)
		{
			ulong lower = (ulong)((long)value._upper >> shiftAmount);
			ulong upper = (ulong)((long)value._upper >> 63);
			return new Int128(upper, lower);
		}
		if (shiftAmount != 0)
		{
			ulong lower2 = (value._lower >> shiftAmount) | (value._upper << 64 - shiftAmount);
			ulong upper2 = (ulong)((long)value._upper >> shiftAmount);
			return new Int128(upper2, lower2);
		}
		return value;
	}

	public static Int128 operator >>>(Int128 value, int shiftAmount)
	{
		shiftAmount &= 0x7F;
		if (((uint)shiftAmount & 0x40u) != 0)
		{
			ulong lower = value._upper >> shiftAmount;
			return new Int128(0uL, lower);
		}
		if (shiftAmount != 0)
		{
			ulong lower2 = (value._lower >> shiftAmount) | (value._upper << 64 - shiftAmount);
			ulong upper = value._upper >> shiftAmount;
			return new Int128(upper, lower2);
		}
		return value;
	}

	public static Int128 Parse(ReadOnlySpan<char> s, IFormatProvider? provider)
	{
		return Parse(s, NumberStyles.Integer, provider);
	}

	public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, out Int128 result)
	{
		return TryParse(s, NumberStyles.Integer, provider, out result);
	}

	public static Int128 operator -(Int128 left, Int128 right)
	{
		ulong num = left._lower - right._lower;
		ulong num2 = (ulong)((num > left._lower) ? 1 : 0);
		ulong upper = left._upper - right._upper - num2;
		return new Int128(upper, num);
	}

	public static Int128 operator checked -(Int128 left, Int128 right)
	{
		Int128 result = left - right;
		uint num = (uint)(left._upper >> 63);
		if (num != (uint)(right._upper >> 63) && num != (uint)(result._upper >> 63))
		{
			ThrowHelper.ThrowOverflowException();
		}
		return result;
	}

	public static Int128 operator -(Int128 value)
	{
		return Zero - value;
	}

	public static Int128 operator checked -(Int128 value)
	{
		return checked(Zero - value);
	}

	public static Int128 operator +(Int128 value)
	{
		return value;
	}

	public static Int128 Parse(ReadOnlySpan<byte> utf8Text, NumberStyles style = NumberStyles.Integer, IFormatProvider? provider = null)
	{
		NumberFormatInfo.ValidateParseStyleInteger(style);
		return Number.ParseBinaryInteger<byte, Int128>(utf8Text, style, NumberFormatInfo.GetInstance(provider));
	}

	public static bool TryParse(ReadOnlySpan<byte> utf8Text, NumberStyles style, IFormatProvider? provider, out Int128 result)
	{
		NumberFormatInfo.ValidateParseStyleInteger(style);
		return Number.TryParseBinaryInteger<byte, Int128>(utf8Text, style, NumberFormatInfo.GetInstance(provider), out result) == Number.ParsingStatus.OK;
	}

	public static Int128 Parse(ReadOnlySpan<byte> utf8Text, IFormatProvider? provider)
	{
		return Parse(utf8Text, NumberStyles.Integer, provider);
	}

	public static bool TryParse(ReadOnlySpan<byte> utf8Text, IFormatProvider? provider, out Int128 result)
	{
		return TryParse(utf8Text, NumberStyles.Integer, provider, out result);
	}

	static bool IBinaryIntegerParseAndFormatInfo<Int128>.IsGreaterThanAsUnsigned(Int128 left, Int128 right)
	{
		return (UInt128)left > (UInt128)right;
	}

	static Int128 IBinaryIntegerParseAndFormatInfo<Int128>.MultiplyBy10(Int128 value)
	{
		return value * 10;
	}

	static Int128 IBinaryIntegerParseAndFormatInfo<Int128>.MultiplyBy16(Int128 value)
	{
		return value * 16;
	}
}
