using System.Diagnostics.Tracing;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Microsoft.Win32.SafeHandles;

namespace System.Threading;

[UnsupportedOSPlatform("browser")]
public sealed class RegisteredWaitHandle : MarshalByRefObject
{
	private readonly object _lock;

	private bool _unregistering;

	private GCHandle _gcHandle;

	private nint _tpWait;

	private SafeWaitHandle _waitHandle;

	private readonly _ThreadPoolWaitOrTimerCallback _callbackHelper;

	private readonly uint _millisecondsTimeout;

	private readonly int _signedMillisecondsTimeout;

	private bool _repeating;

	private static AutoResetEvent s_cachedEvent;

	private static readonly LowLevelLock s_callbackLock = new LowLevelLock();

	private int _numRequestedCallbacks;

	private bool _signalAfterCallbacksComplete;

	private bool _unregisterCalled;

	private bool _unregistered;

	private AutoResetEvent _callbacksComplete;

	private AutoResetEvent _removed;

	internal _ThreadPoolWaitOrTimerCallback? Callback => _callbackHelper;

	internal SafeWaitHandle Handle => _waitHandle;

	internal int TimeoutTimeMs { get; private set; }

	internal int TimeoutDurationMs => _signedMillisecondsTimeout;

	internal bool IsInfiniteTimeout => TimeoutDurationMs == -1;

	internal bool Repeating => _repeating;

	private SafeWaitHandle? UserUnregisterWaitHandle { get; set; }

	private nint UserUnregisterWaitHandleValue { get; set; }

	private static nint InvalidHandleValue => new IntPtr(-1);

	internal bool IsBlocking => UserUnregisterWaitHandleValue == InvalidHandleValue;

	internal PortableThreadPool.WaitThread? WaitThread { get; set; }

	internal unsafe RegisteredWaitHandle(SafeWaitHandle waitHandle, _ThreadPoolWaitOrTimerCallback callbackHelper, uint millisecondsTimeout, bool repeating)
	{
		_lock = new object();
		waitHandle.DangerousAddRef();
		_waitHandle = waitHandle;
		_callbackHelper = callbackHelper;
		_millisecondsTimeout = millisecondsTimeout;
		_repeating = repeating;
		_gcHandle = GCHandle.Alloc(this);
		_tpWait = Interop.Kernel32.CreateThreadpoolWait((delegate* unmanaged<nint, nint, nint, uint, void>)(delegate*<nint, nint, nint, uint, void>)(&RegisteredWaitCallback), (nint)_gcHandle, IntPtr.Zero);
		if (_tpWait == IntPtr.Zero)
		{
			_gcHandle.Free();
			throw new OutOfMemoryException();
		}
		if (NativeRuntimeEventSource.Log.IsEnabled())
		{
			NativeRuntimeEventSource.Log.ThreadPoolIOEnqueue(this);
		}
	}

	[UnmanagedCallersOnly]
	internal static void RegisteredWaitCallback(nint instance, nint context, nint wait, uint waitResult)
	{
		ThreadPoolCallbackWrapper threadPoolCallbackWrapper = ThreadPoolCallbackWrapper.Enter();
		RegisteredWaitHandle registeredWaitHandle = (RegisteredWaitHandle)((GCHandle)context).Target;
		bool timedOut = waitResult == 258;
		registeredWaitHandle.PerformCallbackWindowsThreadPool(timedOut);
		ThreadPool.IncrementCompletedWorkItemCount();
		threadPoolCallbackWrapper.Exit();
	}

	private void PerformCallbackWindowsThreadPool(bool timedOut)
	{
		lock (_lock)
		{
			if (!_unregistering)
			{
				if (_repeating)
				{
					RestartWait();
				}
				else
				{
					_gcHandle.Free();
				}
			}
		}
		if (NativeRuntimeEventSource.Log.IsEnabled())
		{
			NativeRuntimeEventSource.Log.ThreadPoolIODequeue(this);
		}
		_ThreadPoolWaitOrTimerCallback.PerformWaitOrTimerCallback(_callbackHelper, timedOut);
	}

