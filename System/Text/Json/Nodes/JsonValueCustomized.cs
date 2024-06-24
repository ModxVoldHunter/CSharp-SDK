using System.Text.Json.Serialization.Metadata;

namespace System.Text.Json.Nodes;

internal sealed class JsonValueCustomized<TValue> : JsonValue<TValue>
{
	private readonly JsonTypeInfo<TValue> _jsonTypeInfo;

	public JsonValueCustomized(TValue value, JsonTypeInfo<TValue> jsonTypeInfo, JsonNodeOptions? options = null)
		: base(value, options)
	{
		_jsonTypeInfo = jsonTypeInfo;
	}

	public override void WriteTo(Utf8JsonWriter writer, JsonSerializerOptions options = null)
	{
		if (writer == null)
		{
			ThrowHelper.ThrowArgumentNullException("writer");
		}
		JsonTypeInfo<TValue> jsonTypeInfo = _jsonTypeInfo;
		if (options != null && options != jsonTypeInfo.Options)
		{
			options.MakeReadOnly();
			jsonTypeInfo = (JsonTypeInfo<TValue>)options.GetTypeInfoInternal(typeof(TValue), ensureConfigured: true, true);
		}
		jsonTypeInfo.Serialize(writer, in Value);
	}

	internal override JsonNode DeepCloneCore()
	{
		return JsonSerializer.SerializeToNode(Value, _jsonTypeInfo);
	}
}
