namespace System.Threading;

public sealed class PreAllocatedOverlapped : IDisposable, IDeferredDisposable
{
	internal unsafe readonly Win32ThreadPoolNativeOverlapped* _overlappedWindowsThreadPool;

	private DeferredDisposableLifetime<PreAllocatedOverlapped> _lifetime;

	internal ThreadPoolBoundHandleOverlapped _overlappedPortableCore;

	private static PreAllocatedOverlapped UnsafeCreateWindowsThreadPool(IOCompletionCallback callback, object state, object pinData)
	{
		return new PreAllocatedOverlapped(callback, state, pinData, flowExecutionContext: false);
	}

	private bool AddRefWindowsThreadPool()
	{
		return _lifetime.AddRef();
	}

	private void ReleaseWindowsThreadPool()
	{
		_lifetime.Release(this);
	}

	private void DisposeWindowsThreadPool()
	{
		_lifetime.Dispose(this);
		GC.SuppressFinalize(this);
	}

	private unsafe void IDeferredDisposableOnFinalReleaseWindowsThreadPool(bool disposed)
	{
		if (_overlappedWindowsThreadPool != null)
		{
			if (disposed)
			{
				Win32ThreadPoolNativeOverlapped.Free(_overlappedWindowsThreadPool);
			}
			else
			{
				*Win32ThreadPoolNativeOverlapped.ToNativeOverlapped(_overlappedWindowsThreadPool) = default(NativeOverlapped);
			}
		}
	}

	[CLSCompliant(false)]
	public PreAllocatedOverlapped(IOCompletionCallback callback, object? state, object? pinData)
		: this(callback, state, pinData, flowExecutionContext: true)
	{
	}

	[CLSCompliant(false)]
	public static PreAllocatedOverlapped UnsafeCreate(IOCompletionCallback callback, object? state, object? pinData)
	{
		if (!ThreadPool.UseWindowsThreadPool)
		{
			return UnsafeCreatePortableCore(callback, state, pinData);
		}
		return UnsafeCreateWindowsThreadPool(callback, state, pinData);
	}

	private unsafe PreAllocatedOverlapped(IOCompletionCallback callback, object state, object pinData, bool flowExecutionContext)
	{
		if (ThreadPool.UseWindowsThreadPool)
		{
			ArgumentNullException.ThrowIfNull(callback, "callback");
			_overlappedWindowsThreadPool = Win32ThreadPoolNativeOverlapped.Allocate(callback, state, pinData, this, flowExecutionContext);
		}
		else
		{
			ArgumentNullException.ThrowIfNull(callback, "callback");
			_overlappedPortableCore = new ThreadPoolBoundHandleOverlapped(callback, state, pinData, this, flowExecutionContext);
		}
	}

	internal bool AddRef()
	{
		if (!ThreadPool.UseWindowsThreadPool)
		{
			return AddRefPortableCore();
		}
		return AddRefWindowsThreadPool();
	}

	internal void Release()
	{
		if (ThreadPool.UseWindowsThreadPool)
		{
			ReleaseWindowsThreadPool();
		}
		else
		{
			ReleasePortableCore();
		}
	}

	public void Dispose()
	{
		if (ThreadPool.UseWindowsThreadPool)
		{
			DisposeWindowsThreadPool();
		}
		else
		{
			DisposePortableCore();
		}
	}

	~PreAllocatedOverlapped()
	{
		Dispose();
	}

	void IDeferredDisposable.OnFinalRelease(bool disposed)
	{
		if (ThreadPool.UseWindowsThreadPool)
		{
			IDeferredDisposableOnFinalReleaseWindowsThreadPool(disposed);
		}
		else
		{
			IDeferredDisposableOnFinalReleasePortableCore(disposed);
		}
	}

	private static PreAllocatedOverlapped UnsafeCreatePortableCore(IOCompletionCallback callback, object state, object pinData)
	{
		return new PreAllocatedOverlapped(callback, state, pinData, flowExecutionContext: false);
	}

	private bool AddRefPortableCore()
	{
		return _lifetime.AddRef();
	}

	private void ReleasePortableCore()
	{
		_lifetime.Release(this);
	}

	private void DisposePortableCore()
	{
		_lifetime.Dispose(this);
		GC.SuppressFinalize(this);
	}

	private unsafe void IDeferredDisposableOnFinalReleasePortableCore(bool disposed)
	{
		if (_overlappedPortableCore != null)
		{
			if (disposed)
			{
				Overlapped.Free(_overlappedPortableCore._nativeOverlapped);
				return;
			}
			_overlappedPortableCore._boundHandle = null;
			_overlappedPortableCore._completed = false;
			*_overlappedPortableCore._nativeOverlapped = default(NativeOverlapped);
		}
	}
}
