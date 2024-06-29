using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization.Converters;
using System.Text.Json.Serialization.Metadata;

namespace System.Text.Json.Serialization;

public abstract class JsonConverter
{
	private ConverterStrategy _converterStrategy;

	public abstract Type? Type { get; }

	internal ConverterStrategy ConverterStrategy
	{
		get
		{
			return _converterStrategy;
		}
		init
		{
			CanUseDirectReadOrWrite = value == ConverterStrategy.Value && IsInternalConverter;
			RequiresReadAhead = value == ConverterStrategy.Value;
			_converterStrategy = value;
		}
	}

	internal virtual bool SupportsCreateObjectDelegate => false;

	internal virtual bool CanPopulate => false;

	internal bool CanUseDirectReadOrWrite { get; set; }

	internal virtual bool CanHaveMetadata => false;

	internal bool CanBePolymorphic { get; set; }

	internal bool RequiresReadAhead { get; set; }

	internal bool UsesDefaultHandleNull { get; private protected set; }

	internal bool HandleNullOnRead { get; private protected init; }

	internal bool HandleNullOnWrite { get; private protected init; }

	internal virtual JsonConverter? SourceConverterForCastingConverter => null;

	internal abstract Type? ElementType { get; }

	internal abstract Type? KeyType { get; }

	internal bool IsValueType { get; init; }

	internal bool IsInternalConverter { get; init; }

	internal bool IsInternalConverterForNumberType { get; init; }

	internal virtual bool ConstructorIsParameterized { get; }

	internal ConstructorInfo? ConstructorInfo { get; set; }

	internal JsonConverter()
	{
		IsInternalConverter = GetType().Assembly == typeof(JsonConverter).Assembly;
		ConverterStrategy = GetDefaultConverterStrategy();
	}

	public abstract bool CanConvert(Type typeToConvert);

	private protected abstract ConverterStrategy GetDefaultConverterStrategy();

	internal virtual void ReadElementAndSetProperty(object obj, string propertyName, ref Utf8JsonReader reader, JsonSerializerOptions options, scoped ref ReadStack state)
	{
		throw new InvalidOperationException();
	}

	internal virtual JsonTypeInfo CreateJsonTypeInfo(JsonSerializerOptions options)
	{
		throw new InvalidOperationException();
	}

	internal JsonConverter<TTarget> CreateCastingConverter<TTarget>()
	{
		if (this is JsonConverter<TTarget> result)
		{
			return result;
		}
		JsonSerializerOptions.CheckConverterNullabilityIsSameAsPropertyType(this, typeof(TTarget));
		return SourceConverterForCastingConverter?.CreateCastingConverter<TTarget>() ?? new CastingConverter<TTarget>(this);
	}

	internal static bool ShouldFlush(Utf8JsonWriter writer, ref WriteStack state)
	{
		if (state.FlushThreshold > 0)
		{
			return writer.BytesPending > state.FlushThreshold;
		}
		return false;
	}

	internal abstract object ReadAsObject(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options);

	internal abstract bool OnTryReadAsObject(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options, scoped ref ReadStack state, out object value);

	internal abstract bool TryReadAsObject(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options, scoped ref ReadStack state, out object value);

	internal abstract object ReadAsPropertyNameAsObject(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options);

	internal abstract object ReadAsPropertyNameCoreAsObject(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options);

	internal abstract object ReadNumberWithCustomHandlingAsObject(ref Utf8JsonReader reader, JsonNumberHandling handling, JsonSerializerOptions options);

	internal abstract void WriteAsObject(Utf8JsonWriter writer, object value, JsonSerializerOptions options);

	internal abstract bool OnTryWriteAsObject(Utf8JsonWriter writer, object value, JsonSerializerOptions options, ref WriteStack state);

	internal abstract bool TryWriteAsObject(Utf8JsonWriter writer, object value, JsonSerializerOptions options, ref WriteStack state);

	internal abstract void WriteAsPropertyNameAsObject(Utf8JsonWriter writer, object value, JsonSerializerOptions options);

