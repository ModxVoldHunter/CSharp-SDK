using System.Runtime.ExceptionServices;

namespace System.Threading.Tasks.Sources;

internal static class ManualResetValueTaskSourceCoreShared
{
	internal static readonly Action<object> s_sentinel = CompletionSentinel;

	private static void CompletionSentinel(object _)
	{
		ThrowHelper.ThrowInvalidOperationException();
	}

	internal static void ScheduleCapturedContext(object context, Action<object> continuation, object state)
	{
		if (!(context is SynchronizationContext sc2))
		{
			if (context is TaskScheduler scheduler2)
			{
				ScheduleTaskScheduler(scheduler2, continuation, state);
				return;
			}
			CapturedSchedulerAndExecutionContext capturedSchedulerAndExecutionContext = (CapturedSchedulerAndExecutionContext)context;
			if (capturedSchedulerAndExecutionContext._scheduler is SynchronizationContext sc3)
			{
				ScheduleSynchronizationContext(sc3, continuation, state);
			}
			else
			{
				ScheduleTaskScheduler((TaskScheduler)capturedSchedulerAndExecutionContext._scheduler, continuation, state);
			}
		}
		else
		{
			ScheduleSynchronizationContext(sc2, continuation, state);
		}
		static void ScheduleSynchronizationContext(SynchronizationContext sc, Action<object> continuation, object state)
		{
			sc.Post(continuation.Invoke, state);
		}
		static void ScheduleTaskScheduler(TaskScheduler scheduler, Action<object> continuation, object state)
		{
			Task.Factory.StartNew(continuation, state, CancellationToken.None, TaskCreationOptions.DenyChildAttach, scheduler);
		}
	}

	internal static void InvokeContinuationWithContext(object capturedContext, Action<object> continuation, object continuationState, bool runContinuationsAsynchronously)
	{
		ExecutionContext executionContext = ExecutionContext.CaptureForRestore();
		if (capturedContext is ExecutionContext executionContext2)
		{
			ExecutionContext.RestoreInternal(executionContext2);
			if (runContinuationsAsynchronously)
			{
				try
				{
					ThreadPool.QueueUserWorkItem(continuation, continuationState, preferLocal: true);
					return;
				}
				finally
				{
					ExecutionContext.RestoreInternal(executionContext);
				}
			}
			ExceptionDispatchInfo exceptionDispatchInfo = null;
			SynchronizationContext current = SynchronizationContext.Current;
			try
			{
				continuation(continuationState);
			}
			catch (Exception source)
			{
				exceptionDispatchInfo = ExceptionDispatchInfo.Capture(source);
			}
			finally
			{
				SynchronizationContext.SetSynchronizationContext(current);
				ExecutionContext.RestoreInternal(executionContext);
			}
			exceptionDispatchInfo?.Throw();
			return;
		}
		CapturedSchedulerAndExecutionContext capturedSchedulerAndExecutionContext = (CapturedSchedulerAndExecutionContext)capturedContext;
		ExecutionContext.Restore(capturedSchedulerAndExecutionContext._executionContext);
		try
		{
			ScheduleCapturedContext(capturedContext, continuation, continuationState);
		}
		finally
		{
			ExecutionContext.RestoreInternal(executionContext);
		}
	}
}
