using System.Diagnostics;
using System.Threading.Tasks;

namespace System.Runtime.CompilerServices;

public readonly struct ConfiguredTaskAwaitable
{
	public readonly struct ConfiguredTaskAwaiter : ICriticalNotifyCompletion, INotifyCompletion, IConfiguredTaskAwaiter
	{
		internal readonly Task m_task;

		internal readonly ConfigureAwaitOptions m_options;

		public bool IsCompleted
		{
			get
			{
				if ((m_options & ConfigureAwaitOptions.ForceYielding) == 0)
				{
					return m_task.IsCompleted;
				}
				return false;
			}
		}

		internal ConfiguredTaskAwaiter(Task task, ConfigureAwaitOptions options)
		{
			m_task = task;
			m_options = options;
		}

		public void OnCompleted(Action continuation)
		{
			TaskAwaiter.OnCompletedInternal(m_task, continuation, (m_options & ConfigureAwaitOptions.ContinueOnCapturedContext) != 0, flowExecutionContext: true);
		}

		public void UnsafeOnCompleted(Action continuation)
		{
			TaskAwaiter.OnCompletedInternal(m_task, continuation, (m_options & ConfigureAwaitOptions.ContinueOnCapturedContext) != 0, flowExecutionContext: false);
		}

		[StackTraceHidden]
		public void GetResult()
		{
			TaskAwaiter.ValidateEnd(m_task, m_options);
		}
	}

	private readonly ConfiguredTaskAwaiter m_configuredTaskAwaiter;

	internal ConfiguredTaskAwaitable(Task task, ConfigureAwaitOptions options)
	{
		m_configuredTaskAwaiter = new ConfiguredTaskAwaiter(task, options);
	}

	public ConfiguredTaskAwaiter GetAwaiter()
	{
		return m_configuredTaskAwaiter;
	}
}
public readonly struct ConfiguredTaskAwaitable<TResult>
{
	public readonly struct ConfiguredTaskAwaiter : ICriticalNotifyCompletion, INotifyCompletion, IConfiguredTaskAwaiter
	{
		private readonly Task<TResult> m_task;

		internal readonly ConfigureAwaitOptions m_options;

		public bool IsCompleted
		{
			get
			{
				if ((m_options & ConfigureAwaitOptions.ForceYielding) == 0)
				{
					return m_task.IsCompleted;
				}
				return false;
			}
		}

		internal ConfiguredTaskAwaiter(Task<TResult> task, ConfigureAwaitOptions options)
		{
			m_task = task;
			m_options = options;
		}

		public void OnCompleted(Action continuation)
		{
			TaskAwaiter.OnCompletedInternal(m_task, continuation, (m_options & ConfigureAwaitOptions.ContinueOnCapturedContext) != 0, flowExecutionContext: true);
		}

		public void UnsafeOnCompleted(Action continuation)
		{
			TaskAwaiter.OnCompletedInternal(m_task, continuation, (m_options & ConfigureAwaitOptions.ContinueOnCapturedContext) != 0, flowExecutionContext: false);
		}

		[StackTraceHidden]
		public TResult GetResult()
		{
			TaskAwaiter.ValidateEnd(m_task);
			return m_task.ResultOnSuccess;
		}
	}

	private readonly ConfiguredTaskAwaiter m_configuredTaskAwaiter;

	internal ConfiguredTaskAwaitable(Task<TResult> task, ConfigureAwaitOptions options)
	{
		m_configuredTaskAwaiter = new ConfiguredTaskAwaiter(task, options);
	}

	public ConfiguredTaskAwaiter GetAwaiter()
	{
		return m_configuredTaskAwaiter;
	}
}
