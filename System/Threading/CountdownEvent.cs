using System.Diagnostics;
using System.Runtime.Versioning;

namespace System.Threading;

[DebuggerDisplay("InitialCount = {InitialCount}, CurrentCount = {CurrentCount}")]
public class CountdownEvent : IDisposable
{
	private int _initialCount;

	private volatile int _currentCount;

	private readonly ManualResetEventSlim _event;

	private volatile bool _disposed;

	public int CurrentCount
	{
		get
		{
			int currentCount = _currentCount;
			if (currentCount >= 0)
			{
				return currentCount;
			}
			return 0;
		}
	}

	public int InitialCount => _initialCount;

	public bool IsSet => _currentCount <= 0;

	public WaitHandle WaitHandle
	{
		get
		{
			ObjectDisposedException.ThrowIf(_disposed, this);
			return _event.WaitHandle;
		}
	}

	public CountdownEvent(int initialCount)
	{
		ArgumentOutOfRangeException.ThrowIfNegative(initialCount, "initialCount");
		_initialCount = initialCount;
		_currentCount = initialCount;
		_event = new ManualResetEventSlim();
		if (initialCount == 0)
		{
			_event.Set();
		}
	}

	public void Dispose()
	{
		Dispose(disposing: true);
		GC.SuppressFinalize(this);
	}

	protected virtual void Dispose(bool disposing)
	{
		if (disposing)
		{
			_event.Dispose();
			_disposed = true;
		}
	}

	public bool Signal()
	{
		ObjectDisposedException.ThrowIf(_disposed, this);
		if (_currentCount <= 0)
		{
			throw new InvalidOperationException(System.SR.CountdownEvent_Decrement_BelowZero);
		}
		int num = Interlocked.Decrement(ref _currentCount);
		if (num == 0)
		{
			_event.Set();
			return true;
		}
		if (num < 0)
		{
			throw new InvalidOperationException(System.SR.CountdownEvent_Decrement_BelowZero);
		}
		return false;
	}

	public bool Signal(int signalCount)
	{
		ArgumentOutOfRangeException.ThrowIfNegativeOrZero(signalCount, "signalCount");
		ObjectDisposedException.ThrowIf(_disposed, this);
		SpinWait spinWait = default(SpinWait);
		int currentCount;
		while (true)
		{
			currentCount = _currentCount;
			if (currentCount < signalCount)
			{
				throw new InvalidOperationException(System.SR.CountdownEvent_Decrement_BelowZero);
			}
			if (Interlocked.CompareExchange(ref _currentCount, currentCount - signalCount, currentCount) == currentCount)
			{
				break;
			}
			spinWait.SpinOnce(-1);
		}
		if (currentCount == signalCount)
		{
			_event.Set();
			return true;
		}
		return false;
	}

	public void AddCount()
	{
		AddCount(1);
	}

	public bool TryAddCount()
	{
		return TryAddCount(1);
	}

	public void AddCount(int signalCount)
	{
		if (!TryAddCount(signalCount))
		{
			throw new InvalidOperationException(System.SR.CountdownEvent_Increment_AlreadyZero);
		}
	}

	public bool TryAddCount(int signalCount)
	{
		ArgumentOutOfRangeException.ThrowIfNegativeOrZero(signalCount, "signalCount");
		ObjectDisposedException.ThrowIf(_disposed, this);
		SpinWait spinWait = default(SpinWait);
		while (true)
		{
			int currentCount = _currentCount;
			if (currentCount <= 0)
			{
				return false;
			}
			if (currentCount > int.MaxValue - signalCount)
			{
				throw new InvalidOperationException(System.SR.CountdownEvent_Increment_AlreadyMax);
			}
			if (Interlocked.CompareExchange(ref _currentCount, currentCount + signalCount, currentCount) == currentCount)
			{
				break;
			}
			spinWait.SpinOnce(-1);
		}
		return true;
	}

	public void Reset()
	{
		Reset(_initialCount);
	}

	public void Reset(int count)
	{
		ObjectDisposedException.ThrowIf(_disposed, this);
		ArgumentOutOfRangeException.ThrowIfNegative(count, "count");
		_currentCount = count;
		_initialCount = count;
		if (count == 0)
		{
			_event.Set();
		}
		else
		{
			_event.Reset();
		}
	}

	[UnsupportedOSPlatform("browser")]
	public void Wait()
	{
		Wait(-1, CancellationToken.None);
	}

	[UnsupportedOSPlatform("browser")]
	public void Wait(CancellationToken cancellationToken)
	{
		Wait(-1, cancellationToken);
	}

	[UnsupportedOSPlatform("browser")]
	public bool Wait(TimeSpan timeout)
	{
		long num = (long)timeout.TotalMilliseconds;
		ArgumentOutOfRangeException.ThrowIfLessThan(num, -1L, "timeout");
		ArgumentOutOfRangeException.ThrowIfGreaterThan(num, 2147483647L, "timeout");
		return Wait((int)num, CancellationToken.None);
	}

	[UnsupportedOSPlatform("browser")]
	public bool Wait(TimeSpan timeout, CancellationToken cancellationToken)
	{
		long num = (long)timeout.TotalMilliseconds;
		ArgumentOutOfRangeException.ThrowIfLessThan(num, -1L, "timeout");
		ArgumentOutOfRangeException.ThrowIfGreaterThan(num, 2147483647L, "timeout");
		return Wait((int)num, cancellationToken);
	}

	[UnsupportedOSPlatform("browser")]
	public bool Wait(int millisecondsTimeout)
	{
		return Wait(millisecondsTimeout, CancellationToken.None);
	}

	[UnsupportedOSPlatform("browser")]
	public bool Wait(int millisecondsTimeout, CancellationToken cancellationToken)
	{
		ArgumentOutOfRangeException.ThrowIfLessThan(millisecondsTimeout, -1, "millisecondsTimeout");
		ObjectDisposedException.ThrowIf(_disposed, this);
		cancellationToken.ThrowIfCancellationRequested();
		bool flag = _event.IsSet;
		if (!flag)
		{
			flag = _event.Wait(millisecondsTimeout, cancellationToken);
		}
		return flag;
	}
}
