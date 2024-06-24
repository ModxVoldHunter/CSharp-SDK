using System.Globalization;

namespace System.Text.Json.Serialization.Converters;

internal sealed class DateOnlyConverter : JsonPrimitiveConverter<DateOnly>
{
	public override DateOnly Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		if (reader.TokenType != JsonTokenType.String)
		{
			ThrowHelper.ThrowInvalidOperationException_ExpectedString(reader.TokenType);
		}
		return ReadCore(ref reader);
	}

	internal override DateOnly ReadAsPropertyNameCore(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		return ReadCore(ref reader);
	}

	private static DateOnly ReadCore(ref Utf8JsonReader reader)
	{
		if (!JsonHelpers.IsInRangeInclusive(reader.ValueLength, 10, 60))
		{
			ThrowHelper.ThrowFormatException(DataType.DateOnly);
		}
		ReadOnlySpan<byte> source;
		if (!reader.HasValueSequence && !reader.ValueIsEscaped)
		{
			source = reader.ValueSpan;
		}
		else
		{
			Span<byte> utf8Destination = stackalloc byte[60];
			source = utf8Destination[..reader.CopyString(utf8Destination)];
		}
		if (!JsonHelpers.TryParseAsIso(source, out var value))
		{
			ThrowHelper.ThrowFormatException(DataType.DateOnly);
		}
		return value;
	}

	public override void Write(Utf8JsonWriter writer, DateOnly value, JsonSerializerOptions options)
	{
		Span<byte> span = stackalloc byte[10];
		int bytesWritten;
		bool flag = value.TryFormat(span, out bytesWritten, "O", CultureInfo.InvariantCulture);
		writer.WriteStringValue(span);
	}

	internal override void WriteAsPropertyNameCore(Utf8JsonWriter writer, DateOnly value, JsonSerializerOptions options, bool isWritingExtensionDataProperty)
	{
		Span<byte> span = stackalloc byte[10];
		int bytesWritten;
		bool flag = value.TryFormat(span, out bytesWritten, "O", CultureInfo.InvariantCulture);
		writer.WritePropertyName(span);
	}
}
