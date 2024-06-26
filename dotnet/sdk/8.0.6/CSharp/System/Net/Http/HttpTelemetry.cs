using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Tracing;
using System.Threading;

namespace System.Net.Http;

[EventSource(Name = "System.Net.Http")]
internal sealed class HttpTelemetry : EventSource
{
	public static class Keywords
	{
		public const EventKeywords RequestFailedDetailed = (EventKeywords)1L;
	}

	public static readonly HttpTelemetry Log = new HttpTelemetry();

	private long _startedRequests;

	private long _stoppedRequests;

	private long _failedRequests;

	private long _openedHttp11Connections;

	private long _openedHttp20Connections;

	private long _openedHttp30Connections;

	private IncrementingPollingCounter _startedRequestsPerSecondCounter;

	private IncrementingPollingCounter _failedRequestsPerSecondCounter;

	private PollingCounter _startedRequestsCounter;

	private PollingCounter _currentRequestsCounter;

	private PollingCounter _failedRequestsCounter;

	private PollingCounter _totalHttp11ConnectionsCounter;

	private PollingCounter _totalHttp20ConnectionsCounter;

	private PollingCounter _totalHttp30ConnectionsCounter;

	private EventCounter _http11RequestsQueueDurationCounter;

	private EventCounter _http20RequestsQueueDurationCounter;

	private EventCounter _http30RequestsQueueDurationCounter;

	[Event(1, Level = EventLevel.Informational)]
	private void RequestStart(string scheme, string host, int port, string pathAndQuery, byte versionMajor, byte versionMinor, HttpVersionPolicy versionPolicy)
	{
		Interlocked.Increment(ref _startedRequests);
		WriteEvent(1, scheme, host, port, pathAndQuery, versionMajor, versionMinor, versionPolicy);
	}

	[NonEvent]
	public void RequestStart(HttpRequestMessage request)
	{
		RequestStart(request.RequestUri.Scheme, request.RequestUri.IdnHost, request.RequestUri.Port, request.RequestUri.PathAndQuery, (byte)request.Version.Major, (byte)request.Version.Minor, request.VersionPolicy);
	}

	[NonEvent]
	public void RequestStop(HttpResponseMessage response)
	{
		RequestStop((int)(response?.StatusCode ?? ((HttpStatusCode)(-1))));
	}

	[Event(2, Level = EventLevel.Informational, Version = 1)]
	private void RequestStop(int statusCode)
	{
		Interlocked.Increment(ref _stoppedRequests);
		WriteEvent(2, statusCode);
	}

	[NonEvent]
	public void RequestFailed(Exception exception)
	{
		Interlocked.Increment(ref _failedRequests);
		if (IsEnabled(EventLevel.Error, EventKeywords.None))
		{
			RequestFailed(exception.Message);
			if (IsEnabled(EventLevel.Error, (EventKeywords)1L))
			{
				RequestFailedDetailed(exception.ToString());
			}
		}
	}

	[Event(3, Level = EventLevel.Error, Version = 1)]
	private void RequestFailed(string exceptionMessage)
	{
		WriteEvent(3, exceptionMessage);
	}

	[NonEvent]
	private void ConnectionEstablished(byte versionMajor, byte versionMinor, long connectionId, string scheme, string host, int port, IPEndPoint remoteEndPoint)
	{
		string remoteAddress = remoteEndPoint?.Address?.ToString();
		ConnectionEstablished(versionMajor, versionMinor, connectionId, scheme, host, port, remoteAddress);
	}

	[Event(4, Level = EventLevel.Informational, Version = 1)]
	private void ConnectionEstablished(byte versionMajor, byte versionMinor, long connectionId, string scheme, string host, int port, string remoteAddress)
	{
		WriteEvent(4, versionMajor, versionMinor, connectionId, scheme, host, port, remoteAddress);
	}

	[Event(5, Level = EventLevel.Informational, Version = 1)]
	private void ConnectionClosed(byte versionMajor, byte versionMinor, long connectionId)
	{
		WriteEvent(5, versionMajor, versionMinor, connectionId);
	}

