using System.Collections.Generic;

namespace System.Linq.Parallel;

internal readonly struct WrapperEqualityComparer<T> : IEqualityComparer<Wrapper<T>>
{
	private readonly IEqualityComparer<T> _comparer;

	internal WrapperEqualityComparer(IEqualityComparer<T> comparer)
	{
		_comparer = comparer ?? EqualityComparer<T>.Default;
	}

	public bool Equals(Wrapper<T> x, Wrapper<T> y)
	{
		return _comparer.Equals(x.Value, y.Value);
	}

	public int GetHashCode(Wrapper<T> x)
	{
		T value = x.Value;
		if (value != null)
		{
			return _comparer.GetHashCode(value);
		}
		return 0;
	}
}
