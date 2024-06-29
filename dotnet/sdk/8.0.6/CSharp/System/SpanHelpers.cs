using System.Buffers.Binary;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace System;

internal static class SpanHelpers
{
	internal readonly struct ComparerComparable<T, TComparer> : IComparable<T> where TComparer : IComparer<T>
	{
		private readonly T _value;

		private readonly TComparer _comparer;

		public ComparerComparable(T value, TComparer comparer)
		{
			_value = value;
			_comparer = comparer;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public int CompareTo(T other)
		{
			return _comparer.Compare(_value, other);
		}
	}

	internal interface INegator<T> where T : struct
	{
		static abstract bool NegateIfNeeded(bool equals);

		static abstract Vector128<T> NegateIfNeeded(Vector128<T> equals);

		static abstract Vector256<T> NegateIfNeeded(Vector256<T> equals);

		static abstract Vector512<T> NegateIfNeeded(Vector512<T> equals);

		static abstract bool HasMatch(Vector512<T> left, Vector512<T> right);

		static abstract Vector512<T> GetMatchMask(Vector512<T> left, Vector512<T> right);
	}

	[StructLayout(LayoutKind.Sequential, Size = 1)]
	internal readonly struct DontNegate<T> : INegator<T> where T : struct
	{
		public static bool NegateIfNeeded(bool equals)
		{
			return equals;
		}

		public static Vector128<T> NegateIfNeeded(Vector128<T> equals)
		{
			return equals;
		}

		public static Vector256<T> NegateIfNeeded(Vector256<T> equals)
		{
			return equals;
		}

		public static Vector512<T> NegateIfNeeded(Vector512<T> equals)
		{
			return equals;
		}

		public static bool HasMatch(Vector512<T> left, Vector512<T> right)
		{
			return Vector512.EqualsAny(left, right);
		}

		public static Vector512<T> GetMatchMask(Vector512<T> left, Vector512<T> right)
		{
			return Vector512.Equals(left, right);
		}
	}

	[StructLayout(LayoutKind.Sequential, Size = 1)]
	internal readonly struct Negate<T> : INegator<T> where T : struct
	{
		public static bool NegateIfNeeded(bool equals)
		{
			return !equals;
		}

		public static Vector128<T> NegateIfNeeded(Vector128<T> equals)
		{
			return ~equals;
		}

		public static Vector256<T> NegateIfNeeded(Vector256<T> equals)
		{
			return ~equals;
		}

		public static Vector512<T> NegateIfNeeded(Vector512<T> equals)
		{
			return ~equals;
		}

		public static bool HasMatch(Vector512<T> left, Vector512<T> right)
		{
			return !Vector512.EqualsAll(left, right);
		}

		public static Vector512<T> GetMatchMask(Vector512<T> left, Vector512<T> right)
		{
			return ~Vector512.Equals(left, right);
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static int BinarySearch<T, TComparable>(this ReadOnlySpan<T> span, TComparable comparable) where TComparable : IComparable<T>
	{
		if (comparable == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.comparable);
		}
		return BinarySearch(ref MemoryMarshal.GetReference(span), span.Length, comparable);
	}

	public static int BinarySearch<T, TComparable>(ref T spanStart, int length, TComparable comparable) where TComparable : IComparable<T>
	{
		int num = 0;
		int num2 = length - 1;
		while (num <= num2)
		{
			int num3 = num2 + num >>> 1;
			ref TComparable reference = ref comparable;
			TComparable val = default(TComparable);
			if (val == null)
			{
				val = reference;
				reference = ref val;
			}
			int num4 = reference.CompareTo(Unsafe.Add(ref spanStart, num3));
			if (num4 == 0)
			{
				return num3;
			}
			if (num4 > 0)
			{
				num = num3 + 1;
			}
			else
			{
				num2 = num3 - 1;
			}
		}
		return ~num;
	}

	public static int IndexOf(ref byte searchSpace, int searchSpaceLength, ref byte value, int valueLength)
	{
		if (valueLength == 0)
		{
			return 0;
		}
		int num = valueLength - 1;
		if (num == 0)
		{
			return IndexOfValueType(ref searchSpace, value, searchSpaceLength);
		}
		nint num2 = 0;
		byte value2 = value;
		int num3 = searchSpaceLength - num;
		if (!Vector128.IsHardwareAccelerated || num3 < Vector128<byte>.Count)
		{
			ref byte second = ref Unsafe.Add(ref value, 1);
			int num4 = num3;
			while (num4 > 0)
			{
				int num5 = IndexOfValueType(ref Unsafe.Add(ref searchSpace, num2), value2, num4);
				if (num5 < 0)
				{
					break;
				}
				num4 -= num5;
				num2 += num5;
				if (num4 <= 0)
				{
					break;
				}
				if (SequenceEqual(ref Unsafe.Add(ref searchSpace, num2 + 1), ref second, (uint)num))
				{
					return (int)num2;
				}
				num4--;
				num2++;
			}
			return -1;
		}
		if (Vector512.IsHardwareAccelerated && num3 - Vector512<byte>.Count >= 0)
		{
			byte b = Unsafe.Add(ref value, num);
			nint num6 = (nint)(uint)num;
			while (b == value && num6 > 1)
			{
				b = Unsafe.Add(ref value, --num6);
			}
			Vector512<byte> left = Vector512.Create(value);
			Vector512<byte> left2 = Vector512.Create(b);
			nint num7 = (nint)num3 - (nint)Vector512<byte>.Count;
			while (true)
			{
				Vector512<byte> vector = Vector512.Equals(left2, Vector512.LoadUnsafe(ref searchSpace, (nuint)(num2 + num6)));
				Vector512<byte> vector2 = Vector512.Equals(left, Vector512.LoadUnsafe(ref searchSpace, (nuint)num2));
				Vector512<byte> vector3 = (vector2 & vector).AsByte();
				if (vector3 != Vector512<byte>.Zero)
				{
					ulong num8 = vector3.ExtractMostSignificantBits();
					do
					{
						int num9 = BitOperations.TrailingZeroCount(num8);
						if (valueLength == 2 || SequenceEqual(ref Unsafe.Add(ref searchSpace, num2 + num9), ref value, (uint)valueLength))
						{
							return (int)(num2 + num9);
						}
						num8 = BitOperations.ResetLowestSetBit(num8);
					}
					while (num8 != 0L);
				}
				num2 += Vector512<byte>.Count;
				if (num2 == num3)
				{
					break;
				}
				if (num2 > num7)
				{
					num2 = num7;
				}
			}
			return -1;
		}
		if (Vector256.IsHardwareAccelerated && num3 - Vector256<byte>.Count >= 0)
		{
			byte b2 = Unsafe.Add(ref value, num);
			nint num10 = num;
			while (b2 == value && num10 > 1)
			{
				b2 = Unsafe.Add(ref value, --num10);
			}
			Vector256<byte> left3 = Vector256.Create(value);
			Vector256<byte> left4 = Vector256.Create(b2);
			nint num11 = (nint)num3 - (nint)Vector256<byte>.Count;
			while (true)
			{
				Vector256<byte> vector4 = Vector256.Equals(left4, Vector256.LoadUnsafe(ref searchSpace, (nuint)(num2 + num10)));
				Vector256<byte> vector5 = Vector256.Equals(left3, Vector256.LoadUnsafe(ref searchSpace, (nuint)num2));
				Vector256<byte> vector6 = (vector5 & vector4).AsByte();
				if (vector6 != Vector256<byte>.Zero)
				{
					uint num12 = vector6.ExtractMostSignificantBits();
					do
					{
						int num13 = BitOperations.TrailingZeroCount(num12);
						if (valueLength == 2 || SequenceEqual(ref Unsafe.Add(ref searchSpace, num2 + num13), ref value, (uint)valueLength))
						{
							return (int)(num2 + num13);
						}
						num12 = BitOperations.ResetLowestSetBit(num12);
					}
					while (num12 != 0);
				}
				num2 += Vector256<byte>.Count;
				if (num2 == num3)
				{
					break;
				}
				if (num2 > num11)
				{
					num2 = num11;
				}
			}
			return -1;
		}
		byte b3 = Unsafe.Add(ref value, num);
		int num14 = num;
		while (b3 == value && num14 > 1)
		{
			b3 = Unsafe.Add(ref value, --num14);
		}
		Vector128<byte> left5 = Vector128.Create(value);
		Vector128<byte> left6 = Vector128.Create(b3);
		nint num15 = (nint)num3 - (nint)Vector128<byte>.Count;
		while (true)
		{
			Vector128<byte> vector7 = Vector128.Equals(left6, Vector128.LoadUnsafe(ref searchSpace, (nuint)(num2 + num14)));
			Vector128<byte> vector8 = Vector128.Equals(left5, Vector128.LoadUnsafe(ref searchSpace, (nuint)num2));
			Vector128<byte> vector9 = (vector8 & vector7).AsByte();
			if (vector9 != Vector128<byte>.Zero)
			{
				uint num16 = vector9.ExtractMostSignificantBits();
				do
				{
					int num17 = BitOperations.TrailingZeroCount(num16);
					if (valueLength == 2 || SequenceEqual(ref Unsafe.Add(ref searchSpace, num2 + num17), ref value, (uint)valueLength))
					{
						return (int)(num2 + num17);
					}
					num16 = BitOperations.ResetLowestSetBit(num16);
				}
				while (num16 != 0);
			}
			num2 += Vector128<byte>.Count;
			if (num2 == num3)
			{
				break;
			}
			if (num2 > num15)
			{
				num2 = num15;
			}
		}
		return -1;
	}

	public static int LastIndexOf(ref byte searchSpace, int searchSpaceLength, ref byte value, int valueLength)
	{
		if (valueLength == 0)
		{
			return searchSpaceLength;
		}
		int num = valueLength - 1;
		if (num == 0)
		{
			return LastIndexOfValueType(ref searchSpace, value, searchSpaceLength);
		}
		int num2 = 0;
		byte value2 = value;
		int num3 = searchSpaceLength - num;
		if (!Vector128.IsHardwareAccelerated || num3 < Vector128<byte>.Count)
		{
			ref byte second = ref Unsafe.Add(ref value, 1);
			while (true)
			{
				int num4 = searchSpaceLength - num2 - num;
				if (num4 <= 0)
				{
					break;
				}
				int num5 = LastIndexOfValueType(ref searchSpace, value2, num4);
				if (num5 < 0)
				{
					break;
				}
				if (SequenceEqual(ref Unsafe.Add(ref searchSpace, num5 + 1), ref second, (uint)num))
				{
					return num5;
				}
				num2 += num4 - num5;
			}
			return -1;
		}
		if (Vector512.IsHardwareAccelerated && num3 >= Vector512<byte>.Count)
		{
			num2 = num3 - Vector512<byte>.Count;
			byte b = Unsafe.Add(ref value, num);
			int num6 = num;
			while (b == value && num6 > 1)
			{
				b = Unsafe.Add(ref value, --num6);
			}
			Vector512<byte> left = Vector512.Create(value);
			Vector512<byte> left2 = Vector512.Create(b);
			while (true)
			{
				Vector512<byte> vector = Vector512.Equals(left, Vector512.LoadUnsafe(ref searchSpace, (nuint)num2));
				Vector512<byte> vector2 = Vector512.Equals(left2, Vector512.LoadUnsafe(ref searchSpace, (nuint)(num2 + num6)));
				Vector512<byte> vector3 = (vector & vector2).AsByte();
				if (vector3 != Vector512<byte>.Zero)
				{
					ulong num7 = vector3.ExtractMostSignificantBits();
					do
					{
						int num8 = 63 - BitOperations.LeadingZeroCount(num7);
						if (valueLength == 2 || SequenceEqual(ref Unsafe.Add(ref searchSpace, num2 + num8), ref value, (uint)valueLength))
						{
							return num8 + num2;
						}
						num7 = BitOperations.FlipBit(num7, num8);
					}
					while (num7 != 0L);
				}
				num2 -= Vector512<byte>.Count;
				if (num2 == -Vector512<byte>.Count)
				{
					break;
				}
				if (num2 < 0)
				{
					num2 = 0;
				}
			}
			return -1;
		}
		if (Vector256.IsHardwareAccelerated && num3 >= Vector256<byte>.Count)
		{
			num2 = num3 - Vector256<byte>.Count;
			byte b2 = Unsafe.Add(ref value, num);
			int num9 = num;
			while (b2 == value && num9 > 1)
			{
				b2 = Unsafe.Add(ref value, --num9);
			}
			Vector256<byte> left3 = Vector256.Create(value);
			Vector256<byte> left4 = Vector256.Create(b2);
			while (true)
			{
				Vector256<byte> vector4 = Vector256.Equals(left3, Vector256.LoadUnsafe(ref searchSpace, (nuint)num2));
				Vector256<byte> vector5 = Vector256.Equals(left4, Vector256.LoadUnsafe(ref searchSpace, (nuint)(num2 + num9)));
				Vector256<byte> vector6 = (vector4 & vector5).AsByte();
				if (vector6 != Vector256<byte>.Zero)
				{
					uint num10 = vector6.ExtractMostSignificantBits();
					do
					{
						int num11 = 31 - BitOperations.LeadingZeroCount(num10);
						if (valueLength == 2 || SequenceEqual(ref Unsafe.Add(ref searchSpace, num2 + num11), ref value, (uint)valueLength))
						{
							return num11 + num2;
						}
						num10 = BitOperations.FlipBit(num10, num11);
					}
					while (num10 != 0);
				}
				num2 -= Vector256<byte>.Count;
				if (num2 == -Vector256<byte>.Count)
				{
					break;
				}
				if (num2 < 0)
				{
					num2 = 0;
				}
			}
			return -1;
		}
		num2 = num3 - Vector128<byte>.Count;
		byte b3 = Unsafe.Add(ref value, num);
		int num12 = num;
		while (b3 == value && num12 > 1)
		{
			b3 = Unsafe.Add(ref value, --num12);
		}
		Vector128<byte> left5 = Vector128.Create(value);
		Vector128<byte> left6 = Vector128.Create(b3);
		while (true)
		{
			Vector128<byte> vector7 = Vector128.Equals(left5, Vector128.LoadUnsafe(ref searchSpace, (nuint)num2));
			Vector128<byte> vector8 = Vector128.Equals(left6, Vector128.LoadUnsafe(ref searchSpace, (nuint)(num2 + num12)));
			Vector128<byte> vector9 = (vector7 & vector8).AsByte();
			if (vector9 != Vector128<byte>.Zero)
			{
				uint num13 = vector9.ExtractMostSignificantBits();
				do
				{
					int num14 = 31 - BitOperations.LeadingZeroCount(num13);
					if (valueLength == 2 || SequenceEqual(ref Unsafe.Add(ref searchSpace, num2 + num14), ref value, (uint)valueLength))
					{
						return num14 + num2;
					}
					num13 = BitOperations.FlipBit(num13, num14);
				}
				while (num13 != 0);
			}
			num2 -= Vector128<byte>.Count;
			if (num2 == -Vector128<byte>.Count)
			{
				break;
			}
			if (num2 < 0)
			{
				num2 = 0;
			}
		}
		return -1;
	}

	[DoesNotReturn]
	private static void ThrowMustBeNullTerminatedString()
	{
		throw new ArgumentException(SR.Arg_MustBeNullTerminatedString);
	}

	internal unsafe static int IndexOfNullByte(byte* searchSpace)
	{
		nuint num = 0u;
		nuint num2 = 2147483647u;
		if (Vector128.IsHardwareAccelerated)
		{
			num2 = UnalignedCountVector128(searchSpace);
		}
		while (true)
		{
			if (num2 >= 8)
			{
				num2 -= 8;
				if (searchSpace[num] != 0)
				{
					if (searchSpace[num + 1] == 0)
					{
						goto IL_0435;
					}
					if (searchSpace[num + 2] == 0)
					{
						goto IL_043b;
					}
					if (searchSpace[num + 3] != 0)
					{
						if (searchSpace[num + 4] != 0)
						{
							if (searchSpace[num + 5] != 0)
							{
								if (searchSpace[num + 6] != 0)
								{
									if (searchSpace[num + 7] == 0)
									{
										break;
									}
									num += 8;
									continue;
								}
								return (int)(num + 6);
							}
							return (int)(num + 5);
						}
						return (int)(num + 4);
					}
					goto IL_0441;
				}
			}
			else
			{
				if (num2 >= 4)
				{
					num2 -= 4;
					if (searchSpace[num] == 0)
					{
						goto IL_0432;
					}
					if (searchSpace[num + 1] == 0)
					{
						goto IL_0435;
					}
					if (searchSpace[num + 2] == 0)
					{
						goto IL_043b;
					}
					if (searchSpace[num + 3] == 0)
					{
						goto IL_0441;
					}
					num += 4;
				}
				while (num2 != 0)
				{
					num2--;
					if (searchSpace[num] != 0)
					{
						num++;
						continue;
					}
					goto IL_0432;
				}
				if (Vector512.IsHardwareAccelerated)
				{
					if (num < int.MaxValue)
					{
						if ((((uint)searchSpace + num) & (nuint)(Vector256<byte>.Count - 1)) != 0)
						{
							Vector128<byte> right = Vector128.Load(searchSpace + num);
							uint num3 = Vector128.Equals(Vector128<byte>.Zero, right).ExtractMostSignificantBits();
							if (num3 != 0)
							{
								return (int)(num + (uint)BitOperations.TrailingZeroCount(num3));
							}
							num += (nuint)Vector128<byte>.Count;
						}
						if ((((uint)searchSpace + num) & (nuint)(Vector512<byte>.Count - 1)) != 0)
						{
							Vector256<byte> right2 = Vector256.Load(searchSpace + num);
							uint num4 = Vector256.Equals(Vector256<byte>.Zero, right2).ExtractMostSignificantBits();
							if (num4 != 0)
							{
								return (int)(num + (uint)BitOperations.TrailingZeroCount(num4));
							}
							num += (nuint)Vector256<byte>.Count;
						}
						num2 = GetByteVector512SpanLength(num, int.MaxValue);
						if (num2 > num)
						{
							do
							{
								Vector512<byte> right3 = Vector512.Load(searchSpace + num);
								ulong num5 = Vector512.Equals(Vector512<byte>.Zero, right3).ExtractMostSignificantBits();
								if (num5 == 0L)
								{
									num += (nuint)Vector512<byte>.Count;
									continue;
								}
								return (int)(num + (uint)BitOperations.TrailingZeroCount(num5));
							}
							while (num2 > num);
						}
						num2 = GetByteVector256SpanLength(num, int.MaxValue);
						if (num2 > num)
						{
							Vector256<byte> right4 = Vector256.Load(searchSpace + num);
							uint num6 = Vector256.Equals(Vector256<byte>.Zero, right4).ExtractMostSignificantBits();
							if (num6 != 0)
							{
								return (int)(num + (uint)BitOperations.TrailingZeroCount(num6));
							}
							num += (nuint)Vector256<byte>.Count;
						}
						num2 = GetByteVector128SpanLength(num, int.MaxValue);
						if (num2 > num)
						{
							Vector128<byte> right5 = Vector128.Load(searchSpace + num);
							uint num7 = Vector128.Equals(Vector128<byte>.Zero, right5).ExtractMostSignificantBits();
							if (num7 != 0)
							{
								return (int)(num + (uint)BitOperations.TrailingZeroCount(num7));
							}
							num += (nuint)Vector128<byte>.Count;
						}
						if (num < int.MaxValue)
						{
							num2 = int.MaxValue - num;
							continue;
						}
					}
				}
				else if (Vector256.IsHardwareAccelerated)
				{
					if (num < int.MaxValue)
					{
						if ((((uint)searchSpace + num) & (nuint)(Vector256<byte>.Count - 1)) != 0)
						{
							Vector128<byte> right6 = Vector128.Load(searchSpace + num);
							uint num8 = Vector128.Equals(Vector128<byte>.Zero, right6).ExtractMostSignificantBits();
							if (num8 != 0)
							{
								return (int)(num + (uint)BitOperations.TrailingZeroCount(num8));
							}
							num += (nuint)Vector128<byte>.Count;
						}
						num2 = GetByteVector256SpanLength(num, int.MaxValue);
						if (num2 > num)
						{
							do
							{
								Vector256<byte> right7 = Vector256.Load(searchSpace + num);
								uint num9 = Vector256.Equals(Vector256<byte>.Zero, right7).ExtractMostSignificantBits();
								if (num9 == 0)
								{
									num += (nuint)Vector256<byte>.Count;
									continue;
								}
								return (int)(num + (uint)BitOperations.TrailingZeroCount(num9));
							}
							while (num2 > num);
						}
						num2 = GetByteVector128SpanLength(num, int.MaxValue);
						if (num2 > num)
						{
							Vector128<byte> right8 = Vector128.Load(searchSpace + num);
							uint num10 = Vector128.Equals(Vector128<byte>.Zero, right8).ExtractMostSignificantBits();
							if (num10 != 0)
							{
								return (int)(num + (uint)BitOperations.TrailingZeroCount(num10));
							}
							num += (nuint)Vector128<byte>.Count;
						}
						if (num < int.MaxValue)
						{
							num2 = int.MaxValue - num;
							continue;
						}
					}
				}
				else if (Vector128.IsHardwareAccelerated && num < int.MaxValue)
				{
					for (num2 = GetByteVector128SpanLength(num, int.MaxValue); num2 > num; num += (nuint)Vector128<byte>.Count)
					{
						Vector128<byte> right9 = Vector128.Load(searchSpace + num);
						Vector128<byte> vector = Vector128.Equals(Vector128<byte>.Zero, right9);
						if (!(vector == Vector128<byte>.Zero))
						{
							uint value = vector.ExtractMostSignificantBits();
							return (int)(num + (uint)BitOperations.TrailingZeroCount(value));
						}
					}
					if (num < int.MaxValue)
					{
						num2 = int.MaxValue - num;
						continue;
					}
				}
				ThrowMustBeNullTerminatedString();
			}
			goto IL_0432;
			IL_0441:
			return (int)(num + 3);
			IL_043b:
			return (int)(num + 2);
			IL_0435:
			return (int)(num + 1);
			IL_0432:
			return (int)num;
		}
		return (int)(num + 7);
	}

	[Intrinsic]
	public static bool SequenceEqual(ref byte first, ref byte second, nuint length)
	{
		nuint num2;
		nuint num4;
		nuint num6;
		nuint num9;
		nuint num11;
		switch ((nint)length)
		{
		case 0:
		case 1:
		case 2:
		case 3:
		{
			uint num7 = 0u;
			nuint num8 = length & 2;
			if (num8 != 0)
			{
				num7 = LoadUShort(ref first);
				num7 -= LoadUShort(ref second);
			}
			if ((length & 1) != 0)
			{
				num7 |= (uint)(Unsafe.AddByteOffset(ref first, num8) - Unsafe.AddByteOffset(ref second, num8));
			}
			return num7 == 0;
		}
		case 4:
		case 5:
		case 6:
		case 7:
		{
			nuint offset2 = length - 4;
			uint num12 = LoadUInt(ref first) - LoadUInt(ref second);
			num12 |= LoadUInt(ref first, offset2) - LoadUInt(ref second, offset2);
			return num12 == 0;
		}
		default:
			{
				if (Unsafe.AreSame(ref first, ref second))
				{
					goto IL_0086;
				}
				if (!Vector128.IsHardwareAccelerated)
				{
					goto IL_01cc;
				}
				if (Vector512.IsHardwareAccelerated && length >= (nuint)Vector512<byte>.Count)
				{
					nuint num = 0u;
					num2 = length - (nuint)Vector512<byte>.Count;
					if (num2 == 0)
					{
						goto IL_00df;
					}
					while (!(Vector512.LoadUnsafe(ref first, num) != Vector512.LoadUnsafe(ref second, num)))
					{
						num += (nuint)Vector512<byte>.Count;
						if (num2 > num)
						{
							continue;
						}
						goto IL_00df;
					}
				}
				else if (Vector256.IsHardwareAccelerated && length >= (nuint)Vector256<byte>.Count)
				{
					nuint num3 = 0u;
					num4 = length - (nuint)Vector256<byte>.Count;
					if (num4 == 0)
					{
						goto IL_0148;
					}
					while (!(Vector256.LoadUnsafe(ref first, num3) != Vector256.LoadUnsafe(ref second, num3)))
					{
						num3 += (nuint)Vector256<byte>.Count;
						if (num4 > num3)
						{
							continue;
						}
						goto IL_0148;
					}
				}
				else
				{
					if (length < (nuint)Vector128<byte>.Count)
					{
						goto IL_01cc;
					}
					nuint num5 = 0u;
					num6 = length - (nuint)Vector128<byte>.Count;
					if (num6 == 0)
					{
						goto IL_01ad;
					}
					while (!(Vector128.LoadUnsafe(ref first, num5) != Vector128.LoadUnsafe(ref second, num5)))
					{
						num5 += (nuint)Vector128<byte>.Count;
						if (num6 > num5)
						{
							continue;
						}
						goto IL_01ad;
					}
				}
				goto IL_025d;
			}
			IL_0148:
			if (Vector256.LoadUnsafe(ref first, num4) == Vector256.LoadUnsafe(ref second, num4))
			{
				goto IL_0086;
			}
			goto IL_025d;
			IL_025d:
			return false;
			IL_0245:
			return LoadNUInt(ref first, num9) == LoadNUInt(ref second, num9);
			IL_01ad:
			if (Vector128.LoadUnsafe(ref first, num6) == Vector128.LoadUnsafe(ref second, num6))
			{
				goto IL_0086;
			}
			goto IL_025d;
			IL_00df:
			if (Vector512.LoadUnsafe(ref first, num2) == Vector512.LoadUnsafe(ref second, num2))
			{
				goto IL_0086;
			}
			goto IL_025d;
			IL_01cc:
			if (Vector128.IsHardwareAccelerated)
			{
				nuint offset = length - 8;
				nuint num10 = LoadNUInt(ref first) - LoadNUInt(ref second);
				num10 |= LoadNUInt(ref first, offset) - LoadNUInt(ref second, offset);
				return num10 == 0;
			}
			num11 = 0u;
			num9 = length - 8;
			if (num9 == 0)
			{
				goto IL_0245;
			}
			while (LoadNUInt(ref first, num11) == LoadNUInt(ref second, num11))
			{
				num11 += 8;
				if (num9 > num11)
				{
					continue;
				}
				goto IL_0245;
			}
			goto IL_025d;
			IL_0086:
			return true;
		}
	}

	public static int SequenceCompareTo(ref byte first, int firstLength, ref byte second, int secondLength)
	{
		nuint num;
		uint num6;
		nuint num3;
		nuint num2;
		if (!Unsafe.AreSame(ref first, ref second))
		{
			num = (uint)(((uint)firstLength < (uint)secondLength) ? firstLength : secondLength);
			num2 = 0u;
			num3 = num;
			if (Vector256.IsHardwareAccelerated)
			{
				if (Vector512.IsHardwareAccelerated && num3 >= (nuint)Vector512<byte>.Count)
				{
					num3 -= (nuint)Vector512<byte>.Count;
					while (true)
					{
						ulong num4;
						if (num3 > num2)
						{
							num4 = Vector512.Equals(Vector512.LoadUnsafe(ref first, num2), Vector512.LoadUnsafe(ref second, num2)).ExtractMostSignificantBits();
							if (num4 == ulong.MaxValue)
							{
								num2 += (nuint)Vector512<byte>.Count;
								continue;
							}
						}
						else
						{
							num2 = num3;
							num4 = Vector512.Equals(Vector512.LoadUnsafe(ref first, num2), Vector512.LoadUnsafe(ref second, num2)).ExtractMostSignificantBits();
							if (num4 == ulong.MaxValue)
							{
								break;
							}
						}
						ulong value = ~num4;
						num2 += (uint)BitOperations.TrailingZeroCount(value);
						return Unsafe.AddByteOffset(ref first, num2).CompareTo(Unsafe.AddByteOffset(ref second, num2));
					}
				}
				else if (num3 >= (nuint)Vector256<byte>.Count)
				{
					num3 -= (nuint)Vector256<byte>.Count;
					while (true)
					{
						uint num5;
						if (num3 > num2)
						{
							num5 = Vector256.Equals(Vector256.LoadUnsafe(ref first, num2), Vector256.LoadUnsafe(ref second, num2)).ExtractMostSignificantBits();
							if (num5 == uint.MaxValue)
							{
								num2 += (nuint)Vector256<byte>.Count;
								continue;
							}
						}
						else
						{
							num2 = num3;
							num5 = Vector256.Equals(Vector256.LoadUnsafe(ref first, num2), Vector256.LoadUnsafe(ref second, num2)).ExtractMostSignificantBits();
							if (num5 == uint.MaxValue)
							{
								break;
							}
						}
						uint value2 = ~num5;
						num2 += (uint)BitOperations.TrailingZeroCount(value2);
						return Unsafe.AddByteOffset(ref first, num2).CompareTo(Unsafe.AddByteOffset(ref second, num2));
					}
				}
				else
				{
					if (num3 < (nuint)Vector128<byte>.Count)
					{
						goto IL_022f;
					}
					num3 -= (nuint)Vector128<byte>.Count;
					if (num3 > num2)
					{
						num6 = Vector128.Equals(Vector128.LoadUnsafe(ref first, num2), Vector128.LoadUnsafe(ref second, num2)).ExtractMostSignificantBits();
						if (num6 != 65535)
						{
							goto IL_01b0;
						}
					}
					num2 = num3;
					num6 = Vector128.Equals(Vector128.LoadUnsafe(ref first, num2), Vector128.LoadUnsafe(ref second, num2)).ExtractMostSignificantBits();
					if (num6 != 65535)
					{
						goto IL_01b0;
					}
				}
			}
			else
			{
				if (!Vector128.IsHardwareAccelerated || num3 < (nuint)Vector128<byte>.Count)
				{
					goto IL_022f;
				}
				num3 -= (nuint)Vector128<byte>.Count;
				while (num3 > num2)
				{
					if (Vector128.LoadUnsafe(ref first, num2) == Vector128.LoadUnsafe(ref second, num2))
					{
						num2 += (nuint)Vector128<byte>.Count;
						continue;
					}
					goto IL_0284;
				}
				num2 = num3;
				if (!(Vector128.LoadUnsafe(ref first, num2) == Vector128.LoadUnsafe(ref second, num2)))
				{
					goto IL_0284;
				}
			}
		}
		goto IL_0288;
		IL_0288:
		return firstLength - secondLength;
		IL_022f:
		if (num3 > 8)
		{
			for (num3 -= 8; num3 > num2 && LoadNUInt(ref first, num2) == LoadNUInt(ref second, num2); num2 += 8)
			{
			}
		}
		goto IL_0284;
		IL_01b0:
		uint value3 = ~num6;
		num2 += (uint)BitOperations.TrailingZeroCount(value3);
		return Unsafe.AddByteOffset(ref first, num2).CompareTo(Unsafe.AddByteOffset(ref second, num2));
		IL_0284:
		for (; num > num2; num2++)
		{
			int num7 = Unsafe.AddByteOffset(ref first, num2).CompareTo(Unsafe.AddByteOffset(ref second, num2));
			if (num7 != 0)
			{
				return num7;
			}
		}
		goto IL_0288;
	}

	public static nuint CommonPrefixLength(ref byte first, ref byte second, nuint length)
	{
		nuint num;
		if (!Vector128.IsHardwareAccelerated || length < (nuint)Vector128<byte>.Count)
		{
			num = length % 4;
			if (num != 0)
			{
				if (first != second)
				{
					return 0u;
				}
				if (num > 1)
				{
					if (Unsafe.Add(ref first, 1) != Unsafe.Add(ref second, 1))
					{
						return 1u;
					}
					if (num > 2 && Unsafe.Add(ref first, 2) != Unsafe.Add(ref second, 2))
					{
						return 2u;
					}
				}
			}
			while (true)
			{
				if ((nint)num <= (nint)(length - 4))
				{
					if (Unsafe.Add(ref first, num + 0) == Unsafe.Add(ref second, num + 0))
					{
						if (Unsafe.Add(ref first, num + 1) == Unsafe.Add(ref second, num + 1))
						{
							if (Unsafe.Add(ref first, num + 2) == Unsafe.Add(ref second, num + 2))
							{
								if (Unsafe.Add(ref first, num + 3) != Unsafe.Add(ref second, num + 3))
								{
									break;
								}
								num += 4;
								continue;
							}
							return num + 2;
						}
						return num + 1;
					}
					return num;
				}
				return length;
			}
			return num + 3;
		}
		nuint num2 = length - (nuint)Vector128<byte>.Count;
		num = 0u;
		uint num3;
		while (true)
		{
			Vector128<byte> vector;
			if (num < num2)
			{
				vector = Vector128.Equals(Vector128.LoadUnsafe(ref first, num), Vector128.LoadUnsafe(ref second, num));
				num3 = vector.ExtractMostSignificantBits();
				if (num3 != 65535)
				{
					break;
				}
				num += (nuint)Vector128<byte>.Count;
				continue;
			}
			num = num2;
			vector = Vector128.Equals(Vector128.LoadUnsafe(ref first, num), Vector128.LoadUnsafe(ref second, num));
			num3 = vector.ExtractMostSignificantBits();
			if (num3 != 65535)
			{
				break;
			}
			return length;
		}
		num3 = ~num3;
		return num + uint.TrailingZeroCount(num3);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static ushort LoadUShort(ref byte start)
	{
		return Unsafe.ReadUnaligned<ushort>(ref start);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static uint LoadUInt(ref byte start)
	{
		return Unsafe.ReadUnaligned<uint>(ref start);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static uint LoadUInt(ref byte start, nuint offset)
	{
		return Unsafe.ReadUnaligned<uint>(ref Unsafe.AddByteOffset(ref start, offset));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static nuint LoadNUInt(ref byte start)
	{
		return Unsafe.ReadUnaligned<nuint>(ref start);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static nuint LoadNUInt(ref byte start, nuint offset)
	{
		return Unsafe.ReadUnaligned<nuint>(ref Unsafe.AddByteOffset(ref start, offset));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static nuint GetByteVector128SpanLength(nuint offset, int length)
	{
		return (uint)((length - (int)offset) & ~(Vector128<byte>.Count - 1));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static nuint GetByteVector256SpanLength(nuint offset, int length)
	{
		return (uint)((length - (int)offset) & ~(Vector256<byte>.Count - 1));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static nuint GetByteVector512SpanLength(nuint offset, int length)
	{
		return (uint)((length - (int)offset) & ~(Vector512<byte>.Count - 1));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private unsafe static nuint UnalignedCountVector128(byte* searchSpace)
	{
		nint num = (nint)searchSpace & (nint)(Vector128<byte>.Count - 1);
		return (uint)((Vector128<byte>.Count - num) & (Vector128<byte>.Count - 1));
	}

	public static void Reverse(ref byte buf, nuint length)
	{
		nint num = (nint)length;
		nint num2 = 0;
		if (Vector512.IsHardwareAccelerated && num >= Vector512<byte>.Count * 2)
		{
			nint num3 = num - Vector512<byte>.Count;
			do
			{
				Vector512<byte> vector = Vector512.LoadUnsafe(ref buf, (nuint)num2);
				Vector512<byte> vector2 = Vector512.LoadUnsafe(ref buf, (nuint)num3);
				vector = Vector512.Shuffle(vector, Vector512.Create((byte)63, (byte)62, (byte)61, (byte)60, (byte)59, (byte)58, (byte)57, (byte)56, (byte)55, (byte)54, (byte)53, (byte)52, (byte)51, (byte)50, (byte)49, (byte)48, (byte)47, (byte)46, (byte)45, (byte)44, (byte)43, (byte)42, (byte)41, (byte)40, (byte)39, (byte)38, (byte)37, (byte)36, (byte)35, (byte)34, (byte)33, (byte)32, (byte)31, (byte)30, (byte)29, (byte)28, (byte)27, (byte)26, (byte)25, (byte)24, (byte)23, (byte)22, (byte)21, (byte)20, (byte)19, (byte)18, (byte)17, (byte)16, (byte)15, (byte)14, (byte)13, (byte)12, (byte)11, (byte)10, (byte)9, (byte)8, (byte)7, (byte)6, (byte)5, (byte)4, (byte)3, (byte)2, (byte)1, (byte)0));
				vector2 = Vector512.Shuffle(vector2, Vector512.Create((byte)63, (byte)62, (byte)61, (byte)60, (byte)59, (byte)58, (byte)57, (byte)56, (byte)55, (byte)54, (byte)53, (byte)52, (byte)51, (byte)50, (byte)49, (byte)48, (byte)47, (byte)46, (byte)45, (byte)44, (byte)43, (byte)42, (byte)41, (byte)40, (byte)39, (byte)38, (byte)37, (byte)36, (byte)35, (byte)34, (byte)33, (byte)32, (byte)31, (byte)30, (byte)29, (byte)28, (byte)27, (byte)26, (byte)25, (byte)24, (byte)23, (byte)22, (byte)21, (byte)20, (byte)19, (byte)18, (byte)17, (byte)16, (byte)15, (byte)14, (byte)13, (byte)12, (byte)11, (byte)10, (byte)9, (byte)8, (byte)7, (byte)6, (byte)5, (byte)4, (byte)3, (byte)2, (byte)1, (byte)0));
				vector2.StoreUnsafe(ref buf, (nuint)num2);
				vector.StoreUnsafe(ref buf, (nuint)num3);
				num2 += Vector512<byte>.Count;
				num3 -= Vector512<byte>.Count;
			}
			while (num3 >= num2);
			num = num3 + Vector512<byte>.Count - num2;
		}
		else if (Avx2.IsSupported && num >= (nint)((double)Vector256<byte>.Count * 1.5))
		{
			Vector256<byte> mask = Vector256.Create((byte)15, (byte)14, (byte)13, (byte)12, (byte)11, (byte)10, (byte)9, (byte)8, (byte)7, (byte)6, (byte)5, (byte)4, (byte)3, (byte)2, (byte)1, (byte)0, (byte)15, (byte)14, (byte)13, (byte)12, (byte)11, (byte)10, (byte)9, (byte)8, (byte)7, (byte)6, (byte)5, (byte)4, (byte)3, (byte)2, (byte)1, (byte)0);
			nint num4 = num - Vector256<byte>.Count;
			do
			{
				Vector256<byte> value = Vector256.LoadUnsafe(ref buf, (nuint)num2);
				Vector256<byte> value2 = Vector256.LoadUnsafe(ref buf, (nuint)num4);
				value = Avx2.Shuffle(value, mask);
				value = Avx2.Permute2x128(value, value, 1);
				value2 = Avx2.Shuffle(value2, mask);
				value2 = Avx2.Permute2x128(value2, value2, 1);
				value2.StoreUnsafe(ref buf, (nuint)num2);
				value.StoreUnsafe(ref buf, (nuint)num4);
				num2 += Vector256<byte>.Count;
				num4 -= Vector256<byte>.Count;
			}
			while (num4 >= num2);
			num = num4 + Vector256<byte>.Count - num2;
		}
		else if (Vector128.IsHardwareAccelerated && num >= Vector128<byte>.Count * 2)
		{
			nint num5 = num - Vector128<byte>.Count;
			do
			{
				Vector128<byte> vector3 = Vector128.LoadUnsafe(ref buf, (nuint)num2);
				Vector128<byte> vector4 = Vector128.LoadUnsafe(ref buf, (nuint)num5);
				vector3 = Vector128.Shuffle(vector3, Vector128.Create((byte)15, (byte)14, (byte)13, (byte)12, (byte)11, (byte)10, (byte)9, (byte)8, (byte)7, (byte)6, (byte)5, (byte)4, (byte)3, (byte)2, (byte)1, (byte)0));
				vector4 = Vector128.Shuffle(vector4, Vector128.Create((byte)15, (byte)14, (byte)13, (byte)12, (byte)11, (byte)10, (byte)9, (byte)8, (byte)7, (byte)6, (byte)5, (byte)4, (byte)3, (byte)2, (byte)1, (byte)0));
				vector4.StoreUnsafe(ref buf, (nuint)num2);
				vector3.StoreUnsafe(ref buf, (nuint)num5);
				num2 += Vector128<byte>.Count;
				num5 -= Vector128<byte>.Count;
			}
			while (num5 >= num2);
			num = num5 + Vector128<byte>.Count - num2;
		}
		if (num >= 8)
		{
			nint num6 = (nint)length - num2 - 8;
			do
			{
				long value3 = Unsafe.ReadUnaligned<long>(ref Unsafe.Add(ref buf, num2));
				long value4 = Unsafe.ReadUnaligned<long>(ref Unsafe.Add(ref buf, num6));
				Unsafe.WriteUnaligned(ref Unsafe.Add(ref buf, num2), BinaryPrimitives.ReverseEndianness(value4));
				Unsafe.WriteUnaligned(ref Unsafe.Add(ref buf, num6), BinaryPrimitives.ReverseEndianness(value3));
				num2 += 8;
				num6 -= 8;
			}
			while (num6 >= num2);
			num = num6 + 8 - num2;
		}
		if (num >= 4)
		{
			nint num7 = (nint)length - num2 - 4;
			do
			{
				int value5 = Unsafe.ReadUnaligned<int>(ref Unsafe.Add(ref buf, num2));
				int value6 = Unsafe.ReadUnaligned<int>(ref Unsafe.Add(ref buf, num7));
				Unsafe.WriteUnaligned(ref Unsafe.Add(ref buf, num2), BinaryPrimitives.ReverseEndianness(value6));
				Unsafe.WriteUnaligned(ref Unsafe.Add(ref buf, num7), BinaryPrimitives.ReverseEndianness(value5));
				num2 += 4;
				num7 -= 4;
			}
			while (num7 >= num2);
			num = num7 + 4 - num2;
		}
		if (num > 1)
		{
			ReverseInner(ref Unsafe.Add(ref buf, num2), (nuint)num);
		}
	}

	public static int IndexOf(ref char searchSpace, int searchSpaceLength, ref char value, int valueLength)
	{
		if (valueLength == 0)
		{
			return 0;
		}
		int num = valueLength - 1;
		if (num == 0)
		{
			return IndexOfChar(ref searchSpace, value, searchSpaceLength);
		}
		nint num2 = 0;
		char c = value;
		int num3 = searchSpaceLength - num;
		if (!Vector128.IsHardwareAccelerated || num3 < Vector128<ushort>.Count)
		{
			ref byte second = ref Unsafe.As<char, byte>(ref Unsafe.Add(ref value, 1));
			int num4 = num3;
			while (num4 > 0)
			{
				int num5 = NonPackedIndexOfChar(ref Unsafe.Add(ref searchSpace, num2), c, num4);
				if (num5 < 0)
				{
					break;
				}
				num4 -= num5;
				num2 += num5;
				if (num4 <= 0)
				{
					break;
				}
				if (SequenceEqual(ref Unsafe.As<char, byte>(ref Unsafe.Add(ref searchSpace, num2 + 1)), ref second, (nuint)(uint)num * (nuint)2u))
				{
					return (int)num2;
				}
				num4--;
				num2++;
			}
			return -1;
		}
		if (Vector512.IsHardwareAccelerated && num3 - Vector512<ushort>.Count >= 0)
		{
			ushort num6 = Unsafe.Add(ref value, num);
			nint num7 = (nint)(uint)num;
			while (num6 == c && num7 > 1)
			{
				num6 = Unsafe.Add(ref value, --num7);
			}
			Vector512<ushort> left = Vector512.Create((ushort)c);
			Vector512<ushort> left2 = Vector512.Create(num6);
			nint num8 = (nint)num3 - (nint)Vector512<ushort>.Count;
			while (true)
			{
				Vector512<ushort> vector = Vector512.Equals(left2, Vector512.LoadUnsafe(ref searchSpace, (nuint)(num2 + num7)));
				Vector512<ushort> vector2 = Vector512.Equals(left, Vector512.LoadUnsafe(ref searchSpace, (nuint)num2));
				Vector512<byte> vector3 = (vector2 & vector).AsByte();
				if (vector3 != Vector512<byte>.Zero)
				{
					ulong num9 = vector3.ExtractMostSignificantBits();
					do
					{
						int num10 = BitOperations.TrailingZeroCount(num9);
						nint num11 = (nint)((uint)num10 / 2u);
						if (valueLength == 2 || SequenceEqual(ref Unsafe.As<char, byte>(ref Unsafe.Add(ref searchSpace, num2 + num11)), ref Unsafe.As<char, byte>(ref value), (nuint)(uint)valueLength * (nuint)2u))
						{
							return (int)(num2 + num11);
						}
						num9 = ((!Bmi1.X64.IsSupported) ? (num9 & (ulong)(~(3L << num10))) : Bmi1.X64.ResetLowestSetBit(Bmi1.X64.ResetLowestSetBit(num9)));
					}
					while (num9 != 0L);
				}
				num2 += Vector512<ushort>.Count;
				if (num2 == num3)
				{
					break;
				}
				if (num2 > num8)
				{
					num2 = num8;
				}
			}
			return -1;
		}
		if (Vector256.IsHardwareAccelerated && num3 - Vector256<ushort>.Count >= 0)
		{
			ushort num12 = Unsafe.Add(ref value, num);
			nint num13 = num;
			while (num12 == c && num13 > 1)
			{
				num12 = Unsafe.Add(ref value, --num13);
			}
			Vector256<ushort> left3 = Vector256.Create((ushort)c);
			Vector256<ushort> left4 = Vector256.Create(num12);
			nint num14 = (nint)num3 - (nint)Vector256<ushort>.Count;
			while (true)
			{
				Vector256<ushort> vector4 = Vector256.Equals(left4, Vector256.LoadUnsafe(ref searchSpace, (nuint)(num2 + num13)));
				Vector256<ushort> vector5 = Vector256.Equals(left3, Vector256.LoadUnsafe(ref searchSpace, (nuint)num2));
				Vector256<byte> vector6 = (vector5 & vector4).AsByte();
				if (vector6 != Vector256<byte>.Zero)
				{
					uint num15 = vector6.ExtractMostSignificantBits();
					do
					{
						nint num16 = (nint)(uint.TrailingZeroCount(num15) / 2);
						if (valueLength == 2 || SequenceEqual(ref Unsafe.As<char, byte>(ref Unsafe.Add(ref searchSpace, num2 + num16)), ref Unsafe.As<char, byte>(ref value), (nuint)(uint)valueLength * (nuint)2u))
						{
							return (int)(num2 + num16);
						}
						num15 = BitOperations.ResetLowestSetBit(BitOperations.ResetLowestSetBit(num15));
					}
					while (num15 != 0);
				}
				num2 += Vector256<ushort>.Count;
				if (num2 == num3)
				{
					break;
				}
				if (num2 > num14)
				{
					num2 = num14;
				}
			}
			return -1;
		}
		ushort num17 = Unsafe.Add(ref value, num);
		nint num18 = num;
		while (num17 == c && num18 > 1)
		{
			num17 = Unsafe.Add(ref value, --num18);
		}
		Vector128<ushort> left5 = Vector128.Create((ushort)c);
		Vector128<ushort> left6 = Vector128.Create(num17);
		nint num19 = (nint)num3 - (nint)Vector128<ushort>.Count;
		while (true)
		{
			Vector128<ushort> vector7 = Vector128.Equals(left6, Vector128.LoadUnsafe(ref searchSpace, (nuint)(num2 + num18)));
			Vector128<ushort> vector8 = Vector128.Equals(left5, Vector128.LoadUnsafe(ref searchSpace, (nuint)num2));
			Vector128<byte> vector9 = (vector8 & vector7).AsByte();
			if (vector9 != Vector128<byte>.Zero)
			{
				uint num20 = vector9.ExtractMostSignificantBits();
				do
				{
					nint num21 = (nint)(uint.TrailingZeroCount(num20) / 2);
					if (valueLength == 2 || SequenceEqual(ref Unsafe.As<char, byte>(ref Unsafe.Add(ref searchSpace, num2 + num21)), ref Unsafe.As<char, byte>(ref value), (nuint)(uint)valueLength * (nuint)2u))
					{
						return (int)(num2 + num21);
					}
					num20 = BitOperations.ResetLowestSetBit(BitOperations.ResetLowestSetBit(num20));
				}
				while (num20 != 0);
			}
			num2 += Vector128<ushort>.Count;
			if (num2 == num3)
			{
				break;
			}
			if (num2 > num19)
			{
				num2 = num19;
			}
		}
		return -1;
	}

	public static int LastIndexOf(ref char searchSpace, int searchSpaceLength, ref char value, int valueLength)
	{
		if (valueLength == 0)
		{
			return searchSpaceLength;
		}
		int num = valueLength - 1;
		if (num == 0)
		{
			return LastIndexOfValueType(ref Unsafe.As<char, short>(ref searchSpace), (short)value, searchSpaceLength);
		}
		int num2 = 0;
		char c = value;
		int num3 = searchSpaceLength - num;
		if (!Vector128.IsHardwareAccelerated || num3 < Vector128<ushort>.Count)
		{
			ref byte second = ref Unsafe.As<char, byte>(ref Unsafe.Add(ref value, 1));
			while (true)
			{
				int num4 = searchSpaceLength - num2 - num;
				if (num4 <= 0)
				{
					break;
				}
				int num5 = LastIndexOfValueType(ref Unsafe.As<char, short>(ref searchSpace), (short)c, num4);
				if (num5 == -1)
				{
					break;
				}
				if (SequenceEqual(ref Unsafe.As<char, byte>(ref Unsafe.Add(ref searchSpace, num5 + 1)), ref second, (nuint)(uint)num * (nuint)2u))
				{
					return num5;
				}
				num2 += num4 - num5;
			}
			return -1;
		}
		if (Vector512.IsHardwareAccelerated && num3 >= Vector512<ushort>.Count)
		{
			num2 = num3 - Vector512<ushort>.Count;
			char c2 = Unsafe.Add(ref value, num);
			int num6 = num;
			while (c2 == c && num6 > 1)
			{
				c2 = Unsafe.Add(ref value, --num6);
			}
			Vector512<ushort> left = Vector512.Create((ushort)c);
			Vector512<ushort> left2 = Vector512.Create((ushort)c2);
			while (true)
			{
				Vector512<ushort> vector = Vector512.Equals(left, Vector512.LoadUnsafe(ref searchSpace, (nuint)num2));
				Vector512<ushort> vector2 = Vector512.Equals(left2, Vector512.LoadUnsafe(ref searchSpace, (nuint)(num2 + num6)));
				Vector512<byte> vector3 = (vector & vector2).AsByte();
				if (vector3 != Vector512<byte>.Zero)
				{
					ulong num7 = vector3.ExtractMostSignificantBits();
					do
					{
						int num8 = 62 - BitOperations.LeadingZeroCount(num7);
						int num9 = (int)((uint)num8 / 2u);
						if (valueLength == 2 || SequenceEqual(ref Unsafe.As<char, byte>(ref Unsafe.Add(ref searchSpace, num2 + num9)), ref Unsafe.As<char, byte>(ref value), (nuint)(uint)valueLength * (nuint)2u))
						{
							return num9 + num2;
						}
						num7 &= (ulong)(~(3L << num8));
					}
					while (num7 != 0L);
				}
				num2 -= Vector512<ushort>.Count;
				if (num2 == -Vector512<ushort>.Count)
				{
					break;
				}
				if (num2 < 0)
				{
					num2 = 0;
				}
			}
			return -1;
		}
		if (Vector256.IsHardwareAccelerated && num3 >= Vector256<ushort>.Count)
		{
			num2 = num3 - Vector256<ushort>.Count;
			char c3 = Unsafe.Add(ref value, num);
			int num10 = num;
			while (c3 == c && num10 > 1)
			{
				c3 = Unsafe.Add(ref value, --num10);
			}
			Vector256<ushort> left3 = Vector256.Create((ushort)c);
			Vector256<ushort> left4 = Vector256.Create((ushort)c3);
			while (true)
			{
				Vector256<ushort> vector4 = Vector256.Equals(left3, Vector256.LoadUnsafe(ref searchSpace, (nuint)num2));
				Vector256<ushort> vector5 = Vector256.Equals(left4, Vector256.LoadUnsafe(ref searchSpace, (nuint)(num2 + num10)));
				Vector256<byte> vector6 = (vector4 & vector5).AsByte();
				if (vector6 != Vector256<byte>.Zero)
				{
					uint num11 = vector6.ExtractMostSignificantBits();
					do
					{
						int num12 = 30 - BitOperations.LeadingZeroCount(num11);
						int num13 = (int)((uint)num12 / 2u);
						if (valueLength == 2 || SequenceEqual(ref Unsafe.As<char, byte>(ref Unsafe.Add(ref searchSpace, num2 + num13)), ref Unsafe.As<char, byte>(ref value), (nuint)(uint)valueLength * (nuint)2u))
						{
							return num13 + num2;
						}
						num11 &= (uint)(~(3 << num12));
					}
					while (num11 != 0);
				}
				num2 -= Vector256<ushort>.Count;
				if (num2 == -Vector256<ushort>.Count)
				{
					break;
				}
				if (num2 < 0)
				{
					num2 = 0;
				}
			}
			return -1;
		}
		num2 = num3 - Vector128<ushort>.Count;
		char c4 = Unsafe.Add(ref value, num);
		int num14 = num;
		while (c4 == value && num14 > 1)
		{
			c4 = Unsafe.Add(ref value, --num14);
		}
		Vector128<ushort> left5 = Vector128.Create((ushort)value);
		Vector128<ushort> left6 = Vector128.Create((ushort)c4);
		while (true)
		{
			Vector128<ushort> vector7 = Vector128.Equals(left5, Vector128.LoadUnsafe(ref searchSpace, (nuint)num2));
			Vector128<ushort> vector8 = Vector128.Equals(left6, Vector128.LoadUnsafe(ref searchSpace, (nuint)(num2 + num14)));
			Vector128<byte> vector9 = (vector7 & vector8).AsByte();
			if (vector9 != Vector128<byte>.Zero)
			{
				uint num15 = vector9.ExtractMostSignificantBits();
				do
				{
					int num16 = 30 - BitOperations.LeadingZeroCount(num15);
					int num17 = (int)((uint)num16 / 2u);
					if (valueLength == 2 || SequenceEqual(ref Unsafe.As<char, byte>(ref Unsafe.Add(ref searchSpace, num2 + num17)), ref Unsafe.As<char, byte>(ref value), (nuint)(uint)valueLength * (nuint)2u))
					{
						return num17 + num2;
					}
					num15 &= (uint)(~(3 << num16));
				}
				while (num15 != 0);
			}
			num2 -= Vector128<ushort>.Count;
			if (num2 == -Vector128<ushort>.Count)
			{
				break;
			}
			if (num2 < 0)
			{
				num2 = 0;
			}
		}
		return -1;
	}

	public static int SequenceCompareTo(ref char first, int firstLength, ref char second, int secondLength)
	{
		int result = firstLength - secondLength;
		if (!Unsafe.AreSame(ref first, ref second))
		{
			nuint num = (uint)(((uint)firstLength < (uint)secondLength) ? firstLength : secondLength);
			nuint num2 = 0u;
			if (num >= 8 / 2)
			{
				if (Vector.IsHardwareAccelerated && num >= (nuint)Vector<ushort>.Count)
				{
					nuint num3 = num - (nuint)Vector<ushort>.Count;
					while (!(Unsafe.ReadUnaligned<Vector<ushort>>(ref Unsafe.As<char, byte>(ref Unsafe.Add(ref first, (nint)num2))) != Unsafe.ReadUnaligned<Vector<ushort>>(ref Unsafe.As<char, byte>(ref Unsafe.Add(ref second, (nint)num2)))))
					{
						num2 += (nuint)Vector<ushort>.Count;
						if (num3 < num2)
						{
							break;
						}
					}
				}
				for (; num >= num2 + 8 / 2 && Unsafe.ReadUnaligned<nuint>(ref Unsafe.As<char, byte>(ref Unsafe.Add(ref first, (nint)num2))) == Unsafe.ReadUnaligned<nuint>(ref Unsafe.As<char, byte>(ref Unsafe.Add(ref second, (nint)num2))); num2 += 8 / 2)
				{
				}
			}
			if (num >= num2 + 2 && Unsafe.ReadUnaligned<int>(ref Unsafe.As<char, byte>(ref Unsafe.Add(ref first, (nint)num2))) == Unsafe.ReadUnaligned<int>(ref Unsafe.As<char, byte>(ref Unsafe.Add(ref second, (nint)num2))))
			{
				num2 += 2;
			}
			for (; num2 < num; num2++)
			{
				int num4 = Unsafe.Add(ref first, (nint)num2).CompareTo(Unsafe.Add(ref second, (nint)num2));
				if (num4 != 0)
				{
					return num4;
				}
			}
		}
		return result;
	}

	public unsafe static int IndexOfNullCharacter(char* searchSpace)
	{
		nint num = 0;
		nint num2 = int.MaxValue;
		if (((int)searchSpace & 1) == 0 && Vector128.IsHardwareAccelerated)
		{
			num2 = UnalignedCountVector128(searchSpace);
		}
		while (true)
		{
			if (num2 >= 4)
			{
				if (searchSpace[num] == '\0')
				{
					break;
				}
				if (searchSpace[num + 1] == '\0')
				{
					return (int)(num + 1);
				}
				if (searchSpace[num + 2] == '\0')
				{
					return (int)(num + 2);
				}
				if (searchSpace[num + 3] != 0)
				{
					num += 4;
					num2 -= 4;
					continue;
				}
			}
			else
			{
				while (num2 > 0)
				{
					if (searchSpace[num] == '\0')
					{
						goto end_IL_006b;
					}
					num++;
					num2--;
				}
				if (Vector512.IsHardwareAccelerated)
				{
					if (num < int.MaxValue)
					{
						if (((nuint)(searchSpace + num) & (nuint)(Vector256<byte>.Count - 1)) != 0)
						{
							Vector128<ushort> right = *(Vector128<ushort>*)(searchSpace + (nuint)num);
							uint num3 = Vector128.Equals(Vector128<ushort>.Zero, right).AsByte().ExtractMostSignificantBits();
							if (num3 != 0)
							{
								return (int)(num + (uint)BitOperations.TrailingZeroCount(num3) / 2u);
							}
							num += Vector128<ushort>.Count;
						}
						if (((nuint)(searchSpace + num) & (nuint)(Vector512<byte>.Count - 1)) != 0)
						{
							Vector256<ushort> right2 = *(Vector256<ushort>*)(searchSpace + (nuint)num);
							uint num4 = Vector256.Equals(Vector256<ushort>.Zero, right2).AsByte().ExtractMostSignificantBits();
							if (num4 != 0)
							{
								return (int)(num + (uint)BitOperations.TrailingZeroCount(num4) / 2u);
							}
							num += Vector256<ushort>.Count;
						}
						num2 = GetCharVector512SpanLength(num, int.MaxValue);
						if (num2 > 0)
						{
							do
							{
								Vector512<ushort> left = *(Vector512<ushort>*)(searchSpace + (nuint)num);
								if (!Vector512.EqualsAny(left, Vector512<ushort>.Zero))
								{
									num += Vector512<ushort>.Count;
									num2 -= Vector512<ushort>.Count;
									continue;
								}
								ulong value = Vector512.Equals(left, Vector512<ushort>.Zero).ExtractMostSignificantBits();
								return (int)(num + (uint)BitOperations.TrailingZeroCount(value));
							}
							while (num2 > 0);
						}
						num2 = GetCharVector256SpanLength(num, int.MaxValue);
						if (num2 > 0)
						{
							Vector256<ushort> right3 = *(Vector256<ushort>*)(searchSpace + (nuint)num);
							uint num5 = Vector256.Equals(Vector256<ushort>.Zero, right3).AsByte().ExtractMostSignificantBits();
							if (num5 != 0)
							{
								return (int)(num + (uint)BitOperations.TrailingZeroCount(num5) / 2u);
							}
							num += Vector256<ushort>.Count;
						}
						num2 = GetCharVector128SpanLength(num, int.MaxValue);
						if (num2 > 0)
						{
							Vector128<ushort> right4 = *(Vector128<ushort>*)(searchSpace + (nuint)num);
							uint num6 = Vector128.Equals(Vector128<ushort>.Zero, right4).AsByte().ExtractMostSignificantBits();
							if (num6 != 0)
							{
								return (int)(num + (uint)BitOperations.TrailingZeroCount(num6) / 2u);
							}
							num += Vector128<ushort>.Count;
						}
						if (num < int.MaxValue)
						{
							num2 = int.MaxValue - num;
							continue;
						}
					}
				}
				else if (Vector256.IsHardwareAccelerated)
				{
					if (num < int.MaxValue)
					{
						if (((nuint)(searchSpace + num) & (nuint)(Vector256<byte>.Count - 1)) != 0)
						{
							Vector128<ushort> right5 = *(Vector128<ushort>*)(searchSpace + (nuint)num);
							uint num7 = Vector128.Equals(Vector128<ushort>.Zero, right5).AsByte().ExtractMostSignificantBits();
							if (num7 != 0)
							{
								return (int)(num + (uint)BitOperations.TrailingZeroCount(num7) / 2u);
							}
							num += Vector128<ushort>.Count;
						}
						num2 = GetCharVector256SpanLength(num, int.MaxValue);
						if (num2 > 0)
						{
							do
							{
								Vector256<ushort> right6 = *(Vector256<ushort>*)(searchSpace + (nuint)num);
								uint num8 = Vector256.Equals(Vector256<ushort>.Zero, right6).AsByte().ExtractMostSignificantBits();
								if (num8 == 0)
								{
									num += Vector256<ushort>.Count;
									num2 -= Vector256<ushort>.Count;
									continue;
								}
								return (int)(num + (uint)BitOperations.TrailingZeroCount(num8) / 2u);
							}
							while (num2 > 0);
						}
						num2 = GetCharVector128SpanLength(num, int.MaxValue);
						if (num2 > 0)
						{
							Vector128<ushort> right7 = *(Vector128<ushort>*)(searchSpace + (nuint)num);
							uint num9 = Vector128.Equals(Vector128<ushort>.Zero, right7).AsByte().ExtractMostSignificantBits();
							if (num9 != 0)
							{
								return (int)(num + (uint)BitOperations.TrailingZeroCount(num9) / 2u);
							}
							num += Vector128<ushort>.Count;
						}
						if (num < int.MaxValue)
						{
							num2 = int.MaxValue - num;
							continue;
						}
					}
				}
				else if (Vector128.IsHardwareAccelerated && num < int.MaxValue)
				{
					num2 = GetCharVector128SpanLength(num, int.MaxValue);
					if (num2 > 0)
					{
						do
						{
							Vector128<ushort> right8 = *(Vector128<ushort>*)(searchSpace + (nuint)num);
							Vector128<ushort> vector = Vector128.Equals(Vector128<ushort>.Zero, right8);
							if (vector == Vector128<ushort>.Zero)
							{
								num += Vector128<ushort>.Count;
								num2 -= Vector128<ushort>.Count;
								continue;
							}
							uint value2 = vector.AsByte().ExtractMostSignificantBits();
							return (int)(num + (uint)BitOperations.TrailingZeroCount(value2) / 2u);
						}
						while (num2 > 0);
					}
					if (num < int.MaxValue)
					{
						num2 = int.MaxValue - num;
						continue;
					}
				}
				ThrowMustBeNullTerminatedString();
			}
			return (int)(num + 3);
			continue;
			end_IL_006b:
			break;
		}
		return (int)num;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static nint GetCharVector128SpanLength(nint offset, nint length)
	{
		return (length - offset) & ~(Vector128<ushort>.Count - 1);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static nint GetCharVector256SpanLength(nint offset, nint length)
	{
		return (length - offset) & ~(Vector256<ushort>.Count - 1);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static nint GetCharVector512SpanLength(nint offset, nint length)
	{
		return (length - offset) & ~(Vector512<ushort>.Count - 1);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private unsafe static nint UnalignedCountVector128(char* searchSpace)
	{
		return (nint)(uint)(-(int)searchSpace / 2) & (nint)(Vector128<ushort>.Count - 1);
	}

	public static void Reverse(ref char buf, nuint length)
	{
		nint num = (nint)length;
		nint num2 = 0;
		if (Vector512.IsHardwareAccelerated && num >= Vector512<ushort>.Count * 2)
		{
			nint num3 = num - Vector512<ushort>.Count;
			do
			{
				ref ushort reference = ref Unsafe.As<char, ushort>(ref Unsafe.Add(ref buf, num2));
				ref ushort reference2 = ref Unsafe.As<char, ushort>(ref Unsafe.Add(ref buf, num3));
				Vector512<ushort> vector = Vector512.LoadUnsafe(ref reference);
				Vector512<ushort> vector2 = Vector512.LoadUnsafe(ref reference2);
				vector = Vector512.Shuffle(vector, Vector512.Create((ushort)31, (ushort)30, (ushort)29, (ushort)28, (ushort)27, (ushort)26, (ushort)25, (ushort)24, (ushort)23, (ushort)22, (ushort)21, (ushort)20, (ushort)19, (ushort)18, (ushort)17, (ushort)16, (ushort)15, (ushort)14, (ushort)13, (ushort)12, (ushort)11, (ushort)10, (ushort)9, (ushort)8, (ushort)7, (ushort)6, (ushort)5, (ushort)4, (ushort)3, (ushort)2, (ushort)1, (ushort)0));
				vector2 = Vector512.Shuffle(vector2, Vector512.Create((ushort)31, (ushort)30, (ushort)29, (ushort)28, (ushort)27, (ushort)26, (ushort)25, (ushort)24, (ushort)23, (ushort)22, (ushort)21, (ushort)20, (ushort)19, (ushort)18, (ushort)17, (ushort)16, (ushort)15, (ushort)14, (ushort)13, (ushort)12, (ushort)11, (ushort)10, (ushort)9, (ushort)8, (ushort)7, (ushort)6, (ushort)5, (ushort)4, (ushort)3, (ushort)2, (ushort)1, (ushort)0));
				vector2.StoreUnsafe(ref reference);
				vector.StoreUnsafe(ref reference2);
				num2 += Vector512<ushort>.Count;
				num3 -= Vector512<ushort>.Count;
			}
			while (num3 >= num2);
			num = num3 + Vector512<ushort>.Count - num2;
		}
		else if (Avx2.IsSupported && num >= (nint)((double)Vector256<ushort>.Count * 1.5))
		{
			Vector256<byte> mask = Vector256.Create((byte)14, (byte)15, (byte)12, (byte)13, (byte)10, (byte)11, (byte)8, (byte)9, (byte)6, (byte)7, (byte)4, (byte)5, (byte)2, (byte)3, (byte)0, (byte)1, (byte)14, (byte)15, (byte)12, (byte)13, (byte)10, (byte)11, (byte)8, (byte)9, (byte)6, (byte)7, (byte)4, (byte)5, (byte)2, (byte)3, (byte)0, (byte)1);
			nint num4 = num - Vector256<ushort>.Count;
			do
			{
				ref byte reference3 = ref Unsafe.As<char, byte>(ref Unsafe.Add(ref buf, num2));
				ref byte reference4 = ref Unsafe.As<char, byte>(ref Unsafe.Add(ref buf, num4));
				Vector256<byte> value = Vector256.LoadUnsafe(ref reference3);
				Vector256<byte> value2 = Vector256.LoadUnsafe(ref reference4);
				value = Avx2.Shuffle(value, mask);
				value = Avx2.Permute2x128(value, value, 1);
				value2 = Avx2.Shuffle(value2, mask);
				value2 = Avx2.Permute2x128(value2, value2, 1);
				value2.StoreUnsafe(ref reference3);
				value.StoreUnsafe(ref reference4);
				num2 += Vector256<ushort>.Count;
				num4 -= Vector256<ushort>.Count;
			}
			while (num4 >= num2);
			num = num4 + Vector256<ushort>.Count - num2;
		}
		else if (Vector128.IsHardwareAccelerated && num >= Vector128<ushort>.Count * 2)
		{
			nint num5 = num - Vector128<ushort>.Count;
			do
			{
				ref ushort reference5 = ref Unsafe.As<char, ushort>(ref Unsafe.Add(ref buf, num2));
				ref ushort reference6 = ref Unsafe.As<char, ushort>(ref Unsafe.Add(ref buf, num5));
				Vector128<ushort> vector3 = Vector128.LoadUnsafe(ref reference5);
				Vector128<ushort> vector4 = Vector128.LoadUnsafe(ref reference6);
				vector3 = Vector128.Shuffle(vector3, Vector128.Create((ushort)7, (ushort)6, (ushort)5, (ushort)4, (ushort)3, (ushort)2, (ushort)1, (ushort)0));
				vector4 = Vector128.Shuffle(vector4, Vector128.Create((ushort)7, (ushort)6, (ushort)5, (ushort)4, (ushort)3, (ushort)2, (ushort)1, (ushort)0));
				vector4.StoreUnsafe(ref reference5);
				vector3.StoreUnsafe(ref reference6);
				num2 += Vector128<ushort>.Count;
				num5 -= Vector128<ushort>.Count;
			}
			while (num5 >= num2);
			num = num5 + Vector128<ushort>.Count - num2;
		}
		if (num > 1)
		{
			ReverseInner(ref Unsafe.Add(ref buf, num2), (nuint)num);
		}
	}

	public static void ClearWithoutReferences(ref byte b, nuint byteLength)
	{
		if (byteLength != 0)
		{
			if (byteLength <= 768)
			{
				Unsafe.InitBlockUnaligned(ref b, 0, (uint)byteLength);
			}
			else
			{
				Buffer._ZeroMemory(ref b, byteLength);
			}
		}
	}

	public static void ClearWithReferences(ref nint ip, nuint pointerSizeLength)
	{
		while (pointerSizeLength >= 8)
		{
			Unsafe.Add(ref Unsafe.Add(ref ip, (nint)pointerSizeLength), -1) = 0;
			Unsafe.Add(ref Unsafe.Add(ref ip, (nint)pointerSizeLength), -2) = 0;
			Unsafe.Add(ref Unsafe.Add(ref ip, (nint)pointerSizeLength), -3) = 0;
			Unsafe.Add(ref Unsafe.Add(ref ip, (nint)pointerSizeLength), -4) = 0;
			Unsafe.Add(ref Unsafe.Add(ref ip, (nint)pointerSizeLength), -5) = 0;
			Unsafe.Add(ref Unsafe.Add(ref ip, (nint)pointerSizeLength), -6) = 0;
			Unsafe.Add(ref Unsafe.Add(ref ip, (nint)pointerSizeLength), -7) = 0;
			Unsafe.Add(ref Unsafe.Add(ref ip, (nint)pointerSizeLength), -8) = 0;
			pointerSizeLength -= 8;
		}
		if (pointerSizeLength < 4)
		{
			if (pointerSizeLength < 2)
			{
				if (pointerSizeLength == 0)
				{
					return;
				}
				goto IL_00fa;
			}
		}
		else
		{
			Unsafe.Add(ref ip, 2) = 0;
			Unsafe.Add(ref ip, 3) = 0;
			Unsafe.Add(ref Unsafe.Add(ref ip, (nint)pointerSizeLength), -3) = 0;
			Unsafe.Add(ref Unsafe.Add(ref ip, (nint)pointerSizeLength), -2) = 0;
		}
		Unsafe.Add(ref ip, 1) = 0;
		Unsafe.Add(ref Unsafe.Add(ref ip, (nint)pointerSizeLength), -1) = 0;
		goto IL_00fa;
		IL_00fa:
		ip = 0;
	}

	public static void Reverse(ref int buf, nuint length)
	{
		nint num = (nint)length;
		nint num2 = 0;
		if (Vector512.IsHardwareAccelerated && num >= Vector512<int>.Count * 2)
		{
			nint num3 = num - Vector512<int>.Count;
			do
			{
				Vector512<int> vector = Vector512.LoadUnsafe(ref buf, (nuint)num2);
				Vector512<int> vector2 = Vector512.LoadUnsafe(ref buf, (nuint)num3);
				vector = Vector512.Shuffle(vector, Vector512.Create(15, 14, 13, 12, 11, 10, 9, 8, 7, 6, 5, 4, 3, 2, 1, 0));
				vector2 = Vector512.Shuffle(vector2, Vector512.Create(15, 14, 13, 12, 11, 10, 9, 8, 7, 6, 5, 4, 3, 2, 1, 0));
				vector2.StoreUnsafe(ref buf, (nuint)num2);
				vector.StoreUnsafe(ref buf, (nuint)num3);
				num2 += Vector512<int>.Count;
				num3 -= Vector512<int>.Count;
			}
			while (num3 >= num2);
			num = num3 + Vector512<int>.Count - num2;
		}
		else if (Avx2.IsSupported && num >= Vector256<int>.Count * 2)
		{
			nint num4 = num - Vector256<int>.Count;
			do
			{
				Vector256<int> left = Vector256.LoadUnsafe(ref buf, (nuint)num2);
				Vector256<int> left2 = Vector256.LoadUnsafe(ref buf, (nuint)num4);
				left = Avx2.PermuteVar8x32(left, Vector256.Create(7, 6, 5, 4, 3, 2, 1, 0));
				left2 = Avx2.PermuteVar8x32(left2, Vector256.Create(7, 6, 5, 4, 3, 2, 1, 0));
				left2.StoreUnsafe(ref buf, (nuint)num2);
				left.StoreUnsafe(ref buf, (nuint)num4);
				num2 += Vector256<int>.Count;
				num4 -= Vector256<int>.Count;
			}
			while (num4 >= num2);
			num = num4 + Vector256<int>.Count - num2;
		}
		else if (Vector128.IsHardwareAccelerated && num >= Vector128<int>.Count * 2)
		{
			nint num5 = num - Vector128<int>.Count;
			do
			{
				Vector128<int> vector3 = Vector128.LoadUnsafe(ref buf, (nuint)num2);
				Vector128<int> vector4 = Vector128.LoadUnsafe(ref buf, (nuint)num5);
				vector3 = Vector128.Shuffle(vector3, Vector128.Create(3, 2, 1, 0));
				vector4 = Vector128.Shuffle(vector4, Vector128.Create(3, 2, 1, 0));
				vector4.StoreUnsafe(ref buf, (nuint)num2);
				vector3.StoreUnsafe(ref buf, (nuint)num5);
				num2 += Vector128<int>.Count;
				num5 -= Vector128<int>.Count;
			}
			while (num5 >= num2);
			num = num5 + Vector128<int>.Count - num2;
		}
		if (num > 1)
		{
			ReverseInner(ref Unsafe.Add(ref buf, num2), (nuint)num);
		}
	}

	public static void Reverse(ref long buf, nuint length)
	{
		nint num = (nint)length;
		nint num2 = 0;
		if (Vector512.IsHardwareAccelerated && num >= Vector512<long>.Count * 2)
		{
			nint num3 = num - Vector512<long>.Count;
			do
			{
				Vector512<long> vector = Vector512.LoadUnsafe(ref buf, (nuint)num2);
				Vector512<long> vector2 = Vector512.LoadUnsafe(ref buf, (nuint)num3);
				vector = Vector512.Shuffle(vector, Vector512.Create(7L, 6L, 5L, 4L, 3L, 2L, 1L, 0L));
				vector2 = Vector512.Shuffle(vector2, Vector512.Create(7L, 6L, 5L, 4L, 3L, 2L, 1L, 0L));
				vector2.StoreUnsafe(ref buf, (nuint)num2);
				vector.StoreUnsafe(ref buf, (nuint)num3);
				num2 += Vector512<long>.Count;
				num3 -= Vector512<long>.Count;
			}
			while (num3 >= num2);
			num = num3 + Vector512<long>.Count - num2;
		}
		else if (Avx2.IsSupported && num >= Vector256<long>.Count * 2)
		{
			nint num4 = num - Vector256<long>.Count;
			do
			{
				Vector256<long> value = Vector256.LoadUnsafe(ref buf, (nuint)num2);
				Vector256<long> value2 = Vector256.LoadUnsafe(ref buf, (nuint)num4);
				value = Avx2.Permute4x64(value, 27);
				value2 = Avx2.Permute4x64(value2, 27);
				value2.StoreUnsafe(ref buf, (nuint)num2);
				value.StoreUnsafe(ref buf, (nuint)num4);
				num2 += Vector256<long>.Count;
				num4 -= Vector256<long>.Count;
			}
			while (num4 >= num2);
			num = num4 + Vector256<long>.Count - num2;
		}
		else if (Vector128.IsHardwareAccelerated && num >= Vector128<long>.Count * 2)
		{
			nint num5 = num - Vector128<long>.Count;
			do
			{
				Vector128<long> vector3 = Vector128.LoadUnsafe(ref buf, (nuint)num2);
				Vector128<long> vector4 = Vector128.LoadUnsafe(ref buf, (nuint)num5);
				vector3 = Vector128.Shuffle(vector3, Vector128.Create(1L, 0L));
				vector4 = Vector128.Shuffle(vector4, Vector128.Create(1L, 0L));
				vector4.StoreUnsafe(ref buf, (nuint)num2);
				vector3.StoreUnsafe(ref buf, (nuint)num5);
				num2 += Vector128<long>.Count;
				num5 -= Vector128<long>.Count;
			}
			while (num5 >= num2);
			num = num5 + Vector128<long>.Count - num2;
		}
		if (num > 1)
		{
			ReverseInner(ref Unsafe.Add(ref buf, num2), (nuint)num);
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void Reverse<T>(ref T elements, nuint length)
	{
		if (!RuntimeHelpers.IsReferenceOrContainsReferences<T>())
		{
			if (Unsafe.SizeOf<T>() == 1)
			{
				Reverse(ref Unsafe.As<T, byte>(ref elements), length);
				return;
			}
			if (Unsafe.SizeOf<T>() == 2)
			{
				Reverse(ref Unsafe.As<T, char>(ref elements), length);
				return;
			}
			if (Unsafe.SizeOf<T>() == 4)
			{
				Reverse(ref Unsafe.As<T, int>(ref elements), length);
				return;
			}
			if (Unsafe.SizeOf<T>() == 8)
			{
				Reverse(ref Unsafe.As<T, long>(ref elements), length);
				return;
			}
		}
		ReverseInner(ref elements, length);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static void ReverseInner<T>(ref T elements, nuint length)
	{
		ref T reference = ref elements;
		ref T reference2 = ref Unsafe.Subtract(ref Unsafe.Add(ref reference, length), 1);
		do
		{
			T val = reference;
			reference = reference2;
			reference2 = val;
			reference = ref Unsafe.Add(ref reference, 1);
			reference2 = ref Unsafe.Subtract(ref reference2, 1);
		}
		while (Unsafe.IsAddressLessThan(ref reference, ref reference2));
	}

	public static void Fill<T>(ref T refData, nuint numElements, T value)
	{
		if (!RuntimeHelpers.IsReferenceOrContainsReferences<T>() && Vector.IsHardwareAccelerated && Unsafe.SizeOf<T>() <= Vector<byte>.Count && BitOperations.IsPow2(Unsafe.SizeOf<T>()) && numElements >= (uint)(Vector<byte>.Count / Unsafe.SizeOf<T>()))
		{
			T source = value;
			Vector<byte> value2;
			if (Unsafe.SizeOf<T>() == 1)
			{
				value2 = new Vector<byte>(Unsafe.As<T, byte>(ref source));
			}
			else if (Unsafe.SizeOf<T>() == 2)
			{
				value2 = (Vector<byte>)new Vector<ushort>(Unsafe.As<T, ushort>(ref source));
			}
			else if (Unsafe.SizeOf<T>() == 4)
			{
				value2 = ((typeof(T) == typeof(float)) ? ((Vector<byte>)new Vector<float>((float)(object)source)) : ((Vector<byte>)new Vector<uint>(Unsafe.As<T, uint>(ref source))));
			}
			else if (Unsafe.SizeOf<T>() == 8)
			{
				value2 = ((typeof(T) == typeof(double)) ? ((Vector<byte>)new Vector<double>((double)(object)source)) : ((Vector<byte>)new Vector<ulong>(Unsafe.As<T, ulong>(ref source))));
			}
			else if (Unsafe.SizeOf<T>() == 16)
			{
				Vector128<byte> vector = Unsafe.As<T, Vector128<byte>>(ref source);
				if (Vector<byte>.Count == 16)
				{
					value2 = vector.AsVector();
				}
				else
				{
					if (Vector<byte>.Count != 32)
					{
						goto IL_0238;
					}
					value2 = Vector256.Create(vector, vector).AsVector();
				}
			}
			else
			{
				if (Unsafe.SizeOf<T>() != 32 || Vector<byte>.Count != 32)
				{
					goto IL_0238;
				}
				value2 = Unsafe.As<T, Vector256<byte>>(ref source).AsVector();
			}
			ref byte source2 = ref Unsafe.As<T, byte>(ref refData);
			nuint num = numElements * (nuint)Unsafe.SizeOf<T>();
			nuint num2 = num & (nuint)(2 * -Vector<byte>.Count);
			nuint num3 = 0u;
			if (numElements >= (uint)(2 * Vector<byte>.Count / Unsafe.SizeOf<T>()))
			{
				do
				{
					Unsafe.WriteUnaligned(ref Unsafe.AddByteOffset(ref source2, num3), value2);
					Unsafe.WriteUnaligned(ref Unsafe.AddByteOffset(ref source2, num3 + (nuint)Vector<byte>.Count), value2);
					num3 += (uint)(2 * Vector<byte>.Count);
				}
				while (num3 < num2);
			}
			if ((num & (nuint)Vector<byte>.Count) != 0)
			{
				Unsafe.WriteUnaligned(ref Unsafe.AddByteOffset(ref source2, num3), value2);
			}
			Unsafe.WriteUnaligned(ref Unsafe.AddByteOffset(ref source2, num - (nuint)Vector<byte>.Count), value2);
			return;
		}
		goto IL_0238;
		IL_0238:
		nuint num4 = 0u;
		if (numElements >= 8)
		{
			nuint num5 = numElements & ~(nuint)7u;
			do
			{
				Unsafe.Add(ref refData, (nint)(num4 + 0)) = value;
				Unsafe.Add(ref refData, (nint)(num4 + 1)) = value;
				Unsafe.Add(ref refData, (nint)(num4 + 2)) = value;
				Unsafe.Add(ref refData, (nint)(num4 + 3)) = value;
				Unsafe.Add(ref refData, (nint)(num4 + 4)) = value;
				Unsafe.Add(ref refData, (nint)(num4 + 5)) = value;
				Unsafe.Add(ref refData, (nint)(num4 + 6)) = value;
				Unsafe.Add(ref refData, (nint)(num4 + 7)) = value;
			}
			while ((num4 += 8) < num5);
		}
		if ((numElements & 4) != 0)
		{
			Unsafe.Add(ref refData, (nint)(num4 + 0)) = value;
			Unsafe.Add(ref refData, (nint)(num4 + 1)) = value;
			Unsafe.Add(ref refData, (nint)(num4 + 2)) = value;
			Unsafe.Add(ref refData, (nint)(num4 + 3)) = value;
			num4 += 4;
		}
		if ((numElements & 2) != 0)
		{
			Unsafe.Add(ref refData, (nint)(num4 + 0)) = value;
			Unsafe.Add(ref refData, (nint)(num4 + 1)) = value;
			num4 += 2;
		}
		if ((numElements & 1) != 0)
		{
			Unsafe.Add(ref refData, (nint)num4) = value;
		}
	}

	public static int IndexOf<T>(ref T searchSpace, int searchSpaceLength, ref T value, int valueLength) where T : IEquatable<T>
	{
		if (valueLength == 0)
		{
			return 0;
		}
		T value2 = value;
		ref T second = ref Unsafe.Add(ref value, 1);
		int num = valueLength - 1;
		int num2 = 0;
		while (true)
		{
			int num3 = searchSpaceLength - num2 - num;
			if (num3 <= 0)
			{
				break;
			}
			int num4 = IndexOf(ref Unsafe.Add(ref searchSpace, num2), value2, num3);
			if (num4 < 0)
			{
				break;
			}
			num2 += num4;
			if (SequenceEqual(ref Unsafe.Add(ref searchSpace, num2 + 1), ref second, num))
			{
				return num2;
			}
			num2++;
		}
		return -1;
	}

	public static bool Contains<T>(ref T searchSpace, T value, int length) where T : IEquatable<T>
	{
		nint num = 0;
		T val = default(T);
		if (val != null || value != null)
		{
			while (length >= 8)
			{
				length -= 8;
				ref T reference = ref value;
				val = default(T);
				if (val == null)
				{
					val = reference;
					reference = ref val;
				}
				if (!reference.Equals(Unsafe.Add(ref searchSpace, num + 0)))
				{
					ref T reference2 = ref value;
					val = default(T);
					if (val == null)
					{
						val = reference2;
						reference2 = ref val;
					}
					if (!reference2.Equals(Unsafe.Add(ref searchSpace, num + 1)))
					{
						ref T reference3 = ref value;
						val = default(T);
						if (val == null)
						{
							val = reference3;
							reference3 = ref val;
						}
						if (!reference3.Equals(Unsafe.Add(ref searchSpace, num + 2)))
						{
							ref T reference4 = ref value;
							val = default(T);
							if (val == null)
							{
								val = reference4;
								reference4 = ref val;
							}
							if (!reference4.Equals(Unsafe.Add(ref searchSpace, num + 3)))
							{
								ref T reference5 = ref value;
								val = default(T);
								if (val == null)
								{
									val = reference5;
									reference5 = ref val;
								}
								if (!reference5.Equals(Unsafe.Add(ref searchSpace, num + 4)))
								{
									ref T reference6 = ref value;
									val = default(T);
									if (val == null)
									{
										val = reference6;
										reference6 = ref val;
									}
									if (!reference6.Equals(Unsafe.Add(ref searchSpace, num + 5)))
									{
										ref T reference7 = ref value;
										val = default(T);
										if (val == null)
										{
											val = reference7;
											reference7 = ref val;
										}
										if (!reference7.Equals(Unsafe.Add(ref searchSpace, num + 6)))
										{
											ref T reference8 = ref value;
											val = default(T);
											if (val == null)
											{
												val = reference8;
												reference8 = ref val;
											}
											if (!reference8.Equals(Unsafe.Add(ref searchSpace, num + 7)))
											{
												num += 8;
												continue;
											}
										}
									}
								}
							}
						}
					}
				}
				goto IL_035b;
			}
			if (length < 4)
			{
				goto IL_0330;
			}
			length -= 4;
			ref T reference9 = ref value;
			val = default(T);
			if (val == null)
			{
				val = reference9;
				reference9 = ref val;
			}
			if (!reference9.Equals(Unsafe.Add(ref searchSpace, num + 0)))
			{
				ref T reference10 = ref value;
				val = default(T);
				if (val == null)
				{
					val = reference10;
					reference10 = ref val;
				}
				if (!reference10.Equals(Unsafe.Add(ref searchSpace, num + 1)))
				{
					ref T reference11 = ref value;
					val = default(T);
					if (val == null)
					{
						val = reference11;
						reference11 = ref val;
					}
					if (!reference11.Equals(Unsafe.Add(ref searchSpace, num + 2)))
					{
						ref T reference12 = ref value;
						val = default(T);
						if (val == null)
						{
							val = reference12;
							reference12 = ref val;
						}
						if (!reference12.Equals(Unsafe.Add(ref searchSpace, num + 3)))
						{
							num += 4;
							goto IL_0330;
						}
					}
				}
			}
			goto IL_035b;
		}
		nint num2 = length;
		num = 0;
		while (num < num2)
		{
			if (Unsafe.Add(ref searchSpace, num) != null)
			{
				num++;
				continue;
			}
			goto IL_035b;
		}
		goto IL_0359;
		IL_0330:
		while (length > 0)
		{
			length--;
			ref T reference13 = ref value;
			val = default(T);
			if (val == null)
			{
				val = reference13;
				reference13 = ref val;
			}
			if (!reference13.Equals(Unsafe.Add(ref searchSpace, num)))
			{
				num++;
				continue;
			}
			goto IL_035b;
		}
		goto IL_0359;
		IL_0359:
		return false;
		IL_035b:
		return true;
	}

	public static int IndexOf<T>(ref T searchSpace, T value, int length) where T : IEquatable<T>
	{
		nint num = 0;
		T val = default(T);
		if (val != null || value != null)
		{
			while (length >= 8)
			{
				length -= 8;
				ref T reference = ref value;
				val = default(T);
				if (val == null)
				{
					val = reference;
					reference = ref val;
				}
				if (reference.Equals(Unsafe.Add(ref searchSpace, num)))
				{
					goto IL_0355;
				}
				ref T reference2 = ref value;
				val = default(T);
				if (val == null)
				{
					val = reference2;
					reference2 = ref val;
				}
				if (reference2.Equals(Unsafe.Add(ref searchSpace, num + 1)))
				{
					goto IL_0358;
				}
				ref T reference3 = ref value;
				val = default(T);
				if (val == null)
				{
					val = reference3;
					reference3 = ref val;
				}
				if (reference3.Equals(Unsafe.Add(ref searchSpace, num + 2)))
				{
					goto IL_035e;
				}
				ref T reference4 = ref value;
				val = default(T);
				if (val == null)
				{
					val = reference4;
					reference4 = ref val;
				}
				if (!reference4.Equals(Unsafe.Add(ref searchSpace, num + 3)))
				{
					ref T reference5 = ref value;
					val = default(T);
					if (val == null)
					{
						val = reference5;
						reference5 = ref val;
					}
					if (!reference5.Equals(Unsafe.Add(ref searchSpace, num + 4)))
					{
						ref T reference6 = ref value;
						val = default(T);
						if (val == null)
						{
							val = reference6;
							reference6 = ref val;
						}
						if (!reference6.Equals(Unsafe.Add(ref searchSpace, num + 5)))
						{
							ref T reference7 = ref value;
							val = default(T);
							if (val == null)
							{
								val = reference7;
								reference7 = ref val;
							}
							if (!reference7.Equals(Unsafe.Add(ref searchSpace, num + 6)))
							{
								ref T reference8 = ref value;
								val = default(T);
								if (val == null)
								{
									val = reference8;
									reference8 = ref val;
								}
								if (!reference8.Equals(Unsafe.Add(ref searchSpace, num + 7)))
								{
									num += 8;
									continue;
								}
								return (int)(num + 7);
							}
							return (int)(num + 6);
						}
						return (int)(num + 5);
					}
					return (int)(num + 4);
				}
				goto IL_0364;
			}
			if (length >= 4)
			{
				length -= 4;
				ref T reference9 = ref value;
				val = default(T);
				if (val == null)
				{
					val = reference9;
					reference9 = ref val;
				}
				if (reference9.Equals(Unsafe.Add(ref searchSpace, num)))
				{
					goto IL_0355;
				}
				ref T reference10 = ref value;
				val = default(T);
				if (val == null)
				{
					val = reference10;
					reference10 = ref val;
				}
				if (reference10.Equals(Unsafe.Add(ref searchSpace, num + 1)))
				{
					goto IL_0358;
				}
				ref T reference11 = ref value;
				val = default(T);
				if (val == null)
				{
					val = reference11;
					reference11 = ref val;
				}
				if (reference11.Equals(Unsafe.Add(ref searchSpace, num + 2)))
				{
					goto IL_035e;
				}
				ref T reference12 = ref value;
				val = default(T);
				if (val == null)
				{
					val = reference12;
					reference12 = ref val;
				}
				if (reference12.Equals(Unsafe.Add(ref searchSpace, num + 3)))
				{
					goto IL_0364;
				}
				num += 4;
			}
			while (length > 0)
			{
				ref T reference13 = ref value;
				val = default(T);
				if (val == null)
				{
					val = reference13;
					reference13 = ref val;
				}
				if (!reference13.Equals(Unsafe.Add(ref searchSpace, num)))
				{
					num++;
					length--;
					continue;
				}
				goto IL_0355;
			}
		}
		else
		{
			nint num2 = length;
			num = 0;
			while (num < num2)
			{
				if (Unsafe.Add(ref searchSpace, num) != null)
				{
					num++;
					continue;
				}
				goto IL_0355;
			}
		}
		return -1;
		IL_0355:
		return (int)num;
		IL_0358:
		return (int)(num + 1);
		IL_0364:
		return (int)(num + 3);
		IL_035e:
		return (int)(num + 2);
	}

	public static int IndexOfAny<T>(ref T searchSpace, T value0, T value1, int length) where T : IEquatable<T>
	{
		int i = 0;
		if (default(T) != null || (value0 != null && value1 != null))
		{
			while (length - i >= 8)
			{
				T other = Unsafe.Add(ref searchSpace, i);
				if (value0.Equals(other) || value1.Equals(other))
				{
					goto IL_0350;
				}
				other = Unsafe.Add(ref searchSpace, i + 1);
				if (value0.Equals(other) || value1.Equals(other))
				{
					goto IL_0352;
				}
				other = Unsafe.Add(ref searchSpace, i + 2);
				if (value0.Equals(other) || value1.Equals(other))
				{
					goto IL_0356;
				}
				other = Unsafe.Add(ref searchSpace, i + 3);
				if (!value0.Equals(other) && !value1.Equals(other))
				{
					other = Unsafe.Add(ref searchSpace, i + 4);
					if (!value0.Equals(other) && !value1.Equals(other))
					{
						other = Unsafe.Add(ref searchSpace, i + 5);
						if (!value0.Equals(other) && !value1.Equals(other))
						{
							other = Unsafe.Add(ref searchSpace, i + 6);
							if (!value0.Equals(other) && !value1.Equals(other))
							{
								other = Unsafe.Add(ref searchSpace, i + 7);
								if (!value0.Equals(other) && !value1.Equals(other))
								{
									i += 8;
									continue;
								}
								return i + 7;
							}
							return i + 6;
						}
						return i + 5;
					}
					return i + 4;
				}
				goto IL_035a;
			}
			if (length - i >= 4)
			{
				T other = Unsafe.Add(ref searchSpace, i);
				if (value0.Equals(other) || value1.Equals(other))
				{
					goto IL_0350;
				}
				other = Unsafe.Add(ref searchSpace, i + 1);
				if (value0.Equals(other) || value1.Equals(other))
				{
					goto IL_0352;
				}
				other = Unsafe.Add(ref searchSpace, i + 2);
				if (value0.Equals(other) || value1.Equals(other))
				{
					goto IL_0356;
				}
				other = Unsafe.Add(ref searchSpace, i + 3);
				if (value0.Equals(other) || value1.Equals(other))
				{
					goto IL_035a;
				}
				i += 4;
			}
			while (i < length)
			{
				T other = Unsafe.Add(ref searchSpace, i);
				if (!value0.Equals(other) && !value1.Equals(other))
				{
					i++;
					continue;
				}
				goto IL_0350;
			}
		}
		else
		{
			for (i = 0; i < length; i++)
			{
				T other = Unsafe.Add(ref searchSpace, i);
				if (other == null)
				{
					if (value0 != null && value1 != null)
					{
						continue;
					}
				}
				else if (!other.Equals(value0) && !other.Equals(value1))
				{
					continue;
				}
				goto IL_0350;
			}
		}
		return -1;
		IL_0352:
		return i + 1;
		IL_0350:
		return i;
		IL_0356:
		return i + 2;
		IL_035a:
		return i + 3;
	}

	public static int IndexOfAny<T>(ref T searchSpace, T value0, T value1, T value2, int length) where T : IEquatable<T>
	{
		int i = 0;
		if (default(T) != null || (value0 != null && value1 != null && value2 != null))
		{
			while (length - i >= 8)
			{
				T other = Unsafe.Add(ref searchSpace, i);
				if (value0.Equals(other) || value1.Equals(other) || value2.Equals(other))
				{
					goto IL_0471;
				}
				other = Unsafe.Add(ref searchSpace, i + 1);
				if (value0.Equals(other) || value1.Equals(other) || value2.Equals(other))
				{
					goto IL_0473;
				}
				other = Unsafe.Add(ref searchSpace, i + 2);
				if (value0.Equals(other) || value1.Equals(other) || value2.Equals(other))
				{
					goto IL_0477;
				}
				other = Unsafe.Add(ref searchSpace, i + 3);
				if (!value0.Equals(other) && !value1.Equals(other) && !value2.Equals(other))
				{
					other = Unsafe.Add(ref searchSpace, i + 4);
					if (!value0.Equals(other) && !value1.Equals(other) && !value2.Equals(other))
					{
						other = Unsafe.Add(ref searchSpace, i + 5);
						if (!value0.Equals(other) && !value1.Equals(other) && !value2.Equals(other))
						{
							other = Unsafe.Add(ref searchSpace, i + 6);
							if (!value0.Equals(other) && !value1.Equals(other) && !value2.Equals(other))
							{
								other = Unsafe.Add(ref searchSpace, i + 7);
								if (!value0.Equals(other) && !value1.Equals(other) && !value2.Equals(other))
								{
									i += 8;
									continue;
								}
								return i + 7;
							}
							return i + 6;
						}
						return i + 5;
					}
					return i + 4;
				}
				goto IL_047b;
			}
			if (length - i >= 4)
			{
				T other = Unsafe.Add(ref searchSpace, i);
				if (value0.Equals(other) || value1.Equals(other) || value2.Equals(other))
				{
					goto IL_0471;
				}
				other = Unsafe.Add(ref searchSpace, i + 1);
				if (value0.Equals(other) || value1.Equals(other) || value2.Equals(other))
				{
					goto IL_0473;
				}
				other = Unsafe.Add(ref searchSpace, i + 2);
				if (value0.Equals(other) || value1.Equals(other) || value2.Equals(other))
				{
					goto IL_0477;
				}
				other = Unsafe.Add(ref searchSpace, i + 3);
				if (value0.Equals(other) || value1.Equals(other) || value2.Equals(other))
				{
					goto IL_047b;
				}
				i += 4;
			}
			while (i < length)
			{
				T other = Unsafe.Add(ref searchSpace, i);
				if (!value0.Equals(other) && !value1.Equals(other) && !value2.Equals(other))
				{
					i++;
					continue;
				}
				goto IL_0471;
			}
		}
		else
		{
			for (i = 0; i < length; i++)
			{
				T other = Unsafe.Add(ref searchSpace, i);
				if (other == null)
				{
					if (value0 != null && value1 != null && value2 != null)
					{
						continue;
					}
				}
				else if (!other.Equals(value0) && !other.Equals(value1) && !other.Equals(value2))
				{
					continue;
				}
				goto IL_0471;
			}
		}
		return -1;
		IL_0473:
		return i + 1;
		IL_047b:
		return i + 3;
		IL_0471:
		return i;
		IL_0477:
		return i + 2;
	}

	public static int IndexOfAny<T>(ref T searchSpace, int searchSpaceLength, ref T value, int valueLength) where T : IEquatable<T>
	{
		if (valueLength == 0)
		{
			return -1;
		}
		if (typeof(T).IsValueType)
		{
			for (int i = 0; i < searchSpaceLength; i++)
			{
				T other = Unsafe.Add(ref searchSpace, i);
				for (int j = 0; j < valueLength; j++)
				{
					if (Unsafe.Add(ref value, j).Equals(other))
					{
						return i;
					}
				}
			}
		}
		else
		{
			for (int k = 0; k < searchSpaceLength; k++)
			{
				T val = Unsafe.Add(ref searchSpace, k);
				if (val != null)
				{
					for (int l = 0; l < valueLength; l++)
					{
						ref T reference = ref val;
						T val2 = default(T);
						if (val2 == null)
						{
							val2 = reference;
							reference = ref val2;
						}
						if (reference.Equals(Unsafe.Add(ref value, l)))
						{
							return k;
						}
					}
					continue;
				}
				for (int m = 0; m < valueLength; m++)
				{
					if (Unsafe.Add(ref value, m) == null)
					{
						return k;
					}
				}
			}
		}
		return -1;
	}

	public static int LastIndexOf<T>(ref T searchSpace, int searchSpaceLength, ref T value, int valueLength) where T : IEquatable<T>
	{
		if (valueLength == 0)
		{
			return searchSpaceLength;
		}
		int num = valueLength - 1;
		if (num == 0)
		{
			return LastIndexOf(ref searchSpace, value, searchSpaceLength);
		}
		int num2 = 0;
		T value2 = value;
		ref T second = ref Unsafe.Add(ref value, 1);
		while (true)
		{
			int num3 = searchSpaceLength - num2 - num;
			if (num3 <= 0)
			{
				break;
			}
			int num4 = LastIndexOf(ref searchSpace, value2, num3);
			if (num4 < 0)
			{
				break;
			}
			if (SequenceEqual(ref Unsafe.Add(ref searchSpace, num4 + 1), ref second, num))
			{
				return num4;
			}
			num2 += num3 - num4;
		}
		return -1;
	}

	public static int LastIndexOf<T>(ref T searchSpace, T value, int length) where T : IEquatable<T>
	{
		T val = default(T);
		if (val != null || value != null)
		{
			while (length >= 8)
			{
				length -= 8;
				ref T reference = ref value;
				val = default(T);
				if (val == null)
				{
					val = reference;
					reference = ref val;
				}
				if (!reference.Equals(Unsafe.Add(ref searchSpace, length + 7)))
				{
					ref T reference2 = ref value;
					val = default(T);
					if (val == null)
					{
						val = reference2;
						reference2 = ref val;
					}
					if (!reference2.Equals(Unsafe.Add(ref searchSpace, length + 6)))
					{
						ref T reference3 = ref value;
						val = default(T);
						if (val == null)
						{
							val = reference3;
							reference3 = ref val;
						}
						if (!reference3.Equals(Unsafe.Add(ref searchSpace, length + 5)))
						{
							ref T reference4 = ref value;
							val = default(T);
							if (val == null)
							{
								val = reference4;
								reference4 = ref val;
							}
							if (!reference4.Equals(Unsafe.Add(ref searchSpace, length + 4)))
							{
								ref T reference5 = ref value;
								val = default(T);
								if (val == null)
								{
									val = reference5;
									reference5 = ref val;
								}
								if (!reference5.Equals(Unsafe.Add(ref searchSpace, length + 3)))
								{
									ref T reference6 = ref value;
									val = default(T);
									if (val == null)
									{
										val = reference6;
										reference6 = ref val;
									}
									if (!reference6.Equals(Unsafe.Add(ref searchSpace, length + 2)))
									{
										ref T reference7 = ref value;
										val = default(T);
										if (val == null)
										{
											val = reference7;
											reference7 = ref val;
										}
										if (!reference7.Equals(Unsafe.Add(ref searchSpace, length + 1)))
										{
											ref T reference8 = ref value;
											val = default(T);
											if (val == null)
											{
												val = reference8;
												reference8 = ref val;
											}
											if (!reference8.Equals(Unsafe.Add(ref searchSpace, length)))
											{
												continue;
											}
											goto IL_0339;
										}
										goto IL_033b;
									}
									goto IL_033f;
								}
								goto IL_0343;
							}
							return length + 4;
						}
						return length + 5;
					}
					return length + 6;
				}
				return length + 7;
			}
			if (length >= 4)
			{
				length -= 4;
				ref T reference9 = ref value;
				val = default(T);
				if (val == null)
				{
					val = reference9;
					reference9 = ref val;
				}
				if (reference9.Equals(Unsafe.Add(ref searchSpace, length + 3)))
				{
					goto IL_0343;
				}
				ref T reference10 = ref value;
				val = default(T);
				if (val == null)
				{
					val = reference10;
					reference10 = ref val;
				}
				if (reference10.Equals(Unsafe.Add(ref searchSpace, length + 2)))
				{
					goto IL_033f;
				}
				ref T reference11 = ref value;
				val = default(T);
				if (val == null)
				{
					val = reference11;
					reference11 = ref val;
				}
				if (reference11.Equals(Unsafe.Add(ref searchSpace, length + 1)))
				{
					goto IL_033b;
				}
				ref T reference12 = ref value;
				val = default(T);
				if (val == null)
				{
					val = reference12;
					reference12 = ref val;
				}
				if (reference12.Equals(Unsafe.Add(ref searchSpace, length)))
				{
					goto IL_0339;
				}
			}
			while (length > 0)
			{
				length--;
				ref T reference13 = ref value;
				val = default(T);
				if (val == null)
				{
					val = reference13;
					reference13 = ref val;
				}
				if (!reference13.Equals(Unsafe.Add(ref searchSpace, length)))
				{
					continue;
				}
				goto IL_0339;
			}
		}
		else
		{
			length--;
			while (length >= 0)
			{
				if (Unsafe.Add(ref searchSpace, length) != null)
				{
					length--;
					continue;
				}
				goto IL_0339;
			}
		}
		return -1;
		IL_033b:
		return length + 1;
		IL_0339:
		return length;
		IL_033f:
		return length + 2;
		IL_0343:
		return length + 3;
	}

	public static int LastIndexOfAny<T>(ref T searchSpace, T value0, T value1, int length) where T : IEquatable<T>
	{
		if (default(T) != null || (value0 != null && value1 != null))
		{
			while (length >= 8)
			{
				length -= 8;
				T other = Unsafe.Add(ref searchSpace, length + 7);
				if (!value0.Equals(other) && !value1.Equals(other))
				{
					other = Unsafe.Add(ref searchSpace, length + 6);
					if (!value0.Equals(other) && !value1.Equals(other))
					{
						other = Unsafe.Add(ref searchSpace, length + 5);
						if (!value0.Equals(other) && !value1.Equals(other))
						{
							other = Unsafe.Add(ref searchSpace, length + 4);
							if (!value0.Equals(other) && !value1.Equals(other))
							{
								other = Unsafe.Add(ref searchSpace, length + 3);
								if (!value0.Equals(other) && !value1.Equals(other))
								{
									other = Unsafe.Add(ref searchSpace, length + 2);
									if (!value0.Equals(other) && !value1.Equals(other))
									{
										other = Unsafe.Add(ref searchSpace, length + 1);
										if (!value0.Equals(other) && !value1.Equals(other))
										{
											other = Unsafe.Add(ref searchSpace, length);
											if (!value0.Equals(other) && !value1.Equals(other))
											{
												continue;
											}
											goto IL_0351;
										}
										goto IL_0353;
									}
									goto IL_0357;
								}
								goto IL_035b;
							}
							return length + 4;
						}
						return length + 5;
					}
					return length + 6;
				}
				return length + 7;
			}
			if (length >= 4)
			{
				length -= 4;
				T other = Unsafe.Add(ref searchSpace, length + 3);
				if (value0.Equals(other) || value1.Equals(other))
				{
					goto IL_035b;
				}
				other = Unsafe.Add(ref searchSpace, length + 2);
				if (value0.Equals(other) || value1.Equals(other))
				{
					goto IL_0357;
				}
				other = Unsafe.Add(ref searchSpace, length + 1);
				if (value0.Equals(other) || value1.Equals(other))
				{
					goto IL_0353;
				}
				other = Unsafe.Add(ref searchSpace, length);
				if (value0.Equals(other) || value1.Equals(other))
				{
					goto IL_0351;
				}
			}
			while (length > 0)
			{
				length--;
				T other = Unsafe.Add(ref searchSpace, length);
				if (!value0.Equals(other) && !value1.Equals(other))
				{
					continue;
				}
				goto IL_0351;
			}
		}
		else
		{
			for (length--; length >= 0; length--)
			{
				T other = Unsafe.Add(ref searchSpace, length);
				if (other == null)
				{
					if (value0 != null && value1 != null)
					{
						continue;
					}
				}
				else if (!other.Equals(value0) && !other.Equals(value1))
				{
					continue;
				}
				goto IL_0351;
			}
		}
		return -1;
		IL_035b:
		return length + 3;
		IL_0357:
		return length + 2;
		IL_0351:
		return length;
		IL_0353:
		return length + 1;
	}

	public static int LastIndexOfAny<T>(ref T searchSpace, T value0, T value1, T value2, int length) where T : IEquatable<T>
	{
		if (default(T) != null || (value0 != null && value1 != null && value2 != null))
		{
			while (length >= 8)
			{
				length -= 8;
				T other = Unsafe.Add(ref searchSpace, length + 7);
				if (!value0.Equals(other) && !value1.Equals(other) && !value2.Equals(other))
				{
					other = Unsafe.Add(ref searchSpace, length + 6);
					if (!value0.Equals(other) && !value1.Equals(other) && !value2.Equals(other))
					{
						other = Unsafe.Add(ref searchSpace, length + 5);
						if (!value0.Equals(other) && !value1.Equals(other) && !value2.Equals(other))
						{
							other = Unsafe.Add(ref searchSpace, length + 4);
							if (!value0.Equals(other) && !value1.Equals(other) && !value2.Equals(other))
							{
								other = Unsafe.Add(ref searchSpace, length + 3);
								if (!value0.Equals(other) && !value1.Equals(other) && !value2.Equals(other))
								{
									other = Unsafe.Add(ref searchSpace, length + 2);
									if (!value0.Equals(other) && !value1.Equals(other) && !value2.Equals(other))
									{
										other = Unsafe.Add(ref searchSpace, length + 1);
										if (!value0.Equals(other) && !value1.Equals(other) && !value2.Equals(other))
										{
											other = Unsafe.Add(ref searchSpace, length);
											if (!value0.Equals(other) && !value1.Equals(other) && !value2.Equals(other))
											{
												continue;
											}
											goto IL_0485;
										}
										goto IL_0488;
									}
									goto IL_048d;
								}
								goto IL_0492;
							}
							return length + 4;
						}
						return length + 5;
					}
					return length + 6;
				}
				return length + 7;
			}
			if (length >= 4)
			{
				length -= 4;
				T other = Unsafe.Add(ref searchSpace, length + 3);
				if (value0.Equals(other) || value1.Equals(other) || value2.Equals(other))
				{
					goto IL_0492;
				}
				other = Unsafe.Add(ref searchSpace, length + 2);
				if (value0.Equals(other) || value1.Equals(other) || value2.Equals(other))
				{
					goto IL_048d;
				}
				other = Unsafe.Add(ref searchSpace, length + 1);
				if (value0.Equals(other) || value1.Equals(other) || value2.Equals(other))
				{
					goto IL_0488;
				}
				other = Unsafe.Add(ref searchSpace, length);
				if (value0.Equals(other) || value1.Equals(other) || value2.Equals(other))
				{
					goto IL_0485;
				}
			}
			while (length > 0)
			{
				length--;
				T other = Unsafe.Add(ref searchSpace, length);
				if (!value0.Equals(other) && !value1.Equals(other) && !value2.Equals(other))
				{
					continue;
				}
				goto IL_0485;
			}
		}
		else
		{
			for (length--; length >= 0; length--)
			{
				T other = Unsafe.Add(ref searchSpace, length);
				if (other == null)
				{
					if (value0 != null && value1 != null && value2 != null)
					{
						continue;
					}
				}
				else if (!other.Equals(value0) && !other.Equals(value1) && !other.Equals(value2))
				{
					continue;
				}
				goto IL_0485;
			}
		}
		return -1;
		IL_0488:
		return length + 1;
		IL_0485:
		return length;
		IL_0492:
		return length + 3;
		IL_048d:
		return length + 2;
	}

	public static int LastIndexOfAny<T>(ref T searchSpace, int searchSpaceLength, ref T value, int valueLength) where T : IEquatable<T>
	{
		if (valueLength == 0)
		{
			return -1;
		}
		if (typeof(T).IsValueType)
		{
			for (int num = searchSpaceLength - 1; num >= 0; num--)
			{
				T other = Unsafe.Add(ref searchSpace, num);
				for (int i = 0; i < valueLength; i++)
				{
					if (Unsafe.Add(ref value, i).Equals(other))
					{
						return num;
					}
				}
			}
		}
		else
		{
			for (int num2 = searchSpaceLength - 1; num2 >= 0; num2--)
			{
				T val = Unsafe.Add(ref searchSpace, num2);
				if (val != null)
				{
					for (int j = 0; j < valueLength; j++)
					{
						ref T reference = ref val;
						T val2 = default(T);
						if (val2 == null)
						{
							val2 = reference;
							reference = ref val2;
						}
						if (reference.Equals(Unsafe.Add(ref value, j)))
						{
							return num2;
						}
					}
				}
				else
				{
					for (int k = 0; k < valueLength; k++)
					{
						if (Unsafe.Add(ref value, k) == null)
						{
							return num2;
						}
					}
				}
			}
		}
		return -1;
	}

	internal static int IndexOfAnyExcept<T>(ref T searchSpace, T value0, int length)
	{
		for (int i = 0; i < length; i++)
		{
			if (!EqualityComparer<T>.Default.Equals(Unsafe.Add(ref searchSpace, i), value0))
			{
				return i;
			}
		}
		return -1;
	}

	internal static int LastIndexOfAnyExcept<T>(ref T searchSpace, T value0, int length)
	{
		for (int num = length - 1; num >= 0; num--)
		{
			if (!EqualityComparer<T>.Default.Equals(Unsafe.Add(ref searchSpace, num), value0))
			{
				return num;
			}
		}
		return -1;
	}

	internal static int IndexOfAnyExcept<T>(ref T searchSpace, T value0, T value1, int length)
	{
		for (int i = 0; i < length; i++)
		{
			ref T reference = ref Unsafe.Add(ref searchSpace, i);
			if (!EqualityComparer<T>.Default.Equals(reference, value0) && !EqualityComparer<T>.Default.Equals(reference, value1))
			{
				return i;
			}
		}
		return -1;
	}

	internal static int LastIndexOfAnyExcept<T>(ref T searchSpace, T value0, T value1, int length)
	{
		for (int num = length - 1; num >= 0; num--)
		{
			ref T reference = ref Unsafe.Add(ref searchSpace, num);
			if (!EqualityComparer<T>.Default.Equals(reference, value0) && !EqualityComparer<T>.Default.Equals(reference, value1))
			{
				return num;
			}
		}
		return -1;
	}

	internal static int IndexOfAnyExcept<T>(ref T searchSpace, T value0, T value1, T value2, int length)
	{
		for (int i = 0; i < length; i++)
		{
			ref T reference = ref Unsafe.Add(ref searchSpace, i);
			if (!EqualityComparer<T>.Default.Equals(reference, value0) && !EqualityComparer<T>.Default.Equals(reference, value1) && !EqualityComparer<T>.Default.Equals(reference, value2))
			{
				return i;
			}
		}
		return -1;
	}

	internal static int LastIndexOfAnyExcept<T>(ref T searchSpace, T value0, T value1, T value2, int length)
	{
		for (int num = length - 1; num >= 0; num--)
		{
			ref T reference = ref Unsafe.Add(ref searchSpace, num);
			if (!EqualityComparer<T>.Default.Equals(reference, value0) && !EqualityComparer<T>.Default.Equals(reference, value1) && !EqualityComparer<T>.Default.Equals(reference, value2))
			{
				return num;
			}
		}
		return -1;
	}

	internal static int IndexOfAnyExcept<T>(ref T searchSpace, T value0, T value1, T value2, T value3, int length)
	{
		for (int i = 0; i < length; i++)
		{
			ref T reference = ref Unsafe.Add(ref searchSpace, i);
			if (!EqualityComparer<T>.Default.Equals(reference, value0) && !EqualityComparer<T>.Default.Equals(reference, value1) && !EqualityComparer<T>.Default.Equals(reference, value2) && !EqualityComparer<T>.Default.Equals(reference, value3))
			{
				return i;
			}
		}
		return -1;
	}

	internal static int LastIndexOfAnyExcept<T>(ref T searchSpace, T value0, T value1, T value2, T value3, int length)
	{
		for (int num = length - 1; num >= 0; num--)
		{
			ref T reference = ref Unsafe.Add(ref searchSpace, num);
			if (!EqualityComparer<T>.Default.Equals(reference, value0) && !EqualityComparer<T>.Default.Equals(reference, value1) && !EqualityComparer<T>.Default.Equals(reference, value2) && !EqualityComparer<T>.Default.Equals(reference, value3))
			{
				return num;
			}
		}
		return -1;
	}

	public static bool SequenceEqual<T>(ref T first, ref T second, int length) where T : IEquatable<T>
	{
		if (!Unsafe.AreSame(ref first, ref second))
		{
			nint num = 0;
			while (true)
			{
				if (length >= 8)
				{
					length -= 8;
					T val = Unsafe.Add(ref first, num);
					T val2 = Unsafe.Add(ref second, num);
					if (val?.Equals(val2) ?? (val2 == null))
					{
						val = Unsafe.Add(ref first, num + 1);
						val2 = Unsafe.Add(ref second, num + 1);
						if (val?.Equals(val2) ?? (val2 == null))
						{
							val = Unsafe.Add(ref first, num + 2);
							val2 = Unsafe.Add(ref second, num + 2);
							if (val?.Equals(val2) ?? (val2 == null))
							{
								val = Unsafe.Add(ref first, num + 3);
								val2 = Unsafe.Add(ref second, num + 3);
								if (val?.Equals(val2) ?? (val2 == null))
								{
									val = Unsafe.Add(ref first, num + 4);
									val2 = Unsafe.Add(ref second, num + 4);
									if (val?.Equals(val2) ?? (val2 == null))
									{
										val = Unsafe.Add(ref first, num + 5);
										val2 = Unsafe.Add(ref second, num + 5);
										if (val?.Equals(val2) ?? (val2 == null))
										{
											val = Unsafe.Add(ref first, num + 6);
											val2 = Unsafe.Add(ref second, num + 6);
											if (val?.Equals(val2) ?? (val2 == null))
											{
												val = Unsafe.Add(ref first, num + 7);
												val2 = Unsafe.Add(ref second, num + 7);
												if (val?.Equals(val2) ?? (val2 == null))
												{
													num += 8;
													continue;
												}
											}
										}
									}
								}
							}
						}
					}
				}
				else
				{
					if (length < 4)
					{
						goto IL_03b8;
					}
					length -= 4;
					T val = Unsafe.Add(ref first, num);
					T val2 = Unsafe.Add(ref second, num);
					if (val?.Equals(val2) ?? (val2 == null))
					{
						val = Unsafe.Add(ref first, num + 1);
						val2 = Unsafe.Add(ref second, num + 1);
						if (val?.Equals(val2) ?? (val2 == null))
						{
							val = Unsafe.Add(ref first, num + 2);
							val2 = Unsafe.Add(ref second, num + 2);
							if (val?.Equals(val2) ?? (val2 == null))
							{
								val = Unsafe.Add(ref first, num + 3);
								val2 = Unsafe.Add(ref second, num + 3);
								if (val?.Equals(val2) ?? (val2 == null))
								{
									num += 4;
									goto IL_03b8;
								}
							}
						}
					}
				}
				goto IL_03be;
				IL_03b8:
				while (length > 0)
				{
					T val = Unsafe.Add(ref first, num);
					T val2 = Unsafe.Add(ref second, num);
					if (val?.Equals(val2) ?? (val2 == null))
					{
						num++;
						length--;
						continue;
					}
					goto IL_03be;
				}
				break;
				IL_03be:
				return false;
			}
		}
		return true;
	}

	public static int SequenceCompareTo<T>(ref T first, int firstLength, ref T second, int secondLength) where T : IComparable<T>
	{
		int num = firstLength;
		if (num > secondLength)
		{
			num = secondLength;
		}
		for (int i = 0; i < num; i++)
		{
			T val = Unsafe.Add(ref second, i);
			int num2 = Unsafe.Add(ref first, i)?.CompareTo(val) ?? ((val != null) ? (-1) : 0);
			if (num2 != 0)
			{
				return num2;
			}
		}
		return firstLength.CompareTo(secondLength);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal unsafe static bool ContainsValueType<T>(ref T searchSpace, T value, int length) where T : struct, INumber<T>
	{
		if (PackedSpanHelpers.PackedIndexOfIsSupported && typeof(T) == typeof(short) && PackedSpanHelpers.CanUsePackedIndexOf(value))
		{
			return PackedSpanHelpers.Contains(ref Unsafe.As<T, short>(ref searchSpace), *(short*)(&value), length);
		}
		return NonPackedContainsValueType(ref searchSpace, value, length);
	}

	internal static bool NonPackedContainsValueType<T>(ref T searchSpace, T value, int length) where T : struct, INumber<T>
	{
		if (!Vector128.IsHardwareAccelerated || length < Vector128<T>.Count)
		{
			nuint num = 0u;
			while (length >= 8)
			{
				length -= 8;
				if (Unsafe.Add(ref searchSpace, num) == value || Unsafe.Add(ref searchSpace, num + 1) == value || Unsafe.Add(ref searchSpace, num + 2) == value || Unsafe.Add(ref searchSpace, num + 3) == value || Unsafe.Add(ref searchSpace, num + 4) == value || Unsafe.Add(ref searchSpace, num + 5) == value || Unsafe.Add(ref searchSpace, num + 6) == value || Unsafe.Add(ref searchSpace, num + 7) == value)
				{
					return true;
				}
				num += 8;
			}
			if (length >= 4)
			{
				length -= 4;
				if (Unsafe.Add(ref searchSpace, num) == value || Unsafe.Add(ref searchSpace, num + 1) == value || Unsafe.Add(ref searchSpace, num + 2) == value || Unsafe.Add(ref searchSpace, num + 3) == value)
				{
					return true;
				}
				num += 4;
			}
			while (length > 0)
			{
				length--;
				if (Unsafe.Add(ref searchSpace, num) == value)
				{
					return true;
				}
				num++;
			}
		}
		else if (Vector512.IsHardwareAccelerated && length >= Vector512<T>.Count)
		{
			Vector512<T> left = Vector512.Create(value);
			ref T reference = ref searchSpace;
			ref T reference2 = ref Unsafe.Add(ref searchSpace, (uint)(length - Vector512<T>.Count));
			do
			{
				Vector512<T> right = Vector512.LoadUnsafe(ref reference);
				if (Vector512.EqualsAny(left, right))
				{
					return true;
				}
				reference = ref Unsafe.Add(ref reference, Vector512<T>.Count);
			}
			while (!Unsafe.IsAddressGreaterThan(ref reference, ref reference2));
			if ((uint)length % Vector512<T>.Count != 0L)
			{
				Vector512<T> right = Vector512.LoadUnsafe(ref reference2);
				if (Vector512.EqualsAny(left, right))
				{
					return true;
				}
			}
		}
		else if (Vector256.IsHardwareAccelerated && length >= Vector256<T>.Count)
		{
			Vector256<T> left2 = Vector256.Create(value);
			ref T reference3 = ref searchSpace;
			ref T reference4 = ref Unsafe.Add(ref searchSpace, (uint)(length - Vector256<T>.Count));
			do
			{
				Vector256<T> vector = Vector256.Equals(left2, Vector256.LoadUnsafe(ref reference3));
				if (vector == Vector256<T>.Zero)
				{
					reference3 = ref Unsafe.Add(ref reference3, Vector256<T>.Count);
					continue;
				}
				return true;
			}
			while (!Unsafe.IsAddressGreaterThan(ref reference3, ref reference4));
			if ((uint)length % Vector256<T>.Count != 0L)
			{
				Vector256<T> vector = Vector256.Equals(left2, Vector256.LoadUnsafe(ref reference4));
				if (vector != Vector256<T>.Zero)
				{
					return true;
				}
			}
		}
		else
		{
			Vector128<T> left3 = Vector128.Create(value);
			ref T reference5 = ref searchSpace;
			ref T reference6 = ref Unsafe.Add(ref searchSpace, (uint)(length - Vector128<T>.Count));
			do
			{
				Vector128<T> vector2 = Vector128.Equals(left3, Vector128.LoadUnsafe(ref reference5));
				if (vector2 == Vector128<T>.Zero)
				{
					reference5 = ref Unsafe.Add(ref reference5, Vector128<T>.Count);
					continue;
				}
				return true;
			}
			while (!Unsafe.IsAddressGreaterThan(ref reference5, ref reference6));
			if ((uint)length % Vector128<T>.Count != 0L)
			{
				Vector128<T> vector2 = Vector128.Equals(left3, Vector128.LoadUnsafe(ref reference6));
				if (vector2 != Vector128<T>.Zero)
				{
					return true;
				}
			}
		}
		return false;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static int IndexOfChar(ref char searchSpace, char value, int length)
	{
		return IndexOfValueType(ref Unsafe.As<char, short>(ref searchSpace), (short)value, length);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static int NonPackedIndexOfChar(ref char searchSpace, char value, int length)
	{
		return NonPackedIndexOfValueType<short, DontNegate<short>>(ref Unsafe.As<char, short>(ref searchSpace), (short)value, length);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static int IndexOfValueType<T>(ref T searchSpace, T value, int length) where T : struct, INumber<T>
	{
		return IndexOfValueType<T, DontNegate<T>>(ref searchSpace, value, length);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static int IndexOfAnyExceptValueType<T>(ref T searchSpace, T value, int length) where T : struct, INumber<T>
	{
		return IndexOfValueType<T, Negate<T>>(ref searchSpace, value, length);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private unsafe static int IndexOfValueType<TValue, TNegator>(ref TValue searchSpace, TValue value, int length) where TValue : struct, INumber<TValue> where TNegator : struct, INegator<TValue>
	{
		if (PackedSpanHelpers.PackedIndexOfIsSupported && typeof(TValue) == typeof(short) && PackedSpanHelpers.CanUsePackedIndexOf(value))
		{
			if (!(typeof(TNegator) == typeof(DontNegate<short>)))
			{
				return PackedSpanHelpers.IndexOfAnyExcept(ref Unsafe.As<TValue, char>(ref searchSpace), *(char*)(&value), length);
			}
			return PackedSpanHelpers.IndexOf(ref Unsafe.As<TValue, char>(ref searchSpace), *(char*)(&value), length);
		}
		return NonPackedIndexOfValueType<TValue, TNegator>(ref searchSpace, value, length);
	}

	internal static int NonPackedIndexOfValueType<TValue, TNegator>(ref TValue searchSpace, TValue value, int length) where TValue : struct, INumber<TValue> where TNegator : struct, INegator<TValue>
	{
		if (!Vector128.IsHardwareAccelerated || length < Vector128<TValue>.Count)
		{
			nuint num = 0u;
			while (true)
			{
				if (length >= 8)
				{
					length -= 8;
					if (TNegator.NegateIfNeeded(Unsafe.Add(ref searchSpace, num) == value))
					{
						break;
					}
					if (!TNegator.NegateIfNeeded(Unsafe.Add(ref searchSpace, num + 1) == value))
					{
						if (!TNegator.NegateIfNeeded(Unsafe.Add(ref searchSpace, num + 2) == value))
						{
							if (!TNegator.NegateIfNeeded(Unsafe.Add(ref searchSpace, num + 3) == value))
							{
								if (!TNegator.NegateIfNeeded(Unsafe.Add(ref searchSpace, num + 4) == value))
								{
									if (!TNegator.NegateIfNeeded(Unsafe.Add(ref searchSpace, num + 5) == value))
									{
										if (!TNegator.NegateIfNeeded(Unsafe.Add(ref searchSpace, num + 6) == value))
										{
											if (!TNegator.NegateIfNeeded(Unsafe.Add(ref searchSpace, num + 7) == value))
											{
												num += 8;
												continue;
											}
											return (int)(num + 7);
										}
										return (int)(num + 6);
									}
									return (int)(num + 5);
								}
								return (int)(num + 4);
							}
							goto IL_0286;
						}
						goto IL_028c;
					}
					goto IL_0292;
				}
				if (length >= 4)
				{
					length -= 4;
					if (TNegator.NegateIfNeeded(Unsafe.Add(ref searchSpace, num) == value))
					{
						break;
					}
					if (TNegator.NegateIfNeeded(Unsafe.Add(ref searchSpace, num + 1) == value))
					{
						goto IL_0292;
					}
					if (TNegator.NegateIfNeeded(Unsafe.Add(ref searchSpace, num + 2) == value))
					{
						goto IL_028c;
					}
					if (TNegator.NegateIfNeeded(Unsafe.Add(ref searchSpace, num + 3) == value))
					{
						goto IL_0286;
					}
					num += 4;
				}
				while (true)
				{
					if (length > 0)
					{
						length--;
						if (TNegator.NegateIfNeeded(Unsafe.Add(ref searchSpace, num) == value))
						{
							break;
						}
						num++;
						continue;
					}
					return -1;
				}
				break;
				IL_0292:
				return (int)(num + 1);
				IL_0286:
				return (int)(num + 3);
				IL_028c:
				return (int)(num + 2);
			}
			return (int)num;
		}
		if (Vector512.IsHardwareAccelerated && length >= Vector512<TValue>.Count)
		{
			Vector512<TValue> left = Vector512.Create(value);
			ref TValue reference = ref searchSpace;
			ref TValue reference2 = ref Unsafe.Add(ref searchSpace, length - Vector512<TValue>.Count);
			do
			{
				Vector512<TValue> right = Vector512.LoadUnsafe(ref reference);
				if (TNegator.HasMatch(left, right))
				{
					return ComputeFirstIndex(ref searchSpace, ref reference, TNegator.GetMatchMask(left, right));
				}
				reference = ref Unsafe.Add(ref reference, Vector512<TValue>.Count);
			}
			while (!Unsafe.IsAddressGreaterThan(ref reference, ref reference2));
			if ((uint)length % Vector512<TValue>.Count != 0L)
			{
				Vector512<TValue> right = Vector512.LoadUnsafe(ref reference2);
				if (TNegator.HasMatch(left, right))
				{
					return ComputeFirstIndex(ref searchSpace, ref reference2, TNegator.GetMatchMask(left, right));
				}
			}
		}
		else if (Vector256.IsHardwareAccelerated && length >= Vector256<TValue>.Count)
		{
			Vector256<TValue> left2 = Vector256.Create(value);
			ref TValue reference3 = ref searchSpace;
			ref TValue reference4 = ref Unsafe.Add(ref searchSpace, length - Vector256<TValue>.Count);
			do
			{
				Vector256<TValue> vector = TNegator.NegateIfNeeded(Vector256.Equals(left2, Vector256.LoadUnsafe(ref reference3)));
				if (vector == Vector256<TValue>.Zero)
				{
					reference3 = ref Unsafe.Add(ref reference3, Vector256<TValue>.Count);
					continue;
				}
				return ComputeFirstIndex(ref searchSpace, ref reference3, vector);
			}
			while (!Unsafe.IsAddressGreaterThan(ref reference3, ref reference4));
			if ((uint)length % Vector256<TValue>.Count != 0L)
			{
				Vector256<TValue> vector = TNegator.NegateIfNeeded(Vector256.Equals(left2, Vector256.LoadUnsafe(ref reference4)));
				if (vector != Vector256<TValue>.Zero)
				{
					return ComputeFirstIndex(ref searchSpace, ref reference4, vector);
				}
			}
		}
		else
		{
			Vector128<TValue> left3 = Vector128.Create(value);
			ref TValue reference5 = ref searchSpace;
			ref TValue reference6 = ref Unsafe.Add(ref searchSpace, length - Vector128<TValue>.Count);
			do
			{
				Vector128<TValue> vector2 = TNegator.NegateIfNeeded(Vector128.Equals(left3, Vector128.LoadUnsafe(ref reference5)));
				if (vector2 == Vector128<TValue>.Zero)
				{
					reference5 = ref Unsafe.Add(ref reference5, Vector128<TValue>.Count);
					continue;
				}
				return ComputeFirstIndex(ref searchSpace, ref reference5, vector2);
			}
			while (!Unsafe.IsAddressGreaterThan(ref reference5, ref reference6));
			if ((uint)length % Vector128<TValue>.Count != 0L)
			{
				Vector128<TValue> vector2 = TNegator.NegateIfNeeded(Vector128.Equals(left3, Vector128.LoadUnsafe(ref reference6)));
				if (vector2 != Vector128<TValue>.Zero)
				{
					return ComputeFirstIndex(ref searchSpace, ref reference6, vector2);
				}
			}
		}
		return -1;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static int IndexOfAnyChar(ref char searchSpace, char value0, char value1, int length)
	{
		return IndexOfAnyValueType(ref Unsafe.As<char, short>(ref searchSpace), (short)value0, (short)value1, length);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static int IndexOfAnyValueType<T>(ref T searchSpace, T value0, T value1, int length) where T : struct, INumber<T>
	{
		return IndexOfAnyValueType<T, DontNegate<T>>(ref searchSpace, value0, value1, length);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static int IndexOfAnyExceptValueType<T>(ref T searchSpace, T value0, T value1, int length) where T : struct, INumber<T>
	{
		return IndexOfAnyValueType<T, Negate<T>>(ref searchSpace, value0, value1, length);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private unsafe static int IndexOfAnyValueType<TValue, TNegator>(ref TValue searchSpace, TValue value0, TValue value1, int length) where TValue : struct, INumber<TValue> where TNegator : struct, INegator<TValue>
	{
		if (PackedSpanHelpers.PackedIndexOfIsSupported && typeof(TValue) == typeof(short) && PackedSpanHelpers.CanUsePackedIndexOf(value0) && PackedSpanHelpers.CanUsePackedIndexOf(value1))
		{
			if (!(typeof(TNegator) == typeof(DontNegate<short>)))
			{
				return PackedSpanHelpers.IndexOfAnyExcept(ref Unsafe.As<TValue, char>(ref searchSpace), *(char*)(&value0), *(char*)(&value1), length);
			}
			return PackedSpanHelpers.IndexOfAny(ref Unsafe.As<TValue, char>(ref searchSpace), *(char*)(&value0), *(char*)(&value1), length);
		}
		return NonPackedIndexOfAnyValueType<TValue, TNegator>(ref searchSpace, value0, value1, length);
	}

	internal static int NonPackedIndexOfAnyValueType<TValue, TNegator>(ref TValue searchSpace, TValue value0, TValue value1, int length) where TValue : struct, INumber<TValue> where TNegator : struct, INegator<TValue>
	{
		nuint num;
		if (!Vector128.IsHardwareAccelerated || length < Vector128<TValue>.Count)
		{
			num = 0u;
			if (typeof(TValue) == typeof(byte))
			{
				while (length >= 8)
				{
					length -= 8;
					ref TValue reference = ref Unsafe.Add(ref searchSpace, num);
					TValue val = reference;
					if (!TNegator.NegateIfNeeded(val == value0 || val == value1))
					{
						val = Unsafe.Add(ref reference, 1);
						if (!TNegator.NegateIfNeeded(val == value0 || val == value1))
						{
							val = Unsafe.Add(ref reference, 2);
							if (!TNegator.NegateIfNeeded(val == value0 || val == value1))
							{
								val = Unsafe.Add(ref reference, 3);
								if (!TNegator.NegateIfNeeded(val == value0 || val == value1))
								{
									val = Unsafe.Add(ref reference, 4);
									if (!TNegator.NegateIfNeeded(val == value0 || val == value1))
									{
										val = Unsafe.Add(ref reference, 5);
										if (!TNegator.NegateIfNeeded(val == value0 || val == value1))
										{
											val = Unsafe.Add(ref reference, 6);
											if (!TNegator.NegateIfNeeded(val == value0 || val == value1))
											{
												val = Unsafe.Add(ref reference, 7);
												if (!TNegator.NegateIfNeeded(val == value0 || val == value1))
												{
													num += 8;
													continue;
												}
												return (int)(num + 7);
											}
											return (int)(num + 6);
										}
										return (int)(num + 5);
									}
									return (int)(num + 4);
								}
								goto IL_0393;
							}
							goto IL_0399;
						}
						goto IL_039f;
					}
					goto IL_03a5;
				}
			}
			while (true)
			{
				if (length >= 4)
				{
					length -= 4;
					ref TValue reference2 = ref Unsafe.Add(ref searchSpace, num);
					TValue val = reference2;
					if (TNegator.NegateIfNeeded(val == value0 || val == value1))
					{
						break;
					}
					val = Unsafe.Add(ref reference2, 1);
					if (!TNegator.NegateIfNeeded(val == value0 || val == value1))
					{
						val = Unsafe.Add(ref reference2, 2);
						if (!TNegator.NegateIfNeeded(val == value0 || val == value1))
						{
							val = Unsafe.Add(ref reference2, 3);
							if (!TNegator.NegateIfNeeded(val == value0 || val == value1))
							{
								num += 4;
								continue;
							}
							goto IL_0393;
						}
						goto IL_0399;
					}
					goto IL_039f;
				}
				while (true)
				{
					if (length > 0)
					{
						length--;
						TValue val = Unsafe.Add(ref searchSpace, num);
						if (TNegator.NegateIfNeeded(val == value0 || val == value1))
						{
							break;
						}
						num++;
						continue;
					}
					return -1;
				}
				break;
			}
			goto IL_03a5;
		}
		if (Vector512.IsHardwareAccelerated && length >= Vector512<TValue>.Count)
		{
			Vector512<TValue> left = Vector512.Create(value0);
			Vector512<TValue> left2 = Vector512.Create(value1);
			ref TValue reference3 = ref searchSpace;
			ref TValue reference4 = ref Unsafe.Add(ref searchSpace, length - Vector512<TValue>.Count);
			do
			{
				Vector512<TValue> right = Vector512.LoadUnsafe(ref reference3);
				Vector512<TValue> vector = TNegator.NegateIfNeeded(Vector512.Equals(left, right) | Vector512.Equals(left2, right));
				if (vector == Vector512<TValue>.Zero)
				{
					reference3 = ref Unsafe.Add(ref reference3, Vector512<TValue>.Count);
					continue;
				}
				return ComputeFirstIndex(ref searchSpace, ref reference3, vector);
			}
			while (!Unsafe.IsAddressGreaterThan(ref reference3, ref reference4));
			if ((uint)length % Vector512<TValue>.Count != 0L)
			{
				Vector512<TValue> right = Vector512.LoadUnsafe(ref reference4);
				Vector512<TValue> vector = TNegator.NegateIfNeeded(Vector512.Equals(left, right) | Vector512.Equals(left2, right));
				if (vector != Vector512<TValue>.Zero)
				{
					return ComputeFirstIndex(ref searchSpace, ref reference4, vector);
				}
			}
		}
		else if (Vector256.IsHardwareAccelerated && length >= Vector256<TValue>.Count)
		{
			Vector256<TValue> left3 = Vector256.Create(value0);
			Vector256<TValue> left4 = Vector256.Create(value1);
			ref TValue reference5 = ref searchSpace;
			ref TValue reference6 = ref Unsafe.Add(ref searchSpace, length - Vector256<TValue>.Count);
			do
			{
				Vector256<TValue> right2 = Vector256.LoadUnsafe(ref reference5);
				Vector256<TValue> vector2 = TNegator.NegateIfNeeded(Vector256.Equals(left3, right2) | Vector256.Equals(left4, right2));
				if (vector2 == Vector256<TValue>.Zero)
				{
					reference5 = ref Unsafe.Add(ref reference5, Vector256<TValue>.Count);
					continue;
				}
				return ComputeFirstIndex(ref searchSpace, ref reference5, vector2);
			}
			while (!Unsafe.IsAddressGreaterThan(ref reference5, ref reference6));
			if ((uint)length % Vector256<TValue>.Count != 0L)
			{
				Vector256<TValue> right2 = Vector256.LoadUnsafe(ref reference6);
				Vector256<TValue> vector2 = TNegator.NegateIfNeeded(Vector256.Equals(left3, right2) | Vector256.Equals(left4, right2));
				if (vector2 != Vector256<TValue>.Zero)
				{
					return ComputeFirstIndex(ref searchSpace, ref reference6, vector2);
				}
			}
		}
		else
		{
			Vector128<TValue> left5 = Vector128.Create(value0);
			Vector128<TValue> left6 = Vector128.Create(value1);
			ref TValue reference7 = ref searchSpace;
			ref TValue reference8 = ref Unsafe.Add(ref searchSpace, length - Vector128<TValue>.Count);
			do
			{
				Vector128<TValue> right3 = Vector128.LoadUnsafe(ref reference7);
				Vector128<TValue> vector3 = TNegator.NegateIfNeeded(Vector128.Equals(left5, right3) | Vector128.Equals(left6, right3));
				if (vector3 == Vector128<TValue>.Zero)
				{
					reference7 = ref Unsafe.Add(ref reference7, Vector128<TValue>.Count);
					continue;
				}
				return ComputeFirstIndex(ref searchSpace, ref reference7, vector3);
			}
			while (!Unsafe.IsAddressGreaterThan(ref reference7, ref reference8));
			if ((uint)length % Vector128<TValue>.Count != 0L)
			{
				Vector128<TValue> right3 = Vector128.LoadUnsafe(ref reference8);
				Vector128<TValue> vector3 = TNegator.NegateIfNeeded(Vector128.Equals(left5, right3) | Vector128.Equals(left6, right3));
				if (vector3 != Vector128<TValue>.Zero)
				{
					return ComputeFirstIndex(ref searchSpace, ref reference8, vector3);
				}
			}
		}
		return -1;
		IL_0399:
		return (int)(num + 2);
		IL_039f:
		return (int)(num + 1);
		IL_03a5:
		return (int)num;
		IL_0393:
		return (int)(num + 3);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static int IndexOfAnyValueType<T>(ref T searchSpace, T value0, T value1, T value2, int length) where T : struct, INumber<T>
	{
		return IndexOfAnyValueType<T, DontNegate<T>>(ref searchSpace, value0, value1, value2, length);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static int IndexOfAnyExceptValueType<T>(ref T searchSpace, T value0, T value1, T value2, int length) where T : struct, INumber<T>
	{
		return IndexOfAnyValueType<T, Negate<T>>(ref searchSpace, value0, value1, value2, length);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private unsafe static int IndexOfAnyValueType<TValue, TNegator>(ref TValue searchSpace, TValue value0, TValue value1, TValue value2, int length) where TValue : struct, INumber<TValue> where TNegator : struct, INegator<TValue>
	{
		if (PackedSpanHelpers.PackedIndexOfIsSupported && typeof(TValue) == typeof(short) && PackedSpanHelpers.CanUsePackedIndexOf(value0) && PackedSpanHelpers.CanUsePackedIndexOf(value1) && PackedSpanHelpers.CanUsePackedIndexOf(value2))
		{
			if (!(typeof(TNegator) == typeof(DontNegate<short>)))
			{
				return PackedSpanHelpers.IndexOfAnyExcept(ref Unsafe.As<TValue, char>(ref searchSpace), *(char*)(&value0), *(char*)(&value1), *(char*)(&value2), length);
			}
			return PackedSpanHelpers.IndexOfAny(ref Unsafe.As<TValue, char>(ref searchSpace), *(char*)(&value0), *(char*)(&value1), *(char*)(&value2), length);
		}
		return NonPackedIndexOfAnyValueType<TValue, TNegator>(ref searchSpace, value0, value1, value2, length);
	}

	internal static int NonPackedIndexOfAnyValueType<TValue, TNegator>(ref TValue searchSpace, TValue value0, TValue value1, TValue value2, int length) where TValue : struct, INumber<TValue> where TNegator : struct, INegator<TValue>
	{
		nuint num;
		if (!Vector128.IsHardwareAccelerated || length < Vector128<TValue>.Count)
		{
			num = 0u;
			if (typeof(TValue) == typeof(byte))
			{
				while (length >= 8)
				{
					length -= 8;
					ref TValue reference = ref Unsafe.Add(ref searchSpace, num);
					TValue val = reference;
					if (!TNegator.NegateIfNeeded(val == value0 || val == value1 || val == value2))
					{
						val = Unsafe.Add(ref reference, 1);
						if (!TNegator.NegateIfNeeded(val == value0 || val == value1 || val == value2))
						{
							val = Unsafe.Add(ref reference, 2);
							if (!TNegator.NegateIfNeeded(val == value0 || val == value1 || val == value2))
							{
								val = Unsafe.Add(ref reference, 3);
								if (!TNegator.NegateIfNeeded(val == value0 || val == value1 || val == value2))
								{
									val = Unsafe.Add(ref reference, 4);
									if (!TNegator.NegateIfNeeded(val == value0 || val == value1 || val == value2))
									{
										val = Unsafe.Add(ref reference, 5);
										if (!TNegator.NegateIfNeeded(val == value0 || val == value1 || val == value2))
										{
											val = Unsafe.Add(ref reference, 6);
											if (!TNegator.NegateIfNeeded(val == value0 || val == value1 || val == value2))
											{
												val = Unsafe.Add(ref reference, 7);
												if (!TNegator.NegateIfNeeded(val == value0 || val == value1 || val == value2))
												{
													num += 8;
													continue;
												}
												return (int)(num + 7);
											}
											return (int)(num + 6);
										}
										return (int)(num + 5);
									}
									return (int)(num + 4);
								}
								goto IL_0460;
							}
							goto IL_0466;
						}
						goto IL_046c;
					}
					goto IL_0472;
				}
			}
			while (true)
			{
				if (length >= 4)
				{
					length -= 4;
					ref TValue reference2 = ref Unsafe.Add(ref searchSpace, num);
					TValue val = reference2;
					if (TNegator.NegateIfNeeded(val == value0 || val == value1 || val == value2))
					{
						break;
					}
					val = Unsafe.Add(ref reference2, 1);
					if (!TNegator.NegateIfNeeded(val == value0 || val == value1 || val == value2))
					{
						val = Unsafe.Add(ref reference2, 2);
						if (!TNegator.NegateIfNeeded(val == value0 || val == value1 || val == value2))
						{
							val = Unsafe.Add(ref reference2, 3);
							if (!TNegator.NegateIfNeeded(val == value0 || val == value1 || val == value2))
							{
								num += 4;
								continue;
							}
							goto IL_0460;
						}
						goto IL_0466;
					}
					goto IL_046c;
				}
				while (true)
				{
					if (length > 0)
					{
						length--;
						TValue val = Unsafe.Add(ref searchSpace, num);
						if (TNegator.NegateIfNeeded(val == value0 || val == value1 || val == value2))
						{
							break;
						}
						num++;
						continue;
					}
					return -1;
				}
				break;
			}
			goto IL_0472;
		}
		if (Vector512.IsHardwareAccelerated && length >= Vector512<TValue>.Count)
		{
			Vector512<TValue> left = Vector512.Create(value0);
			Vector512<TValue> left2 = Vector512.Create(value1);
			Vector512<TValue> left3 = Vector512.Create(value2);
			ref TValue reference3 = ref searchSpace;
			ref TValue reference4 = ref Unsafe.Add(ref searchSpace, length - Vector512<TValue>.Count);
			do
			{
				Vector512<TValue> right = Vector512.LoadUnsafe(ref reference3);
				Vector512<TValue> vector = TNegator.NegateIfNeeded(Vector512.Equals(left, right) | Vector512.Equals(left2, right) | Vector512.Equals(left3, right));
				if (vector == Vector512<TValue>.Zero)
				{
					reference3 = ref Unsafe.Add(ref reference3, Vector512<TValue>.Count);
					continue;
				}
				return ComputeFirstIndex(ref searchSpace, ref reference3, vector);
			}
			while (!Unsafe.IsAddressGreaterThan(ref reference3, ref reference4));
			if ((uint)length % Vector512<TValue>.Count != 0L)
			{
				Vector512<TValue> right = Vector512.LoadUnsafe(ref reference4);
				Vector512<TValue> vector = TNegator.NegateIfNeeded(Vector512.Equals(left, right) | Vector512.Equals(left2, right) | Vector512.Equals(left3, right));
				if (vector != Vector512<TValue>.Zero)
				{
					return ComputeFirstIndex(ref searchSpace, ref reference4, vector);
				}
			}
		}
		else if (Vector256.IsHardwareAccelerated && length >= Vector256<TValue>.Count)
		{
			Vector256<TValue> left4 = Vector256.Create(value0);
			Vector256<TValue> left5 = Vector256.Create(value1);
			Vector256<TValue> left6 = Vector256.Create(value2);
			ref TValue reference5 = ref searchSpace;
			ref TValue reference6 = ref Unsafe.Add(ref searchSpace, length - Vector256<TValue>.Count);
			do
			{
				Vector256<TValue> right2 = Vector256.LoadUnsafe(ref reference5);
				Vector256<TValue> vector2 = TNegator.NegateIfNeeded(Vector256.Equals(left4, right2) | Vector256.Equals(left5, right2) | Vector256.Equals(left6, right2));
				if (vector2 == Vector256<TValue>.Zero)
				{
					reference5 = ref Unsafe.Add(ref reference5, Vector256<TValue>.Count);
					continue;
				}
				return ComputeFirstIndex(ref searchSpace, ref reference5, vector2);
			}
			while (!Unsafe.IsAddressGreaterThan(ref reference5, ref reference6));
			if ((uint)length % Vector256<TValue>.Count != 0L)
			{
				Vector256<TValue> right2 = Vector256.LoadUnsafe(ref reference6);
				Vector256<TValue> vector2 = TNegator.NegateIfNeeded(Vector256.Equals(left4, right2) | Vector256.Equals(left5, right2) | Vector256.Equals(left6, right2));
				if (vector2 != Vector256<TValue>.Zero)
				{
					return ComputeFirstIndex(ref searchSpace, ref reference6, vector2);
				}
			}
		}
		else
		{
			Vector128<TValue> left7 = Vector128.Create(value0);
			Vector128<TValue> left8 = Vector128.Create(value1);
			Vector128<TValue> left9 = Vector128.Create(value2);
			ref TValue reference7 = ref searchSpace;
			ref TValue reference8 = ref Unsafe.Add(ref searchSpace, length - Vector128<TValue>.Count);
			do
			{
				Vector128<TValue> right3 = Vector128.LoadUnsafe(ref reference7);
				Vector128<TValue> vector3 = TNegator.NegateIfNeeded(Vector128.Equals(left7, right3) | Vector128.Equals(left8, right3) | Vector128.Equals(left9, right3));
				if (vector3 == Vector128<TValue>.Zero)
				{
					reference7 = ref Unsafe.Add(ref reference7, Vector128<TValue>.Count);
					continue;
				}
				return ComputeFirstIndex(ref searchSpace, ref reference7, vector3);
			}
			while (!Unsafe.IsAddressGreaterThan(ref reference7, ref reference8));
			if ((uint)length % Vector128<TValue>.Count != 0L)
			{
				Vector128<TValue> right3 = Vector128.LoadUnsafe(ref reference8);
				Vector128<TValue> vector3 = TNegator.NegateIfNeeded(Vector128.Equals(left7, right3) | Vector128.Equals(left8, right3) | Vector128.Equals(left9, right3));
				if (vector3 != Vector128<TValue>.Zero)
				{
					return ComputeFirstIndex(ref searchSpace, ref reference8, vector3);
				}
			}
		}
		return -1;
		IL_0472:
		return (int)num;
		IL_0460:
		return (int)(num + 3);
		IL_0466:
		return (int)(num + 2);
		IL_046c:
		return (int)(num + 1);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static int IndexOfAnyValueType<T>(ref T searchSpace, T value0, T value1, T value2, T value3, int length) where T : struct, INumber<T>
	{
		return IndexOfAnyValueType<T, DontNegate<T>>(ref searchSpace, value0, value1, value2, value3, length);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static int IndexOfAnyExceptValueType<T>(ref T searchSpace, T value0, T value1, T value2, T value3, int length) where T : struct, INumber<T>
	{
		return IndexOfAnyValueType<T, Negate<T>>(ref searchSpace, value0, value1, value2, value3, length);
	}

	private static int IndexOfAnyValueType<TValue, TNegator>(ref TValue searchSpace, TValue value0, TValue value1, TValue value2, TValue value3, int length) where TValue : struct, INumber<TValue> where TNegator : struct, INegator<TValue>
	{
		if (!Vector128.IsHardwareAccelerated || length < Vector128<TValue>.Count)
		{
			nuint num = 0u;
			while (true)
			{
				if (length >= 4)
				{
					length -= 4;
					ref TValue reference = ref Unsafe.Add(ref searchSpace, num);
					TValue val = reference;
					if (TNegator.NegateIfNeeded(val == value0 || val == value1 || val == value2 || val == value3))
					{
						break;
					}
					val = Unsafe.Add(ref reference, 1);
					if (!TNegator.NegateIfNeeded(val == value0 || val == value1 || val == value2 || val == value3))
					{
						val = Unsafe.Add(ref reference, 2);
						if (!TNegator.NegateIfNeeded(val == value0 || val == value1 || val == value2 || val == value3))
						{
							val = Unsafe.Add(ref reference, 3);
							if (!TNegator.NegateIfNeeded(val == value0 || val == value1 || val == value2 || val == value3))
							{
								num += 4;
								continue;
							}
							return (int)(num + 3);
						}
						return (int)(num + 2);
					}
					return (int)(num + 1);
				}
				while (true)
				{
					if (length > 0)
					{
						length--;
						TValue val = Unsafe.Add(ref searchSpace, num);
						if (TNegator.NegateIfNeeded(val == value0 || val == value1 || val == value2 || val == value3))
						{
							break;
						}
						num++;
						continue;
					}
					return -1;
				}
				break;
			}
			return (int)num;
		}
		if (Vector512.IsHardwareAccelerated && length >= Vector512<TValue>.Count)
		{
			Vector512<TValue> left = Vector512.Create(value0);
			Vector512<TValue> left2 = Vector512.Create(value1);
			Vector512<TValue> left3 = Vector512.Create(value2);
			Vector512<TValue> left4 = Vector512.Create(value3);
			ref TValue reference2 = ref searchSpace;
			ref TValue reference3 = ref Unsafe.Add(ref searchSpace, length - Vector512<TValue>.Count);
			do
			{
				Vector512<TValue> right = Vector512.LoadUnsafe(ref reference2);
				Vector512<TValue> vector = TNegator.NegateIfNeeded(Vector512.Equals(left, right) | Vector512.Equals(left2, right) | Vector512.Equals(left3, right) | Vector512.Equals(left4, right));
				if (vector == Vector512<TValue>.Zero)
				{
					reference2 = ref Unsafe.Add(ref reference2, Vector512<TValue>.Count);
					continue;
				}
				return ComputeFirstIndex(ref searchSpace, ref reference2, vector);
			}
			while (!Unsafe.IsAddressGreaterThan(ref reference2, ref reference3));
			if ((uint)length % Vector512<TValue>.Count != 0L)
			{
				Vector512<TValue> right = Vector512.LoadUnsafe(ref reference3);
				Vector512<TValue> vector = TNegator.NegateIfNeeded(Vector512.Equals(left, right) | Vector512.Equals(left2, right) | Vector512.Equals(left3, right) | Vector512.Equals(left4, right));
				if (vector != Vector512<TValue>.Zero)
				{
					return ComputeFirstIndex(ref searchSpace, ref reference3, vector);
				}
			}
		}
		else if (Vector256.IsHardwareAccelerated && length >= Vector256<TValue>.Count)
		{
			Vector256<TValue> left5 = Vector256.Create(value0);
			Vector256<TValue> left6 = Vector256.Create(value1);
			Vector256<TValue> left7 = Vector256.Create(value2);
			Vector256<TValue> left8 = Vector256.Create(value3);
			ref TValue reference4 = ref searchSpace;
			ref TValue reference5 = ref Unsafe.Add(ref searchSpace, length - Vector256<TValue>.Count);
			do
			{
				Vector256<TValue> right2 = Vector256.LoadUnsafe(ref reference4);
				Vector256<TValue> vector2 = TNegator.NegateIfNeeded(Vector256.Equals(left5, right2) | Vector256.Equals(left6, right2) | Vector256.Equals(left7, right2) | Vector256.Equals(left8, right2));
				if (vector2 == Vector256<TValue>.Zero)
				{
					reference4 = ref Unsafe.Add(ref reference4, Vector256<TValue>.Count);
					continue;
				}
				return ComputeFirstIndex(ref searchSpace, ref reference4, vector2);
			}
			while (!Unsafe.IsAddressGreaterThan(ref reference4, ref reference5));
			if ((uint)length % Vector256<TValue>.Count != 0L)
			{
				Vector256<TValue> right2 = Vector256.LoadUnsafe(ref reference5);
				Vector256<TValue> vector2 = TNegator.NegateIfNeeded(Vector256.Equals(left5, right2) | Vector256.Equals(left6, right2) | Vector256.Equals(left7, right2) | Vector256.Equals(left8, right2));
				if (vector2 != Vector256<TValue>.Zero)
				{
					return ComputeFirstIndex(ref searchSpace, ref reference5, vector2);
				}
			}
		}
		else
		{
			Vector128<TValue> left9 = Vector128.Create(value0);
			Vector128<TValue> left10 = Vector128.Create(value1);
			Vector128<TValue> left11 = Vector128.Create(value2);
			Vector128<TValue> left12 = Vector128.Create(value3);
			ref TValue reference6 = ref searchSpace;
			ref TValue reference7 = ref Unsafe.Add(ref searchSpace, length - Vector128<TValue>.Count);
			do
			{
				Vector128<TValue> right3 = Vector128.LoadUnsafe(ref reference6);
				Vector128<TValue> vector3 = TNegator.NegateIfNeeded(Vector128.Equals(left9, right3) | Vector128.Equals(left10, right3) | Vector128.Equals(left11, right3) | Vector128.Equals(left12, right3));
				if (vector3 == Vector128<TValue>.Zero)
				{
					reference6 = ref Unsafe.Add(ref reference6, Vector128<TValue>.Count);
					continue;
				}
				return ComputeFirstIndex(ref searchSpace, ref reference6, vector3);
			}
			while (!Unsafe.IsAddressGreaterThan(ref reference6, ref reference7));
			if ((uint)length % Vector128<TValue>.Count != 0L)
			{
				Vector128<TValue> right3 = Vector128.LoadUnsafe(ref reference7);
				Vector128<TValue> vector3 = TNegator.NegateIfNeeded(Vector128.Equals(left9, right3) | Vector128.Equals(left10, right3) | Vector128.Equals(left11, right3) | Vector128.Equals(left12, right3));
				if (vector3 != Vector128<TValue>.Zero)
				{
					return ComputeFirstIndex(ref searchSpace, ref reference7, vector3);
				}
			}
		}
		return -1;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static int IndexOfAnyValueType<T>(ref T searchSpace, T value0, T value1, T value2, T value3, T value4, int length) where T : struct, INumber<T>
	{
		return IndexOfAnyValueType<T, DontNegate<T>>(ref searchSpace, value0, value1, value2, value3, value4, length);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static int IndexOfAnyExceptValueType<T>(ref T searchSpace, T value0, T value1, T value2, T value3, T value4, int length) where T : struct, INumber<T>
	{
		return IndexOfAnyValueType<T, Negate<T>>(ref searchSpace, value0, value1, value2, value3, value4, length);
	}

	private static int IndexOfAnyValueType<TValue, TNegator>(ref TValue searchSpace, TValue value0, TValue value1, TValue value2, TValue value3, TValue value4, int length) where TValue : struct, INumber<TValue> where TNegator : struct, INegator<TValue>
	{
		if (!Vector128.IsHardwareAccelerated || length < Vector128<TValue>.Count)
		{
			nuint num = 0u;
			while (true)
			{
				if (length >= 4)
				{
					length -= 4;
					ref TValue reference = ref Unsafe.Add(ref searchSpace, num);
					TValue val = reference;
					if (TNegator.NegateIfNeeded(val == value0 || val == value1 || val == value2 || val == value3 || val == value4))
					{
						break;
					}
					val = Unsafe.Add(ref reference, 1);
					if (!TNegator.NegateIfNeeded(val == value0 || val == value1 || val == value2 || val == value3 || val == value4))
					{
						val = Unsafe.Add(ref reference, 2);
						if (!TNegator.NegateIfNeeded(val == value0 || val == value1 || val == value2 || val == value3 || val == value4))
						{
							val = Unsafe.Add(ref reference, 3);
							if (!TNegator.NegateIfNeeded(val == value0 || val == value1 || val == value2 || val == value3 || val == value4))
							{
								num += 4;
								continue;
							}
							return (int)(num + 3);
						}
						return (int)(num + 2);
					}
					return (int)(num + 1);
				}
				while (true)
				{
					if (length > 0)
					{
						length--;
						TValue val = Unsafe.Add(ref searchSpace, num);
						if (TNegator.NegateIfNeeded(val == value0 || val == value1 || val == value2 || val == value3 || val == value4))
						{
							break;
						}
						num++;
						continue;
					}
					return -1;
				}
				break;
			}
			return (int)num;
		}
		if (Vector512.IsHardwareAccelerated && length >= Vector512<TValue>.Count)
		{
			Vector512<TValue> left = Vector512.Create(value0);
			Vector512<TValue> left2 = Vector512.Create(value1);
			Vector512<TValue> left3 = Vector512.Create(value2);
			Vector512<TValue> left4 = Vector512.Create(value3);
			Vector512<TValue> left5 = Vector512.Create(value4);
			ref TValue reference2 = ref searchSpace;
			ref TValue reference3 = ref Unsafe.Add(ref searchSpace, (uint)(length - Vector512<TValue>.Count));
			do
			{
				Vector512<TValue> right = Vector512.LoadUnsafe(ref reference2);
				Vector512<TValue> vector = TNegator.NegateIfNeeded(Vector512.Equals(left, right) | Vector512.Equals(left2, right) | Vector512.Equals(left3, right) | Vector512.Equals(left4, right) | Vector512.Equals(left5, right));
				if (vector == Vector512<TValue>.Zero)
				{
					reference2 = ref Unsafe.Add(ref reference2, Vector512<TValue>.Count);
					continue;
				}
				return ComputeFirstIndex(ref searchSpace, ref reference2, vector);
			}
			while (!Unsafe.IsAddressGreaterThan(ref reference2, ref reference3));
			if ((uint)length % Vector512<TValue>.Count != 0L)
			{
				Vector512<TValue> right = Vector512.LoadUnsafe(ref reference3);
				Vector512<TValue> vector = TNegator.NegateIfNeeded(Vector512.Equals(left, right) | Vector512.Equals(left2, right) | Vector512.Equals(left3, right) | Vector512.Equals(left4, right) | Vector512.Equals(left5, right));
				if (vector != Vector512<TValue>.Zero)
				{
					return ComputeFirstIndex(ref searchSpace, ref reference3, vector);
				}
			}
		}
		else if (Vector256.IsHardwareAccelerated && length >= Vector256<TValue>.Count)
		{
			Vector256<TValue> left6 = Vector256.Create(value0);
			Vector256<TValue> left7 = Vector256.Create(value1);
			Vector256<TValue> left8 = Vector256.Create(value2);
			Vector256<TValue> left9 = Vector256.Create(value3);
			Vector256<TValue> left10 = Vector256.Create(value4);
			ref TValue reference4 = ref searchSpace;
			ref TValue reference5 = ref Unsafe.Add(ref searchSpace, (uint)(length - Vector256<TValue>.Count));
			do
			{
				Vector256<TValue> right2 = Vector256.LoadUnsafe(ref reference4);
				Vector256<TValue> vector2 = TNegator.NegateIfNeeded(Vector256.Equals(left6, right2) | Vector256.Equals(left7, right2) | Vector256.Equals(left8, right2) | Vector256.Equals(left9, right2) | Vector256.Equals(left10, right2));
				if (vector2 == Vector256<TValue>.Zero)
				{
					reference4 = ref Unsafe.Add(ref reference4, Vector256<TValue>.Count);
					continue;
				}
				return ComputeFirstIndex(ref searchSpace, ref reference4, vector2);
			}
			while (!Unsafe.IsAddressGreaterThan(ref reference4, ref reference5));
			if ((uint)length % Vector256<TValue>.Count != 0L)
			{
				Vector256<TValue> right2 = Vector256.LoadUnsafe(ref reference5);
				Vector256<TValue> vector2 = TNegator.NegateIfNeeded(Vector256.Equals(left6, right2) | Vector256.Equals(left7, right2) | Vector256.Equals(left8, right2) | Vector256.Equals(left9, right2) | Vector256.Equals(left10, right2));
				if (vector2 != Vector256<TValue>.Zero)
				{
					return ComputeFirstIndex(ref searchSpace, ref reference5, vector2);
				}
			}
		}
		else
		{
			Vector128<TValue> left11 = Vector128.Create(value0);
			Vector128<TValue> left12 = Vector128.Create(value1);
			Vector128<TValue> left13 = Vector128.Create(value2);
			Vector128<TValue> left14 = Vector128.Create(value3);
			Vector128<TValue> left15 = Vector128.Create(value4);
			ref TValue reference6 = ref searchSpace;
			ref TValue reference7 = ref Unsafe.Add(ref searchSpace, (uint)(length - Vector128<TValue>.Count));
			do
			{
				Vector128<TValue> right3 = Vector128.LoadUnsafe(ref reference6);
				Vector128<TValue> vector3 = TNegator.NegateIfNeeded(Vector128.Equals(left11, right3) | Vector128.Equals(left12, right3) | Vector128.Equals(left13, right3) | Vector128.Equals(left14, right3) | Vector128.Equals(left15, right3));
				if (vector3 == Vector128<TValue>.Zero)
				{
					reference6 = ref Unsafe.Add(ref reference6, Vector128<TValue>.Count);
					continue;
				}
				return ComputeFirstIndex(ref searchSpace, ref reference6, vector3);
			}
			while (!Unsafe.IsAddressGreaterThan(ref reference6, ref reference7));
			if ((uint)length % Vector128<TValue>.Count != 0L)
			{
				Vector128<TValue> right3 = Vector128.LoadUnsafe(ref reference7);
				Vector128<TValue> vector3 = TNegator.NegateIfNeeded(Vector128.Equals(left11, right3) | Vector128.Equals(left12, right3) | Vector128.Equals(left13, right3) | Vector128.Equals(left14, right3) | Vector128.Equals(left15, right3));
				if (vector3 != Vector128<TValue>.Zero)
				{
					return ComputeFirstIndex(ref searchSpace, ref reference7, vector3);
				}
			}
		}
		return -1;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static int LastIndexOfValueType<T>(ref T searchSpace, T value, int length) where T : struct, INumber<T>
	{
		return LastIndexOfValueType<T, DontNegate<T>>(ref searchSpace, value, length);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static int LastIndexOfAnyExceptValueType<T>(ref T searchSpace, T value, int length) where T : struct, INumber<T>
	{
		return LastIndexOfValueType<T, Negate<T>>(ref searchSpace, value, length);
	}

	private static int LastIndexOfValueType<TValue, TNegator>(ref TValue searchSpace, TValue value, int length) where TValue : struct, INumber<TValue> where TNegator : struct, INegator<TValue>
	{
		if (!Vector128.IsHardwareAccelerated || length < Vector128<TValue>.Count)
		{
			nuint num = (nuint)length - (nuint)1u;
			while (true)
			{
				if (length >= 8)
				{
					length -= 8;
					if (TNegator.NegateIfNeeded(Unsafe.Add(ref searchSpace, num) == value))
					{
						break;
					}
					if (!TNegator.NegateIfNeeded(Unsafe.Add(ref searchSpace, num - 1) == value))
					{
						if (!TNegator.NegateIfNeeded(Unsafe.Add(ref searchSpace, num - 2) == value))
						{
							if (!TNegator.NegateIfNeeded(Unsafe.Add(ref searchSpace, num - 3) == value))
							{
								if (!TNegator.NegateIfNeeded(Unsafe.Add(ref searchSpace, num - 4) == value))
								{
									if (!TNegator.NegateIfNeeded(Unsafe.Add(ref searchSpace, num - 5) == value))
									{
										if (!TNegator.NegateIfNeeded(Unsafe.Add(ref searchSpace, num - 6) == value))
										{
											if (!TNegator.NegateIfNeeded(Unsafe.Add(ref searchSpace, num - 7) == value))
											{
												num -= 8;
												continue;
											}
											return (int)(num - 7);
										}
										return (int)(num - 6);
									}
									return (int)(num - 5);
								}
								return (int)(num - 4);
							}
							goto IL_0289;
						}
						goto IL_028f;
					}
					goto IL_0295;
				}
				if (length >= 4)
				{
					length -= 4;
					if (TNegator.NegateIfNeeded(Unsafe.Add(ref searchSpace, num) == value))
					{
						break;
					}
					if (TNegator.NegateIfNeeded(Unsafe.Add(ref searchSpace, num - 1) == value))
					{
						goto IL_0295;
					}
					if (TNegator.NegateIfNeeded(Unsafe.Add(ref searchSpace, num - 2) == value))
					{
						goto IL_028f;
					}
					if (TNegator.NegateIfNeeded(Unsafe.Add(ref searchSpace, num - 3) == value))
					{
						goto IL_0289;
					}
					num -= 4;
				}
				while (true)
				{
					if (length > 0)
					{
						length--;
						if (TNegator.NegateIfNeeded(Unsafe.Add(ref searchSpace, num) == value))
						{
							break;
						}
						num--;
						continue;
					}
					return -1;
				}
				break;
				IL_0295:
				return (int)(num - 1);
				IL_0289:
				return (int)(num - 3);
				IL_028f:
				return (int)(num - 2);
			}
			return (int)num;
		}
		if (Vector512.IsHardwareAccelerated && length >= Vector512<TValue>.Count)
		{
			Vector512<TValue> left = Vector512.Create(value);
			Vector512<TValue> right;
			for (nint num2 = length - Vector512<TValue>.Count; num2 > 0; num2 -= Vector512<TValue>.Count)
			{
				right = Vector512.LoadUnsafe(ref searchSpace, (nuint)num2);
				if (TNegator.HasMatch(left, right))
				{
					return ComputeLastIndex(num2, TNegator.GetMatchMask(left, right));
				}
			}
			right = Vector512.LoadUnsafe(ref searchSpace);
			if (TNegator.HasMatch(left, right))
			{
				return ComputeLastIndex(0, TNegator.GetMatchMask(left, right));
			}
		}
		else if (Vector256.IsHardwareAccelerated && length >= Vector256<TValue>.Count)
		{
			Vector256<TValue> left2 = Vector256.Create(value);
			nint num3 = length - Vector256<TValue>.Count;
			Vector256<TValue> vector;
			while (num3 > 0)
			{
				vector = TNegator.NegateIfNeeded(Vector256.Equals(left2, Vector256.LoadUnsafe(ref searchSpace, (nuint)num3)));
				if (vector == Vector256<TValue>.Zero)
				{
					num3 -= Vector256<TValue>.Count;
					continue;
				}
				return ComputeLastIndex(num3, vector);
			}
			vector = TNegator.NegateIfNeeded(Vector256.Equals(left2, Vector256.LoadUnsafe(ref searchSpace)));
			if (vector != Vector256<TValue>.Zero)
			{
				return ComputeLastIndex(0, vector);
			}
		}
		else
		{
			Vector128<TValue> left3 = Vector128.Create(value);
			nint num4 = length - Vector128<TValue>.Count;
			Vector128<TValue> vector2;
			while (num4 > 0)
			{
				vector2 = TNegator.NegateIfNeeded(Vector128.Equals(left3, Vector128.LoadUnsafe(ref searchSpace, (nuint)num4)));
				if (vector2 == Vector128<TValue>.Zero)
				{
					num4 -= Vector128<TValue>.Count;
					continue;
				}
				return ComputeLastIndex(num4, vector2);
			}
			vector2 = TNegator.NegateIfNeeded(Vector128.Equals(left3, Vector128.LoadUnsafe(ref searchSpace)));
			if (vector2 != Vector128<TValue>.Zero)
			{
				return ComputeLastIndex(0, vector2);
			}
		}
		return -1;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static int LastIndexOfAnyValueType<T>(ref T searchSpace, T value0, T value1, int length) where T : struct, INumber<T>
	{
		return LastIndexOfAnyValueType<T, DontNegate<T>>(ref searchSpace, value0, value1, length);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static int LastIndexOfAnyExceptValueType<T>(ref T searchSpace, T value0, T value1, int length) where T : struct, INumber<T>
	{
		return LastIndexOfAnyValueType<T, Negate<T>>(ref searchSpace, value0, value1, length);
	}

	private static int LastIndexOfAnyValueType<TValue, TNegator>(ref TValue searchSpace, TValue value0, TValue value1, int length) where TValue : struct, INumber<TValue> where TNegator : struct, INegator<TValue>
	{
		nuint num;
		if (!Vector128.IsHardwareAccelerated || length < Vector128<TValue>.Count)
		{
			num = (nuint)length - (nuint)1u;
			if (typeof(TValue) == typeof(byte))
			{
				while (length >= 8)
				{
					length -= 8;
					ref TValue reference = ref Unsafe.Add(ref searchSpace, num);
					TValue val = reference;
					if (!TNegator.NegateIfNeeded(val == value0 || val == value1))
					{
						val = Unsafe.Add(ref reference, -1);
						if (!TNegator.NegateIfNeeded(val == value0 || val == value1))
						{
							val = Unsafe.Add(ref reference, -2);
							if (!TNegator.NegateIfNeeded(val == value0 || val == value1))
							{
								val = Unsafe.Add(ref reference, -3);
								if (!TNegator.NegateIfNeeded(val == value0 || val == value1))
								{
									val = Unsafe.Add(ref reference, -4);
									if (!TNegator.NegateIfNeeded(val == value0 || val == value1))
									{
										val = Unsafe.Add(ref reference, -5);
										if (!TNegator.NegateIfNeeded(val == value0 || val == value1))
										{
											val = Unsafe.Add(ref reference, -6);
											if (!TNegator.NegateIfNeeded(val == value0 || val == value1))
											{
												val = Unsafe.Add(ref reference, -7);
												if (!TNegator.NegateIfNeeded(val == value0 || val == value1))
												{
													num -= 8;
													continue;
												}
												return (int)(num - 7);
											}
											return (int)(num - 6);
										}
										return (int)(num - 5);
									}
									return (int)(num - 4);
								}
								goto IL_039e;
							}
							goto IL_03a4;
						}
						goto IL_03aa;
					}
					goto IL_03b0;
				}
			}
			while (true)
			{
				if (length >= 4)
				{
					length -= 4;
					ref TValue reference2 = ref Unsafe.Add(ref searchSpace, num);
					TValue val = reference2;
					if (TNegator.NegateIfNeeded(val == value0 || val == value1))
					{
						break;
					}
					val = Unsafe.Add(ref reference2, -1);
					if (!TNegator.NegateIfNeeded(val == value0 || val == value1))
					{
						val = Unsafe.Add(ref reference2, -2);
						if (!TNegator.NegateIfNeeded(val == value0 || val == value1))
						{
							val = Unsafe.Add(ref reference2, -3);
							if (!TNegator.NegateIfNeeded(val == value0 || val == value1))
							{
								num -= 4;
								continue;
							}
							goto IL_039e;
						}
						goto IL_03a4;
					}
					goto IL_03aa;
				}
				while (true)
				{
					if (length > 0)
					{
						length--;
						TValue val = Unsafe.Add(ref searchSpace, num);
						if (TNegator.NegateIfNeeded(val == value0 || val == value1))
						{
							break;
						}
						num--;
						continue;
					}
					return -1;
				}
				break;
			}
			goto IL_03b0;
		}
		if (Vector512.IsHardwareAccelerated && length >= Vector512<TValue>.Count)
		{
			Vector512<TValue> right = Vector512.Create(value0);
			Vector512<TValue> right2 = Vector512.Create(value1);
			nint num2 = length - Vector512<TValue>.Count;
			Vector512<TValue> left;
			Vector512<TValue> vector;
			while (num2 > 0)
			{
				left = Vector512.LoadUnsafe(ref searchSpace, (nuint)num2);
				vector = TNegator.NegateIfNeeded(Vector512.Equals(left, right) | Vector512.Equals(left, right2));
				if (vector == Vector512<TValue>.Zero)
				{
					num2 -= Vector512<TValue>.Count;
					continue;
				}
				return ComputeLastIndex(num2, vector);
			}
			left = Vector512.LoadUnsafe(ref searchSpace);
			vector = TNegator.NegateIfNeeded(Vector512.Equals(left, right) | Vector512.Equals(left, right2));
			if (vector != Vector512<TValue>.Zero)
			{
				return ComputeLastIndex(0, vector);
			}
		}
		else if (Vector256.IsHardwareAccelerated && length >= Vector256<TValue>.Count)
		{
			Vector256<TValue> right3 = Vector256.Create(value0);
			Vector256<TValue> right4 = Vector256.Create(value1);
			nint num3 = length - Vector256<TValue>.Count;
			Vector256<TValue> left2;
			Vector256<TValue> vector2;
			while (num3 > 0)
			{
				left2 = Vector256.LoadUnsafe(ref searchSpace, (nuint)num3);
				vector2 = TNegator.NegateIfNeeded(Vector256.Equals(left2, right3) | Vector256.Equals(left2, right4));
				if (vector2 == Vector256<TValue>.Zero)
				{
					num3 -= Vector256<TValue>.Count;
					continue;
				}
				return ComputeLastIndex(num3, vector2);
			}
			left2 = Vector256.LoadUnsafe(ref searchSpace);
			vector2 = TNegator.NegateIfNeeded(Vector256.Equals(left2, right3) | Vector256.Equals(left2, right4));
			if (vector2 != Vector256<TValue>.Zero)
			{
				return ComputeLastIndex(0, vector2);
			}
		}
		else
		{
			Vector128<TValue> right5 = Vector128.Create(value0);
			Vector128<TValue> right6 = Vector128.Create(value1);
			nint num4 = length - Vector128<TValue>.Count;
			Vector128<TValue> left3;
			Vector128<TValue> vector3;
			while (num4 > 0)
			{
				left3 = Vector128.LoadUnsafe(ref searchSpace, (nuint)num4);
				vector3 = TNegator.NegateIfNeeded(Vector128.Equals(left3, right5) | Vector128.Equals(left3, right6));
				if (vector3 == Vector128<TValue>.Zero)
				{
					num4 -= Vector128<TValue>.Count;
					continue;
				}
				return ComputeLastIndex(num4, vector3);
			}
			left3 = Vector128.LoadUnsafe(ref searchSpace);
			vector3 = TNegator.NegateIfNeeded(Vector128.Equals(left3, right5) | Vector128.Equals(left3, right6));
			if (vector3 != Vector128<TValue>.Zero)
			{
				return ComputeLastIndex(0, vector3);
			}
		}
		return -1;
		IL_039e:
		return (int)(num - 3);
		IL_03aa:
		return (int)(num - 1);
		IL_03b0:
		return (int)num;
		IL_03a4:
		return (int)(num - 2);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static int LastIndexOfAnyValueType<T>(ref T searchSpace, T value0, T value1, T value2, int length) where T : struct, INumber<T>
	{
		return LastIndexOfAnyValueType<T, DontNegate<T>>(ref searchSpace, value0, value1, value2, length);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static int LastIndexOfAnyExceptValueType<T>(ref T searchSpace, T value0, T value1, T value2, int length) where T : struct, INumber<T>
	{
		return LastIndexOfAnyValueType<T, Negate<T>>(ref searchSpace, value0, value1, value2, length);
	}

	private static int LastIndexOfAnyValueType<TValue, TNegator>(ref TValue searchSpace, TValue value0, TValue value1, TValue value2, int length) where TValue : struct, INumber<TValue> where TNegator : struct, INegator<TValue>
	{
		nuint num;
		if (!Vector128.IsHardwareAccelerated || length < Vector128<TValue>.Count)
		{
			num = (nuint)length - (nuint)1u;
			if (typeof(TValue) == typeof(byte))
			{
				while (length >= 8)
				{
					length -= 8;
					ref TValue reference = ref Unsafe.Add(ref searchSpace, num);
					TValue val = reference;
					if (!TNegator.NegateIfNeeded(val == value0 || val == value1 || val == value2))
					{
						val = Unsafe.Add(ref reference, -1);
						if (!TNegator.NegateIfNeeded(val == value0 || val == value1 || val == value2))
						{
							val = Unsafe.Add(ref reference, -2);
							if (!TNegator.NegateIfNeeded(val == value0 || val == value1 || val == value2))
							{
								val = Unsafe.Add(ref reference, -3);
								if (!TNegator.NegateIfNeeded(val == value0 || val == value1 || val == value2))
								{
									val = Unsafe.Add(ref reference, -4);
									if (!TNegator.NegateIfNeeded(val == value0 || val == value1 || val == value2))
									{
										val = Unsafe.Add(ref reference, -5);
										if (!TNegator.NegateIfNeeded(val == value0 || val == value1 || val == value2))
										{
											val = Unsafe.Add(ref reference, -6);
											if (!TNegator.NegateIfNeeded(val == value0 || val == value1 || val == value2))
											{
												val = Unsafe.Add(ref reference, -7);
												if (!TNegator.NegateIfNeeded(val == value0 || val == value1 || val == value2))
												{
													num -= 8;
													continue;
												}
												return (int)(num - 7);
											}
											return (int)(num - 6);
										}
										return (int)(num - 5);
									}
									return (int)(num - 4);
								}
								goto IL_046c;
							}
							goto IL_0472;
						}
						goto IL_0478;
					}
					goto IL_047e;
				}
			}
			while (true)
			{
				if (length >= 4)
				{
					length -= 4;
					ref TValue reference2 = ref Unsafe.Add(ref searchSpace, num);
					TValue val = reference2;
					if (TNegator.NegateIfNeeded(val == value0 || val == value1 || val == value2))
					{
						break;
					}
					val = Unsafe.Add(ref reference2, -1);
					if (!TNegator.NegateIfNeeded(val == value0 || val == value1 || val == value2))
					{
						val = Unsafe.Add(ref reference2, -2);
						if (!TNegator.NegateIfNeeded(val == value0 || val == value1 || val == value2))
						{
							val = Unsafe.Add(ref reference2, -3);
							if (!TNegator.NegateIfNeeded(val == value0 || val == value1 || val == value2))
							{
								num -= 4;
								continue;
							}
							goto IL_046c;
						}
						goto IL_0472;
					}
					goto IL_0478;
				}
				while (true)
				{
					if (length > 0)
					{
						length--;
						TValue val = Unsafe.Add(ref searchSpace, num);
						if (TNegator.NegateIfNeeded(val == value0 || val == value1 || val == value2))
						{
							break;
						}
						num--;
						continue;
					}
					return -1;
				}
				break;
			}
			goto IL_047e;
		}
		if (Vector512.IsHardwareAccelerated && length >= Vector512<TValue>.Count)
		{
			Vector512<TValue> right = Vector512.Create(value0);
			Vector512<TValue> right2 = Vector512.Create(value1);
			Vector512<TValue> right3 = Vector512.Create(value2);
			nint num2 = length - Vector512<TValue>.Count;
			Vector512<TValue> left;
			Vector512<TValue> vector;
			while (num2 > 0)
			{
				left = Vector512.LoadUnsafe(ref searchSpace, (nuint)num2);
				vector = TNegator.NegateIfNeeded(Vector512.Equals(left, right) | Vector512.Equals(left, right2) | Vector512.Equals(left, right3));
				if (vector == Vector512<TValue>.Zero)
				{
					num2 -= Vector512<TValue>.Count;
					continue;
				}
				return ComputeLastIndex(num2, vector);
			}
			left = Vector512.LoadUnsafe(ref searchSpace);
			vector = TNegator.NegateIfNeeded(Vector512.Equals(left, right) | Vector512.Equals(left, right2) | Vector512.Equals(left, right3));
			if (vector != Vector512<TValue>.Zero)
			{
				return ComputeLastIndex(0, vector);
			}
		}
		else if (Vector256.IsHardwareAccelerated && length >= Vector256<TValue>.Count)
		{
			Vector256<TValue> right4 = Vector256.Create(value0);
			Vector256<TValue> right5 = Vector256.Create(value1);
			Vector256<TValue> right6 = Vector256.Create(value2);
			nint num3 = length - Vector256<TValue>.Count;
			Vector256<TValue> left2;
			Vector256<TValue> vector2;
			while (num3 > 0)
			{
				left2 = Vector256.LoadUnsafe(ref searchSpace, (nuint)num3);
				vector2 = TNegator.NegateIfNeeded(Vector256.Equals(left2, right4) | Vector256.Equals(left2, right5) | Vector256.Equals(left2, right6));
				if (vector2 == Vector256<TValue>.Zero)
				{
					num3 -= Vector256<TValue>.Count;
					continue;
				}
				return ComputeLastIndex(num3, vector2);
			}
			left2 = Vector256.LoadUnsafe(ref searchSpace);
			vector2 = TNegator.NegateIfNeeded(Vector256.Equals(left2, right4) | Vector256.Equals(left2, right5) | Vector256.Equals(left2, right6));
			if (vector2 != Vector256<TValue>.Zero)
			{
				return ComputeLastIndex(0, vector2);
			}
		}
		else
		{
			Vector128<TValue> right7 = Vector128.Create(value0);
			Vector128<TValue> right8 = Vector128.Create(value1);
			Vector128<TValue> right9 = Vector128.Create(value2);
			nint num4 = length - Vector128<TValue>.Count;
			Vector128<TValue> left3;
			Vector128<TValue> vector3;
			while (num4 > 0)
			{
				left3 = Vector128.LoadUnsafe(ref searchSpace, (nuint)num4);
				vector3 = TNegator.NegateIfNeeded(Vector128.Equals(left3, right7) | Vector128.Equals(left3, right8) | Vector128.Equals(left3, right9));
				if (vector3 == Vector128<TValue>.Zero)
				{
					num4 -= Vector128<TValue>.Count;
					continue;
				}
				return ComputeLastIndex(num4, vector3);
			}
			left3 = Vector128.LoadUnsafe(ref searchSpace);
			vector3 = TNegator.NegateIfNeeded(Vector128.Equals(left3, right7) | Vector128.Equals(left3, right8) | Vector128.Equals(left3, right9));
			if (vector3 != Vector128<TValue>.Zero)
			{
				return ComputeLastIndex(0, vector3);
			}
		}
		return -1;
		IL_0478:
		return (int)(num - 1);
		IL_046c:
		return (int)(num - 3);
		IL_0472:
		return (int)(num - 2);
		IL_047e:
		return (int)num;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static int LastIndexOfAnyValueType<T>(ref T searchSpace, T value0, T value1, T value2, T value3, int length) where T : struct, INumber<T>
	{
		return LastIndexOfAnyValueType<T, DontNegate<T>>(ref searchSpace, value0, value1, value2, value3, length);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static int LastIndexOfAnyExceptValueType<T>(ref T searchSpace, T value0, T value1, T value2, T value3, int length) where T : struct, INumber<T>
	{
		return LastIndexOfAnyValueType<T, Negate<T>>(ref searchSpace, value0, value1, value2, value3, length);
	}

	private static int LastIndexOfAnyValueType<TValue, TNegator>(ref TValue searchSpace, TValue value0, TValue value1, TValue value2, TValue value3, int length) where TValue : struct, INumber<TValue> where TNegator : struct, INegator<TValue>
	{
		if (!Vector128.IsHardwareAccelerated || length < Vector128<TValue>.Count)
		{
			nuint num = (nuint)length - (nuint)1u;
			while (true)
			{
				if (length >= 4)
				{
					length -= 4;
					ref TValue reference = ref Unsafe.Add(ref searchSpace, num);
					TValue val = reference;
					if (TNegator.NegateIfNeeded(val == value0 || val == value1 || val == value2 || val == value3))
					{
						break;
					}
					val = Unsafe.Add(ref reference, -1);
					if (!TNegator.NegateIfNeeded(val == value0 || val == value1 || val == value2 || val == value3))
					{
						val = Unsafe.Add(ref reference, -2);
						if (!TNegator.NegateIfNeeded(val == value0 || val == value1 || val == value2 || val == value3))
						{
							val = Unsafe.Add(ref reference, -3);
							if (!TNegator.NegateIfNeeded(val == value0 || val == value1 || val == value2 || val == value3))
							{
								num -= 4;
								continue;
							}
							return (int)(num - 3);
						}
						return (int)(num - 2);
					}
					return (int)(num - 1);
				}
				while (true)
				{
					if (length > 0)
					{
						length--;
						TValue val = Unsafe.Add(ref searchSpace, num);
						if (TNegator.NegateIfNeeded(val == value0 || val == value1 || val == value2 || val == value3))
						{
							break;
						}
						num--;
						continue;
					}
					return -1;
				}
				break;
			}
			return (int)num;
		}
		if (Vector512.IsHardwareAccelerated && length >= Vector512<TValue>.Count)
		{
			Vector512<TValue> right = Vector512.Create(value0);
			Vector512<TValue> right2 = Vector512.Create(value1);
			Vector512<TValue> right3 = Vector512.Create(value2);
			Vector512<TValue> right4 = Vector512.Create(value3);
			nint num2 = length - Vector512<TValue>.Count;
			Vector512<TValue> left;
			Vector512<TValue> vector;
			while (num2 > 0)
			{
				left = Vector512.LoadUnsafe(ref searchSpace, (nuint)num2);
				vector = TNegator.NegateIfNeeded(Vector512.Equals(left, right) | Vector512.Equals(left, right2) | Vector512.Equals(left, right3) | Vector512.Equals(left, right4));
				if (vector == Vector512<TValue>.Zero)
				{
					num2 -= Vector512<TValue>.Count;
					continue;
				}
				return ComputeLastIndex(num2, vector);
			}
			left = Vector512.LoadUnsafe(ref searchSpace);
			vector = TNegator.NegateIfNeeded(Vector512.Equals(left, right) | Vector512.Equals(left, right2) | Vector512.Equals(left, right3) | Vector512.Equals(left, right4));
			if (vector != Vector512<TValue>.Zero)
			{
				return ComputeLastIndex(0, vector);
			}
		}
		else if (Vector256.IsHardwareAccelerated && length >= Vector256<TValue>.Count)
		{
			Vector256<TValue> right5 = Vector256.Create(value0);
			Vector256<TValue> right6 = Vector256.Create(value1);
			Vector256<TValue> right7 = Vector256.Create(value2);
			Vector256<TValue> right8 = Vector256.Create(value3);
			nint num3 = length - Vector256<TValue>.Count;
			Vector256<TValue> left2;
			Vector256<TValue> vector2;
			while (num3 > 0)
			{
				left2 = Vector256.LoadUnsafe(ref searchSpace, (nuint)num3);
				vector2 = TNegator.NegateIfNeeded(Vector256.Equals(left2, right5) | Vector256.Equals(left2, right6) | Vector256.Equals(left2, right7) | Vector256.Equals(left2, right8));
				if (vector2 == Vector256<TValue>.Zero)
				{
					num3 -= Vector256<TValue>.Count;
					continue;
				}
				return ComputeLastIndex(num3, vector2);
			}
			left2 = Vector256.LoadUnsafe(ref searchSpace);
			vector2 = TNegator.NegateIfNeeded(Vector256.Equals(left2, right5) | Vector256.Equals(left2, right6) | Vector256.Equals(left2, right7) | Vector256.Equals(left2, right8));
			if (vector2 != Vector256<TValue>.Zero)
			{
				return ComputeLastIndex(0, vector2);
			}
		}
		else
		{
			Vector128<TValue> right9 = Vector128.Create(value0);
			Vector128<TValue> right10 = Vector128.Create(value1);
			Vector128<TValue> right11 = Vector128.Create(value2);
			Vector128<TValue> right12 = Vector128.Create(value3);
			nint num4 = length - Vector128<TValue>.Count;
			Vector128<TValue> left3;
			Vector128<TValue> vector3;
			while (num4 > 0)
			{
				left3 = Vector128.LoadUnsafe(ref searchSpace, (nuint)num4);
				vector3 = TNegator.NegateIfNeeded(Vector128.Equals(left3, right9) | Vector128.Equals(left3, right10) | Vector128.Equals(left3, right11) | Vector128.Equals(left3, right12));
				if (vector3 == Vector128<TValue>.Zero)
				{
					num4 -= Vector128<TValue>.Count;
					continue;
				}
				return ComputeLastIndex(num4, vector3);
			}
			left3 = Vector128.LoadUnsafe(ref searchSpace);
			vector3 = TNegator.NegateIfNeeded(Vector128.Equals(left3, right9) | Vector128.Equals(left3, right10) | Vector128.Equals(left3, right11) | Vector128.Equals(left3, right12));
			if (vector3 != Vector128<TValue>.Zero)
			{
				return ComputeLastIndex(0, vector3);
			}
		}
		return -1;
	}

	public static void Replace<T>(ref T src, ref T dst, T oldValue, T newValue, nuint length) where T : IEquatable<T>
	{
		if (default(T) != null || oldValue != null)
		{
			for (nuint num = 0u; num < length; num++)
			{
				T val = Unsafe.Add(ref src, num);
				Unsafe.Add(ref dst, num) = (oldValue.Equals(val) ? newValue : val);
			}
		}
		else
		{
			for (nuint num2 = 0u; num2 < length; num2++)
			{
				T val2 = Unsafe.Add(ref src, num2);
				Unsafe.Add(ref dst, num2) = ((val2 == null) ? newValue : val2);
			}
		}
	}

	public static void ReplaceValueType<T>(ref T src, ref T dst, T oldValue, T newValue, nuint length) where T : struct
	{
		if (!Vector128.IsHardwareAccelerated || length < (uint)Vector128<T>.Count)
		{
			for (nuint num = 0u; num < length; num++)
			{
				T val = Unsafe.Add(ref src, num);
				Unsafe.Add(ref dst, num) = (EqualityComparer<T>.Default.Equals(val, oldValue) ? newValue : val);
			}
			return;
		}
		nuint num2 = 0u;
		if (!Vector256.IsHardwareAccelerated || length < (uint)Vector256<T>.Count)
		{
			nuint num3 = length - (uint)Vector128<T>.Count;
			Vector128<T> left = Vector128.Create(oldValue);
			Vector128<T> left2 = Vector128.Create(newValue);
			Vector128<T> right;
			Vector128<T> condition;
			Vector128<T> source;
			do
			{
				right = Vector128.LoadUnsafe(ref src, num2);
				condition = Vector128.Equals(left, right);
				source = Vector128.ConditionalSelect(condition, left2, right);
				source.StoreUnsafe(ref dst, num2);
				num2 += (uint)Vector128<T>.Count;
			}
			while (num2 < num3);
			right = Vector128.LoadUnsafe(ref src, num3);
			condition = Vector128.Equals(left, right);
			source = Vector128.ConditionalSelect(condition, left2, right);
			source.StoreUnsafe(ref dst, num3);
		}
		else if (!Vector512.IsHardwareAccelerated || length < (uint)Vector512<T>.Count)
		{
			nuint num4 = length - (uint)Vector256<T>.Count;
			Vector256<T> left3 = Vector256.Create(oldValue);
			Vector256<T> left4 = Vector256.Create(newValue);
			Vector256<T> right2;
			Vector256<T> condition2;
			Vector256<T> source2;
			do
			{
				right2 = Vector256.LoadUnsafe(ref src, num2);
				condition2 = Vector256.Equals(left3, right2);
				source2 = Vector256.ConditionalSelect(condition2, left4, right2);
				source2.StoreUnsafe(ref dst, num2);
				num2 += (uint)Vector256<T>.Count;
			}
			while (num2 < num4);
			right2 = Vector256.LoadUnsafe(ref src, num4);
			condition2 = Vector256.Equals(left3, right2);
			source2 = Vector256.ConditionalSelect(condition2, left4, right2);
			source2.StoreUnsafe(ref dst, num4);
		}
		else
		{
			nuint num5 = length - (uint)Vector512<T>.Count;
			Vector512<T> left5 = Vector512.Create(oldValue);
			Vector512<T> left6 = Vector512.Create(newValue);
			Vector512<T> right3;
			Vector512<T> condition3;
			Vector512<T> source3;
			do
			{
				right3 = Vector512.LoadUnsafe(ref src, num2);
				condition3 = Vector512.Equals(left5, right3);
				source3 = Vector512.ConditionalSelect(condition3, left6, right3);
				source3.StoreUnsafe(ref dst, num2);
				num2 += (uint)Vector512<T>.Count;
			}
			while (num2 < num5);
			right3 = Vector512.LoadUnsafe(ref src, num5);
			condition3 = Vector512.Equals(left5, right3);
			source3 = Vector512.ConditionalSelect(condition3, left6, right3);
			source3.StoreUnsafe(ref dst, num5);
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static int LastIndexOfAnyValueType<T>(ref T searchSpace, T value0, T value1, T value2, T value3, T value4, int length) where T : struct, INumber<T>
	{
		return LastIndexOfAnyValueType<T, DontNegate<T>>(ref searchSpace, value0, value1, value2, value3, value4, length);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static int LastIndexOfAnyExceptValueType<T>(ref T searchSpace, T value0, T value1, T value2, T value3, T value4, int length) where T : struct, INumber<T>
	{
		return LastIndexOfAnyValueType<T, Negate<T>>(ref searchSpace, value0, value1, value2, value3, value4, length);
	}

	private static int LastIndexOfAnyValueType<TValue, TNegator>(ref TValue searchSpace, TValue value0, TValue value1, TValue value2, TValue value3, TValue value4, int length) where TValue : struct, INumber<TValue> where TNegator : struct, INegator<TValue>
	{
		if (!Vector128.IsHardwareAccelerated || length < Vector128<TValue>.Count)
		{
			nuint num = (nuint)length - (nuint)1u;
			while (length >= 4)
			{
				length -= 4;
				ref TValue reference = ref Unsafe.Add(ref searchSpace, num);
				TValue val = reference;
				if (TNegator.NegateIfNeeded(val == value0 || val == value1 || val == value2 || val == value3 || val == value4))
				{
					return (int)num;
				}
				val = Unsafe.Add(ref reference, -1);
				if (TNegator.NegateIfNeeded(val == value0 || val == value1 || val == value2 || val == value3 || val == value4))
				{
					return (int)num - 1;
				}
				val = Unsafe.Add(ref reference, -2);
				if (TNegator.NegateIfNeeded(val == value0 || val == value1 || val == value2 || val == value3 || val == value4))
				{
					return (int)num - 2;
				}
				val = Unsafe.Add(ref reference, -3);
				if (TNegator.NegateIfNeeded(val == value0 || val == value1 || val == value2 || val == value3 || val == value4))
				{
					return (int)num - 3;
				}
				num -= 4;
			}
			while (length > 0)
			{
				length--;
				TValue val = Unsafe.Add(ref searchSpace, num);
				if (TNegator.NegateIfNeeded(val == value0 || val == value1 || val == value2 || val == value3 || val == value4))
				{
					return (int)num;
				}
				num--;
			}
		}
		else if (Vector512.IsHardwareAccelerated && length >= Vector512<TValue>.Count)
		{
			Vector512<TValue> right = Vector512.Create(value0);
			Vector512<TValue> right2 = Vector512.Create(value1);
			Vector512<TValue> right3 = Vector512.Create(value2);
			Vector512<TValue> right4 = Vector512.Create(value3);
			Vector512<TValue> right5 = Vector512.Create(value4);
			nint num2 = length - Vector512<TValue>.Count;
			Vector512<TValue> left;
			Vector512<TValue> vector;
			while (num2 > 0)
			{
				left = Vector512.LoadUnsafe(ref searchSpace, (nuint)num2);
				vector = TNegator.NegateIfNeeded(Vector512.Equals(left, right) | Vector512.Equals(left, right2) | Vector512.Equals(left, right3) | Vector512.Equals(left, right4) | Vector512.Equals(left, right5));
				if (vector == Vector512<TValue>.Zero)
				{
					num2 -= Vector512<TValue>.Count;
					continue;
				}
				return ComputeLastIndex(num2, vector);
			}
			left = Vector512.LoadUnsafe(ref searchSpace);
			vector = TNegator.NegateIfNeeded(Vector512.Equals(left, right) | Vector512.Equals(left, right2) | Vector512.Equals(left, right3) | Vector512.Equals(left, right4) | Vector512.Equals(left, right5));
			if (vector != Vector512<TValue>.Zero)
			{
				return ComputeLastIndex(0, vector);
			}
		}
		else if (Vector256.IsHardwareAccelerated && length >= Vector256<TValue>.Count)
		{
			Vector256<TValue> right6 = Vector256.Create(value0);
			Vector256<TValue> right7 = Vector256.Create(value1);
			Vector256<TValue> right8 = Vector256.Create(value2);
			Vector256<TValue> right9 = Vector256.Create(value3);
			Vector256<TValue> right10 = Vector256.Create(value4);
			nint num3 = length - Vector256<TValue>.Count;
			Vector256<TValue> left2;
			Vector256<TValue> vector2;
			while (num3 > 0)
			{
				left2 = Vector256.LoadUnsafe(ref searchSpace, (nuint)num3);
				vector2 = TNegator.NegateIfNeeded(Vector256.Equals(left2, right6) | Vector256.Equals(left2, right7) | Vector256.Equals(left2, right8) | Vector256.Equals(left2, right9) | Vector256.Equals(left2, right10));
				if (vector2 == Vector256<TValue>.Zero)
				{
					num3 -= Vector256<TValue>.Count;
					continue;
				}
				return ComputeLastIndex(num3, vector2);
			}
			left2 = Vector256.LoadUnsafe(ref searchSpace);
			vector2 = TNegator.NegateIfNeeded(Vector256.Equals(left2, right6) | Vector256.Equals(left2, right7) | Vector256.Equals(left2, right8) | Vector256.Equals(left2, right9) | Vector256.Equals(left2, right10));
			if (vector2 != Vector256<TValue>.Zero)
			{
				return ComputeLastIndex(0, vector2);
			}
		}
		else
		{
			Vector128<TValue> right11 = Vector128.Create(value0);
			Vector128<TValue> right12 = Vector128.Create(value1);
			Vector128<TValue> right13 = Vector128.Create(value2);
			Vector128<TValue> right14 = Vector128.Create(value3);
			Vector128<TValue> right15 = Vector128.Create(value4);
			nint num4 = length - Vector128<TValue>.Count;
			Vector128<TValue> left3;
			Vector128<TValue> vector3;
			while (num4 > 0)
			{
				left3 = Vector128.LoadUnsafe(ref searchSpace, (nuint)num4);
				vector3 = TNegator.NegateIfNeeded(Vector128.Equals(left3, right11) | Vector128.Equals(left3, right12) | Vector128.Equals(left3, right13) | Vector128.Equals(left3, right14) | Vector128.Equals(left3, right15));
				if (vector3 == Vector128<TValue>.Zero)
				{
					num4 -= Vector128<TValue>.Count;
					continue;
				}
				return ComputeLastIndex(num4, vector3);
			}
			left3 = Vector128.LoadUnsafe(ref searchSpace);
			vector3 = TNegator.NegateIfNeeded(Vector128.Equals(left3, right11) | Vector128.Equals(left3, right12) | Vector128.Equals(left3, right13) | Vector128.Equals(left3, right14) | Vector128.Equals(left3, right15));
			if (vector3 != Vector128<TValue>.Zero)
			{
				return ComputeLastIndex(0, vector3);
			}
		}
		return -1;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static int ComputeFirstIndex<T>(ref T searchSpace, ref T current, Vector128<T> equals) where T : struct
	{
		uint value = equals.ExtractMostSignificantBits();
		int num = BitOperations.TrailingZeroCount(value);
		return num + (int)((nuint)Unsafe.ByteOffset(ref searchSpace, ref current) / (nuint)Unsafe.SizeOf<T>());
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static int ComputeFirstIndex<T>(ref T searchSpace, ref T current, Vector256<T> equals) where T : struct
	{
		uint value = equals.ExtractMostSignificantBits();
		int num = BitOperations.TrailingZeroCount(value);
		return num + (int)((nuint)Unsafe.ByteOffset(ref searchSpace, ref current) / (nuint)Unsafe.SizeOf<T>());
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static int ComputeFirstIndex<T>(ref T searchSpace, ref T current, Vector512<T> equals) where T : struct
	{
		ulong value = equals.ExtractMostSignificantBits();
		int num = BitOperations.TrailingZeroCount(value);
		return num + (int)((nuint)Unsafe.ByteOffset(ref searchSpace, ref current) / (nuint)Unsafe.SizeOf<T>());
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static int ComputeLastIndex<T>(nint offset, Vector128<T> equals) where T : struct
	{
		uint value = equals.ExtractMostSignificantBits();
		int num = 31 - BitOperations.LeadingZeroCount(value);
		return (int)offset + num;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static int ComputeLastIndex<T>(nint offset, Vector256<T> equals) where T : struct
	{
		uint value = equals.ExtractMostSignificantBits();
		int num = 31 - BitOperations.LeadingZeroCount(value);
		return (int)offset + num;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static int ComputeLastIndex<T>(nint offset, Vector512<T> equals) where T : struct
	{
		ulong value = equals.ExtractMostSignificantBits();
		int num = 63 - BitOperations.LeadingZeroCount(value);
		return (int)offset + num;
	}

	internal static int IndexOfAnyInRange<T>(ref T searchSpace, T lowInclusive, T highInclusive, int length) where T : IComparable<T>
	{
		for (int i = 0; i < length; i++)
		{
			ref T reference = ref Unsafe.Add(ref searchSpace, i);
			if (lowInclusive.CompareTo(reference) <= 0 && highInclusive.CompareTo(reference) >= 0)
			{
				return i;
			}
		}
		return -1;
	}

	internal static int IndexOfAnyExceptInRange<T>(ref T searchSpace, T lowInclusive, T highInclusive, int length) where T : IComparable<T>
	{
		for (int i = 0; i < length; i++)
		{
			ref T reference = ref Unsafe.Add(ref searchSpace, i);
			if (lowInclusive.CompareTo(reference) > 0 || highInclusive.CompareTo(reference) < 0)
			{
				return i;
			}
		}
		return -1;
	}

	internal static int IndexOfAnyInRangeUnsignedNumber<T>(ref T searchSpace, T lowInclusive, T highInclusive, int length) where T : struct, IUnsignedNumber<T>, IComparisonOperators<T, T, bool>
	{
		return IndexOfAnyInRangeUnsignedNumber<T, DontNegate<T>>(ref searchSpace, lowInclusive, highInclusive, length);
	}

	internal static int IndexOfAnyExceptInRangeUnsignedNumber<T>(ref T searchSpace, T lowInclusive, T highInclusive, int length) where T : struct, IUnsignedNumber<T>, IComparisonOperators<T, T, bool>
	{
		return IndexOfAnyInRangeUnsignedNumber<T, Negate<T>>(ref searchSpace, lowInclusive, highInclusive, length);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private unsafe static int IndexOfAnyInRangeUnsignedNumber<T, TNegator>(ref T searchSpace, T lowInclusive, T highInclusive, int length) where T : struct, IUnsignedNumber<T>, IComparisonOperators<T, T, bool> where TNegator : struct, INegator<T>
	{
		if (PackedSpanHelpers.PackedIndexOfIsSupported && typeof(T) == typeof(ushort) && PackedSpanHelpers.CanUsePackedIndexOf(lowInclusive) && PackedSpanHelpers.CanUsePackedIndexOf(highInclusive) && highInclusive >= lowInclusive)
		{
			ref char searchSpace2 = ref Unsafe.As<T, char>(ref searchSpace);
			char c = *(char*)(&lowInclusive);
			char rangeInclusive = (char)(*(ushort*)(&highInclusive) - c);
			if (!(typeof(TNegator) == typeof(DontNegate<ushort>)))
			{
				return PackedSpanHelpers.IndexOfAnyExceptInRange(ref searchSpace2, c, rangeInclusive, length);
			}
			return PackedSpanHelpers.IndexOfAnyInRange(ref searchSpace2, c, rangeInclusive, length);
		}
		return NonPackedIndexOfAnyInRangeUnsignedNumber<T, TNegator>(ref searchSpace, lowInclusive, highInclusive, length);
	}

	internal static int NonPackedIndexOfAnyInRangeUnsignedNumber<T, TNegator>(ref T searchSpace, T lowInclusive, T highInclusive, int length) where T : struct, IUnsignedNumber<T>, IComparisonOperators<T, T, bool> where TNegator : struct, INegator<T>
	{
		if (!Vector128.IsHardwareAccelerated || length < Vector128<T>.Count)
		{
			T val = highInclusive - lowInclusive;
			for (int i = 0; i < length; i++)
			{
				if (TNegator.NegateIfNeeded(Unsafe.Add(ref searchSpace, i) - lowInclusive <= val))
				{
					return i;
				}
			}
		}
		else if (!Vector256.IsHardwareAccelerated || length < Vector256<T>.Count)
		{
			Vector128<T> vector = Vector128.Create(lowInclusive);
			Vector128<T> right = Vector128.Create(highInclusive - lowInclusive);
			ref T reference = ref searchSpace;
			ref T reference2 = ref Unsafe.Add(ref searchSpace, (uint)(length - Vector128<T>.Count));
			Vector128<T> vector2;
			do
			{
				vector2 = TNegator.NegateIfNeeded(Vector128.LessThanOrEqual(Vector128.LoadUnsafe(ref reference) - vector, right));
				if (vector2 != Vector128<T>.Zero)
				{
					return ComputeFirstIndex(ref searchSpace, ref reference, vector2);
				}
				reference = ref Unsafe.Add(ref reference, Vector128<T>.Count);
			}
			while (Unsafe.IsAddressLessThan(ref reference, ref reference2));
			vector2 = TNegator.NegateIfNeeded(Vector128.LessThanOrEqual(Vector128.LoadUnsafe(ref reference2) - vector, right));
			if (vector2 != Vector128<T>.Zero)
			{
				return ComputeFirstIndex(ref searchSpace, ref reference2, vector2);
			}
		}
		else if (!Vector512.IsHardwareAccelerated || length < (uint)Vector512<T>.Count)
		{
			Vector256<T> vector3 = Vector256.Create(lowInclusive);
			Vector256<T> right2 = Vector256.Create(highInclusive - lowInclusive);
			ref T reference3 = ref searchSpace;
			ref T reference4 = ref Unsafe.Add(ref searchSpace, (uint)(length - Vector256<T>.Count));
			Vector256<T> vector4;
			do
			{
				vector4 = TNegator.NegateIfNeeded(Vector256.LessThanOrEqual(Vector256.LoadUnsafe(ref reference3) - vector3, right2));
				if (vector4 != Vector256<T>.Zero)
				{
					return ComputeFirstIndex(ref searchSpace, ref reference3, vector4);
				}
				reference3 = ref Unsafe.Add(ref reference3, Vector256<T>.Count);
			}
			while (Unsafe.IsAddressLessThan(ref reference3, ref reference4));
			vector4 = TNegator.NegateIfNeeded(Vector256.LessThanOrEqual(Vector256.LoadUnsafe(ref reference4) - vector3, right2));
			if (vector4 != Vector256<T>.Zero)
			{
				return ComputeFirstIndex(ref searchSpace, ref reference4, vector4);
			}
		}
		else
		{
			Vector512<T> vector5 = Vector512.Create(lowInclusive);
			Vector512<T> right3 = Vector512.Create(highInclusive - lowInclusive);
			ref T reference5 = ref searchSpace;
			ref T reference6 = ref Unsafe.Add(ref searchSpace, (uint)(length - Vector512<T>.Count));
			Vector512<T> vector6;
			do
			{
				vector6 = TNegator.NegateIfNeeded(Vector512.LessThanOrEqual(Vector512.LoadUnsafe(ref reference5) - vector5, right3));
				if (vector6 != Vector512<T>.Zero)
				{
					return ComputeFirstIndex(ref searchSpace, ref reference5, vector6);
				}
				reference5 = ref Unsafe.Add(ref reference5, Vector256<T>.Count);
			}
			while (Unsafe.IsAddressLessThan(ref reference5, ref reference6));
			vector6 = TNegator.NegateIfNeeded(Vector512.LessThanOrEqual(Vector512.LoadUnsafe(ref reference6) - vector5, right3));
			if (vector6 != Vector512<T>.Zero)
			{
				return ComputeFirstIndex(ref searchSpace, ref reference6, vector6);
			}
		}
		return -1;
	}

	internal static int LastIndexOfAnyInRange<T>(ref T searchSpace, T lowInclusive, T highInclusive, int length) where T : IComparable<T>
	{
		for (int num = length - 1; num >= 0; num--)
		{
			ref T reference = ref Unsafe.Add(ref searchSpace, num);
			if (lowInclusive.CompareTo(reference) <= 0 && highInclusive.CompareTo(reference) >= 0)
			{
				return num;
			}
		}
		return -1;
	}

	internal static int LastIndexOfAnyExceptInRange<T>(ref T searchSpace, T lowInclusive, T highInclusive, int length) where T : IComparable<T>
	{
		for (int num = length - 1; num >= 0; num--)
		{
			ref T reference = ref Unsafe.Add(ref searchSpace, num);
			if (lowInclusive.CompareTo(reference) > 0 || highInclusive.CompareTo(reference) < 0)
			{
				return num;
			}
		}
		return -1;
	}

	internal static int LastIndexOfAnyInRangeUnsignedNumber<T>(ref T searchSpace, T lowInclusive, T highInclusive, int length) where T : struct, IUnsignedNumber<T>, IComparisonOperators<T, T, bool>
	{
		return LastIndexOfAnyInRangeUnsignedNumber<T, DontNegate<T>>(ref searchSpace, lowInclusive, highInclusive, length);
	}

	internal static int LastIndexOfAnyExceptInRangeUnsignedNumber<T>(ref T searchSpace, T lowInclusive, T highInclusive, int length) where T : struct, IUnsignedNumber<T>, IComparisonOperators<T, T, bool>
	{
		return LastIndexOfAnyInRangeUnsignedNumber<T, Negate<T>>(ref searchSpace, lowInclusive, highInclusive, length);
	}

	private static int LastIndexOfAnyInRangeUnsignedNumber<T, TNegator>(ref T searchSpace, T lowInclusive, T highInclusive, int length) where T : struct, IUnsignedNumber<T>, IComparisonOperators<T, T, bool> where TNegator : struct, INegator<T>
	{
		if (!Vector128.IsHardwareAccelerated || length < Vector128<T>.Count)
		{
			T val = highInclusive - lowInclusive;
			for (int num = length - 1; num >= 0; num--)
			{
				if (TNegator.NegateIfNeeded(Unsafe.Add(ref searchSpace, num) - lowInclusive <= val))
				{
					return num;
				}
			}
		}
		else if (!Vector256.IsHardwareAccelerated || length < Vector256<T>.Count)
		{
			Vector128<T> vector = Vector128.Create(lowInclusive);
			Vector128<T> right = Vector128.Create(highInclusive - lowInclusive);
			Vector128<T> vector2;
			for (nint num2 = length - Vector128<T>.Count; num2 > 0; num2 -= Vector128<T>.Count)
			{
				vector2 = TNegator.NegateIfNeeded(Vector128.LessThanOrEqual(Vector128.LoadUnsafe(ref searchSpace, (nuint)num2) - vector, right));
				if (vector2 != Vector128<T>.Zero)
				{
					return ComputeLastIndex(num2, vector2);
				}
			}
			vector2 = TNegator.NegateIfNeeded(Vector128.LessThanOrEqual(Vector128.LoadUnsafe(ref searchSpace) - vector, right));
			if (vector2 != Vector128<T>.Zero)
			{
				return ComputeLastIndex(0, vector2);
			}
		}
		else if (!Vector512.IsHardwareAccelerated || length < Vector512<T>.Count)
		{
			Vector256<T> vector3 = Vector256.Create(lowInclusive);
			Vector256<T> right2 = Vector256.Create(highInclusive - lowInclusive);
			Vector256<T> vector4;
			for (nint num3 = length - Vector256<T>.Count; num3 > 0; num3 -= Vector256<T>.Count)
			{
				vector4 = TNegator.NegateIfNeeded(Vector256.LessThanOrEqual(Vector256.LoadUnsafe(ref searchSpace, (nuint)num3) - vector3, right2));
				if (vector4 != Vector256<T>.Zero)
				{
					return ComputeLastIndex(num3, vector4);
				}
			}
			vector4 = TNegator.NegateIfNeeded(Vector256.LessThanOrEqual(Vector256.LoadUnsafe(ref searchSpace) - vector3, right2));
			if (vector4 != Vector256<T>.Zero)
			{
				return ComputeLastIndex(0, vector4);
			}
		}
		else
		{
			Vector512<T> vector5 = Vector512.Create(lowInclusive);
			Vector512<T> right3 = Vector512.Create(highInclusive - lowInclusive);
			Vector512<T> vector6;
			for (nint num4 = length - Vector512<T>.Count; num4 > 0; num4 -= Vector512<T>.Count)
			{
				vector6 = TNegator.NegateIfNeeded(Vector512.LessThanOrEqual(Vector512.LoadUnsafe(ref searchSpace, (nuint)num4) - vector5, right3));
				if (vector6 != Vector512<T>.Zero)
				{
					return ComputeLastIndex(num4, vector6);
				}
			}
			vector6 = TNegator.NegateIfNeeded(Vector512.LessThanOrEqual(Vector512.LoadUnsafe(ref searchSpace) - vector5, right3));
			if (vector6 != Vector512<T>.Zero)
			{
				return ComputeLastIndex(0, vector6);
			}
		}
		return -1;
	}

	public static int Count<T>(ref T current, T value, int length) where T : IEquatable<T>
	{
		int num = 0;
		ref T right = ref Unsafe.Add(ref current, length);
		if (value != null)
		{
			while (Unsafe.IsAddressLessThan(ref current, ref right))
			{
				if (value.Equals(current))
				{
					num++;
				}
				current = ref Unsafe.Add(ref current, 1);
			}
		}
		else
		{
			while (Unsafe.IsAddressLessThan(ref current, ref right))
			{
				if (current == null)
				{
					num++;
				}
				current = ref Unsafe.Add(ref current, 1);
			}
		}
		return num;
	}

	public static int CountValueType<T>(ref T current, T value, int length) where T : struct, IEquatable<T>
	{
		int num = 0;
		ref T reference = ref Unsafe.Add(ref current, length);
		if (Vector128.IsHardwareAccelerated && length >= Vector128<T>.Count)
		{
			if (Vector512.IsHardwareAccelerated && length >= Vector512<T>.Count)
			{
				Vector512<T> right = Vector512.Create(value);
				ref T reference2 = ref Unsafe.Subtract(ref reference, Vector512<T>.Count);
				do
				{
					num += BitOperations.PopCount(Vector512.Equals(Vector512.LoadUnsafe(ref current), right).ExtractMostSignificantBits());
					current = ref Unsafe.Add(ref current, Vector512<T>.Count);
				}
				while (!Unsafe.IsAddressGreaterThan(ref current, ref reference2));
				uint num2 = (uint)Unsafe.ByteOffset(ref current, ref reference) / (uint)Unsafe.SizeOf<T>();
				if (num2 > Vector512<T>.Count / 2)
				{
					ulong num3 = Vector512.Equals(Vector512.LoadUnsafe(ref reference2), right).ExtractMostSignificantBits();
					uint num4 = (uint)Vector512<T>.Count - num2;
					num3 >>= (int)num4;
					return num + BitOperations.PopCount(num3);
				}
			}
			else if (Vector256.IsHardwareAccelerated && length >= Vector256<T>.Count)
			{
				Vector256<T> right2 = Vector256.Create(value);
				ref T reference3 = ref Unsafe.Subtract(ref reference, Vector256<T>.Count);
				do
				{
					num += BitOperations.PopCount(Vector256.Equals(Vector256.LoadUnsafe(ref current), right2).ExtractMostSignificantBits());
					current = ref Unsafe.Add(ref current, Vector256<T>.Count);
				}
				while (!Unsafe.IsAddressGreaterThan(ref current, ref reference3));
				uint num5 = (uint)Unsafe.ByteOffset(ref current, ref reference) / (uint)Unsafe.SizeOf<T>();
				if (num5 > Vector256<T>.Count / 2)
				{
					uint num6 = Vector256.Equals(Vector256.LoadUnsafe(ref reference3), right2).ExtractMostSignificantBits();
					uint num7 = (uint)Vector256<T>.Count - num5;
					num6 >>= (int)num7;
					return num + BitOperations.PopCount(num6);
				}
			}
			else
			{
				Vector128<T> right3 = Vector128.Create(value);
				ref T reference4 = ref Unsafe.Subtract(ref reference, Vector128<T>.Count);
				do
				{
					num += BitOperations.PopCount(Vector128.Equals(Vector128.LoadUnsafe(ref current), right3).ExtractMostSignificantBits());
					current = ref Unsafe.Add(ref current, Vector128<T>.Count);
				}
				while (!Unsafe.IsAddressGreaterThan(ref current, ref reference4));
				uint num8 = (uint)Unsafe.ByteOffset(ref current, ref reference) / (uint)Unsafe.SizeOf<T>();
				if (num8 > Vector128<T>.Count / 2)
				{
					uint num9 = Vector128.Equals(Vector128.LoadUnsafe(ref reference4), right3).ExtractMostSignificantBits();
					uint num10 = (uint)Vector128<T>.Count - num8;
					num9 >>= (int)num10;
					return num + BitOperations.PopCount(num9);
				}
			}
		}
		while (Unsafe.IsAddressLessThan(ref current, ref reference))
		{
			if (current.Equals(value))
			{
				num++;
			}
			current = ref Unsafe.Add(ref current, 1);
		}
		return num;
	}
}
