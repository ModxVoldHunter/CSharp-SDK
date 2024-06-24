using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.Versioning;
using System.Text;
using System.Threading;

namespace System.Diagnostics.Metrics;

[EventSource(Name = "System.Diagnostics.Metrics")]
internal sealed class MetricsEventSource : EventSource
{
	public static class Keywords
	{
		public const EventKeywords Messages = (EventKeywords)1L;

		public const EventKeywords TimeSeriesValues = (EventKeywords)2L;

		public const EventKeywords InstrumentPublishing = (EventKeywords)4L;
	}

	private sealed class CommandHandler
	{
		private AggregationManager _aggregationManager;

		private string _sessionId = "";

		private HashSet<string> _sharedSessionClientIds = new HashSet<string>();

		private int _sharedSessionRefCount;

		private bool _disabledRefCount;

		private static readonly char[] s_instrumentSeparators = new char[4] { '\r', '\n', ',', ';' };

		public MetricsEventSource Parent { get; private set; }

		public CommandHandler(MetricsEventSource parent)
		{
			Parent = parent;
		}

		public bool IsSharedSession(string commandSessionId)
		{
			if (_sessionId.Equals("SHARED"))
			{
				if (!string.IsNullOrEmpty(commandSessionId))
				{
					return commandSessionId.Equals("SHARED");
				}
				return true;
			}
			return false;
		}

