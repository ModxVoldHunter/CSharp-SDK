using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Tracing;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System.Net;

[EventSource(Name = "Private.InternalDiagnostics.System.Net.Sockets")]
internal sealed class NetEventSource : EventSource
{
	public static class Keywords
	{
		public const EventKeywords Default = (EventKeywords)1L;

		public const EventKeywords Debug = (EventKeywords)2L;
	}

	public static readonly System.Net.NetEventSource Log = new System.Net.NetEventSource();

	[NonEvent]
	public static void Accepted(Socket socket, object remoteEp, object localEp)
	{
		Log.Accepted(IdOf(remoteEp), IdOf(localEp), GetHashCode(socket));
	}

	[Event(5, Keywords = (EventKeywords)1L, Level = EventLevel.Informational)]
	private void Accepted(string remoteEp, string localEp, int socketHash)
	{
		WriteEvent(5, remoteEp, localEp, socketHash);
	}

	[NonEvent]
	public static void Connected(Socket socket, object localEp, object remoteEp)
	{
		Log.Connected(IdOf(localEp), IdOf(remoteEp), GetHashCode(socket));
	}

	[Event(6, Keywords = (EventKeywords)1L, Level = EventLevel.Informational)]
	private void Connected(string localEp, string remoteEp, int socketHash)
	{
		WriteEvent(6, localEp, remoteEp, socketHash);
	}

	[NonEvent]
	public static void ConnectedAsyncDns(Socket socket)
	{
		Log.ConnectedAsyncDns(GetHashCode(socket));
	}

	[Event(7, Keywords = (EventKeywords)1L, Level = EventLevel.Informational)]
	private void ConnectedAsyncDns(int socketHash)
	{
		WriteEvent(7, socketHash);
	}

	[NonEvent]
	public static void DumpBuffer(object thisOrContextObject, Memory<byte> buffer, int offset, int count, [CallerMemberName] string memberName = null)
	{
		DumpBuffer(thisOrContextObject, buffer.Span.Slice(offset, count), memberName);
	}

	[NonEvent]
	public static void Info(object thisOrContextObject, FormattableString formattableString = null, [CallerMemberName] string memberName = null)
	{
		Log.Info(IdOf(thisOrContextObject), memberName, (formattableString != null) ? Format(formattableString) : "");
	}

	[NonEvent]
	public static void Info(object thisOrContextObject, object message, [CallerMemberName] string memberName = null)
	{
		Log.Info(IdOf(thisOrContextObject), memberName, Format(message));
	}

	[Event(1, Level = EventLevel.Informational, Keywords = (EventKeywords)1L)]
	private void Info(string thisOrContextObject, string memberName, string message)
	{
		WriteEvent(1, thisOrContextObject, memberName ?? "(?)", message);
	}

	[NonEvent]
	public static void Error(object thisOrContextObject, FormattableString formattableString, [CallerMemberName] string memberName = null)
	{
		Log.ErrorMessage(IdOf(thisOrContextObject), memberName, Format(formattableString));
	}

	[NonEvent]
	public static void Error(object thisOrContextObject, object message, [CallerMemberName] string memberName = null)
	{
		Log.ErrorMessage(IdOf(thisOrContextObject), memberName, Format(message));
	}

	[Event(2, Level = EventLevel.Error, Keywords = (EventKeywords)1L)]
	private void ErrorMessage(string thisOrContextObject, string memberName, string message)
	{
		WriteEvent(2, thisOrContextObject, memberName ?? "(?)", message);
	}

	[NonEvent]
	public static string IdOf(object value)
	{
		if (value == null)
		{
			return "(null)";
		}
		return value.GetType().Name + "#" + GetHashCode(value);
	}

	[NonEvent]
	public static int GetHashCode(object value)
	{
		return value?.GetHashCode() ?? 0;
	}

