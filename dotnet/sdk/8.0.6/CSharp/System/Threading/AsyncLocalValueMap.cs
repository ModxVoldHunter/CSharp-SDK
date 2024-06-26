using System.Collections.Generic;

namespace System.Threading;

internal static class AsyncLocalValueMap
{
	private sealed class EmptyAsyncLocalValueMap : IAsyncLocalValueMap
	{
		public IAsyncLocalValueMap Set(IAsyncLocal key, object value, bool treatNullValueAsNonexistent)
		{
			if (value == null && treatNullValueAsNonexistent)
			{
				return this;
			}
			return new OneElementAsyncLocalValueMap(KeyValuePair.Create(key, value));
		}

		public bool TryGetValue(IAsyncLocal key, out object value)
		{
			value = null;
			return false;
		}
	}

	private sealed class OneElementAsyncLocalValueMap : IAsyncLocalValueMap
	{
		private readonly KeyValuePair<IAsyncLocal, object> _item0;

		public OneElementAsyncLocalValueMap(KeyValuePair<IAsyncLocal, object> item0)
		{
			_item0 = item0;
		}

		public IAsyncLocalValueMap Set(IAsyncLocal key, object value, bool treatNullValueAsNonexistent)
		{
			if (value != null || !treatNullValueAsNonexistent)
			{
				KeyValuePair<IAsyncLocal, object> keyValuePair = KeyValuePair.Create(key, value);
				if (key != _item0.Key)
				{
					return new TwoElementAsyncLocalValueMap(_item0, keyValuePair);
				}
				return new OneElementAsyncLocalValueMap(keyValuePair);
			}
			if (key != _item0.Key)
			{
				return this;
			}
			return Empty;
		}

		public bool TryGetValue(IAsyncLocal key, out object value)
		{
			if (key == _item0.Key)
			{
				value = _item0.Value;
				return true;
			}
			value = null;
			return false;
		}
	}

	private sealed class TwoElementAsyncLocalValueMap : IAsyncLocalValueMap
	{
		private readonly KeyValuePair<IAsyncLocal, object> _item0;

		private readonly KeyValuePair<IAsyncLocal, object> _item1;

		public TwoElementAsyncLocalValueMap(KeyValuePair<IAsyncLocal, object> item0, KeyValuePair<IAsyncLocal, object> item1)
		{
			_item0 = item0;
			_item1 = item1;
		}

		public IAsyncLocalValueMap Set(IAsyncLocal key, object value, bool treatNullValueAsNonexistent)
		{
			if (value != null || !treatNullValueAsNonexistent)
			{
				KeyValuePair<IAsyncLocal, object> keyValuePair = KeyValuePair.Create(key, value);
				if (key != _item0.Key)
				{
					if (key != _item1.Key)
					{
						return new ThreeElementAsyncLocalValueMap(_item0, _item1, keyValuePair);
					}
					return new TwoElementAsyncLocalValueMap(_item0, keyValuePair);
				}
				return new TwoElementAsyncLocalValueMap(keyValuePair, _item1);
			}
			if (key != _item0.Key)
			{
				if (key != _item1.Key)
				{
					return this;
				}
				return new OneElementAsyncLocalValueMap(_item0);
			}
			return new OneElementAsyncLocalValueMap(_item1);
		}

		public bool TryGetValue(IAsyncLocal key, out object value)
		{
			if (key == _item0.Key)
			{
				value = _item0.Value;
				return true;
			}
			if (key == _item1.Key)
			{
				value = _item1.Value;
				return true;
			}
			value = null;
			return false;
		}
	}

	private sealed class ThreeElementAsyncLocalValueMap : IAsyncLocalValueMap
	{
		private readonly KeyValuePair<IAsyncLocal, object> _item0;

		private readonly KeyValuePair<IAsyncLocal, object> _item1;

		private readonly KeyValuePair<IAsyncLocal, object> _item2;

		public ThreeElementAsyncLocalValueMap(KeyValuePair<IAsyncLocal, object> item0, KeyValuePair<IAsyncLocal, object> item1, KeyValuePair<IAsyncLocal, object> item2)
		{
			_item0 = item0;
			_item1 = item1;
			_item2 = item2;
		}

		public IAsyncLocalValueMap Set(IAsyncLocal key, object value, bool treatNullValueAsNonexistent)
		{
			if (value != null || !treatNullValueAsNonexistent)
			{
				KeyValuePair<IAsyncLocal, object> keyValuePair = KeyValuePair.Create(key, value);
				if (key != _item0.Key)
				{
					if (key != _item1.Key)
					{
						if (key != _item2.Key)
						{
							return new FourElementAsyncLocalValueMap(_item0, _item1, _item2, keyValuePair);
						}
						return new ThreeElementAsyncLocalValueMap(_item0, _item1, keyValuePair);
					}
					return new ThreeElementAsyncLocalValueMap(_item0, keyValuePair, _item2);
				}
				return new ThreeElementAsyncLocalValueMap(keyValuePair, _item1, _item2);
			}
			if (key != _item0.Key)
			{
				if (key != _item1.Key)
				{
					if (key != _item2.Key)
					{
						return this;
					}
					return new TwoElementAsyncLocalValueMap(_item0, _item1);
				}
				return new TwoElementAsyncLocalValueMap(_item0, _item2);
			}
			return new TwoElementAsyncLocalValueMap(_item1, _item2);
		}

