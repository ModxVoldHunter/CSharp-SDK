using System.Text.Json.Nodes;

namespace System.Text.Json.Serialization.Converters;

internal sealed class JsonNodeConverterFactory : JsonConverterFactory
{
	public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
	{
		if (typeof(JsonValue).IsAssignableFrom(typeToConvert))
		{
			return JsonNodeConverter.ValueConverter;
		}
		if (typeof(JsonObject) == typeToConvert)
		{
			return JsonNodeConverter.ObjectConverter;
		}
		if (typeof(JsonArray) == typeToConvert)
		{
			return JsonNodeConverter.ArrayConverter;
		}
		return JsonNodeConverter.Instance;
	}

	public override bool CanConvert(Type typeToConvert)
	{
		return typeof(JsonNode).IsAssignableFrom(typeToConvert);
	}
}
