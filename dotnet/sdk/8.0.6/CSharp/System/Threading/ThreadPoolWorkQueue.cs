using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Tracing;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Internal;

namespace System.Threading;

internal sealed class ThreadPoolWorkQueue
{
	internal static class WorkStealingQueueList
	{
		private static volatile WorkStealingQueue[] _queues = new WorkStealingQueue[0];

		public static WorkStealingQueue[] Queues => _queues;

		public static void Add(WorkStealingQueue queue)
		{
			WorkStealingQueue[] queues;
			WorkStealingQueue[] array;
			do
			{
				queues = _queues;
				array = new WorkStealingQueue[queues.Length + 1];
				Array.Copy(queues, array, queues.Length);
				array[^1] = queue;
			}
			while (Interlocked.CompareExchange(ref _queues, array, queues) != queues);
		}

		public static void Remove(WorkStealingQueue queue)
		{
			WorkStealingQueue[] queues;
			WorkStealingQueue[] array;
			do
			{
				queues = _queues;
				if (queues.Length == 0)
				{
					break;
				}
				int num = Array.IndexOf(queues, queue);
				if (num < 0)
				{
					break;
				}
				array = new WorkStealingQueue[queues.Length - 1];
				if (num == 0)
				{
					Array.Copy(queues, 1, array, 0, array.Length);
					continue;
				}
				if (num == queues.Length - 1)
				{
					Array.Copy(queues, array, array.Length);
					continue;
				}
				Array.Copy(queues, array, num);
				Array.Copy(queues, num + 1, array, num, array.Length - num);
			}
			while (Interlocked.CompareExchange(ref _queues, array, queues) != queues);
		}
	}

	internal sealed class WorkStealingQueue
	{
		internal volatile object[] m_array = new object[32];

		private volatile int m_mask = 31;

		private volatile int m_headIndex;

		private volatile int m_tailIndex;

		private SpinLock m_foreignLock = new SpinLock(enableThreadOwnerTracking: false);

		public bool CanSteal => m_headIndex < m_tailIndex;

		public int Count
		{
			get
			{
				bool lockTaken = false;
				try
				{
					m_foreignLock.Enter(ref lockTaken);
					return Math.Max(0, m_tailIndex - m_headIndex);
				}
				finally
				{
					if (lockTaken)
					{
						m_foreignLock.Exit(useMemoryBarrier: false);
					}
				}
			}
		}

		public void LocalPush(object obj)
		{
			int num = m_tailIndex;
			if (num == int.MaxValue)
			{
				num = LocalPush_HandleTailOverflow();
			}
			if (num < m_headIndex + m_mask)
			{
				Volatile.Write(ref m_array[num & m_mask], obj);
				m_tailIndex = num + 1;
				return;
			}
			bool lockTaken = false;
			try
			{
				m_foreignLock.Enter(ref lockTaken);
				int headIndex = m_headIndex;
				int num2 = m_tailIndex - m_headIndex;
				if (num2 >= m_mask)
				{
					object[] array = new object[m_array.Length << 1];
					for (int i = 0; i < m_array.Length; i++)
					{
						array[i] = m_array[(i + headIndex) & m_mask];
					}
					m_array = array;
					m_headIndex = 0;
					num = (m_tailIndex = num2);
					m_mask = (m_mask << 1) | 1;
				}
				Volatile.Write(ref m_array[num & m_mask], obj);
				m_tailIndex = num + 1;
			}
			finally
			{
				if (lockTaken)
				{
					m_foreignLock.Exit(useMemoryBarrier: false);
				}
			}
		}

		[MethodImpl(MethodImplOptions.NoInlining)]
		private int LocalPush_HandleTailOverflow()
		{
			bool lockTaken = false;
			try
			{
				m_foreignLock.Enter(ref lockTaken);
				int num = m_tailIndex;
				if (num == int.MaxValue)
				{
					m_headIndex &= m_mask;
					num = (m_tailIndex &= m_mask);
				}
				return num;
			}
			finally
			{
				if (lockTaken)
				{
					m_foreignLock.Exit(useMemoryBarrier: true);
				}
			}
		}

