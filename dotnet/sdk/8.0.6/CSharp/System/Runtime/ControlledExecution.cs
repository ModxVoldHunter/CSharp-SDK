using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Threading;

namespace System.Runtime;

public static class ControlledExecution
{
	private sealed class Canceler
	{
		private readonly Thread _thread;

		private volatile bool _cancelCompleted;

		public bool IsCancelCompleted => _cancelCompleted;

		public Canceler(Thread thread)
		{
			_thread = thread;
		}

		public void Cancel()
		{
			try
			{
				AbortThread(_thread.GetNativeHandle());
			}
			finally
			{
				_cancelCompleted = true;
			}
		}
	}

	[ThreadStatic]
	private static bool t_executing;

	[Obsolete("ControlledExecution.Run method may corrupt the process and should not be used in production code.", DiagnosticId = "SYSLIB0046", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
	public static void Run(Action action, CancellationToken cancellationToken)
	{
		ArgumentNullException.ThrowIfNull(action, "action");
		if (t_executing)
		{
			throw new InvalidOperationException(SR.InvalidOperation_NestedControlledExecutionRun);
		}
		Canceler canceler = new Canceler(Thread.CurrentThread);
		try
		{
			t_executing = true;
			CancellationTokenRegistration cancellationTokenRegistration = cancellationToken.UnsafeRegister(delegate(object e)
			{
				((Canceler)e).Cancel();
			}, canceler);
			try
			{
				action();
			}
			finally
			{
				if (!cancellationTokenRegistration.Unregister())
				{
					SpinWait spinWait = default(SpinWait);
					while (!canceler.IsCancelCompleted)
					{
						ResetAbortThread();
						spinWait.SpinOnce();
					}
				}
			}
		}
		catch (ThreadAbortException ex2)
		{
			OperationCanceledException ex = (cancellationToken.IsCancellationRequested ? new OperationCanceledException(cancellationToken) : new OperationCanceledException());
			string stackTrace = ex2.StackTrace;
			if (stackTrace != null)
			{
				ExceptionDispatchInfo.SetRemoteStackTrace(ex, stackTrace);
			}
			throw ex;
		}
		finally
		{
			t_executing = false;
			if (cancellationToken.IsCancellationRequested)
			{
				ResetAbortThread();
			}
		}
	}

	[DllImport("QCall", EntryPoint = "ThreadNative_Abort", ExactSpelling = true)]
	[LibraryImport("QCall", EntryPoint = "ThreadNative_Abort")]
	private static extern void AbortThread(ThreadHandle thread);

	[DllImport("QCall", EntryPoint = "ThreadNative_ResetAbort", ExactSpelling = true)]
	[LibraryImport("QCall", EntryPoint = "ThreadNative_ResetAbort")]
	[SuppressGCTransition]
	private static extern void ResetAbortThread();
}
