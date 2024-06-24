namespace System.Diagnostics.Metrics;

internal struct StringSequenceMany : IEquatable<StringSequenceMany>, IStringSequence
{
	private readonly string[] _values;

	public StringSequenceMany(string[] values)
	{
		_values = values;
	}

	public Span<string> AsSpan()
	{
		return _values.AsSpan();
	}

	public bool Equals(StringSequenceMany other)
	{
		return _values.AsSpan().SequenceEqual(other._values.AsSpan());
	}

	public override bool Equals(object obj)
	{
		if (obj is StringSequenceMany other)
		{
			return Equals(other);
		}
		return false;
	}

	public override int GetHashCode()
	{
		HashCode hashCode = default(HashCode);
		for (int i = 0; i < _values.Length; i++)
		{
			hashCode.Add(_values[i]);
		}
		return hashCode.ToHashCode();
	}
}
