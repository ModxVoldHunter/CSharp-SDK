using System.Collections.Generic;

namespace System.Diagnostics.Metrics;

public class Meter : IDisposable
{
	private static readonly List<Meter> s_allMeters = new List<Meter>();

	private List<Instrument> _instruments = new List<Instrument>();

	private Dictionary<string, List<Instrument>> _nonObservableInstrumentsCache = new Dictionary<string, List<Instrument>>();

	internal bool Disposed { get; private set; }

	internal static bool IsSupported { get; } = InitializeIsSupported();


	public string Name { get; private set; }

	public string? Version { get; private set; }

	public IEnumerable<KeyValuePair<string, object?>>? Tags { get; private set; }

	public object? Scope { get; private set; }

	private static bool InitializeIsSupported()
	{
		if (!AppContext.TryGetSwitch("System.Diagnostics.Metrics.Meter.IsSupported", out var isEnabled))
		{
			return true;
		}
		return isEnabled;
	}

	public Meter(MeterOptions options)
	{
		if (options == null)
		{
			throw new ArgumentNullException("options");
		}
		Initialize(options.Name, options.Version, options.Tags, options.Scope);
	}

	public Meter(string name)
		: this(name, null, null)
	{
	}

	public Meter(string name, string? version)
		: this(name, version, null)
	{
	}

	public Meter(string name, string? version, IEnumerable<KeyValuePair<string, object?>>? tags, object? scope = null)
	{
		Initialize(name, version, tags, scope);
	}

	private void Initialize(string name, string version, IEnumerable<KeyValuePair<string, object>> tags, object scope = null)
	{
		Name = name ?? throw new ArgumentNullException("name");
		Version = version;
		if (tags != null)
		{
			List<KeyValuePair<string, object>> list = new List<KeyValuePair<string, object>>(tags);
			list.Sort((KeyValuePair<string, object> left, KeyValuePair<string, object> right) => string.Compare(left.Key, right.Key, StringComparison.Ordinal));
			Tags = list;
		}
		Scope = scope;
		if (IsSupported)
		{
			lock (Instrument.SyncObject)
			{
				s_allMeters.Add(this);
			}
			GC.KeepAlive(MetricsEventSource.Log);
		}
	}

	public Counter<T> CreateCounter<T>(string name, string? unit = null, string? description = null) where T : struct
	{
		return CreateCounter<T>(name, unit, description, null);
	}

	public Counter<T> CreateCounter<T>(string name, string? unit, string? description, IEnumerable<KeyValuePair<string, object?>>? tags) where T : struct
	{
		string name = name;
		string unit = unit;
		string description = description;
		IEnumerable<KeyValuePair<string, object>> tags = tags;
		return (Counter<T>)GetOrCreateInstrument<T>(typeof(Counter<T>), name, unit, description, tags, () => new Counter<T>(this, name, unit, description, tags));
	}

	public Histogram<T> CreateHistogram<T>(string name, string? unit = null, string? description = null) where T : struct
	{
		return CreateHistogram<T>(name, unit, description, null);
	}

	public Histogram<T> CreateHistogram<T>(string name, string? unit, string? description, IEnumerable<KeyValuePair<string, object?>>? tags) where T : struct
	{
		string name = name;
		string unit = unit;
		string description = description;
		IEnumerable<KeyValuePair<string, object>> tags = tags;
		return (Histogram<T>)GetOrCreateInstrument<T>(typeof(Histogram<T>), name, unit, description, tags, () => new Histogram<T>(this, name, unit, description, tags));
	}

	public UpDownCounter<T> CreateUpDownCounter<T>(string name, string? unit = null, string? description = null) where T : struct
	{
		return CreateUpDownCounter<T>(name, unit, description, null);
	}

	public UpDownCounter<T> CreateUpDownCounter<T>(string name, string? unit, string? description, IEnumerable<KeyValuePair<string, object?>>? tags) where T : struct
	{
		string name = name;
		string unit = unit;
		string description = description;
		IEnumerable<KeyValuePair<string, object>> tags = tags;
		return (UpDownCounter<T>)GetOrCreateInstrument<T>(typeof(UpDownCounter<T>), name, unit, description, tags, () => new UpDownCounter<T>(this, name, unit, description, tags));
	}