		public void OnEventCommand(EventCommandEventArgs command)
		{
			try
			{
				if (OperatingSystem.IsBrowser())
				{
					Parent.Error("", "System.Diagnostics.Metrics EventSource not supported on browser");
					return;
				}
				string sessionId2 = GetSessionId(command);
				if ((command.Command == EventCommand.Update || command.Command == EventCommand.Disable || command.Command == EventCommand.Enable) && _aggregationManager != null)
				{
					if (command.Command == EventCommand.Update || command.Command == EventCommand.Enable)
					{
						IncrementRefCount(sessionId2, command);
					}
					if (IsSharedSession(sessionId2))
					{
						if (ShouldDisable(command.Command))
						{
							Parent.Message("Previous session with id " + _sessionId + " is stopped");
							_aggregationManager.Dispose();
							_aggregationManager = null;
							_sessionId = string.Empty;
							_sharedSessionClientIds.Clear();
							return;
						}
						bool flag = true;
						double refreshIntervalSeconds;
						lock (_aggregationManager)
						{
							flag = SetSharedRefreshIntervalSecs(command.Arguments, _aggregationManager.CollectionPeriod.TotalSeconds, out refreshIntervalSeconds) && flag;
						}
						flag = SetSharedMaxHistograms(command.Arguments, _aggregationManager.MaxHistograms, out var maxHistograms) && flag;
						flag = SetSharedMaxTimeSeries(command.Arguments, _aggregationManager.MaxTimeSeries, out var maxTimeSeries) && flag;
						if (command.Command != EventCommand.Disable)
						{
							string value;
							if (flag)
							{
								if (ParseMetrics(command.Arguments, out var metricsSpecs))
								{
									ParseSpecs(metricsSpecs);
									_aggregationManager.Update();
								}
							}
							else if (command.Arguments.TryGetValue("ClientId", out value))
							{
								lock (_aggregationManager)
								{
									Parent.MultipleSessionsConfiguredIncorrectlyError(value, _aggregationManager.MaxHistograms.ToString(), maxHistograms.ToString(), _aggregationManager.MaxTimeSeries.ToString(), maxTimeSeries.ToString(), _aggregationManager.CollectionPeriod.TotalSeconds.ToString(), refreshIntervalSeconds.ToString());
									return;
								}
							}
							return;
						}
					}
					else
					{
						if (command.Command == EventCommand.Enable || command.Command == EventCommand.Update)
						{
							Parent.MultipleSessionsNotSupportedError(_sessionId);
							return;
						}
						if (ShouldDisable(command.Command))
						{
							Parent.Message("Previous session with id " + _sessionId + " is stopped");
							_aggregationManager.Dispose();
							_aggregationManager = null;
							_sessionId = string.Empty;
							_sharedSessionClientIds.Clear();
							return;
						}
					}
				}
				if ((command.Command == EventCommand.Update || command.Command == EventCommand.Enable) && command.Arguments != null)
				{
					IncrementRefCount(sessionId2, command);
					_sessionId = sessionId2;
					double defaultValue = 1.0;
					SetRefreshIntervalSecs(command.Arguments, 0.1, defaultValue, out var refreshIntervalSeconds2);
					SetUniqueMaxTimeSeries(command.Arguments, 1000, out var maxTimeSeries2);
					SetUniqueMaxHistograms(command.Arguments, 20, out var maxHistograms2);
					string sessionId = _sessionId;
					_aggregationManager = new AggregationManager(maxTimeSeries2, maxHistograms2, delegate(Instrument i, LabeledAggregationStatistics s)
					{
						TransmitMetricValue(i, s, sessionId);
					}, delegate(DateTime startIntervalTime, DateTime endIntervalTime)
					{
						Parent.CollectionStart(sessionId, startIntervalTime, endIntervalTime);
					}, delegate(DateTime startIntervalTime, DateTime endIntervalTime)
					{
						Parent.CollectionStop(sessionId, startIntervalTime, endIntervalTime);
					}, delegate(Instrument i)
					{
						Parent.BeginInstrumentReporting(sessionId, i.Meter.Name, i.Meter.Version, i.Name, i.GetType().Name, i.Unit, i.Description, FormatTags(i.Tags), FormatTags(i.Meter.Tags), FormatScopeHash(i.Meter.Scope));
					}, delegate(Instrument i)
					{
						Parent.EndInstrumentReporting(sessionId, i.Meter.Name, i.Meter.Version, i.Name, i.GetType().Name, i.Unit, i.Description, FormatTags(i.Tags), FormatTags(i.Meter.Tags), FormatScopeHash(i.Meter.Scope));
					}, delegate(Instrument i)
					{
						Parent.InstrumentPublished(sessionId, i.Meter.Name, i.Meter.Version, i.Name, i.GetType().Name, i.Unit, i.Description, FormatTags(i.Tags), FormatTags(i.Meter.Tags), FormatScopeHash(i.Meter.Scope));
					}, delegate
					{
						Parent.InitialInstrumentEnumerationComplete(sessionId);
					}, delegate(Exception e)
					{
						Parent.Error(sessionId, e.ToString());
					}, delegate
					{
						Parent.TimeSeriesLimitReached(sessionId);
					}, delegate
					{
						Parent.HistogramLimitReached(sessionId);
					}, delegate(Exception e)
					{
						Parent.ObservableInstrumentCallbackError(sessionId, e.ToString());
					});
					_aggregationManager.SetCollectionPeriod(TimeSpan.FromSeconds(refreshIntervalSeconds2));
					if (ParseMetrics(command.Arguments, out var metricsSpecs2))
					{
						ParseSpecs(metricsSpecs2);
					}
					_aggregationManager.Start();
				}
			}
			catch (Exception e2) when (LogError(e2))
			{
			}
		}

		private bool ShouldDisable(EventCommand command)
		{
			if (command == EventCommand.Disable)
			{
				if (_disabledRefCount || Interlocked.Decrement(ref _sharedSessionRefCount) != 0)
				{
					return !Parent.IsEnabled();
				}
				return true;
			}
			return false;
		}

		private bool ParseMetrics(IDictionary<string, string> arguments, out string metricsSpecs)
		{
			if (arguments.TryGetValue("Metrics", out metricsSpecs))
			{
				Parent.Message("Metrics argument received: " + metricsSpecs);
				return true;
			}
			Parent.Message("No Metrics argument received");
			return false;
		}

		private void InvalidateRefCounting()
		{
			_disabledRefCount = true;
			Parent.Message("ClientId not provided; session will remain active indefinitely.");
		}

