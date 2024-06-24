using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.Net.Sockets;
using System.Threading;

namespace System.Net;

[EventSource(Name = "System.Net.NameResolution")]
internal sealed class NameResolutionTelemetry : EventSource
{
	public static readonly NameResolutionTelemetry Log = new NameResolutionTelemetry();

	private PollingCounter _lookupsRequestedCounter;

	private PollingCounter _currentLookupsCounter;

	private EventCounter _lookupsDuration;

	private long _lookupsRequested;

	private long _currentLookups;

	protected override void OnEventCommand(EventCommandEventArgs command)
	{
		if (command.Command != EventCommand.Enable)
		{
			return;
		}
		if (_lookupsRequestedCounter == null)
		{
			_lookupsRequestedCounter = new PollingCounter("dns-lookups-requested", this, () => Interlocked.Read(ref _lookupsRequested))
			{
				DisplayName = "DNS Lookups Requested"
			};
		}
		if (_currentLookupsCounter == null)
		{
			_currentLookupsCounter = new PollingCounter("current-dns-lookups", this, () => Interlocked.Read(ref _currentLookups))
			{
				DisplayName = "Current DNS Lookups"
			};
		}
		if (_lookupsDuration == null)
		{
			_lookupsDuration = new EventCounter("dns-lookups-duration", this)
			{
				DisplayName = "Average DNS Lookup Duration",
				DisplayUnits = "ms"
			};
		}
	}

	[Event(1, Level = EventLevel.Informational)]
	private void ResolutionStart(string hostNameOrAddress)
	{
		WriteEvent(1, hostNameOrAddress);
	}

	[Event(2, Level = EventLevel.Informational)]
	private void ResolutionStop()
	{
		WriteEvent(2);
	}

	[Event(3, Level = EventLevel.Informational)]
	private void ResolutionFailed()
	{
		WriteEvent(3);
	}

	[NonEvent]
	public long BeforeResolution(object hostNameOrAddress)
	{
		if (IsEnabled())
		{
			Interlocked.Increment(ref _lookupsRequested);
			Interlocked.Increment(ref _currentLookups);
			if (IsEnabled(EventLevel.Informational, EventKeywords.None))
			{
				string hostnameFromStateObject = GetHostnameFromStateObject(hostNameOrAddress);
				ResolutionStart(hostnameFromStateObject);
			}
			return Stopwatch.GetTimestamp();
		}
		if (!NameResolutionMetrics.IsEnabled())
		{
			return 0L;
		}
		return Stopwatch.GetTimestamp();
	}

	[NonEvent]
	public void AfterResolution(object hostNameOrAddress, long? startingTimestamp, Exception exception = null)
	{
		if (startingTimestamp == 0)
		{
			return;
		}
		TimeSpan elapsedTime = Stopwatch.GetElapsedTime(startingTimestamp.Value);
		if (IsEnabled())
		{
			Interlocked.Decrement(ref _currentLookups);
			_lookupsDuration?.WriteMetric(elapsedTime.TotalMilliseconds);
			if (IsEnabled(EventLevel.Informational, EventKeywords.None))
			{
				if (exception != null)
				{
					ResolutionFailed();
				}
				ResolutionStop();
			}
		}
		if (NameResolutionMetrics.IsEnabled())
		{
			NameResolutionMetrics.AfterResolution(elapsedTime, GetHostnameFromStateObject(hostNameOrAddress), exception);
		}
	}

	private static string GetHostnameFromStateObject(object hostNameOrAddress)
	{
		if (!(hostNameOrAddress is string result))
		{
			if (!(hostNameOrAddress is KeyValuePair<string, AddressFamily> { Key: var key }))
			{
				if (!(hostNameOrAddress is IPAddress iPAddress))
				{
					if (hostNameOrAddress is KeyValuePair<IPAddress, AddressFamily> keyValuePair2)
					{
						return keyValuePair2.Key.ToString();
					}
					return null;
				}
				return iPAddress.ToString();
			}
			return key;
		}
		return result;
	}
}