	internal abstract void WriteAsPropertyNameCoreAsObject(Utf8JsonWriter writer, object value, JsonSerializerOptions options, bool isWritingExtensionDataProperty);

	internal abstract void WriteNumberWithCustomHandlingAsObject(Utf8JsonWriter writer, object value, JsonNumberHandling handling);

	internal virtual void ConfigureJsonTypeInfo(JsonTypeInfo jsonTypeInfo, JsonSerializerOptions options)
	{
	}

	[RequiresUnreferencedCode("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext, or make sure all of the required types are preserved.")]
	[RequiresDynamicCode("JSON serialization and deserialization might require types that cannot be statically analyzed and might need runtime code generation. Use System.Text.Json source generation for native AOT applications.")]
	internal virtual void ConfigureJsonTypeInfoUsingReflection(JsonTypeInfo jsonTypeInfo, JsonSerializerOptions options)
	{
	}

	internal JsonConverter ResolvePolymorphicConverter(JsonTypeInfo jsonTypeInfo, ref ReadStack state)
	{
		JsonConverter jsonConverter = null;
		switch (state.Current.PolymorphicSerializationState)
		{
		case PolymorphicSerializationState.None:
		{
			PolymorphicTypeResolver polymorphicTypeResolver = jsonTypeInfo.PolymorphicTypeResolver;
			if (polymorphicTypeResolver.TryGetDerivedJsonTypeInfo(state.PolymorphicTypeDiscriminator, out var jsonTypeInfo2))
			{
				jsonConverter = state.InitializePolymorphicReEntry(jsonTypeInfo2);
				if (!jsonConverter.CanHaveMetadata)
				{
					ThrowHelper.ThrowNotSupportedException_DerivedConverterDoesNotSupportMetadata(jsonTypeInfo2.Type);
				}
			}
			else
			{
				state.Current.PolymorphicSerializationState = PolymorphicSerializationState.PolymorphicReEntryNotFound;
			}
			state.PolymorphicTypeDiscriminator = null;
			break;
		}
		case PolymorphicSerializationState.PolymorphicReEntrySuspended:
			jsonConverter = state.ResumePolymorphicReEntry();
			break;
		}
		return jsonConverter;
	}

	internal JsonConverter ResolvePolymorphicConverter(object value, JsonTypeInfo jsonTypeInfo, JsonSerializerOptions options, ref WriteStack state)
	{
		JsonConverter jsonConverter = null;
		switch (state.Current.PolymorphicSerializationState)
		{
		case PolymorphicSerializationState.None:
		{
			Type type = value.GetType();
			if (CanBePolymorphic && type != Type)
			{
				jsonTypeInfo = state.Current.InitializePolymorphicReEntry(type, options);
				jsonConverter = jsonTypeInfo.Converter;
			}
			PolymorphicTypeResolver polymorphicTypeResolver = jsonTypeInfo.PolymorphicTypeResolver;
			if (polymorphicTypeResolver != null && polymorphicTypeResolver.TryGetDerivedJsonTypeInfo(type, out var jsonTypeInfo2, out var typeDiscriminator))
			{
				jsonConverter = state.Current.InitializePolymorphicReEntry(jsonTypeInfo2);
				if (typeDiscriminator != null)
				{
					if (!jsonConverter.CanHaveMetadata)
					{
						ThrowHelper.ThrowNotSupportedException_DerivedConverterDoesNotSupportMetadata(jsonTypeInfo2.Type);
					}
					state.PolymorphicTypeDiscriminator = typeDiscriminator;
					state.PolymorphicTypeResolver = polymorphicTypeResolver;
				}
			}
			if (jsonConverter == null)
			{
				state.Current.PolymorphicSerializationState = PolymorphicSerializationState.PolymorphicReEntryNotFound;
			}
			break;
		}
		case PolymorphicSerializationState.PolymorphicReEntrySuspended:
			jsonConverter = state.Current.ResumePolymorphicReEntry();
			break;
		}
		return jsonConverter;
	}

