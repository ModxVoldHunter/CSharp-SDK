namespace System.Text.Json.Serialization.Metadata;

internal sealed class EmptyJsonTypeInfoResolver : IJsonTypeInfoResolver, IBuiltInJsonTypeInfoResolver
{
	public JsonTypeInfo GetTypeInfo(Type type, JsonSerializerOptions options)
	{
		return null;
	}

	public bool IsCompatibleWithOptions(JsonSerializerOptions _)
	{
		return true;
	}
}
