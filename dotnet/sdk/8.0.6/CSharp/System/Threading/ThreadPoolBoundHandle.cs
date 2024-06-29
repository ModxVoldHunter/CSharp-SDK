using System.Diagnostics.Tracing;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace System.Threading;

public sealed class ThreadPoolBoundHandle : IDisposable, IDeferredDisposable
{
	private readonly SafeHandle _handle;

	private readonly SafeThreadPoolIOHandle _threadPoolHandle;

	private DeferredDisposableLifetime<ThreadPoolBoundHandle> _lifetime;

	private bool _isDisposed;

	public SafeHandle Handle => _handle;

	private unsafe static ThreadPoolBoundHandle BindHandleWindowsThreadPool(SafeHandle handle)
	{
		ArgumentNullException.ThrowIfNull(handle, "handle");
		if (handle.IsClosed || handle.IsInvalid)
		{
			throw new ArgumentException(SR.Argument_InvalidHandle, "handle");
		}
		SafeThreadPoolIOHandle safeThreadPoolIOHandle = Interop.Kernel32.CreateThreadpoolIo(handle, (delegate* unmanaged<nint, nint, nint, uint, nuint, nint, void>)(delegate*<nint, nint, nint, uint, nuint, nint, void>)(&OnNativeIOCompleted), IntPtr.Zero, IntPtr.Zero);
		if (safeThreadPoolIOHandle.IsInvalid)
		{
			int lastWin32Error = Marshal.GetLastWin32Error();
			switch (lastWin32Error)
			{
			case 6:
				throw new ArgumentException(SR.Argument_InvalidHandle, "handle");
			case 87:
				throw new ArgumentException(SR.Argument_AlreadyBoundOrSyncHandle, "handle");
			default:
				throw Win32Marshal.GetExceptionForWin32Error(lastWin32Error);
			}
		}
		return new ThreadPoolBoundHandle(handle, safeThreadPoolIOHandle);
	}

	private unsafe NativeOverlapped* AllocateNativeOverlappedWindowsThreadPool(IOCompletionCallback callback, object state, object pinData)
	{
		return AllocateNativeOverlappedWindowsThreadPool(callback, state, pinData, flowExecutionContext: true);
	}

	private unsafe NativeOverlapped* UnsafeAllocateNativeOverlappedWindowsThreadPool(IOCompletionCallback callback, object state, object pinData)
	{
		return AllocateNativeOverlappedWindowsThreadPool(callback, state, pinData, flowExecutionContext: false);
	}

	private unsafe NativeOverlapped* AllocateNativeOverlappedWindowsThreadPool(IOCompletionCallback callback, object state, object pinData, bool flowExecutionContext)
	{
		ArgumentNullException.ThrowIfNull(callback, "callback");
		AddRef();
		try
		{
			Win32ThreadPoolNativeOverlapped* ptr = Win32ThreadPoolNativeOverlapped.Allocate(callback, state, pinData, null, flowExecutionContext);
			ptr->Data._boundHandle = this;
			if (NativeRuntimeEventSource.Log.IsEnabled())
			{
				NativeRuntimeEventSource.Log.ThreadPoolIOEnqueue(Win32ThreadPoolNativeOverlapped.ToNativeOverlapped(ptr));
			}
			Interop.Kernel32.StartThreadpoolIo(_threadPoolHandle);
			return Win32ThreadPoolNativeOverlapped.ToNativeOverlapped(ptr);
		}
		catch
		{
			Release();
			throw;
		}
	}