	internal unsafe void RestartWait()
	{
		long* pftTimeout = null;
		if (_millisecondsTimeout != uint.MaxValue)
		{
			long num = -10000 * _millisecondsTimeout;
			pftTimeout = &num;
		}
		Interop.Kernel32.SetThreadpoolWait(_tpWait, _waitHandle.DangerousGetHandle(), (nint)pftTimeout);
	}

	private bool UnregisterWindowsThreadPool(WaitHandle waitObject)
	{
		lock (_lock)
		{
			if (!_unregistering)
			{
				_unregistering = true;
				Interop.Kernel32.SetThreadpoolWait(_tpWait, IntPtr.Zero, IntPtr.Zero);
				SafeWaitHandle safeWaitHandle = waitObject?.SafeWaitHandle;
				if (safeWaitHandle != null && safeWaitHandle.DangerousGetHandle() == new IntPtr(-1))
				{
					FinishUnregistering();
				}
				else
				{
					ThreadPool.QueueUserWorkItem(FinishUnregisteringAsync, safeWaitHandle);
				}
				return true;
			}
		}
		return false;
	}

	private void FinishUnregistering()
	{
		Interop.Kernel32.WaitForThreadpoolWaitCallbacks(_tpWait, fCancelPendingCallbacks: false);
		Interop.Kernel32.CloseThreadpoolWait(_tpWait);
		_tpWait = IntPtr.Zero;
		if (_gcHandle.IsAllocated)
		{
			_gcHandle.Free();
		}
		_waitHandle.DangerousRelease();
		_waitHandle = null;
		GC.SuppressFinalize(this);
	}

	private void FinishUnregisteringAsync(object waitObject)
	{
		FinishUnregistering();
		SafeWaitHandle safeWaitHandle = (SafeWaitHandle)waitObject;
		if (safeWaitHandle != null && !safeWaitHandle.IsInvalid)
		{
			Interop.Kernel32.SetEvent(safeWaitHandle);
		}
	}

	~RegisteredWaitHandle()
	{
		if (_lock == null || !Monitor.TryEnter(_lock))
		{
			return;
		}
		try
		{
			if (!_unregistering)
			{
				_unregistering = true;
				if (_tpWait != IntPtr.Zero)
				{
					Interop.Kernel32.CloseThreadpoolWait(_tpWait);
					_tpWait = IntPtr.Zero;
				}
				if (_waitHandle != null)
				{
					_waitHandle.DangerousRelease();
					_waitHandle = null;
				}
			}
		}
		finally
		{
			Monitor.Exit(_lock);
		}
	}

	public bool Unregister(WaitHandle waitObject)
	{
		if (!ThreadPool.UseWindowsThreadPool)
		{
			return UnregisterPortableCore(waitObject);
		}
		return UnregisterWindowsThreadPool(waitObject);
	}

	internal void PerformCallback(bool timedOut)
	{
		if (ThreadPool.UseWindowsThreadPool)
		{
			PerformCallbackWindowsThreadPool(timedOut);
		}
		else
		{
			PerformCallbackPortableCore(timedOut);
		}
	}

	internal RegisteredWaitHandle(WaitHandle waitHandle, _ThreadPoolWaitOrTimerCallback callbackHelper, int millisecondsTimeout, bool repeating)
	{
		GC.SuppressFinalize(this);
		Thread.ThrowIfNoThreadStart();
		_waitHandle = waitHandle.SafeWaitHandle;
		_callbackHelper = callbackHelper;
		_signedMillisecondsTimeout = millisecondsTimeout;
		_repeating = repeating;
		if (!IsInfiniteTimeout)
		{
			RestartTimeout();
		}
	}

	private static AutoResetEvent RentEvent()
	{
		return Interlocked.Exchange(ref s_cachedEvent, null) ?? new AutoResetEvent(initialState: false);
	}

