using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Versioning;
using System.Threading;

namespace System.Diagnostics.Metrics;

[UnsupportedOSPlatform("browser")]
internal sealed class AggregationManager
{
	private static readonly QuantileAggregation s_defaultHistogramConfig = new QuantileAggregation(0.5, 0.95, 0.99);

	private readonly List<Predicate<Instrument>> _instrumentConfigFuncs = new List<Predicate<Instrument>>();

	private Dictionary<Instrument, bool> _instruments = new Dictionary<Instrument, bool>();

	private readonly ConcurrentDictionary<Instrument, InstrumentState> _instrumentStates = new ConcurrentDictionary<Instrument, InstrumentState>();

	private readonly CancellationTokenSource _cts = new CancellationTokenSource();

	private Thread _collectThread;

	private readonly MeterListener _listener;

	private int _currentTimeSeries;

	private int _currentHistograms;

	private readonly Action<Instrument, LabeledAggregationStatistics> _collectMeasurement;

	private readonly Action<DateTime, DateTime> _beginCollection;

	private readonly Action<DateTime, DateTime> _endCollection;

	private readonly Action<Instrument> _beginInstrumentMeasurements;

	private readonly Action<Instrument> _endInstrumentMeasurements;

	private readonly Action<Instrument> _instrumentPublished;

	private readonly Action _initialInstrumentEnumerationComplete;

	private readonly Action<Exception> _collectionError;

	private readonly Action _timeSeriesLimitReached;

	private readonly Action _histogramLimitReached;

	private readonly Action<Exception> _observableInstrumentCallbackError;

	public TimeSpan CollectionPeriod { get; private set; }

	public int MaxTimeSeries { get; }

	public int MaxHistograms { get; }

	public AggregationManager(int maxTimeSeries, int maxHistograms, Action<Instrument, LabeledAggregationStatistics> collectMeasurement, Action<DateTime, DateTime> beginCollection, Action<DateTime, DateTime> endCollection, Action<Instrument> beginInstrumentMeasurements, Action<Instrument> endInstrumentMeasurements, Action<Instrument> instrumentPublished, Action initialInstrumentEnumerationComplete, Action<Exception> collectionError, Action timeSeriesLimitReached, Action histogramLimitReached, Action<Exception> observableInstrumentCallbackError)
	{
		MaxTimeSeries = maxTimeSeries;
		MaxHistograms = maxHistograms;
		_collectMeasurement = collectMeasurement;
		_beginCollection = beginCollection;
		_endCollection = endCollection;
		_beginInstrumentMeasurements = beginInstrumentMeasurements;
		_endInstrumentMeasurements = endInstrumentMeasurements;
		_instrumentPublished = instrumentPublished;
		_initialInstrumentEnumerationComplete = initialInstrumentEnumerationComplete;
		_collectionError = collectionError;
		_timeSeriesLimitReached = timeSeriesLimitReached;
		_histogramLimitReached = histogramLimitReached;
		_observableInstrumentCallbackError = observableInstrumentCallbackError;
		_listener = new MeterListener();
		MeterListener listener = _listener;
		listener.InstrumentPublished = (Action<Instrument, MeterListener>)Delegate.Combine(listener.InstrumentPublished, new Action<Instrument, MeterListener>(PublishedInstrument));
		MeterListener listener2 = _listener;
		listener2.MeasurementsCompleted = (Action<Instrument, object>)Delegate.Combine(listener2.MeasurementsCompleted, new Action<Instrument, object>(CompletedMeasurements));
		_listener.SetMeasurementEventCallback(delegate(Instrument i, double m, ReadOnlySpan<KeyValuePair<string, object>> l, object c)
		{
			((InstrumentState)c).Update(m, l);
		});
		_listener.SetMeasurementEventCallback(delegate(Instrument i, float m, ReadOnlySpan<KeyValuePair<string, object>> l, object c)
		{
			((InstrumentState)c).Update(m, l);
		});
		_listener.SetMeasurementEventCallback(delegate(Instrument i, long m, ReadOnlySpan<KeyValuePair<string, object>> l, object c)
		{
			((InstrumentState)c).Update(m, l);
		});
		_listener.SetMeasurementEventCallback(delegate(Instrument i, int m, ReadOnlySpan<KeyValuePair<string, object>> l, object c)
		{
			((InstrumentState)c).Update(m, l);
		});
		_listener.SetMeasurementEventCallback(delegate(Instrument i, short m, ReadOnlySpan<KeyValuePair<string, object>> l, object c)
		{
			((InstrumentState)c).Update(m, l);
		});
		_listener.SetMeasurementEventCallback(delegate(Instrument i, byte m, ReadOnlySpan<KeyValuePair<string, object>> l, object c)
		{
			((InstrumentState)c).Update((int)m, l);
		});
		_listener.SetMeasurementEventCallback(delegate(Instrument i, decimal m, ReadOnlySpan<KeyValuePair<string, object>> l, object c)
		{
			((InstrumentState)c).Update((double)m, l);
		});
	}

