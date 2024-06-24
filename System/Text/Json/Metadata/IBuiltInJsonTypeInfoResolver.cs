namespace System.Text.Json.Serialization.Metadata;

internal interface IBuiltInJsonTypeInfoResolver
{
	bool IsCompatibleWithOptions(JsonSerializerOptions options);
}
