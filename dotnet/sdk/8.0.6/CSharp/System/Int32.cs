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
public readonly struct Int32 : IComparable, IConvertible, ISpanFormattable, IFormattable, IComparable<int>, IEquatable<int>, IBinaryInteger<int>, IBinaryNumber<int>, IBitwiseOperators<int, int, int>, INumber<int>, IComparisonOperators<int, int, bool>, IEqualityOperators<int, int, bool>, IModulusOperators<int, int, int>, INumberBase<int>, IAdditionOperators<int, int, int>, IAdditiveIdentity<int, int>, IDecrementOperators<int>, IDivisionOperators<int, int, int>, IIncrementOperators<int>, IMultiplicativeIdentity<int, int>, IMultiplyOperators<int, int, int>, ISpanParsable<int>, IParsable<int>, ISubtractionOperators<int, int, int>, IUnaryPlusOperators<int, int>, IUnaryNegationOperators<int, int>, IUtf8SpanFormattable, IUtf8SpanParsable<int>, IShiftOperators<int, int, int>, IMinMaxValue<int>, ISignedNumber<int>, IBinaryIntegerParseAndFormatInfo<int>
{
	private readonly int m_value;

	public const int MaxValue = 2147483647;

	public const int MinValue = -2147483648;

	static int IAdditiveIdentity<int, int>.AdditiveIdentity => 0;

	static int IBinaryNumber<int>.AllBitsSet => -1;

	static int IMinMaxValue<int>.MinValue => int.MinValue;

	static int IMinMaxValue<int>.MaxValue => int.MaxValue;

	static int IMultiplicativeIdentity<int, int>.MultiplicativeIdentity => 1;

	static int INumberBase<int>.One => 1;

	static int INumberBase<int>.Radix => 2;

	static int INumberBase<int>.Zero => 0;

	static int ISignedNumber<int>.NegativeOne => -1;

	static bool IBinaryIntegerParseAndFormatInfo<int>.IsSigned => true;

	static int IBinaryIntegerParseAndFormatInfo<int>.MaxDigitCount => 10;

	static int IBinaryIntegerParseAndFormatInfo<int>.MaxHexDigitCount => 8;

	static int IBinaryIntegerParseAndFormatInfo<int>.MaxValueDiv10 => 214748364;

	static string IBinaryIntegerParseAndFormatInfo<int>.OverflowMessage => SR.Overflow_Int32;

	public int CompareTo(object? value)
	{
		if (value == null)
		{
			return 1;
		}
		if (value is int num)
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
		throw new ArgumentException(SR.Arg_MustBeInt32);
	}

	public int CompareTo(int value)
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
		if (!(obj is int))
		{
			return false;
		}
		return this == (int)obj;
	}

	[NonVersionable]
	public bool Equals(int obj)
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
		return Number.FormatInt32(this, -1, format, provider);
	}

	public bool TryFormat(Span<char> destination, out int charsWritten, [StringSyntax("NumericFormat")] ReadOnlySpan<char> format = default(ReadOnlySpan<char>), IFormatProvider? provider = null)
	{
		return Number.TryFormatInt32(this, -1, format, provider, destination, out charsWritten);
	}

	public bool TryFormat(Span<byte> utf8Destination, out int bytesWritten, [StringSyntax("NumericFormat")] ReadOnlySpan<char> format = default(ReadOnlySpan<char>), IFormatProvider? provider = null)
	{
		return Number.TryFormatInt32(this, -1, format, provider, utf8Destination, out bytesWritten);
	}

	public static int Parse(string s)
	{
		return Parse(s, NumberStyles.Integer, null);
	}

	public static int Parse(string s, NumberStyles style)
	{
		return Parse(s, style, null);
	}

	public static int Parse(string s, IFormatProvider? provider)
	{
		return Parse(s, NumberStyles.Integer, provider);
	}

	public static int Parse(string s, NumberStyles style, IFormatProvider? provider)
	{
		if (s == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.s);
		}
		return Parse(s.AsSpan(), style, provider);
	}

	public static int Parse(ReadOnlySpan<char> s, NumberStyles style = NumberStyles.Integer, IFormatProvider? provider = null)
	{
		NumberFormatInfo.ValidateParseStyleInteger(style);
		return Number.ParseBinaryInteger<char, int>(s, style, NumberFormatInfo.GetInstance(provider));
	}

	public static bool TryParse([NotNullWhen(true)] string? s, out int result)
	{
		return TryParse(s, NumberStyles.Integer, null, out result);
	}

	public static bool TryParse(ReadOnlySpan<char> s, out int result)
	{
		return TryParse(s, NumberStyles.Integer, null, out result);
	}

	public static bool TryParse(ReadOnlySpan<byte> utf8Text, out int result)
	{
		return TryParse(utf8Text, NumberStyles.Integer, null, out result);
	}

	public static bool TryParse([NotNullWhen(true)] string? s, NumberStyles style, IFormatProvider? provider, out int result)
	{
		NumberFormatInfo.ValidateParseStyleInteger(style);
		if (s == null)
		{
			result = 0;
			return false;
		}
		return Number.TryParseBinaryInteger<char, int>(s.AsSpan(), style, NumberFormatInfo.GetInstance(provider), out result) == Number.ParsingStatus.OK;
	}

	public static bool TryParse(ReadOnlySpan<char> s, NumberStyles style, IFormatProvider? provider, out int result)
	{
		NumberFormatInfo.ValidateParseStyleInteger(style);
		return Number.TryParseBinaryInteger<char, int>(s, style, NumberFormatInfo.GetInstance(provider), out result) == Number.ParsingStatus.OK;
	}

	public TypeCode GetTypeCode()
	{
		return TypeCode.Int32;
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
		throw new InvalidCastException(SR.Format(SR.InvalidCast_FromTo, "Int32", "DateTime"));
	}

	object IConvertible.ToType(Type type, IFormatProvider provider)
	{
		return Convert.DefaultToType(this, type, provider);
	}

	static int IAdditionOperators<int, int, int>.operator +(int left, int right)
	{
		return left + right;
	}

	static int IAdditionOperators<int, int, int>.operator checked +(int left, int right)
	{
		return checked(left + right);
	}

	public static (int Quotient, int Remainder) DivRem(int left, int right)
	{
		return Math.DivRem(left, right);
	}

	[Intrinsic]
	public static int LeadingZeroCount(int value)
	{
		return BitOperations.LeadingZeroCount((uint)value);
	}

	[Intrinsic]
	public static int PopCount(int value)
	{
		return BitOperations.PopCount((uint)value);
	}

	[Intrinsic]
	public static int RotateLeft(int value, int rotateAmount)
	{
		return (int)BitOperations.RotateLeft((uint)value, rotateAmount);
	}

	[Intrinsic]
	public static int RotateRight(int value, int rotateAmount)
	{
		return (int)BitOperations.RotateRight((uint)value, rotateAmount);
	}

	[Intrinsic]
	public static int TrailingZeroCount(int value)
	{
		return BitOperations.TrailingZeroCount(value);
	}

	static bool IBinaryInteger<int>.TryReadBigEndian(ReadOnlySpan<byte> source, bool isUnsigned, out int value)
	{
		int num = 0;
		if (source.Length != 0)
		{
			sbyte b = (sbyte)source[0];
			b >>= 31;
			isUnsigned = isUnsigned || b == 0;
			if (isUnsigned && sbyte.IsNegative(b) && source.Length >= 4)
			{
				value = num;
				return false;
			}
			if (source.Length > 4)
			{
				if (source.Slice(0, source.Length - 4).ContainsAnyExcept((byte)b))
				{
					value = num;
					return false;
				}
				if (isUnsigned == sbyte.IsNegative((sbyte)source[source.Length - 4]))
				{
					value = num;
					return false;
				}
			}
			ref byte reference = ref MemoryMarshal.GetReference(source);
			if (source.Length >= 4)
			{
				num = Unsafe.ReadUnaligned<int>(ref Unsafe.Add(ref reference, source.Length - 4));
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
					num |= int.MinValue >> (4 - source.Length) * 8 - 1;
				}
			}
		}
		value = num;
		return true;
	}

	static bool IBinaryInteger<int>.TryReadLittleEndian(ReadOnlySpan<byte> source, bool isUnsigned, out int value)
	{
		int num = 0;
		if (source.Length != 0)
		{
			sbyte b = (sbyte)source[source.Length - 1];
			b >>= 31;
			isUnsigned = isUnsigned || b == 0;
			if (isUnsigned && sbyte.IsNegative(b) && source.Length >= 4)
			{
				value = num;
				return false;
			}
			if (source.Length > 4)
			{
				if (source.Slice(4, source.Length - 4).ContainsAnyExcept((byte)b))
				{
					value = num;
					return false;
				}
				if (isUnsigned == sbyte.IsNegative((sbyte)source[3]))
				{
					value = num;
					return false;
				}
			}
			ref byte reference = ref MemoryMarshal.GetReference(source);
			if (source.Length >= 4)
			{
				num = Unsafe.ReadUnaligned<int>(ref reference);
				if (BitConverter.IsLittleEndian)
				{
					goto IL_0102;
				}
			}
			for (int i = 0; i < source.Length; i++)
			{
				num <<= 8;
				num |= Unsafe.Add(ref reference, i);
			}
			num <<= (4 - source.Length) * 8;
			num = BinaryPrimitives.ReverseEndianness(num);
			if (!isUnsigned)
			{
				num |= int.MinValue >> (4 - source.Length) * 8 - 1;
			}
		}
		goto IL_0102;
		IL_0102:
		value = num;
		return true;
	}

	int IBinaryInteger<int>.GetShortestBitLength()
	{
		int num = this;
		if (num >= 0)
		{
			return 32 - LeadingZeroCount(num);
		}
		return 33 - LeadingZeroCount(~num);
	}

	int IBinaryInteger<int>.GetByteCount()
	{
		return 4;
	}

	bool IBinaryInteger<int>.TryWriteBigEndian(Span<byte> destination, out int bytesWritten)
	{
		if (destination.Length >= 4)
		{
			if (!BitConverter.IsLittleEndian)
			{
			}
			int value = BinaryPrimitives.ReverseEndianness(this);
			Unsafe.WriteUnaligned(ref MemoryMarshal.GetReference(destination), value);
			bytesWritten = 4;
			return true;
		}
		bytesWritten = 0;
		return false;
	}

	bool IBinaryInteger<int>.TryWriteLittleEndian(Span<byte> destination, out int bytesWritten)
	{
		if (destination.Length >= 4)
		{
			if (!BitConverter.IsLittleEndian)
			{
			}
			int value = this;
			Unsafe.WriteUnaligned(ref MemoryMarshal.GetReference(destination), value);
			bytesWritten = 4;
			return true;
		}
		bytesWritten = 0;
		return false;
	}

	public static bool IsPow2(int value)
	{
		return BitOperations.IsPow2(value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static int Log2(int value)
	{
		if (value < 0)
		{
			ThrowHelper.ThrowValueArgumentOutOfRange_NeedNonNegNumException();
		}
		return BitOperations.Log2((uint)value);
	}

	static int IBitwiseOperators<int, int, int>.operator &(int left, int right)
	{
		return left & right;
	}

	static int IBitwiseOperators<int, int, int>.operator |(int left, int right)
	{
		return left | right;
	}

	static int IBitwiseOperators<int, int, int>.operator ^(int left, int right)
	{
		return left ^ right;
	}

	static int IBitwiseOperators<int, int, int>.operator ~(int value)
	{
		return ~value;
	}

	static bool IComparisonOperators<int, int, bool>.operator <(int left, int right)
	{
		return left < right;
	}

	static bool IComparisonOperators<int, int, bool>.operator <=(int left, int right)
	{
		return left <= right;
	}

	static bool IComparisonOperators<int, int, bool>.operator >(int left, int right)
	{
		return left > right;
	}

	static bool IComparisonOperators<int, int, bool>.operator >=(int left, int right)
	{
		return left >= right;
	}

	static int IDecrementOperators<int>.operator --(int value)
	{
		return --value;
	}

	static int IDecrementOperators<int>.operator checked --(int value)
	{
		return value = checked(value - 1);
	}

	static int IDivisionOperators<int, int, int>.operator /(int left, int right)
	{
		return left / right;
	}

	static bool IEqualityOperators<int, int, bool>.operator ==(int left, int right)
	{
		return left == right;
	}

	static bool IEqualityOperators<int, int, bool>.operator !=(int left, int right)
	{
		return left != right;
	}

	static int IIncrementOperators<int>.operator ++(int value)
	{
		return ++value;
	}

	static int IIncrementOperators<int>.operator checked ++(int value)
	{
		return value = checked(value + 1);
	}

	static int IModulusOperators<int, int, int>.operator %(int left, int right)
	{
		return left % right;
	}

	static int IMultiplyOperators<int, int, int>.operator *(int left, int right)
	{
		return left * right;
	}

	static int IMultiplyOperators<int, int, int>.operator checked *(int left, int right)
	{
		return checked(left * right);
	}

	public static int Clamp(int value, int min, int max)
	{
		return Math.Clamp(value, min, max);
	}

	public static int CopySign(int value, int sign)
	{
		int num = value;
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

	public static int Max(int x, int y)
	{
		return Math.Max(x, y);
	}

	static int INumber<int>.MaxNumber(int x, int y)
	{
		return Max(x, y);
	}

	public static int Min(int x, int y)
	{
		return Math.Min(x, y);
	}

	static int INumber<int>.MinNumber(int x, int y)
	{
		return Min(x, y);
	}

	public static int Sign(int value)
	{
		return Math.Sign(value);
	}

	public static int Abs(int value)
	{
		return Math.Abs(value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static int CreateChecked<TOther>(TOther value) where TOther : INumberBase<TOther>
	{
		if (typeof(TOther) == typeof(int))
		{
			return (int)(object)value;
		}
		if (!TryConvertFromChecked(value, out var result) && !TOther.TryConvertToChecked<int>(value, out result))
		{
			ThrowHelper.ThrowNotSupportedException();
		}
		return result;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static int CreateSaturating<TOther>(TOther value) where TOther : INumberBase<TOther>
	{
		if (typeof(TOther) == typeof(int))
		{
			return (int)(object)value;
		}
		if (!TryConvertFromSaturating(value, out var result) && !TOther.TryConvertToSaturating<int>(value, out result))
		{
			ThrowHelper.ThrowNotSupportedException();
		}
		return result;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static int CreateTruncating<TOther>(TOther value) where TOther : INumberBase<TOther>
	{
		if (typeof(TOther) == typeof(int))
		{
			return (int)(object)value;
		}
		if (!TryConvertFromTruncating(value, out var result) && !TOther.TryConvertToTruncating<int>(value, out result))
		{
			ThrowHelper.ThrowNotSupportedException();
		}
		return result;
	}

	static bool INumberBase<int>.IsCanonical(int value)
	{
		return true;
	}

	static bool INumberBase<int>.IsComplexNumber(int value)
	{
		return false;
	}

	public static bool IsEvenInteger(int value)
	{
		return (value & 1) == 0;
	}

	static bool INumberBase<int>.IsFinite(int value)
	{
		return true;
	}

	static bool INumberBase<int>.IsImaginaryNumber(int value)
	{
		return false;
	}

	static bool INumberBase<int>.IsInfinity(int value)
	{
		return false;
	}

	static bool INumberBase<int>.IsInteger(int value)
	{
		return true;
	}

	static bool INumberBase<int>.IsNaN(int value)
	{
		return false;
	}

	public static bool IsNegative(int value)
	{
		return value < 0;
	}

	static bool INumberBase<int>.IsNegativeInfinity(int value)
	{
		return false;
	}

	static bool INumberBase<int>.IsNormal(int value)
	{
		return value != 0;
	}

	public static bool IsOddInteger(int value)
	{
		return (value & 1) != 0;
	}

	public static bool IsPositive(int value)
	{
		return value >= 0;
	}

	static bool INumberBase<int>.IsPositiveInfinity(int value)
	{
		return false;
	}

	static bool INumberBase<int>.IsRealNumber(int value)
	{
		return true;
	}

	static bool INumberBase<int>.IsSubnormal(int value)
	{
		return false;
	}

	static bool INumberBase<int>.IsZero(int value)
	{
		return value == 0;
	}

	public static int MaxMagnitude(int x, int y)
	{
		int num = x;
		if (num < 0)
		{
			num = -num;
			if (num < 0)
			{
				return x;
			}
		}
		int num2 = y;
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

	static int INumberBase<int>.MaxMagnitudeNumber(int x, int y)
	{
		return MaxMagnitude(x, y);
	}

	public static int MinMagnitude(int x, int y)
	{
		int num = x;
		if (num < 0)
		{
			num = -num;
			if (num < 0)
			{
				return y;
			}
		}
		int num2 = y;
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

	static int INumberBase<int>.MinMagnitudeNumber(int x, int y)
	{
		return MinMagnitude(x, y);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	static bool INumberBase<int>.TryConvertFromChecked<TOther>(TOther value, out int result)
	{
		return TryConvertFromChecked(value, out result);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static bool TryConvertFromChecked<TOther>(TOther value, out int result) where TOther : INumberBase<TOther>
	{
		checked
		{
			if (typeof(TOther) == typeof(double))
			{
				double num = (double)(object)value;
				result = (int)num;
				return true;
			}
			if (typeof(TOther) == typeof(Half))
			{
				Half half = (Half)(object)value;
				result = (int)half;
				return true;
			}
			if (typeof(TOther) == typeof(short))
			{
				short num2 = (short)(object)value;
				result = num2;
				return true;
			}
			if (typeof(TOther) == typeof(long))
			{
				long num3 = (long)(object)value;
				result = (int)num3;
				return true;
			}
			if (typeof(TOther) == typeof(Int128))
			{
				Int128 @int = (Int128)(object)value;
				result = (int)@int;
				return true;
			}
			if (typeof(TOther) == typeof(nint))
			{
				nint num4 = (nint)(object)value;
				result = (int)num4;
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
				result = (int)num5;
				return true;
			}
			result = 0;
			return false;
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	static bool INumberBase<int>.TryConvertFromSaturating<TOther>(TOther value, out int result)
	{
		return TryConvertFromSaturating(value, out result);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static bool TryConvertFromSaturating<TOther>(TOther value, out int result) where TOther : INumberBase<TOther>
	{
		if (typeof(TOther) == typeof(double))
		{
			double num = (double)(object)value;
			result = ((num >= 2147483647.0) ? int.MaxValue : ((num <= -2147483648.0) ? int.MinValue : ((int)num)));
			return true;
		}
		if (typeof(TOther) == typeof(Half))
		{
			Half half = (Half)(object)value;
			result = ((half == Half.PositiveInfinity) ? int.MaxValue : ((half == Half.NegativeInfinity) ? int.MinValue : ((int)half)));
			return true;
		}
		if (typeof(TOther) == typeof(short))
		{
			short num2 = (short)(object)value;
			result = num2;
			return true;
		}
		if (typeof(TOther) == typeof(long))
		{
			long num3 = (long)(object)value;
			result = (int)((num3 >= int.MaxValue) ? int.MaxValue : ((num3 <= int.MinValue) ? int.MinValue : num3));
			return true;
		}
		if (typeof(TOther) == typeof(Int128))
		{
			Int128 @int = (Int128)(object)value;
			result = ((@int >= int.MaxValue) ? int.MaxValue : ((@int <= int.MinValue) ? int.MinValue : ((int)@int)));
			return true;
		}
		if (typeof(TOther) == typeof(nint))
		{
			nint num4 = (nint)(object)value;
			result = (int)((num4 >= int.MaxValue) ? int.MaxValue : ((num4 <= int.MinValue) ? int.MinValue : num4));
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
			result = ((num5 >= 2.1474836E+09f) ? int.MaxValue : ((num5 <= -2.1474836E+09f) ? int.MinValue : ((int)num5)));
			return true;
		}
		result = 0;
		return false;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	static bool INumberBase<int>.TryConvertFromTruncating<TOther>(TOther value, out int result)
	{
		return TryConvertFromTruncating(value, out result);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static bool TryConvertFromTruncating<TOther>(TOther value, out int result) where TOther : INumberBase<TOther>
	{
		if (typeof(TOther) == typeof(double))
		{
			double num = (double)(object)value;
			result = ((num >= 2147483647.0) ? int.MaxValue : ((num <= -2147483648.0) ? int.MinValue : ((int)num)));
			return true;
		}
		if (typeof(TOther) == typeof(Half))
		{
			Half half = (Half)(object)value;
			result = ((half == Half.PositiveInfinity) ? int.MaxValue : ((half == Half.NegativeInfinity) ? int.MinValue : ((int)half)));
			return true;
		}
		if (typeof(TOther) == typeof(short))
		{
			short num2 = (short)(object)value;
			result = num2;
			return true;
		}
		if (typeof(TOther) == typeof(long))
		{
			long num3 = (long)(object)value;
			result = (int)num3;
			return true;
		}
		if (typeof(TOther) == typeof(Int128))
		{
			Int128 @int = (Int128)(object)value;
			result = (int)@int;
			return true;
		}
		if (typeof(TOther) == typeof(nint))
		{
			nint num4 = (nint)(object)value;
			result = (int)num4;
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
			result = ((num5 >= 2.1474836E+09f) ? int.MaxValue : ((num5 <= -2.1474836E+09f) ? int.MinValue : ((int)num5)));
			return true;
		}
		result = 0;
		return false;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	static bool INumberBase<int>.TryConvertToChecked<TOther>(int value, [MaybeNullWhen(false)] out TOther result)
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
	static bool INumberBase<int>.TryConvertToSaturating<TOther>(int value, [MaybeNullWhen(false)] out TOther result)
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
			uint num3 = ((value > 0) ? ((uint)value) : 0u);
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
	static bool INumberBase<int>.TryConvertToTruncating<TOther>(int value, [MaybeNullWhen(false)] out TOther result)
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

	public static bool TryParse([NotNullWhen(true)] string? s, IFormatProvider? provider, out int result)
	{
		return TryParse(s, NumberStyles.Integer, provider, out result);
	}

	static int IShiftOperators<int, int, int>.operator <<(int value, int shiftAmount)
	{
		return value << shiftAmount;
	}

	static int IShiftOperators<int, int, int>.operator >>(int value, int shiftAmount)
	{
		return value >> shiftAmount;
	}

	static int IShiftOperators<int, int, int>.operator >>>(int value, int shiftAmount)
	{
		return value >>> shiftAmount;
	}

	public static int Parse(ReadOnlySpan<char> s, IFormatProvider? provider)
	{
		return Parse(s, NumberStyles.Integer, provider);
	}

	public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, out int result)
	{
		return TryParse(s, NumberStyles.Integer, provider, out result);
	}

	static int ISubtractionOperators<int, int, int>.operator -(int left, int right)
	{
		return left - right;
	}

	static int ISubtractionOperators<int, int, int>.operator checked -(int left, int right)
	{
		return checked(left - right);
	}

	static int IUnaryNegationOperators<int, int>.operator -(int value)
	{
		return -value;
	}

	static int IUnaryNegationOperators<int, int>.operator checked -(int value)
	{
		return checked(-value);
	}

	static int IUnaryPlusOperators<int, int>.operator +(int value)
	{
		return value;
	}

	public static int Parse(ReadOnlySpan<byte> utf8Text, NumberStyles style = NumberStyles.Integer, IFormatProvider? provider = null)
	{
		NumberFormatInfo.ValidateParseStyleInteger(style);
		return Number.ParseBinaryInteger<byte, int>(utf8Text, style, NumberFormatInfo.GetInstance(provider));
	}

	public static bool TryParse(ReadOnlySpan<byte> utf8Text, NumberStyles style, IFormatProvider? provider, out int result)
	{
		NumberFormatInfo.ValidateParseStyleInteger(style);
		return Number.TryParseBinaryInteger<byte, int>(utf8Text, style, NumberFormatInfo.GetInstance(provider), out result) == Number.ParsingStatus.OK;
	}

	public static int Parse(ReadOnlySpan<byte> utf8Text, IFormatProvider? provider)
	{
		return Parse(utf8Text, NumberStyles.Integer, provider);
	}

	public static bool TryParse(ReadOnlySpan<byte> utf8Text, IFormatProvider? provider, out int result)
	{
		return TryParse(utf8Text, NumberStyles.Integer, provider, out result);
	}

	static bool IBinaryIntegerParseAndFormatInfo<int>.IsGreaterThanAsUnsigned(int left, int right)
	{
		return (uint)left > (uint)right;
	}

	static int IBinaryIntegerParseAndFormatInfo<int>.MultiplyBy10(int value)
	{
		return value * 10;
	}

	static int IBinaryIntegerParseAndFormatInfo<int>.MultiplyBy16(int value)
	{
		return value * 16;
	}
}
