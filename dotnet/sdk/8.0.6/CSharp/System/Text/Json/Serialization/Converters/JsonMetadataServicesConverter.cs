using System.Text.Json.Serialization.Metadata;

namespace System.Text.Json.Serialization.Converters;

internal sealed class JsonMetadataServicesConverter<T> : JsonResumableConverter<T>
{
	internal JsonConverter<T> Converter { get; }

	internal override Type KeyType => Converter.KeyType;

	internal override Type ElementType => Converter.ElementType;

	public override bool HandleNull { get; }

	internal override bool ConstructorIsParameterized => Converter.ConstructorIsParameterized;

	internal override bool SupportsCreateObjectDelegate => Converter.SupportsCreateObjectDelegate;

	internal override bool CanHaveMetadata => Converter.CanHaveMetadata;

	internal override bool CanPopulate => Converter.CanPopulate;

	public JsonMetadataServicesConverter(JsonConverter<T> converter)
	{
		Converter = converter;
		base.ConverterStrategy = converter.ConverterStrategy;
		base.IsInternalConverter = converter.IsInternalConverter;
		base.IsInternalConverterForNumberType = converter.IsInternalConverterForNumberType;
		base.CanBePolymorphic = converter.CanBePolymorphic;
		base.HandleNullOnRead = converter.HandleNullOnRead;
		base.HandleNullOnWrite = converter.HandleNullOnWrite;
		HandleNull = converter.HandleNullOnWrite;
	}

	internal override bool OnTryRead(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options, scoped ref ReadStack state, out T value)
	{
		return Converter.OnTryRead(ref reader, typeToConvert, options, ref state, out value);
	}

	internal override bool OnTryWrite(Utf8JsonWriter writer, T value, JsonSerializerOptions options, ref WriteStack state)
	{
		JsonTypeInfo jsonTypeInfo = state.Current.JsonTypeInfo;
		if (!state.SupportContinuation && jsonTypeInfo.CanUseSerializeHandler && !JsonHelpers.RequiresSpecialNumberHandlingOnWrite(state.Current.NumberHandling) && !state.CurrentContainsMetadata)
		{
			((JsonTypeInfo<T>)jsonTypeInfo).SerializeHandler(writer, value);
			return true;
		}
		return Converter.OnTryWrite(writer, value, options, ref state);
	}

	internal override void ConfigureJsonTypeInfo(JsonTypeInfo jsonTypeInfo, JsonSerializerOptions options)
	{
		Converter.ConfigureJsonTypeInfo(jsonTypeInfo, options);
	}
}
