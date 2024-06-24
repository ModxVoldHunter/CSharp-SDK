using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace System.Threading;

internal static class StackHelper
{
	public static bool TryEnsureSufficientExecutionStack()
	{
		return RuntimeHelpers.TryEnsureSufficientExecutionStack();
	}

	public static void CallOnEmptyStack<TArg1, TArg2>(Action<TArg1, TArg2> action, TArg1 arg1, TArg2 arg2)
	{
		Task.Run(delegate
		{
			action(arg1, arg2);
		}).ContinueWith(delegate(Task t)
		{
			t.GetAwaiter().GetResult();
		}, CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default).GetAwaiter()
			.GetResult();
	}

	public static void CallOnEmptyStack<TArg1, TArg2, TArg3>(Action<TArg1, TArg2, TArg3> action, TArg1 arg1, TArg2 arg2, TArg3 arg3)
	{
		Task.Run(delegate
		{
			action(arg1, arg2, arg3);
		}).ContinueWith(delegate(Task t)
		{
			t.GetAwaiter().GetResult();
		}, CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default).GetAwaiter()
			.GetResult();
	}

	public static void CallOnEmptyStack<TArg1, TArg2, TArg3, TArg4>(Action<TArg1, TArg2, TArg3, TArg4> action, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4)
	{
		Task.Run(delegate
		{
			action(arg1, arg2, arg3, arg4);
		}).ContinueWith(delegate(Task t)
		{
			t.GetAwaiter().GetResult();
		}, CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default).GetAwaiter()
			.GetResult();
	}

	public static TResult CallOnEmptyStack<TResult>(Func<TResult> func)
	{
		return Task.Run(() => func()).ContinueWith((Task<TResult> t) => t.GetAwaiter().GetResult(), CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default).GetAwaiter()
			.GetResult();
	}

	public static TResult CallOnEmptyStack<TArg1, TResult>(Func<TArg1, TResult> func, TArg1 arg1)
	{
		return Task.Run(() => func(arg1)).ContinueWith((Task<TResult> t) => t.GetAwaiter().GetResult(), CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default).GetAwaiter()
			.GetResult();
	}

	public static TResult CallOnEmptyStack<TArg1, TArg2, TResult>(Func<TArg1, TArg2, TResult> func, TArg1 arg1, TArg2 arg2)
	{
		return Task.Run(() => func(arg1, arg2)).ContinueWith((Task<TResult> t) => t.GetAwaiter().GetResult(), CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default).GetAwaiter()
			.GetResult();
	}

	public static TResult CallOnEmptyStack<TArg1, TArg2, TArg3, TResult>(Func<TArg1, TArg2, TArg3, TResult> func, TArg1 arg1, TArg2 arg2, TArg3 arg3)
	{
		return Task.Run(() => func(arg1, arg2, arg3)).ContinueWith((Task<TResult> t) => t.GetAwaiter().GetResult(), CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default).GetAwaiter()
			.GetResult();
	}

	public static TResult CallOnEmptyStack<TArg1, TArg2, TArg3, TArg4, TResult>(Func<TArg1, TArg2, TArg3, TArg4, TResult> func, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4)
	{
		return Task.Run(() => func(arg1, arg2, arg3, arg4)).ContinueWith((Task<TResult> t) => t.GetAwaiter().GetResult(), CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default).GetAwaiter()
			.GetResult();
	}
}
