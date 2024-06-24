using System.Buffers.Binary;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace System;

[Serializable]
[TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
public readonly struct Int64 : IComparable, IConvertible, ISpanFormattable, IFormattable, IComparable<long>, IEquatable<long>, IBinaryInteger<long>, IBinaryNumber<long>, IBitwiseOperators<long, long, long>, INumber<long>, IComparisonOperators<long, long, bool>, IEqualityOperators<long, long, bool>, IModulusOperators<long, long, long>, INumberBase<long>, IAdditionOperators<long, long, long>, IAdditiveIdentity<long, long>, IDecrementOperators<long>, IDivisionOperators<long, long, long>, IIncrementOperators<long>, IMultiplicativeIdentity<long, long>, IMultiplyOperators<long, long, long>, ISpanParsable<long>, IParsable<long>, ISubtractionOperators<long, long, long>, IUnaryPlusOperators<long, long>, IUnaryNegationOperators<long, long>, IUtf8SpanFormattable, IUtf8SpanParsable<long>, IShiftOperators<long, int, long>, IMinMaxValue<long>, ISignedNumber<long>, IBinaryIntegerParseAndFormatInfo<long>
{
	private readonly long m_value;

	public const long MaxValue = 9223372036854775807L;

	public const long MinValue = -9223372036854775808L;

	static long IAdditiveIdentity<long, long>.AdditiveIdentity => 0L;

	static long IBinaryNumber<long>.AllBitsSet => -1L;

	static long IMinMaxValue<long>.MinValue => long.MinValue;

	static long IMinMaxValue<long>.MaxValue => long.MaxValue;

	static long IMultiplicativeIdentity<long, long>.MultiplicativeIdentity => 1L;

	static long INumberBase<long>.One => 1L;

	static int INumberBase<long>.Radix => 2;

	static long INumberBase<long>.Zero => 0L;

	static long ISignedNumber<long>.NegativeOne => -1L;

	static bool IBinaryIntegerParseAndFormatInfo<long>.IsSigned => true;

	static int IBinaryIntegerParseAndFormatInfo<long>.MaxDigitCount => 19;

	static int IBinaryIntegerParseAndFormatInfo<long>.MaxHexDigitCount => 16;

	static long IBinaryIntegerParseAndFormatInfo<long>.MaxValueDiv10 => 922337203685477580L;

	static string IBinaryIntegerParseAndFormatInfo<long>.OverflowMessage => SR.Overflow_Int64;

	public int CompareTo(object? value)
	{
		if (value == null)
		{
			return 1;
		}
		if (value is long num)
		{
			if (this < num)
			{
				return -1;
			}
			if (this > num)
			{
				return 1;
			}
			return 0;
		}
		throw new ArgumentException(SR.Arg_MustBeInt64);
	}

	public int CompareTo(long value)
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
		if (!(obj is long))
		{
			return false;
		}
		return this == (long)obj;
	}

	[NonVersionable]
	public bool Equals(long obj)
	{
		return this == obj;
	}

	public override int GetHashCode()
	{
		return (int)this ^ (int)(this >> 32);
	}

	public override string ToString()
	{
		return Number.Int64ToDecStr(this);
	}

	public string ToString(IFormatProvider? provider)
	{
		return Number.FormatInt64(this, null, provider);
	}

	public string ToString([StringSyntax("NumericFormat")] string? format)
	{
		return Number.FormatInt64(this, format, null);
	}

	public string ToString([StringSyntax("NumericFormat")] string? format, IFormatProvider? provider)
	{
		return Number.FormatInt64(this, format, provider);
	}

	public bool TryFormat(Span<char> destination, out int charsWritten, [StringSyntax("NumericFormat")] ReadOnlySpan<char> format = default(ReadOnlySpan<char>), IFormatProvider? provider = null)
	{
		return Number.TryFormatInt64(this, format, provider, destination, out charsWritten);
	}

	public bool TryFormat(Span<byte> utf8Destination, out int bytesWritten, [StringSyntax("NumericFormat")] ReadOnlySpan<char> format = default(ReadOnlySpan<char>), IFormatProvider? provider = null)
	{
		return Number.TryFormatInt64(this, format, provider, utf8Destination, out bytesWritten);
	}

	public static long Parse(string s)
	{
		return Parse(s, NumberStyles.Integer, null);
	}

	public static long Parse(string s, NumberStyles style)
	{
		return Parse(s, style, null);
	}

	public static long Parse(string s, IFormatProvider? provider)
	{
		return Parse(s, NumberStyles.Integer, provider);
	}

	public static long Parse(string s, NumberStyles style, IFormatProvider? provider)
	{
		if (s == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.s);
		}
		return Parse(s.AsSpan(), style, provider);
	}

	public static long Parse(ReadOnlySpan<char> s, NumberStyles style = NumberStyles.Integer, IFormatProvider? provider = null)
	{
		NumberFormatInfo.ValidateParseStyleInteger(style);
		return Number.ParseBinaryInteger<char, long>(s, style, NumberFormatInfo.GetInstance(provider));
	}

	public static bool TryParse([NotNullWhen(true)] string? s, out long result)
	{
		return TryParse(s, NumberStyles.Integer, null, out result);
	}

	public static bool TryParse(ReadOnlySpan<char> s, out long result)
	{
		return TryParse(s, NumberStyles.Integer, null, out result);
	}

	public static bool TryParse(ReadOnlySpan<byte> utf8Text, out long result)
	{
		return TryParse(utf8Text, NumberStyles.Integer, null, out result);
	}

	public static bool TryParse([NotNullWhen(true)] string? s, NumberStyles style, IFormatProvider? provider, out long result)
	{
		NumberFormatInfo.ValidateParseStyleInteger(style);
		if (s == null)
		{
			result = 0L;
			return false;
		}
		return Number.TryParseBinaryInteger<char, long>(s.AsSpan(), style, NumberFormatInfo.GetInstance(provider), out result) == Number.ParsingStatus.OK;
	}

	public static bool TryParse(ReadOnlySpan<char> s, NumberStyles style, IFormatProvider? provider, out long result)
	{
		NumberFormatInfo.ValidateParseStyleInteger(style);
		return Number.TryParseBinaryInteger<char, long>(s, style, NumberFormatInfo.GetInstance(provider), out result) == Number.ParsingStatus.OK;
	}

	public TypeCode GetTypeCode()
	{
		return TypeCode.Int64;
	}

	bool IConvertible.ToBoolean(IFormatProvider provider)
	{
		return Convert.ToBoolean(this);
	}

	char IConvertible.ToChar(IFormatProvider provider)
	{
		return Convert.ToChar(this);
	}

	sbyte IConvertible.ToSByte(IFormatProvider provider)
	{
		return Convert.ToSByte(this);
	}

	byte IConvertible.ToByte(IFormatProvider provider)
	{
		return Convert.ToByte(this);
	}

	short IConvertible.ToInt16(IFormatProvider provider)
	{
		return Convert.ToInt16(this);
	}

	ushort IConvertible.ToUInt16(IFormatProvider provider)
	{
		return Convert.ToUInt16(this);
	}

	int IConvertible.ToInt32(IFormatProvider provider)
	{
		return Convert.ToInt32(this);
	}

	uint IConvertible.ToUInt32(IFormatProvider provider)
	{
		return Convert.ToUInt32(this);
	}

	long IConvertible.ToInt64(IFormatProvider provider)
	{
		return this;
	}

	ulong IConvertible.ToUInt64(IFormatProvider provider)
	{
		return Convert.ToUInt64(this);
	}

	float IConvertible.ToSingle(IFormatProvider provider)
	{
		return Convert.ToSingle(this);
	}

	double IConvertible.ToDouble(IFormatProvider provider)
	{
		return Convert.ToDouble(this);
	}

	decimal IConvertible.ToDecimal(IFormatProvider provider)
	{
		return Convert.ToDecimal(this);
	}

	DateTime IConvertible.ToDateTime(IFormatProvider provider)
	{
		throw new InvalidCastException(SR.Format(SR.InvalidCast_FromTo, "Int64", "DateTime"));
	}

	object IConvertible.ToType(Type type, IFormatProvider provider)
	{
		return Convert.DefaultToType(this, type, provider);
	}

	static long IAdditionOperators<long, long, long>.operator +(long left, long right)
	{
		return left + right;
	}

	static long IAdditionOperators<long, long, long>.operator checked +(long left, long right)
	{
		return checked(left + right);
	}

	public static (long Quotient, long Remainder) DivRem(long left, long right)
	{
		return Math.DivRem(left, right);
	}

	[Intrinsic]
	public static long LeadingZeroCount(long value)
	{
		return BitOperations.LeadingZeroCount((ulong)value);
	}

	[Intrinsic]
	public static long PopCount(long value)
	{
		return BitOperations.PopCount((ulong)value);
	}

	[Intrinsic]
	public static long RotateLeft(long value, int rotateAmount)
	{
		return (long)BitOperations.RotateLeft((ulong)value, rotateAmount);
	}

	[Intrinsic]
	public static long RotateRight(long value, int rotateAmount)
	{
		return (long)BitOperations.RotateRight((ulong)value, rotateAmount);
	}

	[Intrinsic]
	public static long TrailingZeroCount(long value)
	{
		return BitOperations.TrailingZeroCount(value);
	}

	static bool IBinaryInteger<long>.TryReadBigEndian(ReadOnlySpan<byte> source, bool isUnsigned, out long value)
	{
		long num = 0L;
		if (source.Length != 0)
		{
			sbyte b = (sbyte)source[0];
			b >>= 31;
			isUnsigned = isUnsigned || b == 0;
			if (isUnsigned && sbyte.IsNegative(b) && source.Length >= 8)
			{
				value = num;
				return false;
			}
			if (source.Length > 8)
			{
				if (source.Slice(0, source.Length - 8).ContainsAnyExcept((byte)b))
				{
					value = num;
					return false;
				}
				if (isUnsigned == sbyte.IsNegative((sbyte)source[source.Length - 8]))
				{
					value = num;
					return false;
				}
			}
			ref byte reference = ref MemoryMarshal.GetReference(source);
			if (source.Length >= 8)
			{
				num = Unsafe.ReadUnaligned<long>(ref Unsafe.Add(ref reference, source.Length - 8));
				_ = BitConverter.IsLittleEndian;
				num = BinaryPrimitives.ReverseEndianness(num);
			}
			else
			{
				for (int i = 0; i < source.Length; i++)
				{
					num <<= 8;
					num |= Unsafe.Add(ref reference, i);
				}
				if (!isUnsigned)
				{
					num |= long.MinValue >> (8 - source.Length) * 8 - 1;
				}
			}
		}
		value = num;
		return true;
	}

	static bool IBinaryInteger<long>.TryReadLittleEndian(ReadOnlySpan<byte> source, bool isUnsigned, out long value)
	{
		long num = 0L;
		if (source.Length != 0)
		{
			sbyte b = (sbyte)source[source.Length - 1];
			b >>= 31;
			isUnsigned = isUnsigned || b == 0;
			if (isUnsigned && sbyte.IsNegative(b) && source.Length >= 8)
			{
				value = num;
				return false;
			}
			if (source.Length > 8)
			{
				if (source.Slice(8, source.Length - 8).ContainsAnyExcept((byte)b))
				{
					value = num;
					return false;
				}
				if (isUnsigned == sbyte.IsNegative((sbyte)source[7]))
				{
					value = num;
					return false;
				}
			}
			ref byte reference = ref MemoryMarshal.GetReference(source);
			if (source.Length >= 8)
			{
				num = Unsafe.ReadUnaligned<long>(ref reference);
				if (BitConverter.IsLittleEndian)
				{
					goto IL_0108;
				}
			}
			for (int i = 0; i < source.Length; i++)
			{
				num <<= 8;
				num |= Unsafe.Add(ref reference, i);
			}
			num <<= (8 - source.Length) * 8;
			num = BinaryPrimitives.ReverseEndianness(num);
			if (!isUnsigned)
			{
				num |= long.MinValue >> (8 - source.Length) * 8 - 1;
			}
		}
		goto IL_0108;
		IL_0108:
		value = num;
		return true;
	}

	int IBinaryInteger<long>.GetShortestBitLength()
	{
		long num = this;
		if (num >= 0)
		{
			return 64 - BitOperations.LeadingZeroCount((ulong)num);
		}
		return 65 - BitOperations.LeadingZeroCount((ulong)(~num));
	}

	int IBinaryInteger<long>.GetByteCount()
	{
		return 8;
	}

	bool IBinaryInteger<long>.TryWriteBigEndian(Span<byte> destination, out int bytesWritten)
	{
		if (destination.Length >= 8)
		{
			if (!BitConverter.IsLittleEndian)
			{
			}
			long value = BinaryPrimitives.ReverseEndianness(this);
			Unsafe.WriteUnaligned(ref MemoryMarshal.GetReference(destination), value);
			bytesWritten = 8;
			return true;
		}
		bytesWritten = 0;
		return false;
	}

	bool IBinaryInteger<long>.TryWriteLittleEndian(Span<byte> destination, out int bytesWritten)
	{
		if (destination.Length >= 8)
		{
			if (!BitConverter.IsLittleEndian)
			{
			}
			long value = this;
			Unsafe.WriteUnaligned(ref MemoryMarshal.GetReference(destination), value);
			bytesWritten = 8;
			return true;
		}
		bytesWritten = 0;
		return false;
	}

	public static bool IsPow2(long value)
	{
		return BitOperations.IsPow2(value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static long Log2(long value)
	{
		if (value < 0)
		{
			ThrowHelper.ThrowValueArgumentOutOfRange_NeedNonNegNumException();
		}
		return BitOperations.Log2((ulong)value);
	}

	static long IBitwiseOperators<long, long, long>.operator &(long left, long right)
	{
		return left & right;
	}

	static long IBitwiseOperators<long, long, long>.operator |(long left, long right)
	{
		return left | right;
	}

	static long IBitwiseOperators<long, long, long>.operator ^(long left, long right)
	{
		return left ^ right;
	}

	static long IBitwiseOperators<long, long, long>.operator ~(long value)
	{
		return ~value;
	}

	static bool IComparisonOperators<long, long, bool>.operator <(long left, long right)
	{
		return left < right;
	}

	static bool IComparisonOperators<long, long, bool>.operator <=(long left, long right)
	{
		return left <= right;
	}

	static bool IComparisonOperators<long, long, bool>.operator >(long left, long right)
	{
		return left > right;
	}

	static bool IComparisonOperators<long, long, bool>.operator >=(long left, long right)
	{
		return left >= right;
	}

	static long IDecrementOperators<long>.operator --(long value)
	{
		return --value;
	}

	static long IDecrementOperators<long>.operator checked --(long value)
	{
		return value = checked(value - 1);
	}

	static long IDivisionOperators<long, long, long>.operator /(long left, long right)
	{
		return left / right;
	}

	static bool IEqualityOperators<long, long, bool>.operator ==(long left, long right)
	{
		return left == right;
	}

	static bool IEqualityOperators<long, long, bool>.operator !=(long left, long right)
	{
		return left != right;
	}

	static long IIncrementOperators<long>.operator ++(long value)
	{
		return ++value;
	}

	static long IIncrementOperators<long>.operator checked ++(long value)
	{
		return value = checked(value + 1);
	}

	static long IModulusOperators<long, long, long>.operator %(long left, long right)
	{
		return left % right;
	}

	static long IMultiplyOperators<long, long, long>.operator *(long left, long right)
	{
		return left * right;
	}

	static long IMultiplyOperators<long, long, long>.operator checked *(long left, long right)
	{
		return checked(left * right);
	}

	public static long Clamp(long value, long min, long max)
	{
		return Math.Clamp(value, min, max);
	}

	public static long CopySign(long value, long sign)
	{
		long num = value;
		if (num < 0)
		{
			num = -num;
		}
		if (sign >= 0)
		{
			if (num < 0)
			{
				Math.ThrowNegateTwosCompOverflow();
			}
			return num;
		}
		return -num;
	}

	public static long Max(long x, long y)
	{
		return Math.Max(x, y);
	}

	static long INumber<long>.MaxNumber(long x, long y)
	{
		return Max(x, y);
	}

	public static long Min(long x, long y)
	{
		return Math.Min(x, y);
	}

	static long INumber<long>.MinNumber(long x, long y)
	{
		return Min(x, y);
	}

	public static int Sign(long value)
	{
		return Math.Sign(value);
	}

	public static long Abs(long value)
	{
		return Math.Abs(value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static long CreateChecked<TOther>(TOther value) where TOther : INumberBase<TOther>
	{
		if (typeof(TOther) == typeof(long))
		{
			return (long)(object)value;
		}
		if (!TryConvertFromChecked(value, out var result) && !TOther.TryConvertToChecked<long>(value, out result))
		{
			ThrowHelper.ThrowNotSupportedException();
		}
		return result;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static long CreateSaturating<TOther>(TOther value) where TOther : INumberBase<TOther>
	{
		if (typeof(TOther) == typeof(long))
		{
			return (long)(object)value;
		}
		if (!TryConvertFromSaturating(value, out var result) && !TOther.TryConvertToSaturating<long>(value, out result))
		{
			ThrowHelper.ThrowNotSupportedException();
		}
		return result;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static long CreateTruncating<TOther>(TOther value) where TOther : INumberBase<TOther>
	{
		if (typeof(TOther) == typeof(long))
		{
			return (long)(object)value;
		}
		if (!TryConvertFromTruncating(value, out var result) && !TOther.TryConvertToTruncating<long>(value, out result))
		{
			ThrowHelper.ThrowNotSupportedException();
		}
		return result;
	}

	static bool INumberBase<long>.IsCanonical(long value)
	{
		return true;
	}

	static bool INumberBase<long>.IsComplexNumber(long value)
	{
		return false;
	}

	public static bool IsEvenInteger(long value)
	{
		return (value & 1) == 0;
	}

	static bool INumberBase<long>.IsFinite(long value)
	{
		return true;
	}

	static bool INumberBase<long>.IsImaginaryNumber(long value)
	{
		return false;
	}

	static bool INumberBase<long>.IsInfinity(long value)
	{
		return false;
	}

	static bool INumberBase<long>.IsInteger(long value)
	{
		return true;
	}

	static bool INumberBase<long>.IsNaN(long value)
	{
		return false;
	}

	public static bool IsNegative(long value)
	{
		return value < 0;
	}

	static bool INumberBase<long>.IsNegativeInfinity(long value)
	{
		return false;
	}

	static bool INumberBase<long>.IsNormal(long value)
	{
		return value != 0;
	}

	public static bool IsOddInteger(long value)
	{
		return (value & 1) != 0;
	}

	public static bool IsPositive(long value)
	{
		return value >= 0;
	}

	static bool INumberBase<long>.IsPositiveInfinity(long value)
	{
		return false;
	}

	static bool INumberBase<long>.IsRealNumber(long value)
	{
		return true;
	}

	static bool INumberBase<long>.IsSubnormal(long value)
	{
		return false;
	}

	static bool INumberBase<long>.IsZero(long value)
	{
		return value == 0;
	}

	public static long MaxMagnitude(long x, long y)
	{
		long num = x;
		if (num < 0)
		{
			num = -num;
			if (num < 0)
			{
				return x;
			}
		}
		long num2 = y;
		if (num2 < 0)
		{
			num2 = -num2;
			if (num2 < 0)
			{
				return y;
			}
		}
		if (num > num2)
		{
			return x;
		}
		if (num == num2)
		{
			if (!IsNegative(x))
			{
				return x;
			}
			return y;
		}
		return y;
	}

	static long INumberBase<long>.MaxMagnitudeNumber(long x, long y)
	{
		return MaxMagnitude(x, y);
	}

	public static long MinMagnitude(long x, long y)
	{
		long num = x;
		if (num < 0)
		{
			num = -num;
			if (num < 0)
			{
				return y;
			}
		}
		long num2 = y;
		if (num2 < 0)
		{
			num2 = -num2;
			if (num2 < 0)
			{
				return x;
			}
		}
		if (num < num2)
		{
			return x;
		}
		if (num == num2)
		{
			if (!IsNegative(x))
			{
				return y;
			}
			return x;
		}
		return y;
	}

	static long INumberBase<long>.MinMagnitudeNumber(long x, long y)
	{
		return MinMagnitude(x, y);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	static bool INumberBase<long>.TryConvertFromChecked<TOther>(TOther value, out long result)
	{
		return TryConvertFromChecked(value, out result);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static bool TryConvertFromChecked<TOther>(TOther value, out long result) where TOther : INumberBase<TOther>
	{
		checked
		{
			if (typeof(TOther) == typeof(double))
			{
				double num = (double)(object)value;
				result = (long)num;
				return true;
			}
			if (typeof(TOther) == typeof(Half))
			{
				Half half = (Half)(object)value;
				result = (long)half;
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
			if (typeof(TOther) == typeof(Int128))
			{
				Int128 @int = (Int128)(object)value;
				result = (long)@int;
				return true;
			}
			if (typeof(TOther) == typeof(nint))
			{
				nint num4 = (nint)(object)value;
				result = num4;
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
				float num5 = (float)(object)value;
				result = (long)num5;
				return true;
			}
			result = 0L;
			return false;
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	static bool INumberBase<long>.TryConvertFromSaturating<TOther>(TOther value, out long result)
	{
		return TryConvertFromSaturating(value, out result);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static bool TryConvertFromSaturating<TOther>(TOther value, out long result) where TOther : INumberBase<TOther>
	{
		if (typeof(TOther) == typeof(double))
		{
			double num = (double)(object)value;
			result = ((num >= 9.223372036854776E+18) ? long.MaxValue : ((num <= -9.223372036854776E+18) ? long.MinValue : ((long)num)));
			return true;
		}
		if (typeof(TOther) == typeof(Half))
		{
			Half half = (Half)(object)value;
			result = ((half == Half.PositiveInfinity) ? long.MaxValue : ((half == Half.NegativeInfinity) ? long.MinValue : ((long)half)));
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
		if (typeof(TOther) == typeof(Int128))
		{
			Int128 @int = (Int128)(object)value;
			result = ((@int >= long.MaxValue) ? long.MaxValue : ((@int <= long.MinValue) ? long.MinValue : ((long)@int)));
			return true;
		}
		if (typeof(TOther) == typeof(nint))
		{
			nint num4 = (nint)(object)value;
			result = num4;
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
			float num5 = (float)(object)value;
			result = ((num5 >= 9.223372E+18f) ? long.MaxValue : ((num5 <= -9.223372E+18f) ? long.MinValue : ((long)num5)));
			return true;
		}
		result = 0L;
		return false;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	static bool INumberBase<long>.TryConvertFromTruncating<TOther>(TOther value, out long result)
	{
		return TryConvertFromTruncating(value, out result);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static bool TryConvertFromTruncating<TOther>(TOther value, out long result) where TOther : INumberBase<TOther>
	{
		if (typeof(TOther) == typeof(double))
		{
			double num = (double)(object)value;
			result = ((num >= 9.223372036854776E+18) ? long.MaxValue : ((num <= -9.223372036854776E+18) ? long.MinValue : ((long)num)));
			return true;
		}
		if (typeof(TOther) == typeof(Half))
		{
			Half half = (Half)(object)value;
			result = ((half == Half.PositiveInfinity) ? long.MaxValue : ((half == Half.NegativeInfinity) ? long.MinValue : ((long)half)));
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
		if (typeof(TOther) == typeof(Int128))
		{
			Int128 @int = (Int128)(object)value;
			result = (long)@int;
			return true;
		}
		if (typeof(TOther) == typeof(nint))
		{
			nint num4 = (nint)(object)value;
			result = num4;
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
			float num5 = (float)(object)value;
			result = ((num5 >= 9.223372E+18f) ? long.MaxValue : ((num5 <= -9.223372E+18f) ? long.MinValue : ((long)num5)));
			return true;
		}
		result = 0L;
		return false;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	static bool INumberBase<long>.TryConvertToChecked<TOther>(long value, [MaybeNullWhen(false)] out TOther result)
	{
		if (typeof(TOther) == typeof(byte))
		{
			byte b = checked((byte)value);
			result = (TOther)(object)b;
			return true;
		}
		if (typeof(TOther) == typeof(char))
		{
			char c = (char)checked((ushort)value);
			result = (TOther)(object)c;
			return true;
		}
		if (typeof(TOther) == typeof(decimal))
		{
			decimal num = value;
			result = (TOther)(object)num;
			return true;
		}
		checked
		{
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
	static bool INumberBase<long>.TryConvertToSaturating<TOther>(long value, [MaybeNullWhen(false)] out TOther result)
	{
		if (typeof(TOther) == typeof(byte))
		{
			byte b = (byte)((value >= 255) ? byte.MaxValue : ((value > 0) ? ((byte)value) : 0));
			result = (TOther)(object)b;
			return true;
		}
		if (typeof(TOther) == typeof(char))
		{
			char c = ((value >= 65535) ? '\uffff' : ((value > 0) ? ((char)value) : '\0'));
			result = (TOther)(object)c;
			return true;
		}
		if (typeof(TOther) == typeof(decimal))
		{
			decimal num = value;
			result = (TOther)(object)num;
			return true;
		}
		if (typeof(TOther) == typeof(ushort))
		{
			ushort num2 = (ushort)((value >= 65535) ? ushort.MaxValue : ((value > 0) ? ((ushort)value) : 0));
			result = (TOther)(object)num2;
			return true;
		}
		if (typeof(TOther) == typeof(uint))
		{
			uint num3 = (uint)((value >= uint.MaxValue) ? uint.MaxValue : ((value > 0) ? value : 0u));
			result = (TOther)(object)num3;
			return true;
		}
		if (typeof(TOther) == typeof(ulong))
		{
			ulong num4 = (ulong)((value <= 0) ? 0 : value);
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
			nuint num5 = (nuint)((value <= 0) ? 0 : value);
			result = (TOther)(object)num5;
			return true;
		}
		result = default(TOther);
		return false;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	static bool INumberBase<long>.TryConvertToTruncating<TOther>(long value, [MaybeNullWhen(false)] out TOther result)
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
			decimal num = value;
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
			result = (TOther)(object)(ulong)value;
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
			nuint num4 = (nuint)value;
			result = (TOther)(object)num4;
			return true;
		}
		result = default(TOther);
		return false;
	}

	public static bool TryParse([NotNullWhen(true)] string? s, IFormatProvider? provider, out long result)
	{
		return TryParse(s, NumberStyles.Integer, provider, out result);
	}

	static long IShiftOperators<long, int, long>.operator <<(long value, int shiftAmount)
	{
		return value << shiftAmount;
	}

	static long IShiftOperators<long, int, long>.operator >>(long value, int shiftAmount)
	{
		return value >> shiftAmount;
	}

	static long IShiftOperators<long, int, long>.operator >>>(long value, int shiftAmount)
	{
		return value >>> shiftAmount;
	}

	public static long Parse(ReadOnlySpan<char> s, IFormatProvider? provider)
	{
		return Parse(s, NumberStyles.Integer, provider);
	}

	public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, out long result)
	{
		return TryParse(s, NumberStyles.Integer, provider, out result);
	}

	static long ISubtractionOperators<long, long, long>.operator -(long left, long right)
	{
		return left - right;
	}

	static long ISubtractionOperators<long, long, long>.operator checked -(long left, long right)
	{
		return checked(left - right);
	}

	static long IUnaryNegationOperators<long, long>.operator -(long value)
	{
		return -value;
	}

	static long IUnaryNegationOperators<long, long>.operator checked -(long value)
	{
		return checked(-value);
	}

	static long IUnaryPlusOperators<long, long>.operator +(long value)
	{
		return value;
	}

	public static long Parse(ReadOnlySpan<byte> utf8Text, NumberStyles style = NumberStyles.Integer, IFormatProvider? provider = null)
	{
		NumberFormatInfo.ValidateParseStyleInteger(style);
		return Number.ParseBinaryInteger<byte, long>(utf8Text, style, NumberFormatInfo.GetInstance(provider));
	}

	public static bool TryParse(ReadOnlySpan<byte> utf8Text, NumberStyles style, IFormatProvider? provider, out long result)
	{
		NumberFormatInfo.ValidateParseStyleInteger(style);
		return Number.TryParseBinaryInteger<byte, long>(utf8Text, style, NumberFormatInfo.GetInstance(provider), out result) == Number.ParsingStatus.OK;
	}

	public static long Parse(ReadOnlySpan<byte> utf8Text, IFormatProvider? provider)
	{
		return Parse(utf8Text, NumberStyles.Integer, provider);
	}

	public static bool TryParse(ReadOnlySpan<byte> utf8Text, IFormatProvider? provider, out long result)
	{
		return TryParse(utf8Text, NumberStyles.Integer, provider, out result);
	}

	static bool IBinaryIntegerParseAndFormatInfo<long>.IsGreaterThanAsUnsigned(long left, long right)
	{
		return (ulong)left > (ulong)right;
	}

	static long IBinaryIntegerParseAndFormatInfo<long>.MultiplyBy10(long value)
	{
		return value * 10;
	}

	static long IBinaryIntegerParseAndFormatInfo<long>.MultiplyBy16(long value)
	{
		return value * 16;
	}
}
