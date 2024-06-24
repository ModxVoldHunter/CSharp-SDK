using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace System.Text.Unicode;

internal static class Utf16Utility
{
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static bool AllCharsInUInt32AreAscii(uint value)
	{
		return (value & 0xFF80FF80u) == 0;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static bool AllCharsInUInt64AreAscii(ulong value)
	{
		return (value & 0xFF80FF80FF80FF80uL) == 0;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static uint ConvertAllAsciiCharsInUInt32ToLowercase(uint value)
	{
		uint num = value + 8388736 - 4259905;
		uint num2 = value + 8388736 - 5963867;
		uint num3 = num ^ num2;
		uint num4 = (num3 & 0x800080) >> 2;
		return value ^ num4;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static uint ConvertAllAsciiCharsInUInt32ToUppercase(uint value)
	{
		uint num = value + 8388736 - 6357089;
		uint num2 = value + 8388736 - 8061051;
		uint num3 = num ^ num2;
		uint num4 = (num3 & 0x800080) >> 2;
		return value ^ num4;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static ulong ConvertAllAsciiCharsInUInt64ToUppercase(ulong value)
	{
		ulong num = value + 36029346783166592L - 27303489359118433L;
		ulong num2 = value + 36029346783166592L - 34621950424449147L;
		ulong num3 = num ^ num2;
		ulong num4 = (num3 & 0x80008000800080L) >> 2;
		return value ^ num4;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static ulong ConvertAllAsciiCharsInUInt64ToLowercase(ulong value)
	{
		ulong num = value + 36029346783166592L - 18296152663326785L;
		ulong num2 = value + 36029346783166592L - 25614613728657499L;
		ulong num3 = num ^ num2;
		ulong num4 = (num3 & 0x80008000800080L) >> 2;
		return value ^ num4;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static bool UInt32ContainsAnyLowercaseAsciiChar(uint value)
	{
		uint num = value + 8388736 - 6357089;
		uint num2 = value + 8388736 - 8061051;
		uint num3 = num ^ num2;
		return (num3 & 0x800080) != 0;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static bool UInt32ContainsAnyUppercaseAsciiChar(uint value)
	{
		uint num = value + 8388736 - 4259905;
		uint num2 = value + 8388736 - 5963867;
		uint num3 = num ^ num2;
		return (num3 & 0x800080) != 0;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static bool UInt32OrdinalIgnoreCaseAscii(uint valueA, uint valueB)
	{
		uint num = (valueA ^ valueB) << 2;
		uint num2 = valueA + 327685;
		num2 |= 0xA000A0u;
		num2 += 1703962;
		num2 |= 0xFF7FFF7Fu;
		return (num & num2) == 0;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static bool UInt64OrdinalIgnoreCaseAscii(ulong valueA, ulong valueB)
	{
		ulong num = (valueA ^ valueB) << 2;
		ulong num2 = valueA + 1407396358717445L;
		num2 |= 0xA000A000A000A0uL;
		num2 += 7318461065330714L;
		num2 |= 0xFF7FFF7FFF7FFF7FuL;
		return (num & num2) == 0;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static bool AllCharsInVector128AreAscii(Vector128<ushort> vec)
	{
		return (vec & Vector128.Create((ushort)65408)) == Vector128<ushort>.Zero;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static bool Vector128OrdinalIgnoreCaseAscii(Vector128<ushort> vec1, Vector128<ushort> vec2)
	{
		Vector128<sbyte> right = Vector128.Create((sbyte)63) + vec1.AsSByte();
		Vector128<sbyte> right2 = Vector128.Create((sbyte)63) + vec2.AsSByte();
		Vector128<sbyte> right3 = Vector128.LessThan(Vector128.Create((sbyte)(-103)), right);
		Vector128<sbyte> right4 = Vector128.LessThan(Vector128.Create((sbyte)(-103)), right2);
		Vector128<sbyte> vector = Vector128.AndNot(Vector128.Create((sbyte)32), right3) + vec1.AsSByte();
		Vector128<sbyte> vector2 = Vector128.AndNot(Vector128.Create((sbyte)32), right4) + vec2.AsSByte();
		return (vector ^ vector2) == Vector128<sbyte>.Zero;
	}

	public unsafe static char* GetPointerToFirstInvalidChar(char* pInputBuffer, int inputLength, out long utf8CodeUnitCountAdjustment, out int scalarCountAdjustment)
	{
		int num = (int)Ascii.GetIndexOfFirstNonAsciiChar(pInputBuffer, (uint)inputLength);
		pInputBuffer += (uint)num;
		inputLength -= num;
		if (inputLength == 0)
		{
			utf8CodeUnitCountAdjustment = 0L;
			scalarCountAdjustment = 0;
			return pInputBuffer;
		}
		long num2 = 0L;
		int num3 = 0;
		char* ptr = pInputBuffer + (uint)inputLength;
		if (Sse2.IsSupported)
		{
			if (inputLength >= Vector128<ushort>.Count)
			{
				Vector128<ushort> right = Vector128.Create((ushort)128);
				Vector128<ushort> vector = Vector128.Create((ushort)30720);
				Vector128<ushort> vector2 = Vector128.Create((ushort)40960);
				char* ptr2 = ptr - Vector128<ushort>.Count;
				do
				{
					Vector128<ushort> vector3 = Vector128.Load((ushort*)pInputBuffer);
					pInputBuffer += Vector128<ushort>.Count;
					Vector128<ushort> vector4 = Vector128.Min(vector3, right);
					Vector128<ushort> vector5 = Vector128.AddSaturate(vector3, vector);
					uint value = (vector4 | vector5).AsByte().ExtractMostSignificantBits();
					nuint num4 = (uint)BitOperations.PopCount(value);
					value = Vector128.LessThan((vector3 + vector2).AsInt16(), vector.AsInt16()).AsByte().ExtractMostSignificantBits();
					while (value != 65535)
					{
						value = ~value;
						uint num5 = Vector128.ShiftRightLogical(vector3, 3).AsByte().ExtractMostSignificantBits();
						uint num6 = num5 & value;
						uint num7 = (num5 ^ 0x5555u) & value;
						num7 <<= 2;
						if ((ushort)num7 == num6)
						{
							if (num7 > 65535)
							{
								num7 = (ushort)num7;
								num4 -= 2;
								pInputBuffer--;
							}
							nuint num8 = (uint)BitOperations.PopCount(num7);
							num3 -= (int)num8;
							_ = 8;
							num2 -= (long)num8;
							num2 -= (long)num8;
							value = 65535u;
							continue;
						}
						goto IL_0187;
					}
					num2 += (long)num4;
					continue;
					IL_0187:
					pInputBuffer -= Vector128<ushort>.Count;
					break;
				}
				while (pInputBuffer <= ptr2);
			}
		}
		else if (Vector128.IsHardwareAccelerated && inputLength >= Vector128<ushort>.Count)
		{
			Vector128<ushort> right2 = Vector128.Create<ushort>((ushort)128);
			Vector128<ushort> right3 = Vector128.Create<ushort>((ushort)1024);
			Vector128<ushort> right4 = Vector128.Create<ushort>((ushort)2048);
			Vector128<ushort> vector6 = Vector128.Create<ushort>((ushort)55296);
			char* ptr3 = ptr - Vector128<ushort>.Count;
			while (true)
			{
				Vector128<ushort> left = Vector128.Load((ushort*)pInputBuffer);
				Vector128<ushort> vector7 = Vector128.GreaterThanOrEqual(left, right2);
				Vector128<ushort> vector8 = Vector128.GreaterThanOrEqual(left, right4);
				Vector128<nuint> vector9 = (Vector128<ushort>.Zero - vector7 - vector8).AsNUInt();
				nuint num9 = 0u;
				for (int i = 0; i < Vector128<nuint>.Count; i++)
				{
					num9 += vector9[i];
				}
				uint num10 = (uint)num9;
				_ = 8;
				num10 += (uint)(int)(num9 >> (0x20 & 0x3F));
				num10 = (ushort)num10 + (num10 >> 16);
				left -= vector6;
				Vector128<ushort> vector10 = Vector128.LessThan(left, right4);
				if (vector10 != Vector128<ushort>.Zero)
				{
					Vector128<ushort> right5 = Vector128.LessThan(left, right3);
					Vector128<ushort> vector11 = Vector128.AndNot(vector10, right5);
					if (vector11[0] != 0)
					{
						break;
					}
					ushort num11 = 0;
					int num12 = 0;
					while (num12 < Vector128<ushort>.Count - 1)
					{
						num11 -= right5[num12];
						if (right5[num12] == vector11[num12 + 1])
						{
							num12++;
							continue;
						}
						goto IL_03ab;
					}
					if (right5[Vector128<ushort>.Count - 1] != 0)
					{
						pInputBuffer--;
						num10 -= 2;
					}
					nint num13 = num11;
					num3 -= (int)num13;
					num2 -= num13;
					num2 -= num13;
				}
				num2 += num10;
				pInputBuffer += Vector128<ushort>.Count;
				if (pInputBuffer <= ptr3)
				{
					continue;
				}
				goto IL_03ab;
			}
			goto IL_03af;
		}
		goto IL_03ab;
		IL_03ab:
		while (pInputBuffer < ptr)
		{
			uint num14 = *pInputBuffer;
			if (num14 > 127)
			{
				num2 += num14 + 129024 >> 16;
				if (UnicodeUtility.IsSurrogateCodePoint(num14))
				{
					num2 -= 2;
					if ((nuint)((byte*)ptr - (nuint)pInputBuffer) < (nuint)4u)
					{
						break;
					}
					num14 = Unsafe.ReadUnaligned<uint>(pInputBuffer);
					uint num15 = num14;
					if (!BitConverter.IsLittleEndian)
					{
					}
					if (((num15 - 3691042816u) & 0xFC00FC00u) != 0)
					{
						break;
					}
					num3--;
					num2 += 2;
					pInputBuffer++;
				}
			}
			pInputBuffer++;
		}
		goto IL_03af;
		IL_03af:
		utf8CodeUnitCountAdjustment = num2;
		scalarCountAdjustment = num3;
		return pInputBuffer;
	}
}
