using System.Collections;
using System.Collections.Generic;

namespace System;

internal sealed class GenericEmptyEnumerator<T> : GenericEmptyEnumeratorBase, IEnumerator<T>, IDisposable, IEnumerator
{
	public static readonly GenericEmptyEnumerator<T> Instance = new GenericEmptyEnumerator<T>();

	public new T Current
	{
		get
		{
			ThrowHelper.ThrowInvalidOperationException_EnumCurrent(-1);
			return default(T);
		}
	}

	private GenericEmptyEnumerator()
	{
	}
}
