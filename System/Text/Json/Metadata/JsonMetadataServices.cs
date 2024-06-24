using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization.Converters;

namespace System.Text.Json.Serialization.Metadata;

[EditorBrowsable(EditorBrowsableState.Never)]
public static class JsonMetadataServices
{
	private static JsonConverter<bool> s_booleanConverter;

	private static JsonConverter<byte[]> s_byteArrayConverter;

	private static JsonConverter<byte> s_byteConverter;

	private static JsonConverter<char> s_charConverter;

	private static JsonConverter<DateTime> s_dateTimeConverter;

	private static JsonConverter<DateTimeOffset> s_dateTimeOffsetConverter;

	private static JsonConverter<DateOnly> s_dateOnlyConverter;

	private static JsonConverter<TimeOnly> s_timeOnlyConverter;

	private static JsonConverter<decimal> s_decimalConverter;

	private static JsonConverter<double> s_doubleConverter;

	private static JsonConverter<Guid> s_guidConverter;

	private static JsonConverter<short> s_int16Converter;

	private static JsonConverter<int> s_int32Converter;

	private static JsonConverter<long> s_int64Converter;

	private static JsonConverter<Int128> s_int128Converter;

	private static JsonConverter<UInt128> s_uint128Converter;

	private static JsonConverter<JsonArray> s_jsonArrayConverter;

	private static JsonConverter<JsonElement> s_jsonElementConverter;

	private static JsonConverter<JsonNode> s_jsonNodeConverter;

	private static JsonConverter<JsonObject> s_jsonObjectConverter;

	private static JsonConverter<JsonValue> s_jsonValueConverter;

	private static JsonConverter<JsonDocument> s_jsonDocumentConverter;

	private static JsonConverter<Memory<byte>> s_memoryByteConverter;

	private static JsonConverter<ReadOnlyMemory<byte>> s_readOnlyMemoryByteConverter;

	private static JsonConverter<object> s_objectConverter;

	private static JsonConverter<Half> s_halfConverter;

	private static JsonConverter<float> s_singleConverter;

	private static JsonConverter<sbyte> s_sbyteConverter;

	private static JsonConverter<string> s_stringConverter;

	private static JsonConverter<TimeSpan> s_timeSpanConverter;

	private static JsonConverter<ushort> s_uint16Converter;

	private static JsonConverter<uint> s_uint32Converter;

	private static JsonConverter<ulong> s_uint64Converter;

	private static JsonConverter<Uri> s_uriConverter;

	private static JsonConverter<Version> s_versionConverter;

	public static JsonConverter<bool> BooleanConverter => s_booleanConverter ?? (s_booleanConverter = new BooleanConverter());

	public static JsonConverter<byte[]?> ByteArrayConverter => s_byteArrayConverter ?? (s_byteArrayConverter = new ByteArrayConverter());

	public static JsonConverter<byte> ByteConverter => s_byteConverter ?? (s_byteConverter = new ByteConverter());

	public static JsonConverter<char> CharConverter => s_charConverter ?? (s_charConverter = new CharConverter());

	public static JsonConverter<DateTime> DateTimeConverter => s_dateTimeConverter ?? (s_dateTimeConverter = new DateTimeConverter());

	public static JsonConverter<DateTimeOffset> DateTimeOffsetConverter => s_dateTimeOffsetConverter ?? (s_dateTimeOffsetConverter = new DateTimeOffsetConverter());

	public static JsonConverter<DateOnly> DateOnlyConverter => s_dateOnlyConverter ?? (s_dateOnlyConverter = new DateOnlyConverter());

	public static JsonConverter<TimeOnly> TimeOnlyConverter => s_timeOnlyConverter ?? (s_timeOnlyConverter = new TimeOnlyConverter());

	public static JsonConverter<decimal> DecimalConverter => s_decimalConverter ?? (s_decimalConverter = new DecimalConverter());

	public static JsonConverter<double> DoubleConverter => s_doubleConverter ?? (s_doubleConverter = new DoubleConverter());

	public static JsonConverter<Guid> GuidConverter => s_guidConverter ?? (s_guidConverter = new GuidConverter());

