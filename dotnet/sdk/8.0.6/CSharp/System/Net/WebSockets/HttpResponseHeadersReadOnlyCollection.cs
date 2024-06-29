using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Net.Http.Headers;

namespace System.Net.WebSockets;

internal sealed class HttpResponseHeadersReadOnlyCollection : IReadOnlyDictionary<string, IEnumerable<string>>, IEnumerable<KeyValuePair<string, IEnumerable<string>>>, IEnumerable, IReadOnlyCollection<KeyValuePair<string, IEnumerable<string>>>
{
	private readonly HttpHeadersNonValidated _headers;

	public IEnumerable<string> this[string key] => _headers[key];

	public IEnumerable<string> Keys
	{
		get
		{
			foreach (KeyValuePair<string, HeaderStringValues> header in _headers)
			{
				yield return header.Key;
			}
		}
	}

	public IEnumerable<IEnumerable<string>> Values
	{
		get
		{
			foreach (KeyValuePair<string, HeaderStringValues> header in _headers)
			{
				yield return header.Value;
			}
		}
	}

	public int Count => _headers.Count;

	public HttpResponseHeadersReadOnlyCollection(HttpResponseHeaders headers)
	{
		_headers = headers.NonValidated;
	}

	public bool ContainsKey(string key)
	{
		return _headers.Contains(key);
	}

	public IEnumerator<KeyValuePair<string, IEnumerable<string>>> GetEnumerator()
	{
		foreach (KeyValuePair<string, HeaderStringValues> header in _headers)
		{
			yield return new KeyValuePair<string, IEnumerable<string>>(header.Key, header.Value);
		}
	}

	public bool TryGetValue(string key, [MaybeNullWhen(false)] out IEnumerable<string> value)
	{
		if (_headers.TryGetValues(key, out var values))
		{
			value = values;
			return true;
		}
		value = null;
		return false;
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return GetEnumerator();
	}
}
