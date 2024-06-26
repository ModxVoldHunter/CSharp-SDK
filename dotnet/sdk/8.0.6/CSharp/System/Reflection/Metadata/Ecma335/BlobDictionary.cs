using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Reflection.Internal;
using System.Runtime.InteropServices;

namespace System.Reflection.Metadata.Ecma335;

[DebuggerDisplay("Count = {Count}")]
internal readonly struct BlobDictionary
{
	private readonly Dictionary<int, KeyValuePair<ImmutableArray<byte>, BlobHandle>> _dictionary;

	public int Count => _dictionary.Count;

	private static int GetNextDictionaryKey(int dictionaryKey)
	{
		return dictionaryKey * 747796405 + -1403630843;
	}

	private ref KeyValuePair<ImmutableArray<byte>, BlobHandle> GetValueRefOrAddDefault(ReadOnlySpan<byte> key, out bool exists)
	{
		int num = Hash.GetFNVHashCode(key);
		ref KeyValuePair<ImmutableArray<byte>, BlobHandle> valueRefOrAddDefault;
		while (true)
		{
			valueRefOrAddDefault = ref CollectionsMarshal.GetValueRefOrAddDefault(_dictionary, num, out exists);
			if (!exists || valueRefOrAddDefault.Key.AsSpan().SequenceEqual(key))
			{
				break;
			}
			num = GetNextDictionaryKey(num);
		}
		return ref valueRefOrAddDefault;
	}

	public BlobHandle GetOrAdd(ReadOnlySpan<byte> key, ImmutableArray<byte> immutableKey, BlobHandle value, out bool exists)
	{
		ref KeyValuePair<ImmutableArray<byte>, BlobHandle> valueRefOrAddDefault = ref GetValueRefOrAddDefault(key, out exists);
		if (exists)
		{
			return valueRefOrAddDefault.Value;
		}
		if (immutableKey.IsDefault)
		{
			immutableKey = key.ToImmutableArray();
		}
		valueRefOrAddDefault = new KeyValuePair<ImmutableArray<byte>, BlobHandle>(immutableKey, value);
		return value;
	}

	public BlobDictionary(int capacity = 0)
	{
		_dictionary = new Dictionary<int, KeyValuePair<ImmutableArray<byte>, BlobHandle>>(capacity);
	}

	public Dictionary<int, KeyValuePair<ImmutableArray<byte>, BlobHandle>>.Enumerator GetEnumerator()
	{
		return _dictionary.GetEnumerator();
	}
}
