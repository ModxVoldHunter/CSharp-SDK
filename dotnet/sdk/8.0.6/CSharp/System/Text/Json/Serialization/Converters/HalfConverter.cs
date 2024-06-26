using System.Buffers;
using System.Globalization;

namespace System.Text.Json.Serialization.Converters;

internal sealed class HalfConverter : JsonPrimitiveConverter<Half>
{
	public HalfConverter()
	{
		base.IsInternalConverterForNumberType = true;
	}

	public override Half Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		if (reader.TokenType != JsonTokenType.Number)
		{
			ThrowHelper.ThrowInvalidOperationException_ExpectedNumber(reader.TokenType);
		}
		return ReadCore(ref reader);
	}

	public override void Write(Utf8JsonWriter writer, Half value, JsonSerializerOptions options)
	{
		WriteCore(writer, value);
	}

	private static Half ReadCore(ref Utf8JsonReader reader)
	{
		byte[] array = null;
		int valueLength = reader.ValueLength;
		Span<byte> span = ((valueLength > 256) ? ((Span<byte>)(array = ArrayPool<byte>.Shared.Rent(valueLength))) : stackalloc byte[256]);
		Span<byte> span2 = span;
		span2 = span2[..reader.CopyValue(span2)];
		Half result;
		bool flag = TryParse(span2, out result);
		if (array != null)
		{
			ArrayPool<byte>.Shared.Return(array);
		}
		if (!flag)
		{
			ThrowHelper.ThrowFormatException(NumericType.Half);
		}
		return result;
	}

	private static void WriteCore(Utf8JsonWriter writer, Half value)
	{
		Span<byte> destination = stackalloc byte[20];
		Format(destination, value, out var written);
		writer.WriteRawValue(destination.Slice(0, written));
	}

	internal override Half ReadAsPropertyNameCore(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		return ReadCore(ref reader);
	}

	internal override void WriteAsPropertyNameCore(Utf8JsonWriter writer, Half value, JsonSerializerOptions options, bool isWritingExtensionDataProperty)
	{
		Span<byte> destination = stackalloc byte[20];
		Format(destination, value, out var written);
		writer.WritePropertyName(destination.Slice(0, written));
	}

	internal override Half ReadNumberWithCustomHandling(ref Utf8JsonReader reader, JsonNumberHandling handling, JsonSerializerOptions options)
	{
		if (reader.TokenType == JsonTokenType.String)
		{
			if ((JsonNumberHandling.AllowReadingFromString & handling) != 0)
			{
				if (TryGetFloatingPointConstant(ref reader, out var value))
				{
					return value;
				}
				return ReadCore(ref reader);
			}
			if ((JsonNumberHandling.AllowNamedFloatingPointLiterals & handling) != 0)
			{
				if (!TryGetFloatingPointConstant(ref reader, out var value2))
				{
					ThrowHelper.ThrowFormatException(NumericType.Half);
				}
				return value2;
			}
		}
		return Read(ref reader, Type, options);
	}

	internal override void WriteNumberWithCustomHandling(Utf8JsonWriter writer, Half value, JsonNumberHandling handling)
	{
		if ((JsonNumberHandling.WriteAsString & handling) != 0)
		{
			Span<byte> span = stackalloc byte[22];
			span[0] = 34;
			Format(span.Slice(1), value, out var written);
			int num = written + 2;
			span[num - 1] = 34;
			writer.WriteRawValue(span.Slice(0, num));
		}
		else if ((JsonNumberHandling.AllowNamedFloatingPointLiterals & handling) != 0)
		{
			WriteFloatingPointConstant(writer, value);
		}
		else
		{
			WriteCore(writer, value);
		}
	}

	private static bool TryGetFloatingPointConstant(ref Utf8JsonReader reader, out Half value)
	{
		Span<byte> utf8Destination = stackalloc byte[20];
		return JsonReaderHelper.TryGetFloatingPointConstant((ReadOnlySpan<byte>)utf8Destination[..reader.CopyValue(utf8Destination)], out value);
	}

	private static void WriteFloatingPointConstant(Utf8JsonWriter writer, Half value)
	{
		if (Half.IsNaN(value))
		{
			writer.WriteNumberValueAsStringUnescaped(JsonConstants.NaNValue);
		}
		else if (Half.IsPositiveInfinity(value))
		{
			writer.WriteNumberValueAsStringUnescaped(JsonConstants.PositiveInfinityValue);
		}
		else if (Half.IsNegativeInfinity(value))
		{
			writer.WriteNumberValueAsStringUnescaped(JsonConstants.NegativeInfinityValue);
		}
		else
		{
			WriteCore(writer, value);
		}
	}

	private static bool TryParse(ReadOnlySpan<byte> buffer, out Half result)
	{
		if (Half.TryParse(buffer, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out result) && (!Half.IsNaN(result) || buffer.SequenceEqual(JsonConstants.NaNValue)) && (!Half.IsPositiveInfinity(result) || buffer.SequenceEqual(JsonConstants.PositiveInfinityValue)))
		{
			if (Half.IsNegativeInfinity(result))
			{
				return buffer.SequenceEqual(JsonConstants.NegativeInfinityValue);
			}
			return true;
		}
		return false;
	}

	private static void Format(Span<byte> destination, Half value, out int written)
	{
		IFormatProvider invariantCulture = CultureInfo.InvariantCulture;
		bool flag = value.TryFormat(destination, out written, default(ReadOnlySpan<char>), invariantCulture);
	}
}