	private unsafe NativeOverlapped* AllocateNativeOverlappedWindowsThreadPool(PreAllocatedOverlapped preAllocated)
	{
		ArgumentNullException.ThrowIfNull(preAllocated, "preAllocated");
		bool flag = false;
		bool flag2 = false;
		try
		{
			flag = AddRef();
			flag2 = preAllocated.AddRef();
			Win32ThreadPoolNativeOverlapped.OverlappedData data = preAllocated._overlappedWindowsThreadPool->Data;
			if (data._boundHandle != null)
			{
				throw new ArgumentException(SR.Argument_PreAllocatedAlreadyAllocated, "preAllocated");
			}
			data._boundHandle = this;
			if (NativeRuntimeEventSource.Log.IsEnabled())
			{
				NativeRuntimeEventSource.Log.ThreadPoolIOEnqueue(Win32ThreadPoolNativeOverlapped.ToNativeOverlapped(preAllocated._overlappedWindowsThreadPool));
			}
			Interop.Kernel32.StartThreadpoolIo(_threadPoolHandle);
			return Win32ThreadPoolNativeOverlapped.ToNativeOverlapped(preAllocated._overlappedWindowsThreadPool);
		}
		catch
		{
			if (flag2)
			{
				preAllocated.Release();
			}
			if (flag)
			{
				Release();
			}
			throw;
		}
	}

	private unsafe void FreeNativeOverlappedWindowsThreadPool(NativeOverlapped* overlapped)
	{
		ArgumentNullException.ThrowIfNull(overlapped, "overlapped");
		Win32ThreadPoolNativeOverlapped* overlapped2 = Win32ThreadPoolNativeOverlapped.FromNativeOverlapped(overlapped);
		Win32ThreadPoolNativeOverlapped.OverlappedData overlappedData = GetOverlappedData(overlapped2, this);
		if (!overlappedData._completed)
		{
			Interop.Kernel32.CancelThreadpoolIo(_threadPoolHandle);
			Release();
		}
		overlappedData._boundHandle = null;
		overlappedData._completed = false;
		if (overlappedData._preAllocated != null)
		{
			overlappedData._preAllocated.Release();
		}
		else
		{
			Win32ThreadPoolNativeOverlapped.Free(overlapped2);
		}
	}

	private unsafe static object GetNativeOverlappedStateWindowsThreadPool(NativeOverlapped* overlapped)
	{
		ArgumentNullException.ThrowIfNull(overlapped, "overlapped");
		Win32ThreadPoolNativeOverlapped* overlapped2 = Win32ThreadPoolNativeOverlapped.FromNativeOverlapped(overlapped);
		Win32ThreadPoolNativeOverlapped.OverlappedData overlappedData = GetOverlappedData(overlapped2, null);
		return overlappedData._state;
	}

	private unsafe static Win32ThreadPoolNativeOverlapped.OverlappedData GetOverlappedData(Win32ThreadPoolNativeOverlapped* overlapped, ThreadPoolBoundHandle expectedBoundHandle)
	{
		Win32ThreadPoolNativeOverlapped.OverlappedData data = overlapped->Data;
		if (data._boundHandle == null)
		{
			throw new ArgumentException(SR.Argument_NativeOverlappedAlreadyFree, "overlapped");
		}
		if (expectedBoundHandle != null && data._boundHandle != expectedBoundHandle)
		{
			throw new ArgumentException(SR.Argument_NativeOverlappedWrongBoundHandle, "overlapped");
		}
		return data;
	}

	[UnmanagedCallersOnly]
	private unsafe static void OnNativeIOCompleted(nint instance, nint context, nint overlappedPtr, uint ioResult, nuint numberOfBytesTransferred, nint ioPtr)
	{
		ThreadPoolCallbackWrapper threadPoolCallbackWrapper = ThreadPoolCallbackWrapper.Enter();
		ThreadPoolBoundHandle boundHandle = ((Win32ThreadPoolNativeOverlapped*)overlappedPtr)->Data._boundHandle;
		if (boundHandle == null)
		{
			throw new InvalidOperationException(SR.Argument_NativeOverlappedAlreadyFree);
		}
		boundHandle.Release();
		if (NativeRuntimeEventSource.Log.IsEnabled())
		{
			NativeRuntimeEventSource.Log.ThreadPoolIODequeue(Win32ThreadPoolNativeOverlapped.ToNativeOverlapped((Win32ThreadPoolNativeOverlapped*)overlappedPtr));
		}
		Win32ThreadPoolNativeOverlapped.CompleteWithCallback(ioResult, (uint)numberOfBytesTransferred, (Win32ThreadPoolNativeOverlapped*)overlappedPtr);
		ThreadPool.IncrementCompletedWorkItemCount();
		threadPoolCallbackWrapper.Exit();
	}

