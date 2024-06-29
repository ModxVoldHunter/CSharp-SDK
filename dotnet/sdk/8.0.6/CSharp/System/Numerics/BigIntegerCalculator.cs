using System.Buffers;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System.Numerics;

internal static class BigIntegerCalculator
{
	private readonly ref struct FastReducer
	{
		private readonly ReadOnlySpan<uint> _modulus;

		private readonly ReadOnlySpan<uint> _mu;

		private readonly Span<uint> _q1;

		private readonly Span<uint> _q2;

		public FastReducer(ReadOnlySpan<uint> modulus, Span<uint> r, Span<uint> mu, Span<uint> q1, Span<uint> q2)
		{
			r[r.Length - 1] = 1u;
			Divide(r, modulus, mu);
			_modulus = modulus;
			_q1 = q1;
			_q2 = q2;
			_mu = mu.Slice(0, ActualLength(mu));
		}

		public int Reduce(Span<uint> value)
		{
			if (value.Length < _modulus.Length)
			{
				return value.Length;
			}
			_q1.Clear();
			int length = DivMul(value, _mu, _q1, _modulus.Length - 1);
			_q2.Clear();
			int length2 = DivMul(_q1.Slice(0, length), _modulus, _q2, _modulus.Length + 1);
			int num = SubMod(value, _q2.Slice(0, length2), _modulus, _modulus.Length + 1);
			value = value.Slice(num);
			value.Clear();
			return num;
		}

		private static int DivMul(ReadOnlySpan<uint> left, ReadOnlySpan<uint> right, Span<uint> bits, int k)
		{
			if (left.Length > k)
			{
				left = left.Slice(k);
				if (left.Length < right.Length)
				{
					Multiply(right, left, bits.Slice(0, left.Length + right.Length));
				}
				else
				{
					Multiply(left, right, bits.Slice(0, left.Length + right.Length));
				}
				return ActualLength(bits.Slice(0, left.Length + right.Length));
			}
			return 0;
		}

		private static int SubMod(Span<uint> left, ReadOnlySpan<uint> right, ReadOnlySpan<uint> modulus, int k)
		{
			if (left.Length > k)
			{
				left = left.Slice(0, k);
			}
			if (right.Length > k)
			{
				right = right.Slice(0, k);
			}
			SubtractSelf(left, right);
			left = left.Slice(0, ActualLength(left));
			while (Compare(left, modulus) >= 0)
			{
				SubtractSelf(left, modulus);
				left = left.Slice(0, ActualLength(left));
			}
			return left.Length;
		}
	}

	internal const int StackAllocThreshold = 64;

	private static void CopyTail(ReadOnlySpan<uint> source, Span<uint> dest, int start)
	{
		source.Slice(start).CopyTo(dest.Slice(start));
	}

	public static void Add(ReadOnlySpan<uint> left, uint right, Span<uint> bits)
	{
		Add(left, bits, ref MemoryMarshal.GetReference(bits), 0, right);
	}

	public static void Add(ReadOnlySpan<uint> left, ReadOnlySpan<uint> right, Span<uint> bits)
	{
		ref uint reference = ref MemoryMarshal.GetReference(bits);
		ref uint reference2 = ref MemoryMarshal.GetReference(right);
		ref uint reference3 = ref MemoryMarshal.GetReference(left);
		int num = 0;
		long num2 = 0L;
		do
		{
			num2 += Unsafe.Add(ref reference3, num);
			num2 += Unsafe.Add(ref reference2, num);
			Unsafe.Add(ref reference, num) = (uint)num2;
			num2 >>= 32;
			num++;
		}
		while (num < right.Length);
		Add(left, bits, ref reference, num, num2);
	}

	private static void AddSelf(Span<uint> left, ReadOnlySpan<uint> right)
	{
		int i = 0;
		long num = 0L;
		ref uint reference = ref MemoryMarshal.GetReference(left);
		for (; i < right.Length; i++)
		{
			long num2 = Unsafe.Add(ref reference, i) + num + right[i];
			Unsafe.Add(ref reference, i) = (uint)num2;
			num = num2 >> 32;
		}
		while (num != 0L && i < left.Length)
		{
			long num3 = left[i] + num;
			left[i] = (uint)num3;
			num = num3 >> 32;
			i++;
		}
	}

	public static void Subtract(ReadOnlySpan<uint> left, uint right, Span<uint> bits)
	{
		Subtract(left, bits, ref MemoryMarshal.GetReference(bits), 0, 0L - (long)right);
	}

	public static void Subtract(ReadOnlySpan<uint> left, ReadOnlySpan<uint> right, Span<uint> bits)
	{
		ref uint reference = ref MemoryMarshal.GetReference(bits);
		ref uint reference2 = ref MemoryMarshal.GetReference(right);
		ref uint reference3 = ref MemoryMarshal.GetReference(left);
		int num = 0;
		long num2 = 0L;
		do
		{
			num2 += Unsafe.Add(ref reference3, num);
			num2 -= Unsafe.Add(ref reference2, num);
			Unsafe.Add(ref reference, num) = (uint)num2;
			num2 >>= 32;
			num++;
		}
		while (num < right.Length);
		Subtract(left, bits, ref reference, num, num2);
	}