	public ObservableUpDownCounter<T> CreateObservableUpDownCounter<T>(string name, Func<T> observeValue, string? unit = null, string? description = null) where T : struct
	{
		return new ObservableUpDownCounter<T>(this, name, observeValue, unit, description, null);
	}

	public ObservableUpDownCounter<T> CreateObservableUpDownCounter<T>(string name, Func<T> observeValue, string? unit, string? description, IEnumerable<KeyValuePair<string, object?>>? tags) where T : struct
	{
		return new ObservableUpDownCounter<T>(this, name, observeValue, unit, description, tags);
	}

	public ObservableUpDownCounter<T> CreateObservableUpDownCounter<T>(string name, Func<Measurement<T>> observeValue, string? unit = null, string? description = null) where T : struct
	{
		return new ObservableUpDownCounter<T>(this, name, observeValue, unit, description, null);
	}

	public ObservableUpDownCounter<T> CreateObservableUpDownCounter<T>(string name, Func<Measurement<T>> observeValue, string? unit, string? description, IEnumerable<KeyValuePair<string, object?>>? tags) where T : struct
	{
		return new ObservableUpDownCounter<T>(this, name, observeValue, unit, description, tags);
	}

	public ObservableUpDownCounter<T> CreateObservableUpDownCounter<T>(string name, Func<IEnumerable<Measurement<T>>> observeValues, string? unit = null, string? description = null) where T : struct
	{
		return new ObservableUpDownCounter<T>(this, name, observeValues, unit, description, null);
	}

	public ObservableUpDownCounter<T> CreateObservableUpDownCounter<T>(string name, Func<IEnumerable<Measurement<T>>> observeValues, string? unit, string? description, IEnumerable<KeyValuePair<string, object?>>? tags) where T : struct
	{
		return new ObservableUpDownCounter<T>(this, name, observeValues, unit, description, tags);
	}

	public ObservableCounter<T> CreateObservableCounter<T>(string name, Func<T> observeValue, string? unit = null, string? description = null) where T : struct
	{
		return new ObservableCounter<T>(this, name, observeValue, unit, description, null);
	}

	public ObservableCounter<T> CreateObservableCounter<T>(string name, Func<T> observeValue, string? unit, string? description, IEnumerable<KeyValuePair<string, object?>>? tags) where T : struct
	{
		return new ObservableCounter<T>(this, name, observeValue, unit, description, tags);
	}

	public ObservableCounter<T> CreateObservableCounter<T>(string name, Func<Measurement<T>> observeValue, string? unit = null, string? description = null) where T : struct
	{
		return new ObservableCounter<T>(this, name, observeValue, unit, description, null);
	}

	public ObservableCounter<T> CreateObservableCounter<T>(string name, Func<Measurement<T>> observeValue, string? unit, string? description, IEnumerable<KeyValuePair<string, object?>>? tags) where T : struct
	{
		return new ObservableCounter<T>(this, name, observeValue, unit, description, tags);
	}

	public ObservableCounter<T> CreateObservableCounter<T>(string name, Func<IEnumerable<Measurement<T>>> observeValues, string? unit = null, string? description = null) where T : struct
	{
		return new ObservableCounter<T>(this, name, observeValues, unit, description, null);
	}

	public ObservableCounter<T> CreateObservableCounter<T>(string name, Func<IEnumerable<Measurement<T>>> observeValues, string? unit, string? description, IEnumerable<KeyValuePair<string, object?>>? tags) where T : struct
	{
		return new ObservableCounter<T>(this, name, observeValues, unit, description, tags);
	}

	public ObservableGauge<T> CreateObservableGauge<T>(string name, Func<T> observeValue, string? unit = null, string? description = null) where T : struct
	{
		return new ObservableGauge<T>(this, name, observeValue, unit, description, null);
	}

	public ObservableGauge<T> CreateObservableGauge<T>(string name, Func<T> observeValue, string? unit, string? description, IEnumerable<KeyValuePair<string, object?>>? tags) where T : struct
	{
		return new ObservableGauge<T>(this, name, observeValue, unit, description, tags);
	}

