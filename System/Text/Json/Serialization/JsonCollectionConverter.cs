using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization.Metadata;

namespace System.Text.Json.Serialization;

internal abstract class JsonCollectionConverter<TCollection, TElement> : JsonResumableConverter<TCollection>
{
	internal override bool SupportsCreateObjectDelegate => true;

	internal override Type ElementType => typeof(TElement);

	private protected sealed override ConverterStrategy GetDefaultConverterStrategy()
	{
		return ConverterStrategy.Enumerable;
	}

	protected abstract void Add(in TElement value, ref ReadStack state);

	protected virtual void CreateCollection(ref Utf8JsonReader reader, scoped ref ReadStack state, JsonSerializerOptions options)
	{
		JsonPropertyInfo parentProperty = state.ParentProperty;
		if (parentProperty != null && parentProperty.TryGetPrePopulatedValue(ref state))
		{
			return;
		}
		JsonTypeInfo jsonTypeInfo = state.Current.JsonTypeInfo;
		if (jsonTypeInfo.CreateObject == null)
		{
			if (Type.IsAbstract || Type.IsInterface)
			{
				ThrowHelper.ThrowNotSupportedException_CannotPopulateCollection(Type, ref reader, ref state);
			}
			else
			{
				ThrowHelper.ThrowNotSupportedException_DeserializeNoConstructor(Type, ref reader, ref state);
			}
		}
		state.Current.ReturnValue = jsonTypeInfo.CreateObject();
	}

	protected virtual void ConvertCollection(ref ReadStack state, JsonSerializerOptions options)
	{
	}

	protected static JsonConverter<TElement> GetElementConverter(JsonTypeInfo elementTypeInfo)
	{
		return ((JsonTypeInfo<TElement>)elementTypeInfo).EffectiveConverter;
	}

	protected static JsonConverter<TElement> GetElementConverter(ref WriteStack state)
	{
		return (JsonConverter<TElement>)state.Current.JsonPropertyInfo.EffectiveConverter;
	}

