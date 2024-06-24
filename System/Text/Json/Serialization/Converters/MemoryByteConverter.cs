namespace System.Text.Json.Serialization.Converters;

internal sealed class MemoryByteConverter : JsonConverter<Memory<byte>>
{
	public override bool HandleNull => true;

	public override Memory<byte> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		return (reader.TokenType == JsonTokenType.Null) ? null : reader.GetBytesFromBase64();
	}

	public override void Write(Utf8JsonWriter writer, Memory<byte> value, JsonSerializerOptions options)
	{
		writer.WriteBase64StringValue(value.Span);
	}
}
