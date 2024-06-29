using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Sources;

namespace System.Net.Quic;

internal sealed class ResettableValueTaskSource : IValueTaskSource
{
	private enum State
	{
		None,
		Awaiting,
		Ready,
		Completed
	}

	private State _state;

	private ManualResetValueTaskSourceCore<bool> _valueTaskSource;

	private CancellationTokenRegistration _cancellationRegistration;

	private Action<object> _cancellationAction;

	private GCHandle _keepAlive;

	private readonly TaskCompletionSource _finalTaskSource;

	public Action<object> CancellationAction
	{
		init
		{
			_cancellationAction = value;
		}
	}

	public bool IsCompleted => Volatile.Read(ref Unsafe.As<State, byte>(ref _state)) == 3;

	public ResettableValueTaskSource(bool runContinuationsAsynchronously = true)
	{
		_state = State.None;
		_valueTaskSource = new ManualResetValueTaskSourceCore<bool>
		{
			RunContinuationsAsynchronously = runContinuationsAsynchronously
		};
		_cancellationRegistration = default(CancellationTokenRegistration);
		_keepAlive = default(GCHandle);
		_finalTaskSource = new TaskCompletionSource(runContinuationsAsynchronously ? TaskCreationOptions.RunContinuationsAsynchronously : TaskCreationOptions.None);
	}

	public bool TryGetValueTask(out ValueTask valueTask, object keepAlive = null, CancellationToken cancellationToken = default(CancellationToken))
	{
		lock (this)
		{
			if (_state == State.None && cancellationToken.CanBeCanceled)
			{
				_cancellationRegistration = cancellationToken.UnsafeRegister(delegate(object obj, CancellationToken cancellationToken)
				{
					var (resettableValueTaskSource, obj2) = ((ResettableValueTaskSource, object))obj;
					if (resettableValueTaskSource.TrySetException(new OperationCanceledException(cancellationToken)))
					{
						resettableValueTaskSource._cancellationAction?.Invoke(obj2);
					}
				}, (this, keepAlive));
			}
			State state = _state;
			if (state == State.None)
			{
				if (keepAlive != null)
				{
					_keepAlive = GCHandle.Alloc(keepAlive);
				}
				_state = State.Awaiting;
			}
			if (state == State.None || state == State.Ready || state == State.Completed)
			{
				valueTask = new ValueTask(this, _valueTaskSource.Version);
				return true;
			}
			valueTask = default(ValueTask);
			return false;
		}
	}

	public Task GetFinalTask()
	{
		return _finalTaskSource.Task;
	}

	private bool TryComplete(Exception exception, bool final)
	{
		CancellationTokenRegistration cancellationTokenRegistration = default(CancellationTokenRegistration);
		try
		{
			lock (this)
			{
				try
				{
					State state = _state;
					switch (state)
					{
					case State.Completed:
						return false;
					case State.None:
					case State.Awaiting:
						_state = (final ? State.Completed : State.Ready);
						break;
					}
					cancellationTokenRegistration = _cancellationRegistration;
					_cancellationRegistration = default(CancellationTokenRegistration);
					if (exception != null)
					{
						exception = ((exception.StackTrace == null) ? ExceptionDispatchInfo.SetCurrentStackTrace(exception) : exception);
						if (state == State.None || state == State.Awaiting)
						{
							_valueTaskSource.SetException(exception);
						}
						if (final)
						{
							return _finalTaskSource.TrySetException(exception);
						}
						return state != State.Ready;
					}
					if (state == State.None || state == State.Awaiting)
					{
						_valueTaskSource.SetResult(final);
					}
					if (final)
					{
						return _finalTaskSource.TrySetResult();
					}
					return state != State.Ready;
				}
				finally
				{
					if (_keepAlive.IsAllocated)
					{
						_keepAlive.Free();
					}
				}
			}
		}
		finally
		{
			cancellationTokenRegistration.Dispose();
		}
	}

	public bool TrySetResult(bool final = false)
	{
		return TryComplete(null, final);
	}

	public bool TrySetException(Exception exception, bool final = false)
	{
		return TryComplete(exception, final);
	}

	ValueTaskSourceStatus IValueTaskSource.GetStatus(short token)
	{
		return _valueTaskSource.GetStatus(token);
	}

	void IValueTaskSource.OnCompleted(Action<object> continuation, object state, short token, ValueTaskSourceOnCompletedFlags flags)
	{
		_valueTaskSource.OnCompleted(continuation, state, token, flags);
	}

	void IValueTaskSource.GetResult(short token)
	{
		bool flag = false;
		try
		{
			_valueTaskSource.GetResult(token);
			flag = true;
		}
		finally
		{
			lock (this)
			{
				State state = _state;
				if (state == State.Ready)
				{
					_valueTaskSource.Reset();
					_state = State.None;
					if (_finalTaskSource.Task.IsCompleted)
					{
						_state = State.Completed;
						if (_finalTaskSource.Task.IsCompletedSuccessfully)
						{
							_valueTaskSource.SetResult(result: true);
						}
						else
						{
							_valueTaskSource.SetException(_finalTaskSource.Task.Exception?.InnerException);
						}
						if (flag)
						{
							_valueTaskSource.GetResult(_valueTaskSource.Version);
						}
					}
				}
			}
		}
	}
}
