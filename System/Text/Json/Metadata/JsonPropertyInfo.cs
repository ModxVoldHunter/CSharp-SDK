using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace System.Text.Json.Serialization.Metadata;

[DebuggerDisplay("{DebuggerDisplay,nq}")]
public abstract class JsonPropertyInfo
{
	internal static readonly JsonPropertyInfo s_missingProperty = GetPropertyPlaceholder();

	private protected JsonConverter _effectiveConverter;

	private JsonConverter _customConverter;

	private protected Func<object, object> _untypedGet;

	private protected Action<object, object> _untypedSet;

	private bool _isUserSpecifiedSetter;

	private protected Func<object, object, bool> _shouldSerialize;

	private bool _isUserSpecifiedShouldSerialize;

	private JsonIgnoreCondition? _ignoreCondition;

	private JsonObjectCreationHandling? _objectCreationHandling;

	private ICustomAttributeProvider _attributeProvider;

	private bool _isExtensionDataProperty;

	private bool _isRequired;

	[CompilerGenerated]
	private bool _003CIsConfigured_003Ek__BackingField;

	private string _name;

	private int _order;

	private JsonTypeInfo _jsonTypeInfo;

	private JsonNumberHandling? _numberHandling;

	private int _index;

	internal JsonTypeInfo? ParentTypeInfo { get; private set; }

	internal JsonConverter EffectiveConverter => _effectiveConverter;

	public JsonConverter? CustomConverter
	{
		get
		{
			return _customConverter;
		}
		set
		{
			VerifyMutable();
			_customConverter = value;
		}
	}

	public Func<object, object?>? Get
	{
		get
		{
			return _untypedGet;
		}
		set
		{
			VerifyMutable();
			SetGetter(value);
		}
	}

	public Action<object, object?>? Set
	{
		get
		{
			return _untypedSet;
		}
		set
		{
			VerifyMutable();
			SetSetter(value);
			_isUserSpecifiedSetter = true;
		}
	}

	public Func<object, object?, bool>? ShouldSerialize
	{
		get
		{
			return _shouldSerialize;
		}
		set
		{
			VerifyMutable();
			SetShouldSerialize(value);
			_isUserSpecifiedShouldSerialize = true;
			IgnoreDefaultValuesOnWrite = false;
		}
	}

	internal JsonIgnoreCondition? IgnoreCondition
	{
		get
		{
			return _ignoreCondition;
		}
		set
		{
			ConfigureIgnoreCondition(value);
			_ignoreCondition = value;
		}
	}

	public ICustomAttributeProvider? AttributeProvider
	{
		get
		{
			return _attributeProvider;
		}
		set
		{
			VerifyMutable();
			_attributeProvider = value;
		}
	}

	internal JsonObjectCreationHandling EffectiveObjectCreationHandling { get; private set; }

	public JsonObjectCreationHandling? ObjectCreationHandling
	{
		get
		{
			return _objectCreationHandling;
		}
		set
		{
			VerifyMutable();
			if (value.HasValue && !JsonSerializer.IsValidCreationHandlingValue(value.Value))
			{
				throw new ArgumentOutOfRangeException("value");
			}
			_objectCreationHandling = value;
		}
	}

	internal string? MemberName { get; set; }

	internal MemberTypes MemberType { get; set; }

	internal bool IsVirtual { get; set; }

	public bool IsExtensionData
	{
		get
		{
			return _isExtensionDataProperty;
		}
		set
		{
			VerifyMutable();
			if (value && !System.Text.Json.Serialization.Metadata.JsonTypeInfo.IsValidExtensionDataProperty(PropertyType))
			{
				ThrowHelper.ThrowInvalidOperationException_SerializationDataExtensionPropertyInvalid(this);
			}
			_isExtensionDataProperty = value;
		}
	}

	public bool IsRequired
	{
		get
		{
			return _isRequired;
		}
		set
		{
			VerifyMutable();
			_isRequired = value;
		}
	}

	public Type PropertyType { get; }

	private bool IsConfigured
	{
		[CompilerGenerated]
		set
		{
			_003CIsConfigured_003Ek__BackingField = value;
		}
	}

