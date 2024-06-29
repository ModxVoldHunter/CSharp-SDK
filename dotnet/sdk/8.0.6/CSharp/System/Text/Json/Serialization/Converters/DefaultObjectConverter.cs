using System.Text.Json.Nodes;

namespace System.Text.Json.Serialization.Converters;

internal sealed class DefaultObjectConverter : ObjectConverter
{
	public DefaultObjectConverter()
	{
		base.RequiresReadAhead = true;
	}

	public override object Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		if (options.UnknownTypeHandling == JsonUnknownTypeHandling.JsonElement)
		{
			return JsonElement.ParseValue(ref reader);
		}
		return JsonNodeConverter.Instance.Read(ref reader, typeToConvert, options);
	}

	internal override bool OnTryRead(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options, scoped ref ReadStack state, out object value)
	{
		object referenceValue;
		if (options.UnknownTypeHandling == JsonUnknownTypeHandling.JsonElement)
		{
			JsonElement jsonElement = JsonElement.ParseValue(ref reader);
			if (options.ReferenceHandlingStrategy == ReferenceHandlingStrategy.Preserve && JsonSerializer.TryHandleReferenceFromJsonElement(ref reader, ref state, jsonElement, out referenceValue))
			{
				value = referenceValue;
			}
			else
			{
				value = jsonElement;
			}
			return true;
		}
		JsonNode jsonNode = JsonNodeConverter.Instance.Read(ref reader, typeToConvert, options);
		if (options.ReferenceHandlingStrategy == ReferenceHandlingStrategy.Preserve && JsonSerializer.TryHandleReferenceFromJsonNode(ref reader, ref state, jsonNode, out referenceValue))
		{
			value = referenceValue;
		}
		else
		{
			value = jsonNode;
		}
		return true;
	}
}