		public bool LocalFindAndPop(object obj)
		{
			if (m_array[(m_tailIndex - 1) & m_mask] == obj)
			{
				object obj2 = LocalPop();
				return obj2 != null;
			}
			for (int num = m_tailIndex - 2; num >= m_headIndex; num--)
			{
				if (m_array[num & m_mask] == obj)
				{
					bool lockTaken = false;
					try
					{
						m_foreignLock.Enter(ref lockTaken);
						if (m_array[num & m_mask] == null)
						{
							return false;
						}
						Volatile.Write(ref m_array[num & m_mask], null);
						if (num == m_tailIndex)
						{
							m_tailIndex--;
						}
						else if (num == m_headIndex)
						{
							m_headIndex++;
						}
						return true;
					}
					finally
					{
						if (lockTaken)
						{
							m_foreignLock.Exit(useMemoryBarrier: false);
						}
					}
				}
			}
			return false;
		}

		public object LocalPop()
		{
			if (m_headIndex >= m_tailIndex)
			{
				return null;
			}
			return LocalPopCore();
		}

		private object LocalPopCore()
		{
			int num;
			object obj;
			while (true)
			{
				int tailIndex = m_tailIndex;
				if (m_headIndex >= tailIndex)
				{
					return null;
				}
				tailIndex--;
				Interlocked.Exchange(ref m_tailIndex, tailIndex);
				if (m_headIndex <= tailIndex)
				{
					num = tailIndex & m_mask;
					obj = Volatile.Read(ref m_array[num]);
					if (obj != null)
					{
						break;
					}
					continue;
				}
				bool lockTaken = false;
				try
				{
					m_foreignLock.Enter(ref lockTaken);
					if (m_headIndex <= tailIndex)
					{
						int num2 = tailIndex & m_mask;
						object obj2 = Volatile.Read(ref m_array[num2]);
						if (obj2 != null)
						{
							m_array[num2] = null;
							return obj2;
						}
						continue;
					}
					m_tailIndex = tailIndex + 1;
					return null;
				}
				finally
				{
					if (lockTaken)
					{
						m_foreignLock.Exit(useMemoryBarrier: false);
					}
				}
			}
			m_array[num] = null;
			return obj;
		}

		public object TrySteal(ref bool missedSteal)
		{
			while (CanSteal)
			{
				bool lockTaken = false;
				try
				{
					m_foreignLock.TryEnter(ref lockTaken);
					if (lockTaken)
					{
						int headIndex = m_headIndex;
						Interlocked.Exchange(ref m_headIndex, headIndex + 1);
						if (headIndex < m_tailIndex)
						{
							int num = headIndex & m_mask;
							object obj = Volatile.Read(ref m_array[num]);
							if (obj == null)
							{
								continue;
							}
							m_array[num] = null;
							return obj;
						}
						m_headIndex = headIndex;
					}
				}
				finally
				{
					if (lockTaken)
					{
						m_foreignLock.Exit(useMemoryBarrier: false);
					}
				}
				missedSteal = true;
				break;
			}
			return null;
		}
	}

	private struct CacheLineSeparated
	{
		private readonly PaddingFor32 pad1;

		public int hasOutstandingThreadRequest;

		private readonly PaddingFor32 pad2;
	}

	private static readonly int s_assignableWorkItemQueueCount = ((Environment.ProcessorCount > 32) ? ((Environment.ProcessorCount + 15) / 16) : 0);

	private bool _loggingEnabled;

	private bool _dispatchNormalPriorityWorkFirst;

	private int _mayHaveHighPriorityWorkItems;

	internal readonly ConcurrentQueue<object> workItems = new ConcurrentQueue<object>();

	internal readonly ConcurrentQueue<object> highPriorityWorkItems = new ConcurrentQueue<object>();

	internal readonly ConcurrentQueue<object>[] _assignableWorkItemQueues = new ConcurrentQueue<object>[s_assignableWorkItemQueueCount];

	private readonly LowLevelLock _queueAssignmentLock = new LowLevelLock();

	private readonly int[] _assignedWorkItemQueueThreadCounts = ((s_assignableWorkItemQueueCount > 0) ? new int[s_assignableWorkItemQueueCount] : Array.Empty<int>());

	private CacheLineSeparated _separated;

	public static long LocalCount
	{
		get
		{
			long num = 0L;
			WorkStealingQueue[] queues = WorkStealingQueueList.Queues;
			foreach (WorkStealingQueue workStealingQueue in queues)
			{
				num += workStealingQueue.Count;
			}
			return num;
		}
	}

	public long GlobalCount
	{
		get
		{
			long num = (long)highPriorityWorkItems.Count + (long)workItems.Count;
			for (int i = 0; i < s_assignableWorkItemQueueCount; i++)
			{
				num += _assignableWorkItemQueues[i].Count;
			}
			return num;
		}
	}

