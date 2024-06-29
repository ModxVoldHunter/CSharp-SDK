using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;

namespace System.Threading.Tasks;

public static class Parallel
{
	private abstract class ForEachAsyncState<TSource> : TaskCompletionSource, IThreadPoolWorkItem
	{
		private readonly CancellationToken _externalCancellationToken;

		protected readonly CancellationTokenRegistration _registration;

		private readonly Func<object, Task> _taskBody;

		private readonly TaskScheduler _scheduler;

		private readonly ExecutionContext _executionContext;

		private readonly SemaphoreSlim _lock;

		private int _completionRefCount;

		private List<Exception> _exceptions;

		private int _remainingDop;

		public readonly Func<TSource, CancellationToken, ValueTask> LoopBody;

		public readonly CancellationTokenSource Cancellation = new CancellationTokenSource();

		protected ForEachAsyncState(Func<object, Task> taskBody, bool needsLock, int dop, TaskScheduler scheduler, CancellationToken cancellationToken, Func<TSource, CancellationToken, ValueTask> body)
		{
			_taskBody = taskBody;
			_lock = (needsLock ? new SemaphoreSlim(1, 1) : null);
			_remainingDop = ((dop < 0) ? DefaultDegreeOfParallelism : dop);
			LoopBody = body;
			_scheduler = scheduler;
			if (scheduler == TaskScheduler.Default)
			{
				_executionContext = ExecutionContext.Capture();
			}
			_externalCancellationToken = cancellationToken;
			_registration = cancellationToken.UnsafeRegister(delegate(object o)
			{
				((ForEachAsyncState<TSource>)o).Cancellation.Cancel();
			}, this);
		}

		public void QueueWorkerIfDopAvailable()
		{
			if (_remainingDop > 0)
			{
				_remainingDop--;
				Interlocked.Increment(ref _completionRefCount);
				if (_scheduler == TaskScheduler.Default)
				{
					ThreadPool.UnsafeQueueUserWorkItem(this, preferLocal: false);
				}
				else
				{
					System.Threading.Tasks.Task.Factory.StartNew(_taskBody, this, default(CancellationToken), TaskCreationOptions.DenyChildAttach, _scheduler);
				}
			}
		}

		public bool SignalWorkerCompletedIterating()
		{
			return Interlocked.Decrement(ref _completionRefCount) == 0;
		}

		public Task AcquireLock()
		{
			return _lock.WaitAsync(CancellationToken.None);
		}

		public void ReleaseLock()
		{
			_lock.Release();
		}

		public void RecordException(Exception e)
		{
			lock (this)
			{
				(_exceptions ?? (_exceptions = new List<Exception>())).Add(e);
			}
			try
			{
				Cancellation.Cancel();
			}
			catch (AggregateException ex)
			{
				lock (this)
				{
					_exceptions.AddRange(ex.InnerExceptions);
				}
			}
		}

		public void Complete()
		{
			if (_externalCancellationToken.IsCancellationRequested)
			{
				bool flag = TrySetCanceled(_externalCancellationToken);
			}
			else if (_exceptions == null)
			{
				bool flag = TrySetResult();
			}
			else
			{
				bool flag = TrySetException(_exceptions);
			}
		}

		void IThreadPoolWorkItem.Execute()
		{
			if (_executionContext == null)
			{
				_taskBody(this);
				return;
			}
			ExecutionContext.Run(_executionContext, delegate(object o)
			{
				((ForEachAsyncState<TSource>)o)._taskBody(o);
			}, this);
		}
	}

	private sealed class SyncForEachAsyncState<TSource> : ForEachAsyncState<TSource>, IDisposable
	{
		public readonly IEnumerator<TSource> Enumerator;

		public SyncForEachAsyncState(IEnumerable<TSource> source, Func<object, Task> taskBody, int dop, TaskScheduler scheduler, CancellationToken cancellationToken, Func<TSource, CancellationToken, ValueTask> body)
			: base(taskBody, needsLock: true, dop, scheduler, cancellationToken, body)
		{
			Enumerator = source.GetEnumerator() ?? throw new InvalidOperationException(System.SR.Parallel_ForEach_NullEnumerator);
		}

		public void Dispose()
		{
			_registration.Dispose();
			Enumerator.Dispose();
		}
	}

	private sealed class AsyncForEachAsyncState<TSource> : ForEachAsyncState<TSource>, IAsyncDisposable
	{
		public readonly IAsyncEnumerator<TSource> Enumerator;

		public AsyncForEachAsyncState(IAsyncEnumerable<TSource> source, Func<object, Task> taskBody, int dop, TaskScheduler scheduler, CancellationToken cancellationToken, Func<TSource, CancellationToken, ValueTask> body)
			: base(taskBody, needsLock: true, dop, scheduler, cancellationToken, body)
		{
			Enumerator = source.GetAsyncEnumerator(Cancellation.Token) ?? throw new InvalidOperationException(System.SR.Parallel_ForEach_NullEnumerator);
		}

		public ValueTask DisposeAsync()
		{
			_registration.Dispose();
			return Enumerator.DisposeAsync();
		}
	}

	private sealed class ForEachState<T> : ForEachAsyncState<T>, IDisposable
	{
		public T NextAvailable;

		public readonly T ToExclusive;

		public ForEachState(T fromExclusive, T toExclusive, Func<object, Task> taskBody, bool needsLock, int dop, TaskScheduler scheduler, CancellationToken cancellationToken, Func<T, CancellationToken, ValueTask> body)
			: base(taskBody, needsLock, dop, scheduler, cancellationToken, body)
		{
			NextAvailable = fromExclusive;
			ToExclusive = toExclusive;
		}

		public void Dispose()
		{
			_registration.Dispose();
		}
	}

	internal static int s_forkJoinContextID;

	internal static readonly ParallelOptions s_defaultParallelOptions = new ParallelOptions();

	private static int DefaultDegreeOfParallelism => Environment.ProcessorCount;

	public static void Invoke(params Action[] actions)
	{
		Invoke(s_defaultParallelOptions, actions);
	}

