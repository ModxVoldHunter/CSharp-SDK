using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Net.Sockets;

namespace System.Net;

internal static class NameResolutionMetrics
{
	private static readonly Meter s_meter = new Meter("System.Net.NameResolution");

	private static readonly Histogram<double> s_lookupDuration = s_meter.CreateHistogram<double>("dns.lookup.duration", "s", "Measures the time taken to perform a DNS lookup.");

	public static bool IsEnabled()
	{
		return s_lookupDuration.Enabled;
	}

	public static void AfterResolution(TimeSpan duration, string hostName, Exception exception)
	{
		KeyValuePair<string, object> keyValuePair = KeyValuePair.Create("dns.question.name", (object)hostName);
		if (exception == null)
		{
			s_lookupDuration.Record(duration.TotalSeconds, keyValuePair);
			return;
		}
		KeyValuePair<string, object> tag = KeyValuePair.Create("error.type", (object)GetErrorType(exception));
		s_lookupDuration.Record(duration.TotalSeconds, keyValuePair, tag);
	}

	private static string GetErrorType(Exception exception)
	{
		return (exception as SocketException)?.SocketErrorCode switch
		{
			SocketError.HostNotFound => "host_not_found", 
			SocketError.TryAgain => "try_again", 
			SocketError.AddressFamilyNotSupported => "address_family_not_supported", 
			SocketError.NoRecovery => "no_recovery", 
			_ => exception.GetType().FullName, 
		};
	}
}
