using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System.Threading;

public static class Interlocked
{
	public static int Increment(ref int location)
	{
		return Add(ref location, 1);
	}

	public static long Increment(ref long location)
	{
		return Add(ref location, 1L);
	}

	public static int Decrement(ref int location)
	{
		return Add(ref location, -1);
	}

	public static long Decrement(ref long location)
	{
		return Add(ref location, -1L);
	}

	[MethodImpl(MethodImplOptions.InternalCall)]
	[Intrinsic]
	public static extern int Exchange(ref int location1, int value);

	[MethodImpl(MethodImplOptions.InternalCall)]
	[Intrinsic]
	public static extern long Exchange(ref long location1, long value);

	[MethodImpl(MethodImplOptions.InternalCall)]
	[Intrinsic]
	[return: NotNullIfNotNull("location1")]
	public static extern object? Exchange([NotNullIfNotNull("value")] ref object? location1, object? value);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[return: NotNullIfNotNull("location1")]
	public static T Exchange<T>([NotNullIfNotNull("value")] ref T location1, T value) where T : class?
	{
		return Unsafe.As<T>(Exchange(ref Unsafe.As<T, object>(ref location1), value));
	}

	[MethodImpl(MethodImplOptions.InternalCall)]
	[Intrinsic]
	public static extern int CompareExchange(ref int location1, int value, int comparand);

	[MethodImpl(MethodImplOptions.InternalCall)]
	[Intrinsic]
	public static extern long CompareExchange(ref long location1, long value, long comparand);

	[MethodImpl(MethodImplOptions.InternalCall)]
	[Intrinsic]
	[return: NotNullIfNotNull("location1")]
	public static extern object? CompareExchange(ref object? location1, object? value, object? comparand);

	[Intrinsic]
	[return: NotNullIfNotNull("location1")]
	public static T CompareExchange<T>(ref T location1, T value, T comparand) where T : class?
	{
		return Unsafe.As<T>(CompareExchange(ref Unsafe.As<T, object>(ref location1), value, comparand));
	}

	public static int Add(ref int location1, int value)
	{
		return ExchangeAdd(ref location1, value) + value;
	}

	public static long Add(ref long location1, long value)
	{
		return ExchangeAdd(ref location1, value) + value;
	}

	[MethodImpl(MethodImplOptions.InternalCall)]
	[Intrinsic]
	private static extern int ExchangeAdd(ref int location1, int value);

	[MethodImpl(MethodImplOptions.InternalCall)]
	[Intrinsic]
	private static extern long ExchangeAdd(ref long location1, long value);

	public static long Read([In][RequiresLocation] ref long location)
	{
		return CompareExchange(ref Unsafe.AsRef(ref location), 0L, 0L);
	}

	[DllImport("QCall", EntryPoint = "Interlocked_MemoryBarrierProcessWide", ExactSpelling = true)]
	[LibraryImport("QCall", EntryPoint = "Interlocked_MemoryBarrierProcessWide")]
	private static extern void _MemoryBarrierProcessWide();