	public static void Invoke(ParallelOptions parallelOptions, params Action[] actions)
	{
		ParallelOptions parallelOptions = parallelOptions;
		ArgumentNullException.ThrowIfNull(parallelOptions, "parallelOptions");
		ArgumentNullException.ThrowIfNull(actions, "actions");
		parallelOptions.CancellationToken.ThrowIfCancellationRequested();
		Action[] actionsCopy = new Action[actions.Length];
		for (int i = 0; i < actionsCopy.Length; i++)
		{
			actionsCopy[i] = actions[i];
			if (actionsCopy[i] == null)
			{
				throw new ArgumentException(System.SR.Parallel_Invoke_ActionNull);
			}
		}
		int forkJoinContextID = 0;
		if (ParallelEtwProvider.Log.IsEnabled())
		{
			forkJoinContextID = Interlocked.Increment(ref s_forkJoinContextID);
			ParallelEtwProvider.Log.ParallelInvokeBegin(TaskScheduler.Current.Id, Task.CurrentId.GetValueOrDefault(), forkJoinContextID, ParallelEtwProvider.ForkJoinOperationType.ParallelInvoke, actionsCopy.Length);
		}
		if (actionsCopy.Length < 1)
		{
			return;
		}
		try
		{
			if (OperatingSystem.IsBrowser() || actionsCopy.Length > 10 || (parallelOptions.MaxDegreeOfParallelism != -1 && parallelOptions.MaxDegreeOfParallelism < actionsCopy.Length))
			{
				ConcurrentQueue<Exception> exceptionQ = null;
				int actionIndex = 0;
				try
				{
					TaskReplicator.Run(delegate(ref object state, long timeout, out bool replicationDelegateYieldedBeforeCompletion)
					{
						replicationDelegateYieldedBeforeCompletion = false;
						for (int num = Interlocked.Increment(ref actionIndex); num <= actionsCopy.Length; num = Interlocked.Increment(ref actionIndex))
						{
							try
							{
								actionsCopy[num - 1]();
							}
							catch (Exception item)
							{
								LazyInitializer.EnsureInitialized(ref exceptionQ, () => new ConcurrentQueue<Exception>());
								exceptionQ.Enqueue(item);
							}
							parallelOptions.CancellationToken.ThrowIfCancellationRequested();
						}
					}, parallelOptions, stopOnFirstFailure: false);
				}
				catch (Exception ex)
				{
					LazyInitializer.EnsureInitialized(ref exceptionQ, () => new ConcurrentQueue<Exception>());
					if (ex is ObjectDisposedException)
					{
						throw;
					}
					if (ex is AggregateException ex2)
					{
						foreach (Exception innerException in ex2.InnerExceptions)
						{
							exceptionQ.Enqueue(innerException);
						}
					}
					else
					{
						exceptionQ.Enqueue(ex);
					}
				}
				if (exceptionQ != null && !exceptionQ.IsEmpty)
				{
					ThrowSingleCancellationExceptionOrOtherException(exceptionQ, parallelOptions.CancellationToken, new AggregateException(exceptionQ));
				}
				return;
			}
			Task[] array = new Task[actionsCopy.Length];
			parallelOptions.CancellationToken.ThrowIfCancellationRequested();
			for (int j = 1; j < array.Length; j++)
			{
				array[j] = Task.Factory.StartNew(actionsCopy[j], parallelOptions.CancellationToken, TaskCreationOptions.None, parallelOptions.EffectiveTaskScheduler);
			}
			array[0] = new Task(actionsCopy[0], parallelOptions.CancellationToken, TaskCreationOptions.None);
			array[0].RunSynchronously(parallelOptions.EffectiveTaskScheduler);
			try
			{
				Task.WaitAll(array);
			}
			catch (AggregateException ex3)
			{
				ThrowSingleCancellationExceptionOrOtherException(ex3.InnerExceptions, parallelOptions.CancellationToken, ex3);
			}
		}
		finally
		{
			if (ParallelEtwProvider.Log.IsEnabled())
			{
				ParallelEtwProvider.Log.ParallelInvokeEnd(TaskScheduler.Current.Id, Task.CurrentId.GetValueOrDefault(), forkJoinContextID);
			}
		}
	}

	public static ParallelLoopResult For(int fromInclusive, int toExclusive, Action<int> body)
	{
		ArgumentNullException.ThrowIfNull(body, "body");
		return ForWorker<object, int>(fromInclusive, toExclusive, s_defaultParallelOptions, body, null, null, null, null);
	}

	public static ParallelLoopResult For(long fromInclusive, long toExclusive, Action<long> body)
	{
		ArgumentNullException.ThrowIfNull(body, "body");
		return ForWorker<object, long>(fromInclusive, toExclusive, s_defaultParallelOptions, body, null, null, null, null);
	}

	public static ParallelLoopResult For(int fromInclusive, int toExclusive, ParallelOptions parallelOptions, Action<int> body)
	{
		ArgumentNullException.ThrowIfNull(parallelOptions, "parallelOptions");
		ArgumentNullException.ThrowIfNull(body, "body");
		return ForWorker<object, int>(fromInclusive, toExclusive, parallelOptions, body, null, null, null, null);
	}

	public static ParallelLoopResult For(long fromInclusive, long toExclusive, ParallelOptions parallelOptions, Action<long> body)
	{
		ArgumentNullException.ThrowIfNull(parallelOptions, "parallelOptions");
		ArgumentNullException.ThrowIfNull(body, "body");
		return ForWorker<object, long>(fromInclusive, toExclusive, parallelOptions, body, null, null, null, null);
	}

	public static ParallelLoopResult For(int fromInclusive, int toExclusive, Action<int, ParallelLoopState> body)
	{
		ArgumentNullException.ThrowIfNull(body, "body");
		return ForWorker<object, int>(fromInclusive, toExclusive, s_defaultParallelOptions, null, body, null, null, null);
	}

	public static ParallelLoopResult For(long fromInclusive, long toExclusive, Action<long, ParallelLoopState> body)
	{
		ArgumentNullException.ThrowIfNull(body, "body");
		return ForWorker<object, long>(fromInclusive, toExclusive, s_defaultParallelOptions, null, body, null, null, null);
	}

	public static ParallelLoopResult For(int fromInclusive, int toExclusive, ParallelOptions parallelOptions, Action<int, ParallelLoopState> body)
	{
		ArgumentNullException.ThrowIfNull(parallelOptions, "parallelOptions");
		ArgumentNullException.ThrowIfNull(body, "body");
		return ForWorker<object, int>(fromInclusive, toExclusive, parallelOptions, null, body, null, null, null);
	}

	public static ParallelLoopResult For(long fromInclusive, long toExclusive, ParallelOptions parallelOptions, Action<long, ParallelLoopState> body)
	{
		ArgumentNullException.ThrowIfNull(parallelOptions, "parallelOptions");
		ArgumentNullException.ThrowIfNull(body, "body");
		return ForWorker<object, long>(fromInclusive, toExclusive, parallelOptions, null, body, null, null, null);
	}

	public static ParallelLoopResult For<TLocal>(int fromInclusive, int toExclusive, Func<TLocal> localInit, Func<int, ParallelLoopState, TLocal, TLocal> body, Action<TLocal> localFinally)
	{
		ArgumentNullException.ThrowIfNull(localInit, "localInit");
		ArgumentNullException.ThrowIfNull(body, "body");
		ArgumentNullException.ThrowIfNull(localFinally, "localFinally");
		return ForWorker(fromInclusive, toExclusive, s_defaultParallelOptions, null, null, body, localInit, localFinally);
	}

	public static ParallelLoopResult For<TLocal>(long fromInclusive, long toExclusive, Func<TLocal> localInit, Func<long, ParallelLoopState, TLocal, TLocal> body, Action<TLocal> localFinally)
	{
		ArgumentNullException.ThrowIfNull(localInit, "localInit");
		ArgumentNullException.ThrowIfNull(body, "body");
		ArgumentNullException.ThrowIfNull(localFinally, "localFinally");
		return ForWorker(fromInclusive, toExclusive, s_defaultParallelOptions, null, null, body, localInit, localFinally);
	}

	public static ParallelLoopResult For<TLocal>(int fromInclusive, int toExclusive, ParallelOptions parallelOptions, Func<TLocal> localInit, Func<int, ParallelLoopState, TLocal, TLocal> body, Action<TLocal> localFinally)
	{
		ArgumentNullException.ThrowIfNull(parallelOptions, "parallelOptions");
		ArgumentNullException.ThrowIfNull(localInit, "localInit");
		ArgumentNullException.ThrowIfNull(body, "body");
		ArgumentNullException.ThrowIfNull(localFinally, "localFinally");
		return ForWorker(fromInclusive, toExclusive, parallelOptions, null, null, body, localInit, localFinally);
	}

	public static ParallelLoopResult For<TLocal>(long fromInclusive, long toExclusive, ParallelOptions parallelOptions, Func<TLocal> localInit, Func<long, ParallelLoopState, TLocal, TLocal> body, Action<TLocal> localFinally)
	{
		ArgumentNullException.ThrowIfNull(parallelOptions, "parallelOptions");
		ArgumentNullException.ThrowIfNull(localInit, "localInit");
		ArgumentNullException.ThrowIfNull(body, "body");
		ArgumentNullException.ThrowIfNull(localFinally, "localFinally");
		return ForWorker(fromInclusive, toExclusive, parallelOptions, null, null, body, localInit, localFinally);
	}

