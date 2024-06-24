using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace System.Diagnostics.Tracing;

internal class EventProvider : IDisposable
{
	public struct EventData
	{
		internal ulong Ptr;

		internal uint Size;

		internal uint Reserved;
	}

	public enum WriteEventErrorCode
	{
		NoError,
		NoFreeBuffers,
		EventTooBig,
		NullInput,
		TooManyArgs,
		Other
	}

	private struct EightObjects
	{
		internal object _arg0;

		private object _arg1;

		private object _arg2;

		private object _arg3;

		private object _arg4;

		private object _arg5;

		private object _arg6;

		private object _arg7;
	}

	internal EventProviderImpl _eventProvider;

	private string _providerName;

	private Guid _providerId;

	internal bool _disposed;

	[ThreadStatic]
	private static WriteEventErrorCode s_returnCode;

	private const int BasicTypeAllocationBufferSize = 16;

	private const int EtwMaxNumberArguments = 128;

	private const int EtwAPIMaxRefObjCount = 8;

	private const int TraceEventMaximumSize = 65482;

	protected EventLevel Level
	{
		get
		{
			return _eventProvider.Level;
		}
		set
		{
			_eventProvider.Level = value;
		}
	}

	protected EventKeywords MatchAnyKeyword
	{
		get
		{
			return _eventProvider.MatchAnyKeyword;
		}
		set
		{
			_eventProvider.MatchAnyKeyword = value;
		}
	}

	protected EventKeywords MatchAllKeyword
	{
		get
		{
			return _eventProvider.MatchAllKeyword;
		}
		set
		{
			_eventProvider.MatchAllKeyword = value;
		}
	}

	internal EventProvider(EventProviderType providerType)
	{
		_eventProvider = providerType switch
		{
			EventProviderType.ETW => new EtwEventProvider(this), 
			EventProviderType.EventPipe => new EventPipeEventProvider(this), 
			_ => new EventProviderImpl(), 
		};
	}

	internal void Register(EventSource eventSource)
	{
		_providerName = eventSource.Name;
		_providerId = eventSource.Guid;
		_eventProvider.Register(eventSource);
	}

	public void Dispose()
	{
		Dispose(disposing: true);
		GC.SuppressFinalize(this);
	}

	protected virtual void Dispose(bool disposing)
	{
		if (_disposed)
		{
			return;
		}
		_eventProvider.Disable();
		lock (EventListener.EventListenersLock)
		{
			if (_disposed)
			{
				return;
			}
			_disposed = true;
		}
		_eventProvider.Unregister();
	}

	public virtual void Close()
	{
		Dispose();
	}

	~EventProvider()
	{
		Dispose(disposing: false);
	}

	internal virtual void OnControllerCommand(ControllerCommand command, IDictionary<string, string> arguments, int sessionId)
	{
	}

	public bool IsEnabled()
	{
		return _eventProvider.IsEnabled();
	}

	public bool IsEnabled(byte level, long keywords)
	{
		return _eventProvider.IsEnabled(level, keywords);
	}

	public static WriteEventErrorCode GetLastWriteEventError()
	{
		return s_returnCode;
	}

	private static void SetLastError(WriteEventErrorCode error)
	{
		s_returnCode = error;
	}

