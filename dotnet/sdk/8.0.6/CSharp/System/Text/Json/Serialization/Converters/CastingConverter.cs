using System.Diagnostics.CodeAnalysis;

namespace System.Text.Json.Serialization.Converters;

internal sealed class CastingConverter<T> : JsonConverter<T>
{
	private readonly JsonConverter _sourceConverter;

	internal override Type KeyType => _sourceConverter.KeyType;

	internal override Type ElementType => _sourceConverter.ElementType;

	public override bool HandleNull { get; }

	internal override bool SupportsCreateObjectDelegate => _sourceConverter.SupportsCreateObjectDelegate;

	internal override JsonConverter SourceConverterForCastingConverter => _sourceConverter;

	internal CastingConverter(JsonConverter sourceConverter)
	{
		_sourceConverter = sourceConverter;
		base.IsInternalConverter = sourceConverter.IsInternalConverter;
		base.IsInternalConverterForNumberType = sourceConverter.IsInternalConverterForNumberType;
		base.ConverterStrategy = sourceConverter.ConverterStrategy;
		base.CanBePolymorphic = sourceConverter.CanBePolymorphic;
		base.HandleNullOnRead = sourceConverter.HandleNullOnRead;
		base.HandleNullOnWrite = sourceConverter.HandleNullOnWrite;
		HandleNull = sourceConverter.HandleNullOnWrite;
	}

	public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		return JsonSerializer.UnboxOnRead<T>(_sourceConverter.ReadAsObject(ref reader, typeToConvert, options));
	}

	public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
	{
		_sourceConverter.WriteAsObject(writer, value, options);
	}

	internal override bool OnTryRead(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options, scoped ref ReadStack state, out T value)
	{
		object value2;
		bool result = _sourceConverter.OnTryReadAsObject(ref reader, typeToConvert, options, ref state, out value2);
		value = JsonSerializer.UnboxOnRead<T>(value2);
		return result;
	}

	internal override bool OnTryWrite(Utf8JsonWriter writer, T value, JsonSerializerOptions options, ref WriteStack state)
	{
		return _sourceConverter.OnTryWriteAsObject(writer, value, options, ref state);
	}

	public override T ReadAsPropertyName(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		return JsonSerializer.UnboxOnRead<T>(_sourceConverter.ReadAsPropertyNameAsObject(ref reader, typeToConvert, options));
	}

	internal override T ReadAsPropertyNameCore(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		return JsonSerializer.UnboxOnRead<T>(_sourceConverter.ReadAsPropertyNameCoreAsObject(ref reader, typeToConvert, options));
	}

	public override void WriteAsPropertyName(Utf8JsonWriter writer, [DisallowNull] T value, JsonSerializerOptions options)
	{
		_sourceConverter.WriteAsPropertyNameAsObject(writer, value, options);
	}

	internal override void WriteAsPropertyNameCore(Utf8JsonWriter writer, T value, JsonSerializerOptions options, bool isWritingExtensionDataProperty)
	{
		_sourceConverter.WriteAsPropertyNameCoreAsObject(writer, value, options, isWritingExtensionDataProperty);
	}

	internal override T ReadNumberWithCustomHandling(ref Utf8JsonReader reader, JsonNumberHandling handling, JsonSerializerOptions options)
	{
		return JsonSerializer.UnboxOnRead<T>(_sourceConverter.ReadNumberWithCustomHandlingAsObject(ref reader, handling, options));
	}

	internal override void WriteNumberWithCustomHandling(Utf8JsonWriter writer, T value, JsonNumberHandling handling)
	{
		_sourceConverter.WriteNumberWithCustomHandlingAsObject(writer, value, handling);
	}
}
