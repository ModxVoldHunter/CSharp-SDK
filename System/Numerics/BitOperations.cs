using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics.X86;

namespace System.Numerics;

public static class BitOperations
{
	private static class Crc32Fallback
	{
		private static readonly uint[] s_crcTable = Crc32ReflectedTable.Generate(2197175160u);

		internal static uint Crc32C(uint crc, byte data)
		{
			crc = Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(s_crcTable), (nint)(byte)(crc ^ data)) ^ (crc >> 8);
			return crc;
		}

		internal static uint Crc32C(uint crc, ushort data)
		{
			ref uint arrayDataReference = ref MemoryMarshal.GetArrayDataReference(s_crcTable);
			crc = Unsafe.Add(ref arrayDataReference, (nint)(byte)(crc ^ (byte)data)) ^ (crc >> 8);
			data >>= 8;
			crc = Unsafe.Add(ref arrayDataReference, (nint)(byte)(crc ^ data)) ^ (crc >> 8);
			return crc;
		}

		internal static uint Crc32C(uint crc, uint data)
		{
			return Crc32CCore(ref MemoryMarshal.GetArrayDataReference(s_crcTable), crc, data);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static uint Crc32CCore(ref uint lookupTable, uint crc, uint data)
		{
			crc = Unsafe.Add(ref lookupTable, (nint)(byte)(crc ^ (byte)data)) ^ (crc >> 8);
			data >>= 8;
			crc = Unsafe.Add(ref lookupTable, (nint)(byte)(crc ^ (byte)data)) ^ (crc >> 8);
			data >>= 8;
			crc = Unsafe.Add(ref lookupTable, (nint)(byte)(crc ^ (byte)data)) ^ (crc >> 8);
			data >>= 8;
			crc = Unsafe.Add(ref lookupTable, (nint)(byte)(crc ^ data)) ^ (crc >> 8);
			return crc;
		}
	}

	private static ReadOnlySpan<byte> TrailingZeroCountDeBruijn => new byte[32]
	{
		0, 1, 28, 2, 29, 14, 24, 3, 30, 22,
		20, 15, 25, 17, 4, 8, 31, 27, 13, 23,
		21, 19, 16, 7, 26, 12, 18, 6, 11, 5,
		10, 9
	};