	private static bool CheckTimeoutReached(long timeoutOccursAt)
	{
		return timeoutOccursAt - Environment.TickCount64 <= 0;
	}

	private static long ComputeTimeoutPoint(long timeoutLength)
	{
		return Environment.TickCount64 + timeoutLength;
	}

	private static ParallelLoopResult ForWorker<TLocal, TInt>(TInt fromInclusive, TInt toExclusive, ParallelOptions parallelOptions, Action<TInt> body, Action<TInt, ParallelLoopState> bodyWithState, Func<TInt, ParallelLoopState, TLocal, TLocal> bodyWithLocal, Func<TLocal> localInit, Action<TLocal> localFinally) where TInt : struct, IBinaryInteger<TInt>, IMinMaxValue<TInt>
	{
		ParallelLoopResult result = default(ParallelLoopResult);
		if (toExclusive <= fromInclusive)
		{
			result._completed = true;
			return result;
		}
		ParallelLoopStateFlags<TInt> sharedPStateFlags = new ParallelLoopStateFlags<TInt>();
		parallelOptions.CancellationToken.ThrowIfCancellationRequested();
		int nNumExpectedWorkers = ((parallelOptions.EffectiveMaxConcurrencyLevel == -1) ? Environment.ProcessorCount : parallelOptions.EffectiveMaxConcurrencyLevel);
		RangeManager rangeManager = new RangeManager(long.CreateTruncating(fromInclusive), long.CreateTruncating(toExclusive), 1L, nNumExpectedWorkers);
		OperationCanceledException oce = null;
		CancellationTokenRegistration cancellationTokenRegistration = ((!parallelOptions.CancellationToken.CanBeCanceled) ? default(CancellationTokenRegistration) : parallelOptions.CancellationToken.UnsafeRegister((Action<object?>)delegate
		{
			oce = new OperationCanceledException(parallelOptions.CancellationToken);
			sharedPStateFlags.Cancel();
		}, (object?)null));
		int forkJoinContextID = 0;
		if (ParallelEtwProvider.Log.IsEnabled())
		{
			forkJoinContextID = Interlocked.Increment(ref s_forkJoinContextID);
			ParallelEtwProvider.Log.ParallelLoopBegin(TaskScheduler.Current.Id, Task.CurrentId.GetValueOrDefault(), forkJoinContextID, ParallelEtwProvider.ForkJoinOperationType.ParallelFor, long.CreateTruncating(fromInclusive), long.CreateTruncating(toExclusive));
		}
		try
		{
			try
			{
				TaskReplicator.Run(delegate(ref RangeWorker currentWorker, long timeout, out bool replicationDelegateYieldedBeforeCompletion)
				{
					if (!currentWorker.IsInitialized)
					{
						currentWorker = rangeManager.RegisterNewWorker();
					}
					replicationDelegateYieldedBeforeCompletion = false;
					if (!currentWorker.FindNewWork<TInt>(out var fromInclusive2, out var toExclusive2) || sharedPStateFlags.ShouldExitLoop(fromInclusive2))
					{
						return;
					}
					if (ParallelEtwProvider.Log.IsEnabled())
					{
						ParallelEtwProvider.Log.ParallelFork(TaskScheduler.Current.Id, Task.CurrentId.GetValueOrDefault(), forkJoinContextID);
					}
					TLocal val = default(TLocal);
					bool flag = false;
					try
					{
						ParallelLoopState<TInt> parallelLoopState = null;
						if (bodyWithState != null)
						{
							parallelLoopState = new ParallelLoopState<TInt>(sharedPStateFlags);
						}
						else if (bodyWithLocal != null)
						{
							parallelLoopState = new ParallelLoopState<TInt>(sharedPStateFlags);
							if (localInit != null)
							{
								val = localInit();
								flag = true;
							}
						}
						long timeoutOccursAt = ComputeTimeoutPoint(timeout);
						do
						{
							if (body != null)
							{
								for (TInt val2 = fromInclusive2; val2 < toExclusive2; ++val2)
								{
									if (sharedPStateFlags.LoopStateFlags != 0 && sharedPStateFlags.ShouldExitLoop())
									{
										break;
									}
									body(val2);
								}
							}
							else if (bodyWithState != null)
							{
								for (TInt val3 = fromInclusive2; val3 < toExclusive2 && (sharedPStateFlags.LoopStateFlags == 0 || !sharedPStateFlags.ShouldExitLoop(val3)); ++val3)
								{
									parallelLoopState.CurrentIteration = val3;
									bodyWithState(val3, parallelLoopState);
								}
							}
							else
							{
								for (TInt val4 = fromInclusive2; val4 < toExclusive2 && (sharedPStateFlags.LoopStateFlags == 0 || !sharedPStateFlags.ShouldExitLoop(val4)); ++val4)
								{
									parallelLoopState.CurrentIteration = val4;
									val = bodyWithLocal(val4, parallelLoopState, val);
								}
							}
							if (CheckTimeoutReached(timeoutOccursAt))
							{
								replicationDelegateYieldedBeforeCompletion = true;
								break;
							}
						}
						while (currentWorker.FindNewWork<TInt>(out fromInclusive2, out toExclusive2) && (sharedPStateFlags.LoopStateFlags == 0 || !sharedPStateFlags.ShouldExitLoop(fromInclusive2)));
					}
					catch (Exception source)
					{
						sharedPStateFlags.SetExceptional();
						ExceptionDispatchInfo.Throw(source);
					}
					finally
					{
						if (localFinally != null && flag)
						{
							localFinally(val);
						}
						if (ParallelEtwProvider.Log.IsEnabled())
						{
							ParallelEtwProvider.Log.ParallelJoin(TaskScheduler.Current.Id, Task.CurrentId.GetValueOrDefault(), forkJoinContextID);
						}
					}
				}, parallelOptions, stopOnFirstFailure: true);
			}
			finally
			{
				if (parallelOptions.CancellationToken.CanBeCanceled)
				{
					cancellationTokenRegistration.Dispose();
				}
			}
			if (oce != null)
			{
				throw oce;
			}
		}
		catch (AggregateException ex)
		{
			ThrowSingleCancellationExceptionOrOtherException(ex.InnerExceptions, parallelOptions.CancellationToken, ex);
		}
		finally
		{
			int loopStateFlags = sharedPStateFlags.LoopStateFlags;
			result._completed = loopStateFlags == 0;
			if (((uint)loopStateFlags & 2u) != 0)
			{
				result._lowestBreakIteration = long.CreateTruncating(sharedPStateFlags.LowestBreakIteration);
			}
			if (ParallelEtwProvider.Log.IsEnabled())
			{
				TInt value = ((loopStateFlags == 0) ? (toExclusive - fromInclusive) : (((loopStateFlags & 2) == 0) ? TInt.CreateTruncating(-1) : (sharedPStateFlags.LowestBreakIteration - fromInclusive)));
				ParallelEtwProvider.Log.ParallelLoopEnd(TaskScheduler.Current.Id, Task.CurrentId.GetValueOrDefault(), forkJoinContextID, long.CreateTruncating(value));
			}
		}
		return result;
	}

	public static ParallelLoopResult ForEach<TSource>(IEnumerable<TSource> source, Action<TSource> body)
	{
		ArgumentNullException.ThrowIfNull(source, "source");
		ArgumentNullException.ThrowIfNull(body, "body");
		return ForEachWorker<TSource, object>(source, s_defaultParallelOptions, body, null, null, null, null, null, null);
	}

