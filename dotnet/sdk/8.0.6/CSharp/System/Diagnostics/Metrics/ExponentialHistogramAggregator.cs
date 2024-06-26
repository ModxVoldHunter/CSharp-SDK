using System.Collections.Generic;

namespace System.Diagnostics.Metrics;

internal sealed class ExponentialHistogramAggregator : Aggregator
{
	private struct Bucket
	{
		public double Value;

		public int Count;

		public Bucket(double value, int count)
		{
			Value = value;
			Count = count;
		}
	}

	private readonly QuantileAggregation _config;

	private int[][] _counters;

	private int _count;

	private double _sum;

	private readonly int _mantissaMax;

	private readonly int _mantissaMask;

	private readonly int _mantissaShift;

	public ExponentialHistogramAggregator(QuantileAggregation config)
	{
		_config = config;
		_counters = new int[4096][];
		if (_config.MaxRelativeError < 0.0001)
		{
			throw new ArgumentException();
		}
		int num = (int)Math.Ceiling(Math.Log(1.0 / _config.MaxRelativeError, 2.0)) - 1;
		_mantissaShift = 52 - num;
		_mantissaMax = 1 << num;
		_mantissaMask = _mantissaMax - 1;
	}

	public override IAggregationStatistics Collect()
	{
		int[][] counters;
		int count;
		double sum;
		lock (this)
		{
			counters = _counters;
			count = _count;
			sum = _sum;
			_counters = new int[4096][];
			_count = 0;
			_sum = 0.0;
		}
		QuantileValue[] array = new QuantileValue[_config.Quantiles.Length];
		int num = 0;
		if (num == _config.Quantiles.Length)
		{
			return new HistogramStatistics(array, count, sum);
		}
		int num2 = QuantileToRank(_config.Quantiles[num], count);
		int num3 = 0;
		foreach (Bucket item in IterateBuckets(counters))
		{
			num3 += item.Count;
			while (num3 > num2)
			{
				array[num] = new QuantileValue(_config.Quantiles[num], item.Value);
				num++;
				if (num == _config.Quantiles.Length)
				{
					return new HistogramStatistics(array, count, sum);
				}
				num2 = QuantileToRank(_config.Quantiles[num], count);
			}
		}
		return new HistogramStatistics(Array.Empty<QuantileValue>(), count, sum);
	}

	private IEnumerable<Bucket> IterateBuckets(int[][] counters)
	{
		for (int exponent = 4094; exponent >= 2048; exponent--)
		{
			int[] mantissaCounts = counters[exponent];
			if (mantissaCounts != null)
			{
				for (int mantissa = _mantissaMax - 1; mantissa >= 0; mantissa--)
				{
					int num = mantissaCounts[mantissa];
					if (num > 0)
					{
						yield return new Bucket(GetBucketCanonicalValue(exponent, mantissa), num);
					}
				}
			}
		}
		for (int exponent = 0; exponent < 2047; exponent++)
		{
			int[] mantissaCounts = counters[exponent];
			if (mantissaCounts == null)
			{
				continue;
			}
			for (int mantissa = 0; mantissa < _mantissaMax; mantissa++)
			{
				int num2 = mantissaCounts[mantissa];
				if (num2 > 0)
				{
					yield return new Bucket(GetBucketCanonicalValue(exponent, mantissa), num2);
				}
			}
		}
	}

	public override void Update(double measurement)
	{
		lock (this)
		{
			ulong num = (ulong)BitConverter.DoubleToInt64Bits(measurement);
			int num2 = (int)(num >> 52);
			int num3 = (int)(num >> _mantissaShift) & _mantissaMask;
			ref int[] reference = ref _counters[num2];
			if (reference == null)
			{
				reference = new int[_mantissaMax];
			}
			reference[num3]++;
			if (num2 != 2047 && num2 != 4095)
			{
				_count++;
				_sum += measurement;
			}
		}
	}

	private static int QuantileToRank(double quantile, int count)
	{
		return Math.Min(Math.Max(0, (int)(quantile * (double)count)), count - 1);
	}

	private double GetBucketCanonicalValue(int exponent, int mantissa)
	{
		long value = ((long)exponent << 52) | ((long)mantissa << _mantissaShift);
		return BitConverter.Int64BitsToDouble(value);
	}
}
