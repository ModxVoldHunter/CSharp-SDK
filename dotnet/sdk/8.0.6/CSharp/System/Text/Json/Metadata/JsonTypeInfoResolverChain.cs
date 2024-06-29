using System.Collections.Generic;

namespace System.Text.Json.Serialization.Metadata;

internal class JsonTypeInfoResolverChain : ConfigurationList<IJsonTypeInfoResolver>, IJsonTypeInfoResolver, IBuiltInJsonTypeInfoResolver
{
	public override bool IsReadOnly => true;

	public JsonTypeInfoResolverChain()
		: base((IEnumerable<IJsonTypeInfoResolver>)null)
	{
	}

	protected override void OnCollectionModifying()
	{
		ThrowHelper.ThrowInvalidOperationException_TypeInfoResolverChainImmutable();
	}

	public JsonTypeInfo GetTypeInfo(Type type, JsonSerializerOptions options)
	{
		foreach (IJsonTypeInfoResolver item in _list)
		{
			JsonTypeInfo typeInfo = item.GetTypeInfo(type, options);
			if (typeInfo != null)
			{
				return typeInfo;
			}
		}
		return null;
	}

	internal void AddFlattened(IJsonTypeInfoResolver resolver)
	{
		if (resolver != null && !(resolver is EmptyJsonTypeInfoResolver))
		{
			if (resolver is JsonTypeInfoResolverChain collection)
			{
				_list.AddRange(collection);
			}
			else
			{
				_list.Add(resolver);
			}
		}
	}

	bool IBuiltInJsonTypeInfoResolver.IsCompatibleWithOptions(JsonSerializerOptions options)
	{
		foreach (IJsonTypeInfoResolver item in _list)
		{
			if (!item.IsCompatibleWithOptions(options))
			{
				return false;
			}
		}
		return true;
	}

	public override string ToString()
	{
		StringBuilder stringBuilder = new StringBuilder("[");
		foreach (IJsonTypeInfoResolver item in _list)
		{
			stringBuilder.Append(item);
			stringBuilder.Append(", ");
		}
		if (_list.Count > 0)
		{
			stringBuilder.Length -= 2;
		}
		stringBuilder.Append(']');
		return stringBuilder.ToString();
	}
}
