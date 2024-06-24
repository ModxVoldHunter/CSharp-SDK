using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace System.Text.Json;

internal sealed class JsonPropertyDictionary<T> where T : class
{
	private sealed class KeyCollection : IList<string>, ICollection<string>, IEnumerable<string>, IEnumerable
	{
		private readonly JsonPropertyDictionary<T> _parent;

		public int Count => _parent.Count;

		public bool IsReadOnly => true;

		public string this[int index]
		{
			get
			{
				return _parent.List[index].Key;
			}
			set
			{
				throw ThrowHelper.GetNotSupportedException_CollectionIsReadOnly();
			}
		}

		public KeyCollection(JsonPropertyDictionary<T> jsonObject)
		{
			_parent = jsonObject;
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			foreach (KeyValuePair<string, T> item in _parent)
			{
				yield return item.Key;
			}
		}

		public void Add(string propertyName)
		{
			ThrowHelper.ThrowNotSupportedException_CollectionIsReadOnly();
		}

		public void Clear()
		{
			ThrowHelper.ThrowNotSupportedException_CollectionIsReadOnly();
		}

		public bool Contains(string propertyName)
		{
			return _parent.ContainsProperty(propertyName);
		}

		public void CopyTo(string[] propertyNameArray, int index)
		{
			if (index < 0)
			{
				ThrowHelper.ThrowArgumentOutOfRangeException_ArrayIndexNegative("index");
			}
			foreach (KeyValuePair<string, T> item in _parent)
			{
				if (index >= propertyNameArray.Length)
				{
					ThrowHelper.ThrowArgumentException_ArrayTooSmall("propertyNameArray");
				}
				propertyNameArray[index++] = item.Key;
			}
		}

		public IEnumerator<string> GetEnumerator()
		{
			foreach (KeyValuePair<string, T> item in _parent)
			{
				yield return item.Key;
			}
		}

		bool ICollection<string>.Remove(string propertyName)
		{
			throw ThrowHelper.GetNotSupportedException_CollectionIsReadOnly();
		}

		public int IndexOf(string item)
		{
			throw ThrowHelper.GetNotSupportedException_CollectionIsReadOnly();
		}

		public void Insert(int index, string item)
		{
			throw ThrowHelper.GetNotSupportedException_CollectionIsReadOnly();
		}

		public void RemoveAt(int index)
		{
			throw ThrowHelper.GetNotSupportedException_CollectionIsReadOnly();
		}
	}

	private sealed class ValueCollection : IList<T>, ICollection<T>, IEnumerable<T>, IEnumerable
	{
		private readonly JsonPropertyDictionary<T> _parent;

		public int Count => _parent.Count;

		public bool IsReadOnly => true;

		public T this[int index]
		{
			get
			{
				return _parent.List[index].Value;
			}
			set
			{
				throw ThrowHelper.GetNotSupportedException_CollectionIsReadOnly();
			}
		}

		public ValueCollection(JsonPropertyDictionary<T> jsonObject)
		{
			_parent = jsonObject;
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			foreach (KeyValuePair<string, T> item in _parent)
			{
				yield return item.Value;
			}
		}

		public void Add(T jsonNode)
		{
			ThrowHelper.ThrowNotSupportedException_CollectionIsReadOnly();
		}

		public void Clear()
		{
			ThrowHelper.ThrowNotSupportedException_CollectionIsReadOnly();
		}

		public bool Contains(T jsonNode)
		{
			return _parent.ContainsValue(jsonNode);
		}

		public void CopyTo(T[] nodeArray, int index)
		{
			if (index < 0)
			{
				ThrowHelper.ThrowArgumentOutOfRangeException_ArrayIndexNegative("index");
			}
			foreach (KeyValuePair<string, T> item in _parent)
			{
				if (index >= nodeArray.Length)
				{
					ThrowHelper.ThrowArgumentException_ArrayTooSmall("nodeArray");
				}
				nodeArray[index++] = item.Value;
			}
		}

		public IEnumerator<T> GetEnumerator()
		{
			foreach (KeyValuePair<string, T> item in _parent)
			{
				yield return item.Value;
			}
		}

		bool ICollection<T>.Remove(T node)
		{
			throw ThrowHelper.GetNotSupportedException_CollectionIsReadOnly();
		}

		public int IndexOf(T item)
		{
			throw ThrowHelper.GetNotSupportedException_CollectionIsReadOnly();
		}