	public static ParallelLoopResult ForEach<TSource>(IEnumerable<TSource> source, ParallelOptions parallelOptions, Action<TSource> body)
	{
		ArgumentNullException.ThrowIfNull(source, "source");
		ArgumentNullException.ThrowIfNull(parallelOptions, "parallelOptions");
		ArgumentNullException.ThrowIfNull(body, "body");
		return ForEachWorker<TSource, object>(source, parallelOptions, body, null, null, null, null, null, null);
	}

	public static ParallelLoopResult ForEach<TSource>(IEnumerable<TSource> source, Action<TSource, ParallelLoopState> body)
	{
		ArgumentNullException.ThrowIfNull(source, "source");
		ArgumentNullException.ThrowIfNull(body, "body");
		return ForEachWorker<TSource, object>(source, s_defaultParallelOptions, null, body, null, null, null, null, null);
	}

	public static ParallelLoopResult ForEach<TSource>(IEnumerable<TSource> source, ParallelOptions parallelOptions, Action<TSource, ParallelLoopState> body)
	{
		ArgumentNullException.ThrowIfNull(source, "source");
		ArgumentNullException.ThrowIfNull(parallelOptions, "parallelOptions");
		ArgumentNullException.ThrowIfNull(body, "body");
		return ForEachWorker<TSource, object>(source, parallelOptions, null, body, null, null, null, null, null);
	}

	public static ParallelLoopResult ForEach<TSource>(IEnumerable<TSource> source, Action<TSource, ParallelLoopState, long> body)
	{
		ArgumentNullException.ThrowIfNull(source, "source");
		ArgumentNullException.ThrowIfNull(body, "body");
		return ForEachWorker<TSource, object>(source, s_defaultParallelOptions, null, null, body, null, null, null, null);
	}

	public static ParallelLoopResult ForEach<TSource>(IEnumerable<TSource> source, ParallelOptions parallelOptions, Action<TSource, ParallelLoopState, long> body)
	{
		ArgumentNullException.ThrowIfNull(source, "source");
		ArgumentNullException.ThrowIfNull(parallelOptions, "parallelOptions");
		ArgumentNullException.ThrowIfNull(body, "body");
		return ForEachWorker<TSource, object>(source, parallelOptions, null, null, body, null, null, null, null);
	}

	public static ParallelLoopResult ForEach<TSource, TLocal>(IEnumerable<TSource> source, Func<TLocal> localInit, Func<TSource, ParallelLoopState, TLocal, TLocal> body, Action<TLocal> localFinally)
	{
		ArgumentNullException.ThrowIfNull(source, "source");
		ArgumentNullException.ThrowIfNull(localInit, "localInit");
		ArgumentNullException.ThrowIfNull(body, "body");
		ArgumentNullException.ThrowIfNull(localFinally, "localFinally");
		return ForEachWorker(source, s_defaultParallelOptions, null, null, null, body, null, localInit, localFinally);
	}

	public static ParallelLoopResult ForEach<TSource, TLocal>(IEnumerable<TSource> source, ParallelOptions parallelOptions, Func<TLocal> localInit, Func<TSource, ParallelLoopState, TLocal, TLocal> body, Action<TLocal> localFinally)
	{
		ArgumentNullException.ThrowIfNull(source, "source");
		ArgumentNullException.ThrowIfNull(parallelOptions, "parallelOptions");
		ArgumentNullException.ThrowIfNull(localInit, "localInit");
		ArgumentNullException.ThrowIfNull(body, "body");
		ArgumentNullException.ThrowIfNull(localFinally, "localFinally");
		return ForEachWorker(source, parallelOptions, null, null, null, body, null, localInit, localFinally);
	}

	public static ParallelLoopResult ForEach<TSource, TLocal>(IEnumerable<TSource> source, Func<TLocal> localInit, Func<TSource, ParallelLoopState, long, TLocal, TLocal> body, Action<TLocal> localFinally)
	{
		ArgumentNullException.ThrowIfNull(source, "source");
		ArgumentNullException.ThrowIfNull(localInit, "localInit");
		ArgumentNullException.ThrowIfNull(body, "body");
		ArgumentNullException.ThrowIfNull(localFinally, "localFinally");
		return ForEachWorker(source, s_defaultParallelOptions, null, null, null, null, body, localInit, localFinally);
	}

	public static ParallelLoopResult ForEach<TSource, TLocal>(IEnumerable<TSource> source, ParallelOptions parallelOptions, Func<TLocal> localInit, Func<TSource, ParallelLoopState, long, TLocal, TLocal> body, Action<TLocal> localFinally)
	{
		ArgumentNullException.ThrowIfNull(source, "source");
		ArgumentNullException.ThrowIfNull(parallelOptions, "parallelOptions");
		ArgumentNullException.ThrowIfNull(localInit, "localInit");
		ArgumentNullException.ThrowIfNull(body, "body");
		ArgumentNullException.ThrowIfNull(localFinally, "localFinally");
		return ForEachWorker(source, parallelOptions, null, null, null, null, body, localInit, localFinally);
	}

	private static ParallelLoopResult ForEachWorker<TSource, TLocal>(IEnumerable<TSource> source, ParallelOptions parallelOptions, Action<TSource> body, Action<TSource, ParallelLoopState> bodyWithState, Action<TSource, ParallelLoopState, long> bodyWithStateAndIndex, Func<TSource, ParallelLoopState, TLocal, TLocal> bodyWithStateAndLocal, Func<TSource, ParallelLoopState, long, TLocal, TLocal> bodyWithEverything, Func<TLocal> localInit, Action<TLocal> localFinally)
	{
		parallelOptions.CancellationToken.ThrowIfCancellationRequested();
		if (source is TSource[] array)
		{
			return ForEachWorker(array, parallelOptions, body, bodyWithState, bodyWithStateAndIndex, bodyWithStateAndLocal, bodyWithEverything, localInit, localFinally);
		}
		if (source is IList<TSource> list)
		{
			return ForEachWorker(list, parallelOptions, body, bodyWithState, bodyWithStateAndIndex, bodyWithStateAndLocal, bodyWithEverything, localInit, localFinally);
		}
		return PartitionerForEachWorker(Partitioner.Create(source), parallelOptions, body, bodyWithState, bodyWithStateAndIndex, bodyWithStateAndLocal, bodyWithEverything, localInit, localFinally);
	}

	private static ParallelLoopResult ForEachWorker<TSource, TLocal>(TSource[] array, ParallelOptions parallelOptions, Action<TSource> body, Action<TSource, ParallelLoopState> bodyWithState, Action<TSource, ParallelLoopState, long> bodyWithStateAndIndex, Func<TSource, ParallelLoopState, TLocal, TLocal> bodyWithStateAndLocal, Func<TSource, ParallelLoopState, long, TLocal, TLocal> bodyWithEverything, Func<TLocal> localInit, Action<TLocal> localFinally)
	{
		int lowerBound = array.GetLowerBound(0);
		int toExclusive = array.GetUpperBound(0) + 1;
		if (body != null)
		{
			return ForWorker<object, int>(lowerBound, toExclusive, parallelOptions, delegate(int i)
			{
				body(array[i]);
			}, null, null, null, null);
		}
		if (bodyWithState != null)
		{
			return ForWorker<object, int>(lowerBound, toExclusive, parallelOptions, null, delegate(int i, ParallelLoopState state)
			{
				bodyWithState(array[i], state);
			}, null, null, null);
		}
		if (bodyWithStateAndIndex != null)
		{
			return ForWorker<object, int>(lowerBound, toExclusive, parallelOptions, null, delegate(int i, ParallelLoopState state)
			{
				bodyWithStateAndIndex(array[i], state, i);
			}, null, null, null);
		}
		if (bodyWithStateAndLocal != null)
		{
			return ForWorker(lowerBound, toExclusive, parallelOptions, null, null, (int i, ParallelLoopState state, TLocal local) => bodyWithStateAndLocal(array[i], state, local), localInit, localFinally);
		}
		return ForWorker(lowerBound, toExclusive, parallelOptions, null, null, (int i, ParallelLoopState state, TLocal local) => bodyWithEverything(array[i], state, i, local), localInit, localFinally);
	}

