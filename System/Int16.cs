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
public readonly struct Int16 : IComparable, IConvertible, ISpanFormattable, IFormattable, IComparable<short>, IEquatable<short>, IBinaryInteger<short>, IBinaryNumber<short>, IBitwiseOperators<short, short, short>, INumber<short>, IComparisonOperators<short, short, bool>, IEqualityOperators<short, short, bool>, IModulusOperators<short, short, short>, INumberBase<short>, IAdditionOperators<short, short, short>, IAdditiveIdentity<short, short>, IDecrementOperators<short>, IDivisionOperators<short, short, short>, IIncrementOperators<short>, IMultiplicativeIdentity<short, short>, IMultiplyOperators<short, short, short>, ISpanParsable<short>, IParsable<short>, ISubtractionOperators<short, short, short>, IUnaryPlusOperators<short, short>, IUnaryNegationOperators<short, short>, IUtf8SpanFormattable, IUtf8SpanParsable<short>, IShiftOperators<short, int, short>, IMinMaxValue<short>, ISignedNumber<short>, IBinaryIntegerParseAndFormatInfo<short>
{
	private readonly short m_value;

	public const short MaxValue = 32767;

	public const short MinValue = -32768;

	static short IAdditiveIdentity<short, short>.AdditiveIdentity => 0;

	static short IBinaryNumber<short>.AllBitsSet => -1;

	static short IMinMaxValue<short>.MinValue => short.MinValue;

	static short IMinMaxValue<short>.MaxValue => short.MaxValue;

	static short IMultiplicativeIdentity<short, short>.MultiplicativeIdentity => 1;

	static short INumberBase<short>.One => 1;

	static int INumberBase<short>.Radix => 2;

	static short INumberBase<short>.Zero => 0;

	static short ISignedNumber<short>.NegativeOne => -1;

	static bool IBinaryIntegerParseAndFormatInfo<short>.IsSigned => true;

	static int IBinaryIntegerParseAndFormatInfo<short>.MaxDigitCount => 5;

	static int IBinaryIntegerParseAndFormatInfo<short>.MaxHexDigitCount => 4;

	static short IBinaryIntegerParseAndFormatInfo<short>.MaxValueDiv10 => 3276;

	static string IBinaryIntegerParseAndFormatInfo<short>.OverflowMessage => SR.Overflow_Int16;

	public int CompareTo(object? value)
	{
		if (value == null)
		{
			return 1;
		}
		if (value is short)
		{
			return this - (short)value;
		}
		throw new ArgumentException(SR.Arg_MustBeInt16);
	}

	public int CompareTo(short value)
	{
		return this - value;
	}

	public override bool Equals([NotNullWhen(true)] object? obj)
	{
		if (!(obj is short))
		{
			return false;
		}
		return this == (short)obj;
	}

	[NonVersionable]
	public bool Equals(short obj)
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

	public string ToString(IFormatProvider? provider)
	{
		return Number.FormatInt32(this, 0, null, provider);
	}

	public string ToString([StringSyntax("NumericFormat")] string? format)
	{
		return ToString(format, null);
	}

	public string ToString([StringSyntax("NumericFormat")] string? format, IFormatProvider? provider)
	{
		return Number.FormatInt32(this, 65535, format, provider);
	}

	public bool TryFormat(Span<char> destination, out int charsWritten, [StringSyntax("NumericFormat")] ReadOnlySpan<char> format = default(ReadOnlySpan<char>), IFormatProvider? provider = null)
	{
		return Number.TryFormatInt32(this, 65535, format, provider, destination, out charsWritten);
	}

	public bool TryFormat(Span<byte> utf8Destination, out int bytesWritten, [StringSyntax("NumericFormat")] ReadOnlySpan<char> format = default(ReadOnlySpan<char>), IFormatProvider? provider = null)
	{
		return Number.TryFormatInt32(this, 65535, format, provider, utf8Destination, out bytesWritten);
	}

	public static short Parse(string s)
	{
		return Parse(s, NumberStyles.Integer, null);
	}

	public static short Parse(string s, NumberStyles style)
	{
		return Parse(s, style, null);
	}

	public static short Parse(string s, IFormatProvider? provider)
	{
		return Parse(s, NumberStyles.Integer, provider);
	}

	public static short Parse(string s, NumberStyles style, IFormatProvider? provider)
	{
		if (s == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.s);
		}
		return Parse(s.AsSpan(), style, provider);
	}

	public static short Parse(ReadOnlySpan<char> s, NumberStyles style = NumberStyles.Integer, IFormatProvider? provider = null)
	{
		NumberFormatInfo.ValidateParseStyleInteger(style);
		return Number.ParseBinaryInteger<char, short>(s, style, NumberFormatInfo.GetInstance(provider));
	}

	public static bool TryParse([NotNullWhen(true)] string? s, out short result)
	{
		return TryParse(s, NumberStyles.Integer, null, out result);
	}

	public static bool TryParse(ReadOnlySpan<char> s, out short result)
	{
		return TryParse(s, NumberStyles.Integer, null, out result);
	}

	public static bool TryParse(ReadOnlySpan<byte> utf8Text, out short result)
	{
		return TryParse(utf8Text, NumberStyles.Integer, null, out result);
	}

	public static bool TryParse([NotNullWhen(true)] string? s, NumberStyles style, IFormatProvider? provider, out short result)
	{
		NumberFormatInfo.ValidateParseStyleInteger(style);
		if (s == null)
		{
			result = 0;
			return false;
		}
		return Number.TryParseBinaryInteger<char, short>(s.AsSpan(), style, NumberFormatInfo.GetInstance(provider), out result) == Number.ParsingStatus.OK;
	}

	public static bool TryParse(ReadOnlySpan<char> s, NumberStyles style, IFormatProvider? provider, out short result)
	{
		NumberFormatInfo.ValidateParseStyleInteger(style);
		return Number.TryParseBinaryInteger<char, short>(s, style, NumberFormatInfo.GetInstance(provider), out result) == Number.ParsingStatus.OK;
	}

	public TypeCode GetTypeCode()
	{
		return TypeCode.Int16;
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
		return this;
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
		throw new InvalidCastException(SR.Format(SR.InvalidCast_FromTo, "Int16", "DateTime"));
	}

	object IConvertible.ToType(Type type, IFormatProvider provider)
	{
		return Convert.DefaultToType(this, type, provider);
	}

	static short IAdditionOperators<short, short, short>.operator +(short left, short right)
	{
		return (short)(left + right);
	}

	static short IAdditionOperators<short, short, short>.operator checked +(short left, short right)
	{
		return checked((short)(left + right));
	}

	public static (short Quotient, short Remainder) DivRem(short left, short right)
	{
		return Math.DivRem(left, right);
	}

	public static short LeadingZeroCount(short value)
	{
		return (short)(BitOperations.LeadingZeroCount((ushort)value) - 16);
	}

	public static short PopCount(short value)
	{
		return (short)BitOperations.PopCount((ushort)value);
	}

	public static short RotateLeft(short value, int rotateAmount)
	{
		return (short)((value << (rotateAmount & 0xF)) | ((ushort)value >> ((16 - rotateAmount) & 0xF)));
	}

	public static short RotateRight(short value, int rotateAmount)
	{
		return (short)(((ushort)value >> (rotateAmount & 0xF)) | (value << ((16 - rotateAmount) & 0xF)));
	}

	public static short TrailingZeroCount(short value)
	{
		return (byte)(BitOperations.TrailingZeroCount(value << 16) - 16);
	}

	static bool IBinaryInteger<short>.TryReadBigEndian(ReadOnlySpan<byte> source, bool isUnsigned, out short value)
	{
		short num = 0;
		if (source.Length != 0)
		{
			sbyte b = (sbyte)source[0];
			b >>= 31;
			isUnsigned = isUnsigned || b == 0;
			if (isUnsigned && sbyte.IsNegative(b) && source.Length >= 2)
			{
				value = num;
				return false;
			}
			if (source.Length > 2)
			{
				if (source.Slice(0, source.Length - 2).ContainsAnyExcept((byte)b))
				{
					value = num;
					return false;
				}
				if (isUnsigned == sbyte.IsNegative((sbyte)source[source.Length - 2]))
				{
					value = num;
					return false;
				}
			}
			ref byte reference = ref MemoryMarshal.GetReference(source);
			if (source.Length < 2)
			{
				num = ((!isUnsigned) ? ((short)(sbyte)reference) : ((short)reference));
			}
			else
			{
				num = Unsafe.ReadUnaligned<short>(ref Unsafe.Add(ref reference, source.Length - 2));
				_ = BitConverter.IsLittleEndian;
				num = BinaryPrimitives.ReverseEndianness(num);
			}
		}
		value = num;
		return true;
	}

	static bool IBinaryInteger<short>.TryReadLittleEndian(ReadOnlySpan<byte> source, bool isUnsigned, out short value)
	{
		short num = 0;
		if (source.Length != 0)
		{
			sbyte b = (sbyte)source[source.Length - 1];
			b >>= 31;
			isUnsigned = isUnsigned || b == 0;
			if (isUnsigned && sbyte.IsNegative(b) && source.Length >= 2)
			{
				value = num;
				return false;
			}
			if (source.Length > 2)
			{
				if (source.Slice(2, source.Length - 2).ContainsAnyExcept((byte)b))
				{
					value = num;
					return false;
				}
				if (isUnsigned == sbyte.IsNegative((sbyte)source[1]))
				{
					value = num;
					return false;
				}
			}
			ref byte reference = ref MemoryMarshal.GetReference(source);
			if (source.Length >= 2)
			{
				num = Unsafe.ReadUnaligned<short>(ref reference);
				if (BitConverter.IsLittleEndian)
				{
					goto IL_00b4;
				}
			}
			num = ((!isUnsigned) ? ((short)(sbyte)reference) : ((short)reference));
		}
		goto IL_00b4;
		IL_00b4:
		value = num;
		return true;
	}

	int IBinaryInteger<short>.GetShortestBitLength()
	{
		short num = this;
		if (num >= 0)
		{
			return 16 - LeadingZeroCount(num);
		}
		return 17 - LeadingZeroCount((short)(~num));
	}

	int IBinaryInteger<short>.GetByteCount()
	{
		return 2;
	}

	bool IBinaryInteger<short>.TryWriteBigEndian(Span<byte> destination, out int bytesWritten)
	{
		if (destination.Length >= 2)
		{
			if (!BitConverter.IsLittleEndian)
			{
			}
			short value = BinaryPrimitives.ReverseEndianness(this);
			Unsafe.WriteUnaligned(ref MemoryMarshal.GetReference(destination), value);
			bytesWritten = 2;
			return true;
		}
		bytesWritten = 0;
		return false;
	}

	bool IBinaryInteger<short>.TryWriteLittleEndian(Span<byte> destination, out int bytesWritten)
	{
		if (destination.Length >= 2)
		{
			if (!BitConverter.IsLittleEndian)
			{
			}
			short value = this;
			Unsafe.WriteUnaligned(ref MemoryMarshal.GetReference(destination), value);
			bytesWritten = 2;
			return true;
		}
		bytesWritten = 0;
		return false;
	}

	public static bool IsPow2(short value)
	{
		return BitOperations.IsPow2(value);
	}

	public static short Log2(short value)
	{
		if (value < 0)
		{
			ThrowHelper.ThrowValueArgumentOutOfRange_NeedNonNegNumException();
		}
		return (short)BitOperations.Log2((ushort)value);
	}

	static short IBitwiseOperators<short, short, short>.operator &(short left, short right)
	{
		return (short)(left & right);
	}

	static short IBitwiseOperators<short, short, short>.operator |(short left, short right)
	{
		return (short)(left | right);
	}

	static short IBitwiseOperators<short, short, short>.operator ^(short left, short right)
	{
		return (short)(left ^ right);
	}

	static short IBitwiseOperators<short, short, short>.operator ~(short value)
	{
		return (short)(~value);
	}

	static bool IComparisonOperators<short, short, bool>.operator <(short left, short right)
	{
		return left < right;
	}

	static bool IComparisonOperators<short, short, bool>.operator <=(short left, short right)
	{
		return left <= right;
	}

	static bool IComparisonOperators<short, short, bool>.operator >(short left, short right)
	{
		return left > right;
	}

	static bool IComparisonOperators<short, short, bool>.operator >=(short left, short right)
	{
		return left >= right;
	}

	static short IDecrementOperators<short>.operator --(short value)
	{
		return --value;
	}

	static short IDecrementOperators<short>.operator checked --(short value)
	{
		return value = checked((short)(value - 1));
	}

	static short IDivisionOperators<short, short, short>.operator /(short left, short right)
	{
		return (short)(left / right);
	}

	static bool IEqualityOperators<short, short, bool>.operator ==(short left, short right)
	{
		return left == right;
	}

	static bool IEqualityOperators<short, short, bool>.operator !=(short left, short right)
	{
		return left != right;
	}

	static short IIncrementOperators<short>.operator ++(short value)
	{
		return ++value;
	}

	static short IIncrementOperators<short>.operator checked ++(short value)
	{
		return value = checked((short)(value + 1));
	}

	static short IModulusOperators<short, short, short>.operator %(short left, short right)
	{
		return (short)(left % right);
	}

	static short IMultiplyOperators<short, short, short>.operator *(short left, short right)
	{
		return (short)(left * right);
	}

	static short IMultiplyOperators<short, short, short>.operator checked *(short left, short right)
	{
		return checked((short)(left * right));
	}

	public static short Clamp(short value, short min, short max)
	{
		return Math.Clamp(value, min, max);
	}

	public static short CopySign(short value, short sign)
	{
		short num = value;
		if (num < 0)
		{
			num = (short)(-num);
		}
		if (sign >= 0)
		{
			if (num < 0)
			{
				Math.ThrowNegateTwosCompOverflow();
			}
			return num;
		}
		return (short)(-num);
	}

	public static short Max(short x, short y)
	{
		return Math.Max(x, y);
	}

	static short INumber<short>.MaxNumber(short x, short y)
	{
		return Max(x, y);
	}

	public static short Min(short x, short y)
	{
		return Math.Min(x, y);
	}

	static short INumber<short>.MinNumber(short x, short y)
	{
		return Min(x, y);
	}

	public static int Sign(short value)
	{
		return Math.Sign(value);
	}

	public static short Abs(short value)
	{
		return Math.Abs(value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static short CreateChecked<TOther>(TOther value) where TOther : INumberBase<TOther>
	{
		if (typeof(TOther) == typeof(short))
		{
			return (short)(object)value;
		}
		if (!TryConvertFromChecked(value, out var result) && !TOther.TryConvertToChecked<short>(value, out result))
		{
			ThrowHelper.ThrowNotSupportedException();
		}
		return result;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static short CreateSaturating<TOther>(TOther value) where TOther : INumberBase<TOther>
	{
		if (typeof(TOther) == typeof(short))
		{
			return (short)(object)value;
		}
		if (!TryConvertFromSaturating(value, out var result) && !TOther.TryConvertToSaturating<short>(value, out result))
		{
			ThrowHelper.ThrowNotSupportedException();
		}
		return result;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static short CreateTruncating<TOther>(TOther value) where TOther : INumberBase<TOther>
	{
		if (typeof(TOther) == typeof(short))
		{
			return (short)(object)value;
		}
		if (!TryConvertFromTruncating(value, out var result) && !TOther.TryConvertToTruncating<short>(value, out result))
		{
			ThrowHelper.ThrowNotSupportedException();
		}
		return result;
	}

	static bool INumberBase<short>.IsCanonical(short value)
	{
		return true;
	}

	static bool INumberBase<short>.IsComplexNumber(short value)
	{
		return false;
	}

	public static bool IsEvenInteger(short value)
	{
		return (value & 1) == 0;
	}

	static bool INumberBase<short>.IsFinite(short value)
	{
		return true;
	}

	static bool INumberBase<short>.IsImaginaryNumber(short value)
	{
		return false;
	}

	static bool INumberBase<short>.IsInfinity(short value)
	{
		return false;
	}

	static bool INumberBase<short>.IsInteger(short value)
	{
		return true;
	}

	static bool INumberBase<short>.IsNaN(short value)
	{
		return false;
	}

	public static bool IsNegative(short value)
	{
		return value < 0;
	}

	static bool INumberBase<short>.IsNegativeInfinity(short value)
	{
		return false;
	}

	static bool INumberBase<short>.IsNormal(short value)
	{
		return value != 0;
	}

	public static bool IsOddInteger(short value)
	{
		return (value & 1) != 0;
	}

	public static bool IsPositive(short value)
	{
		return value >= 0;
	}

	static bool INumberBase<short>.IsPositiveInfinity(short value)
	{
		return false;
	}

	static bool INumberBase<short>.IsRealNumber(short value)
	{
		return true;
	}

	static bool INumberBase<short>.IsSubnormal(short value)
	{
		return false;
	}

	static bool INumberBase<short>.IsZero(short value)
	{
		return value == 0;
	}

	public static short MaxMagnitude(short x, short y)
	{
		short num = x;
		if (num < 0)
		{
			num = (short)(-num);
			if (num < 0)
			{
				return x;
			}
		}
		short num2 = y;
		if (num2 < 0)
		{
			num2 = (short)(-num2);
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

	static short INumberBase<short>.MaxMagnitudeNumber(short x, short y)
	{
		return MaxMagnitude(x, y);
	}

	public static short MinMagnitude(short x, short y)
	{
		short num = x;
		if (num < 0)
		{
			num = (short)(-num);
			if (num < 0)
			{
				return y;
			}
		}
		short num2 = y;
		if (num2 < 0)
		{
			num2 = (short)(-num2);
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

	static short INumberBase<short>.MinMagnitudeNumber(short x, short y)
	{
		return MinMagnitude(x, y);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	static bool INumberBase<short>.TryConvertFromChecked<TOther>(TOther value, out short result)
	{
		return TryConvertFromChecked(value, out result);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static bool TryConvertFromChecked<TOther>(TOther value, out short result) where TOther : INumberBase<TOther>
	{
		checked
		{
			if (typeof(TOther) == typeof(double))
			{
				double num = (double)(object)value;
				result = (short)num;
				return true;
			}
			if (typeof(TOther) == typeof(Half))
			{
				Half half = (Half)(object)value;
				result = (short)half;
				return true;
			}
			if (typeof(TOther) == typeof(int))
			{
				int num2 = (int)(object)value;
				result = (short)num2;
				return true;
			}
			if (typeof(TOther) == typeof(long))
			{
				long num3 = (long)(object)value;
				result = (short)num3;
				return true;
			}
			if (typeof(TOther) == typeof(Int128))
			{
				Int128 @int = (Int128)(object)value;
				result = (short)@int;
				return true;
			}
			if (typeof(TOther) == typeof(nint))
			{
				nint num4 = (nint)(object)value;
				result = (short)num4;
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
				result = (short)num5;
				return true;
			}
			result = 0;
			return false;
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	static bool INumberBase<short>.TryConvertFromSaturating<TOther>(TOther value, out short result)
	{
		return TryConvertFromSaturating(value, out result);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static bool TryConvertFromSaturating<TOther>(TOther value, out short result) where TOther : INumberBase<TOther>
	{
		if (typeof(TOther) == typeof(double))
		{
			double num = (double)(object)value;
			result = ((num >= 32767.0) ? short.MaxValue : ((num <= -32768.0) ? short.MinValue : ((short)num)));
			return true;
		}
		if (typeof(TOther) == typeof(Half))
		{
			Half half = (Half)(object)value;
			result = ((half >= BitConverter.UInt16BitsToHalf(30720)) ? short.MaxValue : ((half <= BitConverter.UInt16BitsToHalf(63488)) ? short.MinValue : ((short)half)));
			return true;
		}
		if (typeof(TOther) == typeof(int))
		{
			int num2 = (int)(object)value;
			result = ((num2 >= 32767) ? short.MaxValue : ((num2 <= -32768) ? short.MinValue : ((short)num2)));
			return true;
		}
		if (typeof(TOther) == typeof(long))
		{
			long num3 = (long)(object)value;
			result = ((num3 >= 32767) ? short.MaxValue : ((num3 <= -32768) ? short.MinValue : ((short)num3)));
			return true;
		}
		if (typeof(TOther) == typeof(Int128))
		{
			Int128 @int = (Int128)(object)value;
			result = ((@int >= short.MaxValue) ? short.MaxValue : ((@int <= short.MinValue) ? short.MinValue : ((short)@int)));
			return true;
		}
		if (typeof(TOther) == typeof(nint))
		{
			nint num4 = (nint)(object)value;
			result = ((num4 >= 32767) ? short.MaxValue : ((num4 <= -32768) ? short.MinValue : ((short)num4)));
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
			result = ((num5 >= 32767f) ? short.MaxValue : ((num5 <= -32768f) ? short.MinValue : ((short)num5)));
			return true;
		}
		result = 0;
		return false;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	static bool INumberBase<short>.TryConvertFromTruncating<TOther>(TOther value, out short result)
	{
		return TryConvertFromTruncating(value, out result);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static bool TryConvertFromTruncating<TOther>(TOther value, out short result) where TOther : INumberBase<TOther>
	{
		if (typeof(TOther) == typeof(double))
		{
			double num = (double)(object)value;
			result = ((num >= 32767.0) ? short.MaxValue : ((num <= -32768.0) ? short.MinValue : ((short)num)));
			return true;
		}
		if (typeof(TOther) == typeof(Half))
		{
			Half half = (Half)(object)value;
			result = ((half >= BitConverter.UInt16BitsToHalf(30720)) ? short.MaxValue : ((half <= BitConverter.UInt16BitsToHalf(63488)) ? short.MinValue : ((short)half)));
			return true;
		}
		if (typeof(TOther) == typeof(int))
		{
			int num2 = (int)(object)value;
			result = (short)num2;
			return true;
		}
		if (typeof(TOther) == typeof(long))
		{
			long num3 = (long)(object)value;
			result = (short)num3;
			return true;
		}
		if (typeof(TOther) == typeof(Int128))
		{
			Int128 @int = (Int128)(object)value;
			result = (short)@int;
			return true;
		}
		if (typeof(TOther) == typeof(nint))
		{
			nint num4 = (nint)(object)value;
			result = (short)num4;
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
			result = ((num5 >= 32767f) ? short.MaxValue : ((num5 <= -32768f) ? short.MinValue : ((short)num5)));
			return true;
		}
		result = 0;
		return false;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	static bool INumberBase<short>.TryConvertToChecked<TOther>(short value, [MaybeNullWhen(false)] out TOther result)
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
	static bool INumberBase<short>.TryConvertToSaturating<TOther>(short value, [MaybeNullWhen(false)] out TOther result)
	{
		if (typeof(TOther) == typeof(byte))
		{
			byte b = (byte)((value >= 255) ? byte.MaxValue : ((value > 0) ? ((byte)value) : 0));
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
	static bool INumberBase<short>.TryConvertToTruncating<TOther>(short value, [MaybeNullWhen(false)] out TOther result)
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

	public static bool TryParse([NotNullWhen(true)] string? s, IFormatProvider? provider, out short result)
	{
		return TryParse(s, NumberStyles.Integer, provider, out result);
	}

	static short IShiftOperators<short, int, short>.operator <<(short value, int shiftAmount)
	{
		return (short)(value << shiftAmount);
	}

	static short IShiftOperators<short, int, short>.operator >>(short value, int shiftAmount)
	{
		return (short)(value >> shiftAmount);
	}

	static short IShiftOperators<short, int, short>.operator >>>(short value, int shiftAmount)
	{
		return (short)((ushort)value >>> shiftAmount);
	}

	public static short Parse(ReadOnlySpan<char> s, IFormatProvider? provider)
	{
		return Parse(s, NumberStyles.Integer, provider);
	}

	public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, out short result)
	{
		return TryParse(s, NumberStyles.Integer, provider, out result);
	}

	static short ISubtractionOperators<short, short, short>.operator -(short left, short right)
	{
		return (short)(left - right);
	}

	static short ISubtractionOperators<short, short, short>.operator checked -(short left, short right)
	{
		return checked((short)(left - right));
	}

	static short IUnaryNegationOperators<short, short>.operator -(short value)
	{
		return (short)(-value);
	}

	static short IUnaryNegationOperators<short, short>.operator checked -(short value)
	{
		return checked((short)(-value));
	}

	static short IUnaryPlusOperators<short, short>.operator +(short value)
	{
		return value;
	}

	public static short Parse(ReadOnlySpan<byte> utf8Text, NumberStyles style = NumberStyles.Integer, IFormatProvider? provider = null)
	{
		NumberFormatInfo.ValidateParseStyleInteger(style);
		return Number.ParseBinaryInteger<byte, short>(utf8Text, style, NumberFormatInfo.GetInstance(provider));
	}

	public static bool TryParse(ReadOnlySpan<byte> utf8Text, NumberStyles style, IFormatProvider? provider, out short result)
	{
		NumberFormatInfo.ValidateParseStyleInteger(style);
		return Number.TryParseBinaryInteger<byte, short>(utf8Text, style, NumberFormatInfo.GetInstance(provider), out result) == Number.ParsingStatus.OK;
	}

	public static short Parse(ReadOnlySpan<byte> utf8Text, IFormatProvider? provider)
	{
		return Parse(utf8Text, NumberStyles.Integer, provider);
	}

	public static bool TryParse(ReadOnlySpan<byte> utf8Text, IFormatProvider? provider, out short result)
	{
		return TryParse(utf8Text, NumberStyles.Integer, provider, out result);
	}

	static bool IBinaryIntegerParseAndFormatInfo<short>.IsGreaterThanAsUnsigned(short left, short right)
	{
		return (ushort)left > (ushort)right;
	}

	static short IBinaryIntegerParseAndFormatInfo<short>.MultiplyBy10(short value)
	{
		return (short)(value * 10);
	}

	static short IBinaryIntegerParseAndFormatInfo<short>.MultiplyBy16(short value)
	{
		return (short)(value * 16);
	}
}