	internal bool HasGetter => _untypedGet != null;

	internal bool HasSetter => _untypedSet != null;

	internal bool IgnoreNullTokensOnRead { get; private protected set; }

	internal bool IgnoreDefaultValuesOnWrite { get; private protected set; }

	internal bool IgnoreReadOnlyMember => MemberType switch
	{
		MemberTypes.Property => Options.IgnoreReadOnlyProperties, 
		MemberTypes.Field => Options.IgnoreReadOnlyFields, 
		_ => false, 
	};

	internal bool IsForTypeInfo { get; set; }

	public string Name
	{
		get
		{
			return _name;
		}
		set
		{
			VerifyMutable();
			if (value == null)
			{
				ThrowHelper.ThrowArgumentNullException("value");
			}
			_name = value;
		}
	}

	internal byte[] NameAsUtf8Bytes { get; set; }

	internal byte[] EscapedNameSection { get; set; }

	public JsonSerializerOptions Options { get; }

	public int Order
	{
		get
		{
			return _order;
		}
		set
		{
			VerifyMutable();
			_order = value;
		}
	}

	internal Type DeclaringType { get; }

	internal JsonTypeInfo JsonTypeInfo
	{
		get
		{
			JsonTypeInfo jsonTypeInfo = _jsonTypeInfo;
			jsonTypeInfo.EnsureConfigured();
			return jsonTypeInfo;
		}
		set
		{
			_jsonTypeInfo = value;
		}
	}

	internal bool IsPropertyTypeInfoConfigured => _jsonTypeInfo?.IsConfigured ?? false;

	internal bool IsIgnored
	{
		get
		{
			JsonIgnoreCondition? ignoreCondition = _ignoreCondition;
			if (ignoreCondition.HasValue && ignoreCondition.GetValueOrDefault() == JsonIgnoreCondition.Always && Get == null)
			{
				return Set == null;
			}
			return false;
		}
	}

	internal bool CanSerialize { get; private set; }

	internal bool CanDeserialize { get; private set; }

	internal bool CanDeserializeOrPopulate { get; private set; }

	internal bool SrcGen_HasJsonInclude { get; set; }

	internal bool SrcGen_IsPublic { get; set; }

	public JsonNumberHandling? NumberHandling
	{
		get
		{
			return _numberHandling;
		}
		set
		{
			VerifyMutable();
			_numberHandling = value;
		}
	}

	internal JsonNumberHandling? EffectiveNumberHandling { get; set; }

	internal abstract bool PropertyTypeCanBeNull { get; }

	[DebuggerBrowsable(DebuggerBrowsableState.Never)]
	internal int RequiredPropertyIndex
	{
		get
		{
			return _index;
		}
		set
		{
			_index = value;
		}
	}

	[DebuggerBrowsable(DebuggerBrowsableState.Never)]
	private string DebuggerDisplay => $"Name = {Name}, PropertyType = {PropertyType}";

	private protected abstract void SetGetter(Delegate getter);

	private protected abstract void SetSetter(Delegate setter);

	private protected abstract void SetShouldSerialize(Delegate predicate);

	private protected abstract void ConfigureIgnoreCondition(JsonIgnoreCondition? ignoreCondition);

	internal JsonPropertyInfo(Type declaringType, Type propertyType, JsonTypeInfo declaringTypeInfo, JsonSerializerOptions options)
	{
		DeclaringType = declaringType;
		PropertyType = propertyType;
		ParentTypeInfo = declaringTypeInfo;
		Options = options;
	}

	internal static JsonPropertyInfo GetPropertyPlaceholder()
	{
		JsonPropertyInfo jsonPropertyInfo = new JsonPropertyInfo<object>(typeof(object), null, null);
		jsonPropertyInfo.Name = string.Empty;
		return jsonPropertyInfo;
	}

	private protected void VerifyMutable()
	{
		ParentTypeInfo?.VerifyMutable();
	}

