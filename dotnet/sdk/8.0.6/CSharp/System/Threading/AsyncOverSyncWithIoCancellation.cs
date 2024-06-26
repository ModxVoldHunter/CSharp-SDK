using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.Win32.SafeHandles;

namespace System.Threading;

internal sealed class AsyncOverSyncWithIoCancellation
{
	private struct SyncAsyncWorkItemRegistration : IDisposable
	{
		public AsyncOverSyncWithIoCancellation WorkItem;

		public CancellationTokenRegistration CancellationRegistration;

		public void Dispose()
		{
			if (WorkItem != null)
			{
				Volatile.Write(ref WorkItem._continueTryingToCancel, value: false);
				CancellationRegistration.Dispose();
				WorkItem._callbackCompleted?.GetAwaiter().GetResult();
			}
		}
	}

	[StructLayout(LayoutKind.Auto)]
	[CompilerGenerated]
	private struct _003CInvokeAsync_003Ed__7<TState> : IAsyncStateMachine
	{
		public int _003C_003E1__state;

		public PoolingAsyncValueTaskMethodBuilder _003C_003Et__builder;

		public CancellationToken cancellationToken;

		public Action<TState> action;

		public TState state;

		private ConfiguredTaskAwaitable.ConfiguredTaskAwaiter _003C_003Eu__1;

		private void MoveNext()
		{
			int num = _003C_003E1__state;
			try
			{
				ConfiguredTaskAwaitable.ConfiguredTaskAwaiter awaiter;
				if (num != 0)
				{
					awaiter = Task.CompletedTask.ConfigureAwait(ConfigureAwaitOptions.ForceYielding).GetAwaiter();
					if (!awaiter.IsCompleted)
					{
						num = (_003C_003E1__state = 0);
						_003C_003Eu__1 = awaiter;
						_003C_003Et__builder.AwaitUnsafeOnCompleted(ref awaiter, ref this);
						return;
					}
				}
				else
				{
					awaiter = _003C_003Eu__1;
					_003C_003Eu__1 = default(ConfiguredTaskAwaitable.ConfiguredTaskAwaiter);
					num = (_003C_003E1__state = -1);
				}
				awaiter.GetResult();
				SyncAsyncWorkItemRegistration syncAsyncWorkItemRegistration = RegisterCancellation(cancellationToken);
				try
				{
					action(state);
				}
				catch (OperationCanceledException ex) when (cancellationToken.IsCancellationRequested && ex.CancellationToken != cancellationToken)
				{
					throw CreateAppropriateCancellationException(cancellationToken, ex);
				}
				finally
				{
					if (num < 0)
					{
						syncAsyncWorkItemRegistration.Dispose();
					}
				}
			}
			catch (Exception exception)
			{
				_003C_003E1__state = -2;
				_003C_003Et__builder.SetException(exception);
				return;
			}
			_003C_003E1__state = -2;
			_003C_003Et__builder.SetResult();
		}

		void IAsyncStateMachine.MoveNext()
		{
			//ILSpy generated this explicit interface implementation from .override directive in MoveNext
			this.MoveNext();
		}

		[DebuggerHidden]
		private void SetStateMachine(IAsyncStateMachine stateMachine)
		{
			_003C_003Et__builder.SetStateMachine(stateMachine);
		}

		void IAsyncStateMachine.SetStateMachine(IAsyncStateMachine stateMachine)
		{
			//ILSpy generated this explicit interface implementation from .override directive in SetStateMachine
			this.SetStateMachine(stateMachine);
		}
	}

	[StructLayout(LayoutKind.Auto)]
	[CompilerGenerated]
	private struct _003CInvokeAsync_003Ed__8<TState, TResult> : IAsyncStateMachine
	{
		public int _003C_003E1__state;

		public PoolingAsyncValueTaskMethodBuilder<TResult> _003C_003Et__builder;

		public CancellationToken cancellationToken;

		public Func<TState, TResult> func;

		public TState state;

		private ConfiguredTaskAwaitable.ConfiguredTaskAwaiter _003C_003Eu__1;

		private void MoveNext()
		{
			int num = _003C_003E1__state;
			TResult result;
			try
			{
				ConfiguredTaskAwaitable.ConfiguredTaskAwaiter awaiter;
				if (num != 0)
				{
					awaiter = Task.CompletedTask.ConfigureAwait(ConfigureAwaitOptions.ForceYielding).GetAwaiter();
					if (!awaiter.IsCompleted)
					{
						num = (_003C_003E1__state = 0);
						_003C_003Eu__1 = awaiter;
						_003C_003Et__builder.AwaitUnsafeOnCompleted(ref awaiter, ref this);
						return;
					}
				}
				else
				{
					awaiter = _003C_003Eu__1;
					_003C_003Eu__1 = default(ConfiguredTaskAwaitable.ConfiguredTaskAwaiter);
					num = (_003C_003E1__state = -1);
				}
				awaiter.GetResult();
				SyncAsyncWorkItemRegistration syncAsyncWorkItemRegistration = RegisterCancellation(cancellationToken);
				try
				{
					result = func(state);
				}
				catch (OperationCanceledException ex) when (cancellationToken.IsCancellationRequested && ex.CancellationToken != cancellationToken)
				{
					throw CreateAppropriateCancellationException(cancellationToken, ex);
				}
				finally
				{
					if (num < 0)
					{
						syncAsyncWorkItemRegistration.Dispose();
					}
				}
			}
			catch (Exception exception)
			{
				_003C_003E1__state = -2;
				_003C_003Et__builder.SetException(exception);
				return;
			}
			_003C_003E1__state = -2;
			_003C_003Et__builder.SetResult(result);
		}

		void IAsyncStateMachine.MoveNext()
		{
			//ILSpy generated this explicit interface implementation from .override directive in MoveNext
			this.MoveNext();
		}