		public bool TryGetValue(IAsyncLocal key, out object value)
		{
			if (key == _item0.Key)
			{
				value = _item0.Value;
				return true;
			}
			if (key == _item1.Key)
			{
				value = _item1.Value;
				return true;
			}
			if (key == _item2.Key)
			{
				value = _item2.Value;
				return true;
			}
			value = null;
			return false;
		}
	}

	private sealed class FourElementAsyncLocalValueMap : IAsyncLocalValueMap
	{
		private readonly KeyValuePair<IAsyncLocal, object> _item0;

		private readonly KeyValuePair<IAsyncLocal, object> _item1;

		private readonly KeyValuePair<IAsyncLocal, object> _item2;

		private readonly KeyValuePair<IAsyncLocal, object> _item3;

		public FourElementAsyncLocalValueMap(KeyValuePair<IAsyncLocal, object> item0, KeyValuePair<IAsyncLocal, object> item1, KeyValuePair<IAsyncLocal, object> item2, KeyValuePair<IAsyncLocal, object> item3)
		{
			_item0 = item0;
			_item1 = item1;
			_item2 = item2;
			_item3 = item3;
		}

		public IAsyncLocalValueMap Set(IAsyncLocal key, object value, bool treatNullValueAsNonexistent)
		{
			if (value != null || !treatNullValueAsNonexistent)
			{
				KeyValuePair<IAsyncLocal, object> keyValuePair = KeyValuePair.Create(key, value);
				if (key != _item0.Key)
				{
					if (key != _item1.Key)
					{
						if (key != _item2.Key)
						{
							if (key != _item3.Key)
							{
								return new MultiElementAsyncLocalValueMap(new KeyValuePair<IAsyncLocal, object>[5] { _item0, _item1, _item2, _item3, keyValuePair });
							}
							return new FourElementAsyncLocalValueMap(_item0, _item1, _item2, keyValuePair);
						}
						return new FourElementAsyncLocalValueMap(_item0, _item1, keyValuePair, _item3);
					}
					return new FourElementAsyncLocalValueMap(_item0, keyValuePair, _item2, _item3);
				}
				return new FourElementAsyncLocalValueMap(keyValuePair, _item1, _item2, _item3);
			}
			if (key != _item0.Key)
			{
				if (key != _item1.Key)
				{
					if (key != _item2.Key)
					{
						if (key != _item3.Key)
						{
							return this;
						}
						return new ThreeElementAsyncLocalValueMap(_item0, _item1, _item2);
					}
					return new ThreeElementAsyncLocalValueMap(_item0, _item1, _item3);
				}
				return new ThreeElementAsyncLocalValueMap(_item0, _item2, _item3);
			}
			return new ThreeElementAsyncLocalValueMap(_item1, _item2, _item3);
		}

		public bool TryGetValue(IAsyncLocal key, out object value)
		{
			if (key == _item0.Key)
			{
				value = _item0.Value;
				return true;
			}
			if (key == _item1.Key)
			{
				value = _item1.Value;
				return true;
			}
			if (key == _item2.Key)
			{
				value = _item2.Value;
				return true;
			}
			if (key == _item3.Key)
			{
				value = _item3.Value;
				return true;
			}
			value = null;
			return false;
		}
	}

	private sealed class MultiElementAsyncLocalValueMap : IAsyncLocalValueMap
	{
		private readonly KeyValuePair<IAsyncLocal, object>[] _keyValues;

		internal MultiElementAsyncLocalValueMap(KeyValuePair<IAsyncLocal, object>[] keyValues)
		{
			_keyValues = keyValues;
		}