	internal void Configure()
	{
		if (IsIgnored)
		{
			CanSerialize = false;
			CanDeserialize = false;
		}
		else
		{
			if (_jsonTypeInfo == null)
			{
				_jsonTypeInfo = Options.GetTypeInfoInternal(PropertyType, ensureConfigured: true, true);
			}
			_jsonTypeInfo.EnsureConfigured();
			DetermineEffectiveConverter(_jsonTypeInfo);
			DetermineNumberHandlingForProperty();
			DetermineEffectiveObjectCreationHandlingForProperty();
			DetermineSerializationCapabilities();
			DetermineIgnoreCondition();
		}
		if (IsForTypeInfo)
		{
			DetermineNumberHandlingForTypeInfo();
		}
		else
		{
			CacheNameAsUtf8BytesAndEscapedNameSection();
		}
		if (IsRequired)
		{
			if (!CanDeserialize)
			{
				ThrowHelper.ThrowInvalidOperationException_JsonPropertyRequiredAndNotDeserializable(this);
			}
			if (IsExtensionData)
			{
				ThrowHelper.ThrowInvalidOperationException_JsonPropertyRequiredAndExtensionData(this);
			}
		}
		IsConfigured = true;
	}

	private protected abstract void DetermineEffectiveConverter(JsonTypeInfo jsonTypeInfo);

	[RequiresUnreferencedCode("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext, or make sure all of the required types are preserved.")]
	[RequiresDynamicCode("JSON serialization and deserialization might require types that cannot be statically analyzed and might need runtime code generation. Use System.Text.Json source generation for native AOT applications.")]
	internal abstract void DetermineReflectionPropertyAccessors(MemberInfo memberInfo, bool useNonPublicAccessors);

	private void CacheNameAsUtf8BytesAndEscapedNameSection()
	{
		NameAsUtf8Bytes = Encoding.UTF8.GetBytes(Name);
		EscapedNameSection = JsonHelpers.GetEscapedPropertyNameSection(NameAsUtf8Bytes, Options.Encoder);
	}

	private void DetermineIgnoreCondition()
	{
		if (_ignoreCondition.HasValue)
		{
			return;
		}
		if (Options.IgnoreNullValues)
		{
			if (PropertyTypeCanBeNull)
			{
				IgnoreNullTokensOnRead = !_isUserSpecifiedSetter && !IsRequired;
				IgnoreDefaultValuesOnWrite = ShouldSerialize == null;
			}
		}
		else if (Options.DefaultIgnoreCondition == JsonIgnoreCondition.WhenWritingNull)
		{
			if (PropertyTypeCanBeNull)
			{
				IgnoreDefaultValuesOnWrite = ShouldSerialize == null;
			}
		}
		else if (Options.DefaultIgnoreCondition == JsonIgnoreCondition.WhenWritingDefault)
		{
			IgnoreDefaultValuesOnWrite = ShouldSerialize == null;
		}
	}

	private void DetermineSerializationCapabilities()
	{
		CanSerialize = HasGetter;
		CanDeserialize = HasSetter;
		if (MemberType == (MemberTypes)0 || _ignoreCondition.HasValue)
		{
			CanDeserializeOrPopulate = CanDeserialize || EffectiveObjectCreationHandling == JsonObjectCreationHandling.Populate;
			return;
		}
		if ((EffectiveConverter.ConverterStrategy & (ConverterStrategy)24) != 0)
		{
			if (Get == null && Set != null && !_isUserSpecifiedSetter)
			{
				CanDeserialize = false;
			}
		}
		else if (Get != null && Set == null && IgnoreReadOnlyMember && !_isUserSpecifiedShouldSerialize)
		{
			CanSerialize = false;
		}
		CanDeserializeOrPopulate = CanDeserialize || EffectiveObjectCreationHandling == JsonObjectCreationHandling.Populate;
	}

	private void DetermineNumberHandlingForTypeInfo()
	{
		JsonNumberHandling? numberHandling = ParentTypeInfo.NumberHandling;
		if (numberHandling.HasValue && numberHandling != JsonNumberHandling.Strict && !EffectiveConverter.IsInternalConverter)
		{
			ThrowHelper.ThrowInvalidOperationException_NumberHandlingOnPropertyInvalid(this);
		}
		if (NumberHandingIsApplicable())
		{
			EffectiveNumberHandling = numberHandling;
			if (!EffectiveNumberHandling.HasValue && Options.NumberHandling != 0)
			{
				EffectiveNumberHandling = Options.NumberHandling;
			}
		}
	}

