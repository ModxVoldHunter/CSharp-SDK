using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace System.Threading;

[DebuggerDisplay("Count = {CountForDebugger}")]
[DebuggerTypeProxy(typeof(TimerQueueDebuggerTypeProxy))]
internal sealed class TimerQueue : IThreadPoolWorkItem
{
	private sealed class TimerQueueDebuggerTypeProxy
	{
		private readonly TimerQueue _queue;

		[DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
		public TimerQueueTimer[] Items => new List<TimerQueueTimer>(_queue.GetTimersForDebugger()).ToArray();

		public TimerQueueDebuggerTypeProxy(TimerQueue queue)
		{
			ArgumentNullException.ThrowIfNull(queue, "queue");
			_queue = queue;
		}
	}

	internal static readonly (long TickCount, DateTime Time) s_tickCountToTimeMap = (TickCount: TickCount64, Time: DateTime.UtcNow);

	private bool _isTimerScheduled;

	private long _currentTimerStartTicks;

	private uint _currentTimerDuration;

	private TimerQueueTimer _shortTimers;

	private TimerQueueTimer _longTimers;

	private long _currentAbsoluteThreshold = TickCount64 + 333;

	private static List<TimerQueue> s_scheduledTimers;

	private static List<TimerQueue> s_scheduledTimersToFire;

	private static readonly AutoResetEvent s_timerEvent = new AutoResetEvent(initialState: false);

	private bool _isScheduled;

	private long _scheduledDueTimeMs;

	private nint _nativeTimer;

	private readonly int _id;

	public static TimerQueue[] Instances { get; } = CreateTimerQueues();


	private int CountForDebugger
	{
		get
		{
			int num = 0;
			foreach (TimerQueueTimer item in GetTimersForDebugger())
			{
				num++;
			}
			return num;
		}
	}

	public long ActiveCount { get; private set; }

	private static long TickCount64
	{
		get
		{
			if (Environment.IsWindows8OrAbove)
			{
				ulong UnbiasedTime;
				bool flag = Interop.Kernel32.QueryUnbiasedInterruptTime(out UnbiasedTime);
				return (long)(UnbiasedTime / 10000);
			}
			return Environment.TickCount64;
		}
	}

	private static TimerQueue[] CreateTimerQueues()
	{
		TimerQueue[] array = new TimerQueue[Environment.ProcessorCount];
		for (int i = 0; i < array.Length; i++)
		{
			array[i] = new TimerQueue(i);
		}
		return array;
	}

	internal IEnumerable<TimerQueueTimer> GetTimersForDebugger()
	{
		for (TimerQueueTimer timer = _shortTimers; timer != null; timer = timer._next)
		{
			yield return timer;
		}
		for (TimerQueueTimer timer = _longTimers; timer != null; timer = timer._next)
		{
			yield return timer;
		}
	}

	private bool EnsureTimerFiresBy(uint requestedDuration)
	{
		uint num = Math.Min(requestedDuration, 268435455u);
		if (_isTimerScheduled)
		{
			long num2 = TickCount64 - _currentTimerStartTicks;
			if (num2 >= _currentTimerDuration)
			{
				return true;
			}
			uint num3 = _currentTimerDuration - (uint)(int)num2;
			if (num >= num3)
			{
				return true;
			}
		}
		if (SetTimer(num))
		{
			_isTimerScheduled = true;
			_currentTimerStartTicks = TickCount64;
			_currentTimerDuration = num;
			return true;
		}
		return false;
	}

