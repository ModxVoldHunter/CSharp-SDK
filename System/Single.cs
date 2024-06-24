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
public readonly struct Single : IComparable, IConvertible, ISpanFormattable, IFormattable, IComparable<float>, IEquatable<float>, IBinaryFloatingPointIeee754<float>, IBinaryNumber<float>, IBitwiseOperators<float, float, float>, INumber<float>, IComparisonOperators<float, float, bool>, IEqualityOperators<float, float, bool>, IModulusOperators<float, float, float>, INumberBase<float>, IAdditionOperators<float, float, float>, IAdditiveIdentity<float, float>, IDecrementOperators<float>, IDivisionOperators<float, float, float>, IIncrementOperators<float>, IMultiplicativeIdentity<float, float>, IMultiplyOperators<float, float, float>, ISpanParsable<float>, IParsable<float>, ISubtractionOperators<float, float, float>, IUnaryPlusOperators<float, float>, IUnaryNegationOperators<float, float>, IUtf8SpanFormattable, IUtf8SpanParsable<float>, IFloatingPointIeee754<float>, IExponentialFunctions<float>, IFloatingPointConstants<float>, IFloatingPoint<float>, ISignedNumber<float>, IHyperbolicFunctions<float>, ILogarithmicFunctions<float>, IPowerFunctions<float>, IRootFunctions<float>, ITrigonometricFunctions<float>, IMinMaxValue<float>, IBinaryFloatParseAndFormatInfo<float>
{
	private readonly float m_value;

	public const float MinValue = -3.4028235E+38f;

	public const float MaxValue = 3.4028235E+38f;

	public const float Epsilon = 1E-45f;

	public const float NegativeInfinity = -1f / 0f;

	public const float PositiveInfinity = 1f / 0f;

	public const float NaN = 0f / 0f;

	public const float NegativeZero = -0f;

	public const float E = 2.7182817f;

	public const float Pi = 3.1415927f;

	public const float Tau = 6.2831855f;

	internal byte BiasedExponent
	{
		get
		{
			uint bits = BitConverter.SingleToUInt32Bits(this);
			return ExtractBiasedExponentFromBits(bits);
		}
	}

	internal sbyte Exponent => (sbyte)(BiasedExponent - 127);

	internal uint Significand => TrailingSignificand | ((BiasedExponent != 0) ? 8388608u : 0u);

	internal uint TrailingSignificand
	{
		get
		{
			uint bits = BitConverter.SingleToUInt32Bits(this);
			return ExtractTrailingSignificandFromBits(bits);
		}
	}

	static float IAdditiveIdentity<float, float>.AdditiveIdentity => 0f;

	static float IBinaryNumber<float>.AllBitsSet => BitConverter.UInt32BitsToSingle(uint.MaxValue);

	static float IFloatingPointConstants<float>.E => (float)Math.E;

	static float IFloatingPointConstants<float>.Pi => (float)Math.PI;

	static float IFloatingPointConstants<float>.Tau => (float)Math.PI * 2f;

	static float IFloatingPointIeee754<float>.Epsilon => float.Epsilon;

	static float IFloatingPointIeee754<float>.NaN => float.NaN;

	static float IFloatingPointIeee754<float>.NegativeInfinity => float.NegativeInfinity;

	static float IFloatingPointIeee754<float>.NegativeZero => -0f;

	static float IFloatingPointIeee754<float>.PositiveInfinity => float.PositiveInfinity;

	static float IMinMaxValue<float>.MinValue => float.MinValue;

	static float IMinMaxValue<float>.MaxValue => float.MaxValue;

	static float IMultiplicativeIdentity<float, float>.MultiplicativeIdentity => 1f;

	static float INumberBase<float>.One => 1f;

	static int INumberBase<float>.Radix => 2;

	static float INumberBase<float>.Zero => 0f;

	static float ISignedNumber<float>.NegativeOne => -1f;

	static int IBinaryFloatParseAndFormatInfo<float>.NumberBufferLength => 114;

	static ulong IBinaryFloatParseAndFormatInfo<float>.ZeroBits => 0uL;

	static ulong IBinaryFloatParseAndFormatInfo<float>.InfinityBits => 2139095040uL;

	static ulong IBinaryFloatParseAndFormatInfo<float>.NormalMantissaMask => 16777215uL;

	static ulong IBinaryFloatParseAndFormatInfo<float>.DenormalMantissaMask => 8388607uL;

	static int IBinaryFloatParseAndFormatInfo<float>.MinBinaryExponent => -126;

	static int IBinaryFloatParseAndFormatInfo<float>.MaxBinaryExponent => 127;

	static int IBinaryFloatParseAndFormatInfo<float>.MinDecimalExponent => -45;

	static int IBinaryFloatParseAndFormatInfo<float>.MaxDecimalExponent => 39;

	static int IBinaryFloatParseAndFormatInfo<float>.ExponentBias => 127;

	static ushort IBinaryFloatParseAndFormatInfo<float>.ExponentBits => 8;

	static int IBinaryFloatParseAndFormatInfo<float>.OverflowDecimalExponent => 58;

	static int IBinaryFloatParseAndFormatInfo<float>.InfinityExponent => 255;

	static ushort IBinaryFloatParseAndFormatInfo<float>.NormalMantissaBits => 24;

	static ushort IBinaryFloatParseAndFormatInfo<float>.DenormalMantissaBits => 23;

	static int IBinaryFloatParseAndFormatInfo<float>.MinFastFloatDecimalExponent => -65;

	static int IBinaryFloatParseAndFormatInfo<float>.MaxFastFloatDecimalExponent => 38;

	static int IBinaryFloatParseAndFormatInfo<float>.MinExponentRoundToEven => -17;

	static int IBinaryFloatParseAndFormatInfo<float>.MaxExponentRoundToEven => 10;

	static int IBinaryFloatParseAndFormatInfo<float>.MaxExponentFastPath => 10;

	static ulong IBinaryFloatParseAndFormatInfo<float>.MaxMantissaFastPath => 16777216uL;

	internal static byte ExtractBiasedExponentFromBits(uint bits)
	{
		return (byte)((bits >> 23) & 0xFFu);
	}

	internal static uint ExtractTrailingSignificandFromBits(uint bits)
	{
		return bits & 0x7FFFFFu;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[NonVersionable]
	public static bool IsFinite(float f)
	{
		int num = BitConverter.SingleToInt32Bits(f);
		return (num & 0x7FFFFFFF) < 2139095040;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[NonVersionable]
	public static bool IsInfinity(float f)
	{
		int num = BitConverter.SingleToInt32Bits(f);
		return (num & 0x7FFFFFFF) == 2139095040;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[NonVersionable]
	public static bool IsNaN(float f)
	{
		return f != f;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[NonVersionable]
	public static bool IsNegative(float f)
	{
		return BitConverter.SingleToInt32Bits(f) < 0;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[NonVersionable]
	public static bool IsNegativeInfinity(float f)
	{
		return f == float.NegativeInfinity;
	}

	[NonVersionable]
	public static bool IsNormal(float f)
	{
		int num = BitConverter.SingleToInt32Bits(f);
		num &= 0x7FFFFFFF;
		if (num < 2139095040 && num != 0)
		{
			return (num & 0x7F800000) != 0;
		}
		return false;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[NonVersionable]
	public static bool IsPositiveInfinity(float f)
	{
		return f == float.PositiveInfinity;
	}

	[NonVersionable]
	public static bool IsSubnormal(float f)
	{
		int num = BitConverter.SingleToInt32Bits(f);
		num &= 0x7FFFFFFF;
		if (num < 2139095040 && num != 0)
		{
			return (num & 0x7F800000) == 0;
		}
		return false;
	}

	public int CompareTo(object? value)
	{
		if (value == null)
		{
			return 1;
		}
		if (value is float num)
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
		throw new ArgumentException(SR.Arg_MustBeSingle);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public int CompareTo(float value)
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

	[NonVersionable]
	public static bool operator ==(float left, float right)
	{
		return left == right;
	}

	[NonVersionable]
	public static bool operator !=(float left, float right)
	{
		return left != right;
	}

	[NonVersionable]
	public static bool operator <(float left, float right)
	{
		return left < right;
	}

	[NonVersionable]
	public static bool operator >(float left, float right)
	{
		return left > right;
	}

	[NonVersionable]
	public static bool operator <=(float left, float right)
	{
		return left <= right;
	}

	[NonVersionable]
	public static bool operator >=(float left, float right)
	{
		return left >= right;
	}

	public override bool Equals([NotNullWhen(true)] object? obj)
	{
		if (!(obj is float num))
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

	public bool Equals(float obj)
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
		int num = Unsafe.As<float, int>(ref Unsafe.AsRef(ref m_value));
		if (((num - 1) & 0x7FFFFFFF) >= 2139095040)
		{
			num &= 0x7F800000;
		}
		return num;
	}

	public override string ToString()
	{
		return Number.FormatSingle(this, null, NumberFormatInfo.CurrentInfo);
	}

	public string ToString(IFormatProvider? provider)
	{
		return Number.FormatSingle(this, null, NumberFormatInfo.GetInstance(provider));
	}

	public string ToString([StringSyntax("NumericFormat")] string? format)
	{
		return Number.FormatSingle(this, format, NumberFormatInfo.CurrentInfo);
	}

	public string ToString([StringSyntax("NumericFormat")] string? format, IFormatProvider? provider)
	{
		return Number.FormatSingle(this, format, NumberFormatInfo.GetInstance(provider));
	}

	public bool TryFormat(Span<char> destination, out int charsWritten, [StringSyntax("NumericFormat")] ReadOnlySpan<char> format = default(ReadOnlySpan<char>), IFormatProvider? provider = null)
	{
		return Number.TryFormatSingle(this, format, NumberFormatInfo.GetInstance(provider), destination, out charsWritten);
	}

	public bool TryFormat(Span<byte> utf8Destination, out int bytesWritten, [StringSyntax("NumericFormat")] ReadOnlySpan<char> format = default(ReadOnlySpan<char>), IFormatProvider? provider = null)
	{
		return Number.TryFormatSingle(this, format, NumberFormatInfo.GetInstance(provider), utf8Destination, out bytesWritten);
	}

	public static float Parse(string s)
	{
		return Parse(s, NumberStyles.Float | NumberStyles.AllowThousands, null);
	}

	public static float Parse(string s, NumberStyles style)
	{
		return Parse(s, style, null);
	}

	public static float Parse(string s, IFormatProvider? provider)
	{
		return Parse(s, NumberStyles.Float | NumberStyles.AllowThousands, provider);
	}

	public static float Parse(string s, NumberStyles style, IFormatProvider? provider)
	{
		if (s == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.s);
		}
		return Parse(s.AsSpan(), style, provider);
	}

	public static float Parse(ReadOnlySpan<char> s, NumberStyles style = NumberStyles.Float | NumberStyles.AllowThousands, IFormatProvider? provider = null)
	{
		NumberFormatInfo.ValidateParseStyleFloatingPoint(style);
		return Number.ParseFloat<char, float>(s, style, NumberFormatInfo.GetInstance(provider));
	}

	public static bool TryParse([NotNullWhen(true)] string? s, out float result)
	{
		return TryParse(s, NumberStyles.Float | NumberStyles.AllowThousands, null, out result);
	}

	public static bool TryParse(ReadOnlySpan<char> s, out float result)
	{
		return TryParse(s, NumberStyles.Float | NumberStyles.AllowThousands, null, out result);
	}

	public static bool TryParse(ReadOnlySpan<byte> utf8Text, out float result)
	{
		return TryParse(utf8Text, NumberStyles.Float | NumberStyles.AllowThousands, null, out result);
	}

	public static bool TryParse([NotNullWhen(true)] string? s, NumberStyles style, IFormatProvider? provider, out float result)
	{
		NumberFormatInfo.ValidateParseStyleFloatingPoint(style);
		if (s == null)
		{
			result = 0f;
			return false;
		}
		return Number.TryParseFloat<char, float>(s.AsSpan(), style, NumberFormatInfo.GetInstance(provider), out result);
	}

	public static bool TryParse(ReadOnlySpan<char> s, NumberStyles style, IFormatProvider? provider, out float result)
	{
		NumberFormatInfo.ValidateParseStyleFloatingPoint(style);
		return Number.TryParseFloat<char, float>(s, style, NumberFormatInfo.GetInstance(provider), out result);
	}

	public TypeCode GetTypeCode()
	{
		return TypeCode.Single;
	}

	bool IConvertible.ToBoolean(IFormatProvider provider)
	{
		return Convert.ToBoolean(this);
	}

	char IConvertible.ToChar(IFormatProvider provider)
	{
		throw new InvalidCastException(SR.Format(SR.InvalidCast_FromTo, "Single", "Char"));
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
		return this;
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
		throw new InvalidCastException(SR.Format(SR.InvalidCast_FromTo, "Single", "DateTime"));
	}

	object IConvertible.ToType(Type type, IFormatProvider provider)
	{
		return Convert.DefaultToType(this, type, provider);
	}

	static float IAdditionOperators<float, float, float>.operator +(float left, float right)
	{
		return left + right;
	}

	public static bool IsPow2(float value)
	{
		uint num = BitConverter.SingleToUInt32Bits(value);
		if ((int)num <= 0)
		{
			return false;
		}
		byte b = ExtractBiasedExponentFromBits(num);
		uint num2 = ExtractTrailingSignificandFromBits(num);
		return b switch
		{
			0 => uint.PopCount(num2) == 1, 
			byte.MaxValue => false, 
			_ => num2 == 0, 
		};
	}

	[Intrinsic]
	public static float Log2(float value)
	{
		return MathF.Log2(value);
	}

	static float IBitwiseOperators<float, float, float>.operator &(float left, float right)
	{
		uint value = BitConverter.SingleToUInt32Bits(left) & BitConverter.SingleToUInt32Bits(right);
		return BitConverter.UInt32BitsToSingle(value);
	}

	static float IBitwiseOperators<float, float, float>.operator |(float left, float right)
	{
		uint value = BitConverter.SingleToUInt32Bits(left) | BitConverter.SingleToUInt32Bits(right);
		return BitConverter.UInt32BitsToSingle(value);
	}

	static float IBitwiseOperators<float, float, float>.operator ^(float left, float right)
	{
		uint value = BitConverter.SingleToUInt32Bits(left) ^ BitConverter.SingleToUInt32Bits(right);
		return BitConverter.UInt32BitsToSingle(value);
	}

	static float IBitwiseOperators<float, float, float>.operator ~(float value)
	{
		uint value2 = ~BitConverter.SingleToUInt32Bits(value);
		return BitConverter.UInt32BitsToSingle(value2);
	}

	static float IDecrementOperators<float>.operator --(float value)
	{
		return value -= 1f;
	}

	static float IDivisionOperators<float, float, float>.operator /(float left, float right)
	{
		return left / right;
	}

	[Intrinsic]
	public static float Exp(float x)
	{
		return MathF.Exp(x);
	}

	public static float ExpM1(float x)
	{
		return MathF.Exp(x) - 1f;
	}

	public static float Exp2(float x)
	{
		return MathF.Pow(2f, x);
	}

	public static float Exp2M1(float x)
	{
		return MathF.Pow(2f, x) - 1f;
	}

	public static float Exp10(float x)
	{
		return MathF.Pow(10f, x);
	}

	public static float Exp10M1(float x)
	{
		return MathF.Pow(10f, x) - 1f;
	}

	[Intrinsic]
	public static float Ceiling(float x)
	{
		return MathF.Ceiling(x);
	}

	[Intrinsic]
	public static float Floor(float x)
	{
		return MathF.Floor(x);
	}

	[Intrinsic]
	public static float Round(float x)
	{
		return MathF.Round(x);
	}

	public static float Round(float x, int digits)
	{
		return MathF.Round(x, digits);
	}

	public static float Round(float x, MidpointRounding mode)
	{
		return MathF.Round(x, mode);
	}

	public static float Round(float x, int digits, MidpointRounding mode)
	{
		return MathF.Round(x, digits, mode);
	}

	[Intrinsic]
	public static float Truncate(float x)
	{
		return MathF.Truncate(x);
	}

	int IFloatingPoint<float>.GetExponentByteCount()
	{
		return 1;
	}

	int IFloatingPoint<float>.GetExponentShortestBitLength()
	{
		sbyte exponent = Exponent;
		if (exponent >= 0)
		{
			return 8 - sbyte.LeadingZeroCount(exponent);
		}
		return 9 - sbyte.LeadingZeroCount((sbyte)(~exponent));
	}

	int IFloatingPoint<float>.GetSignificandByteCount()
	{
		return 4;
	}

	int IFloatingPoint<float>.GetSignificandBitLength()
	{
		return 24;
	}

	bool IFloatingPoint<float>.TryWriteExponentBigEndian(Span<byte> destination, out int bytesWritten)
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

	bool IFloatingPoint<float>.TryWriteExponentLittleEndian(Span<byte> destination, out int bytesWritten)
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

	bool IFloatingPoint<float>.TryWriteSignificandBigEndian(Span<byte> destination, out int bytesWritten)
	{
		if (destination.Length >= 4)
		{
			uint significand = Significand;
			_ = BitConverter.IsLittleEndian;
			significand = BinaryPrimitives.ReverseEndianness(significand);
			Unsafe.WriteUnaligned(ref MemoryMarshal.GetReference(destination), significand);
			bytesWritten = 4;
			return true;
		}
		bytesWritten = 0;
		return false;
	}

	bool IFloatingPoint<float>.TryWriteSignificandLittleEndian(Span<byte> destination, out int bytesWritten)
	{
		if (destination.Length >= 4)
		{
			uint significand = Significand;
			if (!BitConverter.IsLittleEndian)
			{
			}
			Unsafe.WriteUnaligned(ref MemoryMarshal.GetReference(destination), significand);
			bytesWritten = 4;
			return true;
		}
		bytesWritten = 0;
		return false;
	}

	[Intrinsic]
	public static float Atan2(float y, float x)
	{
		return MathF.Atan2(y, x);
	}

	public static float Atan2Pi(float y, float x)
	{
		return Atan2(y, x) / (float)Math.PI;
	}

	public static float BitDecrement(float x)
	{
		return MathF.BitDecrement(x);
	}

	public static float BitIncrement(float x)
	{
		return MathF.BitIncrement(x);
	}

	[Intrinsic]
	public static float FusedMultiplyAdd(float left, float right, float addend)
	{
		return MathF.FusedMultiplyAdd(left, right, addend);
	}

	public static float Ieee754Remainder(float left, float right)
	{
		return MathF.IEEERemainder(left, right);
	}

	public static int ILogB(float x)
	{
		return MathF.ILogB(x);
	}

	public static float Lerp(float value1, float value2, float amount)
	{
		return value1 * (1f - amount) + value2 * amount;
	}

	public static float ReciprocalEstimate(float x)
	{
		return MathF.ReciprocalEstimate(x);
	}

	public static float ReciprocalSqrtEstimate(float x)
	{
		return MathF.ReciprocalSqrtEstimate(x);
	}

	public static float ScaleB(float x, int n)
	{
		return MathF.ScaleB(x, n);
	}

	[Intrinsic]
	public static float Acosh(float x)
	{
		return MathF.Acosh(x);
	}

	[Intrinsic]
	public static float Asinh(float x)
	{
		return MathF.Asinh(x);
	}

	[Intrinsic]
	public static float Atanh(float x)
	{
		return MathF.Atanh(x);
	}

	[Intrinsic]
	public static float Cosh(float x)
	{
		return MathF.Cosh(x);
	}

	[Intrinsic]
	public static float Sinh(float x)
	{
		return MathF.Sinh(x);
	}

	[Intrinsic]
	public static float Tanh(float x)
	{
		return MathF.Tanh(x);
	}

	static float IIncrementOperators<float>.operator ++(float value)
	{
		return value += 1f;
	}

	[Intrinsic]
	public static float Log(float x)
	{
		return MathF.Log(x);
	}

	public static float Log(float x, float newBase)
	{
		return MathF.Log(x, newBase);
	}

	public static float LogP1(float x)
	{
		return MathF.Log(x + 1f);
	}

	[Intrinsic]
	public static float Log10(float x)
	{
		return MathF.Log10(x);
	}

	public static float Log2P1(float x)
	{
		return MathF.Log2(x + 1f);
	}

	public static float Log10P1(float x)
	{
		return MathF.Log10(x + 1f);
	}

	static float IModulusOperators<float, float, float>.operator %(float left, float right)
	{
		return left % right;
	}

	static float IMultiplyOperators<float, float, float>.operator *(float left, float right)
	{
		return left * right;
	}

	public static float Clamp(float value, float min, float max)
	{
		return Math.Clamp(value, min, max);
	}

	public static float CopySign(float value, float sign)
	{
		return MathF.CopySign(value, sign);
	}

	[Intrinsic]
	public static float Max(float x, float y)
	{
		return MathF.Max(x, y);
	}

	[Intrinsic]
	public static float MaxNumber(float x, float y)
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
	public static float Min(float x, float y)
	{
		return MathF.Min(x, y);
	}

	[Intrinsic]
	public static float MinNumber(float x, float y)
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

	public static int Sign(float value)
	{
		return MathF.Sign(value);
	}

	[Intrinsic]
	public static float Abs(float value)
	{
		return MathF.Abs(value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static float CreateChecked<TOther>(TOther value) where TOther : INumberBase<TOther>
	{
		if (typeof(TOther) == typeof(float))
		{
			return (float)(object)value;
		}
		if (!TryConvertFrom(value, out var result) && !TOther.TryConvertToChecked<float>(value, out result))
		{
			ThrowHelper.ThrowNotSupportedException();
		}
		return result;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static float CreateSaturating<TOther>(TOther value) where TOther : INumberBase<TOther>
	{
		if (typeof(TOther) == typeof(float))
		{
			return (float)(object)value;
		}
		if (!TryConvertFrom(value, out var result) && !TOther.TryConvertToSaturating<float>(value, out result))
		{
			ThrowHelper.ThrowNotSupportedException();
		}
		return result;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static float CreateTruncating<TOther>(TOther value) where TOther : INumberBase<TOther>
	{
		if (typeof(TOther) == typeof(float))
		{
			return (float)(object)value;
		}
		if (!TryConvertFrom(value, out var result) && !TOther.TryConvertToTruncating<float>(value, out result))
		{
			ThrowHelper.ThrowNotSupportedException();
		}
		return result;
	}

	static bool INumberBase<float>.IsCanonical(float value)
	{
		return true;
	}

	static bool INumberBase<float>.IsComplexNumber(float value)
	{
		return false;
	}

	public static bool IsEvenInteger(float value)
	{
		if (IsInteger(value))
		{
			return Abs(value % 2f) == 0f;
		}
		return false;
	}

	static bool INumberBase<float>.IsImaginaryNumber(float value)
	{
		return false;
	}

	public static bool IsInteger(float value)
	{
		if (IsFinite(value))
		{
			return value == Truncate(value);
		}
		return false;
	}

	public static bool IsOddInteger(float value)
	{
		if (IsInteger(value))
		{
			return Abs(value % 2f) == 1f;
		}
		return false;
	}

	public static bool IsPositive(float value)
	{
		return BitConverter.SingleToInt32Bits(value) >= 0;
	}

	public static bool IsRealNumber(float value)
	{
		return value == value;
	}

	static bool INumberBase<float>.IsZero(float value)
	{
		return value == 0f;
	}

	[Intrinsic]
	public static float MaxMagnitude(float x, float y)
	{
		return MathF.MaxMagnitude(x, y);
	}

	[Intrinsic]
	public static float MaxMagnitudeNumber(float x, float y)
	{
		float num = Abs(x);
		float num2 = Abs(y);
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
	public static float MinMagnitude(float x, float y)
	{
		return MathF.MinMagnitude(x, y);
	}

	[Intrinsic]
	public static float MinMagnitudeNumber(float x, float y)
	{
		float num = Abs(x);
		float num2 = Abs(y);
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
	static bool INumberBase<float>.TryConvertFromChecked<TOther>(TOther value, out float result)
	{
		return TryConvertFrom(value, out result);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	static bool INumberBase<float>.TryConvertFromSaturating<TOther>(TOther value, out float result)
	{
		return TryConvertFrom(value, out result);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	static bool INumberBase<float>.TryConvertFromTruncating<TOther>(TOther value, out float result)
	{
		return TryConvertFrom(value, out result);
	}

	private static bool TryConvertFrom<TOther>(TOther value, out float result) where TOther : INumberBase<TOther>
	{
		if (typeof(TOther) == typeof(double))
		{
			double num = (double)(object)value;
			result = (float)num;
			return true;
		}
		if (typeof(TOther) == typeof(Half))
		{
			Half half = (Half)(object)value;
			result = (float)half;
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
		if (typeof(TOther) == typeof(long))
		{
			long num4 = (long)(object)value;
			result = num4;
			return true;
		}
		if (typeof(TOther) == typeof(Int128))
		{
			Int128 @int = (Int128)(object)value;
			result = (float)@int;
			return true;
		}
		if (typeof(TOther) == typeof(nint))
		{
			nint num5 = (nint)(object)value;
			result = num5;
			return true;
		}
		if (typeof(TOther) == typeof(sbyte))
		{
			sbyte b = (sbyte)(object)value;
			result = b;
			return true;
		}
		result = 0f;
		return false;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	static bool INumberBase<float>.TryConvertToChecked<TOther>(float value, [MaybeNullWhen(false)] out TOther result)
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
	static bool INumberBase<float>.TryConvertToSaturating<TOther>(float value, [MaybeNullWhen(false)] out TOther result)
	{
		return TryConvertTo<TOther>(value, out result);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	static bool INumberBase<float>.TryConvertToTruncating<TOther>(float value, [MaybeNullWhen(false)] out TOther result)
	{
		return TryConvertTo<TOther>(value, out result);
	}

	private static bool TryConvertTo<TOther>(float value, [MaybeNullWhen(false)] out TOther result) where TOther : INumberBase<TOther>
	{
		if (typeof(TOther) == typeof(byte))
		{
			byte b = (byte)((value >= 255f) ? byte.MaxValue : ((!(value <= 0f)) ? ((byte)value) : 0));
			result = (TOther)(object)b;
			return true;
		}
		if (typeof(TOther) == typeof(char))
		{
			char c = ((value >= 65535f) ? '\uffff' : ((!(value <= 0f)) ? ((char)value) : '\0'));
			result = (TOther)(object)c;
			return true;
		}
		if (typeof(TOther) == typeof(decimal))
		{
			decimal num = ((value >= 7.9228163E+28f) ? decimal.MaxValue : ((value <= -7.9228163E+28f) ? decimal.MinValue : (IsNaN(value) ? 0.0m : ((decimal)value))));
			result = (TOther)(object)num;
			return true;
		}
		if (typeof(TOther) == typeof(ushort))
		{
			ushort num2 = (ushort)((value >= 65535f) ? ushort.MaxValue : ((!(value <= 0f)) ? ((ushort)value) : 0));
			result = (TOther)(object)num2;
			return true;
		}
		if (typeof(TOther) == typeof(uint))
		{
			uint num3 = ((value >= 4.2949673E+09f) ? uint.MaxValue : ((!(value <= 0f)) ? ((uint)value) : 0u));
			result = (TOther)(object)num3;
			return true;
		}
		if (typeof(TOther) == typeof(ulong))
		{
			ulong num4 = ((value >= 1.8446744E+19f) ? ulong.MaxValue : ((value <= 0f) ? 0 : (IsNaN(value) ? 0 : ((ulong)value))));
			result = (TOther)(object)num4;
			return true;
		}
		if (typeof(TOther) == typeof(UInt128))
		{
			UInt128 uInt = ((value == float.PositiveInfinity) ? UInt128.MaxValue : ((value <= 0f) ? UInt128.MinValue : ((UInt128)value)));
			result = (TOther)(object)uInt;
			return true;
		}
		if (typeof(TOther) == typeof(nuint))
		{
			nuint num5 = ((value >= 1.8446744E+19f) ? unchecked((nuint)(-1)) : ((value <= 0f) ? 0 : ((nuint)value)));
			result = (TOther)(object)num5;
			return true;
		}
		result = default(TOther);
		return false;
	}

	public static bool TryParse([NotNullWhen(true)] string? s, IFormatProvider? provider, out float result)
	{
		return TryParse(s, NumberStyles.Float | NumberStyles.AllowThousands, provider, out result);
	}

	[Intrinsic]
	public static float Pow(float x, float y)
	{
		return MathF.Pow(x, y);
	}

	[Intrinsic]
	public static float Cbrt(float x)
	{
		return MathF.Cbrt(x);
	}

	public static float Hypot(float x, float y)
	{
		if (IsFinite(x) && IsFinite(y))
		{
			float num = Abs(x);
			float num2 = Abs(y);
			if (num == 0f)
			{
				return num2;
			}
			if (num2 == 0f)
			{
				return num;
			}
			double num3 = num;
			num3 *= num3;
			double num4 = num2;
			num4 *= num4;
			return (float)double.Sqrt(num3 + num4);
		}
		if (IsInfinity(x) || IsInfinity(y))
		{
			return float.PositiveInfinity;
		}
		return float.NaN;
	}

	public static float RootN(float x, int n)
	{
		if (n > 0)
		{
			return n switch
			{
				2 => (x != 0f) ? Sqrt(x) : 0f, 
				3 => Cbrt(x), 
				_ => PositiveN(x, n), 
			};
		}
		if (n < 0)
		{
			return NegativeN(x, n);
		}
		return float.NaN;
		static float NegativeN(float x, int n)
		{
			if (IsFinite(x))
			{
				if (x != 0f)
				{
					if (x > 0f || IsOddInteger(n))
					{
						float value = (float)double.Pow(Abs(x), 1.0 / (double)n);
						return CopySign(value, x);
					}
					return float.NaN;
				}
				if (IsEvenInteger(n))
				{
					return float.PositiveInfinity;
				}
				return CopySign(float.PositiveInfinity, x);
			}
			if (IsNaN(x))
			{
				return float.NaN;
			}
			if (x > 0f)
			{
				return 0f;
			}
			return int.IsOddInteger(n) ? -0f : float.NaN;
		}
		static float PositiveN(float x, int n)
		{
			if (IsFinite(x))
			{
				if (x != 0f)
				{
					if (x > 0f || IsOddInteger(n))
					{
						float value2 = (float)double.Pow(Abs(x), 1.0 / (double)n);
						return CopySign(value2, x);
					}
					return float.NaN;
				}
				if (IsEvenInteger(n))
				{
					return 0f;
				}
				return CopySign(0f, x);
			}
			if (IsNaN(x))
			{
				return float.NaN;
			}
			if (x > 0f)
			{
				return float.PositiveInfinity;
			}
			return int.IsOddInteger(n) ? float.NegativeInfinity : float.NaN;
		}
	}

	[Intrinsic]
	public static float Sqrt(float x)
	{
		return MathF.Sqrt(x);
	}

	public static float Parse(ReadOnlySpan<char> s, IFormatProvider? provider)
	{
		return Parse(s, NumberStyles.Float | NumberStyles.AllowThousands, provider);
	}

	public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, out float result)
	{
		return TryParse(s, NumberStyles.Float | NumberStyles.AllowThousands, provider, out result);
	}

	static float ISubtractionOperators<float, float, float>.operator -(float left, float right)
	{
		return left - right;
	}

	[Intrinsic]
	public static float Acos(float x)
	{
		return MathF.Acos(x);
	}

	public static float AcosPi(float x)
	{
		return Acos(x) / (float)Math.PI;
	}

	[Intrinsic]
	public static float Asin(float x)
	{
		return MathF.Asin(x);
	}

	public static float AsinPi(float x)
	{
		return Asin(x) / (float)Math.PI;
	}

	[Intrinsic]
	public static float Atan(float x)
	{
		return MathF.Atan(x);
	}

	public static float AtanPi(float x)
	{
		return Atan(x) / (float)Math.PI;
	}

	[Intrinsic]
	public static float Cos(float x)
	{
		return MathF.Cos(x);
	}

	public static float CosPi(float x)
	{
		if (IsFinite(x))
		{
			float num = Abs(x);
			if (num < 8388608f)
			{
				if (num > 0.25f)
				{
					int num2 = (int)num;
					float num3 = num - (float)num2;
					float num4 = (int.IsOddInteger(num2) ? (-1f) : 1f);
					if (num3 <= 0.25f)
					{
						if (num3 != 0f)
						{
							return num4 * CosForIntervalPiBy4(num3 * (float)Math.PI);
						}
						return num4;
					}
					if (num3 <= 0.5f)
					{
						if (num3 != 0.5f)
						{
							return num4 * SinForIntervalPiBy4((0.5f - num3) * (float)Math.PI);
						}
						return 0f;
					}
					if ((double)num3 <= 0.75)
					{
						return (0f - num4) * SinForIntervalPiBy4((num3 - 0.5f) * (float)Math.PI);
					}
					return (0f - num4) * CosForIntervalPiBy4((1f - num3) * (float)Math.PI);
				}
				if (num >= 1f / 128f)
				{
					return CosForIntervalPiBy4(x * (float)Math.PI);
				}
				if (num >= 0.00012207031f)
				{
					float num5 = x * (float)Math.PI;
					return 1f - num5 * num5 * 0.5f;
				}
				return 1f;
			}
			if (num < 16777216f)
			{
				int value = BitConverter.SingleToInt32Bits(num);
				return int.IsOddInteger(value) ? (-1f) : 1f;
			}
			return 1f;
		}
		return float.NaN;
	}

	public static float DegreesToRadians(float degrees)
	{
		return degrees * (float)Math.PI / 180f;
	}

	public static float RadiansToDegrees(float radians)
	{
		return radians * 180f / (float)Math.PI;
	}

	[Intrinsic]
	public static float Sin(float x)
	{
		return MathF.Sin(x);
	}

	public static (float Sin, float Cos) SinCos(float x)
	{
		return MathF.SinCos(x);
	}

	public static (float SinPi, float CosPi) SinCosPi(float x)
	{
		float item;
		float item2;
		if (IsFinite(x))
		{
			float num = Abs(x);
			if (num < 8388608f)
			{
				if (num > 0.25f)
				{
					int num2 = (int)num;
					float num3 = num - (float)num2;
					float num4 = (int.IsOddInteger(num2) ? (-1f) : 1f);
					float num5 = ((x > 0f) ? 1f : (-1f)) * num4;
					float num6 = num4;
					if (num3 <= 0.25f)
					{
						if (num3 != 0f)
						{
							float x2 = num3 * (float)Math.PI;
							item = num5 * SinForIntervalPiBy4(x2);
							item2 = num6 * CosForIntervalPiBy4(x2);
						}
						else
						{
							item = x * 0f;
							item2 = num6;
						}
					}
					else if (num3 <= 0.5f)
					{
						if (num3 != 0.5f)
						{
							float x3 = (0.5f - num3) * (float)Math.PI;
							item = num5 * CosForIntervalPiBy4(x3);
							item2 = num6 * SinForIntervalPiBy4(x3);
						}
						else
						{
							item = num5;
							item2 = 0f;
						}
					}
					else if (num3 <= 0.75f)
					{
						float x4 = (num3 - 0.5f) * (float)Math.PI;
						item = num5 * CosForIntervalPiBy4(x4);
						item2 = (0f - num6) * SinForIntervalPiBy4(x4);
					}
					else
					{
						float x5 = (1f - num3) * (float)Math.PI;
						item = num5 * SinForIntervalPiBy4(x5);
						item2 = (0f - num6) * CosForIntervalPiBy4(x5);
					}
				}
				else if (num >= 1f / 128f)
				{
					float x6 = x * (float)Math.PI;
					item = SinForIntervalPiBy4(x6);
					item2 = CosForIntervalPiBy4(x6);
				}
				else if (num >= 0.00012207031f)
				{
					float num7 = x * (float)Math.PI;
					float num8 = num7 * num7;
					item = num7 - num8 * num7 * (1f / 6f);
					item2 = 1f - num8 * 0.5f;
				}
				else
				{
					item = x * (float)Math.PI;
					item2 = 1f;
				}
			}
			else if (num < 16777216f)
			{
				item = x * 0f;
				int value = BitConverter.SingleToInt32Bits(num);
				item2 = (int.IsOddInteger(value) ? (-1f) : 1f);
			}
			else
			{
				item = x * 0f;
				item2 = 1f;
			}
		}
		else
		{
			item = float.NaN;
			item2 = float.NaN;
		}
		return (SinPi: item, CosPi: item2);
	}

	public static float SinPi(float x)
	{
		if (IsFinite(x))
		{
			float num = Abs(x);
			if (num < 8388608f)
			{
				if (num > 0.25f)
				{
					int num2 = (int)num;
					float num3 = num - (float)num2;
					float num4 = ((x > 0f) ? 1f : (-1f)) * (int.IsOddInteger(num2) ? (-1f) : 1f);
					if (num3 <= 0.25f)
					{
						if (num3 != 0f)
						{
							return num4 * SinForIntervalPiBy4(num3 * (float)Math.PI);
						}
						return x * 0f;
					}
					if (num3 <= 0.5f)
					{
						if (num3 != 0.5f)
						{
							return num4 * CosForIntervalPiBy4((0.5f - num3) * (float)Math.PI);
						}
						return num4;
					}
					if (num3 <= 0.75f)
					{
						return num4 * CosForIntervalPiBy4((num3 - 0.5f) * (float)Math.PI);
					}
					return num4 * SinForIntervalPiBy4((1f - num3) * (float)Math.PI);
				}
				if (num >= 1f / 128f)
				{
					return SinForIntervalPiBy4(x * (float)Math.PI);
				}
				if (num >= 0.00012207031f)
				{
					float num5 = x * (float)Math.PI;
					return num5 - num5 * num5 * num5 * (1f / 6f);
				}
				return x * (float)Math.PI;
			}
			return x * 0f;
		}
		return float.NaN;
	}

	[Intrinsic]
	public static float Tan(float x)
	{
		return MathF.Tan(x);
	}

	public static float TanPi(float x)
	{
		if (IsFinite(x))
		{
			float num = Abs(x);
			float num2 = ((x > 0f) ? 1f : (-1f));
			if (num < 8388608f)
			{
				if (num > 0.25f)
				{
					int num3 = (int)num;
					float num4 = num - (float)num3;
					if (num4 <= 0.25f)
					{
						if (num4 != 0f)
						{
							return num2 * TanForIntervalPiBy4(num4 * (float)Math.PI, isReciprocal: false);
						}
						return num2 * (int.IsOddInteger(num3) ? -0f : 0f);
					}
					if (num4 <= 0.5f)
					{
						if (num4 != 0.5f)
						{
							return (0f - num2) * TanForIntervalPiBy4((0.5f - num4) * (float)Math.PI, isReciprocal: true);
						}
						return num2 * (int.IsOddInteger(num3) ? float.NegativeInfinity : float.PositiveInfinity);
					}
					if (num4 <= 0.75f)
					{
						return num2 * TanForIntervalPiBy4((num4 - 0.5f) * (float)Math.PI, isReciprocal: true);
					}
					return (0f - num2) * TanForIntervalPiBy4((1f - num4) * (float)Math.PI, isReciprocal: false);
				}
				if (num >= 1f / 128f)
				{
					return TanForIntervalPiBy4(x * (float)Math.PI, isReciprocal: false);
				}
				if (num >= 0.00012207031f)
				{
					float num5 = x * (float)Math.PI;
					return num5 + num5 * num5 * num5 * (1f / 3f);
				}
				return x * (float)Math.PI;
			}
			if (num < 16777216f)
			{
				int value = BitConverter.SingleToInt32Bits(num);
				return num2 * (int.IsOddInteger(value) ? -0f : 0f);
			}
			return num2 * 0f;
		}
		return float.NaN;
	}

	static float IUnaryNegationOperators<float, float>.operator -(float value)
	{
		return 0f - value;
	}

	static float IUnaryPlusOperators<float, float>.operator +(float value)
	{
		return value;
	}

	public static float Parse(ReadOnlySpan<byte> utf8Text, NumberStyles style = NumberStyles.Float | NumberStyles.AllowThousands, IFormatProvider? provider = null)
	{
		NumberFormatInfo.ValidateParseStyleInteger(style);
		return Number.ParseFloat<byte, float>(utf8Text, style, NumberFormatInfo.GetInstance(provider));
	}

	public static bool TryParse(ReadOnlySpan<byte> utf8Text, NumberStyles style, IFormatProvider? provider, out float result)
	{
		NumberFormatInfo.ValidateParseStyleInteger(style);
		return Number.TryParseFloat<byte, float>(utf8Text, style, NumberFormatInfo.GetInstance(provider), out result);
	}

	public static float Parse(ReadOnlySpan<byte> utf8Text, IFormatProvider? provider)
	{
		return Parse(utf8Text, NumberStyles.Float | NumberStyles.AllowThousands, provider);
	}

	public static bool TryParse(ReadOnlySpan<byte> utf8Text, IFormatProvider? provider, out float result)
	{
		return TryParse(utf8Text, NumberStyles.Float | NumberStyles.AllowThousands, provider, out result);
	}

	static float IBinaryFloatParseAndFormatInfo<float>.BitsToFloat(ulong bits)
	{
		return BitConverter.UInt32BitsToSingle((uint)bits);
	}

	static ulong IBinaryFloatParseAndFormatInfo<float>.FloatToBits(float value)
	{
		return BitConverter.SingleToUInt32Bits(value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static float CosForIntervalPiBy4(float x)
	{
		double num = x * x;
		double num2 = -2.755731727234419E-07;
		num2 = num2 * num + 2.480158729876704E-05;
		num2 = num2 * num + -0.0013888888888887398;
		num2 = num2 * num + 1.0 / 24.0;
		num2 *= num * num;
		num2 += 1.0 - 0.5 * num;
		return (float)num2;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static float SinForIntervalPiBy4(float x)
	{
		double num = x * x;
		double num2 = 2.7557316103728802E-06;
		num2 = num2 * num + -0.00019841269836761127;
		num2 = num2 * num + 0.00833333333333095;
		num2 = num2 * num + -1.0 / 6.0;
		num2 *= (double)x * num;
		num2 += (double)x;
		return (float)num2;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static float TanForIntervalPiBy4(float x, bool isReciprocal)
	{
		double num = x * x;
		double num2 = 0.01844239256901656;
		num2 = -0.5139650547885454 + num2 * num;
		num2 = 1.1558882143468838 + num2 * num;
		double num3 = -0.017203248047148168;
		num3 = 0.3852960712639954 + num3 * num;
		double num4 = (double)x * num;
		num4 *= num3 / num2;
		num4 += (double)x;
		if (isReciprocal)
		{
			num4 = -1.0 / num4;
		}
		return (float)num4;
	}
}
