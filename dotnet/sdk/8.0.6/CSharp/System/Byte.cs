using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace System;

[Serializable]
[TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
public readonly struct Byte : IComparable, IConvertible, ISpanFormattable, IFormattable, IComparable<byte>, IEquatable<byte>, IBinaryInteger<byte>, IBinaryNumber<byte>, IBitwiseOperators<byte, byte, byte>, INumber<byte>, IComparisonOperators<byte, byte, bool>, IEqualityOperators<byte, byte, bool>, IModulusOperators<byte, byte, byte>, INumberBase<byte>, IAdditionOperators<byte, byte, byte>, IAdditiveIdentity<byte, byte>, IDecrementOperators<byte>, IDivisionOperators<byte, byte, byte>, IIncrementOperators<byte>, IMultiplicativeIdentity<byte, byte>, IMultiplyOperators<byte, byte, byte>, ISpanParsable<byte>, IParsable<byte>, ISubtractionOperators<byte, byte, byte>, IUnaryPlusOperators<byte, byte>, IUnaryNegationOperators<byte, byte>, IUtf8SpanFormattable, IUtf8SpanParsable<byte>, IShiftOperators<byte, int, byte>, IMinMaxValue<byte>, IUnsignedNumber<byte>, IUtfChar<byte>, IBinaryIntegerParseAndFormatInfo<byte>
{
	private readonly byte m_value;

	public const byte MaxValue = 255;

	public const byte MinValue = 0;

	static byte IAdditiveIdentity<byte, byte>.AdditiveIdentity => 0;

	static byte IBinaryNumber<byte>.AllBitsSet => byte.MaxValue;

	static byte IMinMaxValue<byte>.MinValue => 0;

	static byte IMinMaxValue<byte>.MaxValue => byte.MaxValue;

	static byte IMultiplicativeIdentity<byte, byte>.MultiplicativeIdentity => 1;

	static byte INumberBase<byte>.One => 1;

	static int INumberBase<byte>.Radix => 2;

	static byte INumberBase<byte>.Zero => 0;

	static bool IBinaryIntegerParseAndFormatInfo<byte>.IsSigned => false;

	static int IBinaryIntegerParseAndFormatInfo<byte>.MaxDigitCount => 3;

	static int IBinaryIntegerParseAndFormatInfo<byte>.MaxHexDigitCount => 2;

	static byte IBinaryIntegerParseAndFormatInfo<byte>.MaxValueDiv10 => 25;

	static string IBinaryIntegerParseAndFormatInfo<byte>.OverflowMessage => SR.Overflow_Byte;

	public int CompareTo(object? value)
	{
		if (value == null)
		{
			return 1;
		}
		if (!(value is byte))
		{
			throw new ArgumentException(SR.Arg_MustBeByte);
		}
		return this - (byte)value;
	}

	public int CompareTo(byte value)
	{
		return this - value;
	}

	public override bool Equals([NotNullWhen(true)] object? obj)
	{
		if (!(obj is byte))
		{
			return false;
		}
		return this == (byte)obj;
	}

	[NonVersionable]
	public bool Equals(byte obj)
	{
		return this == obj;
	}

	public override int GetHashCode()
	{
		return this;
	}

	public static byte Parse(string s)
	{
		return Parse(s, NumberStyles.Integer, null);
	}

	public static byte Parse(string s, NumberStyles style)
	{
		return Parse(s, style, null);
	}

	public static byte Parse(string s, IFormatProvider? provider)
	{
		return Parse(s, NumberStyles.Integer, provider);
	}

	public static byte Parse(string s, NumberStyles style, IFormatProvider? provider)
	{
		if (s == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.s);
		}
		return Parse(s.AsSpan(), style, provider);
	}

	public static byte Parse(ReadOnlySpan<char> s, NumberStyles style = NumberStyles.Integer, IFormatProvider? provider = null)
	{
		NumberFormatInfo.ValidateParseStyleInteger(style);
		return Number.ParseBinaryInteger<char, byte>(s, style, NumberFormatInfo.GetInstance(provider));
	}

	public static bool TryParse([NotNullWhen(true)] string? s, out byte result)
	{
		return TryParse(s, NumberStyles.Integer, null, out result);
	}

	public static bool TryParse(ReadOnlySpan<char> s, out byte result)
	{
		return TryParse(s, NumberStyles.Integer, null, out result);
	}

	public static bool TryParse(ReadOnlySpan<byte> utf8Text, out byte result)
	{
		return TryParse(utf8Text, NumberStyles.Integer, null, out result);
	}

	public static bool TryParse([NotNullWhen(true)] string? s, NumberStyles style, IFormatProvider? provider, out byte result)
	{
		NumberFormatInfo.ValidateParseStyleInteger(style);
		if (s == null)
		{
			result = 0;
			return false;
		}
		return Number.TryParseBinaryInteger<char, byte>(s.AsSpan(), style, NumberFormatInfo.GetInstance(provider), out result) == Number.ParsingStatus.OK;
	}

	public static bool TryParse(ReadOnlySpan<char> s, NumberStyles style, IFormatProvider? provider, out byte result)
	{
		NumberFormatInfo.ValidateParseStyleInteger(style);
		return Number.TryParseBinaryInteger<char, byte>(s, style, NumberFormatInfo.GetInstance(provider), out result) == Number.ParsingStatus.OK;
	}

	public override string ToString()
	{
		return Number.UInt32ToDecStr(this);
	}

	public string ToString([StringSyntax("NumericFormat")] string? format)
	{
		return Number.FormatUInt32(this, format, null);
	}

	public string ToString(IFormatProvider? provider)
	{
		return Number.UInt32ToDecStr(this);
	}

	public string ToString([StringSyntax("NumericFormat")] string? format, IFormatProvider? provider)
	{
		return Number.FormatUInt32(this, format, provider);
	}

	public bool TryFormat(Span<char> destination, out int charsWritten, [StringSyntax("NumericFormat")] ReadOnlySpan<char> format = default(ReadOnlySpan<char>), IFormatProvider? provider = null)
	{
		return Number.TryFormatUInt32(this, format, provider, destination, out charsWritten);
	}

	public bool TryFormat(Span<byte> utf8Destination, out int bytesWritten, [StringSyntax("NumericFormat")] ReadOnlySpan<char> format = default(ReadOnlySpan<char>), IFormatProvider? provider = null)
	{
		return Number.TryFormatUInt32(this, format, provider, utf8Destination, out bytesWritten);
	}

	public TypeCode GetTypeCode()
	{
		return TypeCode.Byte;
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
		return this;
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
		throw new InvalidCastException(SR.Format(SR.InvalidCast_FromTo, "Byte", "DateTime"));
	}

	object IConvertible.ToType(Type type, IFormatProvider provider)
	{
		return Convert.DefaultToType(this, type, provider);
	}

	static byte IAdditionOperators<byte, byte, byte>.operator +(byte left, byte right)
	{
		return (byte)(left + right);
	}

	static byte IAdditionOperators<byte, byte, byte>.operator checked +(byte left, byte right)
	{
		return checked((byte)(left + right));
	}

	public static (byte Quotient, byte Remainder) DivRem(byte left, byte right)
	{
		return Math.DivRem(left, right);
	}

	public static byte LeadingZeroCount(byte value)
	{
		return (byte)(BitOperations.LeadingZeroCount(value) - 24);
	}

	public static byte PopCount(byte value)
	{
		return (byte)BitOperations.PopCount(value);
	}

	public static byte RotateLeft(byte value, int rotateAmount)
	{
		return (byte)((value << (rotateAmount & 7)) | (value >> ((8 - rotateAmount) & 7)));
	}

	public static byte RotateRight(byte value, int rotateAmount)
	{
		return (byte)((value >> (rotateAmount & 7)) | (value << ((8 - rotateAmount) & 7)));
	}

	public static byte TrailingZeroCount(byte value)
	{
		return (byte)(BitOperations.TrailingZeroCount(value << 24) - 24);
	}

	static bool IBinaryInteger<byte>.TryReadBigEndian(ReadOnlySpan<byte> source, bool isUnsigned, out byte value)
	{
		byte b = 0;
		if (source.Length != 0)
		{
			if (!isUnsigned && sbyte.IsNegative((sbyte)source[0]))
			{
				value = b;
				return false;
			}
			if (source.Length > 1)
			{
				if (source.Slice(0, source.Length - 1).ContainsAnyExcept<byte>(0))
				{
					value = b;
					return false;
				}
			}
			b = Unsafe.Add(ref MemoryMarshal.GetReference(source), source.Length - 1);
		}
		value = b;
		return true;
	}

	static bool IBinaryInteger<byte>.TryReadLittleEndian(ReadOnlySpan<byte> source, bool isUnsigned, out byte value)
	{
		byte b = 0;
		if (source.Length != 0)
		{
			if (!isUnsigned)
			{
				if (sbyte.IsNegative((sbyte)source[source.Length - 1]))
				{
					value = b;
					return false;
				}
			}
			if (source.Length > 1)
			{
				if (source.Slice(1, source.Length - 1).ContainsAnyExcept<byte>(0))
				{
					value = b;
					return false;
				}
			}
			b = MemoryMarshal.GetReference(source);
		}
		value = b;
		return true;
	}

	int IBinaryInteger<byte>.GetShortestBitLength()
	{
		return 8 - LeadingZeroCount(this);
	}

	int IBinaryInteger<byte>.GetByteCount()
	{
		return 1;
	}

	bool IBinaryInteger<byte>.TryWriteBigEndian(Span<byte> destination, out int bytesWritten)
	{
		if (destination.Length >= 1)
		{
			byte b = this;
			MemoryMarshal.GetReference(destination) = b;
			bytesWritten = 1;
			return true;
		}
		bytesWritten = 0;
		return false;
	}

	bool IBinaryInteger<byte>.TryWriteLittleEndian(Span<byte> destination, out int bytesWritten)
	{
		if (destination.Length >= 1)
		{
			byte b = this;
			MemoryMarshal.GetReference(destination) = b;
			bytesWritten = 1;
			return true;
		}
		bytesWritten = 0;
		return false;
	}

	public static bool IsPow2(byte value)
	{
		return BitOperations.IsPow2((uint)value);
	}

	public static byte Log2(byte value)
	{
		return (byte)BitOperations.Log2(value);
	}

	static byte IBitwiseOperators<byte, byte, byte>.operator &(byte left, byte right)
	{
		return (byte)(left & right);
	}

	static byte IBitwiseOperators<byte, byte, byte>.operator |(byte left, byte right)
	{
		return (byte)(left | right);
	}

	static byte IBitwiseOperators<byte, byte, byte>.operator ^(byte left, byte right)
	{
		return (byte)(left ^ right);
	}

	static byte IBitwiseOperators<byte, byte, byte>.operator ~(byte value)
	{
		return (byte)(~value);
	}

	static bool IComparisonOperators<byte, byte, bool>.operator <(byte left, byte right)
	{
		return left < right;
	}

	static bool IComparisonOperators<byte, byte, bool>.operator <=(byte left, byte right)
	{
		return left <= right;
	}

	static bool IComparisonOperators<byte, byte, bool>.operator >(byte left, byte right)
	{
		return left > right;
	}

	static bool IComparisonOperators<byte, byte, bool>.operator >=(byte left, byte right)
	{
		return left >= right;
	}

	static byte IDecrementOperators<byte>.operator --(byte value)
	{
		return --value;
	}

	static byte IDecrementOperators<byte>.operator checked --(byte value)
	{
		checked
		{
			return value = (byte)(unchecked((uint)value) - 1u);
		}
	}

	static byte IDivisionOperators<byte, byte, byte>.operator /(byte left, byte right)
	{
		return (byte)(left / right);
	}

	static bool IEqualityOperators<byte, byte, bool>.operator ==(byte left, byte right)
	{
		return left == right;
	}

	static bool IEqualityOperators<byte, byte, bool>.operator !=(byte left, byte right)
	{
		return left != right;
	}

	static byte IIncrementOperators<byte>.operator ++(byte value)
	{
		return ++value;
	}

	static byte IIncrementOperators<byte>.operator checked ++(byte value)
	{
		checked
		{
			return value = (byte)(unchecked((uint)value) + 1u);
		}
	}

	static byte IModulusOperators<byte, byte, byte>.operator %(byte left, byte right)
	{
		return (byte)(left % right);
	}

	static byte IMultiplyOperators<byte, byte, byte>.operator *(byte left, byte right)
	{
		return (byte)(left * right);
	}

	static byte IMultiplyOperators<byte, byte, byte>.operator checked *(byte left, byte right)
	{
		return checked((byte)(left * right));
	}

	public static byte Clamp(byte value, byte min, byte max)
	{
		return Math.Clamp(value, min, max);
	}

	static byte INumber<byte>.CopySign(byte value, byte sign)
	{
		return value;
	}

	public static byte Max(byte x, byte y)
	{
		return Math.Max(x, y);
	}

	static byte INumber<byte>.MaxNumber(byte x, byte y)
	{
		return Max(x, y);
	}

	public static byte Min(byte x, byte y)
	{
		return Math.Min(x, y);
	}

	static byte INumber<byte>.MinNumber(byte x, byte y)
	{
		return Min(x, y);
	}

	public static int Sign(byte value)
	{
		return (value != 0) ? 1 : 0;
	}

	static byte INumberBase<byte>.Abs(byte value)
	{
		return value;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static byte CreateChecked<TOther>(TOther value) where TOther : INumberBase<TOther>
	{
		if (typeof(TOther) == typeof(byte))
		{
			return (byte)(object)value;
		}
		if (!TryConvertFromChecked(value, out var result) && !TOther.TryConvertToChecked<byte>(value, out result))
		{
			ThrowHelper.ThrowNotSupportedException();
		}
		return result;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static byte CreateSaturating<TOther>(TOther value) where TOther : INumberBase<TOther>
	{
		if (typeof(TOther) == typeof(byte))
		{
			return (byte)(object)value;
		}
		if (!TryConvertFromSaturating(value, out var result) && !TOther.TryConvertToSaturating<byte>(value, out result))
		{
			ThrowHelper.ThrowNotSupportedException();
		}
		return result;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static byte CreateTruncating<TOther>(TOther value) where TOther : INumberBase<TOther>
	{
		if (typeof(TOther) == typeof(byte))
		{
			return (byte)(object)value;
		}
		if (!TryConvertFromTruncating(value, out var result) && !TOther.TryConvertToTruncating<byte>(value, out result))
		{
			ThrowHelper.ThrowNotSupportedException();
		}
		return result;
	}

	static bool INumberBase<byte>.IsCanonical(byte value)
	{
		return true;
	}

	static bool INumberBase<byte>.IsComplexNumber(byte value)
	{
		return false;
	}

	public static bool IsEvenInteger(byte value)
	{
		return (value & 1) == 0;
	}

	static bool INumberBase<byte>.IsFinite(byte value)
	{
		return true;
	}

	static bool INumberBase<byte>.IsImaginaryNumber(byte value)
	{
		return false;
	}

	static bool INumberBase<byte>.IsInfinity(byte value)
	{
		return false;
	}

	static bool INumberBase<byte>.IsInteger(byte value)
	{
		return true;
	}

	static bool INumberBase<byte>.IsNaN(byte value)
	{
		return false;
	}

	static bool INumberBase<byte>.IsNegative(byte value)
	{
		return false;
	}

	static bool INumberBase<byte>.IsNegativeInfinity(byte value)
	{
		return false;
	}

	static bool INumberBase<byte>.IsNormal(byte value)
	{
		return value != 0;
	}

	public static bool IsOddInteger(byte value)
	{
		return (value & 1) != 0;
	}

	static bool INumberBase<byte>.IsPositive(byte value)
	{
		return true;
	}

	static bool INumberBase<byte>.IsPositiveInfinity(byte value)
	{
		return false;
	}

	static bool INumberBase<byte>.IsRealNumber(byte value)
	{
		return true;
	}

	static bool INumberBase<byte>.IsSubnormal(byte value)
	{
		return false;
	}

	static bool INumberBase<byte>.IsZero(byte value)
	{
		return value == 0;
	}

	static byte INumberBase<byte>.MaxMagnitude(byte x, byte y)
	{
		return Max(x, y);
	}

	static byte INumberBase<byte>.MaxMagnitudeNumber(byte x, byte y)
	{
		return Max(x, y);
	}

	static byte INumberBase<byte>.MinMagnitude(byte x, byte y)
	{
		return Min(x, y);
	}

	static byte INumberBase<byte>.MinMagnitudeNumber(byte x, byte y)
	{
		return Min(x, y);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	static bool INumberBase<byte>.TryConvertFromChecked<TOther>(TOther value, out byte result)
	{
		return TryConvertFromChecked(value, out result);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static bool TryConvertFromChecked<TOther>(TOther value, out byte result) where TOther : INumberBase<TOther>
	{
		checked
		{
			if (typeof(TOther) == typeof(char))
			{
				char c = (char)(object)value;
				result = (byte)c;
				return true;
			}
			if (typeof(TOther) == typeof(decimal))
			{
				decimal num = (decimal)(object)value;
				result = (byte)num;
				return true;
			}
			if (typeof(TOther) == typeof(ushort))
			{
				ushort num2 = (ushort)(object)value;
				result = (byte)num2;
				return true;
			}
			if (typeof(TOther) == typeof(uint))
			{
				uint num3 = (uint)(object)value;
				result = (byte)num3;
				return true;
			}
			if (typeof(TOther) == typeof(ulong))
			{
				ulong num4 = (ulong)(object)value;
				result = (byte)num4;
				return true;
			}
			if (typeof(TOther) == typeof(UInt128))
			{
				UInt128 uInt = (UInt128)(object)value;
				result = (byte)uInt;
				return true;
			}
			if (typeof(TOther) == typeof(nuint))
			{
				nuint num5 = (nuint)(object)value;
				result = (byte)num5;
				return true;
			}
			result = 0;
			return false;
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	static bool INumberBase<byte>.TryConvertFromSaturating<TOther>(TOther value, out byte result)
	{
		return TryConvertFromSaturating(value, out result);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static bool TryConvertFromSaturating<TOther>(TOther value, out byte result) where TOther : INumberBase<TOther>
	{
		if (typeof(TOther) == typeof(char))
		{
			char c = (char)(object)value;
			result = ((c >= 'ÿ') ? byte.MaxValue : ((byte)c));
			return true;
		}
		if (typeof(TOther) == typeof(decimal))
		{
			decimal num = (decimal)(object)value;
			result = (byte)((num >= 255m) ? byte.MaxValue : ((!(num <= 0m)) ? ((byte)num) : 0));
			return true;
		}
		if (typeof(TOther) == typeof(ushort))
		{
			ushort num2 = (ushort)(object)value;
			result = ((num2 >= 255) ? byte.MaxValue : ((byte)num2));
			return true;
		}
		if (typeof(TOther) == typeof(uint))
		{
			uint num3 = (uint)(object)value;
			result = ((num3 >= 255) ? byte.MaxValue : ((byte)num3));
			return true;
		}
		if (typeof(TOther) == typeof(ulong))
		{
			ulong num4 = (ulong)(object)value;
			result = ((num4 >= 255) ? byte.MaxValue : ((byte)num4));
			return true;
		}
		if (typeof(TOther) == typeof(UInt128))
		{
			UInt128 uInt = (UInt128)(object)value;
			result = ((uInt >= byte.MaxValue) ? byte.MaxValue : ((byte)uInt));
			return true;
		}
		if (typeof(TOther) == typeof(nuint))
		{
			nuint num5 = (nuint)(object)value;
			result = ((num5 >= 255) ? byte.MaxValue : ((byte)num5));
			return true;
		}
		result = 0;
		return false;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	static bool INumberBase<byte>.TryConvertFromTruncating<TOther>(TOther value, out byte result)
	{
		return TryConvertFromTruncating(value, out result);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static bool TryConvertFromTruncating<TOther>(TOther value, out byte result) where TOther : INumberBase<TOther>
	{
		if (typeof(TOther) == typeof(char))
		{
			char c = (char)(object)value;
			result = (byte)c;
			return true;
		}
		if (typeof(TOther) == typeof(decimal))
		{
			decimal num = (decimal)(object)value;
			result = (byte)((num >= 255m) ? byte.MaxValue : ((!(num <= 0m)) ? ((byte)num) : 0));
			return true;
		}
		if (typeof(TOther) == typeof(ushort))
		{
			ushort num2 = (ushort)(object)value;
			result = (byte)num2;
			return true;
		}
		if (typeof(TOther) == typeof(uint))
		{
			uint num3 = (uint)(object)value;
			result = (byte)num3;
			return true;
		}
		if (typeof(TOther) == typeof(ulong))
		{
			ulong num4 = (ulong)(object)value;
			result = (byte)num4;
			return true;
		}
		if (typeof(TOther) == typeof(UInt128))
		{
			UInt128 uInt = (UInt128)(object)value;
			result = (byte)uInt;
			return true;
		}
		if (typeof(TOther) == typeof(nuint))
		{
			nuint num5 = (nuint)(object)value;
			result = (byte)num5;
			return true;
		}
		result = 0;
		return false;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	static bool INumberBase<byte>.TryConvertToChecked<TOther>(byte value, [MaybeNullWhen(false)] out TOther result)
	{
		if (typeof(TOther) == typeof(double))
		{
			double num = (int)value;
			result = (TOther)(object)num;
			return true;
		}
		if (typeof(TOther) == typeof(Half))
		{
			Half half = value;
			result = (TOther)(object)half;
			return true;
		}
		if (typeof(TOther) == typeof(short))
		{
			short num2 = value;
			result = (TOther)(object)num2;
			return true;
		}
		if (typeof(TOther) == typeof(int))
		{
			result = (TOther)(object)(int)value;
			return true;
		}
		if (typeof(TOther) == typeof(long))
		{
			long num3 = value;
			result = (TOther)(object)num3;
			return true;
		}
		if (typeof(TOther) == typeof(Int128))
		{
			Int128 @int = value;
			result = (TOther)(object)@int;
			return true;
		}
		if (typeof(TOther) == typeof(nint))
		{
			nint num4 = value;
			result = (TOther)(object)num4;
			return true;
		}
		if (typeof(TOther) == typeof(sbyte))
		{
			sbyte b = checked((sbyte)value);
			result = (TOther)(object)b;
			return true;
		}
		if (typeof(TOther) == typeof(float))
		{
			float num5 = (int)value;
			result = (TOther)(object)num5;
			return true;
		}
		result = default(TOther);
		return false;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	static bool INumberBase<byte>.TryConvertToSaturating<TOther>(byte value, [MaybeNullWhen(false)] out TOther result)
	{
		if (typeof(TOther) == typeof(double))
		{
			double num = (int)value;
			result = (TOther)(object)num;
			return true;
		}
		if (typeof(TOther) == typeof(Half))
		{
			Half half = value;
			result = (TOther)(object)half;
			return true;
		}
		if (typeof(TOther) == typeof(short))
		{
			short num2 = value;
			result = (TOther)(object)num2;
			return true;
		}
		if (typeof(TOther) == typeof(int))
		{
			result = (TOther)(object)(int)value;
			return true;
		}
		if (typeof(TOther) == typeof(long))
		{
			long num3 = value;
			result = (TOther)(object)num3;
			return true;
		}
		if (typeof(TOther) == typeof(Int128))
		{
			Int128 @int = value;
			result = (TOther)(object)@int;
			return true;
		}
		if (typeof(TOther) == typeof(nint))
		{
			nint num4 = value;
			result = (TOther)(object)num4;
			return true;
		}
		if (typeof(TOther) == typeof(sbyte))
		{
			sbyte b = ((value >= 127) ? sbyte.MaxValue : ((sbyte)value));
			result = (TOther)(object)b;
			return true;
		}
		if (typeof(TOther) == typeof(float))
		{
			float num5 = (int)value;
			result = (TOther)(object)num5;
			return true;
		}
		result = default(TOther);
		return false;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	static bool INumberBase<byte>.TryConvertToTruncating<TOther>(byte value, [MaybeNullWhen(false)] out TOther result)
	{
		if (typeof(TOther) == typeof(double))
		{
			double num = (int)value;
			result = (TOther)(object)num;
			return true;
		}
		if (typeof(TOther) == typeof(Half))
		{
			Half half = value;
			result = (TOther)(object)half;
			return true;
		}
		if (typeof(TOther) == typeof(short))
		{
			short num2 = value;
			result = (TOther)(object)num2;
			return true;
		}
		if (typeof(TOther) == typeof(int))
		{
			result = (TOther)(object)(int)value;
			return true;
		}
		if (typeof(TOther) == typeof(long))
		{
			long num3 = value;
			result = (TOther)(object)num3;
			return true;
		}
		if (typeof(TOther) == typeof(Int128))
		{
			Int128 @int = value;
			result = (TOther)(object)@int;
			return true;
		}
		if (typeof(TOther) == typeof(nint))
		{
			nint num4 = value;
			result = (TOther)(object)num4;
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
			float num5 = (int)value;
			result = (TOther)(object)num5;
			return true;
		}
		result = default(TOther);
		return false;
	}

	public static bool TryParse([NotNullWhen(true)] string? s, IFormatProvider? provider, out byte result)
	{
		return TryParse(s, NumberStyles.Integer, provider, out result);
	}

	static byte IShiftOperators<byte, int, byte>.operator <<(byte value, int shiftAmount)
	{
		return (byte)(value << shiftAmount);
	}

	static byte IShiftOperators<byte, int, byte>.operator >>(byte value, int shiftAmount)
	{
		return (byte)(value >> shiftAmount);
	}

	static byte IShiftOperators<byte, int, byte>.operator >>>(byte value, int shiftAmount)
	{
		return (byte)((uint)value >> shiftAmount);
	}

	public static byte Parse(ReadOnlySpan<char> s, IFormatProvider? provider)
	{
		return Parse(s, NumberStyles.Integer, provider);
	}

	public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, out byte result)
	{
		return TryParse(s, NumberStyles.Integer, provider, out result);
	}

	static byte ISubtractionOperators<byte, byte, byte>.operator -(byte left, byte right)
	{
		return (byte)(left - right);
	}

	static byte ISubtractionOperators<byte, byte, byte>.operator checked -(byte left, byte right)
	{
		return checked((byte)(left - right));
	}

	static byte IUnaryNegationOperators<byte, byte>.operator -(byte value)
	{
		return (byte)(-value);
	}

	static byte IUnaryNegationOperators<byte, byte>.operator checked -(byte value)
	{
		return checked((byte)(-value));
	}

	static byte IUnaryPlusOperators<byte, byte>.operator +(byte value)
	{
		return value;
	}

	public static byte Parse(ReadOnlySpan<byte> utf8Text, NumberStyles style = NumberStyles.Integer, IFormatProvider? provider = null)
	{
		NumberFormatInfo.ValidateParseStyleInteger(style);
		return Number.ParseBinaryInteger<byte, byte>(utf8Text, style, NumberFormatInfo.GetInstance(provider));
	}

	public static bool TryParse(ReadOnlySpan<byte> utf8Text, NumberStyles style, IFormatProvider? provider, out byte result)
	{
		NumberFormatInfo.ValidateParseStyleInteger(style);
		return Number.TryParseBinaryInteger<byte, byte>(utf8Text, style, NumberFormatInfo.GetInstance(provider), out result) == Number.ParsingStatus.OK;
	}

	public static byte Parse(ReadOnlySpan<byte> utf8Text, IFormatProvider? provider)
	{
		return Parse(utf8Text, NumberStyles.Integer, provider);
	}

	public static bool TryParse(ReadOnlySpan<byte> utf8Text, IFormatProvider? provider, out byte result)
	{
		return TryParse(utf8Text, NumberStyles.Integer, provider, out result);
	}

	static byte IUtfChar<byte>.CastFrom(byte value)
	{
		return value;
	}

	static byte IUtfChar<byte>.CastFrom(char value)
	{
		return (byte)value;
	}

	static byte IUtfChar<byte>.CastFrom(int value)
	{
		return (byte)value;
	}

	static byte IUtfChar<byte>.CastFrom(uint value)
	{
		return (byte)value;
	}

	static byte IUtfChar<byte>.CastFrom(ulong value)
	{
		return (byte)value;
	}

	static uint IUtfChar<byte>.CastToUInt32(byte value)
	{
		return value;
	}

	static bool IBinaryIntegerParseAndFormatInfo<byte>.IsGreaterThanAsUnsigned(byte left, byte right)
	{
		return left > right;
	}

	static byte IBinaryIntegerParseAndFormatInfo<byte>.MultiplyBy10(byte value)
	{
		return (byte)(value * 10);
	}

	static byte IBinaryIntegerParseAndFormatInfo<byte>.MultiplyBy16(byte value)
	{
		return (byte)(value * 16);
	}
}
