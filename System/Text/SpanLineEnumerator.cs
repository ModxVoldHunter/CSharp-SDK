namespace System.Text;

public ref struct SpanLineEnumerator
{
	private ReadOnlySpan<char> _remaining;

	private ReadOnlySpan<char> _current;

	private bool _isEnumeratorActive;

	public ReadOnlySpan<char> Current => _current;

	internal SpanLineEnumerator(ReadOnlySpan<char> buffer)
	{
		_remaining = buffer;
		_current = default(ReadOnlySpan<char>);
		_isEnumeratorActive = true;
	}

	public SpanLineEnumerator GetEnumerator()
	{
		return this;
	}

	public bool MoveNext()
	{
		if (!_isEnumeratorActive)
		{
			return false;
		}
		ReadOnlySpan<char> remaining = _remaining;
		int num = remaining.IndexOfAny(string.SearchValuesStorage.NewLineChars);
		if ((uint)num < (uint)remaining.Length)
		{
			int num2 = 1;
			if (remaining[num] == '\r' && (uint)(num + 1) < (uint)remaining.Length && remaining[num + 1] == '\n')
			{
				num2 = 2;
			}
			_current = remaining.Slice(0, num);
			_remaining = remaining.Slice(num + num2);
		}
		else
		{
			_current = remaining;
			_remaining = default(ReadOnlySpan<char>);
			_isEnumeratorActive = false;
		}
		return true;
	}
}
