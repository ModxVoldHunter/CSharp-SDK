using System.Buffers.Binary;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System;

public readonly struct Half : IComparable, ISpanFormattable, IFormattable, IComparable<Half>, IEquatable<Half>, IBinaryFloatingPointIeee754<Half>, IBinaryNumber<Half>, IBitwiseOperators<Half, Half, Half>, INumber<Half>, IComparisonOperators<Half, Half, bool>, IEqualityOperators<Half, Half, bool>, IModulusOperators<Half, Half, Half>, INumberBase<Half>, IAdditionOperators<Half, Half, Half>, IAdditiveIdentity<Half, Half>, IDecrementOperators<Half>, IDivisionOperators<Half, Half, Half>, IIncrementOperators<Half>, IMultiplicativeIdentity<Half, Half>, IMultiplyOperators<Half, Half, Half>, ISpanParsable<Half>, IParsable<Half>, ISubtractionOperators<Half, Half, Half>, IUnaryPlusOperators<Half, Half>, IUnaryNegationOperators<Half, Half>, IUtf8SpanFormattable, IUtf8SpanParsable<Half>, IFloatingPointIeee754<Half>, IExponentialFunctions<Half>, IFloatingPointConstants<Half>, IFloatingPoint<Half>, ISignedNumber<Half>, IHyperbolicFunctions<Half>, ILogarithmicFunctions<Half>, IPowerFunctions<Half>, IRootFunctions<Half>, ITrigonometricFunctions<Half>, IMinMaxValue<Half>, IBinaryFloatParseAndFormatInfo<Half>
{
	internal readonly ushort _value;

	public static Half Epsilon => new Half(1);

	public static Half PositiveInfinity => new Half(31744);

	public static Half NegativeInfinity => new Half(64512);

	public static Half NaN => new Half(65024);

	public static Half MinValue => new Half(64511);

	public static Half MaxValue => new Half(31743);

	internal byte BiasedExponent
	{
		get
		{
			ushort value = _value;
			return ExtractBiasedExponentFromBits(value);
		}
	}

	internal sbyte Exponent => (sbyte)(BiasedExponent - 15);

	internal ushort Significand => (ushort)(TrailingSignificand | ((BiasedExponent != 0) ? 1024u : 0u));

	internal ushort TrailingSignificand
	{
		get
		{
			ushort value = _value;
			return ExtractTrailingSignificandFromBits(value);
		}
	}

	static Half IAdditiveIdentity<Half, Half>.AdditiveIdentity => new Half(0);

	static Half IBinaryNumber<Half>.AllBitsSet => BitConverter.UInt16BitsToHalf(ushort.MaxValue);

	public static Half E => new Half(16752);

	public static Half Pi => new Half(16968);

	public static Half Tau => new Half(17992);

	public static Half NegativeZero => new Half(32768);

	public static Half MultiplicativeIdentity => new Half(15360);

	public static Half One => new Half(15360);

	static int INumberBase<Half>.Radix => 2;

	public static Half Zero => new Half(0);

	public static Half NegativeOne => new Half(48128);

	static int IBinaryFloatParseAndFormatInfo<Half>.NumberBufferLength => 21;

	static ulong IBinaryFloatParseAndFormatInfo<Half>.ZeroBits => 0uL;

	static ulong IBinaryFloatParseAndFormatInfo<Half>.InfinityBits => 31744uL;

	static ulong IBinaryFloatParseAndFormatInfo<Half>.NormalMantissaMask => 2047uL;

	static ulong IBinaryFloatParseAndFormatInfo<Half>.DenormalMantissaMask => 1023uL;

	static int IBinaryFloatParseAndFormatInfo<Half>.MinBinaryExponent => -14;

	static int IBinaryFloatParseAndFormatInfo<Half>.MaxBinaryExponent => 15;

	static int IBinaryFloatParseAndFormatInfo<Half>.MinDecimalExponent => -8;

	static int IBinaryFloatParseAndFormatInfo<Half>.MaxDecimalExponent => 5;

	static int IBinaryFloatParseAndFormatInfo<Half>.ExponentBias => 15;

	static ushort IBinaryFloatParseAndFormatInfo<Half>.ExponentBits => 5;

	static int IBinaryFloatParseAndFormatInfo<Half>.OverflowDecimalExponent => 12;

	static int IBinaryFloatParseAndFormatInfo<Half>.InfinityExponent => 31;

	static ushort IBinaryFloatParseAndFormatInfo<Half>.NormalMantissaBits => 11;

	static ushort IBinaryFloatParseAndFormatInfo<Half>.DenormalMantissaBits => 10;

	static int IBinaryFloatParseAndFormatInfo<Half>.MinFastFloatDecimalExponent => -8;

	static int IBinaryFloatParseAndFormatInfo<Half>.MaxFastFloatDecimalExponent => 4;

	static int IBinaryFloatParseAndFormatInfo<Half>.MinExponentRoundToEven => -21;

	static int IBinaryFloatParseAndFormatInfo<Half>.MaxExponentRoundToEven => 5;

	static int IBinaryFloatParseAndFormatInfo<Half>.MaxExponentFastPath => 4;

	static ulong IBinaryFloatParseAndFormatInfo<Half>.MaxMantissaFastPath => 2048uL;

	internal Half(ushort value)
	{
		_value = value;
	}

	private Half(bool sign, ushort exp, ushort sig)
	{
		_value = (ushort)((int)((sign ? 1u : 0u) << 15) + (exp << 10) + sig);
	}

	internal static byte ExtractBiasedExponentFromBits(ushort bits)
	{
		return (byte)((uint)(bits >> 10) & 0x1Fu);
	}

	internal static ushort ExtractTrailingSignificandFromBits(ushort bits)
	{
		return (ushort)(bits & 0x3FFu);
	}

	public static bool operator <(Half left, Half right)
	{
		if (IsNaN(left) || IsNaN(right))
		{
			return false;
		}
		bool flag = IsNegative(left);
		if (flag != IsNegative(right))
		{
			if (flag)
			{
				return !AreZero(left, right);
			}
			return false;
		}
		if (left._value != right._value)
		{
			return (left._value < right._value) ^ flag;
		}
		return false;
	}

	public static bool operator >(Half left, Half right)
	{
		return right < left;
	}

	public static bool operator <=(Half left, Half right)
	{
		if (IsNaN(left) || IsNaN(right))
		{
			return false;
		}
		bool flag = IsNegative(left);
		if (flag != IsNegative(right))
		{
			if (!flag)
			{
				return AreZero(left, right);
			}
			return true;
		}
		if (left._value != right._value)
		{
			return (left._value < right._value) ^ flag;
		}
		return true;
	}

	public static bool operator >=(Half left, Half right)
	{
		return right <= left;
	}

	public static bool operator ==(Half left, Half right)
	{
		if (IsNaN(left) || IsNaN(right))
		{
			return false;
		}
		if (left._value != right._value)
		{
			return AreZero(left, right);
		}
		return true;
	}

	public static bool operator !=(Half left, Half right)
	{
		return !(left == right);
	}

	public static bool IsFinite(Half value)
	{
		return StripSign(value) < 31744;
	}

	public static bool IsInfinity(Half value)
	{
		return StripSign(value) == 31744;
	}

	public static bool IsNaN(Half value)
	{
		return StripSign(value) > 31744;
	}

	public static bool IsNegative(Half value)
	{
		return (short)value._value < 0;
	}

	public static bool IsNegativeInfinity(Half value)
	{
		return value._value == 64512;
	}

	public static bool IsNormal(Half value)
	{
		uint num = StripSign(value);
		if (num < 31744 && num != 0)
		{
			return (num & 0x7C00) != 0;
		}
		return false;
	}

	public static bool IsPositiveInfinity(Half value)
	{
		return value._value == 31744;
	}

	public static bool IsSubnormal(Half value)
	{
		uint num = StripSign(value);
		if (num < 31744 && num != 0)
		{
			return (num & 0x7C00) == 0;
		}
		return false;
	}

	public static Half Parse(string s)
	{
		return Parse(s, NumberStyles.Float | NumberStyles.AllowThousands, null);
	}

	public static Half Parse(string s, NumberStyles style)
	{
		return Parse(s, style, null);
	}

	public static Half Parse(string s, IFormatProvider? provider)
	{
		return Parse(s, NumberStyles.Float | NumberStyles.AllowThousands, provider);
	}

	public static Half Parse(string s, NumberStyles style = NumberStyles.Float | NumberStyles.AllowThousands, IFormatProvider? provider = null)
	{
		if (s == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.s);
		}
		return Parse(s.AsSpan(), style, provider);
	}

	public static Half Parse(ReadOnlySpan<char> s, NumberStyles style = NumberStyles.Float | NumberStyles.AllowThousands, IFormatProvider? provider = null)
	{
		NumberFormatInfo.ValidateParseStyleFloatingPoint(style);
		return Number.ParseFloat<char, Half>(s, style, NumberFormatInfo.GetInstance(provider));
	}

	public static bool TryParse([NotNullWhen(true)] string? s, out Half result)
	{
		return TryParse(s, NumberStyles.Float | NumberStyles.AllowThousands, null, out result);
	}

	public static bool TryParse(ReadOnlySpan<char> s, out Half result)
	{
		return TryParse(s, NumberStyles.Float | NumberStyles.AllowThousands, null, out result);
	}

	public static bool TryParse(ReadOnlySpan<byte> utf8Text, out Half result)
	{
		return TryParse(utf8Text, NumberStyles.Float | NumberStyles.AllowThousands, null, out result);
	}

	public static bool TryParse([NotNullWhen(true)] string? s, NumberStyles style, IFormatProvider? provider, out Half result)
	{
		NumberFormatInfo.ValidateParseStyleFloatingPoint(style);
		if (s == null)
		{
			result = Zero;
			return false;
		}
		return Number.TryParseFloat<char, Half>(s.AsSpan(), style, NumberFormatInfo.GetInstance(provider), out result);
	}

	public static bool TryParse(ReadOnlySpan<char> s, NumberStyles style, IFormatProvider? provider, out Half result)
	{
		NumberFormatInfo.ValidateParseStyleFloatingPoint(style);
		return Number.TryParseFloat<char, Half>(s, style, NumberFormatInfo.GetInstance(provider), out result);
	}

	private static bool AreZero(Half left, Half right)
	{
		return (ushort)((left._value | right._value) & -32769) == 0;
	}

	private static bool IsNaNOrZero(Half value)
	{
		return ((value._value - 1) & -32769) >= 31744;
	}

	private static uint StripSign(Half value)
	{
		return (ushort)(value._value & 0xFFFF7FFFu);
	}

	public int CompareTo(object? obj)
	{
		if (!(obj is Half))
		{
			if (obj != null)
			{
				throw new ArgumentException(SR.Arg_MustBeHalf);
			}
			return 1;
		}
		return CompareTo((Half)obj);
	}

	public int CompareTo(Half other)
	{
		if (this < other)
		{
			return -1;
		}
		if (this > other)
		{
			return 1;
		}
		if (this == other)
		{
			return 0;
		}
		if (IsNaN(this))
		{
			if (!IsNaN(other))
			{
				return -1;
			}
			return 0;
		}
		return 1;
	}

	public override bool Equals([NotNullWhen(true)] object? obj)
	{
		if (obj is Half other)
		{
			return Equals(other);
		}
		return false;
	}

	public bool Equals(Half other)
	{
		if (_value != other._value && !AreZero(this, other))
		{
			if (IsNaN(this))
			{
				return IsNaN(other);
			}
			return false;
		}
		return true;
	}

	public override int GetHashCode()
	{
		if (IsNaNOrZero(this))
		{
			return _value & 0x7C00;
		}
		return _value;
	}

	public override string ToString()
	{
		return Number.FormatHalf(this, null, NumberFormatInfo.CurrentInfo);
	}

	public string ToString([StringSyntax("NumericFormat")] string? format)
	{
		return Number.FormatHalf(this, format, NumberFormatInfo.CurrentInfo);
	}

	public string ToString(IFormatProvider? provider)
	{
		return Number.FormatHalf(this, null, NumberFormatInfo.GetInstance(provider));
	}

	public string ToString([StringSyntax("NumericFormat")] string? format, IFormatProvider? provider)
	{
		return Number.FormatHalf(this, format, NumberFormatInfo.GetInstance(provider));
	}

	public bool TryFormat(Span<char> destination, out int charsWritten, [StringSyntax("NumericFormat")] ReadOnlySpan<char> format = default(ReadOnlySpan<char>), IFormatProvider? provider = null)
	{
		return Number.TryFormatHalf(this, format, NumberFormatInfo.GetInstance(provider), destination, out charsWritten);
	}

	public bool TryFormat(Span<byte> utf8Destination, out int bytesWritten, [StringSyntax("NumericFormat")] ReadOnlySpan<char> format = default(ReadOnlySpan<char>), IFormatProvider? provider = null)
	{
		return Number.TryFormatHalf(this, format, NumberFormatInfo.GetInstance(provider), utf8Destination, out bytesWritten);
	}

	public static explicit operator Half(char value)
	{
		return (Half)(float)(int)value;
	}

	public static explicit operator Half(decimal value)
	{
		return (Half)(float)value;
	}

	public static explicit operator Half(double value)
	{
		ulong num = BitConverter.DoubleToUInt64Bits(value);
		bool flag = (num & 0x8000000000000000uL) >> 63 != 0;
		int num2 = (int)((num & 0x7FF0000000000000L) >> 52);
		ulong num3 = num & 0xFFFFFFFFFFFFFuL;
		if (num2 == 2047)
		{
			if (num3 != 0L)
			{
				return CreateHalfNaN(flag, num3 << 12);
			}
			if (!flag)
			{
				return PositiveInfinity;
			}
			return NegativeInfinity;
		}
		uint num4 = (uint)ShiftRightJam(num3, 38);
		if (((uint)num2 | num4) == 0)
		{
			return new Half(flag, 0, 0);
		}
		return new Half(RoundPackToHalf(flag, (short)(num2 - 1009), (ushort)(num4 | 0x4000u)));
	}

	public static explicit operator Half(short value)
	{
		return (Half)(float)value;
	}

	public static explicit operator Half(int value)
	{
		return (Half)(float)value;
	}

	public static explicit operator Half(long value)
	{
		return (Half)(float)value;
	}

	public static explicit operator Half(nint value)
	{
		return (Half)(float)value;
	}

	public static explicit operator Half(float value)
	{
		uint num = BitConverter.SingleToUInt32Bits(value);
		uint num2 = (num & 0x80000000u) >> 16;
		uint num3 = (uint)(Unsafe.BitCast<bool, sbyte>(float.IsNaN(value)) - 1);
		value = float.Abs(value);
		value = float.Min(65520f, value);
		uint num4 = BitConverter.SingleToUInt32Bits(float.Max(value, BitConverter.UInt32BitsToSingle(947912704u)));
		num4 &= 0x7F800000u;
		num4 += 109051904;
		value += BitConverter.UInt32BitsToSingle(num4);
		num = BitConverter.SingleToUInt32Bits(value);
		uint num5 = ~num3 & 0x7C00u;
		num -= 1056964608;
		uint num6 = num >> 13;
		num &= num3;
		num += num6;
		num &= ~num5;
		uint num7 = num5 | num2;
		num |= num7;
		return BitConverter.UInt16BitsToHalf((ushort)num);
	}

	[CLSCompliant(false)]
	public static explicit operator Half(ushort value)
	{
		return (Half)(float)(int)value;
	}

	[CLSCompliant(false)]
	public static explicit operator Half(uint value)
	{
		return (Half)(float)value;
	}

	[CLSCompliant(false)]
	public static explicit operator Half(ulong value)
	{
		return (Half)(float)value;
	}

	[CLSCompliant(false)]
	public static explicit operator Half(nuint value)
	{
		return (Half)(float)value;
	}

	public static explicit operator byte(Half value)
	{
		return (byte)(float)value;
	}

	public static explicit operator checked byte(Half value)
	{
		checked
		{
			return (byte)unchecked((float)value);
		}
	}

	public static explicit operator char(Half value)
	{
		return (char)(float)value;
	}

	public static explicit operator checked char(Half value)
	{
		return (char)checked((ushort)unchecked((float)value));
	}

	public static explicit operator decimal(Half value)
	{
		return (decimal)(float)value;
	}

	public static explicit operator short(Half value)
	{
		return (short)(float)value;
	}

	public static explicit operator checked short(Half value)
	{
		checked
		{
			return (short)unchecked((float)value);
		}
	}

	public static explicit operator int(Half value)
	{
		return (int)(float)value;
	}

	public static explicit operator checked int(Half value)
	{
		checked
		{
			return (int)unchecked((float)value);
		}
	}

	public static explicit operator long(Half value)
	{
		return (long)(float)value;
	}

	public static explicit operator checked long(Half value)
	{
		checked
		{
			return (long)unchecked((float)value);
		}
	}

	public static explicit operator Int128(Half value)
	{
		return (Int128)(double)value;
	}

	public static explicit operator checked Int128(Half value)
	{
		checked
		{
			return (Int128)unchecked((double)value);
		}
	}

	public static explicit operator nint(Half value)
	{
		return (nint)(float)value;
	}

	public static explicit operator checked nint(Half value)
	{
		checked
		{
			return (nint)unchecked((float)value);
		}
	}

	[CLSCompliant(false)]
	public static explicit operator sbyte(Half value)
	{
		return (sbyte)(float)value;
	}

	[CLSCompliant(false)]
	public static explicit operator checked sbyte(Half value)
	{
		checked
		{
			return (sbyte)unchecked((float)value);
		}
	}

	[CLSCompliant(false)]
	public static explicit operator ushort(Half value)
	{
		return (ushort)(float)value;
	}

	[CLSCompliant(false)]
	public static explicit operator checked ushort(Half value)
	{
		checked
		{
			return (ushort)unchecked((float)value);
		}
	}

	[CLSCompliant(false)]
	public static explicit operator uint(Half value)
	{
		return (uint)(float)value;
	}

	[CLSCompliant(false)]
	public static explicit operator checked uint(Half value)
	{
		checked
		{
			return (uint)unchecked((float)value);
		}
	}

	[CLSCompliant(false)]
	public static explicit operator ulong(Half value)
	{
		return (ulong)(float)value;
	}

	[CLSCompliant(false)]
	public static explicit operator checked ulong(Half value)
	{
		checked
		{
			return (ulong)unchecked((float)value);
		}
	}

	[CLSCompliant(false)]
	public static explicit operator UInt128(Half value)
	{
		return (UInt128)(double)value;
	}

	[CLSCompliant(false)]
	public static explicit operator checked UInt128(Half value)
	{
		checked
		{
			return (UInt128)unchecked((double)value);
		}
	}

	[CLSCompliant(false)]
	public static explicit operator nuint(Half value)
	{
		return (nuint)(float)value;
	}

	[CLSCompliant(false)]
	public static explicit operator checked nuint(Half value)
	{
		checked
		{
			return (nuint)unchecked((float)value);
		}
	}

	public static implicit operator Half(byte value)
	{
		return (Half)(float)(int)value;
	}

	[CLSCompliant(false)]
	public static implicit operator Half(sbyte value)
	{
		return (Half)(float)value;
	}

	public static explicit operator double(Half value)
	{
		bool flag = IsNegative(value);
		int num = value.BiasedExponent;
		uint num2 = value.TrailingSignificand;
		switch (num)
		{
		case 31:
			if (num2 != 0)
			{
				return CreateDoubleNaN(flag, (ulong)num2 << 54);
			}
			if (!flag)
			{
				return double.PositiveInfinity;
			}
			return double.NegativeInfinity;
		case 0:
		{
			if (num2 == 0)
			{
				return BitConverter.UInt64BitsToDouble(flag ? 9223372036854775808uL : 0);
			}
			(int Exp, uint Sig) tuple = NormSubnormalF16Sig(num2);
			num = tuple.Exp;
			num2 = tuple.Sig;
			num--;
			break;
		}
		}
		return CreateDouble(flag, (ushort)(num + 1008), (ulong)num2 << 42);
	}

	public static explicit operator float(Half value)
	{
		short num = BitConverter.HalfToInt16Bits(value);
		uint num2 = (uint)num & 0x80000000u;
		uint num3 = (uint)num;
		uint num4 = num3 & 0x7C00u;
		uint num5 = (uint)(-Unsafe.BitCast<bool, byte>(num4 == 0));
		int num6 = Unsafe.BitCast<bool, byte>(num4 == 31744);
		uint num7 = num5 & 0x38800000u;
		uint num8 = 0x38000000u | num7;
		num3 <<= 13;
		num8 <<= num6;
		num3 &= 0xFFFE000u;
		num3 += num8;
		uint num9 = BitConverter.SingleToUInt32Bits(BitConverter.UInt32BitsToSingle(num3) - BitConverter.UInt32BitsToSingle(num7));
		return BitConverter.UInt32BitsToSingle(num9 | num2);
	}

	internal static Half Negate(Half value)
	{
		if (!IsNaN(value))
		{
			return new Half((ushort)(value._value ^ 0x8000u));
		}
		return value;
	}

	private static (int Exp, uint Sig) NormSubnormalF16Sig(uint sig)
	{
		int num = BitOperations.LeadingZeroCount(sig) - 16 - 5;
		return (Exp: 1 - num, Sig: sig << num);
	}

	private static Half CreateHalfNaN(bool sign, ulong significand)
	{
		uint num = (sign ? 1u : 0u) << 15;
		uint num2 = (uint)(significand >> 54);
		return BitConverter.UInt16BitsToHalf((ushort)(num | 0x7E00u | num2));
	}

	private static ushort RoundPackToHalf(bool sign, short exp, ushort sig)
	{
		int num = sig & 0xF;
		if ((uint)exp >= 29u)
		{
			if (exp < 0)
			{
				sig = (ushort)ShiftRightJam(sig, -exp);
				exp = 0;
				num = sig & 0xF;
			}
			else if (exp > 29 || sig + 8 >= 32768)
			{
				if (!sign)
				{
					return 31744;
				}
				return 64512;
			}
		}
		sig = (ushort)(sig + 8 >> 4);
		sig &= (ushort)(~((((num ^ 8) == 0) ? 1u : 0u) & 1u));
		if (sig == 0)
		{
			exp = 0;
		}
		return new Half(sign, (ushort)exp, sig)._value;
	}

	private static uint ShiftRightJam(uint i, int dist)
	{
		if (dist >= 31)
		{
			return (i != 0) ? 1u : 0u;
		}
		return (i >> dist) | ((i << -dist != 0) ? 1u : 0u);
	}

	private static ulong ShiftRightJam(ulong l, int dist)
	{
		if (dist >= 63)
		{
			return (ulong)((l != 0) ? 1 : 0);
		}
		return (l >> dist) | (ulong)((l << -dist != 0) ? 1 : 0);
	}

	private static double CreateDoubleNaN(bool sign, ulong significand)
	{
		ulong num = (ulong)((long)(sign ? 1 : 0) << 63);
		ulong num2 = significand >> 12;
		return BitConverter.UInt64BitsToDouble(num | 0x7FF8000000000000uL | num2);
	}

	private static double CreateDouble(bool sign, ushort exp, ulong sig)
	{
		return BitConverter.UInt64BitsToDouble((ulong)(((long)(sign ? 1 : 0) << 63) + (long)((ulong)exp << 52)) + sig);
	}

	public static Half operator +(Half left, Half right)
	{
		return (Half)((float)left + (float)right);
	}

	public static bool IsPow2(Half value)
	{
		ushort num = BitConverter.HalfToUInt16Bits(value);
		if ((short)num <= 0)
		{
			return false;
		}
		byte b = ExtractBiasedExponentFromBits(num);
		ushort num2 = ExtractTrailingSignificandFromBits(num);
		return b switch
		{
			0 => ushort.PopCount(num2) == 1, 
			31 => false, 
			_ => num2 == 0, 
		};
	}

	public static Half Log2(Half value)
	{
		return (Half)MathF.Log2((float)value);
	}

	static Half IBitwiseOperators<Half, Half, Half>.operator &(Half left, Half right)
	{
		ushort value = (ushort)(BitConverter.HalfToUInt16Bits(left) & BitConverter.HalfToUInt16Bits(right));
		return BitConverter.UInt16BitsToHalf(value);
	}

	static Half IBitwiseOperators<Half, Half, Half>.operator |(Half left, Half right)
	{
		ushort value = (ushort)(BitConverter.HalfToUInt16Bits(left) | BitConverter.HalfToUInt16Bits(right));
		return BitConverter.UInt16BitsToHalf(value);
	}

	static Half IBitwiseOperators<Half, Half, Half>.operator ^(Half left, Half right)
	{
		ushort value = (ushort)(BitConverter.HalfToUInt16Bits(left) ^ BitConverter.HalfToUInt16Bits(right));
		return BitConverter.UInt16BitsToHalf(value);
	}

	static Half IBitwiseOperators<Half, Half, Half>.operator ~(Half value)
	{
		ushort value2 = (ushort)(~BitConverter.HalfToUInt16Bits(value));
		return BitConverter.UInt16BitsToHalf(value2);
	}

	public static Half operator --(Half value)
	{
		float num = (float)value;
		num -= 1f;
		return (Half)num;
	}

	public static Half operator /(Half left, Half right)
	{
		return (Half)((float)left / (float)right);
	}

	public static Half Exp(Half x)
	{
		return (Half)MathF.Exp((float)x);
	}

	public static Half ExpM1(Half x)
	{
		return (Half)float.ExpM1((float)x);
	}

	public static Half Exp2(Half x)
	{
		return (Half)float.Exp2((float)x);
	}

	public static Half Exp2M1(Half x)
	{
		return (Half)float.Exp2M1((float)x);
	}

	public static Half Exp10(Half x)
	{
		return (Half)float.Exp10((float)x);
	}

	public static Half Exp10M1(Half x)
	{
		return (Half)float.Exp10M1((float)x);
	}

	public static Half Ceiling(Half x)
	{
		return (Half)MathF.Ceiling((float)x);
	}

	public static Half Floor(Half x)
	{
		return (Half)MathF.Floor((float)x);
	}

	public static Half Round(Half x)
	{
		return (Half)MathF.Round((float)x);
	}

	public static Half Round(Half x, int digits)
	{
		return (Half)MathF.Round((float)x, digits);
	}

	public static Half Round(Half x, MidpointRounding mode)
	{
		return (Half)MathF.Round((float)x, mode);
	}

	public static Half Round(Half x, int digits, MidpointRounding mode)
	{
		return (Half)MathF.Round((float)x, digits, mode);
	}

	public static Half Truncate(Half x)
	{
		return (Half)MathF.Truncate((float)x);
	}

	int IFloatingPoint<Half>.GetExponentByteCount()
	{
		return 1;
	}

	int IFloatingPoint<Half>.GetExponentShortestBitLength()
	{
		sbyte exponent = Exponent;
		if (exponent >= 0)
		{
			return 8 - sbyte.LeadingZeroCount(exponent);
		}
		return 9 - sbyte.LeadingZeroCount((sbyte)(~exponent));
	}

	int IFloatingPoint<Half>.GetSignificandByteCount()
	{
		return 2;
	}

	int IFloatingPoint<Half>.GetSignificandBitLength()
	{
		return 11;
	}

	bool IFloatingPoint<Half>.TryWriteExponentBigEndian(Span<byte> destination, out int bytesWritten)
	{
		if (destination.Length >= 1)
		{
			sbyte exponent = Exponent;
			Unsafe.WriteUnaligned(ref MemoryMarshal.GetReference(destination), exponent);
			bytesWritten = 1;
			return true;
		}
		bytesWritten = 0;
		return false;
	}

	bool IFloatingPoint<Half>.TryWriteExponentLittleEndian(Span<byte> destination, out int bytesWritten)
	{
		if (destination.Length >= 1)
		{
			sbyte exponent = Exponent;
			Unsafe.WriteUnaligned(ref MemoryMarshal.GetReference(destination), exponent);
			bytesWritten = 1;
			return true;
		}
		bytesWritten = 0;
		return false;
	}

	bool IFloatingPoint<Half>.TryWriteSignificandBigEndian(Span<byte> destination, out int bytesWritten)
	{
		if (destination.Length >= 2)
		{
			ushort significand = Significand;
			_ = BitConverter.IsLittleEndian;
			significand = BinaryPrimitives.ReverseEndianness(significand);
			Unsafe.WriteUnaligned(ref MemoryMarshal.GetReference(destination), significand);
			bytesWritten = 2;
			return true;
		}
		bytesWritten = 0;
		return false;
	}

	bool IFloatingPoint<Half>.TryWriteSignificandLittleEndian(Span<byte> destination, out int bytesWritten)
	{
		if (destination.Length >= 2)
		{
			ushort significand = Significand;
			if (!BitConverter.IsLittleEndian)
			{
			}
			Unsafe.WriteUnaligned(ref MemoryMarshal.GetReference(destination), significand);
			bytesWritten = 2;
			return true;
		}
		bytesWritten = 0;
		return false;
	}

	public static Half Atan2(Half y, Half x)
	{
		return (Half)MathF.Atan2((float)y, (float)x);
	}

	public static Half Atan2Pi(Half y, Half x)
	{
		return (Half)float.Atan2Pi((float)y, (float)x);
	}

	public static Half BitDecrement(Half x)
	{
		ushort value = x._value;
		if ((value & 0x7C00) >= 31744)
		{
			if (value != 31744)
			{
				return x;
			}
			return MaxValue;
		}
		if (value == 0)
		{
			return -Epsilon;
		}
		value += (ushort)(((short)value < 0) ? 1 : (-1));
		return new Half(value);
	}

	public static Half BitIncrement(Half x)
	{
		ushort value = x._value;
		if ((value & 0x7C00) >= 31744)
		{
			if (value != 64512)
			{
				return x;
			}
			return MinValue;
		}
		if (value == 32768)
		{
			return Epsilon;
		}
		value += (ushort)(((short)value >= 0) ? 1 : (-1));
		return new Half(value);
	}

	public static Half FusedMultiplyAdd(Half left, Half right, Half addend)
	{
		return (Half)MathF.FusedMultiplyAdd((float)left, (float)right, (float)addend);
	}

	public static Half Ieee754Remainder(Half left, Half right)
	{
		return (Half)MathF.IEEERemainder((float)left, (float)right);
	}

	public static int ILogB(Half x)
	{
		return MathF.ILogB((float)x);
	}

	public static Half Lerp(Half value1, Half value2, Half amount)
	{
		return (Half)float.Lerp((float)value1, (float)value2, (float)amount);
	}

	public static Half ReciprocalEstimate(Half x)
	{
		return (Half)MathF.ReciprocalEstimate((float)x);
	}

	public static Half ReciprocalSqrtEstimate(Half x)
	{
		return (Half)MathF.ReciprocalSqrtEstimate((float)x);
	}

	public static Half ScaleB(Half x, int n)
	{
		return (Half)MathF.ScaleB((float)x, n);
	}

	public static Half Acosh(Half x)
	{
		return (Half)MathF.Acosh((float)x);
	}

	public static Half Asinh(Half x)
	{
		return (Half)MathF.Asinh((float)x);
	}

	public static Half Atanh(Half x)
	{
		return (Half)MathF.Atanh((float)x);
	}

	public static Half Cosh(Half x)
	{
		return (Half)MathF.Cosh((float)x);
	}

	public static Half Sinh(Half x)
	{
		return (Half)MathF.Sinh((float)x);
	}

	public static Half Tanh(Half x)
	{
		return (Half)MathF.Tanh((float)x);
	}

	public static Half operator ++(Half value)
	{
		float num = (float)value;
		num += 1f;
		return (Half)num;
	}

	public static Half Log(Half x)
	{
		return (Half)MathF.Log((float)x);
	}

	public static Half Log(Half x, Half newBase)
	{
		return (Half)MathF.Log((float)x, (float)newBase);
	}

	public static Half Log10(Half x)
	{
		return (Half)MathF.Log10((float)x);
	}

	public static Half LogP1(Half x)
	{
		return (Half)float.LogP1((float)x);
	}

	public static Half Log2P1(Half x)
	{
		return (Half)float.Log2P1((float)x);
	}

	public static Half Log10P1(Half x)
	{
		return (Half)float.Log10P1((float)x);
	}

	public static Half operator %(Half left, Half right)
	{
		return (Half)((float)left % (float)right);
	}

	public static Half operator *(Half left, Half right)
	{
		return (Half)((float)left * (float)right);
	}

	public static Half Clamp(Half value, Half min, Half max)
	{
		return (Half)Math.Clamp((float)value, (float)min, (float)max);
	}

	public static Half CopySign(Half value, Half sign)
	{
		return (Half)MathF.CopySign((float)value, (float)sign);
	}

	public static Half Max(Half x, Half y)
	{
		return (Half)MathF.Max((float)x, (float)y);
	}

	public static Half MaxNumber(Half x, Half y)
	{
		if (x != y)
		{
			if (!IsNaN(y))
			{
				if (!(y < x))
				{
					return y;
				}
				return x;
			}
			return x;
		}
		if (!IsNegative(y))
		{
			return y;
		}
		return x;
	}

	public static Half Min(Half x, Half y)
	{
		return (Half)MathF.Min((float)x, (float)y);
	}

	public static Half MinNumber(Half x, Half y)
	{
		if (x != y)
		{
			if (!IsNaN(y))
			{
				if (!(x < y))
				{
					return y;
				}
				return x;
			}
			return x;
		}
		if (!IsNegative(x))
		{
			return y;
		}
		return x;
	}

	public static int Sign(Half value)
	{
		return MathF.Sign((float)value);
	}

	public static Half Abs(Half value)
	{
		return (Half)MathF.Abs((float)value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Half CreateChecked<TOther>(TOther value) where TOther : INumberBase<TOther>
	{
		if (typeof(TOther) == typeof(Half))
		{
			return (Half)(object)value;
		}
		if (!TryConvertFrom(value, out var result) && !TOther.TryConvertToChecked<Half>(value, out result))
		{
			ThrowHelper.ThrowNotSupportedException();
		}
		return result;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Half CreateSaturating<TOther>(TOther value) where TOther : INumberBase<TOther>
	{
		if (typeof(TOther) == typeof(Half))
		{
			return (Half)(object)value;
		}
		if (!TryConvertFrom(value, out var result) && !TOther.TryConvertToSaturating<Half>(value, out result))
		{
			ThrowHelper.ThrowNotSupportedException();
		}
		return result;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Half CreateTruncating<TOther>(TOther value) where TOther : INumberBase<TOther>
	{
		if (typeof(TOther) == typeof(Half))
		{
			return (Half)(object)value;
		}
		if (!TryConvertFrom(value, out var result) && !TOther.TryConvertToTruncating<Half>(value, out result))
		{
			ThrowHelper.ThrowNotSupportedException();
		}
		return result;
	}

	static bool INumberBase<Half>.IsCanonical(Half value)
	{
		return true;
	}

	static bool INumberBase<Half>.IsComplexNumber(Half value)
	{
		return false;
	}

	public static bool IsEvenInteger(Half value)
	{
		return float.IsEvenInteger((float)value);
	}

	static bool INumberBase<Half>.IsImaginaryNumber(Half value)
	{
		return false;
	}

	public static bool IsInteger(Half value)
	{
		return float.IsInteger((float)value);
	}

	public static bool IsOddInteger(Half value)
	{
		return float.IsOddInteger((float)value);
	}

	public static bool IsPositive(Half value)
	{
		return (short)value._value >= 0;
	}

	public static bool IsRealNumber(Half value)
	{
		return value == value;
	}

	static bool INumberBase<Half>.IsZero(Half value)
	{
		return value == Zero;
	}

	public static Half MaxMagnitude(Half x, Half y)
	{
		return (Half)MathF.MaxMagnitude((float)x, (float)y);
	}

	public static Half MaxMagnitudeNumber(Half x, Half y)
	{
		Half half = Abs(x);
		Half half2 = Abs(y);
		if (half > half2 || IsNaN(half2))
		{
			return x;
		}
		if (half == half2)
		{
			if (!IsNegative(x))
			{
				return x;
			}
			return y;
		}
		return y;
	}

	public static Half MinMagnitude(Half x, Half y)
	{
		return (Half)MathF.MinMagnitude((float)x, (float)y);
	}

	public static Half MinMagnitudeNumber(Half x, Half y)
	{
		Half half = Abs(x);
		Half half2 = Abs(y);
		if (half < half2 || IsNaN(half2))
		{
			return x;
		}
		if (half == half2)
		{
			if (!IsNegative(x))
			{
				return y;
			}
			return x;
		}
		return y;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	static bool INumberBase<Half>.TryConvertFromChecked<TOther>(TOther value, out Half result)
	{
		return TryConvertFrom(value, out result);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	static bool INumberBase<Half>.TryConvertFromSaturating<TOther>(TOther value, out Half result)
	{
		return TryConvertFrom(value, out result);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	static bool INumberBase<Half>.TryConvertFromTruncating<TOther>(TOther value, out Half result)
	{
		return TryConvertFrom(value, out result);
	}

	private static bool TryConvertFrom<TOther>(TOther value, out Half result) where TOther : INumberBase<TOther>
	{
		if (typeof(TOther) == typeof(double))
		{
			double num = (double)(object)value;
			result = (Half)num;
			return true;
		}
		if (typeof(TOther) == typeof(short))
		{
			short num2 = (short)(object)value;
			result = (Half)num2;
			return true;
		}
		if (typeof(TOther) == typeof(int))
		{
			int num3 = (int)(object)value;
			result = (Half)num3;
			return true;
		}
		if (typeof(TOther) == typeof(long))
		{
			long num4 = (long)(object)value;
			result = (Half)num4;
			return true;
		}
		if (typeof(TOther) == typeof(Int128))
		{
			Int128 @int = (Int128)(object)value;
			result = (Half)@int;
			return true;
		}
		if (typeof(TOther) == typeof(nint))
		{
			nint num5 = (nint)(object)value;
			result = (Half)num5;
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
			result = (Half)num6;
			return true;
		}
		result = default(Half);
		return false;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	static bool INumberBase<Half>.TryConvertToChecked<TOther>(Half value, [MaybeNullWhen(false)] out TOther result)
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
	static bool INumberBase<Half>.TryConvertToSaturating<TOther>(Half value, [MaybeNullWhen(false)] out TOther result)
	{
		return TryConvertTo<TOther>(value, out result);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	static bool INumberBase<Half>.TryConvertToTruncating<TOther>(Half value, [MaybeNullWhen(false)] out TOther result)
	{
		return TryConvertTo<TOther>(value, out result);
	}

	private static bool TryConvertTo<TOther>(Half value, [MaybeNullWhen(false)] out TOther result) where TOther : INumberBase<TOther>
	{
		if (typeof(TOther) == typeof(byte))
		{
			byte b = (byte)((value >= byte.MaxValue) ? byte.MaxValue : ((!(value <= (byte)0)) ? ((byte)value) : 0));
			result = (TOther)(object)b;
			return true;
		}
		if (typeof(TOther) == typeof(char))
		{
			char c = ((value == PositiveInfinity) ? '\uffff' : ((!(value <= Zero)) ? ((char)value) : '\0'));
			result = (TOther)(object)c;
			return true;
		}
		if (typeof(TOther) == typeof(decimal))
		{
			decimal num = ((value == PositiveInfinity) ? decimal.MaxValue : ((value == NegativeInfinity) ? decimal.MinValue : (IsNaN(value) ? 0.0m : ((decimal)value))));
			result = (TOther)(object)num;
			return true;
		}
		if (typeof(TOther) == typeof(ushort))
		{
			ushort num2 = (ushort)((value == PositiveInfinity) ? ushort.MaxValue : ((!(value <= Zero)) ? ((ushort)value) : 0));
			result = (TOther)(object)num2;
			return true;
		}
		if (typeof(TOther) == typeof(uint))
		{
			uint num3 = ((value == PositiveInfinity) ? uint.MaxValue : ((!(value <= Zero)) ? ((uint)value) : 0u));
			result = (TOther)(object)num3;
			return true;
		}
		if (typeof(TOther) == typeof(ulong))
		{
			ulong num4 = ((value == PositiveInfinity) ? ulong.MaxValue : ((value <= Zero) ? 0 : (IsNaN(value) ? 0 : ((ulong)value))));
			result = (TOther)(object)num4;
			return true;
		}
		if (typeof(TOther) == typeof(UInt128))
		{
			UInt128 uInt = ((value == PositiveInfinity) ? UInt128.MaxValue : ((value <= Zero) ? UInt128.MinValue : ((UInt128)value)));
			result = (TOther)(object)uInt;
			return true;
		}
		if (typeof(TOther) == typeof(nuint))
		{
			nuint num5 = ((value == PositiveInfinity) ? UIntPtr.MaxValue : ((value <= Zero) ? UIntPtr.MinValue : ((nuint)value)));
			result = (TOther)(object)num5;
			return true;
		}
		result = default(TOther);
		return false;
	}

	public static bool TryParse([NotNullWhen(true)] string? s, IFormatProvider? provider, out Half result)
	{
		return TryParse(s, NumberStyles.Float | NumberStyles.AllowThousands, provider, out result);
	}

	public static Half Pow(Half x, Half y)
	{
		return (Half)MathF.Pow((float)x, (float)y);
	}

	public static Half Cbrt(Half x)
	{
		return (Half)MathF.Cbrt((float)x);
	}

	public static Half Hypot(Half x, Half y)
	{
		return (Half)float.Hypot((float)x, (float)y);
	}

	public static Half RootN(Half x, int n)
	{
		return (Half)float.RootN((float)x, n);
	}

	public static Half Sqrt(Half x)
	{
		return (Half)MathF.Sqrt((float)x);
	}

	public static Half Parse(ReadOnlySpan<char> s, IFormatProvider? provider)
	{
		return Parse(s, NumberStyles.Float | NumberStyles.AllowThousands, provider);
	}

	public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, out Half result)
	{
		return TryParse(s, NumberStyles.Float | NumberStyles.AllowThousands, provider, out result);
	}

	public static Half operator -(Half left, Half right)
	{
		return (Half)((float)left - (float)right);
	}

	public static Half Acos(Half x)
	{
		return (Half)MathF.Acos((float)x);
	}

	public static Half AcosPi(Half x)
	{
		return (Half)float.AcosPi((float)x);
	}

	public static Half Asin(Half x)
	{
		return (Half)MathF.Asin((float)x);
	}

	public static Half AsinPi(Half x)
	{
		return (Half)float.AsinPi((float)x);
	}

	public static Half Atan(Half x)
	{
		return (Half)MathF.Atan((float)x);
	}

	public static Half AtanPi(Half x)
	{
		return (Half)float.AtanPi((float)x);
	}

	public static Half Cos(Half x)
	{
		return (Half)MathF.Cos((float)x);
	}

	public static Half CosPi(Half x)
	{
		return (Half)float.CosPi((float)x);
	}

	public static Half DegreesToRadians(Half degrees)
	{
		return (Half)float.DegreesToRadians((float)degrees);
	}

	public static Half RadiansToDegrees(Half radians)
	{
		return (Half)float.RadiansToDegrees((float)radians);
	}

	public static Half Sin(Half x)
	{
		return (Half)MathF.Sin((float)x);
	}

	public static (Half Sin, Half Cos) SinCos(Half x)
	{
		var (num, num2) = MathF.SinCos((float)x);
		return (Sin: (Half)num, Cos: (Half)num2);
	}

	public static (Half SinPi, Half CosPi) SinCosPi(Half x)
	{
		var (num, num2) = float.SinCosPi((float)x);
		return (SinPi: (Half)num, CosPi: (Half)num2);
	}

	public static Half SinPi(Half x)
	{
		return (Half)float.SinPi((float)x);
	}

	public static Half Tan(Half x)
	{
		return (Half)MathF.Tan((float)x);
	}

	public static Half TanPi(Half x)
	{
		return (Half)float.TanPi((float)x);
	}

	public static Half operator -(Half value)
	{
		return (Half)(0f - (float)value);
	}

	public static Half operator +(Half value)
	{
		return value;
	}

	public static Half Parse(ReadOnlySpan<byte> utf8Text, NumberStyles style = NumberStyles.Float | NumberStyles.AllowThousands, IFormatProvider? provider = null)
	{
		NumberFormatInfo.ValidateParseStyleInteger(style);
		return Number.ParseFloat<byte, Half>(utf8Text, style, NumberFormatInfo.GetInstance(provider));
	}

	public static bool TryParse(ReadOnlySpan<byte> utf8Text, NumberStyles style, IFormatProvider? provider, out Half result)
	{
		NumberFormatInfo.ValidateParseStyleInteger(style);
		return Number.TryParseFloat<byte, Half>(utf8Text, style, NumberFormatInfo.GetInstance(provider), out result);
	}

	public static Half Parse(ReadOnlySpan<byte> utf8Text, IFormatProvider? provider)
	{
		return Parse(utf8Text, NumberStyles.Float | NumberStyles.AllowThousands, provider);
	}

	public static bool TryParse(ReadOnlySpan<byte> utf8Text, IFormatProvider? provider, out Half result)
	{
		return TryParse(utf8Text, NumberStyles.Float | NumberStyles.AllowThousands, provider, out result);
	}

	static Half IBinaryFloatParseAndFormatInfo<Half>.BitsToFloat(ulong bits)
	{
		return BitConverter.UInt16BitsToHalf((ushort)bits);
	}

	static ulong IBinaryFloatParseAndFormatInfo<Half>.FloatToBits(Half value)
	{
		return BitConverter.HalfToUInt16Bits(value);
	}
}
