using System.Threading.Tasks;

namespace System.Threading;

internal sealed class TimerHolder
{
	internal readonly TimerQueueTimer _timer;

	public TimerHolder(TimerQueueTimer timer)
	{
		_timer = timer;
	}

	~TimerHolder()
	{
		_timer.Dispose();
	}

	public void Dispose()
	{
		_timer.Dispose();
		GC.SuppressFinalize(this);
	}

	public bool Dispose(WaitHandle notifyObject)
	{
		bool result = _timer.Dispose(notifyObject);
		GC.SuppressFinalize(this);
		return result;
	}

	public ValueTask DisposeAsync()
	{
		ValueTask result = _timer.DisposeAsync();
		GC.SuppressFinalize(this);
		return result;
	}
}