		public IAsyncLocalValueMap Set(IAsyncLocal key, object value, bool treatNullValueAsNonexistent)
		{
			for (int i = 0; i < _keyValues.Length; i++)
			{
				if (key == _keyValues[i].Key)
				{
					if (value != null || !treatNullValueAsNonexistent)
					{
						KeyValuePair<IAsyncLocal, object>[] array = _keyValues.AsSpan().ToArray();
						array[i] = KeyValuePair.Create(key, value);
						return new MultiElementAsyncLocalValueMap(array);
					}
					if (_keyValues.Length == 5)
					{
						return i switch
						{
							0 => new FourElementAsyncLocalValueMap(_keyValues[1], _keyValues[2], _keyValues[3], _keyValues[4]), 
							1 => new FourElementAsyncLocalValueMap(_keyValues[0], _keyValues[2], _keyValues[3], _keyValues[4]), 
							2 => new FourElementAsyncLocalValueMap(_keyValues[0], _keyValues[1], _keyValues[3], _keyValues[4]), 
							3 => new FourElementAsyncLocalValueMap(_keyValues[0], _keyValues[1], _keyValues[2], _keyValues[4]), 
							_ => new FourElementAsyncLocalValueMap(_keyValues[0], _keyValues[1], _keyValues[2], _keyValues[3]), 
						};
					}
					KeyValuePair<IAsyncLocal, object>[] array2 = new KeyValuePair<IAsyncLocal, object>[_keyValues.Length - 1];
					if (i != 0)
					{
						Array.Copy(_keyValues, array2, i);
					}
					if (i != _keyValues.Length - 1)
					{
						Array.Copy(_keyValues, i + 1, array2, i, _keyValues.Length - i - 1);
					}
					return new MultiElementAsyncLocalValueMap(array2);
				}
			}
			if (value == null && treatNullValueAsNonexistent)
			{
				return this;
			}
			if (_keyValues.Length < 16)
			{
				KeyValuePair<IAsyncLocal, object>[] array3 = new KeyValuePair<IAsyncLocal, object>[_keyValues.Length + 1];
				Array.Copy(_keyValues, array3, _keyValues.Length);
				array3[_keyValues.Length] = KeyValuePair.Create(key, value);
				return new MultiElementAsyncLocalValueMap(array3);
			}
			ManyElementAsyncLocalValueMap manyElementAsyncLocalValueMap = new ManyElementAsyncLocalValueMap(17);
			KeyValuePair<IAsyncLocal, object>[] keyValues = _keyValues;
			for (int j = 0; j < keyValues.Length; j++)
			{
				KeyValuePair<IAsyncLocal, object> keyValuePair = keyValues[j];
				manyElementAsyncLocalValueMap[keyValuePair.Key] = keyValuePair.Value;
			}
			manyElementAsyncLocalValueMap[key] = value;
			return manyElementAsyncLocalValueMap;
		}

		public bool TryGetValue(IAsyncLocal key, out object value)
		{
			KeyValuePair<IAsyncLocal, object>[] keyValues = _keyValues;
			for (int i = 0; i < keyValues.Length; i++)
			{
				KeyValuePair<IAsyncLocal, object> keyValuePair = keyValues[i];
				if (key == keyValuePair.Key)
				{
					value = keyValuePair.Value;
					return true;
				}
			}
			value = null;
			return false;
		}
	}

	private sealed class ManyElementAsyncLocalValueMap : Dictionary<IAsyncLocal, object>, IAsyncLocalValueMap
	{
		public ManyElementAsyncLocalValueMap(int capacity)
			: base(capacity)
		{
		}

		public IAsyncLocalValueMap Set(IAsyncLocal key, object value, bool treatNullValueAsNonexistent)
		{
			int count = base.Count;
			bool flag = ContainsKey(key);
			if (value != null || !treatNullValueAsNonexistent)
			{
				ManyElementAsyncLocalValueMap manyElementAsyncLocalValueMap = new ManyElementAsyncLocalValueMap(count + ((!flag) ? 1 : 0));
				using (Enumerator enumerator = GetEnumerator())
				{
					while (enumerator.MoveNext())
					{
						KeyValuePair<IAsyncLocal, object> current = enumerator.Current;
						manyElementAsyncLocalValueMap[current.Key] = current.Value;
					}
				}
				manyElementAsyncLocalValueMap[key] = value;
				return manyElementAsyncLocalValueMap;
			}
			if (flag)
			{
				if (count == 17)
				{
					KeyValuePair<IAsyncLocal, object>[] array = new KeyValuePair<IAsyncLocal, object>[16];
					int num = 0;
					using (Enumerator enumerator2 = GetEnumerator())
					{
						while (enumerator2.MoveNext())
						{
							KeyValuePair<IAsyncLocal, object> current2 = enumerator2.Current;
							if (key != current2.Key)
							{
								array[num++] = current2;
							}
						}
					}
					return new MultiElementAsyncLocalValueMap(array);
				}
				ManyElementAsyncLocalValueMap manyElementAsyncLocalValueMap2 = new ManyElementAsyncLocalValueMap(count - 1);
				using Enumerator enumerator3 = GetEnumerator();
				while (enumerator3.MoveNext())
				{
					KeyValuePair<IAsyncLocal, object> current3 = enumerator3.Current;
					if (key != current3.Key)
					{
						manyElementAsyncLocalValueMap2[current3.Key] = current3.Value;
					}
				}
				return manyElementAsyncLocalValueMap2;
			}
			return this;
		}
	}

	public static IAsyncLocalValueMap Empty { get; } = new EmptyAsyncLocalValueMap();


	public static bool IsEmpty(IAsyncLocalValueMap asyncLocalValueMap)
	{
		return asyncLocalValueMap == Empty;
	}

	public static IAsyncLocalValueMap Create(IAsyncLocal key, object value, bool treatNullValueAsNonexistent)
	{
		if (value == null && treatNullValueAsNonexistent)
		{
			return Empty;
		}
		return new OneElementAsyncLocalValueMap(KeyValuePair.Create(key, value));
	}
}