	private void DetermineNumberHandlingForProperty()
	{
		if (NumberHandingIsApplicable())
		{
			JsonNumberHandling? effectiveNumberHandling = NumberHandling ?? ParentTypeInfo.NumberHandling ?? _jsonTypeInfo.NumberHandling;
			if (!effectiveNumberHandling.HasValue && Options.NumberHandling != 0)
			{
				effectiveNumberHandling = Options.NumberHandling;
			}
			EffectiveNumberHandling = effectiveNumberHandling;
		}
		else if (NumberHandling.HasValue && NumberHandling != JsonNumberHandling.Strict)
		{
			ThrowHelper.ThrowInvalidOperationException_NumberHandlingOnPropertyInvalid(this);
		}
	}

	private void DetermineEffectiveObjectCreationHandlingForProperty()
	{
		JsonObjectCreationHandling jsonObjectCreationHandling = JsonObjectCreationHandling.Replace;
		if (!ObjectCreationHandling.HasValue)
		{
			JsonObjectCreationHandling jsonObjectCreationHandling2 = ParentTypeInfo.PreferredPropertyObjectCreationHandling ?? ((!ParentTypeInfo.DetermineUsesParameterizedConstructor()) ? Options.PreferredObjectCreationHandling : JsonObjectCreationHandling.Replace);
			jsonObjectCreationHandling = ((jsonObjectCreationHandling2 == JsonObjectCreationHandling.Populate && EffectiveConverter.CanPopulate && Get != null && (!PropertyType.IsValueType || Set != null) && !ParentTypeInfo.SupportsPolymorphicDeserialization && (Set != null || !IgnoreReadOnlyMember)) ? JsonObjectCreationHandling.Populate : JsonObjectCreationHandling.Replace);
		}
		else if (ObjectCreationHandling == JsonObjectCreationHandling.Populate)
		{
			if (!EffectiveConverter.CanPopulate)
			{
				ThrowHelper.ThrowInvalidOperationException_ObjectCreationHandlingPopulateNotSupportedByConverter(this);
			}
			if (Get == null)
			{
				ThrowHelper.ThrowInvalidOperationException_ObjectCreationHandlingPropertyMustHaveAGetter(this);
			}
			if (PropertyType.IsValueType && Set == null)
			{
				ThrowHelper.ThrowInvalidOperationException_ObjectCreationHandlingPropertyValueTypeMustHaveASetter(this);
			}
			if (JsonTypeInfo.SupportsPolymorphicDeserialization)
			{
				ThrowHelper.ThrowInvalidOperationException_ObjectCreationHandlingPropertyCannotAllowPolymorphicDeserialization(this);
			}
			if (Set == null && IgnoreReadOnlyMember)
			{
				ThrowHelper.ThrowInvalidOperationException_ObjectCreationHandlingPropertyCannotAllowReadOnlyMember(this);
			}
			jsonObjectCreationHandling = JsonObjectCreationHandling.Populate;
		}
		if (jsonObjectCreationHandling == JsonObjectCreationHandling.Populate)
		{
			if (ParentTypeInfo.DetermineUsesParameterizedConstructor())
			{
				ThrowHelper.ThrowNotSupportedException_ObjectCreationHandlingPropertyDoesNotSupportParameterizedConstructors();
			}
			if (Options.ReferenceHandlingStrategy != 0)
			{
				ThrowHelper.ThrowInvalidOperationException_ObjectCreationHandlingPropertyCannotAllowReferenceHandling();
			}
		}
		EffectiveObjectCreationHandling = jsonObjectCreationHandling;
	}