	[Event(6, Level = EventLevel.Informational)]
	private void RequestLeftQueue(double timeOnQueueMilliseconds, byte versionMajor, byte versionMinor)
	{
		WriteEvent(6, timeOnQueueMilliseconds, versionMajor, versionMinor);
	}

	[Event(7, Level = EventLevel.Informational, Version = 1)]
	public void RequestHeadersStart(long connectionId)
	{
		WriteEvent(7, connectionId);
	}

	[Event(8, Level = EventLevel.Informational)]
	public void RequestHeadersStop()
	{
		WriteEvent(8);
	}

	[Event(9, Level = EventLevel.Informational)]
	public void RequestContentStart()
	{
		WriteEvent(9);
	}

	[Event(10, Level = EventLevel.Informational)]
	public void RequestContentStop(long contentLength)
	{
		WriteEvent(10, contentLength);
	}

	[Event(11, Level = EventLevel.Informational)]
	public void ResponseHeadersStart()
	{
		WriteEvent(11);
	}

	[Event(12, Level = EventLevel.Informational, Version = 1)]
	public void ResponseHeadersStop(int statusCode)
	{
		WriteEvent(12, statusCode);
	}

	[Event(13, Level = EventLevel.Informational)]
	public void ResponseContentStart()
	{
		WriteEvent(13);
	}

	[Event(14, Level = EventLevel.Informational)]
	public void ResponseContentStop()
	{
		WriteEvent(14);
	}

	[Event(15, Level = EventLevel.Error, Keywords = (EventKeywords)1L)]
	private void RequestFailedDetailed(string exception)
	{
		WriteEvent(15, exception);
	}

	[Event(16, Level = EventLevel.Informational)]
	public void Redirect(string redirectUri)
	{
		WriteEvent(16, redirectUri);
	}

	[NonEvent]
	public void Http11ConnectionEstablished(long connectionId, string scheme, string host, int port, IPEndPoint remoteEndPoint)
	{
		Interlocked.Increment(ref _openedHttp11Connections);
		ConnectionEstablished(1, 1, connectionId, scheme, host, port, remoteEndPoint);
	}

	[NonEvent]
	public void Http11ConnectionClosed(long connectionId)
	{
		long num = Interlocked.Decrement(ref _openedHttp11Connections);
		ConnectionClosed(1, 1, connectionId);
	}

	[NonEvent]
	public void Http20ConnectionEstablished(long connectionId, string scheme, string host, int port, IPEndPoint remoteEndPoint)
	{
		Interlocked.Increment(ref _openedHttp20Connections);
		ConnectionEstablished(2, 0, connectionId, scheme, host, port, remoteEndPoint);
	}

	[NonEvent]
	public void Http20ConnectionClosed(long connectionId)
	{
		long num = Interlocked.Decrement(ref _openedHttp20Connections);
		ConnectionClosed(2, 0, connectionId);
	}

	[NonEvent]
	public void Http30ConnectionEstablished(long connectionId, string scheme, string host, int port, IPEndPoint remoteEndPoint)
	{
		Interlocked.Increment(ref _openedHttp30Connections);
		ConnectionEstablished(3, 0, connectionId, scheme, host, port, remoteEndPoint);
	}

	[NonEvent]
	public void Http30ConnectionClosed(long connectionId)
	{
		long num = Interlocked.Decrement(ref _openedHttp30Connections);
		ConnectionClosed(3, 0, connectionId);
	}

