using System.Buffers.Text;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace System;

internal static class Number
{
	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	internal ref struct BigInteger
	{
		private int _length;

		private unsafe fixed uint _blocks[116];

		private static ReadOnlySpan<uint> Pow10UInt32Table => RuntimeHelpers.CreateSpan<uint>((RuntimeFieldHandle)/*OpCode not supported: LdMemberToken*/);

		private static ReadOnlySpan<int> Pow10BigNumTableIndices => RuntimeHelpers.CreateSpan<int>((RuntimeFieldHandle)/*OpCode not supported: LdMemberToken*/);

		private static ReadOnlySpan<uint> Pow10BigNumTable => RuntimeHelpers.CreateSpan<uint>((RuntimeFieldHandle)/*OpCode not supported: LdMemberToken*/);

		public unsafe static void Add(scoped ref BigInteger lhs, scoped ref BigInteger rhs, out BigInteger result)
		{
			ref BigInteger reference = ref lhs._length < rhs._length ? ref rhs : ref lhs;
			ref BigInteger reference2 = ref lhs._length < rhs._length ? ref lhs : ref rhs;
			int length = reference._length;
			int length2 = reference2._length;
			result._length = length;
			ulong num = 0uL;
			int num2 = 0;
			int num3 = 0;
			int num4 = 0;
			while (num3 < length2)
			{
				ulong num5 = num + reference._blocks[num2] + reference2._blocks[num3];
				num = num5 >> 32;
				result._blocks[num4] = (uint)num5;
				num2++;
				num3++;
				num4++;
			}
			while (num2 < length)
			{
				ulong num6 = num + reference._blocks[num2];
				num = num6 >> 32;
				result._blocks[num4] = (uint)num6;
				num2++;
				num4++;
			}
			int num7 = length;
			if (num != 0L)
			{
				if ((uint)num7 >= 116u)
				{
					SetZero(out result);
					return;
				}
				result._blocks[num4] = 1u;
				result._length++;
			}
		}

		public unsafe static int Compare(scoped ref BigInteger lhs, scoped ref BigInteger rhs)
		{
			int length = lhs._length;
			int length2 = rhs._length;
			int num = length - length2;
			if (num != 0)
			{
				return num;
			}
			if (length == 0)
			{
				return 0;
			}
			for (int num2 = length - 1; num2 >= 0; num2--)
			{
				long num3 = (long)lhs._blocks[num2] - (long)rhs._blocks[num2];
				if (num3 != 0L)
				{
					if (num3 <= 0)
					{
						return -1;
					}
					return 1;
				}
			}
			return 0;
		}

		public static uint CountSignificantBits(uint value)
		{
			return (uint)(32 - BitOperations.LeadingZeroCount(value));
		}

		public static uint CountSignificantBits(ulong value)
		{
			return (uint)(64 - BitOperations.LeadingZeroCount(value));
		}

		public unsafe static uint CountSignificantBits(ref BigInteger value)
		{
			if (value.IsZero())
			{
				return 0u;
			}
			uint num = (uint)(value._length - 1);
			return num * 32 + CountSignificantBits(value._blocks[num]);
		}

		public unsafe static void DivRem(scoped ref BigInteger lhs, scoped ref BigInteger rhs, out BigInteger quo, out BigInteger rem)
		{
			if (lhs.IsZero())
			{
				SetZero(out quo);
				SetZero(out rem);
				return;
			}
			int length = lhs._length;
			int length2 = rhs._length;
			if (length == 1 && length2 == 1)
			{
				var (value, value2) = Math.DivRem(lhs._blocks[0], rhs._blocks[0]);
				SetUInt32(out quo, value);
				SetUInt32(out rem, value2);
				return;
			}
			if (length2 == 1)
			{
				int num = length;
				ulong right = rhs._blocks[0];
				ulong num2 = 0uL;
				for (int num3 = num - 1; num3 >= 0; num3--)
				{
					ulong left = (num2 << 32) | lhs._blocks[num3];
					ulong num4;
					(num4, num2) = Math.DivRem(left, right);
					if (num4 == 0L && num3 == num - 1)
					{
						num--;
					}
					else
					{
						quo._blocks[num3] = (uint)num4;
					}
				}
				quo._length = num;
				SetUInt32(out rem, (uint)num2);
				return;
			}
			if (length2 > length)
			{
				SetZero(out quo);
				SetValue(out rem, ref lhs);
				return;
			}
			int num5 = length - length2 + 1;
			SetValue(out rem, ref lhs);
			int num6 = length;
			uint num7 = rhs._blocks[length2 - 1];
			uint num8 = rhs._blocks[length2 - 2];
			int num9 = BitOperations.LeadingZeroCount(num7);
			int num10 = 32 - num9;
			if (num9 > 0)
			{
				num7 = (num7 << num9) | (num8 >> num10);
				num8 <<= num9;
				if (length2 > 2)
				{
					num8 |= rhs._blocks[length2 - 3] >> num10;
				}
			}
			for (int num11 = length; num11 >= length2; num11--)
			{
				int num12 = num11 - length2;
				uint num13 = ((num11 < length) ? rem._blocks[num11] : 0u);
				ulong num14 = ((ulong)num13 << 32) | rem._blocks[num11 - 1];
				uint num15 = ((num11 > 1) ? rem._blocks[num11 - 2] : 0u);
				if (num9 > 0)
				{
					num14 = (num14 << num9) | (num15 >> num10);
					num15 <<= num9;
					if (num11 > 2)
					{
						num15 |= rem._blocks[num11 - 3] >> num10;
					}
				}
				ulong num16 = num14 / num7;
				if (num16 > uint.MaxValue)
				{
					num16 = 4294967295uL;
				}
				while (DivideGuessTooBig(num16, num14, num15, num7, num8))
				{
					num16--;
				}
				if (num16 != 0)
				{
					uint num17 = SubtractDivisor(ref rem, num12, ref rhs, num16);
					if (num17 != num13)
					{
						num17 = AddDivisor(ref rem, num12, ref rhs);
						num16--;
					}
				}
				if (num5 != 0)
				{
					if (num16 == 0L && num12 == num5 - 1)
					{
						num5--;
					}
					else
					{
						quo._blocks[num12] = (uint)num16;
					}
				}
				if (num11 < num6)
				{
					num6--;
				}
			}
			quo._length = num5;
			int num18 = num6 - 1;
			while (num18 >= 0 && rem._blocks[num18] == 0)
			{
				num6--;
				num18--;
			}
			rem._length = num6;
		}

		public unsafe static uint HeuristicDivide(ref BigInteger dividend, ref BigInteger divisor)
		{
			int num = divisor._length;
			if (dividend._length < num)
			{
				return 0u;
			}
			int num2 = num - 1;
			uint num3 = dividend._blocks[num2] / (divisor._blocks[num2] + 1);
			if (num3 != 0)
			{
				int num4 = 0;
				ulong num5 = 0uL;
				ulong num6 = 0uL;
				do
				{
					ulong num7 = (ulong)((long)divisor._blocks[num4] * (long)num3) + num6;
					num6 = num7 >> 32;
					ulong num8 = (ulong)((long)dividend._blocks[num4] - (long)(uint)num7) - num5;
					num5 = (num8 >> 32) & 1;
					dividend._blocks[num4] = (uint)num8;
					num4++;
				}
				while (num4 < num);
				while (num > 0 && dividend._blocks[num - 1] == 0)
				{
					num--;
				}
				dividend._length = num;
			}
			if (Compare(ref dividend, ref divisor) >= 0)
			{
				num3++;
				int num9 = 0;
				ulong num10 = 0uL;
				do
				{
					ulong num11 = (ulong)((long)dividend._blocks[num9] - (long)divisor._blocks[num9]) - num10;
					num10 = (num11 >> 32) & 1;
					dividend._blocks[num9] = (uint)num11;
					num9++;
				}
				while (num9 < num);
				while (num > 0 && dividend._blocks[num - 1] == 0)
				{
					num--;
				}
				dividend._length = num;
			}
			return num3;
		}

		public unsafe static void Multiply(scoped ref BigInteger lhs, uint value, out BigInteger result)
		{
			if (lhs._length <= 1)
			{
				SetUInt64(out result, (ulong)lhs.ToUInt32() * (ulong)value);
				return;
			}
			switch (value)
			{
			case 0u:
				SetZero(out result);
				return;
			case 1u:
				SetValue(out result, ref lhs);
				return;
			}
			int length = lhs._length;
			int i = 0;
			uint num = 0u;
			for (; i < length; i++)
			{
				ulong num2 = (ulong)((long)lhs._blocks[i] * (long)value + num);
				result._blocks[i] = (uint)num2;
				num = (uint)(num2 >> 32);
			}
			int num3 = length;
			if (num != 0)
			{
				if ((uint)num3 >= 116u)
				{
					SetZero(out result);
					return;
				}
				result._blocks[i] = num;
				num3++;
			}
			result._length = num3;
		}

		public unsafe static void Multiply(scoped ref BigInteger lhs, scoped ref BigInteger rhs, out BigInteger result)
		{
			if (lhs._length <= 1)
			{
				Multiply(ref rhs, lhs.ToUInt32(), out result);
				return;
			}
			if (rhs._length <= 1)
			{
				Multiply(ref lhs, rhs.ToUInt32(), out result);
				return;
			}
			ref BigInteger reference = ref lhs;
			int length = lhs._length;
			ref BigInteger reference2 = ref rhs;
			int length2 = rhs._length;
			if (length < length2)
			{
				reference = ref rhs;
				length = rhs._length;
				reference2 = ref lhs;
				length2 = lhs._length;
			}
			int num = length2 + length;
			if ((uint)num > 116u)
			{
				SetZero(out result);
				return;
			}
			result._length = num;
			result.Clear((uint)num);
			int num2 = 0;
			int num3 = 0;
			while (num2 < length2)
			{
				if (reference2._blocks[num2] != 0)
				{
					int num4 = 0;
					int num5 = num3;
					ulong num6 = 0uL;
					do
					{
						ulong num7 = (ulong)(result._blocks[num5] + (long)reference2._blocks[num2] * (long)reference._blocks[num4]) + num6;
						num6 = num7 >> 32;
						result._blocks[num5] = (uint)num7;
						num5++;
						num4++;
					}
					while (num4 < length);
					result._blocks[num5] = (uint)num6;
				}
				num2++;
				num3++;
			}
			if (num > 0 && result._blocks[num - 1] == 0)
			{
				result._length--;
			}
		}

		public unsafe static void Pow2(uint exponent, out BigInteger result)
		{
			uint remainder;
			uint num = DivRem32(exponent, out remainder);
			result._length = (int)(num + 1);
			if ((uint)result._length > 116u)
			{
				SetZero(out result);
				return;
			}
			if (num != 0)
			{
				result.Clear(num);
			}
			result._blocks[num] = (uint)(1 << (int)remainder);
		}

		public unsafe static void Pow10(uint exponent, out BigInteger result)
		{
			ReadOnlySpan<uint> readOnlySpan = Pow10UInt32Table;
			SetUInt32(out var result2, readOnlySpan[(int)(exponent & 7)]);
			ref BigInteger reference = ref result2;
			SetZero(out var result3);
			ref BigInteger reference2 = ref result3;
			exponent >>= 3;
			uint num = 0u;
			while (exponent != 0)
			{
				if ((exponent & (true ? 1u : 0u)) != 0)
				{
					readOnlySpan = Pow10BigNumTable;
					fixed (uint* ptr = &readOnlySpan[Pow10BigNumTableIndices[(int)num]])
					{
						Multiply(ref reference, ref *(BigInteger*)ptr, out reference2);
					}
					ref BigInteger reference3 = ref reference2;
					reference2 = ref reference;
					reference = ref reference3;
				}
				num++;
				exponent >>= 1;
			}
			SetValue(out result, ref reference);
		}

		private unsafe static uint AddDivisor(ref BigInteger lhs, int lhsStartIndex, ref BigInteger rhs)
		{
			int length = lhs._length;
			int length2 = rhs._length;
			ulong num = 0uL;
			for (int i = 0; i < length2; i++)
			{
				ref uint reference = ref lhs._blocks[lhsStartIndex + i];
				ulong num2 = reference + num + rhs._blocks[i];
				reference = (uint)num2;
				num = num2 >> 32;
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

		private unsafe static uint SubtractDivisor(ref BigInteger lhs, int lhsStartIndex, ref BigInteger rhs, ulong q)
		{
			int num = lhs._length - lhsStartIndex;
			int length = rhs._length;
			ulong num2 = 0uL;
			for (int i = 0; i < length; i++)
			{
				num2 += rhs._blocks[i] * q;
				uint num3 = (uint)num2;
				num2 >>= 32;
				ref uint reference = ref lhs._blocks[lhsStartIndex + i];
				if (reference < num3)
				{
					num2++;
				}
				reference -= num3;
			}
			return (uint)num2;
		}

		public unsafe void Add(uint value)
		{
			int length = _length;
			if (length == 0)
			{
				SetUInt32(out this, value);
				return;
			}
			_blocks[0] += value;
			if (_blocks[0] >= value)
			{
				return;
			}
			for (int i = 1; i < length; i++)
			{
				ref uint reference = ref _blocks[i];
				reference++;
				if (_blocks[i] != 0)
				{
					return;
				}
			}
			if ((uint)length >= 116u)
			{
				SetZero(out this);
				return;
			}
			_blocks[length] = 1u;
			_length = length + 1;
		}

		public unsafe uint GetBlock(uint index)
		{
			return _blocks[index];
		}

		public int GetLength()
		{
			return _length;
		}

		public bool IsZero()
		{
			return _length == 0;
		}

		public void Multiply(uint value)
		{
			Multiply(ref this, value, out this);
		}

		public void Multiply(scoped ref BigInteger value)
		{
			if (value._length <= 1)
			{
				Multiply(ref this, value.ToUInt32(), out this);
				return;
			}
			SetValue(out var result, ref this);
			Multiply(ref result, ref value, out this);
		}

		public unsafe void Multiply10()
		{
			if (IsZero())
			{
				return;
			}
			int num = 0;
			int length = _length;
			ulong num2 = 0uL;
			do
			{
				ulong num3 = _blocks[num];
				ulong num4 = (num3 << 3) + (num3 << 1) + num2;
				num2 = num4 >> 32;
				_blocks[num] = (uint)num4;
				num++;
			}
			while (num < length);
			if (num2 != 0L)
			{
				if ((uint)length >= 116u)
				{
					SetZero(out this);
					return;
				}
				_blocks[num] = (uint)num2;
				_length = length + 1;
			}
		}

		public void MultiplyPow10(uint exponent)
		{
			if (exponent <= 9)
			{
				Multiply(Pow10UInt32Table[(int)exponent]);
			}
			else if (!IsZero())
			{
				Pow10(exponent, out var result);
				Multiply(ref result);
			}
		}

		public unsafe static void SetUInt32(out BigInteger result, uint value)
		{
			if (value == 0)
			{
				SetZero(out result);
				return;
			}
			result._blocks[0] = value;
			result._length = 1;
		}

		public unsafe static void SetUInt64(out BigInteger result, ulong value)
		{
			if (value <= uint.MaxValue)
			{
				SetUInt32(out result, (uint)value);
				return;
			}
			result._blocks[0] = (uint)value;
			result._blocks[1] = (uint)(value >> 32);
			result._length = 2;
		}

		public unsafe static void SetValue(out BigInteger result, scoped ref BigInteger value)
		{
			Buffer.Memmove(elementCount: (nuint)(result._length = value._length), destination: ref result._blocks[0], source: ref value._blocks[0]);
		}

		public static void SetZero(out BigInteger result)
		{
			result._length = 0;
		}

		public unsafe void ShiftLeft(uint shift)
		{
			int length = _length;
			if (length == 0 || shift == 0)
			{
				return;
			}
			uint remainder;
			uint num = DivRem32(shift, out remainder);
			int num2 = length - 1;
			int num3 = num2 + (int)num;
			if (remainder == 0)
			{
				if ((uint)length >= 116u)
				{
					SetZero(out this);
					return;
				}
				while (num2 >= 0)
				{
					_blocks[num3] = _blocks[num2];
					num2--;
					num3--;
				}
				_length += (int)num;
				Clear(num);
				return;
			}
			num3++;
			if ((uint)length >= 116u)
			{
				SetZero(out this);
				return;
			}
			_length = num3 + 1;
			uint num4 = 32 - remainder;
			uint num5 = 0u;
			uint num6 = _blocks[num2];
			uint num7 = num6 >> (int)num4;
			while (num2 > 0)
			{
				_blocks[num3] = num5 | num7;
				num5 = num6 << (int)remainder;
				num2--;
				num3--;
				num6 = _blocks[num2];
				num7 = num6 >> (int)num4;
			}
			_blocks[num3] = num5 | num7;
			_blocks[num3 - 1] = num6 << (int)remainder;
			Clear(num);
			if (_blocks[_length - 1] == 0)
			{
				_length--;
			}
		}

		public unsafe uint ToUInt32()
		{
			if (_length > 0)
			{
				return _blocks[0];
			}
			return 0u;
		}

		public unsafe ulong ToUInt64()
		{
			if (_length > 1)
			{
				return ((ulong)_blocks[1] << 32) + _blocks[0];
			}
			if (_length > 0)
			{
				return _blocks[0];
			}
			return 0uL;
		}

		private unsafe void Clear(uint length)
		{
			NativeMemory.Clear(Unsafe.AsPointer(ref _blocks[0]), length * 4);
		}

		private static uint DivRem32(uint value, out uint remainder)
		{
			remainder = value & 0x1Fu;
			return value >> 5;
		}
	}

	internal readonly ref struct DiyFp
	{
		public readonly ulong f;

		public readonly int e;

		public static DiyFp CreateAndGetBoundaries(double value, out DiyFp mMinus, out DiyFp mPlus)
		{
			DiyFp result = new DiyFp(value);
			result.GetBoundaries(52, out mMinus, out mPlus);
			return result;
		}

		public static DiyFp CreateAndGetBoundaries(float value, out DiyFp mMinus, out DiyFp mPlus)
		{
			DiyFp result = new DiyFp(value);
			result.GetBoundaries(23, out mMinus, out mPlus);
			return result;
		}

		public static DiyFp CreateAndGetBoundaries(Half value, out DiyFp mMinus, out DiyFp mPlus)
		{
			DiyFp result = new DiyFp(value);
			result.GetBoundaries(10, out mMinus, out mPlus);
			return result;
		}

		public DiyFp(double value)
		{
			f = ExtractFractionAndBiasedExponent(value, out e);
		}

		public DiyFp(float value)
		{
			f = ExtractFractionAndBiasedExponent(value, out e);
		}

		public DiyFp(Half value)
		{
			f = ExtractFractionAndBiasedExponent(value, out e);
		}

		public DiyFp(ulong f, int e)
		{
			this.f = f;
			this.e = e;
		}

		public DiyFp Multiply(in DiyFp other)
		{
			uint num = (uint)(f >> 32);
			uint num2 = (uint)f;
			uint num3 = (uint)(other.f >> 32);
			uint num4 = (uint)other.f;
			ulong num5 = (ulong)num * (ulong)num3;
			ulong num6 = (ulong)num2 * (ulong)num3;
			ulong num7 = (ulong)num * (ulong)num4;
			ulong num8 = (ulong)num2 * (ulong)num4;
			ulong num9 = (num8 >> 32) + (uint)num7 + (uint)num6;
			num9 += 2147483648u;
			return new DiyFp(num5 + (num7 >> 32) + (num6 >> 32) + (num9 >> 32), e + other.e + 64);
		}

		public DiyFp Normalize()
		{
			int num = BitOperations.LeadingZeroCount(f);
			return new DiyFp(f << num, e - num);
		}

		public DiyFp Subtract(in DiyFp other)
		{
			return new DiyFp(f - other.f, e);
		}

		private void GetBoundaries(int implicitBitIndex, out DiyFp mMinus, out DiyFp mPlus)
		{
			mPlus = new DiyFp((f << 1) + 1, e - 1).Normalize();
			if (f == (ulong)(1L << implicitBitIndex))
			{
				mMinus = new DiyFp((f << 2) - 1, e - 2);
			}
			else
			{
				mMinus = new DiyFp((f << 1) - 1, e - 1);
			}
			mMinus = new DiyFp(mMinus.f << mMinus.e - mPlus.e, mPlus.e);
		}
	}

	internal static class Grisu3
	{
		private static ReadOnlySpan<short> CachedPowersBinaryExponent => RuntimeHelpers.CreateSpan<short>((RuntimeFieldHandle)/*OpCode not supported: LdMemberToken*/);

		private static ReadOnlySpan<short> CachedPowersDecimalExponent => RuntimeHelpers.CreateSpan<short>((RuntimeFieldHandle)/*OpCode not supported: LdMemberToken*/);

		private static ReadOnlySpan<ulong> CachedPowersSignificand => RuntimeHelpers.CreateSpan<ulong>((RuntimeFieldHandle)/*OpCode not supported: LdMemberToken*/);

		private static ReadOnlySpan<uint> SmallPowersOfTen => RuntimeHelpers.CreateSpan<uint>((RuntimeFieldHandle)/*OpCode not supported: LdMemberToken*/);

		public static bool TryRunDouble(double value, int requestedDigits, ref NumberBuffer number)
		{
			double value2 = (double.IsNegative(value) ? (0.0 - value) : value);
			DiyFp diyFp;
			bool flag;
			int length;
			int decimalExponent;
			if (requestedDigits == -1)
			{
				diyFp = DiyFp.CreateAndGetBoundaries(value2, out var mMinus, out var mPlus);
				DiyFp w = diyFp.Normalize();
				flag = TryRunShortest(in mMinus, in w, in mPlus, number.Digits, out length, out decimalExponent);
			}
			else
			{
				diyFp = new DiyFp(value2);
				DiyFp w2 = diyFp.Normalize();
				flag = TryRunCounted(in w2, requestedDigits, number.Digits, out length, out decimalExponent);
			}
			if (flag)
			{
				number.Scale = length + decimalExponent;
				number.Digits[length] = 0;
				number.DigitsCount = length;
			}
			return flag;
		}

		public static bool TryRunHalf(Half value, int requestedDigits, ref NumberBuffer number)
		{
			Half value2 = (Half.IsNegative(value) ? Half.Negate(value) : value);
			DiyFp diyFp;
			bool flag;
			int length;
			int decimalExponent;
			if (requestedDigits == -1)
			{
				diyFp = DiyFp.CreateAndGetBoundaries(value2, out var mMinus, out var mPlus);
				DiyFp w = diyFp.Normalize();
				flag = TryRunShortest(in mMinus, in w, in mPlus, number.Digits, out length, out decimalExponent);
			}
			else
			{
				diyFp = new DiyFp(value2);
				DiyFp w2 = diyFp.Normalize();
				flag = TryRunCounted(in w2, requestedDigits, number.Digits, out length, out decimalExponent);
			}
			if (flag)
			{
				number.Scale = length + decimalExponent;
				number.Digits[length] = 0;
				number.DigitsCount = length;
			}
			return flag;
		}

		public static bool TryRunSingle(float value, int requestedDigits, ref NumberBuffer number)
		{
			float value2 = (float.IsNegative(value) ? (0f - value) : value);
			DiyFp diyFp;
			bool flag;
			int length;
			int decimalExponent;
			if (requestedDigits == -1)
			{
				diyFp = DiyFp.CreateAndGetBoundaries(value2, out var mMinus, out var mPlus);
				DiyFp w = diyFp.Normalize();
				flag = TryRunShortest(in mMinus, in w, in mPlus, number.Digits, out length, out decimalExponent);
			}
			else
			{
				diyFp = new DiyFp(value2);
				DiyFp w2 = diyFp.Normalize();
				flag = TryRunCounted(in w2, requestedDigits, number.Digits, out length, out decimalExponent);
			}
			if (flag)
			{
				number.Scale = length + decimalExponent;
				number.Digits[length] = 0;
				number.DigitsCount = length;
			}
			return flag;
		}

		private static bool TryRunCounted(in DiyFp w, int requestedDigits, Span<byte> buffer, out int length, out int decimalExponent)
		{
			int minExponent = -60 - (w.e + 64);
			int maxExponent = -32 - (w.e + 64);
			int decimalExponent2;
			DiyFp other = GetCachedPowerForBinaryExponentRange(minExponent, maxExponent, out decimalExponent2);
			DiyFp w2 = w.Multiply(in other);
			int kappa;
			bool result = TryDigitGenCounted(in w2, requestedDigits, buffer, out length, out kappa);
			decimalExponent = -decimalExponent2 + kappa;
			return result;
		}

		private static bool TryRunShortest(in DiyFp boundaryMinus, in DiyFp w, in DiyFp boundaryPlus, Span<byte> buffer, out int length, out int decimalExponent)
		{
			int minExponent = -60 - (w.e + 64);
			int maxExponent = -32 - (w.e + 64);
			int decimalExponent2;
			DiyFp other = GetCachedPowerForBinaryExponentRange(minExponent, maxExponent, out decimalExponent2);
			DiyFp w2 = w.Multiply(in other);
			DiyFp low = boundaryMinus.Multiply(in other);
			DiyFp high = boundaryPlus.Multiply(in other);
			int kappa;
			bool result = TryDigitGenShortest(in low, in w2, in high, buffer, out length, out kappa);
			decimalExponent = -decimalExponent2 + kappa;
			return result;
		}

		private static uint BiggestPowerTen(uint number, int numberBits, out int exponentPlusOne)
		{
			int num = (numberBits + 1) * 1233 >> 12;
			uint num2 = SmallPowersOfTen[num];
			if (number < num2)
			{
				num--;
				num2 = SmallPowersOfTen[num];
			}
			exponentPlusOne = num + 1;
			return num2;
		}

		private static bool TryDigitGenCounted(in DiyFp w, int requestedDigits, Span<byte> buffer, out int length, out int kappa)
		{
			ulong num = 1uL;
			DiyFp diyFp = new DiyFp((ulong)(1L << -w.e), w.e);
			uint num2 = (uint)(w.f >> -diyFp.e);
			ulong num3 = w.f & (diyFp.f - 1);
			if (num3 == 0L && (requestedDigits >= 11 || num2 < SmallPowersOfTen[requestedDigits - 1]))
			{
				length = 0;
				kappa = 0;
				return false;
			}
			uint num4 = BiggestPowerTen(num2, 64 - -diyFp.e, out kappa);
			length = 0;
			while (kappa > 0)
			{
				uint num5;
				(num5, num2) = Math.DivRem(num2, num4);
				buffer[length] = (byte)(48 + num5);
				length++;
				requestedDigits--;
				kappa--;
				if (requestedDigits == 0)
				{
					break;
				}
				num4 /= 10;
			}
			if (requestedDigits == 0)
			{
				ulong rest = ((ulong)num2 << -diyFp.e) + num3;
				return TryRoundWeedCounted(buffer, length, rest, (ulong)num4 << -diyFp.e, num, ref kappa);
			}
			while (requestedDigits > 0 && num3 > num)
			{
				num3 *= 10;
				num *= 10;
				uint num6 = (uint)(num3 >> -diyFp.e);
				buffer[length] = (byte)(48 + num6);
				length++;
				requestedDigits--;
				kappa--;
				num3 &= diyFp.f - 1;
			}
			if (requestedDigits != 0)
			{
				buffer[0] = 0;
				length = 0;
				kappa = 0;
				return false;
			}
			return TryRoundWeedCounted(buffer, length, num3, diyFp.f, num, ref kappa);
		}

		private static bool TryDigitGenShortest(in DiyFp low, in DiyFp w, in DiyFp high, Span<byte> buffer, out int length, out int kappa)
		{
			ulong num = 1uL;
			DiyFp other = new DiyFp(low.f - num, low.e);
			DiyFp diyFp = new DiyFp(high.f + num, high.e);
			DiyFp diyFp2 = diyFp.Subtract(in other);
			DiyFp diyFp3 = new DiyFp((ulong)(1L << -w.e), w.e);
			uint num2 = (uint)(diyFp.f >> -diyFp3.e);
			ulong num3 = diyFp.f & (diyFp3.f - 1);
			uint num4 = BiggestPowerTen(num2, 64 - -diyFp3.e, out kappa);
			length = 0;
			while (kappa > 0)
			{
				uint num5;
				(num5, num2) = Math.DivRem(num2, num4);
				buffer[length] = (byte)(48 + num5);
				length++;
				kappa--;
				ulong num6 = ((ulong)num2 << -diyFp3.e) + num3;
				if (num6 < diyFp2.f)
				{
					return TryRoundWeedShortest(buffer, length, diyFp.Subtract(in w).f, diyFp2.f, num6, (ulong)num4 << -diyFp3.e, num);
				}
				num4 /= 10;
			}
			do
			{
				num3 *= 10;
				num *= 10;
				diyFp2 = new DiyFp(diyFp2.f * 10, diyFp2.e);
				uint num7 = (uint)(num3 >> -diyFp3.e);
				buffer[length] = (byte)(48 + num7);
				length++;
				kappa--;
				num3 &= diyFp3.f - 1;
			}
			while (num3 >= diyFp2.f);
			return TryRoundWeedShortest(buffer, length, diyFp.Subtract(in w).f * num, diyFp2.f, num3, diyFp3.f, num);
		}

		private static DiyFp GetCachedPowerForBinaryExponentRange(int minExponent, int maxExponent, out int decimalExponent)
		{
			double num = Math.Ceiling((double)(minExponent + 64 - 1) * 0.3010299956639812);
			int index = (348 + (int)num - 1) / 8 + 1;
			decimalExponent = CachedPowersDecimalExponent[index];
			return new DiyFp(CachedPowersSignificand[index], CachedPowersBinaryExponent[index]);
		}

		private static bool TryRoundWeedCounted(Span<byte> buffer, int length, ulong rest, ulong tenKappa, ulong unit, ref int kappa)
		{
			if (unit >= tenKappa || tenKappa - unit <= unit)
			{
				return false;
			}
			if (tenKappa - rest > rest && tenKappa - 2 * rest >= 2 * unit)
			{
				return true;
			}
			if (rest > unit && (tenKappa <= rest - unit || tenKappa - (rest - unit) <= rest - unit))
			{
				buffer[length - 1]++;
				int num = length - 1;
				while (num > 0 && buffer[num] == 58)
				{
					buffer[num] = 48;
					buffer[num - 1]++;
					num--;
				}
				if (buffer[0] == 58)
				{
					buffer[0] = 49;
					kappa++;
				}
				return true;
			}
			return false;
		}

		private static bool TryRoundWeedShortest(Span<byte> buffer, int length, ulong distanceTooHighW, ulong unsafeInterval, ulong rest, ulong tenKappa, ulong unit)
		{
			ulong num = distanceTooHighW - unit;
			ulong num2 = distanceTooHighW + unit;
			while (rest < num && unsafeInterval - rest >= tenKappa && (rest + tenKappa < num || num - rest >= rest + tenKappa - num))
			{
				buffer[length - 1]--;
				rest += tenKappa;
			}
			if (rest < num2 && unsafeInterval - rest >= tenKappa && (rest + tenKappa < num2 || num2 - rest > rest + tenKappa - num2))
			{
				return false;
			}
			if (2 * unit <= rest)
			{
				return rest <= unsafeInterval - 4 * unit;
			}
			return false;
		}
	}

	internal ref struct NumberBuffer
	{
		public int DigitsCount;

		public int Scale;

		public bool IsNegative;

		public bool HasNonZeroTail;

		public NumberBufferKind Kind;

		public Span<byte> Digits;

		public unsafe NumberBuffer(NumberBufferKind kind, byte* digits, int digitsLength)
			: this(kind, new Span<byte>(digits, digitsLength))
		{
		}

		public NumberBuffer(NumberBufferKind kind, Span<byte> digits)
		{
			DigitsCount = 0;
			Scale = 0;
			IsNegative = false;
			HasNonZeroTail = false;
			Kind = kind;
			Digits = digits;
			Digits[0] = 0;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public unsafe byte* GetDigitsPointer()
		{
			return (byte*)Unsafe.AsPointer(ref MemoryMarshal.GetReference(Digits));
		}

		public override string ToString()
		{
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.Append('[');
			stringBuilder.Append('"');
			for (int i = 0; i < Digits.Length; i++)
			{
				byte b = Digits[i];
				if (b == 0)
				{
					break;
				}
				stringBuilder.Append((char)b);
			}
			stringBuilder.Append('"');
			stringBuilder.Append(", Length = ").Append(DigitsCount);
			stringBuilder.Append(", Scale = ").Append(Scale);
			stringBuilder.Append(", IsNegative = ").Append(IsNegative);
			stringBuilder.Append(", HasNonZeroTail = ").Append(HasNonZeroTail);
			stringBuilder.Append(", Kind = ").Append(Kind);
			stringBuilder.Append(']');
			return stringBuilder.ToString();
		}
	}

	internal enum NumberBufferKind : byte
	{
		Unknown,
		Integer,
		Decimal,
		FloatingPoint
	}

	private interface IHexOrBinaryParser<TInteger> where TInteger : unmanaged, IBinaryIntegerParseAndFormatInfo<TInteger>
	{
		static abstract NumberStyles AllowedStyles { get; }

		static abstract uint MaxDigitValue { get; }

		static abstract int MaxDigitCount { get; }

		static abstract bool IsValidChar(uint ch);

		static abstract uint FromChar(uint ch);

		static abstract TInteger ShiftLeftForNextDigit(TInteger value);
	}

	[StructLayout(LayoutKind.Sequential, Size = 1)]
	private readonly struct HexParser<TInteger> : IHexOrBinaryParser<TInteger> where TInteger : unmanaged, IBinaryIntegerParseAndFormatInfo<TInteger>
	{
		public static NumberStyles AllowedStyles => NumberStyles.HexNumber;

		public static uint MaxDigitValue => 15u;

		public static int MaxDigitCount => TInteger.MaxHexDigitCount;

		public static bool IsValidChar(uint ch)
		{
			return HexConverter.IsHexChar((int)ch);
		}

		public static uint FromChar(uint ch)
		{
			return (uint)HexConverter.FromChar((int)ch);
		}

		public static TInteger ShiftLeftForNextDigit(TInteger value)
		{
			return TInteger.MultiplyBy16(value);
		}
	}

	[StructLayout(LayoutKind.Sequential, Size = 1)]
	private readonly struct BinaryParser<TInteger> : IHexOrBinaryParser<TInteger> where TInteger : unmanaged, IBinaryIntegerParseAndFormatInfo<TInteger>
	{
		public static NumberStyles AllowedStyles => NumberStyles.BinaryNumber;

		public static uint MaxDigitValue => 1u;

		public unsafe static int MaxDigitCount => sizeof(TInteger) * 8;

		public static bool IsValidChar(uint ch)
		{
			return ch - 48 <= 1;
		}

		public static uint FromChar(uint ch)
		{
			return ch - 48;
		}

		public static TInteger ShiftLeftForNextDigit(TInteger value)
		{
			return value << 1;
		}
	}

	internal enum ParsingStatus
	{
		OK,
		Failed,
		Overflow
	}

	private static readonly string[] s_smallNumberCache = new string[300];

	private static readonly string[] s_posCurrencyFormats = new string[4] { "$#", "#$", "$ #", "# $" };

	private static readonly string[] s_negCurrencyFormats = new string[17]
	{
		"($#)", "-$#", "$-#", "$#-", "(#$)", "-#$", "#-$", "#$-", "-# $", "-$ #",
		"# $-", "$ #-", "$ -#", "#- $", "($ #)", "(# $)", "$- #"
	};

	private static readonly string[] s_posPercentFormats = new string[4] { "# %", "#%", "%#", "% #" };

	private static readonly string[] s_negPercentFormats = new string[12]
	{
		"-# %", "-#%", "-%#", "%-#", "%#-", "#-%", "#%-", "-% #", "# %-", "% #-",
		"% -#", "#- %"
	};

	private static readonly string[] s_negNumberFormats = new string[5] { "(#)", "-#", "- #", "#-", "# -" };

	private static readonly byte[] TwoDigitsCharsAsBytes = MemoryMarshal.AsBytes<char>("00010203040506070809101112131415161718192021222324252627282930313233343536373839404142434445464748495051525354555657585960616263646566676869707172737475767778798081828384858687888990919293949596979899").ToArray();

	private static readonly byte[] TwoDigitsBytes = "00010203040506070809101112131415161718192021222324252627282930313233343536373839404142434445464748495051525354555657585960616263646566676869707172737475767778798081828384858687888990919293949596979899"u8.ToArray();

	private static ReadOnlySpan<double> Pow10DoubleTable => RuntimeHelpers.CreateSpan<double>((RuntimeFieldHandle)/*OpCode not supported: LdMemberToken*/);

	private static ReadOnlySpan<ulong> Pow5128Table => RuntimeHelpers.CreateSpan<ulong>((RuntimeFieldHandle)/*OpCode not supported: LdMemberToken*/);

	public static void Dragon4Double(double value, int cutoffNumber, bool isSignificantDigits, ref NumberBuffer number)
	{
		double num = (double.IsNegative(value) ? (0.0 - value) : value);
		int exponent;
		ulong num2 = ExtractFractionAndBiasedExponent(value, out exponent);
		bool hasUnequalMargins = false;
		uint mantissaHighBitIdx;
		if (num2 >> 52 != 0L)
		{
			mantissaHighBitIdx = 52u;
			hasUnequalMargins = num2 == 4503599627370496L;
		}
		else
		{
			mantissaHighBitIdx = (uint)BitOperations.Log2(num2);
		}
		int decimalExponent;
		int num3 = (int)Dragon4(num2, exponent, mantissaHighBitIdx, hasUnequalMargins, cutoffNumber, isSignificantDigits, number.Digits, out decimalExponent);
		number.Scale = decimalExponent + 1;
		number.Digits[num3] = 0;
		number.DigitsCount = num3;
	}

	public static void Dragon4Half(Half value, int cutoffNumber, bool isSignificantDigits, ref NumberBuffer number)
	{
		Half half = (Half.IsNegative(value) ? Half.Negate(value) : value);
		int exponent;
		ushort num = ExtractFractionAndBiasedExponent(value, out exponent);
		bool hasUnequalMargins = false;
		uint mantissaHighBitIdx;
		if (num >> 10 != 0)
		{
			mantissaHighBitIdx = 10u;
			hasUnequalMargins = num == 1024;
		}
		else
		{
			mantissaHighBitIdx = (uint)BitOperations.Log2(num);
		}
		int decimalExponent;
		int num2 = (int)Dragon4(num, exponent, mantissaHighBitIdx, hasUnequalMargins, cutoffNumber, isSignificantDigits, number.Digits, out decimalExponent);
		number.Scale = decimalExponent + 1;
		number.Digits[num2] = 0;
		number.DigitsCount = num2;
	}

	public static void Dragon4Single(float value, int cutoffNumber, bool isSignificantDigits, ref NumberBuffer number)
	{
		float num = (float.IsNegative(value) ? (0f - value) : value);
		int exponent;
		uint num2 = ExtractFractionAndBiasedExponent(value, out exponent);
		bool hasUnequalMargins = false;
		uint mantissaHighBitIdx;
		if (num2 >> 23 != 0)
		{
			mantissaHighBitIdx = 23u;
			hasUnequalMargins = num2 == 8388608;
		}
		else
		{
			mantissaHighBitIdx = (uint)BitOperations.Log2(num2);
		}
		int decimalExponent;
		int num3 = (int)Dragon4(num2, exponent, mantissaHighBitIdx, hasUnequalMargins, cutoffNumber, isSignificantDigits, number.Digits, out decimalExponent);
		number.Scale = decimalExponent + 1;
		number.Digits[num3] = 0;
		number.DigitsCount = num3;
	}

	private unsafe static uint Dragon4(ulong mantissa, int exponent, uint mantissaHighBitIdx, bool hasUnequalMargins, int cutoffNumber, bool isSignificantDigits, Span<byte> buffer, out int decimalExponent)
	{
		int num = 0;
		BigInteger lhs;
		BigInteger rhs;
		BigInteger result;
		BigInteger* ptr;
		if (hasUnequalMargins)
		{
			BigInteger result2;
			if (exponent > 0)
			{
				BigInteger.SetUInt64(out lhs, 4 * mantissa);
				lhs.ShiftLeft((uint)exponent);
				BigInteger.SetUInt32(out rhs, 4u);
				BigInteger.Pow2((uint)exponent, out result);
				BigInteger.Pow2((uint)(exponent + 1), out result2);
			}
			else
			{
				BigInteger.SetUInt64(out lhs, 4 * mantissa);
				BigInteger.Pow2((uint)(-exponent + 2), out rhs);
				BigInteger.SetUInt32(out result, 1u);
				BigInteger.SetUInt32(out result2, 2u);
			}
			ptr = &result2;
		}
		else
		{
			if (exponent > 0)
			{
				BigInteger.SetUInt64(out lhs, 2 * mantissa);
				lhs.ShiftLeft((uint)exponent);
				BigInteger.SetUInt32(out rhs, 2u);
				BigInteger.Pow2((uint)exponent, out result);
			}
			else
			{
				BigInteger.SetUInt64(out lhs, 2 * mantissa);
				BigInteger.Pow2((uint)(-exponent + 1), out rhs);
				BigInteger.SetUInt32(out result, 1u);
			}
			ptr = &result;
		}
		int num2 = (int)Math.Ceiling((double)((int)mantissaHighBitIdx + exponent) * 0.3010299956639812 - 0.69);
		if (num2 > 0)
		{
			rhs.MultiplyPow10((uint)num2);
		}
		else if (num2 < 0)
		{
			BigInteger.Pow10((uint)(-num2), out var result3);
			lhs.Multiply(ref result3);
			result.Multiply(ref result3);
			if (ptr != &result)
			{
				BigInteger.Multiply(ref result, 2u, out *ptr);
			}
		}
		bool flag = mantissa % 2 == 0;
		bool flag2 = false;
		if (cutoffNumber == -1)
		{
			BigInteger.Add(ref lhs, ref *ptr, out var result4);
			int num3 = BigInteger.Compare(ref result4, ref rhs);
			flag2 = (flag ? (num3 >= 0) : (num3 > 0));
		}
		else
		{
			flag2 = BigInteger.Compare(ref lhs, ref rhs) >= 0;
		}
		if (flag2)
		{
			num2++;
		}
		else
		{
			lhs.Multiply10();
			result.Multiply10();
			if (ptr != &result)
			{
				BigInteger.Multiply(ref result, 2u, out *ptr);
			}
		}
		int num4 = num2 - buffer.Length;
		if (cutoffNumber != -1)
		{
			int num5 = 0;
			num5 = ((!isSignificantDigits) ? (-cutoffNumber) : (num2 - cutoffNumber));
			if (num5 > num4)
			{
				num4 = num5;
			}
		}
		num2 = (decimalExponent = num2 - 1);
		uint block = rhs.GetBlock((uint)(rhs.GetLength() - 1));
		if (block < 8 || block > 429496729)
		{
			uint num6 = (uint)BitOperations.Log2(block);
			uint shift = (59 - num6) % 32;
			rhs.ShiftLeft(shift);
			lhs.ShiftLeft(shift);
			result.ShiftLeft(shift);
			if (ptr != &result)
			{
				BigInteger.Multiply(ref result, 2u, out *ptr);
			}
		}
		bool flag3;
		bool flag4;
		uint num7;
		if (cutoffNumber == -1)
		{
			while (true)
			{
				num7 = BigInteger.HeuristicDivide(ref lhs, ref rhs);
				BigInteger.Add(ref lhs, ref *ptr, out var result5);
				int num8 = BigInteger.Compare(ref lhs, ref result);
				int num9 = BigInteger.Compare(ref result5, ref rhs);
				if (flag)
				{
					flag3 = num8 <= 0;
					flag4 = num9 >= 0;
				}
				else
				{
					flag3 = num8 < 0;
					flag4 = num9 > 0;
				}
				if (flag3 || flag4 || num2 == num4)
				{
					break;
				}
				buffer[num] = (byte)(48 + num7);
				num++;
				lhs.Multiply10();
				result.Multiply10();
				if (ptr != &result)
				{
					BigInteger.Multiply(ref result, 2u, out *ptr);
				}
				num2--;
			}
		}
		else
		{
			if (num2 < num4)
			{
				num7 = BigInteger.HeuristicDivide(ref lhs, ref rhs);
				if (num7 > 5 || (num7 == 5 && !lhs.IsZero()))
				{
					decimalExponent++;
					num7 = 1u;
				}
				buffer[num] = (byte)(48 + num7);
				return (uint)(num + 1);
			}
			flag3 = false;
			flag4 = false;
			while (true)
			{
				num7 = BigInteger.HeuristicDivide(ref lhs, ref rhs);
				if (lhs.IsZero() || num2 <= num4)
				{
					break;
				}
				buffer[num] = (byte)(48 + num7);
				num++;
				lhs.Multiply10();
				num2--;
			}
		}
		bool flag5 = flag3;
		if (flag3 == flag4)
		{
			lhs.ShiftLeft(1u);
			int num10 = BigInteger.Compare(ref lhs, ref rhs);
			flag5 = num10 < 0;
			if (num10 == 0)
			{
				flag5 = (num7 & 1) == 0;
			}
		}
		if (flag5)
		{
			buffer[num] = (byte)(48 + num7);
			num++;
		}
		else if (num7 == 9)
		{
			while (true)
			{
				if (num == 0)
				{
					buffer[num] = 49;
					num++;
					decimalExponent++;
					break;
				}
				num--;
				if (buffer[num] != 57)
				{
					buffer[num]++;
					num++;
					break;
				}
			}
		}
		else
		{
			buffer[num] = (byte)(48 + num7 + 1);
			num++;
		}
		return (uint)num;
	}

	public unsafe static string FormatDecimal(decimal value, ReadOnlySpan<char> format, NumberFormatInfo info)
	{
		int digits;
		char c = ParseFormatSpecifier(format, out digits);
		byte* digits2 = stackalloc byte[31];
		NumberBuffer number = new NumberBuffer(NumberBufferKind.Decimal, digits2, 31);
		DecimalToNumber(ref value, ref number);
		char* pointer = stackalloc char[32];
		ValueListBuilder<char> vlb = new ValueListBuilder<char>(new Span<char>(pointer, 32));
		if (c != 0)
		{
			NumberToString(ref vlb, ref number, c, digits, info);
		}
		else
		{
			NumberToStringFormat(ref vlb, ref number, format, info);
		}
		string result = vlb.AsSpan().ToString();
		vlb.Dispose();
		return result;
	}

	public unsafe static bool TryFormatDecimal<TChar>(decimal value, ReadOnlySpan<char> format, NumberFormatInfo info, Span<TChar> destination, out int charsWritten) where TChar : unmanaged, IUtfChar<TChar>
	{
		int digits;
		char c = ParseFormatSpecifier(format, out digits);
		byte* digits2 = stackalloc byte[31];
		NumberBuffer number = new NumberBuffer(NumberBufferKind.Decimal, digits2, 31);
		DecimalToNumber(ref value, ref number);
		TChar* pointer = stackalloc TChar[32];
		ValueListBuilder<TChar> vlb = new ValueListBuilder<TChar>(new Span<TChar>(pointer, 32));
		if (c != 0)
		{
			NumberToString(ref vlb, ref number, c, digits, info);
		}
		else
		{
			NumberToStringFormat(ref vlb, ref number, format, info);
		}
		bool result = vlb.TryCopyTo(destination, out charsWritten);
		vlb.Dispose();
		return result;
	}

	internal unsafe static void DecimalToNumber(scoped ref decimal d, ref NumberBuffer number)
	{
		byte* digitsPointer = number.GetDigitsPointer();
		number.DigitsCount = 29;
		number.IsNegative = decimal.IsNegative(d);
		byte* bufferEnd = digitsPointer + 29;
		while ((d.Mid | d.High) != 0)
		{
			bufferEnd = UInt32ToDecChars(bufferEnd, decimal.DecDivMod1E9(ref d), 9);
		}
		bufferEnd = UInt32ToDecChars(bufferEnd, d.Low, 0);
		int num = (number.DigitsCount = (int)(digitsPointer + 29 - bufferEnd));
		number.Scale = num - d.Scale;
		byte* digitsPointer2 = number.GetDigitsPointer();
		while (--num >= 0)
		{
			*(digitsPointer2++) = *(bufferEnd++);
		}
		*digitsPointer2 = 0;
	}

	public static string FormatDouble(double value, string format, NumberFormatInfo info)
	{
		Span<char> initialSpan = stackalloc char[32];
		ValueListBuilder<char> vlb = new ValueListBuilder<char>(initialSpan);
		string result = FormatDouble(ref vlb, value, format, info) ?? vlb.AsSpan().ToString();
		vlb.Dispose();
		return result;
	}

	public static bool TryFormatDouble<TChar>(double value, ReadOnlySpan<char> format, NumberFormatInfo info, Span<TChar> destination, out int charsWritten) where TChar : unmanaged, IUtfChar<TChar>
	{
		Span<TChar> initialSpan = stackalloc TChar[32];
		ValueListBuilder<TChar> vlb = new ValueListBuilder<TChar>(initialSpan);
		string text = FormatDouble(ref vlb, value, format, info);
		bool result = ((text != null) ? TryCopyTo(text, destination, out charsWritten) : vlb.TryCopyTo(destination, out charsWritten));
		vlb.Dispose();
		return result;
	}

	private static int GetFloatingPointMaxDigitsAndPrecision(char fmt, ref int precision, NumberFormatInfo info, out bool isSignificantDigits)
	{
		if (fmt == '\0')
		{
			isSignificantDigits = true;
			return precision;
		}
		int result = precision;
		switch (fmt)
		{
		case 'C':
		case 'c':
			if (precision == -1)
			{
				precision = info.CurrencyDecimalDigits;
			}
			isSignificantDigits = false;
			break;
		case 'E':
		case 'e':
			if (precision == -1)
			{
				precision = 6;
			}
			precision++;
			isSignificantDigits = true;
			break;
		case 'F':
		case 'N':
		case 'f':
		case 'n':
			if (precision == -1)
			{
				precision = info.NumberDecimalDigits;
			}
			isSignificantDigits = false;
			break;
		case 'G':
		case 'g':
			if (precision == 0)
			{
				precision = -1;
			}
			isSignificantDigits = true;
			break;
		case 'P':
		case 'p':
			if (precision == -1)
			{
				precision = info.PercentDecimalDigits;
			}
			precision += 2;
			isSignificantDigits = false;
			break;
		case 'R':
		case 'r':
			precision = -1;
			isSignificantDigits = true;
			break;
		default:
			ThrowHelper.ThrowFormatException_BadFormatSpecifier();
			goto case 'R';
		}
		return result;
	}

	private unsafe static string FormatDouble<TChar>(ref ValueListBuilder<TChar> vlb, double value, ReadOnlySpan<char> format, NumberFormatInfo info) where TChar : unmanaged, IUtfChar<TChar>
	{
		if (!double.IsFinite(value))
		{
			if (double.IsNaN(value))
			{
				if (typeof(TChar) == typeof(char))
				{
					return info.NaNSymbol;
				}
				vlb.Append(info.NaNSymbolTChar<TChar>());
				return null;
			}
			if (typeof(TChar) == typeof(char))
			{
				if (!double.IsNegative(value))
				{
					return info.PositiveInfinitySymbol;
				}
				return info.NegativeInfinitySymbol;
			}
			vlb.Append(double.IsNegative(value) ? info.NegativeInfinitySymbolTChar<TChar>() : info.PositiveInfinitySymbolTChar<TChar>());
			return null;
		}
		int digits;
		char c = ParseFormatSpecifier(format, out digits);
		byte* digits2 = stackalloc byte[769];
		if (c == '\0')
		{
			digits = 15;
		}
		NumberBuffer number = new NumberBuffer(NumberBufferKind.FloatingPoint, digits2, 769);
		number.IsNegative = double.IsNegative(value);
		bool isSignificantDigits;
		int nMaxDigits = GetFloatingPointMaxDigitsAndPrecision(c, ref digits, info, out isSignificantDigits);
		if (value != 0.0 && (!isSignificantDigits || !Grisu3.TryRunDouble(value, digits, ref number)))
		{
			Dragon4Double(value, digits, isSignificantDigits, ref number);
		}
		if (c != 0)
		{
			if (digits == -1)
			{
				nMaxDigits = Math.Max(number.DigitsCount, 17);
			}
			NumberToString(ref vlb, ref number, c, nMaxDigits, info);
		}
		else
		{
			NumberToStringFormat(ref vlb, ref number, format, info);
		}
		return null;
	}

	public static string FormatSingle(float value, string format, NumberFormatInfo info)
	{
		Span<char> initialSpan = stackalloc char[32];
		ValueListBuilder<char> vlb = new ValueListBuilder<char>(initialSpan);
		string result = FormatSingle(ref vlb, value, format, info) ?? vlb.AsSpan().ToString();
		vlb.Dispose();
		return result;
	}

	public static bool TryFormatSingle<TChar>(float value, ReadOnlySpan<char> format, NumberFormatInfo info, Span<TChar> destination, out int charsWritten) where TChar : unmanaged, IUtfChar<TChar>
	{
		Span<TChar> initialSpan = stackalloc TChar[32];
		ValueListBuilder<TChar> vlb = new ValueListBuilder<TChar>(initialSpan);
		string text = FormatSingle(ref vlb, value, format, info);
		bool result = ((text != null) ? TryCopyTo(text, destination, out charsWritten) : vlb.TryCopyTo(destination, out charsWritten));
		vlb.Dispose();
		return result;
	}

	private unsafe static string FormatSingle<TChar>(ref ValueListBuilder<TChar> vlb, float value, ReadOnlySpan<char> format, NumberFormatInfo info) where TChar : unmanaged, IUtfChar<TChar>
	{
		if (!float.IsFinite(value))
		{
			if (float.IsNaN(value))
			{
				if (typeof(TChar) == typeof(char))
				{
					return info.NaNSymbol;
				}
				vlb.Append(info.NaNSymbolTChar<TChar>());
				return null;
			}
			if (typeof(TChar) == typeof(char))
			{
				if (!float.IsNegative(value))
				{
					return info.PositiveInfinitySymbol;
				}
				return info.NegativeInfinitySymbol;
			}
			vlb.Append(float.IsNegative(value) ? info.NegativeInfinitySymbolTChar<TChar>() : info.PositiveInfinitySymbolTChar<TChar>());
			return null;
		}
		int digits;
		char c = ParseFormatSpecifier(format, out digits);
		byte* digits2 = stackalloc byte[114];
		if (c == '\0')
		{
			digits = 7;
		}
		NumberBuffer number = new NumberBuffer(NumberBufferKind.FloatingPoint, digits2, 114);
		number.IsNegative = float.IsNegative(value);
		bool isSignificantDigits;
		int nMaxDigits = GetFloatingPointMaxDigitsAndPrecision(c, ref digits, info, out isSignificantDigits);
		if (value != 0f && (!isSignificantDigits || !Grisu3.TryRunSingle(value, digits, ref number)))
		{
			Dragon4Single(value, digits, isSignificantDigits, ref number);
		}
		if (c != 0)
		{
			if (digits == -1)
			{
				nMaxDigits = Math.Max(number.DigitsCount, 9);
			}
			NumberToString(ref vlb, ref number, c, nMaxDigits, info);
		}
		else
		{
			NumberToStringFormat(ref vlb, ref number, format, info);
		}
		return null;
	}

	public static string FormatHalf(Half value, string format, NumberFormatInfo info)
	{
		Span<char> initialSpan = stackalloc char[32];
		ValueListBuilder<char> vlb = new ValueListBuilder<char>(initialSpan);
		string result = FormatHalf(ref vlb, value, format, info) ?? vlb.AsSpan().ToString();
		vlb.Dispose();
		return result;
	}

	private unsafe static string FormatHalf<TChar>(ref ValueListBuilder<TChar> vlb, Half value, ReadOnlySpan<char> format, NumberFormatInfo info) where TChar : unmanaged, IUtfChar<TChar>
	{
		if (!Half.IsFinite(value))
		{
			if (Half.IsNaN(value))
			{
				if (typeof(TChar) == typeof(char))
				{
					return info.NaNSymbol;
				}
				vlb.Append(info.NaNSymbolTChar<TChar>());
				return null;
			}
			if (typeof(TChar) == typeof(char))
			{
				if (!Half.IsNegative(value))
				{
					return info.PositiveInfinitySymbol;
				}
				return info.NegativeInfinitySymbol;
			}
			vlb.Append(Half.IsNegative(value) ? info.NegativeInfinitySymbolTChar<TChar>() : info.PositiveInfinitySymbolTChar<TChar>());
			return null;
		}
		int digits;
		char c = ParseFormatSpecifier(format, out digits);
		byte* digits2 = stackalloc byte[21];
		if (c == '\0')
		{
			digits = 5;
		}
		NumberBuffer number = new NumberBuffer(NumberBufferKind.FloatingPoint, digits2, 21);
		number.IsNegative = Half.IsNegative(value);
		bool isSignificantDigits;
		int nMaxDigits = GetFloatingPointMaxDigitsAndPrecision(c, ref digits, info, out isSignificantDigits);
		if (value != default(Half) && (!isSignificantDigits || !Grisu3.TryRunHalf(value, digits, ref number)))
		{
			Dragon4Half(value, digits, isSignificantDigits, ref number);
		}
		if (c != 0)
		{
			if (digits == -1)
			{
				nMaxDigits = Math.Max(number.DigitsCount, 5);
			}
			NumberToString(ref vlb, ref number, c, nMaxDigits, info);
		}
		else
		{
			NumberToStringFormat(ref vlb, ref number, format, info);
		}
		return null;
	}

	public static bool TryFormatHalf<TChar>(Half value, ReadOnlySpan<char> format, NumberFormatInfo info, Span<TChar> destination, out int charsWritten) where TChar : unmanaged, IUtfChar<TChar>
	{
		Span<TChar> initialSpan = stackalloc TChar[32];
		ValueListBuilder<TChar> vlb = new ValueListBuilder<TChar>(initialSpan);
		string text = FormatHalf(ref vlb, value, format, info);
		bool result = ((text != null) ? TryCopyTo(text, destination, out charsWritten) : vlb.TryCopyTo(destination, out charsWritten));
		vlb.Dispose();
		return result;
	}

	private static bool TryCopyTo<TChar>(string source, Span<TChar> destination, out int charsWritten) where TChar : unmanaged, IUtfChar<TChar>
	{
		if (typeof(TChar) == typeof(char))
		{
			if (source.TryCopyTo(MemoryMarshal.Cast<TChar, char>(destination)))
			{
				charsWritten = source.Length;
				return true;
			}
			charsWritten = 0;
			return false;
		}
		return Encoding.UTF8.TryGetBytes(source, MemoryMarshal.Cast<TChar, byte>(destination), out charsWritten);
	}

	internal static char GetHexBase(char fmt)
	{
		return (char)(fmt - 33);
	}

	public static string FormatInt32(int value, int hexMask, string format, IFormatProvider provider)
	{
		if (string.IsNullOrEmpty(format))
		{
			if (value < 0)
			{
				return NegativeInt32ToDecStr(value, -1, NumberFormatInfo.GetInstance(provider).NegativeSign);
			}
			return UInt32ToDecStr((uint)value);
		}
		return FormatInt32Slow(value, hexMask, format, provider);
		unsafe static string FormatInt32Slow(int value, int hexMask, string format, IFormatProvider provider)
		{
			ReadOnlySpan<char> format2 = format;
			int digits;
			char c = ParseFormatSpecifier(format2, out digits);
			char c2 = (char)(c & 0xFFDFu);
			if ((c2 == 'G') ? (digits < 1) : (c2 == 'D'))
			{
				if (value < 0)
				{
					return NegativeInt32ToDecStr(value, digits, NumberFormatInfo.GetInstance(provider).NegativeSign);
				}
				return UInt32ToDecStr((uint)value, digits);
			}
			switch (c2)
			{
			case 'X':
				return Int32ToHexStr(value & hexMask, GetHexBase(c), digits);
			case 'B':
				return UInt32ToBinaryStr((uint)(value & hexMask), digits);
			default:
			{
				NumberFormatInfo instance = NumberFormatInfo.GetInstance(provider);
				byte* digits2 = stackalloc byte[11];
				NumberBuffer number = new NumberBuffer(NumberBufferKind.Integer, digits2, 11);
				Int32ToNumber(value, ref number);
				char* pointer = stackalloc char[32];
				ValueListBuilder<char> vlb = new ValueListBuilder<char>(new Span<char>(pointer, 32));
				if (c != 0)
				{
					NumberToString(ref vlb, ref number, c, digits, instance);
				}
				else
				{
					NumberToStringFormat(ref vlb, ref number, format2, instance);
				}
				string result = vlb.AsSpan().ToString();
				vlb.Dispose();
				return result;
			}
			}
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool TryFormatInt32<TChar>(int value, int hexMask, ReadOnlySpan<char> format, IFormatProvider provider, Span<TChar> destination, out int charsWritten) where TChar : unmanaged, IUtfChar<TChar>
	{
		if (format.Length == 0)
		{
			if (value < 0)
			{
				return TryNegativeInt32ToDecStr(value, -1, NumberFormatInfo.GetInstance(provider).NegativeSignTChar<TChar>(), destination, out charsWritten);
			}
			return TryUInt32ToDecStr((uint)value, destination, out charsWritten);
		}
		return TryFormatInt32Slow(value, hexMask, format, provider, destination, out charsWritten);
		unsafe static bool TryFormatInt32Slow(int value, int hexMask, ReadOnlySpan<char> format, IFormatProvider provider, Span<TChar> destination, out int charsWritten)
		{
			int digits;
			char c = ParseFormatSpecifier(format, out digits);
			char c2 = (char)(c & 0xFFDFu);
			if ((c2 == 'G') ? (digits < 1) : (c2 == 'D'))
			{
				if (value < 0)
				{
					return TryNegativeInt32ToDecStr(value, digits, NumberFormatInfo.GetInstance(provider).NegativeSignTChar<TChar>(), destination, out charsWritten);
				}
				return TryUInt32ToDecStr((uint)value, digits, destination, out charsWritten);
			}
			switch (c2)
			{
			case 'X':
				return TryInt32ToHexStr(value & hexMask, GetHexBase(c), digits, destination, out charsWritten);
			case 'B':
				return TryUInt32ToBinaryStr((uint)(value & hexMask), digits, destination, out charsWritten);
			default:
			{
				NumberFormatInfo instance = NumberFormatInfo.GetInstance(provider);
				byte* digits2 = stackalloc byte[11];
				NumberBuffer number = new NumberBuffer(NumberBufferKind.Integer, digits2, 11);
				Int32ToNumber(value, ref number);
				TChar* pointer = stackalloc TChar[32];
				ValueListBuilder<TChar> vlb = new ValueListBuilder<TChar>(new Span<TChar>(pointer, 32));
				if (c != 0)
				{
					NumberToString(ref vlb, ref number, c, digits, instance);
				}
				else
				{
					NumberToStringFormat(ref vlb, ref number, format, instance);
				}
				bool result = vlb.TryCopyTo(destination, out charsWritten);
				vlb.Dispose();
				return result;
			}
			}
		}
	}

	public static string FormatUInt32(uint value, string format, IFormatProvider provider)
	{
		if (string.IsNullOrEmpty(format))
		{
			return UInt32ToDecStr(value);
		}
		return FormatUInt32Slow(value, format, provider);
		unsafe static string FormatUInt32Slow(uint value, string format, IFormatProvider provider)
		{
			ReadOnlySpan<char> format2 = format;
			int digits;
			char c = ParseFormatSpecifier(format2, out digits);
			char c2 = (char)(c & 0xFFDFu);
			if ((c2 == 'G') ? (digits < 1) : (c2 == 'D'))
			{
				return UInt32ToDecStr(value, digits);
			}
			switch (c2)
			{
			case 'X':
				return Int32ToHexStr((int)value, GetHexBase(c), digits);
			case 'B':
				return UInt32ToBinaryStr(value, digits);
			default:
			{
				NumberFormatInfo instance = NumberFormatInfo.GetInstance(provider);
				byte* digits2 = stackalloc byte[11];
				NumberBuffer number = new NumberBuffer(NumberBufferKind.Integer, digits2, 11);
				UInt32ToNumber(value, ref number);
				char* pointer = stackalloc char[32];
				ValueListBuilder<char> vlb = new ValueListBuilder<char>(new Span<char>(pointer, 32));
				if (c != 0)
				{
					NumberToString(ref vlb, ref number, c, digits, instance);
				}
				else
				{
					NumberToStringFormat(ref vlb, ref number, format2, instance);
				}
				string result = vlb.AsSpan().ToString();
				vlb.Dispose();
				return result;
			}
			}
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool TryFormatUInt32<TChar>(uint value, ReadOnlySpan<char> format, IFormatProvider provider, Span<TChar> destination, out int charsWritten) where TChar : unmanaged, IUtfChar<TChar>
	{
		if (format.Length == 0)
		{
			return TryUInt32ToDecStr(value, destination, out charsWritten);
		}
		return TryFormatUInt32Slow(value, format, provider, destination, out charsWritten);
		unsafe static bool TryFormatUInt32Slow(uint value, ReadOnlySpan<char> format, IFormatProvider provider, Span<TChar> destination, out int charsWritten)
		{
			int digits;
			char c = ParseFormatSpecifier(format, out digits);
			char c2 = (char)(c & 0xFFDFu);
			if ((c2 == 'G') ? (digits < 1) : (c2 == 'D'))
			{
				return TryUInt32ToDecStr(value, digits, destination, out charsWritten);
			}
			switch (c2)
			{
			case 'X':
				return TryInt32ToHexStr((int)value, GetHexBase(c), digits, destination, out charsWritten);
			case 'B':
				return TryUInt32ToBinaryStr(value, digits, destination, out charsWritten);
			default:
			{
				NumberFormatInfo instance = NumberFormatInfo.GetInstance(provider);
				byte* digits2 = stackalloc byte[11];
				NumberBuffer number = new NumberBuffer(NumberBufferKind.Integer, digits2, 11);
				UInt32ToNumber(value, ref number);
				TChar* pointer = stackalloc TChar[32];
				ValueListBuilder<TChar> vlb = new ValueListBuilder<TChar>(new Span<TChar>(pointer, 32));
				if (c != 0)
				{
					NumberToString(ref vlb, ref number, c, digits, instance);
				}
				else
				{
					NumberToStringFormat(ref vlb, ref number, format, instance);
				}
				bool result = vlb.TryCopyTo(destination, out charsWritten);
				vlb.Dispose();
				return result;
			}
			}
		}
	}

	public static string FormatInt64(long value, string format, IFormatProvider provider)
	{
		if (string.IsNullOrEmpty(format))
		{
			if (value < 0)
			{
				return NegativeInt64ToDecStr(value, -1, NumberFormatInfo.GetInstance(provider).NegativeSign);
			}
			return UInt64ToDecStr((ulong)value);
		}
		return FormatInt64Slow(value, format, provider);
		unsafe static string FormatInt64Slow(long value, string format, IFormatProvider provider)
		{
			ReadOnlySpan<char> format2 = format;
			int digits;
			char c = ParseFormatSpecifier(format2, out digits);
			char c2 = (char)(c & 0xFFDFu);
			if ((c2 == 'G') ? (digits < 1) : (c2 == 'D'))
			{
				if (value < 0)
				{
					return NegativeInt64ToDecStr(value, digits, NumberFormatInfo.GetInstance(provider).NegativeSign);
				}
				return UInt64ToDecStr((ulong)value, digits);
			}
			switch (c2)
			{
			case 'X':
				return Int64ToHexStr(value, GetHexBase(c), digits);
			case 'B':
				return UInt64ToBinaryStr((ulong)value, digits);
			default:
			{
				NumberFormatInfo instance = NumberFormatInfo.GetInstance(provider);
				byte* digits2 = stackalloc byte[20];
				NumberBuffer number = new NumberBuffer(NumberBufferKind.Integer, digits2, 20);
				Int64ToNumber(value, ref number);
				char* pointer = stackalloc char[32];
				ValueListBuilder<char> vlb = new ValueListBuilder<char>(new Span<char>(pointer, 32));
				if (c != 0)
				{
					NumberToString(ref vlb, ref number, c, digits, instance);
				}
				else
				{
					NumberToStringFormat(ref vlb, ref number, format2, instance);
				}
				string result = vlb.AsSpan().ToString();
				vlb.Dispose();
				return result;
			}
			}
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool TryFormatInt64<TChar>(long value, ReadOnlySpan<char> format, IFormatProvider provider, Span<TChar> destination, out int charsWritten) where TChar : unmanaged, IUtfChar<TChar>
	{
		if (format.Length == 0)
		{
			if (value < 0)
			{
				return TryNegativeInt64ToDecStr(value, -1, NumberFormatInfo.GetInstance(provider).NegativeSignTChar<TChar>(), destination, out charsWritten);
			}
			return TryUInt64ToDecStr((ulong)value, destination, out charsWritten);
		}
		return TryFormatInt64Slow(value, format, provider, destination, out charsWritten);
		unsafe static bool TryFormatInt64Slow(long value, ReadOnlySpan<char> format, IFormatProvider provider, Span<TChar> destination, out int charsWritten)
		{
			int digits;
			char c = ParseFormatSpecifier(format, out digits);
			char c2 = (char)(c & 0xFFDFu);
			if ((c2 == 'G') ? (digits < 1) : (c2 == 'D'))
			{
				if (value < 0)
				{
					return TryNegativeInt64ToDecStr(value, digits, NumberFormatInfo.GetInstance(provider).NegativeSignTChar<TChar>(), destination, out charsWritten);
				}
				return TryUInt64ToDecStr((ulong)value, digits, destination, out charsWritten);
			}
			switch (c2)
			{
			case 'X':
				return TryInt64ToHexStr(value, GetHexBase(c), digits, destination, out charsWritten);
			case 'B':
				return TryUInt64ToBinaryStr((ulong)value, digits, destination, out charsWritten);
			default:
			{
				NumberFormatInfo instance = NumberFormatInfo.GetInstance(provider);
				byte* digits2 = stackalloc byte[20];
				NumberBuffer number = new NumberBuffer(NumberBufferKind.Integer, digits2, 20);
				Int64ToNumber(value, ref number);
				char* pointer = stackalloc char[32];
				ValueListBuilder<TChar> vlb = new ValueListBuilder<TChar>(new Span<TChar>(pointer, 32));
				if (c != 0)
				{
					NumberToString(ref vlb, ref number, c, digits, instance);
				}
				else
				{
					NumberToStringFormat(ref vlb, ref number, format, instance);
				}
				bool result = vlb.TryCopyTo(destination, out charsWritten);
				vlb.Dispose();
				return result;
			}
			}
		}
	}

	public static string FormatUInt64(ulong value, string format, IFormatProvider provider)
	{
		if (string.IsNullOrEmpty(format))
		{
			return UInt64ToDecStr(value);
		}
		return FormatUInt64Slow(value, format, provider);
		unsafe static string FormatUInt64Slow(ulong value, string format, IFormatProvider provider)
		{
			ReadOnlySpan<char> format2 = format;
			int digits;
			char c = ParseFormatSpecifier(format2, out digits);
			char c2 = (char)(c & 0xFFDFu);
			if ((c2 == 'G') ? (digits < 1) : (c2 == 'D'))
			{
				return UInt64ToDecStr(value, digits);
			}
			switch (c2)
			{
			case 'X':
				return Int64ToHexStr((long)value, GetHexBase(c), digits);
			case 'B':
				return UInt64ToBinaryStr(value, digits);
			default:
			{
				NumberFormatInfo instance = NumberFormatInfo.GetInstance(provider);
				byte* digits2 = stackalloc byte[21];
				NumberBuffer number = new NumberBuffer(NumberBufferKind.Integer, digits2, 21);
				UInt64ToNumber(value, ref number);
				char* pointer = stackalloc char[32];
				ValueListBuilder<char> vlb = new ValueListBuilder<char>(new Span<char>(pointer, 32));
				if (c != 0)
				{
					NumberToString(ref vlb, ref number, c, digits, instance);
				}
				else
				{
					NumberToStringFormat(ref vlb, ref number, format2, instance);
				}
				string result = vlb.AsSpan().ToString();
				vlb.Dispose();
				return result;
			}
			}
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool TryFormatUInt64<TChar>(ulong value, ReadOnlySpan<char> format, IFormatProvider provider, Span<TChar> destination, out int charsWritten) where TChar : unmanaged, IUtfChar<TChar>
	{
		if (format.Length == 0)
		{
			return TryUInt64ToDecStr(value, destination, out charsWritten);
		}
		return TryFormatUInt64Slow(value, format, provider, destination, out charsWritten);
		unsafe static bool TryFormatUInt64Slow(ulong value, ReadOnlySpan<char> format, IFormatProvider provider, Span<TChar> destination, out int charsWritten)
		{
			int digits;
			char c = ParseFormatSpecifier(format, out digits);
			char c2 = (char)(c & 0xFFDFu);
			if ((c2 == 'G') ? (digits < 1) : (c2 == 'D'))
			{
				return TryUInt64ToDecStr(value, digits, destination, out charsWritten);
			}
			switch (c2)
			{
			case 'X':
				return TryInt64ToHexStr((long)value, GetHexBase(c), digits, destination, out charsWritten);
			case 'B':
				return TryUInt64ToBinaryStr(value, digits, destination, out charsWritten);
			default:
			{
				NumberFormatInfo instance = NumberFormatInfo.GetInstance(provider);
				byte* digits2 = stackalloc byte[21];
				NumberBuffer number = new NumberBuffer(NumberBufferKind.Integer, digits2, 21);
				UInt64ToNumber(value, ref number);
				TChar* pointer = stackalloc TChar[32];
				ValueListBuilder<TChar> vlb = new ValueListBuilder<TChar>(new Span<TChar>(pointer, 32));
				if (c != 0)
				{
					NumberToString(ref vlb, ref number, c, digits, instance);
				}
				else
				{
					NumberToStringFormat(ref vlb, ref number, format, instance);
				}
				bool result = vlb.TryCopyTo(destination, out charsWritten);
				vlb.Dispose();
				return result;
			}
			}
		}
	}

	public static string FormatInt128(Int128 value, string format, IFormatProvider provider)
	{
		if (string.IsNullOrEmpty(format))
		{
			if (!Int128.IsPositive(value))
			{
				return NegativeInt128ToDecStr(value, -1, NumberFormatInfo.GetInstance(provider).NegativeSign);
			}
			return UInt128ToDecStr((UInt128)value, -1);
		}
		return FormatInt128Slow(value, format, provider);
		unsafe static string FormatInt128Slow(Int128 value, string format, IFormatProvider provider)
		{
			ReadOnlySpan<char> format2 = format;
			int digits;
			char c = ParseFormatSpecifier(format2, out digits);
			char c2 = (char)(c & 0xFFDFu);
			if ((c2 == 'G') ? (digits < 1) : (c2 == 'D'))
			{
				if (!Int128.IsPositive(value))
				{
					return NegativeInt128ToDecStr(value, digits, NumberFormatInfo.GetInstance(provider).NegativeSign);
				}
				return UInt128ToDecStr((UInt128)value, digits);
			}
			switch (c2)
			{
			case 'X':
				return Int128ToHexStr(value, GetHexBase(c), digits);
			case 'B':
				return UInt128ToBinaryStr(value, digits);
			default:
			{
				NumberFormatInfo instance = NumberFormatInfo.GetInstance(provider);
				byte* digits2 = stackalloc byte[40];
				NumberBuffer number = new NumberBuffer(NumberBufferKind.Integer, digits2, 40);
				Int128ToNumber(value, ref number);
				char* pointer = stackalloc char[32];
				ValueListBuilder<char> vlb = new ValueListBuilder<char>(new Span<char>(pointer, 32));
				if (c != 0)
				{
					NumberToString(ref vlb, ref number, c, digits, instance);
				}
				else
				{
					NumberToStringFormat(ref vlb, ref number, format2, instance);
				}
				string result = vlb.AsSpan().ToString();
				vlb.Dispose();
				return result;
			}
			}
		}
	}

	public static bool TryFormatInt128<TChar>(Int128 value, ReadOnlySpan<char> format, IFormatProvider provider, Span<TChar> destination, out int charsWritten) where TChar : unmanaged, IUtfChar<TChar>
	{
		if (format.Length == 0)
		{
			if (!Int128.IsPositive(value))
			{
				return TryNegativeInt128ToDecStr(value, -1, NumberFormatInfo.GetInstance(provider).NegativeSignTChar<TChar>(), destination, out charsWritten);
			}
			return TryUInt128ToDecStr((UInt128)value, -1, destination, out charsWritten);
		}
		return TryFormatInt128Slow(value, format, provider, destination, out charsWritten);
		unsafe static bool TryFormatInt128Slow(Int128 value, ReadOnlySpan<char> format, IFormatProvider provider, Span<TChar> destination, out int charsWritten)
		{
			int digits;
			char c = ParseFormatSpecifier(format, out digits);
			char c2 = (char)(c & 0xFFDFu);
			if ((c2 == 'G') ? (digits < 1) : (c2 == 'D'))
			{
				if (!Int128.IsPositive(value))
				{
					return TryNegativeInt128ToDecStr(value, digits, NumberFormatInfo.GetInstance(provider).NegativeSignTChar<TChar>(), destination, out charsWritten);
				}
				return TryUInt128ToDecStr((UInt128)value, digits, destination, out charsWritten);
			}
			switch (c2)
			{
			case 'X':
				return TryInt128ToHexStr(value, GetHexBase(c), digits, destination, out charsWritten);
			case 'B':
				return TryUInt128ToBinaryStr(value, digits, destination, out charsWritten);
			default:
			{
				NumberFormatInfo instance = NumberFormatInfo.GetInstance(provider);
				byte* digits2 = stackalloc byte[40];
				NumberBuffer number = new NumberBuffer(NumberBufferKind.Integer, digits2, 40);
				Int128ToNumber(value, ref number);
				TChar* pointer = stackalloc TChar[32];
				ValueListBuilder<TChar> vlb = new ValueListBuilder<TChar>(new Span<TChar>(pointer, 32));
				if (c != 0)
				{
					NumberToString(ref vlb, ref number, c, digits, instance);
				}
				else
				{
					NumberToStringFormat(ref vlb, ref number, format, instance);
				}
				bool result = vlb.TryCopyTo(destination, out charsWritten);
				vlb.Dispose();
				return result;
			}
			}
		}
	}

	public static string FormatUInt128(UInt128 value, string format, IFormatProvider provider)
	{
		if (string.IsNullOrEmpty(format))
		{
			return UInt128ToDecStr(value, -1);
		}
		return FormatUInt128Slow(value, format, provider);
		unsafe static string FormatUInt128Slow(UInt128 value, string format, IFormatProvider provider)
		{
			ReadOnlySpan<char> format2 = format;
			int digits;
			char c = ParseFormatSpecifier(format2, out digits);
			char c2 = (char)(c & 0xFFDFu);
			if ((c2 == 'G') ? (digits < 1) : (c2 == 'D'))
			{
				return UInt128ToDecStr(value, digits);
			}
			switch (c2)
			{
			case 'X':
				return Int128ToHexStr((Int128)value, GetHexBase(c), digits);
			case 'B':
				return UInt128ToBinaryStr((Int128)value, digits);
			default:
			{
				NumberFormatInfo instance = NumberFormatInfo.GetInstance(provider);
				byte* digits2 = stackalloc byte[40];
				NumberBuffer number = new NumberBuffer(NumberBufferKind.Integer, digits2, 40);
				UInt128ToNumber(value, ref number);
				char* pointer = stackalloc char[32];
				ValueListBuilder<char> vlb = new ValueListBuilder<char>(new Span<char>(pointer, 32));
				if (c != 0)
				{
					NumberToString(ref vlb, ref number, c, digits, instance);
				}
				else
				{
					NumberToStringFormat(ref vlb, ref number, format2, instance);
				}
				string result = vlb.AsSpan().ToString();
				vlb.Dispose();
				return result;
			}
			}
		}
	}

	public static bool TryFormatUInt128<TChar>(UInt128 value, ReadOnlySpan<char> format, IFormatProvider provider, Span<TChar> destination, out int charsWritten) where TChar : unmanaged, IUtfChar<TChar>
	{
		if (format.Length == 0)
		{
			return TryUInt128ToDecStr(value, -1, destination, out charsWritten);
		}
		return TryFormatUInt128Slow(value, format, provider, destination, out charsWritten);
		unsafe static bool TryFormatUInt128Slow(UInt128 value, ReadOnlySpan<char> format, IFormatProvider provider, Span<TChar> destination, out int charsWritten)
		{
			int digits;
			char c = ParseFormatSpecifier(format, out digits);
			char c2 = (char)(c & 0xFFDFu);
			if ((c2 == 'G') ? (digits < 1) : (c2 == 'D'))
			{
				return TryUInt128ToDecStr(value, digits, destination, out charsWritten);
			}
			switch (c2)
			{
			case 'X':
				return TryInt128ToHexStr((Int128)value, GetHexBase(c), digits, destination, out charsWritten);
			case 'B':
				return TryUInt128ToBinaryStr((Int128)value, digits, destination, out charsWritten);
			default:
			{
				NumberFormatInfo instance = NumberFormatInfo.GetInstance(provider);
				byte* digits2 = stackalloc byte[40];
				NumberBuffer number = new NumberBuffer(NumberBufferKind.Integer, digits2, 40);
				UInt128ToNumber(value, ref number);
				TChar* pointer = stackalloc TChar[32];
				ValueListBuilder<TChar> vlb = new ValueListBuilder<TChar>(new Span<TChar>(pointer, 32));
				if (c != 0)
				{
					NumberToString(ref vlb, ref number, c, digits, instance);
				}
				else
				{
					NumberToStringFormat(ref vlb, ref number, format, instance);
				}
				bool result = vlb.TryCopyTo(destination, out charsWritten);
				vlb.Dispose();
				return result;
			}
			}
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private unsafe static void Int32ToNumber(int value, ref NumberBuffer number)
	{
		number.DigitsCount = 10;
		if (value >= 0)
		{
			number.IsNegative = false;
		}
		else
		{
			number.IsNegative = true;
			value = -value;
		}
		byte* digitsPointer = number.GetDigitsPointer();
		byte* ptr = UInt32ToDecChars(digitsPointer + 10, (uint)value, 0);
		int num = (number.Scale = (number.DigitsCount = (int)(digitsPointer + 10 - ptr)));
		byte* digitsPointer2 = number.GetDigitsPointer();
		while (--num >= 0)
		{
			*(digitsPointer2++) = *(ptr++);
		}
		*digitsPointer2 = 0;
	}

	public static string Int32ToDecStr(int value)
	{
		if (value < 0)
		{
			return NegativeInt32ToDecStr(value, -1, NumberFormatInfo.CurrentInfo.NegativeSign);
		}
		return UInt32ToDecStr((uint)value);
	}

	private unsafe static string NegativeInt32ToDecStr(int value, int digits, string sNegative)
	{
		if (digits < 1)
		{
			digits = 1;
		}
		int num = Math.Max(digits, FormattingHelpers.CountDigits((uint)(-value))) + sNegative.Length;
		string text = string.FastAllocateString(num);
		fixed (char* ptr = text)
		{
			char* ptr2 = UInt32ToDecChars(ptr + num, (uint)(-value), digits);
			for (int num2 = sNegative.Length - 1; num2 >= 0; num2--)
			{
				*(--ptr2) = sNegative[num2];
			}
		}
		return text;
	}

	internal unsafe static bool TryNegativeInt32ToDecStr<TChar>(int value, int digits, ReadOnlySpan<TChar> sNegative, Span<TChar> destination, out int charsWritten) where TChar : unmanaged, IUtfChar<TChar>
	{
		if (digits < 1)
		{
			digits = 1;
		}
		int num = Math.Max(digits, FormattingHelpers.CountDigits((uint)(-value))) + sNegative.Length;
		if (num > destination.Length)
		{
			charsWritten = 0;
			return false;
		}
		charsWritten = num;
		fixed (TChar* ptr = &MemoryMarshal.GetReference(destination))
		{
			TChar* ptr2 = UInt32ToDecChars(ptr + num, (uint)(-value), digits);
			for (int num2 = sNegative.Length - 1; num2 >= 0; num2--)
			{
				*(--ptr2) = sNegative[num2];
			}
		}
		return true;
	}

	private unsafe static string Int32ToHexStr(int value, char hexBase, int digits)
	{
		if (digits < 1)
		{
			digits = 1;
		}
		int num = Math.Max(digits, FormattingHelpers.CountHexDigits((uint)value));
		string text = string.FastAllocateString(num);
		fixed (char* ptr = text)
		{
			char* ptr2 = Int32ToHexChars(ptr + num, (uint)value, hexBase, digits);
		}
		return text;
	}

	internal unsafe static bool TryInt32ToHexStr<TChar>(int value, char hexBase, int digits, Span<TChar> destination, out int charsWritten) where TChar : unmanaged, IUtfChar<TChar>
	{
		if (digits < 1)
		{
			digits = 1;
		}
		int num = Math.Max(digits, FormattingHelpers.CountHexDigits((uint)value));
		if (num > destination.Length)
		{
			charsWritten = 0;
			return false;
		}
		charsWritten = num;
		fixed (TChar* ptr = &MemoryMarshal.GetReference(destination))
		{
			TChar* ptr2 = Int32ToHexChars(ptr + num, (uint)value, hexBase, digits);
		}
		return true;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private unsafe static TChar* Int32ToHexChars<TChar>(TChar* buffer, uint value, int hexBase, int digits) where TChar : unmanaged, IUtfChar<TChar>
	{
		while (--digits >= 0 || value != 0)
		{
			byte b = (byte)(value & 0xFu);
			*(--buffer) = TChar.CastFrom(b + ((b < 10) ? 48 : hexBase));
			value >>= 4;
		}
		return buffer;
	}

	private unsafe static string UInt32ToBinaryStr(uint value, int digits)
	{
		if (digits < 1)
		{
			digits = 1;
		}
		int num = Math.Max(digits, (int)(32 - uint.LeadingZeroCount(value)));
		string text = string.FastAllocateString(num);
		fixed (char* ptr = text)
		{
			char* ptr2 = UInt32ToBinaryChars(ptr + num, value, digits);
		}
		return text;
	}

	private unsafe static bool TryUInt32ToBinaryStr<TChar>(uint value, int digits, Span<TChar> destination, out int charsWritten) where TChar : unmanaged, IUtfChar<TChar>
	{
		if (digits < 1)
		{
			digits = 1;
		}
		int num = Math.Max(digits, (int)(32 - uint.LeadingZeroCount(value)));
		if (num > destination.Length)
		{
			charsWritten = 0;
			return false;
		}
		charsWritten = num;
		fixed (TChar* ptr = &MemoryMarshal.GetReference(destination))
		{
			TChar* ptr2 = UInt32ToBinaryChars(ptr + num, value, digits);
		}
		return true;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private unsafe static TChar* UInt32ToBinaryChars<TChar>(TChar* buffer, uint value, int digits) where TChar : unmanaged, IUtfChar<TChar>
	{
		while (--digits >= 0 || value != 0)
		{
			*(--buffer) = TChar.CastFrom(48 + (byte)(value & 1));
			value >>= 1;
		}
		return buffer;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private unsafe static void UInt32ToNumber(uint value, ref NumberBuffer number)
	{
		number.DigitsCount = 10;
		number.IsNegative = false;
		byte* digitsPointer = number.GetDigitsPointer();
		byte* ptr = UInt32ToDecChars(digitsPointer + 10, value, 0);
		int num = (number.Scale = (number.DigitsCount = (int)(digitsPointer + 10 - ptr)));
		byte* digitsPointer2 = number.GetDigitsPointer();
		while (--num >= 0)
		{
			*(digitsPointer2++) = *(ptr++);
		}
		*digitsPointer2 = 0;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal unsafe static void WriteTwoDigits<TChar>(uint value, TChar* ptr) where TChar : unmanaged, IUtfChar<TChar>
	{
		Unsafe.CopyBlockUnaligned(ref *(byte*)ptr, ref Unsafe.Add(ref MemoryMarshal.GetArrayDataReference((typeof(TChar) == typeof(char)) ? TwoDigitsCharsAsBytes : TwoDigitsBytes), (uint)(sizeof(TChar) * 2) * value), (uint)(sizeof(TChar) * 2));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal unsafe static void WriteFourDigits<TChar>(uint value, TChar* ptr) where TChar : unmanaged, IUtfChar<TChar>
	{
		(uint Quotient, uint Remainder) tuple = Math.DivRem(value, 100u);
		value = tuple.Quotient;
		uint item = tuple.Remainder;
		ref byte arrayDataReference = ref MemoryMarshal.GetArrayDataReference((typeof(TChar) == typeof(char)) ? TwoDigitsCharsAsBytes : TwoDigitsBytes);
		Unsafe.CopyBlockUnaligned(ref *(byte*)ptr, ref Unsafe.Add(ref arrayDataReference, (uint)(sizeof(TChar) * 2) * value), (uint)(sizeof(TChar) * 2));
		Unsafe.CopyBlockUnaligned(ref *(byte*)(ptr + 2), ref Unsafe.Add(ref arrayDataReference, (uint)(sizeof(TChar) * 2) * item), (uint)(sizeof(TChar) * 2));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal unsafe static void WriteDigits<TChar>(uint value, TChar* ptr, int count) where TChar : unmanaged, IUtfChar<TChar>
	{
		TChar* ptr2;
		for (ptr2 = ptr + count - 1; ptr2 > ptr; ptr2--)
		{
			uint num = 48 + value;
			value /= 10;
			*ptr2 = TChar.CastFrom(num - value * 10);
		}
		*ptr2 = TChar.CastFrom(48 + value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal unsafe static TChar* UInt32ToDecChars<TChar>(TChar* bufferEnd, uint value) where TChar : unmanaged, IUtfChar<TChar>
	{
		if (value >= 10)
		{
			while (true)
			{
				switch (value)
				{
				default:
					goto IL_0007;
				case 10u:
				case 11u:
				case 12u:
				case 13u:
				case 14u:
				case 15u:
				case 16u:
				case 17u:
				case 18u:
				case 19u:
				case 20u:
				case 21u:
				case 22u:
				case 23u:
				case 24u:
				case 25u:
				case 26u:
				case 27u:
				case 28u:
				case 29u:
				case 30u:
				case 31u:
				case 32u:
				case 33u:
				case 34u:
				case 35u:
				case 36u:
				case 37u:
				case 38u:
				case 39u:
				case 40u:
				case 41u:
				case 42u:
				case 43u:
				case 44u:
				case 45u:
				case 46u:
				case 47u:
				case 48u:
				case 49u:
				case 50u:
				case 51u:
				case 52u:
				case 53u:
				case 54u:
				case 55u:
				case 56u:
				case 57u:
				case 58u:
				case 59u:
				case 60u:
				case 61u:
				case 62u:
				case 63u:
				case 64u:
				case 65u:
				case 66u:
				case 67u:
				case 68u:
				case 69u:
				case 70u:
				case 71u:
				case 72u:
				case 73u:
				case 74u:
				case 75u:
				case 76u:
				case 77u:
				case 78u:
				case 79u:
				case 80u:
				case 81u:
				case 82u:
				case 83u:
				case 84u:
				case 85u:
				case 86u:
				case 87u:
				case 88u:
				case 89u:
				case 90u:
				case 91u:
				case 92u:
				case 93u:
				case 94u:
				case 95u:
				case 96u:
				case 97u:
				case 98u:
				case 99u:
					bufferEnd -= 2;
					WriteTwoDigits(value, bufferEnd);
					return bufferEnd;
				case 0u:
				case 1u:
				case 2u:
				case 3u:
				case 4u:
				case 5u:
				case 6u:
				case 7u:
				case 8u:
				case 9u:
					break;
				}
				break;
				IL_0007:
				bufferEnd -= 2;
				uint value2;
				(value, value2) = Math.DivRem(value, 100u);
				WriteTwoDigits(value2, bufferEnd);
			}
		}
		*(--bufferEnd) = TChar.CastFrom(value + 48);
		return bufferEnd;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal unsafe static TChar* UInt32ToDecChars<TChar>(TChar* bufferEnd, uint value, int digits) where TChar : unmanaged, IUtfChar<TChar>
	{
		while (value >= 100)
		{
			bufferEnd -= 2;
			digits -= 2;
			uint value2;
			(value, value2) = Math.DivRem(value, 100u);
			WriteTwoDigits(value2, bufferEnd);
		}
		while (value != 0 || digits > 0)
		{
			digits--;
			uint value2;
			(value, value2) = Math.DivRem(value, 10u);
			*(--bufferEnd) = TChar.CastFrom(value2 + 48);
		}
		return bufferEnd;
	}

	internal static string UInt32ToDecStr(uint value)
	{
		if (value < 300)
		{
			return UInt32ToDecStrForKnownSmallNumber(value);
		}
		return UInt32ToDecStr_NoSmallNumberCheck(value);
	}

	internal static string UInt32ToDecStrForKnownSmallNumber(uint value)
	{
		return s_smallNumberCache[value] ?? CreateAndCacheString(value);
		[MethodImpl(MethodImplOptions.NoInlining)]
		static string CreateAndCacheString(uint value)
		{
			return s_smallNumberCache[value] = UInt32ToDecStr_NoSmallNumberCheck(value);
		}
	}

	private unsafe static string UInt32ToDecStr_NoSmallNumberCheck(uint value)
	{
		int num = FormattingHelpers.CountDigits(value);
		string text = string.FastAllocateString(num);
		fixed (char* ptr = text)
		{
			char* bufferEnd = ptr + num;
			bufferEnd = UInt32ToDecChars(bufferEnd, value);
		}
		return text;
	}

	private unsafe static string UInt32ToDecStr(uint value, int digits)
	{
		if (digits <= 1)
		{
			return UInt32ToDecStr(value);
		}
		int num = Math.Max(digits, FormattingHelpers.CountDigits(value));
		string text = string.FastAllocateString(num);
		fixed (char* ptr = text)
		{
			char* bufferEnd = ptr + num;
			bufferEnd = UInt32ToDecChars(bufferEnd, value, digits);
		}
		return text;
	}

	internal unsafe static bool TryUInt32ToDecStr<TChar>(uint value, Span<TChar> destination, out int charsWritten) where TChar : unmanaged, IUtfChar<TChar>
	{
		int num = FormattingHelpers.CountDigits(value);
		if (num <= destination.Length)
		{
			charsWritten = num;
			fixed (TChar* ptr = &MemoryMarshal.GetReference(destination))
			{
				TChar* ptr2 = UInt32ToDecChars(ptr + num, value);
			}
			return true;
		}
		charsWritten = 0;
		return false;
	}

	internal unsafe static bool TryUInt32ToDecStr<TChar>(uint value, int digits, Span<TChar> destination, out int charsWritten) where TChar : unmanaged, IUtfChar<TChar>
	{
		int num = FormattingHelpers.CountDigits(value);
		int num2 = Math.Max(digits, num);
		if (num2 <= destination.Length)
		{
			charsWritten = num2;
			fixed (TChar* ptr = &MemoryMarshal.GetReference(destination))
			{
				TChar* bufferEnd = ptr + num2;
				bufferEnd = ((digits > num) ? UInt32ToDecChars(bufferEnd, value, digits) : UInt32ToDecChars(bufferEnd, value));
			}
			return true;
		}
		charsWritten = 0;
		return false;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private unsafe static void Int64ToNumber(long value, ref NumberBuffer number)
	{
		number.DigitsCount = 19;
		if (value >= 0)
		{
			number.IsNegative = false;
		}
		else
		{
			number.IsNegative = true;
			value = -value;
		}
		byte* digitsPointer = number.GetDigitsPointer();
		byte* ptr = UInt64ToDecChars(digitsPointer + 19, (ulong)value, 0);
		int num = (number.Scale = (number.DigitsCount = (int)(digitsPointer + 19 - ptr)));
		byte* digitsPointer2 = number.GetDigitsPointer();
		while (--num >= 0)
		{
			*(digitsPointer2++) = *(ptr++);
		}
		*digitsPointer2 = 0;
	}

	public static string Int64ToDecStr(long value)
	{
		if (value < 0)
		{
			return NegativeInt64ToDecStr(value, -1, NumberFormatInfo.CurrentInfo.NegativeSign);
		}
		return UInt64ToDecStr((ulong)value);
	}

	private unsafe static string NegativeInt64ToDecStr(long value, int digits, string sNegative)
	{
		if (digits < 1)
		{
			digits = 1;
		}
		int num = Math.Max(digits, FormattingHelpers.CountDigits((ulong)(-value))) + sNegative.Length;
		string text = string.FastAllocateString(num);
		fixed (char* ptr = text)
		{
			char* ptr2 = UInt64ToDecChars(ptr + num, (ulong)(-value), digits);
			for (int num2 = sNegative.Length - 1; num2 >= 0; num2--)
			{
				*(--ptr2) = sNegative[num2];
			}
		}
		return text;
	}

	internal unsafe static bool TryNegativeInt64ToDecStr<TChar>(long value, int digits, ReadOnlySpan<TChar> sNegative, Span<TChar> destination, out int charsWritten) where TChar : unmanaged, IUtfChar<TChar>
	{
		if (digits < 1)
		{
			digits = 1;
		}
		int num = Math.Max(digits, FormattingHelpers.CountDigits((ulong)(-value))) + sNegative.Length;
		if (num > destination.Length)
		{
			charsWritten = 0;
			return false;
		}
		charsWritten = num;
		fixed (TChar* ptr = &MemoryMarshal.GetReference(destination))
		{
			TChar* ptr2 = UInt64ToDecChars(ptr + num, (ulong)(-value), digits);
			for (int num2 = sNegative.Length - 1; num2 >= 0; num2--)
			{
				*(--ptr2) = sNegative[num2];
			}
		}
		return true;
	}

	private unsafe static string Int64ToHexStr(long value, char hexBase, int digits)
	{
		if (digits < 1)
		{
			digits = 1;
		}
		int num = Math.Max(digits, FormattingHelpers.CountHexDigits((ulong)value));
		string text = string.FastAllocateString(num);
		fixed (char* ptr = text)
		{
			char* ptr2 = Int64ToHexChars(ptr + num, (ulong)value, hexBase, digits);
		}
		return text;
	}

	internal unsafe static bool TryInt64ToHexStr<TChar>(long value, char hexBase, int digits, Span<TChar> destination, out int charsWritten) where TChar : unmanaged, IUtfChar<TChar>
	{
		if (digits < 1)
		{
			digits = 1;
		}
		int num = Math.Max(digits, FormattingHelpers.CountHexDigits((ulong)value));
		if (num > destination.Length)
		{
			charsWritten = 0;
			return false;
		}
		charsWritten = num;
		fixed (TChar* ptr = &MemoryMarshal.GetReference(destination))
		{
			TChar* ptr2 = Int64ToHexChars(ptr + num, (ulong)value, hexBase, digits);
		}
		return true;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private unsafe static TChar* Int64ToHexChars<TChar>(TChar* buffer, ulong value, int hexBase, int digits) where TChar : unmanaged, IUtfChar<TChar>
	{
		while (--digits >= 0 || value != 0L)
		{
			byte b = (byte)(value & 0xF);
			*(--buffer) = TChar.CastFrom(b + ((b < 10) ? 48 : hexBase));
			value >>= 4;
		}
		return buffer;
	}

	private unsafe static string UInt64ToBinaryStr(ulong value, int digits)
	{
		if (digits < 1)
		{
			digits = 1;
		}
		int num = Math.Max(digits, 64 - (int)ulong.LeadingZeroCount(value));
		string text = string.FastAllocateString(num);
		fixed (char* ptr = text)
		{
			char* ptr2 = UInt64ToBinaryChars(ptr + num, value, digits);
		}
		return text;
	}

	private unsafe static bool TryUInt64ToBinaryStr<TChar>(ulong value, int digits, Span<TChar> destination, out int charsWritten) where TChar : unmanaged, IUtfChar<TChar>
	{
		if (digits < 1)
		{
			digits = 1;
		}
		int num = Math.Max(digits, 64 - (int)ulong.LeadingZeroCount(value));
		if (num > destination.Length)
		{
			charsWritten = 0;
			return false;
		}
		charsWritten = num;
		fixed (TChar* ptr = &MemoryMarshal.GetReference(destination))
		{
			TChar* ptr2 = UInt64ToBinaryChars(ptr + num, value, digits);
		}
		return true;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private unsafe static TChar* UInt64ToBinaryChars<TChar>(TChar* buffer, ulong value, int digits) where TChar : unmanaged, IUtfChar<TChar>
	{
		while (--digits >= 0 || value != 0L)
		{
			*(--buffer) = TChar.CastFrom(48 + (byte)(value & 1));
			value >>= 1;
		}
		return buffer;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private unsafe static void UInt64ToNumber(ulong value, ref NumberBuffer number)
	{
		number.DigitsCount = 20;
		number.IsNegative = false;
		byte* digitsPointer = number.GetDigitsPointer();
		byte* ptr = UInt64ToDecChars(digitsPointer + 20, value, 0);
		int num = (number.Scale = (number.DigitsCount = (int)(digitsPointer + 20 - ptr)));
		byte* digitsPointer2 = number.GetDigitsPointer();
		while (--num >= 0)
		{
			*(digitsPointer2++) = *(ptr++);
		}
		*digitsPointer2 = 0;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal unsafe static TChar* UInt64ToDecChars<TChar>(TChar* bufferEnd, ulong value) where TChar : unmanaged, IUtfChar<TChar>
	{
		if (value >= 10)
		{
			while (true)
			{
				switch (value)
				{
				default:
					goto IL_0008;
				case 10uL:
				case 11uL:
				case 12uL:
				case 13uL:
				case 14uL:
				case 15uL:
				case 16uL:
				case 17uL:
				case 18uL:
				case 19uL:
				case 20uL:
				case 21uL:
				case 22uL:
				case 23uL:
				case 24uL:
				case 25uL:
				case 26uL:
				case 27uL:
				case 28uL:
				case 29uL:
				case 30uL:
				case 31uL:
				case 32uL:
				case 33uL:
				case 34uL:
				case 35uL:
				case 36uL:
				case 37uL:
				case 38uL:
				case 39uL:
				case 40uL:
				case 41uL:
				case 42uL:
				case 43uL:
				case 44uL:
				case 45uL:
				case 46uL:
				case 47uL:
				case 48uL:
				case 49uL:
				case 50uL:
				case 51uL:
				case 52uL:
				case 53uL:
				case 54uL:
				case 55uL:
				case 56uL:
				case 57uL:
				case 58uL:
				case 59uL:
				case 60uL:
				case 61uL:
				case 62uL:
				case 63uL:
				case 64uL:
				case 65uL:
				case 66uL:
				case 67uL:
				case 68uL:
				case 69uL:
				case 70uL:
				case 71uL:
				case 72uL:
				case 73uL:
				case 74uL:
				case 75uL:
				case 76uL:
				case 77uL:
				case 78uL:
				case 79uL:
				case 80uL:
				case 81uL:
				case 82uL:
				case 83uL:
				case 84uL:
				case 85uL:
				case 86uL:
				case 87uL:
				case 88uL:
				case 89uL:
				case 90uL:
				case 91uL:
				case 92uL:
				case 93uL:
				case 94uL:
				case 95uL:
				case 96uL:
				case 97uL:
				case 98uL:
				case 99uL:
					bufferEnd -= 2;
					WriteTwoDigits((uint)value, bufferEnd);
					return bufferEnd;
				case 0uL:
				case 1uL:
				case 2uL:
				case 3uL:
				case 4uL:
				case 5uL:
				case 6uL:
				case 7uL:
				case 8uL:
				case 9uL:
					break;
				}
				break;
				IL_0008:
				bufferEnd -= 2;
				ulong num;
				(value, num) = Math.DivRem(value, 100uL);
				WriteTwoDigits((uint)num, bufferEnd);
			}
		}
		*(--bufferEnd) = TChar.CastFrom(value + 48);
		return bufferEnd;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal unsafe static TChar* UInt64ToDecChars<TChar>(TChar* bufferEnd, ulong value, int digits) where TChar : unmanaged, IUtfChar<TChar>
	{
		while (value >= 100)
		{
			bufferEnd -= 2;
			digits -= 2;
			ulong num;
			(value, num) = Math.DivRem(value, 100uL);
			WriteTwoDigits((uint)num, bufferEnd);
		}
		while (value != 0L || digits > 0)
		{
			digits--;
			ulong num;
			(value, num) = Math.DivRem(value, 10uL);
			*(--bufferEnd) = TChar.CastFrom(num + 48);
		}
		return bufferEnd;
	}

	internal unsafe static string UInt64ToDecStr(ulong value)
	{
		if (value < 300)
		{
			return UInt32ToDecStrForKnownSmallNumber((uint)value);
		}
		int num = FormattingHelpers.CountDigits(value);
		string text = string.FastAllocateString(num);
		fixed (char* ptr = text)
		{
			char* bufferEnd = ptr + num;
			bufferEnd = UInt64ToDecChars(bufferEnd, value);
		}
		return text;
	}

	internal unsafe static string UInt64ToDecStr(ulong value, int digits)
	{
		if (digits <= 1)
		{
			return UInt64ToDecStr(value);
		}
		int num = Math.Max(digits, FormattingHelpers.CountDigits(value));
		string text = string.FastAllocateString(num);
		fixed (char* ptr = text)
		{
			char* bufferEnd = ptr + num;
			bufferEnd = UInt64ToDecChars(bufferEnd, value, digits);
		}
		return text;
	}

	internal unsafe static bool TryUInt64ToDecStr<TChar>(ulong value, Span<TChar> destination, out int charsWritten) where TChar : unmanaged, IUtfChar<TChar>
	{
		int num = FormattingHelpers.CountDigits(value);
		if (num <= destination.Length)
		{
			charsWritten = num;
			fixed (TChar* ptr = &MemoryMarshal.GetReference(destination))
			{
				TChar* bufferEnd = ptr + num;
				bufferEnd = UInt64ToDecChars(bufferEnd, value);
			}
			return true;
		}
		charsWritten = 0;
		return false;
	}

	internal unsafe static bool TryUInt64ToDecStr<TChar>(ulong value, int digits, Span<TChar> destination, out int charsWritten) where TChar : unmanaged, IUtfChar<TChar>
	{
		int num = FormattingHelpers.CountDigits(value);
		int num2 = Math.Max(digits, num);
		if (num2 <= destination.Length)
		{
			charsWritten = num2;
			fixed (TChar* ptr = &MemoryMarshal.GetReference(destination))
			{
				TChar* bufferEnd = ptr + num2;
				bufferEnd = ((digits > num) ? UInt64ToDecChars(bufferEnd, value, digits) : UInt64ToDecChars(bufferEnd, value));
			}
			return true;
		}
		charsWritten = 0;
		return false;
	}

	private unsafe static void Int128ToNumber(Int128 value, ref NumberBuffer number)
	{
		number.DigitsCount = 39;
		if (Int128.IsPositive(value))
		{
			number.IsNegative = false;
		}
		else
		{
			number.IsNegative = true;
			value = -value;
		}
		byte* digitsPointer = number.GetDigitsPointer();
		byte* ptr = UInt128ToDecChars(digitsPointer + 39, (UInt128)value, 0);
		int num = (number.Scale = (number.DigitsCount = (int)(digitsPointer + 39 - ptr)));
		byte* digitsPointer2 = number.GetDigitsPointer();
		while (--num >= 0)
		{
			*(digitsPointer2++) = *(ptr++);
		}
		*digitsPointer2 = 0;
	}

	public static string Int128ToDecStr(Int128 value)
	{
		if (!Int128.IsPositive(value))
		{
			return NegativeInt128ToDecStr(value, -1, NumberFormatInfo.CurrentInfo.NegativeSign);
		}
		return UInt128ToDecStr((UInt128)value, -1);
	}

	private unsafe static string NegativeInt128ToDecStr(Int128 value, int digits, string sNegative)
	{
		if (digits < 1)
		{
			digits = 1;
		}
		UInt128 value2 = (UInt128)(-value);
		int num = Math.Max(digits, FormattingHelpers.CountDigits(value2)) + sNegative.Length;
		string text = string.FastAllocateString(num);
		fixed (char* ptr = text)
		{
			char* ptr2 = UInt128ToDecChars(ptr + num, value2, digits);
			for (int num2 = sNegative.Length - 1; num2 >= 0; num2--)
			{
				*(--ptr2) = sNegative[num2];
			}
		}
		return text;
	}

	private unsafe static bool TryNegativeInt128ToDecStr<TChar>(Int128 value, int digits, ReadOnlySpan<TChar> sNegative, Span<TChar> destination, out int charsWritten) where TChar : unmanaged, IUtfChar<TChar>
	{
		if (digits < 1)
		{
			digits = 1;
		}
		UInt128 value2 = (UInt128)(-value);
		int num = Math.Max(digits, FormattingHelpers.CountDigits(value2)) + sNegative.Length;
		if (num > destination.Length)
		{
			charsWritten = 0;
			return false;
		}
		charsWritten = num;
		fixed (TChar* ptr = &MemoryMarshal.GetReference(destination))
		{
			TChar* ptr2 = UInt128ToDecChars(ptr + num, value2, digits);
			for (int num2 = sNegative.Length - 1; num2 >= 0; num2--)
			{
				*(--ptr2) = sNegative[num2];
			}
		}
		return true;
	}

	private unsafe static string Int128ToHexStr(Int128 value, char hexBase, int digits)
	{
		if (digits < 1)
		{
			digits = 1;
		}
		UInt128 value2 = (UInt128)value;
		int num = Math.Max(digits, FormattingHelpers.CountHexDigits(value2));
		string text = string.FastAllocateString(num);
		fixed (char* ptr = text)
		{
			char* ptr2 = Int128ToHexChars(ptr + num, value2, hexBase, digits);
		}
		return text;
	}

	private unsafe static bool TryInt128ToHexStr<TChar>(Int128 value, char hexBase, int digits, Span<TChar> destination, out int charsWritten) where TChar : unmanaged, IUtfChar<TChar>
	{
		if (digits < 1)
		{
			digits = 1;
		}
		UInt128 value2 = (UInt128)value;
		int num = Math.Max(digits, FormattingHelpers.CountHexDigits(value2));
		if (num > destination.Length)
		{
			charsWritten = 0;
			return false;
		}
		charsWritten = num;
		fixed (TChar* ptr = &MemoryMarshal.GetReference(destination))
		{
			TChar* ptr2 = Int128ToHexChars(ptr + num, value2, hexBase, digits);
		}
		return true;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private unsafe static TChar* Int128ToHexChars<TChar>(TChar* buffer, UInt128 value, int hexBase, int digits) where TChar : unmanaged, IUtfChar<TChar>
	{
		ulong lower = value.Lower;
		ulong upper = value.Upper;
		if (upper != 0L)
		{
			buffer = Int64ToHexChars(buffer, lower, hexBase, 16);
			return Int64ToHexChars(buffer, upper, hexBase, digits - 16);
		}
		return Int64ToHexChars(buffer, lower, hexBase, Math.Max(digits, 1));
	}

	private unsafe static string UInt128ToBinaryStr(Int128 value, int digits)
	{
		if (digits < 1)
		{
			digits = 1;
		}
		UInt128 value2 = (UInt128)value;
		int num = Math.Max(digits, 128 - (int)UInt128.LeadingZeroCount((UInt128)value));
		string text = string.FastAllocateString(num);
		fixed (char* ptr = text)
		{
			char* ptr2 = UInt128ToBinaryChars(ptr + num, value2, digits);
		}
		return text;
	}

	private unsafe static bool TryUInt128ToBinaryStr<TChar>(Int128 value, int digits, Span<TChar> destination, out int charsWritten) where TChar : unmanaged, IUtfChar<TChar>
	{
		if (digits < 1)
		{
			digits = 1;
		}
		UInt128 value2 = (UInt128)value;
		int num = Math.Max(digits, 128 - (int)UInt128.LeadingZeroCount((UInt128)value));
		if (num > destination.Length)
		{
			charsWritten = 0;
			return false;
		}
		charsWritten = num;
		fixed (TChar* ptr = &MemoryMarshal.GetReference(destination))
		{
			TChar* ptr2 = UInt128ToBinaryChars(ptr + num, value2, digits);
		}
		return true;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private unsafe static TChar* UInt128ToBinaryChars<TChar>(TChar* buffer, UInt128 value, int digits) where TChar : unmanaged, IUtfChar<TChar>
	{
		ulong lower = value.Lower;
		ulong upper = value.Upper;
		if (upper != 0L)
		{
			buffer = UInt64ToBinaryChars(buffer, lower, 64);
			return UInt64ToBinaryChars(buffer, upper, digits - 64);
		}
		return UInt64ToBinaryChars(buffer, lower, Math.Max(digits, 1));
	}

	private unsafe static void UInt128ToNumber(UInt128 value, ref NumberBuffer number)
	{
		number.DigitsCount = 39;
		number.IsNegative = false;
		byte* digitsPointer = number.GetDigitsPointer();
		byte* ptr = UInt128ToDecChars(digitsPointer + 39, value, 0);
		int num = (number.Scale = (number.DigitsCount = (int)(digitsPointer + 39 - ptr)));
		byte* digitsPointer2 = number.GetDigitsPointer();
		while (--num >= 0)
		{
			*(digitsPointer2++) = *(ptr++);
		}
		*digitsPointer2 = 0;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static ulong Int128DivMod1E19(ref UInt128 value)
	{
		UInt128 uInt;
		(value, uInt) = UInt128.DivRem(right: new UInt128(0uL, 10000000000000000000uL), left: value);
		return uInt.Lower;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal unsafe static TChar* UInt128ToDecChars<TChar>(TChar* bufferEnd, UInt128 value) where TChar : unmanaged, IUtfChar<TChar>
	{
		while (value.Upper != 0L)
		{
			bufferEnd = UInt64ToDecChars(bufferEnd, Int128DivMod1E19(ref value), 19);
		}
		return UInt64ToDecChars(bufferEnd, value.Lower);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal unsafe static TChar* UInt128ToDecChars<TChar>(TChar* bufferEnd, UInt128 value, int digits) where TChar : unmanaged, IUtfChar<TChar>
	{
		while (value.Upper != 0L)
		{
			bufferEnd = UInt64ToDecChars(bufferEnd, Int128DivMod1E19(ref value), 19);
			digits -= 19;
		}
		return UInt64ToDecChars(bufferEnd, value.Lower, digits);
	}

	internal unsafe static string UInt128ToDecStr(UInt128 value)
	{
		if (value.Upper == 0L)
		{
			return UInt64ToDecStr(value.Lower);
		}
		int num = FormattingHelpers.CountDigits(value);
		string text = string.FastAllocateString(num);
		fixed (char* ptr = text)
		{
			char* bufferEnd = ptr + num;
			bufferEnd = UInt128ToDecChars(bufferEnd, value);
		}
		return text;
	}

	internal unsafe static string UInt128ToDecStr(UInt128 value, int digits)
	{
		if (digits <= 1)
		{
			return UInt128ToDecStr(value);
		}
		int num = Math.Max(digits, FormattingHelpers.CountDigits(value));
		string text = string.FastAllocateString(num);
		fixed (char* ptr = text)
		{
			char* bufferEnd = ptr + num;
			bufferEnd = UInt128ToDecChars(bufferEnd, value, digits);
		}
		return text;
	}

	private unsafe static bool TryUInt128ToDecStr<TChar>(UInt128 value, int digits, Span<TChar> destination, out int charsWritten) where TChar : unmanaged, IUtfChar<TChar>
	{
		int num = FormattingHelpers.CountDigits(value);
		int num2 = Math.Max(digits, num);
		if (num2 <= destination.Length)
		{
			charsWritten = num2;
			fixed (TChar* ptr = &MemoryMarshal.GetReference(destination))
			{
				TChar* bufferEnd = ptr + num2;
				bufferEnd = ((digits > num) ? UInt128ToDecChars(bufferEnd, value, digits) : UInt128ToDecChars(bufferEnd, value));
			}
			return true;
		}
		charsWritten = 0;
		return false;
	}

	internal static char ParseFormatSpecifier(ReadOnlySpan<char> format, out int digits)
	{
		char c = '\0';
		if (format.Length > 0)
		{
			c = format[0];
			if (char.IsAsciiLetter(c))
			{
				if (format.Length == 1)
				{
					digits = -1;
					return c;
				}
				if (format.Length == 2)
				{
					int num = format[1] - 48;
					if ((uint)num < 10u)
					{
						digits = num;
						return c;
					}
				}
				else if (format.Length == 3)
				{
					int num2 = format[1] - 48;
					int num3 = format[2] - 48;
					if ((uint)num2 < 10u && (uint)num3 < 10u)
					{
						digits = num2 * 10 + num3;
						return c;
					}
				}
				int num4 = 0;
				int num5 = 1;
				while ((uint)num5 < (uint)format.Length && char.IsAsciiDigit(format[num5]))
				{
					if (num4 >= 100000000)
					{
						ThrowHelper.ThrowFormatException_BadFormatSpecifier();
					}
					num4 = num4 * 10 + format[num5++] - 48;
				}
				if ((uint)num5 >= (uint)format.Length || format[num5] == '\0')
				{
					digits = num4;
					return c;
				}
			}
		}
		digits = -1;
		if (format.Length != 0 && c != 0)
		{
			return '\0';
		}
		return 'G';
	}

	internal static void NumberToString<TChar>(ref ValueListBuilder<TChar> vlb, ref NumberBuffer number, char format, int nMaxDigits, NumberFormatInfo info) where TChar : unmanaged, IUtfChar<TChar>
	{
		bool isCorrectlyRounded = number.Kind == NumberBufferKind.FloatingPoint;
		bool suppressScientific;
		switch (format)
		{
		case 'C':
		case 'c':
			if (nMaxDigits < 0)
			{
				nMaxDigits = info.CurrencyDecimalDigits;
			}
			RoundNumber(ref number, number.Scale + nMaxDigits, isCorrectlyRounded);
			FormatCurrency(ref vlb, ref number, nMaxDigits, info);
			break;
		case 'F':
		case 'f':
			if (nMaxDigits < 0)
			{
				nMaxDigits = info.NumberDecimalDigits;
			}
			RoundNumber(ref number, number.Scale + nMaxDigits, isCorrectlyRounded);
			if (number.IsNegative)
			{
				vlb.Append(info.NegativeSignTChar<TChar>());
			}
			FormatFixed(ref vlb, ref number, nMaxDigits, null, info.NumberDecimalSeparatorTChar<TChar>(), null);
			break;
		case 'N':
		case 'n':
			if (nMaxDigits < 0)
			{
				nMaxDigits = info.NumberDecimalDigits;
			}
			RoundNumber(ref number, number.Scale + nMaxDigits, isCorrectlyRounded);
			FormatNumber(ref vlb, ref number, nMaxDigits, info);
			break;
		case 'E':
		case 'e':
			if (nMaxDigits < 0)
			{
				nMaxDigits = 6;
			}
			nMaxDigits++;
			RoundNumber(ref number, nMaxDigits, isCorrectlyRounded);
			if (number.IsNegative)
			{
				vlb.Append(info.NegativeSignTChar<TChar>());
			}
			FormatScientific(ref vlb, ref number, nMaxDigits, info, format);
			break;
		case 'G':
		case 'g':
			suppressScientific = false;
			if (nMaxDigits < 1)
			{
				if (number.Kind == NumberBufferKind.Decimal && nMaxDigits == -1)
				{
					suppressScientific = true;
					if (number.Digits[0] != 0)
					{
						goto IL_018e;
					}
					goto IL_01a3;
				}
				nMaxDigits = number.DigitsCount;
			}
			RoundNumber(ref number, nMaxDigits, isCorrectlyRounded);
			goto IL_018e;
		case 'P':
		case 'p':
			if (nMaxDigits < 0)
			{
				nMaxDigits = info.PercentDecimalDigits;
			}
			number.Scale += 2;
			RoundNumber(ref number, number.Scale + nMaxDigits, isCorrectlyRounded);
			FormatPercent(ref vlb, ref number, nMaxDigits, info);
			break;
		case 'R':
		case 'r':
			format = (char)(format - 11);
			goto case 'G';
		default:
			{
				ThrowHelper.ThrowFormatException_BadFormatSpecifier();
				break;
			}
			IL_018e:
			if (number.IsNegative)
			{
				vlb.Append(info.NegativeSignTChar<TChar>());
			}
			goto IL_01a3;
			IL_01a3:
			FormatGeneral(ref vlb, ref number, nMaxDigits, info, (char)(format - 2), suppressScientific);
			break;
		}
	}

	internal unsafe static void NumberToStringFormat<TChar>(ref ValueListBuilder<TChar> vlb, ref NumberBuffer number, ReadOnlySpan<char> format, NumberFormatInfo info) where TChar : unmanaged, IUtfChar<TChar>
	{
		int num = 0;
		byte* digitsPointer = number.GetDigitsPointer();
		int num2 = FindSection(format, (*digitsPointer == 0) ? 2 : (number.IsNegative ? 1 : 0));
		int num3;
		int num4;
		bool flag;
		bool flag2;
		int num5;
		int num6;
		int num9;
		while (true)
		{
			num3 = 0;
			num4 = -1;
			num5 = int.MaxValue;
			num6 = 0;
			flag = false;
			int num7 = -1;
			flag2 = false;
			int num8 = 0;
			num9 = num2;
			fixed (char* ptr = &MemoryMarshal.GetReference(format))
			{
				char c;
				while (num9 < format.Length && (c = ptr[num9++]) != 0)
				{
					switch (c)
					{
					case ';':
						break;
					case '#':
						num3++;
						continue;
					case '0':
						if (num5 == int.MaxValue)
						{
							num5 = num3;
						}
						num3++;
						num6 = num3;
						continue;
					case '.':
						if (num4 < 0)
						{
							num4 = num3;
						}
						continue;
					case ',':
						if (num3 <= 0 || num4 >= 0)
						{
							continue;
						}
						if (num7 >= 0)
						{
							if (num7 == num3)
							{
								num++;
								continue;
							}
							flag2 = true;
						}
						num7 = num3;
						num = 1;
						continue;
					case '%':
						num8 += 2;
						continue;
					case '‰':
						num8 += 3;
						continue;
					case '"':
					case '\'':
						while (num9 < format.Length && ptr[num9] != 0 && ptr[num9++] != c)
						{
						}
						continue;
					case '\\':
						if (num9 < format.Length && ptr[num9] != 0)
						{
							num9++;
						}
						continue;
					case 'E':
					case 'e':
						if ((num9 < format.Length && ptr[num9] == '0') || (num9 + 1 < format.Length && (ptr[num9] == '+' || ptr[num9] == '-') && ptr[num9 + 1] == '0'))
						{
							while (++num9 < format.Length && ptr[num9] == '0')
							{
							}
							flag = true;
						}
						continue;
					default:
						continue;
					}
					break;
				}
			}
			if (num4 < 0)
			{
				num4 = num3;
			}
			if (num7 >= 0)
			{
				if (num7 == num4)
				{
					num8 -= num * 3;
				}
				else
				{
					flag2 = true;
				}
			}
			if (*digitsPointer != 0)
			{
				number.Scale += num8;
				int pos = (flag ? num3 : (number.Scale + num3 - num4));
				RoundNumber(ref number, pos, isCorrectlyRounded: false);
				if (*digitsPointer != 0)
				{
					break;
				}
				num9 = FindSection(format, 2);
				if (num9 == num2)
				{
					break;
				}
				num2 = num9;
				continue;
			}
			if (number.Kind != NumberBufferKind.FloatingPoint)
			{
				number.IsNegative = false;
			}
			number.Scale = 0;
			break;
		}
		num5 = ((num5 < num4) ? (num4 - num5) : 0);
		num6 = ((num6 > num4) ? (num4 - num6) : 0);
		int num10;
		int num11;
		if (flag)
		{
			num10 = num4;
			num11 = 0;
		}
		else
		{
			num10 = ((number.Scale > num4) ? number.Scale : num4);
			num11 = number.Scale - num4;
		}
		num9 = num2;
		Span<int> span = stackalloc int[4];
		int num12 = -1;
		if (flag2 && info.NumberGroupSeparator.Length > 0)
		{
			int[] numberGroupSizes = info._numberGroupSizes;
			int num13 = 0;
			int i = 0;
			int num14 = numberGroupSizes.Length;
			if (num14 != 0)
			{
				i = numberGroupSizes[num13];
			}
			int num15 = i;
			int num16 = num10 + ((num11 < 0) ? num11 : 0);
			for (int num17 = ((num5 > num16) ? num5 : num16); num17 > i; i += num15)
			{
				if (num15 == 0)
				{
					break;
				}
				num12++;
				if (num12 >= span.Length)
				{
					int[] array = new int[span.Length * 2];
					span.CopyTo(array);
					span = array;
				}
				span[num12] = i;
				if (num13 < num14 - 1)
				{
					num13++;
					num15 = numberGroupSizes[num13];
				}
			}
		}
		if (number.IsNegative && num2 == 0 && number.Scale != 0)
		{
			vlb.Append(info.NegativeSignTChar<TChar>());
		}
		bool flag3 = false;
		fixed (char* ptr3 = &MemoryMarshal.GetReference(format))
		{
			byte* ptr2 = digitsPointer;
			char c;
			while (num9 < format.Length && (c = ptr3[num9++]) != 0 && c != ';')
			{
				if (num11 > 0 && (c == '#' || c == '.' || c == '0'))
				{
					while (num11 > 0)
					{
						vlb.Append(TChar.CastFrom((char)((*ptr2 != 0) ? (*(ptr2++)) : 48)));
						if (flag2 && num10 > 1 && num12 >= 0 && num10 == span[num12] + 1)
						{
							vlb.Append(info.NumberGroupSeparatorTChar<TChar>());
							num12--;
						}
						num10--;
						num11--;
					}
				}
				switch (c)
				{
				case '#':
				case '0':
					if (num11 < 0)
					{
						num11++;
						c = ((num10 <= num5) ? '0' : '\0');
					}
					else
					{
						c = ((*ptr2 != 0) ? ((char)(*(ptr2++))) : ((num10 > num6) ? '0' : '\0'));
					}
					if (c != 0)
					{
						vlb.Append(TChar.CastFrom(c));
						if (flag2 && num10 > 1 && num12 >= 0 && num10 == span[num12] + 1)
						{
							vlb.Append(info.NumberGroupSeparatorTChar<TChar>());
							num12--;
						}
					}
					num10--;
					break;
				case '.':
					if (!(num10 != 0 || flag3) && (num6 < 0 || (num4 < num3 && *ptr2 != 0)))
					{
						vlb.Append(info.NumberDecimalSeparatorTChar<TChar>());
						flag3 = true;
					}
					break;
				case '‰':
					vlb.Append(info.PerMilleSymbolTChar<TChar>());
					break;
				case '%':
					vlb.Append(info.PercentSymbolTChar<TChar>());
					break;
				case '"':
				case '\'':
					while (num9 < format.Length && ptr3[num9] != 0 && ptr3[num9] != c)
					{
						AppendUnknownChar(ref vlb, ptr3[num9++]);
					}
					if (num9 < format.Length && ptr3[num9] != 0)
					{
						num9++;
					}
					break;
				case '\\':
					if (num9 < format.Length && ptr3[num9] != 0)
					{
						AppendUnknownChar(ref vlb, ptr3[num9++]);
					}
					break;
				case 'E':
				case 'e':
				{
					bool positiveSign = false;
					int num18 = 0;
					if (flag)
					{
						if (num9 < format.Length && ptr3[num9] == '0')
						{
							num18++;
						}
						else if (num9 + 1 < format.Length && ptr3[num9] == '+' && ptr3[num9 + 1] == '0')
						{
							positiveSign = true;
						}
						else if (num9 + 1 >= format.Length || ptr3[num9] != '-' || ptr3[num9 + 1] != '0')
						{
							vlb.Append(TChar.CastFrom(c));
							break;
						}
						while (++num9 < format.Length && ptr3[num9] == '0')
						{
							num18++;
						}
						if (num18 > 10)
						{
							num18 = 10;
						}
						int value = ((*digitsPointer != 0) ? (number.Scale - num4) : 0);
						FormatExponent(ref vlb, info, value, c, num18, positiveSign);
						flag = false;
						break;
					}
					vlb.Append(TChar.CastFrom(c));
					if (num9 < format.Length)
					{
						if (ptr3[num9] == '+' || ptr3[num9] == '-')
						{
							AppendUnknownChar(ref vlb, ptr3[num9++]);
						}
						while (num9 < format.Length && ptr3[num9] == '0')
						{
							AppendUnknownChar(ref vlb, ptr3[num9++]);
						}
					}
					break;
				}
				default:
					AppendUnknownChar(ref vlb, c);
					break;
				case ',':
					break;
				}
			}
		}
		if (number.IsNegative && num2 == 0 && number.Scale == 0 && vlb.Length > 0)
		{
			vlb.Insert(0, info.NegativeSignTChar<TChar>());
		}
	}

	private static void FormatCurrency<TChar>(ref ValueListBuilder<TChar> vlb, ref NumberBuffer number, int nMaxDigits, NumberFormatInfo info) where TChar : unmanaged, IUtfChar<TChar>
	{
		string text = (number.IsNegative ? s_negCurrencyFormats[info.CurrencyNegativePattern] : s_posCurrencyFormats[info.CurrencyPositivePattern]);
		string text2 = text;
		foreach (char c in text2)
		{
			switch (c)
			{
			case '#':
				FormatFixed(ref vlb, ref number, nMaxDigits, info._currencyGroupSizes, info.CurrencyDecimalSeparatorTChar<TChar>(), info.CurrencyGroupSeparatorTChar<TChar>());
				break;
			case '-':
				vlb.Append(info.NegativeSignTChar<TChar>());
				break;
			case '$':
				vlb.Append(info.CurrencySymbolTChar<TChar>());
				break;
			default:
				vlb.Append(TChar.CastFrom(c));
				break;
			}
		}
	}

	private unsafe static void FormatFixed<TChar>(ref ValueListBuilder<TChar> vlb, ref NumberBuffer number, int nMaxDigits, int[] groupDigits, ReadOnlySpan<TChar> sDecimal, ReadOnlySpan<TChar> sGroup) where TChar : unmanaged, IUtfChar<TChar>
	{
		int num = number.Scale;
		byte* ptr = number.GetDigitsPointer();
		if (num > 0)
		{
			if (groupDigits != null)
			{
				int num2 = 0;
				int num3 = num;
				int num4 = 0;
				if (groupDigits.Length != 0)
				{
					int num5 = groupDigits[num2];
					while (num > num5 && groupDigits[num2] != 0)
					{
						num3 += sGroup.Length;
						if (num2 < groupDigits.Length - 1)
						{
							num2++;
						}
						num5 += groupDigits[num2];
						if ((num5 | num3) < 0)
						{
							ThrowHelper.ThrowArgumentOutOfRangeException();
						}
					}
					num4 = ((num5 != 0) ? groupDigits[0] : 0);
				}
				num2 = 0;
				int num6 = 0;
				int digitsCount = number.DigitsCount;
				int num7 = ((num < digitsCount) ? num : digitsCount);
				fixed (TChar* ptr2 = &MemoryMarshal.GetReference(vlb.AppendSpan(num3)))
				{
					TChar* ptr3 = ptr2 + num3 - 1;
					for (int num8 = num - 1; num8 >= 0; num8--)
					{
						*(ptr3--) = TChar.CastFrom((char)((num8 < num7) ? ptr[num8] : 48));
						if (num4 > 0)
						{
							num6++;
							if (num6 == num4 && num8 != 0)
							{
								for (int num9 = sGroup.Length - 1; num9 >= 0; num9--)
								{
									*(ptr3--) = sGroup[num9];
								}
								if (num2 < groupDigits.Length - 1)
								{
									num2++;
									num4 = groupDigits[num2];
								}
								num6 = 0;
							}
						}
					}
					ptr += num7;
				}
			}
			else
			{
				do
				{
					vlb.Append(TChar.CastFrom((char)((*ptr != 0) ? (*(ptr++)) : 48)));
				}
				while (--num > 0);
			}
		}
		else
		{
			vlb.Append(TChar.CastFrom('0'));
		}
		if (nMaxDigits <= 0)
		{
			return;
		}
		vlb.Append(sDecimal);
		if (num < 0 && nMaxDigits > 0)
		{
			int num10 = Math.Min(-num, nMaxDigits);
			for (int i = 0; i < num10; i++)
			{
				vlb.Append(TChar.CastFrom('0'));
			}
			num += num10;
			nMaxDigits -= num10;
		}
		while (nMaxDigits > 0)
		{
			vlb.Append(TChar.CastFrom((char)((*ptr != 0) ? (*(ptr++)) : 48)));
			nMaxDigits--;
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static void AppendUnknownChar<TChar>(ref ValueListBuilder<TChar> vlb, char ch) where TChar : unmanaged, IUtfChar<TChar>
	{
		if (typeof(TChar) == typeof(char) || char.IsAscii(ch))
		{
			vlb.Append(TChar.CastFrom(ch));
		}
		else
		{
			AppendNonAsciiBytes(ref vlb, ch);
		}
		[MethodImpl(MethodImplOptions.NoInlining)]
		static void AppendNonAsciiBytes(ref ValueListBuilder<TChar> vlb, char ch)
		{
			Rune rune = new Rune(ch);
			rune.EncodeToUtf8(MemoryMarshal.AsBytes(vlb.AppendSpan(rune.Utf8SequenceLength)));
		}
	}

	private static void FormatNumber<TChar>(ref ValueListBuilder<TChar> vlb, ref NumberBuffer number, int nMaxDigits, NumberFormatInfo info) where TChar : unmanaged, IUtfChar<TChar>
	{
		string text = (number.IsNegative ? s_negNumberFormats[info.NumberNegativePattern] : "#");
		string text2 = text;
		foreach (char c in text2)
		{
			switch (c)
			{
			case '#':
				FormatFixed(ref vlb, ref number, nMaxDigits, info._numberGroupSizes, info.NumberDecimalSeparatorTChar<TChar>(), info.NumberGroupSeparatorTChar<TChar>());
				break;
			case '-':
				vlb.Append(info.NegativeSignTChar<TChar>());
				break;
			default:
				vlb.Append(TChar.CastFrom(c));
				break;
			}
		}
	}

	private unsafe static void FormatScientific<TChar>(ref ValueListBuilder<TChar> vlb, ref NumberBuffer number, int nMaxDigits, NumberFormatInfo info, char expChar) where TChar : unmanaged, IUtfChar<TChar>
	{
		byte* digitsPointer = number.GetDigitsPointer();
		vlb.Append(TChar.CastFrom((char)((*digitsPointer != 0) ? (*(digitsPointer++)) : 48)));
		if (nMaxDigits != 1)
		{
			vlb.Append(info.NumberDecimalSeparatorTChar<TChar>());
		}
		while (--nMaxDigits > 0)
		{
			vlb.Append(TChar.CastFrom((char)((*digitsPointer != 0) ? (*(digitsPointer++)) : 48)));
		}
		int value = ((number.Digits[0] != 0) ? (number.Scale - 1) : 0);
		FormatExponent(ref vlb, info, value, expChar, 3, positiveSign: true);
	}

	private unsafe static void FormatExponent<TChar>(ref ValueListBuilder<TChar> vlb, NumberFormatInfo info, int value, char expChar, int minDigits, bool positiveSign) where TChar : unmanaged, IUtfChar<TChar>
	{
		vlb.Append(TChar.CastFrom(expChar));
		if (value < 0)
		{
			vlb.Append(info.NegativeSignTChar<TChar>());
			value = -value;
		}
		else if (positiveSign)
		{
			vlb.Append(info.PositiveSignTChar<TChar>());
		}
		TChar* ptr = stackalloc TChar[10];
		TChar* ptr2 = UInt32ToDecChars(ptr + 10, (uint)value, minDigits);
		vlb.Append(new ReadOnlySpan<TChar>(ptr2, (int)(ptr + 10 - ptr2)));
	}

	private unsafe static void FormatGeneral<TChar>(ref ValueListBuilder<TChar> vlb, ref NumberBuffer number, int nMaxDigits, NumberFormatInfo info, char expChar, bool suppressScientific) where TChar : unmanaged, IUtfChar<TChar>
	{
		int i = number.Scale;
		bool flag = false;
		if (!suppressScientific && (i > nMaxDigits || i < -3))
		{
			i = 1;
			flag = true;
		}
		byte* digitsPointer = number.GetDigitsPointer();
		if (i > 0)
		{
			do
			{
				vlb.Append(TChar.CastFrom((char)((*digitsPointer != 0) ? (*(digitsPointer++)) : 48)));
			}
			while (--i > 0);
		}
		else
		{
			vlb.Append(TChar.CastFrom('0'));
		}
		if (*digitsPointer != 0 || i < 0)
		{
			vlb.Append(info.NumberDecimalSeparatorTChar<TChar>());
			for (; i < 0; i++)
			{
				vlb.Append(TChar.CastFrom('0'));
			}
			while (*digitsPointer != 0)
			{
				vlb.Append(TChar.CastFrom(*(digitsPointer++)));
			}
		}
		if (flag)
		{
			FormatExponent(ref vlb, info, number.Scale - 1, expChar, 2, positiveSign: true);
		}
	}

	private static void FormatPercent<TChar>(ref ValueListBuilder<TChar> vlb, ref NumberBuffer number, int nMaxDigits, NumberFormatInfo info) where TChar : unmanaged, IUtfChar<TChar>
	{
		string text = (number.IsNegative ? s_negPercentFormats[info.PercentNegativePattern] : s_posPercentFormats[info.PercentPositivePattern]);
		string text2 = text;
		foreach (char c in text2)
		{
			switch (c)
			{
			case '#':
				FormatFixed(ref vlb, ref number, nMaxDigits, info._percentGroupSizes, info.PercentDecimalSeparatorTChar<TChar>(), info.PercentGroupSeparatorTChar<TChar>());
				break;
			case '-':
				vlb.Append(info.NegativeSignTChar<TChar>());
				break;
			case '%':
				vlb.Append(info.PercentSymbolTChar<TChar>());
				break;
			default:
				vlb.Append(TChar.CastFrom(c));
				break;
			}
		}
	}

	internal unsafe static void RoundNumber(ref NumberBuffer number, int pos, bool isCorrectlyRounded)
	{
		byte* digitsPointer = number.GetDigitsPointer();
		int j;
		for (j = 0; j < pos && digitsPointer[j] != 0; j++)
		{
		}
		if (j == pos && ShouldRoundUp(digitsPointer, j, number.Kind, isCorrectlyRounded))
		{
			while (j > 0 && digitsPointer[j - 1] == 57)
			{
				j--;
			}
			if (j > 0)
			{
				byte* num = digitsPointer + (j - 1);
				(*num)++;
			}
			else
			{
				number.Scale++;
				*digitsPointer = 49;
				j = 1;
			}
		}
		else
		{
			while (j > 0 && digitsPointer[j - 1] == 48)
			{
				j--;
			}
		}
		if (j == 0)
		{
			if (number.Kind != NumberBufferKind.FloatingPoint)
			{
				number.IsNegative = false;
			}
			number.Scale = 0;
		}
		digitsPointer[j] = 0;
		number.DigitsCount = j;
		unsafe static bool ShouldRoundUp(byte* dig, int i, NumberBufferKind numberKind, bool isCorrectlyRounded)
		{
			byte b = dig[i];
			if (b == 0 || isCorrectlyRounded)
			{
				return false;
			}
			return b >= 53;
		}
	}

	private unsafe static int FindSection(ReadOnlySpan<char> format, int section)
	{
		if (section == 0)
		{
			return 0;
		}
		fixed (char* ptr = &MemoryMarshal.GetReference(format))
		{
			int num = 0;
			while (true)
			{
				if (num >= format.Length)
				{
					return 0;
				}
				char c;
				char c2 = (c = ptr[num++]);
				if ((uint)c2 <= 34u)
				{
					if (c2 == '\0')
					{
						break;
					}
					if (c2 != '"')
					{
						continue;
					}
				}
				else if (c2 != '\'')
				{
					switch (c2)
					{
					default:
						continue;
					case '\\':
						if (num < format.Length && ptr[num] != 0)
						{
							num++;
						}
						continue;
					case ';':
						break;
					}
					if (--section == 0)
					{
						if (num >= format.Length || ptr[num] == '\0' || ptr[num] == ';')
						{
							break;
						}
						return num;
					}
					continue;
				}
				while (num < format.Length && ptr[num] != 0 && ptr[num++] != c)
				{
				}
			}
			return 0;
		}
	}

	private static ulong ExtractFractionAndBiasedExponent(double value, out int exponent)
	{
		ulong num = BitConverter.DoubleToUInt64Bits(value);
		ulong num2 = num & 0xFFFFFFFFFFFFFuL;
		exponent = (int)(num >> 52) & 0x7FF;
		if (exponent != 0)
		{
			num2 |= 0x10000000000000uL;
			exponent -= 1075;
		}
		else
		{
			exponent = -1074;
		}
		return num2;
	}

	private static ushort ExtractFractionAndBiasedExponent(Half value, out int exponent)
	{
		ushort num = BitConverter.HalfToUInt16Bits(value);
		ushort num2 = (ushort)(num & 0x3FFu);
		exponent = (num >> 10) & 0x1F;
		if (exponent != 0)
		{
			num2 = (ushort)(num2 | 0x400u);
			exponent -= 25;
		}
		else
		{
			exponent = -24;
		}
		return num2;
	}

	private static uint ExtractFractionAndBiasedExponent(float value, out int exponent)
	{
		uint num = BitConverter.SingleToUInt32Bits(value);
		uint num2 = num & 0x7FFFFFu;
		exponent = (int)((num >> 23) & 0xFF);
		if (exponent != 0)
		{
			num2 |= 0x800000u;
			exponent -= 150;
		}
		else
		{
			exponent = -149;
		}
		return num2;
	}

	private unsafe static void AccumulateDecimalDigitsIntoBigInteger(scoped ref NumberBuffer number, uint firstIndex, uint lastIndex, out BigInteger result)
	{
		BigInteger.SetZero(out result);
		byte* ptr = number.GetDigitsPointer() + firstIndex;
		uint num = lastIndex - firstIndex;
		while (num != 0)
		{
			uint num2 = Math.Min(num, 9u);
			uint value = DigitsToUInt32(ptr, (int)num2);
			result.MultiplyPow10(num2);
			result.Add(value);
			ptr += num2;
			num -= num2;
		}
	}

	private static ulong AssembleFloatingPointBits<TFloat>(ulong initialMantissa, int initialExponent, bool hasZeroTail) where TFloat : unmanaged, IBinaryFloatParseAndFormatInfo<TFloat>
	{
		uint num = BigInteger.CountSignificantBits(initialMantissa);
		int num2 = (int)(TFloat.NormalMantissaBits - num);
		int num3 = initialExponent - num2;
		ulong num4 = initialMantissa;
		int num5 = num3;
		if (num3 > TFloat.MaxBinaryExponent)
		{
			return TFloat.InfinityBits;
		}
		if (num3 < TFloat.MinBinaryExponent)
		{
			int num6 = num2 + num3 + TFloat.ExponentBias - 1;
			num5 = -TFloat.ExponentBias;
			if (num6 < 0)
			{
				num4 = RightShiftWithRounding(num4, -num6, hasZeroTail);
				if (num4 == 0L)
				{
					return TFloat.ZeroBits;
				}
				if (num4 > TFloat.DenormalMantissaMask)
				{
					num5 = initialExponent - (num6 + 1) - num2;
				}
			}
			else
			{
				num4 <<= num6;
			}
		}
		else if (num2 < 0)
		{
			num4 = RightShiftWithRounding(num4, -num2, hasZeroTail);
			if (num4 > TFloat.NormalMantissaMask)
			{
				num4 >>= 1;
				num5++;
				if (num5 > TFloat.MaxBinaryExponent)
				{
					return TFloat.InfinityBits;
				}
			}
		}
		else if (num2 > 0)
		{
			num4 <<= num2;
		}
		num4 &= TFloat.DenormalMantissaMask;
		ulong num7 = (ulong)((long)(num5 + TFloat.ExponentBias) << (int)TFloat.DenormalMantissaBits);
		return num7 | num4;
	}

	private static ulong ConvertBigIntegerToFloatingPointBits<TFloat>(ref BigInteger value, uint integerBitsOfPrecision, bool hasNonZeroFractionalPart) where TFloat : unmanaged, IBinaryFloatParseAndFormatInfo<TFloat>
	{
		int denormalMantissaBits = TFloat.DenormalMantissaBits;
		if (integerBitsOfPrecision <= 64)
		{
			return AssembleFloatingPointBits<TFloat>(value.ToUInt64(), denormalMantissaBits, !hasNonZeroFractionalPart);
		}
		(uint Quotient, uint Remainder) tuple = Math.DivRem(integerBitsOfPrecision, 32u);
		uint item = tuple.Quotient;
		uint item2 = tuple.Remainder;
		uint num = item - 1;
		uint num2 = num - 1;
		int num3 = denormalMantissaBits + (int)(num2 * 32);
		bool flag = !hasNonZeroFractionalPart;
		ulong initialMantissa;
		if (item2 == 0)
		{
			initialMantissa = ((ulong)value.GetBlock(num) << 32) + value.GetBlock(num2);
		}
		else
		{
			int num4 = (int)item2;
			int num5 = 64 - num4;
			int num6 = num5 - 32;
			num3 += (int)item2;
			uint block = value.GetBlock(num2);
			uint num7 = block >> num4;
			ulong num8 = (ulong)value.GetBlock(num) << num6;
			ulong num9 = (ulong)value.GetBlock(item) << num5;
			initialMantissa = num9 + num8 + num7;
			uint num10 = (uint)((1 << (int)item2) - 1);
			flag = flag && (block & num10) == 0;
		}
		for (uint num11 = 0u; num11 != num2; num11++)
		{
			flag &= value.GetBlock(num11) == 0;
		}
		return AssembleFloatingPointBits<TFloat>(initialMantissa, num3, flag);
	}

	private unsafe static uint DigitsToUInt32(byte* p, int count)
	{
		byte* ptr = p + count;
		uint num = 0u;
		while (p <= ptr - 8)
		{
			num = num * 100000000 + ParseEightDigitsUnrolled(p);
			p += 8;
		}
		while (p != ptr)
		{
			num = 10 * num + *p - 48;
			p++;
		}
		return num;
	}

	private unsafe static ulong DigitsToUInt64(byte* p, int count)
	{
		byte* ptr = p + count;
		ulong num = 0uL;
		while (ptr - p >= 8)
		{
			num = num * 100000000 + ParseEightDigitsUnrolled(p);
			p += 8;
		}
		while (p != ptr)
		{
			num = 10 * num + *p - 48;
			p++;
		}
		return num;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal unsafe static uint ParseEightDigitsUnrolled(byte* chars)
	{
		ulong num = Unsafe.ReadUnaligned<ulong>(chars);
		if (!BitConverter.IsLittleEndian)
		{
		}
		num -= 3472328296227680304L;
		num = num * 10 + (num >> 8);
		num = (num & 0xFF000000FFL) * 4294967296000100L + ((num >> 16) & 0xFF000000FFL) * 42949672960001L >> 32;
		return (uint)num;
	}

	private unsafe static ulong NumberToFloatingPointBits<TFloat>(ref NumberBuffer number) where TFloat : unmanaged, IBinaryFloatParseAndFormatInfo<TFloat>
	{
		uint digitsCount = (uint)number.DigitsCount;
		uint num = (uint)Math.Max(0, number.Scale);
		uint num2 = Math.Min(num, digitsCount);
		uint num3 = digitsCount - num2;
		if (digitsCount <= 19)
		{
			byte* digitsPointer = number.GetDigitsPointer();
			ulong num4 = DigitsToUInt64(digitsPointer, (int)digitsCount);
			int num5 = (int)(number.Scale - num2 - num3);
			int num6 = Math.Abs(num5);
			if (num4 <= TFloat.MaxMantissaFastPath && num6 <= TFloat.MaxExponentFastPath)
			{
				double num7 = num4;
				double num8 = Pow10DoubleTable[num6];
				num7 = ((num3 == 0) ? (num7 * num8) : (num7 / num8));
				TFloat value = TFloat.CreateSaturating(num7);
				return TFloat.FloatToBits(value);
			}
			(int, ulong) tuple = ComputeFloat<TFloat>(num5, num4);
			if (tuple.Item1 > 0)
			{
				ulong item = tuple.Item2;
				return item | ((ulong)(uint)tuple.Item1 << (int)TFloat.DenormalMantissaBits);
			}
		}
		return NumberToFloatingPointBitsSlow<TFloat>(ref number, num, num2, num3);
	}

	private static ulong NumberToFloatingPointBitsSlow<TFloat>(ref NumberBuffer number, uint positiveExponent, uint integerDigitsPresent, uint fractionalDigitsPresent) where TFloat : unmanaged, IBinaryFloatParseAndFormatInfo<TFloat>
	{
		uint num = (uint)(TFloat.NormalMantissaBits + 1);
		uint digitsCount = (uint)number.DigitsCount;
		uint num2 = positiveExponent - integerDigitsPresent;
		uint lastIndex = digitsCount;
		AccumulateDecimalDigitsIntoBigInteger(ref number, 0u, integerDigitsPresent, out var result);
		if (num2 != 0)
		{
			if (num2 > TFloat.OverflowDecimalExponent)
			{
				return TFloat.InfinityBits;
			}
			result.MultiplyPow10(num2);
		}
		uint num3 = BigInteger.CountSignificantBits(ref result);
		if (num3 >= num || fractionalDigitsPresent == 0)
		{
			return ConvertBigIntegerToFloatingPointBits<TFloat>(ref result, num3, fractionalDigitsPresent != 0);
		}
		uint num4 = fractionalDigitsPresent;
		if (number.Scale < 0)
		{
			num4 += (uint)(-number.Scale);
		}
		if (num3 == 0 && num4 - (int)digitsCount > TFloat.OverflowDecimalExponent)
		{
			return TFloat.ZeroBits;
		}
		AccumulateDecimalDigitsIntoBigInteger(ref number, integerDigitsPresent, lastIndex, out var result2);
		if (result2.IsZero())
		{
			return ConvertBigIntegerToFloatingPointBits<TFloat>(ref result, num3, fractionalDigitsPresent != 0);
		}
		BigInteger.Pow10(num4, out var result3);
		uint num5 = BigInteger.CountSignificantBits(ref result2);
		uint num6 = BigInteger.CountSignificantBits(ref result3);
		uint num7 = 0u;
		if (num6 > num5)
		{
			num7 = num6 - num5;
		}
		if (num7 != 0)
		{
			result2.ShiftLeft(num7);
		}
		uint num8 = num - num3;
		uint num9 = num8;
		if (num3 != 0)
		{
			if (num7 > num9)
			{
				return ConvertBigIntegerToFloatingPointBits<TFloat>(ref result, num3, fractionalDigitsPresent != 0);
			}
			num9 -= num7;
		}
		uint num10 = num7;
		if (BigInteger.Compare(ref result2, ref result3) < 0)
		{
			num10++;
		}
		result2.ShiftLeft(num9);
		BigInteger.DivRem(ref result2, ref result3, out var quo, out var rem);
		ulong num11 = quo.ToUInt64();
		bool flag = !number.HasNonZeroTail && rem.IsZero();
		uint num12 = BigInteger.CountSignificantBits(num11);
		if (num12 > num8)
		{
			int num13 = (int)(num12 - num8);
			flag = flag && (num11 & (ulong)((1L << num13) - 1)) == 0;
			num11 >>= num13;
		}
		ulong num14 = result.ToUInt64();
		ulong initialMantissa = (num14 << (int)num8) + num11;
		int initialExponent = (int)((num3 != 0) ? (num3 - 2) : (0 - num10 - 1));
		return AssembleFloatingPointBits<TFloat>(initialMantissa, initialExponent, flag);
	}

	private static ulong RightShiftWithRounding(ulong value, int shift, bool hasZeroTail)
	{
		if (shift >= 64)
		{
			return 0uL;
		}
		ulong num = (ulong)((1L << shift - 1) - 1);
		ulong num2 = (ulong)(1L << shift - 1);
		ulong num3 = (ulong)(1L << shift);
		bool lsbBit = (value & num3) != 0;
		bool roundBit = (value & num2) != 0;
		bool hasTailBits = !hasZeroTail || (value & num) != 0;
		return (value >> shift) + (ulong)(ShouldRoundUp(lsbBit, roundBit, hasTailBits) ? 1 : 0);
	}

	private static bool ShouldRoundUp(bool lsbBit, bool roundBit, bool hasTailBits)
	{
		if (roundBit)
		{
			return hasTailBits || lsbBit;
		}
		return false;
	}

	internal static (int Exponent, ulong Mantissa) ComputeFloat<TFloat>(long q, ulong w) where TFloat : unmanaged, IBinaryFloatParseAndFormatInfo<TFloat>
	{
		ulong item = 0uL;
		if (w == 0L || q < TFloat.MinFastFloatDecimalExponent)
		{
			return default((int, ulong));
		}
		int infinityExponent;
		if (q > TFloat.MaxFastFloatDecimalExponent)
		{
			infinityExponent = TFloat.InfinityExponent;
			item = 0uL;
			return (Exponent: infinityExponent, Mantissa: item);
		}
		int num = BitOperations.LeadingZeroCount(w);
		w <<= num;
		(ulong, ulong) tuple = ComputeProductApproximation(TFloat.DenormalMantissaBits + 3, q, w);
		if (tuple.Item2 == ulong.MaxValue && (q < -27 || q > 55))
		{
			infinityExponent = -1;
			return (Exponent: infinityExponent, Mantissa: item);
		}
		int num2 = (int)(tuple.Item1 >> 63);
		item = tuple.Item1 >> num2 + 64 - TFloat.DenormalMantissaBits - 3;
		infinityExponent = CalculatePower((int)q) + num2 - num - -TFloat.MaxBinaryExponent;
		if (infinityExponent <= 0)
		{
			if (-infinityExponent + 1 >= 64)
			{
				infinityExponent = 0;
				item = 0uL;
				return (Exponent: infinityExponent, Mantissa: item);
			}
			item >>= -infinityExponent + 1;
			item += item & 1;
			item >>= 1;
			infinityExponent = ((item >= (ulong)(1L << (int)TFloat.DenormalMantissaBits)) ? 1 : 0);
			return (Exponent: infinityExponent, Mantissa: item);
		}
		if (tuple.Item2 <= 1 && q >= TFloat.MinExponentRoundToEven && q <= TFloat.MaxExponentRoundToEven && (item & 3) == 1 && item << num2 + 64 - TFloat.DenormalMantissaBits - 3 == tuple.Item1)
		{
			item &= 0xFFFFFFFFFFFFFFFEuL;
		}
		item += item & 1;
		item >>= 1;
		if (item >= (ulong)(2L << (int)TFloat.DenormalMantissaBits))
		{
			item = (ulong)(1L << (int)TFloat.DenormalMantissaBits);
			infinityExponent++;
		}
		item &= (ulong)(~(1L << (int)TFloat.DenormalMantissaBits));
		if (infinityExponent >= TFloat.InfinityExponent)
		{
			infinityExponent = TFloat.InfinityExponent;
			item = 0uL;
		}
		return (Exponent: infinityExponent, Mantissa: item);
	}

	private static (ulong high, ulong low) ComputeProductApproximation(int bitPrecision, long q, ulong w)
	{
		int num = 2 * (int)(q - -342);
		ulong low;
		ulong num2 = Math.BigMul(w, Pow5128Table[num], out low);
		ulong num3 = ((bitPrecision < 64) ? (ulong.MaxValue >> bitPrecision) : ulong.MaxValue);
		if ((num2 & num3) == num3)
		{
			ulong low2;
			ulong num4 = Math.BigMul(w, Pow5128Table[num + 1], out low2);
			low += num4;
			if (num4 > low)
			{
				num2++;
			}
		}
		return (high: num2, low: low);
	}

	internal static int CalculatePower(int q)
	{
		return (217706 * q >> 16) + 63;
	}

	private unsafe static bool TryNumberBufferToBinaryInteger<TInteger>(ref NumberBuffer number, ref TInteger value) where TInteger : unmanaged, IBinaryIntegerParseAndFormatInfo<TInteger>
	{
		int num = number.Scale;
		if (num > TInteger.MaxDigitCount || num < number.DigitsCount || (!TInteger.IsSigned && number.IsNegative))
		{
			return false;
		}
		byte* digitsPointer = number.GetDigitsPointer();
		TInteger val = TInteger.Zero;
		while (--num >= 0)
		{
			if (TInteger.IsGreaterThanAsUnsigned(val, TInteger.MaxValueDiv10))
			{
				return false;
			}
			val = TInteger.MultiplyBy10(val);
			if (*digitsPointer != 0)
			{
				TInteger val2 = val + TInteger.CreateTruncating(*(digitsPointer++) - 48);
				if (!TInteger.IsSigned && val2 < val)
				{
					return false;
				}
				val = val2;
			}
		}
		if (TInteger.IsSigned)
		{
			if (number.IsNegative)
			{
				val = -val;
				if (val > TInteger.Zero)
				{
					return false;
				}
			}
			else if (val < TInteger.Zero)
			{
				return false;
			}
		}
		value = val;
		return true;
	}

	internal static TInteger ParseBinaryInteger<TChar, TInteger>(ReadOnlySpan<TChar> value, NumberStyles styles, NumberFormatInfo info) where TChar : unmanaged, IUtfChar<TChar> where TInteger : unmanaged, IBinaryIntegerParseAndFormatInfo<TInteger>
	{
		TInteger result;
		ParsingStatus parsingStatus = TryParseBinaryInteger<TChar, TInteger>(value, styles, info, out result);
		if (parsingStatus != 0)
		{
			ThrowOverflowOrFormatException<TChar, TInteger>(parsingStatus, value);
		}
		return result;
	}

	private unsafe static bool TryParseNumber<TChar>(scoped ref TChar* str, TChar* strEnd, NumberStyles styles, ref NumberBuffer number, NumberFormatInfo info) where TChar : unmanaged, IUtfChar<TChar>
	{
		ReadOnlySpan<TChar> value = ReadOnlySpan<TChar>.Empty;
		bool flag = false;
		ReadOnlySpan<TChar> value2;
		ReadOnlySpan<TChar> value3;
		if ((styles & NumberStyles.AllowCurrencySymbol) != 0)
		{
			value = info.CurrencySymbolTChar<TChar>();
			value2 = info.CurrencyDecimalSeparatorTChar<TChar>();
			value3 = info.CurrencyGroupSeparatorTChar<TChar>();
			flag = true;
		}
		else
		{
			value2 = info.NumberDecimalSeparatorTChar<TChar>();
			value3 = info.NumberGroupSeparatorTChar<TChar>();
		}
		int num = 0;
		TChar* ptr = str;
		uint num2 = ((ptr < strEnd) ? TChar.CastToUInt32(*ptr) : 0u);
		while (true)
		{
			if (!IsWhite(num2) || (styles & NumberStyles.AllowLeadingWhite) == 0 || (((uint)num & (true ? 1u : 0u)) != 0 && (num & 0x20) == 0 && info.NumberNegativePattern != 2))
			{
				TChar* ptr2;
				if ((styles & NumberStyles.AllowLeadingSign) != 0 && (num & 1) == 0 && ((ptr2 = MatchChars(ptr, strEnd, info.PositiveSignTChar<TChar>())) != null || ((ptr2 = MatchNegativeSignChars(ptr, strEnd, info)) != null && (number.IsNegative = true))))
				{
					num |= 1;
					ptr = ptr2 - 1;
				}
				else if (num2 == 40 && (styles & NumberStyles.AllowParentheses) != 0 && (num & 1) == 0)
				{
					num |= 3;
					number.IsNegative = true;
				}
				else
				{
					if (value.IsEmpty || (ptr2 = MatchChars(ptr, strEnd, value)) == null)
					{
						break;
					}
					num |= 0x20;
					value = ReadOnlySpan<TChar>.Empty;
					ptr = ptr2 - 1;
				}
			}
			num2 = ((++ptr < strEnd) ? TChar.CastToUInt32(*ptr) : 0u);
		}
		int num3 = 0;
		int num4 = 0;
		int num5 = number.Digits.Length - 1;
		int num6 = 0;
		while (true)
		{
			TChar* ptr2;
			if (IsDigit(num2))
			{
				num |= 4;
				if (num2 != 48 || ((uint)num & 8u) != 0)
				{
					if (num3 < num5)
					{
						number.Digits[num3] = (byte)num2;
						if (num2 != 48 || number.Kind != NumberBufferKind.Integer)
						{
							num4 = num3 + 1;
						}
					}
					else if (num2 != 48)
					{
						number.HasNonZeroTail = true;
					}
					if ((num & 0x10) == 0)
					{
						number.Scale++;
					}
					if (num3 < num5)
					{
						num6 = ((num2 == 48) ? (num6 + 1) : 0);
					}
					num3++;
					num |= 8;
				}
				else if (((uint)num & 0x10u) != 0)
				{
					number.Scale--;
				}
			}
			else if ((styles & NumberStyles.AllowDecimalPoint) != 0 && (num & 0x10) == 0 && ((ptr2 = MatchChars(ptr, strEnd, value2)) != null || (flag && (num & 0x20) == 0 && (ptr2 = MatchChars(ptr, strEnd, info.NumberDecimalSeparatorTChar<TChar>())) != null)))
			{
				num |= 0x10;
				ptr = ptr2 - 1;
			}
			else
			{
				if ((styles & NumberStyles.AllowThousands) == 0 || (num & 4) == 0 || ((uint)num & 0x10u) != 0 || ((ptr2 = MatchChars(ptr, strEnd, value3)) == null && (!flag || ((uint)num & 0x20u) != 0 || (ptr2 = MatchChars(ptr, strEnd, info.NumberGroupSeparatorTChar<TChar>())) == null)))
				{
					break;
				}
				ptr = ptr2 - 1;
			}
			num2 = ((++ptr < strEnd) ? TChar.CastToUInt32(*ptr) : 0u);
		}
		bool flag2 = false;
		number.DigitsCount = num4;
		number.Digits[num4] = 0;
		if (((uint)num & 4u) != 0)
		{
			if ((num2 == 69 || num2 == 101) && (styles & NumberStyles.AllowExponent) != 0)
			{
				TChar* ptr3 = ptr;
				num2 = ((++ptr < strEnd) ? TChar.CastToUInt32(*ptr) : 0u);
				TChar* ptr2;
				if ((ptr2 = MatchChars(ptr, strEnd, info.PositiveSignTChar<TChar>())) != null)
				{
					num2 = (((ptr = ptr2) < strEnd) ? TChar.CastToUInt32(*ptr) : 0u);
				}
				else if ((ptr2 = MatchNegativeSignChars(ptr, strEnd, info)) != null)
				{
					num2 = (((ptr = ptr2) < strEnd) ? TChar.CastToUInt32(*ptr) : 0u);
					flag2 = true;
				}
				if (IsDigit(num2))
				{
					int num7 = 0;
					do
					{
						num7 = num7 * 10 + (int)(num2 - 48);
						num2 = ((++ptr < strEnd) ? TChar.CastToUInt32(*ptr) : 0u);
						if (num7 > 1000)
						{
							num7 = 9999;
							while (IsDigit(num2))
							{
								num2 = ((++ptr < strEnd) ? TChar.CastToUInt32(*ptr) : 0u);
							}
						}
					}
					while (IsDigit(num2));
					if (flag2)
					{
						num7 = -num7;
					}
					number.Scale += num7;
				}
				else
				{
					ptr = ptr3;
					num2 = ((ptr < strEnd) ? TChar.CastToUInt32(*ptr) : 0u);
				}
			}
			if (number.Kind == NumberBufferKind.FloatingPoint && !number.HasNonZeroTail)
			{
				int num8 = num4 - number.Scale;
				if (num8 > 0)
				{
					num6 = Math.Min(num6, num8);
					number.DigitsCount = num4 - num6;
					number.Digits[number.DigitsCount] = 0;
				}
			}
			while (true)
			{
				if (!IsWhite(num2) || (styles & NumberStyles.AllowTrailingWhite) == 0)
				{
					TChar* ptr2;
					if ((styles & NumberStyles.AllowTrailingSign) != 0 && (num & 1) == 0 && ((ptr2 = MatchChars(ptr, strEnd, info.PositiveSignTChar<TChar>())) != null || ((ptr2 = MatchNegativeSignChars(ptr, strEnd, info)) != null && (number.IsNegative = true))))
					{
						num |= 1;
						ptr = ptr2 - 1;
					}
					else if (num2 == 41 && ((uint)num & 2u) != 0)
					{
						num &= -3;
					}
					else
					{
						if (value.IsEmpty || (ptr2 = MatchChars(ptr, strEnd, value)) == null)
						{
							break;
						}
						value = ReadOnlySpan<TChar>.Empty;
						ptr = ptr2 - 1;
					}
				}
				num2 = ((++ptr < strEnd) ? TChar.CastToUInt32(*ptr) : 0u);
			}
			if ((num & 2) == 0)
			{
				if ((num & 8) == 0)
				{
					if (number.Kind != NumberBufferKind.Decimal)
					{
						number.Scale = 0;
					}
					if (number.Kind == NumberBufferKind.Integer && (num & 0x10) == 0)
					{
						number.IsNegative = false;
					}
				}
				str = ptr;
				return true;
			}
		}
		str = ptr;
		return false;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static ParsingStatus TryParseBinaryInteger<TChar, TInteger>(ReadOnlySpan<TChar> value, NumberStyles styles, NumberFormatInfo info, out TInteger result) where TChar : unmanaged, IUtfChar<TChar> where TInteger : unmanaged, IBinaryIntegerParseAndFormatInfo<TInteger>
	{
		if ((styles & ~NumberStyles.Integer) == 0)
		{
			return TryParseBinaryIntegerStyle<TChar, TInteger>(value, styles, info, out result);
		}
		if ((styles & NumberStyles.AllowHexSpecifier) != 0)
		{
			return TryParseBinaryIntegerHexNumberStyle<TChar, TInteger>(value, styles, out result);
		}
		if ((styles & NumberStyles.AllowBinarySpecifier) != 0)
		{
			return TryParseBinaryIntegerHexOrBinaryNumberStyle<TChar, TInteger, BinaryParser<TInteger>>(value, styles, out result);
		}
		return TryParseBinaryIntegerNumber<TChar, TInteger>(value, styles, info, out result);
	}

	private static ParsingStatus TryParseBinaryIntegerNumber<TChar, TInteger>(ReadOnlySpan<TChar> value, NumberStyles styles, NumberFormatInfo info, out TInteger result) where TChar : unmanaged, IUtfChar<TChar> where TInteger : unmanaged, IBinaryIntegerParseAndFormatInfo<TInteger>
	{
		result = TInteger.Zero;
		Span<byte> digits = stackalloc byte[TInteger.MaxDigitCount + 1];
		NumberBuffer number = new NumberBuffer(NumberBufferKind.Integer, digits);
		if (!TryStringToNumber(value, styles, ref number, info))
		{
			return ParsingStatus.Failed;
		}
		if (!TryNumberBufferToBinaryInteger(ref number, ref result))
		{
			return ParsingStatus.Overflow;
		}
		return ParsingStatus.OK;
	}

	internal static ParsingStatus TryParseBinaryIntegerStyle<TChar, TInteger>(ReadOnlySpan<TChar> value, NumberStyles styles, NumberFormatInfo info, out TInteger result) where TChar : unmanaged, IUtfChar<TChar> where TInteger : unmanaged, IBinaryIntegerParseAndFormatInfo<TInteger>
	{
		int i;
		uint num;
		if (!value.IsEmpty)
		{
			i = 0;
			num = TChar.CastToUInt32(value[0]);
			if ((styles & NumberStyles.AllowLeadingWhite) == 0 || !IsWhite(num))
			{
				goto IL_0066;
			}
			while (true)
			{
				i++;
				if ((uint)i >= (uint)value.Length)
				{
					break;
				}
				num = TChar.CastToUInt32(value[i]);
				if (IsWhite(num))
				{
					continue;
				}
				goto IL_0066;
			}
		}
		goto IL_04ab;
		IL_04d7:
		if (IsWhite(num))
		{
			if ((styles & NumberStyles.AllowTrailingWhite) == 0)
			{
				goto IL_04ab;
			}
			for (i++; i < value.Length; i++)
			{
				uint ch = TChar.CastToUInt32(value[i]);
				if (!IsWhite(ch))
				{
					break;
				}
			}
			if ((uint)i >= (uint)value.Length)
			{
				goto IL_0471;
			}
		}
		if (TrailingZeros(value, i))
		{
			goto IL_0471;
		}
		goto IL_04ab;
		IL_0471:
		bool flag;
		if (!flag)
		{
			goto IL_0474;
		}
		goto IL_04c1;
		IL_0245:
		TInteger val = TInteger.CreateTruncating(num - 48);
		i++;
		int num2 = 0;
		while (num2 < TInteger.MaxDigitCount - 2)
		{
			if ((uint)i >= (uint)value.Length)
			{
				goto IL_0269;
			}
			num = TChar.CastToUInt32(value[i]);
			if (IsDigit(num))
			{
				i++;
				val = TInteger.MultiplyBy10(val);
				val += TInteger.CreateTruncating(num - 48);
				num2++;
				continue;
			}
			goto IL_04d7;
		}
		if ((uint)i >= (uint)value.Length)
		{
			if (!TInteger.IsSigned)
			{
				goto IL_0471;
			}
			goto IL_0474;
		}
		num = TChar.CastToUInt32(value[i]);
		bool flag2;
		if (IsDigit(num))
		{
			i++;
			flag = (TInteger.IsSigned ? (val > TInteger.MaxValueDiv10) : (flag | (val > TInteger.MaxValueDiv10 || (val == TInteger.MaxValueDiv10 && num > 53))));
			val = TInteger.MultiplyBy10(val);
			val += TInteger.CreateTruncating(num - 48);
			if (TInteger.IsSigned)
			{
				flag |= TInteger.IsGreaterThanAsUnsigned(val, TInteger.MaxValue + (flag2 ? TInteger.One : TInteger.Zero));
			}
			if ((uint)i >= (uint)value.Length)
			{
				goto IL_0471;
			}
			num = TChar.CastToUInt32(value[i]);
			while (IsDigit(num))
			{
				flag = true;
				i++;
				if ((uint)i < (uint)value.Length)
				{
					num = TChar.CastToUInt32(value[i]);
					continue;
				}
				goto IL_04c1;
			}
		}
		goto IL_04d7;
		IL_04c1:
		result = TInteger.Zero;
		return ParsingStatus.Overflow;
		IL_0066:
		flag2 = false;
		if ((styles & NumberStyles.AllowLeadingSign) != 0)
		{
			if (info.HasInvariantNumberSigns)
			{
				if (num == 45)
				{
					flag2 = true;
					i++;
					if ((uint)i >= (uint)value.Length)
					{
						goto IL_04ab;
					}
					num = TChar.CastToUInt32(value[i]);
				}
				else if (num == 43)
				{
					i++;
					if ((uint)i >= (uint)value.Length)
					{
						goto IL_04ab;
					}
					num = TChar.CastToUInt32(value[i]);
				}
			}
			else if (info.AllowHyphenDuringParsing && num == 45)
			{
				flag2 = true;
				i++;
				if ((uint)i >= (uint)value.Length)
				{
					goto IL_04ab;
				}
				num = TChar.CastToUInt32(value[i]);
			}
			else
			{
				value = value.Slice(i);
				i = 0;
				ReadOnlySpan<TChar> value2 = info.PositiveSignTChar<TChar>();
				ReadOnlySpan<TChar> value3 = info.NegativeSignTChar<TChar>();
				if (!value2.IsEmpty && value.StartsWith(value2))
				{
					i += value2.Length;
					if ((uint)i >= (uint)value.Length)
					{
						goto IL_04ab;
					}
					num = TChar.CastToUInt32(value[i]);
				}
				else if (!value3.IsEmpty && value.StartsWith(value3))
				{
					flag2 = true;
					i += value3.Length;
					if ((uint)i >= (uint)value.Length)
					{
						goto IL_04ab;
					}
					num = TChar.CastToUInt32(value[i]);
				}
			}
		}
		flag = !TInteger.IsSigned && flag2;
		val = TInteger.Zero;
		if (IsDigit(num))
		{
			if (num != 48)
			{
				goto IL_0245;
			}
			while (true)
			{
				i++;
				if ((uint)i >= (uint)value.Length)
				{
					break;
				}
				num = TChar.CastToUInt32(value[i]);
				if (num == 48)
				{
					continue;
				}
				goto IL_0226;
			}
			goto IL_0474;
		}
		goto IL_04ab;
		IL_0269:
		if (!TInteger.IsSigned)
		{
			goto IL_0471;
		}
		goto IL_0474;
		IL_0226:
		if (IsDigit(num))
		{
			goto IL_0245;
		}
		if (!TInteger.IsSigned)
		{
			flag = false;
		}
		goto IL_04d7;
		IL_0474:
		if (!TInteger.IsSigned)
		{
			result = val;
		}
		else
		{
			result = (flag2 ? (-val) : val);
		}
		return ParsingStatus.OK;
		IL_04ab:
		result = TInteger.Zero;
		return ParsingStatus.Failed;
	}

	internal static ParsingStatus TryParseBinaryIntegerHexNumberStyle<TChar, TInteger>(ReadOnlySpan<TChar> value, NumberStyles styles, out TInteger result) where TChar : unmanaged, IUtfChar<TChar> where TInteger : unmanaged, IBinaryIntegerParseAndFormatInfo<TInteger>
	{
		return TryParseBinaryIntegerHexOrBinaryNumberStyle<TChar, TInteger, HexParser<TInteger>>(value, styles, out result);
	}

	private static ParsingStatus TryParseBinaryIntegerHexOrBinaryNumberStyle<TChar, TInteger, TParser>(ReadOnlySpan<TChar> value, NumberStyles styles, out TInteger result) where TChar : unmanaged, IUtfChar<TChar> where TInteger : unmanaged, IBinaryIntegerParseAndFormatInfo<TInteger> where TParser : struct, IHexOrBinaryParser<TInteger>
	{
		int i;
		uint num;
		if (!value.IsEmpty)
		{
			i = 0;
			num = TChar.CastToUInt32(value[0]);
			if ((styles & NumberStyles.AllowLeadingWhite) == 0 || !IsWhite(num))
			{
				goto IL_0066;
			}
			while (true)
			{
				i++;
				if ((uint)i >= (uint)value.Length)
				{
					break;
				}
				num = TChar.CastToUInt32(value[i]);
				if (IsWhite(num))
				{
					continue;
				}
				goto IL_0066;
			}
		}
		goto IL_01f0;
		IL_00ca:
		TInteger val = TInteger.CreateTruncating(TParser.FromChar(num));
		i++;
		int num2 = 0;
		while (num2 < TParser.MaxDigitCount - 1)
		{
			if ((uint)i >= (uint)value.Length)
			{
				goto IL_01e3;
			}
			num = TChar.CastToUInt32(value[i]);
			uint num3 = TParser.FromChar(num);
			if (num3 <= TParser.MaxDigitValue)
			{
				i++;
				val = TParser.ShiftLeftForNextDigit(val);
				val += TInteger.CreateTruncating(num3);
				num2++;
				continue;
			}
			goto IL_021c;
		}
		if ((uint)i >= (uint)value.Length)
		{
			goto IL_01e3;
		}
		num = TChar.CastToUInt32(value[i]);
		if (TParser.IsValidChar(num))
		{
			while (true)
			{
				i++;
				if ((uint)i >= (uint)value.Length)
				{
					break;
				}
				num = TChar.CastToUInt32(value[i]);
				if (TParser.IsValidChar(num))
				{
					continue;
				}
				goto IL_01dc;
			}
			goto IL_0206;
		}
		goto IL_021c;
		IL_0206:
		result = TInteger.Zero;
		return ParsingStatus.Overflow;
		IL_01e0:
		bool flag;
		if (!flag)
		{
			goto IL_01e3;
		}
		goto IL_0206;
		IL_01f0:
		result = TInteger.Zero;
		return ParsingStatus.Failed;
		IL_0066:
		flag = false;
		val = TInteger.Zero;
		if (TParser.IsValidChar(num))
		{
			if (num != 48)
			{
				goto IL_00ca;
			}
			while (true)
			{
				i++;
				if ((uint)i >= (uint)value.Length)
				{
					break;
				}
				num = TChar.CastToUInt32(value[i]);
				if (num == 48)
				{
					continue;
				}
				goto IL_00b9;
			}
			goto IL_01e3;
		}
		goto IL_01f0;
		IL_01dc:
		flag = true;
		goto IL_021c;
		IL_021c:
		if (IsWhite(num))
		{
			if ((styles & NumberStyles.AllowTrailingWhite) == 0)
			{
				goto IL_01f0;
			}
			for (i++; i < value.Length; i++)
			{
				uint ch = TChar.CastToUInt32(value[i]);
				if (!IsWhite(ch))
				{
					break;
				}
			}
			if ((uint)i >= (uint)value.Length)
			{
				goto IL_01e0;
			}
		}
		if (TrailingZeros(value, i))
		{
			goto IL_01e0;
		}
		goto IL_01f0;
		IL_01e3:
		result = val;
		return ParsingStatus.OK;
		IL_00b9:
		if (TParser.IsValidChar(num))
		{
			goto IL_00ca;
		}
		goto IL_021c;
	}

	internal static decimal ParseDecimal<TChar>(ReadOnlySpan<TChar> value, NumberStyles styles, NumberFormatInfo info) where TChar : unmanaged, IUtfChar<TChar>
	{
		decimal result;
		ParsingStatus parsingStatus = TryParseDecimal(value, styles, info, out result);
		if (parsingStatus != 0)
		{
			if (parsingStatus == ParsingStatus.Failed)
			{
				ThrowFormatException(value);
			}
			ThrowOverflowException(SR.Overflow_Decimal);
		}
		return result;
	}

	internal unsafe static bool TryNumberToDecimal(ref NumberBuffer number, ref decimal value)
	{
		byte* ptr = number.GetDigitsPointer();
		int num = number.Scale;
		bool isNegative = number.IsNegative;
		uint num2 = *ptr;
		if (num2 == 0)
		{
			value = new decimal(0, 0, 0, isNegative, (byte)Math.Clamp(-num, 0, 28));
			return true;
		}
		if (num > 29)
		{
			return false;
		}
		ulong num3 = 0uL;
		while (num > -28)
		{
			num--;
			num3 *= 10;
			num3 += num2 - 48;
			num2 = *(++ptr);
			if (num3 >= 1844674407370955161L)
			{
				break;
			}
			if (num2 != 0)
			{
				continue;
			}
			while (num > 0)
			{
				num--;
				num3 *= 10;
				if (num3 >= 1844674407370955161L)
				{
					break;
				}
			}
			break;
		}
		uint num4 = 0u;
		while ((num > 0 || (num2 != 0 && num > -28)) && (num4 < 429496729 || (num4 == 429496729 && (num3 < 11068046444225730969uL || (num3 == 11068046444225730969uL && num2 <= 53)))))
		{
			ulong num5 = (ulong)(uint)num3 * 10uL;
			ulong num6 = (ulong)((long)(uint)(num3 >> 32) * 10L) + (num5 >> 32);
			num3 = (uint)num5 + (num6 << 32);
			num4 = (uint)(int)(num6 >> 32) + num4 * 10;
			if (num2 != 0)
			{
				num2 -= 48;
				num3 += num2;
				if (num3 < num2)
				{
					num4++;
				}
				num2 = *(++ptr);
			}
			num--;
		}
		if (num2 >= 53)
		{
			if (num2 == 53 && (num3 & 1) == 0L)
			{
				num2 = *(++ptr);
				bool flag = !number.HasNonZeroTail;
				while (num2 != 0 && flag)
				{
					flag = flag && num2 == 48;
					num2 = *(++ptr);
				}
				if (flag)
				{
					goto IL_01a8;
				}
			}
			if (++num3 == 0L && ++num4 == 0)
			{
				num3 = 11068046444225730970uL;
				num4 = 429496729u;
				num++;
			}
		}
		goto IL_01a8;
		IL_01a8:
		if (num > 0)
		{
			return false;
		}
		if (num <= -29)
		{
			value = new decimal(0, 0, 0, isNegative, 28);
		}
		else
		{
			value = new decimal((int)num3, (int)(num3 >> 32), (int)num4, isNegative, (byte)(-num));
		}
		return true;
	}

	internal static TFloat ParseFloat<TChar, TFloat>(ReadOnlySpan<TChar> value, NumberStyles styles, NumberFormatInfo info) where TChar : unmanaged, IUtfChar<TChar> where TFloat : unmanaged, IBinaryFloatParseAndFormatInfo<TFloat>
	{
		if (!TryParseFloat<TChar, TFloat>(value, styles, info, out var result))
		{
			ThrowFormatException(value);
		}
		return result;
	}

	internal static ParsingStatus TryParseDecimal<TChar>(ReadOnlySpan<TChar> value, NumberStyles styles, NumberFormatInfo info, out decimal result) where TChar : unmanaged, IUtfChar<TChar>
	{
		Span<byte> digits = stackalloc byte[31];
		NumberBuffer number = new NumberBuffer(NumberBufferKind.Decimal, digits);
		result = default(decimal);
		if (!TryStringToNumber(value, styles, ref number, info))
		{
			return ParsingStatus.Failed;
		}
		if (!TryNumberToDecimal(ref number, ref result))
		{
			return ParsingStatus.Overflow;
		}
		return ParsingStatus.OK;
	}

	internal static bool SpanStartsWith<TChar>(ReadOnlySpan<TChar> span, TChar c) where TChar : unmanaged, IUtfChar<TChar>
	{
		if (!span.IsEmpty)
		{
			return span[0] == c;
		}
		return false;
	}

	internal static bool SpanStartsWith<TChar>(ReadOnlySpan<TChar> span, ReadOnlySpan<TChar> value, StringComparison comparisonType) where TChar : unmanaged, IUtfChar<TChar>
	{
		if (typeof(TChar) == typeof(char))
		{
			ReadOnlySpan<char> span2 = MemoryMarshal.CreateReadOnlySpan(ref Unsafe.As<TChar, char>(ref MemoryMarshal.GetReference(span)), span.Length);
			ReadOnlySpan<char> value2 = MemoryMarshal.CreateReadOnlySpan(ref Unsafe.As<TChar, char>(ref MemoryMarshal.GetReference(value)), value.Length);
			return span2.StartsWith(value2, comparisonType);
		}
		ReadOnlySpan<byte> span3 = MemoryMarshal.CreateReadOnlySpan(ref Unsafe.As<TChar, byte>(ref MemoryMarshal.GetReference(span)), span.Length);
		ReadOnlySpan<byte> value3 = MemoryMarshal.CreateReadOnlySpan(ref Unsafe.As<TChar, byte>(ref MemoryMarshal.GetReference(value)), value.Length);
		return span3.StartsWithUtf8(value3, comparisonType);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static ReadOnlySpan<TChar> SpanTrim<TChar>(ReadOnlySpan<TChar> span) where TChar : unmanaged, IUtfChar<TChar>
	{
		if (typeof(TChar) == typeof(char))
		{
			ReadOnlySpan<char> span2 = MemoryMarshal.CreateReadOnlySpan(ref Unsafe.As<TChar, char>(ref MemoryMarshal.GetReference(span)), span.Length);
			ReadOnlySpan<char> span3 = span2.Trim();
			return MemoryMarshal.CreateReadOnlySpan(ref Unsafe.As<char, TChar>(ref MemoryMarshal.GetReference(span3)), span3.Length);
		}
		ReadOnlySpan<byte> span4 = MemoryMarshal.CreateReadOnlySpan(ref Unsafe.As<TChar, byte>(ref MemoryMarshal.GetReference(span)), span.Length);
		ReadOnlySpan<byte> span5 = span4.TrimUtf8();
		return MemoryMarshal.CreateReadOnlySpan(ref Unsafe.As<byte, TChar>(ref MemoryMarshal.GetReference(span5)), span5.Length);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static bool SpanEqualsOrdinalIgnoreCase<TChar>(ReadOnlySpan<TChar> span, ReadOnlySpan<TChar> value) where TChar : unmanaged, IUtfChar<TChar>
	{
		if (typeof(TChar) == typeof(char))
		{
			ReadOnlySpan<char> span2 = MemoryMarshal.CreateReadOnlySpan(ref Unsafe.As<TChar, char>(ref MemoryMarshal.GetReference(span)), span.Length);
			ReadOnlySpan<char> value2 = MemoryMarshal.CreateReadOnlySpan(ref Unsafe.As<TChar, char>(ref MemoryMarshal.GetReference(value)), value.Length);
			return span2.EqualsOrdinalIgnoreCase(value2);
		}
		ReadOnlySpan<byte> span3 = MemoryMarshal.CreateReadOnlySpan(ref Unsafe.As<TChar, byte>(ref MemoryMarshal.GetReference(span)), span.Length);
		ReadOnlySpan<byte> value3 = MemoryMarshal.CreateReadOnlySpan(ref Unsafe.As<TChar, byte>(ref MemoryMarshal.GetReference(value)), value.Length);
		return span3.EqualsOrdinalIgnoreCaseUtf8(value3);
	}

	internal static bool TryParseFloat<TChar, TFloat>(ReadOnlySpan<TChar> value, NumberStyles styles, NumberFormatInfo info, out TFloat result) where TChar : unmanaged, IUtfChar<TChar> where TFloat : unmanaged, IBinaryFloatParseAndFormatInfo<TFloat>
	{
		Span<byte> digits = stackalloc byte[TFloat.NumberBufferLength];
		NumberBuffer number = new NumberBuffer(NumberBufferKind.FloatingPoint, digits);
		if (!TryStringToNumber(value, styles, ref number, info))
		{
			ReadOnlySpan<TChar> span = SpanTrim(value);
			ReadOnlySpan<TChar> value2 = info.PositiveInfinitySymbolTChar<TChar>();
			if (SpanEqualsOrdinalIgnoreCase(span, value2))
			{
				result = TFloat.PositiveInfinity;
				return true;
			}
			if (SpanEqualsOrdinalIgnoreCase(span, info.NegativeInfinitySymbolTChar<TChar>()))
			{
				result = TFloat.NegativeInfinity;
				return true;
			}
			ReadOnlySpan<TChar> value3 = info.NaNSymbolTChar<TChar>();
			if (SpanEqualsOrdinalIgnoreCase(span, value3))
			{
				result = TFloat.NaN;
				return true;
			}
			ReadOnlySpan<TChar> value4 = info.PositiveSignTChar<TChar>();
			if (SpanStartsWith(span, value4, StringComparison.OrdinalIgnoreCase))
			{
				span = span.Slice(value4.Length);
				if (SpanEqualsOrdinalIgnoreCase(span, value2))
				{
					result = TFloat.PositiveInfinity;
					return true;
				}
				if (SpanEqualsOrdinalIgnoreCase(span, value3))
				{
					result = TFloat.NaN;
					return true;
				}
				result = TFloat.Zero;
				return false;
			}
			ReadOnlySpan<TChar> value5 = info.NegativeSignTChar<TChar>();
			if (SpanStartsWith(span, value5, StringComparison.OrdinalIgnoreCase))
			{
				if (SpanEqualsOrdinalIgnoreCase(span.Slice(value5.Length), value3))
				{
					result = TFloat.NaN;
					return true;
				}
				if (info.AllowHyphenDuringParsing && SpanStartsWith(span, TChar.CastFrom('-')) && SpanEqualsOrdinalIgnoreCase(span.Slice(1), value3))
				{
					result = TFloat.NaN;
					return true;
				}
			}
			result = TFloat.Zero;
			return false;
		}
		result = NumberToFloat<TFloat>(ref number);
		return true;
	}

	internal unsafe static bool TryStringToNumber<TChar>(ReadOnlySpan<TChar> value, NumberStyles styles, ref NumberBuffer number, NumberFormatInfo info) where TChar : unmanaged, IUtfChar<TChar>
	{
		fixed (TChar* ptr = &MemoryMarshal.GetReference(value))
		{
			TChar* str = ptr;
			if (!TryParseNumber(ref str, str + value.Length, styles, ref number, info) || ((int)(str - ptr) < value.Length && !TrailingZeros(value, (int)(str - ptr))))
			{
				return false;
			}
		}
		return true;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private static bool TrailingZeros<TChar>(ReadOnlySpan<TChar> value, int index) where TChar : unmanaged, IUtfChar<TChar>
	{
		return !value.Slice(index).ContainsAnyExcept(TChar.CastFrom('\0'));
	}

	private static bool IsSpaceReplacingChar(uint c)
	{
		if (c != 160)
		{
			return c == 8239;
		}
		return true;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private unsafe static TChar* MatchNegativeSignChars<TChar>(TChar* p, TChar* pEnd, NumberFormatInfo info) where TChar : unmanaged, IUtfChar<TChar>
	{
		TChar* ptr = MatchChars(p, pEnd, info.NegativeSignTChar<TChar>());
		if (ptr == null && info.AllowHyphenDuringParsing && p < pEnd && TChar.CastToUInt32(*p) == 45)
		{
			ptr = p + 1;
		}
		return ptr;
	}

	private unsafe static TChar* MatchChars<TChar>(TChar* p, TChar* pEnd, ReadOnlySpan<TChar> value) where TChar : unmanaged, IUtfChar<TChar>
	{
		fixed (TChar* ptr = &MemoryMarshal.GetReference(value))
		{
			TChar* ptr2 = ptr;
			if (TChar.CastToUInt32(*ptr2) != 0)
			{
				while (true)
				{
					uint num = ((p < pEnd) ? TChar.CastToUInt32(*p) : 0u);
					uint num2 = TChar.CastToUInt32(*ptr2);
					if (num != num2 && (!IsSpaceReplacingChar(num2) || num != 32))
					{
						break;
					}
					p++;
					ptr2++;
					if (TChar.CastToUInt32(*ptr2) == 0)
					{
						return p;
					}
				}
			}
		}
		return null;
	}

	private static bool IsWhite(uint ch)
	{
		if (ch != 32)
		{
			return ch - 9 <= 4;
		}
		return true;
	}

	private static bool IsDigit(uint ch)
	{
		return ch - 48 <= 9;
	}

	[DoesNotReturn]
	internal static void ThrowOverflowOrFormatException<TChar, TInteger>(ParsingStatus status, ReadOnlySpan<TChar> value) where TChar : unmanaged, IUtfChar<TChar> where TInteger : unmanaged, IBinaryIntegerParseAndFormatInfo<TInteger>
	{
		if (status == ParsingStatus.Failed)
		{
			ThrowFormatException(value);
		}
		ThrowOverflowException<TInteger>();
	}

	[DoesNotReturn]
	internal static void ThrowFormatException<TChar>(ReadOnlySpan<TChar> value) where TChar : unmanaged, IUtfChar<TChar>
	{
		throw new FormatException(SR.Format(SR.Format_InvalidStringWithValue, value.ToString()));
	}

	[DoesNotReturn]
	internal static void ThrowOverflowException<TInteger>() where TInteger : unmanaged, IBinaryIntegerParseAndFormatInfo<TInteger>
	{
		throw new OverflowException(TInteger.OverflowMessage);
	}

	[DoesNotReturn]
	internal static void ThrowOverflowException(string message)
	{
		throw new OverflowException(message);
	}

	internal static TFloat NumberToFloat<TFloat>(ref NumberBuffer number) where TFloat : unmanaged, IBinaryFloatParseAndFormatInfo<TFloat>
	{
		TFloat val;
		if (number.DigitsCount == 0 || number.Scale < TFloat.MinDecimalExponent)
		{
			val = TFloat.Zero;
		}
		else if (number.Scale > TFloat.MaxDecimalExponent)
		{
			val = TFloat.PositiveInfinity;
		}
		else
		{
			ulong bits = NumberToFloatingPointBits<TFloat>(ref number);
			val = TFloat.BitsToFloat(bits);
		}
		if (!number.IsNegative)
		{
			return val;
		}
		return -val;
	}
}