	private unsafe static object EncodeObject(ref object data, ref EventData* dataDescriptor, ref byte* dataBuffer, ref uint totalEventSize)
	{
		string text;
		byte[] array;
		while (true)
		{
			dataDescriptor->Reserved = 0u;
			text = data as string;
			array = null;
			if (text != null)
			{
				dataDescriptor->Size = (uint)((text.Length + 1) * 2);
				break;
			}
			if ((array = data as byte[]) != null)
			{
				*(int*)dataBuffer = array.Length;
				dataDescriptor->Ptr = (ulong)dataBuffer;
				dataDescriptor->Size = 4u;
				totalEventSize += dataDescriptor->Size;
				dataDescriptor++;
				dataBuffer += 16;
				dataDescriptor->Size = (uint)array.Length;
				break;
			}
			if (data is nint)
			{
				dataDescriptor->Size = 8u;
				nint* ptr = (nint*)dataBuffer;
				*ptr = (nint)data;
				dataDescriptor->Ptr = (ulong)ptr;
				break;
			}
			if (data is int)
			{
				dataDescriptor->Size = 4u;
				int* ptr2 = (int*)dataBuffer;
				*ptr2 = (int)data;
				dataDescriptor->Ptr = (ulong)ptr2;
				break;
			}
			if (data is long)
			{
				dataDescriptor->Size = 8u;
				long* ptr3 = (long*)dataBuffer;
				*ptr3 = (long)data;
				dataDescriptor->Ptr = (ulong)ptr3;
				break;
			}
			if (data is uint)
			{
				dataDescriptor->Size = 4u;
				uint* ptr4 = (uint*)dataBuffer;
				*ptr4 = (uint)data;
				dataDescriptor->Ptr = (ulong)ptr4;
				break;
			}
			if (data is ulong)
			{
				dataDescriptor->Size = 8u;
				ulong* ptr5 = (ulong*)dataBuffer;
				*ptr5 = (ulong)data;
				dataDescriptor->Ptr = (ulong)ptr5;
				break;
			}
			if (data is char)
			{
				dataDescriptor->Size = 2u;
				char* ptr6 = (char*)dataBuffer;
				*ptr6 = (char)data;
				dataDescriptor->Ptr = (ulong)ptr6;
				break;
			}
			if (data is byte)
			{
				dataDescriptor->Size = 1u;
				byte* ptr7 = dataBuffer;
				*ptr7 = (byte)data;
				dataDescriptor->Ptr = (ulong)ptr7;
				break;
			}
			if (data is short)
			{
				dataDescriptor->Size = 2u;
				short* ptr8 = (short*)dataBuffer;
				*ptr8 = (short)data;
				dataDescriptor->Ptr = (ulong)ptr8;
				break;
			}
			if (data is sbyte)
			{
				dataDescriptor->Size = 1u;
				sbyte* ptr9 = (sbyte*)dataBuffer;
				*ptr9 = (sbyte)data;
				dataDescriptor->Ptr = (ulong)ptr9;
				break;
			}
			if (data is ushort)
			{
				dataDescriptor->Size = 2u;
				ushort* ptr10 = (ushort*)dataBuffer;
				*ptr10 = (ushort)data;
				dataDescriptor->Ptr = (ulong)ptr10;
				break;
			}
			if (data is float)
			{
				dataDescriptor->Size = 4u;
				float* ptr11 = (float*)dataBuffer;
				*ptr11 = (float)data;
				dataDescriptor->Ptr = (ulong)ptr11;
				break;
			}
			if (data is double)
			{
				dataDescriptor->Size = 8u;
				double* ptr12 = (double*)dataBuffer;
				*ptr12 = (double)data;
				dataDescriptor->Ptr = (ulong)ptr12;
				break;
			}
			if (data is bool)
			{
				dataDescriptor->Size = 4u;
				int* ptr13 = (int*)dataBuffer;
				if ((bool)data)
				{
					*ptr13 = 1;
				}
				else
				{
					*ptr13 = 0;
				}
				dataDescriptor->Ptr = (ulong)ptr13;
				break;
			}
			if (data is Guid)
			{
				dataDescriptor->Size = (uint)sizeof(Guid);
				Guid* ptr14 = (Guid*)dataBuffer;
				*ptr14 = (Guid)data;
				dataDescriptor->Ptr = (ulong)ptr14;
				break;
			}
			if (data is decimal)
			{
				dataDescriptor->Size = 16u;
				decimal* ptr15 = (decimal*)dataBuffer;
				*ptr15 = (decimal)data;
				dataDescriptor->Ptr = (ulong)ptr15;
				break;
			}
			if (data is DateTime)
			{
				long num = 0L;
				if (((DateTime)data).Ticks > 504911232000000000L)
				{
					num = ((DateTime)data).ToFileTimeUtc();
				}
				dataDescriptor->Size = 8u;
				long* ptr16 = (long*)dataBuffer;
				*ptr16 = num;
				dataDescriptor->Ptr = (ulong)ptr16;
				break;
			}
			if (data is Enum)
			{
				try
				{
					Type underlyingType = Enum.GetUnderlyingType(data.GetType());
					if (underlyingType == typeof(ulong))
					{
						data = (ulong)data;
					}
					else if (underlyingType == typeof(long))
					{
						data = (long)data;
					}
					else
					{
						data = (int)Convert.ToInt64(data);
					}
				}
				catch
				{
					goto IL_0410;
				}
				continue;
			}
			goto IL_0410;
			IL_0410:
			text = ((data != null) ? data.ToString() : "");
			dataDescriptor->Size = (uint)((text.Length + 1) * 2);
			break;
		}
		totalEventSize += dataDescriptor->Size;
		dataDescriptor++;
		dataBuffer += 16;
		return ((object)text) ?? ((object)array);
	}

