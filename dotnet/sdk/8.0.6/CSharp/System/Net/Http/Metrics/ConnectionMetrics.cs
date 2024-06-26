using System.Collections.Generic;
using System.Diagnostics;

namespace System.Net.Http.Metrics;

internal sealed class ConnectionMetrics
{
	private readonly SocketsHttpHandlerMetrics _metrics;

	private readonly bool _openConnectionsEnabled;

	private readonly object _protocolVersionTag;

	private readonly object _schemeTag;

	private readonly object _hostTag;

	private readonly object _portTag;

	private readonly object _peerAddressTag;

	private bool _currentlyIdle;

	public ConnectionMetrics(SocketsHttpHandlerMetrics metrics, string protocolVersion, string scheme, string host, int? port, string peerAddress)
	{
		_metrics = metrics;
		_openConnectionsEnabled = _metrics.OpenConnections.Enabled;
		_protocolVersionTag = protocolVersion;
		_schemeTag = scheme;
		_hostTag = host;
		_portTag = port;
		_peerAddressTag = peerAddress;
	}

	private TagList GetTags()
	{
		TagList result = default(TagList);
		result.Add("network.protocol.version", _protocolVersionTag);
		result.Add("url.scheme", _schemeTag);
		result.Add("server.address", _hostTag);
		if (_portTag != null)
		{
			result.Add("server.port", _portTag);
		}
		if (_peerAddressTag != null)
		{
			result.Add("network.peer.address", _peerAddressTag);
		}
		return result;
	}

	private static KeyValuePair<string, object> GetStateTag(bool idle)
	{
		return new KeyValuePair<string, object>("http.connection.state", idle ? "idle" : "active");
	}

	public void ConnectionEstablished()
	{
		if (_openConnectionsEnabled)
		{
			_currentlyIdle = true;
			TagList tagList = GetTags();
			tagList.Add(GetStateTag(idle: true));
			_metrics.OpenConnections.Add(1L, in tagList);
		}
	}

	public void ConnectionClosed(long durationMs)
	{
		TagList tagList = GetTags();
		if (_metrics.ConnectionDuration.Enabled)
		{
			_metrics.ConnectionDuration.Record((double)durationMs / 1000.0, in tagList);
		}
		if (_openConnectionsEnabled)
		{
			tagList.Add(GetStateTag(_currentlyIdle));
			_metrics.OpenConnections.Add(-1L, in tagList);
		}
	}

	public void IdleStateChanged(bool idle)
	{
		if (_openConnectionsEnabled && _currentlyIdle != idle)
		{
			_currentlyIdle = idle;
			TagList tagList = GetTags();
			tagList.Add(GetStateTag(!idle));
			_metrics.OpenConnections.Add(-1L, in tagList);
			tagList[tagList.Count - 1] = GetStateTag(idle);
			_metrics.OpenConnections.Add(1L, in tagList);
		}
	}
}
