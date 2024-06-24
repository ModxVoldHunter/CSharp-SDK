using System.Buffers;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Text;
using System.Text.Unicode;

namespace System.Globalization;

internal static class Ordinal
{
	internal static int CompareStringIgnoreCase(ref char strA, int lengthA, ref char strB, int lengthB)
	{
		int num = Math.Min(lengthA, lengthB);
		int num2 = num;
		ref char reference = ref strA;
		ref char reference2 = ref strB;
		while (num != 0 && reference <= '\u007f' && reference2 <= '\u007f')
		{
			if (reference == reference2 || ((reference | 0x20) == (reference2 | 0x20) && char.IsAsciiLetter(reference)))
			{
				num--;
				reference = ref Unsafe.Add(ref reference, 1);
				reference2 = ref Unsafe.Add(ref reference2, 1);
				continue;
			}
			int num3 = reference;
			int num4 = reference2;
			if (char.IsAsciiLetterLower(reference))
			{
				num3 -= 32;
			}
			if (char.IsAsciiLetterLower(reference2))
			{
				num4 -= 32;
			}
			return num3 - num4;
		}
		if (num == 0)
		{
			return lengthA - lengthB;
		}
		num2 -= num;
		return CompareStringIgnoreCaseNonAscii(ref reference, lengthA - num2, ref reference2, lengthB - num2);
	}

	internal static int CompareStringIgnoreCaseNonAscii(ref char strA, int lengthA, ref char strB, int lengthB)
	{
		if (GlobalizationMode.Invariant)
		{
			return InvariantModeCasing.CompareStringIgnoreCase(ref strA, lengthA, ref strB, lengthB);
		}
		if (GlobalizationMode.UseNls)
		{
			return CompareInfo.NlsCompareStringOrdinalIgnoreCase(ref strA, lengthA, ref strB, lengthB);
		}
		return OrdinalCasing.CompareStringIgnoreCase(ref strA, lengthA, ref strB, lengthB);
	}