		private void IncrementRefCount(string clientId, EventCommandEventArgs command)
		{
			if (clientId.Equals("SHARED"))
			{
				if (command.Arguments.TryGetValue("ClientId", out string value) && !string.IsNullOrEmpty(value))
				{
					clientId = value;
				}
				else
				{
					InvalidateRefCounting();
				}
			}
			if (_sharedSessionClientIds.Add(clientId))
			{
				Interlocked.Increment(ref _sharedSessionRefCount);
			}
		}

		private bool SetSharedMaxTimeSeries(IDictionary<string, string> arguments, int sharedValue, out int maxTimeSeries)
		{
			return SetMaxValue(arguments, "MaxTimeSeries", "shared value", sharedValue, out maxTimeSeries);
		}

		private void SetUniqueMaxTimeSeries(IDictionary<string, string> arguments, int defaultValue, out int maxTimeSeries)
		{
			SetMaxValue(arguments, "MaxTimeSeries", "default", defaultValue, out maxTimeSeries);
		}

		private bool SetSharedMaxHistograms(IDictionary<string, string> arguments, int sharedValue, out int maxHistograms)
		{
			return SetMaxValue(arguments, "MaxHistograms", "shared value", sharedValue, out maxHistograms);
		}

		private void SetUniqueMaxHistograms(IDictionary<string, string> arguments, int defaultValue, out int maxHistograms)
		{
			SetMaxValue(arguments, "MaxHistograms", "default", defaultValue, out maxHistograms);
		}

		private bool SetMaxValue(IDictionary<string, string> arguments, string argumentsKey, string valueDescriptor, int defaultValue, out int maxValue)
		{
			if (arguments.TryGetValue(argumentsKey, out var value))
			{
				Parent.Message(argumentsKey + " argument received: " + value);
				if (!int.TryParse(value, out maxValue))
				{
					Parent.Message($"Failed to parse {argumentsKey}. Using {valueDescriptor} {defaultValue}");
					maxValue = defaultValue;
				}
				else if (maxValue != defaultValue)
				{
					return false;
				}
			}
			else
			{
				Parent.Message($"No {argumentsKey} argument received. Using {valueDescriptor} {defaultValue}");
				maxValue = defaultValue;
			}
			return true;
		}

		private void SetRefreshIntervalSecs(IDictionary<string, string> arguments, double minValue, double defaultValue, out double refreshIntervalSeconds)
		{
			if (GetRefreshIntervalSecs(arguments, "default", defaultValue, out refreshIntervalSeconds) && refreshIntervalSeconds < minValue)
			{
				Parent.Message($"{"RefreshInterval"} too small. Using minimum interval {minValue} seconds.");
				refreshIntervalSeconds = minValue;
			}
		}

		private bool SetSharedRefreshIntervalSecs(IDictionary<string, string> arguments, double sharedValue, out double refreshIntervalSeconds)
		{
			if (GetRefreshIntervalSecs(arguments, "shared value", sharedValue, out refreshIntervalSeconds) && refreshIntervalSeconds != sharedValue)
			{
				return false;
			}
			return true;
		}

		private bool GetRefreshIntervalSecs(IDictionary<string, string> arguments, string valueDescriptor, double defaultValue, out double refreshIntervalSeconds)
		{
			if (arguments.TryGetValue("RefreshInterval", out var value))
			{
				Parent.Message("RefreshInterval argument received: " + value);
				if (!double.TryParse(value, out refreshIntervalSeconds))
				{
					Parent.Message($"Failed to parse {"RefreshInterval"}. Using {valueDescriptor} {defaultValue}s.");
					refreshIntervalSeconds = defaultValue;
					return false;
				}
				return true;
			}
			Parent.Message($"No {"RefreshInterval"} argument received. Using {valueDescriptor} {defaultValue}s.");
			refreshIntervalSeconds = defaultValue;
			return false;
		}

