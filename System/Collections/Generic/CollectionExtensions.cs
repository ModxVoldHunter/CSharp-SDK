using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;

namespace System.Collections.Generic;

public static class CollectionExtensions
{
	public static TValue? GetValueOrDefault<TKey, TValue>(this IReadOnlyDictionary<TKey, TValue> dictionary, TKey key)
	{
		return dictionary.GetValueOrDefault(key, default(TValue));
	}

	public static TValue GetValueOrDefault<TKey, TValue>(this IReadOnlyDictionary<TKey, TValue> dictionary, TKey key, TValue defaultValue)
	{
		if (dictionary == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.dictionary);
		}
		if (!dictionary.TryGetValue(key, out TValue value))
		{
			return defaultValue;
		}
		return value;
	}

	public static bool TryAdd<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, TValue value)
	{
		if (dictionary == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.dictionary);
		}
		if (!dictionary.ContainsKey(key))
		{
			dictionary.Add(key, value);
			return true;
		}
		return false;
	}

	public static bool Remove<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, [MaybeNullWhen(false)] out TValue value)
	{
		if (dictionary == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.dictionary);
		}
		if (dictionary.TryGetValue(key, out value))
		{
			dictionary.Remove(key);
			return true;
		}
		value = default(TValue);
		return false;
	}

	public static ReadOnlyCollection<T> AsReadOnly<T>(this IList<T> list)
	{
		return new ReadOnlyCollection<T>(list);
	}

	public static ReadOnlyDictionary<TKey, TValue> AsReadOnly<TKey, TValue>(this IDictionary<TKey, TValue> dictionary) where TKey : notnull
	{
		return new ReadOnlyDictionary<TKey, TValue>(dictionary);
	}

	public static void AddRange<T>(this List<T> list, ReadOnlySpan<T> source)
	{
		if (list == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.list);
		}
		if (!source.IsEmpty)
		{
			if (list._items.Length - list._size < source.Length)
			{
				list.Grow(checked(list._size + source.Length));
			}
			source.CopyTo(list._items.AsSpan(list._size));
			list._size += source.Length;
			list._version++;
		}
	}

	public static void InsertRange<T>(this List<T> list, int index, ReadOnlySpan<T> source)
	{
		if (list == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.list);
		}
		if ((uint)index > (uint)list._size)
		{
			ThrowHelper.ThrowArgumentOutOfRange_IndexMustBeLessOrEqualException();
		}
		if (!source.IsEmpty)
		{
			if (list._items.Length - list._size < source.Length)
			{
				list.Grow(checked(list._size + source.Length));
			}
			if (index < list._size)
			{
				Array.Copy(list._items, index, list._items, index + source.Length, list._size - index);
			}
			source.CopyTo(list._items.AsSpan(index));
			list._size += source.Length;
			list._version++;
		}
	}

	public static void CopyTo<T>(this List<T> list, Span<T> destination)
	{
		if (list == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.list);
		}
		new ReadOnlySpan<T>(list._items, 0, list._size).CopyTo(destination);
	}
}