	private static ParallelLoopResult ForEachWorker<TSource, TLocal>(IList<TSource> list, ParallelOptions parallelOptions, Action<TSource> body, Action<TSource, ParallelLoopState> bodyWithState, Action<TSource, ParallelLoopState, long> bodyWithStateAndIndex, Func<TSource, ParallelLoopState, TLocal, TLocal> bodyWithStateAndLocal, Func<TSource, ParallelLoopState, long, TLocal, TLocal> bodyWithEverything, Func<TLocal> localInit, Action<TLocal> localFinally)
	{
		if (body != null)
		{
			return ForWorker<object, int>(0, list.Count, parallelOptions, delegate(int i)
			{
				body(list[i]);
			}, null, null, null, null);
		}
		if (bodyWithState != null)
		{
			return ForWorker<object, int>(0, list.Count, parallelOptions, null, delegate(int i, ParallelLoopState state)
			{
				bodyWithState(list[i], state);
			}, null, null, null);
		}
		if (bodyWithStateAndIndex != null)
		{
			return ForWorker<object, int>(0, list.Count, parallelOptions, null, delegate(int i, ParallelLoopState state)
			{
				bodyWithStateAndIndex(list[i], state, i);
			}, null, null, null);
		}
		if (bodyWithStateAndLocal != null)
		{
			return ForWorker(0, list.Count, parallelOptions, null, null, (int i, ParallelLoopState state, TLocal local) => bodyWithStateAndLocal(list[i], state, local), localInit, localFinally);
		}
		return ForWorker(0, list.Count, parallelOptions, null, null, (int i, ParallelLoopState state, TLocal local) => bodyWithEverything(list[i], state, i, local), localInit, localFinally);
	}

	public static ParallelLoopResult ForEach<TSource>(Partitioner<TSource> source, Action<TSource> body)
	{
		ArgumentNullException.ThrowIfNull(source, "source");
		ArgumentNullException.ThrowIfNull(body, "body");
		return PartitionerForEachWorker<TSource, object>(source, s_defaultParallelOptions, body, null, null, null, null, null, null);
	}

	public static ParallelLoopResult ForEach<TSource>(Partitioner<TSource> source, Action<TSource, ParallelLoopState> body)
	{
		ArgumentNullException.ThrowIfNull(source, "source");
		ArgumentNullException.ThrowIfNull(body, "body");
		return PartitionerForEachWorker<TSource, object>(source, s_defaultParallelOptions, null, body, null, null, null, null, null);
	}

	public static ParallelLoopResult ForEach<TSource>(OrderablePartitioner<TSource> source, Action<TSource, ParallelLoopState, long> body)
	{
		ArgumentNullException.ThrowIfNull(source, "source");
		ArgumentNullException.ThrowIfNull(body, "body");
		if (!source.KeysNormalized)
		{
			throw new InvalidOperationException(System.SR.Parallel_ForEach_OrderedPartitionerKeysNotNormalized);
		}
		return PartitionerForEachWorker<TSource, object>(source, s_defaultParallelOptions, null, null, body, null, null, null, null);
	}

	public static ParallelLoopResult ForEach<TSource, TLocal>(Partitioner<TSource> source, Func<TLocal> localInit, Func<TSource, ParallelLoopState, TLocal, TLocal> body, Action<TLocal> localFinally)
	{
		ArgumentNullException.ThrowIfNull(source, "source");
		ArgumentNullException.ThrowIfNull(localInit, "localInit");
		ArgumentNullException.ThrowIfNull(body, "body");
		ArgumentNullException.ThrowIfNull(localFinally, "localFinally");
		return PartitionerForEachWorker(source, s_defaultParallelOptions, null, null, null, body, null, localInit, localFinally);
	}

	public static ParallelLoopResult ForEach<TSource, TLocal>(OrderablePartitioner<TSource> source, Func<TLocal> localInit, Func<TSource, ParallelLoopState, long, TLocal, TLocal> body, Action<TLocal> localFinally)
	{
		ArgumentNullException.ThrowIfNull(source, "source");
		ArgumentNullException.ThrowIfNull(localInit, "localInit");
		ArgumentNullException.ThrowIfNull(body, "body");
		ArgumentNullException.ThrowIfNull(localFinally, "localFinally");
		if (!source.KeysNormalized)
		{
			throw new InvalidOperationException(System.SR.Parallel_ForEach_OrderedPartitionerKeysNotNormalized);
		}
		return PartitionerForEachWorker(source, s_defaultParallelOptions, null, null, null, null, body, localInit, localFinally);
	}

	public static ParallelLoopResult ForEach<TSource>(Partitioner<TSource> source, ParallelOptions parallelOptions, Action<TSource> body)
	{
		ArgumentNullException.ThrowIfNull(source, "source");
		ArgumentNullException.ThrowIfNull(parallelOptions, "parallelOptions");
		ArgumentNullException.ThrowIfNull(body, "body");
		return PartitionerForEachWorker<TSource, object>(source, parallelOptions, body, null, null, null, null, null, null);
	}

	public static ParallelLoopResult ForEach<TSource>(Partitioner<TSource> source, ParallelOptions parallelOptions, Action<TSource, ParallelLoopState> body)
	{
		ArgumentNullException.ThrowIfNull(source, "source");
		ArgumentNullException.ThrowIfNull(parallelOptions, "parallelOptions");
		ArgumentNullException.ThrowIfNull(body, "body");
		return PartitionerForEachWorker<TSource, object>(source, parallelOptions, null, body, null, null, null, null, null);
	}

	public static ParallelLoopResult ForEach<TSource>(OrderablePartitioner<TSource> source, ParallelOptions parallelOptions, Action<TSource, ParallelLoopState, long> body)
	{
		ArgumentNullException.ThrowIfNull(source, "source");
		ArgumentNullException.ThrowIfNull(parallelOptions, "parallelOptions");
		ArgumentNullException.ThrowIfNull(body, "body");
		if (!source.KeysNormalized)
		{
			throw new InvalidOperationException(System.SR.Parallel_ForEach_OrderedPartitionerKeysNotNormalized);
		}
		return PartitionerForEachWorker<TSource, object>(source, parallelOptions, null, null, body, null, null, null, null);
	}

	public static ParallelLoopResult ForEach<TSource, TLocal>(Partitioner<TSource> source, ParallelOptions parallelOptions, Func<TLocal> localInit, Func<TSource, ParallelLoopState, TLocal, TLocal> body, Action<TLocal> localFinally)
	{
		ArgumentNullException.ThrowIfNull(source, "source");
		ArgumentNullException.ThrowIfNull(parallelOptions, "parallelOptions");
		ArgumentNullException.ThrowIfNull(localInit, "localInit");
		ArgumentNullException.ThrowIfNull(body, "body");
		ArgumentNullException.ThrowIfNull(localFinally, "localFinally");
		return PartitionerForEachWorker(source, parallelOptions, null, null, null, body, null, localInit, localFinally);
	}

