using System.Runtime.ConstrainedExecution;
using System.Threading;

namespace System.Runtime.InteropServices;

public abstract class SafeHandle : CriticalFinalizerObject, IDisposable
{
	protected nint handle;

	private volatile int _state;

	private readonly bool _ownsHandle;

	private readonly bool _fullyInitialized;

	internal bool OwnsHandle => _ownsHandle;

	public bool IsClosed => (_state & 1) == 1;

	public abstract bool IsInvalid { get; }

	protected SafeHandle(nint invalidHandleValue, bool ownsHandle)
	{
		handle = invalidHandleValue;
		_state = 4;
		_ownsHandle = ownsHandle;
		if (!ownsHandle)
		{
			GC.SuppressFinalize(this);
		}
		Volatile.Write(ref _fullyInitialized, value: true);
	}

	~SafeHandle()
	{
		if (_fullyInitialized)
		{
			Dispose(disposing: false);
		}
	}

	protected internal void SetHandle(nint handle)
	{
		this.handle = handle;
	}

	public nint DangerousGetHandle()
	{
		return handle;
	}

	public void Close()
	{
		Dispose();
	}

	public void Dispose()
	{
		Dispose(disposing: true);
		GC.SuppressFinalize(this);
	}

	protected virtual void Dispose(bool disposing)
	{
		InternalRelease(disposeOrFinalizeOperation: true);
	}

	public void SetHandleAsInvalid()
	{
		Interlocked.Or(ref _state, 1);
		GC.SuppressFinalize(this);
	}

	protected abstract bool ReleaseHandle();

	public void DangerousAddRef(ref bool success)
	{
		int state;
		int value;
		do
		{
			state = _state;
			ObjectDisposedException.ThrowIf((state & 1) != 0, this);
			value = state + 4;
		}
		while (Interlocked.CompareExchange(ref _state, value, state) != state);
		success = true;
	}

	internal void DangerousAddRef()
	{
		bool success = false;
		DangerousAddRef(ref success);
	}

	public void DangerousRelease()
	{
		InternalRelease(disposeOrFinalizeOperation: false);
	}

	private void InternalRelease(bool disposeOrFinalizeOperation)
	{
		int state;
		bool flag;
		int num;
		do
		{
			state = _state;
			if (disposeOrFinalizeOperation && ((uint)state & 2u) != 0)
			{
				return;
			}
			ObjectDisposedException.ThrowIf((state & -4) == 0, this);
			flag = (state & -3) == 4 && _ownsHandle && !IsInvalid;
			num = state - 4;
			if ((state & -4) == 4)
			{
				num |= 1;
			}
			if (disposeOrFinalizeOperation)
			{
				num |= 2;
			}
		}
		while (Interlocked.CompareExchange(ref _state, num, state) != state);
		if (flag)
		{
			int lastPInvokeError = Marshal.GetLastPInvokeError();
			ReleaseHandle();
			Marshal.SetLastPInvokeError(lastPInvokeError);
		}
	}
}
