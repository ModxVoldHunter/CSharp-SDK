using System.Diagnostics;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;

namespace System.Threading.Tasks.Sources;

[StructLayout(LayoutKind.Auto)]
public struct ManualResetValueTaskSourceCore<TResult>
{
	private Action<object> _continuation;

	private object _continuationState;

	private object _capturedContext;

	private ExceptionDispatchInfo _error;

	private TResult _result;

	private short _version;

	private bool _completed;

	private bool _runContinuationsAsynchronously;

	public bool RunContinuationsAsynchronously
	{
		get
		{
			return _runContinuationsAsynchronously;
		}
		set
		{
			_runContinuationsAsynchronously = value;
		}
	}

	public short Version => _version;

	public void Reset()
	{
		_version++;
		_continuation = null;
		_continuationState = null;
		_capturedContext = null;
		_error = null;
		_result = default(TResult);
		_completed = false;
	}

	public void SetResult(TResult result)
	{
		_result = result;
		SignalCompletion();
	}

	public void SetException(Exception error)
	{
		_error = ExceptionDispatchInfo.Capture(error);
		SignalCompletion();
	}

	public ValueTaskSourceStatus GetStatus(short token)
	{
		ValidateToken(token);
		if (Volatile.Read(ref _continuation) != null && _completed)
		{
			if (_error != null)
			{
				if (!(_error.SourceException is OperationCanceledException))
				{
					return ValueTaskSourceStatus.Faulted;
				}
				return ValueTaskSourceStatus.Canceled;
			}
			return ValueTaskSourceStatus.Succeeded;
		}
		return ValueTaskSourceStatus.Pending;
	}

	[StackTraceHidden]
	public TResult GetResult(short token)
	{
		if (token != _version || !_completed || _error != null)
		{
			ThrowForFailedGetResult();
		}
		return _result;
	}

	[StackTraceHidden]
	private void ThrowForFailedGetResult()
	{
		_error?.Throw();
		throw new InvalidOperationException();
	}

	public void OnCompleted(Action<object?> continuation, object? state, short token, ValueTaskSourceOnCompletedFlags flags)
	{
		if (continuation == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.continuation);
		}
		ValidateToken(token);
		if ((flags & ValueTaskSourceOnCompletedFlags.FlowExecutionContext) != 0)
		{
			_capturedContext = ExecutionContext.Capture();
		}
		if ((flags & ValueTaskSourceOnCompletedFlags.UseSchedulingContext) != 0)
		{
			SynchronizationContext current = SynchronizationContext.Current;
			if (current != null && current.GetType() != typeof(SynchronizationContext))
			{
				_capturedContext = ((_capturedContext == null) ? ((object)current) : ((object)new CapturedSchedulerAndExecutionContext(current, (ExecutionContext)_capturedContext)));
			}
			else
			{
				TaskScheduler current2 = TaskScheduler.Current;
				if (current2 != TaskScheduler.Default)
				{
					_capturedContext = ((_capturedContext == null) ? ((object)current2) : ((object)new CapturedSchedulerAndExecutionContext(current2, (ExecutionContext)_capturedContext)));
				}
			}
		}
		object obj = _continuation;
		if (obj == null)
		{
			_continuationState = state;
			obj = Interlocked.CompareExchange(ref _continuation, continuation, null);
			if (obj == null)
			{
				return;
			}
		}
		if (obj != ManualResetValueTaskSourceCoreShared.s_sentinel)
		{
			ThrowHelper.ThrowInvalidOperationException();
		}
		object capturedContext = _capturedContext;
		if (capturedContext != null)
		{
			if (capturedContext is ExecutionContext)
			{
				ThreadPool.QueueUserWorkItem<object>(continuation, state, preferLocal: true);
			}
			else
			{
				ManualResetValueTaskSourceCoreShared.ScheduleCapturedContext(capturedContext, continuation, state);
			}
		}
		else
		{
			ThreadPool.UnsafeQueueUserWorkItem<object>(continuation, state, preferLocal: true);
		}
	}

	private void ValidateToken(short token)
	{
		if (token != _version)
		{
			ThrowHelper.ThrowInvalidOperationException();
		}
	}

	private void SignalCompletion()
	{
		if (_completed)
		{
			ThrowHelper.ThrowInvalidOperationException();
		}
		_completed = true;
		Action<object> action = Volatile.Read(ref _continuation) ?? Interlocked.CompareExchange(ref _continuation, ManualResetValueTaskSourceCoreShared.s_sentinel, null);
		if (action == null)
		{
			return;
		}
		object capturedContext = _capturedContext;
		if (capturedContext == null)
		{
			if (_runContinuationsAsynchronously)
			{
				ThreadPool.UnsafeQueueUserWorkItem(action, _continuationState, preferLocal: true);
			}
			else
			{
				action(_continuationState);
			}
		}
		else if ((capturedContext is ExecutionContext || capturedContext is CapturedSchedulerAndExecutionContext) ? true : false)
		{
			ManualResetValueTaskSourceCoreShared.InvokeContinuationWithContext(capturedContext, action, _continuationState, _runContinuationsAsynchronously);
		}
		else
		{
			ManualResetValueTaskSourceCoreShared.ScheduleCapturedContext(capturedContext, action, _continuationState);
		}
	}
}