	public static ParallelLoopResult ForEach<TSource, TLocal>(OrderablePartitioner<TSource> source, ParallelOptions parallelOptions, Func<TLocal> localInit, Func<TSource, ParallelLoopState, long, TLocal, TLocal> body, Action<TLocal> localFinally)
	{
		ArgumentNullException.ThrowIfNull(source, "source");
		ArgumentNullException.ThrowIfNull(parallelOptions, "parallelOptions");
		ArgumentNullException.ThrowIfNull(localInit, "localInit");
		ArgumentNullException.ThrowIfNull(body, "body");
		ArgumentNullException.ThrowIfNull(localFinally, "localFinally");
		if (!source.KeysNormalized)
		{
			throw new InvalidOperationException(System.SR.Parallel_ForEach_OrderedPartitionerKeysNotNormalized);
		}
		return PartitionerForEachWorker(source, parallelOptions, null, null, null, null, body, localInit, localFinally);
	}

	private static ParallelLoopResult PartitionerForEachWorker<TSource, TLocal>(Partitioner<TSource> source, ParallelOptions parallelOptions, Action<TSource> simpleBody, Action<TSource, ParallelLoopState> bodyWithState, Action<TSource, ParallelLoopState, long> bodyWithStateAndIndex, Func<TSource, ParallelLoopState, TLocal, TLocal> bodyWithStateAndLocal, Func<TSource, ParallelLoopState, long, TLocal, TLocal> bodyWithEverything, Func<TLocal> localInit, Action<TLocal> localFinally)
	{
		OrderablePartitioner<TSource> orderedSource = source as OrderablePartitioner<TSource>;
		if (!source.SupportsDynamicPartitions)
		{
			throw new InvalidOperationException(System.SR.Parallel_ForEach_PartitionerNotDynamic);
		}
		parallelOptions.CancellationToken.ThrowIfCancellationRequested();
		int forkJoinContextID = 0;
		if (ParallelEtwProvider.Log.IsEnabled())
		{
			forkJoinContextID = Interlocked.Increment(ref s_forkJoinContextID);
			ParallelEtwProvider.Log.ParallelLoopBegin(TaskScheduler.Current.Id, Task.CurrentId.GetValueOrDefault(), forkJoinContextID, ParallelEtwProvider.ForkJoinOperationType.ParallelForEach, 0L, 0L);
		}
		ParallelLoopStateFlags<long> sharedPStateFlags = new ParallelLoopStateFlags<long>();
		ParallelLoopResult result = default(ParallelLoopResult);
		OperationCanceledException oce = null;
		CancellationTokenRegistration cancellationTokenRegistration = ((!parallelOptions.CancellationToken.CanBeCanceled) ? default(CancellationTokenRegistration) : parallelOptions.CancellationToken.UnsafeRegister((Action<object?>)delegate
		{
			oce = new OperationCanceledException(parallelOptions.CancellationToken);
			sharedPStateFlags.Cancel();
		}, (object?)null));
		IEnumerable<TSource> partitionerSource = null;
		IEnumerable<KeyValuePair<long, TSource>> orderablePartitionerSource = null;
		if (orderedSource != null)
		{
			orderablePartitionerSource = orderedSource.GetOrderableDynamicPartitions();
			if (orderablePartitionerSource == null)
			{
				throw new InvalidOperationException(System.SR.Parallel_ForEach_PartitionerReturnedNull);
			}
		}
		else
		{
			partitionerSource = source.GetDynamicPartitions();
			if (partitionerSource == null)
			{
				throw new InvalidOperationException(System.SR.Parallel_ForEach_PartitionerReturnedNull);
			}
		}
		try
		{
			try
			{
				TaskReplicator.Run(delegate(ref IEnumerator partitionState, long timeout, out bool replicationDelegateYieldedBeforeCompletion)
				{
					replicationDelegateYieldedBeforeCompletion = false;
					if (ParallelEtwProvider.Log.IsEnabled())
					{
						ParallelEtwProvider.Log.ParallelFork(TaskScheduler.Current.Id, Task.CurrentId.GetValueOrDefault(), forkJoinContextID);
					}
					TLocal val = default(TLocal);
					bool flag = false;
					try
					{
						ParallelLoopState<long> parallelLoopState = null;
						if (bodyWithState != null || bodyWithStateAndIndex != null)
						{
							parallelLoopState = new ParallelLoopState<long>(sharedPStateFlags);
						}
						else if (bodyWithStateAndLocal != null || bodyWithEverything != null)
						{
							parallelLoopState = new ParallelLoopState<long>(sharedPStateFlags);
							if (localInit != null)
							{
								val = localInit();
								flag = true;
							}
						}
						long timeoutOccursAt = ComputeTimeoutPoint(timeout);
						if (orderedSource != null)
						{
							IEnumerator<KeyValuePair<long, TSource>> enumerator = partitionState as IEnumerator<KeyValuePair<long, TSource>>;
							if (enumerator == null)
							{
								enumerator = (IEnumerator<KeyValuePair<long, TSource>>)(partitionState = orderablePartitionerSource.GetEnumerator());
							}
							if (enumerator == null)
							{
								throw new InvalidOperationException(System.SR.Parallel_ForEach_NullEnumerator);
							}
							while (enumerator.MoveNext())
							{
								KeyValuePair<long, TSource> current = enumerator.Current;
								long key = current.Key;
								TSource value = current.Value;
								if (parallelLoopState != null)
								{
									parallelLoopState.CurrentIteration = key;
								}
								if (simpleBody != null)
								{
									simpleBody(value);
								}
								else if (bodyWithState != null)
								{
									bodyWithState(value, parallelLoopState);
								}
								else if (bodyWithStateAndIndex == null)
								{
									val = ((bodyWithStateAndLocal == null) ? bodyWithEverything(value, parallelLoopState, key, val) : bodyWithStateAndLocal(value, parallelLoopState, val));
								}
								else
								{
									bodyWithStateAndIndex(value, parallelLoopState, key);
								}
								if (sharedPStateFlags.ShouldExitLoop(key))
								{
									break;
								}
								if (CheckTimeoutReached(timeoutOccursAt))
								{
									replicationDelegateYieldedBeforeCompletion = true;
									break;
								}
							}
						}
						else
						{
							IEnumerator<TSource> enumerator2 = partitionState as IEnumerator<TSource>;
							if (enumerator2 == null)
							{
								enumerator2 = (IEnumerator<TSource>)(partitionState = partitionerSource.GetEnumerator());
							}
							if (enumerator2 == null)
							{
								throw new InvalidOperationException(System.SR.Parallel_ForEach_NullEnumerator);
							}
							if (parallelLoopState != null)
							{
								parallelLoopState.CurrentIteration = 0L;
							}
							while (enumerator2.MoveNext())
							{
								TSource current2 = enumerator2.Current;
								if (simpleBody != null)
								{
									simpleBody(current2);
								}
								else if (bodyWithState != null)
								{
									bodyWithState(current2, parallelLoopState);
								}
								else if (bodyWithStateAndLocal != null)
								{
									val = bodyWithStateAndLocal(current2, parallelLoopState, val);
								}
								if (sharedPStateFlags.LoopStateFlags != 0)
								{
									break;
								}
								if (CheckTimeoutReached(timeoutOccursAt))
								{
									replicationDelegateYieldedBeforeCompletion = true;
									break;
								}
							}
						}
					}
					catch (Exception source2)
					{
						sharedPStateFlags.SetExceptional();
						ExceptionDispatchInfo.Throw(source2);
					}
					finally
					{
						if (localFinally != null && flag)
						{
							localFinally(val);
						}
						if (!replicationDelegateYieldedBeforeCompletion && partitionState is IDisposable disposable2)
						{
							disposable2.Dispose();
						}
						if (ParallelEtwProvider.Log.IsEnabled())
						{
							ParallelEtwProvider.Log.ParallelJoin(TaskScheduler.Current.Id, Task.CurrentId.GetValueOrDefault(), forkJoinContextID);
						}
					}
				}, parallelOptions, stopOnFirstFailure: true);
			}
			finally
			{
				if (parallelOptions.CancellationToken.CanBeCanceled)
				{
					cancellationTokenRegistration.Dispose();
				}
			}
			if (oce != null)
			{
				throw oce;
			}
		}
		catch (AggregateException ex)
		{
			ThrowSingleCancellationExceptionOrOtherException(ex.InnerExceptions, parallelOptions.CancellationToken, ex);
		}
		finally
		{
			int loopStateFlags = sharedPStateFlags.LoopStateFlags;
			result._completed = loopStateFlags == 0;
			if (((uint)loopStateFlags & 2u) != 0)
			{
				result._lowestBreakIteration = sharedPStateFlags.LowestBreakIteration;
			}
			IDisposable disposable = null;
			((orderablePartitionerSource == null) ? (partitionerSource as IDisposable) : (orderablePartitionerSource as IDisposable))?.Dispose();
			if (ParallelEtwProvider.Log.IsEnabled())
			{
				ParallelEtwProvider.Log.ParallelLoopEnd(TaskScheduler.Current.Id, Task.CurrentId.GetValueOrDefault(), forkJoinContextID, 0L);
			}
		}
		return result;
	}

