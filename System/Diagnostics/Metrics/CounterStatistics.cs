namespace System.Diagnostics.Metrics;

internal sealed class CounterStatistics : IAggregationStatistics
{
	public double? Delta { get; }

	public bool IsMonotonic { get; }

	public double Value { get; }

	public CounterStatistics(double? delta, bool isMonotonic, double value)
	{
		Delta = delta;
		IsMonotonic = isMonotonic;
		Value = value;
	}
}