	internal bool TryHandleSerializedObjectReference(Utf8JsonWriter writer, object value, JsonSerializerOptions options, JsonConverter polymorphicConverter, ref WriteStack state)
	{
		switch (options.ReferenceHandlingStrategy)
		{
		case ReferenceHandlingStrategy.IgnoreCycles:
		{
			ReferenceResolver referenceResolver = state.ReferenceResolver;
			if (referenceResolver.ContainsReferenceForCycleDetection(value))
			{
				writer.WriteNullValue();
				return true;
			}
			referenceResolver.PushReferenceForCycleDetection(value);
			state.Current.IsPushedReferenceForCycleDetection = state.CurrentDepth > 0;
			break;
		}
		case ReferenceHandlingStrategy.Preserve:
			if ((polymorphicConverter?.CanHaveMetadata ?? CanHaveMetadata) && JsonSerializer.TryGetReferenceForValue(value, ref state, writer))
			{
				return true;
			}
			break;
		}
		return false;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static bool SingleValueReadWithReadAhead(bool requiresReadAhead, ref Utf8JsonReader reader, scoped ref ReadStack state)
	{
		if (!requiresReadAhead || !state.ReadAhead)
		{
			return reader.Read();
		}
		return DoSingleValueReadWithReadAhead(ref reader);
	}

	internal static bool DoSingleValueReadWithReadAhead(ref Utf8JsonReader reader)
	{
		Utf8JsonReader utf8JsonReader = reader;
		if (!reader.Read())
		{
			return false;
		}
		JsonTokenType tokenType = reader.TokenType;
		if ((tokenType == JsonTokenType.StartObject || tokenType == JsonTokenType.StartArray) ? true : false)
		{
			bool flag = reader.TrySkip();
			reader = utf8JsonReader;
			if (!flag)
			{
				return false;
			}
			reader.ReadWithVerify();
		}
		return true;
	}
}
public abstract class JsonConverter<T> : JsonConverter
{
	private JsonConverter<T> _fallbackConverterForPropertyNameSerialization;

	internal override Type? KeyType => null;

	internal override Type? ElementType => null;

	public virtual bool HandleNull
	{
		get
		{
			base.UsesDefaultHandleNull = true;
			return false;
		}
	}

	public sealed override Type Type { get; } = typeof(T);


	internal T ReadCore(ref Utf8JsonReader reader, JsonSerializerOptions options, ref ReadStack state)
	{
		try
		{
			if (!state.IsContinuation)
			{
				if (!JsonConverter.SingleValueReadWithReadAhead(base.RequiresReadAhead, ref reader, ref state))
				{
					if (state.SupportContinuation)
					{
						state.BytesConsumed += reader.BytesConsumed;
						if (state.Current.ReturnValue == null)
						{
							return default(T);
						}
						return (T)state.Current.ReturnValue;
					}
					state.BytesConsumed += reader.BytesConsumed;
					return default(T);
				}
			}
			else if (!JsonConverter.SingleValueReadWithReadAhead(requiresReadAhead: true, ref reader, ref state))
			{
				state.BytesConsumed += reader.BytesConsumed;
				return default(T);
			}
			if (TryRead(ref reader, state.Current.JsonTypeInfo.Type, options, ref state, out var value, out var _) && !reader.Read() && !reader.IsFinalBlock)
			{
				state.Current.ReturnValue = value;
			}
			state.BytesConsumed += reader.BytesConsumed;
			return value;
		}
		catch (JsonReaderException ex)
		{
			ThrowHelper.ReThrowWithPath(ref state, ex);
			return default(T);
		}
		catch (FormatException ex2) when (ex2.Source == "System.Text.Json.Rethrowable")
		{
			ThrowHelper.ReThrowWithPath(ref state, in reader, ex2);
			return default(T);
		}
		catch (InvalidOperationException ex3) when (ex3.Source == "System.Text.Json.Rethrowable")
		{
			ThrowHelper.ReThrowWithPath(ref state, in reader, ex3);
			return default(T);
		}
		catch (JsonException ex4) when (ex4.Path == null)
		{
			ThrowHelper.AddJsonExceptionInformation(ref state, in reader, ex4);
			throw;
		}
		catch (NotSupportedException ex5)
		{
			if (ex5.Message.Contains(" Path: "))
			{
				throw;
			}
			ThrowHelper.ThrowNotSupportedException(ref state, in reader, ex5);
			return default(T);
		}
	}

