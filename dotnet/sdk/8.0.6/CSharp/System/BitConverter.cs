using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System;

public static class BitConverter
{
	[Intrinsic]
	public static readonly bool IsLittleEndian = true;

	public static byte[] GetBytes(bool value)
	{
		return new byte[1] { value ? ((byte)1) : ((byte)0) };
	}

	public static bool TryWriteBytes(Span<byte> destination, bool value)
	{
		if (destination.Length < 1)
		{
			return false;
		}
		Unsafe.WriteUnaligned(ref MemoryMarshal.GetReference(destination), value ? ((byte)1) : ((byte)0));
		return true;
	}

	public static byte[] GetBytes(char value)
	{
		byte[] array = new byte[2];
		Unsafe.As<byte, char>(ref array[0]) = value;
		return array;
	}

	public static bool TryWriteBytes(Span<byte> destination, char value)
	{
		if (destination.Length < 2)
		{
			return false;
		}
		Unsafe.WriteUnaligned(ref MemoryMarshal.GetReference(destination), value);
		return true;
	}

	public static byte[] GetBytes(short value)
	{
		byte[] array = new byte[2];
		Unsafe.As<byte, short>(ref array[0]) = value;
		return array;
	}

	public static bool TryWriteBytes(Span<byte> destination, short value)
	{
		if (destination.Length < 2)
		{
			return false;
		}
		Unsafe.WriteUnaligned(ref MemoryMarshal.GetReference(destination), value);
		return true;
	}

	public static byte[] GetBytes(int value)
	{
		byte[] array = new byte[4];
		Unsafe.As<byte, int>(ref array[0]) = value;
		return array;
	}

	public static bool TryWriteBytes(Span<byte> destination, int value)
	{
		if (destination.Length < 4)
		{
			return false;
		}
		Unsafe.WriteUnaligned(ref MemoryMarshal.GetReference(destination), value);
		return true;
	}

	public static byte[] GetBytes(long value)
	{
		byte[] array = new byte[8];
		Unsafe.As<byte, long>(ref array[0]) = value;
		return array;
	}

	public static bool TryWriteBytes(Span<byte> destination, long value)
	{
		if (destination.Length < 8)
		{
			return false;
		}
		Unsafe.WriteUnaligned(ref MemoryMarshal.GetReference(destination), value);
		return true;
	}

	[CLSCompliant(false)]
	public static byte[] GetBytes(ushort value)
	{
		byte[] array = new byte[2];
		Unsafe.As<byte, ushort>(ref array[0]) = value;
		return array;
	}

	[CLSCompliant(false)]
	public static bool TryWriteBytes(Span<byte> destination, ushort value)
	{
		if (destination.Length < 2)
		{
			return false;
		}
		Unsafe.WriteUnaligned(ref MemoryMarshal.GetReference(destination), value);
		return true;
	}

	[CLSCompliant(false)]
	public static byte[] GetBytes(uint value)
	{
		byte[] array = new byte[4];
		Unsafe.As<byte, uint>(ref array[0]) = value;
		return array;
	}

	[CLSCompliant(false)]
	public static bool TryWriteBytes(Span<byte> destination, uint value)
	{
		if (destination.Length < 4)
		{
			return false;
		}
		Unsafe.WriteUnaligned(ref MemoryMarshal.GetReference(destination), value);
		return true;
	}

	[CLSCompliant(false)]
	public static byte[] GetBytes(ulong value)
	{
		byte[] array = new byte[8];
		Unsafe.As<byte, ulong>(ref array[0]) = value;
		return array;
	}

	[CLSCompliant(false)]
	public static bool TryWriteBytes(Span<byte> destination, ulong value)
	{
		if (destination.Length < 8)
		{
			return false;
		}
		Unsafe.WriteUnaligned(ref MemoryMarshal.GetReference(destination), value);
		return true;
	}

	public unsafe static byte[] GetBytes(Half value)
	{
		byte[] array = new byte[sizeof(Half)];
		Unsafe.As<byte, Half>(ref array[0]) = value;
		return array;
	}

	public unsafe static bool TryWriteBytes(Span<byte> destination, Half value)
	{
		if (destination.Length < sizeof(Half))
		{
			return false;
		}
		Unsafe.WriteUnaligned(ref MemoryMarshal.GetReference(destination), value);
		return true;
	}

	public static byte[] GetBytes(float value)
	{
		byte[] array = new byte[4];
		Unsafe.As<byte, float>(ref array[0]) = value;
		return array;
	}

	public static bool TryWriteBytes(Span<byte> destination, float value)
	{
		if (destination.Length < 4)
		{
			return false;
		}
		Unsafe.WriteUnaligned(ref MemoryMarshal.GetReference(destination), value);
		return true;
	}

	public static byte[] GetBytes(double value)
	{
		byte[] array = new byte[8];
		Unsafe.As<byte, double>(ref array[0]) = value;
		return array;
	}

