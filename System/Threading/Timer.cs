using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

namespace System.Threading;

[DebuggerDisplay("{DisplayString,nq}")]
[DebuggerTypeProxy(typeof(TimerQueueTimer.TimerDebuggerTypeProxy))]
public sealed class Timer : MarshalByRefObject, IDisposable, IAsyncDisposable, ITimer
{
	internal TimerHolder _timer;

	public static long ActiveCount
	{
		get
		{
			long num = 0L;
			TimerQueue[] instances = TimerQueue.Instances;
			foreach (TimerQueue timerQueue in instances)
			{
				lock (timerQueue)
				{
					num += timerQueue.ActiveCount;
				}
			}
			return num;
		}
	}

	private string DisplayString => _timer._timer.DisplayString;

	private static IEnumerable<TimerQueueTimer> AllTimers
	{
		get
		{
			List<TimerQueueTimer> list = new List<TimerQueueTimer>();
			TimerQueue[] instances = TimerQueue.Instances;
			foreach (TimerQueue timerQueue in instances)
			{
				list.AddRange(timerQueue.GetTimersForDebugger());
			}
			list.Sort((TimerQueueTimer t1, TimerQueueTimer t2) => t1._dueTime.CompareTo(t2._dueTime));
			return list;
		}
	}

	public Timer(TimerCallback callback, object? state, int dueTime, int period)
		: this(callback, state, dueTime, period, flowExecutionContext: true)
	{
	}

	internal Timer(TimerCallback callback, object state, int dueTime, int period, bool flowExecutionContext)
	{
		ArgumentOutOfRangeException.ThrowIfLessThan(dueTime, -1, "dueTime");
		ArgumentOutOfRangeException.ThrowIfLessThan(period, -1, "period");
		TimerSetup(callback, state, (uint)dueTime, (uint)period, flowExecutionContext);
	}

	public Timer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
	{
		long num = (long)dueTime.TotalMilliseconds;
		ArgumentOutOfRangeException.ThrowIfLessThan(num, -1L, "dueTime");
		ArgumentOutOfRangeException.ThrowIfGreaterThan(num, 4294967294L, "dueTime");
		long num2 = (long)period.TotalMilliseconds;
		ArgumentOutOfRangeException.ThrowIfLessThan(num2, -1L, "period");
		ArgumentOutOfRangeException.ThrowIfGreaterThan(num2, 4294967294L, "period");
		TimerSetup(callback, state, (uint)num, (uint)num2);
	}

	[CLSCompliant(false)]
	public Timer(TimerCallback callback, object? state, uint dueTime, uint period)
	{
		TimerSetup(callback, state, dueTime, period);
	}

	public Timer(TimerCallback callback, object? state, long dueTime, long period)
	{
		ArgumentOutOfRangeException.ThrowIfLessThan(dueTime, -1L, "dueTime");
		ArgumentOutOfRangeException.ThrowIfLessThan(period, -1L, "period");
		ArgumentOutOfRangeException.ThrowIfGreaterThan(dueTime, 4294967294L, "dueTime");
		ArgumentOutOfRangeException.ThrowIfGreaterThan(period, 4294967294L, "period");
		TimerSetup(callback, state, (uint)dueTime, (uint)period);
	}

	public Timer(TimerCallback callback)
	{
		TimerSetup(callback, this, uint.MaxValue, uint.MaxValue);
	}

	[MemberNotNull("_timer")]
	private void TimerSetup(TimerCallback callback, object state, uint dueTime, uint period, bool flowExecutionContext = true)
	{
		ArgumentNullException.ThrowIfNull(callback, "callback");
		_timer = new TimerHolder(new TimerQueueTimer(callback, state, dueTime, period, flowExecutionContext));
	}

	public bool Change(int dueTime, int period)
	{
		ArgumentOutOfRangeException.ThrowIfLessThan(dueTime, -1, "dueTime");
		ArgumentOutOfRangeException.ThrowIfLessThan(period, -1, "period");
		return _timer._timer.Change((uint)dueTime, (uint)period);
	}

	public bool Change(TimeSpan dueTime, TimeSpan period)
	{
		return _timer._timer.Change(dueTime, period);
	}

	[CLSCompliant(false)]
	public bool Change(uint dueTime, uint period)
	{
		return _timer._timer.Change(dueTime, period);
	}

	public bool Change(long dueTime, long period)
	{
		ArgumentOutOfRangeException.ThrowIfLessThan(dueTime, -1L, "dueTime");
		ArgumentOutOfRangeException.ThrowIfLessThan(period, -1L, "period");
		ArgumentOutOfRangeException.ThrowIfGreaterThan(dueTime, 4294967294L, "dueTime");
		ArgumentOutOfRangeException.ThrowIfGreaterThan(period, 4294967294L, "period");
		return _timer._timer.Change((uint)dueTime, (uint)period);
	}

	public bool Dispose(WaitHandle notifyObject)
	{
		ArgumentNullException.ThrowIfNull(notifyObject, "notifyObject");
		return _timer.Dispose(notifyObject);
	}

	public void Dispose()
	{
		_timer.Dispose();
	}

	public ValueTask DisposeAsync()
	{
		return _timer.DisposeAsync();
	}
}
