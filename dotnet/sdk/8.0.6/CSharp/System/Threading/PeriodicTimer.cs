using System.Runtime.ExceptionServices;
using System.Threading.Tasks;
using System.Threading.Tasks.Sources;

namespace System.Threading;

public sealed class PeriodicTimer : IDisposable
{
	private sealed class State : IValueTaskSource<bool>
	{
		private PeriodicTimer _owner;

		private ManualResetValueTaskSourceCore<bool> _mrvtsc;

		private CancellationTokenRegistration _ctr;

		private bool _stopped;

		private bool _signaled;

		private bool _activeWait;

		public ValueTask<bool> WaitForNextTickAsync(PeriodicTimer owner, CancellationToken cancellationToken)
		{
			lock (this)
			{
				if (_activeWait)
				{
					ThrowHelper.ThrowInvalidOperationException();
				}
				if (cancellationToken.IsCancellationRequested)
				{
					return ValueTask.FromCanceled<bool>(cancellationToken);
				}
				if (_signaled)
				{
					if (!_stopped)
					{
						_signaled = false;
					}
					return new ValueTask<bool>(!_stopped);
				}
				_owner = owner;
				_activeWait = true;
				_ctr = cancellationToken.UnsafeRegister(delegate(object state, CancellationToken cancellationToken)
				{
					((State)state).Signal(stopping: false, cancellationToken);
				}, this);
				return new ValueTask<bool>(this, _mrvtsc.Version);
			}
		}

		public void Signal(bool stopping = false, CancellationToken cancellationToken = default(CancellationToken))
		{
			bool flag = false;
			lock (this)
			{
				_stopped |= stopping;
				if (!_signaled)
				{
					_signaled = true;
					flag = _activeWait;
				}
			}
			if (flag)
			{
				if (cancellationToken.IsCancellationRequested)
				{
					_mrvtsc.SetException(ExceptionDispatchInfo.SetCurrentStackTrace(new OperationCanceledException(cancellationToken)));
				}
				else
				{
					_mrvtsc.SetResult(result: true);
				}
			}
		}

		bool IValueTaskSource<bool>.GetResult(short token)
		{
			_ctr.Dispose();
			lock (this)
			{
				try
				{
					_mrvtsc.GetResult(token);
				}
				finally
				{
					_mrvtsc.Reset();
					_ctr = default(CancellationTokenRegistration);
					_activeWait = false;
					_owner = null;
					if (!_stopped)
					{
						_signaled = false;
					}
				}
				return !_stopped;
			}
		}

		ValueTaskSourceStatus IValueTaskSource<bool>.GetStatus(short token)
		{
			return _mrvtsc.GetStatus(token);
		}

		void IValueTaskSource<bool>.OnCompleted(Action<object> continuation, object state, short token, ValueTaskSourceOnCompletedFlags flags)
		{
			_mrvtsc.OnCompleted(continuation, state, token, flags);
		}
	}

	private readonly ITimer _timer;

	private readonly State _state;

	private TimeSpan _period;

	public TimeSpan Period
	{
		get
		{
			return _period;
		}
		set
		{
			if (!TryGetMilliseconds(value, out var _))
			{
				throw new ArgumentOutOfRangeException("value");
			}
			_period = value;
			if (!_timer.Change(value, value))
			{
				ThrowHelper.ThrowObjectDisposedException(this);
			}
		}
	}

	public PeriodicTimer(TimeSpan period)
	{
		if (!TryGetMilliseconds(period, out var milliseconds))
		{
			GC.SuppressFinalize(this);
			throw new ArgumentOutOfRangeException("period");
		}
		_period = period;
		_state = new State();
		_timer = new TimerQueueTimer(delegate(object s)
		{
			((State)s).Signal();
		}, _state, milliseconds, milliseconds, flowExecutionContext: false);
	}

	public PeriodicTimer(TimeSpan period, TimeProvider timeProvider)
	{
		if (!TryGetMilliseconds(period, out var milliseconds))
		{
			GC.SuppressFinalize(this);
			throw new ArgumentOutOfRangeException("period");
		}
		if (timeProvider == null)
		{
			GC.SuppressFinalize(this);
			throw new ArgumentNullException("timeProvider");
		}
		_period = period;
		_state = new State();
		TimerCallback timerCallback = delegate(object s)
		{
			((State)s).Signal();
		};
		if (timeProvider == TimeProvider.System)
		{
			_timer = new TimerQueueTimer(timerCallback, _state, milliseconds, milliseconds, flowExecutionContext: false);
			return;
		}
		using (ExecutionContext.SuppressFlow())
		{
			_timer = timeProvider.CreateTimer(timerCallback, _state, period, period);
		}
	}

	private static bool TryGetMilliseconds(TimeSpan value, out uint milliseconds)
	{
		long num = (long)value.TotalMilliseconds;
		if ((num >= 1 && num <= 4294967294u) || value == Timeout.InfiniteTimeSpan)
		{
			milliseconds = (uint)num;
			return true;
		}
		milliseconds = 0u;
		return false;
	}

	public ValueTask<bool> WaitForNextTickAsync(CancellationToken cancellationToken = default(CancellationToken))
	{
		return _state.WaitForNextTickAsync(this, cancellationToken);
	}

	public void Dispose()
	{
		GC.SuppressFinalize(this);
		_timer.Dispose();
		_state.Signal(stopping: true);
	}

	~PeriodicTimer()
	{
		Dispose();
	}
}