	internal unsafe bool WriteEvent(ref EventDescriptor eventDescriptor, nint eventHandle, Guid* activityID, Guid* childActivityID, object[] eventPayload)
	{
		//The blocks IL_0220, IL_0231, IL_0242, IL_0255, IL_025a, IL_0263, IL_0264, IL_0275, IL_0286, IL_0299, IL_029e, IL_02a7, IL_02a8, IL_02b9, IL_02ca, IL_02dd, IL_02e2, IL_02eb, IL_02ec, IL_02fd, IL_0319, IL_0324, IL_0340, IL_034b, IL_0367, IL_0372, IL_038e, IL_0399, IL_03b5, IL_03c0, IL_03dc, IL_03e7, IL_0403, IL_040e, IL_042a are reachable both inside and outside the pinned region starting at IL_021b. ILSpy has duplicated these blocks in order to place them both within and outside the `fixed` statement.
		//The blocks IL_0264, IL_0275, IL_0286, IL_0299, IL_029e, IL_02a7, IL_02a8, IL_02b9, IL_02ca, IL_02dd, IL_02e2, IL_02eb, IL_02ec, IL_02fd, IL_0319, IL_0324, IL_0340, IL_034b, IL_0367, IL_0372, IL_038e, IL_0399, IL_03b5, IL_03c0, IL_03dc, IL_03e7, IL_0403, IL_040e, IL_042a are reachable both inside and outside the pinned region starting at IL_025f. ILSpy has duplicated these blocks in order to place them both within and outside the `fixed` statement.
		//The blocks IL_02a8, IL_02b9, IL_02ca, IL_02dd, IL_02e2, IL_02eb, IL_02ec, IL_02fd, IL_0319, IL_0324, IL_0340, IL_034b, IL_0367, IL_0372, IL_038e, IL_0399, IL_03b5, IL_03c0, IL_03dc, IL_03e7, IL_0403, IL_040e, IL_042a are reachable both inside and outside the pinned region starting at IL_02a3. ILSpy has duplicated these blocks in order to place them both within and outside the `fixed` statement.
		//The blocks IL_02ec, IL_02fd, IL_0319, IL_0324, IL_0340, IL_034b, IL_0367, IL_0372, IL_038e, IL_0399, IL_03b5, IL_03c0, IL_03dc, IL_03e7, IL_0403, IL_040e, IL_042a are reachable both inside and outside the pinned region starting at IL_02e7. ILSpy has duplicated these blocks in order to place them both within and outside the `fixed` statement.
		//The blocks IL_02ec, IL_02fd, IL_0319, IL_0324, IL_0340, IL_034b, IL_0367, IL_0372, IL_038e, IL_0399, IL_03b5, IL_03c0, IL_03dc, IL_03e7, IL_0403, IL_040e, IL_042a are reachable both inside and outside the pinned region starting at IL_02e7. ILSpy has duplicated these blocks in order to place them both within and outside the `fixed` statement.
		//The blocks IL_02a8, IL_02b9, IL_02ca, IL_02dd, IL_02e2, IL_02eb, IL_02ec, IL_02fd, IL_0319, IL_0324, IL_0340, IL_034b, IL_0367, IL_0372, IL_038e, IL_0399, IL_03b5, IL_03c0, IL_03dc, IL_03e7, IL_0403, IL_040e, IL_042a are reachable both inside and outside the pinned region starting at IL_02a3. ILSpy has duplicated these blocks in order to place them both within and outside the `fixed` statement.
		//The blocks IL_02ec, IL_02fd, IL_0319, IL_0324, IL_0340, IL_034b, IL_0367, IL_0372, IL_038e, IL_0399, IL_03b5, IL_03c0, IL_03dc, IL_03e7, IL_0403, IL_040e, IL_042a are reachable both inside and outside the pinned region starting at IL_02e7. ILSpy has duplicated these blocks in order to place them both within and outside the `fixed` statement.
		//The blocks IL_02ec, IL_02fd, IL_0319, IL_0324, IL_0340, IL_034b, IL_0367, IL_0372, IL_038e, IL_0399, IL_03b5, IL_03c0, IL_03dc, IL_03e7, IL_0403, IL_040e, IL_042a are reachable both inside and outside the pinned region starting at IL_02e7. ILSpy has duplicated these blocks in order to place them both within and outside the `fixed` statement.
		//The blocks IL_0264, IL_0275, IL_0286, IL_0299, IL_029e, IL_02a7, IL_02a8, IL_02b9, IL_02ca, IL_02dd, IL_02e2, IL_02eb, IL_02ec, IL_02fd, IL_0319, IL_0324, IL_0340, IL_034b, IL_0367, IL_0372, IL_038e, IL_0399, IL_03b5, IL_03c0, IL_03dc, IL_03e7, IL_0403, IL_040e, IL_042a are reachable both inside and outside the pinned region starting at IL_025f. ILSpy has duplicated these blocks in order to place them both within and outside the `fixed` statement.
		//The blocks IL_02a8, IL_02b9, IL_02ca, IL_02dd, IL_02e2, IL_02eb, IL_02ec, IL_02fd, IL_0319, IL_0324, IL_0340, IL_034b, IL_0367, IL_0372, IL_038e, IL_0399, IL_03b5, IL_03c0, IL_03dc, IL_03e7, IL_0403, IL_040e, IL_042a are reachable both inside and outside the pinned region starting at IL_02a3. ILSpy has duplicated these blocks in order to place them both within and outside the `fixed` statement.
		//The blocks IL_02ec, IL_02fd, IL_0319, IL_0324, IL_0340, IL_034b, IL_0367, IL_0372, IL_038e, IL_0399, IL_03b5, IL_03c0, IL_03dc, IL_03e7, IL_0403, IL_040e, IL_042a are reachable both inside and outside the pinned region starting at IL_02e7. ILSpy has duplicated these blocks in order to place them both within and outside the `fixed` statement.
		//The blocks IL_02ec, IL_02fd, IL_0319, IL_0324, IL_0340, IL_034b, IL_0367, IL_0372, IL_038e, IL_0399, IL_03b5, IL_03c0, IL_03dc, IL_03e7, IL_0403, IL_040e, IL_042a are reachable both inside and outside the pinned region starting at IL_02e7. ILSpy has duplicated these blocks in order to place them both within and outside the `fixed` statement.
		//The blocks IL_02a8, IL_02b9, IL_02ca, IL_02dd, IL_02e2, IL_02eb, IL_02ec, IL_02fd, IL_0319, IL_0324, IL_0340, IL_034b, IL_0367, IL_0372, IL_038e, IL_0399, IL_03b5, IL_03c0, IL_03dc, IL_03e7, IL_0403, IL_040e, IL_042a are reachable both inside and outside the pinned region starting at IL_02a3. ILSpy has duplicated these blocks in order to place them both within and outside the `fixed` statement.
		//The blocks IL_02ec, IL_02fd, IL_0319, IL_0324, IL_0340, IL_034b, IL_0367, IL_0372, IL_038e, IL_0399, IL_03b5, IL_03c0, IL_03dc, IL_03e7, IL_0403, IL_040e, IL_042a are reachable both inside and outside the pinned region starting at IL_02e7. ILSpy has duplicated these blocks in order to place them both within and outside the `fixed` statement.
		//The blocks IL_02ec, IL_02fd, IL_0319, IL_0324, IL_0340, IL_034b, IL_0367, IL_0372, IL_038e, IL_0399, IL_03b5, IL_03c0, IL_03dc, IL_03e7, IL_0403, IL_040e, IL_042a are reachable both inside and outside the pinned region starting at IL_02e7. ILSpy has duplicated these blocks in order to place them both within and outside the `fixed` statement.
		WriteEventErrorCode writeEventErrorCode = WriteEventErrorCode.NoError;
		if (IsEnabled(eventDescriptor.Level, eventDescriptor.Keywords))
		{
			int num = eventPayload.Length;
			if (num > 128)
			{
				s_returnCode = WriteEventErrorCode.TooManyArgs;
				return false;
			}
			uint totalEventSize = 0u;
			int i = 0;
			EightObjects eightObjects = default(EightObjects);
			Span<int> span = stackalloc int[8];
			Span<object> span2 = new Span<object>(ref eightObjects._arg0, 8);
			EventData* ptr = stackalloc EventData[2 * num];
			for (int j = 0; j < 2 * num; j++)
			{
				ptr[j] = default(EventData);
			}
			EventData* dataDescriptor = ptr;
			byte* ptr2 = stackalloc byte[(int)(uint)(32 * num)];
			byte* dataBuffer = ptr2;
			bool flag = false;
			for (int k = 0; k < eventPayload.Length; k++)
			{
				if (eventPayload[k] != null)
				{
					object obj = EncodeObject(ref eventPayload[k], ref dataDescriptor, ref dataBuffer, ref totalEventSize);
					if (obj == null)
					{
						continue;
					}
					int num2 = (int)(dataDescriptor - ptr - 1);
					if (!(obj is string))
					{
						if (eventPayload.Length + num2 + 1 - k > 128)
						{
							s_returnCode = WriteEventErrorCode.TooManyArgs;
							return false;
						}
						flag = true;
					}
					if (i >= span2.Length)
					{
						Span<object> span3 = new object[span2.Length * 2];
						span2.CopyTo(span3);
						span2 = span3;
						Span<int> span4 = new int[span.Length * 2];
						span.CopyTo(span4);
						span = span4;
					}
					span2[i] = obj;
					span[i] = num2;
					i++;
					continue;
				}
				s_returnCode = WriteEventErrorCode.NullInput;
				return false;
			}
			num = (int)(dataDescriptor - ptr);
			if (totalEventSize > 65482)
			{
				s_returnCode = WriteEventErrorCode.EventTooBig;
				return false;
			}
			if (!flag && i <= 8)
			{
				for (; i < 8; i++)
				{
					span2[i] = null;
					span[i] = -1;
				}
				fixed (char* ptr13 = (string)span2[0])
				{
					string obj2 = (string)span2[1];
					char* intPtr;
					object obj3;
					object obj4;
					char* intPtr2;
					object obj5;
					object obj6;
					char* intPtr3;
					object obj7;
					object obj8;
					char* intPtr4;
					if (obj2 == null)
					{
						char* ptr3;
						intPtr = (ptr3 = null);
						obj3 = (string)span2[2];
						fixed (char* ptr4 = (string)obj3)
						{
							char* ptr5 = ptr4;
							obj4 = (string)span2[3];
							if (obj4 == null)
							{
								char* ptr6;
								intPtr2 = (ptr6 = null);
								obj5 = (string)span2[4];
								fixed (char* ptr7 = (string)obj5)
								{
									char* ptr8 = ptr7;
									obj6 = (string)span2[5];
									if (obj6 == null)
									{
										char* ptr9;
										intPtr3 = (ptr9 = null);
										obj7 = (string)span2[6];
										fixed (char* ptr10 = (string)obj7)
										{
											char* ptr11 = ptr10;
											obj8 = (string)span2[7];
											if (obj8 == null)
											{
												char* ptr12;
												intPtr4 = (ptr12 = null);
												dataDescriptor = ptr;
												if (span2[0] != null)
												{
													dataDescriptor[span[0]].Ptr = (ulong)ptr13;
												}
												if (span2[1] != null)
												{
													dataDescriptor[span[1]].Ptr = (ulong)ptr3;
												}
												if (span2[2] != null)
												{
													dataDescriptor[span[2]].Ptr = (ulong)ptr5;
												}
												if (span2[3] != null)
												{
													dataDescriptor[span[3]].Ptr = (ulong)ptr6;
												}
												if (span2[4] != null)
												{
													dataDescriptor[span[4]].Ptr = (ulong)ptr8;
												}
												if (span2[5] != null)
												{
													dataDescriptor[span[5]].Ptr = (ulong)ptr9;
												}
												if (span2[6] != null)
												{
													dataDescriptor[span[6]].Ptr = (ulong)ptr11;
												}
												if (span2[7] != null)
												{
													dataDescriptor[span[7]].Ptr = (ulong)ptr12;
												}
												writeEventErrorCode = _eventProvider.EventWriteTransfer(in eventDescriptor, eventHandle, activityID, childActivityID, num, ptr);
											}
											else
											{
												fixed (char* ptr14 = &((string)obj8).GetPinnableReference())
												{
													char* ptr12;
													intPtr4 = (ptr12 = ptr14);
													dataDescriptor = ptr;
													if (span2[0] != null)
													{
														dataDescriptor[span[0]].Ptr = (ulong)ptr13;
													}
													if (span2[1] != null)
													{
														dataDescriptor[span[1]].Ptr = (ulong)ptr3;
													}
													if (span2[2] != null)
													{
														dataDescriptor[span[2]].Ptr = (ulong)ptr5;
													}
													if (span2[3] != null)
													{
														dataDescriptor[span[3]].Ptr = (ulong)ptr6;
													}
													if (span2[4] != null)
													{
														dataDescriptor[span[4]].Ptr = (ulong)ptr8;
													}
													if (span2[5] != null)
													{
														dataDescriptor[span[5]].Ptr = (ulong)ptr9;
													}
													if (span2[6] != null)
													{
														dataDescriptor[span[6]].Ptr = (ulong)ptr11;
													}
													if (span2[7] != null)
													{
														dataDescriptor[span[7]].Ptr = (ulong)ptr12;
													}
													writeEventErrorCode = _eventProvider.EventWriteTransfer(in eventDescriptor, eventHandle, activityID, childActivityID, num, ptr);
												}
											}
										}
									}
									else
									{
										fixed (char* ptr15 = &((string)obj6).GetPinnableReference())
										{
											char* ptr9;
											intPtr3 = (ptr9 = ptr15);
											obj7 = (string)span2[6];
											fixed (char* ptr10 = (string)obj7)
											{
												char* ptr11 = ptr10;
												obj8 = (string)span2[7];
												if (obj8 == null)
												{
													char* ptr12;
													intPtr4 = (ptr12 = null);
													dataDescriptor = ptr;
													if (span2[0] != null)
													{
														dataDescriptor[span[0]].Ptr = (ulong)ptr13;
													}
													if (span2[1] != null)
													{
														dataDescriptor[span[1]].Ptr = (ulong)ptr3;
													}
													if (span2[2] != null)
													{
														dataDescriptor[span[2]].Ptr = (ulong)ptr5;
													}
													if (span2[3] != null)
													{
														dataDescriptor[span[3]].Ptr = (ulong)ptr6;
													}
													if (span2[4] != null)
													{
														dataDescriptor[span[4]].Ptr = (ulong)ptr8;
													}
													if (span2[5] != null)
													{
														dataDescriptor[span[5]].Ptr = (ulong)ptr9;
													}
													if (span2[6] != null)
													{
														dataDescriptor[span[6]].Ptr = (ulong)ptr11;
													}
													if (span2[7] != null)
													{
														dataDescriptor[span[7]].Ptr = (ulong)ptr12;
													}
													writeEventErrorCode = _eventProvider.EventWriteTransfer(in eventDescriptor, eventHandle, activityID, childActivityID, num, ptr);
												}
												else
												{
													fixed (char* ptr14 = &((string)obj8).GetPinnableReference())
													{
														char* ptr12;
														intPtr4 = (ptr12 = ptr14);
														dataDescriptor = ptr;
														if (span2[0] != null)
														{
															dataDescriptor[span[0]].Ptr = (ulong)ptr13;
														}
														if (span2[1] != null)
														{
															dataDescriptor[span[1]].Ptr = (ulong)ptr3;
														}
														if (span2[2] != null)
														{
															dataDescriptor[span[2]].Ptr = (ulong)ptr5;
														}
														if (span2[3] != null)
														{
															dataDescriptor[span[3]].Ptr = (ulong)ptr6;
														}
														if (span2[4] != null)
														{
															dataDescriptor[span[4]].Ptr = (ulong)ptr8;
														}
														if (span2[5] != null)
														{
															dataDescriptor[span[5]].Ptr = (ulong)ptr9;
														}
														if (span2[6] != null)
														{
															dataDescriptor[span[6]].Ptr = (ulong)ptr11;
														}
														if (span2[7] != null)
														{
															dataDescriptor[span[7]].Ptr = (ulong)ptr12;
														}
														writeEventErrorCode = _eventProvider.EventWriteTransfer(in eventDescriptor, eventHandle, activityID, childActivityID, num, ptr);
													}
												}
											}
										}
									}
								}
							}
							else
							{
								fixed (char* ptr16 = &((string)obj4).GetPinnableReference())
								{
									char* ptr6;
									intPtr2 = (ptr6 = ptr16);
									obj5 = (string)span2[4];
									fixed (char* ptr7 = (string)obj5)
									{
										char* ptr8 = ptr7;
										obj6 = (string)span2[5];
										if (obj6 == null)
										{
											char* ptr9;
											intPtr3 = (ptr9 = null);
											obj7 = (string)span2[6];
											fixed (char* ptr10 = (string)obj7)
											{
												char* ptr11 = ptr10;
												obj8 = (string)span2[7];
												if (obj8 == null)
												{
													char* ptr12;
													intPtr4 = (ptr12 = null);
													dataDescriptor = ptr;
													if (span2[0] != null)
													{
														dataDescriptor[span[0]].Ptr = (ulong)ptr13;
													}
													if (span2[1] != null)
													{
														dataDescriptor[span[1]].Ptr = (ulong)ptr3;
													}
													if (span2[2] != null)
													{
														dataDescriptor[span[2]].Ptr = (ulong)ptr5;
													}
													if (span2[3] != null)
													{
														dataDescriptor[span[3]].Ptr = (ulong)ptr6;
													}
													if (span2[4] != null)
													{
														dataDescriptor[span[4]].Ptr = (ulong)ptr8;
													}
													if (span2[5] != null)
													{
														dataDescriptor[span[5]].Ptr = (ulong)ptr9;
													}
													if (span2[6] != null)
													{
														dataDescriptor[span[6]].Ptr = (ulong)ptr11;
													}
													if (span2[7] != null)
													{
														dataDescriptor[span[7]].Ptr = (ulong)ptr12;
													}
													writeEventErrorCode = _eventProvider.EventWriteTransfer(in eventDescriptor, eventHandle, activityID, childActivityID, num, ptr);
												}
												else
												{
													fixed (char* ptr14 = &((string)obj8).GetPinnableReference())
													{
														char* ptr12;
														intPtr4 = (ptr12 = ptr14);
														dataDescriptor = ptr;
														if (span2[0] != null)
														{
															dataDescriptor[span[0]].Ptr = (ulong)ptr13;
														}
														if (span2[1] != null)
														{
															dataDescriptor[span[1]].Ptr = (ulong)ptr3;
														}
														if (span2[2] != null)
														{
															dataDescriptor[span[2]].Ptr = (ulong)ptr5;
														}
														if (span2[3] != null)
														{
															dataDescriptor[span[3]].Ptr = (ulong)ptr6;
														}
														if (span2[4] != null)
														{
															dataDescriptor[span[4]].Ptr = (ulong)ptr8;
														}
														if (span2[5] != null)
														{
															dataDescriptor[span[5]].Ptr = (ulong)ptr9;
														}
														if (span2[6] != null)
														{
															dataDescriptor[span[6]].Ptr = (ulong)ptr11;
														}
														if (span2[7] != null)
														{
															dataDescriptor[span[7]].Ptr = (ulong)ptr12;
														}
														writeEventErrorCode = _eventProvider.EventWriteTransfer(in eventDescriptor, eventHandle, activityID, childActivityID, num, ptr);
													}
												}
											}
										}
										else
										{
											fixed (char* ptr15 = &((string)obj6).GetPinnableReference())
											{
												char* ptr9;
												intPtr3 = (ptr9 = ptr15);
												obj7 = (string)span2[6];
												fixed (char* ptr10 = (string)obj7)
												{
													char* ptr11 = ptr10;
													obj8 = (string)span2[7];
													if (obj8 == null)
													{
														char* ptr12;
														intPtr4 = (ptr12 = null);
														dataDescriptor = ptr;
														if (span2[0] != null)
														{
															dataDescriptor[span[0]].Ptr = (ulong)ptr13;
														}
														if (span2[1] != null)
														{
															dataDescriptor[span[1]].Ptr = (ulong)ptr3;
														}
														if (span2[2] != null)
														{
															dataDescriptor[span[2]].Ptr = (ulong)ptr5;
														}
														if (span2[3] != null)
														{
															dataDescriptor[span[3]].Ptr = (ulong)ptr6;
														}
														if (span2[4] != null)
														{
															dataDescriptor[span[4]].Ptr = (ulong)ptr8;
														}
														if (span2[5] != null)
														{
															dataDescriptor[span[5]].Ptr = (ulong)ptr9;
														}
														if (span2[6] != null)
														{
															dataDescriptor[span[6]].Ptr = (ulong)ptr11;
														}
														if (span2[7] != null)
														{
															dataDescriptor[span[7]].Ptr = (ulong)ptr12;
														}
														writeEventErrorCode = _eventProvider.EventWriteTransfer(in eventDescriptor, eventHandle, activityID, childActivityID, num, ptr);
													}
													else
													{
														fixed (char* ptr14 = &((string)obj8).GetPinnableReference())
														{
															char* ptr12;
															intPtr4 = (ptr12 = ptr14);
															dataDescriptor = ptr;
															if (span2[0] != null)
															{
																dataDescriptor[span[0]].Ptr = (ulong)ptr13;
															}
															if (span2[1] != null)
															{
																dataDescriptor[span[1]].Ptr = (ulong)ptr3;
															}
															if (span2[2] != null)
															{
																dataDescriptor[span[2]].Ptr = (ulong)ptr5;
															}
															if (span2[3] != null)
															{
																dataDescriptor[span[3]].Ptr = (ulong)ptr6;
															}
															if (span2[4] != null)
															{
																dataDescriptor[span[4]].Ptr = (ulong)ptr8;
															}
															if (span2[5] != null)
															{
																dataDescriptor[span[5]].Ptr = (ulong)ptr9;
															}
															if (span2[6] != null)
															{
																dataDescriptor[span[6]].Ptr = (ulong)ptr11;
															}
															if (span2[7] != null)
															{
																dataDescriptor[span[7]].Ptr = (ulong)ptr12;
															}
															writeEventErrorCode = _eventProvider.EventWriteTransfer(in eventDescriptor, eventHandle, activityID, childActivityID, num, ptr);
														}
													}
												}
											}
										}
									}
								}
							}
						}
					}
					else
					{
						fixed (char* ptr17 = &obj2.GetPinnableReference())
						{
							char* ptr3;
							intPtr = (ptr3 = ptr17);
							obj3 = (string)span2[2];
							fixed (char* ptr4 = (string)obj3)
							{
								char* ptr5 = ptr4;
								obj4 = (string)span2[3];
								if (obj4 == null)
								{
									char* ptr6;
									intPtr2 = (ptr6 = null);
									obj5 = (string)span2[4];
									fixed (char* ptr7 = (string)obj5)
									{
										char* ptr8 = ptr7;
										obj6 = (string)span2[5];
										if (obj6 == null)
										{
											char* ptr9;
											intPtr3 = (ptr9 = null);
											obj7 = (string)span2[6];
											fixed (char* ptr10 = (string)obj7)
											{
												char* ptr11 = ptr10;
												obj8 = (string)span2[7];
												if (obj8 == null)
												{
													char* ptr12;
													intPtr4 = (ptr12 = null);
													dataDescriptor = ptr;
													if (span2[0] != null)
													{
														dataDescriptor[span[0]].Ptr = (ulong)ptr13;
													}
													if (span2[1] != null)
													{
														dataDescriptor[span[1]].Ptr = (ulong)ptr3;
													}
													if (span2[2] != null)
													{
														dataDescriptor[span[2]].Ptr = (ulong)ptr5;
													}
													if (span2[3] != null)
													{
														dataDescriptor[span[3]].Ptr = (ulong)ptr6;
													}
													if (span2[4] != null)
													{
														dataDescriptor[span[4]].Ptr = (ulong)ptr8;
													}
													if (span2[5] != null)
													{
														dataDescriptor[span[5]].Ptr = (ulong)ptr9;
													}
													if (span2[6] != null)
													{
														dataDescriptor[span[6]].Ptr = (ulong)ptr11;
													}
													if (span2[7] != null)
													{
														dataDescriptor[span[7]].Ptr = (ulong)ptr12;
													}
													writeEventErrorCode = _eventProvider.EventWriteTransfer(in eventDescriptor, eventHandle, activityID, childActivityID, num, ptr);
												}
												else
												{
													fixed (char* ptr14 = &((string)obj8).GetPinnableReference())
													{
														char* ptr12;
														intPtr4 = (ptr12 = ptr14);
														dataDescriptor = ptr;
														if (span2[0] != null)
														{
															dataDescriptor[span[0]].Ptr = (ulong)ptr13;
														}
														if (span2[1] != null)
														{
															dataDescriptor[span[1]].Ptr = (ulong)ptr3;
														}
														if (span2[2] != null)
														{
															dataDescriptor[span[2]].Ptr = (ulong)ptr5;
														}
														if (span2[3] != null)
														{
															dataDescriptor[span[3]].Ptr = (ulong)ptr6;
														}
														if (span2[4] != null)
														{
															dataDescriptor[span[4]].Ptr = (ulong)ptr8;
														}
														if (span2[5] != null)
														{
															dataDescriptor[span[5]].Ptr = (ulong)ptr9;
														}
														if (span2[6] != null)
														{
															dataDescriptor[span[6]].Ptr = (ulong)ptr11;
														}
														if (span2[7] != null)
														{
															dataDescriptor[span[7]].Ptr = (ulong)ptr12;
														}
														writeEventErrorCode = _eventProvider.EventWriteTransfer(in eventDescriptor, eventHandle, activityID, childActivityID, num, ptr);
													}
												}
											}
										}
										else
										{
											fixed (char* ptr15 = &((string)obj6).GetPinnableReference())
											{
												char* ptr9;
												intPtr3 = (ptr9 = ptr15);
												obj7 = (string)span2[6];
												fixed (char* ptr10 = (string)obj7)
												{
													char* ptr11 = ptr10;
													obj8 = (string)span2[7];
													if (obj8 == null)
													{
														char* ptr12;
														intPtr4 = (ptr12 = null);
														dataDescriptor = ptr;
														if (span2[0] != null)
														{
															dataDescriptor[span[0]].Ptr = (ulong)ptr13;
														}
														if (span2[1] != null)
														{
															dataDescriptor[span[1]].Ptr = (ulong)ptr3;
														}
														if (span2[2] != null)
														{
															dataDescriptor[span[2]].Ptr = (ulong)ptr5;
														}
														if (span2[3] != null)
														{
															dataDescriptor[span[3]].Ptr = (ulong)ptr6;
														}
														if (span2[4] != null)
														{
															dataDescriptor[span[4]].Ptr = (ulong)ptr8;
														}
														if (span2[5] != null)
														{
															dataDescriptor[span[5]].Ptr = (ulong)ptr9;
														}
														if (span2[6] != null)
														{
															dataDescriptor[span[6]].Ptr = (ulong)ptr11;
														}
														if (span2[7] != null)
														{
															dataDescriptor[span[7]].Ptr = (ulong)ptr12;
														}
														writeEventErrorCode = _eventProvider.EventWriteTransfer(in eventDescriptor, eventHandle, activityID, childActivityID, num, ptr);
													}
													else
													{
														fixed (char* ptr14 = &((string)obj8).GetPinnableReference())
														{
															char* ptr12;
															intPtr4 = (ptr12 = ptr14);
															dataDescriptor = ptr;
															if (span2[0] != null)
															{
																dataDescriptor[span[0]].Ptr = (ulong)ptr13;
															}
															if (span2[1] != null)
															{
																dataDescriptor[span[1]].Ptr = (ulong)ptr3;
															}
															if (span2[2] != null)
															{
																dataDescriptor[span[2]].Ptr = (ulong)ptr5;
															}
															if (span2[3] != null)
															{
																dataDescriptor[span[3]].Ptr = (ulong)ptr6;
															}
															if (span2[4] != null)
															{
																dataDescriptor[span[4]].Ptr = (ulong)ptr8;
															}
															if (span2[5] != null)
															{
																dataDescriptor[span[5]].Ptr = (ulong)ptr9;
															}
															if (span2[6] != null)
															{
																dataDescriptor[span[6]].Ptr = (ulong)ptr11;
															}
															if (span2[7] != null)
															{
																dataDescriptor[span[7]].Ptr = (ulong)ptr12;
															}
															writeEventErrorCode = _eventProvider.EventWriteTransfer(in eventDescriptor, eventHandle, activityID, childActivityID, num, ptr);
														}
													}
												}
											}
										}
									}
								}
								else
								{
									fixed (char* ptr16 = &((string)obj4).GetPinnableReference())
									{
										char* ptr6;
										intPtr2 = (ptr6 = ptr16);
										obj5 = (string)span2[4];
										fixed (char* ptr7 = (string)obj5)
										{
											char* ptr8 = ptr7;
											obj6 = (string)span2[5];
											if (obj6 == null)
											{
												char* ptr9;
												intPtr3 = (ptr9 = null);
												obj7 = (string)span2[6];
												fixed (char* ptr10 = (string)obj7)
												{
													char* ptr11 = ptr10;
													obj8 = (string)span2[7];
													if (obj8 == null)
													{
														char* ptr12;
														intPtr4 = (ptr12 = null);
														dataDescriptor = ptr;
														if (span2[0] != null)
														{
															dataDescriptor[span[0]].Ptr = (ulong)ptr13;
														}
														if (span2[1] != null)
														{
															dataDescriptor[span[1]].Ptr = (ulong)ptr3;
														}
														if (span2[2] != null)
														{
															dataDescriptor[span[2]].Ptr = (ulong)ptr5;
														}
														if (span2[3] != null)
														{
															dataDescriptor[span[3]].Ptr = (ulong)ptr6;
														}
														if (span2[4] != null)
														{
															dataDescriptor[span[4]].Ptr = (ulong)ptr8;
														}
														if (span2[5] != null)
														{
															dataDescriptor[span[5]].Ptr = (ulong)ptr9;
														}
														if (span2[6] != null)
														{
															dataDescriptor[span[6]].Ptr = (ulong)ptr11;
														}
														if (span2[7] != null)
														{
															dataDescriptor[span[7]].Ptr = (ulong)ptr12;
														}
														writeEventErrorCode = _eventProvider.EventWriteTransfer(in eventDescriptor, eventHandle, activityID, childActivityID, num, ptr);
													}
													else
													{
														fixed (char* ptr14 = &((string)obj8).GetPinnableReference())
														{
															char* ptr12;
															intPtr4 = (ptr12 = ptr14);
															dataDescriptor = ptr;
															if (span2[0] != null)
															{
																dataDescriptor[span[0]].Ptr = (ulong)ptr13;
															}
															if (span2[1] != null)
															{
																dataDescriptor[span[1]].Ptr = (ulong)ptr3;
															}
															if (span2[2] != null)
															{
																dataDescriptor[span[2]].Ptr = (ulong)ptr5;
															}
															if (span2[3] != null)
															{
																dataDescriptor[span[3]].Ptr = (ulong)ptr6;
															}
															if (span2[4] != null)
															{
																dataDescriptor[span[4]].Ptr = (ulong)ptr8;
															}
															if (span2[5] != null)
															{
																dataDescriptor[span[5]].Ptr = (ulong)ptr9;
															}
															if (span2[6] != null)
															{
																dataDescriptor[span[6]].Ptr = (ulong)ptr11;
															}
															if (span2[7] != null)
															{
																dataDescriptor[span[7]].Ptr = (ulong)ptr12;
															}
															writeEventErrorCode = _eventProvider.EventWriteTransfer(in eventDescriptor, eventHandle, activityID, childActivityID, num, ptr);
														}
													}
												}
											}
											else
											{
												fixed (char* ptr15 = &((string)obj6).GetPinnableReference())
												{
													char* ptr9;
													intPtr3 = (ptr9 = ptr15);
													obj7 = (string)span2[6];
													fixed (char* ptr10 = (string)obj7)
													{
														char* ptr11 = ptr10;
														obj8 = (string)span2[7];
														if (obj8 == null)
														{
															char* ptr12;
															intPtr4 = (ptr12 = null);
															dataDescriptor = ptr;
															if (span2[0] != null)
															{
																dataDescriptor[span[0]].Ptr = (ulong)ptr13;
															}
															if (span2[1] != null)
															{
																dataDescriptor[span[1]].Ptr = (ulong)ptr3;
															}
															if (span2[2] != null)
															{
																dataDescriptor[span[2]].Ptr = (ulong)ptr5;
															}
															if (span2[3] != null)
															{
																dataDescriptor[span[3]].Ptr = (ulong)ptr6;
															}
															if (span2[4] != null)
															{
																dataDescriptor[span[4]].Ptr = (ulong)ptr8;
															}
															if (span2[5] != null)
															{
																dataDescriptor[span[5]].Ptr = (ulong)ptr9;
															}
															if (span2[6] != null)
															{
																dataDescriptor[span[6]].Ptr = (ulong)ptr11;
															}
															if (span2[7] != null)
															{
																dataDescriptor[span[7]].Ptr = (ulong)ptr12;
															}
															writeEventErrorCode = _eventProvider.EventWriteTransfer(in eventDescriptor, eventHandle, activityID, childActivityID, num, ptr);
														}
														else
														{
															fixed (char* ptr14 = &((string)obj8).GetPinnableReference())
															{
																char* ptr12;
																intPtr4 = (ptr12 = ptr14);
																dataDescriptor = ptr;
																if (span2[0] != null)
																{
																	dataDescriptor[span[0]].Ptr = (ulong)ptr13;
																}
																if (span2[1] != null)
																{
																	dataDescriptor[span[1]].Ptr = (ulong)ptr3;
																}
																if (span2[2] != null)
																{
																	dataDescriptor[span[2]].Ptr = (ulong)ptr5;
																}
																if (span2[3] != null)
																{
																	dataDescriptor[span[3]].Ptr = (ulong)ptr6;
																}
																if (span2[4] != null)
																{
																	dataDescriptor[span[4]].Ptr = (ulong)ptr8;
																}
																if (span2[5] != null)
																{
																	dataDescriptor[span[5]].Ptr = (ulong)ptr9;
																}
																if (span2[6] != null)
																{
																	dataDescriptor[span[6]].Ptr = (ulong)ptr11;
																}
																if (span2[7] != null)
																{
																	dataDescriptor[span[7]].Ptr = (ulong)ptr12;
																}
																writeEventErrorCode = _eventProvider.EventWriteTransfer(in eventDescriptor, eventHandle, activityID, childActivityID, num, ptr);
															}
														}
													}
												}
											}
										}
									}
								}
							}
						}
					}
				}
			}
			else
			{
				dataDescriptor = ptr;
				GCHandle[] array = new GCHandle[i];
				for (int l = 0; l < i; l++)
				{
					array[l] = GCHandle.Alloc(span2[l], GCHandleType.Pinned);
					if (span2[l] is string)
					{
						fixed (char* ptr18 = (string)span2[l])
						{
							dataDescriptor[span[l]].Ptr = (ulong)ptr18;
						}
					}
					else
					{
						fixed (byte* ptr19 = (byte[])span2[l])
						{
							dataDescriptor[span[l]].Ptr = (ulong)ptr19;
						}
					}
				}
				writeEventErrorCode = _eventProvider.EventWriteTransfer(in eventDescriptor, eventHandle, activityID, childActivityID, num, ptr);
				for (int m = 0; m < i; m++)
				{
					array[m].Free();
				}
			}
		}
		if (writeEventErrorCode != 0)
		{
			SetLastError(writeEventErrorCode);
			return false;
		}
		return true;
	}