	public static JsonConverter<short> Int16Converter => s_int16Converter ?? (s_int16Converter = new Int16Converter());

	public static JsonConverter<int> Int32Converter => s_int32Converter ?? (s_int32Converter = new Int32Converter());

	public static JsonConverter<long> Int64Converter => s_int64Converter ?? (s_int64Converter = new Int64Converter());

	public static JsonConverter<Int128> Int128Converter => s_int128Converter ?? (s_int128Converter = new Int128Converter());

	[CLSCompliant(false)]
	public static JsonConverter<UInt128> UInt128Converter => s_uint128Converter ?? (s_uint128Converter = new UInt128Converter());

	public static JsonConverter<JsonArray?> JsonArrayConverter => s_jsonArrayConverter ?? (s_jsonArrayConverter = new JsonArrayConverter());

	public static JsonConverter<JsonElement> JsonElementConverter => s_jsonElementConverter ?? (s_jsonElementConverter = new JsonElementConverter());

	public static JsonConverter<JsonNode?> JsonNodeConverter => s_jsonNodeConverter ?? (s_jsonNodeConverter = new JsonNodeConverter());

	public static JsonConverter<JsonObject?> JsonObjectConverter => s_jsonObjectConverter ?? (s_jsonObjectConverter = new JsonObjectConverter());

	public static JsonConverter<JsonValue?> JsonValueConverter => s_jsonValueConverter ?? (s_jsonValueConverter = new JsonValueConverter());

	public static JsonConverter<JsonDocument?> JsonDocumentConverter => s_jsonDocumentConverter ?? (s_jsonDocumentConverter = new JsonDocumentConverter());

	public static JsonConverter<Memory<byte>> MemoryByteConverter => s_memoryByteConverter ?? (s_memoryByteConverter = new MemoryByteConverter());

	public static JsonConverter<ReadOnlyMemory<byte>> ReadOnlyMemoryByteConverter => s_readOnlyMemoryByteConverter ?? (s_readOnlyMemoryByteConverter = new ReadOnlyMemoryByteConverter());

	public static JsonConverter<object?> ObjectConverter => s_objectConverter ?? (s_objectConverter = new DefaultObjectConverter());

	public static JsonConverter<Half> HalfConverter => s_halfConverter ?? (s_halfConverter = new HalfConverter());

	public static JsonConverter<float> SingleConverter => s_singleConverter ?? (s_singleConverter = new SingleConverter());

	[CLSCompliant(false)]
	public static JsonConverter<sbyte> SByteConverter => s_sbyteConverter ?? (s_sbyteConverter = new SByteConverter());

	public static JsonConverter<string?> StringConverter => s_stringConverter ?? (s_stringConverter = new StringConverter());

	public static JsonConverter<TimeSpan> TimeSpanConverter => s_timeSpanConverter ?? (s_timeSpanConverter = new TimeSpanConverter());

	[CLSCompliant(false)]
	public static JsonConverter<ushort> UInt16Converter => s_uint16Converter ?? (s_uint16Converter = new UInt16Converter());

	[CLSCompliant(false)]
	public static JsonConverter<uint> UInt32Converter => s_uint32Converter ?? (s_uint32Converter = new UInt32Converter());

	[CLSCompliant(false)]
	public static JsonConverter<ulong> UInt64Converter => s_uint64Converter ?? (s_uint64Converter = new UInt64Converter());

	public static JsonConverter<Uri?> UriConverter => s_uriConverter ?? (s_uriConverter = new UriConverter());

	public static JsonConverter<Version?> VersionConverter => s_versionConverter ?? (s_versionConverter = new VersionConverter());

	public static JsonTypeInfo<TElement[]> CreateArrayInfo<TElement>(JsonSerializerOptions options, JsonCollectionInfoValues<TElement[]> collectionInfo)
	{
		return CreateCore<TElement[]>(options, collectionInfo, new ArrayConverter<TElement[], TElement>());
	}

	public static JsonTypeInfo<TCollection> CreateListInfo<TCollection, TElement>(JsonSerializerOptions options, JsonCollectionInfoValues<TCollection> collectionInfo) where TCollection : List<TElement>
	{
		return CreateCore<TCollection>(options, collectionInfo, new ListOfTConverter<TCollection, TElement>());
	}