		private string GetSessionId(EventCommandEventArgs command)
		{
			if (command.Arguments.TryGetValue("SessionId", out string value))
			{
				Parent.Message("SessionId argument received: " + value);
				return value;
			}
			string text = string.Empty;
			if (command.Command != EventCommand.Disable)
			{
				text = Guid.NewGuid().ToString();
				Parent.Message("New session started. SessionId auto-generated: " + text);
			}
			return text;
		}

		private bool LogError(Exception e)
		{
			Parent.Error(_sessionId, e.ToString());
			return false;
		}

		[UnsupportedOSPlatform("browser")]
		private void ParseSpecs(string metricsSpecs)
		{
			if (metricsSpecs == null)
			{
				return;
			}
			string[] array = metricsSpecs.Split(s_instrumentSeparators, StringSplitOptions.RemoveEmptyEntries);
			string[] array2 = array;
			foreach (string text in array2)
			{
				MetricSpec metricSpec = MetricSpec.Parse(text);
				Parent.Message($"Parsed metric: {metricSpec}");
				if (metricSpec.InstrumentName != null)
				{
					_aggregationManager.Include(metricSpec.MeterName, metricSpec.InstrumentName);
				}
				else
				{
					_aggregationManager.Include(metricSpec.MeterName);
				}
			}
		}

		private static void TransmitMetricValue(Instrument instrument, LabeledAggregationStatistics stats, string sessionId)
		{
			if (stats.AggregationStatistics is CounterStatistics counterStatistics)
			{
				if (counterStatistics.IsMonotonic)
				{
					Log.CounterRateValuePublished(sessionId, instrument.Meter.Name, instrument.Meter.Version, instrument.Name, instrument.Unit, FormatTags(stats.Labels), counterStatistics.Delta.HasValue ? counterStatistics.Delta.Value.ToString(CultureInfo.InvariantCulture) : "", counterStatistics.Value.ToString(CultureInfo.InvariantCulture));
				}
				else
				{
					Log.UpDownCounterRateValuePublished(sessionId, instrument.Meter.Name, instrument.Meter.Version, instrument.Name, instrument.Unit, FormatTags(stats.Labels), counterStatistics.Delta.HasValue ? counterStatistics.Delta.Value.ToString(CultureInfo.InvariantCulture) : "", counterStatistics.Value.ToString(CultureInfo.InvariantCulture));
				}
			}
			else if (stats.AggregationStatistics is LastValueStatistics lastValueStatistics)
			{
				Log.GaugeValuePublished(sessionId, instrument.Meter.Name, instrument.Meter.Version, instrument.Name, instrument.Unit, FormatTags(stats.Labels), lastValueStatistics.LastValue.HasValue ? lastValueStatistics.LastValue.Value.ToString(CultureInfo.InvariantCulture) : "");
			}
			else if (stats.AggregationStatistics is HistogramStatistics histogramStatistics)
			{
				Log.HistogramValuePublished(sessionId, instrument.Meter.Name, instrument.Meter.Version, instrument.Name, instrument.Unit, FormatTags(stats.Labels), FormatQuantiles(histogramStatistics.Quantiles), histogramStatistics.Count, histogramStatistics.Sum);
			}
		}

		private static string FormatScopeHash(object scope)
		{
			if (scope != null)
			{
				return RuntimeHelpers.GetHashCode(scope).ToString(CultureInfo.InvariantCulture);
			}
			return string.Empty;
		}

		private static string FormatTags(IEnumerable<KeyValuePair<string, object>> tags)
		{
			if (tags == null)
			{
				return string.Empty;
			}
			StringBuilder stringBuilder = new StringBuilder();
			bool flag = true;
			foreach (KeyValuePair<string, object> tag in tags)
			{
				if (flag)
				{
					flag = false;
				}
				else
				{
					stringBuilder.Append(',');
				}
				stringBuilder.Append(tag.Key).Append('=');
				if (tag.Value != null)
				{
					stringBuilder.Append(tag.Value.ToString());
				}
			}
			return stringBuilder.ToString();
		}

