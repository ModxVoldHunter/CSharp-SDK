using System.Collections.Generic;

namespace System.Xml.Xsl.IlGen;

internal sealed class UniqueList<T>
{
	private readonly Dictionary<T, int> _lookup = new Dictionary<T, int>();

	private readonly List<T> _list = new List<T>();

	public int Add(T value)
	{
		if (!_lookup.TryGetValue(value, out var value2))
		{
			value2 = _list.Count;
			_lookup.Add(value, value2);
			_list.Add(value);
		}
		return value2;
	}

	public T[] ToArray()
	{
		return _list.ToArray();
	}
}
