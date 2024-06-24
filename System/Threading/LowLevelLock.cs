namespace System.Threading;

internal sealed class LowLevelLock : IDisposable
{
	private static readonly Func<object, bool> s_spinWaitTryAcquireCallback = SpinWaitTryAcquireCallback;

	private int _state;

	private bool _isAnyWaitingThreadSignaled;

	private LowLevelSpinWaiter _spinWaiter;

	private LowLevelMonitor _monitor;

	public LowLevelLock()
	{
		_spinWaiter = default(LowLevelSpinWaiter);
		_monitor.Initialize();
	}

	~LowLevelLock()
	{
		Dispose();
	}

	public void Dispose()
	{
		_monitor.Dispose();
		GC.SuppressFinalize(this);
	}

	public bool TryAcquire()
	{
		int num = Interlocked.CompareExchange(ref _state, 1, 0);
		if (num == 0 || TryAcquire_NoFastPath(num))
		{
			return true;
		}
		return false;
	}

	private bool TryAcquire_NoFastPath(int state)
	{
		if ((state & 1) == 0)
		{
			return Interlocked.CompareExchange(ref _state, state + 1, state) == state;
		}
		return false;
	}

	private static bool SpinWaitTryAcquireCallback(object state)
	{
		LowLevelLock lowLevelLock = (LowLevelLock)state;
		return lowLevelLock.TryAcquire_NoFastPath(lowLevelLock._state);
	}

	public void Acquire()
	{
		if (!TryAcquire())
		{
			WaitAndAcquire();
		}
	}

	private void WaitAndAcquire()
	{
		if (!_spinWaiter.SpinWaitForCondition(s_spinWaitTryAcquireCallback, this, 8, 4))
		{
			_monitor.Acquire();
			int num = Interlocked.Add(ref _state, 2);
			while (((uint)num & (true ? 1u : 0u)) != 0 || Interlocked.CompareExchange(ref _state, num + -1, num) != num)
			{
				_monitor.Wait();
				_isAnyWaitingThreadSignaled = false;
				num = _state;
			}
			_monitor.Release();
		}
	}

	public void Release()
	{
		if (Interlocked.Decrement(ref _state) != 0)
		{
			SignalWaiter();
		}
	}

	private void SignalWaiter()
	{
		_monitor.Acquire();
		if ((uint)_state >= 2u && !_isAnyWaitingThreadSignaled)
		{
			_isAnyWaitingThreadSignaled = true;
			_monitor.Signal_Release();
		}
		else
		{
			_monitor.Release();
		}
	}
}
