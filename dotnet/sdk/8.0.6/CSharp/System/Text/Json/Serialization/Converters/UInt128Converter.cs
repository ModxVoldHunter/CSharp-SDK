using System.Buffers;
using System.Globalization;

namespace System.Text.Json.Serialization.Converters;

internal sealed class UInt128Converter : JsonPrimitiveConverter<UInt128>
{
	public UInt128Converter()
	{
		base.IsInternalConverterForNumberType = true;
	}

	public override UInt128 Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		if (reader.TokenType != JsonTokenType.Number)
		{
			ThrowHelper.ThrowInvalidOperationException_ExpectedNumber(reader.TokenType);
		}
		return ReadCore(ref reader);
	}

	public override void Write(Utf8JsonWriter writer, UInt128 value, JsonSerializerOptions options)
	{
		WriteCore(writer, value);
	}

	private static UInt128 ReadCore(ref Utf8JsonReader reader)
	{
		int valueLength = reader.ValueLength;
		byte[] array = null;
		Span<byte> span = ((valueLength > 256) ? ((Span<byte>)(array = ArrayPool<byte>.Shared.Rent(valueLength))) : stackalloc byte[256]);
		Span<byte> utf8Destination = span;
		if (!TryParse(utf8Destination[..reader.CopyValue(utf8Destination)], out var result))
		{
			ThrowHelper.ThrowFormatException(NumericType.UInt128);
		}
		if (array != null)
		{
			ArrayPool<byte>.Shared.Return(array);
		}
		return result;
	}

	private static void WriteCore(Utf8JsonWriter writer, UInt128 value)
	{
		Span<byte> destination = stackalloc byte[39];
		Format(destination, value, out var written);
		writer.WriteRawValue(destination.Slice(0, written));
	}

	internal override UInt128 ReadAsPropertyNameCore(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		return ReadCore(ref reader);
	}

	internal override void WriteAsPropertyNameCore(Utf8JsonWriter writer, UInt128 value, JsonSerializerOptions options, bool isWritingExtensionDataProperty)
	{
		Span<byte> span = stackalloc byte[39];
		Format(span, value, out var _);
		writer.WritePropertyName(span);
	}

	internal override UInt128 ReadNumberWithCustomHandling(ref Utf8JsonReader reader, JsonNumberHandling handling, JsonSerializerOptions options)
	{
		if (reader.TokenType == JsonTokenType.String && (JsonNumberHandling.AllowReadingFromString & handling) != 0)
		{
			return ReadCore(ref reader);
		}
		return Read(ref reader, Type, options);
	}

	internal override void WriteNumberWithCustomHandling(Utf8JsonWriter writer, UInt128 value, JsonNumberHandling handling)
	{
		if ((JsonNumberHandling.WriteAsString & handling) != 0)
		{
			Span<byte> span = stackalloc byte[41];
			span[0] = 34;
			Format(span.Slice(1), value, out var written);
			int num = written + 2;
			span[num - 1] = 34;
			writer.WriteRawValue(span.Slice(0, num));
		}
		else
		{
			WriteCore(writer, value);
		}
	}

	private static bool TryParse(ReadOnlySpan<byte> buffer, out UInt128 result)
	{
		return UInt128.TryParse(buffer, CultureInfo.InvariantCulture, out result);
	}

	private static void Format(Span<byte> destination, UInt128 value, out int written)
	{
		IFormatProvider invariantCulture = CultureInfo.InvariantCulture;
		bool flag = value.TryFormat(destination, out written, default(ReadOnlySpan<char>), invariantCulture);
	}
}