	internal override bool OnTryRead(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options, scoped ref ReadStack state, [MaybeNullWhen(false)] out TCollection value)
	{
		JsonTypeInfo elementTypeInfo = state.Current.JsonTypeInfo.ElementTypeInfo;
		bool isPopulatedValue;
		if (!state.SupportContinuation && !state.Current.CanContainMetadata)
		{
			if (reader.TokenType != JsonTokenType.StartArray)
			{
				ThrowHelper.ThrowJsonException_DeserializeUnableToConvertValue(Type);
			}
			CreateCollection(ref reader, ref state, options);
			state.Current.JsonPropertyInfo = elementTypeInfo.PropertyInfoForTypeInfo;
			JsonConverter<TElement> elementConverter = GetElementConverter(elementTypeInfo);
			if (elementConverter.CanUseDirectReadOrWrite && !state.Current.NumberHandling.HasValue)
			{
				while (true)
				{
					reader.ReadWithVerify();
					if (reader.TokenType == JsonTokenType.EndArray)
					{
						break;
					}
					TElement value2 = elementConverter.Read(ref reader, elementConverter.Type, options);
					Add(in value2, ref state);
				}
			}
			else
			{
				while (true)
				{
					reader.ReadWithVerify();
					if (reader.TokenType == JsonTokenType.EndArray)
					{
						break;
					}
					elementConverter.TryRead(ref reader, typeof(TElement), options, ref state, out var value3, out isPopulatedValue);
					Add(in value3, ref state);
				}
			}
		}
		else
		{
			JsonTypeInfo jsonTypeInfo = state.Current.JsonTypeInfo;
			if (state.Current.ObjectState == StackFrameObjectState.None)
			{
				if (reader.TokenType == JsonTokenType.StartArray)
				{
					state.Current.ObjectState = StackFrameObjectState.ReadMetadata;
				}
				else if (state.Current.CanContainMetadata)
				{
					if (reader.TokenType != JsonTokenType.StartObject)
					{
						ThrowHelper.ThrowJsonException_DeserializeUnableToConvertValue(Type);
					}
					state.Current.ObjectState = StackFrameObjectState.StartToken;
				}
				else
				{
					ThrowHelper.ThrowJsonException_DeserializeUnableToConvertValue(Type);
				}
			}
			if (state.Current.CanContainMetadata && (int)state.Current.ObjectState < 2)
			{
				if (!JsonSerializer.TryReadMetadata(this, jsonTypeInfo, ref reader, ref state))
				{
					value = default(TCollection);
					return false;
				}
				if (state.Current.MetadataPropertyNames == MetadataPropertyName.Ref)
				{
					value = JsonSerializer.ResolveReferenceId<TCollection>(ref state);
					return true;
				}
				state.Current.ObjectState = StackFrameObjectState.ReadMetadata;
			}
			if ((state.Current.MetadataPropertyNames & MetadataPropertyName.Type) != 0 && state.Current.PolymorphicSerializationState != PolymorphicSerializationState.PolymorphicReEntryStarted)
			{
				JsonConverter jsonConverter = ResolvePolymorphicConverter(jsonTypeInfo, ref state);
				if (jsonConverter != null)
				{
					object value4;
					bool flag = jsonConverter.OnTryReadAsObject(ref reader, jsonConverter.Type, options, ref state, out value4);
					value = (TCollection)value4;
					state.ExitPolymorphicConverter(flag);
					return flag;
				}
			}
			if ((int)state.Current.ObjectState < 4)
			{
				if (state.Current.CanContainMetadata)
				{
					JsonSerializer.ValidateMetadataForArrayConverter(this, ref reader, ref state);
				}
				CreateCollection(ref reader, ref state, options);
				if ((state.Current.MetadataPropertyNames & MetadataPropertyName.Id) != 0)
				{
					state.ReferenceResolver.AddReference(state.ReferenceId, state.Current.ReturnValue);
					state.ReferenceId = null;
				}
				state.Current.ObjectState = StackFrameObjectState.CreatedObject;
			}
			if ((int)state.Current.ObjectState < 5)
			{
				JsonConverter<TElement> elementConverter2 = GetElementConverter(elementTypeInfo);
				state.Current.JsonPropertyInfo = elementTypeInfo.PropertyInfoForTypeInfo;
				while (true)
				{
					if ((int)state.Current.PropertyState < 3)
					{
						state.Current.PropertyState = StackFramePropertyState.ReadValue;
						if (!JsonConverter.SingleValueReadWithReadAhead(elementConverter2.RequiresReadAhead, ref reader, ref state))
						{
							value = default(TCollection);
							return false;
						}
					}
					if ((int)state.Current.PropertyState < 4)
					{
						if (reader.TokenType == JsonTokenType.EndArray)
						{
							break;
						}
						state.Current.PropertyState = StackFramePropertyState.ReadValueIsEnd;
					}
					if ((int)state.Current.PropertyState < 5)
					{
						if (!elementConverter2.TryRead(ref reader, typeof(TElement), options, ref state, out var value5, out isPopulatedValue))
						{
							value = default(TCollection);
							return false;
						}
						Add(in value5, ref state);
						state.Current.EndElement();
					}
				}
				state.Current.ObjectState = StackFrameObjectState.ReadElements;
			}
			if ((int)state.Current.ObjectState < 6)
			{
				state.Current.ObjectState = StackFrameObjectState.EndToken;
				if ((state.Current.MetadataPropertyNames & MetadataPropertyName.Values) != 0 && !reader.Read())
				{
					value = default(TCollection);
					return false;
				}
			}
			if ((int)state.Current.ObjectState < 7 && (state.Current.MetadataPropertyNames & MetadataPropertyName.Values) != 0 && reader.TokenType != JsonTokenType.EndObject)
			{
				ThrowHelper.ThrowJsonException_MetadataInvalidPropertyInArrayMetadata(ref state, typeToConvert, in reader);
			}
		}
		ConvertCollection(ref state, options);
		value = (TCollection)state.Current.ReturnValue;
		return true;
	}

	internal override bool OnTryWrite(Utf8JsonWriter writer, TCollection value, JsonSerializerOptions options, ref WriteStack state)
	{
		bool flag;
		if (value == null)
		{
			writer.WriteNullValue();
			flag = true;
		}
		else
		{
			if (!state.Current.ProcessedStartToken)
			{
				state.Current.ProcessedStartToken = true;
				if (state.CurrentContainsMetadata && CanHaveMetadata)
				{
					state.Current.MetadataPropertyName = JsonSerializer.WriteMetadataForCollection(this, ref state, writer);
				}
				writer.WriteStartArray();
				state.Current.JsonPropertyInfo = state.Current.JsonTypeInfo.ElementTypeInfo.PropertyInfoForTypeInfo;
			}
			flag = OnWriteResume(writer, value, options, ref state);
			if (flag && !state.Current.ProcessedEndToken)
			{
				state.Current.ProcessedEndToken = true;
				writer.WriteEndArray();
				if (state.Current.MetadataPropertyName != 0)
				{
					writer.WriteEndObject();
				}
			}
		}
		return flag;
	}

	protected abstract bool OnWriteResume(Utf8JsonWriter writer, TCollection value, JsonSerializerOptions options, ref WriteStack state);
}