	private bool NumberHandingIsApplicable()
	{
		if (EffectiveConverter.IsInternalConverterForNumberType)
		{
			return true;
		}
		Type type = ((EffectiveConverter.IsInternalConverter && ((ConverterStrategy)24 & EffectiveConverter.ConverterStrategy) != 0) ? EffectiveConverter.ElementType : PropertyType);
		type = Nullable.GetUnderlyingType(type) ?? type;
		if (!(type == typeof(byte)) && !(type == typeof(decimal)) && !(type == typeof(double)) && !(type == typeof(short)) && !(type == typeof(int)) && !(type == typeof(long)) && !(type == typeof(sbyte)) && !(type == typeof(float)) && !(type == typeof(ushort)) && !(type == typeof(uint)) && !(type == typeof(ulong)) && !(type == typeof(Half)) && !(type == typeof(Int128)) && !(type == typeof(UInt128)))
		{
			return type == System.Text.Json.Serialization.Metadata.JsonTypeInfo.ObjectType;
		}
		return true;
	}

	internal abstract JsonParameterInfo CreateJsonParameterInfo(JsonParameterInfoValues parameterInfoValues);

	internal abstract bool GetMemberAndWriteJson(object obj, ref WriteStack state, Utf8JsonWriter writer);

	internal abstract bool GetMemberAndWriteJsonExtensionData(object obj, ref WriteStack state, Utf8JsonWriter writer);

	internal abstract object GetValueAsObject(object obj);

	internal bool ReadJsonAndAddExtensionProperty(object obj, scoped ref ReadStack state, ref Utf8JsonReader reader)
	{
		object valueAsObject = GetValueAsObject(obj);
		if (valueAsObject is IDictionary<string, object> dictionary)
		{
			if (reader.TokenType == JsonTokenType.Null)
			{
				dictionary[state.Current.JsonPropertyNameAsString] = null;
			}
			else
			{
				JsonConverter<object> jsonConverter = GetDictionaryValueConverter<object>();
				object value = jsonConverter.Read(ref reader, System.Text.Json.Serialization.Metadata.JsonTypeInfo.ObjectType, Options);
				dictionary[state.Current.JsonPropertyNameAsString] = value;
			}
		}
		else if (valueAsObject is IDictionary<string, JsonElement> dictionary2)
		{
			JsonConverter<JsonElement> jsonConverter2 = GetDictionaryValueConverter<JsonElement>();
			JsonElement value2 = jsonConverter2.Read(ref reader, typeof(JsonElement), Options);
			dictionary2[state.Current.JsonPropertyNameAsString] = value2;
		}
		else
		{
			EffectiveConverter.ReadElementAndSetProperty(valueAsObject, state.Current.JsonPropertyNameAsString, ref reader, Options, ref state);
		}
		return true;
		JsonConverter<TValue> GetDictionaryValueConverter<TValue>()
		{
			JsonTypeInfo jsonTypeInfo = JsonTypeInfo.ElementTypeInfo ?? Options.GetTypeInfoInternal(typeof(TValue), ensureConfigured: true, true);
			return ((JsonTypeInfo<TValue>)jsonTypeInfo).EffectiveConverter;
		}
	}

	internal abstract bool ReadJsonAndSetMember(object obj, scoped ref ReadStack state, ref Utf8JsonReader reader);

	internal abstract bool ReadJsonAsObject(scoped ref ReadStack state, ref Utf8JsonReader reader, out object value);

	internal bool ReadJsonExtensionDataValue(scoped ref ReadStack state, ref Utf8JsonReader reader, out object value)
	{
		if (JsonTypeInfo.ElementType == System.Text.Json.Serialization.Metadata.JsonTypeInfo.ObjectType && reader.TokenType == JsonTokenType.Null)
		{
			value = null;
			return true;
		}
		JsonConverter<JsonElement> jsonConverter = (JsonConverter<JsonElement>)Options.GetConverterInternal(typeof(JsonElement));
		if (!jsonConverter.TryRead(ref reader, typeof(JsonElement), Options, ref state, out var value2, out var _))
		{
			value = null;
			return false;
		}
		value = value2;
		return true;
	}

	internal void EnsureChildOf(JsonTypeInfo parent)
	{
		if (ParentTypeInfo == null)
		{
			ParentTypeInfo = parent;
		}
		else if (ParentTypeInfo != parent)
		{
			ThrowHelper.ThrowInvalidOperationException_JsonPropertyInfoIsBoundToDifferentJsonTypeInfo(this);
		}
	}

