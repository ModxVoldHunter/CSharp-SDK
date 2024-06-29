using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;

namespace System.Buffers.Binary;

public static class BinaryPrimitives
{
	[StructLayout(LayoutKind.Sequential, Size = 1)]
	private readonly struct Int16EndiannessReverser : IEndiannessReverser<short>
	{
		public static short Reverse(short value)
		{
			return ReverseEndianness(value);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Vector128<short> Reverse(Vector128<short> vector)
		{
			return Vector128.ShiftLeft(vector, 8) | Vector128.ShiftRightLogical(vector, 8);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Vector256<short> Reverse(Vector256<short> vector)
		{
			return Vector256.ShiftLeft(vector, 8) | Vector256.ShiftRightLogical(vector, 8);
		}
	}

	[StructLayout(LayoutKind.Sequential, Size = 1)]
	private readonly struct Int32EndiannessReverser : IEndiannessReverser<int>
	{
		public static int Reverse(int value)
		{
			return ReverseEndianness(value);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Vector128<int> Reverse(Vector128<int> vector)
		{
			return Vector128.Shuffle(vector.AsByte(), Vector128.Create((byte)3, (byte)2, (byte)1, (byte)0, (byte)7, (byte)6, (byte)5, (byte)4, (byte)11, (byte)10, (byte)9, (byte)8, (byte)15, (byte)14, (byte)13, (byte)12)).AsInt32();
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Vector256<int> Reverse(Vector256<int> vector)
		{
			return Vector256.Shuffle(vector.AsByte(), Vector256.Create((byte)3, (byte)2, (byte)1, (byte)0, (byte)7, (byte)6, (byte)5, (byte)4, (byte)11, (byte)10, (byte)9, (byte)8, (byte)15, (byte)14, (byte)13, (byte)12, (byte)19, (byte)18, (byte)17, (byte)16, (byte)23, (byte)22, (byte)21, (byte)20, (byte)27, (byte)26, (byte)25, (byte)24, (byte)31, (byte)30, (byte)29, (byte)28)).AsInt32();
		}
	}

	[StructLayout(LayoutKind.Sequential, Size = 1)]
	private readonly struct Int64EndiannessReverser : IEndiannessReverser<long>
	{
		public static long Reverse(long value)
		{
			return ReverseEndianness(value);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Vector128<long> Reverse(Vector128<long> vector)
		{
			return Vector128.Shuffle(vector.AsByte(), Vector128.Create((byte)7, (byte)6, (byte)5, (byte)4, (byte)3, (byte)2, (byte)1, (byte)0, (byte)15, (byte)14, (byte)13, (byte)12, (byte)11, (byte)10, (byte)9, (byte)8)).AsInt64();
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Vector256<long> Reverse(Vector256<long> vector)
		{
			return Vector256.Shuffle(vector.AsByte(), Vector256.Create((byte)7, (byte)6, (byte)5, (byte)4, (byte)3, (byte)2, (byte)1, (byte)0, (byte)15, (byte)14, (byte)13, (byte)12, (byte)11, (byte)10, (byte)9, (byte)8, (byte)23, (byte)22, (byte)21, (byte)20, (byte)19, (byte)18, (byte)17, (byte)16, (byte)31, (byte)30, (byte)29, (byte)28, (byte)27, (byte)26, (byte)25, (byte)24)).AsInt64();
		}
	}

	private interface IEndiannessReverser<T> where T : struct
	{
		static abstract T Reverse(T value);

		static abstract Vector128<T> Reverse(Vector128<T> vector);

		static abstract Vector256<T> Reverse(Vector256<T> vector);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static double ReadDoubleBigEndian(ReadOnlySpan<byte> source)
	{
		if (!BitConverter.IsLittleEndian)
		{
		}
		return BitConverter.Int64BitsToDouble(ReverseEndianness(MemoryMarshal.Read<long>(source)));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Half ReadHalfBigEndian(ReadOnlySpan<byte> source)
	{
		if (!BitConverter.IsLittleEndian)
		{
		}
		return BitConverter.Int16BitsToHalf(ReverseEndianness(MemoryMarshal.Read<short>(source)));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static short ReadInt16BigEndian(ReadOnlySpan<byte> source)
	{
		if (!BitConverter.IsLittleEndian)
		{
		}
		return ReverseEndianness(MemoryMarshal.Read<short>(source));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static int ReadInt32BigEndian(ReadOnlySpan<byte> source)
	{
		if (!BitConverter.IsLittleEndian)
		{
		}
		return ReverseEndianness(MemoryMarshal.Read<int>(source));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static long ReadInt64BigEndian(ReadOnlySpan<byte> source)
	{
		if (!BitConverter.IsLittleEndian)
		{
		}
		return ReverseEndianness(MemoryMarshal.Read<long>(source));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Int128 ReadInt128BigEndian(ReadOnlySpan<byte> source)
	{
		if (!BitConverter.IsLittleEndian)
		{
		}
		return ReverseEndianness(MemoryMarshal.Read<Int128>(source));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static nint ReadIntPtrBigEndian(ReadOnlySpan<byte> source)
	{
		if (!BitConverter.IsLittleEndian)
		{
		}
		return ReverseEndianness(MemoryMarshal.Read<nint>(source));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static float ReadSingleBigEndian(ReadOnlySpan<byte> source)
	{
		if (!BitConverter.IsLittleEndian)
		{
		}
		return BitConverter.Int32BitsToSingle(ReverseEndianness(MemoryMarshal.Read<int>(source)));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[CLSCompliant(false)]
	public static ushort ReadUInt16BigEndian(ReadOnlySpan<byte> source)
	{
		if (!BitConverter.IsLittleEndian)
		{
		}
		return ReverseEndianness(MemoryMarshal.Read<ushort>(source));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[CLSCompliant(false)]
	public static uint ReadUInt32BigEndian(ReadOnlySpan<byte> source)
	{
		if (!BitConverter.IsLittleEndian)
		{
		}
		return ReverseEndianness(MemoryMarshal.Read<uint>(source));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[CLSCompliant(false)]
	public static ulong ReadUInt64BigEndian(ReadOnlySpan<byte> source)
	{
		if (!BitConverter.IsLittleEndian)
		{
		}
		return ReverseEndianness(MemoryMarshal.Read<ulong>(source));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[CLSCompliant(false)]
	public static UInt128 ReadUInt128BigEndian(ReadOnlySpan<byte> source)
	{
		if (!BitConverter.IsLittleEndian)
		{
		}
		return ReverseEndianness(MemoryMarshal.Read<UInt128>(source));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[CLSCompliant(false)]
	public static nuint ReadUIntPtrBigEndian(ReadOnlySpan<byte> source)
	{
		if (!BitConverter.IsLittleEndian)
		{
		}
		return ReverseEndianness(MemoryMarshal.Read<nuint>(source));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool TryReadDoubleBigEndian(ReadOnlySpan<byte> source, out double value)
	{
		_ = BitConverter.IsLittleEndian;
		long value2;
		bool result = MemoryMarshal.TryRead<long>(source, out value2);
		value = BitConverter.Int64BitsToDouble(ReverseEndianness(value2));
		return result;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool TryReadHalfBigEndian(ReadOnlySpan<byte> source, out Half value)
	{
		_ = BitConverter.IsLittleEndian;
		short value2;
		bool result = MemoryMarshal.TryRead<short>(source, out value2);
		value = BitConverter.Int16BitsToHalf(ReverseEndianness(value2));
		return result;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool TryReadInt16BigEndian(ReadOnlySpan<byte> source, out short value)
	{
		_ = BitConverter.IsLittleEndian;
		short value2;
		bool result = MemoryMarshal.TryRead<short>(source, out value2);
		value = ReverseEndianness(value2);
		return result;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool TryReadInt32BigEndian(ReadOnlySpan<byte> source, out int value)
	{
		_ = BitConverter.IsLittleEndian;
		int value2;
		bool result = MemoryMarshal.TryRead<int>(source, out value2);
		value = ReverseEndianness(value2);
		return result;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool TryReadInt64BigEndian(ReadOnlySpan<byte> source, out long value)
	{
		_ = BitConverter.IsLittleEndian;
		long value2;
		bool result = MemoryMarshal.TryRead<long>(source, out value2);
		value = ReverseEndianness(value2);
		return result;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool TryReadInt128BigEndian(ReadOnlySpan<byte> source, out Int128 value)
	{
		_ = BitConverter.IsLittleEndian;
		Int128 value2;
		bool result = MemoryMarshal.TryRead<Int128>(source, out value2);
		value = ReverseEndianness(value2);
		return result;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool TryReadIntPtrBigEndian(ReadOnlySpan<byte> source, out nint value)
	{
		_ = BitConverter.IsLittleEndian;
		nint value2;
		bool result = MemoryMarshal.TryRead<nint>(source, out value2);
		value = ReverseEndianness(value2);
		return result;
	}

	public static bool TryReadSingleBigEndian(ReadOnlySpan<byte> source, out float value)
	{
		_ = BitConverter.IsLittleEndian;
		int value2;
		bool result = MemoryMarshal.TryRead<int>(source, out value2);
		value = BitConverter.Int32BitsToSingle(ReverseEndianness(value2));
		return result;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[CLSCompliant(false)]
	public static bool TryReadUInt16BigEndian(ReadOnlySpan<byte> source, out ushort value)
	{
		_ = BitConverter.IsLittleEndian;
		ushort value2;
		bool result = MemoryMarshal.TryRead<ushort>(source, out value2);
		value = ReverseEndianness(value2);
		return result;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[CLSCompliant(false)]
	public static bool TryReadUInt32BigEndian(ReadOnlySpan<byte> source, out uint value)
	{
		_ = BitConverter.IsLittleEndian;
		uint value2;
		bool result = MemoryMarshal.TryRead<uint>(source, out value2);
		value = ReverseEndianness(value2);
		return result;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[CLSCompliant(false)]
	public static bool TryReadUInt64BigEndian(ReadOnlySpan<byte> source, out ulong value)
	{
		_ = BitConverter.IsLittleEndian;
		ulong value2;
		bool result = MemoryMarshal.TryRead<ulong>(source, out value2);
		value = ReverseEndianness(value2);
		return result;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[CLSCompliant(false)]
	public static bool TryReadUInt128BigEndian(ReadOnlySpan<byte> source, out UInt128 value)
	{
		_ = BitConverter.IsLittleEndian;
		UInt128 value2;
		bool result = MemoryMarshal.TryRead<UInt128>(source, out value2);
		value = ReverseEndianness(value2);
		return result;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[CLSCompliant(false)]
	public static bool TryReadUIntPtrBigEndian(ReadOnlySpan<byte> source, out nuint value)
	{
		_ = BitConverter.IsLittleEndian;
		nuint value2;
		bool result = MemoryMarshal.TryRead<nuint>(source, out value2);
		value = ReverseEndianness(value2);
		return result;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static double ReadDoubleLittleEndian(ReadOnlySpan<byte> source)
	{
		_ = BitConverter.IsLittleEndian;
		return MemoryMarshal.Read<double>(source);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Half ReadHalfLittleEndian(ReadOnlySpan<byte> source)
	{
		_ = BitConverter.IsLittleEndian;
		return MemoryMarshal.Read<Half>(source);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static short ReadInt16LittleEndian(ReadOnlySpan<byte> source)
	{
		_ = BitConverter.IsLittleEndian;
		return MemoryMarshal.Read<short>(source);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static int ReadInt32LittleEndian(ReadOnlySpan<byte> source)
	{
		_ = BitConverter.IsLittleEndian;
		return MemoryMarshal.Read<int>(source);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static long ReadInt64LittleEndian(ReadOnlySpan<byte> source)
	{
		_ = BitConverter.IsLittleEndian;
		return MemoryMarshal.Read<long>(source);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Int128 ReadInt128LittleEndian(ReadOnlySpan<byte> source)
	{
		_ = BitConverter.IsLittleEndian;
		return MemoryMarshal.Read<Int128>(source);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static nint ReadIntPtrLittleEndian(ReadOnlySpan<byte> source)
	{
		_ = BitConverter.IsLittleEndian;
		return MemoryMarshal.Read<nint>(source);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static float ReadSingleLittleEndian(ReadOnlySpan<byte> source)
	{
		_ = BitConverter.IsLittleEndian;
		return MemoryMarshal.Read<float>(source);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[CLSCompliant(false)]
	public static ushort ReadUInt16LittleEndian(ReadOnlySpan<byte> source)
	{
		_ = BitConverter.IsLittleEndian;
		return MemoryMarshal.Read<ushort>(source);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[CLSCompliant(false)]
	public static uint ReadUInt32LittleEndian(ReadOnlySpan<byte> source)
	{
		_ = BitConverter.IsLittleEndian;
		return MemoryMarshal.Read<uint>(source);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[CLSCompliant(false)]
	public static ulong ReadUInt64LittleEndian(ReadOnlySpan<byte> source)
	{
		_ = BitConverter.IsLittleEndian;
		return MemoryMarshal.Read<ulong>(source);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[CLSCompliant(false)]
	public static UInt128 ReadUInt128LittleEndian(ReadOnlySpan<byte> source)
	{
		_ = BitConverter.IsLittleEndian;
		return MemoryMarshal.Read<UInt128>(source);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[CLSCompliant(false)]
	public static nuint ReadUIntPtrLittleEndian(ReadOnlySpan<byte> source)
	{
		_ = BitConverter.IsLittleEndian;
		return MemoryMarshal.Read<nuint>(source);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool TryReadDoubleLittleEndian(ReadOnlySpan<byte> source, out double value)
	{
		if (!BitConverter.IsLittleEndian)
		{
		}
		return MemoryMarshal.TryRead<double>(source, out value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool TryReadHalfLittleEndian(ReadOnlySpan<byte> source, out Half value)
	{
		if (!BitConverter.IsLittleEndian)
		{
		}
		return MemoryMarshal.TryRead<Half>(source, out value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool TryReadInt16LittleEndian(ReadOnlySpan<byte> source, out short value)
	{
		_ = BitConverter.IsLittleEndian;
		return MemoryMarshal.TryRead<short>(source, out value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool TryReadInt32LittleEndian(ReadOnlySpan<byte> source, out int value)
	{
		_ = BitConverter.IsLittleEndian;
		return MemoryMarshal.TryRead<int>(source, out value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool TryReadInt64LittleEndian(ReadOnlySpan<byte> source, out long value)
	{
		_ = BitConverter.IsLittleEndian;
		return MemoryMarshal.TryRead<long>(source, out value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool TryReadInt128LittleEndian(ReadOnlySpan<byte> source, out Int128 value)
	{
		_ = BitConverter.IsLittleEndian;
		return MemoryMarshal.TryRead<Int128>(source, out value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool TryReadIntPtrLittleEndian(ReadOnlySpan<byte> source, out nint value)
	{
		_ = BitConverter.IsLittleEndian;
		return MemoryMarshal.TryRead<nint>(source, out value);
	}

	public static bool TryReadSingleLittleEndian(ReadOnlySpan<byte> source, out float value)
	{
		if (!BitConverter.IsLittleEndian)
		{
		}
		return MemoryMarshal.TryRead<float>(source, out value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[CLSCompliant(false)]
	public static bool TryReadUInt16LittleEndian(ReadOnlySpan<byte> source, out ushort value)
	{
		_ = BitConverter.IsLittleEndian;
		return MemoryMarshal.TryRead<ushort>(source, out value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[CLSCompliant(false)]
	public static bool TryReadUInt32LittleEndian(ReadOnlySpan<byte> source, out uint value)
	{
		_ = BitConverter.IsLittleEndian;
		return MemoryMarshal.TryRead<uint>(source, out value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[CLSCompliant(false)]
	public static bool TryReadUInt64LittleEndian(ReadOnlySpan<byte> source, out ulong value)
	{
		_ = BitConverter.IsLittleEndian;
		return MemoryMarshal.TryRead<ulong>(source, out value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[CLSCompliant(false)]
	public static bool TryReadUInt128LittleEndian(ReadOnlySpan<byte> source, out UInt128 value)
	{
		_ = BitConverter.IsLittleEndian;
		return MemoryMarshal.TryRead<UInt128>(source, out value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[CLSCompliant(false)]
	public static bool TryReadUIntPtrLittleEndian(ReadOnlySpan<byte> source, out nuint value)
	{
		_ = BitConverter.IsLittleEndian;
		return MemoryMarshal.TryRead<nuint>(source, out value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[CLSCompliant(false)]
	public static sbyte ReverseEndianness(sbyte value)
	{
		return value;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static short ReverseEndianness(short value)
	{
		return (short)ReverseEndianness((ushort)value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static int ReverseEndianness(int value)
	{
		return (int)ReverseEndianness((uint)value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static long ReverseEndianness(long value)
	{
		return (long)ReverseEndianness((ulong)value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static nint ReverseEndianness(nint value)
	{
		return (nint)ReverseEndianness((long)value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Int128 ReverseEndianness(Int128 value)
	{
		return new Int128(ReverseEndianness(value.Lower), ReverseEndianness(value.Upper));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static byte ReverseEndianness(byte value)
	{
		return value;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[CLSCompliant(false)]
	[Intrinsic]
	public static ushort ReverseEndianness(ushort value)
	{
		return (ushort)((value >> 8) + (value << 8));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static char ReverseEndianness(char value)
	{
		return (char)ReverseEndianness((ushort)value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[CLSCompliant(false)]
	[Intrinsic]
	public static uint ReverseEndianness(uint value)
	{
		return BitOperations.RotateRight(value & 0xFF00FFu, 8) + BitOperations.RotateLeft(value & 0xFF00FF00u, 8);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[CLSCompliant(false)]
	[Intrinsic]
	public static ulong ReverseEndianness(ulong value)
	{
		return ((ulong)ReverseEndianness((uint)value) << 32) + ReverseEndianness((uint)(value >> 32));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[CLSCompliant(false)]
	public static nuint ReverseEndianness(nuint value)
	{
		return (nuint)ReverseEndianness((ulong)value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[CLSCompliant(false)]
	public static UInt128 ReverseEndianness(UInt128 value)
	{
		return new UInt128(ReverseEndianness(value.Lower), ReverseEndianness(value.Upper));
	}

	[CLSCompliant(false)]
	public static void ReverseEndianness(ReadOnlySpan<ushort> source, Span<ushort> destination)
	{
		ReverseEndianness<short, Int16EndiannessReverser>(MemoryMarshal.Cast<ushort, short>(source), MemoryMarshal.Cast<ushort, short>(destination));
	}

	public static void ReverseEndianness(ReadOnlySpan<short> source, Span<short> destination)
	{
		ReverseEndianness<short, Int16EndiannessReverser>(source, destination);
	}

	[CLSCompliant(false)]
	public static void ReverseEndianness(ReadOnlySpan<uint> source, Span<uint> destination)
	{
		ReverseEndianness<int, Int32EndiannessReverser>(MemoryMarshal.Cast<uint, int>(source), MemoryMarshal.Cast<uint, int>(destination));
	}

	public static void ReverseEndianness(ReadOnlySpan<int> source, Span<int> destination)
	{
		ReverseEndianness<int, Int32EndiannessReverser>(source, destination);
	}

	[CLSCompliant(false)]
	public static void ReverseEndianness(ReadOnlySpan<ulong> source, Span<ulong> destination)
	{
		ReverseEndianness<long, Int64EndiannessReverser>(MemoryMarshal.Cast<ulong, long>(source), MemoryMarshal.Cast<ulong, long>(destination));
	}

	public static void ReverseEndianness(ReadOnlySpan<long> source, Span<long> destination)
	{
		ReverseEndianness<long, Int64EndiannessReverser>(source, destination);
	}

	[CLSCompliant(false)]
	public static void ReverseEndianness(ReadOnlySpan<nuint> source, Span<nuint> destination)
	{
		ReverseEndianness<long, Int64EndiannessReverser>(MemoryMarshal.Cast<nuint, long>(source), MemoryMarshal.Cast<nuint, long>(destination));
	}

	public static void ReverseEndianness(ReadOnlySpan<nint> source, Span<nint> destination)
	{
		ReverseEndianness<long, Int64EndiannessReverser>(MemoryMarshal.Cast<nint, long>(source), MemoryMarshal.Cast<nint, long>(destination));
	}

	private static void ReverseEndianness<T, TReverser>(ReadOnlySpan<T> source, Span<T> destination) where T : struct where TReverser : IEndiannessReverser<T>
	{
		if (destination.Length < source.Length)
		{
			ThrowDestinationTooSmall();
		}
		ref T reference = ref MemoryMarshal.GetReference(source);
		ref T reference2 = ref MemoryMarshal.GetReference(destination);
		if (Unsafe.AreSame(ref reference, ref reference2) || !source.Overlaps(destination, out var elementOffset) || elementOffset < 0)
		{
			int i = 0;
			if (Vector256.IsHardwareAccelerated)
			{
				for (; i <= source.Length - Vector256<T>.Count; i += Vector256<T>.Count)
				{
					TReverser.Reverse(Vector256.LoadUnsafe(ref reference, (uint)i)).StoreUnsafe(ref reference2, (uint)i);
				}
			}
			if (Vector128.IsHardwareAccelerated)
			{
				for (; i <= source.Length - Vector128<T>.Count; i += Vector128<T>.Count)
				{
					TReverser.Reverse(Vector128.LoadUnsafe(ref reference, (uint)i)).StoreUnsafe(ref reference2, (uint)i);
				}
			}
			for (; i < source.Length; i++)
			{
				Unsafe.Add(ref reference2, i) = TReverser.Reverse(Unsafe.Add(ref reference, i));
			}
			return;
		}
		int num = source.Length;
		if (Vector256.IsHardwareAccelerated)
		{
			while (num >= Vector256<T>.Count)
			{
				num -= Vector256<T>.Count;
				TReverser.Reverse(Vector256.LoadUnsafe(ref reference, (uint)num)).StoreUnsafe(ref reference2, (uint)num);
			}
		}
		if (Vector128.IsHardwareAccelerated)
		{
			while (num >= Vector128<T>.Count)
			{
				num -= Vector128<T>.Count;
				TReverser.Reverse(Vector128.LoadUnsafe(ref reference, (uint)num)).StoreUnsafe(ref reference2, (uint)num);
			}
		}
		while (num > 0)
		{
			num--;
			Unsafe.Add(ref reference2, num) = TReverser.Reverse(Unsafe.Add(ref reference, num));
		}
	}

	[CLSCompliant(false)]
	public static void ReverseEndianness(ReadOnlySpan<UInt128> source, Span<UInt128> destination)
	{
		ReverseEndianness(MemoryMarshal.Cast<UInt128, Int128>(source), MemoryMarshal.Cast<UInt128, Int128>(destination));
	}

	public static void ReverseEndianness(ReadOnlySpan<Int128> source, Span<Int128> destination)
	{
		if (destination.Length < source.Length)
		{
			ThrowDestinationTooSmall();
		}
		if (Unsafe.AreSame(ref MemoryMarshal.GetReference(source), ref MemoryMarshal.GetReference(destination)) || !source.Overlaps(destination, out var elementOffset) || elementOffset < 0)
		{
			for (int i = 0; i < source.Length; i++)
			{
				destination[i] = ReverseEndianness(source[i]);
			}
			return;
		}
		for (int num = source.Length - 1; num >= 0; num--)
		{
			destination[num] = ReverseEndianness(source[num]);
		}
	}

	[DoesNotReturn]
	private static void ThrowDestinationTooSmall()
	{
		throw new ArgumentException(SR.Arg_BufferTooSmall, "destination");
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void WriteDoubleBigEndian(Span<byte> destination, double value)
	{
		_ = BitConverter.IsLittleEndian;
		long value2 = ReverseEndianness(BitConverter.DoubleToInt64Bits(value));
		MemoryMarshal.Write(destination, in value2);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void WriteHalfBigEndian(Span<byte> destination, Half value)
	{
		_ = BitConverter.IsLittleEndian;
		short value2 = ReverseEndianness(BitConverter.HalfToInt16Bits(value));
		MemoryMarshal.Write(destination, in value2);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void WriteInt16BigEndian(Span<byte> destination, short value)
	{
		_ = BitConverter.IsLittleEndian;
		short value2 = ReverseEndianness(value);
		MemoryMarshal.Write(destination, in value2);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void WriteInt32BigEndian(Span<byte> destination, int value)
	{
		_ = BitConverter.IsLittleEndian;
		int value2 = ReverseEndianness(value);
		MemoryMarshal.Write(destination, in value2);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void WriteInt64BigEndian(Span<byte> destination, long value)
	{
		_ = BitConverter.IsLittleEndian;
		long value2 = ReverseEndianness(value);
		MemoryMarshal.Write(destination, in value2);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void WriteInt128BigEndian(Span<byte> destination, Int128 value)
	{
		_ = BitConverter.IsLittleEndian;
		Int128 value2 = ReverseEndianness(value);
		MemoryMarshal.Write(destination, in value2);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void WriteIntPtrBigEndian(Span<byte> destination, nint value)
	{
		_ = BitConverter.IsLittleEndian;
		nint value2 = ReverseEndianness(value);
		MemoryMarshal.Write<nint>(destination, in value2);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void WriteSingleBigEndian(Span<byte> destination, float value)
	{
		_ = BitConverter.IsLittleEndian;
		int value2 = ReverseEndianness(BitConverter.SingleToInt32Bits(value));
		MemoryMarshal.Write(destination, in value2);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[CLSCompliant(false)]
	public static void WriteUInt16BigEndian(Span<byte> destination, ushort value)
	{
		_ = BitConverter.IsLittleEndian;
		ushort value2 = ReverseEndianness(value);
		MemoryMarshal.Write(destination, in value2);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[CLSCompliant(false)]
	public static void WriteUInt32BigEndian(Span<byte> destination, uint value)
	{
		_ = BitConverter.IsLittleEndian;
		uint value2 = ReverseEndianness(value);
		MemoryMarshal.Write(destination, in value2);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[CLSCompliant(false)]
	public static void WriteUInt64BigEndian(Span<byte> destination, ulong value)
	{
		_ = BitConverter.IsLittleEndian;
		ulong value2 = ReverseEndianness(value);
		MemoryMarshal.Write(destination, in value2);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[CLSCompliant(false)]
	public static void WriteUInt128BigEndian(Span<byte> destination, UInt128 value)
	{
		_ = BitConverter.IsLittleEndian;
		UInt128 value2 = ReverseEndianness(value);
		MemoryMarshal.Write(destination, in value2);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[CLSCompliant(false)]
	public static void WriteUIntPtrBigEndian(Span<byte> destination, nuint value)
	{
		_ = BitConverter.IsLittleEndian;
		nuint value2 = ReverseEndianness(value);
		MemoryMarshal.Write<nuint>(destination, in value2);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool TryWriteDoubleBigEndian(Span<byte> destination, double value)
	{
		_ = BitConverter.IsLittleEndian;
		long value2 = ReverseEndianness(BitConverter.DoubleToInt64Bits(value));
		return MemoryMarshal.TryWrite(destination, in value2);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool TryWriteHalfBigEndian(Span<byte> destination, Half value)
	{
		_ = BitConverter.IsLittleEndian;
		short value2 = ReverseEndianness(BitConverter.HalfToInt16Bits(value));
		return MemoryMarshal.TryWrite(destination, in value2);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool TryWriteInt16BigEndian(Span<byte> destination, short value)
	{
		_ = BitConverter.IsLittleEndian;
		short value2 = ReverseEndianness(value);
		return MemoryMarshal.TryWrite(destination, in value2);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool TryWriteInt32BigEndian(Span<byte> destination, int value)
	{
		_ = BitConverter.IsLittleEndian;
		int value2 = ReverseEndianness(value);
		return MemoryMarshal.TryWrite(destination, in value2);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool TryWriteInt64BigEndian(Span<byte> destination, long value)
	{
		_ = BitConverter.IsLittleEndian;
		long value2 = ReverseEndianness(value);
		return MemoryMarshal.TryWrite(destination, in value2);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool TryWriteInt128BigEndian(Span<byte> destination, Int128 value)
	{
		_ = BitConverter.IsLittleEndian;
		Int128 value2 = ReverseEndianness(value);
		return MemoryMarshal.TryWrite(destination, in value2);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool TryWriteIntPtrBigEndian(Span<byte> destination, nint value)
	{
		_ = BitConverter.IsLittleEndian;
		nint value2 = ReverseEndianness(value);
		return MemoryMarshal.TryWrite<nint>(destination, in value2);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool TryWriteSingleBigEndian(Span<byte> destination, float value)
	{
		_ = BitConverter.IsLittleEndian;
		int value2 = ReverseEndianness(BitConverter.SingleToInt32Bits(value));
		return MemoryMarshal.TryWrite(destination, in value2);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[CLSCompliant(false)]
	public static bool TryWriteUInt16BigEndian(Span<byte> destination, ushort value)
	{
		_ = BitConverter.IsLittleEndian;
		ushort value2 = ReverseEndianness(value);
		return MemoryMarshal.TryWrite(destination, in value2);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[CLSCompliant(false)]
	public static bool TryWriteUInt32BigEndian(Span<byte> destination, uint value)
	{
		_ = BitConverter.IsLittleEndian;
		uint value2 = ReverseEndianness(value);
		return MemoryMarshal.TryWrite(destination, in value2);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[CLSCompliant(false)]
	public static bool TryWriteUInt64BigEndian(Span<byte> destination, ulong value)
	{
		_ = BitConverter.IsLittleEndian;
		ulong value2 = ReverseEndianness(value);
		return MemoryMarshal.TryWrite(destination, in value2);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[CLSCompliant(false)]
	public static bool TryWriteUInt128BigEndian(Span<byte> destination, UInt128 value)
	{
		_ = BitConverter.IsLittleEndian;
		UInt128 value2 = ReverseEndianness(value);
		return MemoryMarshal.TryWrite(destination, in value2);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[CLSCompliant(false)]
	public static bool TryWriteUIntPtrBigEndian(Span<byte> destination, nuint value)
	{
		_ = BitConverter.IsLittleEndian;
		nuint value2 = ReverseEndianness(value);
		return MemoryMarshal.TryWrite<nuint>(destination, in value2);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void WriteDoubleLittleEndian(Span<byte> destination, double value)
	{
		if (!BitConverter.IsLittleEndian)
		{
		}
		MemoryMarshal.Write(destination, in value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void WriteHalfLittleEndian(Span<byte> destination, Half value)
	{
		if (!BitConverter.IsLittleEndian)
		{
		}
		MemoryMarshal.Write(destination, in value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void WriteInt16LittleEndian(Span<byte> destination, short value)
	{
		if (!BitConverter.IsLittleEndian)
		{
		}
		MemoryMarshal.Write(destination, in value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void WriteInt32LittleEndian(Span<byte> destination, int value)
	{
		if (!BitConverter.IsLittleEndian)
		{
		}
		MemoryMarshal.Write(destination, in value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void WriteInt64LittleEndian(Span<byte> destination, long value)
	{
		if (!BitConverter.IsLittleEndian)
		{
		}
		MemoryMarshal.Write(destination, in value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void WriteInt128LittleEndian(Span<byte> destination, Int128 value)
	{
		if (!BitConverter.IsLittleEndian)
		{
		}
		MemoryMarshal.Write(destination, in value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void WriteIntPtrLittleEndian(Span<byte> destination, nint value)
	{
		if (!BitConverter.IsLittleEndian)
		{
		}
		MemoryMarshal.Write<nint>(destination, in value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void WriteSingleLittleEndian(Span<byte> destination, float value)
	{
		if (!BitConverter.IsLittleEndian)
		{
		}
		MemoryMarshal.Write(destination, in value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[CLSCompliant(false)]
	public static void WriteUInt16LittleEndian(Span<byte> destination, ushort value)
	{
		if (!BitConverter.IsLittleEndian)
		{
		}
		MemoryMarshal.Write(destination, in value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[CLSCompliant(false)]
	public static void WriteUInt32LittleEndian(Span<byte> destination, uint value)
	{
		if (!BitConverter.IsLittleEndian)
		{
		}
		MemoryMarshal.Write(destination, in value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[CLSCompliant(false)]
	public static void WriteUInt64LittleEndian(Span<byte> destination, ulong value)
	{
		if (!BitConverter.IsLittleEndian)
		{
		}
		MemoryMarshal.Write(destination, in value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[CLSCompliant(false)]
	public static void WriteUInt128LittleEndian(Span<byte> destination, UInt128 value)
	{
		if (!BitConverter.IsLittleEndian)
		{
		}
		MemoryMarshal.Write(destination, in value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[CLSCompliant(false)]
	public static void WriteUIntPtrLittleEndian(Span<byte> destination, nuint value)
	{
		if (!BitConverter.IsLittleEndian)
		{
		}
		MemoryMarshal.Write<nuint>(destination, in value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool TryWriteDoubleLittleEndian(Span<byte> destination, double value)
	{
		if (!BitConverter.IsLittleEndian)
		{
		}
		return MemoryMarshal.TryWrite(destination, in value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool TryWriteHalfLittleEndian(Span<byte> destination, Half value)
	{
		if (!BitConverter.IsLittleEndian)
		{
		}
		return MemoryMarshal.TryWrite(destination, in value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool TryWriteInt16LittleEndian(Span<byte> destination, short value)
	{
		if (!BitConverter.IsLittleEndian)
		{
		}
		return MemoryMarshal.TryWrite(destination, in value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool TryWriteInt32LittleEndian(Span<byte> destination, int value)
	{
		if (!BitConverter.IsLittleEndian)
		{
		}
		return MemoryMarshal.TryWrite(destination, in value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool TryWriteInt64LittleEndian(Span<byte> destination, long value)
	{
		if (!BitConverter.IsLittleEndian)
		{
		}
		return MemoryMarshal.TryWrite(destination, in value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool TryWriteInt128LittleEndian(Span<byte> destination, Int128 value)
	{
		if (!BitConverter.IsLittleEndian)
		{
		}
		return MemoryMarshal.TryWrite(destination, in value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool TryWriteIntPtrLittleEndian(Span<byte> destination, nint value)
	{
		if (!BitConverter.IsLittleEndian)
		{
		}
		return MemoryMarshal.TryWrite<nint>(destination, in value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool TryWriteSingleLittleEndian(Span<byte> destination, float value)
	{
		if (!BitConverter.IsLittleEndian)
		{
		}
		return MemoryMarshal.TryWrite(destination, in value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[CLSCompliant(false)]
	public static bool TryWriteUInt16LittleEndian(Span<byte> destination, ushort value)
	{
		if (!BitConverter.IsLittleEndian)
		{
		}
		return MemoryMarshal.TryWrite(destination, in value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[CLSCompliant(false)]
	public static bool TryWriteUInt32LittleEndian(Span<byte> destination, uint value)
	{
		if (!BitConverter.IsLittleEndian)
		{
		}
		return MemoryMarshal.TryWrite(destination, in value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[CLSCompliant(false)]
	public static bool TryWriteUInt64LittleEndian(Span<byte> destination, ulong value)
	{
		if (!BitConverter.IsLittleEndian)
		{
		}
		return MemoryMarshal.TryWrite(destination, in value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[CLSCompliant(false)]
	public static bool TryWriteUInt128LittleEndian(Span<byte> destination, UInt128 value)
	{
		if (!BitConverter.IsLittleEndian)
		{
		}
		return MemoryMarshal.TryWrite(destination, in value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[CLSCompliant(false)]
	public static bool TryWriteUIntPtrLittleEndian(Span<byte> destination, nuint value)
	{
		if (!BitConverter.IsLittleEndian)
		{
		}
		return MemoryMarshal.TryWrite<nuint>(destination, in value);
	}
}
