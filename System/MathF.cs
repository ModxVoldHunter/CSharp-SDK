using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace System;

public static class MathF
{
	public const float E = 2.7182817f;

	public const float PI = 3.1415927f;

	public const float Tau = (float)Math.PI * 2f;

	private static ReadOnlySpan<float> RoundPower10Single => RuntimeHelpers.CreateSpan<float>((RuntimeFieldHandle)/*OpCode not supported: LdMemberToken*/);

	[MethodImpl(MethodImplOptions.InternalCall)]
	[Intrinsic]
	public static extern float Acos(float x);

	[MethodImpl(MethodImplOptions.InternalCall)]
	[Intrinsic]
	public static extern float Acosh(float x);

	[MethodImpl(MethodImplOptions.InternalCall)]
	[Intrinsic]
	public static extern float Asin(float x);

	[MethodImpl(MethodImplOptions.InternalCall)]
	[Intrinsic]
	public static extern float Asinh(float x);

	[MethodImpl(MethodImplOptions.InternalCall)]
	[Intrinsic]
	public static extern float Atan(float x);

	[MethodImpl(MethodImplOptions.InternalCall)]
	[Intrinsic]
	public static extern float Atanh(float x);

	[MethodImpl(MethodImplOptions.InternalCall)]
	[Intrinsic]
	public static extern float Atan2(float y, float x);

	[MethodImpl(MethodImplOptions.InternalCall)]
	[Intrinsic]
	public static extern float Cbrt(float x);

	[MethodImpl(MethodImplOptions.InternalCall)]
	[Intrinsic]
	public static extern float Ceiling(float x);

	[MethodImpl(MethodImplOptions.InternalCall)]
	[Intrinsic]
	public static extern float Cos(float x);

	[MethodImpl(MethodImplOptions.InternalCall)]
	[Intrinsic]
	public static extern float Cosh(float x);

	[MethodImpl(MethodImplOptions.InternalCall)]
	[Intrinsic]
	public static extern float Exp(float x);

	[MethodImpl(MethodImplOptions.InternalCall)]
	[Intrinsic]
	public static extern float Floor(float x);

	[MethodImpl(MethodImplOptions.InternalCall)]
	[Intrinsic]
	public static extern float FusedMultiplyAdd(float x, float y, float z);

	[MethodImpl(MethodImplOptions.InternalCall)]
	[Intrinsic]
	public static extern float Log(float x);

	[MethodImpl(MethodImplOptions.InternalCall)]
	[Intrinsic]
	public static extern float Log2(float x);

	[MethodImpl(MethodImplOptions.InternalCall)]
	[Intrinsic]
	public static extern float Log10(float x);

	[MethodImpl(MethodImplOptions.InternalCall)]
	[Intrinsic]
	public static extern float Pow(float x, float y);

	[MethodImpl(MethodImplOptions.InternalCall)]
	[Intrinsic]
	public static extern float Sin(float x);

	public unsafe static (float Sin, float Cos) SinCos(float x)
	{
		Unsafe.SkipInit(out float item);
		Unsafe.SkipInit(out float item2);
		SinCos(x, &item, &item2);
		return (Sin: item, Cos: item2);
	}

	[MethodImpl(MethodImplOptions.InternalCall)]
	[Intrinsic]
	public static extern float Sinh(float x);

	[MethodImpl(MethodImplOptions.InternalCall)]
	[Intrinsic]
	public static extern float Sqrt(float x);

	[MethodImpl(MethodImplOptions.InternalCall)]
	[Intrinsic]
	public static extern float Tan(float x);

	[MethodImpl(MethodImplOptions.InternalCall)]
	[Intrinsic]
	public static extern float Tanh(float x);

	[MethodImpl(MethodImplOptions.InternalCall)]
	private unsafe static extern float ModF(float x, float* intptr);

	[MethodImpl(MethodImplOptions.InternalCall)]
	private unsafe static extern void SinCos(float x, float* sin, float* cos);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static float Abs(float x)
	{
		return Math.Abs(x);
	}

