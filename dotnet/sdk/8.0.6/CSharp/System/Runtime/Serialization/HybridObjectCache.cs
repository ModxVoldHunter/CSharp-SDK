using System.Collections.Generic;

namespace System.Runtime.Serialization;

internal sealed class HybridObjectCache
{
	private Dictionary<string, object> _objectDictionary;

	private Dictionary<string, object> _referencedObjectDictionary;

	internal HybridObjectCache()
	{
	}

	internal void Add(string id, object obj)
	{
		if (_objectDictionary == null)
		{
			_objectDictionary = new Dictionary<string, object>();
		}
		if (_objectDictionary.ContainsKey(id))
		{
			throw XmlObjectSerializer.CreateSerializationException(System.SR.Format(System.SR.MultipleIdDefinition, id));
		}
		_objectDictionary.Add(id, obj);
	}

	internal void Remove(string id)
	{
		_objectDictionary?.Remove(id);
	}

	internal object GetObject(string id)
	{
		if (_referencedObjectDictionary == null)
		{
			_referencedObjectDictionary = new Dictionary<string, object>();
		}
		_referencedObjectDictionary.TryAdd(id, null);
		if (_objectDictionary != null)
		{
			_objectDictionary.TryGetValue(id, out var value);
			return value;
		}
		return null;
	}

	internal bool IsObjectReferenced(string id)
	{
		if (_referencedObjectDictionary != null)
		{
			return _referencedObjectDictionary.ContainsKey(id);
		}
		return false;
	}
}
