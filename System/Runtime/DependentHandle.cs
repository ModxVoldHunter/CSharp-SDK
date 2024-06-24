using System.Runtime.CompilerServices;

namespace System.Runtime;

public struct DependentHandle : IDisposable
{
	private nint _handle;

	public bool IsAllocated => _handle != 0;

	public object? Target
	{
		get
		{
			nint handle = _handle;
			if (handle == 0)
			{
				ThrowHelper.ThrowInvalidOperationException();
			}
			return InternalGetTarget(handle);
		}
		set
		{
			nint handle = _handle;
			if (handle == 0 || value != null)
			{
				ThrowHelper.ThrowInvalidOperationException();
			}
			InternalSetTargetToNull(handle);
		}
	}

	public object? Dependent
	{
		get
		{
			nint handle = _handle;
			if (handle == 0)
			{
				ThrowHelper.ThrowInvalidOperationException();
			}
			return InternalGetDependent(handle);
		}
		set
		{
			nint handle = _handle;
			if (handle == 0)
			{
				ThrowHelper.ThrowInvalidOperationException();
			}
			InternalSetDependent(handle, value);
		}
	}

	public (object? Target, object? Dependent) TargetAndDependent
	{
		get
		{
			nint handle = _handle;
			if (handle == 0)
			{
				ThrowHelper.ThrowInvalidOperationException();
			}
			object dependent;
			object item = InternalGetTargetAndDependent(handle, out dependent);
			return (Target: item, Dependent: dependent);
		}
	}

	public DependentHandle(object? target, object? dependent)
	{
		_handle = InternalInitialize(target, dependent);
	}

	internal object UnsafeGetTarget()
	{
		return InternalGetTarget(_handle);
	}

	internal object UnsafeGetTargetAndDependent(out object dependent)
	{
		return InternalGetTargetAndDependent(_handle, out dependent);
	}

	internal void UnsafeSetTargetToNull()
	{
		InternalSetTargetToNull(_handle);
	}

	internal void UnsafeSetDependent(object dependent)
	{
		InternalSetDependent(_handle, dependent);
	}

	public void Dispose()
	{
		nint handle = _handle;
		if (handle != 0)
		{
			_handle = IntPtr.Zero;
			InternalFree(handle);
		}
	}

	[MethodImpl(MethodImplOptions.InternalCall)]
	private static extern nint InternalInitialize(object target, object dependent);

	private unsafe static object InternalGetTarget(nint dependentHandle)
	{
		return Unsafe.Read<object>((void*)dependentHandle);
	}

	[MethodImpl(MethodImplOptions.InternalCall)]
	private static extern object InternalGetDependent(nint dependentHandle);

	[MethodImpl(MethodImplOptions.InternalCall)]
	private static extern object InternalGetTargetAndDependent(nint dependentHandle, out object dependent);

	[MethodImpl(MethodImplOptions.InternalCall)]
	private static extern void InternalSetDependent(nint dependentHandle, object dependent);

	[MethodImpl(MethodImplOptions.InternalCall)]
	private static extern void InternalSetTargetToNull(nint dependentHandle);

	[MethodImpl(MethodImplOptions.InternalCall)]
	private static extern void InternalFree(nint dependentHandle);
}