		private static string FormatTags(KeyValuePair<string, string>[] labels)
		{
			StringBuilder stringBuilder = new StringBuilder();
			for (int i = 0; i < labels.Length; i++)
			{
				stringBuilder.Append(labels[i].Key).Append('=').Append(labels[i].Value);
				if (i != labels.Length - 1)
				{
					stringBuilder.Append(',');
				}
			}
			return stringBuilder.ToString();
		}

		private static string FormatQuantiles(QuantileValue[] quantiles)
		{
			StringBuilder stringBuilder = new StringBuilder();
			for (int i = 0; i < quantiles.Length; i++)
			{
				StringBuilder stringBuilder2 = stringBuilder;
				IFormatProvider invariantCulture = CultureInfo.InvariantCulture;
				StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(1, 2, stringBuilder2, invariantCulture);
				handler.AppendFormatted(quantiles[i].Quantile);
				handler.AppendLiteral("=");
				handler.AppendFormatted(quantiles[i].Value);
				stringBuilder2.Append(invariantCulture, ref handler);
				if (i != quantiles.Length - 1)
				{
					stringBuilder.Append(';');
				}
			}
			return stringBuilder.ToString();
		}
	}

	private sealed class MetricSpec
	{
		public string MeterName { get; private set; }

		public string InstrumentName { get; private set; }

		public MetricSpec(string meterName, string instrumentName)
		{
			MeterName = meterName;
			InstrumentName = instrumentName;
		}

		public static MetricSpec Parse(string text)
		{
			int num = text.IndexOf('\\');
			if (num < 0)
			{
				return new MetricSpec(text.Trim(), null);
			}
			string meterName = text.AsSpan(0, num).Trim().ToString();
			string instrumentName = text.AsSpan(num + 1).Trim().ToString();
			return new MetricSpec(meterName, instrumentName);
		}

		public override string ToString()
		{
			if (InstrumentName == null)
			{
				return MeterName;
			}
			return MeterName + "\\" + InstrumentName;
		}
	}

	public static readonly MetricsEventSource Log = new MetricsEventSource();

	private CommandHandler _handler;

	private CommandHandler Handler
	{
		get
		{
			if (_handler == null)
			{
				Interlocked.CompareExchange(ref _handler, new CommandHandler(this), null);
			}
			return _handler;
		}
	}

	private MetricsEventSource()
	{
	}

	[Event(1, Keywords = (EventKeywords)1L)]
	public void Message(string Message)
	{
		WriteEvent(1, Message);
	}

	[Event(2, Keywords = (EventKeywords)2L)]
	public void CollectionStart(string sessionId, DateTime intervalStartTime, DateTime intervalEndTime)
	{
		WriteEvent(2, sessionId, intervalStartTime, intervalEndTime);
	}

	[Event(3, Keywords = (EventKeywords)2L)]
	public void CollectionStop(string sessionId, DateTime intervalStartTime, DateTime intervalEndTime)
	{
		WriteEvent(3, sessionId, intervalStartTime, intervalEndTime);
	}

	[Event(4, Keywords = (EventKeywords)2L, Version = 1)]
	public void CounterRateValuePublished(string sessionId, string meterName, string meterVersion, string instrumentName, string unit, string tags, string rate, string value)
	{
		WriteEvent(4, sessionId, meterName, meterVersion ?? "", instrumentName, unit ?? "", tags, rate, value);
	}

	[Event(5, Keywords = (EventKeywords)2L)]
	public void GaugeValuePublished(string sessionId, string meterName, string meterVersion, string instrumentName, string unit, string tags, string lastValue)
	{
		WriteEvent(5, sessionId, meterName, meterVersion ?? "", instrumentName, unit ?? "", tags, lastValue);
	}