	public static JsonTypeInfo<TCollection> CreateDictionaryInfo<TCollection, TKey, TValue>(JsonSerializerOptions options, JsonCollectionInfoValues<TCollection> collectionInfo) where TCollection : Dictionary<TKey, TValue> where TKey : notnull
	{
		return CreateCore<TCollection>(options, collectionInfo, new DictionaryOfTKeyTValueConverter<TCollection, TKey, TValue>());
	}

	public static JsonTypeInfo<TCollection> CreateImmutableDictionaryInfo<TCollection, TKey, TValue>(JsonSerializerOptions options, JsonCollectionInfoValues<TCollection> collectionInfo, Func<IEnumerable<KeyValuePair<TKey, TValue>>, TCollection> createRangeFunc) where TCollection : IReadOnlyDictionary<TKey, TValue> where TKey : notnull
	{
		if (createRangeFunc == null)
		{
			ThrowHelper.ThrowArgumentNullException("createRangeFunc");
		}
		return CreateCore<TCollection>(options, collectionInfo, new ImmutableDictionaryOfTKeyTValueConverter<TCollection, TKey, TValue>(), createRangeFunc);
	}

	public static JsonTypeInfo<TCollection> CreateIDictionaryInfo<TCollection, TKey, TValue>(JsonSerializerOptions options, JsonCollectionInfoValues<TCollection> collectionInfo) where TCollection : IDictionary<TKey, TValue> where TKey : notnull
	{
		return CreateCore<TCollection>(options, collectionInfo, new IDictionaryOfTKeyTValueConverter<TCollection, TKey, TValue>());
	}

	public static JsonTypeInfo<TCollection> CreateIReadOnlyDictionaryInfo<TCollection, TKey, TValue>(JsonSerializerOptions options, JsonCollectionInfoValues<TCollection> collectionInfo) where TCollection : IReadOnlyDictionary<TKey, TValue> where TKey : notnull
	{
		return CreateCore<TCollection>(options, collectionInfo, new IReadOnlyDictionaryOfTKeyTValueConverter<TCollection, TKey, TValue>());
	}

	public static JsonTypeInfo<TCollection> CreateImmutableEnumerableInfo<TCollection, TElement>(JsonSerializerOptions options, JsonCollectionInfoValues<TCollection> collectionInfo, Func<IEnumerable<TElement>, TCollection> createRangeFunc) where TCollection : IEnumerable<TElement>
	{
		if (createRangeFunc == null)
		{
			ThrowHelper.ThrowArgumentNullException("createRangeFunc");
		}
		return CreateCore<TCollection>(options, collectionInfo, new ImmutableEnumerableOfTConverter<TCollection, TElement>(), createRangeFunc);
	}

	public static JsonTypeInfo<TCollection> CreateIListInfo<TCollection>(JsonSerializerOptions options, JsonCollectionInfoValues<TCollection> collectionInfo) where TCollection : IList
	{
		return CreateCore<TCollection>(options, collectionInfo, new IListConverter<TCollection>());
	}

	public static JsonTypeInfo<TCollection> CreateIListInfo<TCollection, TElement>(JsonSerializerOptions options, JsonCollectionInfoValues<TCollection> collectionInfo) where TCollection : IList<TElement>
	{
		return CreateCore<TCollection>(options, collectionInfo, new IListOfTConverter<TCollection, TElement>());
	}

	public static JsonTypeInfo<TCollection> CreateISetInfo<TCollection, TElement>(JsonSerializerOptions options, JsonCollectionInfoValues<TCollection> collectionInfo) where TCollection : ISet<TElement>
	{
		return CreateCore<TCollection>(options, collectionInfo, new ISetOfTConverter<TCollection, TElement>());
	}

	public static JsonTypeInfo<TCollection> CreateICollectionInfo<TCollection, TElement>(JsonSerializerOptions options, JsonCollectionInfoValues<TCollection> collectionInfo) where TCollection : ICollection<TElement>
	{
		return CreateCore<TCollection>(options, collectionInfo, new ICollectionOfTConverter<TCollection, TElement>());
	}