	private static OperationCanceledException ReduceToSingleCancellationException(ICollection exceptions, CancellationToken cancelToken)
	{
		if (exceptions == null || exceptions.Count == 0)
		{
			return null;
		}
		if (!cancelToken.IsCancellationRequested)
		{
			return null;
		}
		Exception ex = null;
		foreach (object exception in exceptions)
		{
			Exception ex2 = (Exception)exception;
			if (ex == null)
			{
				ex = ex2;
			}
			if (!(ex2 is OperationCanceledException ex3) || !cancelToken.Equals(ex3.CancellationToken))
			{
				return null;
			}
		}
		return (OperationCanceledException)ex;
	}

	private static void ThrowSingleCancellationExceptionOrOtherException(ICollection exceptions, CancellationToken cancelToken, Exception otherException)
	{
		OperationCanceledException ex = ReduceToSingleCancellationException(exceptions, cancelToken);
		ExceptionDispatchInfo.Throw(ex ?? otherException);
	}

	public static Task ForAsync<T>(T fromInclusive, T toExclusive, Func<T, CancellationToken, ValueTask> body) where T : notnull, IBinaryInteger<T>
	{
		if (fromInclusive == null)
		{
			throw new ArgumentNullException("fromInclusive");
		}
		if (toExclusive == null)
		{
			throw new ArgumentNullException("toExclusive");
		}
		ArgumentNullException.ThrowIfNull(body, "body");
		return ForAsync(fromInclusive, toExclusive, DefaultDegreeOfParallelism, TaskScheduler.Default, default(CancellationToken), body);
	}

	public static Task ForAsync<T>(T fromInclusive, T toExclusive, CancellationToken cancellationToken, Func<T, CancellationToken, ValueTask> body) where T : notnull, IBinaryInteger<T>
	{
		if (fromInclusive == null)
		{
			throw new ArgumentNullException("fromInclusive");
		}
		if (toExclusive == null)
		{
			throw new ArgumentNullException("toExclusive");
		}
		ArgumentNullException.ThrowIfNull(body, "body");
		return ForAsync(fromInclusive, toExclusive, DefaultDegreeOfParallelism, TaskScheduler.Default, cancellationToken, body);
	}

	public static Task ForAsync<T>(T fromInclusive, T toExclusive, ParallelOptions parallelOptions, Func<T, CancellationToken, ValueTask> body) where T : notnull, IBinaryInteger<T>
	{
		if (fromInclusive == null)
		{
			throw new ArgumentNullException("fromInclusive");
		}
		if (toExclusive == null)
		{
			throw new ArgumentNullException("toExclusive");
		}
		ArgumentNullException.ThrowIfNull(parallelOptions, "parallelOptions");
		ArgumentNullException.ThrowIfNull(body, "body");
		return ForAsync(fromInclusive, toExclusive, parallelOptions.EffectiveMaxConcurrencyLevel, parallelOptions.EffectiveTaskScheduler, parallelOptions.CancellationToken, body);
	}

