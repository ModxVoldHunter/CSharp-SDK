using System.Collections;
using System.Collections.Generic;

namespace System.Net.Http.Headers;

public sealed class HttpHeaderValueCollection<T> : ICollection<T>, IEnumerable<T>, IEnumerable where T : class
{
	private readonly HeaderDescriptor _descriptor;

	private readonly HttpHeaders _store;

	public int Count => GetCount();

	public bool IsReadOnly => false;

	internal HttpHeaderValueCollection(HeaderDescriptor descriptor, HttpHeaders store)
	{
		_store = store;
		_descriptor = descriptor;
	}

	public void Add(T item)
	{
		CheckValue(item);
		_store.AddParsedValue(_descriptor, item);
	}

	public void ParseAdd(string? input)
	{
		_store.Add(_descriptor, input);
	}

	public bool TryParseAdd(string? input)
	{
		return _store.TryParseAndAddValue(_descriptor, input);
	}

	public void Clear()
	{
		_store.Remove(_descriptor);
	}

	public bool Contains(T item)
	{
		CheckValue(item);
		return _store.ContainsParsedValue(_descriptor, item);
	}

	public void CopyTo(T[] array, int arrayIndex)
	{
		ArgumentNullException.ThrowIfNull(array, "array");
		ArgumentOutOfRangeException.ThrowIfNegative(arrayIndex, "arrayIndex");
		ArgumentOutOfRangeException.ThrowIfGreaterThan(arrayIndex, array.Length, "arrayIndex");
		object parsedAndInvalidValues = _store.GetParsedAndInvalidValues(_descriptor);
		if (parsedAndInvalidValues == null)
		{
			return;
		}
		if (!(parsedAndInvalidValues is List<object> list))
		{
			if (!(parsedAndInvalidValues is HttpHeaders.InvalidValue))
			{
				if (arrayIndex == array.Length)
				{
					throw new ArgumentException(System.SR.net_http_copyto_array_too_small);
				}
				array[arrayIndex] = (T)parsedAndInvalidValues;
			}
			return;
		}
		foreach (object item in list)
		{
			if (!(item is HttpHeaders.InvalidValue))
			{
				if (arrayIndex == array.Length)
				{
					throw new ArgumentException(System.SR.net_http_copyto_array_too_small);
				}
				array[arrayIndex++] = (T)item;
			}
		}
	}

	public bool Remove(T item)
	{
		CheckValue(item);
		return _store.RemoveParsedValue(_descriptor, item);
	}

	public IEnumerator<T> GetEnumerator()
	{
		object parsedAndInvalidValues = _store.GetParsedAndInvalidValues(_descriptor);
		if (parsedAndInvalidValues != null && !(parsedAndInvalidValues is HttpHeaders.InvalidValue))
		{
			return Iterate(parsedAndInvalidValues);
		}
		return ((IEnumerable<T>)Array.Empty<T>()).GetEnumerator();
		static IEnumerator<T> Iterate(object storeValue)
		{
			if (storeValue is List<object> list)
			{
				foreach (object item in list)
				{
					if (!(item is HttpHeaders.InvalidValue))
					{
						yield return (T)item;
					}
				}
			}
			else
			{
				yield return (T)storeValue;
			}
		}
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return GetEnumerator();
	}

	public override string ToString()
	{
		return _store.GetHeaderString(_descriptor);
	}

	private void CheckValue(T item)
	{
		ArgumentNullException.ThrowIfNull(item, "item");
		if (_descriptor.Parser == GenericHeaderParser.TokenListParser)
		{
			HeaderUtilities.CheckValidToken((string)(object)item, "item");
		}
	}

	private int GetCount()
	{
		object parsedAndInvalidValues = _store.GetParsedAndInvalidValues(_descriptor);
		if (parsedAndInvalidValues == null)
		{
			return 0;
		}
		if (!(parsedAndInvalidValues is List<object> list))
		{
			if (!(parsedAndInvalidValues is HttpHeaders.InvalidValue))
			{
				return 1;
			}
			return 0;
		}
		int num = 0;
		foreach (object item in list)
		{
			if (!(item is HttpHeaders.InvalidValue))
			{
				num++;
			}
		}
		return num;
	}
}
