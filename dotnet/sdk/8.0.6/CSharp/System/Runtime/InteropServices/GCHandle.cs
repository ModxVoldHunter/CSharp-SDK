using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Threading;

namespace System.Runtime.InteropServices;

public struct GCHandle : IEquatable<GCHandle>
{
	private nint _handle;

	public object? Target
	{
		get
		{
			nint handle = _handle;
			ThrowIfInvalid(handle);
			return InternalGet(GetHandleValue(handle));
		}
		set
		{
			nint handle = _handle;
			ThrowIfInvalid(handle);
			if (IsPinned(handle) && !Marshal.IsPinnable(value))
			{
				throw new ArgumentException(SR.ArgumentException_NotIsomorphic, "value");
			}
			InternalSet(GetHandleValue(handle), value);
		}
	}

	public bool IsAllocated => _handle != 0;

	[MethodImpl(MethodImplOptions.InternalCall)]
	internal static extern nint InternalAlloc(object value, GCHandleType type);

	[MethodImpl(MethodImplOptions.InternalCall)]
	internal static extern void InternalFree(nint handle);

	internal unsafe static object InternalGet(nint handle)
	{
		return Unsafe.Read<object>((void*)handle);
	}

	[MethodImpl(MethodImplOptions.InternalCall)]
	internal static extern void InternalSet(nint handle, object value);

	[MethodImpl(MethodImplOptions.InternalCall)]
	internal static extern object InternalCompareExchange(nint handle, object value, object oldValue);

	private GCHandle(object value, GCHandleType type)
	{
		switch (type)
		{
		default:
			throw new ArgumentOutOfRangeException("type", SR.ArgumentOutOfRange_Enum);
		case GCHandleType.Pinned:
			if (!Marshal.IsPinnable(value))
			{
				throw new ArgumentException(SR.ArgumentException_NotIsomorphic, "value");
			}
			break;
		case GCHandleType.Weak:
		case GCHandleType.WeakTrackResurrection:
		case GCHandleType.Normal:
			break;
		}
		nint num = InternalAlloc(value, type);
		if (type == GCHandleType.Pinned)
		{
			num |= 1;
		}
		_handle = num;
	}

	private GCHandle(nint handle)
	{
		_handle = handle;
	}

	public static GCHandle Alloc(object? value)
	{
		return new GCHandle(value, GCHandleType.Normal);
	}

	public static GCHandle Alloc(object? value, GCHandleType type)
	{
		return new GCHandle(value, type);
	}

	public void Free()
	{
		nint handle = Interlocked.Exchange(ref _handle, IntPtr.Zero);
		ThrowIfInvalid(handle);
		InternalFree(GetHandleValue(handle));
	}

	public unsafe nint AddrOfPinnedObject()
	{
		nint handle = _handle;
		ThrowIfInvalid(handle);
		if (!IsPinned(handle))
		{
			ThrowHelper.ThrowInvalidOperationException_HandleIsNotPinned();
		}
		object obj = InternalGet(GetHandleValue(handle));
		if (obj == null)
		{
			return 0;
		}
		if (RuntimeHelpers.ObjectHasComponentSize(obj))
		{
			if (obj.GetType() == typeof(string))
			{
				return (nint)Unsafe.AsPointer(ref Unsafe.As<string>(obj).GetRawStringData());
			}
			return (nint)Unsafe.AsPointer(ref MemoryMarshal.GetArrayDataReference(Unsafe.As<Array>(obj)));
		}
		return (nint)Unsafe.AsPointer(ref obj.GetRawData());
	}

	public static explicit operator GCHandle(nint value)
	{
		return FromIntPtr(value);
	}

	public static GCHandle FromIntPtr(nint value)
	{
		ThrowIfInvalid(value);
		return new GCHandle(value);
	}

	public static explicit operator nint(GCHandle value)
	{
		return ToIntPtr(value);
	}

	public static nint ToIntPtr(GCHandle value)
	{
		return value._handle;
	}

	public override int GetHashCode()
	{
		return ((IntPtr)_handle).GetHashCode();
	}

	public override bool Equals([NotNullWhen(true)] object? o)
	{
		if (o is GCHandle other)
		{
			return Equals(other);
		}
		return false;
	}

	public bool Equals(GCHandle other)
	{
		return _handle == other._handle;
	}

	public static bool operator ==(GCHandle a, GCHandle b)
	{
		return a._handle == b._handle;
	}

	public static bool operator !=(GCHandle a, GCHandle b)
	{
		return a._handle != b._handle;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static nint GetHandleValue(nint handle)
	{
		return new IntPtr(handle & ~(nint)1);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static bool IsPinned(nint handle)
	{
		return (handle & 1) != 0;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static void ThrowIfInvalid(nint handle)
	{
		if (handle == 0)
		{
			ThrowHelper.ThrowInvalidOperationException_HandleIsNotInitialized();
		}
	}
}