	[NonEvent]
	public static string Format(object value)
	{
		if (value == null)
		{
			return "(null)";
		}
		string text = null;
		if (text != null)
		{
			return text;
		}
		if (value is Array array)
		{
			return $"{array.GetType().GetElementType()}[{((Array)value).Length}]";
		}
		if (value is ICollection collection)
		{
			return $"{collection.GetType().Name}({collection.Count})";
		}
		if (value is SafeHandle safeHandle)
		{
			return $"{safeHandle.GetType().Name}:{safeHandle.GetHashCode()}(0x{safeHandle.DangerousGetHandle():X})";
		}
		if (value is nint)
		{
			return $"0x{value:X}";
		}
		string text2 = value.ToString();
		if (text2 == null || text2 == value.GetType().FullName)
		{
			return IdOf(value);
		}
		return value.ToString();
	}

	[NonEvent]
	private static string Format(FormattableString s)
	{
		switch (s.ArgumentCount)
		{
		case 0:
			return s.Format;
		case 1:
			return string.Format(s.Format, Format(s.GetArgument(0)));
		case 2:
			return string.Format(s.Format, Format(s.GetArgument(0)), Format(s.GetArgument(1)));
		case 3:
			return string.Format(s.Format, Format(s.GetArgument(0)), Format(s.GetArgument(1)), Format(s.GetArgument(2)));
		default:
		{
			string[] array = new string[s.ArgumentCount];
			for (int i = 0; i < array.Length; i++)
			{
				array[i] = Format(s.GetArgument(i));
			}
			string format = s.Format;
			object[] args = array;
			return string.Format(format, args);
		}
		}
	}

