namespace System.Buffers;

internal sealed class EmptySearchValues<T> : SearchValues<T> where T : IEquatable<T>
{
	internal override T[] GetValues()
	{
		return Array.Empty<T>();
	}

	internal override bool ContainsCore(T value)
	{
		return false;
	}

	internal override int IndexOfAny(ReadOnlySpan<T> span)
	{
		return -1;
	}

	internal override int IndexOfAnyExcept(ReadOnlySpan<T> span)
	{
		if (!span.IsEmpty)
		{
			return 0;
		}
		return -1;
	}

	internal override int LastIndexOfAny(ReadOnlySpan<T> span)
	{
		return -1;
	}

	internal override int LastIndexOfAnyExcept(ReadOnlySpan<T> span)
	{
		return span.Length - 1;
	}
}