	public static bool TryWriteBytes(Span<byte> destination, double value)
	{
		if (destination.Length < 8)
		{
			return false;
		}
		Unsafe.WriteUnaligned(ref MemoryMarshal.GetReference(destination), value);
		return true;
	}

	public static char ToChar(byte[] value, int startIndex)
	{
		return (char)ToInt16(value, startIndex);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static char ToChar(ReadOnlySpan<byte> value)
	{
		if (value.Length < 2)
		{
			ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.value);
		}
		return Unsafe.ReadUnaligned<char>(ref MemoryMarshal.GetReference(value));
	}

	public static short ToInt16(byte[] value, int startIndex)
	{
		if (value == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.value);
		}
		if ((uint)startIndex >= (uint)value.Length)
		{
			ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.startIndex, ExceptionResource.ArgumentOutOfRange_IndexMustBeLess);
		}
		if (startIndex > value.Length - 2)
		{
			ThrowHelper.ThrowArgumentException(ExceptionResource.Arg_ByteArrayTooSmallForValue, ExceptionArgument.value);
		}
		return Unsafe.ReadUnaligned<short>(ref value[startIndex]);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static short ToInt16(ReadOnlySpan<byte> value)
	{
		if (value.Length < 2)
		{
			ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.value);
		}
		return Unsafe.ReadUnaligned<short>(ref MemoryMarshal.GetReference(value));
	}

	public static int ToInt32(byte[] value, int startIndex)
	{
		if (value == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.value);
		}
		if ((uint)startIndex >= (uint)value.Length)
		{
			ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.startIndex, ExceptionResource.ArgumentOutOfRange_IndexMustBeLess);
		}
		if (startIndex > value.Length - 4)
		{
			ThrowHelper.ThrowArgumentException(ExceptionResource.Arg_ByteArrayTooSmallForValue, ExceptionArgument.value);
		}
		return Unsafe.ReadUnaligned<int>(ref value[startIndex]);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static int ToInt32(ReadOnlySpan<byte> value)
	{
		if (value.Length < 4)
		{
			ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.value);
		}
		return Unsafe.ReadUnaligned<int>(ref MemoryMarshal.GetReference(value));
	}

	public static long ToInt64(byte[] value, int startIndex)
	{
		if (value == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.value);
		}
		if ((uint)startIndex >= (uint)value.Length)
		{
			ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.startIndex, ExceptionResource.ArgumentOutOfRange_IndexMustBeLess);
		}
		if (startIndex > value.Length - 8)
		{
			ThrowHelper.ThrowArgumentException(ExceptionResource.Arg_ByteArrayTooSmallForValue, ExceptionArgument.value);
		}
		return Unsafe.ReadUnaligned<long>(ref value[startIndex]);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static long ToInt64(ReadOnlySpan<byte> value)
	{
		if (value.Length < 8)
		{
			ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.value);
		}
		return Unsafe.ReadUnaligned<long>(ref MemoryMarshal.GetReference(value));
	}

	[CLSCompliant(false)]
	public static ushort ToUInt16(byte[] value, int startIndex)
	{
		return (ushort)ToInt16(value, startIndex);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[CLSCompliant(false)]
	public static ushort ToUInt16(ReadOnlySpan<byte> value)
	{
		if (value.Length < 2)
		{
			ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.value);
		}
		return Unsafe.ReadUnaligned<ushort>(ref MemoryMarshal.GetReference(value));
	}

	[CLSCompliant(false)]
	public static uint ToUInt32(byte[] value, int startIndex)
	{
		return (uint)ToInt32(value, startIndex);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[CLSCompliant(false)]
	public static uint ToUInt32(ReadOnlySpan<byte> value)
	{
		if (value.Length < 4)
		{
			ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.value);
		}
		return Unsafe.ReadUnaligned<uint>(ref MemoryMarshal.GetReference(value));
	}

	[CLSCompliant(false)]
	public static ulong ToUInt64(byte[] value, int startIndex)
	{
		return (ulong)ToInt64(value, startIndex);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[CLSCompliant(false)]
	public static ulong ToUInt64(ReadOnlySpan<byte> value)
	{
		if (value.Length < 8)
		{
			ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.value);
		}
		return Unsafe.ReadUnaligned<ulong>(ref MemoryMarshal.GetReference(value));
	}

	public static Half ToHalf(byte[] value, int startIndex)
	{
		return Int16BitsToHalf(ToInt16(value, startIndex));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public unsafe static Half ToHalf(ReadOnlySpan<byte> value)
	{
		if (value.Length < sizeof(Half))
		{
			ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.value);
		}
		return Unsafe.ReadUnaligned<Half>(ref MemoryMarshal.GetReference(value));
	}

	public static float ToSingle(byte[] value, int startIndex)
	{
		return Int32BitsToSingle(ToInt32(value, startIndex));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static float ToSingle(ReadOnlySpan<byte> value)
	{
		if (value.Length < 4)
		{
			ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.value);
		}
		return Unsafe.ReadUnaligned<float>(ref MemoryMarshal.GetReference(value));
	}

	public static double ToDouble(byte[] value, int startIndex)
	{
		return Int64BitsToDouble(ToInt64(value, startIndex));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static double ToDouble(ReadOnlySpan<byte> value)
	{
		if (value.Length < 8)
		{
			ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.value);
		}
		return Unsafe.ReadUnaligned<double>(ref MemoryMarshal.GetReference(value));
	}

	public static string ToString(byte[] value, int startIndex, int length)
	{
		if (value == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.value);
		}
		if (startIndex < 0 || (startIndex >= value.Length && startIndex > 0))
		{
			ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.startIndex, ExceptionResource.ArgumentOutOfRange_IndexMustBeLess);
		}
		ArgumentOutOfRangeException.ThrowIfNegative(length, "length");
		if (startIndex > value.Length - length)
		{
			ThrowHelper.ThrowArgumentException(ExceptionResource.Arg_ByteArrayTooSmallForValue, ExceptionArgument.value);
		}
		if (length == 0)
		{
			return string.Empty;
		}
		ArgumentOutOfRangeException.ThrowIfGreaterThan(length, 715827882, "length");
		string text = string.FastAllocateString(length * 3 - 1);
		Span<char> span = new Span<char>(ref text.GetRawStringData(), text.Length);
		ReadOnlySpan<byte> readOnlySpan = new ReadOnlySpan<byte>(value, startIndex, length);
		int num = 0;
		int num2 = 0;
		byte b = readOnlySpan[num++];
		span[num2++] = HexConverter.ToCharUpper(b >> 4);
		span[num2++] = HexConverter.ToCharUpper(b);
		while (num < readOnlySpan.Length)
		{
			b = readOnlySpan[num++];
			span[num2++] = '-';
			span[num2++] = HexConverter.ToCharUpper(b >> 4);
			span[num2++] = HexConverter.ToCharUpper(b);
		}
		return text;
	}

	public static string ToString(byte[] value)
	{
		if (value == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.value);
		}
		return ToString(value, 0, value.Length);
	}

	public static string ToString(byte[] value, int startIndex)
	{
		if (value == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.value);
		}
		return ToString(value, startIndex, value.Length - startIndex);
	}

	public static bool ToBoolean(byte[] value, int startIndex)
	{
		if (value == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.value);
		}
		if (startIndex < 0)
		{
			ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.startIndex, ExceptionResource.ArgumentOutOfRange_IndexMustBeLess);
		}
		if (startIndex >= value.Length)
		{
			ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.startIndex, ExceptionResource.ArgumentOutOfRange_IndexMustBeLess);
		}
		return value[startIndex] != 0;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool ToBoolean(ReadOnlySpan<byte> value)
	{
		if (value.Length < 1)
		{
			ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.value);
		}
		return Unsafe.ReadUnaligned<byte>(ref MemoryMarshal.GetReference(value)) != 0;
	}

	[Intrinsic]
	public static long DoubleToInt64Bits(double value)
	{
		return Unsafe.BitCast<double, long>(value);
	}

	[Intrinsic]
	public static double Int64BitsToDouble(long value)
	{
		return Unsafe.BitCast<long, double>(value);
	}

	[Intrinsic]
	public static int SingleToInt32Bits(float value)
	{
		return Unsafe.BitCast<float, int>(value);
	}

	[Intrinsic]
	public static float Int32BitsToSingle(int value)
	{
		return Unsafe.BitCast<int, float>(value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static short HalfToInt16Bits(Half value)
	{
		return (short)value._value;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Half Int16BitsToHalf(short value)
	{
		return new Half((ushort)value);
	}

	[CLSCompliant(false)]
	[Intrinsic]
	public static ulong DoubleToUInt64Bits(double value)
	{
		return Unsafe.BitCast<double, ulong>(value);
	}

	[CLSCompliant(false)]
	[Intrinsic]
	public static double UInt64BitsToDouble(ulong value)
	{
		return Unsafe.BitCast<ulong, double>(value);
	}

	[CLSCompliant(false)]
	[Intrinsic]
	public static uint SingleToUInt32Bits(float value)
	{
		return Unsafe.BitCast<float, uint>(value);
	}

	[CLSCompliant(false)]
	[Intrinsic]
	public static float UInt32BitsToSingle(uint value)
	{
		return Unsafe.BitCast<uint, float>(value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[CLSCompliant(false)]
	public static ushort HalfToUInt16Bits(Half value)
	{
		return value._value;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[CLSCompliant(false)]
	public static Half UInt16BitsToHalf(ushort value)
	{
		return new Half(value);
	}
}
