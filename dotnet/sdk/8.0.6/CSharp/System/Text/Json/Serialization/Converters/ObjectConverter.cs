namespace System.Text.Json.Serialization.Converters;

internal abstract class ObjectConverter : JsonConverter<object>
{
	private protected override ConverterStrategy GetDefaultConverterStrategy()
	{
		return ConverterStrategy.Object;
	}

	public ObjectConverter()
	{
		base.CanBePolymorphic = true;
	}

	public sealed override object ReadAsPropertyName(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		ThrowHelper.ThrowNotSupportedException_DictionaryKeyTypeNotSupported(Type, this);
		return null;
	}

	internal sealed override object ReadAsPropertyNameCore(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		ThrowHelper.ThrowNotSupportedException_DictionaryKeyTypeNotSupported(Type, this);
		return null;
	}

	public sealed override void Write(Utf8JsonWriter writer, object value, JsonSerializerOptions options)
	{
		if (value == null)
		{
			writer.WriteNullValue();
			return;
		}
		writer.WriteStartObject();
		writer.WriteEndObject();
	}

	public sealed override void WriteAsPropertyName(Utf8JsonWriter writer, object value, JsonSerializerOptions options)
	{
		WriteAsPropertyNameCore(writer, value, options, isWritingExtensionDataProperty: false);
	}

	internal sealed override void WriteAsPropertyNameCore(Utf8JsonWriter writer, object value, JsonSerializerOptions options, bool isWritingExtensionDataProperty)
	{
		if (value == null)
		{
			ThrowHelper.ThrowArgumentNullException("value");
		}
		Type type = value.GetType();
		if (type == Type)
		{
			ThrowHelper.ThrowNotSupportedException_DictionaryKeyTypeNotSupported(type, this);
		}
		JsonConverter converterInternal = options.GetConverterInternal(type);
		converterInternal.WriteAsPropertyNameCoreAsObject(writer, value, options, isWritingExtensionDataProperty);
	}
}
