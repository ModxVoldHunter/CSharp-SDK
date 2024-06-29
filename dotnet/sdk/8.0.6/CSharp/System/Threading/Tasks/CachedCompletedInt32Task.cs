using System.Runtime.CompilerServices;

namespace System.Threading.Tasks;

internal struct CachedCompletedInt32Task
{
	private Task<int> _task;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Task<int> GetTask(int result)
	{
		Task<int> task = _task;
		if (task != null && task.Result == result)
		{
			return task;
		}
		return _task = Task.FromResult(result);
	}
}
