using System.Diagnostics.CodeAnalysis;

namespace System.Text.Json.Serialization.Converters;

internal abstract class JsonPrimitiveConverter<T> : JsonConverter<T>
{
	public sealed override void WriteAsPropertyName(Utf8JsonWriter writer, [DisallowNull] T value, JsonSerializerOptions options)
	{
		if (value == null)
		{
			ThrowHelper.ThrowArgumentNullException("value");
		}
		WriteAsPropertyNameCore(writer, value, options, isWritingExtensionDataProperty: false);
	}

	public sealed override T ReadAsPropertyName(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		if (reader.TokenType != JsonTokenType.PropertyName)
		{
			ThrowHelper.ThrowInvalidOperationException_ExpectedPropertyName(reader.TokenType);
		}
		return ReadAsPropertyNameCore(ref reader, typeToConvert, options);
	}
}
