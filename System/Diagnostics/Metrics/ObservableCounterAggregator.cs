namespace System.Diagnostics.Metrics;

internal sealed class ObservableCounterAggregator : Aggregator
{
	private readonly bool _isMonotonic;

	private double? _prevValue;

	private double _currValue;

	public ObservableCounterAggregator(bool isMonotonic)
	{
		_isMonotonic = isMonotonic;
	}

	public override void Update(double value)
	{
		lock (this)
		{
			_currValue = value;
		}
	}

	public override IAggregationStatistics Collect()
	{
		lock (this)
		{
			double? delta = null;
			if (_prevValue.HasValue)
			{
				delta = _currValue - _prevValue.Value;
			}
			CounterStatistics result = new CounterStatistics(delta, _isMonotonic, _currValue);
			_prevValue = _currValue;
			return result;
		}
	}
}
