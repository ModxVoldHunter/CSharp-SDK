using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace System.Runtime.CompilerServices;

public static class Unsafe
{
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[NonVersionable]
	[CLSCompliant(false)]
	public unsafe static void* AsPointer<T>(ref T value)
	{
		throw new PlatformNotSupportedException();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[NonVersionable]
	public static int SizeOf<T>()
	{
		return Unsafe.SizeOf<T>();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[NonVersionable]
	[return: NotNullIfNotNull("o")]
	public static T As<T>(object? o) where T : class?
	{
		throw new PlatformNotSupportedException();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[NonVersionable]
	public static ref TTo As<TFrom, TTo>(ref TFrom source)
	{
		throw new PlatformNotSupportedException();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[NonVersionable]
	public static ref T Add<T>(ref T source, int elementOffset)
	{
		typeof(T).ToString();
		throw new PlatformNotSupportedException();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[NonVersionable]
	public static ref T Add<T>(ref T source, nint elementOffset)
	{
		typeof(T).ToString();
		throw new PlatformNotSupportedException();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[NonVersionable]
	[CLSCompliant(false)]
	public unsafe static void* Add<T>(void* source, int elementOffset)
	{
		typeof(T).ToString();
		throw new PlatformNotSupportedException();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[NonVersionable]
	[CLSCompliant(false)]
	public static ref T Add<T>(ref T source, nuint elementOffset)
	{
		typeof(T).ToString();
		throw new PlatformNotSupportedException();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[NonVersionable]
	[CLSCompliant(false)]
	public static ref T AddByteOffset<T>(ref T source, nuint byteOffset)
	{
		typeof(T).ToString();
		throw new PlatformNotSupportedException();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[NonVersionable]
	public static bool AreSame<T>([In][RequiresLocation][AllowNull] ref T left, [In][RequiresLocation][AllowNull] ref T right)
	{
		throw new PlatformNotSupportedException();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[NonVersionable]
	public static TTo BitCast<TFrom, TTo>(TFrom source) where TFrom : struct where TTo : struct
	{
		if (Unsafe.SizeOf<TFrom>() != Unsafe.SizeOf<TTo>())
		{
			ThrowHelper.ThrowNotSupportedException();
		}
		return ReadUnaligned<TTo>(ref As<TFrom, byte>(ref source));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[NonVersionable]
	[CLSCompliant(false)]
	public unsafe static void Copy<T>(void* destination, [In][RequiresLocation] ref T source)
	{
		throw new PlatformNotSupportedException();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[NonVersionable]
	[CLSCompliant(false)]
	public unsafe static void Copy<T>(ref T destination, void* source)
	{
		throw new PlatformNotSupportedException();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[NonVersionable]
	[CLSCompliant(false)]
	public unsafe static void CopyBlock(void* destination, void* source, uint byteCount)
	{
		throw new PlatformNotSupportedException();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[NonVersionable]
	[CLSCompliant(false)]
	public static void CopyBlock(ref byte destination, [In][RequiresLocation] ref byte source, uint byteCount)
	{
		throw new PlatformNotSupportedException();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[NonVersionable]
	[CLSCompliant(false)]
	public unsafe static void CopyBlockUnaligned(void* destination, void* source, uint byteCount)
	{
		throw new PlatformNotSupportedException();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[NonVersionable]
	[CLSCompliant(false)]
	public static void CopyBlockUnaligned(ref byte destination, [In][RequiresLocation] ref byte source, uint byteCount)
	{
		throw new PlatformNotSupportedException();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[NonVersionable]
	public static bool IsAddressGreaterThan<T>([In][RequiresLocation][AllowNull] ref T left, [In][RequiresLocation][AllowNull] ref T right)
	{
		throw new PlatformNotSupportedException();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[NonVersionable]
	public static bool IsAddressLessThan<T>([In][RequiresLocation][AllowNull] ref T left, [In][RequiresLocation][AllowNull] ref T right)
	{
		throw new PlatformNotSupportedException();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[NonVersionable]
	[CLSCompliant(false)]
	public unsafe static void InitBlock(void* startAddress, byte value, uint byteCount)
	{
		throw new PlatformNotSupportedException();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[NonVersionable]
	[CLSCompliant(false)]
	public static void InitBlock(ref byte startAddress, byte value, uint byteCount)
	{
		throw new PlatformNotSupportedException();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[NonVersionable]
	[CLSCompliant(false)]
	public unsafe static void InitBlockUnaligned(void* startAddress, byte value, uint byteCount)
	{
		throw new PlatformNotSupportedException();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[NonVersionable]
	[CLSCompliant(false)]
	public static void InitBlockUnaligned(ref byte startAddress, byte value, uint byteCount)
	{
		for (uint num = 0u; num < byteCount; num++)
		{
			AddByteOffset(ref startAddress, num) = value;
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[NonVersionable]
	[CLSCompliant(false)]
	public unsafe static T ReadUnaligned<T>(void* source)
	{
		typeof(T).ToString();
		throw new PlatformNotSupportedException();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[NonVersionable]
	public static T ReadUnaligned<T>([In][RequiresLocation] ref byte source)
	{
		typeof(T).ToString();
		throw new PlatformNotSupportedException();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[NonVersionable]
	[CLSCompliant(false)]
	public unsafe static void WriteUnaligned<T>(void* destination, T value)
	{
		typeof(T).ToString();
		throw new PlatformNotSupportedException();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[NonVersionable]
	public static void WriteUnaligned<T>(ref byte destination, T value)
	{
		typeof(T).ToString();
		throw new PlatformNotSupportedException();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[NonVersionable]
	public static ref T AddByteOffset<T>(ref T source, nint byteOffset)
	{
		throw new PlatformNotSupportedException();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[NonVersionable]
	[CLSCompliant(false)]
	public unsafe static T Read<T>(void* source)
	{
		return Unsafe.Read<T>(source);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[NonVersionable]
	[CLSCompliant(false)]
	public unsafe static void Write<T>(void* destination, T value)
	{
		Unsafe.Write(destination, value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[NonVersionable]
	[CLSCompliant(false)]
	public unsafe static ref T AsRef<T>(void* source)
	{
		return ref *(T*)source;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[NonVersionable]
	public static ref T AsRef<T>([In][RequiresLocation] scoped ref T source)
	{
		throw new PlatformNotSupportedException();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[NonVersionable]
	public static nint ByteOffset<T>([In][RequiresLocation][AllowNull] ref T origin, [In][RequiresLocation][AllowNull] ref T target)
	{
		throw new PlatformNotSupportedException();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[NonVersionable]
	public unsafe static ref T NullRef<T>()
	{
		return ref AsRef<T>(null);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[NonVersionable]
	public unsafe static bool IsNullRef<T>([In][RequiresLocation] ref T source)
	{
		return AsPointer(ref AsRef(ref source)) == null;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[NonVersionable]
	public static void SkipInit<T>(out T value)
	{
		throw new PlatformNotSupportedException();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[NonVersionable]
	public static ref T Subtract<T>(ref T source, int elementOffset)
	{
		typeof(T).ToString();
		throw new PlatformNotSupportedException();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[NonVersionable]
	[CLSCompliant(false)]
	public unsafe static void* Subtract<T>(void* source, int elementOffset)
	{
		typeof(T).ToString();
		throw new PlatformNotSupportedException();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[NonVersionable]
	public static ref T Subtract<T>(ref T source, nint elementOffset)
	{
		typeof(T).ToString();
		throw new PlatformNotSupportedException();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[NonVersionable]
	[CLSCompliant(false)]
	public static ref T Subtract<T>(ref T source, nuint elementOffset)
	{
		typeof(T).ToString();
		throw new PlatformNotSupportedException();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[NonVersionable]
	public static ref T SubtractByteOffset<T>(ref T source, nint byteOffset)
	{
		throw new PlatformNotSupportedException();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[NonVersionable]
	[CLSCompliant(false)]
	public static ref T SubtractByteOffset<T>(ref T source, nuint byteOffset)
	{
		typeof(T).ToString();
		throw new PlatformNotSupportedException();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[NonVersionable]
	public static ref T Unbox<T>(object box) where T : struct
	{
		throw new PlatformNotSupportedException();
	}
}
