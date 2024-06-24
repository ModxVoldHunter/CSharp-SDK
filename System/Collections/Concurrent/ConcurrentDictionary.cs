using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Threading;

namespace System.Collections.Concurrent;

[DebuggerTypeProxy(typeof(IDictionaryDebugView<, >))]
[DebuggerDisplay("Count = {Count}")]
public class ConcurrentDictionary<TKey, TValue> : IDictionary<TKey, TValue>, ICollection<KeyValuePair<TKey, TValue>>, IEnumerable<KeyValuePair<TKey, TValue>>, IEnumerable, IDictionary, ICollection, IReadOnlyDictionary<TKey, TValue>, IReadOnlyCollection<KeyValuePair<TKey, TValue>> where TKey : notnull
{
	private sealed class Enumerator : IEnumerator<KeyValuePair<TKey, TValue>>, IEnumerator, IDisposable
	{
		private readonly ConcurrentDictionary<TKey, TValue> _dictionary;

		private VolatileNode[] _buckets;

		private Node _node;

		private int _i;

		private int _state;

		public KeyValuePair<TKey, TValue> Current { get; private set; }

		object IEnumerator.Current => Current;

		public Enumerator(ConcurrentDictionary<TKey, TValue> dictionary)
		{
			_dictionary = dictionary;
			_i = -1;
		}

		public void Reset()
		{
			_buckets = null;
			_node = null;
			Current = default(KeyValuePair<TKey, TValue>);
			_i = -1;
			_state = 0;
		}

		public void Dispose()
		{
		}

		public bool MoveNext()
		{
			switch (_state)
			{
			case 0:
				_buckets = _dictionary._tables._buckets;
				_i = -1;
				goto case 1;
			case 1:
			{
				VolatileNode[] buckets = _buckets;
				int num = ++_i;
				if ((uint)num >= (uint)buckets.Length)
				{
					break;
				}
				_node = buckets[num]._node;
				_state = 2;
				goto case 2;
			}
			case 2:
			{
				Node node = _node;
				if (node != null)
				{
					Current = new KeyValuePair<TKey, TValue>(node._key, node._value);
					_node = node._next;
					return true;
				}
				goto case 1;
			}
			}
			_state = 3;
			return false;
		}
	}

	private struct VolatileNode
	{
		internal volatile Node _node;
	}

	private sealed class Node
	{
		internal readonly TKey _key;

		internal TValue _value;

		internal volatile Node _next;

		internal readonly int _hashcode;

		internal Node(TKey key, TValue value, int hashcode, Node next)
		{
			_key = key;
			_value = value;
			_next = next;
			_hashcode = hashcode;
		}
	}

	private sealed class Tables
	{
		internal readonly IEqualityComparer<TKey> _comparer;

		internal readonly VolatileNode[] _buckets;

		internal readonly ulong _fastModBucketsMultiplier;

		internal readonly object[] _locks;

		internal readonly int[] _countPerLock;

		internal Tables(VolatileNode[] buckets, object[] locks, int[] countPerLock, IEqualityComparer<TKey> comparer)
		{
			_buckets = buckets;
			_locks = locks;
			_countPerLock = countPerLock;
			_comparer = comparer;
			if (IntPtr.Size == 8)
			{
				_fastModBucketsMultiplier = System.Collections.HashHelpers.GetFastModMultiplier((uint)buckets.Length);
			}
		}
	}

	private sealed class DictionaryEnumerator : IDictionaryEnumerator, IEnumerator
	{
		private readonly IEnumerator<KeyValuePair<TKey, TValue>> _enumerator;

		public DictionaryEntry Entry => new DictionaryEntry(_enumerator.Current.Key, _enumerator.Current.Value);

		public object Key => _enumerator.Current.Key;

		public object Value => _enumerator.Current.Value;

		public object Current => Entry;

		internal DictionaryEnumerator(ConcurrentDictionary<TKey, TValue> dictionary)
		{
			_enumerator = dictionary.GetEnumerator();
		}

		public bool MoveNext()
		{
			return _enumerator.MoveNext();
		}

		public void Reset()
		{
			_enumerator.Reset();
		}
	}

	private volatile Tables _tables;

	private int _budget;

	private readonly bool _growLockArray;

	private readonly bool _comparerIsDefaultForClasses;

	public TValue this[TKey key]
	{
		get
		{
			if (!TryGetValue(key, out var value))
			{
				ThrowKeyNotFoundException(key);
			}
			return value;
		}
		set
		{
			if (key == null)
			{
				System.ThrowHelper.ThrowKeyNullException();
			}
			TryAddInternal(_tables, key, null, value, updateIfExists: true, acquireLock: true, out var _);
		}
	}

	public IEqualityComparer<TKey> Comparer
	{
		get
		{
			IEqualityComparer<TKey> comparer = _tables._comparer;
			if (typeof(TKey) == typeof(string))
			{
				IEqualityComparer<string> equalityComparer = (comparer as NonRandomizedStringEqualityComparer)?.GetUnderlyingEqualityComparer();
				if (equalityComparer != null)
				{
					return (IEqualityComparer<TKey>)equalityComparer;
				}
			}
			return comparer ?? EqualityComparer<TKey>.Default;
		}
	}

