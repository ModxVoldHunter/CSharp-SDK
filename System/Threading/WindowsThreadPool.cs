using System.Diagnostics.Tracing;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Internal;

namespace System.Threading;

internal static class WindowsThreadPool
{
	private sealed class ThreadCountHolder
	{
		internal ThreadCountHolder()
		{
			Interlocked.Increment(ref s_threadCount);
		}

		~ThreadCountHolder()
		{
			Interlocked.Decrement(ref s_threadCount);
		}
	}

	private struct WorkingThreadCounter
	{
		private readonly PaddingFor32 pad1;

		public volatile int Count;

		private readonly PaddingFor32 pad2;
	}

	private static readonly int MaxThreadCount = Math.Max(8 * Environment.ProcessorCount, 768);

	private static nint s_work;

	[ThreadStatic]
	private static ThreadCountHolder t_threadCountHolder;

	private static int s_threadCount;

	private static WorkingThreadCounter s_workingThreadCounter;

	private static readonly ThreadInt64PersistentCounter s_completedWorkItemCounter = new ThreadInt64PersistentCounter();

	[ThreadStatic]
	private static object t_completionCountObject;

	public static int ThreadCount => s_threadCount;

	public static long CompletedWorkItemCount => s_completedWorkItemCounter.Count;

	internal static void InitializeForThreadPoolThread()
	{
		t_threadCountHolder = new ThreadCountHolder();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static void IncrementCompletedWorkItemCount()
	{
		ThreadInt64PersistentCounter.Increment(GetOrCreateThreadLocalCompletionCountObject());
	}

	internal static object GetOrCreateThreadLocalCompletionCountObject()
	{
		return t_completionCountObject ?? CreateThreadLocalCompletionCountObject();
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private static object CreateThreadLocalCompletionCountObject()
	{
		return t_completionCountObject = s_completedWorkItemCounter.CreateThreadLocalCountObject();
	}

	public static bool SetMaxThreads(int workerThreads, int completionPortThreads)
	{
		return false;
	}

	public static void GetMaxThreads(out int workerThreads, out int completionPortThreads)
	{
		workerThreads = MaxThreadCount;
		completionPortThreads = MaxThreadCount;
	}

	public static bool SetMinThreads(int workerThreads, int completionPortThreads)
	{
		return false;
	}

	public static void GetMinThreads(out int workerThreads, out int completionPortThreads)
	{
		workerThreads = 0;
		completionPortThreads = 0;
	}

	public static void GetAvailableThreads(out int workerThreads, out int completionPortThreads)
	{
		completionPortThreads = (workerThreads = Math.Max(MaxThreadCount - s_workingThreadCounter.Count, 0));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static void NotifyWorkItemProgress()
	{
		IncrementCompletedWorkItemCount();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static bool NotifyWorkItemComplete(object threadLocalCompletionCountObject, int _)
	{
		ThreadInt64PersistentCounter.Increment(threadLocalCompletionCountObject);
		return true;
	}

	internal static void NotifyThreadUnblocked()
	{
	}

	[UnmanagedCallersOnly]
	private static void DispatchCallback(nint instance, nint context, nint work)
	{
		ThreadPoolCallbackWrapper threadPoolCallbackWrapper = ThreadPoolCallbackWrapper.Enter();
		Interlocked.Increment(ref s_workingThreadCounter.Count);
		ThreadPoolWorkQueue.Dispatch();
		Interlocked.Decrement(ref s_workingThreadCounter.Count);
		threadPoolCallbackWrapper.Exit(resetThread: false);
	}

	internal unsafe static void RequestWorkerThread()
	{
		if (s_work == IntPtr.Zero)
		{
			nint num = Interop.Kernel32.CreateThreadpoolWork((delegate* unmanaged<nint, nint, nint, void>)(delegate*<nint, nint, nint, void>)(&DispatchCallback), IntPtr.Zero, IntPtr.Zero);
			if (num == IntPtr.Zero)
			{
				throw new OutOfMemoryException();
			}
			if (Interlocked.CompareExchange(ref s_work, num, IntPtr.Zero) != IntPtr.Zero)
			{
				Interop.Kernel32.CloseThreadpoolWork(num);
			}
		}
		Interop.Kernel32.SubmitThreadpoolWork(s_work);
	}

	internal static RegisteredWaitHandle RegisterWaitForSingleObject(WaitHandle waitObject, WaitOrTimerCallback callBack, object state, uint millisecondsTimeOutInterval, bool executeOnlyOnce, bool flowExecutionContext)
	{
		ArgumentNullException.ThrowIfNull(waitObject, "waitObject");
		ArgumentNullException.ThrowIfNull(callBack, "callBack");
		_ThreadPoolWaitOrTimerCallback callbackHelper = new _ThreadPoolWaitOrTimerCallback(callBack, state, flowExecutionContext);
		RegisteredWaitHandle registeredWaitHandle = new RegisteredWaitHandle(waitObject.SafeWaitHandle, callbackHelper, millisecondsTimeOutInterval, !executeOnlyOnce);
		registeredWaitHandle.RestartWait();
		return registeredWaitHandle;
	}

	private unsafe static void NativeOverlappedCallback(nint overlappedPtr)
	{
		if (NativeRuntimeEventSource.Log.IsEnabled())
		{
			NativeRuntimeEventSource.Log.ThreadPoolIODequeue((NativeOverlapped*)overlappedPtr);
		}
		IOCompletionCallbackHelper.PerformSingleIOCompletionCallback(0u, 0u, (NativeOverlapped*)overlappedPtr);
	}

	[SupportedOSPlatform("windows")]
	public unsafe static bool UnsafeQueueNativeOverlapped(NativeOverlapped* overlapped)
	{
		if (overlapped == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.overlapped);
		}
		overlapped->InternalLow = 0;
		if (NativeRuntimeEventSource.Log.IsEnabled())
		{
			NativeRuntimeEventSource.Log.ThreadPoolIOEnqueue(overlapped);
		}
		return ThreadPool.UnsafeQueueUserWorkItem(NativeOverlappedCallback, (nint)overlapped, preferLocal: false);
	}

	[Obsolete("ThreadPool.BindHandle(IntPtr) has been deprecated. Use ThreadPool.BindHandle(SafeHandle) instead.")]
	[SupportedOSPlatform("windows")]
	public static bool BindHandle(nint osHandle)
	{
		throw new PlatformNotSupportedException(SR.Arg_PlatformNotSupported);
	}

	[SupportedOSPlatform("windows")]
	public static bool BindHandle(SafeHandle osHandle)
	{
		throw new PlatformNotSupportedException(SR.Arg_PlatformNotSupported);
	}
}
