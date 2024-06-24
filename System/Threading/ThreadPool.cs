using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Threading.Tasks;

namespace System.Threading;

public static class ThreadPool
{
	private static readonly bool s_initialized = InitializeConfig();

	internal static readonly ThreadPoolWorkQueue s_workQueue = new ThreadPoolWorkQueue();

	internal static readonly Action<object> s_invokeAsyncStateMachineBox = delegate(object state)
	{
		if (state is IAsyncStateMachineBox asyncStateMachineBox)
		{
			asyncStateMachineBox.MoveNext();
		}
		else
		{
			ThrowHelper.ThrowUnexpectedStateForKnownCallback(state);
		}
	};

	private static readonly bool s_useWindowsThreadPool = UseWindowsThreadPool;

	private static readonly bool IsWorkerTrackingEnabledInConfig = !UseWindowsThreadPool && AppContextConfigHelper.GetBooleanConfig("System.Threading.ThreadPool.EnableWorkerTracking", "DOTNET_ThreadPool_EnableWorkerTracking");

	internal static bool EnableWorkerTracking
	{
		get
		{
			if (IsWorkerTrackingEnabledInConfig)
			{
				return EventSource.IsSupported;
			}
			return false;
		}
	}

	public static long PendingWorkItemCount
	{
		get
		{
			ThreadPoolWorkQueue threadPoolWorkQueue = s_workQueue;
			return ThreadPoolWorkQueue.LocalCount + threadPoolWorkQueue.GlobalCount;
		}
	}

	internal static bool UseWindowsThreadPool { get; } = AppContextConfigHelper.GetBooleanConfig("System.Threading.ThreadPool.UseWindowsThreadPool", "DOTNET_ThreadPool_UseWindowsThreadPool");


	internal static bool YieldFromDispatchLoop => UseWindowsThreadPool;

	public static int ThreadCount
	{
		get
		{
			if (!UseWindowsThreadPool)
			{
				return PortableThreadPool.ThreadPoolInstance.ThreadCount;
			}
			return WindowsThreadPool.ThreadCount;
		}
	}

	public static long CompletedWorkItemCount
	{
		get
		{
			if (!UseWindowsThreadPool)
			{
				return PortableThreadPool.ThreadPoolInstance.CompletedWorkItemCount;
			}
			return WindowsThreadPool.CompletedWorkItemCount;
		}
	}

	internal static bool EnsureConfigInitialized()
	{
		return s_initialized;
	}

	private unsafe static bool InitializeConfig()
	{
		int configVariableIndex = 1;
		while (true)
		{
			uint configValue;
			bool isBoolean;
			char* appContextConfigName;
			int nextConfigUInt32Value = GetNextConfigUInt32Value(configVariableIndex, out configValue, out isBoolean, out appContextConfigName);
			if (nextConfigUInt32Value < 0)
			{
				break;
			}
			configVariableIndex = nextConfigUInt32Value;
			string text = new string(appContextConfigName);
			if (isBoolean)
			{
				AppContext.SetSwitch(text, configValue != 0);
			}
			else
			{
				AppContext.SetData(text, configValue);
			}
		}
		return true;
	}

	[MethodImpl(MethodImplOptions.InternalCall)]
	private unsafe static extern int GetNextConfigUInt32Value(int configVariableIndex, out uint configValue, out bool isBoolean, out char* appContextConfigName);

	[UnsupportedOSPlatform("browser")]
	[CLSCompliant(false)]
	public static RegisteredWaitHandle RegisterWaitForSingleObject(WaitHandle waitObject, WaitOrTimerCallback callBack, object? state, uint millisecondsTimeOutInterval, bool executeOnlyOnce)
	{
		if (millisecondsTimeOutInterval > int.MaxValue && millisecondsTimeOutInterval != uint.MaxValue)
		{
			throw new ArgumentOutOfRangeException("millisecondsTimeOutInterval", SR.ArgumentOutOfRange_LessEqualToIntegerMaxVal);
		}
		return RegisterWaitForSingleObject(waitObject, callBack, state, millisecondsTimeOutInterval, executeOnlyOnce, flowExecutionContext: true);
	}