	private static void SubtractSelf(Span<uint> left, ReadOnlySpan<uint> right)
	{
		int i = 0;
		long num = 0L;
		ref uint reference = ref MemoryMarshal.GetReference(left);
		for (; i < right.Length; i++)
		{
			long num2 = Unsafe.Add(ref reference, i) + num - right[i];
			Unsafe.Add(ref reference, i) = (uint)num2;
			num = num2 >> 32;
		}
		while (num != 0L && i < left.Length)
		{
			long num3 = left[i] + num;
			left[i] = (uint)num3;
			num = num3 >> 32;
			i++;
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static void Add(ReadOnlySpan<uint> left, Span<uint> bits, ref uint resultPtr, int startIndex, long initialCarry)
	{
		int i = startIndex;
		long num = initialCarry;
		if (left.Length <= 8)
		{
			for (; i < left.Length; i++)
			{
				num += left[i];
				Unsafe.Add(ref resultPtr, i) = (uint)num;
				num >>= 32;
			}
			Unsafe.Add(ref resultPtr, left.Length) = (uint)num;
			return;
		}
		while (i < left.Length)
		{
			num += left[i];
			Unsafe.Add(ref resultPtr, i) = (uint)num;
			i++;
			num >>= 32;
			if (num == 0L)
			{
				break;
			}
		}
		Unsafe.Add(ref resultPtr, left.Length) = (uint)num;
		if (i < left.Length)
		{
			CopyTail(left, bits, i);
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static void Subtract(ReadOnlySpan<uint> left, Span<uint> bits, ref uint resultPtr, int startIndex, long initialCarry)
	{
		int i = startIndex;
		long num = initialCarry;
		if (left.Length <= 8)
		{
			for (; i < left.Length; i++)
			{
				num += left[i];
				Unsafe.Add(ref resultPtr, i) = (uint)num;
				num >>= 32;
			}
			return;
		}
		while (i < left.Length)
		{
			num += left[i];
			Unsafe.Add(ref resultPtr, i) = (uint)num;
			i++;
			num >>= 32;
			if (num == 0L)
			{
				break;
			}
		}
		if (i < left.Length)
		{
			CopyTail(left, bits, i);
		}
	}

	public static void Divide(ReadOnlySpan<uint> left, uint right, Span<uint> quotient, out uint remainder)
	{
		ulong num = 0uL;
		for (int num2 = left.Length - 1; num2 >= 0; num2--)
		{
			ulong num3 = (num << 32) | left[num2];
			ulong num4 = num3 / right;
			quotient[num2] = (uint)num4;
			num = num3 - num4 * right;
		}
		remainder = (uint)num;
	}

	public static void Divide(ReadOnlySpan<uint> left, uint right, Span<uint> quotient)
	{
		ulong num = 0uL;
		for (int num2 = left.Length - 1; num2 >= 0; num2--)
		{
			ulong num3 = (num << 32) | left[num2];
			ulong num4 = num3 / right;
			quotient[num2] = (uint)num4;
			num = num3 - num4 * right;
		}
	}

	public static uint Remainder(ReadOnlySpan<uint> left, uint right)
	{
		ulong num = 0uL;
		for (int num2 = left.Length - 1; num2 >= 0; num2--)
		{
			ulong num3 = (num << 32) | left[num2];
			num = num3 % right;
		}
		return (uint)num;
	}

	public static void Divide(ReadOnlySpan<uint> left, ReadOnlySpan<uint> right, Span<uint> quotient, Span<uint> remainder)
	{
		left.CopyTo(remainder);
		Divide(remainder, right, quotient);
	}

	public static void Divide(ReadOnlySpan<uint> left, ReadOnlySpan<uint> right, Span<uint> quotient)
	{
		uint[] array = null;
		Span<uint> span = ((left.Length > 64) ? ((Span<uint>)(array = ArrayPool<uint>.Shared.Rent(left.Length))) : stackalloc uint[64]).Slice(0, left.Length);
		left.CopyTo(span);
		Divide(span, right, quotient);
		if (array != null)
		{
			ArrayPool<uint>.Shared.Return(array);
		}
	}

	public static void Remainder(ReadOnlySpan<uint> left, ReadOnlySpan<uint> right, Span<uint> remainder)
	{
		left.CopyTo(remainder);
		Divide(remainder, right, default(Span<uint>));
	}

	private static void Divide(Span<uint> left, ReadOnlySpan<uint> right, Span<uint> bits)
	{
		uint num = right[right.Length - 1];
		uint num2 = ((right.Length > 1) ? right[right.Length - 2] : 0u);
		int num3 = BitOperations.LeadingZeroCount(num);
		int num4 = 32 - num3;
		if (num3 > 0)
		{
			uint num5 = ((right.Length > 2) ? right[right.Length - 3] : 0u);
			num = (num << num3) | (num2 >> num4);
			num2 = (num2 << num3) | (num5 >> num4);
		}
		for (int num6 = left.Length; num6 >= right.Length; num6--)
		{
			int num7 = num6 - right.Length;
			uint num8 = (((uint)num6 < (uint)left.Length) ? left[num6] : 0u);
			ulong num9 = ((ulong)num8 << 32) | left[num6 - 1];
			uint num10 = ((num6 > 1) ? left[num6 - 2] : 0u);
			if (num3 > 0)
			{
				uint num11 = ((num6 > 2) ? left[num6 - 3] : 0u);
				num9 = (num9 << num3) | (num10 >> num4);
				num10 = (num10 << num3) | (num11 >> num4);
			}
			ulong num12 = num9 / num;
			if (num12 > uint.MaxValue)
			{
				num12 = 4294967295uL;
			}
			while (DivideGuessTooBig(num12, num9, num10, num, num2))
			{
				num12--;
			}
			if (num12 != 0)
			{
				uint num13 = SubtractDivisor(left.Slice(num7), right, num12);
				if (num13 != num8)
				{
					num13 = AddDivisor(left.Slice(num7), right);
					num12--;
				}
			}
			if ((uint)num7 < (uint)bits.Length)
			{
				bits[num7] = (uint)num12;
			}
			if ((uint)num6 < (uint)left.Length)
			{
				left[num6] = 0u;
			}
		}
	}

	private static uint AddDivisor(Span<uint> left, ReadOnlySpan<uint> right)
	{
		ulong num = 0uL;
		for (int i = 0; i < right.Length; i++)
		{
			ref uint reference = ref left[i];
			ulong num2 = reference + num + right[i];
			reference = (uint)num2;
			num = num2 >> 32;
		}
		return (uint)num;
	}

	private static uint SubtractDivisor(Span<uint> left, ReadOnlySpan<uint> right, ulong q)
	{
		ulong num = 0uL;
		for (int i = 0; i < right.Length; i++)
		{
			num += right[i] * q;
			uint num2 = (uint)num;
			num >>= 32;
			ref uint reference = ref left[i];
			if (reference < num2)
			{
				num++;
			}
			reference -= num2;
		}
		return (uint)num;
	}

	private static bool DivideGuessTooBig(ulong q, ulong valHi, uint valLo, uint divHi, uint divLo)
	{
		ulong num = divHi * q;
		ulong num2 = divLo * q;
		num += num2 >> 32;
		num2 &= 0xFFFFFFFFu;
		if (num < valHi)
		{
			return false;
		}
		if (num > valHi)
		{
			return true;
		}
		if (num2 < valLo)
		{
			return false;
		}
		if (num2 > valLo)
		{
			return true;
		}
		return false;
	}

	public static uint Gcd(uint left, uint right)
	{
		while (right != 0)
		{
			uint num = left % right;
			left = right;
			right = num;
		}
		return left;
	}

	public static ulong Gcd(ulong left, ulong right)
	{
		while (right > uint.MaxValue)
		{
			ulong num = left % right;
			left = right;
			right = num;
		}
		if (right != 0L)
		{
			return Gcd((uint)right, (uint)(left % right));
		}
		return left;
	}

	public static uint Gcd(ReadOnlySpan<uint> left, uint right)
	{
		uint right2 = Remainder(left, right);
		return Gcd(right, right2);
	}

	public static void Gcd(ReadOnlySpan<uint> left, ReadOnlySpan<uint> right, Span<uint> result)
	{
		left.CopyTo(result);
		uint[] array = null;
		Span<uint> span = ((right.Length > 64) ? ((Span<uint>)(array = ArrayPool<uint>.Shared.Rent(right.Length))) : stackalloc uint[64]).Slice(0, right.Length);
		right.CopyTo(span);
		Gcd(result, span);
		if (array != null)
		{
			ArrayPool<uint>.Shared.Return(array);
		}
	}

	private static void Gcd(Span<uint> left, Span<uint> right)
	{
		Span<uint> destination = left;
		while (right.Length > 2)
		{
			ExtractDigits(left, right, out var x, out var y);
			uint num = 1u;
			uint num2 = 0u;
			uint num3 = 0u;
			uint num4 = 1u;
			int num5 = 0;
			while (y != 0L)
			{
				ulong num6 = x / y;
				if (num6 > uint.MaxValue)
				{
					break;
				}
				ulong num7 = num + num6 * num3;
				ulong num8 = num2 + num6 * num4;
				ulong num9 = x - num6 * y;
				if (num7 > int.MaxValue || num8 > int.MaxValue || num9 < num8 || num9 + num7 > y - num3)
				{
					break;
				}
				num = (uint)num7;
				num2 = (uint)num8;
				x = num9;
				num5++;
				if (x == num2)
				{
					break;
				}
				num6 = y / x;
				if (num6 > uint.MaxValue)
				{
					break;
				}
				num7 = num4 + num6 * num2;
				num8 = num3 + num6 * num;
				num9 = y - num6 * x;
				if (num7 > int.MaxValue || num8 > int.MaxValue || num9 < num8 || num9 + num7 > x - num2)
				{
					break;
				}
				num4 = (uint)num7;
				num3 = (uint)num8;
				y = num9;
				num5++;
				if (y == num3)
				{
					break;
				}
			}
			if (num2 == 0)
			{
				left = left.Slice(0, Reduce(left, right));
				Span<uint> span = left;
				left = right;
				right = span;
				continue;
			}
			int maxLength = LehmerCore(left, right, num, num2, num3, num4);
			left = left.Slice(0, Refresh(left, maxLength));
			right = right.Slice(0, Refresh(right, maxLength));
			if (num5 % 2 == 1)
			{
				Span<uint> span2 = left;
				left = right;
				right = span2;
			}
		}
		if (right.Length > 0)
		{
			Reduce(left, right);
			ulong num10 = right[0];
			ulong num11 = left[0];
			if (right.Length > 1)
			{
				num10 |= (ulong)right[1] << 32;
				num11 |= (ulong)left[1] << 32;
			}
			left = left.Slice(0, Overwrite(left, Gcd(num10, num11)));
			right.Clear();
		}
		left.CopyTo(destination);
	}

	private static int Overwrite(Span<uint> buffer, ulong value)
	{
		if (buffer.Length > 2)
		{
			buffer.Slice(2).Clear();
		}
		uint num = (uint)value;
		uint num2 = (uint)(value >> 32);
		buffer[1] = num2;
		buffer[0] = num;
		if (num2 == 0)
		{
			return (num != 0) ? 1 : 0;
		}
		return 2;
	}

	private static void ExtractDigits(ReadOnlySpan<uint> xBuffer, ReadOnlySpan<uint> yBuffer, out ulong x, out ulong y)
	{
		ulong num = xBuffer[xBuffer.Length - 1];
		ulong num2 = xBuffer[xBuffer.Length - 2];
		ulong num3 = xBuffer[xBuffer.Length - 3];
		ulong num4;
		ulong num5;
		ulong num6;
		switch (xBuffer.Length - yBuffer.Length)
		{
		case 0:
			num4 = yBuffer[yBuffer.Length - 1];
			num5 = yBuffer[yBuffer.Length - 2];
			num6 = yBuffer[yBuffer.Length - 3];
			break;
		case 1:
			num4 = 0uL;
			num5 = yBuffer[yBuffer.Length - 1];
			num6 = yBuffer[yBuffer.Length - 2];
			break;
		case 2:
			num4 = 0uL;
			num5 = 0uL;
			num6 = yBuffer[yBuffer.Length - 1];
			break;
		default:
			num4 = 0uL;
			num5 = 0uL;
			num6 = 0uL;
			break;
		}
		int num7 = BitOperations.LeadingZeroCount((uint)num);
		x = ((num << 32 + num7) | (num2 << num7) | (num3 >> 32 - num7)) >> 1;
		y = ((num4 << 32 + num7) | (num5 << num7) | (num6 >> 32 - num7)) >> 1;
	}

	private static int LehmerCore(Span<uint> x, Span<uint> y, long a, long b, long c, long d)
	{
		int length = y.Length;
		long num = 0L;
		long num2 = 0L;
		for (int i = 0; i < length; i++)
		{
			long num3 = a * x[i] - b * y[i] + num;
			long num4 = d * y[i] - c * x[i] + num2;
			num = num3 >> 32;
			num2 = num4 >> 32;
			x[i] = (uint)num3;
			y[i] = (uint)num4;
		}
		return length;
	}

	private static int Refresh(Span<uint> bits, int maxLength)
	{
		if (bits.Length > maxLength)
		{
			bits.Slice(maxLength).Clear();
		}
		return ActualLength(bits.Slice(0, maxLength));
	}

	public static void Pow(uint value, uint power, Span<uint> bits)
	{
		Pow((value != 0) ? new ReadOnlySpan<uint>(ref value) : default(ReadOnlySpan<uint>), power, bits);
	}

	public static void Pow(ReadOnlySpan<uint> value, uint power, Span<uint> bits)
	{
		uint[] array = null;
		Span<uint> temp = ((bits.Length > 64) ? ((Span<uint>)(array = ArrayPool<uint>.Shared.Rent(bits.Length))) : stackalloc uint[64]).Slice(0, bits.Length);
		temp.Clear();
		uint[] array2 = null;
		Span<uint> span = ((bits.Length > 64) ? ((Span<uint>)(array2 = ArrayPool<uint>.Shared.Rent(bits.Length))) : stackalloc uint[64]).Slice(0, bits.Length);
		value.CopyTo(span);
		span.Slice(value.Length).Clear();
		Span<uint> span2 = PowCore(span, value.Length, temp, power, bits);
		span2.CopyTo(bits);
		bits.Slice(span2.Length).Clear();
		if (array != null)
		{
			ArrayPool<uint>.Shared.Return(array);
		}
		if (array2 != null)
		{
			ArrayPool<uint>.Shared.Return(array2);
		}
	}

	private static Span<uint> PowCore(Span<uint> value, int valueLength, Span<uint> temp, uint power, Span<uint> result)
	{
		result[0] = 1u;
		int num = 1;
		while (power != 0)
		{
			if ((power & 1) == 1)
			{
				num = MultiplySelf(ref result, num, value.Slice(0, valueLength), ref temp);
			}
			if (power != 1)
			{
				valueLength = SquareSelf(ref value, valueLength, ref temp);
			}
			power >>= 1;
		}
		return result.Slice(0, num);
	}

	private static int MultiplySelf(ref Span<uint> left, int leftLength, ReadOnlySpan<uint> right, ref Span<uint> temp)
	{
		int length = leftLength + right.Length;
		if (leftLength >= right.Length)
		{
			Multiply(left.Slice(0, leftLength), right, temp.Slice(0, length));
		}
		else
		{
			Multiply(right, left.Slice(0, leftLength), temp.Slice(0, length));
		}
		left.Clear();
		Span<uint> span = left;
		left = temp;
		temp = span;
		return ActualLength(left.Slice(0, length));
	}

	private static int SquareSelf(ref Span<uint> value, int valueLength, ref Span<uint> temp)
	{
		int length = valueLength + valueLength;
		Square(value.Slice(0, valueLength), temp.Slice(0, length));
		value.Clear();
		Span<uint> span = value;
		value = temp;
		temp = span;
		return ActualLength(value.Slice(0, length));
	}

	public static int PowBound(uint power, int valueLength)
	{
		int num = 1;
		checked
		{
			while (power != 0)
			{
				if ((power & 1) == 1)
				{
					num += valueLength;
				}
				if (power != 1)
				{
					valueLength += valueLength;
				}
				power >>= 1;
			}
			return num;
		}
	}

	public static uint Pow(uint value, uint power, uint modulus)
	{
		return PowCore(value, power, modulus, 1uL);
	}

	public static uint Pow(ReadOnlySpan<uint> value, uint power, uint modulus)
	{
		uint num = Remainder(value, modulus);
		return PowCore(num, power, modulus, 1uL);
	}

	public static uint Pow(uint value, ReadOnlySpan<uint> power, uint modulus)
	{
		return PowCore(value, power, modulus, 1uL);
	}

	public static uint Pow(ReadOnlySpan<uint> value, ReadOnlySpan<uint> power, uint modulus)
	{
		uint num = Remainder(value, modulus);
		return PowCore(num, power, modulus, 1uL);
	}

	private static uint PowCore(ulong value, ReadOnlySpan<uint> power, uint modulus, ulong result)
	{
		for (int i = 0; i < power.Length - 1; i++)
		{
			uint num = power[i];
			for (int j = 0; j < 32; j++)
			{
				if ((num & 1) == 1)
				{
					result = result * value % modulus;
				}
				value = value * value % modulus;
				num >>= 1;
			}
		}
		return PowCore(value, power[power.Length - 1], modulus, result);
	}

	private static uint PowCore(ulong value, uint power, uint modulus, ulong result)
	{
		while (power != 0)
		{
			if ((power & 1) == 1)
			{
				result = result * value % modulus;
			}
			if (power != 1)
			{
				value = value * value % modulus;
			}
			power >>= 1;
		}
		return (uint)(result % modulus);
	}

	public static void Pow(uint value, uint power, ReadOnlySpan<uint> modulus, Span<uint> bits)
	{
		Pow((value != 0) ? new ReadOnlySpan<uint>(ref value) : default(ReadOnlySpan<uint>), power, modulus, bits);
	}

	public static void Pow(ReadOnlySpan<uint> value, uint power, ReadOnlySpan<uint> modulus, Span<uint> bits)
	{
		uint[] array = null;
		int num = Math.Max(value.Length, bits.Length);
		Span<uint> span = ((num > 64) ? ((Span<uint>)(array = ArrayPool<uint>.Shared.Rent(num))) : stackalloc uint[64]).Slice(0, num);
		span.Slice(value.Length).Clear();
		if (value.Length > modulus.Length)
		{
			Remainder(value, modulus, span);
		}
		else
		{
			value.CopyTo(span);
		}
		uint[] array2 = null;
		Span<uint> temp = ((bits.Length > 64) ? ((Span<uint>)(array2 = ArrayPool<uint>.Shared.Rent(bits.Length))) : stackalloc uint[64]).Slice(0, bits.Length);
		temp.Clear();
		PowCore(span, ActualLength(span), power, modulus, temp, bits);
		if (array != null)
		{
			ArrayPool<uint>.Shared.Return(array);
		}
		if (array2 != null)
		{
			ArrayPool<uint>.Shared.Return(array2);
		}
	}

	public static void Pow(uint value, ReadOnlySpan<uint> power, ReadOnlySpan<uint> modulus, Span<uint> bits)
	{
		Pow((value != 0) ? new ReadOnlySpan<uint>(ref value) : default(ReadOnlySpan<uint>), power, modulus, bits);
	}

	public static void Pow(ReadOnlySpan<uint> value, ReadOnlySpan<uint> power, ReadOnlySpan<uint> modulus, Span<uint> bits)
	{
		int num = Math.Max(value.Length, bits.Length);
		uint[] array = null;
		Span<uint> span = ((num > 64) ? ((Span<uint>)(array = ArrayPool<uint>.Shared.Rent(num))) : stackalloc uint[64]).Slice(0, num);
		span.Slice(value.Length).Clear();
		if (value.Length > modulus.Length)
		{
			Remainder(value, modulus, span);
		}
		else
		{
			value.CopyTo(span);
		}
		uint[] array2 = null;
		Span<uint> temp = ((bits.Length > 64) ? ((Span<uint>)(array2 = ArrayPool<uint>.Shared.Rent(bits.Length))) : stackalloc uint[64]).Slice(0, bits.Length);
		temp.Clear();
		PowCore(span, ActualLength(span), power, modulus, temp, bits);
		if (array != null)
		{
			ArrayPool<uint>.Shared.Return(array);
		}
		if (array2 != null)
		{
			ArrayPool<uint>.Shared.Return(array2);
		}
	}

	private static void PowCore(Span<uint> value, int valueLength, ReadOnlySpan<uint> power, ReadOnlySpan<uint> modulus, Span<uint> temp, Span<uint> bits)
	{
		bits[0] = 1u;
		if (modulus.Length < 32)
		{
			Span<uint> span = PowCore(value, valueLength, power, modulus, bits, 1, temp);
			span.CopyTo(bits);
			bits.Slice(span.Length).Clear();
			return;
		}
		int num = modulus.Length * 2 + 1;
		uint[] array = null;
		Span<uint> r = (((uint)num > 64u) ? ((Span<uint>)(array = ArrayPool<uint>.Shared.Rent(num))) : stackalloc uint[64]).Slice(0, num);
		r.Clear();
		num = r.Length - modulus.Length + 1;
		uint[] array2 = null;
		Span<uint> mu = (((uint)num > 64u) ? ((Span<uint>)(array2 = ArrayPool<uint>.Shared.Rent(num))) : stackalloc uint[64]).Slice(0, num);
		mu.Clear();
		num = modulus.Length * 2 + 2;
		uint[] array3 = null;
		Span<uint> q = (((uint)num > 64u) ? ((Span<uint>)(array3 = ArrayPool<uint>.Shared.Rent(num))) : stackalloc uint[64]).Slice(0, num);
		q.Clear();
		uint[] array4 = null;
		Span<uint> q2 = (((uint)num > 64u) ? ((Span<uint>)(array4 = ArrayPool<uint>.Shared.Rent(num))) : stackalloc uint[64]).Slice(0, num);
		q2.Clear();
		FastReducer reducer = new FastReducer(modulus, r, mu, q, q2);
		if (array != null)
		{
			ArrayPool<uint>.Shared.Return(array);
		}
		Span<uint> span2 = PowCore(value, valueLength, power, in reducer, bits, 1, temp);
		span2.CopyTo(bits);
		bits.Slice(span2.Length).Clear();
		if (array2 != null)
		{
			ArrayPool<uint>.Shared.Return(array2);
		}
		if (array3 != null)
		{
			ArrayPool<uint>.Shared.Return(array3);
		}
		if (array4 != null)
		{
			ArrayPool<uint>.Shared.Return(array4);
		}
	}

	private static void PowCore(Span<uint> value, int valueLength, uint power, ReadOnlySpan<uint> modulus, Span<uint> temp, Span<uint> bits)
	{
		bits[0] = 1u;
		if (modulus.Length < 32)
		{
			Span<uint> span = PowCore(value, valueLength, power, modulus, bits, 1, temp);
			span.CopyTo(bits);
			bits.Slice(span.Length).Clear();
			return;
		}
		int num = modulus.Length * 2 + 1;
		uint[] array = null;
		Span<uint> r = (((uint)num > 64u) ? ((Span<uint>)(array = ArrayPool<uint>.Shared.Rent(num))) : stackalloc uint[64]).Slice(0, num);
		r.Clear();
		num = r.Length - modulus.Length + 1;
		uint[] array2 = null;
		Span<uint> mu = (((uint)num > 64u) ? ((Span<uint>)(array2 = ArrayPool<uint>.Shared.Rent(num))) : stackalloc uint[64]).Slice(0, num);
		mu.Clear();
		num = modulus.Length * 2 + 2;
		uint[] array3 = null;
		Span<uint> q = (((uint)num > 64u) ? ((Span<uint>)(array3 = ArrayPool<uint>.Shared.Rent(num))) : stackalloc uint[64]).Slice(0, num);
		q.Clear();
		uint[] array4 = null;
		Span<uint> q2 = (((uint)num > 64u) ? ((Span<uint>)(array4 = ArrayPool<uint>.Shared.Rent(num))) : stackalloc uint[64]).Slice(0, num);
		q2.Clear();
		FastReducer reducer = new FastReducer(modulus, r, mu, q, q2);
		if (array != null)
		{
			ArrayPool<uint>.Shared.Return(array);
		}
		Span<uint> span2 = PowCore(value, valueLength, power, in reducer, bits, 1, temp);
		span2.CopyTo(bits);
		bits.Slice(span2.Length).Clear();
		if (array2 != null)
		{
			ArrayPool<uint>.Shared.Return(array2);
		}
		if (array3 != null)
		{
			ArrayPool<uint>.Shared.Return(array3);
		}
		if (array4 != null)
		{
			ArrayPool<uint>.Shared.Return(array4);
		}
	}

	private static Span<uint> PowCore(Span<uint> value, int valueLength, ReadOnlySpan<uint> power, ReadOnlySpan<uint> modulus, Span<uint> result, int resultLength, Span<uint> temp)
	{
		for (int i = 0; i < power.Length - 1; i++)
		{
			uint num = power[i];
			for (int j = 0; j < 32; j++)
			{
				if ((num & 1) == 1)
				{
					resultLength = MultiplySelf(ref result, resultLength, value.Slice(0, valueLength), ref temp);
					resultLength = Reduce(result.Slice(0, resultLength), modulus);
				}
				valueLength = SquareSelf(ref value, valueLength, ref temp);
				valueLength = Reduce(value.Slice(0, valueLength), modulus);
				num >>= 1;
			}
		}
		return PowCore(value, valueLength, power[power.Length - 1], modulus, result, resultLength, temp);
	}

	private static Span<uint> PowCore(Span<uint> value, int valueLength, uint power, ReadOnlySpan<uint> modulus, Span<uint> result, int resultLength, Span<uint> temp)
	{
		while (power != 0)
		{
			if ((power & 1) == 1)
			{
				resultLength = MultiplySelf(ref result, resultLength, value.Slice(0, valueLength), ref temp);
				resultLength = Reduce(result.Slice(0, resultLength), modulus);
			}
			if (power != 1)
			{
				valueLength = SquareSelf(ref value, valueLength, ref temp);
				valueLength = Reduce(value.Slice(0, valueLength), modulus);
			}
			power >>= 1;
		}
		return result.Slice(0, resultLength);
	}

	private static Span<uint> PowCore(Span<uint> value, int valueLength, ReadOnlySpan<uint> power, in FastReducer reducer, Span<uint> result, int resultLength, Span<uint> temp)
	{
		for (int i = 0; i < power.Length - 1; i++)
		{
			uint num = power[i];
			for (int j = 0; j < 32; j++)
			{
				if ((num & 1) == 1)
				{
					resultLength = MultiplySelf(ref result, resultLength, value.Slice(0, valueLength), ref temp);
					resultLength = reducer.Reduce(result.Slice(0, resultLength));
				}
				valueLength = SquareSelf(ref value, valueLength, ref temp);
				valueLength = reducer.Reduce(value.Slice(0, valueLength));
				num >>= 1;
			}
		}
		return PowCore(value, valueLength, power[power.Length - 1], in reducer, result, resultLength, temp);
	}

	private static Span<uint> PowCore(Span<uint> value, int valueLength, uint power, in FastReducer reducer, Span<uint> result, int resultLength, Span<uint> temp)
	{
		while (power != 0)
		{
			if ((power & 1) == 1)
			{
				resultLength = MultiplySelf(ref result, resultLength, value.Slice(0, valueLength), ref temp);
				resultLength = reducer.Reduce(result.Slice(0, resultLength));
			}
			if (power != 1)
			{
				valueLength = SquareSelf(ref value, valueLength, ref temp);
				valueLength = reducer.Reduce(value.Slice(0, valueLength));
			}
			power >>= 1;
		}
		return result.Slice(0, resultLength);
	}

	public static void Square(ReadOnlySpan<uint> value, Span<uint> bits)
	{
		if (value.Length < 32)
		{
			ref uint reference = ref MemoryMarshal.GetReference(bits);
			for (int i = 0; i < value.Length; i++)
			{
				ulong num = 0uL;
				uint num2 = value[i];
				for (int j = 0; j < i; j++)
				{
					ulong num3 = Unsafe.Add(ref reference, i + j) + num;
					ulong num4 = (ulong)value[j] * (ulong)num2;
					Unsafe.Add(ref reference, i + j) = (uint)(num3 + (num4 << 1));
					num = num4 + (num3 >> 1) >> 31;
				}
				ulong num5 = (ulong)((long)num2 * (long)num2) + num;
				Unsafe.Add(ref reference, i + i) = (uint)num5;
				Unsafe.Add(ref reference, i + i + 1) = (uint)(num5 >> 32);
			}
			return;
		}
		int num6 = value.Length >> 1;
		int num7 = num6 << 1;
		ReadOnlySpan<uint> readOnlySpan = value.Slice(0, num6);
		ReadOnlySpan<uint> readOnlySpan2 = value.Slice(num6);
		Span<uint> span = bits.Slice(0, num7);
		Span<uint> span2 = bits.Slice(num7);
		Square(readOnlySpan, span);
		Square(readOnlySpan2, span2);
		int num8 = readOnlySpan2.Length + 1;
		uint[] array = null;
		Span<uint> span3 = (((uint)num8 > 64u) ? ((Span<uint>)(array = ArrayPool<uint>.Shared.Rent(num8))) : stackalloc uint[64]).Slice(0, num8);
		span3.Clear();
		int num9 = num8 + num8;
		uint[] array2 = null;
		Span<uint> span4 = (((uint)num9 > 64u) ? ((Span<uint>)(array2 = ArrayPool<uint>.Shared.Rent(num9))) : stackalloc uint[64]).Slice(0, num9);
		span4.Clear();
		Add(readOnlySpan2, readOnlySpan, span3);
		Square(span3, span4);
		if (array != null)
		{
			ArrayPool<uint>.Shared.Return(array);
		}
		SubtractCore(span2, span, span4);
		AddSelf(bits.Slice(num6), span4);
		if (array2 != null)
		{
			ArrayPool<uint>.Shared.Return(array2);
		}
	}

	public static void Multiply(ReadOnlySpan<uint> left, uint right, Span<uint> bits)
	{
		int i = 0;
		ulong num = 0uL;
		for (; i < left.Length; i++)
		{
			ulong num2 = (ulong)((long)left[i] * (long)right) + num;
			bits[i] = (uint)num2;
			num = num2 >> 32;
		}
		bits[i] = (uint)num;
	}

	public static void Multiply(ReadOnlySpan<uint> left, ReadOnlySpan<uint> right, Span<uint> bits)
	{
		if (right.Length < 32)
		{
			ref uint reference = ref MemoryMarshal.GetReference(bits);
			for (int i = 0; i < right.Length; i++)
			{
				ulong num = 0uL;
				for (int j = 0; j < left.Length; j++)
				{
					ref uint reference2 = ref Unsafe.Add(ref reference, i + j);
					ulong num2 = reference2 + num + (ulong)((long)left[j] * (long)right[i]);
					reference2 = (uint)num2;
					num = num2 >> 32;
				}
				Unsafe.Add(ref reference, i + left.Length) = (uint)num;
			}
			return;
		}
		int num3 = right.Length >> 1;
		int num4 = num3 << 1;
		ReadOnlySpan<uint> readOnlySpan = left.Slice(0, num3);
		ReadOnlySpan<uint> left2 = left.Slice(num3);
		ReadOnlySpan<uint> right2 = right.Slice(0, num3);
		ReadOnlySpan<uint> readOnlySpan2 = right.Slice(num3);
		Span<uint> span = bits.Slice(0, num4);
		Span<uint> span2 = bits.Slice(num4);
		Multiply(readOnlySpan, right2, span);
		Multiply(left2, readOnlySpan2, span2);
		int num5 = left2.Length + 1;
		uint[] array = null;
		Span<uint> span3 = (((uint)num5 > 64u) ? ((Span<uint>)(array = ArrayPool<uint>.Shared.Rent(num5))) : stackalloc uint[64]).Slice(0, num5);
		span3.Clear();
		int num6 = readOnlySpan2.Length + 1;
		uint[] array2 = null;
		Span<uint> span4 = (((uint)num6 > 64u) ? ((Span<uint>)(array2 = ArrayPool<uint>.Shared.Rent(num6))) : stackalloc uint[64]).Slice(0, num6);
		span4.Clear();
		int num7 = num5 + num6;
		uint[] array3 = null;
		Span<uint> span5 = (((uint)num7 > 64u) ? ((Span<uint>)(array3 = ArrayPool<uint>.Shared.Rent(num7))) : stackalloc uint[64]).Slice(0, num7);
		span5.Clear();
		Add(left2, readOnlySpan, span3);
		Add(readOnlySpan2, right2, span4);
		Multiply(span3, span4, span5);
		if (array != null)
		{
			ArrayPool<uint>.Shared.Return(array);
		}
		if (array2 != null)
		{
			ArrayPool<uint>.Shared.Return(array2);
		}
		SubtractCore(span2, span, span5);
		AddSelf(bits.Slice(num3), span5);
		if (array3 != null)
		{
			ArrayPool<uint>.Shared.Return(array3);
		}
	}

	private static void SubtractCore(ReadOnlySpan<uint> left, ReadOnlySpan<uint> right, Span<uint> core)
	{
		int i = 0;
		long num = 0L;
		ref uint reference = ref MemoryMarshal.GetReference(left);
		ref uint reference2 = ref MemoryMarshal.GetReference(core);
		for (; i < right.Length; i++)
		{
			long num2 = Unsafe.Add(ref reference2, i) + num - Unsafe.Add(ref reference, i) - right[i];
			Unsafe.Add(ref reference2, i) = (uint)num2;
			num = num2 >> 32;
		}
		for (; i < left.Length; i++)
		{
			long num3 = Unsafe.Add(ref reference2, i) + num - left[i];
			Unsafe.Add(ref reference2, i) = (uint)num3;
			num = num3 >> 32;
		}
		while (num != 0L && i < core.Length)
		{
			long num4 = core[i] + num;
			core[i] = (uint)num4;
			num = num4 >> 32;
			i++;
		}
	}

	public static int Compare(ReadOnlySpan<uint> left, ReadOnlySpan<uint> right)
	{
		if (left.Length < right.Length)
		{
			return -1;
		}
		if (left.Length > right.Length)
		{
			return 1;
		}
		for (int num = left.Length - 1; num >= 0; num--)
		{
			uint num2 = left[num];
			uint num3 = right[num];
			if (num2 < num3)
			{
				return -1;
			}
			if (num2 > num3)
			{
				return 1;
			}
		}
		return 0;
	}

	private static int ActualLength(ReadOnlySpan<uint> value)
	{
		int num = value.Length;
		while (num > 0 && value[num - 1] == 0)
		{
			num--;
		}
		return num;
	}

	private static int Reduce(Span<uint> bits, ReadOnlySpan<uint> modulus)
	{
		if (bits.Length >= modulus.Length)
		{
			Divide(bits, modulus, default(Span<uint>));
			return ActualLength(bits.Slice(0, modulus.Length));
		}
		return bits.Length;
	}
}
