using System.Collections;
using System.Collections.Generic;

namespace System;

public sealed class CharEnumerator : IEnumerator, IEnumerator<char>, IDisposable, ICloneable
{
	private string _str;

	private int _index = -1;

	object? IEnumerator.Current => Current;

	public char Current
	{
		get
		{
			int index = _index;
			string str = _str;
			if ((uint)index >= (uint)str.Length)
			{
				ThrowHelper.ThrowInvalidOperationException_EnumCurrent(_index);
			}
			return str[index];
		}
	}

	internal CharEnumerator(string str)
	{
		_str = str;
	}

	public object Clone()
	{
		return MemberwiseClone();
	}

	public bool MoveNext()
	{
		int num = _index + 1;
		int length = _str.Length;
		if (num < length)
		{
			_index = num;
			return true;
		}
		_index = length;
		return false;
	}

	public void Dispose()
	{
		_str = null;
	}

	public void Reset()
	{
		_index = -1;
	}
}
