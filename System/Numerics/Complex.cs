using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System.Numerics;

[Serializable]
[TypeForwardedFrom("System.Numerics, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
public readonly struct Complex : IEquatable<Complex>, IFormattable, INumberBase<Complex>, IParsable<Complex>, ISpanFormattable, ISpanParsable<Complex>, IAdditionOperators<Complex, Complex, Complex>, IAdditiveIdentity<Complex, Complex>, IDecrementOperators<Complex>, IDivisionOperators<Complex, Complex, Complex>, IEqualityOperators<Complex, Complex, bool>, IIncrementOperators<Complex>, IMultiplicativeIdentity<Complex, Complex>, IMultiplyOperators<Complex, Complex, Complex>, ISubtractionOperators<Complex, Complex, Complex>, IUnaryNegationOperators<Complex, Complex>, IUnaryPlusOperators<Complex, Complex>, IUtf8SpanFormattable, IUtf8SpanParsable<Complex>, ISignedNumber<Complex>
{
	public static readonly Complex Zero = new Complex(0.0, 0.0);

	public static readonly Complex One = new Complex(1.0, 0.0);

	public static readonly Complex ImaginaryOne = new Complex(0.0, 1.0);

	public static readonly Complex NaN = new Complex(double.NaN, double.NaN);

	public static readonly Complex Infinity = new Complex(double.PositiveInfinity, double.PositiveInfinity);

	private static readonly double s_sqrtRescaleThreshold = double.MaxValue / (Math.Sqrt(2.0) + 1.0);

	private static readonly double s_asinOverflowThreshold = Math.Sqrt(double.MaxValue) / 2.0;

	private static readonly double s_log2 = Math.Log(2.0);

	private readonly double m_real;

	private readonly double m_imaginary;

	public double Real => m_real;

	public double Imaginary => m_imaginary;

	public double Magnitude => Abs(this);

	public double Phase => Math.Atan2(m_imaginary, m_real);

	static Complex IAdditiveIdentity<Complex, Complex>.AdditiveIdentity => new Complex(0.0, 0.0);

	static Complex IMultiplicativeIdentity<Complex, Complex>.MultiplicativeIdentity => new Complex(1.0, 0.0);

	static Complex INumberBase<Complex>.One => new Complex(1.0, 0.0);

	static int INumberBase<Complex>.Radix => 2;

	static Complex INumberBase<Complex>.Zero => new Complex(0.0, 0.0);

	static Complex ISignedNumber<Complex>.NegativeOne => new Complex(-1.0, 0.0);

	public Complex(double real, double imaginary)
	{
		m_real = real;
		m_imaginary = imaginary;
	}

	public static Complex FromPolarCoordinates(double magnitude, double phase)
	{
		return new Complex(magnitude * Math.Cos(phase), magnitude * Math.Sin(phase));
	}

	public static Complex Negate(Complex value)
	{
		return -value;
	}

	public static Complex Add(Complex left, Complex right)
	{
		return left + right;
	}

	public static Complex Add(Complex left, double right)
	{
		return left + right;
	}

	public static Complex Add(double left, Complex right)
	{
		return left + right;
	}

	public static Complex Subtract(Complex left, Complex right)
	{
		return left - right;
	}

	public static Complex Subtract(Complex left, double right)
	{
		return left - right;
	}

	public static Complex Subtract(double left, Complex right)
	{
		return left - right;
	}

	public static Complex Multiply(Complex left, Complex right)
	{
		return left * right;
	}

	public static Complex Multiply(Complex left, double right)
	{
		return left * right;
	}

	public static Complex Multiply(double left, Complex right)
	{
		return left * right;
	}

	public static Complex Divide(Complex dividend, Complex divisor)
	{
		return dividend / divisor;
	}

	public static Complex Divide(Complex dividend, double divisor)
	{
		return dividend / divisor;
	}

	public static Complex Divide(double dividend, Complex divisor)
	{
		return dividend / divisor;
	}

	public static Complex operator -(Complex value)
	{
		return new Complex(0.0 - value.m_real, 0.0 - value.m_imaginary);
	}

	public static Complex operator +(Complex left, Complex right)
	{
		return new Complex(left.m_real + right.m_real, left.m_imaginary + right.m_imaginary);
	}

	public static Complex operator +(Complex left, double right)
	{
		return new Complex(left.m_real + right, left.m_imaginary);
	}

	public static Complex operator +(double left, Complex right)
	{
		return new Complex(left + right.m_real, right.m_imaginary);
	}

	public static Complex operator -(Complex left, Complex right)
	{
		return new Complex(left.m_real - right.m_real, left.m_imaginary - right.m_imaginary);
	}

	public static Complex operator -(Complex left, double right)
	{
		return new Complex(left.m_real - right, left.m_imaginary);
	}

	public static Complex operator -(double left, Complex right)
	{
		return new Complex(left - right.m_real, 0.0 - right.m_imaginary);
	}

	public static Complex operator *(Complex left, Complex right)
	{
		double real = left.m_real * right.m_real - left.m_imaginary * right.m_imaginary;
		double imaginary = left.m_imaginary * right.m_real + left.m_real * right.m_imaginary;
		return new Complex(real, imaginary);
	}

	public static Complex operator *(Complex left, double right)
	{
		if (!double.IsFinite(left.m_real))
		{
			if (!double.IsFinite(left.m_imaginary))
			{
				return new Complex(double.NaN, double.NaN);
			}
			return new Complex(left.m_real * right, double.NaN);
		}
		if (!double.IsFinite(left.m_imaginary))
		{
			return new Complex(double.NaN, left.m_imaginary * right);
		}
		return new Complex(left.m_real * right, left.m_imaginary * right);
	}

	public static Complex operator *(double left, Complex right)
	{
		if (!double.IsFinite(right.m_real))
		{
			if (!double.IsFinite(right.m_imaginary))
			{
				return new Complex(double.NaN, double.NaN);
			}
			return new Complex(left * right.m_real, double.NaN);
		}
		if (!double.IsFinite(right.m_imaginary))
		{
			return new Complex(double.NaN, left * right.m_imaginary);
		}
		return new Complex(left * right.m_real, left * right.m_imaginary);
	}

	public static Complex operator /(Complex left, Complex right)
	{
		double real = left.m_real;
		double imaginary = left.m_imaginary;
		double real2 = right.m_real;
		double imaginary2 = right.m_imaginary;
		if (Math.Abs(imaginary2) < Math.Abs(real2))
		{
			double num = imaginary2 / real2;
			return new Complex((real + imaginary * num) / (real2 + imaginary2 * num), (imaginary - real * num) / (real2 + imaginary2 * num));
		}
		double num2 = real2 / imaginary2;
		return new Complex((imaginary + real * num2) / (imaginary2 + real2 * num2), (0.0 - real + imaginary * num2) / (imaginary2 + real2 * num2));
	}

	public static Complex operator /(Complex left, double right)
	{
		if (right == 0.0)
		{
			return new Complex(double.NaN, double.NaN);
		}
		if (!double.IsFinite(left.m_real))
		{
			if (!double.IsFinite(left.m_imaginary))
			{
				return new Complex(double.NaN, double.NaN);
			}
			return new Complex(left.m_real / right, double.NaN);
		}
		if (!double.IsFinite(left.m_imaginary))
		{
			return new Complex(double.NaN, left.m_imaginary / right);
		}
		return new Complex(left.m_real / right, left.m_imaginary / right);
	}

	public static Complex operator /(double left, Complex right)
	{
		double real = right.m_real;
		double imaginary = right.m_imaginary;
		if (Math.Abs(imaginary) < Math.Abs(real))
		{
			double num = imaginary / real;
			return new Complex(left / (real + imaginary * num), (0.0 - left) * num / (real + imaginary * num));
		}
		double num2 = real / imaginary;
		return new Complex(left * num2 / (imaginary + real * num2), (0.0 - left) / (imaginary + real * num2));
	}

	public static double Abs(Complex value)
	{
		return Hypot(value.m_real, value.m_imaginary);
	}

	private static double Hypot(double a, double b)
	{
		a = Math.Abs(a);
		b = Math.Abs(b);
		double num;
		double num2;
		if (a < b)
		{
			num = a;
			num2 = b;
		}
		else
		{
			num = b;
			num2 = a;
		}
		if (num == 0.0)
		{
			return num2;
		}
		if (double.IsPositiveInfinity(num2) && !double.IsNaN(num))
		{
			return double.PositiveInfinity;
		}
		double num3 = num / num2;
		return num2 * Math.Sqrt(1.0 + num3 * num3);
	}

	private static double Log1P(double x)
	{
		double num = 1.0 + x;
		if (num == 1.0)
		{
			return x;
		}
		if (x < 0.75)
		{
			return x * Math.Log(num) / (num - 1.0);
		}
		return Math.Log(num);
	}

	public static Complex Conjugate(Complex value)
	{
		return new Complex(value.m_real, 0.0 - value.m_imaginary);
	}

	public static Complex Reciprocal(Complex value)
	{
		if (value.m_real == 0.0 && value.m_imaginary == 0.0)
		{
			return Zero;
		}
		return One / value;
	}

	public static bool operator ==(Complex left, Complex right)
	{
		if (left.m_real == right.m_real)
		{
			return left.m_imaginary == right.m_imaginary;
		}
		return false;
	}

	public static bool operator !=(Complex left, Complex right)
	{
		if (left.m_real == right.m_real)
		{
			return left.m_imaginary != right.m_imaginary;
		}
		return true;
	}

	public override bool Equals([NotNullWhen(true)] object? obj)
	{
		if (obj is Complex value)
		{
			return Equals(value);
		}
		return false;
	}

	public bool Equals(Complex value)
	{
		if (m_real.Equals(value.m_real))
		{
			return m_imaginary.Equals(value.m_imaginary);
		}
		return false;
	}

	public override int GetHashCode()
	{
		return HashCode.Combine(m_real, m_imaginary);
	}

	public override string ToString()
	{
		return ToString(null, null);
	}

	public string ToString([StringSyntax("NumericFormat")] string? format)
	{
		return ToString(format, null);
	}

	public string ToString(IFormatProvider? provider)
	{
		return ToString(null, provider);
	}

	public string ToString([StringSyntax("NumericFormat")] string? format, IFormatProvider? provider)
	{
		Span<char> initialBuffer = stackalloc char[512];
		DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(4, 2, provider, initialBuffer);
		defaultInterpolatedStringHandler.AppendLiteral("<");
		defaultInterpolatedStringHandler.AppendFormatted(m_real, format);
		defaultInterpolatedStringHandler.AppendLiteral("; ");
		defaultInterpolatedStringHandler.AppendFormatted(m_imaginary, format);
		defaultInterpolatedStringHandler.AppendLiteral(">");
		return defaultInterpolatedStringHandler.ToStringAndClear();
	}

	public static Complex Sin(Complex value)
	{
		double num = Math.Exp(value.m_imaginary);
		double num2 = 1.0 / num;
		double num3 = (num - num2) * 0.5;
		double num4 = (num + num2) * 0.5;
		return new Complex(Math.Sin(value.m_real) * num4, Math.Cos(value.m_real) * num3);
	}

	public static Complex Sinh(Complex value)
	{
		Complex complex = Sin(new Complex(0.0 - value.m_imaginary, value.m_real));
		return new Complex(complex.m_imaginary, 0.0 - complex.m_real);
	}

	public static Complex Asin(Complex value)
	{
		Asin_Internal(Math.Abs(value.Real), Math.Abs(value.Imaginary), out var b, out var bPrime, out var v);
		double num = ((!(bPrime < 0.0)) ? Math.Atan(bPrime) : Math.Asin(b));
		if (value.Real < 0.0)
		{
			num = 0.0 - num;
		}
		if (value.Imaginary < 0.0)
		{
			v = 0.0 - v;
		}
		return new Complex(num, v);
	}

	public static Complex Cos(Complex value)
	{
		double num = Math.Exp(value.m_imaginary);
		double num2 = 1.0 / num;
		double num3 = (num - num2) * 0.5;
		double num4 = (num + num2) * 0.5;
		return new Complex(Math.Cos(value.m_real) * num4, (0.0 - Math.Sin(value.m_real)) * num3);
	}

	public static Complex Cosh(Complex value)
	{
		return Cos(new Complex(0.0 - value.m_imaginary, value.m_real));
	}

	public static Complex Acos(Complex value)
	{
		Asin_Internal(Math.Abs(value.Real), Math.Abs(value.Imaginary), out var b, out var bPrime, out var v);
		double num = ((!(bPrime < 0.0)) ? Math.Atan(1.0 / bPrime) : Math.Acos(b));
		if (value.Real < 0.0)
		{
			num = Math.PI - num;
		}
		if (value.Imaginary > 0.0)
		{
			v = 0.0 - v;
		}
		return new Complex(num, v);
	}

	public static Complex Tan(Complex value)
	{
		double num = 2.0 * value.m_real;
		double num2 = 2.0 * value.m_imaginary;
		double num3 = Math.Exp(num2);
		double num4 = 1.0 / num3;
		double num5 = (num3 + num4) * 0.5;
		if (Math.Abs(value.m_imaginary) <= 4.0)
		{
			double num6 = (num3 - num4) * 0.5;
			double num7 = Math.Cos(num) + num5;
			return new Complex(Math.Sin(num) / num7, num6 / num7);
		}
		double num8 = 1.0 + Math.Cos(num) / num5;
		return new Complex(Math.Sin(num) / num5 / num8, Math.Tanh(num2) / num8);
	}

	public static Complex Tanh(Complex value)
	{
		Complex complex = Tan(new Complex(0.0 - value.m_imaginary, value.m_real));
		return new Complex(complex.m_imaginary, 0.0 - complex.m_real);
	}

	public static Complex Atan(Complex value)
	{
		Complex complex = new Complex(2.0, 0.0);
		return ImaginaryOne / complex * (Log(One - ImaginaryOne * value) - Log(One + ImaginaryOne * value));
	}

	private static void Asin_Internal(double x, double y, out double b, out double bPrime, out double v)
	{
		if (x > s_asinOverflowThreshold || y > s_asinOverflowThreshold)
		{
			b = -1.0;
			bPrime = x / y;
			double num;
			double num2;
			if (x < y)
			{
				num = x;
				num2 = y;
			}
			else
			{
				num = y;
				num2 = x;
			}
			double num3 = num / num2;
			v = s_log2 + Math.Log(num2) + 0.5 * Log1P(num3 * num3);
			return;
		}
		double num4 = Hypot(x + 1.0, y);
		double num5 = Hypot(x - 1.0, y);
		double num6 = (num4 + num5) * 0.5;
		b = x / num6;
		if (b > 0.75)
		{
			if (x <= 1.0)
			{
				double num7 = (y * y / (num4 + (x + 1.0)) + (num5 + (1.0 - x))) * 0.5;
				bPrime = x / Math.Sqrt((num6 + x) * num7);
			}
			else
			{
				double num8 = (1.0 / (num4 + (x + 1.0)) + 1.0 / (num5 + (x - 1.0))) * 0.5;
				bPrime = x / y / Math.Sqrt((num6 + x) * num8);
			}
		}
		else
		{
			bPrime = -1.0;
		}
		if (num6 < 1.5)
		{
			if (x < 1.0)
			{
				double num9 = (1.0 / (num4 + (x + 1.0)) + 1.0 / (num5 + (1.0 - x))) * 0.5;
				double num10 = y * y * num9;
				v = Log1P(num10 + y * Math.Sqrt(num9 * (num6 + 1.0)));
			}
			else
			{
				double num11 = (y * y / (num4 + (x + 1.0)) + (num5 + (x - 1.0))) * 0.5;
				v = Log1P(num11 + Math.Sqrt(num11 * (num6 + 1.0)));
			}
		}
		else
		{
			v = Math.Log(num6 + Math.Sqrt((num6 - 1.0) * (num6 + 1.0)));
		}
	}

	public static bool IsFinite(Complex value)
	{
		if (double.IsFinite(value.m_real))
		{
			return double.IsFinite(value.m_imaginary);
		}
		return false;
	}

	public static bool IsInfinity(Complex value)
	{
		if (!double.IsInfinity(value.m_real))
		{
			return double.IsInfinity(value.m_imaginary);
		}
		return true;
	}

	public static bool IsNaN(Complex value)
	{
		if (!IsInfinity(value))
		{
			return !IsFinite(value);
		}
		return false;
	}

	public static Complex Log(Complex value)
	{
		return new Complex(Math.Log(Abs(value)), Math.Atan2(value.m_imaginary, value.m_real));
	}

	public static Complex Log(Complex value, double baseValue)
	{
		return Log(value) / Log(baseValue);
	}

	public static Complex Log10(Complex value)
	{
		Complex value2 = Log(value);
		return Scale(value2, 0.43429448190325);
	}

	public static Complex Exp(Complex value)
	{
		double num = Math.Exp(value.m_real);
		double real = num * Math.Cos(value.m_imaginary);
		double imaginary = num * Math.Sin(value.m_imaginary);
		return new Complex(real, imaginary);
	}

	public static Complex Sqrt(Complex value)
	{
		if (value.m_imaginary == 0.0)
		{
			if (value.m_real < 0.0)
			{
				return new Complex(0.0, Math.Sqrt(0.0 - value.m_real));
			}
			return new Complex(Math.Sqrt(value.m_real), 0.0);
		}
		bool flag = false;
		double num = value.m_real;
		double num2 = value.m_imaginary;
		if (Math.Abs(num) >= s_sqrtRescaleThreshold || Math.Abs(num2) >= s_sqrtRescaleThreshold)
		{
			if (double.IsInfinity(value.m_imaginary) && !double.IsNaN(value.m_real))
			{
				return new Complex(double.PositiveInfinity, num2);
			}
			num *= 0.25;
			num2 *= 0.25;
			flag = true;
		}
		double num3;
		double num4;
		if (num >= 0.0)
		{
			num3 = Math.Sqrt((Hypot(num, num2) + num) * 0.5);
			num4 = num2 / (2.0 * num3);
		}
		else
		{
			num4 = Math.Sqrt((Hypot(num, num2) - num) * 0.5);
			if (num2 < 0.0)
			{
				num4 = 0.0 - num4;
			}
			num3 = num2 / (2.0 * num4);
		}
		if (flag)
		{
			num3 *= 2.0;
			num4 *= 2.0;
		}
		return new Complex(num3, num4);
	}

	public static Complex Pow(Complex value, Complex power)
	{
		if (power == Zero)
		{
			return One;
		}
		if (value == Zero)
		{
			return Zero;
		}
		double real = value.m_real;
		double imaginary = value.m_imaginary;
		double real2 = power.m_real;
		double imaginary2 = power.m_imaginary;
		double num = Abs(value);
		double num2 = Math.Atan2(imaginary, real);
		double num3 = real2 * num2 + imaginary2 * Math.Log(num);
		double num4 = Math.Pow(num, real2) * Math.Pow(Math.E, (0.0 - imaginary2) * num2);
		return new Complex(num4 * Math.Cos(num3), num4 * Math.Sin(num3));
	}

	public static Complex Pow(Complex value, double power)
	{
		return Pow(value, new Complex(power, 0.0));
	}

	private static Complex Scale(Complex value, double factor)
	{
		double real = factor * value.m_real;
		double imaginary = factor * value.m_imaginary;
		return new Complex(real, imaginary);
	}

	public static explicit operator Complex(decimal value)
	{
		return new Complex((double)value, 0.0);
	}

	public static explicit operator Complex(Int128 value)
	{
		return new Complex((double)value, 0.0);
	}

	public static explicit operator Complex(BigInteger value)
	{
		return new Complex((double)value, 0.0);
	}

	[CLSCompliant(false)]
	public static explicit operator Complex(UInt128 value)
	{
		return new Complex((double)value, 0.0);
	}

	public static implicit operator Complex(byte value)
	{
		return new Complex((int)value, 0.0);
	}

	public static implicit operator Complex(char value)
	{
		return new Complex((int)value, 0.0);
	}

	public static implicit operator Complex(double value)
	{
		return new Complex(value, 0.0);
	}

	public static implicit operator Complex(Half value)
	{
		return new Complex((double)value, 0.0);
	}

	public static implicit operator Complex(short value)
	{
		return new Complex(value, 0.0);
	}

	public static implicit operator Complex(int value)
	{
		return new Complex(value, 0.0);
	}

	public static implicit operator Complex(long value)
	{
		return new Complex(value, 0.0);
	}

	public static implicit operator Complex(nint value)
	{
		return new Complex(value, 0.0);
	}

	[CLSCompliant(false)]
	public static implicit operator Complex(sbyte value)
	{
		return new Complex(value, 0.0);
	}

	public static implicit operator Complex(float value)
	{
		return new Complex(value, 0.0);
	}

	[CLSCompliant(false)]
	public static implicit operator Complex(ushort value)
	{
		return new Complex((int)value, 0.0);
	}

	[CLSCompliant(false)]
	public static implicit operator Complex(uint value)
	{
		return new Complex(value, 0.0);
	}

	[CLSCompliant(false)]
	public static implicit operator Complex(ulong value)
	{
		return new Complex(value, 0.0);
	}

	[CLSCompliant(false)]
	public static implicit operator Complex(nuint value)
	{
		return new Complex(value, 0.0);
	}

	public static Complex operator --(Complex value)
	{
		return value - One;
	}

	public static Complex operator ++(Complex value)
	{
		return value + One;
	}

	static Complex INumberBase<Complex>.Abs(Complex value)
	{
		return Abs(value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Complex CreateChecked<TOther>(TOther value) where TOther : INumberBase<TOther>
	{
		if (typeof(TOther) == typeof(Complex))
		{
			return (Complex)(object)value;
		}
		if (!TryConvertFrom(value, out var result) && !TOther.TryConvertToChecked<Complex>(value, out result))
		{
			System.ThrowHelper.ThrowNotSupportedException();
		}
		return result;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Complex CreateSaturating<TOther>(TOther value) where TOther : INumberBase<TOther>
	{
		if (typeof(TOther) == typeof(Complex))
		{
			return (Complex)(object)value;
		}
		if (!TryConvertFrom(value, out var result) && !TOther.TryConvertToSaturating<Complex>(value, out result))
		{
			System.ThrowHelper.ThrowNotSupportedException();
		}
		return result;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Complex CreateTruncating<TOther>(TOther value) where TOther : INumberBase<TOther>
	{
		if (typeof(TOther) == typeof(Complex))
		{
			return (Complex)(object)value;
		}
		if (!TryConvertFrom(value, out var result) && !TOther.TryConvertToTruncating<Complex>(value, out result))
		{
			System.ThrowHelper.ThrowNotSupportedException();
		}
		return result;
	}

	static bool INumberBase<Complex>.IsCanonical(Complex value)
	{
		return true;
	}

	public static bool IsComplexNumber(Complex value)
	{
		if (value.m_real != 0.0)
		{
			return value.m_imaginary != 0.0;
		}
		return false;
	}

	public static bool IsEvenInteger(Complex value)
	{
		if (value.m_imaginary == 0.0)
		{
			return double.IsEvenInteger(value.m_real);
		}
		return false;
	}

	public static bool IsImaginaryNumber(Complex value)
	{
		if (value.m_real == 0.0)
		{
			return double.IsRealNumber(value.m_imaginary);
		}
		return false;
	}

	public static bool IsInteger(Complex value)
	{
		if (value.m_imaginary == 0.0)
		{
			return double.IsInteger(value.m_real);
		}
		return false;
	}

	public static bool IsNegative(Complex value)
	{
		if (value.m_imaginary == 0.0)
		{
			return double.IsNegative(value.m_real);
		}
		return false;
	}

	public static bool IsNegativeInfinity(Complex value)
	{
		if (value.m_imaginary == 0.0)
		{
			return double.IsNegativeInfinity(value.m_real);
		}
		return false;
	}

	public static bool IsNormal(Complex value)
	{
		if (double.IsNormal(value.m_real))
		{
			if (value.m_imaginary != 0.0)
			{
				return double.IsNormal(value.m_imaginary);
			}
			return true;
		}
		return false;
	}

	public static bool IsOddInteger(Complex value)
	{
		if (value.m_imaginary == 0.0)
		{
			return double.IsOddInteger(value.m_real);
		}
		return false;
	}

	public static bool IsPositive(Complex value)
	{
		if (value.m_imaginary == 0.0)
		{
			return double.IsPositive(value.m_real);
		}
		return false;
	}

	public static bool IsPositiveInfinity(Complex value)
	{
		if (value.m_imaginary == 0.0)
		{
			return double.IsPositiveInfinity(value.m_real);
		}
		return false;
	}

	public static bool IsRealNumber(Complex value)
	{
		if (value.m_imaginary == 0.0)
		{
			return double.IsRealNumber(value.m_real);
		}
		return false;
	}

	public static bool IsSubnormal(Complex value)
	{
		if (!double.IsSubnormal(value.m_real))
		{
			return double.IsSubnormal(value.m_imaginary);
		}
		return true;
	}

	static bool INumberBase<Complex>.IsZero(Complex value)
	{
		if (value.m_real == 0.0)
		{
			return value.m_imaginary == 0.0;
		}
		return false;
	}

	public static Complex MaxMagnitude(Complex x, Complex y)
	{
		double num = Abs(x);
		double num2 = Abs(y);
		if (num > num2 || double.IsNaN(num))
		{
			return x;
		}
		if (num == num2)
		{
			if (double.IsNegative(y.m_real))
			{
				if (double.IsNegative(y.m_imaginary))
				{
					return x;
				}
				if (double.IsNegative(x.m_real))
				{
					return y;
				}
				return x;
			}
			if (double.IsNegative(y.m_imaginary))
			{
				if (double.IsNegative(x.m_real))
				{
					return y;
				}
				return x;
			}
		}
		return y;
	}

	static Complex INumberBase<Complex>.MaxMagnitudeNumber(Complex x, Complex y)
	{
		double num = Abs(x);
		double num2 = Abs(y);
		if (num > num2 || double.IsNaN(num2))
		{
			return x;
		}
		if (num == num2)
		{
			if (double.IsNegative(y.m_real))
			{
				if (double.IsNegative(y.m_imaginary))
				{
					return x;
				}
				if (double.IsNegative(x.m_real))
				{
					return y;
				}
				return x;
			}
			if (double.IsNegative(y.m_imaginary))
			{
				if (double.IsNegative(x.m_real))
				{
					return y;
				}
				return x;
			}
		}
		return y;
	}

	public static Complex MinMagnitude(Complex x, Complex y)
	{
		double num = Abs(x);
		double num2 = Abs(y);
		if (num < num2 || double.IsNaN(num))
		{
			return x;
		}
		if (num == num2)
		{
			if (double.IsNegative(y.m_real))
			{
				if (double.IsNegative(y.m_imaginary))
				{
					return y;
				}
				if (double.IsNegative(x.m_real))
				{
					return x;
				}
				return y;
			}
			if (double.IsNegative(y.m_imaginary))
			{
				if (double.IsNegative(x.m_real))
				{
					return x;
				}
				return y;
			}
			return x;
		}
		return y;
	}

	static Complex INumberBase<Complex>.MinMagnitudeNumber(Complex x, Complex y)
	{
		double num = Abs(x);
		double num2 = Abs(y);
		if (num < num2 || double.IsNaN(num2))
		{
			return x;
		}
		if (num == num2)
		{
			if (double.IsNegative(y.m_real))
			{
				if (double.IsNegative(y.m_imaginary))
				{
					return y;
				}
				if (double.IsNegative(x.m_real))
				{
					return x;
				}
				return y;
			}
			if (double.IsNegative(y.m_imaginary))
			{
				if (double.IsNegative(x.m_real))
				{
					return x;
				}
				return y;
			}
			return x;
		}
		return y;
	}

	public static Complex Parse(ReadOnlySpan<char> s, NumberStyles style, IFormatProvider? provider)
	{
		if (!TryParse(s, style, provider, out var result))
		{
			System.ThrowHelper.ThrowOverflowException();
		}
		return result;
	}

	public static Complex Parse(string s, NumberStyles style, IFormatProvider? provider)
	{
		ArgumentNullException.ThrowIfNull(s, "s");
		return Parse(s.AsSpan(), style, provider);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	static bool INumberBase<Complex>.TryConvertFromChecked<TOther>(TOther value, out Complex result)
	{
		return TryConvertFrom(value, out result);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	static bool INumberBase<Complex>.TryConvertFromSaturating<TOther>(TOther value, out Complex result)
	{
		return TryConvertFrom(value, out result);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	static bool INumberBase<Complex>.TryConvertFromTruncating<TOther>(TOther value, out Complex result)
	{
		return TryConvertFrom(value, out result);
	}

	private static bool TryConvertFrom<TOther>(TOther value, out Complex result) where TOther : INumberBase<TOther>
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
			result = (Complex)num;
			return true;
		}
		if (typeof(TOther) == typeof(double))
		{
			double num2 = (double)(object)value;
			result = num2;
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
			result = (Complex)@int;
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
			result = (Complex)uInt;
			return true;
		}
		if (typeof(TOther) == typeof(nuint))
		{
			nuint num11 = (nuint)(object)value;
			result = num11;
			return true;
		}
		result = default(Complex);
		return false;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	static bool INumberBase<Complex>.TryConvertToChecked<TOther>(Complex value, [MaybeNullWhen(false)] out TOther result)
	{
		if (typeof(TOther) == typeof(byte))
		{
			if (value.m_imaginary != 0.0)
			{
				System.ThrowHelper.ThrowOverflowException();
			}
			byte b = checked((byte)value.m_real);
			result = (TOther)(object)b;
			return true;
		}
		if (typeof(TOther) == typeof(char))
		{
			if (value.m_imaginary != 0.0)
			{
				System.ThrowHelper.ThrowOverflowException();
			}
			char c = (char)checked((ushort)value.m_real);
			result = (TOther)(object)c;
			return true;
		}
		if (typeof(TOther) == typeof(decimal))
		{
			if (value.m_imaginary != 0.0)
			{
				System.ThrowHelper.ThrowOverflowException();
			}
			decimal num = (decimal)value.m_real;
			result = (TOther)(object)num;
			return true;
		}
		if (typeof(TOther) == typeof(double))
		{
			double num2 = ((value.m_imaginary != 0.0) ? double.NaN : value.m_real);
			result = (TOther)(object)num2;
			return true;
		}
		if (typeof(TOther) == typeof(Half))
		{
			Half half = ((value.m_imaginary != 0.0) ? Half.NaN : ((Half)value.m_real));
			result = (TOther)(object)half;
			return true;
		}
		checked
		{
			if (typeof(TOther) == typeof(short))
			{
				if (value.m_imaginary != 0.0)
				{
					System.ThrowHelper.ThrowOverflowException();
				}
				short num3 = (short)value.m_real;
				result = (TOther)(object)num3;
				return true;
			}
			if (typeof(TOther) == typeof(int))
			{
				if (value.m_imaginary != 0.0)
				{
					System.ThrowHelper.ThrowOverflowException();
				}
				int num4 = (int)value.m_real;
				result = (TOther)(object)num4;
				return true;
			}
			if (typeof(TOther) == typeof(long))
			{
				if (value.m_imaginary != 0.0)
				{
					System.ThrowHelper.ThrowOverflowException();
				}
				long num5 = (long)value.m_real;
				result = (TOther)(object)num5;
				return true;
			}
			if (typeof(TOther) == typeof(Int128))
			{
				if (value.m_imaginary != 0.0)
				{
					System.ThrowHelper.ThrowOverflowException();
				}
				Int128 @int = (Int128)value.m_real;
				result = (TOther)(object)@int;
				return true;
			}
			if (typeof(TOther) == typeof(nint))
			{
				if (value.m_imaginary != 0.0)
				{
					System.ThrowHelper.ThrowOverflowException();
				}
				nint num6 = (nint)value.m_real;
				result = (TOther)(object)num6;
				return true;
			}
			if (typeof(TOther) == typeof(BigInteger))
			{
				if (value.m_imaginary != 0.0)
				{
					System.ThrowHelper.ThrowOverflowException();
				}
				BigInteger bigInteger = (BigInteger)value.m_real;
				result = (TOther)(object)bigInteger;
				return true;
			}
			if (typeof(TOther) == typeof(sbyte))
			{
				if (value.m_imaginary != 0.0)
				{
					System.ThrowHelper.ThrowOverflowException();
				}
				sbyte b2 = (sbyte)value.m_real;
				result = (TOther)(object)b2;
				return true;
			}
			if (typeof(TOther) == typeof(float))
			{
				float num7 = ((value.m_imaginary != 0.0) ? float.NaN : ((float)value.m_real));
				result = (TOther)(object)num7;
				return true;
			}
			if (typeof(TOther) == typeof(ushort))
			{
				if (value.m_imaginary != 0.0)
				{
					System.ThrowHelper.ThrowOverflowException();
				}
				ushort num8 = (ushort)value.m_real;
				result = (TOther)(object)num8;
				return true;
			}
			if (typeof(TOther) == typeof(uint))
			{
				if (value.m_imaginary != 0.0)
				{
					System.ThrowHelper.ThrowOverflowException();
				}
				uint num9 = (uint)value.m_real;
				result = (TOther)(object)num9;
				return true;
			}
			if (typeof(TOther) == typeof(ulong))
			{
				if (value.m_imaginary != 0.0)
				{
					System.ThrowHelper.ThrowOverflowException();
				}
				ulong num10 = (ulong)value.m_real;
				result = (TOther)(object)num10;
				return true;
			}
			if (typeof(TOther) == typeof(UInt128))
			{
				if (value.m_imaginary != 0.0)
				{
					System.ThrowHelper.ThrowOverflowException();
				}
				UInt128 uInt = (UInt128)value.m_real;
				result = (TOther)(object)uInt;
				return true;
			}
			if (typeof(TOther) == typeof(nuint))
			{
				if (value.m_imaginary != 0.0)
				{
					System.ThrowHelper.ThrowOverflowException();
				}
				nuint num11 = (nuint)value.m_real;
				result = (TOther)(object)num11;
				return true;
			}
			result = default(TOther);
			return false;
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	static bool INumberBase<Complex>.TryConvertToSaturating<TOther>(Complex value, [MaybeNullWhen(false)] out TOther result)
	{
		if (typeof(TOther) == typeof(byte))
		{
			byte b = (byte)((value.m_real >= 255.0) ? byte.MaxValue : ((!(value.m_real <= 0.0)) ? ((byte)value.m_real) : 0));
			result = (TOther)(object)b;
			return true;
		}
		if (typeof(TOther) == typeof(char))
		{
			char c = ((value.m_real >= 65535.0) ? '\uffff' : ((!(value.m_real <= 0.0)) ? ((char)value.m_real) : '\0'));
			result = (TOther)(object)c;
			return true;
		}
		if (typeof(TOther) == typeof(decimal))
		{
			decimal num = ((value.m_real >= 7.922816251426434E+28) ? decimal.MaxValue : ((value.m_real <= -7.922816251426434E+28) ? decimal.MinValue : ((decimal)value.m_real)));
			result = (TOther)(object)num;
			return true;
		}
		if (typeof(TOther) == typeof(double))
		{
			double real = value.m_real;
			result = (TOther)(object)real;
			return true;
		}
		if (typeof(TOther) == typeof(Half))
		{
			Half half = (Half)value.m_real;
			result = (TOther)(object)half;
			return true;
		}
		if (typeof(TOther) == typeof(short))
		{
			short num2 = ((value.m_real >= 32767.0) ? short.MaxValue : ((value.m_real <= -32768.0) ? short.MinValue : ((short)value.m_real)));
			result = (TOther)(object)num2;
			return true;
		}
		if (typeof(TOther) == typeof(int))
		{
			int num3 = ((value.m_real >= 2147483647.0) ? int.MaxValue : ((value.m_real <= -2147483648.0) ? int.MinValue : ((int)value.m_real)));
			result = (TOther)(object)num3;
			return true;
		}
		if (typeof(TOther) == typeof(long))
		{
			long num4 = ((value.m_real >= 9.223372036854776E+18) ? long.MaxValue : ((value.m_real <= -9.223372036854776E+18) ? long.MinValue : ((long)value.m_real)));
			result = (TOther)(object)num4;
			return true;
		}
		if (typeof(TOther) == typeof(Int128))
		{
			Int128 @int = ((value.m_real >= 1.7014118346046923E+38) ? Int128.MaxValue : ((value.m_real <= -1.7014118346046923E+38) ? Int128.MinValue : ((Int128)value.m_real)));
			result = (TOther)(object)@int;
			return true;
		}
		if (typeof(TOther) == typeof(nint))
		{
			nint num5 = ((value.m_real >= (double)IntPtr.MaxValue) ? IntPtr.MaxValue : ((value.m_real <= (double)IntPtr.MinValue) ? IntPtr.MinValue : ((nint)value.m_real)));
			result = (TOther)(object)num5;
			return true;
		}
		if (typeof(TOther) == typeof(BigInteger))
		{
			BigInteger bigInteger = (BigInteger)value.m_real;
			result = (TOther)(object)bigInteger;
			return true;
		}
		if (typeof(TOther) == typeof(sbyte))
		{
			sbyte b2 = ((value.m_real >= 127.0) ? sbyte.MaxValue : ((value.m_real <= -128.0) ? sbyte.MinValue : ((sbyte)value.m_real)));
			result = (TOther)(object)b2;
			return true;
		}
		if (typeof(TOther) == typeof(float))
		{
			float num6 = (float)value.m_real;
			result = (TOther)(object)num6;
			return true;
		}
		if (typeof(TOther) == typeof(ushort))
		{
			ushort num7 = (ushort)((value.m_real >= 65535.0) ? ushort.MaxValue : ((!(value.m_real <= 0.0)) ? ((ushort)value.m_real) : 0));
			result = (TOther)(object)num7;
			return true;
		}
		if (typeof(TOther) == typeof(uint))
		{
			uint num8 = ((value.m_real >= 4294967295.0) ? uint.MaxValue : ((!(value.m_real <= 0.0)) ? ((uint)value.m_real) : 0u));
			result = (TOther)(object)num8;
			return true;
		}
		if (typeof(TOther) == typeof(ulong))
		{
			ulong num9 = ((value.m_real >= 1.8446744073709552E+19) ? ulong.MaxValue : ((value.m_real <= 0.0) ? 0 : ((ulong)value.m_real)));
			result = (TOther)(object)num9;
			return true;
		}
		if (typeof(TOther) == typeof(UInt128))
		{
			UInt128 uInt = ((value.m_real >= 3.402823669209385E+38) ? UInt128.MaxValue : ((value.m_real <= 0.0) ? UInt128.MinValue : ((UInt128)value.m_real)));
			result = (TOther)(object)uInt;
			return true;
		}
		if (typeof(TOther) == typeof(nuint))
		{
			nuint num10 = ((value.m_real >= (double)UIntPtr.MaxValue) ? UIntPtr.MaxValue : ((value.m_real <= (double)UIntPtr.MinValue) ? UIntPtr.MinValue : ((nuint)value.m_real)));
			result = (TOther)(object)num10;
			return true;
		}
		result = default(TOther);
		return false;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	static bool INumberBase<Complex>.TryConvertToTruncating<TOther>(Complex value, [MaybeNullWhen(false)] out TOther result)
	{
		if (typeof(TOther) == typeof(byte))
		{
			byte b = (byte)((value.m_real >= 255.0) ? byte.MaxValue : ((!(value.m_real <= 0.0)) ? ((byte)value.m_real) : 0));
			result = (TOther)(object)b;
			return true;
		}
		if (typeof(TOther) == typeof(char))
		{
			char c = ((value.m_real >= 65535.0) ? '\uffff' : ((!(value.m_real <= 0.0)) ? ((char)value.m_real) : '\0'));
			result = (TOther)(object)c;
			return true;
		}
		if (typeof(TOther) == typeof(decimal))
		{
			decimal num = ((value.m_real >= 7.922816251426434E+28) ? decimal.MaxValue : ((value.m_real <= -7.922816251426434E+28) ? decimal.MinValue : ((decimal)value.m_real)));
			result = (TOther)(object)num;
			return true;
		}
		if (typeof(TOther) == typeof(double))
		{
			double real = value.m_real;
			result = (TOther)(object)real;
			return true;
		}
		if (typeof(TOther) == typeof(Half))
		{
			Half half = (Half)value.m_real;
			result = (TOther)(object)half;
			return true;
		}
		if (typeof(TOther) == typeof(short))
		{
			short num2 = ((value.m_real >= 32767.0) ? short.MaxValue : ((value.m_real <= -32768.0) ? short.MinValue : ((short)value.m_real)));
			result = (TOther)(object)num2;
			return true;
		}
		if (typeof(TOther) == typeof(int))
		{
			int num3 = ((value.m_real >= 2147483647.0) ? int.MaxValue : ((value.m_real <= -2147483648.0) ? int.MinValue : ((int)value.m_real)));
			result = (TOther)(object)num3;
			return true;
		}
		if (typeof(TOther) == typeof(long))
		{
			long num4 = ((value.m_real >= 9.223372036854776E+18) ? long.MaxValue : ((value.m_real <= -9.223372036854776E+18) ? long.MinValue : ((long)value.m_real)));
			result = (TOther)(object)num4;
			return true;
		}
		if (typeof(TOther) == typeof(Int128))
		{
			Int128 @int = ((value.m_real >= 1.7014118346046923E+38) ? Int128.MaxValue : ((value.m_real <= -1.7014118346046923E+38) ? Int128.MinValue : ((Int128)value.m_real)));
			result = (TOther)(object)@int;
			return true;
		}
		if (typeof(TOther) == typeof(nint))
		{
			nint num5 = ((value.m_real >= (double)IntPtr.MaxValue) ? IntPtr.MaxValue : ((value.m_real <= (double)IntPtr.MinValue) ? IntPtr.MinValue : ((nint)value.m_real)));
			result = (TOther)(object)num5;
			return true;
		}
		if (typeof(TOther) == typeof(BigInteger))
		{
			BigInteger bigInteger = (BigInteger)value.m_real;
			result = (TOther)(object)bigInteger;
			return true;
		}
		if (typeof(TOther) == typeof(sbyte))
		{
			sbyte b2 = ((value.m_real >= 127.0) ? sbyte.MaxValue : ((value.m_real <= -128.0) ? sbyte.MinValue : ((sbyte)value.m_real)));
			result = (TOther)(object)b2;
			return true;
		}
		if (typeof(TOther) == typeof(float))
		{
			float num6 = (float)value.m_real;
			result = (TOther)(object)num6;
			return true;
		}
		if (typeof(TOther) == typeof(ushort))
		{
			ushort num7 = (ushort)((value.m_real >= 65535.0) ? ushort.MaxValue : ((!(value.m_real <= 0.0)) ? ((ushort)value.m_real) : 0));
			result = (TOther)(object)num7;
			return true;
		}
		if (typeof(TOther) == typeof(uint))
		{
			uint num8 = ((value.m_real >= 4294967295.0) ? uint.MaxValue : ((!(value.m_real <= 0.0)) ? ((uint)value.m_real) : 0u));
			result = (TOther)(object)num8;
			return true;
		}
		if (typeof(TOther) == typeof(ulong))
		{
			ulong num9 = ((value.m_real >= 1.8446744073709552E+19) ? ulong.MaxValue : ((value.m_real <= 0.0) ? 0 : ((ulong)value.m_real)));
			result = (TOther)(object)num9;
			return true;
		}
		if (typeof(TOther) == typeof(UInt128))
		{
			UInt128 uInt = ((value.m_real >= 3.402823669209385E+38) ? UInt128.MaxValue : ((value.m_real <= 0.0) ? UInt128.MinValue : ((UInt128)value.m_real)));
			result = (TOther)(object)uInt;
			return true;
		}
		if (typeof(TOther) == typeof(nuint))
		{
			nuint num10 = ((value.m_real >= (double)UIntPtr.MaxValue) ? UIntPtr.MaxValue : ((value.m_real <= (double)UIntPtr.MinValue) ? UIntPtr.MinValue : ((nuint)value.m_real)));
			result = (TOther)(object)num10;
			return true;
		}
		result = default(TOther);
		return false;
	}

	public static bool TryParse(ReadOnlySpan<char> s, NumberStyles style, IFormatProvider? provider, out Complex result)
	{
		ValidateParseStyleFloatingPoint(style);
		int num = s.IndexOf('<');
		int num2 = s.IndexOf(';');
		int num3 = s.IndexOf('>');
		if (s.Length < 5 || num == -1 || num2 == -1 || num3 == -1 || num > num2 || num > num3 || num2 > num3)
		{
			result = default(Complex);
			return false;
		}
		if (num != 0 && ((style & NumberStyles.AllowLeadingWhite) == 0 || !s.Slice(0, num).IsWhiteSpace()))
		{
			result = default(Complex);
			return false;
		}
		if (!double.TryParse(s.Slice(num + 1, num2), style, provider, out var result2))
		{
			result = default(Complex);
			return false;
		}
		if (char.IsWhiteSpace(s[num2 + 1]))
		{
			num2++;
		}
		if (!double.TryParse(s.Slice(num2 + 1, num3 - num2), style, provider, out var result3))
		{
			result = default(Complex);
			return false;
		}
		if (num3 != s.Length - 1 && ((style & NumberStyles.AllowTrailingWhite) == 0 || !s.Slice(num3).IsWhiteSpace()))
		{
			result = default(Complex);
			return false;
		}
		result = new Complex(result2, result3);
		return true;
		static void ThrowInvalid(NumberStyles value)
		{
			if (((uint)value & 0xFFFFFC00u) != 0)
			{
				throw new ArgumentException(System.SR.Argument_InvalidNumberStyles, "style");
			}
			throw new ArgumentException(System.SR.Arg_HexStyleNotSupported);
		}
		static void ValidateParseStyleFloatingPoint(NumberStyles style)
		{
			if (((uint)style & 0xFFFFFE00u) != 0)
			{
				ThrowInvalid(style);
			}
		}
	}

	public static bool TryParse([NotNullWhen(true)] string? s, NumberStyles style, IFormatProvider? provider, out Complex result)
	{
		if (s == null)
		{
			result = default(Complex);
			return false;
		}
		return TryParse(s.AsSpan(), style, provider, out result);
	}

	public static Complex Parse(string s, IFormatProvider? provider)
	{
		return Parse(s, NumberStyles.Float | NumberStyles.AllowThousands, provider);
	}

	public static bool TryParse([NotNullWhen(true)] string? s, IFormatProvider? provider, out Complex result)
	{
		return TryParse(s, NumberStyles.Float | NumberStyles.AllowThousands, provider, out result);
	}

	public bool TryFormat(Span<char> destination, out int charsWritten, [StringSyntax("NumericFormat")] ReadOnlySpan<char> format = default(ReadOnlySpan<char>), IFormatProvider? provider = null)
	{
		return TryFormatCore(destination, out charsWritten, format, provider);
	}

	public bool TryFormat(Span<byte> utf8Destination, out int bytesWritten, [StringSyntax("NumericFormat")] ReadOnlySpan<char> format = default(ReadOnlySpan<char>), IFormatProvider? provider = null)
	{
		return TryFormatCore(utf8Destination, out bytesWritten, format, provider);
	}

	private bool TryFormatCore<TChar>(Span<TChar> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider provider) where TChar : unmanaged, IBinaryInteger<TChar>
	{
		if (destination.Length >= 6 && ((typeof(TChar) == typeof(char)) ? m_real.TryFormat(MemoryMarshal.Cast<TChar, char>(destination.Slice(1)), out var charsWritten2, format, provider) : m_real.TryFormat(MemoryMarshal.Cast<TChar, byte>(destination.Slice(1)), out charsWritten2, format, provider)))
		{
			destination[0] = TChar.CreateTruncating('<');
			destination = destination.Slice(1 + charsWritten2);
			if (destination.Length >= 4 && ((typeof(TChar) == typeof(char)) ? m_imaginary.TryFormat(MemoryMarshal.Cast<TChar, char>(destination.Slice(2)), out var charsWritten3, format, provider) : m_imaginary.TryFormat(MemoryMarshal.Cast<TChar, byte>(destination.Slice(2)), out charsWritten3, format, provider)) && (uint)(2 + charsWritten3) < (uint)destination.Length)
			{
				destination[0] = TChar.CreateTruncating(';');
				destination[1] = TChar.CreateTruncating(' ');
				destination[2 + charsWritten3] = TChar.CreateTruncating('>');
				charsWritten = charsWritten2 + charsWritten3 + 4;
				return true;
			}
		}
		charsWritten = 0;
		return false;
	}

	public static Complex Parse(ReadOnlySpan<char> s, IFormatProvider? provider)
	{
		return Parse(s, NumberStyles.Float | NumberStyles.AllowThousands, provider);
	}

	public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, out Complex result)
	{
		return TryParse(s, NumberStyles.Float | NumberStyles.AllowThousands, provider, out result);
	}

	public static Complex operator +(Complex value)
	{
		return value;
	}
}
