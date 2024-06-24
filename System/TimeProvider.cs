using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace System;

public abstract class TimeProvider
{
	private sealed class SystemTimeProviderTimer : ITimer, IDisposable, IAsyncDisposable
	{
		private readonly TimerQueueTimer _timer;

		public SystemTimeProviderTimer(TimeSpan dueTime, TimeSpan period, TimerCallback callback, object state)
		{
			_timer = new TimerQueueTimer(callback, state, dueTime, period, flowExecutionContext: true);
		}

		public bool Change(TimeSpan dueTime, TimeSpan period)
		{
			try
			{
				return _timer.Change(dueTime, period);
			}
			catch (ObjectDisposedException)
			{
				return false;
			}
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

	private sealed class SystemTimeProvider : TimeProvider
	{
		internal SystemTimeProvider()
		{
		}
	}

	private static readonly long s_minDateTicks = DateTime.MinValue.Ticks;

	private static readonly long s_maxDateTicks = DateTime.MaxValue.Ticks;

	public static TimeProvider System { get; } = new SystemTimeProvider();


	public virtual TimeZoneInfo LocalTimeZone => TimeZoneInfo.Local;

	public virtual long TimestampFrequency => Stopwatch.Frequency;

	public virtual DateTimeOffset GetUtcNow()
	{
		return DateTimeOffset.UtcNow;
	}

	public DateTimeOffset GetLocalNow()
	{
		DateTimeOffset utcNow = GetUtcNow();
		TimeZoneInfo localTimeZone = LocalTimeZone;
		if (localTimeZone == null)
		{
			ThrowHelper.ThrowInvalidOperationException(ExceptionResource.InvalidOperation_TimeProviderNullLocalTimeZone);
		}
		TimeSpan utcOffset = localTimeZone.GetUtcOffset(utcNow);
		if (utcOffset.Ticks == 0L)
		{
			return utcNow;
		}
		long num = utcNow.Ticks + utcOffset.Ticks;
		if ((ulong)num > (ulong)s_maxDateTicks)
		{
			num = ((num < s_minDateTicks) ? s_minDateTicks : s_maxDateTicks);
		}
		return new DateTimeOffset(num, utcOffset);
	}

	public virtual long GetTimestamp()
	{
		return Stopwatch.GetTimestamp();
	}

	public TimeSpan GetElapsedTime(long startingTimestamp, long endingTimestamp)
	{
		long timestampFrequency = TimestampFrequency;
		if (timestampFrequency <= 0)
		{
			ThrowHelper.ThrowInvalidOperationException(ExceptionResource.InvalidOperation_TimeProviderInvalidTimestampFrequency);
		}
		return new TimeSpan((long)((double)(endingTimestamp - startingTimestamp) * (10000000.0 / (double)timestampFrequency)));
	}

	public TimeSpan GetElapsedTime(long startingTimestamp)
	{
		return GetElapsedTime(startingTimestamp, GetTimestamp());
	}

	public virtual ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
	{
		ArgumentNullException.ThrowIfNull(callback, "callback");
		return new SystemTimeProviderTimer(dueTime, period, callback, state);
	}
}