	private bool AddRef()
	{
		return _lifetime.AddRef();
	}

	private void Release()
	{
		_lifetime.Release(this);
	}

	private void DisposeWindowsThreadPool()
	{
		_lifetime.Dispose(this);
		GC.SuppressFinalize(this);
	}

	private void FinalizeWindowsThreadPool()
	{
		_ = 0;
		Dispose();
	}

	private void IDeferredDisposableOnFinalReleaseWindowsThreadPool(bool disposed)
	{
		if (disposed)
		{
			_threadPoolHandle.Dispose();
		}
	}

	private unsafe NativeOverlapped* AllocateNativeOverlappedPortableCore(IOCompletionCallback callback, object state, object pinData)
	{
		return AllocateNativeOverlappedPortableCore(callback, state, pinData, flowExecutionContext: true);
	}

	private unsafe NativeOverlapped* UnsafeAllocateNativeOverlappedPortableCore(IOCompletionCallback callback, object state, object pinData)
	{
		return AllocateNativeOverlappedPortableCore(callback, state, pinData, flowExecutionContext: false);
	}

	private unsafe NativeOverlapped* AllocateNativeOverlappedPortableCore(IOCompletionCallback callback, object state, object pinData, bool flowExecutionContext)
	{
		ArgumentNullException.ThrowIfNull(callback, "callback");
		ObjectDisposedException.ThrowIf(_isDisposed, this);
		ThreadPoolBoundHandleOverlapped threadPoolBoundHandleOverlapped = new ThreadPoolBoundHandleOverlapped(callback, state, pinData, null, flowExecutionContext);
		threadPoolBoundHandleOverlapped._boundHandle = this;
		return threadPoolBoundHandleOverlapped._nativeOverlapped;
	}

	private unsafe NativeOverlapped* AllocateNativeOverlappedPortableCore(PreAllocatedOverlapped preAllocated)
	{
		ArgumentNullException.ThrowIfNull(preAllocated, "preAllocated");
		ObjectDisposedException.ThrowIf(_isDisposed, this);
		preAllocated.AddRef();
		try
		{
			ThreadPoolBoundHandleOverlapped overlappedPortableCore = preAllocated._overlappedPortableCore;
			if (overlappedPortableCore._boundHandle != null)
			{
				throw new ArgumentException(SR.Argument_PreAllocatedAlreadyAllocated, "preAllocated");
			}
			overlappedPortableCore._boundHandle = this;
			return overlappedPortableCore._nativeOverlapped;
		}
		catch
		{
			preAllocated.Release();
			throw;
		}
	}

	private unsafe void FreeNativeOverlappedPortableCore(NativeOverlapped* overlapped)
	{
		ArgumentNullException.ThrowIfNull(overlapped, "overlapped");
		ThreadPoolBoundHandleOverlapped overlappedWrapper = GetOverlappedWrapper(overlapped);
		if (overlappedWrapper._boundHandle != this)
		{
			throw new ArgumentException(SR.Argument_NativeOverlappedWrongBoundHandle, "overlapped");
		}
		if (overlappedWrapper._preAllocated != null)
		{
			overlappedWrapper._preAllocated.Release();
		}
		else
		{
			Overlapped.Free(overlapped);
		}
	}

	private unsafe static object GetNativeOverlappedStatePortableCore(NativeOverlapped* overlapped)
	{
		ArgumentNullException.ThrowIfNull(overlapped, "overlapped");
		ThreadPoolBoundHandleOverlapped overlappedWrapper = GetOverlappedWrapper(overlapped);
		return overlappedWrapper._userState;
	}

	private unsafe static ThreadPoolBoundHandleOverlapped GetOverlappedWrapper(NativeOverlapped* overlapped)
	{
		try
		{
			return (ThreadPoolBoundHandleOverlapped)Overlapped.Unpack(overlapped);
		}
		catch (NullReferenceException innerException)
		{
			throw new ArgumentException(SR.Argument_NativeOverlappedAlreadyFree, "overlapped", innerException);
		}
	}

