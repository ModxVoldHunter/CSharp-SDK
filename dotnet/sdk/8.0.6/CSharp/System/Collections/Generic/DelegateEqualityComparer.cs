using System.Diagnostics.CodeAnalysis;

namespace System.Collections.Generic;

internal sealed class DelegateEqualityComparer<T> : EqualityComparer<T>
{
	private readonly Func<T, T, bool> _equals;

	private readonly Func<T, int> _getHashCode;

	public DelegateEqualityComparer(Func<T, T, bool> equals, Func<T, int> getHashCode)
	{
		_equals = equals;
		_getHashCode = getHashCode;
	}

	public override bool Equals(T x, T y)
	{
		return _equals(x, y);
	}

	public override int GetHashCode([DisallowNull] T obj)
	{
		return _getHashCode(obj);
	}

	public override bool Equals(object obj)
	{
		if (obj is DelegateEqualityComparer<T> delegateEqualityComparer && _equals == delegateEqualityComparer._equals)
		{
			return _getHashCode == delegateEqualityComparer._getHashCode;
		}
		return false;
	}

	public override int GetHashCode()
	{
		return HashCode.Combine(_equals.GetHashCode(), _getHashCode.GetHashCode());
	}
}