	public ThreadPoolWorkQueue()
	{
		for (int i = 0; i < s_assignableWorkItemQueueCount; i++)
		{
			_assignableWorkItemQueues[i] = new ConcurrentQueue<object>();
		}
		RefreshLoggingEnabled();
	}

	private void AssignWorkItemQueue(ThreadPoolWorkQueueThreadLocals tl)
	{
		_queueAssignmentLock.Acquire();
		int num = -1;
		int num2 = int.MaxValue;
		int num3 = 0;
		for (int i = 0; i < s_assignableWorkItemQueueCount; i++)
		{
			int num4 = _assignedWorkItemQueueThreadCounts[i];
			if (num4 < 16)
			{
				num = i;
				_assignedWorkItemQueueThreadCounts[num] = num4 + 1;
				break;
			}
			if (num4 < num2)
			{
				num2 = num4;
				num3 = i;
			}
		}
		if (num < 0)
		{
			num = num3;
			_assignedWorkItemQueueThreadCounts[num]++;
		}
		_queueAssignmentLock.Release();
		tl.queueIndex = num;
		tl.assignedGlobalWorkItemQueue = _assignableWorkItemQueues[num];
	}

	private void TryReassignWorkItemQueue(ThreadPoolWorkQueueThreadLocals tl)
	{
		int num = tl.queueIndex;
		if (num == 0 || !_queueAssignmentLock.TryAcquire())
		{
			return;
		}
		if (_assignedWorkItemQueueThreadCounts[num] > 1)
		{
			for (int i = 0; i < num; i++)
			{
				if (_assignedWorkItemQueueThreadCounts[i] < 16)
				{
					_assignedWorkItemQueueThreadCounts[num]--;
					num = i;
					_assignedWorkItemQueueThreadCounts[num]++;
					break;
				}
			}
		}
		_queueAssignmentLock.Release();
		tl.queueIndex = num;
		tl.assignedGlobalWorkItemQueue = _assignableWorkItemQueues[num];
	}

	private void UnassignWorkItemQueue(ThreadPoolWorkQueueThreadLocals tl)
	{
		int queueIndex = tl.queueIndex;
		_queueAssignmentLock.Acquire();
		int num = --_assignedWorkItemQueueThreadCounts[queueIndex];
		_queueAssignmentLock.Release();
		if (num <= 0)
		{
			bool flag = false;
			ConcurrentQueue<object> assignedGlobalWorkItemQueue = tl.assignedGlobalWorkItemQueue;
			object result;
			while (_assignedWorkItemQueueThreadCounts[queueIndex] <= 0 && assignedGlobalWorkItemQueue.TryDequeue(out result))
			{
				workItems.Enqueue(result);
				flag = true;
			}
			if (flag)
			{
				EnsureThreadRequested();
			}
		}
	}