	[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:UnrecognizedReflectionPattern", Justification = "Parameters to this method are primitive and are trimmer safe")]
	[NonEvent]
	private unsafe void WriteEvent(int eventId, string arg1, string arg2, int arg3)
	{
		//The blocks IL_0035 are reachable both inside and outside the pinned region starting at IL_0032. ILSpy has duplicated these blocks in order to place them both within and outside the `fixed` statement.
		if (arg1 == null)
		{
			arg1 = "";
		}
		if (arg2 == null)
		{
			arg2 = "";
		}
		fixed (char* dataPointer2 = arg1)
		{
			char* intPtr;
			EventData* intPtr2;
			nint num;
			nint num2;
			if (arg2 == null)
			{
				char* dataPointer;
				intPtr = (dataPointer = null);
				EventData* ptr = stackalloc EventData[3];
				intPtr2 = ptr;
				*intPtr2 = new EventData
				{
					DataPointer = (nint)dataPointer2,
					Size = (arg1.Length + 1) * 2
				};
				num = (nint)(ptr + 1);
				*(EventData*)num = new EventData
				{
					DataPointer = (nint)dataPointer,
					Size = (arg2.Length + 1) * 2
				};
				num2 = (nint)(ptr + 2);
				*(EventData*)num2 = new EventData
				{
					DataPointer = (nint)(&arg3),
					Size = 4
				};
				WriteEventCore(eventId, 3, ptr);
				return;
			}
			fixed (char* ptr2 = &arg2.GetPinnableReference())
			{
				char* dataPointer;
				intPtr = (dataPointer = ptr2);
				EventData* ptr = stackalloc EventData[3];
				intPtr2 = ptr;
				*intPtr2 = new EventData
				{
					DataPointer = (nint)dataPointer2,
					Size = (arg1.Length + 1) * 2
				};
				num = (nint)(ptr + 1);
				*(EventData*)num = new EventData
				{
					DataPointer = (nint)dataPointer,
					Size = (arg2.Length + 1) * 2
				};
				num2 = (nint)(ptr + 2);
				*(EventData*)num2 = new EventData
				{
					DataPointer = (nint)(&arg3),
					Size = 4
				};
				WriteEventCore(eventId, 3, ptr);
			}
		}
	}

	[NonEvent]
	public static void DumpBuffer(object thisOrContextObject, byte[] buffer, int offset, int count, [CallerMemberName] string memberName = null)
	{
		DumpBuffer(thisOrContextObject, buffer.AsSpan(offset, count), memberName);
	}

	[NonEvent]
	public static void DumpBuffer(object thisOrContextObject, ReadOnlySpan<byte> buffer, [CallerMemberName] string memberName = null)
	{
		Log.DumpBuffer(IdOf(thisOrContextObject), memberName, buffer.Slice(0, Math.Min(buffer.Length, 1024)).ToArray());
	}

	[Event(4, Level = EventLevel.Verbose, Keywords = (EventKeywords)2L)]
	private void DumpBuffer(string thisOrContextObject, string memberName, byte[] buffer)
	{
		WriteEvent(4, thisOrContextObject, memberName ?? "(?)", buffer);
	}

	[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:UnrecognizedReflectionPattern", Justification = "Parameters to this method are primitive and are trimmer safe")]
	[NonEvent]
	private unsafe void WriteEvent(int eventId, string arg1, string arg2, byte[] arg3)
	{
		//The blocks IL_0040, IL_0044, IL_0046, IL_005f, IL_0132 are reachable both inside and outside the pinned region starting at IL_003d. ILSpy has duplicated these blocks in order to place them both within and outside the `fixed` statement.
		if (arg1 == null)
		{
			arg1 = "";
		}
		if (arg2 == null)
		{
			arg2 = "";
		}
		if (arg3 == null)
		{
			arg3 = Array.Empty<byte>();
		}
		fixed (char* dataPointer3 = arg1)
		{
			char* intPtr;
			byte[] array;
			int size;
			EventData* intPtr2;
			nint num;
			nint num2;
			nint num3;
			if (arg2 == null)
			{
				char* dataPointer;
				intPtr = (dataPointer = null);
				array = arg3;
				fixed (byte* ptr = array)
				{
					byte* dataPointer2 = ptr;
					size = arg3.Length;
					EventData* ptr2 = stackalloc EventData[4];
					intPtr2 = ptr2;
					*intPtr2 = new EventData
					{
						DataPointer = (nint)dataPointer3,
						Size = (arg1.Length + 1) * 2
					};
					num = (nint)(ptr2 + 1);
					*(EventData*)num = new EventData
					{
						DataPointer = (nint)dataPointer,
						Size = (arg2.Length + 1) * 2
					};
					num2 = (nint)(ptr2 + 2);
					*(EventData*)num2 = new EventData
					{
						DataPointer = (nint)(&size),
						Size = 4
					};
					num3 = (nint)(ptr2 + 3);
					*(EventData*)num3 = new EventData
					{
						DataPointer = (nint)dataPointer2,
						Size = size
					};
					WriteEventCore(eventId, 4, ptr2);
				}
				return;
			}
			fixed (char* ptr3 = &arg2.GetPinnableReference())
			{
				char* dataPointer;
				intPtr = (dataPointer = ptr3);
				array = arg3;
				fixed (byte* ptr = array)
				{
					byte* dataPointer2 = ptr;
					size = arg3.Length;
					EventData* ptr2 = stackalloc EventData[4];
					intPtr2 = ptr2;
					*intPtr2 = new EventData
					{
						DataPointer = (nint)dataPointer3,
						Size = (arg1.Length + 1) * 2
					};
					num = (nint)(ptr2 + 1);
					*(EventData*)num = new EventData
					{
						DataPointer = (nint)dataPointer,
						Size = (arg2.Length + 1) * 2
					};
					num2 = (nint)(ptr2 + 2);
					*(EventData*)num2 = new EventData
					{
						DataPointer = (nint)(&size),
						Size = 4
					};
					num3 = (nint)(ptr2 + 3);
					*(EventData*)num3 = new EventData
					{
						DataPointer = (nint)dataPointer2,
						Size = size
					};
					WriteEventCore(eventId, 4, ptr2);
				}
			}
		}
	}
}