	private static ReadOnlySpan<byte> Log2DeBruijn => new byte[32]
	{
		0, 9, 1, 10, 13, 21, 2, 29, 11, 14,
		16, 18, 22, 25, 3, 30, 8, 12, 20, 28,
		15, 17, 24, 7, 19, 27, 23, 6, 26, 5,
		4, 31
	};

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool IsPow2(int value)
	{
		if ((value & (value - 1)) == 0)
		{
			return value > 0;
		}
		return false;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[CLSCompliant(false)]
	public static bool IsPow2(uint value)
	{
		if ((value & (value - 1)) == 0)
		{
			return value != 0;
		}
		return false;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool IsPow2(long value)
	{
		if ((value & (value - 1)) == 0L)
		{
			return value > 0;
		}
		return false;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[CLSCompliant(false)]
	public static bool IsPow2(ulong value)
	{
		if ((value & (value - 1)) == 0L)
		{
			return value != 0;
		}
		return false;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool IsPow2(nint value)
	{
		if ((value & (value - 1)) == 0)
		{
			return value > 0;
		}
		return false;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[CLSCompliant(false)]
	public static bool IsPow2(nuint value)
	{
		if ((value & (value - 1)) == 0)
		{
			return value != 0;
		}
		return false;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[CLSCompliant(false)]
	public static uint RoundUpToPowerOf2(uint value)
	{
		if (!X86Base.IsSupported)
		{
			_ = 0;
			if (0 == 0)
			{
				value--;
				value |= value >> 1;
				value |= value >> 2;
				value |= value >> 4;
				value |= value >> 8;
				value |= value >> 16;
				return value + 1;
			}
		}
		return (uint)(4294967296uL >> LeadingZeroCount(value - 1));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[CLSCompliant(false)]
	public static ulong RoundUpToPowerOf2(ulong value)
	{
		if (!X86Base.X64.IsSupported)
		{
			_ = 0;
			if (0 == 0)
			{
				value--;
				value |= value >> 1;
				value |= value >> 2;
				value |= value >> 4;
				value |= value >> 8;
				value |= value >> 16;
				value |= value >> 32;
				return value + 1;
			}
		}
		int num = 64 - LeadingZeroCount(value - 1);
		return (1uL ^ (ulong)(num >> 6)) << num;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[CLSCompliant(false)]
	public static nuint RoundUpToPowerOf2(nuint value)
	{
		return (nuint)RoundUpToPowerOf2((ulong)value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static int LeadingZeroCount(Int128 value)
	{
		ulong upper = value.Upper;
		if (upper == 0L)
		{
			return 64 + LeadingZeroCount(value.Lower);
		}
		return LeadingZeroCount(upper);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[CLSCompliant(false)]
	public static int LeadingZeroCount(uint value)
	{
		if (Lzcnt.IsSupported)
		{
			return (int)Lzcnt.LeadingZeroCount(value);
		}
		if (false)
		{
		}
		if (false)
		{
		}
		if (value == 0)
		{
			return 32;
		}
		if (X86Base.IsSupported)
		{
			return (int)(0x1F ^ X86Base.BitScanReverse(value));
		}
		return 0x1F ^ Log2SoftwareFallback(value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[CLSCompliant(false)]
	public static int LeadingZeroCount(ulong value)
	{
		if (Lzcnt.X64.IsSupported)
		{
			return (int)Lzcnt.X64.LeadingZeroCount(value);
		}
		if (false)
		{
		}
		if (false)
		{
		}
		if (X86Base.X64.IsSupported)
		{
			if (value != 0L)
			{
				return 0x3F ^ (int)X86Base.X64.BitScanReverse(value);
			}
			return 64;
		}
		uint num = (uint)(value >> 32);
		if (num == 0)
		{
			return 32 + LeadingZeroCount((uint)value);
		}
		return LeadingZeroCount(num);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static int LeadingZeroCount(UInt128 value)
	{
		ulong upper = value.Upper;
		if (upper == 0L)
		{
			return 64 + LeadingZeroCount(value.Lower);
		}
		return LeadingZeroCount(upper);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[CLSCompliant(false)]
	public static int LeadingZeroCount(nuint value)
	{
		return LeadingZeroCount((ulong)value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[CLSCompliant(false)]
	public static int Log2(uint value)
	{
		value |= 1u;
		if (Lzcnt.IsSupported)
		{
			return (int)(0x1F ^ Lzcnt.LeadingZeroCount(value));
		}
		if (false)
		{
		}
		if (false)
		{
		}
		if (X86Base.IsSupported)
		{
			return (int)X86Base.BitScanReverse(value);
		}
		return Log2SoftwareFallback(value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[CLSCompliant(false)]
	public static int Log2(ulong value)
	{
		value |= 1;
		if (Lzcnt.X64.IsSupported)
		{
			return 0x3F ^ (int)Lzcnt.X64.LeadingZeroCount(value);
		}
		if (false)
		{
		}
		if (X86Base.X64.IsSupported)
		{
			return (int)X86Base.X64.BitScanReverse(value);
		}
		if (false)
		{
		}
		uint num = (uint)(value >> 32);
		if (num == 0)
		{
			return Log2((uint)value);
		}
		return 32 + Log2(num);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[CLSCompliant(false)]
	public static int Log2(nuint value)
	{
		return Log2((ulong)value);
	}

	private static int Log2SoftwareFallback(uint value)
	{
		value |= value >> 1;
		value |= value >> 2;
		value |= value >> 4;
		value |= value >> 8;
		value |= value >> 16;
		return Unsafe.AddByteOffset(ref MemoryMarshal.GetReference(Log2DeBruijn), (int)(value * 130329821 >> 27));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static int Log2Ceiling(ulong value)
	{
		int num = Log2(value);
		if (PopCount(value) != 1)
		{
			num++;
		}
		return num;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[CLSCompliant(false)]
	public static int PopCount(uint value)
	{
		if (Popcnt.IsSupported)
		{
			return (int)Popcnt.PopCount(value);
		}
		if (false)
		{
		}
		return SoftwareFallback(value);
		static int SoftwareFallback(uint value)
		{
			value -= (value >> 1) & 0x55555555;
			value = (value & 0x33333333) + ((value >> 2) & 0x33333333);
			value = ((value + (value >> 4)) & 0xF0F0F0F) * 16843009 >> 24;
			return (int)value;
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[CLSCompliant(false)]
	public static int PopCount(ulong value)
	{
		if (Popcnt.X64.IsSupported)
		{
			return (int)Popcnt.X64.PopCount(value);
		}
		if (false)
		{
		}
		return SoftwareFallback(value);
		static int SoftwareFallback(ulong value)
		{
			value -= (value >> 1) & 0x5555555555555555L;
			value = (value & 0x3333333333333333L) + ((value >> 2) & 0x3333333333333333L);
			value = ((value + (value >> 4)) & 0xF0F0F0F0F0F0F0FL) * 72340172838076673L >> 56;
			return (int)value;
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[CLSCompliant(false)]
	public static int PopCount(nuint value)
	{
		return PopCount((ulong)value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static int TrailingZeroCount(int value)
	{
		return TrailingZeroCount((uint)value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[CLSCompliant(false)]
	public static int TrailingZeroCount(uint value)
	{
		if (Bmi1.IsSupported)
		{
			return (int)Bmi1.TrailingZeroCount(value);
		}
		if (false)
		{
		}
		if (false)
		{
		}
		if (value == 0)
		{
			return 32;
		}
		if (X86Base.IsSupported)
		{
			return (int)X86Base.BitScanForward(value);
		}
		return Unsafe.AddByteOffset(ref MemoryMarshal.GetReference(TrailingZeroCountDeBruijn), (int)((value & (0 - value)) * 125613361 >> 27));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static int TrailingZeroCount(long value)
	{
		return TrailingZeroCount((ulong)value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[CLSCompliant(false)]
	public static int TrailingZeroCount(ulong value)
	{
		if (Bmi1.X64.IsSupported)
		{
			return (int)Bmi1.X64.TrailingZeroCount(value);
		}
		if (false)
		{
		}
		if (false)
		{
		}
		if (X86Base.X64.IsSupported)
		{
			if (value != 0L)
			{
				return (int)X86Base.X64.BitScanForward(value);
			}
			return 64;
		}
		uint num = (uint)value;
		if (num == 0)
		{
			return 32 + TrailingZeroCount((uint)(value >> 32));
		}
		return TrailingZeroCount(num);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static int TrailingZeroCount(nint value)
	{
		return TrailingZeroCount((ulong)(nuint)value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[CLSCompliant(false)]
	public static int TrailingZeroCount(nuint value)
	{
		return TrailingZeroCount((ulong)value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[CLSCompliant(false)]
	public static uint RotateLeft(uint value, int offset)
	{
		return (value << offset) | (value >> 32 - offset);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[CLSCompliant(false)]
	public static ulong RotateLeft(ulong value, int offset)
	{
		return (value << offset) | (value >> 64 - offset);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[CLSCompliant(false)]
	public static nuint RotateLeft(nuint value, int offset)
	{
		return (nuint)RotateLeft((ulong)value, offset);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[CLSCompliant(false)]
	public static uint RotateRight(uint value, int offset)
	{
		return (value >> offset) | (value << 32 - offset);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[CLSCompliant(false)]
	public static ulong RotateRight(ulong value, int offset)
	{
		return (value >> offset) | (value << 64 - offset);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[CLSCompliant(false)]
	public static nuint RotateRight(nuint value, int offset)
	{
		return (nuint)RotateRight((ulong)value, offset);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[CLSCompliant(false)]
	public static uint Crc32C(uint crc, byte data)
	{
		if (Sse42.IsSupported)
		{
			return Sse42.Crc32(crc, data);
		}
		if (false)
		{
		}
		return Crc32Fallback.Crc32C(crc, data);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[CLSCompliant(false)]
	public static uint Crc32C(uint crc, ushort data)
	{
		if (Sse42.IsSupported)
		{
			return Sse42.Crc32(crc, data);
		}
		if (false)
		{
		}
		return Crc32Fallback.Crc32C(crc, data);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[CLSCompliant(false)]
	public static uint Crc32C(uint crc, uint data)
	{
		if (Sse42.IsSupported)
		{
			return Sse42.Crc32(crc, data);
		}
		if (false)
		{
		}
		return Crc32Fallback.Crc32C(crc, data);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[CLSCompliant(false)]
	public static uint Crc32C(uint crc, ulong data)
	{
		if (Sse42.X64.IsSupported)
		{
			return (uint)Sse42.X64.Crc32(crc, data);
		}
		if (false)
		{
		}
		return Crc32C(Crc32C(crc, (uint)data), (uint)(data >> 32));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static uint ResetLowestSetBit(uint value)
	{
		return value & (value - 1);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static ulong ResetLowestSetBit(ulong value)
	{
		return value & (value - 1);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static uint FlipBit(uint value, int index)
	{
		return value ^ (uint)(1 << index);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static ulong FlipBit(ulong value, int index)
	{
		return value ^ (ulong)(1L << index);
	}
}