	private static Task ForAsync<T>(T fromInclusive, T toExclusive, int dop, TaskScheduler scheduler, CancellationToken cancellationToken, Func<T, CancellationToken, ValueTask> body) where T : IBinaryInteger<T>
	{
		if (cancellationToken.IsCancellationRequested)
		{
			return Task.FromCanceled(cancellationToken);
		}
		if (fromInclusive >= toExclusive)
		{
			return Task.CompletedTask;
		}
		Func<object, Task> taskBody = async delegate(object o)
		{
			ForEachState<T> state = (ForEachState<T>)o;
			bool launchedNext = false;
			try
			{
				while (!state.Cancellation.IsCancellationRequested)
				{
					T nextAvailable;
					if (Interlockable())
					{
						do
						{
							nextAvailable = state.NextAvailable;
							if (nextAvailable >= state.ToExclusive)
							{
								return;
							}
						}
						while (!CompareExchange(ref state.NextAvailable, nextAvailable + T.One, nextAvailable));
					}
					else
					{
						await state.AcquireLock();
						try
						{
							if (state.Cancellation.IsCancellationRequested || state.NextAvailable >= state.ToExclusive)
							{
								break;
							}
							nextAvailable = state.NextAvailable;
							++state.NextAvailable;
						}
						finally
						{
							state.ReleaseLock();
						}
					}
					if (!launchedNext)
					{
						launchedNext = true;
						state.QueueWorkerIfDopAvailable();
					}
					await state.LoopBody(nextAvailable, state.Cancellation.Token);
				}
			}
			catch (Exception e)
			{
				state.RecordException(e);
			}
			finally
			{
				if (state.SignalWorkerCompletedIterating())
				{
					state.Dispose();
					state.Complete();
				}
			}
		};
		try
		{
			ForEachState<T> forEachState = new ForEachState<T>(fromInclusive, toExclusive, taskBody, !Interlockable(), dop, scheduler, cancellationToken, body);
			forEachState.QueueWorkerIfDopAvailable();
			return forEachState.Task;
		}
		catch (Exception exception)
		{
			return Task.FromException(exception);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		static bool CompareExchange(ref T location, T value, T comparand)
		{
			if (!(typeof(T) == typeof(int)))
			{
				if (!(typeof(T) == typeof(uint)))
				{
					if (!(typeof(T) == typeof(long)))
					{
						if (!(typeof(T) == typeof(ulong)))
						{
							if (!(typeof(T) == typeof(nint)))
							{
								if (!(typeof(T) == typeof(nuint)))
								{
									throw new UnreachableException();
								}
								return Interlocked.CompareExchange(ref Unsafe.As<T, nuint>(ref location), Unsafe.As<T, nuint>(ref value), Unsafe.As<T, nuint>(ref comparand)) == Unsafe.As<T, nuint>(ref comparand);
							}
							return Interlocked.CompareExchange(ref Unsafe.As<T, nint>(ref location), Unsafe.As<T, nint>(ref value), Unsafe.As<T, nint>(ref comparand)) == Unsafe.As<T, nint>(ref comparand);
						}
						return Interlocked.CompareExchange(ref Unsafe.As<T, ulong>(ref location), Unsafe.As<T, ulong>(ref value), Unsafe.As<T, ulong>(ref comparand)) == Unsafe.As<T, ulong>(ref comparand);
					}
					return Interlocked.CompareExchange(ref Unsafe.As<T, long>(ref location), Unsafe.As<T, long>(ref value), Unsafe.As<T, long>(ref comparand)) == Unsafe.As<T, long>(ref comparand);
				}
				return Interlocked.CompareExchange(ref Unsafe.As<T, uint>(ref location), Unsafe.As<T, uint>(ref value), Unsafe.As<T, uint>(ref comparand)) == Unsafe.As<T, uint>(ref comparand);
			}
			return Interlocked.CompareExchange(ref Unsafe.As<T, int>(ref location), Unsafe.As<T, int>(ref value), Unsafe.As<T, int>(ref comparand)) == Unsafe.As<T, int>(ref comparand);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		static bool Interlockable()
		{
			if (!(typeof(T) == typeof(int)) && !(typeof(T) == typeof(uint)) && !(typeof(T) == typeof(long)) && !(typeof(T) == typeof(ulong)) && !(typeof(T) == typeof(nint)))
			{
				return typeof(T) == typeof(nuint);
			}
			return true;
		}
	}

	public static Task ForEachAsync<TSource>(IEnumerable<TSource> source, Func<TSource, CancellationToken, ValueTask> body)
	{
		ArgumentNullException.ThrowIfNull(source, "source");
		ArgumentNullException.ThrowIfNull(body, "body");
		return ForEachAsync(source, DefaultDegreeOfParallelism, TaskScheduler.Default, default(CancellationToken), body);
	}

	public static Task ForEachAsync<TSource>(IEnumerable<TSource> source, CancellationToken cancellationToken, Func<TSource, CancellationToken, ValueTask> body)
	{
		ArgumentNullException.ThrowIfNull(source, "source");
		ArgumentNullException.ThrowIfNull(body, "body");
		return ForEachAsync(source, DefaultDegreeOfParallelism, TaskScheduler.Default, cancellationToken, body);
	}

	public static Task ForEachAsync<TSource>(IEnumerable<TSource> source, ParallelOptions parallelOptions, Func<TSource, CancellationToken, ValueTask> body)
	{
		ArgumentNullException.ThrowIfNull(source, "source");
		ArgumentNullException.ThrowIfNull(parallelOptions, "parallelOptions");
		ArgumentNullException.ThrowIfNull(body, "body");
		return ForEachAsync(source, parallelOptions.EffectiveMaxConcurrencyLevel, parallelOptions.EffectiveTaskScheduler, parallelOptions.CancellationToken, body);
	}

	private static Task ForEachAsync<TSource>(IEnumerable<TSource> source, int dop, TaskScheduler scheduler, CancellationToken cancellationToken, Func<TSource, CancellationToken, ValueTask> body)
	{
		if (cancellationToken.IsCancellationRequested)
		{
			return Task.FromCanceled(cancellationToken);
		}
		Func<object, Task> taskBody = async delegate(object o)
		{
			SyncForEachAsyncState<TSource> state = (SyncForEachAsyncState<TSource>)o;
			bool launchedNext = false;
			try
			{
				while (!state.Cancellation.IsCancellationRequested)
				{
					await state.AcquireLock();
					TSource current;
					try
					{
						if (state.Cancellation.IsCancellationRequested || !state.Enumerator.MoveNext())
						{
							break;
						}
						current = state.Enumerator.Current;
					}
					finally
					{
						state.ReleaseLock();
					}
					if (!launchedNext)
					{
						launchedNext = true;
						state.QueueWorkerIfDopAvailable();
					}
					await state.LoopBody(current, state.Cancellation.Token);
				}
			}
			catch (Exception e)
			{
				state.RecordException(e);
			}
			finally
			{
				if (state.SignalWorkerCompletedIterating())
				{
					try
					{
						state.Dispose();
					}
					catch (Exception e2)
					{
						state.RecordException(e2);
					}
					state.Complete();
				}
			}
		};
		try
		{
			SyncForEachAsyncState<TSource> syncForEachAsyncState = new SyncForEachAsyncState<TSource>(source, taskBody, dop, scheduler, cancellationToken, body);
			syncForEachAsyncState.QueueWorkerIfDopAvailable();
			return syncForEachAsyncState.Task;
		}
		catch (Exception exception)
		{
			return Task.FromException(exception);
		}
	}

	public static Task ForEachAsync<TSource>(IAsyncEnumerable<TSource> source, Func<TSource, CancellationToken, ValueTask> body)
	{
		ArgumentNullException.ThrowIfNull(source, "source");
		ArgumentNullException.ThrowIfNull(body, "body");
		return ForEachAsync(source, DefaultDegreeOfParallelism, TaskScheduler.Default, default(CancellationToken), body);
	}

	public static Task ForEachAsync<TSource>(IAsyncEnumerable<TSource> source, CancellationToken cancellationToken, Func<TSource, CancellationToken, ValueTask> body)
	{
		ArgumentNullException.ThrowIfNull(source, "source");
		ArgumentNullException.ThrowIfNull(body, "body");
		return ForEachAsync(source, DefaultDegreeOfParallelism, TaskScheduler.Default, cancellationToken, body);
	}

	public static Task ForEachAsync<TSource>(IAsyncEnumerable<TSource> source, ParallelOptions parallelOptions, Func<TSource, CancellationToken, ValueTask> body)
	{
		ArgumentNullException.ThrowIfNull(source, "source");
		ArgumentNullException.ThrowIfNull(parallelOptions, "parallelOptions");
		ArgumentNullException.ThrowIfNull(body, "body");
		return ForEachAsync(source, parallelOptions.EffectiveMaxConcurrencyLevel, parallelOptions.EffectiveTaskScheduler, parallelOptions.CancellationToken, body);
	}

	private static Task ForEachAsync<TSource>(IAsyncEnumerable<TSource> source, int dop, TaskScheduler scheduler, CancellationToken cancellationToken, Func<TSource, CancellationToken, ValueTask> body)
	{
		if (cancellationToken.IsCancellationRequested)
		{
			return Task.FromCanceled(cancellationToken);
		}
		Func<object, Task> taskBody = async delegate(object o)
		{
			AsyncForEachAsyncState<TSource> state = (AsyncForEachAsyncState<TSource>)o;
			bool launchedNext = false;
			try
			{
				_ = 2;
				try
				{
					while (!state.Cancellation.IsCancellationRequested)
					{
						await state.AcquireLock();
						TSource current;
						try
						{
							bool isCancellationRequested = state.Cancellation.IsCancellationRequested;
							bool flag = isCancellationRequested;
							if (!flag)
							{
								flag = !(await state.Enumerator.MoveNextAsync());
							}
							if (!flag)
							{
								current = state.Enumerator.Current;
								goto IL_0173;
							}
						}
						finally
						{
							state.ReleaseLock();
						}
						break;
						IL_0173:
						if (!launchedNext)
						{
							launchedNext = true;
							state.QueueWorkerIfDopAvailable();
						}
						await state.LoopBody(current, state.Cancellation.Token);
					}
				}
				catch (Exception e)
				{
					state.RecordException(e);
				}
			}
			finally
			{
				if (state.SignalWorkerCompletedIterating())
				{
					try
					{
						await state.DisposeAsync();
					}
					catch (Exception e2)
					{
						state.RecordException(e2);
					}
					state.Complete();
				}
			}
		};
		try
		{
			AsyncForEachAsyncState<TSource> asyncForEachAsyncState = new AsyncForEachAsyncState<TSource>(source, taskBody, dop, scheduler, cancellationToken, body);
			asyncForEachAsyncState.QueueWorkerIfDopAvailable();
			return asyncForEachAsyncState.Task;
		}
		catch (Exception exception)
		{
			return Task.FromException(exception);
		}
	}
}
