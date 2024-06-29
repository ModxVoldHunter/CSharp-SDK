using System.Collections;
using System.Collections.Generic;

namespace System.Net.Http.Headers;

public readonly struct HttpHeadersNonValidated : IReadOnlyDictionary<string, HeaderStringValues>, IEnumerable<KeyValuePair<string, HeaderStringValues>>, IEnumerable, IReadOnlyCollection<KeyValuePair<string, HeaderStringValues>>
{
	public struct Enumerator : IEnumerator<KeyValuePair<string, HeaderStringValues>>, IEnumerator, IDisposable
	{
		private readonly HeaderEntry[] _entries;

		private readonly int _numberOfEntries;

		private int _index;

		private KeyValuePair<string, HeaderStringValues> _current;

		public KeyValuePair<string, HeaderStringValues> Current => _current;

		object IEnumerator.Current => _current;

		internal Enumerator(HeaderEntry[] entries, int numberOfEntries)
		{
			_entries = entries;
			_numberOfEntries = numberOfEntries;
			_index = 0;
			_current = default(KeyValuePair<string, HeaderStringValues>);
		}

		public bool MoveNext()
		{
			int index = _index;
			HeaderEntry[] entries = _entries;
			if (entries != null && index < _numberOfEntries && (uint)index < (uint)entries.Length)
			{
				HeaderEntry headerEntry = entries[index];
				_index++;
				HttpHeaders.GetStoreValuesAsStringOrStringArray(headerEntry.Key, headerEntry.Value, out var singleValue, out var multiValue);
				_current = new KeyValuePair<string, HeaderStringValues>(headerEntry.Key.Name, (singleValue != null) ? new HeaderStringValues(headerEntry.Key, singleValue) : new HeaderStringValues(headerEntry.Key, multiValue));
				return true;
			}
			_current = default(KeyValuePair<string, HeaderStringValues>);
			return false;
		}

		public void Dispose()
		{
		}

		void IEnumerator.Reset()
		{
			throw new NotSupportedException();
		}
	}

	private readonly HttpHeaders _headers;

	public int Count => _headers?.Count ?? 0;

	public HeaderStringValues this[string headerName]
	{
		get
		{
			if (TryGetValues(headerName, out var values))
			{
				return values;
			}
			throw new KeyNotFoundException(System.SR.net_http_headers_not_found);
		}
	}

	IEnumerable<string> IReadOnlyDictionary<string, HeaderStringValues>.Keys
	{
		get
		{
			using Enumerator enumerator = GetEnumerator();
			while (enumerator.MoveNext())
			{
				yield return enumerator.Current.Key;
			}
		}
	}

	IEnumerable<HeaderStringValues> IReadOnlyDictionary<string, HeaderStringValues>.Values
	{
		get
		{
			using Enumerator enumerator = GetEnumerator();
			while (enumerator.MoveNext())
			{
				yield return enumerator.Current.Value;
			}
		}
	}

	internal HttpHeadersNonValidated(HttpHeaders headers)
	{
		_headers = headers;
	}

	public bool Contains(string headerName)
	{
		HttpHeaders headers = _headers;
		if (headers != null && headers.TryGetHeaderDescriptor(headerName, out var descriptor))
		{
			return headers.Contains(descriptor);
		}
		return false;
	}

	bool IReadOnlyDictionary<string, HeaderStringValues>.ContainsKey(string key)
	{
		return Contains(key);
	}

	public bool TryGetValues(string headerName, out HeaderStringValues values)
	{
		HttpHeaders headers = _headers;
		if (headers != null && headers.TryGetHeaderDescriptor(headerName, out var descriptor) && headers.TryGetHeaderValue(descriptor, out var value))
		{
			HttpHeaders.GetStoreValuesAsStringOrStringArray(descriptor, value, out var singleValue, out var multiValue);
			values = ((singleValue != null) ? new HeaderStringValues(descriptor, singleValue) : new HeaderStringValues(descriptor, multiValue));
			return true;
		}
		values = default(HeaderStringValues);
		return false;
	}

	bool IReadOnlyDictionary<string, HeaderStringValues>.TryGetValue(string key, out HeaderStringValues value)
	{
		return TryGetValues(key, out value);
	}

	public Enumerator GetEnumerator()
	{
		HttpHeaders headers = _headers;
		if (headers != null)
		{
			HeaderEntry[] entriesArray = headers.GetEntriesArray();
			if (entriesArray != null)
			{
				return new Enumerator(entriesArray, headers.Count);
			}
		}
		return default(Enumerator);
	}

	IEnumerator<KeyValuePair<string, HeaderStringValues>> IEnumerable<KeyValuePair<string, HeaderStringValues>>.GetEnumerator()
	{
		return GetEnumerator();
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return GetEnumerator();
	}
}