	[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode", Justification = "Parameters to this method are primitive and are trimmer safe")]
	[NonEvent]
	private unsafe void WriteEvent(int eventId, string arg1, string arg2, int arg3, string arg4, byte arg5, byte arg6, HttpVersionPolicy arg7)
	{
		//The blocks IL_0040, IL_0043, IL_0055, IL_01b5 are reachable both inside and outside the pinned region starting at IL_003d. ILSpy has duplicated these blocks in order to place them both within and outside the `fixed` statement.
		if (arg1 == null)
		{
			arg1 = "";
		}
		if (arg2 == null)
		{
			arg2 = "";
		}
		if (arg4 == null)
		{
			arg4 = "";
		}
		fixed (char* dataPointer3 = arg1)
		{
			char* intPtr;
			EventData* intPtr2;
			nint num;
			nint num2;
			nint num3;
			nint num4;
			nint num5;
			nint num6;
			if (arg2 == null)
			{
				char* dataPointer;
				intPtr = (dataPointer = null);
				fixed (char* ptr = arg4)
				{
					char* dataPointer2 = ptr;
					EventData* ptr2 = stackalloc EventData[7];
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
						DataPointer = (nint)(&arg3),
						Size = 4
					};
					num3 = (nint)(ptr2 + 3);
					*(EventData*)num3 = new EventData
					{
						DataPointer = (nint)dataPointer2,
						Size = (arg4.Length + 1) * 2
					};
					num4 = (nint)(ptr2 + 4);
					*(EventData*)num4 = new EventData
					{
						DataPointer = (nint)(&arg5),
						Size = 1
					};
					num5 = (nint)(ptr2 + 5);
					*(EventData*)num5 = new EventData
					{
						DataPointer = (nint)(&arg6),
						Size = 1
					};
					num6 = (nint)(ptr2 + 6);
					*(EventData*)num6 = new EventData
					{
						DataPointer = (nint)(&arg7),
						Size = 4
					};
					WriteEventCore(eventId, 7, ptr2);
				}
				return;
			}
			fixed (char* ptr3 = &arg2.GetPinnableReference())
			{
				char* dataPointer;
				intPtr = (dataPointer = ptr3);
				fixed (char* ptr = arg4)
				{
					char* dataPointer2 = ptr;
					EventData* ptr2 = stackalloc EventData[7];
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
						DataPointer = (nint)(&arg3),
						Size = 4
					};
					num3 = (nint)(ptr2 + 3);
					*(EventData*)num3 = new EventData
					{
						DataPointer = (nint)dataPointer2,
						Size = (arg4.Length + 1) * 2
					};
					num4 = (nint)(ptr2 + 4);
					*(EventData*)num4 = new EventData
					{
						DataPointer = (nint)(&arg5),
						Size = 1
					};
					num5 = (nint)(ptr2 + 5);
					*(EventData*)num5 = new EventData
					{
						DataPointer = (nint)(&arg6),
						Size = 1
					};
					num6 = (nint)(ptr2 + 6);
					*(EventData*)num6 = new EventData
					{
						DataPointer = (nint)(&arg7),
						Size = 4
					};
					WriteEventCore(eventId, 7, ptr2);
				}
			}
		}
	}

	[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode", Justification = "Parameters to this method are primitive and are trimmer safe")]
	[NonEvent]
	private unsafe void WriteEvent(int eventId, double arg1, byte arg2, byte arg3)
	{
		EventData* ptr = stackalloc EventData[3];
		*ptr = new EventData
		{
			DataPointer = (nint)(&arg1),
			Size = 8
		};
		ptr[1] = new EventData
		{
			DataPointer = (nint)(&arg2),
			Size = 1
		};
		ptr[2] = new EventData
		{
			DataPointer = (nint)(&arg3),
			Size = 1
		};
		WriteEventCore(eventId, 3, ptr);
	}

	[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode", Justification = "Parameters to this method are primitive and are trimmer safe")]
	[NonEvent]
	private unsafe void WriteEvent(int eventId, byte arg1, byte arg2, long arg3)
	{
		EventData* ptr = stackalloc EventData[3];
		*ptr = new EventData
		{
			DataPointer = (nint)(&arg1),
			Size = 1
		};
		ptr[1] = new EventData
		{
			DataPointer = (nint)(&arg2),
			Size = 1
		};
		ptr[2] = new EventData
		{
			DataPointer = (nint)(&arg3),
			Size = 8
		};
		WriteEventCore(eventId, 3, ptr);
	}

	[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode", Justification = "Parameters to this method are primitive and are trimmer safe")]
	[NonEvent]
	private unsafe void WriteEvent(int eventId, byte arg1, byte arg2, long arg3, string arg4, string arg5, int arg6, string arg7)
	{
		//The blocks IL_0046, IL_0049, IL_005b, IL_01bd are reachable both inside and outside the pinned region starting at IL_0043. ILSpy has duplicated these blocks in order to place them both within and outside the `fixed` statement.
		if (arg4 == null)
		{
			arg4 = "";
		}
		if (arg5 == null)
		{
			arg5 = "";
		}
		if (arg7 == null)
		{
			arg7 = "";
		}
		fixed (char* dataPointer3 = arg4)
		{
			char* intPtr;
			EventData* intPtr2;
			nint num;
			nint num2;
			nint num3;
			nint num4;
			nint num5;
			nint num6;
			if (arg5 == null)
			{
				char* dataPointer;
				intPtr = (dataPointer = null);
				fixed (char* ptr = arg7)
				{
					char* dataPointer2 = ptr;
					EventData* ptr2 = stackalloc EventData[7];
					intPtr2 = ptr2;
					*intPtr2 = new EventData
					{
						DataPointer = (nint)(&arg1),
						Size = 1
					};
					num = (nint)(ptr2 + 1);
					*(EventData*)num = new EventData
					{
						DataPointer = (nint)(&arg2),
						Size = 1
					};
					num2 = (nint)(ptr2 + 2);
					*(EventData*)num2 = new EventData
					{
						DataPointer = (nint)(&arg3),
						Size = 8
					};
					num3 = (nint)(ptr2 + 3);
					*(EventData*)num3 = new EventData
					{
						DataPointer = (nint)dataPointer3,
						Size = (arg4.Length + 1) * 2
					};
					num4 = (nint)(ptr2 + 4);
					*(EventData*)num4 = new EventData
					{
						DataPointer = (nint)dataPointer,
						Size = (arg5.Length + 1) * 2
					};
					num5 = (nint)(ptr2 + 5);
					*(EventData*)num5 = new EventData
					{
						DataPointer = (nint)(&arg6),
						Size = 4
					};
					num6 = (nint)(ptr2 + 6);
					*(EventData*)num6 = new EventData
					{
						DataPointer = (nint)dataPointer2,
						Size = (arg7.Length + 1) * 2
					};
					WriteEventCore(eventId, 7, ptr2);
				}
				return;
			}
			fixed (char* ptr3 = &arg5.GetPinnableReference())
			{
				char* dataPointer;
				intPtr = (dataPointer = ptr3);
				fixed (char* ptr = arg7)
				{
					char* dataPointer2 = ptr;
					EventData* ptr2 = stackalloc EventData[7];
					intPtr2 = ptr2;
					*intPtr2 = new EventData
					{
						DataPointer = (nint)(&arg1),
						Size = 1
					};
					num = (nint)(ptr2 + 1);
					*(EventData*)num = new EventData
					{
						DataPointer = (nint)(&arg2),
						Size = 1
					};
					num2 = (nint)(ptr2 + 2);
					*(EventData*)num2 = new EventData
					{
						DataPointer = (nint)(&arg3),
						Size = 8
					};
					num3 = (nint)(ptr2 + 3);
					*(EventData*)num3 = new EventData
					{
						DataPointer = (nint)dataPointer3,
						Size = (arg4.Length + 1) * 2
					};
					num4 = (nint)(ptr2 + 4);
					*(EventData*)num4 = new EventData
					{
						DataPointer = (nint)dataPointer,
						Size = (arg5.Length + 1) * 2
					};
					num5 = (nint)(ptr2 + 5);
					*(EventData*)num5 = new EventData
					{
						DataPointer = (nint)(&arg6),
						Size = 4
					};
					num6 = (nint)(ptr2 + 6);
					*(EventData*)num6 = new EventData
					{
						DataPointer = (nint)dataPointer2,
						Size = (arg7.Length + 1) * 2
					};
					WriteEventCore(eventId, 7, ptr2);
				}
			}
		}
	}

	[NonEvent]
	public void RequestLeftQueue(int versionMajor, TimeSpan duration)
	{
		EventCounter eventCounter = versionMajor switch
		{
			1 => _http11RequestsQueueDurationCounter, 
			2 => _http20RequestsQueueDurationCounter, 
			_ => _http30RequestsQueueDurationCounter, 
		};
		double totalMilliseconds = duration.TotalMilliseconds;
		eventCounter?.WriteMetric(totalMilliseconds);
		RequestLeftQueue(totalMilliseconds, (byte)versionMajor, (versionMajor == 1) ? ((byte)1) : ((byte)0));
	}

	protected override void OnEventCommand(EventCommandEventArgs command)
	{
		if (command.Command != EventCommand.Enable)
		{
			return;
		}
		if (_startedRequestsCounter == null)
		{
			_startedRequestsCounter = new PollingCounter("requests-started", this, () => Interlocked.Read(ref _startedRequests))
			{
				DisplayName = "Requests Started"
			};
		}
		if (_startedRequestsPerSecondCounter == null)
		{
			_startedRequestsPerSecondCounter = new IncrementingPollingCounter("requests-started-rate", this, () => Interlocked.Read(ref _startedRequests))
			{
				DisplayName = "Requests Started Rate",
				DisplayRateTimeScale = TimeSpan.FromSeconds(1.0)
			};
		}
		if (_failedRequestsCounter == null)
		{
			_failedRequestsCounter = new PollingCounter("requests-failed", this, () => Interlocked.Read(ref _failedRequests))
			{
				DisplayName = "Requests Failed"
			};
		}
		if (_failedRequestsPerSecondCounter == null)
		{
			_failedRequestsPerSecondCounter = new IncrementingPollingCounter("requests-failed-rate", this, () => Interlocked.Read(ref _failedRequests))
			{
				DisplayName = "Requests Failed Rate",
				DisplayRateTimeScale = TimeSpan.FromSeconds(1.0)
			};
		}
		if (_currentRequestsCounter == null)
		{
			_currentRequestsCounter = new PollingCounter("current-requests", this, () => -Interlocked.Read(ref _stoppedRequests) + Interlocked.Read(ref _startedRequests))
			{
				DisplayName = "Current Requests"
			};
		}
		if (_totalHttp11ConnectionsCounter == null)
		{
			_totalHttp11ConnectionsCounter = new PollingCounter("http11-connections-current-total", this, () => Interlocked.Read(ref _openedHttp11Connections))
			{
				DisplayName = "Current Http 1.1 Connections"
			};
		}
		if (_totalHttp20ConnectionsCounter == null)
		{
			_totalHttp20ConnectionsCounter = new PollingCounter("http20-connections-current-total", this, () => Interlocked.Read(ref _openedHttp20Connections))
			{
				DisplayName = "Current Http 2.0 Connections"
			};
		}
		if (_totalHttp30ConnectionsCounter == null)
		{
			_totalHttp30ConnectionsCounter = new PollingCounter("http30-connections-current-total", this, () => Interlocked.Read(ref _openedHttp30Connections))
			{
				DisplayName = "Current Http 3.0 Connections"
			};
		}
		if (_http11RequestsQueueDurationCounter == null)
		{
			_http11RequestsQueueDurationCounter = new EventCounter("http11-requests-queue-duration", this)
			{
				DisplayName = "HTTP 1.1 Requests Queue Duration",
				DisplayUnits = "ms"
			};
		}
		if (_http20RequestsQueueDurationCounter == null)
		{
			_http20RequestsQueueDurationCounter = new EventCounter("http20-requests-queue-duration", this)
			{
				DisplayName = "HTTP 2.0 Requests Queue Duration",
				DisplayUnits = "ms"
			};
		}
		if (_http30RequestsQueueDurationCounter == null)
		{
			_http30RequestsQueueDurationCounter = new EventCounter("http30-requests-queue-duration", this)
			{
				DisplayName = "HTTP 3.0 Requests Queue Duration",
				DisplayUnits = "ms"
			};
		}
	}
}