	public void Include(string meterName)
	{
		Include((Instrument i) => i.Meter.Name == meterName);
	}

	public void Include(string meterName, string instrumentName)
	{
		Include((Instrument i) => i.Meter.Name == meterName && i.Name == instrumentName);
	}

	private void Include(Predicate<Instrument> instrumentFilter)
	{
		lock (this)
		{
			_instrumentConfigFuncs.Add(instrumentFilter);
		}
	}

	public AggregationManager SetCollectionPeriod(TimeSpan collectionPeriod)
	{
		lock (this)
		{
			CollectionPeriod = collectionPeriod;
			return this;
		}
	}

	private void CompletedMeasurements(Instrument instrument, object cookie)
	{
		_instruments.Remove(instrument);
		_endInstrumentMeasurements(instrument);
		RemoveInstrumentState(instrument);
	}

	private void PublishedInstrument(Instrument instrument, MeterListener _)
	{
		_instrumentPublished(instrument);
		InstrumentState instrumentState = GetInstrumentState(instrument);
		if (instrumentState != null)
		{
			_beginInstrumentMeasurements(instrument);
			if (!_instruments.ContainsKey(instrument))
			{
				_listener.EnableMeasurementEvents(instrument, instrumentState);
				_instruments.Add(instrument, value: true);
			}
		}
	}

	public void Start()
	{
		_collectThread = new Thread((ThreadStart)delegate
		{
			CollectWorker(_cts.Token);
		});
		_collectThread.IsBackground = true;
		_collectThread.Name = "MetricsEventSource CollectWorker";
		_collectThread.Start();
		_listener.Start();
		_initialInstrumentEnumerationComplete();
	}

	public void Update()
	{
		using (MeterListener meterListener = new MeterListener())
		{
			meterListener.InstrumentPublished = (Action<Instrument, MeterListener>)Delegate.Combine(meterListener.InstrumentPublished, new Action<Instrument, MeterListener>(PublishedInstrument));
			meterListener.MeasurementsCompleted = (Action<Instrument, object>)Delegate.Combine(meterListener.MeasurementsCompleted, new Action<Instrument, object>(CompletedMeasurements));
			meterListener.Start();
		}
		_initialInstrumentEnumerationComplete();
	}

	private void CollectWorker(CancellationToken cancelToken)
	{
		try
		{
			double num = -1.0;
			lock (this)
			{
				num = CollectionPeriod.TotalSeconds;
			}
			DateTime utcNow = DateTime.UtcNow;
			DateTime arg = utcNow;
			while (!cancelToken.IsCancellationRequested)
			{
				DateTime utcNow2 = DateTime.UtcNow;
				double totalSeconds = (utcNow2 - utcNow).TotalSeconds;
				double value = Math.Ceiling(totalSeconds / num) * num;
				DateTime dateTime = utcNow.AddSeconds(value);
				DateTime dateTime2 = arg.AddSeconds(num);
				if (dateTime <= dateTime2)
				{
					dateTime = dateTime2;
				}
				TimeSpan timeout = dateTime - utcNow2;
				if (!cancelToken.WaitHandle.WaitOne(timeout))
				{
					_beginCollection(arg, dateTime);
					Collect();
					_endCollection(arg, dateTime);
					arg = dateTime;
					continue;
				}
				break;
			}
		}
		catch (Exception obj)
		{
			_collectionError(obj);
		}
	}

	public void Dispose()
	{
		_cts.Cancel();
		if (_collectThread != null)
		{
			_collectThread.Join();
			_collectThread = null;
		}
		_listener.Dispose();
	}