	public int Count
	{
		get
		{
			int locksAcquired = 0;
			try
			{
				AcquireAllLocks(ref locksAcquired);
				return GetCountNoLocks();
			}
			finally
			{
				ReleaseLocks(locksAcquired);
			}
		}
	}

	public bool IsEmpty
	{
		get
		{
			if (!AreAllBucketsEmpty())
			{
				return false;
			}
			int locksAcquired = 0;
			try
			{
				AcquireAllLocks(ref locksAcquired);
				return AreAllBucketsEmpty();
			}
			finally
			{
				ReleaseLocks(locksAcquired);
			}
		}
	}

	public ICollection<TKey> Keys => GetKeys();

	IEnumerable<TKey> IReadOnlyDictionary<TKey, TValue>.Keys => GetKeys();

	public ICollection<TValue> Values => GetValues();

	IEnumerable<TValue> IReadOnlyDictionary<TKey, TValue>.Values => GetValues();

	bool ICollection<KeyValuePair<TKey, TValue>>.IsReadOnly => false;

	bool IDictionary.IsFixedSize => false;

	bool IDictionary.IsReadOnly => false;

	ICollection IDictionary.Keys => GetKeys();

	ICollection IDictionary.Values => GetValues();

	object? IDictionary.this[object key]
	{
		get
		{
			if (key == null)
			{
				System.ThrowHelper.ThrowKeyNullException();
			}
			if (key is TKey key2 && TryGetValue(key2, out var value))
			{
				return value;
			}
			return null;
		}
		set
		{
			if (key == null)
			{
				System.ThrowHelper.ThrowKeyNullException();
			}
			if (!(key is TKey))
			{
				throw new ArgumentException(System.SR.ConcurrentDictionary_TypeOfKeyIncorrect);
			}
			ThrowIfInvalidObjectValue(value);
			this[(TKey)key] = (TValue)value;
		}
	}

	bool ICollection.IsSynchronized => false;

	object ICollection.SyncRoot
	{
		get
		{
			throw new NotSupportedException(System.SR.ConcurrentCollection_SyncRoot_NotSupported);
		}
	}

	private static int DefaultConcurrencyLevel => Environment.ProcessorCount;

	public ConcurrentDictionary()
		: this(DefaultConcurrencyLevel, 31, growLockArray: true, (IEqualityComparer<TKey>)null)
	{
	}

	public ConcurrentDictionary(int concurrencyLevel, int capacity)
		: this(concurrencyLevel, capacity, growLockArray: false, (IEqualityComparer<TKey>)null)
	{
	}

	public ConcurrentDictionary(IEnumerable<KeyValuePair<TKey, TValue>> collection)
		: this(DefaultConcurrencyLevel, collection, (IEqualityComparer<TKey>?)null)
	{
	}

	public ConcurrentDictionary(IEqualityComparer<TKey>? comparer)
		: this(DefaultConcurrencyLevel, 31, growLockArray: true, comparer)
	{
	}

	public ConcurrentDictionary(IEnumerable<KeyValuePair<TKey, TValue>> collection, IEqualityComparer<TKey>? comparer)
		: this(DefaultConcurrencyLevel, GetCapacityFromCollection(collection), comparer)
	{
		ArgumentNullException.ThrowIfNull(collection, "collection");
		InitializeFromCollection(collection);
	}

	public ConcurrentDictionary(int concurrencyLevel, IEnumerable<KeyValuePair<TKey, TValue>> collection, IEqualityComparer<TKey>? comparer)
		: this(concurrencyLevel, GetCapacityFromCollection(collection), growLockArray: false, comparer)
	{
		ArgumentNullException.ThrowIfNull(collection, "collection");
		InitializeFromCollection(collection);
	}

	public ConcurrentDictionary(int concurrencyLevel, int capacity, IEqualityComparer<TKey>? comparer)
		: this(concurrencyLevel, capacity, growLockArray: false, comparer)
	{
	}

	internal ConcurrentDictionary(int concurrencyLevel, int capacity, bool growLockArray, IEqualityComparer<TKey> comparer)
	{
		if (concurrencyLevel <= 0)
		{
			if (concurrencyLevel != -1)
			{
				throw new ArgumentOutOfRangeException("concurrencyLevel", System.SR.ConcurrentDictionary_ConcurrencyLevelMustBePositiveOrNegativeOne);
			}
			concurrencyLevel = DefaultConcurrencyLevel;
		}
		ArgumentOutOfRangeException.ThrowIfNegative(capacity, "capacity");
		if (capacity < concurrencyLevel)
		{
			capacity = concurrencyLevel;
		}
		capacity = System.Collections.HashHelpers.GetPrime(capacity);
		object[] array = new object[concurrencyLevel];
		array[0] = array;
		for (int i = 1; i < array.Length; i++)
		{
			array[i] = new object();
		}
		int[] countPerLock = new int[array.Length];
		VolatileNode[] array2 = new VolatileNode[capacity];
		if (typeof(TKey).IsValueType)
		{
			if (comparer != null && comparer == EqualityComparer<TKey>.Default)
			{
				comparer = null;
			}
		}
		else
		{
			if (comparer == null)
			{
				comparer = EqualityComparer<TKey>.Default;
			}
			if (typeof(TKey) == typeof(string))
			{
				IEqualityComparer<string> stringComparer = NonRandomizedStringEqualityComparer.GetStringComparer(comparer);
				if (stringComparer != null)
				{
					comparer = (IEqualityComparer<TKey>)stringComparer;
					goto IL_00e1;
				}
			}
			if (comparer == EqualityComparer<TKey>.Default)
			{
				_comparerIsDefaultForClasses = true;
			}
		}
		goto IL_00e1;
		IL_00e1:
		_tables = new Tables(array2, array, countPerLock, comparer);
		_growLockArray = growLockArray;
		_budget = array2.Length / array.Length;
	}