	[Event(6, Keywords = (EventKeywords)2L, Version = 1)]
	public void HistogramValuePublished(string sessionId, string meterName, string meterVersion, string instrumentName, string unit, string tags, string quantiles, int count, double sum)
	{
		WriteEvent(6, sessionId, meterName, meterVersion ?? "", instrumentName, unit ?? "", tags, quantiles, count, sum);
	}

	[Event(7, Keywords = (EventKeywords)2L, Version = 1)]
	public void BeginInstrumentReporting(string sessionId, string meterName, string meterVersion, string instrumentName, string instrumentType, string unit, string description, string instrumentTags, string meterTags, string meterScopeHash)
	{
		WriteEvent(7, sessionId, meterName, meterVersion ?? "", instrumentName, instrumentType, unit ?? "", description ?? "", instrumentTags, meterTags, meterScopeHash);
	}

	[Event(8, Keywords = (EventKeywords)2L, Version = 1)]
	public void EndInstrumentReporting(string sessionId, string meterName, string meterVersion, string instrumentName, string instrumentType, string unit, string description, string instrumentTags, string meterTags, string meterScopeHash)
	{
		WriteEvent(8, sessionId, meterName, meterVersion ?? "", instrumentName, instrumentType, unit ?? "", description ?? "", instrumentTags, meterTags, meterScopeHash);
	}

	[Event(9, Keywords = (EventKeywords)7L)]
	public void Error(string sessionId, string errorMessage)
	{
		WriteEvent(9, sessionId, errorMessage);
	}

	[Event(10, Keywords = (EventKeywords)6L)]
	public void InitialInstrumentEnumerationComplete(string sessionId)
	{
		WriteEvent(10, sessionId);
	}

	[Event(11, Keywords = (EventKeywords)4L, Version = 1)]
	public void InstrumentPublished(string sessionId, string meterName, string meterVersion, string instrumentName, string instrumentType, string unit, string description, string instrumentTags, string meterTags, string meterScopeHash)
	{
		WriteEvent(11, sessionId, meterName, meterVersion ?? "", instrumentName, instrumentType, unit ?? "", description ?? "", instrumentTags, meterTags, meterScopeHash);
	}

	[Event(12, Keywords = (EventKeywords)2L)]
	public void TimeSeriesLimitReached(string sessionId)
	{
		WriteEvent(12, sessionId);
	}

	[Event(13, Keywords = (EventKeywords)2L)]
	public void HistogramLimitReached(string sessionId)
	{
		WriteEvent(13, sessionId);
	}

	[Event(14, Keywords = (EventKeywords)2L)]
	public void ObservableInstrumentCallbackError(string sessionId, string errorMessage)
	{
		WriteEvent(14, sessionId, errorMessage);
	}

	[Event(15, Keywords = (EventKeywords)7L)]
	public void MultipleSessionsNotSupportedError(string runningSessionId)
	{
		WriteEvent(15, runningSessionId);
	}

	[Event(16, Keywords = (EventKeywords)2L, Version = 1)]
	public void UpDownCounterRateValuePublished(string sessionId, string meterName, string meterVersion, string instrumentName, string unit, string tags, string rate, string value)
	{
		WriteEvent(16, sessionId, meterName, meterVersion ?? "", instrumentName, unit ?? "", tags, rate, value);
	}

	[Event(17, Keywords = (EventKeywords)2L)]
	public void MultipleSessionsConfiguredIncorrectlyError(string clientId, string expectedMaxHistograms, string actualMaxHistograms, string expectedMaxTimeSeries, string actualMaxTimeSeries, string expectedRefreshInterval, string actualRefreshInterval)
	{
		WriteEvent(17, clientId, expectedMaxHistograms, actualMaxHistograms, expectedMaxTimeSeries, actualMaxTimeSeries, expectedRefreshInterval, actualRefreshInterval);
	}

	[NonEvent]
	protected override void OnEventCommand(EventCommandEventArgs command)
	{
		lock (this)
		{
			Handler.OnEventCommand(command);
		}
	}
}