	internal bool WriteCore(Utf8JsonWriter writer, in T value, JsonSerializerOptions options, ref WriteStack state)
	{
		try
		{
			return TryWrite(writer, in value, options, ref state);
		}
		catch (InvalidOperationException ex) when (ex.Source == "System.Text.Json.Rethrowable")
		{
			ThrowHelper.ReThrowWithPath(ref state, ex);
			throw;
		}
		catch (JsonException ex2) when (ex2.Path == null)
		{
			ThrowHelper.AddJsonExceptionInformation(ref state, ex2);
			throw;
		}
		catch (NotSupportedException ex3)
		{
			if (ex3.Message.Contains(" Path: "))
			{
				throw;
			}
			ThrowHelper.ThrowNotSupportedException(ref state, ex3);
			return false;
		}
	}

	protected internal JsonConverter()
	{
		base.IsValueType = typeof(T).IsValueType;
		if (HandleNull)
		{
			base.HandleNullOnRead = true;
			base.HandleNullOnWrite = true;
		}
		else if (base.UsesDefaultHandleNull)
		{
			base.HandleNullOnRead = default(T) != null;
			base.HandleNullOnWrite = false;
		}
	}

	public override bool CanConvert(Type typeToConvert)
	{
		return typeToConvert == typeof(T);
	}

	private protected override ConverterStrategy GetDefaultConverterStrategy()
	{
		return ConverterStrategy.Value;
	}

	internal sealed override JsonTypeInfo CreateJsonTypeInfo(JsonSerializerOptions options)
	{
		return new JsonTypeInfo<T>(this, options);
	}

	internal sealed override void WriteAsObject(Utf8JsonWriter writer, object value, JsonSerializerOptions options)
	{
		T value2 = JsonSerializer.UnboxOnWrite<T>(value);
		Write(writer, value2, options);
	}

	internal sealed override bool OnTryWriteAsObject(Utf8JsonWriter writer, object value, JsonSerializerOptions options, ref WriteStack state)
	{
		T value2 = JsonSerializer.UnboxOnWrite<T>(value);
		return OnTryWrite(writer, value2, options, ref state);
	}

	internal sealed override void WriteAsPropertyNameAsObject(Utf8JsonWriter writer, object value, JsonSerializerOptions options)
	{
		T value2 = JsonSerializer.UnboxOnWrite<T>(value);
		WriteAsPropertyName(writer, value2, options);
	}

	internal sealed override void WriteAsPropertyNameCoreAsObject(Utf8JsonWriter writer, object value, JsonSerializerOptions options, bool isWritingExtensionDataProperty)
	{
		T value2 = JsonSerializer.UnboxOnWrite<T>(value);
		WriteAsPropertyNameCore(writer, value2, options, isWritingExtensionDataProperty);
	}

	internal sealed override void WriteNumberWithCustomHandlingAsObject(Utf8JsonWriter writer, object value, JsonNumberHandling handling)
	{
		T value2 = JsonSerializer.UnboxOnWrite<T>(value);
		WriteNumberWithCustomHandling(writer, value2, handling);
	}

	internal sealed override bool TryWriteAsObject(Utf8JsonWriter writer, object value, JsonSerializerOptions options, ref WriteStack state)
	{
		T value2 = JsonSerializer.UnboxOnWrite<T>(value);
		return TryWrite(writer, in value2, options, ref state);
	}

	internal virtual bool OnTryWrite(Utf8JsonWriter writer, T value, JsonSerializerOptions options, ref WriteStack state)
	{
		Write(writer, value, options);
		return true;
	}

	internal virtual bool OnTryRead(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options, scoped ref ReadStack state, out T value)
	{
		value = Read(ref reader, typeToConvert, options);
		return true;
	}

	public abstract T? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options);

