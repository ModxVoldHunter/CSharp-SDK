using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.Arm;
using System.Runtime.Intrinsics.X86;

namespace System.Buffers.Text;

public static class Base64
{
	private interface IBase64Validatable<T>
	{
		static abstract int IndexOfAnyExcept(ReadOnlySpan<T> span);

		static abstract bool IsWhiteSpace(T value);

		static abstract bool IsEncodingPad(T value);
	}

	[StructLayout(LayoutKind.Sequential, Size = 1)]
	private readonly struct Base64CharValidatable : IBase64Validatable<char>
	{
		private static readonly SearchValues<char> s_validBase64Chars = SearchValues.Create("ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+/");

		public static int IndexOfAnyExcept(ReadOnlySpan<char> span)
		{
			return span.IndexOfAnyExcept(s_validBase64Chars);
		}

		public static bool IsWhiteSpace(char value)
		{
			return Base64.IsWhiteSpace((int)value);
		}

		public static bool IsEncodingPad(char value)
		{
			return value == '=';
		}
	}

	[StructLayout(LayoutKind.Sequential, Size = 1)]
	private readonly struct Base64ByteValidatable : IBase64Validatable<byte>
	{
		private static readonly SearchValues<byte> s_validBase64Chars = SearchValues.Create(EncodingMap);

		public static int IndexOfAnyExcept(ReadOnlySpan<byte> span)
		{
			return span.IndexOfAnyExcept(s_validBase64Chars);
		}

		public static bool IsWhiteSpace(byte value)
		{
			return Base64.IsWhiteSpace((int)value);
		}

		public static bool IsEncodingPad(byte value)
		{
			return value == 61;
		}
	}

	internal static ReadOnlySpan<byte> EncodingMap => "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+/"u8;

	private static ReadOnlySpan<sbyte> DecodingMap => new sbyte[256]
	{
		-1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
		-1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
		-1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
		-1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
		-1, -1, -1, 62, -1, -1, -1, 63, 52, 53,
		54, 55, 56, 57, 58, 59, 60, 61, -1, -1,
		-1, -1, -1, -1, -1, 0, 1, 2, 3, 4,
		5, 6, 7, 8, 9, 10, 11, 12, 13, 14,
		15, 16, 17, 18, 19, 20, 21, 22, 23, 24,
		25, -1, -1, -1, -1, -1, -1, 26, 27, 28,
		29, 30, 31, 32, 33, 34, 35, 36, 37, 38,
		39, 40, 41, 42, 43, 44, 45, 46, 47, 48,
		49, 50, 51, -1, -1, -1, -1, -1, -1, -1,
		-1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
		-1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
		-1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
		-1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
		-1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
		-1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
		-1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
		-1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
		-1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
		-1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
		-1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
		-1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
		-1, -1, -1, -1, -1, -1
	};

