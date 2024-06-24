using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json.Reflection;
using System.Text.Json.Serialization.Converters;
using System.Threading;

namespace System.Text.Json.Serialization.Metadata;

public class DefaultJsonTypeInfoResolver : IJsonTypeInfoResolver, IBuiltInJsonTypeInfoResolver
{
	private sealed class ModifierCollection : ConfigurationList<Action<JsonTypeInfo>>
	{
		private readonly DefaultJsonTypeInfoResolver _resolver;

		public override bool IsReadOnly => !_resolver._mutable;

		public ModifierCollection(DefaultJsonTypeInfoResolver resolver)
			: base((IEnumerable<Action<JsonTypeInfo>>)null)
		{
			_resolver = resolver;
		}

		protected override void OnCollectionModifying()
		{
			if (!_resolver._mutable)
			{
				ThrowHelper.ThrowInvalidOperationException_DefaultTypeInfoResolverImmutable();
			}
		}
	}

	private static Dictionary<Type, JsonConverter> s_defaultSimpleConverters;

	private static JsonConverterFactory[] s_defaultFactoryConverters;

	private static MemberAccessor s_memberAccessor;

	private bool _mutable;

	private ModifierCollection _modifiers;

	private static DefaultJsonTypeInfoResolver s_defaultInstance;

	internal static MemberAccessor MemberAccessor
	{
		[RequiresDynamicCode("JSON serialization and deserialization might require types that cannot be statically analyzed and might need runtime code generation. Use System.Text.Json source generation for native AOT applications.")]
		get
		{
			return s_memberAccessor ?? (s_memberAccessor = (RuntimeFeature.IsDynamicCodeSupported ? ((MemberAccessor)new ReflectionEmitCachingMemberAccessor()) : ((MemberAccessor)new ReflectionMemberAccessor())));
		}
	}

	public IList<Action<JsonTypeInfo>> Modifiers => _modifiers ?? (_modifiers = new ModifierCollection(this));

	[RequiresUnreferencedCode("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext, or make sure all of the required types are preserved.")]
	[RequiresDynamicCode("JSON serialization and deserialization might require types that cannot be statically analyzed and might need runtime code generation. Use System.Text.Json source generation for native AOT applications.")]
	private static JsonConverterFactory[] GetDefaultFactoryConverters()
	{
		return new JsonConverterFactory[9]
		{
			new UnsupportedTypeConverterFactory(),
			new NullableConverterFactory(),
			new EnumConverterFactory(),
			new JsonNodeConverterFactory(),
			new FSharpTypeConverterFactory(),
			new MemoryConverterFactory(),
			new IAsyncEnumerableConverterFactory(),
			new IEnumerableConverterFactory(),
			new ObjectConverterFactory()
		};
	}

	private static Dictionary<Type, JsonConverter> GetDefaultSimpleConverters()
	{
		Dictionary<Type, JsonConverter> converters = new Dictionary<Type, JsonConverter>(31);
		Add(JsonMetadataServices.BooleanConverter);
		Add(JsonMetadataServices.ByteConverter);
		Add(JsonMetadataServices.ByteArrayConverter);
		Add(JsonMetadataServices.CharConverter);
		Add(JsonMetadataServices.DateTimeConverter);
		Add(JsonMetadataServices.DateTimeOffsetConverter);
		Add(JsonMetadataServices.DateOnlyConverter);
		Add(JsonMetadataServices.TimeOnlyConverter);
		Add(JsonMetadataServices.HalfConverter);
		Add(JsonMetadataServices.DoubleConverter);
		Add(JsonMetadataServices.DecimalConverter);
		Add(JsonMetadataServices.GuidConverter);
		Add(JsonMetadataServices.Int16Converter);
		Add(JsonMetadataServices.Int32Converter);
		Add(JsonMetadataServices.Int64Converter);
		Add(JsonMetadataServices.JsonElementConverter);
		Add(JsonMetadataServices.JsonDocumentConverter);
		Add(JsonMetadataServices.MemoryByteConverter);
		Add(JsonMetadataServices.ReadOnlyMemoryByteConverter);
		Add(JsonMetadataServices.ObjectConverter);
		Add(JsonMetadataServices.SByteConverter);
		Add(JsonMetadataServices.SingleConverter);
		Add(JsonMetadataServices.StringConverter);
		Add(JsonMetadataServices.TimeSpanConverter);
		Add(JsonMetadataServices.UInt16Converter);
		Add(JsonMetadataServices.UInt32Converter);
		Add(JsonMetadataServices.UInt64Converter);
		Add(JsonMetadataServices.Int128Converter);
		Add(JsonMetadataServices.UInt128Converter);
		Add(JsonMetadataServices.UriConverter);
		Add(JsonMetadataServices.VersionConverter);
		return converters;
		void Add(JsonConverter converter)
		{
			converters.Add(converter.Type, converter);
		}
	}

