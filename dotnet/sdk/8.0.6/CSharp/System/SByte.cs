using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace System;

[Serializable]
[CLSCompliant(false)]
[TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
public readonly struct SByte : IComparable, IConvertible, ISpanFormattable, IFormattable, IComparable<sbyte>, IEquatable<sbyte>, IBinaryInteger<sbyte>, IBinaryNumber<sbyte>, IBitwiseOperators<sbyte, sbyte, sbyte>, INumber<sbyte>, IComparisonOperators<sbyte, sbyte, bool>, IEqualityOperators<sbyte, sbyte, bool>, IModulusOperators<sbyte, sbyte, sbyte>, INumberBase<sbyte>, IAdditionOperators<sbyte, sbyte, sbyte>, IAdditiveIdentity<sbyte, sbyte>, IDecrementOperators<sbyte>, IDivisionOperators<sbyte, sbyte, sbyte>, IIncrementOperators<sbyte>, IMultiplicativeIdentity<sbyte, sbyte>, IMultiplyOperators<sbyte, sbyte, sbyte>, ISpanParsable<sbyte>, IParsable<sbyte>, ISubtractionOperators<sbyte, sbyte, sbyte>, IUnaryPlusOperators<sbyte, sbyte>, IUnaryNegationOperators<sbyte, sbyte>, IUtf8SpanFormattable, IUtf8SpanParsable<sbyte>, IShiftOperators<sbyte, int, sbyte>, IMinMaxValue<sbyte>, ISignedNumber<sbyte>, IBinaryIntegerParseAndFormatInfo<sbyte>
{
	private readonly sbyte m_value;

	public const sbyte MaxValue = 127;

	public const sbyte MinValue = -128;

	static sbyte IAdditiveIdentity<sbyte, sbyte>.AdditiveIdentity => 0;

	static sbyte IBinaryNumber<sbyte>.AllBitsSet => -1;

	static sbyte IMinMaxValue<sbyte>.MinValue => sbyte.MinValue;

	static sbyte IMinMaxValue<sbyte>.MaxValue => sbyte.MaxValue;

	static sbyte IMultiplicativeIdentity<sbyte, sbyte>.MultiplicativeIdentity => 1;

	static sbyte INumberBase<sbyte>.One => 1;

	static int INumberBase<sbyte>.Radix => 2;

	static sbyte INumberBase<sbyte>.Zero => 0;

	static sbyte ISignedNumber<sbyte>.NegativeOne => -1;

	static bool IBinaryIntegerParseAndFormatInfo<sbyte>.IsSigned => true;

	static int IBinaryIntegerParseAndFormatInfo<sbyte>.MaxDigitCount => 3;

	static int IBinaryIntegerParseAndFormatInfo<sbyte>.MaxHexDigitCount => 2;

	static sbyte IBinaryIntegerParseAndFormatInfo<sbyte>.MaxValueDiv10 => 12;

	static string IBinaryIntegerParseAndFormatInfo<sbyte>.OverflowMessage => SR.Overflow_SByte;

	public int CompareTo(object? obj)
	{
		if (obj == null)
		{
			return 1;
		}
		if (!(obj is sbyte))
		{
			throw new ArgumentException(SR.Arg_MustBeSByte);
		}
		return this - (sbyte)obj;
	}

	public int CompareTo(sbyte value)
	{
		return this - value;
	}

	public override bool Equals([NotNullWhen(true)] object? obj)
	{
		if (!(obj is sbyte))
		{
			return false;
		}
		return this == (sbyte)obj;
	}

	[NonVersionable]
	public bool Equals(sbyte obj)
	{
		return this == obj;
	}

	public override int GetHashCode()
	{
		return this;
	}

	public override string ToString()
	{
		return Number.Int32ToDecStr(this);
	}

	public string ToString([StringSyntax("NumericFormat")] string? format)
	{
		return ToString(format, null);
	}

	public string ToString(IFormatProvider? provider)
	{
		return Number.FormatInt32(this, 0, null, provider);
	}

	public string ToString([StringSyntax("NumericFormat")] string? format, IFormatProvider? provider)
	{
		return Number.FormatInt32(this, 255, format, provider);
	}

	public bool TryFormat(Span<char> destination, out int charsWritten, [StringSyntax("NumericFormat")] ReadOnlySpan<char> format = default(ReadOnlySpan<char>), IFormatProvider? provider = null)
	{
		return Number.TryFormatInt32(this, 255, format, provider, destination, out charsWritten);
	}

	public bool TryFormat(Span<byte> utf8Destination, out int bytesWritten, [StringSyntax("NumericFormat")] ReadOnlySpan<char> format = default(ReadOnlySpan<char>), IFormatProvider? provider = null)
	{
		return Number.TryFormatInt32(this, 255, format, provider, utf8Destination, out bytesWritten);
	}

	public static sbyte Parse(string s)
	{
		return Parse(s, NumberStyles.Integer, null);
	}

	public static sbyte Parse(string s, NumberStyles style)
	{
		return Parse(s, style, null);
	}

	public static sbyte Parse(string s, IFormatProvider? provider)
	{
		return Parse(s, NumberStyles.Integer, provider);
	}

	public static sbyte Parse(string s, NumberStyles style, IFormatProvider? provider)
	{
		if (s == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.s);
		}
		return Parse(s.AsSpan(), style, provider);
	}

	public static sbyte Parse(ReadOnlySpan<char> s, NumberStyles style = NumberStyles.Integer, IFormatProvider? provider = null)
	{
		NumberFormatInfo.ValidateParseStyleInteger(style);
		return Number.ParseBinaryInteger<char, sbyte>(s, style, NumberFormatInfo.GetInstance(provider));
	}

	public static bool TryParse([NotNullWhen(true)] string? s, out sbyte result)
	{
		return TryParse(s, NumberStyles.Integer, null, out result);
	}

	public static bool TryParse(ReadOnlySpan<char> s, out sbyte result)
	{
		return TryParse(s, NumberStyles.Integer, null, out result);
	}

	public static bool TryParse(ReadOnlySpan<byte> utf8Text, out sbyte result)
	{
		return TryParse(utf8Text, NumberStyles.Integer, null, out result);
	}

	public static bool TryParse([NotNullWhen(true)] string? s, NumberStyles style, IFormatProvider? provider, out sbyte result)
	{
		NumberFormatInfo.ValidateParseStyleInteger(style);
		if (s == null)
		{
			result = 0;
			return false;
		}
		return Number.TryParseBinaryInteger<char, sbyte>(s.AsSpan(), style, NumberFormatInfo.GetInstance(provider), out result) == Number.ParsingStatus.OK;
	}

	public static bool TryParse(ReadOnlySpan<char> s, NumberStyles style, IFormatProvider? provider, out sbyte result)
	{
		NumberFormatInfo.ValidateParseStyleInteger(style);
		return Number.TryParseBinaryInteger<char, sbyte>(s, style, NumberFormatInfo.GetInstance(provider), out result) == Number.ParsingStatus.OK;
	}

	public TypeCode GetTypeCode()
	{
		return TypeCode.SByte;
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
		return this;
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
		return this;
	}

	uint IConvertible.ToUInt32(IFormatProvider provider)
	{
		return Convert.ToUInt32(this);
	}

	long IConvertible.ToInt64(IFormatProvider provider)
	{
		return Convert.ToInt64(this);
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
		throw new InvalidCastException(SR.Format(SR.InvalidCast_FromTo, "SByte", "DateTime"));
	}

	object IConvertible.ToType(Type type, IFormatProvider provider)
	{
		return Convert.DefaultToType(this, type, provider);
	}

	static sbyte IAdditionOperators<sbyte, sbyte, sbyte>.operator +(sbyte left, sbyte right)
	{
		return (sbyte)(left + right);
	}

	static sbyte IAdditionOperators<sbyte, sbyte, sbyte>.operator checked +(sbyte left, sbyte right)
	{
		return checked((sbyte)(left + right));
	}

	public static (sbyte Quotient, sbyte Remainder) DivRem(sbyte left, sbyte right)
	{
		return Math.DivRem(left, right);
	}

	public static sbyte LeadingZeroCount(sbyte value)
	{
		return (sbyte)(BitOperations.LeadingZeroCount((byte)value) - 24);
	}

	public static sbyte PopCount(sbyte value)
	{
		return (sbyte)BitOperations.PopCount((byte)value);
	}

	public static sbyte RotateLeft(sbyte value, int rotateAmount)
	{
		return (sbyte)((value << (rotateAmount & 7)) | ((byte)value >> ((8 - rotateAmount) & 7)));
	}

	public static sbyte RotateRight(sbyte value, int rotateAmount)
	{
		return (sbyte)(((byte)value >> (rotateAmount & 7)) | (value << ((8 - rotateAmount) & 7)));
	}

	public static sbyte TrailingZeroCount(sbyte value)
	{
		return (sbyte)(BitOperations.TrailingZeroCount(value << 24) - 24);
	}

	static bool IBinaryInteger<sbyte>.TryReadBigEndian(ReadOnlySpan<byte> source, bool isUnsigned, out sbyte value)
	{
		sbyte b = 0;
		if (source.Length != 0)
		{
			sbyte b2 = (sbyte)source[0];
			b2 >>= 31;
			isUnsigned = isUnsigned || b2 == 0;
			if (isUnsigned && IsNegative(b2))
			{
				value = b;
				return false;
			}
			if (source.Length > 1)
			{
				if (source.Slice(0, source.Length - 1).ContainsAnyExcept((byte)b2))
				{
					value = b;
					return false;
				}
				if (isUnsigned == IsNegative((sbyte)source[source.Length - 1]))
				{
					value = b;
					return false;
				}
			}
			b = (sbyte)Unsafe.Add(ref MemoryMarshal.GetReference(source), source.Length - 1);
		}
		value = b;
		return true;
	}

	static bool IBinaryInteger<sbyte>.TryReadLittleEndian(ReadOnlySpan<byte> source, bool isUnsigned, out sbyte value)
	{
		sbyte b = 0;
		if (source.Length != 0)
		{
			sbyte b2 = (sbyte)source[source.Length - 1];
			b2 >>= 31;
			isUnsigned = isUnsigned || b2 == 0;
			if (isUnsigned && IsNegative(b2))
			{
				value = b;
				return false;
			}
			if (source.Length > 1)
			{
				if (source.Slice(1, source.Length - 1).ContainsAnyExcept((byte)b2))
				{
					value = b;
					return false;
				}
				if (isUnsigned == IsNegative((sbyte)source[0]))
				{
					value = b;
					return false;
				}
			}
			b = (sbyte)MemoryMarshal.GetReference(source);
		}
		value = b;
		return true;
	}

	int IBinaryInteger<sbyte>.GetShortestBitLength()
	{
		sbyte b = this;
		if (b >= 0)
		{
			return 8 - LeadingZeroCount(b);
		}
		return 9 - LeadingZeroCount((sbyte)(~b));
	}

	int IBinaryInteger<sbyte>.GetByteCount()
	{
		return 1;
	}

	bool IBinaryInteger<sbyte>.TryWriteBigEndian(Span<byte> destination, out int bytesWritten)
	{
		if (destination.Length >= 1)
		{
			sbyte value = this;
			Unsafe.WriteUnaligned(ref MemoryMarshal.GetReference(destination), value);
			bytesWritten = 1;
			return true;
		}
		bytesWritten = 0;
		return false;
	}

	bool IBinaryInteger<sbyte>.TryWriteLittleEndian(Span<byte> destination, out int bytesWritten)
	{
		if (destination.Length >= 1)
		{
			sbyte value = this;
			Unsafe.WriteUnaligned(ref MemoryMarshal.GetReference(destination), value);
			bytesWritten = 1;
			return true;
		}
		bytesWritten = 0;
		return false;
	}

	public static bool IsPow2(sbyte value)
	{
		return BitOperations.IsPow2(value);
	}

	public static sbyte Log2(sbyte value)
	{
		if (value < 0)
		{
			ThrowHelper.ThrowValueArgumentOutOfRange_NeedNonNegNumException();
		}
		return (sbyte)BitOperations.Log2((byte)value);
	}

	static sbyte IBitwiseOperators<sbyte, sbyte, sbyte>.operator &(sbyte left, sbyte right)
	{
		return (sbyte)(left & right);
	}

	static sbyte IBitwiseOperators<sbyte, sbyte, sbyte>.operator |(sbyte left, sbyte right)
	{
		return (sbyte)(left | right);
	}

	static sbyte IBitwiseOperators<sbyte, sbyte, sbyte>.operator ^(sbyte left, sbyte right)
	{
		return (sbyte)(left ^ right);
	}

	static sbyte IBitwiseOperators<sbyte, sbyte, sbyte>.operator ~(sbyte value)
	{
		return (sbyte)(~value);
	}

	static bool IComparisonOperators<sbyte, sbyte, bool>.operator <(sbyte left, sbyte right)
	{
		return left < right;
	}

	static bool IComparisonOperators<sbyte, sbyte, bool>.operator <=(sbyte left, sbyte right)
	{
		return left <= right;
	}

	static bool IComparisonOperators<sbyte, sbyte, bool>.operator >(sbyte left, sbyte right)
	{
		return left > right;
	}

	static bool IComparisonOperators<sbyte, sbyte, bool>.operator >=(sbyte left, sbyte right)
	{
		return left >= right;
	}

	static sbyte IDecrementOperators<sbyte>.operator --(sbyte value)
	{
		return --value;
	}

	static sbyte IDecrementOperators<sbyte>.operator checked --(sbyte value)
	{
		return value = checked((sbyte)(value - 1));
	}

	static sbyte IDivisionOperators<sbyte, sbyte, sbyte>.operator /(sbyte left, sbyte right)
	{
		return (sbyte)(left / right);
	}

	static bool IEqualityOperators<sbyte, sbyte, bool>.operator ==(sbyte left, sbyte right)
	{
		return left == right;
	}

	static bool IEqualityOperators<sbyte, sbyte, bool>.operator !=(sbyte left, sbyte right)
	{
		return left != right;
	}

	static sbyte IIncrementOperators<sbyte>.operator ++(sbyte value)
	{
		return ++value;
	}

	static sbyte IIncrementOperators<sbyte>.operator checked ++(sbyte value)
	{
		return value = checked((sbyte)(value + 1));
	}

	static sbyte IModulusOperators<sbyte, sbyte, sbyte>.operator %(sbyte left, sbyte right)
	{
		return (sbyte)(left % right);
	}

	static sbyte IMultiplyOperators<sbyte, sbyte, sbyte>.operator *(sbyte left, sbyte right)
	{
		return (sbyte)(left * right);
	}

	static sbyte IMultiplyOperators<sbyte, sbyte, sbyte>.operator checked *(sbyte left, sbyte right)
	{
		return checked((sbyte)(left * right));
	}

	public static sbyte Clamp(sbyte value, sbyte min, sbyte max)
	{
		return Math.Clamp(value, min, max);
	}

	public static sbyte CopySign(sbyte value, sbyte sign)
	{
		sbyte b = value;
		if (b < 0)
		{
			b = (sbyte)(-b);
		}
		if (sign >= 0)
		{
			if (b < 0)
			{
				Math.ThrowNegateTwosCompOverflow();
			}
			return b;
		}
		return (sbyte)(-b);
	}

	public static sbyte Max(sbyte x, sbyte y)
	{
		return Math.Max(x, y);
	}

	static sbyte INumber<sbyte>.MaxNumber(sbyte x, sbyte y)
	{
		return Max(x, y);
	}

	public static sbyte Min(sbyte x, sbyte y)
	{
		return Math.Min(x, y);
	}

	static sbyte INumber<sbyte>.MinNumber(sbyte x, sbyte y)
	{
		return Min(x, y);
	}

	public static int Sign(sbyte value)
	{
		return Math.Sign(value);
	}

	public static sbyte Abs(sbyte value)
	{
		return Math.Abs(value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static sbyte CreateChecked<TOther>(TOther value) where TOther : INumberBase<TOther>
	{
		if (typeof(TOther) == typeof(sbyte))
		{
			return (sbyte)(object)value;
		}
		if (!TryConvertFromChecked(value, out var result) && !TOther.TryConvertToChecked<sbyte>(value, out result))
		{
			ThrowHelper.ThrowNotSupportedException();
		}
		return result;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static sbyte CreateSaturating<TOther>(TOther value) where TOther : INumberBase<TOther>
	{
		if (typeof(TOther) == typeof(sbyte))
		{
			return (sbyte)(object)value;
		}
		if (!TryConvertFromSaturating(value, out var result) && !TOther.TryConvertToSaturating<sbyte>(value, out result))
		{
			ThrowHelper.ThrowNotSupportedException();
		}
		return result;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static sbyte CreateTruncating<TOther>(TOther value) where TOther : INumberBase<TOther>
	{
		if (typeof(TOther) == typeof(sbyte))
		{
			return (sbyte)(object)value;
		}
		if (!TryConvertFromTruncating(value, out var result) && !TOther.TryConvertToTruncating<sbyte>(value, out result))
		{
			ThrowHelper.ThrowNotSupportedException();
		}
		return result;
	}

	static bool INumberBase<sbyte>.IsCanonical(sbyte value)
	{
		return true;
	}

	static bool INumberBase<sbyte>.IsComplexNumber(sbyte value)
	{
		return false;
	}

	public static bool IsEvenInteger(sbyte value)
	{
		return (value & 1) == 0;
	}

	static bool INumberBase<sbyte>.IsFinite(sbyte value)
	{
		return true;
	}

	static bool INumberBase<sbyte>.IsImaginaryNumber(sbyte value)
	{
		return false;
	}

	static bool INumberBase<sbyte>.IsInfinity(sbyte value)
	{
		return false;
	}

	static bool INumberBase<sbyte>.IsNaN(sbyte value)
	{
		return false;
	}

	static bool INumberBase<sbyte>.IsInteger(sbyte value)
	{
		return true;
	}

	public static bool IsNegative(sbyte value)
	{
		return value < 0;
	}

	static bool INumberBase<sbyte>.IsNegativeInfinity(sbyte value)
	{
		return false;
	}

	static bool INumberBase<sbyte>.IsNormal(sbyte value)
	{
		return value != 0;
	}

	public static bool IsOddInteger(sbyte value)
	{
		return (value & 1) != 0;
	}

	public static bool IsPositive(sbyte value)
	{
		return value >= 0;
	}

	static bool INumberBase<sbyte>.IsPositiveInfinity(sbyte value)
	{
		return false;
	}

	static bool INumberBase<sbyte>.IsRealNumber(sbyte value)
	{
		return true;
	}

	static bool INumberBase<sbyte>.IsSubnormal(sbyte value)
	{
		return false;
	}

	static bool INumberBase<sbyte>.IsZero(sbyte value)
	{
		return value == 0;
	}

	public static sbyte MaxMagnitude(sbyte x, sbyte y)
	{
		sbyte b = x;
		if (b < 0)
		{
			b = (sbyte)(-b);
			if (b < 0)
			{
				return x;
			}
		}
		sbyte b2 = y;
		if (b2 < 0)
		{
			b2 = (sbyte)(-b2);
			if (b2 < 0)
			{
				return y;
			}
		}
		if (b > b2)
		{
			return x;
		}
		if (b == b2)
		{
			if (!IsNegative(x))
			{
				return x;
			}
			return y;
		}
		return y;
	}

	static sbyte INumberBase<sbyte>.MaxMagnitudeNumber(sbyte x, sbyte y)
	{
		return MaxMagnitude(x, y);
	}

	public static sbyte MinMagnitude(sbyte x, sbyte y)
	{
		sbyte b = x;
		if (b < 0)
		{
			b = (sbyte)(-b);
			if (b < 0)
			{
				return y;
			}
		}
		sbyte b2 = y;
		if (b2 < 0)
		{
			b2 = (sbyte)(-b2);
			if (b2 < 0)
			{
				return x;
			}
		}
		if (b < b2)
		{
			return x;
		}
		if (b == b2)
		{
			if (!IsNegative(x))
			{
				return y;
			}
			return x;
		}
		return y;
	}

	static sbyte INumberBase<sbyte>.MinMagnitudeNumber(sbyte x, sbyte y)
	{
		return MinMagnitude(x, y);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	static bool INumberBase<sbyte>.TryConvertFromChecked<TOther>(TOther value, out sbyte result)
	{
		return TryConvertFromChecked(value, out result);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static bool TryConvertFromChecked<TOther>(TOther value, out sbyte result) where TOther : INumberBase<TOther>
	{
		checked
		{
			if (typeof(TOther) == typeof(double))
			{
				double num = (double)(object)value;
				result = (sbyte)num;
				return true;
			}
			if (typeof(TOther) == typeof(Half))
			{
				Half half = (Half)(object)value;
				result = (sbyte)half;
				return true;
			}
			if (typeof(TOther) == typeof(short))
			{
				short num2 = (short)(object)value;
				result = (sbyte)num2;
				return true;
			}
			if (typeof(TOther) == typeof(int))
			{
				int num3 = (int)(object)value;
				result = (sbyte)num3;
				return true;
			}
			if (typeof(TOther) == typeof(long))
			{
				long num4 = (long)(object)value;
				result = (sbyte)num4;
				return true;
			}
			if (typeof(TOther) == typeof(Int128))
			{
				Int128 @int = (Int128)(object)value;
				result = (sbyte)@int;
				return true;
			}
			if (typeof(TOther) == typeof(nint))
			{
				nint num5 = (nint)(object)value;
				result = (sbyte)num5;
				return true;
			}
			if (typeof(TOther) == typeof(float))
			{
				float num6 = (float)(object)value;
				result = (sbyte)num6;
				return true;
			}
			result = 0;
			return false;
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	static bool INumberBase<sbyte>.TryConvertFromSaturating<TOther>(TOther value, out sbyte result)
	{
		return TryConvertFromSaturating(value, out result);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static bool TryConvertFromSaturating<TOther>(TOther value, out sbyte result) where TOther : INumberBase<TOther>
	{
		if (typeof(TOther) == typeof(double))
		{
			double num = (double)(object)value;
			result = ((num >= 127.0) ? sbyte.MaxValue : ((num <= -128.0) ? sbyte.MinValue : ((sbyte)num)));
			return true;
		}
		if (typeof(TOther) == typeof(Half))
		{
			Half half = (Half)(object)value;
			result = ((half >= sbyte.MaxValue) ? sbyte.MaxValue : ((half <= sbyte.MinValue) ? sbyte.MinValue : ((sbyte)half)));
			return true;
		}
		if (typeof(TOther) == typeof(short))
		{
			short num2 = (short)(object)value;
			result = ((num2 >= 127) ? sbyte.MaxValue : ((num2 <= -128) ? sbyte.MinValue : ((sbyte)num2)));
			return true;
		}
		if (typeof(TOther) == typeof(int))
		{
			int num3 = (int)(object)value;
			result = ((num3 >= 127) ? sbyte.MaxValue : ((num3 <= -128) ? sbyte.MinValue : ((sbyte)num3)));
			return true;
		}
		if (typeof(TOther) == typeof(long))
		{
			long num4 = (long)(object)value;
			result = ((num4 >= 127) ? sbyte.MaxValue : ((num4 <= -128) ? sbyte.MinValue : ((sbyte)num4)));
			return true;
		}
		if (typeof(TOther) == typeof(Int128))
		{
			Int128 @int = (Int128)(object)value;
			result = ((@int >= sbyte.MaxValue) ? sbyte.MaxValue : ((@int <= sbyte.MinValue) ? sbyte.MinValue : ((sbyte)@int)));
			return true;
		}
		if (typeof(TOther) == typeof(nint))
		{
			nint num5 = (nint)(object)value;
			result = ((num5 >= 127) ? sbyte.MaxValue : ((num5 <= -128) ? sbyte.MinValue : ((sbyte)num5)));
			return true;
		}
		if (typeof(TOther) == typeof(float))
		{
			float num6 = (float)(object)value;
			result = ((num6 >= 127f) ? sbyte.MaxValue : ((num6 <= -128f) ? sbyte.MinValue : ((sbyte)num6)));
			return true;
		}
		result = 0;
		return false;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	static bool INumberBase<sbyte>.TryConvertFromTruncating<TOther>(TOther value, out sbyte result)
	{
		return TryConvertFromTruncating(value, out result);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static bool TryConvertFromTruncating<TOther>(TOther value, out sbyte result) where TOther : INumberBase<TOther>
	{
		if (typeof(TOther) == typeof(double))
		{
			double num = (double)(object)value;
			result = ((num >= 127.0) ? sbyte.MaxValue : ((num <= -128.0) ? sbyte.MinValue : ((sbyte)num)));
			return true;
		}
		if (typeof(TOther) == typeof(Half))
		{
			Half half = (Half)(object)value;
			result = ((half >= sbyte.MaxValue) ? sbyte.MaxValue : ((half <= sbyte.MinValue) ? sbyte.MinValue : ((sbyte)half)));
			return true;
		}
		if (typeof(TOther) == typeof(short))
		{
			short num2 = (short)(object)value;
			result = (sbyte)num2;
			return true;
		}
		if (typeof(TOther) == typeof(int))
		{
			int num3 = (int)(object)value;
			result = (sbyte)num3;
			return true;
		}
		if (typeof(TOther) == typeof(long))
		{
			long num4 = (long)(object)value;
			result = (sbyte)num4;
			return true;
		}
		if (typeof(TOther) == typeof(Int128))
		{
			Int128 @int = (Int128)(object)value;
			result = (sbyte)@int;
			return true;
		}
		if (typeof(TOther) == typeof(nint))
		{
			nint num5 = (nint)(object)value;
			result = (sbyte)num5;
			return true;
		}
		if (typeof(TOther) == typeof(float))
		{
			float num6 = (float)(object)value;
			result = ((num6 >= 127f) ? sbyte.MaxValue : ((num6 <= -128f) ? sbyte.MinValue : ((sbyte)num6)));
			return true;
		}
		result = 0;
		return false;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	static bool INumberBase<sbyte>.TryConvertToChecked<TOther>(sbyte value, [MaybeNullWhen(false)] out TOther result)
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
	static bool INumberBase<sbyte>.TryConvertToSaturating<TOther>(sbyte value, [MaybeNullWhen(false)] out TOther result)
	{
		if (typeof(TOther) == typeof(byte))
		{
			byte b = (byte)((value > 0) ? ((byte)value) : 0);
			result = (TOther)(object)b;
			return true;
		}
		if (typeof(TOther) == typeof(char))
		{
			char c = ((value > 0) ? ((char)value) : '\0');
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
			ushort num2 = (ushort)((value > 0) ? ((ushort)value) : 0);
			result = (TOther)(object)num2;
			return true;
		}
		if (typeof(TOther) == typeof(uint))
		{
			uint num3 = (uint)((value > 0) ? value : 0);
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
	static bool INumberBase<sbyte>.TryConvertToTruncating<TOther>(sbyte value, [MaybeNullWhen(false)] out TOther result)
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
			result = (TOther)(object)(uint)value;
			return true;
		}
		if (typeof(TOther) == typeof(ulong))
		{
			ulong num3 = (ulong)value;
			result = (TOther)(object)num3;
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

	public static bool TryParse([NotNullWhen(true)] string? s, IFormatProvider? provider, out sbyte result)
	{
		return TryParse(s, NumberStyles.Integer, provider, out result);
	}

	static sbyte IShiftOperators<sbyte, int, sbyte>.operator <<(sbyte value, int shiftAmount)
	{
		return (sbyte)(value << shiftAmount);
	}

	static sbyte IShiftOperators<sbyte, int, sbyte>.operator >>(sbyte value, int shiftAmount)
	{
		return (sbyte)(value >> shiftAmount);
	}

	static sbyte IShiftOperators<sbyte, int, sbyte>.operator >>>(sbyte value, int shiftAmount)
	{
		return (sbyte)((byte)value >>> shiftAmount);
	}

	public static sbyte Parse(ReadOnlySpan<char> s, IFormatProvider? provider)
	{
		return Parse(s, NumberStyles.Integer, provider);
	}

	public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, out sbyte result)
	{
		return TryParse(s, NumberStyles.Integer, provider, out result);
	}

	static sbyte ISubtractionOperators<sbyte, sbyte, sbyte>.operator -(sbyte left, sbyte right)
	{
		return (sbyte)(left - right);
	}

	static sbyte ISubtractionOperators<sbyte, sbyte, sbyte>.operator checked -(sbyte left, sbyte right)
	{
		return checked((sbyte)(left - right));
	}

	static sbyte IUnaryNegationOperators<sbyte, sbyte>.operator -(sbyte value)
	{
		return (sbyte)(-value);
	}

	static sbyte IUnaryNegationOperators<sbyte, sbyte>.operator checked -(sbyte value)
	{
		return checked((sbyte)(-value));
	}

	static sbyte IUnaryPlusOperators<sbyte, sbyte>.operator +(sbyte value)
	{
		return value;
	}

	public static sbyte Parse(ReadOnlySpan<byte> utf8Text, NumberStyles style = NumberStyles.Integer, IFormatProvider? provider = null)
	{
		NumberFormatInfo.ValidateParseStyleInteger(style);
		return Number.ParseBinaryInteger<byte, sbyte>(utf8Text, style, NumberFormatInfo.GetInstance(provider));
	}

	public static bool TryParse(ReadOnlySpan<byte> utf8Text, NumberStyles style, IFormatProvider? provider, out sbyte result)
	{
		NumberFormatInfo.ValidateParseStyleInteger(style);
		return Number.TryParseBinaryInteger<byte, sbyte>(utf8Text, style, NumberFormatInfo.GetInstance(provider), out result) == Number.ParsingStatus.OK;
	}

	public static sbyte Parse(ReadOnlySpan<byte> utf8Text, IFormatProvider? provider)
	{
		return Parse(utf8Text, NumberStyles.Integer, provider);
	}

	public static bool TryParse(ReadOnlySpan<byte> utf8Text, IFormatProvider? provider, out sbyte result)
	{
		return TryParse(utf8Text, NumberStyles.Integer, provider, out result);
	}

	static bool IBinaryIntegerParseAndFormatInfo<sbyte>.IsGreaterThanAsUnsigned(sbyte left, sbyte right)
	{
		return (byte)left > (byte)right;
	}

	static sbyte IBinaryIntegerParseAndFormatInfo<sbyte>.MultiplyBy10(sbyte value)
	{
		return (sbyte)(value * 10);
	}

	static sbyte IBinaryIntegerParseAndFormatInfo<sbyte>.MultiplyBy16(sbyte value)
	{
		return (sbyte)(value * 16);
	}
}
