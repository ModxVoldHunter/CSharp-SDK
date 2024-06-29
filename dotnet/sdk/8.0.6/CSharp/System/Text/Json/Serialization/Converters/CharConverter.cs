using System.Runtime.InteropServices;

namespace System.Text.Json.Serialization.Converters;

internal sealed class CharConverter : JsonPrimitiveConverter<char>
{
	public override char Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		JsonTokenType tokenType = reader.TokenType;
		if ((tokenType != JsonTokenType.PropertyName && tokenType != JsonTokenType.String) || 1 == 0)
		{
			ThrowHelper.ThrowInvalidOperationException_ExpectedString(reader.TokenType);
		}
		if (!JsonHelpers.IsInRangeInclusive(reader.ValueLength, 1, 6))
		{
			ThrowHelper.ThrowInvalidOperationException_ExpectedChar(reader.TokenType);
		}
		Span<char> destination = stackalloc char[6];
		int num = reader.CopyString(destination);
		if (num != 1)
		{
			ThrowHelper.ThrowInvalidOperationException_ExpectedChar(reader.TokenType);
		}
		return destination[0];
	}

	public override void Write(Utf8JsonWriter writer, char value, JsonSerializerOptions options)
	{
		writer.WriteStringValue(MemoryMarshal.CreateSpan(ref value, 1));
	}

	internal override char ReadAsPropertyNameCore(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		return Read(ref reader, typeToConvert, options);
	}

	internal override void WriteAsPropertyNameCore(Utf8JsonWriter writer, char value, JsonSerializerOptions options, bool isWritingExtensionDataProperty)
	{
		writer.WritePropertyName(MemoryMarshal.CreateSpan(ref value, 1));
	}
}