	private static JsonConverter GetBuiltInConverter(Type typeToConvert)
	{
		if (s_defaultSimpleConverters.TryGetValue(typeToConvert, out var value))
		{
			return value;
		}
		JsonConverterFactory[] array = s_defaultFactoryConverters;
		foreach (JsonConverterFactory jsonConverterFactory in array)
		{
			if (jsonConverterFactory.CanConvert(typeToConvert))
			{
				return jsonConverterFactory;
			}
		}
		return value;
	}

	internal static bool TryGetDefaultSimpleConverter(Type typeToConvert, [NotNullWhen(true)] out JsonConverter converter)
	{
		if (s_defaultSimpleConverters == null)
		{
			converter = null;
			return false;
		}
		return s_defaultSimpleConverters.TryGetValue(typeToConvert, out converter);
	}

	[RequiresUnreferencedCode("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext, or make sure all of the required types are preserved.")]
	[RequiresDynamicCode("JSON serialization and deserialization might require types that cannot be statically analyzed and might need runtime code generation. Use System.Text.Json source generation for native AOT applications.")]
	private static JsonConverter GetCustomConverterForMember(Type typeToConvert, MemberInfo memberInfo, JsonSerializerOptions options)
	{
		JsonConverterAttribute uniqueCustomAttribute = memberInfo.GetUniqueCustomAttribute<JsonConverterAttribute>(inherit: false);
		if (uniqueCustomAttribute != null)
		{
			return GetConverterFromAttribute(uniqueCustomAttribute, typeToConvert, memberInfo, options);
		}
		return null;
	}

	[RequiresUnreferencedCode("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext, or make sure all of the required types are preserved.")]
	[RequiresDynamicCode("JSON serialization and deserialization might require types that cannot be statically analyzed and might need runtime code generation. Use System.Text.Json source generation for native AOT applications.")]
	internal static JsonConverter GetConverterForType(Type typeToConvert, JsonSerializerOptions options, bool resolveJsonConverterAttribute = true)
	{
		RootDefaultInstance();
		JsonConverter jsonConverter = options.GetConverterFromList(typeToConvert);
		if (resolveJsonConverterAttribute && jsonConverter == null)
		{
			JsonConverterAttribute uniqueCustomAttribute = typeToConvert.GetUniqueCustomAttribute<JsonConverterAttribute>(inherit: false);
			if (uniqueCustomAttribute != null)
			{
				jsonConverter = GetConverterFromAttribute(uniqueCustomAttribute, typeToConvert, null, options);
			}
		}
		if (jsonConverter == null)
		{
			jsonConverter = GetBuiltInConverter(typeToConvert);
		}
		jsonConverter = options.ExpandConverterFactory(jsonConverter, typeToConvert);
		if (!jsonConverter.Type.IsInSubtypeRelationshipWith(typeToConvert))
		{
			ThrowHelper.ThrowInvalidOperationException_SerializationConverterNotCompatible(jsonConverter.GetType(), typeToConvert);
		}
		JsonSerializerOptions.CheckConverterNullabilityIsSameAsPropertyType(jsonConverter, typeToConvert);
		return jsonConverter;
	}