	[UnsupportedOSPlatform("browser")]
	[CLSCompliant(false)]
	public static RegisteredWaitHandle UnsafeRegisterWaitForSingleObject(WaitHandle waitObject, WaitOrTimerCallback callBack, object? state, uint millisecondsTimeOutInterval, bool executeOnlyOnce)
	{
		if (millisecondsTimeOutInterval > int.MaxValue && millisecondsTimeOutInterval != uint.MaxValue)
		{
			throw new ArgumentOutOfRangeException("millisecondsTimeOutInterval", SR.ArgumentOutOfRange_NeedNonNegOrNegative1);
		}
		return RegisterWaitForSingleObject(waitObject, callBack, state, millisecondsTimeOutInterval, executeOnlyOnce, flowExecutionContext: false);
	}

	[UnsupportedOSPlatform("browser")]
	public static RegisteredWaitHandle RegisterWaitForSingleObject(WaitHandle waitObject, WaitOrTimerCallback callBack, object? state, int millisecondsTimeOutInterval, bool executeOnlyOnce)
	{
		ArgumentOutOfRangeException.ThrowIfLessThan(millisecondsTimeOutInterval, -1, "millisecondsTimeOutInterval");
		return RegisterWaitForSingleObject(waitObject, callBack, state, (uint)millisecondsTimeOutInterval, executeOnlyOnce, flowExecutionContext: true);
	}

	[UnsupportedOSPlatform("browser")]
	public static RegisteredWaitHandle UnsafeRegisterWaitForSingleObject(WaitHandle waitObject, WaitOrTimerCallback callBack, object? state, int millisecondsTimeOutInterval, bool executeOnlyOnce)
	{
		ArgumentOutOfRangeException.ThrowIfLessThan(millisecondsTimeOutInterval, -1, "millisecondsTimeOutInterval");
		return RegisterWaitForSingleObject(waitObject, callBack, state, (uint)millisecondsTimeOutInterval, executeOnlyOnce, flowExecutionContext: false);
	}

	[UnsupportedOSPlatform("browser")]
	public static RegisteredWaitHandle RegisterWaitForSingleObject(WaitHandle waitObject, WaitOrTimerCallback callBack, object? state, long millisecondsTimeOutInterval, bool executeOnlyOnce)
	{
		ArgumentOutOfRangeException.ThrowIfLessThan(millisecondsTimeOutInterval, -1L, "millisecondsTimeOutInterval");
		ArgumentOutOfRangeException.ThrowIfGreaterThan(millisecondsTimeOutInterval, 2147483647L, "millisecondsTimeOutInterval");
		return RegisterWaitForSingleObject(waitObject, callBack, state, (uint)millisecondsTimeOutInterval, executeOnlyOnce, flowExecutionContext: true);
	}

	[UnsupportedOSPlatform("browser")]
	public static RegisteredWaitHandle UnsafeRegisterWaitForSingleObject(WaitHandle waitObject, WaitOrTimerCallback callBack, object? state, long millisecondsTimeOutInterval, bool executeOnlyOnce)
	{
		ArgumentOutOfRangeException.ThrowIfLessThan(millisecondsTimeOutInterval, -1L, "millisecondsTimeOutInterval");
		ArgumentOutOfRangeException.ThrowIfGreaterThan(millisecondsTimeOutInterval, 2147483647L, "millisecondsTimeOutInterval");
		return RegisterWaitForSingleObject(waitObject, callBack, state, (uint)millisecondsTimeOutInterval, executeOnlyOnce, flowExecutionContext: false);
	}

	[UnsupportedOSPlatform("browser")]
	public static RegisteredWaitHandle RegisterWaitForSingleObject(WaitHandle waitObject, WaitOrTimerCallback callBack, object? state, TimeSpan timeout, bool executeOnlyOnce)
	{
		long num = (long)timeout.TotalMilliseconds;
		ArgumentOutOfRangeException.ThrowIfLessThan(num, -1L, "timeout");
		ArgumentOutOfRangeException.ThrowIfGreaterThan(num, 2147483647L, "timeout");
		return RegisterWaitForSingleObject(waitObject, callBack, state, (uint)num, executeOnlyOnce, flowExecutionContext: true);
	}