	public static JsonTypeInfo<TCollection> CreateStackInfo<TCollection, TElement>(JsonSerializerOptions options, JsonCollectionInfoValues<TCollection> collectionInfo) where TCollection : Stack<TElement>
	{
		return CreateCore<TCollection>(options, collectionInfo, new StackOfTConverter<TCollection, TElement>());
	}

	public static JsonTypeInfo<TCollection> CreateQueueInfo<TCollection, TElement>(JsonSerializerOptions options, JsonCollectionInfoValues<TCollection> collectionInfo) where TCollection : Queue<TElement>
	{
		return CreateCore<TCollection>(options, collectionInfo, new QueueOfTConverter<TCollection, TElement>());
	}

	public static JsonTypeInfo<TCollection> CreateConcurrentStackInfo<TCollection, TElement>(JsonSerializerOptions options, JsonCollectionInfoValues<TCollection> collectionInfo) where TCollection : ConcurrentStack<TElement>
	{
		return CreateCore<TCollection>(options, collectionInfo, new ConcurrentStackOfTConverter<TCollection, TElement>());
	}

	public static JsonTypeInfo<TCollection> CreateConcurrentQueueInfo<TCollection, TElement>(JsonSerializerOptions options, JsonCollectionInfoValues<TCollection> collectionInfo) where TCollection : ConcurrentQueue<TElement>
	{
		return CreateCore<TCollection>(options, collectionInfo, new ConcurrentQueueOfTConverter<TCollection, TElement>());
	}

	public static JsonTypeInfo<TCollection> CreateIEnumerableInfo<TCollection, TElement>(JsonSerializerOptions options, JsonCollectionInfoValues<TCollection> collectionInfo) where TCollection : IEnumerable<TElement>
	{
		return CreateCore<TCollection>(options, collectionInfo, new IEnumerableOfTConverter<TCollection, TElement>());
	}

	public static JsonTypeInfo<TCollection> CreateIAsyncEnumerableInfo<TCollection, TElement>(JsonSerializerOptions options, JsonCollectionInfoValues<TCollection> collectionInfo) where TCollection : IAsyncEnumerable<TElement>
	{
		return CreateCore<TCollection>(options, collectionInfo, new IAsyncEnumerableOfTConverter<TCollection, TElement>());
	}

	public static JsonTypeInfo<TCollection> CreateIDictionaryInfo<TCollection>(JsonSerializerOptions options, JsonCollectionInfoValues<TCollection> collectionInfo) where TCollection : IDictionary
	{
		return CreateCore<TCollection>(options, collectionInfo, new IDictionaryConverter<TCollection>());
	}

	public static JsonTypeInfo<TCollection> CreateStackInfo<TCollection>(JsonSerializerOptions options, JsonCollectionInfoValues<TCollection> collectionInfo, Action<TCollection, object?> addFunc) where TCollection : IEnumerable
	{
		return CreateStackOrQueueInfo(options, collectionInfo, addFunc);
	}

	public static JsonTypeInfo<TCollection> CreateQueueInfo<TCollection>(JsonSerializerOptions options, JsonCollectionInfoValues<TCollection> collectionInfo, Action<TCollection, object?> addFunc) where TCollection : IEnumerable
	{
		return CreateStackOrQueueInfo(options, collectionInfo, addFunc);
	}

	private static JsonTypeInfo<TCollection> CreateStackOrQueueInfo<TCollection>(JsonSerializerOptions options, JsonCollectionInfoValues<TCollection> collectionInfo, Action<TCollection, object> addFunc) where TCollection : IEnumerable
	{
		if (addFunc == null)
		{
			ThrowHelper.ThrowArgumentNullException("addFunc");
		}
		return CreateCore(options, collectionInfo, new StackOrQueueConverter<TCollection>(), null, addFunc);
	}

	public static JsonTypeInfo<TCollection> CreateIEnumerableInfo<TCollection>(JsonSerializerOptions options, JsonCollectionInfoValues<TCollection> collectionInfo) where TCollection : IEnumerable
	{
		return CreateCore<TCollection>(options, collectionInfo, new IEnumerableConverter<TCollection>());
	}

