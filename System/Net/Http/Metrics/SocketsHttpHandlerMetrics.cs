using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace System.Net.Http.Metrics;

internal sealed class SocketsHttpHandlerMetrics
{
	public readonly UpDownCounter<long> OpenConnections = meter.CreateUpDownCounter<long>("http.client.open_connections", "{connection}", "Number of outbound HTTP connections that are currently active or idle on the client.");

	public readonly Histogram<double> ConnectionDuration = meter.CreateHistogram<double>("http.client.connection.duration", "s", "The duration of successfully established outbound HTTP connections.");

	public readonly Histogram<double> RequestsQueueDuration = meter.CreateHistogram<double>("http.client.request.time_in_queue", "s", "The amount of time requests spent on a queue waiting for an available connection.");

	public SocketsHttpHandlerMetrics(Meter meter)
	{
	}

	public void RequestLeftQueue(HttpRequestMessage request, HttpConnectionPool pool, TimeSpan duration, int versionMajor)
	{
		if (RequestsQueueDuration.Enabled)
		{
			TagList tagList = default(TagList);
			tagList.Add("network.protocol.version", versionMajor switch
			{
				1 => "1.1", 
				2 => "2", 
				_ => "3", 
			});
			tagList.Add("url.scheme", pool.IsSecure ? "https" : "http");
			tagList.Add("server.address", pool.OriginAuthority.HostValue);
			if (!pool.IsDefaultPort)
			{
				tagList.Add("server.port", pool.OriginAuthority.Port);
			}
			tagList.Add(MetricsHandler.GetMethodTag(request.Method));
			RequestsQueueDuration.Record(duration.TotalSeconds, in tagList);
		}
	}
}