	public unsafe static OperationStatus EncodeToUtf8(ReadOnlySpan<byte> bytes, Span<byte> utf8, out int bytesConsumed, out int bytesWritten, bool isFinalBlock = true)
	{
		if (bytes.IsEmpty)
		{
			bytesConsumed = 0;
			bytesWritten = 0;
			return OperationStatus.Done;
		}
		fixed (byte* ptr = &MemoryMarshal.GetReference(bytes))
		{
			fixed (byte* ptr2 = &MemoryMarshal.GetReference(utf8))
			{
				int length = bytes.Length;
				int length2 = utf8.Length;
				int num = ((length > 1610612733 || length2 < GetMaxEncodedToUtf8Length(length)) ? ((length2 >> 2) * 3) : length);
				byte* srcBytes = ptr;
				byte* destBytes = ptr2;
				byte* ptr3 = ptr + (uint)length;
				byte* ptr4 = ptr + (uint)num;
				if (num >= 16)
				{
					byte* ptr5 = ptr4 - 32;
					if (Avx2.IsSupported && ptr5 >= srcBytes)
					{
						Avx2Encode(ref srcBytes, ref destBytes, ptr5, num, length2, ptr, ptr2);
						if (srcBytes == ptr3)
						{
							goto IL_017e;
						}
					}
					ptr5 = ptr4 - 16;
					if (Ssse3.IsSupported ? true : false)
					{
						_ = BitConverter.IsLittleEndian;
						if (ptr5 >= srcBytes)
						{
							Vector128Encode(ref srcBytes, ref destBytes, ptr5, num, length2, ptr, ptr2);
							if (srcBytes == ptr3)
							{
								goto IL_017e;
							}
						}
					}
				}
				ref byte reference = ref MemoryMarshal.GetReference(EncodingMap);
				uint num2 = 0u;
				ptr4 -= 2;
				while (srcBytes < ptr4)
				{
					num2 = Encode(srcBytes, ref reference);
					Unsafe.WriteUnaligned(destBytes, num2);
					srcBytes += 3;
					destBytes += 4;
				}
				if (ptr4 + 2 == ptr3)
				{
					if (!isFinalBlock)
					{
						if (srcBytes != ptr3)
						{
							bytesConsumed = (int)(srcBytes - ptr);
							bytesWritten = (int)(destBytes - ptr2);
							return OperationStatus.NeedMoreData;
						}
					}
					else if (srcBytes + 1 == ptr3)
					{
						num2 = EncodeAndPadTwo(srcBytes, ref reference);
						Unsafe.WriteUnaligned(destBytes, num2);
						srcBytes++;
						destBytes += 4;
					}
					else if (srcBytes + 2 == ptr3)
					{
						num2 = EncodeAndPadOne(srcBytes, ref reference);
						Unsafe.WriteUnaligned(destBytes, num2);
						srcBytes += 2;
						destBytes += 4;
					}
					goto IL_017e;
				}
				bytesConsumed = (int)(srcBytes - ptr);
				bytesWritten = (int)(destBytes - ptr2);
				return OperationStatus.DestinationTooSmall;
				IL_017e:
				bytesConsumed = (int)(srcBytes - ptr);
				bytesWritten = (int)(destBytes - ptr2);
				return OperationStatus.Done;
			}
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static int GetMaxEncodedToUtf8Length(int length)
	{
		if ((uint)length > 1610612733u)
		{
			ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.length);
		}
		return (length + 2) / 3 * 4;
	}

	public unsafe static OperationStatus EncodeToUtf8InPlace(Span<byte> buffer, int dataLength, out int bytesWritten)
	{
		if (buffer.IsEmpty)
		{
			bytesWritten = 0;
			return OperationStatus.Done;
		}
		fixed (byte* ptr = &MemoryMarshal.GetReference(buffer))
		{
			int maxEncodedToUtf8Length = GetMaxEncodedToUtf8Length(dataLength);
			if (buffer.Length >= maxEncodedToUtf8Length)
			{
				int num = dataLength - dataLength / 3 * 3;
				uint num2 = (uint)(maxEncodedToUtf8Length - 4);
				uint num3 = (uint)(dataLength - num);
				uint num4 = 0u;
				ref byte reference = ref MemoryMarshal.GetReference(EncodingMap);
				if (num != 0)
				{
					num4 = ((num != 1) ? EncodeAndPadOne(ptr + num3, ref reference) : EncodeAndPadTwo(ptr + num3, ref reference));
					Unsafe.WriteUnaligned(ptr + num2, num4);
					num2 -= 4;
				}
				num3 -= 3;
				while ((int)num3 >= 0)
				{
					num4 = Encode(ptr + num3, ref reference);
					Unsafe.WriteUnaligned(ptr + num2, num4);
					num2 -= 4;
					num3 -= 3;
				}
				bytesWritten = maxEncodedToUtf8Length;
				return OperationStatus.Done;
			}
			bytesWritten = 0;
			return OperationStatus.DestinationTooSmall;
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[CompExactlyDependsOn(typeof(Avx2))]
	private unsafe static void Avx2Encode(ref byte* srcBytes, ref byte* destBytes, byte* srcEnd, int sourceLength, int destLength, byte* srcStart, byte* destStart)
	{
		Vector256<sbyte> mask = Vector256.Create(5, 4, 6, 5, 8, 7, 9, 8, 11, 10, 12, 11, 14, 13, 15, 14, 1, 0, 2, 1, 4, 3, 5, 4, 7, 6, 8, 7, 10, 9, 11, 10);
		Vector256<sbyte> value = Vector256.Create(65, 71, -4, -4, -4, -4, -4, -4, -4, -4, -4, -4, -19, -16, 0, 0, 65, 71, -4, -4, -4, -4, -4, -4, -4, -4, -4, -4, -19, -16, 0, 0);
		Vector256<sbyte> right = Vector256.Create(264305664).AsSByte();
		Vector256<sbyte> right2 = Vector256.Create(4129776).AsSByte();
		Vector256<ushort> right3 = Vector256.Create(67108928).AsUInt16();
		Vector256<short> right4 = Vector256.Create(16777232).AsInt16();
		Vector256<byte> right5 = Vector256.Create((byte)51);
		Vector256<sbyte> right6 = Vector256.Create((sbyte)25);
		byte* ptr = srcBytes;
		byte* ptr2 = destBytes;
		Vector256<sbyte> vector = Avx.LoadVector256(ptr).AsSByte();
		vector = Avx2.PermuteVar8x32(vector.AsInt32(), Vector256.Create(0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 2, 0, 0, 0, 3, 0, 0, 0, 4, 0, 0, 0, 5, 0, 0, 0, 6, 0, 0, 0).AsInt32()).AsSByte();
		ptr -= 4;
		while (true)
		{
			vector = Avx2.Shuffle(vector, mask);
			Vector256<sbyte> vector2 = Avx2.And(vector, right);
			Vector256<sbyte> vector3 = Avx2.And(vector, right2);
			Vector256<ushort> vector4 = Avx2.MultiplyHigh(vector2.AsUInt16(), right3);
			Vector256<short> vector5 = Avx2.MultiplyLow(vector3.AsInt16(), right4);
			vector = Avx2.Or(vector4.AsSByte(), vector5.AsSByte());
			Vector256<byte> vector6 = Avx2.SubtractSaturate(vector.AsByte(), right5);
			Vector256<sbyte> right7 = Avx2.CompareGreaterThan(vector, right6);
			Vector256<sbyte> mask2 = Avx2.Subtract(vector6.AsSByte(), right7);
			vector = Avx2.Add(vector, Avx2.Shuffle(value, mask2));
			Avx.Store(ptr2, vector.AsByte());
			ptr += 24;
			ptr2 += 32;
			if (ptr > srcEnd)
			{
				break;
			}
			vector = Avx.LoadVector256(ptr).AsSByte();
		}
		srcBytes = ptr + 4;
		destBytes = ptr2;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[CompExactlyDependsOn(typeof(Ssse3))]
	[CompExactlyDependsOn(typeof(AdvSimd.Arm64))]
	private unsafe static void Vector128Encode(ref byte* srcBytes, ref byte* destBytes, byte* srcEnd, int sourceLength, int destLength, byte* srcStart, byte* destStart)
	{
		Vector128<byte> right = Vector128.Create(16908289, 67437316, 117966343, 168495370).AsByte();
		Vector128<byte> left = Vector128.Create(4244391745u, 4244438268u, 4244438268u, 61677u).AsByte();
		Vector128<byte> vector = Vector128.Create(264305664).AsByte();
		Vector128<byte> vector2 = Vector128.Create(4129776).AsByte();
		Vector128<ushort> right2 = Vector128.Create(67108928).AsUInt16();
		Vector128<short> vector3 = Vector128.Create(16777232).AsInt16();
		Vector128<byte> right3 = Vector128.Create((byte)51);
		Vector128<sbyte> right4 = Vector128.Create((sbyte)25);
		Vector128<byte> mask8F = Vector128.Create((byte)143);
		byte* ptr = srcBytes;
		byte* ptr2 = destBytes;
		do
		{
			Vector128<byte> left2 = Vector128.LoadUnsafe(ref *ptr);
			left2 = SimdShuffle(left2, right, mask8F);
			Vector128<byte> vector4 = left2 & vector;
			Vector128<byte> vector5 = left2 & vector2;
			Vector128<ushort> vector6;
			if (Ssse3.IsSupported)
			{
				vector6 = Sse2.MultiplyHigh(vector4.AsUInt16(), right2);
			}
			else
			{
				Vector128<ushort> right5 = Vector128.ShiftRightLogical(AdvSimd.Arm64.UnzipOdd(vector4.AsUInt16(), vector4.AsUInt16()), 6);
				Vector128<ushort> left3 = Vector128.ShiftRightLogical(AdvSimd.Arm64.UnzipEven(vector4.AsUInt16(), vector4.AsUInt16()), 10);
				vector6 = AdvSimd.Arm64.ZipLow(left3, right5);
			}
			Vector128<short> vector7 = vector5.AsInt16() * vector3;
			left2 = vector6.AsByte() | vector7.AsByte();
			Vector128<byte> vector8 = ((!Ssse3.IsSupported) ? AdvSimd.SubtractSaturate(left2.AsByte(), right3) : Sse2.SubtractSaturate(left2.AsByte(), right3));
			Vector128<sbyte> vector9 = Vector128.GreaterThan(left2.AsSByte(), right4);
			Vector128<sbyte> vector10 = vector8.AsSByte() - vector9;
			left2 += SimdShuffle(left, vector10.AsByte(), mask8F);
			left2.Store(ptr2);
			ptr += 12;
			ptr2 += 16;
		}
		while (ptr <= srcEnd);
		srcBytes = ptr;
		destBytes = ptr2;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private unsafe static uint Encode(byte* threeBytes, ref byte encodingMap)
	{
		uint num = *threeBytes;
		uint num2 = threeBytes[1];
		uint num3 = threeBytes[2];
		uint num4 = (num << 16) | (num2 << 8) | num3;
		uint num5 = Unsafe.Add(ref encodingMap, (nint)(num4 >> 18));
		uint num6 = Unsafe.Add(ref encodingMap, (nint)((num4 >> 12) & 0x3F));
		uint num7 = Unsafe.Add(ref encodingMap, (nint)((num4 >> 6) & 0x3F));
		uint num8 = Unsafe.Add(ref encodingMap, (nint)(num4 & 0x3F));
		_ = BitConverter.IsLittleEndian;
		return num5 | (num6 << 8) | (num7 << 16) | (num8 << 24);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private unsafe static uint EncodeAndPadOne(byte* twoBytes, ref byte encodingMap)
	{
		uint num = *twoBytes;
		uint num2 = twoBytes[1];
		uint num3 = (num << 16) | (num2 << 8);
		uint num4 = Unsafe.Add(ref encodingMap, (nint)(num3 >> 18));
		uint num5 = Unsafe.Add(ref encodingMap, (nint)((num3 >> 12) & 0x3F));
		uint num6 = Unsafe.Add(ref encodingMap, (nint)((num3 >> 6) & 0x3F));
		_ = BitConverter.IsLittleEndian;
		return num4 | (num5 << 8) | (num6 << 16) | 0x3D000000u;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private unsafe static uint EncodeAndPadTwo(byte* oneByte, ref byte encodingMap)
	{
		uint num = *oneByte;
		uint num2 = num << 8;
		uint num3 = Unsafe.Add(ref encodingMap, (nint)(num2 >> 10));
		uint num4 = Unsafe.Add(ref encodingMap, (nint)((num2 >> 4) & 0x3F));
		_ = BitConverter.IsLittleEndian;
		return num3 | (num4 << 8) | 0x3D0000u | 0x3D000000u;
	}

	public static OperationStatus DecodeFromUtf8(ReadOnlySpan<byte> utf8, Span<byte> bytes, out int bytesConsumed, out int bytesWritten, bool isFinalBlock = true)
	{
		return DecodeFromUtf8(utf8, bytes, out bytesConsumed, out bytesWritten, isFinalBlock, ignoreWhiteSpace: true);
	}

	private unsafe static OperationStatus DecodeFromUtf8(ReadOnlySpan<byte> utf8, Span<byte> bytes, out int bytesConsumed, out int bytesWritten, bool isFinalBlock, bool ignoreWhiteSpace)
	{
		if (utf8.IsEmpty)
		{
			bytesConsumed = 0;
			bytesWritten = 0;
			return OperationStatus.Done;
		}
		fixed (byte* ptr = &MemoryMarshal.GetReference(utf8))
		{
			fixed (byte* ptr2 = &MemoryMarshal.GetReference(bytes))
			{
				int num = utf8.Length & -4;
				int length = bytes.Length;
				int num2 = num;
				int maxDecodedFromUtf8Length = GetMaxDecodedFromUtf8Length(num);
				if (length < maxDecodedFromUtf8Length - 2)
				{
					num2 = length / 3 * 4;
				}
				byte* srcBytes = ptr;
				byte* destBytes = ptr2;
				byte* ptr3 = ptr + (uint)num;
				byte* ptr4 = ptr + (uint)num2;
				if (num2 >= 24)
				{
					byte* ptr5 = ptr4 - 45;
					if (Avx2.IsSupported && ptr5 >= srcBytes)
					{
						Avx2Decode(ref srcBytes, ref destBytes, ptr5, num2, length, ptr, ptr2);
						if (srcBytes == ptr3)
						{
							goto IL_028c;
						}
					}
					ptr5 = ptr4 - 24;
					if (Ssse3.IsSupported ? true : false)
					{
						_ = BitConverter.IsLittleEndian;
						if (ptr5 >= srcBytes)
						{
							Vector128Decode(ref srcBytes, ref destBytes, ptr5, num2, length, ptr, ptr2);
							if (srcBytes == ptr3)
							{
								goto IL_028c;
							}
						}
					}
				}
				int num3 = (isFinalBlock ? 4 : 0);
				num2 = ((length < maxDecodedFromUtf8Length) ? (length / 3 * 4) : (num - num3));
				ref sbyte reference = ref MemoryMarshal.GetReference(DecodingMap);
				ptr4 = ptr + num2;
				while (true)
				{
					if (srcBytes < ptr4)
					{
						int num4 = Decode(srcBytes, ref reference);
						if (num4 >= 0)
						{
							WriteThreeLowOrderBytes(destBytes, num4);
							srcBytes += 4;
							destBytes += 3;
							continue;
						}
					}
					else
					{
						if (num2 != num - num3)
						{
							goto IL_02a2;
						}
						if (srcBytes == ptr3)
						{
							if (!isFinalBlock)
							{
								if (srcBytes == ptr + utf8.Length)
								{
									break;
								}
								bytesConsumed = (int)(srcBytes - ptr);
								bytesWritten = (int)(destBytes - ptr2);
								return OperationStatus.NeedMoreData;
							}
						}
						else
						{
							uint num5 = ptr3[-4];
							uint num6 = ptr3[-3];
							uint num7 = ptr3[-2];
							uint num8 = ptr3[-1];
							int num9 = Unsafe.Add(ref reference, (nint)num5);
							int num10 = Unsafe.Add(ref reference, (nint)num6);
							num9 <<= 18;
							num10 <<= 12;
							num9 |= num10;
							byte* ptr6 = ptr2 + (uint)length;
							if (num8 != 61)
							{
								int num11 = Unsafe.Add(ref reference, (nint)num7);
								int num12 = Unsafe.Add(ref reference, (nint)num8);
								num11 <<= 6;
								num9 |= num12;
								num9 |= num11;
								if (num9 >= 0)
								{
									if (destBytes + 3 <= ptr6)
									{
										WriteThreeLowOrderBytes(destBytes, num9);
										destBytes += 3;
										goto IL_027b;
									}
									goto IL_02a2;
								}
							}
							else if (num7 != 61)
							{
								int num13 = Unsafe.Add(ref reference, (nint)num7);
								num13 <<= 6;
								num9 |= num13;
								if (num9 >= 0)
								{
									if (destBytes + 2 <= ptr6)
									{
										*destBytes = (byte)(num9 >> 16);
										destBytes[1] = (byte)(num9 >> 8);
										destBytes += 2;
										goto IL_027b;
									}
									goto IL_02a2;
								}
							}
							else if (num9 >= 0)
							{
								if (destBytes + 1 <= ptr6)
								{
									*destBytes = (byte)(num9 >> 16);
									destBytes++;
									goto IL_027b;
								}
								goto IL_02a2;
							}
						}
					}
					goto IL_02e1;
					IL_027b:
					srcBytes += 4;
					if (num == utf8.Length)
					{
						break;
					}
					goto IL_02e1;
					IL_02a2:
					if (!(num != utf8.Length && isFinalBlock))
					{
						bytesConsumed = (int)(srcBytes - ptr);
						bytesWritten = (int)(destBytes - ptr2);
						return OperationStatus.DestinationTooSmall;
					}
					goto IL_02e1;
					IL_02e1:
					bytesConsumed = (int)(srcBytes - ptr);
					bytesWritten = (int)(destBytes - ptr2);
					if (!ignoreWhiteSpace)
					{
						return OperationStatus.InvalidData;
					}
					return InvalidDataFallback(utf8, bytes, ref bytesConsumed, ref bytesWritten, isFinalBlock);
				}
				goto IL_028c;
				IL_028c:
				bytesConsumed = (int)(srcBytes - ptr);
				bytesWritten = (int)(destBytes - ptr2);
				return OperationStatus.Done;
			}
		}
		static OperationStatus InvalidDataFallback(ReadOnlySpan<byte> utf8, Span<byte> bytes, ref int bytesConsumed, ref int bytesWritten, bool isFinalBlock)
		{
			utf8 = utf8.Slice(bytesConsumed);
			bytes = bytes.Slice(bytesWritten);
			OperationStatus operationStatus;
			do
			{
				int bytesConsumed2 = IndexOfAnyExceptWhiteSpace(utf8);
				if (bytesConsumed2 < 0)
				{
					bytesConsumed += utf8.Length;
					operationStatus = OperationStatus.Done;
					break;
				}
				if (bytesConsumed2 == 0)
				{
					return DecodeWithWhiteSpaceBlockwise(utf8, bytes, ref bytesConsumed, ref bytesWritten, isFinalBlock);
				}
				bytesConsumed += bytesConsumed2;
				utf8 = utf8.Slice(bytesConsumed2);
				operationStatus = DecodeFromUtf8(utf8, bytes, out bytesConsumed2, out var bytesWritten2, isFinalBlock, ignoreWhiteSpace: false);
				bytesConsumed += bytesConsumed2;
				bytesWritten += bytesWritten2;
				if (operationStatus != OperationStatus.InvalidData)
				{
					break;
				}
				utf8 = utf8.Slice(bytesConsumed2);
				bytes = bytes.Slice(bytesWritten2);
			}
			while (!utf8.IsEmpty);
			return operationStatus;
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static int GetMaxDecodedFromUtf8Length(int length)
	{
		if (length < 0)
		{
			ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.length);
		}
		return (length >> 2) * 3;
	}

	public static OperationStatus DecodeFromUtf8InPlace(Span<byte> buffer, out int bytesWritten)
	{
		return DecodeFromUtf8InPlace(buffer, out bytesWritten, ignoreWhiteSpace: true);
	}

	private unsafe static OperationStatus DecodeFromUtf8InPlace(Span<byte> buffer, out int bytesWritten, bool ignoreWhiteSpace)
	{
		if (buffer.IsEmpty)
		{
			bytesWritten = 0;
			return OperationStatus.Done;
		}
		fixed (byte* ptr = &MemoryMarshal.GetReference(buffer))
		{
			uint length = (uint)buffer.Length;
			uint num = 0u;
			uint num2 = 0u;
			if (length % 4 == 0)
			{
				ref sbyte reference = ref MemoryMarshal.GetReference(DecodingMap);
				while (true)
				{
					if (num < length - 4)
					{
						int num3 = Decode(ptr + num, ref reference);
						if (num3 < 0)
						{
							break;
						}
						WriteThreeLowOrderBytes(ptr + num2, num3);
						num2 += 3;
						num += 4;
						continue;
					}
					uint num4 = ptr[length - 4];
					uint num5 = ptr[length - 3];
					uint num6 = ptr[length - 2];
					uint num7 = ptr[length - 1];
					int num8 = Unsafe.Add(ref reference, num4);
					int num9 = Unsafe.Add(ref reference, num5);
					num8 <<= 18;
					num9 <<= 12;
					num8 |= num9;
					if (num7 != 61)
					{
						int num10 = Unsafe.Add(ref reference, num6);
						int num11 = Unsafe.Add(ref reference, num7);
						num10 <<= 6;
						num8 |= num11;
						num8 |= num10;
						if (num8 < 0)
						{
							break;
						}
						WriteThreeLowOrderBytes(ptr + num2, num8);
						num2 += 3;
					}
					else if (num6 != 61)
					{
						int num12 = Unsafe.Add(ref reference, num6);
						num12 <<= 6;
						num8 |= num12;
						if (num8 < 0)
						{
							break;
						}
						ptr[num2] = (byte)(num8 >> 16);
						ptr[num2 + 1] = (byte)(num8 >> 8);
						num2 += 2;
					}
					else
					{
						if (num8 < 0)
						{
							break;
						}
						ptr[num2] = (byte)(num8 >> 16);
						num2++;
					}
					bytesWritten = (int)num2;
					return OperationStatus.Done;
				}
			}
			bytesWritten = (int)num2;
			if (!ignoreWhiteSpace)
			{
				return OperationStatus.InvalidData;
			}
			return DecodeWithWhiteSpaceFromUtf8InPlace(buffer, ref bytesWritten, num);
		}
	}

	private static OperationStatus DecodeWithWhiteSpaceBlockwise(ReadOnlySpan<byte> utf8, Span<byte> bytes, ref int bytesConsumed, ref int bytesWritten, bool isFinalBlock = true)
	{
		Span<byte> span = stackalloc byte[4];
		OperationStatus operationStatus = OperationStatus.Done;
		while (!utf8.IsEmpty)
		{
			int i = 0;
			int num = 0;
			int num2 = 0;
			for (; i < utf8.Length; i++)
			{
				if ((uint)num >= (uint)span.Length)
				{
					break;
				}
				if (IsWhiteSpace(utf8[i]))
				{
					num2++;
					continue;
				}
				span[num] = utf8[i];
				num++;
			}
			utf8 = utf8.Slice(i);
			bytesConsumed += num2;
			if (num == 0)
			{
				continue;
			}
			bool flag = utf8.Length >= 4 && num == 4;
			bool flag2 = !flag;
			if (flag)
			{
				int paddingCount = GetPaddingCount(ref span[span.Length - 1]);
				if (paddingCount > 0)
				{
					flag = false;
					flag2 = true;
				}
			}
			if (flag2 && !isFinalBlock)
			{
				flag2 = false;
			}
			operationStatus = DecodeFromUtf8(span.Slice(0, num), bytes, out var bytesConsumed2, out var bytesWritten2, flag2, ignoreWhiteSpace: false);
			bytesConsumed += bytesConsumed2;
			bytesWritten += bytesWritten2;
			if (operationStatus != 0)
			{
				return operationStatus;
			}
			if (!flag)
			{
				for (int j = 0; j < utf8.Length; j++)
				{
					if (!IsWhiteSpace(utf8[j]))
					{
						bytesConsumed -= bytesConsumed2;
						bytesWritten -= bytesWritten2;
						return OperationStatus.InvalidData;
					}
					bytesConsumed++;
				}
				break;
			}
			bytes = bytes.Slice(bytesWritten2);
		}
		return operationStatus;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static int GetPaddingCount(ref byte ptrToLastElement)
	{
		int num = 0;
		if (ptrToLastElement == 61)
		{
			num++;
		}
		if (Unsafe.Subtract(ref ptrToLastElement, 1) == 61)
		{
			num++;
		}
		return num;
	}

	private static OperationStatus DecodeWithWhiteSpaceFromUtf8InPlace(Span<byte> utf8, ref int destIndex, uint sourceIndex)
	{
		Span<byte> buffer = stackalloc byte[4];
		OperationStatus operationStatus = OperationStatus.Done;
		int num = destIndex;
		bool flag = false;
		int bytesWritten = 0;
		while (sourceIndex < (uint)utf8.Length)
		{
			int num2 = 0;
			while (num2 < 4 && sourceIndex < (uint)utf8.Length)
			{
				if (!IsWhiteSpace(utf8[(int)sourceIndex]))
				{
					buffer[num2] = utf8[(int)sourceIndex];
					num2++;
				}
				sourceIndex++;
			}
			switch (num2)
			{
			default:
				operationStatus = OperationStatus.InvalidData;
				break;
			case 4:
			{
				if (flag)
				{
					num -= bytesWritten;
					operationStatus = OperationStatus.InvalidData;
					break;
				}
				operationStatus = DecodeFromUtf8InPlace(buffer, out bytesWritten, ignoreWhiteSpace: false);
				num += bytesWritten;
				flag = bytesWritten < 3;
				if (operationStatus != 0)
				{
					break;
				}
				for (int i = 0; i < bytesWritten; i++)
				{
					utf8[num - bytesWritten + i] = buffer[i];
				}
				continue;
			}
			case 0:
				continue;
			}
			break;
		}
		destIndex = num;
		return operationStatus;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[CompExactlyDependsOn(typeof(Avx2))]
	private unsafe static void Avx2Decode(ref byte* srcBytes, ref byte* destBytes, byte* srcEnd, int sourceLength, int destLength, byte* srcStart, byte* destStart)
	{
		Vector256<sbyte> value = Vector256.Create(16, 16, 1, 2, 4, 8, 4, 8, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 1, 2, 4, 8, 4, 8, 16, 16, 16, 16, 16, 16, 16, 16);
		Vector256<sbyte> value2 = Vector256.Create(21, 17, 17, 17, 17, 17, 17, 17, 17, 17, 19, 26, 27, 27, 27, 26, 21, 17, 17, 17, 17, 17, 17, 17, 17, 17, 19, 26, 27, 27, 27, 26);
		Vector256<sbyte> value3 = Vector256.Create(0, 16, 19, 4, -65, -65, -71, -71, 0, 0, 0, 0, 0, 0, 0, 0, 0, 16, 19, 4, -65, -65, -71, -71, 0, 0, 0, 0, 0, 0, 0, 0);
		Vector256<sbyte> mask = Vector256.Create(2, 1, 0, 6, 5, 4, 10, 9, 8, 14, 13, 12, -1, -1, -1, -1, 2, 1, 0, 6, 5, 4, 10, 9, 8, 14, 13, 12, -1, -1, -1, -1);
		Vector256<int> control = Vector256.Create(0, 0, 0, 0, 1, 0, 0, 0, 2, 0, 0, 0, 4, 0, 0, 0, 5, 0, 0, 0, 6, 0, 0, 0, -1, -1, -1, -1, -1, -1, -1, -1).AsInt32();
		Vector256<sbyte> right = Vector256.Create((sbyte)47);
		Vector256<sbyte> right2 = Vector256.Create(20971840).AsSByte();
		Vector256<short> right3 = Vector256.Create(69632).AsInt16();
		byte* ptr = srcBytes;
		byte* ptr2 = destBytes;
		do
		{
			Vector256<sbyte> vector = Avx.LoadVector256(ptr).AsSByte();
			Vector256<sbyte> vector2 = Avx2.And(Avx2.ShiftRightLogical(vector.AsInt32(), 4).AsSByte(), right);
			Vector256<sbyte> mask2 = Avx2.And(vector, right);
			Vector256<sbyte> right4 = Avx2.Shuffle(value, vector2);
			Vector256<sbyte> left = Avx2.Shuffle(value2, mask2);
			if (!Avx.TestZ(left, right4))
			{
				break;
			}
			Vector256<sbyte> left2 = Avx2.CompareEqual(vector, right);
			Vector256<sbyte> right5 = Avx2.Shuffle(value3, Avx2.Add(left2, vector2));
			vector = Avx2.Add(vector, right5);
			Vector256<short> left3 = Avx2.MultiplyAddAdjacent(vector.AsByte(), right2);
			Vector256<int> vector3 = Avx2.MultiplyAddAdjacent(left3, right3);
			vector3 = Avx2.Shuffle(vector3.AsSByte(), mask).AsInt32();
			vector = Avx2.PermuteVar8x32(vector3, control).AsSByte();
			Avx.Store(ptr2, vector.AsByte());
			ptr += 32;
			ptr2 += 24;
		}
		while (ptr <= srcEnd);
		srcBytes = ptr;
		destBytes = ptr2;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[CompExactlyDependsOn(typeof(Ssse3))]
	[CompExactlyDependsOn(typeof(AdvSimd.Arm64))]
	private static Vector128<byte> SimdShuffle(Vector128<byte> left, Vector128<byte> right, Vector128<byte> mask8F)
	{
		if (false)
		{
		}
		return Vector128.ShuffleUnsafe(left, right);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[CompExactlyDependsOn(typeof(AdvSimd.Arm64))]
	[CompExactlyDependsOn(typeof(Ssse3))]
	private unsafe static void Vector128Decode(ref byte* srcBytes, ref byte* destBytes, byte* srcEnd, int sourceLength, int destLength, byte* srcStart, byte* destStart)
	{
		Vector128<byte> left = Vector128.Create(33624080, 134481924, 269488144, 269488144).AsByte();
		Vector128<byte> left2 = Vector128.Create(286331157, 286331153, 437457169, 437984027).AsByte();
		Vector128<sbyte> vector = Vector128.Create(68358144u, 3115958207u, 0u, 0u).AsSByte();
		Vector128<sbyte> vector2 = Vector128.Create(100663554u, 151651333u, 202182152u, uint.MaxValue).AsSByte();
		Vector128<byte> vector3 = Vector128.Create(20971840).AsByte();
		Vector128<short> right = Vector128.Create(69632).AsInt16();
		Vector128<byte> vector4 = Vector128.Create((byte)1);
		Vector128<byte> vector5 = Vector128.Create((byte)47);
		Vector128<byte> mask8F = Vector128.Create((byte)143);
		byte* ptr = srcBytes;
		byte* ptr2 = destBytes;
		do
		{
			Vector128<byte> vector6 = Vector128.LoadUnsafe(ref *ptr);
			Vector128<byte> vector7 = Vector128.ShiftRightLogical(vector6.AsInt32(), 4).AsByte() & vector5;
			Vector128<byte> right2 = vector6 & vector5;
			Vector128<byte> vector8 = SimdShuffle(left, vector7, mask8F);
			Vector128<byte> vector9 = SimdShuffle(left2, right2, mask8F);
			if ((vector9 & vector8) != Vector128<byte>.Zero)
			{
				break;
			}
			Vector128<byte> vector10 = Vector128.Equals(vector6, vector5);
			Vector128<byte> vector11 = SimdShuffle(vector.AsByte(), vector10 + vector7, mask8F);
			vector6 += vector11;
			Vector128<short> left3;
			if (Ssse3.IsSupported)
			{
				left3 = Ssse3.MultiplyAddAdjacent(vector6.AsByte(), vector3.AsSByte());
			}
			else
			{
				Vector128<ushort> left4 = AdvSimd.ShiftLeftLogicalWideningLower(AdvSimd.Arm64.UnzipEven(vector6, vector4).GetLower(), 6);
				Vector128<ushort> right3 = AdvSimd.Arm64.TransposeOdd(vector6, Vector128<byte>.Zero).AsUInt16();
				left3 = Vector128.Add(left4, right3).AsInt16();
			}
			Vector128<int> vector12;
			if (Ssse3.IsSupported)
			{
				vector12 = Sse2.MultiplyAddAdjacent(left3, right);
			}
			else
			{
				Vector128<int> left5 = AdvSimd.ShiftLeftLogicalWideningLower(AdvSimd.Arm64.UnzipEven(left3, vector4.AsInt16()).GetLower(), 12);
				Vector128<int> right4 = AdvSimd.Arm64.TransposeOdd(left3, Vector128<short>.Zero).AsInt32();
				vector12 = Vector128.Add(left5, right4).AsInt32();
			}
			vector6 = SimdShuffle(vector12.AsByte(), vector2.AsByte(), mask8F);
			vector6.Store(ptr2);
			ptr += 16;
			ptr2 += 12;
		}
		while (ptr <= srcEnd);
		srcBytes = ptr;
		destBytes = ptr2;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private unsafe static int Decode(byte* encodedBytes, ref sbyte decodingMap)
	{
		uint num = *encodedBytes;
		uint num2 = encodedBytes[1];
		uint num3 = encodedBytes[2];
		uint num4 = encodedBytes[3];
		int num5 = Unsafe.Add(ref decodingMap, num);
		int num6 = Unsafe.Add(ref decodingMap, num2);
		int num7 = Unsafe.Add(ref decodingMap, num3);
		int num8 = Unsafe.Add(ref decodingMap, num4);
		num5 <<= 18;
		num6 <<= 12;
		num7 <<= 6;
		num5 |= num8;
		num6 |= num7;
		return num5 | num6;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private unsafe static void WriteThreeLowOrderBytes(byte* destination, int value)
	{
		*destination = (byte)(value >> 16);
		destination[1] = (byte)(value >> 8);
		destination[2] = (byte)value;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static int IndexOfAnyExceptWhiteSpace(ReadOnlySpan<byte> span)
	{
		for (int i = 0; i < span.Length; i++)
		{
			if (!IsWhiteSpace(span[i]))
			{
				return i;
			}
		}
		return -1;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static bool IsWhiteSpace(int value)
	{
		_ = 1;
		ulong num = (uint)(value - 9);
		ulong num2 = (ulong)(-4035224166612336640L << (int)num);
		ulong num3 = num - 64;
		return (long)(num2 & num3) < 0L;
	}

	public static bool IsValid(ReadOnlySpan<char> base64Text)
	{
		int decodedLength;
		return IsValid<char, Base64CharValidatable>(base64Text, out decodedLength);
	}

	public static bool IsValid(ReadOnlySpan<char> base64Text, out int decodedLength)
	{
		return IsValid<char, Base64CharValidatable>(base64Text, out decodedLength);
	}

	public static bool IsValid(ReadOnlySpan<byte> base64TextUtf8)
	{
		int decodedLength;
		return IsValid<byte, Base64ByteValidatable>(base64TextUtf8, out decodedLength);
	}

	public static bool IsValid(ReadOnlySpan<byte> base64TextUtf8, out int decodedLength)
	{
		return IsValid<byte, Base64ByteValidatable>(base64TextUtf8, out decodedLength);
	}

	private static bool IsValid<T, TBase64Validatable>(ReadOnlySpan<T> base64Text, out int decodedLength) where TBase64Validatable : IBase64Validatable<T>
	{
		int num = 0;
		int num2 = 0;
		if (!base64Text.IsEmpty)
		{
			while (true)
			{
				int num3 = TBase64Validatable.IndexOfAnyExcept(base64Text);
				if ((uint)num3 >= (uint)base64Text.Length)
				{
					num += base64Text.Length;
				}
				else
				{
					num += num3;
					T value = base64Text[num3];
					base64Text = base64Text.Slice(num3 + 1);
					if (TBase64Validatable.IsWhiteSpace(value))
					{
						while (!base64Text.IsEmpty && TBase64Validatable.IsWhiteSpace(base64Text[0]))
						{
							base64Text = base64Text.Slice(1);
						}
						continue;
					}
					if (!TBase64Validatable.IsEncodingPad(value))
					{
						goto IL_010b;
					}
					num2 = 1;
					ReadOnlySpan<T> readOnlySpan = base64Text;
					for (int i = 0; i < readOnlySpan.Length; i++)
					{
						T value2 = readOnlySpan[i];
						if (TBase64Validatable.IsEncodingPad(value2))
						{
							if (num2 < 2)
							{
								num2++;
								continue;
							}
						}
						else if (TBase64Validatable.IsWhiteSpace(value2))
						{
							continue;
						}
						goto IL_010b;
					}
					num += num2;
				}
				if (num % 4 == 0)
				{
					break;
				}
				goto IL_010b;
				IL_010b:
				decodedLength = 0;
				return false;
			}
		}
		decodedLength = (int)((uint)num / 4u * 3) - num2;
		return true;
	}
}
