using System.Collections.Generic;
using System.Globalization;
using System.Numerics;
using System.Runtime.InteropServices;
using Internal.Win32;

namespace System.Diagnostics.Tracing;

internal sealed class EtwEventProvider : EventProviderImpl
{
	public struct SessionInfo
	{
		internal int sessionIdBit;

		internal int etwSessionId;

		internal SessionInfo(int sessionIdBit_, int etwSessionId_)
		{
			sessionIdBit = sessionIdBit_;
			etwSessionId = etwSessionId_;
		}
	}

	private delegate void SessionInfoCallback(int etwSessionId, long matchAllKeywords, ref List<SessionInfo> sessionList);

	private readonly WeakReference<EventProvider> _eventProvider;

	private long _registrationHandle;

	private GCHandle _gcHandle;

	private List<SessionInfo> _liveSessions;

	private Guid _providerId;

	private static bool s_setInformationMissing;

	internal EtwEventProvider(EventProvider eventProvider)
	{
		_eventProvider = new WeakReference<EventProvider>(eventProvider);
	}

	internal override void Disable()
	{
		base.Disable();
		_liveSessions = null;
	}

	protected unsafe override void HandleEnableNotification(EventProvider target, byte* additionalData, byte level, long matchAnyKeywords, long matchAllKeywords, Interop.Advapi32.EVENT_FILTER_DESCRIPTOR* filterData)
	{
		List<KeyValuePair<SessionInfo, bool>> changedSessions = GetChangedSessions();
		foreach (KeyValuePair<SessionInfo, bool> item in changedSessions)
		{
			int sessionIdBit = item.Key.sessionIdBit;
			bool value = item.Value;
			IDictionary<string, string> arguments = null;
			ControllerCommand command = ControllerCommand.Update;
			if (value)
			{
				byte[] data;
				if (changedSessions.Count > 1 || filterData == null)
				{
					TryReadRegistryFilterData(item.Key.etwSessionId, out command, out data);
				}
				else
				{
					MarshalFilterData(filterData, out command, out data);
				}
				arguments = EventProviderImpl.ParseFilterData(data);
			}
			target.OnControllerCommand(command, arguments, value ? sessionIdBit : (-sessionIdBit));
		}
	}

	[UnmanagedCallersOnly]
	private unsafe static void Callback(Guid* sourceId, int isEnabled, byte level, long matchAnyKeywords, long matchAllKeywords, Interop.Advapi32.EVENT_FILTER_DESCRIPTOR* filterData, void* callbackContext)
	{
		EtwEventProvider etwEventProvider = (EtwEventProvider)GCHandle.FromIntPtr((nint)callbackContext).Target;
		if (etwEventProvider._eventProvider.TryGetTarget(out var target))
		{
			etwEventProvider.ProviderCallback(target, null, isEnabled, level, matchAnyKeywords, matchAllKeywords, filterData);
		}
	}

	internal unsafe override void Register(EventSource eventSource)
	{
		_gcHandle = GCHandle.Alloc(this);
		long registrationHandle = 0L;
		_providerId = eventSource.Guid;
		Guid providerId = _providerId;
		uint num = Interop.Advapi32.EventRegister(&providerId, (delegate* unmanaged<Guid*, int, byte, long, long, Interop.Advapi32.EVENT_FILTER_DESCRIPTOR*, void*, void>)(delegate*<Guid*, int, byte, long, long, Interop.Advapi32.EVENT_FILTER_DESCRIPTOR*, void*, void>)(&Callback), (void*)GCHandle.ToIntPtr(_gcHandle), &registrationHandle);
		if (num != 0)
		{
			_gcHandle.Free();
			throw new ArgumentException(Interop.Kernel32.GetMessage((int)num));
		}
		_registrationHandle = registrationHandle;
	}

	internal override void Unregister()
	{
		if (_registrationHandle != 0L)
		{
			Interop.Advapi32.EventUnregister(_registrationHandle);
			_registrationHandle = 0L;
		}
		if (_gcHandle.IsAllocated)
		{
			_gcHandle.Free();
		}
	}

	internal unsafe override EventProvider.WriteEventErrorCode EventWriteTransfer(in EventDescriptor eventDescriptor, nint eventHandle, Guid* activityId, Guid* relatedActivityId, int userDataCount, EventProvider.EventData* userData)
	{
		switch (Interop.Advapi32.EventWriteTransfer(_registrationHandle, in eventDescriptor, activityId, relatedActivityId, userDataCount, userData))
		{
		case 234:
		case 534:
			return EventProvider.WriteEventErrorCode.EventTooBig;
		case 8:
			return EventProvider.WriteEventErrorCode.NoFreeBuffers;
		default:
			return EventProvider.WriteEventErrorCode.NoError;
		}
	}

	internal unsafe override nint DefineEventHandle(uint eventID, string eventName, long keywords, uint eventVersion, uint level, byte* pMetadata, uint metadataLength)
	{
		throw new NotSupportedException();
	}

	internal unsafe int SetInformation(Interop.Advapi32.EVENT_INFO_CLASS eventInfoClass, void* data, uint dataSize)
	{
		int result = 50;
		if (!s_setInformationMissing)
		{
			try
			{
				result = Interop.Advapi32.EventSetInformation(_registrationHandle, eventInfoClass, data, dataSize);
			}
			catch (TypeLoadException)
			{
				s_setInformationMissing = true;
			}
		}
		return result;
	}

