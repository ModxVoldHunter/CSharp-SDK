using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Sources;

namespace System.Net.Quic;

internal sealed class ValueTaskSource : IValueTaskSource
{
	private enum State : byte
	{
		None,
		Awaiting,
		Completed
	}

	private State _state;

	private ManualResetValueTaskSourceCore<bool> _valueTaskSource;

	private CancellationTokenRegistration _cancellationRegistration;

	private GCHandle _keepAlive;

	public bool IsCompleted => Volatile.Read(ref Unsafe.As<State, byte>(ref _state)) == 2;

	public bool IsCompletedSuccessfully
	{
		get
		{
			if (IsCompleted)
			{
				return _valueTaskSource.GetStatus(_valueTaskSource.Version) == ValueTaskSourceStatus.Succeeded;
			}
			return false;
		}
	}

	public ValueTaskSource(bool runContinuationsAsynchronously = true)
	{
		_state = State.None;
		_valueTaskSource = new ManualResetValueTaskSourceCore<bool>
		{
			RunContinuationsAsynchronously = runContinuationsAsynchronously
		};
		_cancellationRegistration = default(CancellationTokenRegistration);
		_keepAlive = default(GCHandle);
	}

	public bool TryInitialize(out ValueTask valueTask, object keepAlive = null, CancellationToken cancellationToken = default(CancellationToken))
	{
		lock (this)
		{
			valueTask = new ValueTask(this, _valueTaskSource.Version);
			if (_state == State.None && cancellationToken.CanBeCanceled)
			{
				_cancellationRegistration = cancellationToken.UnsafeRegister(delegate(object obj, CancellationToken cancellationToken)
				{
					ValueTaskSource valueTaskSource = (ValueTaskSource)obj;
					valueTaskSource.TrySetException(new OperationCanceledException(cancellationToken));
				}, this);
			}
			if (_state == State.None)
			{
				if (keepAlive != null)
				{
					_keepAlive = GCHandle.Alloc(keepAlive);
				}
				_state = State.Awaiting;
				return true;
			}
			return false;
		}
	}

	private bool TryComplete(Exception exception)
	{
		CancellationTokenRegistration cancellationTokenRegistration = default(CancellationTokenRegistration);
		try
		{
			lock (this)
			{
				try
				{
					State state = _state;
					if (state != State.Completed)
					{
						_state = State.Completed;
						cancellationTokenRegistration = _cancellationRegistration;
						_cancellationRegistration = default(CancellationTokenRegistration);
						if (exception != null)
						{
							exception = ((exception.StackTrace == null) ? ExceptionDispatchInfo.SetCurrentStackTrace(exception) : exception);
							_valueTaskSource.SetException(exception);
						}
						else
						{
							_valueTaskSource.SetResult(result: true);
						}
						return true;
					}
					return false;
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

	public bool TrySetResult()
	{
		return TryComplete(null);
	}

	public bool TrySetException(Exception exception)
	{
		return TryComplete(exception);
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
		_valueTaskSource.GetResult(token);
	}
}
