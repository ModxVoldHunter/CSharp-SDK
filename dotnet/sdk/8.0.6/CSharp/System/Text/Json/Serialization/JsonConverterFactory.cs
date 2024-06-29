namespace System.Text.Json.Serialization;

public abstract class JsonConverterFactory : JsonConverter
{
	internal sealed override Type? KeyType => null;

	internal sealed override Type? ElementType => null;

	public sealed override Type? Type => null;

	private protected override ConverterStrategy GetDefaultConverterStrategy()
	{
		return ConverterStrategy.None;
	}

	public abstract JsonConverter? CreateConverter(Type typeToConvert, JsonSerializerOptions options);

	internal JsonConverter GetConverterInternal(Type typeToConvert, JsonSerializerOptions options)
	{
		JsonConverter jsonConverter = CreateConverter(typeToConvert, options);
		if (jsonConverter != null)
		{
			if (jsonConverter is JsonConverterFactory)
			{
				ThrowHelper.ThrowInvalidOperationException_SerializerConverterFactoryReturnsJsonConverterFactorty(GetType());
			}
		}
		else
		{
			ThrowHelper.ThrowInvalidOperationException_SerializerConverterFactoryReturnsNull(GetType());
		}
		return jsonConverter;
	}

	internal sealed override object ReadAsObject(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		throw new InvalidOperationException();
	}

	internal sealed override bool OnTryReadAsObject(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options, scoped ref ReadStack state, out object value)
	{
		throw new InvalidOperationException();
	}

	internal sealed override bool TryReadAsObject(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options, scoped ref ReadStack state, out object value)
	{
		throw new InvalidOperationException();
	}

	internal sealed override object ReadAsPropertyNameAsObject(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		throw new InvalidOperationException();
	}

	internal sealed override object ReadAsPropertyNameCoreAsObject(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		throw new InvalidOperationException();
	}

	internal sealed override object ReadNumberWithCustomHandlingAsObject(ref Utf8JsonReader reader, JsonNumberHandling handling, JsonSerializerOptions options)
	{
		throw new InvalidOperationException();
	}

	internal sealed override void WriteAsObject(Utf8JsonWriter writer, object value, JsonSerializerOptions options)
	{
		throw new InvalidOperationException();
	}

	internal sealed override bool OnTryWriteAsObject(Utf8JsonWriter writer, object value, JsonSerializerOptions options, ref WriteStack state)
	{
		throw new InvalidOperationException();
	}

	internal sealed override bool TryWriteAsObject(Utf8JsonWriter writer, object value, JsonSerializerOptions options, ref WriteStack state)
	{
		throw new InvalidOperationException();
	}

	internal sealed override void WriteAsPropertyNameAsObject(Utf8JsonWriter writer, object value, JsonSerializerOptions options)
	{
		throw new InvalidOperationException();
	}

	internal sealed override void WriteAsPropertyNameCoreAsObject(Utf8JsonWriter writer, object value, JsonSerializerOptions options, bool isWritingExtensionDataProperty)
	{
		throw new InvalidOperationException();
	}

	internal sealed override void WriteNumberWithCustomHandlingAsObject(Utf8JsonWriter writer, object value, JsonNumberHandling handling)
	{
		throw new InvalidOperationException();
	}
}