	private bool TryReadRegistryFilterData(int etwSessionId, out ControllerCommand command, out byte[] data)
	{
		command = ControllerCommand.Update;
		data = null;
		string text = "\\Microsoft\\Windows\\CurrentVersion\\Winevt\\Publishers\\{" + _providerId.ToString() + "}";
		_ = 8;
		text = "Software\\Wow6432Node" + text;
		string name = "ControllerData_Session_" + etwSessionId.ToString(CultureInfo.InvariantCulture);
		using (RegistryKey registryKey = Registry.LocalMachine.OpenSubKey(text))
		{
			data = registryKey?.GetValue(name, null) as byte[];
			if (data != null)
			{
				return true;
			}
		}
		return false;
	}

	private List<KeyValuePair<SessionInfo, bool>> GetChangedSessions()
	{
		List<SessionInfo> sessionList = null;
		GetSessionInfo(GetSessionInfoCallback, ref sessionList);
		List<KeyValuePair<SessionInfo, bool>> list = new List<KeyValuePair<SessionInfo, bool>>();
		if (_liveSessions != null)
		{
			foreach (SessionInfo liveSession in _liveSessions)
			{
				int index;
				if ((index = IndexOfSessionInList(sessionList, liveSession.etwSessionId)) < 0 || sessionList[index].sessionIdBit != liveSession.sessionIdBit)
				{
					list.Add(new KeyValuePair<SessionInfo, bool>(liveSession, value: false));
				}
			}
		}
		if (sessionList != null)
		{
			foreach (SessionInfo item in sessionList)
			{
				int index2;
				if ((index2 = IndexOfSessionInList(_liveSessions, item.etwSessionId)) < 0 || _liveSessions[index2].sessionIdBit != item.sessionIdBit)
				{
					list.Add(new KeyValuePair<SessionInfo, bool>(item, value: true));
				}
			}
		}
		_liveSessions = sessionList;
		return list;
	}

	private static void GetSessionInfoCallback(int etwSessionId, long matchAllKeywords, ref List<SessionInfo> sessionList)
	{
		uint value = (uint)SessionMask.FromEventKeywords((ulong)matchAllKeywords);
		int num = BitOperations.PopCount(value);
		if (num <= 1)
		{
			if (sessionList == null)
			{
				sessionList = new List<SessionInfo>(8);
			}
			num = ((num != 1) ? BitOperations.PopCount((uint)SessionMask.All) : BitOperations.TrailingZeroCount(value));
			sessionList.Add(new SessionInfo(num + 1, etwSessionId));
		}
	}

	private unsafe void GetSessionInfo(SessionInfoCallback action, ref List<SessionInfo> sessionList)
	{
		int ReturnLength = 256;
		byte* ptr = stackalloc byte[(int)(uint)ReturnLength];
		byte* ptr2 = ptr;
		try
		{
			while (true)
			{
				int num = 0;
				fixed (Guid* inBuffer = &_providerId)
				{
					num = Interop.Advapi32.EnumerateTraceGuidsEx(Interop.Advapi32.TRACE_QUERY_INFO_CLASS.TraceGuidQueryInfo, inBuffer, sizeof(Guid), ptr2, ReturnLength, out ReturnLength);
				}
				switch (num)
				{
				default:
					return;
				case 122:
					if (ptr2 != ptr)
					{
						byte* hglobal = ptr2;
						ptr2 = null;
						Marshal.FreeHGlobal((nint)hglobal);
					}
					break;
				case 0:
				{
					Interop.Advapi32.TRACE_GUID_INFO* ptr3 = (Interop.Advapi32.TRACE_GUID_INFO*)ptr2;
					Interop.Advapi32.TRACE_PROVIDER_INSTANCE_INFO* ptr4 = (Interop.Advapi32.TRACE_PROVIDER_INSTANCE_INFO*)(ptr3 + 1);
					int currentProcessId = (int)Interop.Kernel32.GetCurrentProcessId();
					for (int i = 0; i < ptr3->InstanceCount; i++)
					{
						if (ptr4->Pid == currentProcessId)
						{
							Interop.Advapi32.TRACE_ENABLE_INFO* ptr5 = (Interop.Advapi32.TRACE_ENABLE_INFO*)(ptr4 + 1);
							for (int j = 0; j < ptr4->EnableCount; j++)
							{
								action(ptr5[j].LoggerId, ptr5[j].MatchAllKeyword, ref sessionList);
							}
						}
						if (ptr4->NextOffset == 0)
						{
							break;
						}
						byte* ptr6 = (byte*)ptr4;
						ptr4 = (Interop.Advapi32.TRACE_PROVIDER_INSTANCE_INFO*)(ptr6 + ptr4->NextOffset);
					}
					return;
				}
				}
				ptr2 = (byte*)Marshal.AllocHGlobal(ReturnLength);
			}
		}
		finally
		{
			if (ptr2 != null && ptr2 != ptr)
			{
				Marshal.FreeHGlobal((nint)ptr2);
			}
		}
	}

	private static int IndexOfSessionInList(List<SessionInfo> sessions, int etwSessionId)
	{
		if (sessions == null)
		{
			return -1;
		}
		for (int i = 0; i < sessions.Count; i++)
		{
			if (sessions[i].etwSessionId == etwSessionId)
			{
				return i;
			}
		}
		return -1;
	}
}