	internal bool TryGetPrePopulatedValue(scoped ref ReadStack state)
	{
		if (EffectiveObjectCreationHandling != JsonObjectCreationHandling.Populate)
		{
			return false;
		}
		object obj = Get(state.Parent.ReturnValue);
		state.Current.ReturnValue = obj;
		state.Current.IsPopulating = obj != null;
		return obj != null;
	}

	internal bool IsOverriddenOrShadowedBy(JsonPropertyInfo other)
	{
		if (MemberName == other.MemberName)
		{
			return DeclaringType.IsAssignableFrom(other.DeclaringType);
		}
		return false;
	}
}
internal sealed class JsonPropertyInfo<T> : JsonPropertyInfo
{
	private Func<object, T> _typedGet;

	private Action<object, T> _typedSet;

	private Func<object, T, bool> _shouldSerializeTyped;

	private JsonConverter<T> _typedEffectiveConverter;

	internal new Func<object, T> Get
	{
		get
		{
			return _typedGet;
		}
		set
		{
			SetGetter(value);
		}
	}

	internal new Action<object, T> Set
	{
		get
		{
			return _typedSet;
		}
		set
		{
			SetSetter(value);
		}
	}

	internal new Func<object, T, bool> ShouldSerialize
	{
		get
		{
			return _shouldSerializeTyped;
		}
		set
		{
			SetShouldSerialize(value);
		}
	}

	internal override bool PropertyTypeCanBeNull => default(T) == null;

	internal new JsonConverter<T> EffectiveConverter => _typedEffectiveConverter;

	internal JsonPropertyInfo(Type declaringType, JsonTypeInfo declaringTypeInfo, JsonSerializerOptions options)
		: base(declaringType, typeof(T), declaringTypeInfo, options)
	{
	}

	private protected override void SetGetter(Delegate getter)
	{
		if ((object)getter == null)
		{
			_typedGet = null;
			_untypedGet = null;
			return;
		}
		Func<object, T> typedGetter = getter as Func<object, T>;
		if (typedGetter != null)
		{
			_typedGet = typedGetter;
			_untypedGet = ((getter is Func<object, object> func) ? func : ((Func<object, object>)((object obj) => typedGetter(obj))));
			return;
		}
		Func<object, object> untypedGet = (Func<object, object>)getter;
		_typedGet = (object obj) => (T)untypedGet(obj);
		_untypedGet = untypedGet;
	}

	private protected override void SetSetter(Delegate setter)
	{
		if ((object)setter == null)
		{
			_typedSet = null;
			_untypedSet = null;
			return;
		}
		Action<object, T> typedSetter = setter as Action<object, T>;
		if (typedSetter != null)
		{
			_typedSet = typedSetter;
			_untypedSet = ((setter is Action<object, object> action) ? action : ((Action<object, object>)delegate(object obj, object value)
			{
				typedSetter(obj, (T)value);
			}));
			return;
		}
		Action<object, object> untypedSet = (Action<object, object>)setter;
		_typedSet = delegate(object obj, T value)
		{
			untypedSet(obj, value);
		};
		_untypedSet = untypedSet;
	}

	private protected override void SetShouldSerialize(Delegate predicate)
	{
		if ((object)predicate == null)
		{
			_shouldSerializeTyped = null;
			_shouldSerialize = null;
			return;
		}
		Func<object, T, bool> typedPredicate = predicate as Func<object, T, bool>;
		if (typedPredicate != null)
		{
			_shouldSerializeTyped = typedPredicate;
			_shouldSerialize = ((typedPredicate is Func<object, object, bool> func) ? func : ((Func<object, object, bool>)((object obj, object value) => typedPredicate(obj, (T)value))));
			return;
		}
		Func<object, object, bool> untypedPredicate = (Func<object, object, bool>)predicate;
		_shouldSerializeTyped = (object obj, T value) => untypedPredicate(obj, value);
		_shouldSerialize = untypedPredicate;
	}