	[RequiresUnreferencedCode("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext, or make sure all of the required types are preserved.")]
	[RequiresDynamicCode("JSON serialization and deserialization might require types that cannot be statically analyzed and might need runtime code generation. Use System.Text.Json source generation for native AOT applications.")]
	private static JsonConverter GetConverterFromAttribute(JsonConverterAttribute converterAttribute, Type typeToConvert, MemberInfo memberInfo, JsonSerializerOptions options)
	{
		Type type = memberInfo?.DeclaringType ?? typeToConvert;
		Type converterType = converterAttribute.ConverterType;
		JsonConverter jsonConverter;
		if (converterType == null)
		{
			jsonConverter = converterAttribute.CreateConverter(typeToConvert);
			if (jsonConverter == null)
			{
				ThrowHelper.ThrowInvalidOperationException_SerializationConverterOnAttributeNotCompatible(type, memberInfo, typeToConvert);
			}
		}
		else
		{
			ConstructorInfo constructor = converterType.GetConstructor(Type.EmptyTypes);
			if (!typeof(JsonConverter).IsAssignableFrom(converterType) || constructor == null || !constructor.IsPublic)
			{
				ThrowHelper.ThrowInvalidOperationException_SerializationConverterOnAttributeInvalid(type, memberInfo);
			}
			jsonConverter = (JsonConverter)Activator.CreateInstance(converterType);
		}
		if (!jsonConverter.CanConvert(typeToConvert))
		{
			Type underlyingType = Nullable.GetUnderlyingType(typeToConvert);
			if (underlyingType != null && jsonConverter.CanConvert(underlyingType))
			{
				if (jsonConverter is JsonConverterFactory jsonConverterFactory)
				{
					jsonConverter = jsonConverterFactory.GetConverterInternal(underlyingType, options);
				}
				return NullableConverterFactory.CreateValueConverter(underlyingType, jsonConverter);
			}
			ThrowHelper.ThrowInvalidOperationException_SerializationConverterOnAttributeNotCompatible(type, memberInfo, typeToConvert);
		}
		return jsonConverter;
	}

	[RequiresUnreferencedCode("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext, or make sure all of the required types are preserved.")]
	[RequiresDynamicCode("JSON serialization and deserialization might require types that cannot be statically analyzed and might need runtime code generation. Use System.Text.Json source generation for native AOT applications.")]
	private static JsonTypeInfo CreateTypeInfoCore(Type type, JsonConverter converter, JsonSerializerOptions options)
	{
		JsonTypeInfo jsonTypeInfo = JsonTypeInfo.CreateJsonTypeInfo(type, converter, options);
		JsonNumberHandling? numberHandlingForType = GetNumberHandlingForType(jsonTypeInfo.Type);
		if (numberHandlingForType.HasValue)
		{
			JsonNumberHandling valueOrDefault = numberHandlingForType.GetValueOrDefault();
			jsonTypeInfo.NumberHandling = valueOrDefault;
		}
		JsonObjectCreationHandling? objectCreationHandlingForType = GetObjectCreationHandlingForType(jsonTypeInfo.Type);
		if (objectCreationHandlingForType.HasValue)
		{
			JsonObjectCreationHandling valueOrDefault2 = objectCreationHandlingForType.GetValueOrDefault();
			jsonTypeInfo.PreferredPropertyObjectCreationHandling = valueOrDefault2;
		}
		JsonUnmappedMemberHandling? unmappedMemberHandling = GetUnmappedMemberHandling(jsonTypeInfo.Type);
		if (unmappedMemberHandling.HasValue)
		{
			JsonUnmappedMemberHandling valueOrDefault3 = unmappedMemberHandling.GetValueOrDefault();
			jsonTypeInfo.UnmappedMemberHandling = valueOrDefault3;
		}
		jsonTypeInfo.PopulatePolymorphismMetadata();
		jsonTypeInfo.MapInterfaceTypesToCallbacks();
		Func<object> func = DetermineCreateObjectDelegate(type, converter);
		jsonTypeInfo.SetCreateObjectIfCompatible(func);
		jsonTypeInfo.CreateObjectForExtensionDataProperty = func;
		if (jsonTypeInfo.Kind == JsonTypeInfoKind.Object)
		{
			PopulateProperties(jsonTypeInfo);
			if (converter.ConstructorIsParameterized)
			{
				PopulateParameterInfoValues(jsonTypeInfo);
			}
		}
		converter.ConfigureJsonTypeInfo(jsonTypeInfo, options);
		converter.ConfigureJsonTypeInfoUsingReflection(jsonTypeInfo, options);
		return jsonTypeInfo;
	}

