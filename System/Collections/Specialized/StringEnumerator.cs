namespace System.Collections.Specialized;

public class StringEnumerator
{
	private readonly IEnumerator _baseEnumerator;

	public string? Current => (string)_baseEnumerator.Current;

	internal StringEnumerator(StringCollection mappings)
	{
		_baseEnumerator = ((IEnumerable)mappings).GetEnumerator();
	}

	public bool MoveNext()
	{
		return _baseEnumerator.MoveNext();
	}

	public void Reset()
	{
		_baseEnumerator.Reset();
	}
}
