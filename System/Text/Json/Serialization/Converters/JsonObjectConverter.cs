using System.Text.Json.Nodes;
using System.Text.Json.Serialization.Metadata;

namespace System.Text.Json.Serialization.Converters;

internal sealed class JsonObjectConverter : JsonConverter<JsonObject>
{
	internal override void ConfigureJsonTypeInfo(JsonTypeInfo jsonTypeInfo, JsonSerializerOptions options)
	{
		jsonTypeInfo.CreateObjectForExtensionDataProperty = () => new JsonObject(options.GetNodeOptions());
	}

	internal override void ReadElementAndSetProperty(object obj, string propertyName, ref Utf8JsonReader reader, JsonSerializerOptions options, scoped ref ReadStack state)
	{
		JsonNode value;
		bool isPopulatedValue;
		bool flag = JsonNodeConverter.Instance.TryRead(ref reader, typeof(JsonNode), options, ref state, out value, out isPopulatedValue);
		JsonObject jsonObject = (JsonObject)obj;
		JsonNode value2 = value;
		jsonObject[propertyName] = value2;
	}

	public override void Write(Utf8JsonWriter writer, JsonObject value, JsonSerializerOptions options)
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

	public override JsonObject Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		return reader.TokenType switch
		{
			JsonTokenType.StartObject => ReadObject(ref reader, options.GetNodeOptions()), 
			JsonTokenType.Null => null, 
			_ => throw ThrowHelper.GetInvalidOperationException_ExpectedObject(reader.TokenType), 
		};
	}

	public static JsonObject ReadObject(ref Utf8JsonReader reader, JsonNodeOptions? options)
	{
		JsonElement element = JsonElement.ParseValue(ref reader);
		return new JsonObject(element, options);
	}
}