	[RequiresUnreferencedCode("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext, or make sure all of the required types are preserved.")]
	[RequiresDynamicCode("JSON serialization and deserialization might require types that cannot be statically analyzed and might need runtime code generation. Use System.Text.Json source generation for native AOT applications.")]
	private static void PopulateProperties(JsonTypeInfo typeInfo)
	{
		bool constructorHasSetsRequiredMembersAttribute = typeInfo.Converter.ConstructorInfo?.HasSetsRequiredMembersAttribute() ?? false;
		JsonTypeInfo.PropertyHierarchyResolutionState state = new JsonTypeInfo.PropertyHierarchyResolutionState(typeInfo.Options);
		Type[] sortedTypeHierarchy = typeInfo.Type.GetSortedTypeHierarchy();
		foreach (Type type in sortedTypeHierarchy)
		{
			if (type == JsonTypeInfo.ObjectType || type == typeof(ValueType))
			{
				break;
			}
			AddMembersDeclaredBySuperType(typeInfo, type, constructorHasSetsRequiredMembersAttribute, ref state);
		}
		if (state.IsPropertyOrderSpecified)
		{
			typeInfo.PropertyList.SortProperties();
		}
	}

	[RequiresUnreferencedCode("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext, or make sure all of the required types are preserved.")]
	[RequiresDynamicCode("JSON serialization and deserialization might require types that cannot be statically analyzed and might need runtime code generation. Use System.Text.Json source generation for native AOT applications.")]
	private static void AddMembersDeclaredBySuperType(JsonTypeInfo typeInfo, Type currentType, bool constructorHasSetsRequiredMembersAttribute, ref JsonTypeInfo.PropertyHierarchyResolutionState state)
	{
		bool shouldCheckForRequiredKeyword = !constructorHasSetsRequiredMembersAttribute && currentType.HasRequiredMemberAttribute();
		PropertyInfo[] properties = currentType.GetProperties(BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
		foreach (PropertyInfo propertyInfo in properties)
		{
			if (propertyInfo.GetIndexParameters().Length == 0 && !PropertyIsOverriddenAndIgnored(propertyInfo, state.IgnoredProperties))
			{
				bool flag = propertyInfo.GetCustomAttribute<JsonIncludeAttribute>(inherit: false) != null;
				MethodInfo? getMethod = propertyInfo.GetMethod;
				if (((object)getMethod != null && getMethod.IsPublic) || (propertyInfo.SetMethod?.IsPublic ?? false) || flag)
				{
					AddMember(typeInfo, propertyInfo.PropertyType, propertyInfo, shouldCheckForRequiredKeyword, flag, ref state);
				}
			}
		}
		FieldInfo[] fields = currentType.GetFields(BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
		foreach (FieldInfo fieldInfo in fields)
		{
			bool flag2 = fieldInfo.GetCustomAttribute<JsonIncludeAttribute>(inherit: false) != null;
			if (flag2 || (fieldInfo.IsPublic && typeInfo.Options.IncludeFields))
			{
				AddMember(typeInfo, fieldInfo.FieldType, fieldInfo, shouldCheckForRequiredKeyword, flag2, ref state);
			}
		}
	}

	[RequiresUnreferencedCode("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext, or make sure all of the required types are preserved.")]
	[RequiresDynamicCode("JSON serialization and deserialization might require types that cannot be statically analyzed and might need runtime code generation. Use System.Text.Json source generation for native AOT applications.")]
	private static void AddMember(JsonTypeInfo typeInfo, Type typeToConvert, MemberInfo memberInfo, bool shouldCheckForRequiredKeyword, bool hasJsonIncludeAttribute, ref JsonTypeInfo.PropertyHierarchyResolutionState state)
	{
		JsonPropertyInfo jsonPropertyInfo = CreatePropertyInfo(typeInfo, typeToConvert, memberInfo, typeInfo.Options, shouldCheckForRequiredKeyword, hasJsonIncludeAttribute);
		if (jsonPropertyInfo != null)
		{
			typeInfo.PropertyList.AddPropertyWithConflictResolution(jsonPropertyInfo, ref state);
		}
	}

	[RequiresUnreferencedCode("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext, or make sure all of the required types are preserved.")]
	[RequiresDynamicCode("JSON serialization and deserialization might require types that cannot be statically analyzed and might need runtime code generation. Use System.Text.Json source generation for native AOT applications.")]
	private static JsonPropertyInfo CreatePropertyInfo(JsonTypeInfo typeInfo, Type typeToConvert, MemberInfo memberInfo, JsonSerializerOptions options, bool shouldCheckForRequiredKeyword, bool hasJsonIncludeAttribute)
	{
		JsonIgnoreCondition? jsonIgnoreCondition = memberInfo.GetCustomAttribute<JsonIgnoreAttribute>(inherit: false)?.Condition;
		if (JsonTypeInfo.IsInvalidForSerialization(typeToConvert))
		{
			if (jsonIgnoreCondition == JsonIgnoreCondition.Always)
			{
				return null;
			}
			ThrowHelper.ThrowInvalidOperationException_CannotSerializeInvalidType(typeToConvert, memberInfo.DeclaringType, memberInfo);
		}
		JsonConverter customConverterForMember;
		try
		{
			customConverterForMember = GetCustomConverterForMember(typeToConvert, memberInfo, options);
		}
		catch (InvalidOperationException) when (jsonIgnoreCondition == JsonIgnoreCondition.Always)
		{
			return null;
		}
		JsonPropertyInfo jsonPropertyInfo = typeInfo.CreatePropertyUsingReflection(typeToConvert, memberInfo.DeclaringType);
		PopulatePropertyInfo(jsonPropertyInfo, memberInfo, customConverterForMember, jsonIgnoreCondition, shouldCheckForRequiredKeyword, hasJsonIncludeAttribute);
		return jsonPropertyInfo;
	}

	private static JsonNumberHandling? GetNumberHandlingForType(Type type)
	{
		return type.GetUniqueCustomAttribute<JsonNumberHandlingAttribute>(inherit: false)?.Handling;
	}

	private static JsonObjectCreationHandling? GetObjectCreationHandlingForType(Type type)
	{
		return type.GetUniqueCustomAttribute<JsonObjectCreationHandlingAttribute>(inherit: false)?.Handling;
	}

	private static JsonUnmappedMemberHandling? GetUnmappedMemberHandling(Type type)
	{
		return type.GetUniqueCustomAttribute<JsonUnmappedMemberHandlingAttribute>(inherit: false)?.UnmappedMemberHandling;
	}

	private static bool PropertyIsOverriddenAndIgnored(PropertyInfo propertyInfo, Dictionary<string, JsonPropertyInfo> ignoredMembers)
	{
		if (propertyInfo.IsVirtual() && ignoredMembers != null && ignoredMembers.TryGetValue(propertyInfo.Name, out var value) && value.IsVirtual)
		{
			return propertyInfo.PropertyType == value.PropertyType;
		}
		return false;
	}

	private static void PopulateParameterInfoValues(JsonTypeInfo typeInfo)
	{
		ParameterInfo[] parameters = typeInfo.Converter.ConstructorInfo.GetParameters();
		int num = parameters.Length;
		JsonParameterInfoValues[] array = new JsonParameterInfoValues[num];
		for (int i = 0; i < num; i++)
		{
			ParameterInfo parameterInfo = parameters[i];
			if (string.IsNullOrEmpty(parameterInfo.Name))
			{
				ThrowHelper.ThrowNotSupportedException_ConstructorContainsNullParameterNames(typeInfo.Converter.ConstructorInfo.DeclaringType);
			}
			JsonParameterInfoValues jsonParameterInfoValues = new JsonParameterInfoValues
			{
				Name = parameterInfo.Name,
				ParameterType = parameterInfo.ParameterType,
				Position = parameterInfo.Position,
				HasDefaultValue = parameterInfo.HasDefaultValue,
				DefaultValue = parameterInfo.GetDefaultValue()
			};
			array[i] = jsonParameterInfoValues;
		}
		typeInfo.ParameterInfoValues = array;
	}

	[RequiresUnreferencedCode("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext, or make sure all of the required types are preserved.")]
	[RequiresDynamicCode("JSON serialization and deserialization might require types that cannot be statically analyzed and might need runtime code generation. Use System.Text.Json source generation for native AOT applications.")]
	private static void PopulatePropertyInfo(JsonPropertyInfo jsonPropertyInfo, MemberInfo memberInfo, JsonConverter customConverter, JsonIgnoreCondition? ignoreCondition, bool shouldCheckForRequiredKeyword, bool hasJsonIncludeAttribute)
	{
		ICustomAttributeProvider customAttributeProvider2 = (jsonPropertyInfo.AttributeProvider = memberInfo);
		ICustomAttributeProvider customAttributeProvider3 = customAttributeProvider2;
		if (!(customAttributeProvider3 is PropertyInfo propertyInfo))
		{
			if (customAttributeProvider3 is FieldInfo fieldInfo)
			{
				jsonPropertyInfo.MemberName = fieldInfo.Name;
				jsonPropertyInfo.MemberType = MemberTypes.Field;
			}
		}
		else
		{
			jsonPropertyInfo.MemberName = propertyInfo.Name;
			jsonPropertyInfo.IsVirtual = propertyInfo.IsVirtual();
			jsonPropertyInfo.MemberType = MemberTypes.Property;
		}
		jsonPropertyInfo.CustomConverter = customConverter;
		DeterminePropertyPolicies(jsonPropertyInfo, memberInfo);
		DeterminePropertyName(jsonPropertyInfo, memberInfo);
		DeterminePropertyIsRequired(jsonPropertyInfo, memberInfo, shouldCheckForRequiredKeyword);
		if (ignoreCondition != JsonIgnoreCondition.Always)
		{
			jsonPropertyInfo.DetermineReflectionPropertyAccessors(memberInfo, hasJsonIncludeAttribute);
		}
		jsonPropertyInfo.IgnoreCondition = ignoreCondition;
		jsonPropertyInfo.IsExtensionData = memberInfo.GetCustomAttribute<JsonExtensionDataAttribute>(inherit: false) != null;
	}

	private static void DeterminePropertyPolicies(JsonPropertyInfo propertyInfo, MemberInfo memberInfo)
	{
		propertyInfo.Order = memberInfo.GetCustomAttribute<JsonPropertyOrderAttribute>(inherit: false)?.Order ?? 0;
		propertyInfo.NumberHandling = memberInfo.GetCustomAttribute<JsonNumberHandlingAttribute>(inherit: false)?.Handling;
		propertyInfo.ObjectCreationHandling = memberInfo.GetCustomAttribute<JsonObjectCreationHandlingAttribute>(inherit: false)?.Handling;
	}

	private static void DeterminePropertyName(JsonPropertyInfo propertyInfo, MemberInfo memberInfo)
	{
		JsonPropertyNameAttribute customAttribute = memberInfo.GetCustomAttribute<JsonPropertyNameAttribute>(inherit: false);
		string text = ((customAttribute != null) ? customAttribute.Name : ((propertyInfo.Options.PropertyNamingPolicy == null) ? memberInfo.Name : propertyInfo.Options.PropertyNamingPolicy.ConvertName(memberInfo.Name)));
		if (text == null)
		{
			ThrowHelper.ThrowInvalidOperationException_SerializerPropertyNameNull(propertyInfo);
		}
		propertyInfo.Name = text;
	}

	private static void DeterminePropertyIsRequired(JsonPropertyInfo propertyInfo, MemberInfo memberInfo, bool shouldCheckForRequiredKeyword)
	{
		propertyInfo.IsRequired = memberInfo.GetCustomAttribute<JsonRequiredAttribute>(inherit: false) != null || (shouldCheckForRequiredKeyword && memberInfo.HasRequiredMemberAttribute());
	}

	[RequiresUnreferencedCode("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext, or make sure all of the required types are preserved.")]
	[RequiresDynamicCode("JSON serialization and deserialization might require types that cannot be statically analyzed and might need runtime code generation. Use System.Text.Json source generation for native AOT applications.")]
	internal static void DeterminePropertyAccessors<T>(JsonPropertyInfo<T> jsonPropertyInfo, MemberInfo memberInfo, bool useNonPublicAccessors)
	{
		if (!(memberInfo is PropertyInfo propertyInfo))
		{
			if (memberInfo is FieldInfo fieldInfo)
			{
				jsonPropertyInfo.Get = MemberAccessor.CreateFieldGetter<T>(fieldInfo);
				if (!fieldInfo.IsInitOnly)
				{
					jsonPropertyInfo.Set = MemberAccessor.CreateFieldSetter<T>(fieldInfo);
				}
			}
			return;
		}
		MethodInfo getMethod = propertyInfo.GetMethod;
		if (getMethod != null && (getMethod.IsPublic || useNonPublicAccessors))
		{
			jsonPropertyInfo.Get = MemberAccessor.CreatePropertyGetter<T>(propertyInfo);
		}
		MethodInfo setMethod = propertyInfo.SetMethod;
		if (setMethod != null && (setMethod.IsPublic || useNonPublicAccessors))
		{
			jsonPropertyInfo.Set = MemberAccessor.CreatePropertySetter<T>(propertyInfo);
		}
	}

	[RequiresUnreferencedCode("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext, or make sure all of the required types are preserved.")]
	[RequiresDynamicCode("JSON serialization and deserialization might require types that cannot be statically analyzed and might need runtime code generation. Use System.Text.Json source generation for native AOT applications.")]
	private static Func<object> DetermineCreateObjectDelegate(Type type, JsonConverter converter)
	{
		ConstructorInfo constructorInfo = null;
		if (converter.ConstructorInfo != null && !converter.ConstructorIsParameterized)
		{
			constructorInfo = converter.ConstructorInfo;
		}
		if ((object)constructorInfo == null)
		{
			constructorInfo = type.GetConstructor(BindingFlags.Instance | BindingFlags.Public, null, Type.EmptyTypes, null);
		}
		return MemberAccessor.CreateParameterlessConstructor(type, constructorInfo);
	}

	[RequiresUnreferencedCode("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext, or make sure all of the required types are preserved.")]
	[RequiresDynamicCode("JSON serialization and deserialization might require types that cannot be statically analyzed and might need runtime code generation. Use System.Text.Json source generation for native AOT applications.")]
	public DefaultJsonTypeInfoResolver()
		: this(mutable: true)
	{
	}

	[RequiresUnreferencedCode("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext, or make sure all of the required types are preserved.")]
	[RequiresDynamicCode("JSON serialization and deserialization might require types that cannot be statically analyzed and might need runtime code generation. Use System.Text.Json source generation for native AOT applications.")]
	private DefaultJsonTypeInfoResolver(bool mutable)
	{
		_mutable = mutable;
		if (s_defaultFactoryConverters == null)
		{
			s_defaultFactoryConverters = GetDefaultFactoryConverters();
		}
		if (s_defaultSimpleConverters == null)
		{
			s_defaultSimpleConverters = GetDefaultSimpleConverters();
		}
	}

	[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode", Justification = "The ctor is marked RequiresUnreferencedCode.")]
	[UnconditionalSuppressMessage("AotAnalysis", "IL3050:RequiresDynamicCode", Justification = "The ctor is marked RequiresDynamicCode.")]
	public virtual JsonTypeInfo GetTypeInfo(Type type, JsonSerializerOptions options)
	{
		if (type == null)
		{
			ThrowHelper.ThrowArgumentNullException("type");
		}
		if (options == null)
		{
			ThrowHelper.ThrowArgumentNullException("options");
		}
		_mutable = false;
		JsonTypeInfo.ValidateType(type);
		JsonTypeInfo jsonTypeInfo = CreateJsonTypeInfo(type, options);
		jsonTypeInfo.OriginatingResolver = this;
		jsonTypeInfo.IsCustomized = false;
		if (_modifiers != null)
		{
			foreach (Action<JsonTypeInfo> modifier in _modifiers)
			{
				modifier(jsonTypeInfo);
			}
		}
		return jsonTypeInfo;
	}

	[RequiresUnreferencedCode("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext, or make sure all of the required types are preserved.")]
	[RequiresDynamicCode("JSON serialization and deserialization might require types that cannot be statically analyzed and might need runtime code generation. Use System.Text.Json source generation for native AOT applications.")]
	private static JsonTypeInfo CreateJsonTypeInfo(Type type, JsonSerializerOptions options)
	{
		JsonConverter converterForType = GetConverterForType(type, options);
		return CreateTypeInfoCore(type, converterForType, options);
	}

	bool IBuiltInJsonTypeInfoResolver.IsCompatibleWithOptions(JsonSerializerOptions _)
	{
		ModifierCollection modifiers = _modifiers;
		if ((modifiers == null || modifiers.Count == 0) ? true : false)
		{
			return GetType() == typeof(DefaultJsonTypeInfoResolver);
		}
		return false;
	}

	[RequiresUnreferencedCode("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext, or make sure all of the required types are preserved.")]
	[RequiresDynamicCode("JSON serialization and deserialization might require types that cannot be statically analyzed and might need runtime code generation. Use System.Text.Json source generation for native AOT applications.")]
	internal static DefaultJsonTypeInfoResolver RootDefaultInstance()
	{
		DefaultJsonTypeInfoResolver defaultJsonTypeInfoResolver = s_defaultInstance;
		if (defaultJsonTypeInfoResolver != null)
		{
			return defaultJsonTypeInfoResolver;
		}
		DefaultJsonTypeInfoResolver defaultJsonTypeInfoResolver2 = new DefaultJsonTypeInfoResolver(mutable: false);
		DefaultJsonTypeInfoResolver defaultJsonTypeInfoResolver3 = Interlocked.CompareExchange(ref s_defaultInstance, defaultJsonTypeInfoResolver2, null);
		return defaultJsonTypeInfoResolver3 ?? defaultJsonTypeInfoResolver2;
	}
}
