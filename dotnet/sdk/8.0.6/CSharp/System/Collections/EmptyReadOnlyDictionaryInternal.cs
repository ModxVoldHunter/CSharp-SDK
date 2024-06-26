namespace System.Collections;

internal sealed class EmptyReadOnlyDictionaryInternal : IDictionary, ICollection, IEnumerable
{
	private sealed class NodeEnumerator : IDictionaryEnumerator, IEnumerator
	{
		public object Current
		{
			get
			{
				throw new InvalidOperationException(SR.InvalidOperation_EnumOpCantHappen);
			}
		}

		public object Key
		{
			get
			{
				throw new InvalidOperationException(SR.InvalidOperation_EnumOpCantHappen);
			}
		}

		public object Value
		{
			get
			{
				throw new InvalidOperationException(SR.InvalidOperation_EnumOpCantHappen);
			}
		}

		public DictionaryEntry Entry
		{
			get
			{
				throw new InvalidOperationException(SR.InvalidOperation_EnumOpCantHappen);
			}
		}

		public bool MoveNext()
		{
			return false;
		}

		public void Reset()
		{
		}
	}

	public int Count => 0;

	public object SyncRoot => this;

	public bool IsSynchronized => false;

	public object this[object key]
	{
		get
		{
			ArgumentNullException.ThrowIfNull(key, "key");
			return null;
		}
		set
		{
			ArgumentNullException.ThrowIfNull(key, "key");
			throw new InvalidOperationException(SR.InvalidOperation_ReadOnly);
		}
	}

	public ICollection Keys => Array.Empty<object>();

	public ICollection Values => Array.Empty<object>();

	public bool IsReadOnly => true;

	public bool IsFixedSize => true;

	IEnumerator IEnumerable.GetEnumerator()
	{
		return new NodeEnumerator();
	}

	public void CopyTo(Array array, int index)
	{
		ArgumentNullException.ThrowIfNull(array, "array");
		if (array.Rank != 1)
		{
			throw new ArgumentException(SR.Arg_RankMultiDimNotSupported);
		}
		ArgumentOutOfRangeException.ThrowIfNegative(index, "index");
		if (array.Length - index < Count)
		{
			throw new ArgumentException(SR.ArgumentOutOfRange_IndexMustBeLessOrEqual, "index");
		}
	}

	public bool Contains(object key)
	{
		return false;
	}

	public void Add(object key, object value)
	{
		ArgumentNullException.ThrowIfNull(key, "key");
		throw new InvalidOperationException(SR.InvalidOperation_ReadOnly);
	}

	public void Clear()
	{
		throw new InvalidOperationException(SR.InvalidOperation_ReadOnly);
	}

	public IDictionaryEnumerator GetEnumerator()
	{
		return new NodeEnumerator();
	}

	public void Remove(object key)
	{
		throw new InvalidOperationException(SR.InvalidOperation_ReadOnly);
	}
}
