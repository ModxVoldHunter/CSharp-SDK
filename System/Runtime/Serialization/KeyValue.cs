namespace System.Runtime.Serialization;

[DataContract(Namespace = "http://schemas.microsoft.com/2003/10/Serialization/Arrays")]
internal struct KeyValue<K, V> : IKeyValue
{
	[DataMember(IsRequired = true)]
	public K Key { get; set; }

	[DataMember(IsRequired = true)]
	public V Value { get; set; }

	object IKeyValue.Key
	{
		get
		{
			return Key;
		}
		set
		{
			Key = (K)value;
		}
	}

	object IKeyValue.Value
	{
		get
		{
			return Value;
		}
		set
		{
			Value = (V)value;
		}
	}

	internal KeyValue(K key, V value)
	{
		Key = key;
		Value = value;
	}
}
