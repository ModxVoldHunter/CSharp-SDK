using System.Buffers.Binary;
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
public readonly struct UInt64 : IComparable, IConvertible, ISpanFormattable, IFormattable, IComparable<ulong>, IEquatable<ulong>, IBinaryInteger<ulong>, IBinaryNumber<ulong>, IBitwiseOperators<ulong, ulong, ulong>, INumber<ulong>, IComparisonOperators<ulong, ulong, bool>, IEqualityOperators<ulong, ulong, bool>, IModulusOperators<ulong, ulong, ulong>, INumberBase<ulong>, IAdditionOperators<ulong, ulong, ulong>, IAdditiveIdentity<ulong, ulong>, IDecrementOperators<ulong>, IDivisionOperators<ulong, ulong, ulong>, IIncrementOperators<ulong>, IMultiplicativeIdentity<ulong, ulong>, IMultiplyOperators<ulong, ulong, ulong>, ISpanParsable<ulong>, IParsable<ulong>, ISubtractionOperators<ulong, ulong, ulong>, IUnaryPlusOperators<ulong, ulong>, IUnaryNegationOperators<ulong, ulong>, IUtf8SpanFormattable, IUtf8SpanParsable<ulong>, IShiftOperators<ulong, int, ulong>, IMinMaxValue<ulong>, IUnsignedNumber<ulong>, IBinaryIntegerParseAndFormatInfo<ulong>
{
	private readonly ulong m_value;

	public const ulong MaxValue = 18446744073709551615uL;

	public const ulong MinValue = 0uL;

	static ulong IAdditiveIdentity<ulong, ulong>.AdditiveIdentity => 0uL;

	static ulong IBinaryNumber<ulong>.AllBitsSet => ulong.MaxValue;

	static ulong IMinMaxValue<ulong>.MinValue => 0uL;

	static ulong IMinMaxValue<ulong>.MaxValue => ulong.MaxValue;

	static ulong IMultiplicativeIdentity<ulong, ulong>.MultiplicativeIdentity => 1uL;

	static ulong INumberBase<ulong>.One => 1uL;

	static int INumberBase<ulong>.Radix => 2;

	static ulong INumberBase<ulong>.Zero => 0uL;

	static bool IBinaryIntegerParseAndFormatInfo<ulong>.IsSigned => false;

	static int IBinaryIntegerParseAndFormatInfo<ulong>.MaxDigitCount => 20;

	static int IBinaryIntegerParseAndFormatInfo<ulong>.MaxHexDigitCount => 16;

	static ulong IBinaryIntegerParseAndFormatInfo<ulong>.MaxValueDiv10 => 1844674407370955161uL;

	static string IBinaryIntegerParseAndFormatInfo<ulong>.OverflowMessage => SR.Overflow_UInt64;

	public int CompareTo(object? value)
	{
		if (value == null)
		{
			return 1;
		}
		if (value is ulong num)
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
		throw new ArgumentException(SR.Arg_MustBeUInt64);
	}

	public int CompareTo(ulong value)
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
		if (!(obj is ulong))
		{
			return false;
		}
		return this == (ulong)obj;
	}

	[NonVersionable]
	public bool Equals(ulong obj)
	{
		return this == obj;
	}

	public override int GetHashCode()
	{
		return (int)this ^ (int)(this >> 32);
	}

	public override string ToString()
	{
		return Number.UInt64ToDecStr(this);
	}

	public string ToString(IFormatProvider? provider)
	{
		return Number.UInt64ToDecStr(this);
	}

	public string ToString([StringSyntax("NumericFormat")] string? format)
	{
		return Number.FormatUInt64(this, format, null);
	}

	public string ToString([StringSyntax("NumericFormat")] string? format, IFormatProvider? provider)
	{
		return Number.FormatUInt64(this, format, provider);
	}

	public bool TryFormat(Span<char> destination, out int charsWritten, [StringSyntax("NumericFormat")] ReadOnlySpan<char> format = default(ReadOnlySpan<char>), IFormatProvider? provider = null)
	{
		return Number.TryFormatUInt64(this, format, provider, destination, out charsWritten);
	}

	public bool TryFormat(Span<byte> utf8Destination, out int bytesWritten, [StringSyntax("NumericFormat")] ReadOnlySpan<char> format = default(ReadOnlySpan<char>), IFormatProvider? provider = null)
	{
		return Number.TryFormatUInt64(this, format, provider, utf8Destination, out bytesWritten);
	}

	public static ulong Parse(string s)
	{
		return Parse(s, NumberStyles.Integer, null);
	}

	public static ulong Parse(string s, NumberStyles style)
	{
		return Parse(s, style, null);
	}

	public static ulong Parse(string s, IFormatProvider? provider)
	{
		return Parse(s, NumberStyles.Integer, provider);
	}

	public static ulong Parse(string s, NumberStyles style, IFormatProvider? provider)
	{
		if (s == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.s);
		}
		return Parse(s.AsSpan(), style, provider);
	}

	public static ulong Parse(ReadOnlySpan<char> s, NumberStyles style = NumberStyles.Integer, IFormatProvider? provider = null)
	{
		NumberFormatInfo.ValidateParseStyleInteger(style);
		return Number.ParseBinaryInteger<char, ulong>(s, style, NumberFormatInfo.GetInstance(provider));
	}

	public static bool TryParse([NotNullWhen(true)] string? s, out ulong result)
	{
		return TryParse(s, NumberStyles.Integer, null, out result);
	}

	public static bool TryParse(ReadOnlySpan<char> s, out ulong result)
	{
		return TryParse(s, NumberStyles.Integer, null, out result);
	}

	public static bool TryParse(ReadOnlySpan<byte> utf8Text, out ulong result)
	{
		return TryParse(utf8Text, NumberStyles.Integer, null, out result);
	}

	public static bool TryParse([NotNullWhen(true)] string? s, NumberStyles style, IFormatProvider? provider, out ulong result)
	{
		NumberFormatInfo.ValidateParseStyleInteger(style);
		if (s == null)
		{
			result = 0uL;
			return false;
		}
		return Number.TryParseBinaryInteger<char, ulong>(s.AsSpan(), style, NumberFormatInfo.GetInstance(provider), out result) == Number.ParsingStatus.OK;
	}

	public static bool TryParse(ReadOnlySpan<char> s, NumberStyles style, IFormatProvider? provider, out ulong result)
	{
		NumberFormatInfo.ValidateParseStyleInteger(style);
		return Number.TryParseBinaryInteger<char, ulong>(s, style, NumberFormatInfo.GetInstance(provider), out result) == Number.ParsingStatus.OK;
	}

	public TypeCode GetTypeCode()
	{
		return TypeCode.UInt64;
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
		return Convert.ToInt64(this);
	}

	ulong IConvertible.ToUInt64(IFormatProvider provider)
	{
		return this;
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
		throw new InvalidCastException(SR.Format(SR.InvalidCast_FromTo, "UInt64", "DateTime"));
	}

	object IConvertible.ToType(Type type, IFormatProvider provider)
	{
		return Convert.DefaultToType(this, type, provider);
	}

	static ulong IAdditionOperators<ulong, ulong, ulong>.operator +(ulong left, ulong right)
	{
		return left + right;
	}

	static ulong IAdditionOperators<ulong, ulong, ulong>.operator checked +(ulong left, ulong right)
	{
		return checked(left + right);
	}

	public static (ulong Quotient, ulong Remainder) DivRem(ulong left, ulong right)
	{
		return Math.DivRem(left, right);
	}

	[Intrinsic]
	public static ulong LeadingZeroCount(ulong value)
	{
		return (ulong)BitOperations.LeadingZeroCount(value);
	}

	[Intrinsic]
	public static ulong PopCount(ulong value)
	{
		return (ulong)BitOperations.PopCount(value);
	}

	[Intrinsic]
	public static ulong RotateLeft(ulong value, int rotateAmount)
	{
		return BitOperations.RotateLeft(value, rotateAmount);
	}

	[Intrinsic]
	public static ulong RotateRight(ulong value, int rotateAmount)
	{
		return BitOperations.RotateRight(value, rotateAmount);
	}

	[Intrinsic]
	public static ulong TrailingZeroCount(ulong value)
	{
		return (ulong)BitOperations.TrailingZeroCount(value);
	}

	static bool IBinaryInteger<ulong>.TryReadBigEndian(ReadOnlySpan<byte> source, bool isUnsigned, out ulong value)
	{
		ulong num = 0uL;
		if (source.Length != 0)
		{
			if (!isUnsigned && sbyte.IsNegative((sbyte)source[0]))
			{
				value = num;
				return false;
			}
			if (source.Length > 8)
			{
				if (source.Slice(0, source.Length - 8).ContainsAnyExcept<byte>(0))
				{
					value = num;
					return false;
				}
			}
			ref byte reference = ref MemoryMarshal.GetReference(source);
			if (source.Length >= 8)
			{
				num = Unsafe.ReadUnaligned<ulong>(ref Unsafe.Add(ref reference, source.Length - 8));
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
			}
		}
		value = num;
		return true;
	}

	static bool IBinaryInteger<ulong>.TryReadLittleEndian(ReadOnlySpan<byte> source, bool isUnsigned, out ulong value)
	{
		ulong num = 0uL;
		if (source.Length != 0)
		{
			if (!isUnsigned)
			{
				if (sbyte.IsNegative((sbyte)source[source.Length - 1]))
				{
					value = num;
					return false;
				}
			}
			if (source.Length > 8)
			{
				if (source.Slice(8, source.Length - 8).ContainsAnyExcept<byte>(0))
				{
					value = num;
					return false;
				}
			}
			ref byte reference = ref MemoryMarshal.GetReference(source);
			if (source.Length >= 8)
			{
				num = Unsafe.ReadUnaligned<ulong>(ref reference);
				if (BitConverter.IsLittleEndian)
				{
					goto IL_00a4;
				}
			}
			for (int i = 0; i < source.Length; i++)
			{
				ulong num2 = Unsafe.Add(ref reference, i);
				num2 <<= i * 8;
				num |= num2;
			}
		}
		goto IL_00a4;
		IL_00a4:
		value = num;
		return true;
	}

	int IBinaryInteger<ulong>.GetShortestBitLength()
	{
		return 64 - BitOperations.LeadingZeroCount(this);
	}

	int IBinaryInteger<ulong>.GetByteCount()
	{
		return 8;
	}

	bool IBinaryInteger<ulong>.TryWriteBigEndian(Span<byte> destination, out int bytesWritten)
	{
		if (destination.Length >= 8)
		{
			if (!BitConverter.IsLittleEndian)
			{
			}
			ulong value = BinaryPrimitives.ReverseEndianness(this);
			Unsafe.WriteUnaligned(ref MemoryMarshal.GetReference(destination), value);
			bytesWritten = 8;
			return true;
		}
		bytesWritten = 0;
		return false;
	}

	bool IBinaryInteger<ulong>.TryWriteLittleEndian(Span<byte> destination, out int bytesWritten)
	{
		if (destination.Length >= 8)
		{
			if (!BitConverter.IsLittleEndian)
			{
			}
			ulong value = this;
			Unsafe.WriteUnaligned(ref MemoryMarshal.GetReference(destination), value);
			bytesWritten = 8;
			return true;
		}
		bytesWritten = 0;
		return false;
	}

	public static bool IsPow2(ulong value)
	{
		return BitOperations.IsPow2(value);
	}

	[Intrinsic]
	public static ulong Log2(ulong value)
	{
		return (ulong)BitOperations.Log2(value);
	}

	static ulong IBitwiseOperators<ulong, ulong, ulong>.operator &(ulong left, ulong right)
	{
		return left & right;
	}

	static ulong IBitwiseOperators<ulong, ulong, ulong>.operator |(ulong left, ulong right)
	{
		return left | right;
	}

	static ulong IBitwiseOperators<ulong, ulong, ulong>.operator ^(ulong left, ulong right)
	{
		return left ^ right;
	}

	static ulong IBitwiseOperators<ulong, ulong, ulong>.operator ~(ulong value)
	{
		return ~value;
	}

	static bool IComparisonOperators<ulong, ulong, bool>.operator <(ulong left, ulong right)
	{
		return left < right;
	}

	static bool IComparisonOperators<ulong, ulong, bool>.operator <=(ulong left, ulong right)
	{
		return left <= right;
	}

	static bool IComparisonOperators<ulong, ulong, bool>.operator >(ulong left, ulong right)
	{
		return left > right;
	}

	static bool IComparisonOperators<ulong, ulong, bool>.operator >=(ulong left, ulong right)
	{
		return left >= right;
	}

	static ulong IDecrementOperators<ulong>.operator --(ulong value)
	{
		return --value;
	}

	static ulong IDecrementOperators<ulong>.operator checked --(ulong value)
	{
		return value = checked(value - 1);
	}

	static ulong IDivisionOperators<ulong, ulong, ulong>.operator /(ulong left, ulong right)
	{
		return left / right;
	}

	static bool IEqualityOperators<ulong, ulong, bool>.operator ==(ulong left, ulong right)
	{
		return left == right;
	}

	static bool IEqualityOperators<ulong, ulong, bool>.operator !=(ulong left, ulong right)
	{
		return left != right;
	}

	static ulong IIncrementOperators<ulong>.operator ++(ulong value)
	{
		return ++value;
	}

	static ulong IIncrementOperators<ulong>.operator checked ++(ulong value)
	{
		return value = checked(value + 1);
	}

	static ulong IModulusOperators<ulong, ulong, ulong>.operator %(ulong left, ulong right)
	{
		return left % right;
	}

	static ulong IMultiplyOperators<ulong, ulong, ulong>.operator *(ulong left, ulong right)
	{
		return left * right;
	}

	static ulong IMultiplyOperators<ulong, ulong, ulong>.operator checked *(ulong left, ulong right)
	{
		return checked(left * right);
	}

	public static ulong Clamp(ulong value, ulong min, ulong max)
	{
		return Math.Clamp(value, min, max);
	}

	static ulong INumber<ulong>.CopySign(ulong value, ulong sign)
	{
		return value;
	}

	public static ulong Max(ulong x, ulong y)
	{
		return Math.Max(x, y);
	}

	static ulong INumber<ulong>.MaxNumber(ulong x, ulong y)
	{
		return Max(x, y);
	}

	public static ulong Min(ulong x, ulong y)
	{
		return Math.Min(x, y);
	}

	static ulong INumber<ulong>.MinNumber(ulong x, ulong y)
	{
		return Min(x, y);
	}

	public static int Sign(ulong value)
	{
		return (value != 0) ? 1 : 0;
	}

	static ulong INumberBase<ulong>.Abs(ulong value)
	{
		return value;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static ulong CreateChecked<TOther>(TOther value) where TOther : INumberBase<TOther>
	{
		if (typeof(TOther) == typeof(ulong))
		{
			return (ulong)(object)value;
		}
		if (!TryConvertFromChecked(value, out var result) && !TOther.TryConvertToChecked<ulong>(value, out result))
		{
			ThrowHelper.ThrowNotSupportedException();
		}
		return result;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static ulong CreateSaturating<TOther>(TOther value) where TOther : INumberBase<TOther>
	{
		if (typeof(TOther) == typeof(ulong))
		{
			return (ulong)(object)value;
		}
		if (!TryConvertFromSaturating(value, out var result) && !TOther.TryConvertToSaturating<ulong>(value, out result))
		{
			ThrowHelper.ThrowNotSupportedException();
		}
		return result;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static ulong CreateTruncating<TOther>(TOther value) where TOther : INumberBase<TOther>
	{
		if (typeof(TOther) == typeof(ulong))
		{
			return (ulong)(object)value;
		}
		if (!TryConvertFromTruncating(value, out var result) && !TOther.TryConvertToTruncating<ulong>(value, out result))
		{
			ThrowHelper.ThrowNotSupportedException();
		}
		return result;
	}

	static bool INumberBase<ulong>.IsCanonical(ulong value)
	{
		return true;
	}

	static bool INumberBase<ulong>.IsComplexNumber(ulong value)
	{
		return false;
	}

	public static bool IsEvenInteger(ulong value)
	{
		return (value & 1) == 0;
	}

	static bool INumberBase<ulong>.IsFinite(ulong value)
	{
		return true;
	}

	static bool INumberBase<ulong>.IsImaginaryNumber(ulong value)
	{
		return false;
	}

	static bool INumberBase<ulong>.IsInfinity(ulong value)
	{
		return false;
	}

	static bool INumberBase<ulong>.IsInteger(ulong value)
	{
		return true;
	}

	static bool INumberBase<ulong>.IsNaN(ulong value)
	{
		return false;
	}

	static bool INumberBase<ulong>.IsNegative(ulong value)
	{
		return false;
	}

	static bool INumberBase<ulong>.IsNegativeInfinity(ulong value)
	{
		return false;
	}

	static bool INumberBase<ulong>.IsNormal(ulong value)
	{
		return value != 0;
	}

	public static bool IsOddInteger(ulong value)
	{
		return (value & 1) != 0;
	}

	static bool INumberBase<ulong>.IsPositive(ulong value)
	{
		return true;
	}

	static bool INumberBase<ulong>.IsPositiveInfinity(ulong value)
	{
		return false;
	}

	static bool INumberBase<ulong>.IsRealNumber(ulong value)
	{
		return true;
	}

	static bool INumberBase<ulong>.IsSubnormal(ulong value)
	{
		return false;
	}

	static bool INumberBase<ulong>.IsZero(ulong value)
	{
		return value == 0;
	}

	static ulong INumberBase<ulong>.MaxMagnitude(ulong x, ulong y)
	{
		return Max(x, y);
	}

	static ulong INumberBase<ulong>.MaxMagnitudeNumber(ulong x, ulong y)
	{
		return Max(x, y);
	}

	static ulong INumberBase<ulong>.MinMagnitude(ulong x, ulong y)
	{
		return Min(x, y);
	}

	static ulong INumberBase<ulong>.MinMagnitudeNumber(ulong x, ulong y)
	{
		return Min(x, y);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	static bool INumberBase<ulong>.TryConvertFromChecked<TOther>(TOther value, out ulong result)
	{
		return TryConvertFromChecked(value, out result);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static bool TryConvertFromChecked<TOther>(TOther value, out ulong result) where TOther : INumberBase<TOther>
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
			result = (ulong)num;
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
		if (typeof(TOther) == typeof(UInt128))
		{
			UInt128 uInt = (UInt128)(object)value;
			result = checked((ulong)uInt);
			return true;
		}
		if (typeof(TOther) == typeof(nuint))
		{
			nuint num4 = (nuint)(object)value;
			result = num4;
			return true;
		}
		result = 0uL;
		return false;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	static bool INumberBase<ulong>.TryConvertFromSaturating<TOther>(TOther value, out ulong result)
	{
		return TryConvertFromSaturating(value, out result);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static bool TryConvertFromSaturating<TOther>(TOther value, out ulong result) where TOther : INumberBase<TOther>
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
			result = ((num >= 18446744073709551615m) ? ulong.MaxValue : ((num <= 0m) ? 0 : ((ulong)num)));
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
		if (typeof(TOther) == typeof(UInt128))
		{
			UInt128 uInt = (UInt128)(object)value;
			result = ((uInt >= ulong.MaxValue) ? ulong.MaxValue : ((ulong)uInt));
			return true;
		}
		if (typeof(TOther) == typeof(nuint))
		{
			nuint num4 = (nuint)(object)value;
			result = num4;
			return true;
		}
		result = 0uL;
		return false;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	static bool INumberBase<ulong>.TryConvertFromTruncating<TOther>(TOther value, out ulong result)
	{
		return TryConvertFromTruncating(value, out result);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static bool TryConvertFromTruncating<TOther>(TOther value, out ulong result) where TOther : INumberBase<TOther>
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
			result = ((num >= 18446744073709551615m) ? ulong.MaxValue : ((num <= 0m) ? 0 : ((ulong)num)));
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
		if (typeof(TOther) == typeof(UInt128))
		{
			UInt128 uInt = (UInt128)(object)value;
			result = (ulong)uInt;
			return true;
		}
		if (typeof(TOther) == typeof(nuint))
		{
			nuint num4 = (nuint)(object)value;
			result = num4;
			return true;
		}
		result = 0uL;
		return false;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	static bool INumberBase<ulong>.TryConvertToChecked<TOther>(ulong value, [MaybeNullWhen(false)] out TOther result)
	{
		if (typeof(TOther) == typeof(double))
		{
			double num = value;
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
				Int128 @int = value;
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
				float num6 = value;
				result = (TOther)(object)num6;
				return true;
			}
			result = default(TOther);
			return false;
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	static bool INumberBase<ulong>.TryConvertToSaturating<TOther>(ulong value, [MaybeNullWhen(false)] out TOther result)
	{
		if (typeof(TOther) == typeof(double))
		{
			double num = value;
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
			short num2 = ((value >= 32767) ? short.MaxValue : ((short)value));
			result = (TOther)(object)num2;
			return true;
		}
		if (typeof(TOther) == typeof(int))
		{
			int num3 = (int)((value >= int.MaxValue) ? int.MaxValue : value);
			result = (TOther)(object)num3;
			return true;
		}
		if (typeof(TOther) == typeof(long))
		{
			long num4 = (long)((value >= long.MaxValue) ? long.MaxValue : value);
			result = (TOther)(object)num4;
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
			nint num5 = ((value >= (ulong)IntPtr.MaxValue) ? IntPtr.MaxValue : ((nint)value));
			result = (TOther)(object)num5;
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
			float num6 = value;
			result = (TOther)(object)num6;
			return true;
		}
		result = default(TOther);
		return false;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	static bool INumberBase<ulong>.TryConvertToTruncating<TOther>(ulong value, [MaybeNullWhen(false)] out TOther result)
	{
		if (typeof(TOther) == typeof(double))
		{
			double num = value;
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
			result = (TOther)(object)(long)value;
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
			nint num4 = (nint)value;
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
			float num5 = value;
			result = (TOther)(object)num5;
			return true;
		}
		result = default(TOther);
		return false;
	}

	public static bool TryParse([NotNullWhen(true)] string? s, IFormatProvider? provider, out ulong result)
	{
		return TryParse(s, NumberStyles.Integer, provider, out result);
	}

	static ulong IShiftOperators<ulong, int, ulong>.operator <<(ulong value, int shiftAmount)
	{
		return value << shiftAmount;
	}

	static ulong IShiftOperators<ulong, int, ulong>.operator >>(ulong value, int shiftAmount)
	{
		return value >> shiftAmount;
	}

	static ulong IShiftOperators<ulong, int, ulong>.operator >>>(ulong value, int shiftAmount)
	{
		return value >> shiftAmount;
	}

	public static ulong Parse(ReadOnlySpan<char> s, IFormatProvider? provider)
	{
		return Parse(s, NumberStyles.Integer, provider);
	}

	public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, out ulong result)
	{
		return TryParse(s, NumberStyles.Integer, provider, out result);
	}

	static ulong ISubtractionOperators<ulong, ulong, ulong>.operator -(ulong left, ulong right)
	{
		return left - right;
	}

	static ulong ISubtractionOperators<ulong, ulong, ulong>.operator checked -(ulong left, ulong right)
	{
		return checked(left - right);
	}

	static ulong IUnaryNegationOperators<ulong, ulong>.operator -(ulong value)
	{
		return 0 - value;
	}

	static ulong IUnaryNegationOperators<ulong, ulong>.operator checked -(ulong value)
	{
		return checked(0 - value);
	}

	static ulong IUnaryPlusOperators<ulong, ulong>.operator +(ulong value)
	{
		return value;
	}

	public static ulong Parse(ReadOnlySpan<byte> utf8Text, NumberStyles style = NumberStyles.Integer, IFormatProvider? provider = null)
	{
		NumberFormatInfo.ValidateParseStyleInteger(style);
		return Number.ParseBinaryInteger<byte, ulong>(utf8Text, style, NumberFormatInfo.GetInstance(provider));
	}

	public static bool TryParse(ReadOnlySpan<byte> utf8Text, NumberStyles style, IFormatProvider? provider, out ulong result)
	{
		NumberFormatInfo.ValidateParseStyleInteger(style);
		return Number.TryParseBinaryInteger<byte, ulong>(utf8Text, style, NumberFormatInfo.GetInstance(provider), out result) == Number.ParsingStatus.OK;
	}

	public static ulong Parse(ReadOnlySpan<byte> utf8Text, IFormatProvider? provider)
	{
		return Parse(utf8Text, NumberStyles.Integer, provider);
	}

	public static bool TryParse(ReadOnlySpan<byte> utf8Text, IFormatProvider? provider, out ulong result)
	{
		return TryParse(utf8Text, NumberStyles.Integer, provider, out result);
	}

	static bool IBinaryIntegerParseAndFormatInfo<ulong>.IsGreaterThanAsUnsigned(ulong left, ulong right)
	{
		return left > right;
	}

	static ulong IBinaryIntegerParseAndFormatInfo<ulong>.MultiplyBy10(ulong value)
	{
		return value * 10;
	}

	static ulong IBinaryIntegerParseAndFormatInfo<ulong>.MultiplyBy16(ulong value)
	{
		return value * 16;
	}
}
