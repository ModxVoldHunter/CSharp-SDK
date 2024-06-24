using System.Runtime.InteropServices;

namespace System.Threading;

internal sealed class LowLevelLifoSemaphore : LowLevelLifoSemaphoreBase, IDisposable
{
	private nint _completionPort;

	public LowLevelLifoSemaphore(int initialSignalCount, int maximumSignalCount, int spinCount, Action onWait)
		: base(initialSignalCount, maximumSignalCount, spinCount, onWait)
	{
		Create(maximumSignalCount);
	}

	public bool Wait(int timeoutMs, bool spinWait)
	{
		int num = (spinWait ? _spinCount : 0);
		Counts counts = _separated._counts;
		Counts newCounts;
		while (true)
		{
			newCounts = counts;
			if (counts.SignalCount != 0)
			{
				newCounts.DecrementSignalCount();
			}
			else if (timeoutMs != 0)
			{
				if (num > 0 && newCounts.SpinnerCount < byte.MaxValue)
				{
					newCounts.IncrementSpinnerCount();
				}
				else
				{
					newCounts.IncrementWaiterCount();
				}
			}
			Counts counts2 = _separated._counts.InterlockedCompareExchange(newCounts, counts);
			if (counts2 == counts)
			{
				break;
			}
			counts = counts2;
		}
		if (counts.SignalCount != 0)
		{
			return true;
		}
		if (newCounts.WaiterCount != counts.WaiterCount)
		{
			return WaitForSignal(timeoutMs);
		}
		if (timeoutMs == 0)
		{
			return false;
		}
		int processorCount = Environment.ProcessorCount;
		int num2 = ((processorCount <= 1) ? 10 : 0);
		while (num2 < num)
		{
			LowLevelSpinWaiter.Wait(num2, 10, processorCount);
			num2++;
			counts = _separated._counts;
			while (counts.SignalCount != 0)
			{
				Counts newCounts2 = counts;
				newCounts2.DecrementSignalCount();
				newCounts2.DecrementSpinnerCount();
				Counts counts3 = _separated._counts.InterlockedCompareExchange(newCounts2, counts);
				if (counts3 == counts)
				{
					return true;
				}
				counts = counts3;
			}
		}
		counts = _separated._counts;
		while (true)
		{
			Counts newCounts3 = counts;
			newCounts3.DecrementSpinnerCount();
			if (counts.SignalCount != 0)
			{
				newCounts3.DecrementSignalCount();
			}
			else
			{
				newCounts3.IncrementWaiterCount();
			}
			Counts counts4 = _separated._counts.InterlockedCompareExchange(newCounts3, counts);
			if (counts4 == counts)
			{
				break;
			}
			counts = counts4;
		}
		if (counts.SignalCount == 0)
		{
			return WaitForSignal(timeoutMs);
		}
		return true;
	}

	private bool WaitForSignal(int timeoutMs)
	{
		_onWait();
		Counts counts;
		do
		{
			int num = ((timeoutMs != -1) ? Environment.TickCount : 0);
			if (timeoutMs == 0 || !WaitCore(timeoutMs))
			{
				_separated._counts.InterlockedDecrementWaiterCount();
				return false;
			}
			int num2 = ((timeoutMs != -1) ? Environment.TickCount : 0);
			counts = _separated._counts;
			while (true)
			{
				Counts newCounts = counts;
				if (counts.SignalCount != 0)
				{
					newCounts.DecrementSignalCount();
					newCounts.DecrementWaiterCount();
				}
				if (counts.CountOfWaitersSignaledToWake != 0)
				{
					newCounts.DecrementCountOfWaitersSignaledToWake();
				}
				Counts counts2 = _separated._counts.InterlockedCompareExchange(newCounts, counts);
				if (counts2 == counts)
				{
					break;
				}
				counts = counts2;
				if (timeoutMs != -1)
				{
					int num3 = num2 - num;
					timeoutMs = ((num3 >= 0 && num3 < timeoutMs) ? (timeoutMs - num3) : 0);
				}
			}
		}
		while (counts.SignalCount == 0);
		return true;
	}

	private void Create(int maximumSignalCount)
	{
		_completionPort = Interop.Kernel32.CreateIoCompletionPort(new IntPtr(-1), IntPtr.Zero, UIntPtr.Zero, maximumSignalCount);
		if (_completionPort == IntPtr.Zero)
		{
			int hRForLastWin32Error = Marshal.GetHRForLastWin32Error();
			OutOfMemoryException ex = new OutOfMemoryException();
			ex.HResult = hRForLastWin32Error;
			throw ex;
		}
	}

	~LowLevelLifoSemaphore()
	{
		if (_completionPort != IntPtr.Zero)
		{
			Dispose();
		}
	}

	public bool WaitCore(int timeoutMs)
	{
		uint lpNumberOfBytesTransferred;
		nuint CompletionKey;
		nint lpOverlapped;
		return Interop.Kernel32.GetQueuedCompletionStatus(_completionPort, out lpNumberOfBytesTransferred, out CompletionKey, out lpOverlapped, timeoutMs);
	}

	protected override void ReleaseCore(int count)
	{
		for (int i = 0; i < count; i++)
		{
			if (!Interop.Kernel32.PostQueuedCompletionStatus(_completionPort, 1u, UIntPtr.Zero, IntPtr.Zero))
			{
				int lastPInvokeError = Marshal.GetLastPInvokeError();
				OutOfMemoryException ex = new OutOfMemoryException();
				ex.HResult = lastPInvokeError;
				throw ex;
			}
		}
	}

	public void Dispose()
	{
		Interop.Kernel32.CloseHandle(_completionPort);
		_completionPort = IntPtr.Zero;
		GC.SuppressFinalize(this);
	}
}