	public ObservableGauge<T> CreateObservableGauge<T>(string name, Func<Measurement<T>> observeValue, string? unit = null, string? description = null) where T : struct
	{
		return new ObservableGauge<T>(this, name, observeValue, unit, description, null);
	}

	public ObservableGauge<T> CreateObservableGauge<T>(string name, Func<Measurement<T>> observeValue, string? unit, string? description, IEnumerable<KeyValuePair<string, object?>>? tags) where T : struct
	{
		return new ObservableGauge<T>(this, name, observeValue, unit, description, tags);
	}

	public ObservableGauge<T> CreateObservableGauge<T>(string name, Func<IEnumerable<Measurement<T>>> observeValues, string? unit = null, string? description = null) where T : struct
	{
		return new ObservableGauge<T>(this, name, observeValues, unit, description, null);
	}

	public ObservableGauge<T> CreateObservableGauge<T>(string name, Func<IEnumerable<Measurement<T>>> observeValues, string? unit, string? description, IEnumerable<KeyValuePair<string, object?>>? tags) where T : struct
	{
		return new ObservableGauge<T>(this, name, observeValues, unit, description, tags);
	}

	public void Dispose()
	{
		Dispose(disposing: true);
	}

	protected virtual void Dispose(bool disposing)
	{
		if (!disposing)
		{
			return;
		}
		List<Instrument> list = null;
		lock (Instrument.SyncObject)
		{
			if (Disposed)
			{
				return;
			}
			Disposed = true;
			s_allMeters.Remove(this);
			list = _instruments;
			_instruments = new List<Instrument>();
		}
		lock (_nonObservableInstrumentsCache)
		{
			_nonObservableInstrumentsCache.Clear();
		}
		if (list == null)
		{
			return;
		}
		foreach (Instrument item in list)
		{
			item.NotifyForUnpublishedInstrument();
		}
	}

	private static Instrument GetCachedInstrument(List<Instrument> instrumentList, Type instrumentType, string unit, string description, IEnumerable<KeyValuePair<string, object>> tags)
	{
		foreach (Instrument instrument in instrumentList)
		{
			if (instrument.GetType() == instrumentType && instrument.Unit == unit && instrument.Description == description && DiagnosticsHelper.CompareTags(instrument.Tags as List<KeyValuePair<string, object>>, tags))
			{
				return instrument;
			}
		}
		return null;
	}

	private Instrument GetOrCreateInstrument<T>(Type instrumentType, string name, string unit, string description, IEnumerable<KeyValuePair<string, object>> tags, Func<Instrument> instrumentCreator)
	{
		List<Instrument> value;
		lock (_nonObservableInstrumentsCache)
		{
			if (!_nonObservableInstrumentsCache.TryGetValue(name, out value))
			{
				value = new List<Instrument>();
				_nonObservableInstrumentsCache.Add(name, value);
			}
		}
		lock (value)
		{
			Instrument cachedInstrument = GetCachedInstrument(value, instrumentType, unit, description, tags);
			if (cachedInstrument != null)
			{
				return cachedInstrument;
			}
		}
		Instrument instrument = instrumentCreator();
		lock (value)
		{
			Instrument cachedInstrument2 = GetCachedInstrument(value, instrumentType, unit, description, tags);
			if (cachedInstrument2 != null)
			{
				return cachedInstrument2;
			}
			value.Add(instrument);
			return instrument;
		}
	}

	internal bool AddInstrument(Instrument instrument)
	{
		if (!_instruments.Contains(instrument))
		{
			_instruments.Add(instrument);
			return true;
		}
		return false;
	}

	internal static List<Instrument> GetPublishedInstruments()
	{
		List<Instrument> list = null;
		if (s_allMeters.Count > 0)
		{
			list = new List<Instrument>();
			foreach (Meter s_allMeter in s_allMeters)
			{
				foreach (Instrument instrument in s_allMeter._instruments)
				{
					list.Add(instrument);
				}
			}
		}
		return list;
	}
}