	internal bool TryRead(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options, scoped ref ReadStack state, out T value, out bool isPopulatedValue)
	{
		if (reader.TokenType == JsonTokenType.Null && !base.HandleNullOnRead && !state.IsContinuation)
		{
			if (default(T) != null)
			{
				ThrowHelper.ThrowJsonException_DeserializeUnableToConvertValue(Type);
			}
			value = default(T);
			isPopulatedValue = false;
			return true;
		}
		if (base.ConverterStrategy == ConverterStrategy.Value)
		{
			if (base.IsInternalConverter)
			{
				if (state.Current.NumberHandling.HasValue && base.IsInternalConverterForNumberType)
				{
					value = ReadNumberWithCustomHandling(ref reader, state.Current.NumberHandling.Value, options);
				}
				else
				{
					value = Read(ref reader, typeToConvert, options);
				}
			}
			else
			{
				JsonTokenType tokenType = reader.TokenType;
				int currentDepth = reader.CurrentDepth;
				long bytesConsumed = reader.BytesConsumed;
				if (state.Current.NumberHandling.HasValue && base.IsInternalConverterForNumberType)
				{
					value = ReadNumberWithCustomHandling(ref reader, state.Current.NumberHandling.Value, options);
				}
				else
				{
					value = Read(ref reader, typeToConvert, options);
				}
				VerifyRead(tokenType, currentDepth, bytesConsumed, isValueConverter: true, ref reader);
			}
			isPopulatedValue = false;
			return true;
		}
		bool isContinuation = state.IsContinuation;
		bool flag;
		if (!typeof(T).IsValueType && base.CanBePolymorphic)
		{
			flag = OnTryRead(ref reader, typeToConvert, options, ref state, out value);
			isPopulatedValue = false;
			return true;
		}
		JsonPropertyInfo jsonPropertyInfo = state.Current.JsonPropertyInfo;
		object returnValue = state.Current.ReturnValue;
		state.Push();
		if (returnValue != null && jsonPropertyInfo != null && !jsonPropertyInfo.IsForTypeInfo)
		{
			state.Current.HasParentObject = true;
		}
		flag = OnTryRead(ref reader, typeToConvert, options, ref state, out value);
		isPopulatedValue = state.Current.IsPopulating;
		state.Pop(flag);
		return flag;
	}

	internal sealed override bool OnTryReadAsObject(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options, scoped ref ReadStack state, out object value)
	{
		T value2;
		bool result = OnTryRead(ref reader, typeToConvert, options, ref state, out value2);
		value = value2;
		return result;
	}

	internal sealed override bool TryReadAsObject(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options, scoped ref ReadStack state, out object value)
	{
		T value2;
		bool isPopulatedValue;
		bool result = TryRead(ref reader, typeToConvert, options, ref state, out value2, out isPopulatedValue);
		value = value2;
		return result;
	}

	internal sealed override object ReadAsObject(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		T val = Read(ref reader, typeToConvert, options);
		return val;
	}

	internal sealed override object ReadAsPropertyNameAsObject(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		T val = ReadAsPropertyName(ref reader, typeToConvert, options);
		return val;
	}

	internal sealed override object ReadAsPropertyNameCoreAsObject(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		T val = ReadAsPropertyNameCore(ref reader, typeToConvert, options);
		return val;
	}

	internal sealed override object ReadNumberWithCustomHandlingAsObject(ref Utf8JsonReader reader, JsonNumberHandling handling, JsonSerializerOptions options)
	{
		T val = ReadNumberWithCustomHandling(ref reader, handling, options);
		return val;
	}

	private static bool IsNull(T value)
	{
		return value == null;
	}

