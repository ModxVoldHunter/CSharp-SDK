namespace System.Threading.Tasks;

public static class TaskExtensions
{
	public static Task Unwrap(this Task<Task> task)
	{
		ArgumentNullException.ThrowIfNull(task, "task");
		object task2;
		if (task.IsCompletedSuccessfully)
		{
			task2 = task.Result;
			if (task2 == null)
			{
				return Task.FromCanceled(new CancellationToken(canceled: true));
			}
		}
		else
		{
			task2 = Task.CreateUnwrapPromise<VoidTaskResult>(task, lookForOce: false);
		}
		return (Task)task2;
	}

	public static Task<TResult> Unwrap<TResult>(this Task<Task<TResult>> task)
	{
		ArgumentNullException.ThrowIfNull(task, "task");
		Task<TResult> task2;
		if (task.IsCompletedSuccessfully)
		{
			task2 = task.Result;
			if (task2 == null)
			{
				return Task.FromCanceled<TResult>(new CancellationToken(canceled: true));
			}
		}
		else
		{
			task2 = Task.CreateUnwrapPromise<TResult>(task, lookForOce: false);
		}
		return task2;
	}
}
