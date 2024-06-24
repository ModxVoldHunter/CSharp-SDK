using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace System.Threading;

public static class Volatile
{
	private struct VolatileBoolean
	{
		public volatile bool Value;
	}

	private struct VolatileByte
	{
		public volatile byte Value;
	}

	private struct VolatileInt16
	{
		public volatile short Value;
	}

	private struct VolatileInt32
	{
		public volatile int Value;
	}

	private struct VolatileIntPtr
	{
		public volatile nint Value;
	}

	private struct VolatileSByte
	{
		public volatile sbyte Value;
	}

	private struct VolatileSingle
	{
		public volatile float Value;
	}

	private struct VolatileUInt16
	{
		public volatile ushort Value;
	}

	private struct VolatileUInt32
	{
		public volatile uint Value;
	}

	private struct VolatileUIntPtr
	{
		public volatile nuint Value;
	}

	private struct VolatileObject
	{
		public volatile object Value;
	}

	[Intrinsic]
	[NonVersionable]
	public static bool Read([In][RequiresLocation] ref bool location)
	{
		return Unsafe.As<bool, VolatileBoolean>(ref Unsafe.AsRef(ref location)).Value;
	}

	[Intrinsic]
	[NonVersionable]
	public static void Write(ref bool location, bool value)
	{
		Unsafe.As<bool, VolatileBoolean>(ref location).Value = value;
	}

	[Intrinsic]
	[NonVersionable]
	public static byte Read([In][RequiresLocation] ref byte location)
	{
		return Unsafe.As<byte, VolatileByte>(ref Unsafe.AsRef(ref location)).Value;
	}

	[Intrinsic]
	[NonVersionable]
	public static void Write(ref byte location, byte value)
	{
		Unsafe.As<byte, VolatileByte>(ref location).Value = value;
	}

	[Intrinsic]
	[NonVersionable]
	public static double Read([In][RequiresLocation] ref double location)
	{
		long value = Read(ref Unsafe.As<double, long>(ref Unsafe.AsRef(ref location)));
		return BitConverter.Int64BitsToDouble(value);
	}

	[Intrinsic]
	[NonVersionable]
	public static void Write(ref double location, double value)
	{
		Write(ref Unsafe.As<double, long>(ref location), BitConverter.DoubleToInt64Bits(value));
	}

	[Intrinsic]
	[NonVersionable]
	public static short Read([In][RequiresLocation] ref short location)
	{
		return Unsafe.As<short, VolatileInt16>(ref Unsafe.AsRef(ref location)).Value;
	}

	[Intrinsic]
	[NonVersionable]
	public static void Write(ref short location, short value)
	{
		Unsafe.As<short, VolatileInt16>(ref location).Value = value;
	}

	[Intrinsic]
	[NonVersionable]
	public static int Read([In][RequiresLocation] ref int location)
	{
		return Unsafe.As<int, VolatileInt32>(ref Unsafe.AsRef(ref location)).Value;
	}

	[Intrinsic]
	[NonVersionable]
	public static void Write(ref int location, int value)
	{
		Unsafe.As<int, VolatileInt32>(ref location).Value = value;
	}

	[Intrinsic]
	[NonVersionable]
	public static long Read([In][RequiresLocation] ref long location)
	{
		return Unsafe.As<long, VolatileIntPtr>(ref Unsafe.AsRef(ref location)).Value;
	}

	[Intrinsic]
	[NonVersionable]
	public static void Write(ref long location, long value)
	{
		Unsafe.As<long, VolatileIntPtr>(ref location).Value = (nint)value;
	}

	[Intrinsic]
	[NonVersionable]
	public static nint Read([In][RequiresLocation] ref nint location)
	{
		return Unsafe.As<nint, VolatileIntPtr>(ref Unsafe.AsRef(ref location)).Value;
	}

	[Intrinsic]
	[NonVersionable]
	public static void Write(ref nint location, nint value)
	{
		Unsafe.As<nint, VolatileIntPtr>(ref location).Value = value;
	}

	[CLSCompliant(false)]
	[Intrinsic]
	[NonVersionable]
	public static sbyte Read([In][RequiresLocation] ref sbyte location)
	{
		return Unsafe.As<sbyte, VolatileSByte>(ref Unsafe.AsRef(ref location)).Value;
	}

	[CLSCompliant(false)]
	[Intrinsic]
	[NonVersionable]
	public static void Write(ref sbyte location, sbyte value)
	{
		Unsafe.As<sbyte, VolatileSByte>(ref location).Value = value;
	}

	[Intrinsic]
	[NonVersionable]
	public static float Read([In][RequiresLocation] ref float location)
	{
		return Unsafe.As<float, VolatileSingle>(ref Unsafe.AsRef(ref location)).Value;
	}

	[Intrinsic]
	[NonVersionable]
	public static void Write(ref float location, float value)
	{
		Unsafe.As<float, VolatileSingle>(ref location).Value = value;
	}

	[CLSCompliant(false)]
	[Intrinsic]
	[NonVersionable]
	public static ushort Read([In][RequiresLocation] ref ushort location)
	{
		return Unsafe.As<ushort, VolatileUInt16>(ref Unsafe.AsRef(ref location)).Value;
	}

	[CLSCompliant(false)]
	[Intrinsic]
	[NonVersionable]
	public static void Write(ref ushort location, ushort value)
	{
		Unsafe.As<ushort, VolatileUInt16>(ref location).Value = value;
	}

	[CLSCompliant(false)]
	[Intrinsic]
	[NonVersionable]
	public static uint Read([In][RequiresLocation] ref uint location)
	{
		return Unsafe.As<uint, VolatileUInt32>(ref Unsafe.AsRef(ref location)).Value;
	}

	[CLSCompliant(false)]
	[Intrinsic]
	[NonVersionable]
	public static void Write(ref uint location, uint value)
	{
		Unsafe.As<uint, VolatileUInt32>(ref location).Value = value;
	}

	[CLSCompliant(false)]
	[Intrinsic]
	[NonVersionable]
	public static ulong Read([In][RequiresLocation] ref ulong location)
	{
		return (ulong)Read(ref Unsafe.As<ulong, long>(ref Unsafe.AsRef(ref location)));
	}

	[CLSCompliant(false)]
	[Intrinsic]
	[NonVersionable]
	public static void Write(ref ulong location, ulong value)
	{
		Write(ref Unsafe.As<ulong, long>(ref location), (long)value);
	}

	[CLSCompliant(false)]
	[Intrinsic]
	[NonVersionable]
	public static nuint Read([In][RequiresLocation] ref nuint location)
	{
		return Unsafe.As<nuint, VolatileUIntPtr>(ref Unsafe.AsRef(ref location)).Value;
	}

	[CLSCompliant(false)]
	[Intrinsic]
	[NonVersionable]
	public static void Write(ref nuint location, nuint value)
	{
		Unsafe.As<nuint, VolatileUIntPtr>(ref location).Value = value;
	}

	[Intrinsic]
	[NonVersionable]
	[return: NotNullIfNotNull("location")]
	public static T Read<T>([In][RequiresLocation][NotNullIfNotNull("location")] ref T location) where T : class?
	{
		return Unsafe.As<T>(Unsafe.As<T, VolatileObject>(ref Unsafe.AsRef(ref location)).Value);
	}

	[Intrinsic]
	[NonVersionable]
	public static void Write<T>([NotNullIfNotNull("value")] ref T location, T value) where T : class?
	{
		Unsafe.As<T, VolatileObject>(ref location).Value = value;
	}
}
