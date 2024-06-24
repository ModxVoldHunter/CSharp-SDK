namespace System.Text.Json.Serialization.Metadata;

public static class JsonTypeInfoResolver
{
	internal static IJsonTypeInfoResolver Empty { get; } = new EmptyJsonTypeInfoResolver();


	public static IJsonTypeInfoResolver Combine(params IJsonTypeInfoResolver?[] resolvers)
	{
		if (resolvers == null)
		{
			ThrowHelper.ThrowArgumentNullException("resolvers");
		}
		JsonTypeInfoResolverChain jsonTypeInfoResolverChain = new JsonTypeInfoResolverChain();
		foreach (IJsonTypeInfoResolver resolver in resolvers)
		{
			jsonTypeInfoResolverChain.AddFlattened(resolver);
		}
		if (jsonTypeInfoResolverChain.Count != 1)
		{
			return jsonTypeInfoResolverChain;
		}
		return jsonTypeInfoResolverChain[0];
	}

	public static IJsonTypeInfoResolver WithAddedModifier(this IJsonTypeInfoResolver resolver, Action<JsonTypeInfo> modifier)
	{
		if (resolver == null)
		{
			ThrowHelper.ThrowArgumentNullException("resolver");
		}
		if (modifier == null)
		{
			ThrowHelper.ThrowArgumentNullException("modifier");
		}
		if (!(resolver is JsonTypeInfoResolverWithAddedModifiers jsonTypeInfoResolverWithAddedModifiers))
		{
			return new JsonTypeInfoResolverWithAddedModifiers(resolver, new Action<JsonTypeInfo>[1] { modifier });
		}
		return jsonTypeInfoResolverWithAddedModifiers.WithAddedModifier(modifier);
	}

	internal static bool IsCompatibleWithOptions(this IJsonTypeInfoResolver resolver, JsonSerializerOptions options)
	{
		if (resolver is IBuiltInJsonTypeInfoResolver builtInJsonTypeInfoResolver)
		{
			return builtInJsonTypeInfoResolver.IsCompatibleWithOptions(options);
		}
		return false;
	}
}
