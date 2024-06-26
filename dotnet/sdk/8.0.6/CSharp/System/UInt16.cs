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
public readonly struct UInt16 : IComparable, IConvertible, ISpanFormattable, IFormattable, IComparable<ushort>, IEquatable<ushort>, IBinaryInteger<ushort>, IBinaryNumber<ushort>, IBitwiseOperators<ushort, ushort, ushort>, INumber<ushort>, IComparisonOperators<ushort, ushort, bool>, IEqualityOperators<ushort, ushort, bool>, IModulusOperators<ushort, ushort, ushort>, INumberBase<ushort>, IAdditionOperators<ushort, ushort, ushort>, IAdditiveIdentity<ushort, ushort>, IDecrementOperators<ushort>, IDivisionOperators<ushort, ushort, ushort>, IIncrementOperators<ushort>, IMultiplicativeIdentity<ushort, ushort>, IMultiplyOperators<ushort, ushort, ushort>, ISpanParsable<ushort>, IParsable<ushort>, ISubtractionOperators<ushort, ushort, ushort>, IUnaryPlusOperators<ushort, ushort>, IUnaryNegationOperators<ushort, ushort>, IUtf8SpanFormattable, IUtf8SpanParsable<ushort>, IShiftOperators<ushort, int, ushort>, IMinMaxValue<ushort>, IUnsignedNumber<ushort>, IBinaryIntegerParseAndFormatInfo<ushort>
{
	private readonly ushort m_value;

	public const ushort MaxValue = 65535;

	public const ushort MinValue = 0;

	static ushort IAdditiveIdentity<ushort, ushort>.AdditiveIdentity => 0;

	static ushort IBinaryNumber<ushort>.AllBitsSet => ushort.MaxValue;

	static ushort IMinMaxValue<ushort>.MinValue => 0;

	static ushort IMinMaxValue<ushort>.MaxValue => ushort.MaxValue;

	static ushort IMultiplicativeIdentity<ushort, ushort>.MultiplicativeIdentity => 1;

	static ushort INumberBase<ushort>.One => 1;

	static int INumberBase<ushort>.Radix => 2;

	static ushort INumberBase<ushort>.Zero => 0;

	static bool IBinaryIntegerParseAndFormatInfo<ushort>.IsSigned => false;

	static int IBinaryIntegerParseAndFormatInfo<ushort>.MaxDigitCount => 5;

	static int IBinaryIntegerParseAndFormatInfo<ushort>.MaxHexDigitCount => 4;

	static ushort IBinaryIntegerParseAndFormatInfo<ushort>.MaxValueDiv10 => 6553;

	static string IBinaryIntegerParseAndFormatInfo<ushort>.OverflowMessage => SR.Overflow_UInt16;

	public int CompareTo(object? value)
	{
		if (value == null)
		{
			return 1;
		}
		if (value is ushort)
		{
			return this - (ushort)value;
		}
		throw new ArgumentException(SR.Arg_MustBeUInt16);
	}

	public int CompareTo(ushort value)
	{
		return this - value;
	}

	public override bool Equals([NotNullWhen(true)] object? obj)
	{
		if (!(obj is ushort))
		{
			return false;
		}
		return this == (ushort)obj;
	}

	[NonVersionable]
	public bool Equals(ushort obj)
	{
		return this == obj;
	}

	public override int GetHashCode()
	{
		return this;
	}

	public override string ToString()
	{
		return Number.UInt32ToDecStr(this);
	}

	public string ToString(IFormatProvider? provider)
	{
		return Number.UInt32ToDecStr(this);
	}

	public string ToString([StringSyntax("NumericFormat")] string? format)
	{
		return Number.FormatUInt32(this, format, null);
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

	public static ushort Parse(string s)
	{
		return Parse(s, NumberStyles.Integer, null);
	}

	public static ushort Parse(string s, NumberStyles style)
	{
		return Parse(s, style, null);
	}

	public static ushort Parse(string s, IFormatProvider? provider)
	{
		return Parse(s, NumberStyles.Integer, provider);
	}

	public static ushort Parse(string s, NumberStyles style, IFormatProvider? provider)
	{
		if (s == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.s);
		}
		return Parse(s.AsSpan(), style, provider);
	}

	public static ushort Parse(ReadOnlySpan<char> s, NumberStyles style = NumberStyles.Integer, IFormatProvider? provider = null)
	{
		NumberFormatInfo.ValidateParseStyleInteger(style);
		return Number.ParseBinaryInteger<char, ushort>(s, style, NumberFormatInfo.GetInstance(provider));
	}

	public static bool TryParse([NotNullWhen(true)] string? s, out ushort result)
	{
		return TryParse(s, NumberStyles.Integer, null, out result);
	}

	public static bool TryParse(ReadOnlySpan<char> s, out ushort result)
	{
		return TryParse(s, NumberStyles.Integer, null, out result);
	}

	public static bool TryParse(ReadOnlySpan<byte> utf8Text, out ushort result)
	{
		return TryParse(utf8Text, NumberStyles.Integer, null, out result);
	}

	public static bool TryParse([NotNullWhen(true)] string? s, NumberStyles style, IFormatProvider? provider, out ushort result)
	{
		NumberFormatInfo.ValidateParseStyleInteger(style);
		if (s == null)
		{
			result = 0;
			return false;
		}
		return Number.TryParseBinaryInteger<char, ushort>(s.AsSpan(), style, NumberFormatInfo.GetInstance(provider), out result) == Number.ParsingStatus.OK;
	}

	public static bool TryParse(ReadOnlySpan<char> s, NumberStyles style, IFormatProvider? provider, out ushort result)
	{
		NumberFormatInfo.ValidateParseStyleInteger(style);
		return Number.TryParseBinaryInteger<char, ushort>(s, style, NumberFormatInfo.GetInstance(provider), out result) == Number.ParsingStatus.OK;
	}

	public TypeCode GetTypeCode()
	{
		return TypeCode.UInt16;
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
		return this;
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
		throw new InvalidCastException(SR.Format(SR.InvalidCast_FromTo, "UInt16", "DateTime"));
	}

	object IConvertible.ToType(Type type, IFormatProvider provider)
	{
		return Convert.DefaultToType(this, type, provider);
	}

	static ushort IAdditionOperators<ushort, ushort, ushort>.operator +(ushort left, ushort right)
	{
		return (ushort)(left + right);
	}

	static ushort IAdditionOperators<ushort, ushort, ushort>.operator checked +(ushort left, ushort right)
	{
		return checked((ushort)(left + right));
	}

	public static (ushort Quotient, ushort Remainder) DivRem(ushort left, ushort right)
	{
		return Math.DivRem(left, right);
	}

	public static ushort LeadingZeroCount(ushort value)
	{
		return (ushort)(BitOperations.LeadingZeroCount(value) - 16);
	}

	public static ushort PopCount(ushort value)
	{
		return (ushort)BitOperations.PopCount(value);
	}

	public static ushort RotateLeft(ushort value, int rotateAmount)
	{
		return (ushort)((value << (rotateAmount & 0xF)) | (value >> ((16 - rotateAmount) & 0xF)));
	}

	public static ushort RotateRight(ushort value, int rotateAmount)
	{
		return (ushort)((value >> (rotateAmount & 0xF)) | (value << ((16 - rotateAmount) & 0xF)));
	}

	public static ushort TrailingZeroCount(ushort value)
	{
		return (ushort)(BitOperations.TrailingZeroCount(value << 16) - 16);
	}

	static bool IBinaryInteger<ushort>.TryReadBigEndian(ReadOnlySpan<byte> source, bool isUnsigned, out ushort value)
	{
		ushort num = 0;
		if (source.Length != 0)
		{
			if (!isUnsigned && sbyte.IsNegative((sbyte)source[0]))
			{
				value = num;
				return false;
			}
			if (source.Length > 2)
			{
				if (source.Slice(0, source.Length - 2).ContainsAnyExcept<byte>(0))
				{
					value = num;
					return false;
				}
			}
			ref byte reference = ref MemoryMarshal.GetReference(source);
			if (source.Length >= 2)
			{
				num = Unsafe.ReadUnaligned<ushort>(ref Unsafe.Add(ref reference, source.Length - 2));
				_ = BitConverter.IsLittleEndian;
				num = BinaryPrimitives.ReverseEndianness(num);
			}
			else
			{
				num = reference;
			}
		}
		value = num;
		return true;
	}

	static bool IBinaryInteger<ushort>.TryReadLittleEndian(ReadOnlySpan<byte> source, bool isUnsigned, out ushort value)
	{
		ushort num = 0;
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
			if (source.Length > 2)
			{
				if (source.Slice(2, source.Length - 2).ContainsAnyExcept<byte>(0))
				{
					value = num;
					return false;
				}
			}
			ref byte reference = ref MemoryMarshal.GetReference(source);
			if (source.Length >= 2)
			{
				num = Unsafe.ReadUnaligned<ushort>(ref reference);
				if (BitConverter.IsLittleEndian)
				{
					goto IL_0076;
				}
			}
			num = reference;
		}
		goto IL_0076;
		IL_0076:
		value = num;
		return true;
	}

	int IBinaryInteger<ushort>.GetShortestBitLength()
	{
		return 16 - LeadingZeroCount(this);
	}

	int IBinaryInteger<ushort>.GetByteCount()
	{
		return 2;
	}

	bool IBinaryInteger<ushort>.TryWriteBigEndian(Span<byte> destination, out int bytesWritten)
	{
		if (destination.Length >= 2)
		{
			if (!BitConverter.IsLittleEndian)
			{
			}
			ushort value = BinaryPrimitives.ReverseEndianness(this);
			Unsafe.WriteUnaligned(ref MemoryMarshal.GetReference(destination), value);
			bytesWritten = 2;
			return true;
		}
		bytesWritten = 0;
		return false;
	}

	bool IBinaryInteger<ushort>.TryWriteLittleEndian(Span<byte> destination, out int bytesWritten)
	{
		if (destination.Length >= 2)
		{
			if (!BitConverter.IsLittleEndian)
			{
			}
			ushort value = this;
			Unsafe.WriteUnaligned(ref MemoryMarshal.GetReference(destination), value);
			bytesWritten = 2;
			return true;
		}
		bytesWritten = 0;
		return false;
	}

	public static bool IsPow2(ushort value)
	{
		return BitOperations.IsPow2((uint)value);
	}

	public static ushort Log2(ushort value)
	{
		return (ushort)BitOperations.Log2(value);
	}

	static ushort IBitwiseOperators<ushort, ushort, ushort>.operator &(ushort left, ushort right)
	{
		return (ushort)(left & right);
	}

	static ushort IBitwiseOperators<ushort, ushort, ushort>.operator |(ushort left, ushort right)
	{
		return (ushort)(left | right);
	}

	static ushort IBitwiseOperators<ushort, ushort, ushort>.operator ^(ushort left, ushort right)
	{
		return (ushort)(left ^ right);
	}

	static ushort IBitwiseOperators<ushort, ushort, ushort>.operator ~(ushort value)
	{
		return (ushort)(~value);
	}

	static bool IComparisonOperators<ushort, ushort, bool>.operator <(ushort left, ushort right)
	{
		return left < right;
	}

	static bool IComparisonOperators<ushort, ushort, bool>.operator <=(ushort left, ushort right)
	{
		return left <= right;
	}

	static bool IComparisonOperators<ushort, ushort, bool>.operator >(ushort left, ushort right)
	{
		return left > right;
	}

	static bool IComparisonOperators<ushort, ushort, bool>.operator >=(ushort left, ushort right)
	{
		return left >= right;
	}

	static ushort IDecrementOperators<ushort>.operator --(ushort value)
	{
		return --value;
	}

	static ushort IDecrementOperators<ushort>.operator checked --(ushort value)
	{
		checked
		{
			return value = (ushort)(unchecked((uint)value) - 1u);
		}
	}

	static ushort IDivisionOperators<ushort, ushort, ushort>.operator /(ushort left, ushort right)
	{
		return (ushort)(left / right);
	}

	static bool IEqualityOperators<ushort, ushort, bool>.operator ==(ushort left, ushort right)
	{
		return left == right;
	}

	static bool IEqualityOperators<ushort, ushort, bool>.operator !=(ushort left, ushort right)
	{
		return left != right;
	}

	static ushort IIncrementOperators<ushort>.operator ++(ushort value)
	{
		return ++value;
	}

	static ushort IIncrementOperators<ushort>.operator checked ++(ushort value)
	{
		checked
		{
			return value = (ushort)(unchecked((uint)value) + 1u);
		}
	}

	static ushort IModulusOperators<ushort, ushort, ushort>.operator %(ushort left, ushort right)
	{
		return (ushort)(left % right);
	}

	static ushort IMultiplyOperators<ushort, ushort, ushort>.operator *(ushort left, ushort right)
	{
		return (ushort)(left * right);
	}

	static ushort IMultiplyOperators<ushort, ushort, ushort>.operator checked *(ushort left, ushort right)
	{
		return checked((ushort)(left * right));
	}

	public static ushort Clamp(ushort value, ushort min, ushort max)
	{
		return Math.Clamp(value, min, max);
	}

	static ushort INumber<ushort>.CopySign(ushort value, ushort sign)
	{
		return value;
	}

	public static ushort Max(ushort x, ushort y)
	{
		return Math.Max(x, y);
	}

	static ushort INumber<ushort>.MaxNumber(ushort x, ushort y)
	{
		return Max(x, y);
	}

	public static ushort Min(ushort x, ushort y)
	{
		return Math.Min(x, y);
	}

	static ushort INumber<ushort>.MinNumber(ushort x, ushort y)
	{
		return Min(x, y);
	}

	public static int Sign(ushort value)
	{
		return (value != 0) ? 1 : 0;
	}

	static ushort INumberBase<ushort>.Abs(ushort value)
	{
		return value;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static ushort CreateChecked<TOther>(TOther value) where TOther : INumberBase<TOther>
	{
		if (typeof(TOther) == typeof(ushort))
		{
			return (ushort)(object)value;
		}
		if (!TryConvertFromChecked(value, out var result) && !TOther.TryConvertToChecked<ushort>(value, out result))
		{
			ThrowHelper.ThrowNotSupportedException();
		}
		return result;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static ushort CreateSaturating<TOther>(TOther value) where TOther : INumberBase<TOther>
	{
		if (typeof(TOther) == typeof(ushort))
		{
			return (ushort)(object)value;
		}
		if (!TryConvertFromSaturating(value, out var result) && !TOther.TryConvertToSaturating<ushort>(value, out result))
		{
			ThrowHelper.ThrowNotSupportedException();
		}
		return result;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static ushort CreateTruncating<TOther>(TOther value) where TOther : INumberBase<TOther>
	{
		if (typeof(TOther) == typeof(ushort))
		{
			return (ushort)(object)value;
		}
		if (!TryConvertFromTruncating(value, out var result) && !TOther.TryConvertToTruncating<ushort>(value, out result))
		{
			ThrowHelper.ThrowNotSupportedException();
		}
		return result;
	}

	static bool INumberBase<ushort>.IsCanonical(ushort value)
	{
		return true;
	}

	static bool INumberBase<ushort>.IsComplexNumber(ushort value)
	{
		return false;
	}

	public static bool IsEvenInteger(ushort value)
	{
		return (value & 1) == 0;
	}

	static bool INumberBase<ushort>.IsFinite(ushort value)
	{
		return true;
	}

	static bool INumberBase<ushort>.IsImaginaryNumber(ushort value)
	{
		return false;
	}

	static bool INumberBase<ushort>.IsInfinity(ushort value)
	{
		return false;
	}

	static bool INumberBase<ushort>.IsInteger(ushort value)
	{
		return true;
	}

	static bool INumberBase<ushort>.IsNaN(ushort value)
	{
		return false;
	}

	static bool INumberBase<ushort>.IsNegative(ushort value)
	{
		return false;
	}

	static bool INumberBase<ushort>.IsNegativeInfinity(ushort value)
	{
		return false;
	}

	static bool INumberBase<ushort>.IsNormal(ushort value)
	{
		return value != 0;
	}

	public static bool IsOddInteger(ushort value)
	{
		return (value & 1) != 0;
	}

	static bool INumberBase<ushort>.IsPositive(ushort value)
	{
		return true;
	}

	static bool INumberBase<ushort>.IsPositiveInfinity(ushort value)
	{
		return false;
	}

	static bool INumberBase<ushort>.IsRealNumber(ushort value)
	{
		return true;
	}

	static bool INumberBase<ushort>.IsSubnormal(ushort value)
	{
		return false;
	}

	static bool INumberBase<ushort>.IsZero(ushort value)
	{
		return value == 0;
	}

	static ushort INumberBase<ushort>.MaxMagnitude(ushort x, ushort y)
	{
		return Max(x, y);
	}

	static ushort INumberBase<ushort>.MaxMagnitudeNumber(ushort x, ushort y)
	{
		return Max(x, y);
	}

	static ushort INumberBase<ushort>.MinMagnitude(ushort x, ushort y)
	{
		return Min(x, y);
	}

	static ushort INumberBase<ushort>.MinMagnitudeNumber(ushort x, ushort y)
	{
		return Min(x, y);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	static bool INumberBase<ushort>.TryConvertFromChecked<TOther>(TOther value, out ushort result)
	{
		return TryConvertFromChecked(value, out result);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static bool TryConvertFromChecked<TOther>(TOther value, out ushort result) where TOther : INumberBase<TOther>
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
			result = (ushort)num;
			return true;
		}
		checked
		{
			if (typeof(TOther) == typeof(uint))
			{
				uint num2 = (uint)(object)value;
				result = (ushort)num2;
				return true;
			}
			if (typeof(TOther) == typeof(ulong))
			{
				ulong num3 = (ulong)(object)value;
				result = (ushort)num3;
				return true;
			}
			if (typeof(TOther) == typeof(UInt128))
			{
				UInt128 uInt = (UInt128)(object)value;
				result = (ushort)uInt;
				return true;
			}
			if (typeof(TOther) == typeof(nuint))
			{
				nuint num4 = (nuint)(object)value;
				result = (ushort)num4;
				return true;
			}
			result = 0;
			return false;
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	static bool INumberBase<ushort>.TryConvertFromSaturating<TOther>(TOther value, out ushort result)
	{
		return TryConvertFromSaturating(value, out result);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static bool TryConvertFromSaturating<TOther>(TOther value, out ushort result) where TOther : INumberBase<TOther>
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
			result = (ushort)((num >= 65535m) ? ushort.MaxValue : ((!(num <= 0m)) ? ((ushort)num) : 0));
			return true;
		}
		if (typeof(TOther) == typeof(uint))
		{
			uint num2 = (uint)(object)value;
			result = ((num2 >= 65535) ? ushort.MaxValue : ((ushort)num2));
			return true;
		}
		if (typeof(TOther) == typeof(ulong))
		{
			ulong num3 = (ulong)(object)value;
			result = ((num3 >= 65535) ? ushort.MaxValue : ((ushort)num3));
			return true;
		}
		if (typeof(TOther) == typeof(UInt128))
		{
			UInt128 uInt = (UInt128)(object)value;
			result = ((uInt >= ushort.MaxValue) ? ushort.MaxValue : ((ushort)uInt));
			return true;
		}
		if (typeof(TOther) == typeof(nuint))
		{
			nuint num4 = (nuint)(object)value;
			result = ((num4 >= 65535) ? ushort.MaxValue : ((ushort)num4));
			return true;
		}
		result = 0;
		return false;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	static bool INumberBase<ushort>.TryConvertFromTruncating<TOther>(TOther value, out ushort result)
	{
		return TryConvertFromTruncating(value, out result);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static bool TryConvertFromTruncating<TOther>(TOther value, out ushort result) where TOther : INumberBase<TOther>
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
			result = (ushort)((num >= 65535m) ? ushort.MaxValue : ((!(num <= 0m)) ? ((ushort)num) : 0));
			return true;
		}
		if (typeof(TOther) == typeof(uint))
		{
			uint num2 = (uint)(object)value;
			result = (ushort)num2;
			return true;
		}
		if (typeof(TOther) == typeof(ulong))
		{
			ulong num3 = (ulong)(object)value;
			result = (ushort)num3;
			return true;
		}
		if (typeof(TOther) == typeof(UInt128))
		{
			UInt128 uInt = (UInt128)(object)value;
			result = (ushort)uInt;
			return true;
		}
		if (typeof(TOther) == typeof(nuint))
		{
			nuint num4 = (nuint)(object)value;
			result = (ushort)num4;
			return true;
		}
		result = 0;
		return false;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	static bool INumberBase<ushort>.TryConvertToChecked<TOther>(ushort value, [MaybeNullWhen(false)] out TOther result)
	{
		if (typeof(TOther) == typeof(double))
		{
			double num = (int)value;
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
			short num2 = checked((short)value);
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
	static bool INumberBase<ushort>.TryConvertToSaturating<TOther>(ushort value, [MaybeNullWhen(false)] out TOther result)
	{
		if (typeof(TOther) == typeof(double))
		{
			double num = (int)value;
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
	static bool INumberBase<ushort>.TryConvertToTruncating<TOther>(ushort value, [MaybeNullWhen(false)] out TOther result)
	{
		if (typeof(TOther) == typeof(double))
		{
			double num = (int)value;
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

	public static bool TryParse([NotNullWhen(true)] string? s, IFormatProvider? provider, out ushort result)
	{
		return TryParse(s, NumberStyles.Integer, provider, out result);
	}

	static ushort IShiftOperators<ushort, int, ushort>.operator <<(ushort value, int shiftAmount)
	{
		return (ushort)(value << shiftAmount);
	}

	static ushort IShiftOperators<ushort, int, ushort>.operator >>(ushort value, int shiftAmount)
	{
		return (ushort)(value >> shiftAmount);
	}

	static ushort IShiftOperators<ushort, int, ushort>.operator >>>(ushort value, int shiftAmount)
	{
		return (ushort)((uint)value >> shiftAmount);
	}

	public static ushort Parse(ReadOnlySpan<char> s, IFormatProvider? provider)
	{
		return Parse(s, NumberStyles.Integer, provider);
	}

	public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, out ushort result)
	{
		return TryParse(s, NumberStyles.Integer, provider, out result);
	}

	static ushort ISubtractionOperators<ushort, ushort, ushort>.operator -(ushort left, ushort right)
	{
		return (ushort)(left - right);
	}

	static ushort ISubtractionOperators<ushort, ushort, ushort>.operator checked -(ushort left, ushort right)
	{
		return checked((ushort)(left - right));
	}

	static ushort IUnaryNegationOperators<ushort, ushort>.operator -(ushort value)
	{
		return (ushort)(-value);
	}

	static ushort IUnaryNegationOperators<ushort, ushort>.operator checked -(ushort value)
	{
		return checked((ushort)(-value));
	}

	static ushort IUnaryPlusOperators<ushort, ushort>.operator +(ushort value)
	{
		return value;
	}

	public static ushort Parse(ReadOnlySpan<byte> utf8Text, NumberStyles style = NumberStyles.Integer, IFormatProvider? provider = null)
	{
		NumberFormatInfo.ValidateParseStyleInteger(style);
		return Number.ParseBinaryInteger<byte, ushort>(utf8Text, style, NumberFormatInfo.GetInstance(provider));
	}

	public static bool TryParse(ReadOnlySpan<byte> utf8Text, NumberStyles style, IFormatProvider? provider, out ushort result)
	{
		NumberFormatInfo.ValidateParseStyleInteger(style);
		return Number.TryParseBinaryInteger<byte, ushort>(utf8Text, style, NumberFormatInfo.GetInstance(provider), out result) == Number.ParsingStatus.OK;
	}

	public static ushort Parse(ReadOnlySpan<byte> utf8Text, IFormatProvider? provider)
	{
		return Parse(utf8Text, NumberStyles.Integer, provider);
	}

	public static bool TryParse(ReadOnlySpan<byte> utf8Text, IFormatProvider? provider, out ushort result)
	{
		return TryParse(utf8Text, NumberStyles.Integer, provider, out result);
	}

	static bool IBinaryIntegerParseAndFormatInfo<ushort>.IsGreaterThanAsUnsigned(ushort left, ushort right)
	{
		return left > right;
	}

	static ushort IBinaryIntegerParseAndFormatInfo<ushort>.MultiplyBy10(ushort value)
	{
		return (ushort)(value * 10);
	}

	static ushort IBinaryIntegerParseAndFormatInfo<ushort>.MultiplyBy16(ushort value)
	{
		return (ushort)(value * 16);
	}
}