	private void FireNextTimers()
	{
		TimerQueueTimer timerQueueTimer = null;
		lock (this)
		{
			_isTimerScheduled = false;
			bool flag = false;
			uint num = uint.MaxValue;
			long tickCount = TickCount64;
			TimerQueueTimer timerQueueTimer2 = _shortTimers;
			for (int i = 0; i < 2; i++)
			{
				while (timerQueueTimer2 != null)
				{
					TimerQueueTimer next = timerQueueTimer2._next;
					long num2 = tickCount - timerQueueTimer2._startTicks;
					long num3 = timerQueueTimer2._dueTime - num2;
					if (num3 <= 0)
					{
						timerQueueTimer2._everQueued = true;
						if (timerQueueTimer2._period != uint.MaxValue)
						{
							timerQueueTimer2._startTicks = tickCount;
							long num4 = num2 - timerQueueTimer2._dueTime;
							timerQueueTimer2._dueTime = ((num4 >= timerQueueTimer2._period) ? 1u : (timerQueueTimer2._period - (uint)(int)num4));
							if (timerQueueTimer2._dueTime < num)
							{
								flag = true;
								num = timerQueueTimer2._dueTime;
							}
							bool flag2 = tickCount + timerQueueTimer2._dueTime - _currentAbsoluteThreshold <= 0;
							if (timerQueueTimer2._short != flag2)
							{
								MoveTimerToCorrectList(timerQueueTimer2, flag2);
							}
						}
						else
						{
							DeleteTimer(timerQueueTimer2);
						}
						if (timerQueueTimer == null)
						{
							timerQueueTimer = timerQueueTimer2;
						}
						else
						{
							ThreadPool.UnsafeQueueUserWorkItemInternal(timerQueueTimer2, preferLocal: false);
						}
					}
					else
					{
						if (num3 < num)
						{
							flag = true;
							num = (uint)num3;
						}
						if (!timerQueueTimer2._short && num3 <= 333)
						{
							MoveTimerToCorrectList(timerQueueTimer2, shortList: true);
						}
					}
					timerQueueTimer2 = next;
				}
				if (i != 0)
				{
					continue;
				}
				long num5 = _currentAbsoluteThreshold - tickCount;
				if (num5 > 0)
				{
					if (_shortTimers == null && _longTimers != null)
					{
						num = (uint)((int)num5 + 1);
						flag = true;
					}
					break;
				}
				timerQueueTimer2 = _longTimers;
				_currentAbsoluteThreshold = tickCount + 333;
			}
			if (flag)
			{
				EnsureTimerFiresBy(num);
			}
		}
		timerQueueTimer?.Fire();
	}

	public bool UpdateTimer(TimerQueueTimer timer, uint dueTime, uint period)
	{
		long tickCount = TickCount64;
		long num = tickCount + dueTime;
		bool flag = _currentAbsoluteThreshold - num >= 0;
		if (timer._dueTime == uint.MaxValue)
		{
			timer._short = flag;
			LinkTimer(timer);
			long activeCount = ActiveCount + 1;
			ActiveCount = activeCount;
		}
		else if (timer._short != flag)
		{
			UnlinkTimer(timer);
			timer._short = flag;
			LinkTimer(timer);
		}
		timer._dueTime = dueTime;
		timer._period = ((period == 0) ? uint.MaxValue : period);
		timer._startTicks = tickCount;
		return EnsureTimerFiresBy(dueTime);
	}

	public void MoveTimerToCorrectList(TimerQueueTimer timer, bool shortList)
	{
		UnlinkTimer(timer);
		timer._short = shortList;
		LinkTimer(timer);
	}

	private void LinkTimer(TimerQueueTimer timer)
	{
		ref TimerQueueTimer reference = ref timer._short ? ref _shortTimers : ref _longTimers;
		timer._next = reference;
		if (timer._next != null)
		{
			timer._next._prev = timer;
		}
		timer._prev = null;
		reference = timer;
	}

	private void UnlinkTimer(TimerQueueTimer timer)
	{
		TimerQueueTimer next = timer._next;
		if (next != null)
		{
			next._prev = timer._prev;
		}
		if (_shortTimers == timer)
		{
			_shortTimers = next;
		}
		else if (_longTimers == timer)
		{
			_longTimers = next;
		}
		next = timer._prev;
		if (next != null)
		{
			next._next = timer._next;
		}
	}

	public void DeleteTimer(TimerQueueTimer timer)
	{
		if (timer._dueTime != uint.MaxValue)
		{
			long activeCount = ActiveCount - 1;
			ActiveCount = activeCount;
			UnlinkTimer(timer);
			timer._prev = null;
			timer._next = null;
			timer._dueTime = uint.MaxValue;
			timer._period = uint.MaxValue;
			timer._startTicks = 0L;
			timer._short = false;
		}
	}

