namespace System.Diagnostics.Metrics;

internal sealed class HistogramStatistics : IAggregationStatistics
{
	public QuantileValue[] Quantiles { get; }

	public int Count { get; }

	public double Sum { get; }

	internal HistogramStatistics(QuantileValue[] quantiles, int count, double sum)
	{
		Quantiles = quantiles;
		Count = count;
		Sum = sum;
	}
}