	internal bool TryWrite(Utf8JsonWriter writer, in T value, JsonSerializerOptions options, ref WriteStack state)
	{
		if (writer.CurrentDepth >= options.EffectiveMaxDepth)
		{
			ThrowHelper.ThrowJsonException_SerializerCycleDetected(options.EffectiveMaxDepth);
		}
		if (default(T) == null && !base.HandleNullOnWrite && IsNull(value))
		{
			writer.WriteNullValue();
			return true;
		}
		if (base.ConverterStrategy == ConverterStrategy.Value)
		{
			int currentDepth = writer.CurrentDepth;
			if (state.Current.NumberHandling.HasValue && base.IsInternalConverterForNumberType)
			{
				WriteNumberWithCustomHandling(writer, value, state.Current.NumberHandling.Value);
			}
			else
			{
				Write(writer, value, options);
			}
			VerifyWrite(currentDepth, writer);
			return true;
		}
		bool isContinuation = state.IsContinuation;
		bool flag;
		if (!typeof(T).IsValueType && value != null && state.Current.PolymorphicSerializationState != PolymorphicSerializationState.PolymorphicReEntryStarted)
		{
			JsonTypeInfo jsonTypeInfo = state.PeekNestedJsonTypeInfo();
			JsonConverter jsonConverter = ((base.CanBePolymorphic || jsonTypeInfo.PolymorphicTypeResolver != null) ? ResolvePolymorphicConverter(value, jsonTypeInfo, options, ref state) : null);
			if (!isContinuation && options.ReferenceHandlingStrategy != 0 && TryHandleSerializedObjectReference(writer, value, options, jsonConverter, ref state))
			{
				return true;
			}
			if (jsonConverter != null)
			{
				flag = jsonConverter.TryWriteAsObject(writer, value, options, ref state);
				state.Current.ExitPolymorphicConverter(flag);
				if (flag && state.Current.IsPushedReferenceForCycleDetection)
				{
					state.ReferenceResolver.PopReferenceForCycleDetection();
					state.Current.IsPushedReferenceForCycleDetection = false;
				}
				return flag;
			}
		}
		state.Push();
		flag = OnTryWrite(writer, value, options, ref state);
		state.Pop(flag);
		if (flag && state.Current.IsPushedReferenceForCycleDetection)
		{
			state.ReferenceResolver.PopReferenceForCycleDetection();
			state.Current.IsPushedReferenceForCycleDetection = false;
		}
		return flag;
	}

	internal bool TryWriteDataExtensionProperty(Utf8JsonWriter writer, T value, JsonSerializerOptions options, ref WriteStack state)
	{
		if (!base.IsInternalConverter)
		{
			return TryWrite(writer, in value, options, ref state);
		}
		JsonDictionaryConverter<T> jsonDictionaryConverter = (this as JsonDictionaryConverter<T>) ?? ((this as JsonMetadataServicesConverter<T>)?.Converter as JsonDictionaryConverter<T>);
		if (jsonDictionaryConverter == null)
		{
			return TryWrite(writer, in value, options, ref state);
		}
		if (writer.CurrentDepth >= options.EffectiveMaxDepth)
		{
			ThrowHelper.ThrowJsonException_SerializerCycleDetected(options.EffectiveMaxDepth);
		}
		bool isContinuation = state.IsContinuation;
		state.Push();
		if (!isContinuation)
		{
			state.Current.OriginalDepth = writer.CurrentDepth;
		}
		state.Current.IsWritingExtensionDataProperty = true;
		state.Current.JsonPropertyInfo = state.Current.JsonTypeInfo.ElementTypeInfo.PropertyInfoForTypeInfo;
		bool flag = jsonDictionaryConverter.OnWriteResume(writer, value, options, ref state);
		if (flag)
		{
			VerifyWrite(state.Current.OriginalDepth, writer);
		}
		state.Pop(flag);
		return flag;
	}

	internal void VerifyRead(JsonTokenType tokenType, int depth, long bytesConsumed, bool isValueConverter, ref Utf8JsonReader reader)
	{
		switch (tokenType)
		{
		case JsonTokenType.StartArray:
			if (reader.TokenType != JsonTokenType.EndArray)
			{
				ThrowHelper.ThrowJsonException_SerializationConverterRead(this);
			}
			else if (depth != reader.CurrentDepth)
			{
				ThrowHelper.ThrowJsonException_SerializationConverterRead(this);
			}
			return;
		case JsonTokenType.StartObject:
			if (reader.TokenType != JsonTokenType.EndObject)
			{
				ThrowHelper.ThrowJsonException_SerializationConverterRead(this);
			}
			else if (depth != reader.CurrentDepth)
			{
				ThrowHelper.ThrowJsonException_SerializationConverterRead(this);
			}
			return;
		}
		if (isValueConverter)
		{
			if (reader.BytesConsumed != bytesConsumed)
			{
				ThrowHelper.ThrowJsonException_SerializationConverterRead(this);
			}
		}
		else if (!base.CanBePolymorphic && (!base.HandleNullOnRead || tokenType != JsonTokenType.Null))
		{
			ThrowHelper.ThrowJsonException_SerializationConverterRead(this);
		}
	}