	private static List<TimerQueue> InitializeScheduledTimerManager_Locked()
	{
		List<TimerQueue> result = new List<TimerQueue>(Instances.Length);
		if (s_scheduledTimersToFire == null)
		{
			s_scheduledTimersToFire = new List<TimerQueue>(Instances.Length);
		}
		Thread thread = new Thread(TimerThread)
		{
			Name = ".NET Timer",
			IsBackground = true
		};
		thread.UnsafeStart();
		s_scheduledTimers = result;
		return result;
	}

	private bool SetTimerPortable(uint actualDuration)
	{
		long scheduledDueTimeMs = TickCount64 + (int)actualDuration;
		AutoResetEvent autoResetEvent = s_timerEvent;
		lock (autoResetEvent)
		{
			if (!_isScheduled)
			{
				List<TimerQueue> list = s_scheduledTimers ?? InitializeScheduledTimerManager_Locked();
				list.Add(this);
				_isScheduled = true;
			}
			_scheduledDueTimeMs = scheduledDueTimeMs;
		}
		autoResetEvent.Set();
		return true;
	}

	private static void TimerThread()
	{
		AutoResetEvent autoResetEvent = s_timerEvent;
		List<TimerQueue> list = s_scheduledTimersToFire;
		List<TimerQueue> list2;
		lock (autoResetEvent)
		{
			list2 = s_scheduledTimers;
		}
		int num = -1;
		while (true)
		{
			autoResetEvent.WaitOne(num);
			long tickCount = TickCount64;
			num = int.MaxValue;
			lock (autoResetEvent)
			{
				for (int num2 = list2.Count - 1; num2 >= 0; num2--)
				{
					TimerQueue timerQueue = list2[num2];
					long num3 = timerQueue._scheduledDueTimeMs - tickCount;
					if (num3 <= 0)
					{
						timerQueue._isScheduled = false;
						list.Add(timerQueue);
						int num4 = list2.Count - 1;
						if (num2 != num4)
						{
							list2[num2] = list2[num4];
						}
						list2.RemoveAt(num4);
					}
					else if (num3 < num)
					{
						num = (int)num3;
					}
				}
			}
			if (list.Count > 0)
			{
				foreach (TimerQueue item in list)
				{
					ThreadPool.UnsafeQueueHighPriorityWorkItemInternal(item);
				}
				list.Clear();
			}
			if (num == int.MaxValue)
			{
				num = -1;
			}
		}
	}

	void IThreadPoolWorkItem.Execute()
	{
		FireNextTimers();
	}

	private TimerQueue(int id)
	{
		_id = id;
	}

	private bool SetTimer(uint actualDuration)
	{
		if (!ThreadPool.UseWindowsThreadPool)
		{
			return SetTimerPortable(actualDuration);
		}
		return SetTimerWindowsThreadPool(actualDuration);
	}

	[UnmanagedCallersOnly]
	private unsafe static void TimerCallbackWindowsThreadPool(void* instance, void* context, void* timer)
	{
		int num = (int)context;
		ThreadPoolCallbackWrapper threadPoolCallbackWrapper = ThreadPoolCallbackWrapper.Enter();
		Instances[num].FireNextTimers();
		ThreadPool.IncrementCompletedWorkItemCount();
		threadPoolCallbackWrapper.Exit();
	}

	private unsafe bool SetTimerWindowsThreadPool(uint actualDuration)
	{
		if (_nativeTimer == IntPtr.Zero)
		{
			_nativeTimer = Interop.Kernel32.CreateThreadpoolTimer((delegate* unmanaged<void*, void*, void*, void>)(delegate*<void*, void*, void*, void>)(&TimerCallbackWindowsThreadPool), _id, IntPtr.Zero);
			if (_nativeTimer == IntPtr.Zero)
			{
				throw new OutOfMemoryException();
			}
		}
		long num = -10000 * actualDuration;
		Interop.Kernel32.SetThreadpoolTimer(_nativeTimer, &num, 0u, 0u);
		return true;
	}
}