	internal override JsonParameterInfo CreateJsonParameterInfo(JsonParameterInfoValues parameterInfoValues)
	{
		return new JsonParameterInfo<T>(parameterInfoValues, this);
	}

	[RequiresUnreferencedCode("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext, or make sure all of the required types are preserved.")]
	[RequiresDynamicCode("JSON serialization and deserialization might require types that cannot be statically analyzed and might need runtime code generation. Use System.Text.Json source generation for native AOT applications.")]
	internal override void DetermineReflectionPropertyAccessors(MemberInfo memberInfo, bool useNonPublicAccessors)
	{
		DefaultJsonTypeInfoResolver.DeterminePropertyAccessors(this, memberInfo, useNonPublicAccessors);
	}

	private protected override void DetermineEffectiveConverter(JsonTypeInfo jsonTypeInfo)
	{
		_typedEffectiveConverter = (JsonConverter<T>)(_effectiveConverter = base.Options.ExpandConverterFactory(base.CustomConverter, base.PropertyType)?.CreateCastingConverter<T>() ?? ((JsonTypeInfo<T>)jsonTypeInfo).EffectiveConverter);
	}

	internal override object GetValueAsObject(object obj)
	{
		if (base.IsForTypeInfo)
		{
			return obj;
		}
		return Get(obj);
	}

	internal override bool GetMemberAndWriteJson(object obj, ref WriteStack state, Utf8JsonWriter writer)
	{
		T value = Get(obj);
		if (!typeof(T).IsValueType && base.Options.ReferenceHandlingStrategy == ReferenceHandlingStrategy.IgnoreCycles && value != null && !state.IsContinuation && EffectiveConverter.ConverterStrategy != ConverterStrategy.Value && state.ReferenceResolver.ContainsReferenceForCycleDetection(value))
		{
			value = default(T);
		}
		if (base.IgnoreDefaultValuesOnWrite)
		{
			if (IsDefaultValue(value))
			{
				return true;
			}
		}
		else
		{
			Func<object, T, bool> shouldSerialize = ShouldSerialize;
			if (shouldSerialize != null && !shouldSerialize(obj, value))
			{
				return true;
			}
		}
		if (value == null)
		{
			if (EffectiveConverter.HandleNullOnWrite)
			{
				if ((int)state.Current.PropertyState < 2)
				{
					state.Current.PropertyState = StackFramePropertyState.Name;
					writer.WritePropertyNameSection(base.EscapedNameSection);
				}
				int currentDepth = writer.CurrentDepth;
				EffectiveConverter.Write(writer, value, base.Options);
				if (currentDepth != writer.CurrentDepth)
				{
					ThrowHelper.ThrowJsonException_SerializationConverterWrite(EffectiveConverter);
				}
			}
			else
			{
				writer.WriteNullSection(base.EscapedNameSection);
			}
			return true;
		}
		if ((int)state.Current.PropertyState < 2)
		{
			state.Current.PropertyState = StackFramePropertyState.Name;
			writer.WritePropertyNameSection(base.EscapedNameSection);
		}
		return EffectiveConverter.TryWrite(writer, in value, base.Options, ref state);
	}

	internal override bool GetMemberAndWriteJsonExtensionData(object obj, ref WriteStack state, Utf8JsonWriter writer)
	{
		T val = Get(obj);
		Func<object, T, bool> shouldSerialize = ShouldSerialize;
		if (shouldSerialize != null && !shouldSerialize(obj, val))
		{
			return true;
		}
		if (val == null)
		{
			return true;
		}
		return EffectiveConverter.TryWriteDataExtensionProperty(writer, val, base.Options, ref state);
	}

