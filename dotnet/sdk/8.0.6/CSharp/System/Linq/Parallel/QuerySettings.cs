using System.Threading;
using System.Threading.Tasks;

namespace System.Linq.Parallel;

internal struct QuerySettings
{
	private TaskScheduler _taskScheduler;

	private int? _degreeOfParallelism;

	private CancellationState _cancellationState;

	private ParallelExecutionMode? _executionMode;

	private ParallelMergeOptions? _mergeOptions;

	private int _queryId;

	internal CancellationState CancellationState
	{
		get
		{
			return _cancellationState;
		}
		set
		{
			_cancellationState = value;
		}
	}

	internal TaskScheduler TaskScheduler
	{
		get
		{
			return _taskScheduler;
		}
		set
		{
			_taskScheduler = value;
		}
	}

	internal int? DegreeOfParallelism
	{
		get
		{
			return _degreeOfParallelism;
		}
		set
		{
			_degreeOfParallelism = value;
		}
	}

	internal ParallelExecutionMode? ExecutionMode
	{
		get
		{
			return _executionMode;
		}
		set
		{
			_executionMode = value;
		}
	}

	internal ParallelMergeOptions? MergeOptions
	{
		get
		{
			return _mergeOptions;
		}
		set
		{
			_mergeOptions = value;
		}
	}

	internal int QueryId => _queryId;

	internal static QuerySettings Empty => new QuerySettings(null, null, CancellationToken.None, null, null);

	internal QuerySettings(TaskScheduler taskScheduler, int? degreeOfParallelism, CancellationToken externalCancellationToken, ParallelExecutionMode? executionMode, ParallelMergeOptions? mergeOptions)
	{
		_taskScheduler = taskScheduler;
		_degreeOfParallelism = degreeOfParallelism;
		_cancellationState = new CancellationState(externalCancellationToken);
		_executionMode = executionMode;
		_mergeOptions = mergeOptions;
		_queryId = -1;
	}

	internal QuerySettings Merge(QuerySettings settings2)
	{
		if (TaskScheduler != null && settings2.TaskScheduler != null)
		{
			throw new InvalidOperationException(System.SR.ParallelQuery_DuplicateTaskScheduler);
		}
		if (DegreeOfParallelism.HasValue && settings2.DegreeOfParallelism.HasValue)
		{
			throw new InvalidOperationException(System.SR.ParallelQuery_DuplicateDOP);
		}
		if (CancellationState.ExternalCancellationToken.CanBeCanceled && settings2.CancellationState.ExternalCancellationToken.CanBeCanceled)
		{
			throw new InvalidOperationException(System.SR.ParallelQuery_DuplicateWithCancellation);
		}
		if (ExecutionMode.HasValue && settings2.ExecutionMode.HasValue)
		{
			throw new InvalidOperationException(System.SR.ParallelQuery_DuplicateExecutionMode);
		}
		if (MergeOptions.HasValue && settings2.MergeOptions.HasValue)
		{
			throw new InvalidOperationException(System.SR.ParallelQuery_DuplicateMergeOptions);
		}
		TaskScheduler taskScheduler = TaskScheduler ?? settings2.TaskScheduler;
		int? degreeOfParallelism = (DegreeOfParallelism.HasValue ? DegreeOfParallelism : settings2.DegreeOfParallelism);
		CancellationToken externalCancellationToken = (CancellationState.ExternalCancellationToken.CanBeCanceled ? CancellationState.ExternalCancellationToken : settings2.CancellationState.ExternalCancellationToken);
		ParallelExecutionMode? executionMode = (ExecutionMode.HasValue ? ExecutionMode : settings2.ExecutionMode);
		ParallelMergeOptions? mergeOptions = (MergeOptions.HasValue ? MergeOptions : settings2.MergeOptions);
		return new QuerySettings(taskScheduler, degreeOfParallelism, externalCancellationToken, executionMode, mergeOptions);
	}

	internal QuerySettings WithPerExecutionSettings()
	{
		return WithPerExecutionSettings(new CancellationTokenSource(), new Shared<bool>(value: false));
	}

	internal QuerySettings WithPerExecutionSettings(CancellationTokenSource topLevelCancellationTokenSource, Shared<bool> topLevelDisposedFlag)
	{
		QuerySettings result = new QuerySettings(TaskScheduler, DegreeOfParallelism, CancellationState.ExternalCancellationToken, ExecutionMode, MergeOptions);
		result.CancellationState.InternalCancellationTokenSource = topLevelCancellationTokenSource;
		result.CancellationState.MergedCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(result.CancellationState.InternalCancellationTokenSource.Token, result.CancellationState.ExternalCancellationToken);
		result.CancellationState.TopLevelDisposedFlag = topLevelDisposedFlag;
		result._queryId = PlinqEtwProvider.NextQueryId();
		return result;
	}

	internal QuerySettings WithDefaults()
	{
		QuerySettings result = this;
		ref QuerySettings reference = ref result;
		if (reference.TaskScheduler == null)
		{
			TaskScheduler taskScheduler = (reference.TaskScheduler = TaskScheduler.Default);
		}
		reference = ref result;
		int? degreeOfParallelism = reference.DegreeOfParallelism;
		int valueOrDefault = degreeOfParallelism.GetValueOrDefault();
		if (!degreeOfParallelism.HasValue)
		{
			valueOrDefault = Scheduling.GetDefaultDegreeOfParallelism();
			ref QuerySettings reference2 = ref reference;
			int? degreeOfParallelism2 = valueOrDefault;
			reference2.DegreeOfParallelism = degreeOfParallelism2;
		}
		reference = ref result;
		ParallelExecutionMode? executionMode = reference.ExecutionMode;
		ParallelExecutionMode valueOrDefault2 = executionMode.GetValueOrDefault();
		if (!executionMode.HasValue)
		{
			valueOrDefault2 = ParallelExecutionMode.Default;
			ref QuerySettings reference3 = ref reference;
			ParallelExecutionMode? executionMode2 = valueOrDefault2;
			reference3.ExecutionMode = executionMode2;
		}
		reference = ref result;
		ParallelMergeOptions? mergeOptions = reference.MergeOptions;
		ParallelMergeOptions valueOrDefault3 = mergeOptions.GetValueOrDefault();
		if (!mergeOptions.HasValue)
		{
			valueOrDefault3 = ParallelMergeOptions.Default;
			ref QuerySettings reference4 = ref reference;
			ParallelMergeOptions? mergeOptions2 = valueOrDefault3;
			reference4.MergeOptions = mergeOptions2;
		}
		if (result.MergeOptions == ParallelMergeOptions.Default)
		{
			result.MergeOptions = ParallelMergeOptions.AutoBuffered;
		}
		return result;
	}

	public void CleanStateAtQueryEnd()
	{
		_cancellationState.MergedCancellationTokenSource.Dispose();
	}
}