	protected internal unsafe bool WriteEvent(ref EventDescriptor eventDescriptor, nint eventHandle, Guid* activityID, Guid* childActivityID, int dataCount, nint data)
	{
		_ = 0u;
		WriteEventErrorCode writeEventErrorCode = _eventProvider.EventWriteTransfer(in eventDescriptor, eventHandle, activityID, childActivityID, dataCount, (EventData*)data);
		if (writeEventErrorCode != 0)
		{
			SetLastError(writeEventErrorCode);
			return false;
		}
		return true;
	}

	internal unsafe bool WriteEventRaw(ref EventDescriptor eventDescriptor, nint eventHandle, Guid* activityID, Guid* relatedActivityID, int dataCount, nint data)
	{
		WriteEventErrorCode writeEventErrorCode = _eventProvider.EventWriteTransfer(in eventDescriptor, eventHandle, activityID, relatedActivityID, dataCount, (EventData*)data);
		if (writeEventErrorCode != 0)
		{
			SetLastError(writeEventErrorCode);
			return false;
		}
		return true;
	}

	internal unsafe int SetInformation(Interop.Advapi32.EVENT_INFO_CLASS eventInfoClass, void* data, uint dataSize)
	{
		return ((EtwEventProvider)_eventProvider).SetInformation(eventInfoClass, data, dataSize);
	}
}
