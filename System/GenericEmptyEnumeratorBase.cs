using System.Collections;

namespace System;

internal abstract class GenericEmptyEnumeratorBase : IDisposable, IEnumerator
{
	public object Current
	{
		get
		{
			ThrowHelper.ThrowInvalidOperationException_EnumCurrent(-1);
			return null;
		}
	}

	public bool MoveNext()
	{
		return false;
	}

	public void Reset()
	{
	}

	public void Dispose()
	{
	}
}
