using System.Collections.Generic;

namespace System.Text.RegularExpressions.Symbolic;

internal sealed class SparseIntMap<T> where T : struct
{
	private readonly List<KeyValuePair<int, T>> _dense = new List<KeyValuePair<int, T>>();

	private int[] _sparse = new int[16];

	public int Count => _dense.Count;

	public List<KeyValuePair<int, T>> Values => _dense;

	public void Clear()
	{
		_dense.Clear();
	}

	public bool Add(int key, out int index)
	{
		int[] sparse = _sparse;
		if ((uint)key < (uint)sparse.Length)
		{
			List<KeyValuePair<int, T>> dense = _dense;
			int num = sparse[key];
			if (num < dense.Count)
			{
				int key2 = dense[num].Key;
				if (key == key2)
				{
					index = num;
					return false;
				}
			}
			sparse[key] = (index = _dense.Count);
			dense.Add(new KeyValuePair<int, T>(key, default(T)));
			return true;
		}
		return GrowAndAdd(key, out index);
	}

	public bool Add(int key, T value)
	{
		int index;
		bool result = Add(key, out index);
		Update(index, key, value);
		return result;
	}

	public void Update(int index, int key, T value)
	{
		_dense[index] = new KeyValuePair<int, T>(key, value);
	}

	private bool GrowAndAdd(int key, out int index)
	{
		int val = key + 1;
		val = Math.Max(2 * _sparse.Length, val);
		Array.Resize(ref _sparse, val);
		_sparse[key] = (index = _dense.Count);
		_dense.Add(new KeyValuePair<int, T>(key, default(T)));
		return true;
	}
}
