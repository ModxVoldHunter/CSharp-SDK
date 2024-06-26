namespace System.Text.Json.Serialization.Converters;

internal sealed class UriConverter : JsonPrimitiveConverter<Uri>
{
	public override Uri Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		if (reader.TokenType != JsonTokenType.Null)
		{
			return ReadCore(ref reader);
		}
		return null;
	}

	public override void Write(Utf8JsonWriter writer, Uri value, JsonSerializerOptions options)
	{
		if ((object)value == null)
		{
			writer.WriteNullValue();
		}
		else
		{
			writer.WriteStringValue(value.OriginalString);
		}
	}

	internal override Uri ReadAsPropertyNameCore(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		return ReadCore(ref reader);
	}

	private static Uri ReadCore(ref Utf8JsonReader reader)
	{
		string @string = reader.GetString();
		if (!Uri.TryCreate(@string, UriKind.RelativeOrAbsolute, out Uri result))
		{
			ThrowHelper.ThrowJsonException();
		}
		return result;
	}

	internal override void WriteAsPropertyNameCore(Utf8JsonWriter writer, Uri value, JsonSerializerOptions options, bool isWritingExtensionDataProperty)
	{
		if ((object)value == null)
		{
			ThrowHelper.ThrowArgumentNullException("value");
		}
		writer.WritePropertyName(value.OriginalString);
	}
}
