using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace System.Diagnostics.Tracing;

internal sealed class EventPipeEventDispatcher
{
	internal sealed class EventListenerSubscription
	{
		internal EventKeywords MatchAnyKeywords { get; private set; }

		internal EventLevel Level { get; private set; }

		internal EventListenerSubscription(EventKeywords matchAnyKeywords, EventLevel level)
		{
			MatchAnyKeywords = matchAnyKeywords;
			Level = level;
		}
	}

	internal static readonly EventPipeEventDispatcher Instance = new EventPipeEventDispatcher();

	private readonly nint m_RuntimeProviderID;

	private ulong m_sessionID;

	private CancellationTokenSource m_dispatchTaskCancellationSource;

	private Task m_dispatchTask;

	private readonly object m_dispatchControlLock = new object();

	private readonly Dictionary<EventListener, EventListenerSubscription> m_subscriptions = new Dictionary<EventListener, EventListenerSubscription>();

	private EventPipeEventDispatcher()
	{
		m_RuntimeProviderID = EventPipeInternal.GetProvider("Microsoft-Windows-DotNETRuntime");
	}

	internal void SendCommand(EventListener eventListener, EventCommand command, bool enable, EventLevel level, EventKeywords matchAnyKeywords)
	{
		lock (m_dispatchControlLock)
		{
			if (command == EventCommand.Update && enable)
			{
				m_subscriptions[eventListener] = new EventListenerSubscription(matchAnyKeywords, level);
			}
			else if (command == EventCommand.Update && !enable)
			{
				m_subscriptions.Remove(eventListener);
			}
			CommitDispatchConfiguration();
		}
	}

	private unsafe void CommitDispatchConfiguration()
	{
		SetStopDispatchTask();
		if (m_subscriptions.Count <= 0)
		{
			return;
		}
		EventKeywords eventKeywords = EventKeywords.None;
		EventLevel eventLevel = EventLevel.Critical;
		foreach (EventListenerSubscription value in m_subscriptions.Values)
		{
			eventKeywords |= value.MatchAnyKeywords;
			if (eventLevel != 0 && (eventLevel < value.Level || value.Level == EventLevel.LogAlways))
			{
				eventLevel = value.Level;
			}
		}
		EventPipeProviderConfiguration[] providers = new EventPipeProviderConfiguration[1]
		{
			new EventPipeProviderConfiguration("Microsoft-Windows-DotNETRuntime", (ulong)eventKeywords, (uint)eventLevel, null)
		};
		ulong num = EventPipeInternal.Enable(null, EventPipeSerializationFormat.NetTrace, 10u, providers);
		if (num == 0L)
		{
			throw new EventSourceException(SR.EventSource_CouldNotEnableEventPipe);
		}
		Unsafe.SkipInit(out EventPipeSessionInfo eventPipeSessionInfo);
		EventPipeInternal.GetSessionInfo(num, &eventPipeSessionInfo);
		DateTime syncTimeUtc = DateTime.FromFileTimeUtc(eventPipeSessionInfo.StartTimeAsUTCFileTime);
		long startTimeStamp = eventPipeSessionInfo.StartTimeStamp;
		long timeStampFrequency = eventPipeSessionInfo.TimeStampFrequency;
		Volatile.Write(ref m_sessionID, num);
		StartDispatchTask(num, syncTimeUtc, startTimeStamp, timeStampFrequency);
	}

	private void StartDispatchTask(ulong sessionID, DateTime syncTimeUtc, long syncTimeQPC, long timeQPCFrequency)
	{
		m_dispatchTaskCancellationSource = new CancellationTokenSource();
		Task previousDispatchTask = m_dispatchTask;
		m_dispatchTask = Task.Factory.StartNew(delegate
		{
			DispatchEventsToEventListeners(sessionID, syncTimeUtc, syncTimeQPC, timeQPCFrequency, previousDispatchTask, m_dispatchTaskCancellationSource.Token);
		}, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default);
	}

	private void SetStopDispatchTask()
	{
		CancellationTokenSource dispatchTaskCancellationSource = m_dispatchTaskCancellationSource;
		if (dispatchTaskCancellationSource != null && !dispatchTaskCancellationSource.IsCancellationRequested)
		{
			ulong sessionID = Volatile.Read(ref m_sessionID);
			m_dispatchTaskCancellationSource.Cancel();
			EventPipeInternal.SignalSession(sessionID);
			Volatile.Write(ref m_sessionID, 0uL);
		}
	}

	private unsafe void DispatchEventsToEventListeners(ulong sessionID, DateTime syncTimeUtc, long syncTimeQPC, long timeQPCFrequency, Task previousDispatchTask, CancellationToken token)
	{
		previousDispatchTask?.Wait(CancellationToken.None);
		Unsafe.SkipInit(out EventPipeEventInstanceData eventPipeEventInstanceData);
		while (!token.IsCancellationRequested)
		{
			bool flag = false;
			while (!token.IsCancellationRequested && EventPipeInternal.GetNextEvent(sessionID, &eventPipeEventInstanceData))
			{
				flag = true;
				if (eventPipeEventInstanceData.ProviderID == m_RuntimeProviderID)
				{
					ReadOnlySpan<byte> payload = new ReadOnlySpan<byte>((void*)eventPipeEventInstanceData.Payload, (int)eventPipeEventInstanceData.PayloadLength);
					DateTime timeStamp = TimeStampToDateTime(eventPipeEventInstanceData.TimeStamp, syncTimeUtc, syncTimeQPC, timeQPCFrequency);
					NativeRuntimeEventSource.Log.ProcessEvent(eventPipeEventInstanceData.EventID, eventPipeEventInstanceData.ThreadID, timeStamp, eventPipeEventInstanceData.ActivityId, eventPipeEventInstanceData.ChildActivityId, payload);
				}
			}
			if (!token.IsCancellationRequested)
			{
				if (!flag)
				{
					EventPipeInternal.WaitForSessionSignal(sessionID, -1);
				}
				Thread.Sleep(10);
			}
		}
		SpinWait spinWait = default(SpinWait);
		while (Volatile.Read(ref m_sessionID) == sessionID)
		{
			spinWait.SpinOnce();
		}
		EventPipeInternal.Disable(sessionID);
	}

	private static DateTime TimeStampToDateTime(long timeStamp, DateTime syncTimeUtc, long syncTimeQPC, long timeQPCFrequency)
	{
		if (timeStamp == long.MaxValue)
		{
			return DateTime.MaxValue;
		}
		long num = (long)((double)(timeStamp - syncTimeQPC) * 10000000.0 / (double)timeQPCFrequency) + syncTimeUtc.Ticks;
		if (num < 0 || 3155378975999999999L < num)
		{
			num = 3155378975999999999L;
		}
		return new DateTime(num, DateTimeKind.Utc);
	}
}