	public static float BitDecrement(float x)
	{
		int num = BitConverter.SingleToInt32Bits(x);
		if ((num & 0x7F800000) >= 2139095040)
		{
			if (num != 2139095040)
			{
				return x;
			}
			return float.MaxValue;
		}
		if (num == 0)
		{
			return -1E-45f;
		}
		num += ((num < 0) ? 1 : (-1));
		return BitConverter.Int32BitsToSingle(num);
	}

	public static float BitIncrement(float x)
	{
		int num = BitConverter.SingleToInt32Bits(x);
		if ((num & 0x7F800000) >= 2139095040)
		{
			if (num != -8388608)
			{
				return x;
			}
			return float.MinValue;
		}
		if (num == int.MinValue)
		{
			return float.Epsilon;
		}
		num += ((num >= 0) ? 1 : (-1));
		return BitConverter.Int32BitsToSingle(num);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static float CopySign(float x, float y)
	{
		if (Sse.IsSupported ? true : false)
		{
			return VectorMath.ConditionalSelectBitwise(Vector128.CreateScalarUnsafe(-0f), Vector128.CreateScalarUnsafe(y), Vector128.CreateScalarUnsafe(x)).ToScalar();
		}
		return SoftwareFallback(x, y);
		static float SoftwareFallback(float x, float y)
		{
			int num = BitConverter.SingleToInt32Bits(x);
			int num2 = BitConverter.SingleToInt32Bits(y);
			num &= 0x7FFFFFFF;
			num2 &= int.MinValue;
			return BitConverter.Int32BitsToSingle(num | num2);
		}
	}

	public static float IEEERemainder(float x, float y)
	{
		if (float.IsNaN(x))
		{
			return x;
		}
		if (float.IsNaN(y))
		{
			return y;
		}
		float num = x % y;
		if (float.IsNaN(num))
		{
			return float.NaN;
		}
		if (num == 0f && float.IsNegative(x))
		{
			return -0f;
		}
		float num2 = num - Abs(y) * (float)Sign(x);
		if (Abs(num2) == Abs(num))
		{
			float x2 = x / y;
			float x3 = Round(x2);
			if (Abs(x3) > Abs(x2))
			{
				return num2;
			}
			return num;
		}
		if (Abs(num2) < Abs(num))
		{
			return num2;
		}
		return num;
	}

	public static int ILogB(float x)
	{
		if (float.IsNaN(x))
		{
			return int.MaxValue;
		}
		uint num = BitConverter.SingleToUInt32Bits(x);
		int num2 = (int)((num >> 23) & 0xFF);
		switch (num2)
		{
		case 0:
			num <<= 9;
			if (num == 0)
			{
				return int.MinValue;
			}
			num2 = -127;
			while (num >> 31 == 0)
			{
				num2--;
				num <<= 1;
			}
			return num2;
		case 255:
			if (num << 9 == 0)
			{
				return int.MaxValue;
			}
			return int.MinValue;
		default:
			return num2 - 127;
		}
	}

	public static float Log(float x, float y)
	{
		if (float.IsNaN(x))
		{
			return x;
		}
		if (float.IsNaN(y))
		{
			return y;
		}
		if (y == 1f)
		{
			return float.NaN;
		}
		if (x != 1f && (y == 0f || float.IsPositiveInfinity(y)))
		{
			return float.NaN;
		}
		return Log(x) / Log(y);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static float Max(float x, float y)
	{
		return Math.Max(x, y);
	}

	[Intrinsic]
	public static float MaxMagnitude(float x, float y)
	{
		float num = Abs(x);
		float num2 = Abs(y);
		if (num > num2 || float.IsNaN(num))
		{
			return x;
		}
		if (num == num2)
		{
			if (!float.IsNegative(x))
			{
				return x;
			}
			return y;
		}
		return y;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static float Min(float x, float y)
	{
		return Math.Min(x, y);
	}

	[Intrinsic]
	public static float MinMagnitude(float x, float y)
	{
		float num = Abs(x);
		float num2 = Abs(y);
		if (num < num2 || float.IsNaN(num))
		{
			return x;
		}
		if (num == num2)
		{
			if (!float.IsNegative(x))
			{
				return y;
			}
			return x;
		}
		return y;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static float ReciprocalEstimate(float x)
	{
		if (Sse.IsSupported)
		{
			return Sse.ReciprocalScalar(Vector128.CreateScalarUnsafe(x)).ToScalar();
		}
		if (false)
		{
		}
		return 1f / x;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static float ReciprocalSqrtEstimate(float x)
	{
		if (Sse.IsSupported)
		{
			return Sse.ReciprocalSqrtScalar(Vector128.CreateScalarUnsafe(x)).ToScalar();
		}
		if (false)
		{
		}
		return 1f / Sqrt(x);
	}

	[Intrinsic]
	public static float Round(float x)
	{
		uint num = BitConverter.SingleToUInt32Bits(x);
		byte b = float.ExtractBiasedExponentFromBits(num);
		if (b <= 126)
		{
			if (num << 1 == 0)
			{
				return x;
			}
			float x2 = ((b == 126 && float.ExtractTrailingSignificandFromBits(num) != 0) ? 1f : 0f);
			return CopySign(x2, x);
		}
		if (b >= 150)
		{
			return x;
		}
		uint num2 = (uint)(1 << 150 - b);
		uint num3 = num2 - 1;
		num += num2 >> 1;
		num = (((num & num3) != 0) ? (num & ~num3) : (num & ~num2));
		return BitConverter.UInt32BitsToSingle(num);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static float Round(float x, int digits)
	{
		return Round(x, digits, MidpointRounding.ToEven);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static float Round(float x, MidpointRounding mode)
	{
		if (RuntimeHelpers.IsKnownConstant((int)mode))
		{
			switch (mode)
			{
			case MidpointRounding.ToEven:
				return Round(x);
			case MidpointRounding.AwayFromZero:
				if (false)
				{
				}
				return Truncate(x + CopySign(0.49999997f, x));
			}
		}
		return Round(x, 0, mode);
	}

	public static float Round(float x, int digits, MidpointRounding mode)
	{
		if (digits < 0 || digits > 6)
		{
			throw new ArgumentOutOfRangeException("digits", SR.ArgumentOutOfRange_RoundingDigits_MathF);
		}
		if (mode < MidpointRounding.ToEven || mode > MidpointRounding.ToPositiveInfinity)
		{
			throw new ArgumentException(SR.Format(SR.Argument_InvalidEnumValue, mode, "MidpointRounding"), "mode");
		}
		if (Abs(x) < 100000000f)
		{
			float num = RoundPower10Single[digits];
			x *= num;
			x = mode switch
			{
				MidpointRounding.ToEven => Round(x), 
				MidpointRounding.AwayFromZero => Truncate(x + CopySign(0.49999997f, x)), 
				MidpointRounding.ToZero => Truncate(x), 
				MidpointRounding.ToNegativeInfinity => Floor(x), 
				MidpointRounding.ToPositiveInfinity => Ceiling(x), 
				_ => throw new ArgumentException(SR.Format(SR.Argument_InvalidEnumValue, mode, "MidpointRounding"), "mode"), 
			};
			x /= num;
		}
		return x;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static int Sign(float x)
	{
		return Math.Sign(x);
	}

	[Intrinsic]
	public unsafe static float Truncate(float x)
	{
		ModF(x, &x);
		return x;
	}

	public static float ScaleB(float x, int n)
	{
		float num = x;
		if (n > 127)
		{
			num *= 1.7014118E+38f;
			n -= 127;
			if (n > 127)
			{
				num *= 1.7014118E+38f;
				n -= 127;
				if (n > 127)
				{
					n = 127;
				}
			}
		}
		else if (n < -126)
		{
			num *= 1.9721523E-31f;
			n += 102;
			if (n < -126)
			{
				num *= 1.9721523E-31f;
				n += 102;
				if (n < -126)
				{
					n = -126;
				}
			}
		}
		float num2 = BitConverter.Int32BitsToSingle(127 + n << 23);
		return num * num2;
	}
}
