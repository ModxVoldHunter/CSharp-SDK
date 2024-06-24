using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace System.Diagnostics.Tracing;

internal class EventProviderImpl
{
	protected byte _level;

	protected long _anyKeywordMask;

	protected long _allKeywordMask;

	protected bool _enabled;

	internal EventLevel Level
	{
		get
		{
			return (EventLevel)_level;
		}
		set
		{
			_level = (byte)value;
		}
	}

	internal EventKeywords MatchAnyKeyword
	{
		get
		{
			return (EventKeywords)_anyKeywordMask;
		}
		set
		{
			_anyKeywordMask = (long)value;
		}
	}

	internal EventKeywords MatchAllKeyword
	{
		get
		{
			return (EventKeywords)_allKeywordMask;
		}
		set
		{
			_allKeywordMask = (long)value;
		}
	}

	protected unsafe virtual void HandleEnableNotification(EventProvider target, byte* additionalData, byte level, long matchAnyKeywords, long matchAllKeywords, Interop.Advapi32.EVENT_FILTER_DESCRIPTOR* filterData)
	{
	}

	public bool IsEnabled()
	{
		return _enabled;
	}

	public bool IsEnabled(byte level, long keywords)
	{
		if (!_enabled)
		{
			return false;
		}
		if ((level <= _level || _level == 0) && (keywords == 0L || ((keywords & _anyKeywordMask) != 0L && (keywords & _allKeywordMask) == _allKeywordMask)))
		{
			return true;
		}
		return false;
	}

	internal void Enable(byte level, long anyKeyword, long allKeyword)
	{
		_enabled = true;
		_level = level;
		_anyKeywordMask = anyKeyword;
		_allKeywordMask = allKeyword;
	}

	internal virtual void Disable()
	{
		_enabled = false;
		_level = 0;
		_anyKeywordMask = 0L;
		_allKeywordMask = 0L;
	}

	internal virtual void Register(EventSource eventSource)
	{
	}

	internal virtual void Unregister()
	{
	}

	internal unsafe virtual EventProvider.WriteEventErrorCode EventWriteTransfer(in EventDescriptor eventDescriptor, nint eventHandle, Guid* activityId, Guid* relatedActivityId, int userDataCount, EventProvider.EventData* userData)
	{
		return EventProvider.WriteEventErrorCode.NoError;
	}

	internal unsafe virtual nint DefineEventHandle(uint eventID, string eventName, long keywords, uint eventVersion, uint level, byte* pMetadata, uint metadataLength)
	{
		return IntPtr.Zero;
	}

	protected unsafe void ProviderCallback(EventProvider target, byte* additionalData, int controlCode, byte level, long matchAnyKeywords, long matchAllKeywords, Interop.Advapi32.EVENT_FILTER_DESCRIPTOR* filterData)
	{
		try
		{
			if (controlCode == 1)
			{
				Enable(level, matchAnyKeywords, matchAllKeywords);
				HandleEnableNotification(target, additionalData, level, matchAnyKeywords, matchAllKeywords, filterData);
				return;
			}
			ControllerCommand command = ControllerCommand.Update;
			switch (controlCode)
			{
			case 0:
				Disable();
				break;
			case 2:
				command = ControllerCommand.SendManifest;
				break;
			default:
				return;
			}
			target.OnControllerCommand(command, null, 0);
		}
		catch
		{
		}
	}

	private static int FindNull(byte[] buffer, int idx)
	{
		while (idx < buffer.Length && buffer[idx] != 0)
		{
			idx++;
		}
		return idx;
	}

	protected static IDictionary<string, string> ParseFilterData(byte[] data)
	{
		IDictionary<string, string> dictionary = null;
		if (data != null)
		{
			dictionary = new Dictionary<string, string>(4);
			int num = 0;
			while (num < data.Length)
			{
				int num2 = FindNull(data, num);
				int num3 = num2 + 1;
				int num4 = FindNull(data, num3);
				if (num4 < data.Length)
				{
					string @string = Encoding.UTF8.GetString(data, num, num2 - num);
					string string2 = Encoding.UTF8.GetString(data, num3, num4 - num3);
					dictionary[@string] = string2;
				}
				num = num4 + 1;
			}
		}
		return dictionary;
	}

	protected unsafe bool MarshalFilterData(Interop.Advapi32.EVENT_FILTER_DESCRIPTOR* filterData, out ControllerCommand command, out byte[] data)
	{
		data = null;
		if (filterData->Ptr != 0L && 0 < filterData->Size && filterData->Size <= 102400)
		{
			data = new byte[filterData->Size];
			Marshal.Copy((nint)filterData->Ptr, data, 0, data.Length);
		}
		command = (ControllerCommand)filterData->Type;
		return true;
	}
}
