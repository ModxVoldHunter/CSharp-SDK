using System.Collections.Generic;
using System.Runtime.Versioning;
using System.Text;

namespace System.Diagnostics.Tracing;

[UnsupportedOSPlatform("browser")]
public abstract class DiagnosticCounter : IDisposable
{
	private string _displayName = "";

	private string _displayUnits = "";

	private CounterGroup _group;

	private Dictionary<string, string> _metadata;

	public string DisplayName
	{
		get
		{
			return _displayName;
		}
		set
		{
			ArgumentNullException.ThrowIfNull(DisplayName, "DisplayName");
			_displayName = value;
		}
	}

	public string DisplayUnits
	{
		get
		{
			return _displayUnits;
		}
		set
		{
			ArgumentNullException.ThrowIfNull(DisplayUnits, "DisplayUnits");
			_displayUnits = value;
		}
	}

	public string Name { get; }

	public EventSource EventSource { get; }

	internal DiagnosticCounter(string Name, EventSource EventSource)
	{
		ArgumentNullException.ThrowIfNull(Name, "Name");
		ArgumentNullException.ThrowIfNull(EventSource, "EventSource");
		this.Name = Name;
		this.EventSource = EventSource;
	}

	private protected void Publish()
	{
		_group = CounterGroup.GetCounterGroup(EventSource);
		_group.Add(this);
	}

	public void Dispose()
	{
		if (_group != null)
		{
			_group.Remove(this);
			_group = null;
		}
	}

	public void AddMetadata(string key, string? value)
	{
		lock (this)
		{
			if (_metadata == null)
			{
				_metadata = new Dictionary<string, string>();
			}
			_metadata.Add(key, value);
		}
	}

	internal abstract void WritePayload(float intervalSec, int pollingIntervalMillisec);

	internal void ReportOutOfBandMessage(string message)
	{
		EventSource.ReportOutOfBandMessage(message);
	}

	internal string GetMetadataString()
	{
		if (_metadata == null)
		{
			return "";
		}
		Dictionary<string, string>.Enumerator enumerator = _metadata.GetEnumerator();
		bool flag = enumerator.MoveNext();
		KeyValuePair<string, string> current = enumerator.Current;
		if (!enumerator.MoveNext())
		{
			return current.Key + ":" + current.Value;
		}
		StringBuilder stringBuilder = new StringBuilder().Append(current.Key).Append(':').Append(current.Value);
		do
		{
			current = enumerator.Current;
			stringBuilder.Append(',').Append(current.Key).Append(':')
				.Append(current.Value);
		}
		while (enumerator.MoveNext());
		return stringBuilder.ToString();
	}
}