	[UnsupportedOSPlatform("browser")]
	public static RegisteredWaitHandle UnsafeRegisterWaitForSingleObject(WaitHandle waitObject, WaitOrTimerCallback callBack, object? state, TimeSpan timeout, bool executeOnlyOnce)
	{
		long num = (long)timeout.TotalMilliseconds;
		ArgumentOutOfRangeException.ThrowIfLessThan(num, -1L, "timeout");
		ArgumentOutOfRangeException.ThrowIfGreaterThan(num, 2147483647L, "timeout");
		return RegisterWaitForSingleObject(waitObject, callBack, state, (uint)num, executeOnlyOnce, flowExecutionContext: false);
	}

	public static bool QueueUserWorkItem(WaitCallback callBack)
	{
		return QueueUserWorkItem(callBack, null);
	}

	public static bool QueueUserWorkItem(WaitCallback callBack, object? state)
	{
		if (callBack == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.callBack);
		}
		ExecutionContext executionContext = ExecutionContext.Capture();
		object callback = ((executionContext == null || executionContext.IsDefault) ? ((QueueUserWorkItemCallbackBase)new QueueUserWorkItemCallbackDefaultContext(callBack, state)) : ((QueueUserWorkItemCallbackBase)new QueueUserWorkItemCallback(callBack, state, executionContext)));
		s_workQueue.Enqueue(callback, forceGlobal: true);
		return true;
	}

	public static bool QueueUserWorkItem<TState>(Action<TState> callBack, TState state, bool preferLocal)
	{
		if (callBack == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.callBack);
		}
		ExecutionContext executionContext = ExecutionContext.Capture();
		object callback = ((executionContext == null || executionContext.IsDefault) ? ((QueueUserWorkItemCallbackBase)new QueueUserWorkItemCallbackDefaultContext<TState>(callBack, state)) : ((QueueUserWorkItemCallbackBase)new QueueUserWorkItemCallback<TState>(callBack, state, executionContext)));
		s_workQueue.Enqueue(callback, !preferLocal);
		return true;
	}

	public static bool UnsafeQueueUserWorkItem<TState>(Action<TState> callBack, TState state, bool preferLocal)
	{
		if (callBack == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.callBack);
		}
		if ((object)callBack == s_invokeAsyncStateMachineBox)
		{
			if (!(state is IAsyncStateMachineBox))
			{
				ThrowHelper.ThrowUnexpectedStateForKnownCallback(state);
			}
			UnsafeQueueUserWorkItemInternal(state, preferLocal);
			return true;
		}
		s_workQueue.Enqueue(new QueueUserWorkItemCallbackDefaultContext<TState>(callBack, state), !preferLocal);
		return true;
	}

	public static bool UnsafeQueueUserWorkItem(WaitCallback callBack, object? state)
	{
		if (callBack == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.callBack);
		}
		object callback = new QueueUserWorkItemCallbackDefaultContext(callBack, state);
		s_workQueue.Enqueue(callback, forceGlobal: true);
		return true;
	}

	public static bool UnsafeQueueUserWorkItem(IThreadPoolWorkItem callBack, bool preferLocal)
	{
		if (callBack == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.callBack);
		}
		if (callBack is Task)
		{
			ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.callBack);
		}
		UnsafeQueueUserWorkItemInternal(callBack, preferLocal);
		return true;
	}

	internal static void UnsafeQueueUserWorkItemInternal(object callBack, bool preferLocal)
	{
		s_workQueue.Enqueue(callBack, !preferLocal);
	}

	internal static void UnsafeQueueHighPriorityWorkItemInternal(IThreadPoolWorkItem callBack)
	{
		s_workQueue.EnqueueAtHighPriority(callBack);
	}

	internal static bool TryPopCustomWorkItem(object workItem)
	{
		return ThreadPoolWorkQueue.LocalFindAndPop(workItem);
	}

	internal static IEnumerable<object> GetQueuedWorkItems()
	{
		foreach (object highPriorityWorkItem in s_workQueue.highPriorityWorkItems)
		{
			yield return highPriorityWorkItem;
		}
		ConcurrentQueue<object>[] assignableWorkItemQueues = s_workQueue._assignableWorkItemQueues;
		foreach (ConcurrentQueue<object> concurrentQueue in assignableWorkItemQueues)
		{
			foreach (object item in concurrentQueue)
			{
				yield return item;
			}
		}
		foreach (object workItem in s_workQueue.workItems)
		{
			yield return workItem;
		}
		ThreadPoolWorkQueue.WorkStealingQueue[] queues = ThreadPoolWorkQueue.WorkStealingQueueList.Queues;
		foreach (ThreadPoolWorkQueue.WorkStealingQueue workStealingQueue in queues)
		{
			if (workStealingQueue == null || workStealingQueue.m_array == null)
			{
				continue;
			}
			object[] items = workStealingQueue.m_array;
			foreach (object obj in items)
			{
				if (obj != null)
				{
					yield return obj;
				}
			}
		}
	}

	[CLSCompliant(false)]
	[SupportedOSPlatform("windows")]
	public unsafe static bool UnsafeQueueNativeOverlapped(NativeOverlapped* overlapped)
	{
		if (!UseWindowsThreadPool)
		{
			return UnsafeQueueNativeOverlappedPortableCore(overlapped);
		}
		return WindowsThreadPool.UnsafeQueueNativeOverlapped(overlapped);
	}

	[Obsolete("ThreadPool.BindHandle(IntPtr) has been deprecated. Use ThreadPool.BindHandle(SafeHandle) instead.")]
	[SupportedOSPlatform("windows")]
	public static bool BindHandle(nint osHandle)
	{
		if (!UseWindowsThreadPool)
		{
			return BindHandlePortableCore(osHandle);
		}
		return WindowsThreadPool.BindHandle(osHandle);
	}

	[SupportedOSPlatform("windows")]
	public static bool BindHandle(SafeHandle osHandle)
	{
		if (!UseWindowsThreadPool)
		{
			return BindHandlePortableCore(osHandle);
		}
		return WindowsThreadPool.BindHandle(osHandle);
	}

	internal static void InitializeForThreadPoolThread()
	{
		if (UseWindowsThreadPool)
		{
			WindowsThreadPool.InitializeForThreadPoolThread();
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static void IncrementCompletedWorkItemCount()
	{
		WindowsThreadPool.IncrementCompletedWorkItemCount();
	}

	internal static object GetOrCreateThreadLocalCompletionCountObject()
	{
		if (!UseWindowsThreadPool)
		{
			return PortableThreadPool.ThreadPoolInstance.GetOrCreateThreadLocalCompletionCountObject();
		}
		return WindowsThreadPool.GetOrCreateThreadLocalCompletionCountObject();
	}

	public static bool SetMaxThreads(int workerThreads, int completionPortThreads)
	{
		if (!UseWindowsThreadPool)
		{
			return PortableThreadPool.ThreadPoolInstance.SetMaxThreads(workerThreads, completionPortThreads);
		}
		return WindowsThreadPool.SetMaxThreads(workerThreads, completionPortThreads);
	}

	public static void GetMaxThreads(out int workerThreads, out int completionPortThreads)
	{
		if (UseWindowsThreadPool)
		{
			WindowsThreadPool.GetMaxThreads(out workerThreads, out completionPortThreads);
		}
		else
		{
			PortableThreadPool.ThreadPoolInstance.GetMaxThreads(out workerThreads, out completionPortThreads);
		}
	}

	public static bool SetMinThreads(int workerThreads, int completionPortThreads)
	{
		if (!UseWindowsThreadPool)
		{
			return PortableThreadPool.ThreadPoolInstance.SetMinThreads(workerThreads, completionPortThreads);
		}
		return WindowsThreadPool.SetMinThreads(workerThreads, completionPortThreads);
	}

	public static void GetMinThreads(out int workerThreads, out int completionPortThreads)
	{
		if (UseWindowsThreadPool)
		{
			WindowsThreadPool.GetMinThreads(out workerThreads, out completionPortThreads);
		}
		else
		{
			PortableThreadPool.ThreadPoolInstance.GetMinThreads(out workerThreads, out completionPortThreads);
		}
	}

	public static void GetAvailableThreads(out int workerThreads, out int completionPortThreads)
	{
		if (UseWindowsThreadPool)
		{
			WindowsThreadPool.GetAvailableThreads(out workerThreads, out completionPortThreads);
		}
		else
		{
			PortableThreadPool.ThreadPoolInstance.GetAvailableThreads(out workerThreads, out completionPortThreads);
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static void NotifyWorkItemProgress()
	{
		if (UseWindowsThreadPool)
		{
			WindowsThreadPool.NotifyWorkItemProgress();
		}
		else
		{
			PortableThreadPool.ThreadPoolInstance.NotifyWorkItemProgress();
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static bool NotifyWorkItemComplete(object threadLocalCompletionCountObject, int currentTimeMs)
	{
		if (!UseWindowsThreadPool)
		{
			return PortableThreadPool.ThreadPoolInstance.NotifyWorkItemComplete(threadLocalCompletionCountObject, currentTimeMs);
		}
		return WindowsThreadPool.NotifyWorkItemComplete(threadLocalCompletionCountObject, currentTimeMs);
	}

	internal static bool NotifyThreadBlocked()
	{
		if (!UseWindowsThreadPool)
		{
			return PortableThreadPool.ThreadPoolInstance.NotifyThreadBlocked();
		}
		return false;
	}

	internal static void NotifyThreadUnblocked()
	{
		if (UseWindowsThreadPool)
		{
			WindowsThreadPool.NotifyThreadUnblocked();
		}
		else
		{
			PortableThreadPool.ThreadPoolInstance.NotifyThreadUnblocked();
		}
	}

	internal static void RequestWorkerThread()
	{
		if (UseWindowsThreadPool)
		{
			WindowsThreadPool.RequestWorkerThread();
		}
		else
		{
			PortableThreadPool.ThreadPoolInstance.RequestWorker();
		}
	}

	internal static void ReportThreadStatus(bool isWorking)
	{
		PortableThreadPool.ThreadPoolInstance.ReportThreadStatus(isWorking);
	}

	private static RegisteredWaitHandle RegisterWaitForSingleObject(WaitHandle waitObject, WaitOrTimerCallback callBack, object state, uint millisecondsTimeOutInterval, bool executeOnlyOnce, bool flowExecutionContext)
	{
		if (UseWindowsThreadPool)
		{
			return WindowsThreadPool.RegisterWaitForSingleObject(waitObject, callBack, state, millisecondsTimeOutInterval, executeOnlyOnce, flowExecutionContext);
		}
		return PortableThreadPool.RegisterWaitForSingleObject(waitObject, callBack, state, millisecondsTimeOutInterval, executeOnlyOnce, flowExecutionContext);
	}

	[SupportedOSPlatform("windows")]
	private unsafe static bool UnsafeQueueNativeOverlappedPortableCore(NativeOverlapped* overlapped)
	{
		if (overlapped == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.overlapped);
		}
		overlapped->InternalLow = IntPtr.Zero;
		PortableThreadPool.ThreadPoolInstance.QueueNativeOverlapped(overlapped);
		return true;
	}

	[Obsolete("ThreadPool.BindHandle(IntPtr) has been deprecated. Use ThreadPool.BindHandle(SafeHandle) instead.")]
	[SupportedOSPlatform("windows")]
	private static bool BindHandlePortableCore(nint osHandle)
	{
		PortableThreadPool.ThreadPoolInstance.RegisterForIOCompletionNotifications(osHandle);
		return true;
	}

	[SupportedOSPlatform("windows")]
	private static bool BindHandlePortableCore(SafeHandle osHandle)
	{
		ArgumentNullException.ThrowIfNull(osHandle, "osHandle");
		bool success = false;
		try
		{
			osHandle.DangerousAddRef(ref success);
			PortableThreadPool.ThreadPoolInstance.RegisterForIOCompletionNotifications(osHandle.DangerousGetHandle());
			return true;
		}
		finally
		{
			if (success)
			{
				osHandle.DangerousRelease();
			}
		}
	}
}
