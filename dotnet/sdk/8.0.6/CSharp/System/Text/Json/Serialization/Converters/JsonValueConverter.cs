using System.Text.Json.Nodes;
using System.Text.Json.Serialization.Metadata;

namespace System.Text.Json.Serialization.Converters;

internal sealed class JsonValueConverter : JsonConverter<JsonValue>
{
	public override void Write(Utf8JsonWriter writer, JsonValue value, JsonSerializerOptions options)
	{
		if (value == null)
		{
			writer.WriteNullValue();
		}
		else
		{
			value.WriteTo(writer, options);
		}
	}

	public override JsonValue Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		if (reader.TokenType == JsonTokenType.Null)
		{
			return null;
		}
		JsonElement value = JsonElement.ParseValue(ref reader);
		return new JsonValuePrimitive<JsonElement>(value, JsonMetadataServices.JsonElementConverter, options.GetNodeOptions());
	}
}
