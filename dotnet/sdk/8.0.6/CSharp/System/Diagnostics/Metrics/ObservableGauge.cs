using System.Collections.Generic;

namespace System.Diagnostics.Metrics;

public sealed class ObservableGauge<T> : ObservableInstrument<T> where T : struct
{
	private readonly object _callback;

	internal ObservableGauge(Meter meter, string name, Func<T> observeValue, string unit, string description, IEnumerable<KeyValuePair<string, object>> tags)
		: base(meter, name, unit, description, tags)
	{
		_callback = observeValue ?? throw new ArgumentNullException("observeValue");
		Publish();
	}

	internal ObservableGauge(Meter meter, string name, Func<Measurement<T>> observeValue, string unit, string description, IEnumerable<KeyValuePair<string, object>> tags)
		: base(meter, name, unit, description, tags)
	{
		_callback = observeValue ?? throw new ArgumentNullException("observeValue");
		Publish();
	}

	internal ObservableGauge(Meter meter, string name, Func<IEnumerable<Measurement<T>>> observeValues, string unit, string description, IEnumerable<KeyValuePair<string, object>> tags)
		: base(meter, name, unit, description, tags)
	{
		_callback = observeValues ?? throw new ArgumentNullException("observeValues");
		Publish();
	}

	protected override IEnumerable<Measurement<T>> Observe()
	{
		return ObservableInstrument<T>.Observe(_callback);
	}
}
