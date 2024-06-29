using System.Buffers;
using System.Buffers.Binary;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System.Numerics;

[Serializable]
[TypeForwardedFrom("System.Numerics, Version=4.0.0.0, PublicKeyToken=b77a5c561934e089")]
[DebuggerDisplay("{DebuggerDisplay,nq}")]
public readonly struct BigInteger : ISpanFormattable, IFormattable, IComparable, IComparable<BigInteger>, IEquatable<BigInteger>, IBinaryInteger<BigInteger>, IParsable<BigInteger>, ISpanParsable<BigInteger>, IAdditionOperators<BigInteger, BigInteger, BigInteger>, IAdditiveIdentity<BigInteger, BigInteger>, IBinaryNumber<BigInteger>, IBitwiseOperators<BigInteger, BigInteger, BigInteger>, IComparisonOperators<BigInteger, BigInteger, bool>, IEqualityOperators<BigInteger, BigInteger, bool>, IDecrementOperators<BigInteger>, IDivisionOperators<BigInteger, BigInteger, BigInteger>, IIncrementOperators<BigInteger>, IModulusOperators<BigInteger, BigInteger, BigInteger>, IMultiplicativeIdentity<BigInteger, BigInteger>, IMultiplyOperators<BigInteger, BigInteger, BigInteger>, INumber<BigInteger>, INumberBase<BigInteger>, ISubtractionOperators<BigInteger, BigInteger, BigInteger>, IUnaryNegationOperators<BigInteger, BigInteger>, IUnaryPlusOperators<BigInteger, BigInteger>, IUtf8SpanFormattable, IUtf8SpanParsable<BigInteger>, IShiftOperators<BigInteger, int, BigInteger>, ISignedNumber<BigInteger>
{
	private enum GetBytesMode
	{
		AllocateArray,
		Count,
		Span
	}

	internal readonly int _sign;

	internal readonly uint[]? _bits;

	private static readonly BigInteger s_bnMinInt = new BigInteger(-1, new uint[1] { 2147483648u });

	private static readonly BigInteger s_bnOneInt = new BigInteger(1);

	private static readonly BigInteger s_bnZeroInt = new BigInteger(0);

	private static readonly BigInteger s_bnMinusOneInt = new BigInteger(-1);

	public static BigInteger Zero => s_bnZeroInt;

	public static BigInteger One => s_bnOneInt;

	public static BigInteger MinusOne => s_bnMinusOneInt;

	internal static int MaxLength => Array.MaxLength / 4;

	public bool IsPowerOfTwo
	{
		get
		{
			if (_bits == null)
			{
				return BitOperations.IsPow2(_sign);
			}
			if (_sign != 1)
			{
				return false;
			}
			int num = _bits.Length - 1;
			if (!BitOperations.IsPow2(_bits[num]))
			{
				return false;
			}
			while (--num >= 0)
			{
				if (_bits[num] != 0)
				{
					return false;
				}
			}
			return true;
		}
	}

	public bool IsZero => _sign == 0;

	public bool IsOne
	{
		get
		{
			if (_sign == 1)
			{
				return _bits == null;
			}
			return false;
		}
	}

	public bool IsEven
	{
		get
		{
			if (_bits != null)
			{
				return (_bits[0] & 1) == 0;
			}
			return (_sign & 1) == 0;
		}
	}

	public int Sign => (_sign >> 31) - (-_sign >> 31);

	private string DebuggerDisplay
	{
		get
		{
			if (_bits == null || _bits.Length <= 4)
			{
				return ToString();
			}
			ulong num = ((ulong)_bits[^1] << 32) + _bits[^2];
			double num2 = _bits.Length - 2;
			double num3 = num2 * 32.0 * 0.3010299956639812;
			long num4 = (long)num3;
			double num5 = (double)num * Math.Pow(10.0, num3 - (double)num4);
			double num6 = Math.Log10(num5);
			if (num6 >= 1.0)
			{
				num4 += (long)num6;
				num5 /= Math.Pow(10.0, Math.Floor(num6));
			}
			num5 = Math.Round(num5, 8);
			if (num5 >= 10.0)
			{
				num5 /= 10.0;
				num4++;
			}
			string value = ((_sign < 0) ? NumberFormatInfo.CurrentInfo.NegativeSign : "");
			return $"{value}{num5:F8}e+{num4}";
		}
	}

	static BigInteger IAdditiveIdentity<BigInteger, BigInteger>.AdditiveIdentity => Zero;

	static BigInteger IBinaryNumber<BigInteger>.AllBitsSet => MinusOne;

	static BigInteger IMultiplicativeIdentity<BigInteger, BigInteger>.MultiplicativeIdentity => One;

	static int INumberBase<BigInteger>.Radix => 2;

	static BigInteger ISignedNumber<BigInteger>.NegativeOne => MinusOne;

	public BigInteger(int value)
	{
		if (value == int.MinValue)
		{
			this = s_bnMinInt;
			return;
		}
		_sign = value;
		_bits = null;
	}

	[CLSCompliant(false)]
	public BigInteger(uint value)
	{
		if (value <= int.MaxValue)
		{
			_sign = (int)value;
			_bits = null;
		}
		else
		{
			_sign = 1;
			_bits = new uint[1];
			_bits[0] = value;
		}
	}

	public BigInteger(long value)
	{
		if (int.MinValue < value && value <= int.MaxValue)
		{
			_sign = (int)value;
			_bits = null;
			return;
		}
		if (value == int.MinValue)
		{
			this = s_bnMinInt;
			return;
		}
		ulong num;
		if (value < 0)
		{
			num = (ulong)(-value);
			_sign = -1;
		}
		else
		{
			num = (ulong)value;
			_sign = 1;
		}
		if (num <= uint.MaxValue)
		{
			_bits = new uint[1];
			_bits[0] = (uint)num;
		}
		else
		{
			_bits = new uint[2];
			_bits[0] = (uint)num;
			_bits[1] = (uint)(num >> 32);
		}
	}

	[CLSCompliant(false)]
	public BigInteger(ulong value)
	{
		if (value <= int.MaxValue)
		{
			_sign = (int)value;
			_bits = null;
		}
		else if (value <= uint.MaxValue)
		{
			_sign = 1;
			_bits = new uint[1];
			_bits[0] = (uint)value;
		}
		else
		{
			_sign = 1;
			_bits = new uint[2];
			_bits[0] = (uint)value;
			_bits[1] = (uint)(value >> 32);
		}
	}

	public BigInteger(float value)
		: this((double)value)
	{
	}

	public BigInteger(double value)
	{
		if (!double.IsFinite(value))
		{
			if (double.IsInfinity(value))
			{
				throw new OverflowException(System.SR.Overflow_BigIntInfinity);
			}
			throw new OverflowException(System.SR.Overflow_NotANumber);
		}
		_sign = 0;
		_bits = null;
		NumericsHelpers.GetDoubleParts(value, out var sign, out var exp, out var man, out var _);
		if (man == 0L)
		{
			this = Zero;
			return;
		}
		if (exp <= 0)
		{
			if (exp <= -64)
			{
				this = Zero;
				return;
			}
			this = man >> -exp;
			if (sign < 0)
			{
				_sign = -_sign;
			}
			return;
		}
		if (exp <= 11)
		{
			this = man << exp;
			if (sign < 0)
			{
				_sign = -_sign;
			}
			return;
		}
		man <<= 11;
		exp -= 11;
		int num = (exp - 1) / 32 + 1;
		int num2 = num * 32 - exp;
		_bits = new uint[num + 2];
		_bits[num + 1] = (uint)(man >> num2 + 32);
		_bits[num] = (uint)(man >> num2);
		if (num2 > 0)
		{
			_bits[num - 1] = (uint)((int)man << 32 - num2);
		}
		_sign = sign;
	}

	public BigInteger(decimal value)
	{
		Span<int> destination = stackalloc int[4];
		decimal.GetBits(decimal.Truncate(value), destination);
		int num = 3;
		while (num > 0 && destination[num - 1] == 0)
		{
			num--;
		}
		switch (num)
		{
		case 0:
			this = s_bnZeroInt;
			return;
		case 1:
			if (destination[0] > 0)
			{
				_sign = destination[0];
				_sign *= (((destination[3] & int.MinValue) == 0) ? 1 : (-1));
				_bits = null;
				return;
			}
			break;
		}
		_bits = new uint[num];
		_bits[0] = (uint)destination[0];
		if (num > 1)
		{
			_bits[1] = (uint)destination[1];
		}
		if (num > 2)
		{
			_bits[2] = (uint)destination[2];
		}
		_sign = (((destination[3] & int.MinValue) == 0) ? 1 : (-1));
	}

	[CLSCompliant(false)]
	public BigInteger(byte[] value)
		: this(new ReadOnlySpan<byte>(value ?? throw new ArgumentNullException("value")))
	{
	}

	public BigInteger(ReadOnlySpan<byte> value, bool isUnsigned = false, bool isBigEndian = false)
	{
		int num = value.Length;
		bool flag;
		if (num > 0)
		{
			byte b = (isBigEndian ? value[0] : value[num - 1]);
			flag = (b & 0x80u) != 0 && !isUnsigned;
			if (b == 0)
			{
				if (isBigEndian)
				{
					int i;
					for (i = 1; i < num && value[i] == 0; i++)
					{
					}
					value = value.Slice(i);
					num = value.Length;
				}
				else
				{
					num -= 2;
					while (num >= 0 && value[num] == 0)
					{
						num--;
					}
					num++;
				}
			}
		}
		else
		{
			flag = false;
		}
		if (num == 0)
		{
			_sign = 0;
			_bits = null;
			return;
		}
		if (num <= 4)
		{
			_sign = (flag ? (-1) : 0);
			if (isBigEndian)
			{
				for (int j = 0; j < num; j++)
				{
					_sign = (_sign << 8) | value[j];
				}
			}
			else
			{
				for (int num2 = num - 1; num2 >= 0; num2--)
				{
					_sign = (_sign << 8) | value[num2];
				}
			}
			_bits = null;
			if (_sign < 0 && !flag)
			{
				_bits = new uint[1] { (uint)_sign };
				_sign = 1;
			}
			if (_sign == int.MinValue)
			{
				this = s_bnMinInt;
			}
			return;
		}
		int num3 = num % 4;
		int num4 = num / 4 + ((num3 != 0) ? 1 : 0);
		uint[] array = new uint[num4];
		int num5 = num - 1;
		int k;
		if (isBigEndian)
		{
			int num6 = num - 4;
			for (k = 0; k < num4 - ((num3 != 0) ? 1 : 0); k++)
			{
				for (int l = 0; l < 4; l++)
				{
					byte b2 = value[num6];
					array[k] = (array[k] << 8) | b2;
					num6++;
				}
				num6 -= 8;
			}
		}
		else
		{
			int num6 = 3;
			for (k = 0; k < num4 - ((num3 != 0) ? 1 : 0); k++)
			{
				for (int m = 0; m < 4; m++)
				{
					byte b3 = value[num6];
					array[k] = (array[k] << 8) | b3;
					num6--;
				}
				num6 += 8;
			}
		}
		if (num3 != 0)
		{
			if (flag)
			{
				array[num4 - 1] = uint.MaxValue;
			}
			if (isBigEndian)
			{
				for (int num6 = 0; num6 < num3; num6++)
				{
					byte b4 = value[num6];
					array[k] = (array[k] << 8) | b4;
				}
			}
			else
			{
				for (int num6 = num5; num6 >= num - num3; num6--)
				{
					byte b5 = value[num6];
					array[k] = (array[k] << 8) | b5;
				}
			}
		}
		if (flag)
		{
			NumericsHelpers.DangerousMakeTwosComplement(array);
			int num7 = array.Length - 1;
			while (num7 >= 0 && array[num7] == 0)
			{
				num7--;
			}
			num7++;
			if (num7 == 1)
			{
				switch (array[0])
				{
				case 1u:
					this = s_bnMinusOneInt;
					return;
				case 2147483648u:
					this = s_bnMinInt;
					return;
				}
				if ((int)array[0] > 0)
				{
					_sign = -1 * (int)array[0];
					_bits = null;
					return;
				}
			}
			if (num7 != array.Length)
			{
				_sign = -1;
				_bits = new uint[num7];
				Array.Copy(array, _bits, num7);
			}
			else
			{
				_sign = -1;
				_bits = array;
			}
		}
		else
		{
			_sign = 1;
			_bits = array;
		}
	}

	internal BigInteger(int n, uint[]? rgu)
	{
		if (rgu != null && rgu.Length > MaxLength)
		{
			System.ThrowHelper.ThrowOverflowException();
		}
		_sign = n;
		_bits = rgu;
	}

	private BigInteger(ReadOnlySpan<uint> value, bool negative)
	{
		if (value.Length > MaxLength)
		{
			System.ThrowHelper.ThrowOverflowException();
		}
		int num = value.Length;
		while (num > 0 && value[num - 1] == 0)
		{
			num--;
		}
		switch (num)
		{
		case 0:
			this = s_bnZeroInt;
			break;
		case 1:
			if (value[0] < 2147483648u)
			{
				_sign = (int)(negative ? (0 - value[0]) : value[0]);
				_bits = null;
				if (_sign == int.MinValue)
				{
					this = s_bnMinInt;
				}
				break;
			}
			goto default;
		default:
			_sign = ((!negative) ? 1 : (-1));
			_bits = value.Slice(0, num).ToArray();
			break;
		}
	}

	private BigInteger(Span<uint> value)
	{
		if (value.Length > MaxLength)
		{
			System.ThrowHelper.ThrowOverflowException();
		}
		int num = value.Length;
		bool flag = num > 0 && (value[num - 1] & 0x80000000u) == 2147483648u;
		while (num > 0 && value[num - 1] == 0)
		{
			num--;
		}
		switch (num)
		{
		case 0:
			this = s_bnZeroInt;
			return;
		case 1:
			if ((int)value[0] < 0 && !flag)
			{
				_bits = new uint[1];
				_bits[0] = value[0];
				_sign = 1;
			}
			else if (int.MinValue == (int)value[0])
			{
				this = s_bnMinInt;
			}
			else
			{
				_sign = (int)value[0];
				_bits = null;
			}
			return;
		}
		if (!flag)
		{
			_sign = 1;
			value = value.Slice(0, num);
			_bits = value.ToArray();
			return;
		}
		NumericsHelpers.DangerousMakeTwosComplement(value);
		int num2 = value.Length;
		while (num2 > 0 && value[num2 - 1] == 0)
		{
			num2--;
		}
		if (num2 == 1 && (int)value[0] > 0)
		{
			if (value[0] == 1)
			{
				this = s_bnMinusOneInt;
				return;
			}
			if (value[0] == 2147483648u)
			{
				this = s_bnMinInt;
				return;
			}
			_sign = -1 * (int)value[0];
			_bits = null;
		}
		else
		{
			_sign = -1;
			_bits = value.Slice(0, num2).ToArray();
		}
	}

	public static BigInteger Parse(string value)
	{
		return Parse(value, NumberStyles.Integer);
	}

	public static BigInteger Parse(string value, NumberStyles style)
	{
		return Parse(value, style, NumberFormatInfo.CurrentInfo);
	}

	public static BigInteger Parse(string value, IFormatProvider? provider)
	{
		return Parse(value, NumberStyles.Integer, NumberFormatInfo.GetInstance(provider));
	}

	public static BigInteger Parse(string value, NumberStyles style, IFormatProvider? provider)
	{
		return BigNumber.ParseBigInteger(value, style, NumberFormatInfo.GetInstance(provider));
	}

	public static bool TryParse([NotNullWhen(true)] string? value, out BigInteger result)
	{
		return TryParse(value, NumberStyles.Integer, NumberFormatInfo.CurrentInfo, out result);
	}

	public static bool TryParse([NotNullWhen(true)] string? value, NumberStyles style, IFormatProvider? provider, out BigInteger result)
	{
		return BigNumber.TryParseBigInteger(value, style, NumberFormatInfo.GetInstance(provider), out result) == BigNumber.ParsingStatus.OK;
	}

	public static BigInteger Parse(ReadOnlySpan<char> value, NumberStyles style = NumberStyles.Integer, IFormatProvider? provider = null)
	{
		return BigNumber.ParseBigInteger(value, style, NumberFormatInfo.GetInstance(provider));
	}

	public static bool TryParse(ReadOnlySpan<char> value, out BigInteger result)
	{
		return TryParse(value, NumberStyles.Integer, NumberFormatInfo.CurrentInfo, out result);
	}

	public static bool TryParse(ReadOnlySpan<char> value, NumberStyles style, IFormatProvider? provider, out BigInteger result)
	{
		return BigNumber.TryParseBigInteger(value, style, NumberFormatInfo.GetInstance(provider), out result) == BigNumber.ParsingStatus.OK;
	}

	public static int Compare(BigInteger left, BigInteger right)
	{
		return left.CompareTo(right);
	}

	public static BigInteger Abs(BigInteger value)
	{
		if (!(value >= Zero))
		{
			return -value;
		}
		return value;
	}

	public static BigInteger Add(BigInteger left, BigInteger right)
	{
		return left + right;
	}

	public static BigInteger Subtract(BigInteger left, BigInteger right)
	{
		return left - right;
	}

	public static BigInteger Multiply(BigInteger left, BigInteger right)
	{
		return left * right;
	}

	public static BigInteger Divide(BigInteger dividend, BigInteger divisor)
	{
		return dividend / divisor;
	}

	public static BigInteger Remainder(BigInteger dividend, BigInteger divisor)
	{
		return dividend % divisor;
	}

	public static BigInteger DivRem(BigInteger dividend, BigInteger divisor, out BigInteger remainder)
	{
		bool flag = dividend._bits == null;
		bool flag2 = divisor._bits == null;
		if (flag && flag2)
		{
			(int Quotient, int Remainder) tuple = Math.DivRem(dividend._sign, divisor._sign);
			BigInteger bigInteger = tuple.Quotient;
			BigInteger bigInteger2 = tuple.Remainder;
			BigInteger result = bigInteger;
			remainder = bigInteger2;
			return result;
		}
		if (flag)
		{
			remainder = dividend;
			return s_bnZeroInt;
		}
		if (flag2)
		{
			uint[] array = null;
			int num = dividend._bits.Length;
			Span<uint> span = (((uint)num > 64u) ? ((Span<uint>)(array = ArrayPool<uint>.Shared.Rent(num))) : stackalloc uint[64]).Slice(0, num);
			try
			{
				BigIntegerCalculator.Divide(dividend._bits, NumericsHelpers.Abs(divisor._sign), span, out var remainder2);
				remainder = ((dividend._sign < 0) ? (-1 * remainder2) : remainder2);
				return new BigInteger(span, (dividend._sign < 0) ^ (divisor._sign < 0));
			}
			finally
			{
				if (array != null)
				{
					ArrayPool<uint>.Shared.Return(array);
				}
			}
		}
		if (dividend._bits.Length < divisor._bits.Length)
		{
			remainder = dividend;
			return s_bnZeroInt;
		}
		uint[] array2 = null;
		int num2 = dividend._bits.Length;
		Span<uint> span2 = (((uint)num2 > 64u) ? ((Span<uint>)(array2 = ArrayPool<uint>.Shared.Rent(num2))) : stackalloc uint[64]).Slice(0, num2);
		uint[] array3 = null;
		num2 = dividend._bits.Length - divisor._bits.Length + 1;
		Span<uint> span3 = (((uint)num2 > 64u) ? ((Span<uint>)(array3 = ArrayPool<uint>.Shared.Rent(num2))) : stackalloc uint[64]).Slice(0, num2);
		BigIntegerCalculator.Divide(dividend._bits, divisor._bits, span3, span2);
		remainder = new BigInteger(span2, dividend._sign < 0);
		BigInteger result2 = new BigInteger(span3, (dividend._sign < 0) ^ (divisor._sign < 0));
		if (array2 != null)
		{
			ArrayPool<uint>.Shared.Return(array2);
		}
		if (array3 != null)
		{
			ArrayPool<uint>.Shared.Return(array3);
		}
		return result2;
	}

	public static BigInteger Negate(BigInteger value)
	{
		return -value;
	}

	public static double Log(BigInteger value)
	{
		return Log(value, Math.E);
	}

	public static double Log(BigInteger value, double baseValue)
	{
		if (value._sign < 0 || baseValue == 1.0)
		{
			return double.NaN;
		}
		if (baseValue == double.PositiveInfinity)
		{
			if (!value.IsOne)
			{
				return double.NaN;
			}
			return 0.0;
		}
		if (baseValue == 0.0 && !value.IsOne)
		{
			return double.NaN;
		}
		if (value._bits == null)
		{
			return Math.Log(value._sign, baseValue);
		}
		ulong num = value._bits[value._bits.Length - 1];
		ulong num2 = ((value._bits.Length > 1) ? value._bits[value._bits.Length - 2] : 0u);
		ulong num3 = ((value._bits.Length > 2) ? value._bits[value._bits.Length - 3] : 0u);
		int num4 = BitOperations.LeadingZeroCount((uint)num);
		long num5 = (long)value._bits.Length * 32L - num4;
		ulong num6 = (num << 32 + num4) | (num2 << num4) | (num3 >> 32 - num4);
		return Math.Log(num6, baseValue) + (double)(num5 - 64) / Math.Log(baseValue, 2.0);
	}

	public static double Log10(BigInteger value)
	{
		return Log(value, 10.0);
	}

	public static BigInteger GreatestCommonDivisor(BigInteger left, BigInteger right)
	{
		bool flag = left._bits == null;
		bool flag2 = right._bits == null;
		if (flag && flag2)
		{
			return BigIntegerCalculator.Gcd(NumericsHelpers.Abs(left._sign), NumericsHelpers.Abs(right._sign));
		}
		if (flag)
		{
			if (left._sign == 0)
			{
				return new BigInteger(right._bits, negative: false);
			}
			return BigIntegerCalculator.Gcd(right._bits, NumericsHelpers.Abs(left._sign));
		}
		if (flag2)
		{
			if (right._sign == 0)
			{
				return new BigInteger(left._bits, negative: false);
			}
			return BigIntegerCalculator.Gcd(left._bits, NumericsHelpers.Abs(right._sign));
		}
		if (BigIntegerCalculator.Compare(left._bits, right._bits) < 0)
		{
			return GreatestCommonDivisor(right._bits, left._bits);
		}
		return GreatestCommonDivisor(left._bits, right._bits);
	}

	private static BigInteger GreatestCommonDivisor(ReadOnlySpan<uint> leftBits, ReadOnlySpan<uint> rightBits)
	{
		uint[] array = null;
		BigInteger result;
		if (rightBits.Length == 1)
		{
			uint right = BigIntegerCalculator.Remainder(leftBits, rightBits[0]);
			result = BigIntegerCalculator.Gcd(rightBits[0], right);
		}
		else if (rightBits.Length == 2)
		{
			Span<uint> remainder = ((leftBits.Length > 64) ? ((Span<uint>)(array = ArrayPool<uint>.Shared.Rent(leftBits.Length))) : stackalloc uint[64]).Slice(0, leftBits.Length);
			BigIntegerCalculator.Remainder(leftBits, rightBits, remainder);
			ulong left = ((ulong)rightBits[1] << 32) | rightBits[0];
			ulong right2 = ((ulong)remainder[1] << 32) | remainder[0];
			result = BigIntegerCalculator.Gcd(left, right2);
		}
		else
		{
			Span<uint> span = ((leftBits.Length > 64) ? ((Span<uint>)(array = ArrayPool<uint>.Shared.Rent(leftBits.Length))) : stackalloc uint[64]).Slice(0, leftBits.Length);
			BigIntegerCalculator.Gcd(leftBits, rightBits, span);
			result = new BigInteger(span, negative: false);
		}
		if (array != null)
		{
			ArrayPool<uint>.Shared.Return(array);
		}
		return result;
	}

	public static BigInteger Max(BigInteger left, BigInteger right)
	{
		if (left.CompareTo(right) < 0)
		{
			return right;
		}
		return left;
	}

	public static BigInteger Min(BigInteger left, BigInteger right)
	{
		if (left.CompareTo(right) <= 0)
		{
			return left;
		}
		return right;
	}

	public static BigInteger ModPow(BigInteger value, BigInteger exponent, BigInteger modulus)
	{
		ArgumentOutOfRangeException.ThrowIfNegative(exponent.Sign, "exponent");
		bool flag = value._bits == null;
		bool flag2 = exponent._bits == null;
		if (modulus._bits == null)
		{
			uint num = ((flag && flag2) ? BigIntegerCalculator.Pow(NumericsHelpers.Abs(value._sign), NumericsHelpers.Abs(exponent._sign), NumericsHelpers.Abs(modulus._sign)) : (flag ? BigIntegerCalculator.Pow(NumericsHelpers.Abs(value._sign), exponent._bits, NumericsHelpers.Abs(modulus._sign)) : (flag2 ? BigIntegerCalculator.Pow(value._bits, NumericsHelpers.Abs(exponent._sign), NumericsHelpers.Abs(modulus._sign)) : BigIntegerCalculator.Pow(value._bits, exponent._bits, NumericsHelpers.Abs(modulus._sign)))));
			return (value._sign < 0 && !exponent.IsEven) ? (-1 * num) : num;
		}
		uint[]? bits = modulus._bits;
		int num2 = ((bits == null) ? 1 : bits.Length) << 1;
		uint[] array = null;
		Span<uint> span = (((uint)num2 > 64u) ? ((Span<uint>)(array = ArrayPool<uint>.Shared.Rent(num2))) : stackalloc uint[64]).Slice(0, num2);
		span.Clear();
		if (flag)
		{
			if (flag2)
			{
				BigIntegerCalculator.Pow(NumericsHelpers.Abs(value._sign), NumericsHelpers.Abs(exponent._sign), modulus._bits, span);
			}
			else
			{
				BigIntegerCalculator.Pow(NumericsHelpers.Abs(value._sign), exponent._bits, modulus._bits, span);
			}
		}
		else if (flag2)
		{
			BigIntegerCalculator.Pow(value._bits, NumericsHelpers.Abs(exponent._sign), modulus._bits, span);
		}
		else
		{
			BigIntegerCalculator.Pow(value._bits, exponent._bits, modulus._bits, span);
		}
		BigInteger result = new BigInteger(span, value._sign < 0 && !exponent.IsEven);
		if (array != null)
		{
			ArrayPool<uint>.Shared.Return(array);
		}
		return result;
	}

	public static BigInteger Pow(BigInteger value, int exponent)
	{
		ArgumentOutOfRangeException.ThrowIfNegative(exponent, "exponent");
		switch (exponent)
		{
		case 0:
			return s_bnOneInt;
		case 1:
			return value;
		default:
		{
			bool flag = value._bits == null;
			uint power = NumericsHelpers.Abs(exponent);
			uint[] array = null;
			BigInteger result;
			if (flag)
			{
				if (value._sign == 1)
				{
					return value;
				}
				if (value._sign == -1)
				{
					if ((exponent & 1) == 0)
					{
						return s_bnOneInt;
					}
					return value;
				}
				if (value._sign == 0)
				{
					return value;
				}
				int num = BigIntegerCalculator.PowBound(power, 1);
				Span<uint> span = (((uint)num > 64u) ? ((Span<uint>)(array = ArrayPool<uint>.Shared.Rent(num))) : stackalloc uint[64]).Slice(0, num);
				span.Clear();
				BigIntegerCalculator.Pow(NumericsHelpers.Abs(value._sign), power, span);
				result = new BigInteger(span, value._sign < 0 && (exponent & 1) != 0);
			}
			else
			{
				int num2 = BigIntegerCalculator.PowBound(power, value._bits.Length);
				Span<uint> span2 = (((uint)num2 > 64u) ? ((Span<uint>)(array = ArrayPool<uint>.Shared.Rent(num2))) : stackalloc uint[64]).Slice(0, num2);
				span2.Clear();
				BigIntegerCalculator.Pow(value._bits, power, span2);
				result = new BigInteger(span2, value._sign < 0 && (exponent & 1) != 0);
			}
			if (array != null)
			{
				ArrayPool<uint>.Shared.Return(array);
			}
			return result;
		}
		}
	}

	public override int GetHashCode()
	{
		if (_bits == null)
		{
			return _sign;
		}
		HashCode hashCode = default(HashCode);
		hashCode.AddBytes(MemoryMarshal.AsBytes(_bits.AsSpan()));
		hashCode.Add(_sign);
		return hashCode.ToHashCode();
	}

	public override bool Equals([NotNullWhen(true)] object? obj)
	{
		if (obj is BigInteger other)
		{
			return Equals(other);
		}
		return false;
	}

	public bool Equals(long other)
	{
		if (_bits == null)
		{
			return _sign == other;
		}
		int num;
		if ((_sign ^ other) < 0 || (num = _bits.Length) > 2)
		{
			return false;
		}
		ulong num2 = (ulong)((other < 0) ? (-other) : other);
		if (num == 1)
		{
			return _bits[0] == num2;
		}
		return NumericsHelpers.MakeUInt64(_bits[1], _bits[0]) == num2;
	}

	[CLSCompliant(false)]
	public bool Equals(ulong other)
	{
		if (_sign < 0)
		{
			return false;
		}
		if (_bits == null)
		{
			return (ulong)_sign == other;
		}
		int num = _bits.Length;
		if (num > 2)
		{
			return false;
		}
		if (num == 1)
		{
			return _bits[0] == other;
		}
		return NumericsHelpers.MakeUInt64(_bits[1], _bits[0]) == other;
	}

	public bool Equals(BigInteger other)
	{
		if (_sign != other._sign)
		{
			return false;
		}
		if (_bits == other._bits)
		{
			return true;
		}
		if (_bits == null || other._bits == null)
		{
			return false;
		}
		int num = _bits.Length;
		if (num != other._bits.Length)
		{
			return false;
		}
		int diffLength = GetDiffLength(_bits, other._bits, num);
		return diffLength == 0;
	}

	public int CompareTo(long other)
	{
		if (_bits == null)
		{
			return ((long)_sign).CompareTo(other);
		}
		int num;
		if ((_sign ^ other) < 0 || (num = _bits.Length) > 2)
		{
			return _sign;
		}
		ulong value = (ulong)((other < 0) ? (-other) : other);
		ulong num2 = ((num == 2) ? NumericsHelpers.MakeUInt64(_bits[1], _bits[0]) : _bits[0]);
		return _sign * num2.CompareTo(value);
	}

	[CLSCompliant(false)]
	public int CompareTo(ulong other)
	{
		if (_sign < 0)
		{
			return -1;
		}
		if (_bits == null)
		{
			return ((ulong)_sign).CompareTo(other);
		}
		int num = _bits.Length;
		if (num > 2)
		{
			return 1;
		}
		return ((num == 2) ? NumericsHelpers.MakeUInt64(_bits[1], _bits[0]) : _bits[0]).CompareTo(other);
	}

	public int CompareTo(BigInteger other)
	{
		if ((_sign ^ other._sign) < 0)
		{
			if (_sign >= 0)
			{
				return 1;
			}
			return -1;
		}
		if (_bits == null)
		{
			if (other._bits == null)
			{
				if (_sign >= other._sign)
				{
					return (_sign > other._sign) ? 1 : 0;
				}
				return -1;
			}
			return -other._sign;
		}
		int num;
		int num2;
		if (other._bits == null || (num = _bits.Length) > (num2 = other._bits.Length))
		{
			return _sign;
		}
		if (num < num2)
		{
			return -_sign;
		}
		int diffLength = GetDiffLength(_bits, other._bits, num);
		if (diffLength == 0)
		{
			return 0;
		}
		if (_bits[diffLength - 1] >= other._bits[diffLength - 1])
		{
			return _sign;
		}
		return -_sign;
	}

	public int CompareTo(object? obj)
	{
		if (obj == null)
		{
			return 1;
		}
		if (!(obj is BigInteger other))
		{
			throw new ArgumentException(System.SR.Argument_MustBeBigInt, "obj");
		}
		return CompareTo(other);
	}

	public byte[] ToByteArray()
	{
		return ToByteArray(isUnsigned: false, isBigEndian: false);
	}

	public byte[] ToByteArray(bool isUnsigned = false, bool isBigEndian = false)
	{
		int bytesWritten = 0;
		return TryGetBytes(GetBytesMode.AllocateArray, default(Span<byte>), isUnsigned, isBigEndian, ref bytesWritten);
	}

	public bool TryWriteBytes(Span<byte> destination, out int bytesWritten, bool isUnsigned = false, bool isBigEndian = false)
	{
		bytesWritten = 0;
		if (TryGetBytes(GetBytesMode.Span, destination, isUnsigned, isBigEndian, ref bytesWritten) == null)
		{
			bytesWritten = 0;
			return false;
		}
		return true;
	}

	internal bool TryWriteOrCountBytes(Span<byte> destination, out int bytesWritten, bool isUnsigned = false, bool isBigEndian = false)
	{
		bytesWritten = 0;
		return TryGetBytes(GetBytesMode.Span, destination, isUnsigned, isBigEndian, ref bytesWritten) != null;
	}

	public int GetByteCount(bool isUnsigned = false)
	{
		int bytesWritten = 0;
		TryGetBytes(GetBytesMode.Count, default(Span<byte>), isUnsigned, isBigEndian: false, ref bytesWritten);
		return bytesWritten;
	}

	private byte[] TryGetBytes(GetBytesMode mode, Span<byte> destination, bool isUnsigned, bool isBigEndian, ref int bytesWritten)
	{
		int sign = _sign;
		if (sign == 0)
		{
			switch (mode)
			{
			case GetBytesMode.AllocateArray:
				return new byte[1];
			case GetBytesMode.Count:
				bytesWritten = 1;
				return null;
			default:
				bytesWritten = 1;
				if (destination.Length != 0)
				{
					destination[0] = 0;
					return Array.Empty<byte>();
				}
				return null;
			}
		}
		if (isUnsigned && sign < 0)
		{
			throw new OverflowException(System.SR.Overflow_Negative_Unsigned);
		}
		int i = 0;
		uint[] bits = _bits;
		byte b;
		uint num;
		if (bits == null)
		{
			b = (byte)((sign < 0) ? 255u : 0u);
			num = (uint)sign;
		}
		else if (sign == -1)
		{
			b = byte.MaxValue;
			for (; bits[i] == 0; i++)
			{
			}
			num = ~bits[^1];
			if (bits.Length - 1 == i)
			{
				num++;
			}
		}
		else
		{
			b = 0;
			num = bits[^1];
		}
		byte b2;
		int num2;
		if ((b2 = (byte)(num >> 24)) != b)
		{
			num2 = 3;
		}
		else if ((b2 = (byte)(num >> 16)) != b)
		{
			num2 = 2;
		}
		else if ((b2 = (byte)(num >> 8)) != b)
		{
			num2 = 1;
		}
		else
		{
			b2 = (byte)num;
			num2 = 0;
		}
		bool flag = (b2 & 0x80) != (b & 0x80) && !isUnsigned;
		int num3 = num2 + 1 + (flag ? 1 : 0);
		if (bits != null)
		{
			num3 = checked(4 * (bits.Length - 1) + num3);
		}
		byte[] result;
		switch (mode)
		{
		case GetBytesMode.AllocateArray:
			destination = (result = new byte[num3]);
			break;
		case GetBytesMode.Count:
			bytesWritten = num3;
			return null;
		default:
			bytesWritten = num3;
			if (destination.Length < num3)
			{
				return null;
			}
			result = Array.Empty<byte>();
			break;
		}
		int num4 = (isBigEndian ? (num3 - 1) : 0);
		int num5 = ((!isBigEndian) ? 1 : (-1));
		if (bits != null)
		{
			for (int j = 0; j < bits.Length - 1; j++)
			{
				uint num6 = bits[j];
				if (sign == -1)
				{
					num6 = ~num6;
					if (j <= i)
					{
						num6++;
					}
				}
				destination[num4] = (byte)num6;
				num4 += num5;
				destination[num4] = (byte)(num6 >> 8);
				num4 += num5;
				destination[num4] = (byte)(num6 >> 16);
				num4 += num5;
				destination[num4] = (byte)(num6 >> 24);
				num4 += num5;
			}
		}
		destination[num4] = (byte)num;
		if (num2 != 0)
		{
			num4 += num5;
			destination[num4] = (byte)(num >> 8);
			if (num2 != 1)
			{
				num4 += num5;
				destination[num4] = (byte)(num >> 16);
				if (num2 != 2)
				{
					num4 += num5;
					destination[num4] = (byte)(num >> 24);
				}
			}
		}
		if (flag)
		{
			num4 += num5;
			destination[num4] = b;
		}
		return result;
	}

	private int WriteTo(Span<uint> buffer)
	{
		uint num;
		if (_bits == null)
		{
			buffer[0] = (uint)_sign;
			num = ((_sign < 0) ? uint.MaxValue : 0u);
		}
		else
		{
			_bits.CopyTo(buffer);
			buffer = buffer.Slice(0, _bits.Length + 1);
			if (_sign == -1)
			{
				NumericsHelpers.DangerousMakeTwosComplement(buffer.Slice(0, buffer.Length - 1));
				num = uint.MaxValue;
			}
			else
			{
				num = 0u;
			}
		}
		int num2 = buffer.Length - 2;
		while (num2 > 0 && buffer[num2] == num)
		{
			num2--;
		}
		int num3;
		if ((buffer[num2] & 0x80000000u) != (num & 0x80000000u))
		{
			num3 = num2 + 2;
			buffer = buffer.Slice(0, num3);
			buffer[buffer.Length - 1] = num;
		}
		else
		{
			num3 = num2 + 1;
		}
		return num3;
	}

	public override string ToString()
	{
		return BigNumber.FormatBigInteger(this, null, NumberFormatInfo.CurrentInfo);
	}

	public string ToString(IFormatProvider? provider)
	{
		return BigNumber.FormatBigInteger(this, null, NumberFormatInfo.GetInstance(provider));
	}

	public string ToString([StringSyntax("NumericFormat")] string? format)
	{
		return BigNumber.FormatBigInteger(this, format, NumberFormatInfo.CurrentInfo);
	}

	public string ToString([StringSyntax("NumericFormat")] string? format, IFormatProvider? provider)
	{
		return BigNumber.FormatBigInteger(this, format, NumberFormatInfo.GetInstance(provider));
	}

	public bool TryFormat(Span<char> destination, out int charsWritten, [StringSyntax("NumericFormat")] ReadOnlySpan<char> format = default(ReadOnlySpan<char>), IFormatProvider? provider = null)
	{
		return BigNumber.TryFormatBigInteger(this, format, NumberFormatInfo.GetInstance(provider), destination, out charsWritten);
	}

	private static BigInteger Add(ReadOnlySpan<uint> leftBits, int leftSign, ReadOnlySpan<uint> rightBits, int rightSign)
	{
		bool isEmpty = leftBits.IsEmpty;
		bool isEmpty2 = rightBits.IsEmpty;
		uint[] array = null;
		BigInteger result;
		if (isEmpty)
		{
			int num = rightBits.Length + 1;
			Span<uint> span = (((uint)num > 64u) ? ((Span<uint>)(array = ArrayPool<uint>.Shared.Rent(num))) : stackalloc uint[64]).Slice(0, num);
			BigIntegerCalculator.Add(rightBits, NumericsHelpers.Abs(leftSign), span);
			result = new BigInteger(span, leftSign < 0);
		}
		else if (isEmpty2)
		{
			int num2 = leftBits.Length + 1;
			Span<uint> span2 = (((uint)num2 > 64u) ? ((Span<uint>)(array = ArrayPool<uint>.Shared.Rent(num2))) : stackalloc uint[64]).Slice(0, num2);
			BigIntegerCalculator.Add(leftBits, NumericsHelpers.Abs(rightSign), span2);
			result = new BigInteger(span2, leftSign < 0);
		}
		else if (leftBits.Length < rightBits.Length)
		{
			int num3 = rightBits.Length + 1;
			Span<uint> span3 = (((uint)num3 > 64u) ? ((Span<uint>)(array = ArrayPool<uint>.Shared.Rent(num3))) : stackalloc uint[64]).Slice(0, num3);
			BigIntegerCalculator.Add(rightBits, leftBits, span3);
			result = new BigInteger(span3, leftSign < 0);
		}
		else
		{
			int num4 = leftBits.Length + 1;
			Span<uint> span4 = (((uint)num4 > 64u) ? ((Span<uint>)(array = ArrayPool<uint>.Shared.Rent(num4))) : stackalloc uint[64]).Slice(0, num4);
			BigIntegerCalculator.Add(leftBits, rightBits, span4);
			result = new BigInteger(span4, leftSign < 0);
		}
		if (array != null)
		{
			ArrayPool<uint>.Shared.Return(array);
		}
		return result;
	}

	public static BigInteger operator -(BigInteger left, BigInteger right)
	{
		if (left._bits == null && right._bits == null)
		{
			return (long)left._sign - (long)right._sign;
		}
		if (left._sign < 0 != right._sign < 0)
		{
			return Add(left._bits, left._sign, right._bits, -1 * right._sign);
		}
		return Subtract(left._bits, left._sign, right._bits, right._sign);
	}

	private static BigInteger Subtract(ReadOnlySpan<uint> leftBits, int leftSign, ReadOnlySpan<uint> rightBits, int rightSign)
	{
		bool isEmpty = leftBits.IsEmpty;
		bool isEmpty2 = rightBits.IsEmpty;
		uint[] array = null;
		BigInteger result;
		if (isEmpty)
		{
			int length = rightBits.Length;
			Span<uint> span = ((length > 64) ? ((Span<uint>)(array = ArrayPool<uint>.Shared.Rent(length))) : stackalloc uint[64]).Slice(0, length);
			BigIntegerCalculator.Subtract(rightBits, NumericsHelpers.Abs(leftSign), span);
			result = new BigInteger(span, leftSign >= 0);
		}
		else if (isEmpty2)
		{
			int length2 = leftBits.Length;
			Span<uint> span2 = ((length2 > 64) ? ((Span<uint>)(array = ArrayPool<uint>.Shared.Rent(length2))) : stackalloc uint[64]).Slice(0, length2);
			BigIntegerCalculator.Subtract(leftBits, NumericsHelpers.Abs(rightSign), span2);
			result = new BigInteger(span2, leftSign < 0);
		}
		else if (BigIntegerCalculator.Compare(leftBits, rightBits) < 0)
		{
			int length3 = rightBits.Length;
			Span<uint> span3 = ((length3 > 64) ? ((Span<uint>)(array = ArrayPool<uint>.Shared.Rent(length3))) : stackalloc uint[64]).Slice(0, length3);
			BigIntegerCalculator.Subtract(rightBits, leftBits, span3);
			result = new BigInteger(span3, leftSign >= 0);
		}
		else
		{
			int length4 = leftBits.Length;
			Span<uint> span4 = ((length4 > 64) ? ((Span<uint>)(array = ArrayPool<uint>.Shared.Rent(length4))) : stackalloc uint[64]).Slice(0, length4);
			BigIntegerCalculator.Subtract(leftBits, rightBits, span4);
			result = new BigInteger(span4, leftSign < 0);
		}
		if (array != null)
		{
			ArrayPool<uint>.Shared.Return(array);
		}
		return result;
	}

	public static explicit operator byte(BigInteger value)
	{
		return checked((byte)(int)value);
	}

	public static explicit operator char(BigInteger value)
	{
		return (char)checked((ushort)(int)value);
	}

	public static explicit operator decimal(BigInteger value)
	{
		if (value._bits == null)
		{
			return value._sign;
		}
		int num = value._bits.Length;
		if (num > 3)
		{
			throw new OverflowException(System.SR.Overflow_Decimal);
		}
		int lo = 0;
		int mid = 0;
		int hi = 0;
		if (num > 2)
		{
			hi = (int)value._bits[2];
		}
		if (num > 1)
		{
			mid = (int)value._bits[1];
		}
		if (num > 0)
		{
			lo = (int)value._bits[0];
		}
		return new decimal(lo, mid, hi, value._sign < 0, 0);
	}

	public static explicit operator double(BigInteger value)
	{
		int sign = value._sign;
		uint[] bits = value._bits;
		if (bits == null)
		{
			return sign;
		}
		int num = bits.Length;
		if (num > 32)
		{
			if (sign == 1)
			{
				return double.PositiveInfinity;
			}
			return double.NegativeInfinity;
		}
		ulong num2 = bits[num - 1];
		ulong num3 = ((num > 1) ? bits[num - 2] : 0u);
		ulong num4 = ((num > 2) ? bits[num - 3] : 0u);
		int num5 = BitOperations.LeadingZeroCount((uint)num2);
		int exp = (num - 2) * 32 - num5;
		ulong man = (num2 << 32 + num5) | (num3 << num5) | (num4 >> 32 - num5);
		return NumericsHelpers.GetDoubleFromParts(sign, exp, man);
	}

	public static explicit operator Half(BigInteger value)
	{
		return (Half)(double)value;
	}

	public static explicit operator short(BigInteger value)
	{
		return checked((short)(int)value);
	}

	public static explicit operator int(BigInteger value)
	{
		if (value._bits == null)
		{
			return value._sign;
		}
		if (value._bits.Length > 1)
		{
			throw new OverflowException(System.SR.Overflow_Int32);
		}
		if (value._sign > 0)
		{
			return checked((int)value._bits[0]);
		}
		if (value._bits[0] > 2147483648u)
		{
			throw new OverflowException(System.SR.Overflow_Int32);
		}
		return (int)(0 - value._bits[0]);
	}

	public static explicit operator long(BigInteger value)
	{
		if (value._bits == null)
		{
			return value._sign;
		}
		int num = value._bits.Length;
		if (num > 2)
		{
			throw new OverflowException(System.SR.Overflow_Int64);
		}
		ulong num2 = ((num <= 1) ? value._bits[0] : NumericsHelpers.MakeUInt64(value._bits[1], value._bits[0]));
		long num3 = (long)((value._sign > 0) ? num2 : (0L - num2));
		if ((num3 > 0 && value._sign > 0) || (num3 < 0 && value._sign < 0))
		{
			return num3;
		}
		throw new OverflowException(System.SR.Overflow_Int64);
	}

	public static explicit operator Int128(BigInteger value)
	{
		if (value._bits == null)
		{
			return value._sign;
		}
		int num = value._bits.Length;
		if (num > 4)
		{
			throw new OverflowException(System.SR.Overflow_Int128);
		}
		UInt128 uInt = ((num > 2) ? new UInt128(NumericsHelpers.MakeUInt64((num > 3) ? value._bits[3] : 0u, value._bits[2]), NumericsHelpers.MakeUInt64(value._bits[1], value._bits[0])) : ((num <= 1) ? ((UInt128)value._bits[0]) : ((UInt128)NumericsHelpers.MakeUInt64(value._bits[1], value._bits[0]))));
		Int128 @int = ((value._sign > 0) ? ((Int128)uInt) : (-(Int128)uInt));
		if ((@int > 0 && value._sign > 0) || (@int < 0 && value._sign < 0))
		{
			return @int;
		}
		throw new OverflowException(System.SR.Overflow_Int128);
	}

	public static explicit operator nint(BigInteger value)
	{
		if (Environment.Is64BitProcess)
		{
			return (nint)(long)value;
		}
		return (int)value;
	}

	[CLSCompliant(false)]
	public static explicit operator sbyte(BigInteger value)
	{
		return checked((sbyte)(int)value);
	}

	public static explicit operator float(BigInteger value)
	{
		return (float)(double)value;
	}

	[CLSCompliant(false)]
	public static explicit operator ushort(BigInteger value)
	{
		return checked((ushort)(int)value);
	}

	[CLSCompliant(false)]
	public static explicit operator uint(BigInteger value)
	{
		if (value._bits == null)
		{
			return checked((uint)value._sign);
		}
		if (value._bits.Length > 1 || value._sign < 0)
		{
			throw new OverflowException(System.SR.Overflow_UInt32);
		}
		return value._bits[0];
	}

	[CLSCompliant(false)]
	public static explicit operator ulong(BigInteger value)
	{
		if (value._bits == null)
		{
			return checked((ulong)value._sign);
		}
		int num = value._bits.Length;
		if (num > 2 || value._sign < 0)
		{
			throw new OverflowException(System.SR.Overflow_UInt64);
		}
		if (num > 1)
		{
			return NumericsHelpers.MakeUInt64(value._bits[1], value._bits[0]);
		}
		return value._bits[0];
	}

	[CLSCompliant(false)]
	public static explicit operator UInt128(BigInteger value)
	{
		if (value._bits == null)
		{
			return checked((UInt128)value._sign);
		}
		int num = value._bits.Length;
		if (num > 4 || value._sign < 0)
		{
			throw new OverflowException(System.SR.Overflow_UInt128);
		}
		if (num > 2)
		{
			return new UInt128(NumericsHelpers.MakeUInt64((num > 3) ? value._bits[3] : 0u, value._bits[2]), NumericsHelpers.MakeUInt64(value._bits[1], value._bits[0]));
		}
		if (num > 1)
		{
			return NumericsHelpers.MakeUInt64(value._bits[1], value._bits[0]);
		}
		return value._bits[0];
	}

	[CLSCompliant(false)]
	public static explicit operator nuint(BigInteger value)
	{
		if (Environment.Is64BitProcess)
		{
			return (nuint)(ulong)value;
		}
		return (uint)value;
	}

	public static explicit operator BigInteger(decimal value)
	{
		return new BigInteger(value);
	}

	public static explicit operator BigInteger(double value)
	{
		return new BigInteger(value);
	}

	public static explicit operator BigInteger(Half value)
	{
		return new BigInteger((float)value);
	}

	public static explicit operator BigInteger(Complex value)
	{
		if (value.Imaginary != 0.0)
		{
			System.ThrowHelper.ThrowOverflowException();
		}
		return (BigInteger)value.Real;
	}

	public static explicit operator BigInteger(float value)
	{
		return new BigInteger(value);
	}

	public static implicit operator BigInteger(byte value)
	{
		return new BigInteger(value);
	}

	public static implicit operator BigInteger(char value)
	{
		return new BigInteger(value);
	}

	public static implicit operator BigInteger(short value)
	{
		return new BigInteger(value);
	}

	public static implicit operator BigInteger(int value)
	{
		return new BigInteger(value);
	}

	public static implicit operator BigInteger(long value)
	{
		return new BigInteger(value);
	}

	public static implicit operator BigInteger(Int128 value)
	{
		int n;
		uint[] rgu;
		if (int.MinValue < value && value <= int.MaxValue)
		{
			n = (int)value;
			rgu = null;
		}
		else
		{
			if (value == int.MinValue)
			{
				return s_bnMinInt;
			}
			UInt128 uInt;
			if (value < 0)
			{
				uInt = (UInt128)(-value);
				n = -1;
			}
			else
			{
				uInt = (UInt128)value;
				n = 1;
			}
			rgu = ((uInt <= uint.MaxValue) ? new uint[1] { (uint)(uInt >> 0) } : ((uInt <= ulong.MaxValue) ? new uint[2]
			{
				(uint)(uInt >> 0),
				(uint)(uInt >> 32)
			} : ((uInt <= new UInt128(4294967295uL, ulong.MaxValue)) ? new uint[3]
			{
				(uint)(uInt >> 0),
				(uint)(uInt >> 32),
				(uint)(uInt >> 64)
			} : new uint[4]
			{
				(uint)(uInt >> 0),
				(uint)(uInt >> 32),
				(uint)(uInt >> 64),
				(uint)(uInt >> 96)
			})));
		}
		return new BigInteger(n, rgu);
	}

	public static implicit operator BigInteger(nint value)
	{
		if (Environment.Is64BitProcess)
		{
			return new BigInteger(value);
		}
		return new BigInteger((int)value);
	}

	[CLSCompliant(false)]
	public static implicit operator BigInteger(sbyte value)
	{
		return new BigInteger(value);
	}

	[CLSCompliant(false)]
	public static implicit operator BigInteger(ushort value)
	{
		return new BigInteger(value);
	}

	[CLSCompliant(false)]
	public static implicit operator BigInteger(uint value)
	{
		return new BigInteger(value);
	}

	[CLSCompliant(false)]
	public static implicit operator BigInteger(ulong value)
	{
		return new BigInteger(value);
	}

	[CLSCompliant(false)]
	public static implicit operator BigInteger(UInt128 value)
	{
		int n = 1;
		uint[] rgu;
		if (!(value <= 2147483647u))
		{
			rgu = ((value <= uint.MaxValue) ? new uint[1] { (uint)(value >> 0) } : ((value <= ulong.MaxValue) ? new uint[2]
			{
				(uint)(value >> 0),
				(uint)(value >> 32)
			} : ((value <= new UInt128(4294967295uL, ulong.MaxValue)) ? new uint[3]
			{
				(uint)(value >> 0),
				(uint)(value >> 32),
				(uint)(value >> 64)
			} : new uint[4]
			{
				(uint)(value >> 0),
				(uint)(value >> 32),
				(uint)(value >> 64),
				(uint)(value >> 96)
			})));
		}
		else
		{
			n = (int)value;
			rgu = null;
		}
		return new BigInteger(n, rgu);
	}

	[CLSCompliant(false)]
	public static implicit operator BigInteger(nuint value)
	{
		if (Environment.Is64BitProcess)
		{
			return new BigInteger(value);
		}
		return new BigInteger((uint)value);
	}

	public static BigInteger operator &(BigInteger left, BigInteger right)
	{
		if (left.IsZero || right.IsZero)
		{
			return Zero;
		}
		if (left._bits == null && right._bits == null)
		{
			return left._sign & right._sign;
		}
		uint num = ((left._sign < 0) ? uint.MaxValue : 0u);
		uint num2 = ((right._sign < 0) ? uint.MaxValue : 0u);
		uint[] array = null;
		uint[]? bits = left._bits;
		int num3 = ((bits == null) ? 1 : bits.Length) + 1;
		Span<uint> buffer = (((uint)num3 > 64u) ? ((Span<uint>)(array = ArrayPool<uint>.Shared.Rent(num3))) : stackalloc uint[64]).Slice(0, num3);
		buffer = buffer.Slice(0, left.WriteTo(buffer));
		uint[] array2 = null;
		uint[]? bits2 = right._bits;
		num3 = ((bits2 == null) ? 1 : bits2.Length) + 1;
		Span<uint> buffer2 = (((uint)num3 > 64u) ? ((Span<uint>)(array2 = ArrayPool<uint>.Shared.Rent(num3))) : stackalloc uint[64]).Slice(0, num3);
		buffer2 = buffer2.Slice(0, right.WriteTo(buffer2));
		uint[] array3 = null;
		num3 = Math.Max(buffer.Length, buffer2.Length);
		Span<uint> value = ((num3 > 64) ? ((Span<uint>)(array3 = ArrayPool<uint>.Shared.Rent(num3))) : stackalloc uint[64]).Slice(0, num3);
		for (int i = 0; i < value.Length; i++)
		{
			uint num4 = (((uint)i < (uint)buffer.Length) ? buffer[i] : num);
			uint num5 = (((uint)i < (uint)buffer2.Length) ? buffer2[i] : num2);
			value[i] = num4 & num5;
		}
		if (array != null)
		{
			ArrayPool<uint>.Shared.Return(array);
		}
		if (array2 != null)
		{
			ArrayPool<uint>.Shared.Return(array2);
		}
		BigInteger result = new BigInteger(value);
		if (array3 != null)
		{
			ArrayPool<uint>.Shared.Return(array3);
		}
		return result;
	}

	public static BigInteger operator |(BigInteger left, BigInteger right)
	{
		if (left.IsZero)
		{
			return right;
		}
		if (right.IsZero)
		{
			return left;
		}
		if (left._bits == null && right._bits == null)
		{
			return left._sign | right._sign;
		}
		uint num = ((left._sign < 0) ? uint.MaxValue : 0u);
		uint num2 = ((right._sign < 0) ? uint.MaxValue : 0u);
		uint[] array = null;
		uint[]? bits = left._bits;
		int num3 = ((bits == null) ? 1 : bits.Length) + 1;
		Span<uint> buffer = (((uint)num3 > 64u) ? ((Span<uint>)(array = ArrayPool<uint>.Shared.Rent(num3))) : stackalloc uint[64]).Slice(0, num3);
		buffer = buffer.Slice(0, left.WriteTo(buffer));
		uint[] array2 = null;
		uint[]? bits2 = right._bits;
		num3 = ((bits2 == null) ? 1 : bits2.Length) + 1;
		Span<uint> buffer2 = (((uint)num3 > 64u) ? ((Span<uint>)(array2 = ArrayPool<uint>.Shared.Rent(num3))) : stackalloc uint[64]).Slice(0, num3);
		buffer2 = buffer2.Slice(0, right.WriteTo(buffer2));
		uint[] array3 = null;
		num3 = Math.Max(buffer.Length, buffer2.Length);
		Span<uint> value = ((num3 > 64) ? ((Span<uint>)(array3 = ArrayPool<uint>.Shared.Rent(num3))) : stackalloc uint[64]).Slice(0, num3);
		for (int i = 0; i < value.Length; i++)
		{
			uint num4 = (((uint)i < (uint)buffer.Length) ? buffer[i] : num);
			uint num5 = (((uint)i < (uint)buffer2.Length) ? buffer2[i] : num2);
			value[i] = num4 | num5;
		}
		if (array != null)
		{
			ArrayPool<uint>.Shared.Return(array);
		}
		if (array2 != null)
		{
			ArrayPool<uint>.Shared.Return(array2);
		}
		BigInteger result = new BigInteger(value);
		if (array3 != null)
		{
			ArrayPool<uint>.Shared.Return(array3);
		}
		return result;
	}

	public static BigInteger operator ^(BigInteger left, BigInteger right)
	{
		if (left._bits == null && right._bits == null)
		{
			return left._sign ^ right._sign;
		}
		uint num = ((left._sign < 0) ? uint.MaxValue : 0u);
		uint num2 = ((right._sign < 0) ? uint.MaxValue : 0u);
		uint[] array = null;
		uint[]? bits = left._bits;
		int num3 = ((bits == null) ? 1 : bits.Length) + 1;
		Span<uint> buffer = (((uint)num3 > 64u) ? ((Span<uint>)(array = ArrayPool<uint>.Shared.Rent(num3))) : stackalloc uint[64]).Slice(0, num3);
		buffer = buffer.Slice(0, left.WriteTo(buffer));
		uint[] array2 = null;
		uint[]? bits2 = right._bits;
		num3 = ((bits2 == null) ? 1 : bits2.Length) + 1;
		Span<uint> buffer2 = (((uint)num3 > 64u) ? ((Span<uint>)(array2 = ArrayPool<uint>.Shared.Rent(num3))) : stackalloc uint[64]).Slice(0, num3);
		buffer2 = buffer2.Slice(0, right.WriteTo(buffer2));
		uint[] array3 = null;
		num3 = Math.Max(buffer.Length, buffer2.Length);
		Span<uint> value = ((num3 > 64) ? ((Span<uint>)(array3 = ArrayPool<uint>.Shared.Rent(num3))) : stackalloc uint[64]).Slice(0, num3);
		for (int i = 0; i < value.Length; i++)
		{
			uint num4 = (((uint)i < (uint)buffer.Length) ? buffer[i] : num);
			uint num5 = (((uint)i < (uint)buffer2.Length) ? buffer2[i] : num2);
			value[i] = num4 ^ num5;
		}
		if (array != null)
		{
			ArrayPool<uint>.Shared.Return(array);
		}
		if (array2 != null)
		{
			ArrayPool<uint>.Shared.Return(array2);
		}
		BigInteger result = new BigInteger(value);
		if (array3 != null)
		{
			ArrayPool<uint>.Shared.Return(array3);
		}
		return result;
	}

	public static BigInteger operator <<(BigInteger value, int shift)
	{
		if (shift == 0)
		{
			return value;
		}
		if (shift == int.MinValue)
		{
			return value >> int.MaxValue >> 1;
		}
		if (shift < 0)
		{
			return value >> -shift;
		}
		(int Quotient, int Remainder) tuple = Math.DivRem(shift, 32);
		int item = tuple.Quotient;
		int item2 = tuple.Remainder;
		uint[] array = null;
		uint[]? bits = value._bits;
		int num = ((bits == null) ? 1 : bits.Length);
		Span<uint> xd = ((num > 64) ? ((Span<uint>)(array = ArrayPool<uint>.Shared.Rent(num))) : stackalloc uint[64]).Slice(0, num);
		bool partsForBitManipulation = value.GetPartsForBitManipulation(xd);
		int num2 = num + item + 1;
		uint[] array2 = null;
		Span<uint> span = (((uint)num2 > 64u) ? ((Span<uint>)(array2 = ArrayPool<uint>.Shared.Rent(num2))) : stackalloc uint[64]).Slice(0, num2);
		span.Clear();
		uint num3 = 0u;
		if (item2 == 0)
		{
			for (int i = 0; i < xd.Length; i++)
			{
				span[i + item] = xd[i];
			}
		}
		else
		{
			int num4 = 32 - item2;
			for (int j = 0; j < xd.Length; j++)
			{
				uint num5 = xd[j];
				span[j + item] = (num5 << item2) | num3;
				num3 = num5 >> num4;
			}
		}
		span[span.Length - 1] = num3;
		BigInteger result = new BigInteger(span, partsForBitManipulation);
		if (array != null)
		{
			ArrayPool<uint>.Shared.Return(array);
		}
		if (array2 != null)
		{
			ArrayPool<uint>.Shared.Return(array2);
		}
		return result;
	}

	public static BigInteger operator >>(BigInteger value, int shift)
	{
		if (shift == 0)
		{
			return value;
		}
		if (shift == int.MinValue)
		{
			return value << int.MaxValue << 1;
		}
		if (shift < 0)
		{
			return value << -shift;
		}
		(int Quotient, int Remainder) tuple = Math.DivRem(shift, 32);
		int item = tuple.Quotient;
		int item2 = tuple.Remainder;
		uint[] array = null;
		uint[]? bits = value._bits;
		int num = ((bits == null) ? 1 : bits.Length);
		Span<uint> span = ((num > 64) ? ((Span<uint>)(array = ArrayPool<uint>.Shared.Rent(num))) : stackalloc uint[64]).Slice(0, num);
		bool partsForBitManipulation = value.GetPartsForBitManipulation(span);
		bool flag = false;
		BigInteger result;
		if (partsForBitManipulation)
		{
			if (shift >= 32L * (long)span.Length)
			{
				result = MinusOne;
				goto IL_022d;
			}
			NumericsHelpers.DangerousMakeTwosComplement(span);
			flag = item2 == 0 && span[span.Length - 1] == 0;
		}
		uint[] array2 = null;
		int num2 = Math.Max(num - item, 0) + (flag ? 1 : 0);
		Span<uint> span2 = (((uint)num2 > 64u) ? ((Span<uint>)(array2 = ArrayPool<uint>.Shared.Rent(num2))) : stackalloc uint[64]).Slice(0, num2);
		span2.Clear();
		if (item2 == 0)
		{
			for (int num3 = span.Length - 1; num3 >= item; num3--)
			{
				span2[num3 - item] = span[num3];
			}
		}
		else
		{
			int num4 = 32 - item2;
			uint num5 = 0u;
			for (int num6 = span.Length - 1; num6 >= item; num6--)
			{
				uint num7 = span[num6];
				if (partsForBitManipulation && num6 == span.Length - 1)
				{
					span2[num6 - item] = (num7 >> item2) | (uint)(-1 << num4);
				}
				else
				{
					span2[num6 - item] = (num7 >> item2) | num5;
				}
				num5 = num7 << num4;
			}
		}
		if (partsForBitManipulation)
		{
			if (flag)
			{
				span2[span2.Length - 1] = uint.MaxValue;
			}
			NumericsHelpers.DangerousMakeTwosComplement(span2);
		}
		result = new BigInteger(span2, partsForBitManipulation);
		if (array2 != null)
		{
			ArrayPool<uint>.Shared.Return(array2);
		}
		goto IL_022d;
		IL_022d:
		if (array != null)
		{
			ArrayPool<uint>.Shared.Return(array);
		}
		return result;
	}

	public static BigInteger operator ~(BigInteger value)
	{
		return -(value + One);
	}

	public static BigInteger operator -(BigInteger value)
	{
		return new BigInteger(-value._sign, value._bits);
	}

	public static BigInteger operator +(BigInteger value)
	{
		return value;
	}

	public static BigInteger operator ++(BigInteger value)
	{
		return value + One;
	}

	public static BigInteger operator --(BigInteger value)
	{
		return value - One;
	}

	public static BigInteger operator +(BigInteger left, BigInteger right)
	{
		if (left._bits == null && right._bits == null)
		{
			return (long)left._sign + (long)right._sign;
		}
		if (left._sign < 0 != right._sign < 0)
		{
			return Subtract(left._bits, left._sign, right._bits, -1 * right._sign);
		}
		return Add(left._bits, left._sign, right._bits, right._sign);
	}

	public static BigInteger operator *(BigInteger left, BigInteger right)
	{
		if (left._bits == null && right._bits == null)
		{
			return (long)left._sign * (long)right._sign;
		}
		return Multiply(left._bits, left._sign, right._bits, right._sign);
	}

	private static BigInteger Multiply(ReadOnlySpan<uint> left, int leftSign, ReadOnlySpan<uint> right, int rightSign)
	{
		bool isEmpty = left.IsEmpty;
		bool isEmpty2 = right.IsEmpty;
		uint[] array = null;
		BigInteger result;
		if (isEmpty)
		{
			int num = right.Length + 1;
			Span<uint> span = (((uint)num > 64u) ? ((Span<uint>)(array = ArrayPool<uint>.Shared.Rent(num))) : stackalloc uint[64]).Slice(0, num);
			BigIntegerCalculator.Multiply(right, NumericsHelpers.Abs(leftSign), span);
			result = new BigInteger(span, (leftSign < 0) ^ (rightSign < 0));
		}
		else if (isEmpty2)
		{
			int num2 = left.Length + 1;
			Span<uint> span2 = (((uint)num2 > 64u) ? ((Span<uint>)(array = ArrayPool<uint>.Shared.Rent(num2))) : stackalloc uint[64]).Slice(0, num2);
			BigIntegerCalculator.Multiply(left, NumericsHelpers.Abs(rightSign), span2);
			result = new BigInteger(span2, (leftSign < 0) ^ (rightSign < 0));
		}
		else if (left == right)
		{
			int num3 = left.Length + right.Length;
			Span<uint> span3 = (((uint)num3 > 64u) ? ((Span<uint>)(array = ArrayPool<uint>.Shared.Rent(num3))) : stackalloc uint[64]).Slice(0, num3);
			BigIntegerCalculator.Square(left, span3);
			result = new BigInteger(span3, (leftSign < 0) ^ (rightSign < 0));
		}
		else if (left.Length < right.Length)
		{
			int num4 = left.Length + right.Length;
			Span<uint> span4 = (((uint)num4 > 64u) ? ((Span<uint>)(array = ArrayPool<uint>.Shared.Rent(num4))) : stackalloc uint[64]).Slice(0, num4);
			span4.Clear();
			BigIntegerCalculator.Multiply(right, left, span4);
			result = new BigInteger(span4, (leftSign < 0) ^ (rightSign < 0));
		}
		else
		{
			int num5 = left.Length + right.Length;
			Span<uint> span5 = (((uint)num5 > 64u) ? ((Span<uint>)(array = ArrayPool<uint>.Shared.Rent(num5))) : stackalloc uint[64]).Slice(0, num5);
			span5.Clear();
			BigIntegerCalculator.Multiply(left, right, span5);
			result = new BigInteger(span5, (leftSign < 0) ^ (rightSign < 0));
		}
		if (array != null)
		{
			ArrayPool<uint>.Shared.Return(array);
		}
		return result;
	}

	public static BigInteger operator /(BigInteger dividend, BigInteger divisor)
	{
		bool flag = dividend._bits == null;
		bool flag2 = divisor._bits == null;
		if (flag && flag2)
		{
			return dividend._sign / divisor._sign;
		}
		if (flag)
		{
			return s_bnZeroInt;
		}
		uint[] array = null;
		if (flag2)
		{
			int num = dividend._bits.Length;
			Span<uint> span = (((uint)num > 64u) ? ((Span<uint>)(array = ArrayPool<uint>.Shared.Rent(num))) : stackalloc uint[64]).Slice(0, num);
			try
			{
				BigIntegerCalculator.Divide(dividend._bits, NumericsHelpers.Abs(divisor._sign), span);
				return new BigInteger(span, (dividend._sign < 0) ^ (divisor._sign < 0));
			}
			finally
			{
				if (array != null)
				{
					ArrayPool<uint>.Shared.Return(array);
				}
			}
		}
		if (dividend._bits.Length < divisor._bits.Length)
		{
			return s_bnZeroInt;
		}
		int num2 = dividend._bits.Length - divisor._bits.Length + 1;
		Span<uint> span2 = (((uint)num2 >= 64u) ? ((Span<uint>)(array = ArrayPool<uint>.Shared.Rent(num2))) : stackalloc uint[64]).Slice(0, num2);
		BigIntegerCalculator.Divide(dividend._bits, divisor._bits, span2);
		BigInteger result = new BigInteger(span2, (dividend._sign < 0) ^ (divisor._sign < 0));
		if (array != null)
		{
			ArrayPool<uint>.Shared.Return(array);
		}
		return result;
	}

	public static BigInteger operator %(BigInteger dividend, BigInteger divisor)
	{
		bool flag = dividend._bits == null;
		bool flag2 = divisor._bits == null;
		if (flag && flag2)
		{
			return dividend._sign % divisor._sign;
		}
		if (flag)
		{
			return dividend;
		}
		if (flag2)
		{
			uint num = BigIntegerCalculator.Remainder(dividend._bits, NumericsHelpers.Abs(divisor._sign));
			return (dividend._sign < 0) ? (-1 * num) : num;
		}
		if (dividend._bits.Length < divisor._bits.Length)
		{
			return dividend;
		}
		uint[] array = null;
		int num2 = dividend._bits.Length;
		Span<uint> span = ((num2 > 64) ? ((Span<uint>)(array = ArrayPool<uint>.Shared.Rent(num2))) : stackalloc uint[64]).Slice(0, num2);
		BigIntegerCalculator.Remainder(dividend._bits, divisor._bits, span);
		BigInteger result = new BigInteger(span, dividend._sign < 0);
		if (array != null)
		{
			ArrayPool<uint>.Shared.Return(array);
		}
		return result;
	}

	public static bool operator <(BigInteger left, BigInteger right)
	{
		return left.CompareTo(right) < 0;
	}

	public static bool operator <=(BigInteger left, BigInteger right)
	{
		return left.CompareTo(right) <= 0;
	}

	public static bool operator >(BigInteger left, BigInteger right)
	{
		return left.CompareTo(right) > 0;
	}

	public static bool operator >=(BigInteger left, BigInteger right)
	{
		return left.CompareTo(right) >= 0;
	}

	public static bool operator ==(BigInteger left, BigInteger right)
	{
		return left.Equals(right);
	}

	public static bool operator !=(BigInteger left, BigInteger right)
	{
		return !left.Equals(right);
	}

	public static bool operator <(BigInteger left, long right)
	{
		return left.CompareTo(right) < 0;
	}

	public static bool operator <=(BigInteger left, long right)
	{
		return left.CompareTo(right) <= 0;
	}

	public static bool operator >(BigInteger left, long right)
	{
		return left.CompareTo(right) > 0;
	}

	public static bool operator >=(BigInteger left, long right)
	{
		return left.CompareTo(right) >= 0;
	}

	public static bool operator ==(BigInteger left, long right)
	{
		return left.Equals(right);
	}

	public static bool operator !=(BigInteger left, long right)
	{
		return !left.Equals(right);
	}

	public static bool operator <(long left, BigInteger right)
	{
		return right.CompareTo(left) > 0;
	}

	public static bool operator <=(long left, BigInteger right)
	{
		return right.CompareTo(left) >= 0;
	}

	public static bool operator >(long left, BigInteger right)
	{
		return right.CompareTo(left) < 0;
	}

	public static bool operator >=(long left, BigInteger right)
	{
		return right.CompareTo(left) <= 0;
	}

	public static bool operator ==(long left, BigInteger right)
	{
		return right.Equals(left);
	}

	public static bool operator !=(long left, BigInteger right)
	{
		return !right.Equals(left);
	}

	[CLSCompliant(false)]
	public static bool operator <(BigInteger left, ulong right)
	{
		return left.CompareTo(right) < 0;
	}

	[CLSCompliant(false)]
	public static bool operator <=(BigInteger left, ulong right)
	{
		return left.CompareTo(right) <= 0;
	}

	[CLSCompliant(false)]
	public static bool operator >(BigInteger left, ulong right)
	{
		return left.CompareTo(right) > 0;
	}

	[CLSCompliant(false)]
	public static bool operator >=(BigInteger left, ulong right)
	{
		return left.CompareTo(right) >= 0;
	}

	[CLSCompliant(false)]
	public static bool operator ==(BigInteger left, ulong right)
	{
		return left.Equals(right);
	}

	[CLSCompliant(false)]
	public static bool operator !=(BigInteger left, ulong right)
	{
		return !left.Equals(right);
	}

	[CLSCompliant(false)]
	public static bool operator <(ulong left, BigInteger right)
	{
		return right.CompareTo(left) > 0;
	}

	[CLSCompliant(false)]
	public static bool operator <=(ulong left, BigInteger right)
	{
		return right.CompareTo(left) >= 0;
	}

	[CLSCompliant(false)]
	public static bool operator >(ulong left, BigInteger right)
	{
		return right.CompareTo(left) < 0;
	}

	[CLSCompliant(false)]
	public static bool operator >=(ulong left, BigInteger right)
	{
		return right.CompareTo(left) <= 0;
	}

	[CLSCompliant(false)]
	public static bool operator ==(ulong left, BigInteger right)
	{
		return right.Equals(left);
	}

	[CLSCompliant(false)]
	public static bool operator !=(ulong left, BigInteger right)
	{
		return !right.Equals(left);
	}

	public long GetBitLength()
	{
		int sign = _sign;
		uint[] bits = _bits;
		int num;
		uint num2;
		if (bits == null)
		{
			num = 1;
			num2 = (uint)((sign < 0) ? (-sign) : sign);
		}
		else
		{
			num = bits.Length;
			num2 = bits[num - 1];
		}
		long num3 = (long)num * 32L - BitOperations.LeadingZeroCount(num2);
		if (sign >= 0)
		{
			return num3;
		}
		if ((num2 & (num2 - 1)) != 0)
		{
			return num3;
		}
		for (int num4 = num - 2; num4 >= 0; num4--)
		{
			if (bits[num4] != 0)
			{
				return num3;
			}
		}
		return num3 - 1;
	}

	private bool GetPartsForBitManipulation(Span<uint> xd)
	{
		if (_bits == null)
		{
			xd[0] = (uint)((_sign < 0) ? (-_sign) : _sign);
		}
		else
		{
			_bits.CopyTo(xd);
		}
		return _sign < 0;
	}

	internal static int GetDiffLength(uint[] rgu1, uint[] rgu2, int cu)
	{
		int num = cu;
		while (--num >= 0)
		{
			if (rgu1[num] != rgu2[num])
			{
				return num + 1;
			}
		}
		return 0;
	}

	public static (BigInteger Quotient, BigInteger Remainder) DivRem(BigInteger left, BigInteger right)
	{
		BigInteger remainder;
		BigInteger item = DivRem(left, right, out remainder);
		return (Quotient: item, Remainder: remainder);
	}

	public static BigInteger LeadingZeroCount(BigInteger value)
	{
		if (value._bits == null)
		{
			return int.LeadingZeroCount(value._sign);
		}
		return (value._sign >= 0) ? uint.LeadingZeroCount(value._bits[^1]) : 0u;
	}

	public static BigInteger PopCount(BigInteger value)
	{
		if (value._bits == null)
		{
			return int.PopCount(value._sign);
		}
		ulong num = 0uL;
		if (value._sign >= 0)
		{
			for (int i = 0; i < value._bits.Length; i++)
			{
				uint value2 = value._bits[i];
				num += uint.PopCount(value2);
			}
		}
		else
		{
			int j = 0;
			uint num2;
			do
			{
				num2 = ~value._bits[j] + 1;
				num += uint.PopCount(num2);
				j++;
			}
			while (num2 == 0 && j < value._bits.Length);
			for (; j < value._bits.Length; j++)
			{
				num2 = ~value._bits[j];
				num += uint.PopCount(num2);
			}
		}
		return num;
	}

	public static BigInteger RotateLeft(BigInteger value, int rotateAmount)
	{
		int num = ((value._bits == null) ? 4 : (value._bits.Length * 4));
		rotateAmount = (int)(rotateAmount % ((long)num * 8L));
		if (rotateAmount == 0)
		{
			return value;
		}
		if (rotateAmount == int.MinValue)
		{
			return RotateRight(RotateRight(value, int.MaxValue), 1);
		}
		if (rotateAmount < 0)
		{
			return RotateRight(value, -rotateAmount);
		}
		(int Quotient, int Remainder) tuple = Math.DivRem(rotateAmount, 32);
		int item = tuple.Quotient;
		int item2 = tuple.Remainder;
		uint[] array = null;
		uint[]? bits = value._bits;
		int num2 = ((bits == null) ? 1 : bits.Length);
		Span<uint> span = ((num2 > 64) ? ((Span<uint>)(array = ArrayPool<uint>.Shared.Rent(num2))) : stackalloc uint[64]);
		Span<uint> span2 = span;
		span2 = span2.Slice(0, num2);
		bool flag = value.GetPartsForBitManipulation(span2);
		int num3 = num2;
		uint[] array2 = null;
		Span<uint> span3 = ((num3 > 64) ? ((Span<uint>)(array2 = ArrayPool<uint>.Shared.Rent(num3))) : stackalloc uint[64]);
		Span<uint> span4 = span3;
		span4 = span4.Slice(0, num3);
		span4.Clear();
		if (flag)
		{
			NumericsHelpers.DangerousMakeTwosComplement(span2);
		}
		if (item2 == 0)
		{
			int num4 = 0;
			int num5 = span2.Length - item;
			do
			{
				span4[num4] = span2[num5];
				num4++;
				num5++;
			}
			while (num5 < span2.Length);
			num5 = 0;
			while (num4 < span4.Length)
			{
				span4[num4] = span2[num5];
				num4++;
				num5++;
			}
		}
		else
		{
			int num6 = 32 - item2;
			int num7 = 0;
			int num8 = 0;
			uint num9 = 0u;
			if (item == 0)
			{
				num9 = span2[span2.Length - 1] >> num6;
			}
			else
			{
				num8 = span2.Length - item;
				num9 = span2[num8 - 1] >> num6;
			}
			do
			{
				uint num10 = span2[num8];
				span4[num7] = (num10 << item2) | num9;
				num9 = num10 >> num6;
				num7++;
				num8++;
			}
			while (num8 < span2.Length);
			num8 = 0;
			while (num7 < span4.Length)
			{
				uint num11 = span2[num8];
				span4[num7] = (num11 << item2) | num9;
				num9 = num11 >> num6;
				num7++;
				num8++;
			}
		}
		if (flag)
		{
			if ((int)span4[span4.Length - 1] < 0)
			{
				NumericsHelpers.DangerousMakeTwosComplement(span4);
				goto IL_0284;
			}
		}
		flag = false;
		goto IL_0284;
		IL_0284:
		BigInteger result = new BigInteger(span4, flag);
		if (array != null)
		{
			ArrayPool<uint>.Shared.Return(array);
		}
		if (array2 != null)
		{
			ArrayPool<uint>.Shared.Return(array2);
		}
		return result;
	}

	public static BigInteger RotateRight(BigInteger value, int rotateAmount)
	{
		int num = ((value._bits == null) ? 4 : (value._bits.Length * 4));
		rotateAmount = (int)(rotateAmount % ((long)num * 8L));
		if (rotateAmount == 0)
		{
			return value;
		}
		if (rotateAmount == int.MinValue)
		{
			return RotateLeft(RotateLeft(value, int.MaxValue), 1);
		}
		if (rotateAmount < 0)
		{
			return RotateLeft(value, -rotateAmount);
		}
		(int Quotient, int Remainder) tuple = Math.DivRem(rotateAmount, 32);
		int item = tuple.Quotient;
		int item2 = tuple.Remainder;
		uint[] array = null;
		uint[]? bits = value._bits;
		int num2 = ((bits == null) ? 1 : bits.Length);
		Span<uint> span = ((num2 > 64) ? ((Span<uint>)(array = ArrayPool<uint>.Shared.Rent(num2))) : stackalloc uint[64]);
		Span<uint> span2 = span;
		span2 = span2.Slice(0, num2);
		bool flag = value.GetPartsForBitManipulation(span2);
		int num3 = num2;
		uint[] array2 = null;
		Span<uint> span3 = ((num3 > 64) ? ((Span<uint>)(array2 = ArrayPool<uint>.Shared.Rent(num3))) : stackalloc uint[64]);
		Span<uint> span4 = span3;
		span4 = span4.Slice(0, num3);
		span4.Clear();
		if (flag)
		{
			NumericsHelpers.DangerousMakeTwosComplement(span2);
		}
		if (item2 == 0)
		{
			int num4 = 0;
			int num5 = item;
			do
			{
				span4[num4] = span2[num5];
				num4++;
				num5++;
			}
			while (num5 < span2.Length);
			num5 = 0;
			while (num4 < span4.Length)
			{
				span4[num4] = span2[num5];
				num4++;
				num5++;
			}
		}
		else
		{
			int num6 = 32 - item2;
			int num7 = 0;
			int num8 = item;
			uint num9 = 0u;
			if (item == 0)
			{
				num9 = span2[span2.Length - 1] << num6;
			}
			else
			{
				num9 = span2[num8 - 1] << num6;
			}
			do
			{
				uint num10 = span2[num8];
				span4[num7] = (num10 >> item2) | num9;
				num9 = num10 << num6;
				num7++;
				num8++;
			}
			while (num8 < span2.Length);
			num8 = 0;
			while (num7 < span4.Length)
			{
				uint num11 = span2[num8];
				span4[num7] = (num11 >> item2) | num9;
				num9 = num11 << num6;
				num7++;
				num8++;
			}
		}
		if (flag)
		{
			if ((int)span4[span4.Length - 1] < 0)
			{
				NumericsHelpers.DangerousMakeTwosComplement(span4);
				goto IL_0271;
			}
		}
		flag = false;
		goto IL_0271;
		IL_0271:
		BigInteger result = new BigInteger(span4, flag);
		if (array != null)
		{
			ArrayPool<uint>.Shared.Return(array);
		}
		if (array2 != null)
		{
			ArrayPool<uint>.Shared.Return(array2);
		}
		return result;
	}

	public static BigInteger TrailingZeroCount(BigInteger value)
	{
		if (value._bits == null)
		{
			return int.TrailingZeroCount(value._sign);
		}
		ulong num = 0uL;
		uint num2 = value._bits[0];
		int num3 = 1;
		while (num2 == 0 && num3 < value._bits.Length)
		{
			num2 = value._bits[num3];
			num += 32;
			num3++;
		}
		num += uint.TrailingZeroCount(num2);
		return num;
	}

	static bool IBinaryInteger<BigInteger>.TryReadBigEndian(ReadOnlySpan<byte> source, bool isUnsigned, out BigInteger value)
	{
		value = new BigInteger(source, isUnsigned, isBigEndian: true);
		return true;
	}

	static bool IBinaryInteger<BigInteger>.TryReadLittleEndian(ReadOnlySpan<byte> source, bool isUnsigned, out BigInteger value)
	{
		value = new BigInteger(source, isUnsigned);
		return true;
	}

	int IBinaryInteger<BigInteger>.GetShortestBitLength()
	{
		uint[] bits = _bits;
		if (bits == null)
		{
			int sign = _sign;
			if (sign >= 0)
			{
				return 32 - BitOperations.LeadingZeroCount((uint)sign);
			}
			return 33 - BitOperations.LeadingZeroCount((uint)(~sign));
		}
		int num = (bits.Length - 1) * 32;
		if (_sign >= 0)
		{
			return num + (32 - BitOperations.LeadingZeroCount(bits[^1]));
		}
		uint num2 = ~bits[^1] + 1;
		for (int i = 0; i < bits.Length - 1; i++)
		{
			if (bits[i] != 0)
			{
				num2--;
				break;
			}
		}
		return num + (33 - BitOperations.LeadingZeroCount(~num2));
	}

	int IBinaryInteger<BigInteger>.GetByteCount()
	{
		return GetGenericMathByteCount();
	}

	bool IBinaryInteger<BigInteger>.TryWriteBigEndian(Span<byte> destination, out int bytesWritten)
	{
		uint[] bits = _bits;
		int genericMathByteCount = GetGenericMathByteCount();
		if (destination.Length >= genericMathByteCount)
		{
			if (bits == null)
			{
				int value = (BitConverter.IsLittleEndian ? BinaryPrimitives.ReverseEndianness(_sign) : _sign);
				Unsafe.WriteUnaligned(ref MemoryMarshal.GetReference(destination), value);
			}
			else if (_sign >= 0)
			{
				ref byte reference = ref Unsafe.Add(ref MemoryMarshal.GetReference(destination), (bits.Length - 1) * 4);
				for (int i = 0; i < bits.Length; i++)
				{
					uint value2 = bits[i];
					if (BitConverter.IsLittleEndian)
					{
						value2 = BinaryPrimitives.ReverseEndianness(value2);
					}
					Unsafe.WriteUnaligned(ref reference, value2);
					reference = ref Unsafe.Subtract(ref reference, 4);
				}
			}
			else
			{
				ref byte reference2 = ref MemoryMarshal.GetReference(destination);
				ref byte reference3 = ref Unsafe.Add(ref reference2, genericMathByteCount - 4);
				int j = 0;
				uint num;
				do
				{
					num = ~bits[j] + 1;
					if (BitConverter.IsLittleEndian)
					{
						num = BinaryPrimitives.ReverseEndianness(num);
					}
					Unsafe.WriteUnaligned(ref reference3, num);
					reference3 = ref Unsafe.Subtract(ref reference3, 4);
					j++;
				}
				while (num == 0 && j < bits.Length);
				for (; j < bits.Length; j++)
				{
					num = ~bits[j];
					if (BitConverter.IsLittleEndian)
					{
						num = BinaryPrimitives.ReverseEndianness(num);
					}
					Unsafe.WriteUnaligned(ref reference3, num);
					reference3 = ref Unsafe.Subtract(ref reference3, 4);
				}
				if (Unsafe.AreSame(ref reference3, ref reference2))
				{
					Unsafe.WriteUnaligned(ref reference3, uint.MaxValue);
				}
			}
			bytesWritten = genericMathByteCount;
			return true;
		}
		bytesWritten = 0;
		return false;
	}

	bool IBinaryInteger<BigInteger>.TryWriteLittleEndian(Span<byte> destination, out int bytesWritten)
	{
		uint[] bits = _bits;
		int genericMathByteCount = GetGenericMathByteCount();
		if (destination.Length >= genericMathByteCount)
		{
			if (bits == null)
			{
				int value = (BitConverter.IsLittleEndian ? _sign : BinaryPrimitives.ReverseEndianness(_sign));
				Unsafe.WriteUnaligned(ref MemoryMarshal.GetReference(destination), value);
			}
			else if (_sign >= 0)
			{
				ref byte reference = ref MemoryMarshal.GetReference(destination);
				for (int i = 0; i < bits.Length; i++)
				{
					uint value2 = bits[i];
					if (!BitConverter.IsLittleEndian)
					{
						value2 = BinaryPrimitives.ReverseEndianness(value2);
					}
					Unsafe.WriteUnaligned(ref reference, value2);
					reference = ref Unsafe.Add(ref reference, 4);
				}
			}
			else
			{
				ref byte reference2 = ref MemoryMarshal.GetReference(destination);
				ref byte right = ref Unsafe.Add(ref reference2, genericMathByteCount - 4);
				int j = 0;
				uint num;
				do
				{
					num = ~bits[j] + 1;
					if (!BitConverter.IsLittleEndian)
					{
						num = BinaryPrimitives.ReverseEndianness(num);
					}
					Unsafe.WriteUnaligned(ref reference2, num);
					reference2 = ref Unsafe.Add(ref reference2, 4);
					j++;
				}
				while (num == 0 && j < bits.Length);
				for (; j < bits.Length; j++)
				{
					num = ~bits[j];
					if (!BitConverter.IsLittleEndian)
					{
						num = BinaryPrimitives.ReverseEndianness(num);
					}
					Unsafe.WriteUnaligned(ref reference2, num);
					reference2 = ref Unsafe.Add(ref reference2, 4);
				}
				if (Unsafe.AreSame(ref reference2, ref right))
				{
					Unsafe.WriteUnaligned(ref reference2, uint.MaxValue);
				}
			}
			bytesWritten = genericMathByteCount;
			return true;
		}
		bytesWritten = 0;
		return false;
	}

	private int GetGenericMathByteCount()
	{
		uint[] bits = _bits;
		if (bits == null)
		{
			return 4;
		}
		int num = bits.Length * 4;
		if (_sign < 0)
		{
			uint num2 = ~bits[^1] + 1;
			for (int i = 0; i < bits.Length - 1; i++)
			{
				if (bits[i] != 0)
				{
					num2--;
					break;
				}
			}
			if ((int)num2 >= 0)
			{
				num += 4;
			}
		}
		return num;
	}

	public static bool IsPow2(BigInteger value)
	{
		return value.IsPowerOfTwo;
	}

	public static BigInteger Log2(BigInteger value)
	{
		if (IsNegative(value))
		{
			System.ThrowHelper.ThrowValueArgumentOutOfRange_NeedNonNegNumException();
		}
		if (value._bits == null)
		{
			return 0x1Fu ^ uint.LeadingZeroCount((uint)value._sign | 1u);
		}
		return (value._bits.Length * 32 - 1) ^ uint.LeadingZeroCount(value._bits[^1]);
	}

	public static BigInteger Clamp(BigInteger value, BigInteger min, BigInteger max)
	{
		if (min > max)
		{
			ThrowMinMaxException<BigInteger>(min, max);
		}
		if (value < min)
		{
			return min;
		}
		if (value > max)
		{
			return max;
		}
		return value;
		[DoesNotReturn]
		static void ThrowMinMaxException<T>(T min, T max)
		{
			throw new ArgumentException(System.SR.Format(System.SR.Argument_MinMaxValue, min, max));
		}
	}

	public static BigInteger CopySign(BigInteger value, BigInteger sign)
	{
		int num = value._sign;
		if (value._bits == null)
		{
			num = ((num >= 0) ? 1 : (-1));
		}
		int num2 = sign._sign;
		if (sign._bits == null)
		{
			num2 = ((num2 >= 0) ? 1 : (-1));
		}
		if (num != num2)
		{
			return -value;
		}
		return value;
	}

	static BigInteger INumber<BigInteger>.MaxNumber(BigInteger x, BigInteger y)
	{
		return Max(x, y);
	}

	static BigInteger INumber<BigInteger>.MinNumber(BigInteger x, BigInteger y)
	{
		return Min(x, y);
	}

	static int INumber<BigInteger>.Sign(BigInteger value)
	{
		if (value._bits == null)
		{
			return int.Sign(value._sign);
		}
		return value._sign;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static BigInteger CreateChecked<TOther>(TOther value) where TOther : INumberBase<TOther>
	{
		if (typeof(TOther) == typeof(BigInteger))
		{
			return (BigInteger)(object)value;
		}
		if (!TryConvertFromChecked(value, out var result) && !TOther.TryConvertToChecked<BigInteger>(value, out result))
		{
			System.ThrowHelper.ThrowNotSupportedException();
		}
		return result;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static BigInteger CreateSaturating<TOther>(TOther value) where TOther : INumberBase<TOther>
	{
		if (typeof(TOther) == typeof(BigInteger))
		{
			return (BigInteger)(object)value;
		}
		if (!TryConvertFromSaturating(value, out var result) && !TOther.TryConvertToSaturating<BigInteger>(value, out result))
		{
			System.ThrowHelper.ThrowNotSupportedException();
		}
		return result;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static BigInteger CreateTruncating<TOther>(TOther value) where TOther : INumberBase<TOther>
	{
		if (typeof(TOther) == typeof(BigInteger))
		{
			return (BigInteger)(object)value;
		}
		if (!TryConvertFromTruncating(value, out var result) && !TOther.TryConvertToTruncating<BigInteger>(value, out result))
		{
			System.ThrowHelper.ThrowNotSupportedException();
		}
		return result;
	}

	static bool INumberBase<BigInteger>.IsCanonical(BigInteger value)
	{
		return true;
	}

	static bool INumberBase<BigInteger>.IsComplexNumber(BigInteger value)
	{
		return false;
	}

	public static bool IsEvenInteger(BigInteger value)
	{
		if (value._bits == null)
		{
			return (value._sign & 1) == 0;
		}
		return (value._bits[0] & 1) == 0;
	}

	static bool INumberBase<BigInteger>.IsFinite(BigInteger value)
	{
		return true;
	}

	static bool INumberBase<BigInteger>.IsImaginaryNumber(BigInteger value)
	{
		return false;
	}

	static bool INumberBase<BigInteger>.IsInfinity(BigInteger value)
	{
		return false;
	}

	static bool INumberBase<BigInteger>.IsInteger(BigInteger value)
	{
		return true;
	}

	static bool INumberBase<BigInteger>.IsNaN(BigInteger value)
	{
		return false;
	}

	public static bool IsNegative(BigInteger value)
	{
		return value._sign < 0;
	}

	static bool INumberBase<BigInteger>.IsNegativeInfinity(BigInteger value)
	{
		return false;
	}

	static bool INumberBase<BigInteger>.IsNormal(BigInteger value)
	{
		return value != 0L;
	}

	public static bool IsOddInteger(BigInteger value)
	{
		if (value._bits == null)
		{
			return (value._sign & 1) != 0;
		}
		return (value._bits[0] & 1) != 0;
	}

	public static bool IsPositive(BigInteger value)
	{
		return value._sign >= 0;
	}

	static bool INumberBase<BigInteger>.IsPositiveInfinity(BigInteger value)
	{
		return false;
	}

	static bool INumberBase<BigInteger>.IsRealNumber(BigInteger value)
	{
		return true;
	}

	static bool INumberBase<BigInteger>.IsSubnormal(BigInteger value)
	{
		return false;
	}

	static bool INumberBase<BigInteger>.IsZero(BigInteger value)
	{
		return value._sign == 0;
	}

	public static BigInteger MaxMagnitude(BigInteger x, BigInteger y)
	{
		BigInteger bigInteger = Abs(x);
		BigInteger bigInteger2 = Abs(y);
		if (bigInteger > bigInteger2)
		{
			return x;
		}
		if (bigInteger == bigInteger2)
		{
			if (!IsNegative(x))
			{
				return x;
			}
			return y;
		}
		return y;
	}

	static BigInteger INumberBase<BigInteger>.MaxMagnitudeNumber(BigInteger x, BigInteger y)
	{
		return MaxMagnitude(x, y);
	}

	public static BigInteger MinMagnitude(BigInteger x, BigInteger y)
	{
		BigInteger bigInteger = Abs(x);
		BigInteger bigInteger2 = Abs(y);
		if (bigInteger < bigInteger2)
		{
			return x;
		}
		if (bigInteger == bigInteger2)
		{
			if (!IsNegative(x))
			{
				return y;
			}
			return x;
		}
		return y;
	}

	static BigInteger INumberBase<BigInteger>.MinMagnitudeNumber(BigInteger x, BigInteger y)
	{
		return MinMagnitude(x, y);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	static bool INumberBase<BigInteger>.TryConvertFromChecked<TOther>(TOther value, out BigInteger result)
	{
		return TryConvertFromChecked(value, out result);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static bool TryConvertFromChecked<TOther>(TOther value, out BigInteger result) where TOther : INumberBase<TOther>
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
			result = (BigInteger)num;
			return true;
		}
		if (typeof(TOther) == typeof(double))
		{
			double num2 = (double)(object)value;
			result = (BigInteger)num2;
			return true;
		}
		if (typeof(TOther) == typeof(Half))
		{
			Half half = (Half)(object)value;
			result = (BigInteger)half;
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
			result = @int;
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
			result = (BigInteger)num7;
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
			result = uInt;
			return true;
		}
		if (typeof(TOther) == typeof(nuint))
		{
			nuint num11 = (nuint)(object)value;
			result = num11;
			return true;
		}
		result = default(BigInteger);
		return false;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	static bool INumberBase<BigInteger>.TryConvertFromSaturating<TOther>(TOther value, out BigInteger result)
	{
		return TryConvertFromSaturating(value, out result);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static bool TryConvertFromSaturating<TOther>(TOther value, out BigInteger result) where TOther : INumberBase<TOther>
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
			result = (BigInteger)num;
			return true;
		}
		if (typeof(TOther) == typeof(double))
		{
			double num2 = (double)(object)value;
			result = (double.IsNaN(num2) ? Zero : ((BigInteger)num2));
			return true;
		}
		if (typeof(TOther) == typeof(Half))
		{
			Half half = (Half)(object)value;
			result = (Half.IsNaN(half) ? Zero : ((BigInteger)half));
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
			result = @int;
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
			result = (float.IsNaN(num7) ? Zero : ((BigInteger)num7));
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
			result = uInt;
			return true;
		}
		if (typeof(TOther) == typeof(nuint))
		{
			nuint num11 = (nuint)(object)value;
			result = num11;
			return true;
		}
		result = default(BigInteger);
		return false;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	static bool INumberBase<BigInteger>.TryConvertFromTruncating<TOther>(TOther value, out BigInteger result)
	{
		return TryConvertFromTruncating(value, out result);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static bool TryConvertFromTruncating<TOther>(TOther value, out BigInteger result) where TOther : INumberBase<TOther>
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
			result = (BigInteger)num;
			return true;
		}
		if (typeof(TOther) == typeof(double))
		{
			double num2 = (double)(object)value;
			result = (double.IsNaN(num2) ? Zero : ((BigInteger)num2));
			return true;
		}
		if (typeof(TOther) == typeof(Half))
		{
			Half half = (Half)(object)value;
			result = (Half.IsNaN(half) ? Zero : ((BigInteger)half));
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
			result = @int;
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
			result = (float.IsNaN(num7) ? Zero : ((BigInteger)num7));
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
			result = uInt;
			return true;
		}
		if (typeof(TOther) == typeof(nuint))
		{
			nuint num11 = (nuint)(object)value;
			result = num11;
			return true;
		}
		result = default(BigInteger);
		return false;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	static bool INumberBase<BigInteger>.TryConvertToChecked<TOther>(BigInteger value, [MaybeNullWhen(false)] out TOther result)
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
			decimal num = (decimal)value;
			result = (TOther)(object)num;
			return true;
		}
		if (typeof(TOther) == typeof(double))
		{
			double num2 = (double)value;
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
		if (typeof(TOther) == typeof(Complex))
		{
			Complex complex = (Complex)value;
			result = (TOther)(object)complex;
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
			float num7 = (float)value;
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

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	static bool INumberBase<BigInteger>.TryConvertToSaturating<TOther>(BigInteger value, [MaybeNullWhen(false)] out TOther result)
	{
		if (typeof(TOther) == typeof(byte))
		{
			byte b = (byte)((value._bits == null) ? ((value._sign >= 255) ? byte.MaxValue : ((value._sign > 0) ? ((byte)value._sign) : 0)) : ((!IsNegative(value)) ? byte.MaxValue : 0));
			result = (TOther)(object)b;
			return true;
		}
		if (typeof(TOther) == typeof(char))
		{
			char c = ((value._bits == null) ? ((value._sign >= 65535) ? '\uffff' : ((value._sign > 0) ? ((char)value._sign) : '\0')) : ((!IsNegative(value)) ? '\uffff' : '\0'));
			result = (TOther)(object)c;
			return true;
		}
		if (typeof(TOther) == typeof(decimal))
		{
			decimal num = ((value >= new Int128(4294967295uL, ulong.MaxValue)) ? decimal.MaxValue : ((value <= new Int128(18446744069414584320uL, 1uL)) ? decimal.MinValue : ((decimal)value)));
			result = (TOther)(object)num;
			return true;
		}
		if (typeof(TOther) == typeof(double))
		{
			double num2 = (double)value;
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
			short num3 = ((value._bits == null) ? ((value._sign >= 32767) ? short.MaxValue : ((value._sign <= -32768) ? short.MinValue : ((short)value._sign))) : (IsNegative(value) ? short.MinValue : short.MaxValue));
			result = (TOther)(object)num3;
			return true;
		}
		if (typeof(TOther) == typeof(int))
		{
			int num4 = ((value._bits == null) ? ((value._sign >= int.MaxValue) ? int.MaxValue : ((value._sign <= int.MinValue) ? int.MinValue : value._sign)) : (IsNegative(value) ? int.MinValue : int.MaxValue));
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
			Int128 @int = ((value >= Int128.MaxValue) ? Int128.MaxValue : ((value <= Int128.MinValue) ? Int128.MinValue : ((Int128)value)));
			result = (TOther)(object)@int;
			return true;
		}
		if (typeof(TOther) == typeof(nint))
		{
			nint num6 = ((value >= IntPtr.MaxValue) ? IntPtr.MaxValue : ((value <= IntPtr.MinValue) ? IntPtr.MinValue : ((nint)value)));
			result = (TOther)(object)num6;
			return true;
		}
		if (typeof(TOther) == typeof(Complex))
		{
			Complex complex = (Complex)value;
			result = (TOther)(object)complex;
			return true;
		}
		if (typeof(TOther) == typeof(sbyte))
		{
			sbyte b2 = ((value._bits == null) ? ((value._sign >= 127) ? sbyte.MaxValue : ((value._sign <= -128) ? sbyte.MinValue : ((sbyte)value._sign))) : (IsNegative(value) ? sbyte.MinValue : sbyte.MaxValue));
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
			ushort num8 = (ushort)((value._bits == null) ? ((value._sign >= 65535) ? ushort.MaxValue : ((value._sign > 0) ? ((ushort)value._sign) : 0)) : ((!IsNegative(value)) ? ushort.MaxValue : 0));
			result = (TOther)(object)num8;
			return true;
		}
		if (typeof(TOther) == typeof(uint))
		{
			uint num9 = ((value >= 4294967295L) ? uint.MaxValue : ((!IsNegative(value)) ? ((uint)value) : 0u));
			result = (TOther)(object)num9;
			return true;
		}
		if (typeof(TOther) == typeof(ulong))
		{
			ulong num10 = ((value >= ulong.MaxValue) ? ulong.MaxValue : (IsNegative(value) ? 0 : ((ulong)value)));
			result = (TOther)(object)num10;
			return true;
		}
		if (typeof(TOther) == typeof(UInt128))
		{
			UInt128 uInt = ((value >= UInt128.MaxValue) ? UInt128.MaxValue : (IsNegative(value) ? UInt128.MinValue : ((UInt128)value)));
			result = (TOther)(object)uInt;
			return true;
		}
		if (typeof(TOther) == typeof(nuint))
		{
			nuint num11 = ((value >= UIntPtr.MaxValue) ? UIntPtr.MaxValue : (IsNegative(value) ? UIntPtr.MinValue : ((nuint)value)));
			result = (TOther)(object)num11;
			return true;
		}
		result = default(TOther);
		return false;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	static bool INumberBase<BigInteger>.TryConvertToTruncating<TOther>(BigInteger value, [MaybeNullWhen(false)] out TOther result)
	{
		if (typeof(TOther) == typeof(byte))
		{
			byte b;
			if (value._bits != null)
			{
				uint num = value._bits[0];
				if (IsNegative(value))
				{
					num = ~num + 1;
				}
				b = (byte)num;
			}
			else
			{
				b = (byte)value._sign;
			}
			result = (TOther)(object)b;
			return true;
		}
		if (typeof(TOther) == typeof(char))
		{
			char c;
			if (value._bits != null)
			{
				uint num2 = value._bits[0];
				if (IsNegative(value))
				{
					num2 = ~num2 + 1;
				}
				c = (char)num2;
			}
			else
			{
				c = (char)value._sign;
			}
			result = (TOther)(object)c;
			return true;
		}
		if (typeof(TOther) == typeof(decimal))
		{
			decimal num3 = ((value >= new Int128(4294967295uL, ulong.MaxValue)) ? decimal.MaxValue : ((value <= new Int128(18446744069414584320uL, 1uL)) ? decimal.MinValue : ((decimal)value)));
			result = (TOther)(object)num3;
			return true;
		}
		if (typeof(TOther) == typeof(double))
		{
			double num4 = (double)value;
			result = (TOther)(object)num4;
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
			short num5 = ((value._bits == null) ? ((short)value._sign) : (IsNegative(value) ? ((short)(~value._bits[0] + 1)) : ((short)value._bits[0])));
			result = (TOther)(object)num5;
			return true;
		}
		if (typeof(TOther) == typeof(int))
		{
			int num6 = ((value._bits == null) ? value._sign : ((int)(IsNegative(value) ? (~value._bits[0] + 1) : value._bits[0])));
			result = (TOther)(object)num6;
			return true;
		}
		if (typeof(TOther) == typeof(long))
		{
			long num8;
			if (value._bits != null)
			{
				ulong num7 = 0uL;
				if (value._bits.Length >= 2)
				{
					num7 = value._bits[1];
					num7 <<= 32;
				}
				num7 |= value._bits[0];
				if (IsNegative(value))
				{
					num7 = ~num7 + 1;
				}
				num8 = (long)num7;
			}
			else
			{
				num8 = value._sign;
			}
			result = (TOther)(object)num8;
			return true;
		}
		if (typeof(TOther) == typeof(Int128))
		{
			Int128 @int;
			if (value._bits != null)
			{
				ulong num9 = 0uL;
				ulong num10 = 0uL;
				if (value._bits.Length >= 4)
				{
					num10 = value._bits[3];
					num10 <<= 32;
				}
				if (value._bits.Length >= 3)
				{
					num10 |= value._bits[2];
				}
				if (value._bits.Length >= 2)
				{
					num9 = value._bits[1];
					num9 <<= 32;
				}
				num9 |= value._bits[0];
				UInt128 uInt = new UInt128(num10, num9);
				if (IsNegative(value))
				{
					uInt = ~uInt + (byte)1;
				}
				@int = (Int128)uInt;
			}
			else
			{
				@int = value._sign;
			}
			result = (TOther)(object)@int;
			return true;
		}
		if (typeof(TOther) == typeof(nint))
		{
			nint num12;
			if (value._bits != null)
			{
				nuint num11 = 0u;
				if (Environment.Is64BitProcess && value._bits.Length >= 2)
				{
					num11 = value._bits[1];
					num11 <<= 32;
				}
				num11 |= value._bits[0];
				if (IsNegative(value))
				{
					num11 = ~num11 + 1;
				}
				num12 = (nint)num11;
			}
			else
			{
				num12 = value._sign;
			}
			result = (TOther)(object)num12;
			return true;
		}
		if (typeof(TOther) == typeof(Complex))
		{
			Complex complex = (Complex)value;
			result = (TOther)(object)complex;
			return true;
		}
		if (typeof(TOther) == typeof(sbyte))
		{
			sbyte b2 = ((value._bits == null) ? ((sbyte)value._sign) : (IsNegative(value) ? ((sbyte)(~value._bits[0] + 1)) : ((sbyte)value._bits[0])));
			result = (TOther)(object)b2;
			return true;
		}
		if (typeof(TOther) == typeof(float))
		{
			float num13 = (float)value;
			result = (TOther)(object)num13;
			return true;
		}
		if (typeof(TOther) == typeof(ushort))
		{
			ushort num15;
			if (value._bits != null)
			{
				uint num14 = value._bits[0];
				if (IsNegative(value))
				{
					num14 = ~num14 + 1;
				}
				num15 = (ushort)num14;
			}
			else
			{
				num15 = (ushort)value._sign;
			}
			result = (TOther)(object)num15;
			return true;
		}
		if (typeof(TOther) == typeof(uint))
		{
			uint num17;
			if (value._bits != null)
			{
				uint num16 = value._bits[0];
				if (IsNegative(value))
				{
					num16 = ~num16 + 1;
				}
				num17 = num16;
			}
			else
			{
				num17 = (uint)value._sign;
			}
			result = (TOther)(object)num17;
			return true;
		}
		if (typeof(TOther) == typeof(ulong))
		{
			ulong num19;
			if (value._bits != null)
			{
				ulong num18 = 0uL;
				if (value._bits.Length >= 2)
				{
					num18 = value._bits[1];
					num18 <<= 32;
				}
				num18 |= value._bits[0];
				if (IsNegative(value))
				{
					num18 = ~num18 + 1;
				}
				num19 = num18;
			}
			else
			{
				num19 = (ulong)value._sign;
			}
			result = (TOther)(object)num19;
			return true;
		}
		if (typeof(TOther) == typeof(UInt128))
		{
			UInt128 uInt3;
			if (value._bits != null)
			{
				ulong num20 = 0uL;
				ulong num21 = 0uL;
				if (value._bits.Length >= 4)
				{
					num21 = value._bits[3];
					num21 <<= 32;
				}
				if (value._bits.Length >= 3)
				{
					num21 |= value._bits[2];
				}
				if (value._bits.Length >= 2)
				{
					num20 = value._bits[1];
					num20 <<= 32;
				}
				num20 |= value._bits[0];
				UInt128 uInt2 = new UInt128(num21, num20);
				if (IsNegative(value))
				{
					uInt2 = ~uInt2 + (byte)1;
				}
				uInt3 = uInt2;
			}
			else
			{
				uInt3 = (UInt128)value._sign;
			}
			result = (TOther)(object)uInt3;
			return true;
		}
		if (typeof(TOther) == typeof(nuint))
		{
			nuint num23;
			if (value._bits != null)
			{
				nuint num22 = 0u;
				if (Environment.Is64BitProcess && value._bits.Length >= 2)
				{
					num22 = value._bits[1];
					num22 <<= 32;
				}
				num22 |= value._bits[0];
				if (IsNegative(value))
				{
					num22 = ~num22 + 1;
				}
				num23 = num22;
			}
			else
			{
				num23 = (nuint)value._sign;
			}
			result = (TOther)(object)num23;
			return true;
		}
		result = default(TOther);
		return false;
	}

	public static bool TryParse([NotNullWhen(true)] string? s, IFormatProvider? provider, out BigInteger result)
	{
		return TryParse(s, NumberStyles.Integer, provider, out result);
	}

	public static BigInteger operator >>>(BigInteger value, int shiftAmount)
	{
		if (shiftAmount == 0)
		{
			return value;
		}
		if (shiftAmount == int.MinValue)
		{
			return value << int.MaxValue << 1;
		}
		if (shiftAmount < 0)
		{
			return value << -shiftAmount;
		}
		(int Quotient, int Remainder) tuple = Math.DivRem(shiftAmount, 32);
		int item = tuple.Quotient;
		int item2 = tuple.Remainder;
		uint[] array = null;
		uint[]? bits = value._bits;
		int num = ((bits == null) ? 1 : bits.Length);
		Span<uint> span = ((num > 64) ? ((Span<uint>)(array = ArrayPool<uint>.Shared.Rent(num))) : stackalloc uint[64]).Slice(0, num);
		bool flag = value.GetPartsForBitManipulation(span);
		BigInteger result;
		if (flag)
		{
			if (shiftAmount >= 32L * (long)span.Length)
			{
				result = MinusOne;
				goto IL_01dc;
			}
			NumericsHelpers.DangerousMakeTwosComplement(span);
		}
		uint[] array2 = null;
		int num2 = Math.Max(num - item, 0);
		Span<uint> span2 = (((uint)num2 > 64u) ? ((Span<uint>)(array2 = ArrayPool<uint>.Shared.Rent(num2))) : stackalloc uint[64]).Slice(0, num2);
		span2.Clear();
		if (item2 == 0)
		{
			for (int num3 = span.Length - 1; num3 >= item; num3--)
			{
				span2[num3 - item] = span[num3];
			}
		}
		else
		{
			int num4 = 32 - item2;
			uint num5 = 0u;
			for (int num6 = span.Length - 1; num6 >= item; num6--)
			{
				uint num7 = span[num6];
				span2[num6 - item] = (num7 >> item2) | num5;
				num5 = num7 << num4;
			}
		}
		if (flag)
		{
			if ((int)span2[span2.Length - 1] < 0)
			{
				NumericsHelpers.DangerousMakeTwosComplement(span2);
				goto IL_01bb;
			}
		}
		flag = false;
		goto IL_01bb;
		IL_01dc:
		if (array != null)
		{
			ArrayPool<uint>.Shared.Return(array);
		}
		return result;
		IL_01bb:
		result = new BigInteger(span2, flag);
		if (array2 != null)
		{
			ArrayPool<uint>.Shared.Return(array2);
		}
		goto IL_01dc;
	}

	public static BigInteger Parse(ReadOnlySpan<char> s, IFormatProvider? provider)
	{
		return Parse(s, NumberStyles.Integer, provider);
	}

	public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, out BigInteger result)
	{
		return TryParse(s, NumberStyles.Integer, provider, out result);
	}
}
