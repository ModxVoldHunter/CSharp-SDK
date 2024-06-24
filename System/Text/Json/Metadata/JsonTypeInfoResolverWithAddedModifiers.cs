namespace System.Text.Json.Serialization.Metadata;

internal sealed class JsonTypeInfoResolverWithAddedModifiers : IJsonTypeInfoResolver
{
	private readonly IJsonTypeInfoResolver _source;

	private readonly Action<JsonTypeInfo>[] _modifiers;

	public JsonTypeInfoResolverWithAddedModifiers(IJsonTypeInfoResolver source, Action<JsonTypeInfo>[] modifiers)
	{
		_source = source;
		_modifiers = modifiers;
	}

	public JsonTypeInfoResolverWithAddedModifiers WithAddedModifier(Action<JsonTypeInfo> modifier)
	{
		Action<JsonTypeInfo>[] array = new Action<JsonTypeInfo>[_modifiers.Length + 1];
		_modifiers.CopyTo(array, 0);
		array[_modifiers.Length] = modifier;
		return new JsonTypeInfoResolverWithAddedModifiers(_source, array);
	}

	public JsonTypeInfo GetTypeInfo(Type type, JsonSerializerOptions options)
	{
		JsonTypeInfo typeInfo = _source.GetTypeInfo(type, options);
		if (typeInfo != null)
		{
			Action<JsonTypeInfo>[] modifiers = _modifiers;
			foreach (Action<JsonTypeInfo> action in modifiers)
			{
				action(typeInfo);
			}
		}
		return typeInfo;
	}
}
