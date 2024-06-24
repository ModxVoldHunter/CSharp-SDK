using System.Collections;
using System.Collections.Generic;

namespace System;

internal sealed class SZGenericArrayEnumerator<T> : SZGenericArrayEnumeratorBase, IEnumerator<T>, IDisposable, IEnumerator
{
	private readonly T[] _array;

	internal static readonly SZGenericArrayEnumerator<T> Empty = new SZGenericArrayEnumerator<T>(null, 0);

	public T Current
	{
		get
		{
			if ((uint)_index >= (uint)_endIndex)
			{
				ThrowHelper.ThrowInvalidOperationException_EnumCurrent(_index);
			}
			return _array[_index];
		}
	}

	object IEnumerator.Current => Current;

	internal SZGenericArrayEnumerator(T[] array, int endIndex)
		: base(endIndex)
	{
		_array = array;
	}
}