	internal override bool ReadJsonAndSetMember(object obj, scoped ref ReadStack state, ref Utf8JsonReader reader)
	{
		bool flag = reader.TokenType == JsonTokenType.Null;
		bool flag2;
		if (flag && !EffectiveConverter.HandleNullOnRead && !state.IsContinuation)
		{
			if (default(T) != null || !base.CanDeserialize)
			{
				if (default(T) == null)
				{
					ThrowHelper.ThrowInvalidOperationException_DeserializeUnableToAssignNull(EffectiveConverter.Type);
				}
				ThrowHelper.ThrowJsonException_DeserializeUnableToConvertValue(EffectiveConverter.Type);
			}
			if (!base.IgnoreNullTokensOnRead)
			{
				Set(obj, default(T));
			}
			flag2 = true;
			state.Current.MarkRequiredPropertyAsRead(this);
		}
		else if (EffectiveConverter.CanUseDirectReadOrWrite && !state.Current.NumberHandling.HasValue)
		{
			if (!flag || !base.IgnoreNullTokensOnRead || default(T) != null)
			{
				T arg = EffectiveConverter.Read(ref reader, base.PropertyType, base.Options);
				Set(obj, arg);
			}
			flag2 = true;
			state.Current.MarkRequiredPropertyAsRead(this);
		}
		else
		{
			flag2 = true;
			if (!flag || !base.IgnoreNullTokensOnRead || default(T) != null || state.IsContinuation)
			{
				state.Current.ReturnValue = obj;
				flag2 = EffectiveConverter.TryRead(ref reader, base.PropertyType, base.Options, ref state, out var value, out var isPopulatedValue);
				if (flag2)
				{
					if ((typeof(T).IsValueType || !isPopulatedValue) && base.CanDeserialize)
					{
						Set(obj, value);
					}
					state.Current.MarkRequiredPropertyAsRead(this);
				}
			}
		}
		return flag2;
	}

	internal override bool ReadJsonAsObject(scoped ref ReadStack state, ref Utf8JsonReader reader, out object value)
	{
		bool result;
		if (reader.TokenType == JsonTokenType.Null && !EffectiveConverter.HandleNullOnRead && !state.IsContinuation)
		{
			if (default(T) != null)
			{
				ThrowHelper.ThrowJsonException_DeserializeUnableToConvertValue(EffectiveConverter.Type);
			}
			value = default(T);
			result = true;
		}
		else if (EffectiveConverter.CanUseDirectReadOrWrite && !state.Current.NumberHandling.HasValue)
		{
			value = EffectiveConverter.Read(ref reader, base.PropertyType, base.Options);
			result = true;
		}
		else
		{
			result = EffectiveConverter.TryRead(ref reader, base.PropertyType, base.Options, ref state, out var value2, out var _);
			value = value2;
		}
		return result;
	}

	private protected override void ConfigureIgnoreCondition(JsonIgnoreCondition? ignoreCondition)
	{
		if (!ignoreCondition.HasValue)
		{
			return;
		}
		switch (ignoreCondition.GetValueOrDefault())
		{
		case JsonIgnoreCondition.Never:
			ShouldSerialize = ShouldSerializeIgnoreConditionNever;
			break;
		case JsonIgnoreCondition.Always:
			ShouldSerialize = ShouldSerializeIgnoreConditionAlways;
			break;
		case JsonIgnoreCondition.WhenWritingNull:
			if (PropertyTypeCanBeNull)
			{
				ShouldSerialize = ShouldSerializeIgnoreWhenWritingDefault;
				base.IgnoreDefaultValuesOnWrite = true;
			}
			else
			{
				ThrowHelper.ThrowInvalidOperationException_IgnoreConditionOnValueTypeInvalid(base.MemberName, base.DeclaringType);
			}
			break;
		case JsonIgnoreCondition.WhenWritingDefault:
			ShouldSerialize = ShouldSerializeIgnoreWhenWritingDefault;
			base.IgnoreDefaultValuesOnWrite = true;
			break;
		}
		static bool ShouldSerializeIgnoreConditionAlways(object _, T value)
		{
			return false;
		}
		static bool ShouldSerializeIgnoreConditionNever(object _, T value)
		{
			return true;
		}
		static bool ShouldSerializeIgnoreWhenWritingDefault(object _, T value)
		{
			if (default(T) != null)
			{
				return !EqualityComparer<T>.Default.Equals(default(T), value);
			}
			return value != null;
		}
	}

	private static bool IsDefaultValue(T value)
	{
		if (default(T) != null)
		{
			return EqualityComparer<T>.Default.Equals(default(T), value);
		}
		return value == null;
	}
}
