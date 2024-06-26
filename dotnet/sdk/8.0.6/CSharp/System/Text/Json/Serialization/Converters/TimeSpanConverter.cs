using System.Buffers.Text;

namespace System.Text.Json.Serialization.Converters;

internal sealed class TimeSpanConverter : JsonPrimitiveConverter<TimeSpan>
{
	public override TimeSpan Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		if (reader.TokenType != JsonTokenType.String)
		{
			ThrowHelper.ThrowInvalidOperationException_ExpectedString(reader.TokenType);
		}
		return ReadCore(ref reader);
	}

	internal override TimeSpan ReadAsPropertyNameCore(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		return ReadCore(ref reader);
	}

	private static TimeSpan ReadCore(ref Utf8JsonReader reader)
	{
		if (!JsonHelpers.IsInRangeInclusive(reader.ValueLength, 8, 156))
		{
			ThrowHelper.ThrowFormatException(DataType.TimeSpan);
		}
		ReadOnlySpan<byte> source;
		if (!reader.HasValueSequence && !reader.ValueIsEscaped)
		{
			source = reader.ValueSpan;
		}
		else
		{
			Span<byte> utf8Destination = stackalloc byte[156];
			source = utf8Destination[..reader.CopyString(utf8Destination)];
		}
		byte b = source[0];
		if (!JsonHelpers.IsDigit(b) && b != 45)
		{
			ThrowHelper.ThrowFormatException(DataType.TimeSpan);
		}
		if (!Utf8Parser.TryParse(source, out TimeSpan value, out int bytesConsumed, 'c') || source.Length != bytesConsumed)
		{
			ThrowHelper.ThrowFormatException(DataType.TimeSpan);
		}
		return value;
	}

	public override void Write(Utf8JsonWriter writer, TimeSpan value, JsonSerializerOptions options)
	{
		Span<byte> destination = stackalloc byte[26];
		int bytesWritten;
		bool flag = Utf8Formatter.TryFormat(value, destination, out bytesWritten, 'c');
		writer.WriteStringValue(destination.Slice(0, bytesWritten));
	}

	internal override void WriteAsPropertyNameCore(Utf8JsonWriter writer, TimeSpan value, JsonSerializerOptions options, bool isWritingExtensionDataProperty)
	{
		Span<byte> destination = stackalloc byte[26];
		int bytesWritten;
		bool flag = Utf8Formatter.TryFormat(value, destination, out bytesWritten, 'c');
		writer.WritePropertyName(destination.Slice(0, bytesWritten));
	}
}