	public static JsonTypeInfo<Memory<TElement>> CreateMemoryInfo<TElement>(JsonSerializerOptions options, JsonCollectionInfoValues<Memory<TElement>> collectionInfo)
	{
		return CreateCore<Memory<TElement>>(options, collectionInfo, new MemoryConverter<TElement>());
	}

	public static JsonTypeInfo<ReadOnlyMemory<TElement>> CreateReadOnlyMemoryInfo<TElement>(JsonSerializerOptions options, JsonCollectionInfoValues<ReadOnlyMemory<TElement>> collectionInfo)
	{
		return CreateCore<ReadOnlyMemory<TElement>>(options, collectionInfo, new ReadOnlyMemoryConverter<TElement>());
	}

	public static JsonConverter<T> GetUnsupportedTypeConverter<T>()
	{
		return new UnsupportedTypeConverter<T>();
	}

	public static JsonConverter<T> GetEnumConverter<T>(JsonSerializerOptions options) where T : struct, Enum
	{
		if (options == null)
		{
			ThrowHelper.ThrowArgumentNullException("options");
		}
		return new EnumConverter<T>(EnumConverterOptions.AllowNumbers, options);
	}

	public static JsonConverter<T?> GetNullableConverter<T>(JsonTypeInfo<T> underlyingTypeInfo) where T : struct
	{
		if (underlyingTypeInfo == null)
		{
			ThrowHelper.ThrowArgumentNullException("underlyingTypeInfo");
		}
		JsonConverter<T> typedConverter = GetTypedConverter<T>(underlyingTypeInfo.Converter);
		return new NullableConverter<T>(typedConverter);
	}

	public static JsonConverter<T?> GetNullableConverter<T>(JsonSerializerOptions options) where T : struct
	{
		if (options == null)
		{
			ThrowHelper.ThrowArgumentNullException("options");
		}
		JsonConverter<T> typedConverter = GetTypedConverter<T>(options.GetConverterInternal(typeof(T)));
		return new NullableConverter<T>(typedConverter);
	}

	internal static JsonConverter<T> GetTypedConverter<T>(JsonConverter converter)
	{
		JsonConverter<T> jsonConverter = converter as JsonConverter<T>;
		if (jsonConverter == null)
		{
			throw new InvalidOperationException(System.SR.Format(System.SR.SerializationConverterNotCompatible, jsonConverter, typeof(T)));
		}
		return jsonConverter;
	}

	private static JsonTypeInfo<T> CreateCore<T>(JsonConverter converter, JsonSerializerOptions options)
	{
		JsonTypeInfo<T> jsonTypeInfo = new JsonTypeInfo<T>(converter, options);
		jsonTypeInfo.PopulatePolymorphismMetadata();
		jsonTypeInfo.MapInterfaceTypesToCallbacks();
		converter.ConfigureJsonTypeInfo(jsonTypeInfo, options);
		return jsonTypeInfo;
	}

	private static JsonTypeInfo<T> CreateCore<T>(JsonSerializerOptions options, JsonObjectInfoValues<T> objectInfo)
	{
		JsonConverter<T> converter = GetConverter(objectInfo);
		JsonTypeInfo<T> jsonTypeInfo = new JsonTypeInfo<T>(converter, options);
		if (objectInfo.ObjectWithParameterizedConstructorCreator != null)
		{
			jsonTypeInfo.CreateObjectWithArgs = objectInfo.ObjectWithParameterizedConstructorCreator;
			PopulateParameterInfoValues(jsonTypeInfo, objectInfo.ConstructorParameterMetadataInitializer);
		}
		else
		{
			jsonTypeInfo.SetCreateObjectIfCompatible(objectInfo.ObjectCreator);
			jsonTypeInfo.CreateObjectForExtensionDataProperty = ((JsonTypeInfo)jsonTypeInfo).CreateObject;
		}
		if (objectInfo.PropertyMetadataInitializer != null)
		{
			jsonTypeInfo.SourceGenDelayedPropertyInitializer = objectInfo.PropertyMetadataInitializer;
		}
		else
		{
			jsonTypeInfo.PropertyMetadataSerializationNotSupported = true;
		}
		jsonTypeInfo.SerializeHandler = objectInfo.SerializeHandler;
		jsonTypeInfo.NumberHandling = objectInfo.NumberHandling;
		jsonTypeInfo.PopulatePolymorphismMetadata();
		jsonTypeInfo.MapInterfaceTypesToCallbacks();
		converter.ConfigureJsonTypeInfo(jsonTypeInfo, options);
		return jsonTypeInfo;
	}