	private static int GetCapacityFromCollection(IEnumerable<KeyValuePair<TKey, TValue>> collection)
	{
		if (!(collection is ICollection<KeyValuePair<TKey, TValue>> collection2))
		{
			if (collection is IReadOnlyCollection<KeyValuePair<TKey, TValue>> readOnlyCollection)
			{
				return Math.Max(31, readOnlyCollection.Count);
			}
			return 31;
		}
		return Math.Max(31, collection2.Count);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private int GetHashCode(IEqualityComparer<TKey> comparer, TKey key)
	{
		if (typeof(TKey).IsValueType)
		{
			return comparer?.GetHashCode(key) ?? key.GetHashCode();
		}
		if (!_comparerIsDefaultForClasses)
		{
			return comparer.GetHashCode(key);
		}
		return key.GetHashCode();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static bool NodeEqualsKey(IEqualityComparer<TKey> comparer, Node node, TKey key)
	{
		if (typeof(TKey).IsValueType)
		{
			return comparer?.Equals(node._key, key) ?? EqualityComparer<TKey>.Default.Equals(node._key, key);
		}
		return comparer.Equals(node._key, key);
	}

	private void InitializeFromCollection(IEnumerable<KeyValuePair<TKey, TValue>> collection)
	{
		foreach (KeyValuePair<TKey, TValue> item in collection)
		{
			if (item.Key == null)
			{
				System.ThrowHelper.ThrowKeyNullException();
			}
			if (!TryAddInternal(_tables, item.Key, null, item.Value, updateIfExists: false, acquireLock: false, out var _))
			{
				throw new ArgumentException(System.SR.ConcurrentDictionary_SourceContainsDuplicateKeys);
			}
		}
		if (_budget == 0)
		{
			Tables tables = _tables;
			_budget = tables._buckets.Length / tables._locks.Length;
		}
	}

	public bool TryAdd(TKey key, TValue value)
	{
		if (key == null)
		{
			System.ThrowHelper.ThrowKeyNullException();
		}
		TValue resultingValue;
		return TryAddInternal(_tables, key, null, value, updateIfExists: false, acquireLock: true, out resultingValue);
	}

	public bool ContainsKey(TKey key)
	{
		TValue value;
		return TryGetValue(key, out value);
	}

	public bool TryRemove(TKey key, [MaybeNullWhen(false)] out TValue value)
	{
		if (key == null)
		{
			System.ThrowHelper.ThrowKeyNullException();
		}
		return TryRemoveInternal(key, out value, matchValue: false, default(TValue));
	}

	public bool TryRemove(KeyValuePair<TKey, TValue> item)
	{
		if (item.Key == null)
		{
			System.ThrowHelper.ThrowArgumentNullException("item", System.SR.ConcurrentDictionary_ItemKeyIsNull);
		}
		TValue value;
		return TryRemoveInternal(item.Key, out value, matchValue: true, item.Value);
	}

	private bool TryRemoveInternal(TKey key, [MaybeNullWhen(false)] out TValue value, bool matchValue, TValue oldValue)
	{
		Tables tables = _tables;
		IEqualityComparer<TKey> comparer = tables._comparer;
		int hashCode = GetHashCode(comparer, key);
		while (true)
		{
			object[] locks = tables._locks;
			uint lockNo;
			ref Node bucketAndLock = ref GetBucketAndLock(tables, hashCode, out lockNo);
			if (tables._countPerLock[lockNo] == 0)
			{
				break;
			}
			lock (locks[lockNo])
			{
				if (tables != _tables)
				{
					tables = _tables;
					if (comparer != tables._comparer)
					{
						comparer = tables._comparer;
						hashCode = GetHashCode(comparer, key);
					}
					continue;
				}
				Node node = null;
				for (Node node2 = bucketAndLock; node2 != null; node2 = node2._next)
				{
					if (hashCode == node2._hashcode && NodeEqualsKey(comparer, node2, key))
					{
						if (matchValue && !EqualityComparer<TValue>.Default.Equals(oldValue, node2._value))
						{
							value = default(TValue);
							return false;
						}
						if (node == null)
						{
							Volatile.Write(ref bucketAndLock, node2._next);
						}
						else
						{
							node._next = node2._next;
						}
						value = node2._value;
						tables._countPerLock[lockNo]--;
						return true;
					}
					node = node2;
				}
			}
			break;
		}
		value = default(TValue);
		return false;
	}

	public bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value)
	{
		if (key == null)
		{
			System.ThrowHelper.ThrowKeyNullException();
		}
		Tables tables = _tables;
		IEqualityComparer<TKey> comparer = tables._comparer;
		if (typeof(TKey).IsValueType && comparer == null)
		{
			int hashCode = key.GetHashCode();
			for (Node node = GetBucket(tables, hashCode); node != null; node = node._next)
			{
				if (hashCode == node._hashcode && EqualityComparer<TKey>.Default.Equals(node._key, key))
				{
					value = node._value;
					return true;
				}
			}
		}
		else
		{
			int hashCode2 = GetHashCode(comparer, key);
			for (Node node2 = GetBucket(tables, hashCode2); node2 != null; node2 = node2._next)
			{
				if (hashCode2 == node2._hashcode && comparer.Equals(node2._key, key))
				{
					value = node2._value;
					return true;
				}
			}
		}
		value = default(TValue);
		return false;
	}

	private static bool TryGetValueInternal(Tables tables, TKey key, int hashcode, [MaybeNullWhen(false)] out TValue value)
	{
		IEqualityComparer<TKey> comparer = tables._comparer;
		if (typeof(TKey).IsValueType && comparer == null)
		{
			for (Node node = GetBucket(tables, hashcode); node != null; node = node._next)
			{
				if (hashcode == node._hashcode && EqualityComparer<TKey>.Default.Equals(node._key, key))
				{
					value = node._value;
					return true;
				}
			}
		}
		else
		{
			for (Node node2 = GetBucket(tables, hashcode); node2 != null; node2 = node2._next)
			{
				if (hashcode == node2._hashcode && comparer.Equals(node2._key, key))
				{
					value = node2._value;
					return true;
				}
			}
		}
		value = default(TValue);
		return false;
	}

	public bool TryUpdate(TKey key, TValue newValue, TValue comparisonValue)
	{
		if (key == null)
		{
			System.ThrowHelper.ThrowKeyNullException();
		}
		return TryUpdateInternal(_tables, key, null, newValue, comparisonValue);
	}

	private bool TryUpdateInternal(Tables tables, TKey key, int? nullableHashcode, TValue newValue, TValue comparisonValue)
	{
		IEqualityComparer<TKey> comparer = tables._comparer;
		int num = nullableHashcode ?? GetHashCode(comparer, key);
		EqualityComparer<TValue> @default = EqualityComparer<TValue>.Default;
		while (true)
		{
			object[] locks = tables._locks;
			uint lockNo;
			ref Node bucketAndLock = ref GetBucketAndLock(tables, num, out lockNo);
			lock (locks[lockNo])
			{
				if (tables != _tables)
				{
					tables = _tables;
					if (comparer != tables._comparer)
					{
						comparer = tables._comparer;
						num = GetHashCode(comparer, key);
					}
					continue;
				}
				Node node = null;
				for (Node node2 = bucketAndLock; node2 != null; node2 = node2._next)
				{
					if (num == node2._hashcode && NodeEqualsKey(comparer, node2, key))
					{
						if (@default.Equals(node2._value, comparisonValue))
						{
							if (!typeof(TValue).IsValueType || ConcurrentDictionaryTypeProps<TValue>.IsWriteAtomic)
							{
								node2._value = newValue;
							}
							else
							{
								Node node3 = new Node(node2._key, newValue, num, node2._next);
								if (node == null)
								{
									Volatile.Write(ref bucketAndLock, node3);
								}
								else
								{
									node._next = node3;
								}
							}
							return true;
						}
						return false;
					}
					node = node2;
				}
				return false;
			}
		}
	}

	public void Clear()
	{
		int locksAcquired = 0;
		try
		{
			AcquireAllLocks(ref locksAcquired);
			if (!AreAllBucketsEmpty())
			{
				Tables tables = _tables;
				Tables tables2 = (_tables = new Tables(new VolatileNode[System.Collections.HashHelpers.GetPrime(31)], tables._locks, new int[tables._countPerLock.Length], tables._comparer));
				_budget = Math.Max(1, tables2._buckets.Length / tables2._locks.Length);
			}
		}
		finally
		{
			ReleaseLocks(locksAcquired);
		}
	}

	void ICollection<KeyValuePair<TKey, TValue>>.CopyTo(KeyValuePair<TKey, TValue>[] array, int index)
	{
		ArgumentNullException.ThrowIfNull(array, "array");
		ArgumentOutOfRangeException.ThrowIfNegative(index, "index");
		int locksAcquired = 0;
		try
		{
			AcquireAllLocks(ref locksAcquired);
			int countNoLocks = GetCountNoLocks();
			if (array.Length - countNoLocks < index)
			{
				throw new ArgumentException(System.SR.ConcurrentDictionary_ArrayNotLargeEnough);
			}
			CopyToPairs(array, index);
		}
		finally
		{
			ReleaseLocks(locksAcquired);
		}
	}

	public KeyValuePair<TKey, TValue>[] ToArray()
	{
		int locksAcquired = 0;
		try
		{
			AcquireAllLocks(ref locksAcquired);
			int countNoLocks = GetCountNoLocks();
			if (countNoLocks == 0)
			{
				return Array.Empty<KeyValuePair<TKey, TValue>>();
			}
			KeyValuePair<TKey, TValue>[] array = new KeyValuePair<TKey, TValue>[countNoLocks];
			CopyToPairs(array, 0);
			return array;
		}
		finally
		{
			ReleaseLocks(locksAcquired);
		}
	}

	private void CopyToPairs(KeyValuePair<TKey, TValue>[] array, int index)
	{
		VolatileNode[] buckets = _tables._buckets;
		for (int i = 0; i < buckets.Length; i++)
		{
			VolatileNode volatileNode = buckets[i];
			for (Node node = volatileNode._node; node != null; node = node._next)
			{
				array[index] = new KeyValuePair<TKey, TValue>(node._key, node._value);
				index++;
			}
		}
	}

	private void CopyToEntries(DictionaryEntry[] array, int index)
	{
		VolatileNode[] buckets = _tables._buckets;
		for (int i = 0; i < buckets.Length; i++)
		{
			VolatileNode volatileNode = buckets[i];
			for (Node node = volatileNode._node; node != null; node = node._next)
			{
				array[index] = new DictionaryEntry(node._key, node._value);
				index++;
			}
		}
	}

	private void CopyToObjects(object[] array, int index)
	{
		VolatileNode[] buckets = _tables._buckets;
		for (int i = 0; i < buckets.Length; i++)
		{
			VolatileNode volatileNode = buckets[i];
			for (Node node = volatileNode._node; node != null; node = node._next)
			{
				array[index] = new KeyValuePair<TKey, TValue>(node._key, node._value);
				index++;
			}
		}
	}

	public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
	{
		return new Enumerator(this);
	}

	private bool TryAddInternal(Tables tables, TKey key, int? nullableHashcode, TValue value, bool updateIfExists, bool acquireLock, out TValue resultingValue)
	{
		IEqualityComparer<TKey> comparer = tables._comparer;
		int num = nullableHashcode ?? GetHashCode(comparer, key);
		bool flag;
		bool flag2;
		while (true)
		{
			object[] locks = tables._locks;
			uint lockNo;
			ref Node bucketAndLock = ref GetBucketAndLock(tables, num, out lockNo);
			flag = false;
			flag2 = false;
			bool lockTaken = false;
			try
			{
				if (acquireLock)
				{
					Monitor.Enter(locks[lockNo], ref lockTaken);
				}
				if (tables != _tables)
				{
					tables = _tables;
					if (comparer != tables._comparer)
					{
						comparer = tables._comparer;
						num = GetHashCode(comparer, key);
					}
					continue;
				}
				uint num2 = 0u;
				Node node = null;
				for (Node node2 = bucketAndLock; node2 != null; node2 = node2._next)
				{
					if (num == node2._hashcode && NodeEqualsKey(comparer, node2, key))
					{
						if (updateIfExists)
						{
							if (!typeof(TValue).IsValueType || ConcurrentDictionaryTypeProps<TValue>.IsWriteAtomic)
							{
								node2._value = value;
							}
							else
							{
								Node node3 = new Node(node2._key, value, num, node2._next);
								if (node == null)
								{
									Volatile.Write(ref bucketAndLock, node3);
								}
								else
								{
									node._next = node3;
								}
							}
							resultingValue = value;
						}
						else
						{
							resultingValue = node2._value;
						}
						return false;
					}
					node = node2;
					if (!typeof(TKey).IsValueType)
					{
						num2++;
					}
				}
				Node value2 = new Node(key, value, num, bucketAndLock);
				Volatile.Write(ref bucketAndLock, value2);
				checked
				{
					tables._countPerLock[lockNo]++;
					if (tables._countPerLock[lockNo] > _budget)
					{
						flag = true;
					}
					if (!typeof(TKey).IsValueType && num2 > 100 && comparer is NonRandomizedStringEqualityComparer)
					{
						flag2 = true;
					}
				}
			}
			finally
			{
				if (lockTaken)
				{
					Monitor.Exit(locks[lockNo]);
				}
			}
			break;
		}
		if (flag || flag2)
		{
			GrowTable(tables, flag, flag2);
		}
		resultingValue = value;
		return true;
	}

	[DoesNotReturn]
	private static void ThrowKeyNotFoundException(TKey key)
	{
		throw new KeyNotFoundException(System.SR.Format(System.SR.Arg_KeyNotFoundWithKey, key.ToString()));
	}

	private int GetCountNoLocks()
	{
		int num = 0;
		int[] countPerLock = _tables._countPerLock;
		foreach (int num2 in countPerLock)
		{
			num = checked(num + num2);
		}
		return num;
	}

	public TValue GetOrAdd(TKey key, Func<TKey, TValue> valueFactory)
	{
		if (key == null)
		{
			System.ThrowHelper.ThrowKeyNullException();
		}
		if (valueFactory == null)
		{
			System.ThrowHelper.ThrowArgumentNullException("valueFactory");
		}
		Tables tables = _tables;
		IEqualityComparer<TKey> comparer = tables._comparer;
		int hashCode = GetHashCode(comparer, key);
		if (!TryGetValueInternal(tables, key, hashCode, out var value))
		{
			TryAddInternal(tables, key, hashCode, valueFactory(key), updateIfExists: false, acquireLock: true, out value);
		}
		return value;
	}

	public TValue GetOrAdd<TArg>(TKey key, Func<TKey, TArg, TValue> valueFactory, TArg factoryArgument)
	{
		if (key == null)
		{
			System.ThrowHelper.ThrowKeyNullException();
		}
		if (valueFactory == null)
		{
			System.ThrowHelper.ThrowArgumentNullException("valueFactory");
		}
		Tables tables = _tables;
		IEqualityComparer<TKey> comparer = tables._comparer;
		int hashCode = GetHashCode(comparer, key);
		if (!TryGetValueInternal(tables, key, hashCode, out var value))
		{
			TryAddInternal(tables, key, hashCode, valueFactory(key, factoryArgument), updateIfExists: false, acquireLock: true, out value);
		}
		return value;
	}

	public TValue GetOrAdd(TKey key, TValue value)
	{
		if (key == null)
		{
			System.ThrowHelper.ThrowKeyNullException();
		}
		Tables tables = _tables;
		IEqualityComparer<TKey> comparer = tables._comparer;
		int hashCode = GetHashCode(comparer, key);
		if (!TryGetValueInternal(tables, key, hashCode, out var value2))
		{
			TryAddInternal(tables, key, hashCode, value, updateIfExists: false, acquireLock: true, out value2);
		}
		return value2;
	}

	public TValue AddOrUpdate<TArg>(TKey key, Func<TKey, TArg, TValue> addValueFactory, Func<TKey, TValue, TArg, TValue> updateValueFactory, TArg factoryArgument)
	{
		if (key == null)
		{
			System.ThrowHelper.ThrowKeyNullException();
		}
		if (addValueFactory == null)
		{
			System.ThrowHelper.ThrowArgumentNullException("addValueFactory");
		}
		if (updateValueFactory == null)
		{
			System.ThrowHelper.ThrowArgumentNullException("updateValueFactory");
		}
		Tables tables = _tables;
		IEqualityComparer<TKey> comparer = tables._comparer;
		int hashCode = GetHashCode(comparer, key);
		TValue resultingValue;
		while (true)
		{
			if (TryGetValueInternal(tables, key, hashCode, out var value))
			{
				TValue val = updateValueFactory(key, value, factoryArgument);
				if (TryUpdateInternal(tables, key, hashCode, val, value))
				{
					return val;
				}
			}
			else if (TryAddInternal(tables, key, hashCode, addValueFactory(key, factoryArgument), updateIfExists: false, acquireLock: true, out resultingValue))
			{
				break;
			}
			if (tables != _tables)
			{
				tables = _tables;
				if (comparer != tables._comparer)
				{
					comparer = tables._comparer;
					hashCode = GetHashCode(comparer, key);
				}
			}
		}
		return resultingValue;
	}

	public TValue AddOrUpdate(TKey key, Func<TKey, TValue> addValueFactory, Func<TKey, TValue, TValue> updateValueFactory)
	{
		if (key == null)
		{
			System.ThrowHelper.ThrowKeyNullException();
		}
		if (addValueFactory == null)
		{
			System.ThrowHelper.ThrowArgumentNullException("addValueFactory");
		}
		if (updateValueFactory == null)
		{
			System.ThrowHelper.ThrowArgumentNullException("updateValueFactory");
		}
		Tables tables = _tables;
		IEqualityComparer<TKey> comparer = tables._comparer;
		int hashCode = GetHashCode(comparer, key);
		TValue resultingValue;
		while (true)
		{
			if (TryGetValueInternal(tables, key, hashCode, out var value))
			{
				TValue val = updateValueFactory(key, value);
				if (TryUpdateInternal(tables, key, hashCode, val, value))
				{
					return val;
				}
			}
			else if (TryAddInternal(tables, key, hashCode, addValueFactory(key), updateIfExists: false, acquireLock: true, out resultingValue))
			{
				break;
			}
			if (tables != _tables)
			{
				tables = _tables;
				if (comparer != tables._comparer)
				{
					comparer = tables._comparer;
					hashCode = GetHashCode(comparer, key);
				}
			}
		}
		return resultingValue;
	}

	public TValue AddOrUpdate(TKey key, TValue addValue, Func<TKey, TValue, TValue> updateValueFactory)
	{
		if (key == null)
		{
			System.ThrowHelper.ThrowKeyNullException();
		}
		if (updateValueFactory == null)
		{
			System.ThrowHelper.ThrowArgumentNullException("updateValueFactory");
		}
		Tables tables = _tables;
		IEqualityComparer<TKey> comparer = tables._comparer;
		int hashCode = GetHashCode(comparer, key);
		TValue resultingValue;
		while (true)
		{
			if (TryGetValueInternal(tables, key, hashCode, out var value))
			{
				TValue val = updateValueFactory(key, value);
				if (TryUpdateInternal(tables, key, hashCode, val, value))
				{
					return val;
				}
			}
			else if (TryAddInternal(tables, key, hashCode, addValue, updateIfExists: false, acquireLock: true, out resultingValue))
			{
				break;
			}
			if (tables != _tables)
			{
				tables = _tables;
				if (comparer != tables._comparer)
				{
					comparer = tables._comparer;
					hashCode = GetHashCode(comparer, key);
				}
			}
		}
		return resultingValue;
	}

	void IDictionary<TKey, TValue>.Add(TKey key, TValue value)
	{
		if (!TryAdd(key, value))
		{
			throw new ArgumentException(System.SR.ConcurrentDictionary_KeyAlreadyExisted);
		}
	}

	bool IDictionary<TKey, TValue>.Remove(TKey key)
	{
		TValue value;
		return TryRemove(key, out value);
	}

	void ICollection<KeyValuePair<TKey, TValue>>.Add(KeyValuePair<TKey, TValue> keyValuePair)
	{
		((IDictionary<TKey, TValue>)this).Add(keyValuePair.Key, keyValuePair.Value);
	}

	bool ICollection<KeyValuePair<TKey, TValue>>.Contains(KeyValuePair<TKey, TValue> keyValuePair)
	{
		if (TryGetValue(keyValuePair.Key, out var value))
		{
			return EqualityComparer<TValue>.Default.Equals(value, keyValuePair.Value);
		}
		return false;
	}

	bool ICollection<KeyValuePair<TKey, TValue>>.Remove(KeyValuePair<TKey, TValue> keyValuePair)
	{
		return TryRemove(keyValuePair);
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return GetEnumerator();
	}

	void IDictionary.Add(object key, object value)
	{
		if (key == null)
		{
			System.ThrowHelper.ThrowKeyNullException();
		}
		if (!(key is TKey))
		{
			throw new ArgumentException(System.SR.ConcurrentDictionary_TypeOfKeyIncorrect);
		}
		ThrowIfInvalidObjectValue(value);
		((IDictionary<TKey, TValue>)this).Add((TKey)key, (TValue)value);
	}

	bool IDictionary.Contains(object key)
	{
		if (key == null)
		{
			System.ThrowHelper.ThrowKeyNullException();
		}
		if (key is TKey key2)
		{
			return ContainsKey(key2);
		}
		return false;
	}

	IDictionaryEnumerator IDictionary.GetEnumerator()
	{
		return new DictionaryEnumerator(this);
	}

	void IDictionary.Remove(object key)
	{
		if (key == null)
		{
			System.ThrowHelper.ThrowKeyNullException();
		}
		if (key is TKey key2)
		{
			TryRemove(key2, out var _);
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static void ThrowIfInvalidObjectValue(object value)
	{
		if (value != null)
		{
			if (!(value is TValue))
			{
				System.ThrowHelper.ThrowValueNullException();
			}
		}
		else if (default(TValue) != null)
		{
			System.ThrowHelper.ThrowValueNullException();
		}
	}

	void ICollection.CopyTo(Array array, int index)
	{
		ArgumentNullException.ThrowIfNull(array, "array");
		ArgumentOutOfRangeException.ThrowIfNegative(index, "index");
		int locksAcquired = 0;
		try
		{
			AcquireAllLocks(ref locksAcquired);
			int countNoLocks = GetCountNoLocks();
			if (array.Length - countNoLocks < index)
			{
				throw new ArgumentException(System.SR.ConcurrentDictionary_ArrayNotLargeEnough);
			}
			if (array is KeyValuePair<TKey, TValue>[] array2)
			{
				CopyToPairs(array2, index);
				return;
			}
			if (array is DictionaryEntry[] array3)
			{
				CopyToEntries(array3, index);
				return;
			}
			if (array is object[] array4)
			{
				CopyToObjects(array4, index);
				return;
			}
			throw new ArgumentException(System.SR.ConcurrentDictionary_ArrayIncorrectType, "array");
		}
		finally
		{
			ReleaseLocks(locksAcquired);
		}
	}

	private bool AreAllBucketsEmpty()
	{
		return !_tables._countPerLock.AsSpan().ContainsAnyExcept(0);
	}

	private void GrowTable(Tables tables, bool resizeDesired, bool forceRehashIfNonRandomized)
	{
		int locksAcquired = 0;
		try
		{
			AcquireFirstLock(ref locksAcquired);
			if (tables != _tables)
			{
				return;
			}
			int num = tables._buckets.Length;
			IEqualityComparer<TKey> equalityComparer = null;
			if (forceRehashIfNonRandomized && tables._comparer is NonRandomizedStringEqualityComparer nonRandomizedStringEqualityComparer)
			{
				equalityComparer = (IEqualityComparer<TKey>)nonRandomizedStringEqualityComparer.GetUnderlyingEqualityComparer();
			}
			if (resizeDesired)
			{
				if (equalityComparer == null && GetCountNoLocks() < tables._buckets.Length / 4)
				{
					_budget = 2 * _budget;
					if (_budget < 0)
					{
						_budget = int.MaxValue;
					}
					return;
				}
				if ((num = tables._buckets.Length * 2) < 0 || (num = System.Collections.HashHelpers.GetPrime(num)) > Array.MaxLength)
				{
					num = Array.MaxLength;
					_budget = int.MaxValue;
				}
			}
			object[] array = tables._locks;
			if (_growLockArray && tables._locks.Length < 1024)
			{
				array = new object[tables._locks.Length * 2];
				Array.Copy(tables._locks, array, tables._locks.Length);
				for (int i = tables._locks.Length; i < array.Length; i++)
				{
					array[i] = new object();
				}
			}
			VolatileNode[] array2 = new VolatileNode[num];
			int[] array3 = new int[array.Length];
			Tables tables2 = new Tables(array2, array, array3, equalityComparer ?? tables._comparer);
			AcquirePostFirstLock(tables, ref locksAcquired);
			VolatileNode[] buckets = tables._buckets;
			for (int j = 0; j < buckets.Length; j++)
			{
				VolatileNode volatileNode = buckets[j];
				Node node = volatileNode._node;
				checked
				{
					while (node != null)
					{
						int hashcode = equalityComparer?.GetHashCode(node._key) ?? node._hashcode;
						Node next = node._next;
						uint lockNo;
						ref Node bucketAndLock = ref GetBucketAndLock(tables2, hashcode, out lockNo);
						bucketAndLock = new Node(node._key, node._value, hashcode, bucketAndLock);
						array3[lockNo]++;
						node = next;
					}
				}
			}
			_budget = Math.Max(1, array2.Length / array.Length);
			_tables = tables2;
		}
		finally
		{
			ReleaseLocks(locksAcquired);
		}
	}

	private void AcquireAllLocks(ref int locksAcquired)
	{
		if (CDSCollectionETWBCLProvider.Log.IsEnabled())
		{
			CDSCollectionETWBCLProvider.Log.ConcurrentDictionary_AcquiringAllLocks(_tables._buckets.Length);
		}
		AcquireFirstLock(ref locksAcquired);
		AcquirePostFirstLock(_tables, ref locksAcquired);
	}

	private void AcquireFirstLock(ref int locksAcquired)
	{
		object[] locks = _tables._locks;
		Monitor.Enter(locks[0]);
		locksAcquired = 1;
	}

	private static void AcquirePostFirstLock(Tables tables, ref int locksAcquired)
	{
		object[] locks = tables._locks;
		for (int i = 1; i < locks.Length; i++)
		{
			Monitor.Enter(locks[i]);
			locksAcquired++;
		}
	}

	private void ReleaseLocks(int locksAcquired)
	{
		object[] locks = _tables._locks;
		for (int i = 0; i < locksAcquired; i++)
		{
			Monitor.Exit(locks[i]);
		}
	}

	private ReadOnlyCollection<TKey> GetKeys()
	{
		int locksAcquired = 0;
		try
		{
			AcquireAllLocks(ref locksAcquired);
			int countNoLocks = GetCountNoLocks();
			if (countNoLocks == 0)
			{
				return ReadOnlyCollection<TKey>.Empty;
			}
			TKey[] array = new TKey[countNoLocks];
			int num = 0;
			VolatileNode[] buckets = _tables._buckets;
			for (int i = 0; i < buckets.Length; i++)
			{
				VolatileNode volatileNode = buckets[i];
				for (Node node = volatileNode._node; node != null; node = node._next)
				{
					array[num] = node._key;
					num++;
				}
			}
			return new ReadOnlyCollection<TKey>(array);
		}
		finally
		{
			ReleaseLocks(locksAcquired);
		}
	}

	private ReadOnlyCollection<TValue> GetValues()
	{
		int locksAcquired = 0;
		try
		{
			AcquireAllLocks(ref locksAcquired);
			int countNoLocks = GetCountNoLocks();
			if (countNoLocks == 0)
			{
				return ReadOnlyCollection<TValue>.Empty;
			}
			TValue[] array = new TValue[countNoLocks];
			int num = 0;
			VolatileNode[] buckets = _tables._buckets;
			for (int i = 0; i < buckets.Length; i++)
			{
				VolatileNode volatileNode = buckets[i];
				for (Node node = volatileNode._node; node != null; node = node._next)
				{
					array[num] = node._value;
					num++;
				}
			}
			return new ReadOnlyCollection<TValue>(array);
		}
		finally
		{
			ReleaseLocks(locksAcquired);
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static Node GetBucket(Tables tables, int hashcode)
	{
		VolatileNode[] buckets = tables._buckets;
		if (IntPtr.Size == 8)
		{
			return buckets[System.Collections.HashHelpers.FastMod((uint)hashcode, (uint)buckets.Length, tables._fastModBucketsMultiplier)]._node;
		}
		return buckets[(uint)hashcode % (uint)buckets.Length]._node;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static ref Node GetBucketAndLock(Tables tables, int hashcode, out uint lockNo)
	{
		VolatileNode[] buckets = tables._buckets;
		uint num = ((IntPtr.Size != 8) ? ((uint)hashcode % (uint)buckets.Length) : System.Collections.HashHelpers.FastMod((uint)hashcode, (uint)buckets.Length, tables._fastModBucketsMultiplier));
		lockNo = num % (uint)tables._locks.Length;
		return ref buckets[num]._node;
	}
}