	internal void VerifyWrite(int originalDepth, Utf8JsonWriter writer)
	{
		if (originalDepth != writer.CurrentDepth)
		{
			ThrowHelper.ThrowJsonException_SerializationConverterWrite(this);
		}
	}

	public abstract void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options);

	public virtual T ReadAsPropertyName(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		JsonConverter<T> fallbackConverterForPropertyNameSerialization = GetFallbackConverterForPropertyNameSerialization(options);
		if (fallbackConverterForPropertyNameSerialization == null)
		{
			ThrowHelper.ThrowNotSupportedException_DictionaryKeyTypeNotSupported(Type, this);
		}
		return fallbackConverterForPropertyNameSerialization.ReadAsPropertyNameCore(ref reader, typeToConvert, options);
	}

	internal virtual T ReadAsPropertyNameCore(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		long bytesConsumed = reader.BytesConsumed;
		T result = ReadAsPropertyName(ref reader, typeToConvert, options);
		if (reader.BytesConsumed != bytesConsumed)
		{
			ThrowHelper.ThrowJsonException_SerializationConverterRead(this);
		}
		return result;
	}

	public virtual void WriteAsPropertyName(Utf8JsonWriter writer, [DisallowNull] T value, JsonSerializerOptions options)
	{
		JsonConverter<T> fallbackConverterForPropertyNameSerialization = GetFallbackConverterForPropertyNameSerialization(options);
		if (fallbackConverterForPropertyNameSerialization == null)
		{
			ThrowHelper.ThrowNotSupportedException_DictionaryKeyTypeNotSupported(Type, this);
		}
		fallbackConverterForPropertyNameSerialization.WriteAsPropertyNameCore(writer, value, options, isWritingExtensionDataProperty: false);
	}

	internal virtual void WriteAsPropertyNameCore(Utf8JsonWriter writer, [DisallowNull] T value, JsonSerializerOptions options, bool isWritingExtensionDataProperty)
	{
		if (value == null)
		{
			ThrowHelper.ThrowArgumentNullException("value");
		}
		if (isWritingExtensionDataProperty)
		{
			writer.WritePropertyName((string)(object)value);
			return;
		}
		int currentDepth = writer.CurrentDepth;
		WriteAsPropertyName(writer, value, options);
		if (currentDepth != writer.CurrentDepth || writer.TokenType != JsonTokenType.PropertyName)
		{
			ThrowHelper.ThrowJsonException_SerializationConverterWrite(this);
		}
	}

	private JsonConverter<T> GetFallbackConverterForPropertyNameSerialization(JsonSerializerOptions options)
	{
		JsonConverter<T> jsonConverter = null;
		if (!base.IsInternalConverter && !(options.TypeInfoResolver is JsonSerializerContext))
		{
			jsonConverter = _fallbackConverterForPropertyNameSerialization;
			if (jsonConverter == null && DefaultJsonTypeInfoResolver.TryGetDefaultSimpleConverter(Type, out var converter))
			{
				jsonConverter = (_fallbackConverterForPropertyNameSerialization = (JsonConverter<T>)converter);
			}
		}
		return jsonConverter;
	}

	internal virtual T ReadNumberWithCustomHandling(ref Utf8JsonReader reader, JsonNumberHandling handling, JsonSerializerOptions options)
	{
		throw new InvalidOperationException();
	}

	internal virtual void WriteNumberWithCustomHandling(Utf8JsonWriter writer, T value, JsonNumberHandling handling)
	{
		throw new InvalidOperationException();
	}
}