	private static JsonTypeInfo<T> CreateCore<T>(JsonSerializerOptions options, JsonCollectionInfoValues<T> collectionInfo, JsonConverter<T> converter, object createObjectWithArgs = null, object addFunc = null)
	{
		if (collectionInfo == null)
		{
			ThrowHelper.ThrowArgumentNullException("collectionInfo");
		}
		converter = ((collectionInfo.SerializeHandler != null) ? new JsonMetadataServicesConverter<T>(converter) : converter);
		JsonTypeInfo<T> jsonTypeInfo = new JsonTypeInfo<T>(converter, options);
		jsonTypeInfo.KeyTypeInfo = collectionInfo.KeyInfo;
		jsonTypeInfo.ElementTypeInfo = collectionInfo.ElementInfo;
		jsonTypeInfo.NumberHandling = collectionInfo.NumberHandling;
		jsonTypeInfo.SerializeHandler = collectionInfo.SerializeHandler;
		jsonTypeInfo.CreateObjectWithArgs = createObjectWithArgs;
		jsonTypeInfo.AddMethodDelegate = addFunc;
		jsonTypeInfo.SetCreateObjectIfCompatible(collectionInfo.ObjectCreator);
		jsonTypeInfo.PopulatePolymorphismMetadata();
		jsonTypeInfo.MapInterfaceTypesToCallbacks();
		converter.ConfigureJsonTypeInfo(jsonTypeInfo, options);
		return jsonTypeInfo;
	}

	private static JsonConverter<T> GetConverter<T>(JsonObjectInfoValues<T> objectInfo)
	{
		JsonConverter<T> jsonConverter = ((objectInfo.ObjectWithParameterizedConstructorCreator != null) ? new LargeObjectWithParameterizedConstructorConverter<T>() : new ObjectDefaultConverter<T>());
		if (objectInfo.SerializeHandler == null)
		{
			return jsonConverter;
		}
		return new JsonMetadataServicesConverter<T>(jsonConverter);
	}

	private static void PopulateParameterInfoValues(JsonTypeInfo typeInfo, Func<JsonParameterInfoValues[]> paramFactory)
	{
		JsonParameterInfoValues[] array = paramFactory?.Invoke();
		if (array != null)
		{
			typeInfo.ParameterInfoValues = array;
		}
		else
		{
			typeInfo.PropertyMetadataSerializationNotSupported = true;
		}
	}

	internal static void PopulateProperties(JsonTypeInfo typeInfo, JsonTypeInfo.JsonPropertyInfoList propertyList, Func<JsonSerializerContext, JsonPropertyInfo[]> propInitFunc)
	{
		JsonSerializerContext arg = typeInfo.Options.TypeInfoResolver as JsonSerializerContext;
		JsonPropertyInfo[] array = propInitFunc(arg);
		JsonTypeInfo.PropertyHierarchyResolutionState state = new JsonTypeInfo.PropertyHierarchyResolutionState(typeInfo.Options);
		JsonPropertyInfo[] array2 = array;
		foreach (JsonPropertyInfo jsonPropertyInfo in array2)
		{
			if (!jsonPropertyInfo.SrcGen_IsPublic)
			{
				if (jsonPropertyInfo.SrcGen_HasJsonInclude)
				{
					ThrowHelper.ThrowInvalidOperationException_JsonIncludeOnInaccessibleProperty(jsonPropertyInfo.MemberName, jsonPropertyInfo.DeclaringType);
				}
			}
			else if (jsonPropertyInfo.MemberType != MemberTypes.Field || jsonPropertyInfo.SrcGen_HasJsonInclude || typeInfo.Options.IncludeFields)
			{
				propertyList.AddPropertyWithConflictResolution(jsonPropertyInfo, ref state);
			}
		}
		if (state.IsPropertyOrderSpecified)
		{
			propertyList.SortProperties();
		}
	}

