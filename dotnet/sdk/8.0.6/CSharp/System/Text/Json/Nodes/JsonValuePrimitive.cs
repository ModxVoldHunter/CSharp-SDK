using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace System.Text.Json.Nodes;

internal sealed class JsonValuePrimitive<TValue> : JsonValue<TValue>
{
	private static readonly JsonSerializerOptions s_defaultOptions = new JsonSerializerOptions();

	private readonly JsonConverter<TValue> _converter;

	public JsonValuePrimitive(TValue value, JsonConverter<TValue> converter, JsonNodeOptions? options = null)
		: base(value, options)
	{
		_converter = converter;
	}

	public override void WriteTo(Utf8JsonWriter writer, JsonSerializerOptions options = null)
	{
		if (writer == null)
		{
			ThrowHelper.ThrowArgumentNullException("writer");
		}
		JsonConverter<TValue> converter = _converter;
		if (options == null)
		{
			options = s_defaultOptions;
		}
		if (converter.IsInternalConverterForNumberType)
		{
			converter.WriteNumberWithCustomHandling(writer, Value, options.NumberHandling);
		}
		else
		{
			converter.Write(writer, Value, options);
		}
	}

	internal override JsonNode DeepCloneCore()
	{
		TValue value = Value;
		if (!(value is JsonElement jsonElement))
		{
			return new JsonValuePrimitive<TValue>(Value, _converter, base.Options);
		}
		return new JsonValuePrimitive<JsonElement>(jsonElement.Clone(), JsonMetadataServices.JsonElementConverter, base.Options);
	}
}