		[DebuggerHidden]
		private void SetStateMachine(IAsyncStateMachine stateMachine)
		{
			_003C_003Et__builder.SetStateMachine(stateMachine);
		}

		void IAsyncStateMachine.SetStateMachine(IAsyncStateMachine stateMachine)
		{
			//ILSpy generated this explicit interface implementation from .override directive in SetStateMachine
			this.SetStateMachine(stateMachine);
		}
	}

	[ThreadStatic]
	private static AsyncOverSyncWithIoCancellation t_instance;

	private readonly SafeThreadHandle _threadHandle;

	private bool _finishedCancellationRegistration;

	private bool _continueTryingToCancel;

	private Task _callbackCompleted;

	private AsyncOverSyncWithIoCancellation()
	{
		SafeThreadHandle safeThreadHandle = Interop.Kernel32.OpenThread(1, bInheritHandle: false, Interop.Kernel32.GetCurrentThreadId());
		if (!safeThreadHandle.IsInvalid)
		{
			_threadHandle = safeThreadHandle;
		}
		else
		{
			safeThreadHandle.Dispose();
		}
	}

	private void Reset()
	{
		_finishedCancellationRegistration = false;
		_continueTryingToCancel = true;
		_callbackCompleted = null;
	}

	[AsyncStateMachine(typeof(_003CInvokeAsync_003Ed__7<>))]
	[AsyncMethodBuilder(typeof(PoolingAsyncValueTaskMethodBuilder))]
	public static ValueTask InvokeAsync<TState>(Action<TState> action, TState state, CancellationToken cancellationToken)
	{
		Unsafe.SkipInit(out _003CInvokeAsync_003Ed__7<TState> stateMachine);
		stateMachine._003C_003Et__builder = PoolingAsyncValueTaskMethodBuilder.Create();
		stateMachine.action = action;
		stateMachine.state = state;
		stateMachine.cancellationToken = cancellationToken;
		stateMachine._003C_003E1__state = -1;
		stateMachine._003C_003Et__builder.Start(ref stateMachine);
		return stateMachine._003C_003Et__builder.Task;
	}

	[AsyncStateMachine(typeof(_003CInvokeAsync_003Ed__8<, >))]
	[AsyncMethodBuilder(typeof(PoolingAsyncValueTaskMethodBuilder<>))]
	public static ValueTask<TResult> InvokeAsync<TState, TResult>(Func<TState, TResult> func, TState state, CancellationToken cancellationToken)
	{
		Unsafe.SkipInit(out _003CInvokeAsync_003Ed__8<TState, TResult> stateMachine);
		stateMachine._003C_003Et__builder = PoolingAsyncValueTaskMethodBuilder<TResult>.Create();
		stateMachine.func = func;
		stateMachine.state = state;
		stateMachine.cancellationToken = cancellationToken;
		stateMachine._003C_003E1__state = -1;
		stateMachine._003C_003Et__builder.Start(ref stateMachine);
		return stateMachine._003C_003Et__builder.Task;
	}

	private static OperationCanceledException CreateAppropriateCancellationException(CancellationToken cancellationToken, OperationCanceledException originalOce)
	{
		OperationCanceledException ex = new OperationCanceledException(cancellationToken);
		string stackTrace = originalOce.StackTrace;
		if (stackTrace != null)
		{
			ExceptionDispatchInfo.SetRemoteStackTrace(ex, stackTrace);
		}
		return ex;
	}

	private static SyncAsyncWorkItemRegistration RegisterCancellation(CancellationToken cancellationToken)
	{
		if (!cancellationToken.CanBeCanceled)
		{
			return default(SyncAsyncWorkItemRegistration);
		}
		cancellationToken.ThrowIfCancellationRequested();
		AsyncOverSyncWithIoCancellation asyncOverSyncWithIoCancellation = t_instance ?? (t_instance = new AsyncOverSyncWithIoCancellation());
		if (asyncOverSyncWithIoCancellation._threadHandle == null)
		{
			return default(SyncAsyncWorkItemRegistration);
		}
		asyncOverSyncWithIoCancellation.Reset();
		SyncAsyncWorkItemRegistration result = default(SyncAsyncWorkItemRegistration);
		result.WorkItem = asyncOverSyncWithIoCancellation;
		result.CancellationRegistration = cancellationToken.UnsafeRegister(delegate(object s)
		{
			AsyncOverSyncWithIoCancellation asyncOverSyncWithIoCancellation2 = (AsyncOverSyncWithIoCancellation)s;
			if (Volatile.Read(ref asyncOverSyncWithIoCancellation2._finishedCancellationRegistration))
			{
				asyncOverSyncWithIoCancellation2._callbackCompleted = Task.Factory.StartNew(delegate(object s)
				{
					AsyncOverSyncWithIoCancellation asyncOverSyncWithIoCancellation3 = (AsyncOverSyncWithIoCancellation)s;
					SpinWait spinWait = default(SpinWait);
					while (Volatile.Read(ref asyncOverSyncWithIoCancellation3._continueTryingToCancel) && !Interop.Kernel32.CancelSynchronousIo(asyncOverSyncWithIoCancellation3._threadHandle) && Marshal.GetLastPInvokeError() == 1168)
					{
						spinWait.SpinOnce();
					}
				}, asyncOverSyncWithIoCancellation2, CancellationToken.None, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
			}
		}, asyncOverSyncWithIoCancellation);
		Volatile.Write(ref asyncOverSyncWithIoCancellation._finishedCancellationRegistration, value: true);
		if (cancellationToken.IsCancellationRequested)
		{
			result.Dispose();
			throw new OperationCanceledException(cancellationToken);
		}
		return result;
	}
}