	private static JsonPropertyInfo<T> CreatePropertyInfoCore<T>(JsonPropertyInfoValues<T> propertyInfoValues, JsonSerializerOptions options)
	{
		JsonPropertyInfo<T> jsonPropertyInfo = new JsonPropertyInfo<T>(propertyInfoValues.DeclaringType, null, options);
		DeterminePropertyName(jsonPropertyInfo, propertyInfoValues.PropertyName, propertyInfoValues.JsonPropertyName);
		jsonPropertyInfo.MemberName = propertyInfoValues.PropertyName;
		jsonPropertyInfo.MemberType = (propertyInfoValues.IsProperty ? MemberTypes.Property : MemberTypes.Field);
		jsonPropertyInfo.SrcGen_IsPublic = propertyInfoValues.IsPublic;
		jsonPropertyInfo.SrcGen_HasJsonInclude = propertyInfoValues.HasJsonInclude;
		jsonPropertyInfo.IsExtensionData = propertyInfoValues.IsExtensionData;
		jsonPropertyInfo.CustomConverter = propertyInfoValues.Converter;
		if (jsonPropertyInfo.IgnoreCondition != JsonIgnoreCondition.Always)
		{
			jsonPropertyInfo.Get = propertyInfoValues.Getter;
			jsonPropertyInfo.Set = propertyInfoValues.Setter;
		}
		jsonPropertyInfo.IgnoreCondition = propertyInfoValues.IgnoreCondition;
		jsonPropertyInfo.JsonTypeInfo = propertyInfoValues.PropertyTypeInfo;
		jsonPropertyInfo.NumberHandling = propertyInfoValues.NumberHandling;
		return jsonPropertyInfo;
	}

	private static void DeterminePropertyName(JsonPropertyInfo propertyInfo, string declaredPropertyName, string declaredJsonPropertyName)
	{
		string text = ((declaredJsonPropertyName != null) ? declaredJsonPropertyName : ((propertyInfo.Options.PropertyNamingPolicy != null) ? propertyInfo.Options.PropertyNamingPolicy.ConvertName(declaredPropertyName) : declaredPropertyName));
		if (text == null)
		{
			ThrowHelper.ThrowInvalidOperationException_SerializerPropertyNameNull(propertyInfo);
		}
		propertyInfo.Name = text;
	}

	public static JsonPropertyInfo CreatePropertyInfo<T>(JsonSerializerOptions options, JsonPropertyInfoValues<T> propertyInfo)
	{
		if (options == null)
		{
			ThrowHelper.ThrowArgumentNullException("options");
		}
		if (propertyInfo == null)
		{
			ThrowHelper.ThrowArgumentNullException("propertyInfo");
		}
		Type declaringType = propertyInfo.DeclaringType;
		if (declaringType == null)
		{
			throw new ArgumentException("DeclaringType");
		}
		string propertyName = propertyInfo.PropertyName;
		if (propertyName == null)
		{
			throw new ArgumentException("PropertyName");
		}
		if (!propertyInfo.IsProperty && propertyInfo.IsVirtual)
		{
			throw new InvalidOperationException(System.SR.Format(System.SR.FieldCannotBeVirtual, "IsProperty", "IsVirtual"));
		}
		return CreatePropertyInfoCore(propertyInfo, options);
	}

	public static JsonTypeInfo<T> CreateObjectInfo<T>(JsonSerializerOptions options, JsonObjectInfoValues<T> objectInfo) where T : notnull
	{
		if (options == null)
		{
			ThrowHelper.ThrowArgumentNullException("options");
		}
		if (objectInfo == null)
		{
			ThrowHelper.ThrowArgumentNullException("objectInfo");
		}
		return CreateCore(options, objectInfo);
	}

	public static JsonTypeInfo<T> CreateValueInfo<T>(JsonSerializerOptions options, JsonConverter converter)
	{
		if (options == null)
		{
			ThrowHelper.ThrowArgumentNullException("options");
		}
		if (converter == null)
		{
			ThrowHelper.ThrowArgumentNullException("converter");
		}
		return CreateCore<T>(converter, options);
	}
}
