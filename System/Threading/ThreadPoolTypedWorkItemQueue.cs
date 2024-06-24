using System.Collections.Concurrent;

namespace System.Threading;

internal sealed class ThreadPoolTypedWorkItemQueue<T, TCallback> : IThreadPoolWorkItem where T : struct where TCallback : struct, IThreadPoolTypedWorkItemQueueCallback<T>
{
	private int _isScheduledForProcessing;

	private readonly ConcurrentQueue<T> _workItems = new ConcurrentQueue<T>();

	public void BatchEnqueue(T workItem)
	{
		_workItems.Enqueue(workItem);
	}

	public void CompleteBatchEnqueue()
	{
		ScheduleForProcessing();
	}

	private void ScheduleForProcessing()
	{
		if (Interlocked.CompareExchange(ref _isScheduledForProcessing, 1, 0) == 0)
		{
			ThreadPool.UnsafeQueueHighPriorityWorkItemInternal(this);
		}
	}

	void IThreadPoolWorkItem.Execute()
	{
		_isScheduledForProcessing = 0;
		Interlocked.MemoryBarrier();
		if (!_workItems.TryDequeue(out var result))
		{
			return;
		}
		ScheduleForProcessing();
		ThreadPoolWorkQueueThreadLocals threadLocals = ThreadPoolWorkQueueThreadLocals.threadLocals;
		Thread currentThread = threadLocals.currentThread;
		uint num = 0u;
		int tickCount = Environment.TickCount;
		while (true)
		{
			TCallback.Invoke(result);
			if (++num == uint.MaxValue || threadLocals.workStealingQueue.CanSteal || (uint)(Environment.TickCount - tickCount) >= 15u || !_workItems.TryDequeue(out result))
			{
				break;
			}
			ExecutionContext.ResetThreadPoolThread(currentThread);
			currentThread.ResetThreadPoolThread();
		}
		ThreadInt64PersistentCounter.Add(threadLocals.threadLocalCompletionCountObject, num);
	}
}