	private void DisposePortableCore()
	{
		_isDisposed = true;
	}

	private ThreadPoolBoundHandle(SafeHandle handle, SafeThreadPoolIOHandle threadPoolHandle)
	{
		_threadPoolHandle = threadPoolHandle;
		_handle = handle;
	}

	private ThreadPoolBoundHandle(SafeHandle handle)
	{
		_handle = handle;
		GC.SuppressFinalize(this);
	}

	private static ThreadPoolBoundHandle BindHandleCore(SafeHandle handle)
	{
		ArgumentNullException.ThrowIfNull(handle, "handle");
		if (handle.IsClosed || handle.IsInvalid)
		{
			throw new ArgumentException(SR.Argument_InvalidHandle, "handle");
		}
		try
		{
			bool flag = ThreadPool.BindHandle(handle);
		}
		catch (Exception ex)
		{
			if (ex.HResult == -2147024890)
			{
				throw new ArgumentException(SR.Argument_InvalidHandle, "handle");
			}
			if (ex.HResult == -2147024809)
			{
				throw new ArgumentException(SR.Argument_AlreadyBoundOrSyncHandle, "handle");
			}
			throw;
		}
		return new ThreadPoolBoundHandle(handle);
	}

	public static ThreadPoolBoundHandle BindHandle(SafeHandle handle)
	{
		if (!ThreadPool.UseWindowsThreadPool)
		{
			return BindHandleCore(handle);
		}
		return BindHandleWindowsThreadPool(handle);
	}

	[CLSCompliant(false)]
	public unsafe NativeOverlapped* AllocateNativeOverlapped(IOCompletionCallback callback, object? state, object? pinData)
	{
		if (!ThreadPool.UseWindowsThreadPool)
		{
			return AllocateNativeOverlappedPortableCore(callback, state, pinData);
		}
		return AllocateNativeOverlappedWindowsThreadPool(callback, state, pinData);
	}

	[CLSCompliant(false)]
	public unsafe NativeOverlapped* UnsafeAllocateNativeOverlapped(IOCompletionCallback callback, object? state, object? pinData)
	{
		if (!ThreadPool.UseWindowsThreadPool)
		{
			return UnsafeAllocateNativeOverlappedPortableCore(callback, state, pinData);
		}
		return UnsafeAllocateNativeOverlappedWindowsThreadPool(callback, state, pinData);
	}

	[CLSCompliant(false)]
	public unsafe NativeOverlapped* AllocateNativeOverlapped(PreAllocatedOverlapped preAllocated)
	{
		if (!ThreadPool.UseWindowsThreadPool)
		{
			return AllocateNativeOverlappedPortableCore(preAllocated);
		}
		return AllocateNativeOverlappedWindowsThreadPool(preAllocated);
	}

	[CLSCompliant(false)]
	public unsafe void FreeNativeOverlapped(NativeOverlapped* overlapped)
	{
		if (ThreadPool.UseWindowsThreadPool)
		{
			FreeNativeOverlappedWindowsThreadPool(overlapped);
		}
		else
		{
			FreeNativeOverlappedPortableCore(overlapped);
		}
	}

	[CLSCompliant(false)]
	public unsafe static object? GetNativeOverlappedState(NativeOverlapped* overlapped)
	{
		if (!ThreadPool.UseWindowsThreadPool)
		{
			return GetNativeOverlappedStatePortableCore(overlapped);
		}
		return GetNativeOverlappedStateWindowsThreadPool(overlapped);
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

	~ThreadPoolBoundHandle()
	{
		FinalizeWindowsThreadPool();
	}

	void IDeferredDisposable.OnFinalRelease(bool disposed)
	{
		if (ThreadPool.UseWindowsThreadPool)
		{
			IDeferredDisposableOnFinalReleaseWindowsThreadPool(disposed);
		}
	}
}
