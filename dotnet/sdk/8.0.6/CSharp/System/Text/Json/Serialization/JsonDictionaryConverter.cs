using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization.Metadata;

namespace System.Text.Json.Serialization;

internal abstract class JsonDictionaryConverter<TDictionary> : JsonResumableConverter<TDictionary>
{
	internal override bool SupportsCreateObjectDelegate => true;

	private protected sealed override ConverterStrategy GetDefaultConverterStrategy()
	{
		return ConverterStrategy.Dictionary;
	}

	protected internal abstract bool OnWriteResume(Utf8JsonWriter writer, TDictionary dictionary, JsonSerializerOptions options, ref WriteStack state);
}
internal abstract class JsonDictionaryConverter<TDictionary, TKey, TValue> : JsonDictionaryConverter<TDictionary>
{
	protected JsonConverter<TKey> _keyConverter;

	protected JsonConverter<TValue> _valueConverter;

	internal override Type ElementType => typeof(TValue);

	internal override Type KeyType => typeof(TKey);

	protected abstract void Add(TKey key, in TValue value, JsonSerializerOptions options, ref ReadStack state);

	protected virtual void ConvertCollection(ref ReadStack state, JsonSerializerOptions options)
	{
	}

	protected virtual void CreateCollection(ref Utf8JsonReader reader, scoped ref ReadStack state)
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

	protected static JsonConverter<T> GetConverter<T>(JsonTypeInfo typeInfo)
	{
		return ((JsonTypeInfo<T>)typeInfo).EffectiveConverter;
	}