	private static bool EqualsIgnoreCase_Vector128(ref char charA, ref char charB, int length)
	{
		nuint num = (nuint)length;
		nuint num2 = num - (nuint)Vector128<ushort>.Count;
		nuint num3 = 0u;
		Vector128<ushort> vector;
		Vector128<ushort> vector2;
		while (true)
		{
			vector = Vector128.LoadUnsafe(ref charA, num3);
			vector2 = Vector128.LoadUnsafe(ref charB, num3);
			if (!Utf16Utility.AllCharsInVector128AreAscii(vector | vector2))
			{
				break;
			}
			if (!Utf16Utility.Vector128OrdinalIgnoreCaseAscii(vector, vector2))
			{
				return false;
			}
			num3 += (nuint)Vector128<ushort>.Count;
			if (num3 > num2)
			{
				if (num3 != num)
				{
					return EqualsIgnoreCase(ref Unsafe.Add(ref charA, num3), ref Unsafe.Add(ref charB, num3), (int)(num - num3));
				}
				return true;
			}
		}
		if (Utf16Utility.AllCharsInVector128AreAscii(vector) || Utf16Utility.AllCharsInVector128AreAscii(vector2))
		{
			return false;
		}
		return CompareStringIgnoreCase(ref Unsafe.Add(ref charA, num3), (int)(num - num3), ref Unsafe.Add(ref charB, num3), (int)(num - num3)) == 0;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static bool EqualsIgnoreCase(ref char charA, ref char charB, int length)
	{
		if (!Vector128.IsHardwareAccelerated || length < Vector128<ushort>.Count)
		{
			return EqualsIgnoreCase_Scalar(ref charA, ref charB, length);
		}
		return EqualsIgnoreCase_Vector128(ref charA, ref charB, length);
	}

	internal static bool EqualsIgnoreCase_Scalar(ref char charA, ref char charB, int length)
	{
		nint num = IntPtr.Zero;
		ulong num2 = 0uL;
		ulong num3 = 0uL;
		while (true)
		{
			if ((uint)length >= 4u)
			{
				num2 = Unsafe.ReadUnaligned<ulong>(ref Unsafe.As<char, byte>(ref Unsafe.AddByteOffset(ref charA, num)));
				num3 = Unsafe.ReadUnaligned<ulong>(ref Unsafe.As<char, byte>(ref Unsafe.AddByteOffset(ref charB, num)));
				ulong num4 = num2 | num3;
				if (Utf16Utility.AllCharsInUInt32AreAscii((uint)((int)num4 | (int)(num4 >> 32))))
				{
					if (!Utf16Utility.UInt64OrdinalIgnoreCaseAscii(num2, num3))
					{
						return false;
					}
					num += 8;
					length -= 4;
					continue;
				}
				if (!Utf16Utility.AllCharsInUInt64AreAscii(num2) && !Utf16Utility.AllCharsInUInt64AreAscii(num3))
				{
					break;
				}
				return false;
			}
			uint num5 = 0u;
			uint num6 = 0u;
			if ((uint)length >= 2u)
			{
				num5 = Unsafe.ReadUnaligned<uint>(ref Unsafe.As<char, byte>(ref Unsafe.AddByteOffset(ref charA, num)));
				num6 = Unsafe.ReadUnaligned<uint>(ref Unsafe.As<char, byte>(ref Unsafe.AddByteOffset(ref charB, num)));
				if (!Utf16Utility.AllCharsInUInt32AreAscii(num5 | num6))
				{
					goto IL_00f2;
				}
				if (!Utf16Utility.UInt32OrdinalIgnoreCaseAscii(num5, num6))
				{
					return false;
				}
				num += 4;
				length -= 2;
			}
			if (length != 0)
			{
				num5 = Unsafe.AddByteOffset(ref charA, num);
				num6 = Unsafe.AddByteOffset(ref charB, num);
				if ((num5 | num6) <= 127)
				{
					if (num5 == num6)
					{
						return true;
					}
					num5 |= 0x20u;
					if (num5 - 97 > 25)
					{
						return false;
					}
					return num5 == (num6 | 0x20);
				}
				goto IL_00f2;
			}
			return true;
			IL_00f2:
			if (!Utf16Utility.AllCharsInUInt32AreAscii(num5) && !Utf16Utility.AllCharsInUInt32AreAscii(num6))
			{
				break;
			}
			return false;
		}
		return CompareStringIgnoreCase(ref Unsafe.AddByteOffset(ref charA, num), length, ref Unsafe.AddByteOffset(ref charB, num), length) == 0;
	}

	internal static int IndexOf(string source, string value, int startIndex, int count, bool ignoreCase)
	{
		if (source == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
		}
		if (value == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.value);
		}
		if (!source.TryGetSpan(startIndex, count, out var slice))
		{
			if ((uint)startIndex > (uint)source.Length)
			{
				ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.startIndex, ExceptionResource.ArgumentOutOfRange_IndexMustBeLessOrEqual);
			}
			else
			{
				ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.count, ExceptionResource.ArgumentOutOfRange_Count);
			}
		}
		int num = (ignoreCase ? IndexOfOrdinalIgnoreCase(slice, value) : slice.IndexOf(value));
		if (num < 0)
		{
			return num;
		}
		return num + startIndex;
	}

	internal static int IndexOfOrdinalIgnoreCase(ReadOnlySpan<char> source, ReadOnlySpan<char> value)
	{
		if (value.Length == 0)
		{
			return 0;
		}
		if (value.Length > source.Length)
		{
			return -1;
		}
		if (GlobalizationMode.Invariant)
		{
			return InvariantModeCasing.IndexOfIgnoreCase(source, value);
		}
		if (GlobalizationMode.UseNls)
		{
			return CompareInfo.NlsIndexOfOrdinalCore(source, value, ignoreCase: true, fromBeginning: true);
		}
		ref char reference = ref MemoryMarshal.GetReference(value);
		char c = reference;
		if (!char.IsAscii(c))
		{
			return OrdinalCasing.IndexOf(source, value);
		}
		int num = value.Length - 1;
		int num2 = source.Length - num;
		ref char reference2 = ref MemoryMarshal.GetReference(source);
		char c2 = '\0';
		char value2 = '\0';
		nint num3 = 0;
		bool flag = false;
		if (Vector128.IsHardwareAccelerated && num != 0 && num2 >= Vector128<ushort>.Count)
		{
			c2 = Unsafe.Add(ref reference, num);
			if (char.IsAscii(c2))
			{
				c = (char)(c | 0x20u);
				c2 = (char)(c2 | 0x20u);
				nint num4 = num;
				while (c2 == c && num4 > 1)
				{
					char c3 = Unsafe.Add(ref reference, num4 - 1);
					if (!char.IsAscii(c3))
					{
						break;
					}
					num4--;
					c2 = (char)(c3 | 0x20u);
				}
				if (Vector256.IsHardwareAccelerated && num2 - Vector256<ushort>.Count >= 0)
				{
					Vector256<ushort> left = Vector256.Create((ushort)c);
					Vector256<ushort> left2 = Vector256.Create((ushort)c2);
					nint num5 = (nint)num2 - (nint)Vector256<ushort>.Count;
					while (true)
					{
						Vector256<ushort> vector = Vector256.Equals(left2, Vector256.BitwiseOr(Vector256.LoadUnsafe(ref reference2, (nuint)(num3 + num4)), Vector256.Create((ushort)32)));
						Vector256<ushort> vector2 = Vector256.Equals(left, Vector256.BitwiseOr(Vector256.LoadUnsafe(ref reference2, (nuint)num3), Vector256.Create((ushort)32)));
						Vector256<byte> vector3 = (vector2 & vector).AsByte();
						if (vector3 != Vector256<byte>.Zero)
						{
							uint num6 = vector3.ExtractMostSignificantBits();
							do
							{
								nint num7 = (nint)(uint.TrailingZeroCount(num6) / 2);
								if (EqualsIgnoreCase(ref Unsafe.Add(ref reference2, num3 + num7), ref reference, value.Length))
								{
									return (int)(num3 + num7);
								}
								num6 = BitOperations.ResetLowestSetBit(BitOperations.ResetLowestSetBit(num6));
							}
							while (num6 != 0);
						}
						num3 += Vector256<ushort>.Count;
						if (num3 == num2)
						{
							break;
						}
						if (num3 > num5)
						{
							num3 = num5;
						}
					}
					return -1;
				}
				Vector128<ushort> left3 = Vector128.Create((ushort)c);
				Vector128<ushort> left4 = Vector128.Create((ushort)c2);
				nint num8 = (nint)num2 - (nint)Vector128<ushort>.Count;
				while (true)
				{
					Vector128<ushort> vector4 = Vector128.Equals(left4, Vector128.BitwiseOr(Vector128.LoadUnsafe(ref reference2, (nuint)(num3 + num4)), Vector128.Create((ushort)32)));
					Vector128<ushort> vector5 = Vector128.Equals(left3, Vector128.BitwiseOr(Vector128.LoadUnsafe(ref reference2, (nuint)num3), Vector128.Create((ushort)32)));
					Vector128<byte> vector6 = (vector5 & vector4).AsByte();
					if (vector6 != Vector128<byte>.Zero)
					{
						uint num9 = vector6.ExtractMostSignificantBits();
						do
						{
							nint num10 = (nint)(uint.TrailingZeroCount(num9) / 2);
							if (EqualsIgnoreCase(ref Unsafe.Add(ref reference2, num3 + num10), ref reference, value.Length))
							{
								return (int)(num3 + num10);
							}
							num9 = BitOperations.ResetLowestSetBit(BitOperations.ResetLowestSetBit(num9));
						}
						while (num9 != 0);
					}
					num3 += Vector128<ushort>.Count;
					if (num3 == num2)
					{
						break;
					}
					if (num3 > num8)
					{
						num3 = num8;
					}
				}
				return -1;
			}
		}
		if (char.IsAsciiLetter(c))
		{
			c2 = (char)(c & 0xFFFFFFDFu);
			value2 = (char)(c | 0x20u);
			flag = true;
		}
		do
		{
			int num11 = ((!flag) ? SpanHelpers.IndexOfChar(ref Unsafe.Add(ref reference2, num3), c, num2) : (PackedSpanHelpers.PackedIndexOfIsSupported ? PackedSpanHelpers.IndexOfAny(ref Unsafe.Add(ref reference2, num3), c2, value2, num2) : SpanHelpers.IndexOfAnyChar(ref Unsafe.Add(ref reference2, num3), c2, value2, num2)));
			if (num11 < 0)
			{
				break;
			}
			num2 -= num11;
			if (num2 <= 0)
			{
				break;
			}
			num3 += num11;
			if (num == 0 || EqualsIgnoreCase(ref Unsafe.Add(ref reference2, (nuint)(num3 + 1)), ref Unsafe.Add(ref reference, 1), num))
			{
				return (int)num3;
			}
			num2--;
			num3++;
		}
		while (num2 > 0);
		return -1;
	}

	internal static int LastIndexOfOrdinalIgnoreCase(ReadOnlySpan<char> source, ReadOnlySpan<char> value)
	{
		if (value.Length == 0)
		{
			return source.Length;
		}
		if (value.Length > source.Length)
		{
			return -1;
		}
		if (GlobalizationMode.Invariant)
		{
			return InvariantModeCasing.LastIndexOfIgnoreCase(source, value);
		}
		if (GlobalizationMode.UseNls)
		{
			return CompareInfo.NlsIndexOfOrdinalCore(source, value, ignoreCase: true, fromBeginning: false);
		}
		return OrdinalCasing.LastIndexOf(source, value);
	}

	internal static int ToUpperOrdinal(ReadOnlySpan<char> source, Span<char> destination)
	{
		if (source.Overlaps(destination))
		{
			ThrowHelper.ThrowInvalidOperationException(ExceptionResource.InvalidOperation_SpanOverlappedOperation);
		}
		if (destination.Length < source.Length)
		{
			return -1;
		}
		if (GlobalizationMode.Invariant)
		{
			InvariantModeCasing.ToUpper(source, destination);
			return source.Length;
		}
		if (GlobalizationMode.UseNls)
		{
			TextInfo.Invariant.ChangeCaseToUpper(source, destination);
			return source.Length;
		}
		OrdinalCasing.ToUpperOrdinal(source, destination);
		return source.Length;
	}

	internal static bool EqualsStringIgnoreCaseUtf8(ref byte strA, int lengthA, ref byte strB, int lengthB)
	{
		int num = Math.Min(lengthA, lengthB);
		int num2 = num;
		ref byte reference = ref strA;
		ref byte reference2 = ref strB;
		while (num != 0 && reference <= 127 && reference2 <= 127)
		{
			if (reference == reference2 || ((reference | 0x20) == (reference2 | 0x20) && char.IsAsciiLetter((char)reference)))
			{
				num--;
				reference = ref Unsafe.Add(ref reference, 1);
				reference2 = ref Unsafe.Add(ref reference2, 1);
				continue;
			}
			return false;
		}
		if (num == 0)
		{
			return lengthA == lengthB;
		}
		num2 -= num;
		return EqualsStringIgnoreCaseNonAsciiUtf8(ref reference, lengthA - num2, ref reference2, lengthB - num2);
	}

	internal static bool EqualsStringIgnoreCaseNonAsciiUtf8(ref byte strA, int lengthA, ref byte strB, int lengthB)
	{
		ReadOnlySpan<byte> source = MemoryMarshal.CreateReadOnlySpan(ref strA, lengthA);
		ReadOnlySpan<byte> source2 = MemoryMarshal.CreateReadOnlySpan(ref strB, lengthB);
		do
		{
			Rune result;
			int bytesConsumed;
			OperationStatus operationStatus = Rune.DecodeFromUtf8(source, out result, out bytesConsumed);
			Rune result2;
			int bytesConsumed2;
			OperationStatus operationStatus2 = Rune.DecodeFromUtf8(source2, out result2, out bytesConsumed2);
			if (operationStatus != operationStatus2)
			{
				return false;
			}
			if (operationStatus == OperationStatus.Done)
			{
				if (Rune.ToUpperInvariant(result) != Rune.ToUpperInvariant(result2))
				{
					return false;
				}
			}
			else if (!source.Slice(0, bytesConsumed).SequenceEqual(source2.Slice(0, bytesConsumed2)))
			{
				return false;
			}
			source = source.Slice(bytesConsumed);
			source2 = source2.Slice(bytesConsumed2);
		}
		while ((source.Length | source2.Length) != 0);
		return true;
	}

	private static bool EqualsIgnoreCaseUtf8_Vector128(ref byte charA, int lengthA, ref byte charB, int lengthB)
	{
		nuint num = Math.Min((uint)lengthA, (uint)lengthB);
		nuint num2 = num - (nuint)Vector128<byte>.Count;
		nuint num3 = 0u;
		Vector128<byte> vector;
		Vector128<byte> vector2;
		while (true)
		{
			vector = Vector128.LoadUnsafe(ref charA, num3);
			vector2 = Vector128.LoadUnsafe(ref charB, num3);
			if (!Utf8Utility.AllBytesInVector128AreAscii(vector | vector2))
			{
				break;
			}
			if (!Utf8Utility.Vector128OrdinalIgnoreCaseAscii(vector, vector2))
			{
				return false;
			}
			num3 += (nuint)Vector128<byte>.Count;
			if (num3 > num2)
			{
				if (num3 == num)
				{
					return lengthA == lengthB;
				}
				return EqualsIgnoreCaseUtf8_Scalar(ref Unsafe.Add(ref charA, num3), (int)(num - num3), ref Unsafe.Add(ref charB, num3), (int)(num - num3));
			}
		}
		if (Utf8Utility.AllBytesInVector128AreAscii(vector) || Utf8Utility.AllBytesInVector128AreAscii(vector2))
		{
			return false;
		}
		return EqualsStringIgnoreCaseUtf8(ref Unsafe.Add(ref charA, num3), lengthA - (int)num3, ref Unsafe.Add(ref charB, num3), lengthB - (int)num3);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static bool EqualsIgnoreCaseUtf8(ref byte charA, int lengthA, ref byte charB, int lengthB)
	{
		if (!Vector128.IsHardwareAccelerated || lengthA < Vector128<byte>.Count || lengthB < Vector128<byte>.Count)
		{
			return EqualsIgnoreCaseUtf8_Scalar(ref charA, lengthA, ref charB, lengthB);
		}
		return EqualsIgnoreCaseUtf8_Vector128(ref charA, lengthA, ref charB, lengthB);
	}

	internal static bool EqualsIgnoreCaseUtf8_Scalar(ref byte charA, int lengthA, ref byte charB, int lengthB)
	{
		nint num = IntPtr.Zero;
		int num2 = Math.Min(lengthA, lengthB);
		int num3 = num2;
		ulong num4 = 0uL;
		ulong num5 = 0uL;
		while (true)
		{
			if ((uint)num2 >= 8u)
			{
				num4 = Unsafe.ReadUnaligned<ulong>(ref Unsafe.AddByteOffset(ref charA, num));
				num5 = Unsafe.ReadUnaligned<ulong>(ref Unsafe.AddByteOffset(ref charB, num));
				ulong num6 = num4 | num5;
				if (Utf8Utility.AllBytesInUInt32AreAscii((uint)((int)num6 | (int)(num6 >> 32))))
				{
					if (!Utf8Utility.UInt64OrdinalIgnoreCaseAscii(num4, num5))
					{
						return false;
					}
					num += 8;
					num2 -= 8;
					continue;
				}
				if (!Utf8Utility.AllBytesInUInt64AreAscii(num4) && !Utf8Utility.AllBytesInUInt64AreAscii(num5))
				{
					break;
				}
				return false;
			}
			uint num7 = 0u;
			uint num8 = 0u;
			if ((uint)num2 >= 4u)
			{
				num7 = Unsafe.ReadUnaligned<uint>(ref Unsafe.AddByteOffset(ref charA, num));
				num8 = Unsafe.ReadUnaligned<uint>(ref Unsafe.AddByteOffset(ref charB, num));
				if (!Utf8Utility.AllBytesInUInt32AreAscii(num7 | num8))
				{
					goto IL_015e;
				}
				if (!Utf8Utility.UInt32OrdinalIgnoreCaseAscii(num7, num8))
				{
					return false;
				}
				num += 4;
				num2 -= 4;
			}
			if (num2 != 0)
			{
				switch (num2)
				{
				case 3:
					num7 = Unsafe.ReadUnaligned<ushort>(ref Unsafe.AddByteOffset(ref charA, num));
					num8 = Unsafe.ReadUnaligned<ushort>(ref Unsafe.AddByteOffset(ref charB, num));
					num += 2;
					num7 |= (uint)(Unsafe.AddByteOffset(ref charA, num) << 16);
					num8 |= (uint)(Unsafe.AddByteOffset(ref charB, num) << 16);
					break;
				case 2:
					num7 = Unsafe.ReadUnaligned<ushort>(ref Unsafe.AddByteOffset(ref charA, num));
					num8 = Unsafe.ReadUnaligned<ushort>(ref Unsafe.AddByteOffset(ref charB, num));
					break;
				default:
					num7 = Unsafe.AddByteOffset(ref charA, num);
					num8 = Unsafe.AddByteOffset(ref charB, num);
					break;
				}
				if (Utf8Utility.AllBytesInUInt32AreAscii(num7 | num8))
				{
					if (lengthA != lengthB)
					{
						return false;
					}
					if (num7 == num8)
					{
						return true;
					}
					return Utf8Utility.UInt32OrdinalIgnoreCaseAscii(num7, num8);
				}
				goto IL_015e;
			}
			return lengthA == lengthB;
			IL_015e:
			if (!Utf8Utility.AllBytesInUInt32AreAscii(num7) && !Utf8Utility.AllBytesInUInt32AreAscii(num8))
			{
				break;
			}
			return false;
		}
		num3 -= num2;
		return EqualsStringIgnoreCaseUtf8(ref Unsafe.AddByteOffset(ref charA, num), lengthA - num3, ref Unsafe.AddByteOffset(ref charB, num), lengthB - num3);
	}

	internal static bool StartsWithStringIgnoreCaseUtf8(ref byte source, int sourceLength, ref byte prefix, int prefixLength)
	{
		int num = Math.Min(sourceLength, prefixLength);
		int num2 = num;
		while (num != 0 && source <= 127 && prefix <= 127)
		{
			if (source == prefix || ((source | 0x20) == (prefix | 0x20) && char.IsAsciiLetter((char)source)))
			{
				num--;
				source = ref Unsafe.Add(ref source, 1);
				prefix = ref Unsafe.Add(ref prefix, 1);
				continue;
			}
			return false;
		}
		if (num == 0)
		{
			return prefixLength == 0;
		}
		num2 -= num;
		return StartsWithStringIgnoreCaseNonAsciiUtf8(ref source, sourceLength - num2, ref prefix, prefixLength - num2);
	}

	internal static bool StartsWithStringIgnoreCaseNonAsciiUtf8(ref byte source, int sourceLength, ref byte prefix, int prefixLength)
	{
		ReadOnlySpan<byte> source2 = MemoryMarshal.CreateReadOnlySpan(ref source, sourceLength);
		ReadOnlySpan<byte> source3 = MemoryMarshal.CreateReadOnlySpan(ref prefix, prefixLength);
		do
		{
			Rune result;
			int bytesConsumed;
			OperationStatus operationStatus = Rune.DecodeFromUtf8(source2, out result, out bytesConsumed);
			Rune result2;
			int bytesConsumed2;
			OperationStatus operationStatus2 = Rune.DecodeFromUtf8(source3, out result2, out bytesConsumed2);
			if (operationStatus != operationStatus2)
			{
				return false;
			}
			if (operationStatus == OperationStatus.Done)
			{
				if (Rune.ToUpperInvariant(result) != Rune.ToUpperInvariant(result2))
				{
					return false;
				}
			}
			else if (!source2.Slice(0, bytesConsumed).SequenceEqual(source3.Slice(0, bytesConsumed2)))
			{
				return false;
			}
			source2 = source2.Slice(bytesConsumed);
			source3 = source3.Slice(bytesConsumed2);
		}
		while (source3.Length != 0);
		return true;
	}

	private static bool StartsWithIgnoreCaseUtf8_Vector128(ref byte source, int sourceLength, ref byte prefix, int prefixLength)
	{
		nuint num = Math.Min((uint)sourceLength, (uint)prefixLength);
		nuint num2 = num - (nuint)Vector128<byte>.Count;
		nuint num3 = 0u;
		Vector128<byte> vector;
		Vector128<byte> vector2;
		while (true)
		{
			vector = Vector128.LoadUnsafe(ref source, num3);
			vector2 = Vector128.LoadUnsafe(ref prefix, num3);
			if (!Utf8Utility.AllBytesInVector128AreAscii(vector | vector2))
			{
				break;
			}
			if (!Utf8Utility.Vector128OrdinalIgnoreCaseAscii(vector, vector2))
			{
				return false;
			}
			num3 += (nuint)Vector128<byte>.Count;
			if (num3 > num2)
			{
				if (num3 == (uint)prefixLength)
				{
					return true;
				}
				return StartsWithIgnoreCaseUtf8_Scalar(ref Unsafe.Add(ref source, num3), (int)(num - num3), ref Unsafe.Add(ref prefix, num3), (int)(num - num3));
			}
		}
		if (Utf8Utility.AllBytesInVector128AreAscii(vector) || Utf8Utility.AllBytesInVector128AreAscii(vector2))
		{
			return false;
		}
		return StartsWithStringIgnoreCaseUtf8(ref Unsafe.Add(ref source, num3), sourceLength - (int)num3, ref Unsafe.Add(ref prefix, num3), prefixLength - (int)num3);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static bool StartsWithIgnoreCaseUtf8(ref byte source, int sourceLength, ref byte prefix, int prefixLength)
	{
		if (!Vector128.IsHardwareAccelerated || sourceLength < Vector128<byte>.Count || prefixLength < Vector128<byte>.Count)
		{
			return StartsWithIgnoreCaseUtf8_Scalar(ref source, sourceLength, ref prefix, prefixLength);
		}
		return StartsWithIgnoreCaseUtf8_Vector128(ref source, sourceLength, ref prefix, prefixLength);
	}

	internal static bool StartsWithIgnoreCaseUtf8_Scalar(ref byte source, int sourceLength, ref byte prefix, int prefixLength)
	{
		nint num = IntPtr.Zero;
		int num2 = Math.Min(sourceLength, prefixLength);
		int num3 = num2;
		ulong num4 = 0uL;
		ulong num5 = 0uL;
		while (true)
		{
			if ((uint)num2 >= 8u)
			{
				num4 = Unsafe.ReadUnaligned<ulong>(ref Unsafe.AddByteOffset(ref source, num));
				num5 = Unsafe.ReadUnaligned<ulong>(ref Unsafe.AddByteOffset(ref prefix, num));
				ulong num6 = num4 | num5;
				if (Utf8Utility.AllBytesInUInt32AreAscii((uint)((int)num6 | (int)(num6 >> 32))))
				{
					if (!Utf8Utility.UInt64OrdinalIgnoreCaseAscii(num4, num5))
					{
						return false;
					}
					num += 8;
					num2 -= 8;
					continue;
				}
				if (!Utf8Utility.AllBytesInUInt64AreAscii(num4) && !Utf8Utility.AllBytesInUInt64AreAscii(num5))
				{
					break;
				}
				return false;
			}
			uint num7 = 0u;
			uint num8 = 0u;
			if ((uint)num2 >= 4u)
			{
				num7 = Unsafe.ReadUnaligned<uint>(ref Unsafe.AddByteOffset(ref source, num));
				num8 = Unsafe.ReadUnaligned<uint>(ref Unsafe.AddByteOffset(ref prefix, num));
				if (!Utf8Utility.AllBytesInUInt32AreAscii(num7 | num8))
				{
					goto IL_016d;
				}
				if (!Utf8Utility.UInt32OrdinalIgnoreCaseAscii(num7, num8))
				{
					return false;
				}
				num += 4;
				num2 -= 4;
			}
			if (num2 != 0)
			{
				switch (num2)
				{
				case 3:
					num7 = Unsafe.ReadUnaligned<ushort>(ref Unsafe.AddByteOffset(ref source, num));
					num8 = Unsafe.ReadUnaligned<ushort>(ref Unsafe.AddByteOffset(ref prefix, num));
					num += 2;
					num7 |= (uint)(Unsafe.AddByteOffset(ref source, num) << 16);
					num8 |= (uint)(Unsafe.AddByteOffset(ref prefix, num) << 16);
					break;
				case 2:
					num7 = Unsafe.ReadUnaligned<ushort>(ref Unsafe.AddByteOffset(ref source, num));
					num8 = Unsafe.ReadUnaligned<ushort>(ref Unsafe.AddByteOffset(ref prefix, num));
					break;
				default:
					num7 = Unsafe.AddByteOffset(ref source, num);
					num8 = Unsafe.AddByteOffset(ref prefix, num);
					break;
				}
				if (!Utf8Utility.AllBytesInUInt32AreAscii(num7 | num8))
				{
					goto IL_016d;
				}
				if (num3 != prefixLength)
				{
					return false;
				}
				if (num7 == num8)
				{
					return true;
				}
				if (!Utf8Utility.UInt32OrdinalIgnoreCaseAscii(num7, num8))
				{
					return false;
				}
				num += 4;
				num2 -= 4;
			}
			return prefixLength <= sourceLength;
			IL_016d:
			if (!Utf8Utility.AllBytesInUInt32AreAscii(num7) && !Utf8Utility.AllBytesInUInt32AreAscii(num8))
			{
				break;
			}
			return false;
		}
		num3 -= num2;
		return StartsWithStringIgnoreCaseUtf8(ref Unsafe.AddByteOffset(ref source, num), sourceLength - num3, ref Unsafe.AddByteOffset(ref prefix, num), prefixLength - num3);
	}
}