	private void RemoveInstrumentState(Instrument instrument)
	{
		_instrumentStates.TryRemove(instrument, out var _);
	}

	private InstrumentState GetInstrumentState(Instrument instrument)
	{
		if (!_instrumentStates.TryGetValue(instrument, out var value))
		{
			lock (this)
			{
				foreach (Predicate<Instrument> instrumentConfigFunc in _instrumentConfigFuncs)
				{
					if (instrumentConfigFunc(instrument))
					{
						value = BuildInstrumentState(instrument);
						if (value != null)
						{
							_instrumentStates.TryAdd(instrument, value);
							_instrumentStates.TryGetValue(instrument, out value);
						}
						break;
					}
				}
			}
		}
		return value;
	}

	[UnconditionalSuppressMessage("AotAnalysis", "IL3050:RequiresDynamicCode", Justification = "MakeGenericType is creating instances over reference types that works fine in AOT.")]
	internal InstrumentState BuildInstrumentState(Instrument instrument)
	{
		Func<Aggregator> aggregatorFactory = GetAggregatorFactory(instrument);
		if (aggregatorFactory == null)
		{
			return null;
		}
		Type type = aggregatorFactory.GetType().GenericTypeArguments[0];
		Type type2 = typeof(InstrumentState<>).MakeGenericType(type);
		return (InstrumentState)Activator.CreateInstance(type2, aggregatorFactory);
	}

	private Func<Aggregator> GetAggregatorFactory(Instrument instrument)
	{
		Type type = instrument.GetType();
		Type type2 = null;
		type2 = (type.IsGenericType ? type.GetGenericTypeDefinition() : null);
		if (type2 == typeof(Counter<>))
		{
			return delegate
			{
				lock (this)
				{
					return CheckTimeSeriesAllowed() ? new CounterAggregator(isMonotonic: true) : null;
				}
			};
		}
		if (type2 == typeof(ObservableCounter<>))
		{
			return delegate
			{
				lock (this)
				{
					return CheckTimeSeriesAllowed() ? new ObservableCounterAggregator(isMonotonic: true) : null;
				}
			};
		}
		if (type2 == typeof(ObservableGauge<>))
		{
			return delegate
			{
				lock (this)
				{
					return CheckTimeSeriesAllowed() ? new LastValue() : null;
				}
			};
		}
		if (type2 == typeof(Histogram<>))
		{
			return delegate
			{
				lock (this)
				{
					return (!CheckHistogramAllowed() || !CheckTimeSeriesAllowed()) ? null : new ExponentialHistogramAggregator(s_defaultHistogramConfig);
				}
			};
		}
		if (type2 == typeof(UpDownCounter<>))
		{
			return delegate
			{
				lock (this)
				{
					return CheckTimeSeriesAllowed() ? new CounterAggregator(isMonotonic: false) : null;
				}
			};
		}
		if (type2 == typeof(ObservableUpDownCounter<>))
		{
			return delegate
			{
				lock (this)
				{
					return CheckTimeSeriesAllowed() ? new ObservableCounterAggregator(isMonotonic: false) : null;
				}
			};
		}
		return null;
	}

	private bool CheckTimeSeriesAllowed()
	{
		if (_currentTimeSeries < MaxTimeSeries)
		{
			_currentTimeSeries++;
			return true;
		}
		if (_currentTimeSeries == MaxTimeSeries)
		{
			_currentTimeSeries++;
			_timeSeriesLimitReached();
			return false;
		}
		return false;
	}

	private bool CheckHistogramAllowed()
	{
		if (_currentHistograms < MaxHistograms)
		{
			_currentHistograms++;
			return true;
		}
		if (_currentHistograms == MaxHistograms)
		{
			_currentHistograms++;
			_histogramLimitReached();
			return false;
		}
		return false;
	}

	internal void Collect()
	{
		try
		{
			_listener.RecordObservableInstruments();
		}
		catch (Exception obj)
		{
			_observableInstrumentCallbackError(obj);
		}
		foreach (KeyValuePair<Instrument, InstrumentState> kv in _instrumentStates)
		{
			kv.Value.Collect(kv.Key, delegate(LabeledAggregationStatistics labeledAggStats)
			{
				_collectMeasurement(kv.Key, labeledAggStats);
			});
		}
	}
}
