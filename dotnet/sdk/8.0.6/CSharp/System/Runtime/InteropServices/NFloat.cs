using System.Buffers.Binary;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Versioning;

namespace System.Runtime.InteropServices;

[Intrinsic]
[NonVersionable]
public readonly struct NFloat : IBinaryFloatingPointIeee754<NFloat>, IBinaryNumber<NFloat>, IBitwiseOperators<NFloat, NFloat, NFloat>, INumber<NFloat>, IComparable, IComparable<NFloat>, IComparisonOperators<NFloat, NFloat, bool>, IEqualityOperators<NFloat, NFloat, bool>, IModulusOperators<NFloat, NFloat, NFloat>, INumberBase<NFloat>, IAdditionOperators<NFloat, NFloat, NFloat>, IAdditiveIdentity<NFloat, NFloat>, IDecrementOperators<NFloat>, IDivisionOperators<NFloat, NFloat, NFloat>, IEquatable<NFloat>, IIncrementOperators<NFloat>, IMultiplicativeIdentity<NFloat, NFloat>, IMultiplyOperators<NFloat, NFloat, NFloat>, ISpanFormattable, IFormattable, ISpanParsable<NFloat>, IParsable<NFloat>, ISubtractionOperators<NFloat, NFloat, NFloat>, IUnaryPlusOperators<NFloat, NFloat>, IUnaryNegationOperators<NFloat, NFloat>, IUtf8SpanFormattable, IUtf8SpanParsable<NFloat>, IFloatingPointIeee754<NFloat>, IExponentialFunctions<NFloat>, IFloatingPointConstants<NFloat>, IFloatingPoint<NFloat>, ISignedNumber<NFloat>, IHyperbolicFunctions<NFloat>, ILogarithmicFunctions<NFloat>, IPowerFunctions<NFloat>, IRootFunctions<NFloat>, ITrigonometricFunctions<NFloat>, IMinMaxValue<NFloat>
{
	private readonly double _value;

	public static NFloat Epsilon
	{
		[NonVersionable]
		get
		{
			return new NFloat(double.Epsilon);
		}
	}

	public static NFloat MaxValue
	{
		[NonVersionable]
		get
		{
			return new NFloat(double.MaxValue);
		}
	}

	public static NFloat MinValue
	{
		[NonVersionable]
		get
		{
			return new NFloat(double.MinValue);
		}
	}

	public static NFloat NaN
	{
		[NonVersionable]
		get
		{
			return new NFloat(double.NaN);
		}
	}

	public static NFloat NegativeInfinity
	{
		[NonVersionable]
		get
		{
			return new NFloat(double.NegativeInfinity);
		}
	}

	public static NFloat PositiveInfinity
	{
		[NonVersionable]
		get
		{
			return new NFloat(double.PositiveInfinity);
		}
	}

	public static int Size
	{
		[NonVersionable]
		get
		{
			return 8;
		}
	}

	public double Value
	{
		[NonVersionable]
		get
		{
			return _value;
		}
	}

	static NFloat IAdditiveIdentity<NFloat, NFloat>.AdditiveIdentity => new NFloat(0.0);

	static NFloat IBinaryNumber<NFloat>.AllBitsSet
	{
		[NonVersionable]
		get
		{
			return (NFloat)BitConverter.UInt64BitsToDouble(ulong.MaxValue);
		}
	}

	public static NFloat E => new NFloat(Math.E);

	public static NFloat Pi => new NFloat(Math.PI);

	public static NFloat Tau => new NFloat(Math.PI * 2.0);

	public static NFloat NegativeZero => new NFloat(-0.0);

	static NFloat IMultiplicativeIdentity<NFloat, NFloat>.MultiplicativeIdentity => new NFloat(1.0);

	static NFloat INumberBase<NFloat>.One => new NFloat(1.0);

	static int INumberBase<NFloat>.Radix => 2;

	static NFloat INumberBase<NFloat>.Zero => new NFloat(0.0);

	static NFloat ISignedNumber<NFloat>.NegativeOne => new NFloat(-1.0);

	[NonVersionable]
	public NFloat(float value)
	{
		_value = value;
	}

	[NonVersionable]
	public NFloat(double value)
	{
		_value = value;
	}

	[NonVersionable]
	public static NFloat operator +(NFloat value)
	{
		return value;
	}

	[NonVersionable]
	public static NFloat operator -(NFloat value)
	{
		return new NFloat(0.0 - value._value);
	}

	[NonVersionable]
	public static NFloat operator ++(NFloat value)
	{
		double value2 = value._value;
		value2 += 1.0;
		return new NFloat(value2);
	}

	[NonVersionable]
	public static NFloat operator --(NFloat value)
	{
		double value2 = value._value;
		value2 -= 1.0;
		return new NFloat(value2);
	}

	[NonVersionable]
	public static NFloat operator +(NFloat left, NFloat right)
	{
		return new NFloat(left._value + right._value);
	}

	[NonVersionable]
	public static NFloat operator -(NFloat left, NFloat right)
	{
		return new NFloat(left._value - right._value);
	}

	[NonVersionable]
	public static NFloat operator *(NFloat left, NFloat right)
	{
		return new NFloat(left._value * right._value);
	}

	[NonVersionable]
	public static NFloat operator /(NFloat left, NFloat right)
	{
		return new NFloat(left._value / right._value);
	}

	[NonVersionable]
	public static NFloat operator %(NFloat left, NFloat right)
	{
		return new NFloat(left._value % right._value);
	}

	[NonVersionable]
	public static bool operator ==(NFloat left, NFloat right)
	{
		return left._value == right._value;
	}

	[NonVersionable]
	public static bool operator !=(NFloat left, NFloat right)
	{
		return left._value != right._value;
	}

	[NonVersionable]
	public static bool operator <(NFloat left, NFloat right)
	{
		return left._value < right._value;
	}

	[NonVersionable]
	public static bool operator <=(NFloat left, NFloat right)
	{
		return left._value <= right._value;
	}

	[NonVersionable]
	public static bool operator >(NFloat left, NFloat right)
	{
		return left._value > right._value;
	}

	[NonVersionable]
	public static bool operator >=(NFloat left, NFloat right)
	{
		return left._value >= right._value;
	}

	[NonVersionable]
	public static explicit operator NFloat(decimal value)
	{
		return new NFloat((double)value);
	}

	[NonVersionable]
	public static explicit operator NFloat(double value)
	{
		return new NFloat(value);
	}

	[NonVersionable]
	public static explicit operator byte(NFloat value)
	{
		return (byte)value._value;
	}

	[NonVersionable]
	public static explicit operator checked byte(NFloat value)
	{
		return checked((byte)value._value);
	}

	[NonVersionable]
	public static explicit operator char(NFloat value)
	{
		return (char)value._value;
	}

	[NonVersionable]
	public static explicit operator checked char(NFloat value)
	{
		return (char)checked((ushort)value._value);
	}

	[NonVersionable]
	public static explicit operator decimal(NFloat value)
	{
		return (decimal)value._value;
	}

	[NonVersionable]
	public static explicit operator Half(NFloat value)
	{
		return (Half)value._value;
	}

	[NonVersionable]
	public static explicit operator short(NFloat value)
	{
		return (short)value._value;
	}

	[NonVersionable]
	public static explicit operator checked short(NFloat value)
	{
		return checked((short)value._value);
	}

	[NonVersionable]
	public static explicit operator int(NFloat value)
	{
		return (int)value._value;
	}

	[NonVersionable]
	public static explicit operator checked int(NFloat value)
	{
		return checked((int)value._value);
	}

	[NonVersionable]
	public static explicit operator long(NFloat value)
	{
		return (long)value._value;
	}

	[NonVersionable]
	public static explicit operator checked long(NFloat value)
	{
		return checked((long)value._value);
	}

	[NonVersionable]
	public static explicit operator Int128(NFloat value)
	{
		return (Int128)value._value;
	}

	[NonVersionable]
	public static explicit operator checked Int128(NFloat value)
	{
		return checked((Int128)value._value);
	}

	[NonVersionable]
	public static explicit operator nint(NFloat value)
	{
		return (nint)value._value;
	}

	[NonVersionable]
	public static explicit operator checked nint(NFloat value)
	{
		return checked((nint)value._value);
	}

	[NonVersionable]
	[CLSCompliant(false)]
	public static explicit operator sbyte(NFloat value)
	{
		return (sbyte)value._value;
	}

	[NonVersionable]
	[CLSCompliant(false)]
	public static explicit operator checked sbyte(NFloat value)
	{
		return checked((sbyte)value._value);
	}

	[NonVersionable]
	public static explicit operator float(NFloat value)
	{
		return (float)value._value;
	}

	[NonVersionable]
	[CLSCompliant(false)]
	public static explicit operator ushort(NFloat value)
	{
		return (ushort)value._value;
	}

	[NonVersionable]
	[CLSCompliant(false)]
	public static explicit operator checked ushort(NFloat value)
	{
		return checked((ushort)value._value);
	}

	[NonVersionable]
	[CLSCompliant(false)]
	public static explicit operator uint(NFloat value)
	{
		return (uint)value._value;
	}

	[NonVersionable]
	[CLSCompliant(false)]
	public static explicit operator checked uint(NFloat value)
	{
		return checked((uint)value._value);
	}

	[NonVersionable]
	[CLSCompliant(false)]
	public static explicit operator ulong(NFloat value)
	{
		return (ulong)value._value;
	}

	[NonVersionable]
	[CLSCompliant(false)]
	public static explicit operator checked ulong(NFloat value)
	{
		return checked((ulong)value._value);
	}

	[NonVersionable]
	[CLSCompliant(false)]
	public static explicit operator UInt128(NFloat value)
	{
		return (UInt128)value._value;
	}

	[NonVersionable]
	[CLSCompliant(false)]
	public static explicit operator checked UInt128(NFloat value)
	{
		return checked((UInt128)value._value);
	}

	[NonVersionable]
	[CLSCompliant(false)]
	public static explicit operator nuint(NFloat value)
	{
		return (nuint)value._value;
	}

	[NonVersionable]
	[CLSCompliant(false)]
	public static explicit operator checked nuint(NFloat value)
	{
		return checked((nuint)value._value);
	}

	[NonVersionable]
	public static implicit operator NFloat(byte value)
	{
		return new NFloat((double)(int)value);
	}

	[NonVersionable]
	public static implicit operator NFloat(char value)
	{
		return new NFloat((double)(int)value);
	}

	[NonVersionable]
	public static implicit operator NFloat(Half value)
	{
		return (float)value;
	}

	[NonVersionable]
	public static implicit operator NFloat(short value)
	{
		return new NFloat((double)value);
	}

	[NonVersionable]
	public static implicit operator NFloat(int value)
	{
		return new NFloat((double)value);
	}

	[NonVersionable]
	public static implicit operator NFloat(long value)
	{
		return new NFloat((double)value);
	}

	[NonVersionable]
	public static explicit operator NFloat(Int128 value)
	{
		if (Int128.IsNegative(value))
		{
			value = -value;
			return -(NFloat)(UInt128)value;
		}
		return (NFloat)(UInt128)value;
	}

	[NonVersionable]
	public static implicit operator NFloat(nint value)
	{
		return new NFloat((double)value);
	}

	[NonVersionable]
	[CLSCompliant(false)]
	public static implicit operator NFloat(sbyte value)
	{
		return new NFloat((double)value);
	}

	[NonVersionable]
	public static implicit operator NFloat(float value)
	{
		return new NFloat((double)value);
	}

	[NonVersionable]
	[CLSCompliant(false)]
	public static implicit operator NFloat(ushort value)
	{
		return new NFloat((double)(int)value);
	}

	[NonVersionable]
	[CLSCompliant(false)]
	public static implicit operator NFloat(uint value)
	{
		return new NFloat((double)value);
	}

	[NonVersionable]
	[CLSCompliant(false)]
	public static implicit operator NFloat(ulong value)
	{
		return new NFloat((double)value);
	}

	[NonVersionable]
	[CLSCompliant(false)]
	public static explicit operator NFloat(UInt128 value)
	{
		return (NFloat)(double)value;
	}

	[NonVersionable]
	[CLSCompliant(false)]
	public static implicit operator NFloat(nuint value)
	{
		return new NFloat((double)value);
	}

	public static implicit operator double(NFloat value)
	{
		return value._value;
	}

	[NonVersionable]
	public static bool IsFinite(NFloat value)
	{
		return double.IsFinite(value._value);
	}

	[NonVersionable]
	public static bool IsInfinity(NFloat value)
	{
		return double.IsInfinity(value._value);
	}

	[NonVersionable]
	public static bool IsNaN(NFloat value)
	{
		return double.IsNaN(value._value);
	}

	[NonVersionable]
	public static bool IsNegative(NFloat value)
	{
		return double.IsNegative(value._value);
	}

	[NonVersionable]
	public static bool IsNegativeInfinity(NFloat value)
	{
		return double.IsNegativeInfinity(value._value);
	}

	[NonVersionable]
	public static bool IsNormal(NFloat value)
	{
		return double.IsNormal(value._value);
	}

	[NonVersionable]
	public static bool IsPositiveInfinity(NFloat value)
	{
		return double.IsPositiveInfinity(value._value);
	}

	[NonVersionable]
	public static bool IsSubnormal(NFloat value)
	{
		return double.IsSubnormal(value._value);
	}

	public static NFloat Parse(string s)
	{
		double value = double.Parse(s);
		return new NFloat(value);
	}

	public static NFloat Parse(string s, NumberStyles style)
	{
		double value = double.Parse(s, style);
		return new NFloat(value);
	}

	public static NFloat Parse(string s, IFormatProvider? provider)
	{
		double value = double.Parse(s, provider);
		return new NFloat(value);
	}

	public static NFloat Parse(string s, NumberStyles style, IFormatProvider? provider)
	{
		double value = double.Parse(s, style, provider);
		return new NFloat(value);
	}

	public static NFloat Parse(ReadOnlySpan<char> s, NumberStyles style = NumberStyles.Float | NumberStyles.AllowThousands, IFormatProvider? provider = null)
	{
		double value = double.Parse(s, style, provider);
		return new NFloat(value);
	}

	public static bool TryParse([NotNullWhen(true)] string? s, out NFloat result)
	{
		Unsafe.SkipInit<NFloat>(out result);
		return double.TryParse(s, out Unsafe.As<NFloat, double>(ref result));
	}

	public static bool TryParse(ReadOnlySpan<char> s, out NFloat result)
	{
		Unsafe.SkipInit<NFloat>(out result);
		return double.TryParse(s, out Unsafe.As<NFloat, double>(ref result));
	}

	public static bool TryParse(ReadOnlySpan<byte> utf8Text, out NFloat result)
	{
		Unsafe.SkipInit<NFloat>(out result);
		return double.TryParse(utf8Text, out Unsafe.As<NFloat, double>(ref result));
	}

	public static bool TryParse([NotNullWhen(true)] string? s, NumberStyles style, IFormatProvider? provider, out NFloat result)
	{
		Unsafe.SkipInit<NFloat>(out result);
		return double.TryParse(s, style, provider, out Unsafe.As<NFloat, double>(ref result));
	}

	public static bool TryParse(ReadOnlySpan<char> s, NumberStyles style, IFormatProvider? provider, out NFloat result)
	{
		Unsafe.SkipInit<NFloat>(out result);
		return double.TryParse(s, style, provider, out Unsafe.As<NFloat, double>(ref result));
	}

	public int CompareTo(object? obj)
	{
		if (obj is NFloat nFloat)
		{
			if (_value < nFloat._value)
			{
				return -1;
			}
			if (_value > nFloat._value)
			{
				return 1;
			}
			if (_value == nFloat._value)
			{
				return 0;
			}
			if (double.IsNaN(_value))
			{
				if (!double.IsNaN(nFloat._value))
				{
					return -1;
				}
				return 0;
			}
			return 1;
		}
		if (obj == null)
		{
			return 1;
		}
		throw new ArgumentException(SR.Arg_MustBeNFloat);
	}

	public int CompareTo(NFloat other)
	{
		return _value.CompareTo(other._value);
	}

	public override bool Equals([NotNullWhen(true)] object? obj)
	{
		if (obj is NFloat other)
		{
			return Equals(other);
		}
		return false;
	}

	public bool Equals(NFloat other)
	{
		return _value.Equals(other._value);
	}

	public override int GetHashCode()
	{
		return _value.GetHashCode();
	}

	public override string ToString()
	{
		return _value.ToString();
	}

	public string ToString([StringSyntax("NumericFormat")] string? format)
	{
		return _value.ToString(format);
	}

	public string ToString(IFormatProvider? provider)
	{
		return _value.ToString(provider);
	}

	public string ToString([StringSyntax("NumericFormat")] string? format, IFormatProvider? provider)
	{
		return _value.ToString(format, provider);
	}

	public bool TryFormat(Span<char> destination, out int charsWritten, [StringSyntax("NumericFormat")] ReadOnlySpan<char> format = default(ReadOnlySpan<char>), IFormatProvider? provider = null)
	{
		return _value.TryFormat(destination, out charsWritten, format, provider);
	}

	public bool TryFormat(Span<byte> utf8Destination, out int bytesWritten, [StringSyntax("NumericFormat")] ReadOnlySpan<char> format = default(ReadOnlySpan<char>), IFormatProvider? provider = null)
	{
		return _value.TryFormat(utf8Destination, out bytesWritten, format, provider);
	}

	public static bool IsPow2(NFloat value)
	{
		return double.IsPow2(value._value);
	}

	public static NFloat Log2(NFloat value)
	{
		return new NFloat(double.Log2(value._value));
	}

	static NFloat IBitwiseOperators<NFloat, NFloat, NFloat>.operator &(NFloat left, NFloat right)
	{
		ulong value = BitConverter.DoubleToUInt64Bits(left._value) & BitConverter.DoubleToUInt64Bits(right._value);
		double value2 = BitConverter.UInt64BitsToDouble(value);
		return new NFloat(value2);
	}

	static NFloat IBitwiseOperators<NFloat, NFloat, NFloat>.operator |(NFloat left, NFloat right)
	{
		ulong value = BitConverter.DoubleToUInt64Bits(left._value) | BitConverter.DoubleToUInt64Bits(right._value);
		double value2 = BitConverter.UInt64BitsToDouble(value);
		return new NFloat(value2);
	}

	static NFloat IBitwiseOperators<NFloat, NFloat, NFloat>.operator ^(NFloat left, NFloat right)
	{
		ulong value = BitConverter.DoubleToUInt64Bits(left._value) ^ BitConverter.DoubleToUInt64Bits(right._value);
		double value2 = BitConverter.UInt64BitsToDouble(value);
		return new NFloat(value2);
	}

	static NFloat IBitwiseOperators<NFloat, NFloat, NFloat>.operator ~(NFloat value)
	{
		ulong value2 = ~BitConverter.DoubleToUInt64Bits(value._value);
		double value3 = BitConverter.UInt64BitsToDouble(value2);
		return new NFloat(value3);
	}

	public static NFloat Exp(NFloat x)
	{
		return new NFloat(double.Exp(x._value));
	}

	public static NFloat ExpM1(NFloat x)
	{
		return new NFloat(double.ExpM1(x._value));
	}

	public static NFloat Exp2(NFloat x)
	{
		return new NFloat(double.Exp2(x._value));
	}

	public static NFloat Exp2M1(NFloat x)
	{
		return new NFloat(double.Exp2M1(x._value));
	}

	public static NFloat Exp10(NFloat x)
	{
		return new NFloat(double.Exp10(x._value));
	}

	public static NFloat Exp10M1(NFloat x)
	{
		return new NFloat(double.Exp10M1(x._value));
	}

	public static NFloat Ceiling(NFloat x)
	{
		return new NFloat(double.Ceiling(x._value));
	}

	public static NFloat Floor(NFloat x)
	{
		return new NFloat(double.Floor(x._value));
	}

	public static NFloat Round(NFloat x)
	{
		return new NFloat(double.Round(x._value));
	}

	public static NFloat Round(NFloat x, int digits)
	{
		return new NFloat(double.Round(x._value, digits));
	}

	public static NFloat Round(NFloat x, MidpointRounding mode)
	{
		return new NFloat(double.Round(x._value, mode));
	}

	public static NFloat Round(NFloat x, int digits, MidpointRounding mode)
	{
		return new NFloat(double.Round(x._value, digits, mode));
	}

	public static NFloat Truncate(NFloat x)
	{
		return new NFloat(double.Truncate(x._value));
	}

	int IFloatingPoint<NFloat>.GetExponentByteCount()
	{
		return 2;
	}

	int IFloatingPoint<NFloat>.GetExponentShortestBitLength()
	{
		short exponent = _value.Exponent;
		if (exponent >= 0)
		{
			return 16 - short.LeadingZeroCount(exponent);
		}
		return 17 - short.LeadingZeroCount((short)(~exponent));
	}

	int IFloatingPoint<NFloat>.GetSignificandByteCount()
	{
		return 8;
	}

	int IFloatingPoint<NFloat>.GetSignificandBitLength()
	{
		return 53;
	}

	bool IFloatingPoint<NFloat>.TryWriteExponentBigEndian(Span<byte> destination, out int bytesWritten)
	{
		if (destination.Length >= 2)
		{
			short exponent = _value.Exponent;
			_ = BitConverter.IsLittleEndian;
			exponent = BinaryPrimitives.ReverseEndianness(exponent);
			Unsafe.WriteUnaligned(ref MemoryMarshal.GetReference(destination), exponent);
			bytesWritten = 2;
			return true;
		}
		bytesWritten = 0;
		return false;
	}

	bool IFloatingPoint<NFloat>.TryWriteExponentLittleEndian(Span<byte> destination, out int bytesWritten)
	{
		if (destination.Length >= 2)
		{
			short exponent = _value.Exponent;
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

	bool IFloatingPoint<NFloat>.TryWriteSignificandBigEndian(Span<byte> destination, out int bytesWritten)
	{
		if (destination.Length >= 8)
		{
			ulong significand = _value.Significand;
			_ = BitConverter.IsLittleEndian;
			significand = BinaryPrimitives.ReverseEndianness(significand);
			Unsafe.WriteUnaligned(ref MemoryMarshal.GetReference(destination), significand);
			bytesWritten = 8;
			return true;
		}
		bytesWritten = 0;
		return false;
	}

	bool IFloatingPoint<NFloat>.TryWriteSignificandLittleEndian(Span<byte> destination, out int bytesWritten)
	{
		if (destination.Length >= 8)
		{
			ulong significand = _value.Significand;
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

	public static NFloat Atan2(NFloat y, NFloat x)
	{
		return new NFloat(double.Atan2(y._value, x._value));
	}

	public static NFloat Atan2Pi(NFloat y, NFloat x)
	{
		return new NFloat(double.Atan2Pi(y._value, x._value));
	}

	public static NFloat BitDecrement(NFloat x)
	{
		return new NFloat(double.BitDecrement(x._value));
	}

	public static NFloat BitIncrement(NFloat x)
	{
		return new NFloat(double.BitIncrement(x._value));
	}

	public static NFloat FusedMultiplyAdd(NFloat left, NFloat right, NFloat addend)
	{
		return new NFloat(double.FusedMultiplyAdd(left._value, right._value, addend._value));
	}

	public static NFloat Ieee754Remainder(NFloat left, NFloat right)
	{
		return new NFloat(double.Ieee754Remainder(left._value, right._value));
	}

	public static int ILogB(NFloat x)
	{
		return double.ILogB(x._value);
	}

	public static NFloat Lerp(NFloat value1, NFloat value2, NFloat amount)
	{
		return new NFloat(double.Lerp(value1._value, value2._value, amount._value));
	}

	public static NFloat ReciprocalEstimate(NFloat x)
	{
		return new NFloat(double.ReciprocalEstimate(x._value));
	}

	public static NFloat ReciprocalSqrtEstimate(NFloat x)
	{
		return new NFloat(double.ReciprocalSqrtEstimate(x._value));
	}

	public static NFloat ScaleB(NFloat x, int n)
	{
		return new NFloat(double.ScaleB(x._value, n));
	}

	public static NFloat Acosh(NFloat x)
	{
		return new NFloat(double.Acosh(x._value));
	}

	public static NFloat Asinh(NFloat x)
	{
		return new NFloat(double.Asinh(x._value));
	}

	public static NFloat Atanh(NFloat x)
	{
		return new NFloat(double.Atanh(x._value));
	}

	public static NFloat Cosh(NFloat x)
	{
		return new NFloat(double.Cosh(x._value));
	}

	public static NFloat Sinh(NFloat x)
	{
		return new NFloat(double.Sinh(x._value));
	}

	public static NFloat Tanh(NFloat x)
	{
		return new NFloat(double.Tanh(x._value));
	}

	public static NFloat Log(NFloat x)
	{
		return new NFloat(double.Log(x._value));
	}

	public static NFloat Log(NFloat x, NFloat newBase)
	{
		return new NFloat(double.Log(x._value, newBase._value));
	}

	public static NFloat LogP1(NFloat x)
	{
		return new NFloat(double.LogP1(x._value));
	}

	public static NFloat Log2P1(NFloat x)
	{
		return new NFloat(double.Log2P1(x._value));
	}

	public static NFloat Log10(NFloat x)
	{
		return new NFloat(double.Log10(x._value));
	}

	public static NFloat Log10P1(NFloat x)
	{
		return new NFloat(double.Log10P1(x._value));
	}

	public static NFloat Clamp(NFloat value, NFloat min, NFloat max)
	{
		return new NFloat(double.Clamp(value._value, min._value, max._value));
	}

	public static NFloat CopySign(NFloat value, NFloat sign)
	{
		return new NFloat(double.CopySign(value._value, sign._value));
	}

	public static NFloat Max(NFloat x, NFloat y)
	{
		return new NFloat(double.Max(x._value, y._value));
	}

	public static NFloat MaxNumber(NFloat x, NFloat y)
	{
		return new NFloat(double.MaxNumber(x._value, y._value));
	}

	public static NFloat Min(NFloat x, NFloat y)
	{
		return new NFloat(double.Min(x._value, y._value));
	}

	public static NFloat MinNumber(NFloat x, NFloat y)
	{
		return new NFloat(double.MinNumber(x._value, y._value));
	}

	public static int Sign(NFloat value)
	{
		return double.Sign(value._value);
	}

	public static NFloat Abs(NFloat value)
	{
		return new NFloat(double.Abs(value._value));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static NFloat CreateChecked<TOther>(TOther value) where TOther : INumberBase<TOther>
	{
		if (typeof(TOther) == typeof(NFloat))
		{
			return (NFloat)(object)value;
		}
		if (!TryConvertFrom(value, out var result) && !TOther.TryConvertToChecked<NFloat>(value, out result))
		{
			ThrowHelper.ThrowNotSupportedException();
		}
		return result;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static NFloat CreateSaturating<TOther>(TOther value) where TOther : INumberBase<TOther>
	{
		if (typeof(TOther) == typeof(NFloat))
		{
			return (NFloat)(object)value;
		}
		if (!TryConvertFrom(value, out var result) && !TOther.TryConvertToSaturating<NFloat>(value, out result))
		{
			ThrowHelper.ThrowNotSupportedException();
		}
		return result;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static NFloat CreateTruncating<TOther>(TOther value) where TOther : INumberBase<TOther>
	{
		if (typeof(TOther) == typeof(NFloat))
		{
			return (NFloat)(object)value;
		}
		if (!TryConvertFrom(value, out var result) && !TOther.TryConvertToTruncating<NFloat>(value, out result))
		{
			ThrowHelper.ThrowNotSupportedException();
		}
		return result;
	}

	static bool INumberBase<NFloat>.IsCanonical(NFloat value)
	{
		return true;
	}

	static bool INumberBase<NFloat>.IsComplexNumber(NFloat value)
	{
		return false;
	}

	public static bool IsEvenInteger(NFloat value)
	{
		return double.IsEvenInteger(value._value);
	}

	static bool INumberBase<NFloat>.IsImaginaryNumber(NFloat value)
	{
		return false;
	}

	public static bool IsInteger(NFloat value)
	{
		return double.IsInteger(value._value);
	}

	public static bool IsOddInteger(NFloat value)
	{
		return double.IsOddInteger(value._value);
	}

	public static bool IsPositive(NFloat value)
	{
		return double.IsPositive(value._value);
	}

	public static bool IsRealNumber(NFloat value)
	{
		return double.IsRealNumber(value._value);
	}

	static bool INumberBase<NFloat>.IsZero(NFloat value)
	{
		return value == 0;
	}

	public static NFloat MaxMagnitude(NFloat x, NFloat y)
	{
		return new NFloat(double.MaxMagnitude(x._value, y._value));
	}

	public static NFloat MaxMagnitudeNumber(NFloat x, NFloat y)
	{
		return new NFloat(double.MaxMagnitudeNumber(x._value, y._value));
	}

	public static NFloat MinMagnitude(NFloat x, NFloat y)
	{
		return new NFloat(double.MinMagnitude(x._value, y._value));
	}

	public static NFloat MinMagnitudeNumber(NFloat x, NFloat y)
	{
		return new NFloat(double.MinMagnitudeNumber(x._value, y._value));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	static bool INumberBase<NFloat>.TryConvertFromChecked<TOther>(TOther value, out NFloat result)
	{
		return TryConvertFrom(value, out result);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	static bool INumberBase<NFloat>.TryConvertFromSaturating<TOther>(TOther value, out NFloat result)
	{
		return TryConvertFrom(value, out result);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	static bool INumberBase<NFloat>.TryConvertFromTruncating<TOther>(TOther value, out NFloat result)
	{
		return TryConvertFrom(value, out result);
	}

	private static bool TryConvertFrom<TOther>(TOther value, out NFloat result) where TOther : INumberBase<TOther>
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
			result = (NFloat)num;
			return true;
		}
		if (typeof(TOther) == typeof(double))
		{
			double num2 = (double)(object)value;
			result = (NFloat)num2;
			return true;
		}
		if (typeof(TOther) == typeof(Half))
		{
			Half half = (Half)(object)value;
			result = half;
			return true;
		}
		if (typeof(TOther) == typeof(short))
		{
			short num3 = (short)(object)value;
			result = num3;
			return true;
		}
		if (typeof(TOther) == typeof(int))
		{
			int num4 = (int)(object)value;
			result = num4;
			return true;
		}
		if (typeof(TOther) == typeof(long))
		{
			long num5 = (long)(object)value;
			result = num5;
			return true;
		}
		if (typeof(TOther) == typeof(Int128))
		{
			Int128 @int = (Int128)(object)value;
			result = (NFloat)@int;
			return true;
		}
		if (typeof(TOther) == typeof(nint))
		{
			nint num6 = (nint)(object)value;
			result = num6;
			return true;
		}
		if (typeof(TOther) == typeof(sbyte))
		{
			sbyte b2 = (sbyte)(object)value;
			result = b2;
			return true;
		}
		if (typeof(TOther) == typeof(float))
		{
			float num7 = (float)(object)value;
			result = num7;
			return true;
		}
		if (typeof(TOther) == typeof(ushort))
		{
			ushort num8 = (ushort)(object)value;
			result = num8;
			return true;
		}
		if (typeof(TOther) == typeof(uint))
		{
			uint num9 = (uint)(object)value;
			result = num9;
			return true;
		}
		if (typeof(TOther) == typeof(ulong))
		{
			ulong num10 = (ulong)(object)value;
			result = num10;
			return true;
		}
		if (typeof(TOther) == typeof(UInt128))
		{
			UInt128 uInt = (UInt128)(object)value;
			result = (NFloat)uInt;
			return true;
		}
		if (typeof(TOther) == typeof(nuint))
		{
			nuint num11 = (nuint)(object)value;
			result = num11;
			return true;
		}
		result = default(NFloat);
		return false;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	static bool INumberBase<NFloat>.TryConvertToChecked<TOther>(NFloat value, [MaybeNullWhen(false)] out TOther result)
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
		}
		if (typeof(TOther) == typeof(decimal))
		{
			decimal num = (decimal)value;
			result = (TOther)(object)num;
			return true;
		}
		if (typeof(TOther) == typeof(double))
		{
			double num2 = value;
			result = (TOther)(object)num2;
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
				short num3 = (short)value;
				result = (TOther)(object)num3;
				return true;
			}
			if (typeof(TOther) == typeof(int))
			{
				int num4 = (int)value;
				result = (TOther)(object)num4;
				return true;
			}
			if (typeof(TOther) == typeof(long))
			{
				long num5 = (long)value;
				result = (TOther)(object)num5;
				return true;
			}
			if (typeof(TOther) == typeof(Int128))
			{
				Int128 @int = (Int128)value;
				result = (TOther)(object)@int;
				return true;
			}
			if (typeof(TOther) == typeof(nint))
			{
				nint num6 = (nint)value;
				result = (TOther)(object)num6;
				return true;
			}
			if (typeof(TOther) == typeof(sbyte))
			{
				sbyte b2 = (sbyte)value;
				result = (TOther)(object)b2;
				return true;
			}
			if (typeof(TOther) == typeof(float))
			{
				float num7 = unchecked((float)value);
				result = (TOther)(object)num7;
				return true;
			}
			if (typeof(TOther) == typeof(ushort))
			{
				ushort num8 = (ushort)value;
				result = (TOther)(object)num8;
				return true;
			}
			if (typeof(TOther) == typeof(uint))
			{
				uint num9 = (uint)value;
				result = (TOther)(object)num9;
				return true;
			}
			if (typeof(TOther) == typeof(ulong))
			{
				ulong num10 = (ulong)value;
				result = (TOther)(object)num10;
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
				nuint num11 = (nuint)value;
				result = (TOther)(object)num11;
				return true;
			}
			result = default(TOther);
			return false;
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	static bool INumberBase<NFloat>.TryConvertToSaturating<TOther>(NFloat value, [MaybeNullWhen(false)] out TOther result)
	{
		return TryConvertTo<TOther>(value, out result);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	static bool INumberBase<NFloat>.TryConvertToTruncating<TOther>(NFloat value, [MaybeNullWhen(false)] out TOther result)
	{
		return TryConvertTo<TOther>(value, out result);
	}

	private static bool TryConvertTo<TOther>(NFloat value, [MaybeNullWhen(false)] out TOther result) where TOther : INumberBase<TOther>
	{
		if (typeof(TOther) == typeof(byte))
		{
			byte b = (byte)((value >= byte.MaxValue) ? byte.MaxValue : ((!(value <= (byte)0)) ? ((byte)value) : 0));
			result = (TOther)(object)b;
			return true;
		}
		if (typeof(TOther) == typeof(char))
		{
			char c = ((value >= '\uffff') ? '\uffff' : ((!(value <= '\0')) ? ((char)value) : '\0'));
			result = (TOther)(object)c;
			return true;
		}
		if (typeof(TOther) == typeof(decimal))
		{
			decimal num = ((value >= 7.9228163E+28f) ? decimal.MaxValue : ((value <= -7.9228163E+28f) ? decimal.MinValue : (IsNaN(value) ? 0.0m : ((decimal)value))));
			result = (TOther)(object)num;
			return true;
		}
		if (typeof(TOther) == typeof(double))
		{
			double num2 = value;
			result = (TOther)(object)num2;
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
			short num3 = ((value >= short.MaxValue) ? short.MaxValue : ((value <= short.MinValue) ? short.MinValue : ((short)value)));
			result = (TOther)(object)num3;
			return true;
		}
		if (typeof(TOther) == typeof(int))
		{
			int num4 = ((value >= int.MaxValue) ? int.MaxValue : ((value <= int.MinValue) ? int.MinValue : ((int)value)));
			result = (TOther)(object)num4;
			return true;
		}
		if (typeof(TOther) == typeof(long))
		{
			long num5 = ((value >= long.MaxValue) ? long.MaxValue : ((value <= long.MinValue) ? long.MinValue : ((long)value)));
			result = (TOther)(object)num5;
			return true;
		}
		if (typeof(TOther) == typeof(Int128))
		{
			Int128 @int = (((double)value >= 1.7014118346046923E+38) ? Int128.MaxValue : (((double)value <= -1.7014118346046923E+38) ? Int128.MinValue : ((Int128)value)));
			result = (TOther)(object)@int;
			return true;
		}
		if (typeof(TOther) == typeof(nint))
		{
			nint num6 = ((value >= IntPtr.MaxValue) ? IntPtr.MaxValue : ((value <= IntPtr.MinValue) ? IntPtr.MinValue : ((nint)value)));
			result = (TOther)(object)num6;
			return true;
		}
		if (typeof(TOther) == typeof(sbyte))
		{
			sbyte b2 = ((value >= sbyte.MaxValue) ? sbyte.MaxValue : ((value <= sbyte.MinValue) ? sbyte.MinValue : ((sbyte)value)));
			result = (TOther)(object)b2;
			return true;
		}
		if (typeof(TOther) == typeof(float))
		{
			float num7 = (float)value;
			result = (TOther)(object)num7;
			return true;
		}
		if (typeof(TOther) == typeof(ushort))
		{
			ushort num8 = (ushort)((value >= ushort.MaxValue) ? ushort.MaxValue : ((!(value <= (ushort)0)) ? ((ushort)value) : 0));
			result = (TOther)(object)num8;
			return true;
		}
		if (typeof(TOther) == typeof(uint))
		{
			uint num9 = ((value >= uint.MaxValue) ? uint.MaxValue : ((!(value <= 0u)) ? ((uint)value) : 0u));
			result = (TOther)(object)num9;
			return true;
		}
		if (typeof(TOther) == typeof(ulong))
		{
			ulong num10 = ((value >= ulong.MaxValue) ? ulong.MaxValue : ((value <= 0uL) ? 0 : (IsNaN(value) ? 0 : ((ulong)value))));
			result = (TOther)(object)num10;
			return true;
		}
		if (typeof(TOther) == typeof(UInt128))
		{
			UInt128 uInt = (((double)value >= 3.402823669209385E+38) ? UInt128.MaxValue : (((double)value <= 0.0) ? UInt128.MinValue : ((UInt128)value)));
			result = (TOther)(object)uInt;
			return true;
		}
		if (typeof(TOther) == typeof(nuint))
		{
			nuint num11 = ((value >= ulong.MaxValue) ? unchecked((nuint)(-1)) : ((value <= 0uL) ? 0 : ((nuint)value)));
			result = (TOther)(object)num11;
			return true;
		}
		result = default(TOther);
		return false;
	}

	public static bool TryParse([NotNullWhen(true)] string? s, IFormatProvider? provider, out NFloat result)
	{
		return TryParse(s, NumberStyles.Float | NumberStyles.AllowThousands, provider, out result);
	}

	public static NFloat Pow(NFloat x, NFloat y)
	{
		return new NFloat(double.Pow(x._value, y._value));
	}

	public static NFloat Cbrt(NFloat x)
	{
		return new NFloat(double.Cbrt(x._value));
	}

	public static NFloat Hypot(NFloat x, NFloat y)
	{
		return new NFloat(double.Hypot(x._value, y._value));
	}

	public static NFloat RootN(NFloat x, int n)
	{
		return new NFloat(double.RootN(x._value, n));
	}

	public static NFloat Sqrt(NFloat x)
	{
		return new NFloat(double.Sqrt(x._value));
	}

	public static NFloat Parse(ReadOnlySpan<char> s, IFormatProvider? provider)
	{
		return Parse(s, NumberStyles.Float | NumberStyles.AllowThousands, provider);
	}

	public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, out NFloat result)
	{
		return TryParse(s, NumberStyles.Float | NumberStyles.AllowThousands, provider, out result);
	}

	public static NFloat Acos(NFloat x)
	{
		return new NFloat(double.Acos(x._value));
	}

	public static NFloat AcosPi(NFloat x)
	{
		return new NFloat(double.AcosPi(x._value));
	}

	public static NFloat Asin(NFloat x)
	{
		return new NFloat(double.Asin(x._value));
	}

	public static NFloat AsinPi(NFloat x)
	{
		return new NFloat(double.AsinPi(x._value));
	}

	public static NFloat Atan(NFloat x)
	{
		return new NFloat(double.Atan(x._value));
	}

	public static NFloat AtanPi(NFloat x)
	{
		return new NFloat(double.AtanPi(x._value));
	}

	public static NFloat Cos(NFloat x)
	{
		return new NFloat(double.Cos(x._value));
	}

	public static NFloat CosPi(NFloat x)
	{
		return new NFloat(double.CosPi(x._value));
	}

	public static NFloat DegreesToRadians(NFloat degrees)
	{
		return new NFloat(double.DegreesToRadians(degrees._value));
	}

	public static NFloat RadiansToDegrees(NFloat radians)
	{
		return new NFloat(double.RadiansToDegrees(radians._value));
	}

	public static NFloat Sin(NFloat x)
	{
		return new NFloat(double.Sin(x._value));
	}

	public static (NFloat Sin, NFloat Cos) SinCos(NFloat x)
	{
		var (value, value2) = double.SinCos(x._value);
		return (Sin: new NFloat(value), Cos: new NFloat(value2));
	}

	public static (NFloat SinPi, NFloat CosPi) SinCosPi(NFloat x)
	{
		var (value, value2) = double.SinCosPi(x._value);
		return (SinPi: new NFloat(value), CosPi: new NFloat(value2));
	}

	public static NFloat SinPi(NFloat x)
	{
		return new NFloat(double.SinPi(x._value));
	}

	public static NFloat Tan(NFloat x)
	{
		return new NFloat(double.Tan(x._value));
	}

	public static NFloat TanPi(NFloat x)
	{
		return new NFloat(double.TanPi(x._value));
	}

	public static NFloat Parse(ReadOnlySpan<byte> utf8Text, NumberStyles style = NumberStyles.Float | NumberStyles.AllowThousands, IFormatProvider? provider = null)
	{
		double value = double.Parse(utf8Text, style, provider);
		return new NFloat(value);
	}

	public static bool TryParse(ReadOnlySpan<byte> utf8Text, NumberStyles style, IFormatProvider? provider, out NFloat result)
	{
		Unsafe.SkipInit<NFloat>(out result);
		return double.TryParse(utf8Text, style, provider, out Unsafe.As<NFloat, double>(ref result));
	}

	public static NFloat Parse(ReadOnlySpan<byte> utf8Text, IFormatProvider? provider)
	{
		return Parse(utf8Text, NumberStyles.Float | NumberStyles.AllowThousands, provider);
	}

	public static bool TryParse(ReadOnlySpan<byte> utf8Text, IFormatProvider? provider, out NFloat result)
	{
		return TryParse(utf8Text, NumberStyles.Float | NumberStyles.AllowThousands, provider, out result);
	}
}