	internal sealed override bool OnTryRead(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options, scoped ref ReadStack state, [MaybeNullWhen(false)] out TDictionary value)
	{
		JsonTypeInfo keyTypeInfo = state.Current.JsonTypeInfo.KeyTypeInfo;
		JsonTypeInfo elementTypeInfo = state.Current.JsonTypeInfo.ElementTypeInfo;
		bool isPopulatedValue;
		if (!state.SupportContinuation && !state.Current.CanContainMetadata)
		{
			if (reader.TokenType != JsonTokenType.StartObject)
			{
				ThrowHelper.ThrowJsonException_DeserializeUnableToConvertValue(Type);
			}
			CreateCollection(ref reader, ref state);
			if (_keyConverter == null)
			{
				_keyConverter = GetConverter<TKey>(keyTypeInfo);
			}
			if (_valueConverter == null)
			{
				_valueConverter = GetConverter<TValue>(elementTypeInfo);
			}
			if (_valueConverter.CanUseDirectReadOrWrite && !state.Current.NumberHandling.HasValue)
			{
				while (true)
				{
					reader.ReadWithVerify();
					if (reader.TokenType == JsonTokenType.EndObject)
					{
						break;
					}
					state.Current.JsonPropertyInfo = keyTypeInfo.PropertyInfoForTypeInfo;
					TKey key = ReadDictionaryKey(_keyConverter, ref reader, ref state, options);
					reader.ReadWithVerify();
					state.Current.JsonPropertyInfo = elementTypeInfo.PropertyInfoForTypeInfo;
					TValue value2 = _valueConverter.Read(ref reader, ElementType, options);
					Add(key, in value2, options, ref state);
				}
			}
			else
			{
				while (true)
				{
					reader.ReadWithVerify();
					if (reader.TokenType == JsonTokenType.EndObject)
					{
						break;
					}
					state.Current.JsonPropertyInfo = keyTypeInfo.PropertyInfoForTypeInfo;
					TKey key2 = ReadDictionaryKey(_keyConverter, ref reader, ref state, options);
					reader.ReadWithVerify();
					state.Current.JsonPropertyInfo = elementTypeInfo.PropertyInfoForTypeInfo;
					_valueConverter.TryRead(ref reader, ElementType, options, ref state, out var value3, out isPopulatedValue);
					Add(key2, in value3, options, ref state);
				}
			}
		}
		else
		{
			JsonTypeInfo jsonTypeInfo = state.Current.JsonTypeInfo;
			if (state.Current.ObjectState == StackFrameObjectState.None)
			{
				if (reader.TokenType != JsonTokenType.StartObject)
				{
					ThrowHelper.ThrowJsonException_DeserializeUnableToConvertValue(Type);
				}
				state.Current.ObjectState = StackFrameObjectState.StartToken;
			}
			if (state.Current.CanContainMetadata && (int)state.Current.ObjectState < 2)
			{
				if (!JsonSerializer.TryReadMetadata(this, jsonTypeInfo, ref reader, ref state))
				{
					value = default(TDictionary);
					return false;
				}
				if (state.Current.MetadataPropertyNames == MetadataPropertyName.Ref)
				{
					value = JsonSerializer.ResolveReferenceId<TDictionary>(ref state);
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
					value = (TDictionary)value4;
					state.ExitPolymorphicConverter(flag);
					return flag;
				}
			}
			if ((int)state.Current.ObjectState < 4)
			{
				if (state.Current.CanContainMetadata)
				{
					JsonSerializer.ValidateMetadataForObjectConverter(ref state);
				}
				CreateCollection(ref reader, ref state);
				if ((state.Current.MetadataPropertyNames & MetadataPropertyName.Id) != 0)
				{
					state.ReferenceResolver.AddReference(state.ReferenceId, state.Current.ReturnValue);
					state.ReferenceId = null;
				}
				state.Current.ObjectState = StackFrameObjectState.CreatedObject;
			}
			if (_keyConverter == null)
			{
				_keyConverter = GetConverter<TKey>(keyTypeInfo);
			}
			if (_valueConverter == null)
			{
				_valueConverter = GetConverter<TValue>(elementTypeInfo);
			}
			while (true)
			{
				if (state.Current.PropertyState == StackFramePropertyState.None)
				{
					state.Current.PropertyState = StackFramePropertyState.ReadName;
					if (!reader.Read())
					{
						value = default(TDictionary);
						return false;
					}
				}
				TKey val;
				if ((int)state.Current.PropertyState < 2)
				{
					if (reader.TokenType == JsonTokenType.EndObject)
					{
						break;
					}
					state.Current.PropertyState = StackFramePropertyState.Name;
					if (state.Current.CanContainMetadata)
					{
						ReadOnlySpan<byte> span = reader.GetSpan();
						if (JsonSerializer.IsMetadataPropertyName(span, state.Current.BaseJsonTypeInfo.PolymorphicTypeResolver))
						{
							ThrowHelper.ThrowUnexpectedMetadataException(span, ref reader, ref state);
						}
					}
					state.Current.JsonPropertyInfo = keyTypeInfo.PropertyInfoForTypeInfo;
					val = ReadDictionaryKey(_keyConverter, ref reader, ref state, options);
				}
				else
				{
					val = (TKey)state.Current.DictionaryKey;
				}
				if ((int)state.Current.PropertyState < 3)
				{
					state.Current.PropertyState = StackFramePropertyState.ReadValue;
					if (!JsonConverter.SingleValueReadWithReadAhead(_valueConverter.RequiresReadAhead, ref reader, ref state))
					{
						state.Current.DictionaryKey = val;
						value = default(TDictionary);
						return false;
					}
				}
				if ((int)state.Current.PropertyState < 5)
				{
					state.Current.JsonPropertyInfo = elementTypeInfo.PropertyInfoForTypeInfo;
					if (!_valueConverter.TryRead(ref reader, typeof(TValue), options, ref state, out var value5, out isPopulatedValue))
					{
						state.Current.DictionaryKey = val;
						value = default(TDictionary);
						return false;
					}
					Add(val, in value5, options, ref state);
					state.Current.EndElement();
				}
			}
		}
		ConvertCollection(ref state, options);
		value = (TDictionary)state.Current.ReturnValue;
		return true;
		static TKey ReadDictionaryKey(JsonConverter<TKey> keyConverter, ref Utf8JsonReader reader, scoped ref ReadStack state, JsonSerializerOptions options)
		{
			string @string = reader.GetString();
			state.Current.JsonPropertyNameAsString = @string;
			if (keyConverter.IsInternalConverter && keyConverter.Type == typeof(string))
			{
				return (TKey)(object)@string;
			}
			return keyConverter.ReadAsPropertyNameCore(ref reader, keyConverter.Type, options);
		}
	}

	internal sealed override bool OnTryWrite(Utf8JsonWriter writer, TDictionary dictionary, JsonSerializerOptions options, ref WriteStack state)
	{
		if (dictionary == null)
		{
			writer.WriteNullValue();
			return true;
		}
		if (!state.Current.ProcessedStartToken)
		{
			state.Current.ProcessedStartToken = true;
			writer.WriteStartObject();
			if (state.CurrentContainsMetadata && CanHaveMetadata)
			{
				JsonSerializer.WriteMetadataForObject(this, ref state, writer);
			}
			state.Current.JsonPropertyInfo = state.Current.JsonTypeInfo.ElementTypeInfo.PropertyInfoForTypeInfo;
		}
		bool flag = OnWriteResume(writer, dictionary, options, ref state);
		if (flag && !state.Current.ProcessedEndToken)
		{
			state.Current.ProcessedEndToken = true;
			writer.WriteEndObject();
		}
		return flag;
	}
}
