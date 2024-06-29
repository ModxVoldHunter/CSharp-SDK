using System.Text.Json.Serialization.Metadata;

namespace System.Text.Json.Serialization.Converters;

internal sealed class NullableConverter<T> : JsonConverter<T?> where T : struct
{
	private readonly JsonConverter<T> _elementConverter;

	internal override Type ElementType => typeof(T);

	public override bool HandleNull => true;

	internal override bool CanPopulate => _elementConverter.CanPopulate;

	internal override bool ConstructorIsParameterized => _elementConverter.ConstructorIsParameterized;

	public NullableConverter(JsonConverter<T> elementConverter)
	{
		_elementConverter = elementConverter;
		base.IsInternalConverterForNumberType = elementConverter.IsInternalConverterForNumberType;
		base.ConverterStrategy = elementConverter.ConverterStrategy;
		base.ConstructorInfo = elementConverter.ConstructorInfo;
	}

	internal override bool OnTryRead(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options, scoped ref ReadStack state, out T? value)
	{
		if (!state.IsContinuation && reader.TokenType == JsonTokenType.Null)
		{
			value = null;
			return true;
		}
		JsonTypeInfo jsonTypeInfo = state.Current.JsonTypeInfo;
		state.Current.JsonTypeInfo = state.Current.JsonTypeInfo.ElementTypeInfo;
		if (_elementConverter.OnTryRead(ref reader, typeof(T), options, ref state, out var value2))
		{
			value = value2;
			state.Current.JsonTypeInfo = jsonTypeInfo;
			return true;
		}
		state.Current.JsonTypeInfo = jsonTypeInfo;
		value = null;
		return false;
	}

	internal override bool OnTryWrite(Utf8JsonWriter writer, T? value, JsonSerializerOptions options, ref WriteStack state)
	{
		if (!value.HasValue)
		{
			writer.WriteNullValue();
			return true;
		}
		state.Current.JsonPropertyInfo = state.Current.JsonTypeInfo.ElementTypeInfo.PropertyInfoForTypeInfo;
		JsonConverter<T> elementConverter = _elementConverter;
		T value2 = value.Value;
		return elementConverter.TryWrite(writer, in value2, options, ref state);
	}

	public override T? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		if (reader.TokenType == JsonTokenType.Null)
		{
			return null;
		}
		return _elementConverter.Read(ref reader, typeof(T), options);
	}

	public override void Write(Utf8JsonWriter writer, T? value, JsonSerializerOptions options)
	{
		if (!value.HasValue)
		{
			writer.WriteNullValue();
		}
		else
		{
			_elementConverter.Write(writer, value.Value, options);
		}
	}

	internal override T? ReadNumberWithCustomHandling(ref Utf8JsonReader reader, JsonNumberHandling numberHandling, JsonSerializerOptions options)
	{
		if (reader.TokenType == JsonTokenType.Null)
		{
			return null;
		}
		return _elementConverter.ReadNumberWithCustomHandling(ref reader, numberHandling, options);
	}

	internal override void WriteNumberWithCustomHandling(Utf8JsonWriter writer, T? value, JsonNumberHandling handling)
	{
		if (!value.HasValue)
		{
			writer.WriteNullValue();
		}
		else
		{
			_elementConverter.WriteNumberWithCustomHandling(writer, value.Value, handling);
		}
	}
}