	private static void ReturnEvent(AutoResetEvent resetEvent)
	{
		if (Interlocked.CompareExchange(ref s_cachedEvent, resetEvent, null) != null)
		{
			resetEvent.Dispose();
		}
	}

	internal void RestartTimeout()
	{
		TimeoutTimeMs = Environment.TickCount + TimeoutDurationMs;
	}

	private bool UnregisterPortableCore(WaitHandle waitObject)
	{
		s_callbackLock.Acquire();
		bool success = false;
		try
		{
			if (_unregisterCalled)
			{
				return false;
			}
			UserUnregisterWaitHandle = waitObject?.SafeWaitHandle;
			UserUnregisterWaitHandle?.DangerousAddRef(ref success);
			UserUnregisterWaitHandleValue = UserUnregisterWaitHandle?.DangerousGetHandle() ?? IntPtr.Zero;
			if (_unregistered)
			{
				SignalUserWaitHandle();
				return true;
			}
			if (IsBlocking)
			{
				_callbacksComplete = RentEvent();
			}
			else
			{
				_removed = RentEvent();
			}
		}
		catch (Exception)
		{
			if (_removed != null)
			{
				ReturnEvent(_removed);
				_removed = null;
			}
			else if (_callbacksComplete != null)
			{
				ReturnEvent(_callbacksComplete);
				_callbacksComplete = null;
			}
			UserUnregisterWaitHandleValue = IntPtr.Zero;
			if (success)
			{
				UserUnregisterWaitHandle?.DangerousRelease();
			}
			UserUnregisterWaitHandle = null;
			throw;
		}
		finally
		{
			_unregisterCalled = true;
			s_callbackLock.Release();
		}
		WaitThread.UnregisterWait(this);
		return true;
	}

	private void SignalUserWaitHandle()
	{
		SafeWaitHandle userUnregisterWaitHandle = UserUnregisterWaitHandle;
		nint userUnregisterWaitHandleValue = UserUnregisterWaitHandleValue;
		try
		{
			if (userUnregisterWaitHandleValue != IntPtr.Zero && userUnregisterWaitHandleValue != InvalidHandleValue)
			{
				EventWaitHandle.Set(userUnregisterWaitHandle);
			}
		}
		finally
		{
			userUnregisterWaitHandle?.DangerousRelease();
			_callbacksComplete?.Set();
			_unregistered = true;
		}
	}

	internal void PerformCallbackPortableCore(bool timedOut)
	{
		_ThreadPoolWaitOrTimerCallback.PerformWaitOrTimerCallback(Callback, timedOut);
		CompleteCallbackRequest();
	}

	internal void RequestCallback()
	{
		s_callbackLock.Acquire();
		try
		{
			_numRequestedCallbacks++;
		}
		finally
		{
			s_callbackLock.Release();
		}
	}

	internal void OnRemoveWait()
	{
		s_callbackLock.Acquire();
		try
		{
			_removed?.Set();
			if (_numRequestedCallbacks == 0)
			{
				SignalUserWaitHandle();
			}
			else
			{
				_signalAfterCallbacksComplete = true;
			}
		}
		finally
		{
			s_callbackLock.Release();
		}
	}

	private void CompleteCallbackRequest()
	{
		s_callbackLock.Acquire();
		try
		{
			_numRequestedCallbacks--;
			if (_numRequestedCallbacks == 0 && _signalAfterCallbacksComplete)
			{
				SignalUserWaitHandle();
			}
		}
		finally
		{
			s_callbackLock.Release();
		}
	}

	internal void WaitForCallbacks()
	{
		_callbacksComplete.WaitOne();
		ReturnEvent(_callbacksComplete);
		_callbacksComplete = null;
	}

	internal void WaitForRemoval()
	{
		_removed.WaitOne();
		ReturnEvent(_removed);
		_removed = null;
	}
}
