namespace System.Diagnostics.Metrics;

internal sealed class CounterAggregator : Aggregator
{
	private readonly bool _isMonotonic;

	private double _delta;

	private double _aggregatedValue;

	public CounterAggregator(bool isMonotonic)
	{
		_isMonotonic = isMonotonic;
	}

	public override void Update(double value)
	{
		lock (this)
		{
			_delta += value;
		}
	}

	public override IAggregationStatistics Collect()
	{
		lock (this)
		{
			_aggregatedValue += _delta;
			CounterStatistics result = new CounterStatistics(_delta, _isMonotonic, _aggregatedValue);
			_delta = 0.0;
			return result;
		}
	}
}
