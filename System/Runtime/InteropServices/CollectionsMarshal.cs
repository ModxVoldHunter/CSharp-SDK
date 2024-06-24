using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace System.Runtime.InteropServices;

public static class CollectionsMarshal
{
	public static Span<T> AsSpan<T>(List<T>? list)
	{
		if (list != null)
		{
			return new Span<T>(list._items, 0, list._size);
		}
		return default(Span<T>);
	}

	public static ref TValue GetValueRefOrNullRef<TKey, TValue>(Dictionary<TKey, TValue> dictionary, TKey key) where TKey : notnull
	{
		return ref dictionary.FindValue(key);
	}

	public static ref TValue? GetValueRefOrAddDefault<TKey, TValue>(Dictionary<TKey, TValue> dictionary, TKey key, out bool exists) where TKey : notnull
	{
		return ref Dictionary<TKey, TValue>.CollectionsMarshalHelper.GetValueRefOrAddDefault(dictionary, key, out exists);
	}

	public static void SetCount<T>(List<T> list, int count)
	{
		if (count < 0)
		{
			ThrowHelper.ThrowArgumentOutOfRangeException_NeedNonNegNum("count");
		}
		list._version++;
		if (count > list.Capacity)
		{
			list.Grow(count);
		}
		else if (count < list._size && RuntimeHelpers.IsReferenceOrContainsReferences<T>())
		{
			Array.Clear(list._items, count, list._size - count);
		}
		list._size = count;
	}
}