		public void Insert(int index, T item)
		{
			throw ThrowHelper.GetNotSupportedException_CollectionIsReadOnly();
		}

		public void RemoveAt(int index)
		{
			throw ThrowHelper.GetNotSupportedException_CollectionIsReadOnly();
		}
	}

	private Dictionary<string, T> _propertyDictionary;

	private readonly List<KeyValuePair<string, T>> _propertyList;

	private readonly StringComparer _stringComparer;

	private KeyCollection _keyCollection;

	private ValueCollection _valueCollection;

	public List<KeyValuePair<string, T>> List => _propertyList;

	public int Count => _propertyList.Count;

	public IList<string> Keys => GetKeyCollection();

	public IList<T> Values => GetValueCollection();

	public bool IsReadOnly { get; }

	public T this[string propertyName]
	{
		get
		{
			if (TryGetPropertyValue(propertyName, out var value))
			{
				return value;
			}
			return null;
		}
	}

	public JsonPropertyDictionary(bool caseInsensitive)
	{
		_stringComparer = (caseInsensitive ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal);
		_propertyList = new List<KeyValuePair<string, T>>();
	}

	public JsonPropertyDictionary(bool caseInsensitive, int capacity)
	{
		_stringComparer = (caseInsensitive ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal);
		_propertyList = new List<KeyValuePair<string, T>>(capacity);
	}

	public void Add(string propertyName, T value)
	{
		if (IsReadOnly)
		{
			ThrowHelper.ThrowNotSupportedException_CollectionIsReadOnly();
		}
		if (propertyName == null)
		{
			ThrowHelper.ThrowArgumentNullException("propertyName");
		}
		AddValue(propertyName, value);
	}

	public void Add(KeyValuePair<string, T> property)
	{
		if (IsReadOnly)
		{
			ThrowHelper.ThrowNotSupportedException_CollectionIsReadOnly();
		}
		Add(property.Key, property.Value);
	}

	public void Clear()
	{
		if (IsReadOnly)
		{
			ThrowHelper.ThrowNotSupportedException_CollectionIsReadOnly();
		}
		_propertyList.Clear();
		_propertyDictionary?.Clear();
	}

	public bool ContainsKey(string propertyName)
	{
		if (propertyName == null)
		{
			ThrowHelper.ThrowArgumentNullException("propertyName");
		}
		return ContainsProperty(propertyName);
	}

	public bool Contains(KeyValuePair<string, T> item)
	{
		using (List<KeyValuePair<string, T>>.Enumerator enumerator = GetEnumerator())
		{
			while (enumerator.MoveNext())
			{
				KeyValuePair<string, T> current = enumerator.Current;
				if (item.Value == current.Value && _stringComparer.Equals(item.Key, current.Key))
				{
					return true;
				}
			}
		}
		return false;
	}

	public void CopyTo(KeyValuePair<string, T>[] array, int index)
	{
		if (index < 0)
		{
			ThrowHelper.ThrowArgumentOutOfRangeException_ArrayIndexNegative("index");
		}
		foreach (KeyValuePair<string, T> property in _propertyList)
		{
			if (index >= array.Length)
			{
				ThrowHelper.ThrowArgumentException_ArrayTooSmall("array");
			}
			array[index++] = property;
		}
	}

	public List<KeyValuePair<string, T>>.Enumerator GetEnumerator()
	{
		return _propertyList.GetEnumerator();
	}

	public bool TryGetValue(string propertyName, [MaybeNullWhen(false)] out T value)
	{
		if (propertyName == null)
		{
			ThrowHelper.ThrowArgumentNullException("propertyName");
		}
		if (_propertyDictionary != null)
		{
			return _propertyDictionary.TryGetValue(propertyName, out value);
		}
		foreach (KeyValuePair<string, T> property in _propertyList)
		{
			if (_stringComparer.Equals(propertyName, property.Key))
			{
				value = property.Value;
				return true;
			}
		}
		value = null;
		return false;
	}

