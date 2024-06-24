using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace System.Diagnostics.Tracing;

internal sealed class EventPipeEventProvider : EventProviderImpl
{
	private readonly WeakReference<EventProvider> _eventProvider;

	private nint _provHandle;

	private GCHandle _gcHandle;

	internal EventPipeEventProvider(EventProvider eventProvider)
	{
		_eventProvider = new WeakReference<EventProvider>(eventProvider);
	}

	protected unsafe override void HandleEnableNotification(EventProvider target, byte* additionalData, byte level, long matchAnyKeywords, long matchAllKeywords, Interop.Advapi32.EVENT_FILTER_DESCRIPTOR* filterData)
	{
		ulong num = 0uL;
		if (additionalData != null)
		{
			num = BitConverter.ToUInt64(new ReadOnlySpan<byte>(additionalData, 8));
		}
		bool flag = num != 0;
		IDictionary<string, string> arguments = null;
		ControllerCommand command = ControllerCommand.Update;
		if (flag)
		{
			byte[] data = null;
			if (filterData != null)
			{
				MarshalFilterData(filterData, out command, out data);
			}
			arguments = EventProviderImpl.ParseFilterData(data);
		}
		target.OnControllerCommand(command, arguments, flag ? 4 : (-1));
	}

	[UnmanagedCallersOnly]
	private unsafe static void Callback(byte* sourceId, int isEnabled, byte level, long matchAnyKeywords, long matchAllKeywords, Interop.Advapi32.EVENT_FILTER_DESCRIPTOR* filterData, void* callbackContext)
	{
		EventPipeEventProvider eventPipeEventProvider = (EventPipeEventProvider)GCHandle.FromIntPtr((nint)callbackContext).Target;
		if (eventPipeEventProvider._eventProvider.TryGetTarget(out var target))
		{
			eventPipeEventProvider.ProviderCallback(target, sourceId, isEnabled, level, matchAnyKeywords, matchAllKeywords, filterData);
		}
	}

	internal unsafe override void Register(EventSource eventSource)
	{
		_gcHandle = GCHandle.Alloc(this);
		_provHandle = EventPipeInternal.CreateProvider(eventSource.Name, (delegate* unmanaged<byte*, int, byte, long, long, Interop.Advapi32.EVENT_FILTER_DESCRIPTOR*, void*, void>)(delegate*<byte*, int, byte, long, long, Interop.Advapi32.EVENT_FILTER_DESCRIPTOR*, void*, void>)(&Callback), (void*)GCHandle.ToIntPtr(_gcHandle));
		if (_provHandle == 0)
		{
			_gcHandle.Free();
			throw new OutOfMemoryException();
		}
	}

	internal override void Unregister()
	{
		if (_provHandle != 0)
		{
			EventPipeInternal.DeleteProvider(_provHandle);
			_provHandle = 0;
		}
		if (_gcHandle.IsAllocated)
		{
			_gcHandle.Free();
		}
	}

	internal unsafe override EventProvider.WriteEventErrorCode EventWriteTransfer(in EventDescriptor eventDescriptor, nint eventHandle, Guid* activityId, Guid* relatedActivityId, int userDataCount, EventProvider.EventData* userData)
	{
		if (eventHandle != IntPtr.Zero)
		{
			if (userDataCount == 0)
			{
				EventPipeInternal.WriteEventData(eventHandle, null, 0u, activityId, relatedActivityId);
				return EventProvider.WriteEventErrorCode.NoError;
			}
			if (eventDescriptor.Channel == 11)
			{
				userData += 3;
				userDataCount -= 3;
			}
			EventPipeInternal.WriteEventData(eventHandle, userData, (uint)userDataCount, activityId, relatedActivityId);
		}
		return EventProvider.WriteEventErrorCode.NoError;
	}

	internal unsafe override nint DefineEventHandle(uint eventID, string eventName, long keywords, uint eventVersion, uint level, byte* pMetadata, uint metadataLength)
	{
		return EventPipeInternal.DefineEvent(_provHandle, eventID, keywords, eventVersion, level, pMetadata, metadataLength);
	}

	internal static int EventActivityIdControl(Interop.Advapi32.ActivityControl controlCode, ref Guid activityId)
	{
		return EventPipeInternal.EventActivityIdControl((uint)controlCode, ref activityId);
	}
}
