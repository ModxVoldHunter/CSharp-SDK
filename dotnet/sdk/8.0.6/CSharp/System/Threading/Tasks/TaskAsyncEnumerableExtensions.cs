using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.Versioning;

namespace System.Threading.Tasks;

public static class TaskAsyncEnumerableExtensions
{
	private sealed class ManualResetEventWithAwaiterSupport : ManualResetEventSlim
	{
		private readonly Action _onCompleted;

		public ManualResetEventWithAwaiterSupport()
		{
			_onCompleted = base.Set;
		}

		[UnsupportedOSPlatform("browser")]
		public void Wait<TAwaiter>(TAwaiter awaiter) where TAwaiter : ICriticalNotifyCompletion
		{
			awaiter.UnsafeOnCompleted(_onCompleted);
			Wait();
			Reset();
		}
	}

	public static ConfiguredAsyncDisposable ConfigureAwait(this IAsyncDisposable source, bool continueOnCapturedContext)
	{
		return new ConfiguredAsyncDisposable(source, continueOnCapturedContext);
	}

	public static ConfiguredCancelableAsyncEnumerable<T> ConfigureAwait<T>(this IAsyncEnumerable<T> source, bool continueOnCapturedContext)
	{
		return new ConfiguredCancelableAsyncEnumerable<T>(source, continueOnCapturedContext, default(CancellationToken));
	}

	public static ConfiguredCancelableAsyncEnumerable<T> WithCancellation<T>(this IAsyncEnumerable<T> source, CancellationToken cancellationToken)
	{
		return new ConfiguredCancelableAsyncEnumerable<T>(source, continueOnCapturedContext: true, cancellationToken);
	}

	[UnsupportedOSPlatform("browser")]
	public static IEnumerable<T> ToBlockingEnumerable<T>(this IAsyncEnumerable<T> source, CancellationToken cancellationToken = default(CancellationToken))
	{
		IAsyncEnumerator<T> enumerator = source.GetAsyncEnumerator(cancellationToken);
		ManualResetEventWithAwaiterSupport mres = null;
		try
		{
			while (true)
			{
				ValueTask<bool> valueTask = enumerator.MoveNextAsync();
				if (!valueTask.IsCompleted)
				{
					ManualResetEventWithAwaiterSupport manualResetEventWithAwaiterSupport = mres;
					if (manualResetEventWithAwaiterSupport == null)
					{
						ManualResetEventWithAwaiterSupport manualResetEventWithAwaiterSupport2;
						mres = (manualResetEventWithAwaiterSupport2 = new ManualResetEventWithAwaiterSupport());
						manualResetEventWithAwaiterSupport = manualResetEventWithAwaiterSupport2;
					}
					manualResetEventWithAwaiterSupport.Wait(valueTask.ConfigureAwait(continueOnCapturedContext: false).GetAwaiter());
				}
				if (valueTask.Result)
				{
					yield return enumerator.Current;
					continue;
				}
				break;
			}
		}
		finally
		{
			ValueTask valueTask2 = enumerator.DisposeAsync();
			if (!valueTask2.IsCompleted)
			{
				(mres ?? new ManualResetEventWithAwaiterSupport()).Wait(valueTask2.ConfigureAwait(continueOnCapturedContext: false).GetAwaiter());
			}
			valueTask2.GetAwaiter().GetResult();
		}
	}
}
