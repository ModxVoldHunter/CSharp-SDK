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
public readonly struct Double : IComparable, IConvertible, ISpanFormattable, IFormattable, IComparable<double>, IEquatable<double>, IBinaryFloatingPointIeee754<double>, IBinaryNumber<double>, IBitwiseOperators<double, double, double>, INumber<double>, IComparisonOperators<double, double, bool>, IEqualityOperators<double, double, bool>, IModulusOperators<double, double, double>, INumberBase<double>, IAdditionOperators<double, double, double>, IAdditiveIdentity<double, double>, IDecrementOperators<double>, IDivisionOperators<double, double, double>, IIncrementOperators<double>, IMultiplicativeIdentity<double, double>, IMultiplyOperators<double, double, double>, ISpanParsable<double>, IParsable<double>, ISubtractionOperators<double, double, double>, IUnaryPlusOperators<double, double>, IUnaryNegationOperators<double, double>, IUtf8SpanFormattable, IUtf8SpanParsable<double>, IFloatingPointIeee754<double>, IExponentialFunctions<double>, IFloatingPointConstants<double>, IFloatingPoint<double>, ISignedNumber<double>, IHyperbolicFunctions<double>, ILogarithmicFunctions<double>, IPowerFunctions<double>, IRootFunctions<double>, ITrigonometricFunctions<double>, IMinMaxValue<double>, IBinaryFloatParseAndFormatInfo<double>
{
	private readonly double m_value;

	public const double MinValue = -1.7976931348623157E+308;

	public const double MaxValue = 1.7976931348623157E+308;

	public const double Epsilon = 5E-324;

	public const double NegativeInfinity = -1.0 / 0.0;

	public const double PositiveInfinity = 1.0 / 0.0;

	public const double NaN = 0.0 / 0.0;

	public const double NegativeZero = -0.0;

	public const double E = 2.718281828459045;

	public const double Pi = 3.141592653589793;

	public const double Tau = 6.283185307179586;

	internal ushort BiasedExponent
	{
		get
		{
			ulong bits = BitConverter.DoubleToUInt64Bits(this);
			return ExtractBiasedExponentFromBits(bits);
		}
	}

	internal short Exponent => (short)(BiasedExponent - 1023);

	internal ulong Significand => TrailingSignificand | ((BiasedExponent != 0) ? 4503599627370496uL : 0);

	internal ulong TrailingSignificand
	{
		get
		{
			ulong bits = BitConverter.DoubleToUInt64Bits(this);
			return ExtractTrailingSignificandFromBits(bits);
		}
	}

	static double IAdditiveIdentity<double, double>.AdditiveIdentity => 0.0;

	static double IBinaryNumber<double>.AllBitsSet => BitConverter.UInt64BitsToDouble(ulong.MaxValue);

	static double IFloatingPointConstants<double>.E => Math.E;

	static double IFloatingPointConstants<double>.Pi => Math.PI;

	static double IFloatingPointConstants<double>.Tau => Math.PI * 2.0;

	static double IFloatingPointIeee754<double>.Epsilon => double.Epsilon;

	static double IFloatingPointIeee754<double>.NaN => double.NaN;

	static double IFloatingPointIeee754<double>.NegativeInfinity => double.NegativeInfinity;

	static double IFloatingPointIeee754<double>.NegativeZero => -0.0;

	static double IFloatingPointIeee754<double>.PositiveInfinity => double.PositiveInfinity;

	static double IMinMaxValue<double>.MinValue => double.MinValue;

	static double IMinMaxValue<double>.MaxValue => double.MaxValue;

	static double IMultiplicativeIdentity<double, double>.MultiplicativeIdentity => 1.0;

	static double INumberBase<double>.One => 1.0;

	static int INumberBase<double>.Radix => 2;

	static double INumberBase<double>.Zero => 0.0;

	static double ISignedNumber<double>.NegativeOne => -1.0;

	static int IBinaryFloatParseAndFormatInfo<double>.NumberBufferLength => 769;

	static ulong IBinaryFloatParseAndFormatInfo<double>.ZeroBits => 0uL;

	static ulong IBinaryFloatParseAndFormatInfo<double>.InfinityBits => 9218868437227405312uL;

	static ulong IBinaryFloatParseAndFormatInfo<double>.NormalMantissaMask => 9007199254740991uL;

	static ulong IBinaryFloatParseAndFormatInfo<double>.DenormalMantissaMask => 4503599627370495uL;

	static int IBinaryFloatParseAndFormatInfo<double>.MinBinaryExponent => -1022;

	static int IBinaryFloatParseAndFormatInfo<double>.MaxBinaryExponent => 1023;

	static int IBinaryFloatParseAndFormatInfo<double>.MinDecimalExponent => -324;

	static int IBinaryFloatParseAndFormatInfo<double>.MaxDecimalExponent => 309;

	static int IBinaryFloatParseAndFormatInfo<double>.ExponentBias => 1023;

	static ushort IBinaryFloatParseAndFormatInfo<double>.ExponentBits => 11;

	static int IBinaryFloatParseAndFormatInfo<double>.OverflowDecimalExponent => 376;

	static int IBinaryFloatParseAndFormatInfo<double>.InfinityExponent => 2047;

	static ushort IBinaryFloatParseAndFormatInfo<double>.NormalMantissaBits => 53;

	static ushort IBinaryFloatParseAndFormatInfo<double>.DenormalMantissaBits => 52;

	static int IBinaryFloatParseAndFormatInfo<double>.MinFastFloatDecimalExponent => -342;

	static int IBinaryFloatParseAndFormatInfo<double>.MaxFastFloatDecimalExponent => 308;

	static int IBinaryFloatParseAndFormatInfo<double>.MinExponentRoundToEven => -4;

	static int IBinaryFloatParseAndFormatInfo<double>.MaxExponentRoundToEven => 23;

	static int IBinaryFloatParseAndFormatInfo<double>.MaxExponentFastPath => 22;

	static ulong IBinaryFloatParseAndFormatInfo<double>.MaxMantissaFastPath => 9007199254740992uL;

	internal static ushort ExtractBiasedExponentFromBits(ulong bits)
	{
		return (ushort)((bits >> 52) & 0x7FF);
	}

	internal static ulong ExtractTrailingSignificandFromBits(ulong bits)
	{
		return bits & 0xFFFFFFFFFFFFFuL;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[NonVersionable]
	public static bool IsFinite(double d)
	{
		long num = BitConverter.DoubleToInt64Bits(d);
		return (num & 0x7FFFFFFFFFFFFFFFL) < 9218868437227405312L;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[NonVersionable]
	public static bool IsInfinity(double d)
	{
		long num = BitConverter.DoubleToInt64Bits(d);
		return (num & 0x7FFFFFFFFFFFFFFFL) == 9218868437227405312L;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[NonVersionable]
	public static bool IsNaN(double d)
	{
		return d != d;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[NonVersionable]
	public static bool IsNegative(double d)
	{
		return BitConverter.DoubleToInt64Bits(d) < 0;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[NonVersionable]
	public static bool IsNegativeInfinity(double d)
	{
		return d == double.NegativeInfinity;
	}

	[NonVersionable]
	public static bool IsNormal(double d)
	{
		long num = BitConverter.DoubleToInt64Bits(d);
		num &= 0x7FFFFFFFFFFFFFFFL;
		if (num < 9218868437227405312L && num != 0L)
		{
			return (num & 0x7FF0000000000000L) != 0;
		}
		return false;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[NonVersionable]
	public static bool IsPositiveInfinity(double d)
	{
		return d == double.PositiveInfinity;
	}

	[NonVersionable]
	public static bool IsSubnormal(double d)
	{
		long num = BitConverter.DoubleToInt64Bits(d);
		num &= 0x7FFFFFFFFFFFFFFFL;
		if (num < 9218868437227405312L && num != 0L)
		{
			return (num & 0x7FF0000000000000L) == 0;
		}
		return false;
	}

	public int CompareTo(object? value)
	{
		if (value == null)
		{
			return 1;
		}
		if (value is double num)
		{
			if (this < num)
			{
				return -1;
			}
			if (this > num)
			{
				return 1;
			}
			if (this == num)
			{
				return 0;
			}
			if (IsNaN(this))
			{
				if (!IsNaN(num))
				{
					return -1;
				}
				return 0;
			}
			return 1;
		}
		throw new ArgumentException(SR.Arg_MustBeDouble);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public int CompareTo(double value)
	{
		if (this < value)
		{
			return -1;
		}
		if (this > value)
		{
			return 1;
		}
		if (this == value)
		{
			return 0;
		}
		if (IsNaN(this))
		{
			if (!IsNaN(value))
			{
				return -1;
			}
			return 0;
		}
		return 1;
	}

	public override bool Equals([NotNullWhen(true)] object? obj)
	{
		if (!(obj is double num))
		{
			return false;
		}
		if (num == this)
		{
			return true;
		}
		if (IsNaN(num))
		{
			return IsNaN(this);
		}
		return false;
	}

	[NonVersionable]
	public static bool operator ==(double left, double right)
	{
		return left == right;
	}

	[NonVersionable]
	public static bool operator !=(double left, double right)
	{
		return left != right;
	}

	[NonVersionable]
	public static bool operator <(double left, double right)
	{
		return left < right;
	}

	[NonVersionable]
	public static bool operator >(double left, double right)
	{
		return left > right;
	}

	[NonVersionable]
	public static bool operator <=(double left, double right)
	{
		return left <= right;
	}

	[NonVersionable]
	public static bool operator >=(double left, double right)
	{
		return left >= right;
	}

	public bool Equals(double obj)
	{
		if (obj == this)
		{
			return true;
		}
		if (IsNaN(obj))
		{
			return IsNaN(this);
		}
		return false;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public override int GetHashCode()
	{
		long num = Unsafe.As<double, long>(ref Unsafe.AsRef(ref m_value));
		if (((num - 1) & 0x7FFFFFFFFFFFFFFFL) >= 9218868437227405312L)
		{
			num &= 0x7FF0000000000000L;
		}
		return (int)num ^ (int)(num >> 32);
	}

	public override string ToString()
	{
		return Number.FormatDouble(this, null, NumberFormatInfo.CurrentInfo);
	}

	public string ToString([StringSyntax("NumericFormat")] string? format)
	{
		return Number.FormatDouble(this, format, NumberFormatInfo.CurrentInfo);
	}

	public string ToString(IFormatProvider? provider)
	{
		return Number.FormatDouble(this, null, NumberFormatInfo.GetInstance(provider));
	}

	public string ToString([StringSyntax("NumericFormat")] string? format, IFormatProvider? provider)
	{
		return Number.FormatDouble(this, format, NumberFormatInfo.GetInstance(provider));
	}

	public bool TryFormat(Span<char> destination, out int charsWritten, [StringSyntax("NumericFormat")] ReadOnlySpan<char> format = default(ReadOnlySpan<char>), IFormatProvider? provider = null)
	{
		return Number.TryFormatDouble(this, format, NumberFormatInfo.GetInstance(provider), destination, out charsWritten);
	}

	public bool TryFormat(Span<byte> utf8Destination, out int bytesWritten, [StringSyntax("NumericFormat")] ReadOnlySpan<char> format = default(ReadOnlySpan<char>), IFormatProvider? provider = null)
	{
		return Number.TryFormatDouble(this, format, NumberFormatInfo.GetInstance(provider), utf8Destination, out bytesWritten);
	}

	public static double Parse(string s)
	{
		return Parse(s, NumberStyles.Float | NumberStyles.AllowThousands, null);
	}

	public static double Parse(string s, NumberStyles style)
	{
		return Parse(s, style, null);
	}

	public static double Parse(string s, IFormatProvider? provider)
	{
		return Parse(s, NumberStyles.Float | NumberStyles.AllowThousands, provider);
	}

	public static double Parse(string s, NumberStyles style, IFormatProvider? provider)
	{
		if (s == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.s);
		}
		return Parse(s.AsSpan(), style, provider);
	}

	public static double Parse(ReadOnlySpan<char> s, NumberStyles style = NumberStyles.Float | NumberStyles.AllowThousands, IFormatProvider? provider = null)
	{
		NumberFormatInfo.ValidateParseStyleFloatingPoint(style);
		return Number.ParseFloat<char, double>(s, style, NumberFormatInfo.GetInstance(provider));
	}

	public static bool TryParse([NotNullWhen(true)] string? s, out double result)
	{
		return TryParse(s, NumberStyles.Float | NumberStyles.AllowThousands, null, out result);
	}

	public static bool TryParse(ReadOnlySpan<char> s, out double result)
	{
		return TryParse(s, NumberStyles.Float | NumberStyles.AllowThousands, null, out result);
	}

	public static bool TryParse(ReadOnlySpan<byte> utf8Text, out double result)
	{
		return TryParse(utf8Text, NumberStyles.Float | NumberStyles.AllowThousands, null, out result);
	}

	public static bool TryParse([NotNullWhen(true)] string? s, NumberStyles style, IFormatProvider? provider, out double result)
	{
		NumberFormatInfo.ValidateParseStyleFloatingPoint(style);
		if (s == null)
		{
			result = 0.0;
			return false;
		}
		return Number.TryParseFloat<char, double>(s.AsSpan(), style, NumberFormatInfo.GetInstance(provider), out result);
	}

	public static bool TryParse(ReadOnlySpan<char> s, NumberStyles style, IFormatProvider? provider, out double result)
	{
		NumberFormatInfo.ValidateParseStyleFloatingPoint(style);
		return Number.TryParseFloat<char, double>(s, style, NumberFormatInfo.GetInstance(provider), out result);
	}

	public TypeCode GetTypeCode()
	{
		return TypeCode.Double;
	}

	bool IConvertible.ToBoolean(IFormatProvider provider)
	{
		return Convert.ToBoolean(this);
	}

	char IConvertible.ToChar(IFormatProvider provider)
	{
		throw new InvalidCastException(SR.Format(SR.InvalidCast_FromTo, "Double", "Char"));
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
		return Convert.ToUInt64(this);
	}

	float IConvertible.ToSingle(IFormatProvider provider)
	{
		return Convert.ToSingle(this);
	}

	double IConvertible.ToDouble(IFormatProvider provider)
	{
		return this;
	}

	decimal IConvertible.ToDecimal(IFormatProvider provider)
	{
		return Convert.ToDecimal(this);
	}

	DateTime IConvertible.ToDateTime(IFormatProvider provider)
	{
		throw new InvalidCastException(SR.Format(SR.InvalidCast_FromTo, "Double", "DateTime"));
	}

	object IConvertible.ToType(Type type, IFormatProvider provider)
	{
		return Convert.DefaultToType(this, type, provider);
	}

	static double IAdditionOperators<double, double, double>.operator +(double left, double right)
	{
		return left + right;
	}

	public static bool IsPow2(double value)
	{
		ulong num = BitConverter.DoubleToUInt64Bits(value);
		if ((long)num <= 0L)
		{
			return false;
		}
		ushort num2 = ExtractBiasedExponentFromBits(num);
		ulong num3 = ExtractTrailingSignificandFromBits(num);
		return num2 switch
		{
			0 => ulong.PopCount(num3) == 1, 
			2047 => false, 
			_ => num3 == 0, 
		};
	}

	[Intrinsic]
	public static double Log2(double value)
	{
		return Math.Log2(value);
	}

	static double IBitwiseOperators<double, double, double>.operator &(double left, double right)
	{
		ulong value = BitConverter.DoubleToUInt64Bits(left) & BitConverter.DoubleToUInt64Bits(right);
		return BitConverter.UInt64BitsToDouble(value);
	}

	static double IBitwiseOperators<double, double, double>.operator |(double left, double right)
	{
		ulong value = BitConverter.DoubleToUInt64Bits(left) | BitConverter.DoubleToUInt64Bits(right);
		return BitConverter.UInt64BitsToDouble(value);
	}

	static double IBitwiseOperators<double, double, double>.operator ^(double left, double right)
	{
		ulong value = BitConverter.DoubleToUInt64Bits(left) ^ BitConverter.DoubleToUInt64Bits(right);
		return BitConverter.UInt64BitsToDouble(value);
	}

	static double IBitwiseOperators<double, double, double>.operator ~(double value)
	{
		ulong value2 = ~BitConverter.DoubleToUInt64Bits(value);
		return BitConverter.UInt64BitsToDouble(value2);
	}

	static double IDecrementOperators<double>.operator --(double value)
	{
		return value -= 1.0;
	}

	static double IDivisionOperators<double, double, double>.operator /(double left, double right)
	{
		return left / right;
	}

	[Intrinsic]
	public static double Exp(double x)
	{
		return Math.Exp(x);
	}

	public static double ExpM1(double x)
	{
		return Math.Exp(x) - 1.0;
	}

	public static double Exp2(double x)
	{
		return Math.Pow(2.0, x);
	}

	public static double Exp2M1(double x)
	{
		return Math.Pow(2.0, x) - 1.0;
	}

	public static double Exp10(double x)
	{
		return Math.Pow(10.0, x);
	}

	public static double Exp10M1(double x)
	{
		return Math.Pow(10.0, x) - 1.0;
	}

	[Intrinsic]
	public static double Ceiling(double x)
	{
		return Math.Ceiling(x);
	}

	[Intrinsic]
	public static double Floor(double x)
	{
		return Math.Floor(x);
	}

	[Intrinsic]
	public static double Round(double x)
	{
		return Math.Round(x);
	}

	public static double Round(double x, int digits)
	{
		return Math.Round(x, digits);
	}

	public static double Round(double x, MidpointRounding mode)
	{
		return Math.Round(x, mode);
	}

	public static double Round(double x, int digits, MidpointRounding mode)
	{
		return Math.Round(x, digits, mode);
	}

	[Intrinsic]
	public static double Truncate(double x)
	{
		return Math.Truncate(x);
	}

	int IFloatingPoint<double>.GetExponentByteCount()
	{
		return 2;
	}

	int IFloatingPoint<double>.GetExponentShortestBitLength()
	{
		short exponent = Exponent;
		if (exponent >= 0)
		{
			return 16 - short.LeadingZeroCount(exponent);
		}
		return 17 - short.LeadingZeroCount((short)(~exponent));
	}

	int IFloatingPoint<double>.GetSignificandByteCount()
	{
		return 8;
	}

	int IFloatingPoint<double>.GetSignificandBitLength()
	{
		return 53;
	}

	bool IFloatingPoint<double>.TryWriteExponentBigEndian(Span<byte> destination, out int bytesWritten)
	{
		if (destination.Length >= 2)
		{
			short exponent = Exponent;
			_ = BitConverter.IsLittleEndian;
			exponent = BinaryPrimitives.ReverseEndianness(exponent);
			Unsafe.WriteUnaligned(ref MemoryMarshal.GetReference(destination), exponent);
			bytesWritten = 2;
			return true;
		}
		bytesWritten = 0;
		return false;
	}

	bool IFloatingPoint<double>.TryWriteExponentLittleEndian(Span<byte> destination, out int bytesWritten)
	{
		if (destination.Length >= 2)
		{
			short exponent = Exponent;
			if (!BitConverter.IsLittleEndian)
			{
			}
			Unsafe.WriteUnaligned(ref MemoryMarshal.GetReference(destination), exponent);
			bytesWritten = 2;
			return true;
		}
		bytesWritten = 0;
		return false;
	}

	bool IFloatingPoint<double>.TryWriteSignificandBigEndian(Span<byte> destination, out int bytesWritten)
	{
		if (destination.Length >= 8)
		{
			ulong significand = Significand;
			_ = BitConverter.IsLittleEndian;
			significand = BinaryPrimitives.ReverseEndianness(significand);
			Unsafe.WriteUnaligned(ref MemoryMarshal.GetReference(destination), significand);
			bytesWritten = 8;
			return true;
		}
		bytesWritten = 0;
		return false;
	}

	bool IFloatingPoint<double>.TryWriteSignificandLittleEndian(Span<byte> destination, out int bytesWritten)
	{
		if (destination.Length >= 8)
		{
			ulong significand = Significand;
			if (!BitConverter.IsLittleEndian)
			{
			}
			Unsafe.WriteUnaligned(ref MemoryMarshal.GetReference(destination), significand);
			bytesWritten = 8;
			return true;
		}
		bytesWritten = 0;
		return false;
	}

	[Intrinsic]
	public static double Atan2(double y, double x)
	{
		return Math.Atan2(y, x);
	}

	public static double Atan2Pi(double y, double x)
	{
		return Atan2(y, x) / Math.PI;
	}

	public static double BitDecrement(double x)
	{
		return Math.BitDecrement(x);
	}

	public static double BitIncrement(double x)
	{
		return Math.BitIncrement(x);
	}

	[Intrinsic]
	public static double FusedMultiplyAdd(double left, double right, double addend)
	{
		return Math.FusedMultiplyAdd(left, right, addend);
	}

	public static double Ieee754Remainder(double left, double right)
	{
		return Math.IEEERemainder(left, right);
	}

	public static int ILogB(double x)
	{
		return Math.ILogB(x);
	}

	public static double Lerp(double value1, double value2, double amount)
	{
		return value1 * (1.0 - amount) + value2 * amount;
	}

	public static double ReciprocalEstimate(double x)
	{
		return Math.ReciprocalEstimate(x);
	}

	public static double ReciprocalSqrtEstimate(double x)
	{
		return Math.ReciprocalSqrtEstimate(x);
	}

	public static double ScaleB(double x, int n)
	{
		return Math.ScaleB(x, n);
	}

	[Intrinsic]
	public static double Acosh(double x)
	{
		return Math.Acosh(x);
	}

	[Intrinsic]
	public static double Asinh(double x)
	{
		return Math.Asinh(x);
	}

	[Intrinsic]
	public static double Atanh(double x)
	{
		return Math.Atanh(x);
	}

	[Intrinsic]
	public static double Cosh(double x)
	{
		return Math.Cosh(x);
	}

	[Intrinsic]
	public static double Sinh(double x)
	{
		return Math.Sinh(x);
	}

	[Intrinsic]
	public static double Tanh(double x)
	{
		return Math.Tanh(x);
	}

	static double IIncrementOperators<double>.operator ++(double value)
	{
		return value += 1.0;
	}

	[Intrinsic]
	public static double Log(double x)
	{
		return Math.Log(x);
	}

	public static double Log(double x, double newBase)
	{
		return Math.Log(x, newBase);
	}

	public static double LogP1(double x)
	{
		return Math.Log(x + 1.0);
	}

	public static double Log2P1(double x)
	{
		return Math.Log2(x + 1.0);
	}

	[Intrinsic]
	public static double Log10(double x)
	{
		return Math.Log10(x);
	}

	public static double Log10P1(double x)
	{
		return Math.Log10(x + 1.0);
	}

	static double IModulusOperators<double, double, double>.operator %(double left, double right)
	{
		return left % right;
	}

	static double IMultiplyOperators<double, double, double>.operator *(double left, double right)
	{
		return left * right;
	}

	public static double Clamp(double value, double min, double max)
	{
		return Math.Clamp(value, min, max);
	}

	public static double CopySign(double value, double sign)
	{
		return Math.CopySign(value, sign);
	}

	[Intrinsic]
	public static double Max(double x, double y)
	{
		return Math.Max(x, y);
	}

	[Intrinsic]
	public static double MaxNumber(double x, double y)
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

	[Intrinsic]
	public static double Min(double x, double y)
	{
		return Math.Min(x, y);
	}

	[Intrinsic]
	public static double MinNumber(double x, double y)
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

	public static int Sign(double value)
	{
		return Math.Sign(value);
	}

	[Intrinsic]
	public static double Abs(double value)
	{
		return Math.Abs(value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static double CreateChecked<TOther>(TOther value) where TOther : INumberBase<TOther>
	{
		if (typeof(TOther) == typeof(double))
		{
			return (double)(object)value;
		}
		if (!TryConvertFrom(value, out var result) && !TOther.TryConvertToChecked<double>(value, out result))
		{
			ThrowHelper.ThrowNotSupportedException();
		}
		return result;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static double CreateSaturating<TOther>(TOther value) where TOther : INumberBase<TOther>
	{
		if (typeof(TOther) == typeof(double))
		{
			return (double)(object)value;
		}
		if (!TryConvertFrom(value, out var result) && !TOther.TryConvertToSaturating<double>(value, out result))
		{
			ThrowHelper.ThrowNotSupportedException();
		}
		return result;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static double CreateTruncating<TOther>(TOther value) where TOther : INumberBase<TOther>
	{
		if (typeof(TOther) == typeof(double))
		{
			return (double)(object)value;
		}
		if (!TryConvertFrom(value, out var result) && !TOther.TryConvertToTruncating<double>(value, out result))
		{
			ThrowHelper.ThrowNotSupportedException();
		}
		return result;
	}

	static bool INumberBase<double>.IsCanonical(double value)
	{
		return true;
	}

	static bool INumberBase<double>.IsComplexNumber(double value)
	{
		return false;
	}

	public static bool IsEvenInteger(double value)
	{
		if (IsInteger(value))
		{
			return Abs(value % 2.0) == 0.0;
		}
		return false;
	}

	static bool INumberBase<double>.IsImaginaryNumber(double value)
	{
		return false;
	}

	public static bool IsInteger(double value)
	{
		if (IsFinite(value))
		{
			return value == Truncate(value);
		}
		return false;
	}

	public static bool IsOddInteger(double value)
	{
		if (IsInteger(value))
		{
			return Abs(value % 2.0) == 1.0;
		}
		return false;
	}

	public static bool IsPositive(double value)
	{
		return BitConverter.DoubleToInt64Bits(value) >= 0;
	}

	public static bool IsRealNumber(double value)
	{
		return value == value;
	}

	static bool INumberBase<double>.IsZero(double value)
	{
		return value == 0.0;
	}

	[Intrinsic]
	public static double MaxMagnitude(double x, double y)
	{
		return Math.MaxMagnitude(x, y);
	}

	[Intrinsic]
	public static double MaxMagnitudeNumber(double x, double y)
	{
		double num = Abs(x);
		double num2 = Abs(y);
		if (num > num2 || IsNaN(num2))
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

	[Intrinsic]
	public static double MinMagnitude(double x, double y)
	{
		return Math.MinMagnitude(x, y);
	}

	[Intrinsic]
	public static double MinMagnitudeNumber(double x, double y)
	{
		double num = Abs(x);
		double num2 = Abs(y);
		if (num < num2 || IsNaN(num2))
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

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	static bool INumberBase<double>.TryConvertFromChecked<TOther>(TOther value, out double result)
	{
		return TryConvertFrom(value, out result);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	static bool INumberBase<double>.TryConvertFromSaturating<TOther>(TOther value, out double result)
	{
		return TryConvertFrom(value, out result);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	static bool INumberBase<double>.TryConvertFromTruncating<TOther>(TOther value, out double result)
	{
		return TryConvertFrom(value, out result);
	}

	private static bool TryConvertFrom<TOther>(TOther value, out double result) where TOther : INumberBase<TOther>
	{
		if (typeof(TOther) == typeof(Half))
		{
			Half half = (Half)(object)value;
			result = (double)half;
			return true;
		}
		if (typeof(TOther) == typeof(short))
		{
			short num = (short)(object)value;
			result = num;
			return true;
		}
		if (typeof(TOther) == typeof(int))
		{
			int num2 = (int)(object)value;
			result = num2;
			return true;
		}
		if (typeof(TOther) == typeof(long))
		{
			long num3 = (long)(object)value;
			result = num3;
			return true;
		}
		if (typeof(TOther) == typeof(Int128))
		{
			Int128 @int = (Int128)(object)value;
			result = (double)@int;
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
			result = num5;
			return true;
		}
		result = 0.0;
		return false;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	static bool INumberBase<double>.TryConvertToChecked<TOther>(double value, [MaybeNullWhen(false)] out TOther result)
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
			decimal num = (decimal)value;
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
	static bool INumberBase<double>.TryConvertToSaturating<TOther>(double value, [MaybeNullWhen(false)] out TOther result)
	{
		return TryConvertTo<TOther>(value, out result);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	static bool INumberBase<double>.TryConvertToTruncating<TOther>(double value, [MaybeNullWhen(false)] out TOther result)
	{
		return TryConvertTo<TOther>(value, out result);
	}

	private static bool TryConvertTo<TOther>(double value, [MaybeNullWhen(false)] out TOther result) where TOther : INumberBase<TOther>
	{
		if (typeof(TOther) == typeof(byte))
		{
			byte b = (byte)((value >= 255.0) ? byte.MaxValue : ((!(value <= 0.0)) ? ((byte)value) : 0));
			result = (TOther)(object)b;
			return true;
		}
		if (typeof(TOther) == typeof(char))
		{
			char c = ((value >= 65535.0) ? '\uffff' : ((!(value <= 0.0)) ? ((char)value) : '\0'));
			result = (TOther)(object)c;
			return true;
		}
		if (typeof(TOther) == typeof(decimal))
		{
			decimal num = ((value >= 7.922816251426434E+28) ? decimal.MaxValue : ((value <= -7.922816251426434E+28) ? decimal.MinValue : (IsNaN(value) ? 0.0m : ((decimal)value))));
			result = (TOther)(object)num;
			return true;
		}
		if (typeof(TOther) == typeof(ushort))
		{
			ushort num2 = (ushort)((value >= 65535.0) ? ushort.MaxValue : ((!(value <= 0.0)) ? ((ushort)value) : 0));
			result = (TOther)(object)num2;
			return true;
		}
		if (typeof(TOther) == typeof(uint))
		{
			uint num3 = ((value >= 4294967295.0) ? uint.MaxValue : ((!(value <= 0.0)) ? ((uint)value) : 0u));
			result = (TOther)(object)num3;
			return true;
		}
		if (typeof(TOther) == typeof(ulong))
		{
			ulong num4 = ((value >= 1.8446744073709552E+19) ? ulong.MaxValue : ((value <= 0.0) ? 0 : (IsNaN(value) ? 0 : ((ulong)value))));
			result = (TOther)(object)num4;
			return true;
		}
		if (typeof(TOther) == typeof(UInt128))
		{
			UInt128 uInt = ((value >= 3.402823669209385E+38) ? UInt128.MaxValue : ((value <= 0.0) ? UInt128.MinValue : ((UInt128)value)));
			result = (TOther)(object)uInt;
			return true;
		}
		if (typeof(TOther) == typeof(nuint))
		{
			nuint num5 = ((value >= 1.8446744073709552E+19) ? unchecked((nuint)(-1)) : ((value <= 0.0) ? 0 : ((nuint)value)));
			result = (TOther)(object)num5;
			return true;
		}
		result = default(TOther);
		return false;
	}

	public static bool TryParse([NotNullWhen(true)] string? s, IFormatProvider? provider, out double result)
	{
		return TryParse(s, NumberStyles.Float | NumberStyles.AllowThousands, provider, out result);
	}

	[Intrinsic]
	public static double Pow(double x, double y)
	{
		return Math.Pow(x, y);
	}

	[Intrinsic]
	public static double Cbrt(double x)
	{
		return Math.Cbrt(x);
	}

	public static double Hypot(double x, double y)
	{
		if (IsFinite(x) && IsFinite(y))
		{
			double num = Abs(x);
			double num2 = Abs(y);
			if (num == 0.0)
			{
				return num2;
			}
			if (num2 == 0.0)
			{
				return num;
			}
			ulong num3 = BitConverter.DoubleToUInt64Bits(num);
			ulong num4 = BitConverter.DoubleToUInt64Bits(num2);
			uint num5 = (uint)((num3 >> 52) & 0x7FF);
			uint num6 = (uint)((num4 >> 52) & 0x7FF);
			int num7 = (int)(num5 - num6);
			double num8 = 1.0;
			if (num7 <= 54 && num7 >= -54)
			{
				if (num5 > 1523 || num6 > 1523)
				{
					num8 = 4.149515568880993E+180;
					num3 -= 2702159776422297600L;
					num4 -= 2702159776422297600L;
				}
				else if (num5 < 523 || num6 < 523)
				{
					num8 = 2.409919865102884E-181;
					num3 += 2702159776422297600L;
					num4 += 2702159776422297600L;
					if (num5 == 0)
					{
						num3 += 4503599627370496L;
						num = BitConverter.UInt64BitsToDouble(num3);
						num -= 9.232978617785736E-128;
						num3 = BitConverter.DoubleToUInt64Bits(num);
					}
					if (num6 == 0)
					{
						num4 += 4503599627370496L;
						num2 = BitConverter.UInt64BitsToDouble(num4);
						num2 -= 9.232978617785736E-128;
						num4 = BitConverter.DoubleToUInt64Bits(num2);
					}
				}
				num = BitConverter.UInt64BitsToDouble(num3);
				num2 = BitConverter.UInt64BitsToDouble(num4);
				if (num < num2)
				{
					double num9 = num;
					num = num2;
					num2 = num9;
					ulong num10 = num3;
					num3 = num4;
					num4 = num10;
				}
				double num11 = BitConverter.UInt64BitsToDouble(num3 & 0xFFFFFFFFF8000000uL);
				double num12 = BitConverter.UInt64BitsToDouble(num4 & 0xFFFFFFFFF8000000uL);
				double num13 = num - num11;
				double num14 = num2 - num12;
				double num15 = num * num;
				double num16 = num2 * num2;
				double num17 = num15 + num16;
				double num18 = num15 - num17 + num16;
				num18 += num11 * num11 - num15;
				num18 += 2.0 * num11 * num13;
				num18 += num13 * num13;
				if (num7 == 0)
				{
					num18 += num12 * num12 - num16;
					num18 += 2.0 * num12 * num14;
					num18 += num14 * num14;
				}
				return Sqrt(num17 + num18) * num8;
			}
			return num + num2;
		}
		if (IsInfinity(x) || IsInfinity(y))
		{
			return double.PositiveInfinity;
		}
		return double.NaN;
	}

	public static double RootN(double x, int n)
	{
		if (n > 0)
		{
			return n switch
			{
				2 => (x != 0.0) ? Sqrt(x) : 0.0, 
				3 => Cbrt(x), 
				_ => PositiveN(x, n), 
			};
		}
		if (n < 0)
		{
			return NegativeN(x, n);
		}
		return double.NaN;
		static double NegativeN(double x, int n)
		{
			if (IsFinite(x))
			{
				if (x != 0.0)
				{
					if (x > 0.0 || IsOddInteger(n))
					{
						double value = Pow(Abs(x), 1.0 / (double)n);
						return CopySign(value, x);
					}
					return double.NaN;
				}
				if (IsEvenInteger(n))
				{
					return double.PositiveInfinity;
				}
				return CopySign(double.PositiveInfinity, x);
			}
			if (IsNaN(x))
			{
				return double.NaN;
			}
			if (x > 0.0)
			{
				return 0.0;
			}
			return int.IsOddInteger(n) ? -0.0 : double.NaN;
		}
		static double PositiveN(double x, int n)
		{
			if (IsFinite(x))
			{
				if (x != 0.0)
				{
					if (x > 0.0 || IsOddInteger(n))
					{
						double value2 = Pow(Abs(x), 1.0 / (double)n);
						return CopySign(value2, x);
					}
					return double.NaN;
				}
				if (IsEvenInteger(n))
				{
					return 0.0;
				}
				return CopySign(0.0, x);
			}
			if (IsNaN(x))
			{
				return double.NaN;
			}
			if (x > 0.0)
			{
				return double.PositiveInfinity;
			}
			return int.IsOddInteger(n) ? double.NegativeInfinity : double.NaN;
		}
	}

	[Intrinsic]
	public static double Sqrt(double x)
	{
		return Math.Sqrt(x);
	}

	public static double Parse(ReadOnlySpan<char> s, IFormatProvider? provider)
	{
		return Parse(s, NumberStyles.Float | NumberStyles.AllowThousands, provider);
	}

	public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, out double result)
	{
		return TryParse(s, NumberStyles.Float | NumberStyles.AllowThousands, provider, out result);
	}

	static double ISubtractionOperators<double, double, double>.operator -(double left, double right)
	{
		return left - right;
	}

	[Intrinsic]
	public static double Acos(double x)
	{
		return Math.Acos(x);
	}

	public static double AcosPi(double x)
	{
		return Acos(x) / Math.PI;
	}

	[Intrinsic]
	public static double Asin(double x)
	{
		return Math.Asin(x);
	}

	public static double AsinPi(double x)
	{
		return Asin(x) / Math.PI;
	}

	[Intrinsic]
	public static double Atan(double x)
	{
		return Math.Atan(x);
	}

	public static double AtanPi(double x)
	{
		return Atan(x) / Math.PI;
	}

	[Intrinsic]
	public static double Cos(double x)
	{
		return Math.Cos(x);
	}

	public static double CosPi(double x)
	{
		if (IsFinite(x))
		{
			double num = Abs(x);
			if (num < 4503599627370496.0)
			{
				if (num > 0.25)
				{
					long num2 = (long)num;
					double num3 = num - (double)num2;
					double num4 = (long.IsOddInteger(num2) ? (-1.0) : 1.0);
					if (num3 <= 0.25)
					{
						if (num3 != 0.0)
						{
							return num4 * CosForIntervalPiBy4(num3 * Math.PI, 0.0);
						}
						return num4;
					}
					if (num3 <= 0.5)
					{
						if (num3 != 0.5)
						{
							return num4 * SinForIntervalPiBy4((0.5 - num3) * Math.PI, 0.0);
						}
						return 0.0;
					}
					if (num3 <= 0.75)
					{
						return (0.0 - num4) * SinForIntervalPiBy4((num3 - 0.5) * Math.PI, 0.0);
					}
					return (0.0 - num4) * CosForIntervalPiBy4((1.0 - num3) * Math.PI, 0.0);
				}
				if (num >= 6.103515625E-05)
				{
					return CosForIntervalPiBy4(x * Math.PI, 0.0);
				}
				if (num >= 7.450580596923828E-09)
				{
					double num5 = x * Math.PI;
					return 1.0 - num5 * num5 * 0.5;
				}
				return 1.0;
			}
			if (num < 9007199254740992.0)
			{
				long value = BitConverter.DoubleToInt64Bits(num);
				return long.IsOddInteger(value) ? (-1.0) : 1.0;
			}
			return 1.0;
		}
		return double.NaN;
	}

	public static double DegreesToRadians(double degrees)
	{
		return degrees * Math.PI / 180.0;
	}

	public static double RadiansToDegrees(double radians)
	{
		return radians * 180.0 / Math.PI;
	}

	[Intrinsic]
	public static double Sin(double x)
	{
		return Math.Sin(x);
	}

	public static (double Sin, double Cos) SinCos(double x)
	{
		return Math.SinCos(x);
	}

	public static (double SinPi, double CosPi) SinCosPi(double x)
	{
		double item;
		double item2;
		if (IsFinite(x))
		{
			double num = Abs(x);
			if (num < 4503599627370496.0)
			{
				if (num > 0.25)
				{
					long num2 = (long)num;
					double num3 = num - (double)num2;
					double num4 = (long.IsOddInteger(num2) ? (-1.0) : 1.0);
					double num5 = ((x > 0.0) ? 1.0 : (-1.0)) * num4;
					double num6 = num4;
					if (num3 <= 0.25)
					{
						if (num3 != 0.0)
						{
							double x2 = num3 * Math.PI;
							item = num5 * SinForIntervalPiBy4(x2, 0.0);
							item2 = num6 * CosForIntervalPiBy4(x2, 0.0);
						}
						else
						{
							item = x * 0.0;
							item2 = num6;
						}
					}
					else if (num3 <= 0.5)
					{
						if (num3 != 0.5)
						{
							double x3 = (0.5 - num3) * Math.PI;
							item = num5 * CosForIntervalPiBy4(x3, 0.0);
							item2 = num6 * SinForIntervalPiBy4(x3, 0.0);
						}
						else
						{
							item = num5;
							item2 = 0.0;
						}
					}
					else if (num3 <= 0.75)
					{
						double x4 = (num3 - 0.5) * Math.PI;
						item = num5 * CosForIntervalPiBy4(x4, 0.0);
						item2 = (0.0 - num6) * SinForIntervalPiBy4(x4, 0.0);
					}
					else
					{
						double x5 = (1.0 - num3) * Math.PI;
						item = num5 * SinForIntervalPiBy4(x5, 0.0);
						item2 = (0.0 - num6) * CosForIntervalPiBy4(x5, 0.0);
					}
				}
				else if (num >= 0.0001220703125)
				{
					double x6 = x * Math.PI;
					item = SinForIntervalPiBy4(x6, 0.0);
					item2 = CosForIntervalPiBy4(x6, 0.0);
				}
				else if (num >= 7.450580596923828E-09)
				{
					double num7 = x * Math.PI;
					double num8 = num7 * num7;
					item = num7 - num8 * num7 * (1.0 / 6.0);
					item2 = 1.0 - num8 * 0.5;
				}
				else
				{
					item = x * Math.PI;
					item2 = 1.0;
				}
			}
			else if (num < 9007199254740992.0)
			{
				item = x * 0.0;
				long value = BitConverter.DoubleToInt64Bits(num);
				item2 = (long.IsOddInteger(value) ? (-1.0) : 1.0);
			}
			else
			{
				item = x * 0.0;
				item2 = 1.0;
			}
		}
		else
		{
			item = double.NaN;
			item2 = double.NaN;
		}
		return (SinPi: item, CosPi: item2);
	}

	public static double SinPi(double x)
	{
		if (IsFinite(x))
		{
			double num = Abs(x);
			if (num < 4503599627370496.0)
			{
				if (num > 0.25)
				{
					long num2 = (long)num;
					double num3 = num - (double)num2;
					double num4 = ((x > 0.0) ? 1.0 : (-1.0)) * (long.IsOddInteger(num2) ? (-1.0) : 1.0);
					if (num3 <= 0.25)
					{
						if (num3 != 0.0)
						{
							return num4 * SinForIntervalPiBy4(num3 * Math.PI, 0.0);
						}
						return x * 0.0;
					}
					if (num3 <= 0.5)
					{
						if (num3 != 0.5)
						{
							return num4 * CosForIntervalPiBy4((0.5 - num3) * Math.PI, 0.0);
						}
						return num4;
					}
					if (num3 <= 0.75)
					{
						return num4 * CosForIntervalPiBy4((num3 - 0.5) * Math.PI, 0.0);
					}
					return num4 * SinForIntervalPiBy4((1.0 - num3) * Math.PI, 0.0);
				}
				if (num >= 0.0001220703125)
				{
					return SinForIntervalPiBy4(x * Math.PI, 0.0);
				}
				if (num >= 7.450580596923828E-09)
				{
					double num5 = x * Math.PI;
					return num5 - num5 * num5 * num5 * (1.0 / 6.0);
				}
				return x * Math.PI;
			}
			return x * 0.0;
		}
		return double.NaN;
	}

	[Intrinsic]
	public static double Tan(double x)
	{
		return Math.Tan(x);
	}

	public static double TanPi(double x)
	{
		if (IsFinite(x))
		{
			double num = Abs(x);
			double num2 = ((x > 0.0) ? 1.0 : (-1.0));
			if (num < 4503599627370496.0)
			{
				if (num > 0.25)
				{
					long num3 = (long)num;
					double num4 = num - (double)num3;
					if (num4 <= 0.25)
					{
						if (num4 != 0.0)
						{
							return num2 * TanForIntervalPiBy4(num4 * Math.PI, 0.0, isReciprocal: false);
						}
						return num2 * (long.IsOddInteger(num3) ? -0.0 : 0.0);
					}
					if (num4 <= 0.5)
					{
						if (num4 != 0.5)
						{
							return (0.0 - num2) * TanForIntervalPiBy4((0.5 - num4) * Math.PI, 0.0, isReciprocal: true);
						}
						return num2 * (long.IsOddInteger(num3) ? double.NegativeInfinity : double.PositiveInfinity);
					}
					if (num4 <= 0.75)
					{
						return num2 * TanForIntervalPiBy4((num4 - 0.5) * Math.PI, 0.0, isReciprocal: true);
					}
					return (0.0 - num2) * TanForIntervalPiBy4((1.0 - num4) * Math.PI, 0.0, isReciprocal: false);
				}
				if (num >= 6.103515625E-05)
				{
					return TanForIntervalPiBy4(x * Math.PI, 0.0, isReciprocal: false);
				}
				if (num >= 7.450580596923828E-09)
				{
					double num5 = x * Math.PI;
					return num5 + num5 * num5 * num5 * (1.0 / 3.0);
				}
				return x * Math.PI;
			}
			if (num < 9007199254740992.0)
			{
				long value = BitConverter.DoubleToInt64Bits(num);
				return num2 * (long.IsOddInteger(value) ? -0.0 : 0.0);
			}
			return num2 * 0.0;
		}
		return double.NaN;
	}

	static double IUnaryNegationOperators<double, double>.operator -(double value)
	{
		return 0.0 - value;
	}

	static double IUnaryPlusOperators<double, double>.operator +(double value)
	{
		return value;
	}

	public static double Parse(ReadOnlySpan<byte> utf8Text, NumberStyles style = NumberStyles.Float | NumberStyles.AllowThousands, IFormatProvider? provider = null)
	{
		NumberFormatInfo.ValidateParseStyleInteger(style);
		return Number.ParseFloat<byte, double>(utf8Text, style, NumberFormatInfo.GetInstance(provider));
	}

	public static bool TryParse(ReadOnlySpan<byte> utf8Text, NumberStyles style, IFormatProvider? provider, out double result)
	{
		NumberFormatInfo.ValidateParseStyleInteger(style);
		return Number.TryParseFloat<byte, double>(utf8Text, style, NumberFormatInfo.GetInstance(provider), out result);
	}

	public static double Parse(ReadOnlySpan<byte> utf8Text, IFormatProvider? provider)
	{
		return Parse(utf8Text, NumberStyles.Float | NumberStyles.AllowThousands, provider);
	}

	public static bool TryParse(ReadOnlySpan<byte> utf8Text, IFormatProvider? provider, out double result)
	{
		return TryParse(utf8Text, NumberStyles.Float | NumberStyles.AllowThousands, provider, out result);
	}

	static double IBinaryFloatParseAndFormatInfo<double>.BitsToFloat(ulong bits)
	{
		return BitConverter.UInt64BitsToDouble(bits);
	}

	static ulong IBinaryFloatParseAndFormatInfo<double>.FloatToBits(double value)
	{
		return BitConverter.DoubleToUInt64Bits(value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static double CosForIntervalPiBy4(double x, double xTail)
	{
		double num = x * x;
		double num2 = 0.5 * num;
		double num3 = 1.0 - num2;
		double num4 = -1.1382639806794487E-11;
		num4 = num4 * num + 2.0876146382232963E-09;
		num4 = num4 * num + -2.755731727234419E-07;
		num4 = num4 * num + 2.480158729876704E-05;
		num4 = num4 * num + -0.0013888888888887398;
		num4 = num4 * num + 1.0 / 24.0;
		num4 *= num * num;
		num4 += 1.0 - num3 - num2 - x * xTail;
		return num4 + num3;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static double SinForIntervalPiBy4(double x, double xTail)
	{
		double num = x * x;
		double num2 = num * x;
		double num3 = 1.5918144304485914E-10;
		num3 = num3 * num + -2.5051132068021698E-08;
		num3 = num3 * num + 2.7557316103728802E-06;
		num3 = num3 * num + -0.00019841269836761127;
		num3 = num3 * num + 0.00833333333333095;
		if (xTail == 0.0)
		{
			num3 = num * num3 + -1.0 / 6.0;
			return num2 * num3 + x;
		}
		return x - (num * (0.5 * xTail - num2 * num3) - xTail - num2 * (-1.0 / 6.0));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static double TanForIntervalPiBy4(double x, double xTail, bool isReciprocal)
	{
		int num = 0;
		if (x > 0.68)
		{
			num = 1;
			x = Math.PI / 4.0 - x + (3.0616169978683824E-17 - xTail);
			xTail = 0.0;
		}
		else if (x < -0.68)
		{
			num = -1;
			x = Math.PI / 4.0 + x + (3.0616169978683824E-17 + xTail);
			xTail = 0.0;
		}
		double num2 = x * x + 2.0 * x * xTail;
		double num3 = -0.00023237149408856356;
		num3 = 0.026065662039864542 + num3 * num2;
		num3 = -0.5156585157290311 + num3 * num2;
		num3 = 1.1171374792793767 + num3 * num2;
		double num4 = 0.0002240444485370221;
		num4 = -0.022934508005756565 + num4 * num2;
		num4 = 0.3723791597597922 + num4 * num2;
		double num5 = x * num2;
		num5 *= num4 / num3;
		num5 += xTail;
		double num6 = x + num5;
		if (num != 0)
		{
			num6 = ((!isReciprocal) ? ((double)num * (1.0 - 2.0 * num6 / (1.0 + num6))) : ((double)num * (2.0 * num6 / (num6 - 1.0)) - 1.0));
		}
		else if (isReciprocal)
		{
			ulong num7 = BitConverter.DoubleToUInt64Bits(num6);
			num7 &= 0xFFFFFFFF00000000uL;
			double num8 = BitConverter.UInt64BitsToDouble(num7);
			double num9 = num5 - (num8 - x);
			double num10 = -1.0 / num6;
			num7 = BitConverter.DoubleToUInt64Bits(num10);
			num7 &= 0xFFFFFFFF00000000uL;
			double num11 = BitConverter.UInt64BitsToDouble(num7);
			num6 = num11 + num10 * (1.0 + num11 * num8 + num11 * num9);
		}
		return num6;
	}
}