	public ThreadPoolWorkQueueThreadLocals GetOrCreateThreadLocals()
	{
		return ThreadPoolWorkQueueThreadLocals.threadLocals ?? CreateThreadLocals();
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private ThreadPoolWorkQueueThreadLocals CreateThreadLocals()
	{
		return ThreadPoolWorkQueueThreadLocals.threadLocals = new ThreadPoolWorkQueueThreadLocals(this);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void RefreshLoggingEnabled()
	{
		if (!FrameworkEventSource.Log.IsEnabled())
		{
			if (_loggingEnabled)
			{
				_loggingEnabled = false;
			}
		}
		else
		{
			RefreshLoggingEnabledFull();
		}
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	public void RefreshLoggingEnabledFull()
	{
		_loggingEnabled = FrameworkEventSource.Log.IsEnabled(EventLevel.Verbose, (EventKeywords)18L);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal void EnsureThreadRequested()
	{
		if (Interlocked.CompareExchange(ref _separated.hasOutstandingThreadRequest, 1, 0) == 0)
		{
			ThreadPool.RequestWorkerThread();
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal void MarkThreadRequestSatisfied()
	{
		_separated.hasOutstandingThreadRequest = 0;
		Interlocked.MemoryBarrier();
	}

	public void Enqueue(object callback, bool forceGlobal)
	{
		if (_loggingEnabled && FrameworkEventSource.Log.IsEnabled())
		{
			FrameworkEventSource.Log.ThreadPoolEnqueueWorkObject(callback);
		}
		ThreadPoolWorkQueueThreadLocals threadLocals;
		if (!forceGlobal && (threadLocals = ThreadPoolWorkQueueThreadLocals.threadLocals) != null)
		{
			threadLocals.workStealingQueue.LocalPush(callback);
		}
		else
		{
			ConcurrentQueue<object> concurrentQueue = ((s_assignableWorkItemQueueCount > 0 && (threadLocals = ThreadPoolWorkQueueThreadLocals.threadLocals) != null) ? threadLocals.assignedGlobalWorkItemQueue : workItems);
			concurrentQueue.Enqueue(callback);
		}
		EnsureThreadRequested();
	}

	public void EnqueueAtHighPriority(object workItem)
	{
		if (_loggingEnabled && FrameworkEventSource.Log.IsEnabled())
		{
			FrameworkEventSource.Log.ThreadPoolEnqueueWorkObject(workItem);
		}
		highPriorityWorkItems.Enqueue(workItem);
		Volatile.Write(ref _mayHaveHighPriorityWorkItems, 1);
		EnsureThreadRequested();
	}

	internal static bool LocalFindAndPop(object callback)
	{
		return ThreadPoolWorkQueueThreadLocals.threadLocals?.workStealingQueue.LocalFindAndPop(callback) ?? false;
	}

	public object Dequeue(ThreadPoolWorkQueueThreadLocals tl, ref bool missedSteal)
	{
		object workItem = tl.workStealingQueue.LocalPop();
		if (workItem != null)
		{
			return workItem;
		}
		if (tl.isProcessingHighPriorityWorkItems)
		{
			if (highPriorityWorkItems.TryDequeue(out workItem))
			{
				return workItem;
			}
			tl.isProcessingHighPriorityWorkItems = false;
		}
		else if (_mayHaveHighPriorityWorkItems != 0 && Interlocked.CompareExchange(ref _mayHaveHighPriorityWorkItems, 0, 1) != 0 && TryStartProcessingHighPriorityWorkItemsAndDequeue(tl, out workItem))
		{
			return workItem;
		}
		if (s_assignableWorkItemQueueCount > 0 && tl.assignedGlobalWorkItemQueue.TryDequeue(out workItem))
		{
			return workItem;
		}
		if (workItems.TryDequeue(out workItem))
		{
			return workItem;
		}
		uint num = tl.random.NextUInt32();
		if (s_assignableWorkItemQueueCount > 0)
		{
			int queueIndex = tl.queueIndex;
			int num2 = s_assignableWorkItemQueueCount;
			int num3 = num2 - 1;
			int num4 = (int)(num % (uint)num2);
			while (num2 > 0)
			{
				if (num4 != queueIndex && _assignableWorkItemQueues[num4].TryDequeue(out workItem))
				{
					return workItem;
				}
				num4 = ((num4 < num3) ? (num4 + 1) : 0);
				num2--;
			}
		}
		WorkStealingQueue workStealingQueue = tl.workStealingQueue;
		WorkStealingQueue[] queues = WorkStealingQueueList.Queues;
		int num5 = queues.Length;
		int num6 = num5 - 1;
		int num7 = (int)(num % (uint)num5);
		while (num5 > 0)
		{
			WorkStealingQueue workStealingQueue2 = queues[num7];
			if (workStealingQueue2 != workStealingQueue && workStealingQueue2.CanSteal)
			{
				workItem = workStealingQueue2.TrySteal(ref missedSteal);
				if (workItem != null)
				{
					return workItem;
				}
			}
			num7 = ((num7 < num6) ? (num7 + 1) : 0);
			num5--;
		}
		return null;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private bool TryStartProcessingHighPriorityWorkItemsAndDequeue(ThreadPoolWorkQueueThreadLocals tl, [MaybeNullWhen(false)] out object workItem)
	{
		if (!highPriorityWorkItems.TryDequeue(out workItem))
		{
			return false;
		}
		tl.isProcessingHighPriorityWorkItems = true;
		_mayHaveHighPriorityWorkItems = 1;
		return true;
	}

	internal static bool Dispatch()
	{
		ThreadPoolWorkQueue s_workQueue = ThreadPool.s_workQueue;
		ThreadPoolWorkQueueThreadLocals orCreateThreadLocals = s_workQueue.GetOrCreateThreadLocals();
		if (s_assignableWorkItemQueueCount > 0)
		{
			s_workQueue.AssignWorkItemQueue(orCreateThreadLocals);
		}
		s_workQueue.MarkThreadRequestSatisfied();
		object result = null;
		bool dispatchNormalPriorityWorkFirst = s_workQueue._dispatchNormalPriorityWorkFirst;
		if (dispatchNormalPriorityWorkFirst && !orCreateThreadLocals.workStealingQueue.CanSteal)
		{
			s_workQueue._dispatchNormalPriorityWorkFirst = !dispatchNormalPriorityWorkFirst;
			ConcurrentQueue<object> concurrentQueue = ((s_assignableWorkItemQueueCount > 0) ? orCreateThreadLocals.assignedGlobalWorkItemQueue : s_workQueue.workItems);
			if (!concurrentQueue.TryDequeue(out result) && s_assignableWorkItemQueueCount > 0)
			{
				s_workQueue.workItems.TryDequeue(out result);
			}
		}
		if (result == null)
		{
			bool missedSteal = false;
			result = s_workQueue.Dequeue(orCreateThreadLocals, ref missedSteal);
			if (result == null)
			{
				if (s_assignableWorkItemQueueCount > 0)
				{
					s_workQueue.UnassignWorkItemQueue(orCreateThreadLocals);
				}
				if (missedSteal)
				{
					s_workQueue.EnsureThreadRequested();
				}
				return true;
			}
		}
		s_workQueue.EnsureThreadRequested();
		s_workQueue.RefreshLoggingEnabled();
		object threadLocalCompletionCountObject = orCreateThreadLocals.threadLocalCompletionCountObject;
		Thread currentThread = orCreateThreadLocals.currentThread;
		currentThread._executionContext = null;
		currentThread._synchronizationContext = null;
		int num = Environment.TickCount;
		while (true)
		{
			if (result == null)
			{
				bool missedSteal2 = false;
				result = s_workQueue.Dequeue(orCreateThreadLocals, ref missedSteal2);
				if (result == null)
				{
					if (s_assignableWorkItemQueueCount > 0)
					{
						s_workQueue.UnassignWorkItemQueue(orCreateThreadLocals);
					}
					if (missedSteal2)
					{
						s_workQueue.EnsureThreadRequested();
					}
					return true;
				}
			}
			if (s_workQueue._loggingEnabled && FrameworkEventSource.Log.IsEnabled())
			{
				FrameworkEventSource.Log.ThreadPoolDequeueWorkObject(result);
			}
			if (ThreadPool.EnableWorkerTracking)
			{
				DispatchWorkItemWithWorkerTracking(result, currentThread);
			}
			else
			{
				DispatchWorkItem(result, currentThread);
			}
			result = null;
			ExecutionContext.ResetThreadPoolThread(currentThread);
			currentThread.ResetThreadPoolThread();
			int tickCount = Environment.TickCount;
			if (!ThreadPool.NotifyWorkItemComplete(threadLocalCompletionCountObject, tickCount))
			{
				orCreateThreadLocals.TransferLocalWork();
				orCreateThreadLocals.isProcessingHighPriorityWorkItems = false;
				if (s_assignableWorkItemQueueCount > 0)
				{
					s_workQueue.UnassignWorkItemQueue(orCreateThreadLocals);
				}
				return false;
			}
			if ((uint)(tickCount - num) >= 30u)
			{
				if (ThreadPool.YieldFromDispatchLoop)
				{
					break;
				}
				if (s_assignableWorkItemQueueCount > 0)
				{
					s_workQueue.TryReassignWorkItemQueue(orCreateThreadLocals);
				}
				num = tickCount;
				s_workQueue.RefreshLoggingEnabled();
			}
		}
		orCreateThreadLocals.isProcessingHighPriorityWorkItems = false;
		if (s_assignableWorkItemQueueCount > 0)
		{
			s_workQueue.UnassignWorkItemQueue(orCreateThreadLocals);
		}
		return true;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private static void DispatchWorkItemWithWorkerTracking(object workItem, Thread currentThread)
	{
		bool flag = false;
		try
		{
			ThreadPool.ReportThreadStatus(isWorking: true);
			flag = true;
			DispatchWorkItem(workItem, currentThread);
		}
		finally
		{
			if (flag)
			{
				ThreadPool.ReportThreadStatus(isWorking: false);
			}
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static void DispatchWorkItem(object workItem, Thread currentThread)
	{
		if (workItem is Task task)
		{
			task.ExecuteFromThreadPool(currentThread);
		}
		else
		{
			Unsafe.As<IThreadPoolWorkItem>(workItem).Execute();
		}
	}
}
