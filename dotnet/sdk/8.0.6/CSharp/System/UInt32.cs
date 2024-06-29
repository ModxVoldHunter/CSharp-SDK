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
public readonly struct UInt32 : IComparable, IConvertible, ISpanFormattable, IFormattable, IComparable<uint>, IEquatable<uint>, IBinaryInteger<uint>, IBinaryNumber<uint>, IBitwiseOperators<uint, uint, uint>, INumber<uint>, IComparisonOperators<uint, uint, bool>, IEqualityOperators<uint, uint, bool>, IModulusOperators<uint, uint, uint>, INumberBase<uint>, IAdditionOperators<uint, uint, uint>, IAdditiveIdentity<uint, uint>, IDecrementOperators<uint>, IDivisionOperators<uint, uint, uint>, IIncrementOperators<uint>, IMultiplicativeIdentity<uint, uint>, IMultiplyOperators<uint, uint, uint>, ISpanParsable<uint>, IParsable<uint>, ISubtractionOperators<uint, uint, uint>, IUnaryPlusOperators<uint, uint>, IUnaryNegationOperators<uint, uint>, IUtf8SpanFormattable, IUtf8SpanParsable<uint>, IShiftOperators<uint, int, uint>, IMinMaxValue<uint>, IUnsignedNumber<uint>, IBinaryIntegerParseAndFormatInfo<uint>
{
	private readonly uint m_value;

	public const uint MaxValue = 4294967295u;

	public const uint MinValue = 0u;

	static uint IAdditiveIdentity<uint, uint>.AdditiveIdentity => 0u;

	static uint IBinaryNumber<uint>.AllBitsSet => uint.MaxValue;

	static uint IMinMaxValue<uint>.MinValue => 0u;

	static uint IMinMaxValue<uint>.MaxValue => uint.MaxValue;

	static uint IMultiplicativeIdentity<uint, uint>.MultiplicativeIdentity => 1u;

	static uint INumberBase<uint>.One => 1u;

	static int INumberBase<uint>.Radix => 2;

	static uint INumberBase<uint>.Zero => 0u;

	static bool IBinaryIntegerParseAndFormatInfo<uint>.IsSigned => false;

	static int IBinaryIntegerParseAndFormatInfo<uint>.MaxDigitCount => 10;

	static int IBinaryIntegerParseAndFormatInfo<uint>.MaxHexDigitCount => 8;

	static uint IBinaryIntegerParseAndFormatInfo<uint>.MaxValueDiv10 => 429496729u;

	static string IBinaryIntegerParseAndFormatInfo<uint>.OverflowMessage => SR.Overflow_UInt32;

	public int CompareTo(object? value)
	{
		if (value == null)
		{
			return 1;
		}
		if (value is uint num)
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
		throw new ArgumentException(SR.Arg_MustBeUInt32);
	}

	public int CompareTo(uint value)
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
		if (!(obj is uint))
		{
			return false;
		}
		return this == (uint)obj;
	}

	[NonVersionable]
	public bool Equals(uint obj)
	{
		return this == obj;
	}

	public override int GetHashCode()
	{
		return (int)this;
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

	public static uint Parse(string s)
	{
		return Parse(s, NumberStyles.Integer, null);
	}

	public static uint Parse(string s, NumberStyles style)
	{
		return Parse(s, style, null);
	}

	public static uint Parse(string s, IFormatProvider? provider)
	{
		return Parse(s, NumberStyles.Integer, provider);
	}

	public static uint Parse(string s, NumberStyles style, IFormatProvider? provider)
	{
		if (s == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.s);
		}
		return Parse(s.AsSpan(), style, provider);
	}

	public static uint Parse(ReadOnlySpan<char> s, NumberStyles style = NumberStyles.Integer, IFormatProvider? provider = null)
	{
		NumberFormatInfo.ValidateParseStyleInteger(style);
		return Number.ParseBinaryInteger<char, uint>(s, style, NumberFormatInfo.GetInstance(provider));
	}

	public static bool TryParse([NotNullWhen(true)] string? s, out uint result)
	{
		return TryParse(s, NumberStyles.Integer, null, out result);
	}

	public static bool TryParse(ReadOnlySpan<char> s, out uint result)
	{
		return TryParse(s, NumberStyles.Integer, null, out result);
	}

	public static bool TryParse(ReadOnlySpan<byte> utf8Text, out uint result)
	{
		return TryParse(utf8Text, NumberStyles.Integer, null, out result);
	}

	public static bool TryParse([NotNullWhen(true)] string? s, NumberStyles style, IFormatProvider? provider, out uint result)
	{
		NumberFormatInfo.ValidateParseStyleInteger(style);
		if (s == null)
		{
			result = 0u;
			return false;
		}
		return Number.TryParseBinaryInteger<char, uint>(s.AsSpan(), style, NumberFormatInfo.GetInstance(provider), out result) == Number.ParsingStatus.OK;
	}

	public static bool TryParse(ReadOnlySpan<char> s, NumberStyles style, IFormatProvider? provider, out uint result)
	{
		NumberFormatInfo.ValidateParseStyleInteger(style);
		return Number.TryParseBinaryInteger<char, uint>(s, style, NumberFormatInfo.GetInstance(provider), out result) == Number.ParsingStatus.OK;
	}

	public TypeCode GetTypeCode()
	{
		return TypeCode.UInt32;
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
		return this;
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
		throw new InvalidCastException(SR.Format(SR.InvalidCast_FromTo, "UInt32", "DateTime"));
	}

	object IConvertible.ToType(Type type, IFormatProvider provider)
	{
		return Convert.DefaultToType(this, type, provider);
	}

	static uint IAdditionOperators<uint, uint, uint>.operator +(uint left, uint right)
	{
		return left + right;
	}

	static uint IAdditionOperators<uint, uint, uint>.operator checked +(uint left, uint right)
	{
		return checked(left + right);
	}

	public static (uint Quotient, uint Remainder) DivRem(uint left, uint right)
	{
		return Math.DivRem(left, right);
	}

	[Intrinsic]
	public static uint LeadingZeroCount(uint value)
	{
		return (uint)BitOperations.LeadingZeroCount(value);
	}

	[Intrinsic]
	public static uint PopCount(uint value)
	{
		return (uint)BitOperations.PopCount(value);
	}

	[Intrinsic]
	public static uint RotateLeft(uint value, int rotateAmount)
	{
		return BitOperations.RotateLeft(value, rotateAmount);
	}

	[Intrinsic]
	public static uint RotateRight(uint value, int rotateAmount)
	{
		return BitOperations.RotateRight(value, rotateAmount);
	}

	[Intrinsic]
	public static uint TrailingZeroCount(uint value)
	{
		return (uint)BitOperations.TrailingZeroCount(value);
	}

	static bool IBinaryInteger<uint>.TryReadBigEndian(ReadOnlySpan<byte> source, bool isUnsigned, out uint value)
	{
		uint num = 0u;
		if (source.Length != 0)
		{
			if (!isUnsigned && sbyte.IsNegative((sbyte)source[0]))
			{
				value = num;
				return false;
			}
			if (source.Length > 4)
			{
				if (source.Slice(0, source.Length - 4).ContainsAnyExcept<byte>(0))
				{
					value = num;
					return false;
				}
			}
			ref byte reference = ref MemoryMarshal.GetReference(source);
			if (source.Length >= 4)
			{
				num = Unsafe.ReadUnaligned<uint>(ref Unsafe.Add(ref reference, source.Length - 4));
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

	static bool IBinaryInteger<uint>.TryReadLittleEndian(ReadOnlySpan<byte> source, bool isUnsigned, out uint value)
	{
		uint num = 0u;
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
			if (source.Length > 4)
			{
				if (source.Slice(4, source.Length - 4).ContainsAnyExcept<byte>(0))
				{
					value = num;
					return false;
				}
			}
			ref byte reference = ref MemoryMarshal.GetReference(source);
			if (source.Length >= 4)
			{
				num = Unsafe.ReadUnaligned<uint>(ref reference);
				if (BitConverter.IsLittleEndian)
				{
					goto IL_00a2;
				}
			}
			for (int i = 0; i < source.Length; i++)
			{
				uint num2 = Unsafe.Add(ref reference, i);
				num2 <<= i * 8;
				num |= num2;
			}
		}
		goto IL_00a2;
		IL_00a2:
		value = num;
		return true;
	}

	int IBinaryInteger<uint>.GetShortestBitLength()
	{
		return 32 - BitOperations.LeadingZeroCount(this);
	}

	int IBinaryInteger<uint>.GetByteCount()
	{
		return 4;
	}

	bool IBinaryInteger<uint>.TryWriteBigEndian(Span<byte> destination, out int bytesWritten)
	{
		if (destination.Length >= 4)
		{
			if (!BitConverter.IsLittleEndian)
			{
			}
			uint value = BinaryPrimitives.ReverseEndianness(this);
			Unsafe.WriteUnaligned(ref MemoryMarshal.GetReference(destination), value);
			bytesWritten = 4;
			return true;
		}
		bytesWritten = 0;
		return false;
	}

	bool IBinaryInteger<uint>.TryWriteLittleEndian(Span<byte> destination, out int bytesWritten)
	{
		if (destination.Length >= 4)
		{
			if (!BitConverter.IsLittleEndian)
			{
			}
			uint value = this;
			Unsafe.WriteUnaligned(ref MemoryMarshal.GetReference(destination), value);
			bytesWritten = 4;
			return true;
		}
		bytesWritten = 0;
		return false;
	}

	public static bool IsPow2(uint value)
	{
		return BitOperations.IsPow2(value);
	}

	[Intrinsic]
	public static uint Log2(uint value)
	{
		return (uint)BitOperations.Log2(value);
	}

	static uint IBitwiseOperators<uint, uint, uint>.operator &(uint left, uint right)
	{
		return left & right;
	}

	static uint IBitwiseOperators<uint, uint, uint>.operator |(uint left, uint right)
	{
		return left | right;
	}

	static uint IBitwiseOperators<uint, uint, uint>.operator ^(uint left, uint right)
	{
		return left ^ right;
	}

	static uint IBitwiseOperators<uint, uint, uint>.operator ~(uint value)
	{
		return ~value;
	}

	static bool IComparisonOperators<uint, uint, bool>.operator <(uint left, uint right)
	{
		return left < right;
	}

	static bool IComparisonOperators<uint, uint, bool>.operator <=(uint left, uint right)
	{
		return left <= right;
	}

	static bool IComparisonOperators<uint, uint, bool>.operator >(uint left, uint right)
	{
		return left > right;
	}

	static bool IComparisonOperators<uint, uint, bool>.operator >=(uint left, uint right)
	{
		return left >= right;
	}

	static uint IDecrementOperators<uint>.operator --(uint value)
	{
		return --value;
	}

	static uint IDecrementOperators<uint>.operator checked --(uint value)
	{
		return value = checked(value - 1);
	}

	static uint IDivisionOperators<uint, uint, uint>.operator /(uint left, uint right)
	{
		return left / right;
	}

	static bool IEqualityOperators<uint, uint, bool>.operator ==(uint left, uint right)
	{
		return left == right;
	}

	static bool IEqualityOperators<uint, uint, bool>.operator !=(uint left, uint right)
	{
		return left != right;
	}

	static uint IIncrementOperators<uint>.operator ++(uint value)
	{
		return ++value;
	}

	static uint IIncrementOperators<uint>.operator checked ++(uint value)
	{
		return value = checked(value + 1);
	}

	static uint IModulusOperators<uint, uint, uint>.operator %(uint left, uint right)
	{
		return left % right;
	}

	static uint IMultiplyOperators<uint, uint, uint>.operator *(uint left, uint right)
	{
		return left * right;
	}

	static uint IMultiplyOperators<uint, uint, uint>.operator checked *(uint left, uint right)
	{
		return checked(left * right);
	}

	public static uint Clamp(uint value, uint min, uint max)
	{
		return Math.Clamp(value, min, max);
	}

	static uint INumber<uint>.CopySign(uint value, uint sign)
	{
		return value;
	}

	public static uint Max(uint x, uint y)
	{
		return Math.Max(x, y);
	}

	static uint INumber<uint>.MaxNumber(uint x, uint y)
	{
		return Max(x, y);
	}

	public static uint Min(uint x, uint y)
	{
		return Math.Min(x, y);
	}

	static uint INumber<uint>.MinNumber(uint x, uint y)
	{
		return Min(x, y);
	}

	public static int Sign(uint value)
	{
		return (value != 0) ? 1 : 0;
	}

	static uint INumberBase<uint>.Abs(uint value)
	{
		return value;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static uint CreateChecked<TOther>(TOther value) where TOther : INumberBase<TOther>
	{
		if (typeof(TOther) == typeof(uint))
		{
			return (uint)(object)value;
		}
		if (!TryConvertFromChecked(value, out var result) && !TOther.TryConvertToChecked<uint>(value, out result))
		{
			ThrowHelper.ThrowNotSupportedException();
		}
		return result;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static uint CreateSaturating<TOther>(TOther value) where TOther : INumberBase<TOther>
	{
		if (typeof(TOther) == typeof(uint))
		{
			return (uint)(object)value;
		}
		if (!TryConvertFromSaturating(value, out var result) && !TOther.TryConvertToSaturating<uint>(value, out result))
		{
			ThrowHelper.ThrowNotSupportedException();
		}
		return result;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static uint CreateTruncating<TOther>(TOther value) where TOther : INumberBase<TOther>
	{
		if (typeof(TOther) == typeof(uint))
		{
			return (uint)(object)value;
		}
		if (!TryConvertFromTruncating(value, out var result) && !TOther.TryConvertToTruncating<uint>(value, out result))
		{
			ThrowHelper.ThrowNotSupportedException();
		}
		return result;
	}

	static bool INumberBase<uint>.IsCanonical(uint value)
	{
		return true;
	}

	static bool INumberBase<uint>.IsComplexNumber(uint value)
	{
		return false;
	}

	public static bool IsEvenInteger(uint value)
	{
		return (value & 1) == 0;
	}

	static bool INumberBase<uint>.IsFinite(uint value)
	{
		return true;
	}

	static bool INumberBase<uint>.IsImaginaryNumber(uint value)
	{
		return false;
	}

	static bool INumberBase<uint>.IsInfinity(uint value)
	{
		return false;
	}

	static bool INumberBase<uint>.IsInteger(uint value)
	{
		return true;
	}

	static bool INumberBase<uint>.IsNaN(uint value)
	{
		return false;
	}

	static bool INumberBase<uint>.IsNegative(uint value)
	{
		return false;
	}

	static bool INumberBase<uint>.IsNegativeInfinity(uint value)
	{
		return false;
	}

	static bool INumberBase<uint>.IsNormal(uint value)
	{
		return value != 0;
	}

	public static bool IsOddInteger(uint value)
	{
		return (value & 1) != 0;
	}

	static bool INumberBase<uint>.IsPositive(uint value)
	{
		return true;
	}

	static bool INumberBase<uint>.IsPositiveInfinity(uint value)
	{
		return false;
	}

	static bool INumberBase<uint>.IsRealNumber(uint value)
	{
		return true;
	}

	static bool INumberBase<uint>.IsSubnormal(uint value)
	{
		return false;
	}

	static bool INumberBase<uint>.IsZero(uint value)
	{
		return value == 0;
	}

	static uint INumberBase<uint>.MaxMagnitude(uint x, uint y)
	{
		return Max(x, y);
	}

	static uint INumberBase<uint>.MaxMagnitudeNumber(uint x, uint y)
	{
		return Max(x, y);
	}

	static uint INumberBase<uint>.MinMagnitude(uint x, uint y)
	{
		return Min(x, y);
	}

	static uint INumberBase<uint>.MinMagnitudeNumber(uint x, uint y)
	{
		return Min(x, y);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	static bool INumberBase<uint>.TryConvertFromChecked<TOther>(TOther value, out uint result)
	{
		return TryConvertFromChecked(value, out result);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static bool TryConvertFromChecked<TOther>(TOther value, out uint result) where TOther : INumberBase<TOther>
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
			result = (uint)num;
			return true;
		}
		if (typeof(TOther) == typeof(ushort))
		{
			ushort num2 = (ushort)(object)value;
			result = num2;
			return true;
		}
		checked
		{
			if (typeof(TOther) == typeof(ulong))
			{
				ulong num3 = (ulong)(object)value;
				result = (uint)num3;
				return true;
			}
			if (typeof(TOther) == typeof(UInt128))
			{
				UInt128 uInt = (UInt128)(object)value;
				result = (uint)uInt;
				return true;
			}
			if (typeof(TOther) == typeof(nuint))
			{
				nuint num4 = (nuint)(object)value;
				result = (uint)num4;
				return true;
			}
			result = 0u;
			return false;
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	static bool INumberBase<uint>.TryConvertFromSaturating<TOther>(TOther value, out uint result)
	{
		return TryConvertFromSaturating(value, out result);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static bool TryConvertFromSaturating<TOther>(TOther value, out uint result) where TOther : INumberBase<TOther>
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
			result = ((num >= 4294967295m) ? uint.MaxValue : ((!(num <= 0m)) ? ((uint)num) : 0u));
			return true;
		}
		if (typeof(TOther) == typeof(ushort))
		{
			ushort num2 = (ushort)(object)value;
			result = num2;
			return true;
		}
		if (typeof(TOther) == typeof(ulong))
		{
			ulong num3 = (ulong)(object)value;
			result = (uint)((num3 >= uint.MaxValue) ? uint.MaxValue : num3);
			return true;
		}
		if (typeof(TOther) == typeof(UInt128))
		{
			UInt128 uInt = (UInt128)(object)value;
			result = ((uInt >= uint.MaxValue) ? uint.MaxValue : ((uint)uInt));
			return true;
		}
		if (typeof(TOther) == typeof(nuint))
		{
			nuint num4 = (nuint)(object)value;
			result = (uint)((num4 >= uint.MaxValue) ? uint.MaxValue : num4);
			return true;
		}
		result = 0u;
		return false;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	static bool INumberBase<uint>.TryConvertFromTruncating<TOther>(TOther value, out uint result)
	{
		return TryConvertFromTruncating(value, out result);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static bool TryConvertFromTruncating<TOther>(TOther value, out uint result) where TOther : INumberBase<TOther>
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
			result = ((num >= 4294967295m) ? uint.MaxValue : ((!(num <= 0m)) ? ((uint)num) : 0u));
			return true;
		}
		if (typeof(TOther) == typeof(ushort))
		{
			ushort num2 = (ushort)(object)value;
			result = num2;
			return true;
		}
		if (typeof(TOther) == typeof(ulong))
		{
			ulong num3 = (ulong)(object)value;
			result = (uint)num3;
			return true;
		}
		if (typeof(TOther) == typeof(UInt128))
		{
			UInt128 uInt = (UInt128)(object)value;
			result = (uint)uInt;
			return true;
		}
		if (typeof(TOther) == typeof(nuint))
		{
			nuint num4 = (nuint)(object)value;
			result = (uint)num4;
			return true;
		}
		result = 0u;
		return false;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	static bool INumberBase<uint>.TryConvertToChecked<TOther>(uint value, [MaybeNullWhen(false)] out TOther result)
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
				long num4 = value;
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
	static bool INumberBase<uint>.TryConvertToSaturating<TOther>(uint value, [MaybeNullWhen(false)] out TOther result)
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
			long num4 = value;
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
	static bool INumberBase<uint>.TryConvertToTruncating<TOther>(uint value, [MaybeNullWhen(false)] out TOther result)
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

	public static bool TryParse([NotNullWhen(true)] string? s, IFormatProvider? provider, out uint result)
	{
		return TryParse(s, NumberStyles.Integer, provider, out result);
	}

	static uint IShiftOperators<uint, int, uint>.operator <<(uint value, int shiftAmount)
	{
		return value << shiftAmount;
	}

	static uint IShiftOperators<uint, int, uint>.operator >>(uint value, int shiftAmount)
	{
		return value >> shiftAmount;
	}

	static uint IShiftOperators<uint, int, uint>.operator >>>(uint value, int shiftAmount)
	{
		return value >> shiftAmount;
	}

	public static uint Parse(ReadOnlySpan<char> s, IFormatProvider? provider)
	{
		return Parse(s, NumberStyles.Integer, provider);
	}

	public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, out uint result)
	{
		return TryParse(s, NumberStyles.Integer, provider, out result);
	}

	static uint ISubtractionOperators<uint, uint, uint>.operator -(uint left, uint right)
	{
		return left - right;
	}

	static uint ISubtractionOperators<uint, uint, uint>.operator checked -(uint left, uint right)
	{
		return checked(left - right);
	}

	static uint IUnaryNegationOperators<uint, uint>.operator -(uint value)
	{
		return 0 - value;
	}

	static uint IUnaryNegationOperators<uint, uint>.operator checked -(uint value)
	{
		return checked(0 - value);
	}

	static uint IUnaryPlusOperators<uint, uint>.operator +(uint value)
	{
		return value;
	}

	public static uint Parse(ReadOnlySpan<byte> utf8Text, NumberStyles style = NumberStyles.Integer, IFormatProvider? provider = null)
	{
		NumberFormatInfo.ValidateParseStyleInteger(style);
		return Number.ParseBinaryInteger<byte, uint>(utf8Text, style, NumberFormatInfo.GetInstance(provider));
	}

	public static bool TryParse(ReadOnlySpan<byte> utf8Text, NumberStyles style, IFormatProvider? provider, out uint result)
	{
		NumberFormatInfo.ValidateParseStyleInteger(style);
		return Number.TryParseBinaryInteger<byte, uint>(utf8Text, style, NumberFormatInfo.GetInstance(provider), out result) == Number.ParsingStatus.OK;
	}

	public static uint Parse(ReadOnlySpan<byte> utf8Text, IFormatProvider? provider)
	{
		return Parse(utf8Text, NumberStyles.Integer, provider);
	}

	public static bool TryParse(ReadOnlySpan<byte> utf8Text, IFormatProvider? provider, out uint result)
	{
		return TryParse(utf8Text, NumberStyles.Integer, provider, out result);
	}

	static bool IBinaryIntegerParseAndFormatInfo<uint>.IsGreaterThanAsUnsigned(uint left, uint right)
	{
		return left > right;
	}

	static uint IBinaryIntegerParseAndFormatInfo<uint>.MultiplyBy10(uint value)
	{
		return value * 10;
	}

	static uint IBinaryIntegerParseAndFormatInfo<uint>.MultiplyBy16(uint value)
	{
		return value * 16;
	}
}
