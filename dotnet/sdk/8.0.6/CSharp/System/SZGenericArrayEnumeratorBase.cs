namespace System;

internal abstract class SZGenericArrayEnumeratorBase : IDisposable
{
	protected int _index;

	protected readonly int _endIndex;

	protected SZGenericArrayEnumeratorBase(int endIndex)
	{
		_index = -1;
		_endIndex = endIndex;
	}

	public bool MoveNext()
	{
		int num = _index + 1;
		if ((uint)num < (uint)_endIndex)
		{
			_index = num;
			return true;
		}
		_index = _endIndex;
		return false;
	}

	public void Reset()
	{
		_index = -1;
	}

	public void Dispose()
	{
	}
}