	public T SetValue(string propertyName, T value, out bool valueAlreadyInDictionary)
	{
		if (IsReadOnly)
		{
			ThrowHelper.ThrowNotSupportedException_CollectionIsReadOnly();
		}
		if (propertyName == null)
		{
			ThrowHelper.ThrowArgumentNullException("propertyName");
		}
		CreateDictionaryIfThresholdMet();
		valueAlreadyInDictionary = false;
		T val = null;
		if (_propertyDictionary != null)
		{
			if (_propertyDictionary.TryAdd(propertyName, value))
			{
				_propertyList.Add(new KeyValuePair<string, T>(propertyName, value));
				return null;
			}
			val = _propertyDictionary[propertyName];
			if (val == value)
			{
				valueAlreadyInDictionary = true;
				return null;
			}
		}
		int num = FindValueIndex(propertyName);
		if (num >= 0)
		{
			if (_propertyDictionary != null)
			{
				_propertyDictionary[propertyName] = value;
			}
			else
			{
				KeyValuePair<string, T> keyValuePair = _propertyList[num];
				if (keyValuePair.Value == value)
				{
					valueAlreadyInDictionary = true;
					return null;
				}
				val = keyValuePair.Value;
			}
			_propertyList[num] = new KeyValuePair<string, T>(propertyName, value);
		}
		else
		{
			_propertyDictionary?.Add(propertyName, value);
			_propertyList.Add(new KeyValuePair<string, T>(propertyName, value));
		}
		return val;
	}

	private void AddValue(string propertyName, T value)
	{
		if (!TryAddValue(propertyName, value))
		{
			ThrowHelper.ThrowArgumentException_DuplicateKey("propertyName", propertyName);
		}
	}

	internal bool TryAddValue(string propertyName, T value)
	{
		if (IsReadOnly)
		{
			ThrowHelper.ThrowNotSupportedException_CollectionIsReadOnly();
		}
		CreateDictionaryIfThresholdMet();
		if (_propertyDictionary == null)
		{
			if (ContainsProperty(propertyName))
			{
				return false;
			}
		}
		else if (!_propertyDictionary.TryAdd(propertyName, value))
		{
			return false;
		}
		_propertyList.Add(new KeyValuePair<string, T>(propertyName, value));
		return true;
	}

	private void CreateDictionaryIfThresholdMet()
	{
		if (_propertyDictionary == null && _propertyList.Count > 9)
		{
			_propertyDictionary = JsonHelpers.CreateDictionaryFromCollection(_propertyList, _stringComparer);
		}
	}

	internal bool ContainsValue(T value)
	{
		foreach (T item in GetValueCollection())
		{
			if (item == value)
			{
				return true;
			}
		}
		return false;
	}

	public KeyValuePair<string, T>? FindValue(T value)
	{
		using (List<KeyValuePair<string, T>>.Enumerator enumerator = GetEnumerator())
		{
			while (enumerator.MoveNext())
			{
				KeyValuePair<string, T> current = enumerator.Current;
				if (current.Value == value)
				{
					return current;
				}
			}
		}
		return null;
	}

	private bool ContainsProperty(string propertyName)
	{
		if (_propertyDictionary != null)
		{
			return _propertyDictionary.ContainsKey(propertyName);
		}
		foreach (KeyValuePair<string, T> property in _propertyList)
		{
			if (_stringComparer.Equals(propertyName, property.Key))
			{
				return true;
			}
		}
		return false;
	}

	private int FindValueIndex(string propertyName)
	{
		for (int i = 0; i < _propertyList.Count; i++)
		{
			KeyValuePair<string, T> keyValuePair = _propertyList[i];
			if (_stringComparer.Equals(propertyName, keyValuePair.Key))
			{
				return i;
			}
		}
		return -1;
	}

	public bool TryGetPropertyValue(string propertyName, [MaybeNullWhen(false)] out T value)
	{
		return TryGetValue(propertyName, out value);
	}

	public bool TryRemoveProperty(string propertyName, [MaybeNullWhen(false)] out T existing)
	{
		if (IsReadOnly)
		{
			ThrowHelper.ThrowNotSupportedException_CollectionIsReadOnly();
		}
		if (_propertyDictionary != null)
		{
			if (!_propertyDictionary.TryGetValue(propertyName, out existing))
			{
				return false;
			}
			bool flag = _propertyDictionary.Remove(propertyName);
		}
		for (int i = 0; i < _propertyList.Count; i++)
		{
			KeyValuePair<string, T> keyValuePair = _propertyList[i];
			if (_stringComparer.Equals(keyValuePair.Key, propertyName))
			{
				_propertyList.RemoveAt(i);
				existing = keyValuePair.Value;
				return true;
			}
		}
		existing = null;
		return false;
	}

	public IList<string> GetKeyCollection()
	{
		return _keyCollection ?? (_keyCollection = new KeyCollection(this));
	}

	public IList<T> GetValueCollection()
	{
		return _valueCollection ?? (_valueCollection = new ValueCollection(this));
	}
}
