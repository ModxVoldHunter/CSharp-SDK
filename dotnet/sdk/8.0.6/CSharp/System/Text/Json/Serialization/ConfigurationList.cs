using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace System.Text.Json.Serialization;

[DebuggerDisplay("{DebuggerDisplay,nq}")]
[DebuggerTypeProxy(typeof(ConfigurationList<>.ConfigurationListDebugView))]
internal abstract class ConfigurationList<TItem> : IList<TItem>, ICollection<TItem>, IEnumerable<TItem>, IEnumerable
{
	private sealed class ConfigurationListDebugView
	{
		[CompilerGenerated]
		private ConfigurationList<TItem> _003Ccollection_003EP;

		[DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
		public TItem[] Items => _003Ccollection_003EP._list.ToArray();

		public ConfigurationListDebugView(ConfigurationList<TItem> collection)
		{
			_003Ccollection_003EP = collection;
			base._002Ector();
		}
	}

	protected readonly List<TItem> _list;

	public abstract bool IsReadOnly { get; }

	public TItem this[int index]
	{
		get
		{
			return _list[index];
		}
		set
		{
			if (value == null)
			{
				ThrowHelper.ThrowArgumentNullException("value");
			}
			ValidateAddedValue(value);
			OnCollectionModifying();
			_list[index] = value;
		}
	}

	public int Count => _list.Count;

	[DebuggerBrowsable(DebuggerBrowsableState.Never)]
	private string DebuggerDisplay => $"Count = {Count}, IsReadOnly = {IsReadOnly}";

	public ConfigurationList(IEnumerable<TItem> source = null)
	{
		_list = ((source == null) ? new List<TItem>() : new List<TItem>(source));
	}

	protected abstract void OnCollectionModifying();

	protected virtual void ValidateAddedValue(TItem item)
	{
	}

	public void Add(TItem item)
	{
		if (item == null)
		{
			ThrowHelper.ThrowArgumentNullException("item");
		}
		ValidateAddedValue(item);
		OnCollectionModifying();
		_list.Add(item);
	}

	public void Clear()
	{
		OnCollectionModifying();
		_list.Clear();
	}

	public bool Contains(TItem item)
	{
		return _list.Contains(item);
	}

	public void CopyTo(TItem[] array, int arrayIndex)
	{
		_list.CopyTo(array, arrayIndex);
	}

	public List<TItem>.Enumerator GetEnumerator()
	{
		return _list.GetEnumerator();
	}

	public int IndexOf(TItem item)
	{
		return _list.IndexOf(item);
	}

	public void Insert(int index, TItem item)
	{
		if (item == null)
		{
			ThrowHelper.ThrowArgumentNullException("item");
		}
		ValidateAddedValue(item);
		OnCollectionModifying();
		_list.Insert(index, item);
	}

	public bool Remove(TItem item)
	{
		OnCollectionModifying();
		return _list.Remove(item);
	}

	public void RemoveAt(int index)
	{
		OnCollectionModifying();
		_list.RemoveAt(index);
	}

	IEnumerator<TItem> IEnumerable<TItem>.GetEnumerator()
	{
		return _list.GetEnumerator();
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return _list.GetEnumerator();
	}
}
