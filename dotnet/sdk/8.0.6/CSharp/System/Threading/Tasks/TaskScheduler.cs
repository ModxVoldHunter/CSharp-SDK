using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace System.Threading.Tasks;

[DebuggerDisplay("Id = {Id}")]
[DebuggerTypeProxy(typeof(SystemThreadingTasks_TaskSchedulerDebugView))]
public abstract class TaskScheduler
{
	internal sealed class SystemThreadingTasks_TaskSchedulerDebugView
	{
		private readonly TaskScheduler m_taskScheduler;

		public int Id => m_taskScheduler.Id;

		public IEnumerable<Task> ScheduledTasks => m_taskScheduler.GetScheduledTasks();

		public SystemThreadingTasks_TaskSchedulerDebugView(TaskScheduler scheduler)
		{
			m_taskScheduler = scheduler;
		}
	}

	private static ConditionalWeakTable<TaskScheduler, object> s_activeTaskSchedulers;

	private static readonly TaskScheduler s_defaultTaskScheduler = new ThreadPoolTaskScheduler();

	internal static int s_taskSchedulerIdCounter;

	private volatile int m_taskSchedulerId;

	public virtual int MaximumConcurrencyLevel => int.MaxValue;

	public static TaskScheduler Default => s_defaultTaskScheduler;

	public static TaskScheduler Current => InternalCurrent ?? Default;

	internal static TaskScheduler? InternalCurrent
	{
		get
		{
			Task internalCurrent = Task.InternalCurrent;
			if (internalCurrent == null || (internalCurrent.CreationOptions & TaskCreationOptions.HideScheduler) != 0)
			{
				return null;
			}
			return internalCurrent.ExecutingTaskScheduler;
		}
	}

	public int Id
	{
		get
		{
			int taskSchedulerId = m_taskSchedulerId;
			if (taskSchedulerId == 0)
			{
				int num;
				do
				{
					num = Interlocked.Increment(ref s_taskSchedulerIdCounter);
				}
				while (num == 0);
				Interlocked.CompareExchange(ref m_taskSchedulerId, num, 0);
				taskSchedulerId = m_taskSchedulerId;
			}
			return taskSchedulerId;
		}
	}

	public static event EventHandler<UnobservedTaskExceptionEventArgs>? UnobservedTaskException;

	protected internal abstract void QueueTask(Task task);

	protected abstract bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued);

	protected abstract IEnumerable<Task>? GetScheduledTasks();

	internal bool TryRunInline(Task task, bool taskWasPreviouslyQueued)
	{
		TaskScheduler executingTaskScheduler = task.ExecutingTaskScheduler;
		if (executingTaskScheduler != this && executingTaskScheduler != null)
		{
			return executingTaskScheduler.TryRunInline(task, taskWasPreviouslyQueued);
		}
		if (executingTaskScheduler == null || (object)task.m_action == null || task.IsDelegateInvoked || task.IsCanceled || !RuntimeHelpers.TryEnsureSufficientExecutionStack())
		{
			return false;
		}
		if (TplEventSource.Log.IsEnabled())
		{
			task.FireTaskScheduledIfNeeded(this);
		}
		bool flag = TryExecuteTaskInline(task, taskWasPreviouslyQueued);
		if (flag && !task.IsDelegateInvoked && !task.IsCanceled)
		{
			throw new InvalidOperationException(SR.TaskScheduler_InconsistentStateAfterTryExecuteTaskInline);
		}
		return flag;
	}

	protected internal virtual bool TryDequeue(Task task)
	{
		return false;
	}

	internal virtual void NotifyWorkItemProgress()
	{
	}

	internal void InternalQueueTask(Task task)
	{
		if (TplEventSource.Log.IsEnabled())
		{
			task.FireTaskScheduledIfNeeded(this);
		}
		QueueTask(task);
	}

	protected TaskScheduler()
	{
		if (Debugger.IsAttached)
		{
			AddToActiveTaskSchedulers();
		}
	}

	private void AddToActiveTaskSchedulers()
	{
		ConditionalWeakTable<TaskScheduler, object> conditionalWeakTable = s_activeTaskSchedulers;
		if (conditionalWeakTable == null)
		{
			Interlocked.CompareExchange(ref s_activeTaskSchedulers, new ConditionalWeakTable<TaskScheduler, object>(), null);
			conditionalWeakTable = s_activeTaskSchedulers;
		}
		conditionalWeakTable.Add(this, null);
	}

	public static TaskScheduler FromCurrentSynchronizationContext()
	{
		return new SynchronizationContextTaskScheduler();
	}

	protected bool TryExecuteTask(Task task)
	{
		if (task.ExecutingTaskScheduler != this)
		{
			throw new InvalidOperationException(SR.TaskScheduler_ExecuteTask_WrongTaskScheduler);
		}
		return task.ExecuteEntry();
	}

	internal static void PublishUnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs ueea)
	{
		TaskScheduler.UnobservedTaskException?.Invoke(sender, ueea);
	}

	internal Task[] GetScheduledTasksForDebugger()
	{
		IEnumerable<Task> scheduledTasks = GetScheduledTasks();
		if (scheduledTasks == null)
		{
			return null;
		}
		Task[] array = scheduledTasks as Task[];
		if (array == null)
		{
			array = new List<Task>(scheduledTasks).ToArray();
		}
		Task[] array2 = array;
		foreach (Task task in array2)
		{
			_ = task.Id;
		}
		return array;
	}

	internal static TaskScheduler[] GetTaskSchedulersForDebugger()
	{
		if (s_activeTaskSchedulers == null)
		{
			return new TaskScheduler[1] { s_defaultTaskScheduler };
		}
		List<TaskScheduler> list = new List<TaskScheduler>();
		foreach (KeyValuePair<TaskScheduler, object> item in (IEnumerable<KeyValuePair<TaskScheduler, object>>)s_activeTaskSchedulers)
		{
			list.Add(item.Key);
		}
		if (!list.Contains(s_defaultTaskScheduler))
		{
			list.Add(s_defaultTaskScheduler);
		}
		TaskScheduler[] array = list.ToArray();
		TaskScheduler[] array2 = array;
		foreach (TaskScheduler taskScheduler in array2)
		{
			_ = taskScheduler.Id;
		}
		return array;
	}
}