	public static void MemoryBarrierProcessWide()
	{
		_MemoryBarrierProcessWide();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[CLSCompliant(false)]
	public static uint Increment(ref uint location)
	{
		return Add(ref location, 1u);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[CLSCompliant(false)]
	public static ulong Increment(ref ulong location)
	{
		return Add(ref location, 1uL);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[CLSCompliant(false)]
	public static uint Decrement(ref uint location)
	{
		return (uint)Add(ref Unsafe.As<uint, int>(ref location), -1);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[CLSCompliant(false)]
	public static ulong Decrement(ref ulong location)
	{
		return (ulong)Add(ref Unsafe.As<ulong, long>(ref location), -1L);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[CLSCompliant(false)]
	public static uint Exchange(ref uint location1, uint value)
	{
		return (uint)Exchange(ref Unsafe.As<uint, int>(ref location1), (int)value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[CLSCompliant(false)]
	public static ulong Exchange(ref ulong location1, ulong value)
	{
		return (ulong)Exchange(ref Unsafe.As<ulong, long>(ref location1), (long)value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static float Exchange(ref float location1, float value)
	{
		return Unsafe.BitCast<int, float>(Exchange(ref Unsafe.As<float, int>(ref location1), Unsafe.BitCast<float, int>(value)));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static double Exchange(ref double location1, double value)
	{
		return Unsafe.BitCast<long, double>(Exchange(ref Unsafe.As<double, long>(ref location1), Unsafe.BitCast<double, long>(value)));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static nint Exchange(ref nint location1, nint value)
	{
		return (nint)Exchange(ref Unsafe.As<nint, long>(ref location1), value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[CLSCompliant(false)]
	public static nuint Exchange(ref nuint location1, nuint value)
	{
		return (nuint)Exchange(ref Unsafe.As<nuint, long>(ref location1), (long)value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[CLSCompliant(false)]
	public static uint CompareExchange(ref uint location1, uint value, uint comparand)
	{
		return (uint)CompareExchange(ref Unsafe.As<uint, int>(ref location1), (int)value, (int)comparand);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[CLSCompliant(false)]
	public static ulong CompareExchange(ref ulong location1, ulong value, ulong comparand)
	{
		return (ulong)CompareExchange(ref Unsafe.As<ulong, long>(ref location1), (long)value, (long)comparand);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static float CompareExchange(ref float location1, float value, float comparand)
	{
		return Unsafe.BitCast<int, float>(CompareExchange(ref Unsafe.As<float, int>(ref location1), Unsafe.BitCast<float, int>(value), Unsafe.BitCast<float, int>(comparand)));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static double CompareExchange(ref double location1, double value, double comparand)
	{
		return Unsafe.BitCast<long, double>(CompareExchange(ref Unsafe.As<double, long>(ref location1), Unsafe.BitCast<double, long>(value), Unsafe.BitCast<double, long>(comparand)));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static nint CompareExchange(ref nint location1, nint value, nint comparand)
	{
		return (nint)CompareExchange(ref Unsafe.As<nint, long>(ref location1), value, comparand);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[CLSCompliant(false)]
	public static nuint CompareExchange(ref nuint location1, nuint value, nuint comparand)
	{
		return (nuint)CompareExchange(ref Unsafe.As<nuint, long>(ref location1), (long)value, (long)comparand);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[CLSCompliant(false)]
	public static uint Add(ref uint location1, uint value)
	{
		return (uint)Add(ref Unsafe.As<uint, int>(ref location1), (int)value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[CLSCompliant(false)]
	public static ulong Add(ref ulong location1, ulong value)
	{
		return (ulong)Add(ref Unsafe.As<ulong, long>(ref location1), (long)value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[CLSCompliant(false)]
	public static ulong Read([In][RequiresLocation] ref ulong location)
	{
		return CompareExchange(ref Unsafe.AsRef(ref location), 0uL, 0uL);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static int And(ref int location1, int value)
	{
		int num = location1;
		int num2;
		while (true)
		{
			int value2 = num & value;
			num2 = CompareExchange(ref location1, value2, num);
			if (num2 == num)
			{
				break;
			}
			num = num2;
		}
		return num2;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[CLSCompliant(false)]
	public static uint And(ref uint location1, uint value)
	{
		return (uint)And(ref Unsafe.As<uint, int>(ref location1), (int)value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static long And(ref long location1, long value)
	{
		long num = location1;
		long num2;
		while (true)
		{
			long value2 = num & value;
			num2 = CompareExchange(ref location1, value2, num);
			if (num2 == num)
			{
				break;
			}
			num = num2;
		}
		return num2;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[CLSCompliant(false)]
	public static ulong And(ref ulong location1, ulong value)
	{
		return (ulong)And(ref Unsafe.As<ulong, long>(ref location1), (long)value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static int Or(ref int location1, int value)
	{
		int num = location1;
		int num2;
		while (true)
		{
			int value2 = num | value;
			num2 = CompareExchange(ref location1, value2, num);
			if (num2 == num)
			{
				break;
			}
			num = num2;
		}
		return num2;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[CLSCompliant(false)]
	public static uint Or(ref uint location1, uint value)
	{
		return (uint)Or(ref Unsafe.As<uint, int>(ref location1), (int)value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static long Or(ref long location1, long value)
	{
		long num = location1;
		long num2;
		while (true)
		{
			long value2 = num | value;
			num2 = CompareExchange(ref location1, value2, num);
			if (num2 == num)
			{
				break;
			}
			num = num2;
		}
		return num2;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[CLSCompliant(false)]
	public static ulong Or(ref ulong location1, ulong value)
	{
		return (ulong)Or(ref Unsafe.As<ulong, long>(ref location1), (long)value);
	}

	[Intrinsic]
	public static void MemoryBarrier()
	{
		MemoryBarrier();
	}

	[Intrinsic]
	internal static void ReadMemoryBarrier()
	{
		ReadMemoryBarrier();
	}
}
