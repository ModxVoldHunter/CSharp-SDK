namespace System.Threading.Tasks.Sources;

internal sealed class CapturedSchedulerAndExecutionContext
{
	internal readonly object _scheduler;

	internal readonly ExecutionContext _executionContext;

	public CapturedSchedulerAndExecutionContext(object scheduler, ExecutionContext executionContext)
	{
		_scheduler = scheduler;
		_executionContext = executionContext;
	}
}
