using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Text.Json.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace System.Text.Json.Serialization.Metadata;

public sealed class JsonTypeInfo<T> : JsonTypeInfo
{
	internal JsonTypeInfo _asyncEnumerableQueueTypeInfo;

	private volatile int _canUseSerializeHandlerInStreamingState;

	private volatile int _serializationCount;

	private Action<Utf8JsonWriter, T> _serialize;

	private Func<T> _typedCreateObject;

	private bool CanUseSerializeHandlerInStreaming => _canUseSerializeHandlerInStreamingState == 1;

	internal JsonConverter<T> EffectiveConverter { get; }

	public new Func<T>? CreateObject
	{
		get
		{
			return _typedCreateObject;
		}
		set
		{
			SetCreateObject(value);
		}
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	public Action<Utf8JsonWriter, T>? SerializeHandler
	{
		get
		{
			return _serialize;
		}
		internal set
		{
			_serialize = value;
			base.HasSerializeHandler = value != null;
		}
	}

	internal T Deserialize(ref Utf8JsonReader reader, ref ReadStack state)
	{
		return EffectiveConverter.ReadCore(ref reader, base.Options, ref state);
	}

	internal async ValueTask<T> DeserializeAsync(Stream utf8Json, CancellationToken cancellationToken)
	{
		JsonSerializerOptions options = base.Options;
		ReadBufferState bufferState = new ReadBufferState(options.DefaultBufferSize);
		ReadStack readStack = default(ReadStack);
		readStack.Initialize(this, supportContinuation: true);
		JsonReaderState jsonReaderState = new JsonReaderState(options.GetReaderOptions());
		try
		{
			T result;
			do
			{
				bufferState = await bufferState.ReadFromStreamAsync(utf8Json, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
				result = ContinueDeserialize(ref bufferState, ref jsonReaderState, ref readStack);
			}
			while (!bufferState.IsFinalBlock);
			return result;
		}
		finally
		{
			bufferState.Dispose();
		}
	}

	internal T Deserialize(Stream utf8Json)
	{
		JsonSerializerOptions options = base.Options;
		ReadBufferState bufferState = new ReadBufferState(options.DefaultBufferSize);
		ReadStack readStack = default(ReadStack);
		readStack.Initialize(this, supportContinuation: true);
		JsonReaderState jsonReaderState = new JsonReaderState(options.GetReaderOptions());
		try
		{
			T result;
			do
			{
				bufferState.ReadFromStream(utf8Json);
				result = ContinueDeserialize(ref bufferState, ref jsonReaderState, ref readStack);
			}
			while (!bufferState.IsFinalBlock);
			return result;
		}
		finally
		{
			bufferState.Dispose();
		}
	}

	internal sealed override object DeserializeAsObject(ref Utf8JsonReader reader, ref ReadStack state)
	{
		return Deserialize(ref reader, ref state);
	}

	internal sealed override async ValueTask<object> DeserializeAsObjectAsync(Stream utf8Json, CancellationToken cancellationToken)
	{
		return await DeserializeAsync(utf8Json, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
	}

	internal sealed override object DeserializeAsObject(Stream utf8Json)
	{
		return Deserialize(utf8Json);
	}

	internal T ContinueDeserialize(ref ReadBufferState bufferState, ref JsonReaderState jsonReaderState, ref ReadStack readStack)
	{
		Utf8JsonReader reader = new Utf8JsonReader(bufferState.Bytes, bufferState.IsFinalBlock, jsonReaderState);
		readStack.ReadAhead = !bufferState.IsFinalBlock;
		readStack.BytesConsumed = 0L;
		T result = EffectiveConverter.ReadCore(ref reader, base.Options, ref readStack);
		bufferState.AdvanceBuffer((int)readStack.BytesConsumed);
		jsonReaderState = reader.CurrentState;
		return result;
	}

	internal void Serialize(Utf8JsonWriter writer, in T rootValue, object rootValueBoxed = null)
	{
		if (base.CanUseSerializeHandler)
		{
			SerializeHandler(writer, rootValue);
			writer.Flush();
			return;
		}
		if (!typeof(T).IsValueType && base.Converter.CanBePolymorphic && rootValue != null && base.Options.TryGetPolymorphicTypeInfoForRootType(rootValue, out var polymorphicTypeInfo))
		{
			polymorphicTypeInfo.SerializeAsObject(writer, rootValue);
			return;
		}
		WriteStack state = default(WriteStack);
		state.Initialize(this, rootValueBoxed);
		bool flag = EffectiveConverter.WriteCore(writer, in rootValue, base.Options, ref state);
		writer.Flush();
	}

	internal async Task SerializeAsync(Stream utf8Json, T rootValue, CancellationToken cancellationToken, object rootValueBoxed = null)
	{
		if (CanUseSerializeHandlerInStreaming)
		{
			using (PooledByteBufferWriter bufferWriter = new PooledByteBufferWriter(base.Options.DefaultBufferSize))
			{
				Utf8JsonWriter utf8JsonWriter = Utf8JsonWriterCache.RentWriter(base.Options, bufferWriter);
				try
				{
					SerializeHandler(utf8JsonWriter, rootValue);
					utf8JsonWriter.Flush();
				}
				finally
				{
					OnRootLevelAsyncSerializationCompleted(utf8JsonWriter.BytesCommitted + utf8JsonWriter.BytesPending);
					Utf8JsonWriterCache.ReturnWriter(utf8JsonWriter);
				}
				await bufferWriter.WriteToStreamAsync(utf8Json, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
			}
			return;
		}
		if (!typeof(T).IsValueType && base.Converter.CanBePolymorphic && rootValue != null && base.Options.TryGetPolymorphicTypeInfoForRootType(rootValue, out var polymorphicTypeInfo))
		{
			await polymorphicTypeInfo.SerializeAsObjectAsync(utf8Json, rootValue, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
			return;
		}
		WriteStack state = default(WriteStack);
		state.Initialize(this, rootValueBoxed, supportContinuation: true, supportAsync: true);
		state.CancellationToken = cancellationToken;
		using PooledByteBufferWriter bufferWriter = new PooledByteBufferWriter(base.Options.DefaultBufferSize);
		using Utf8JsonWriter writer = new Utf8JsonWriter(bufferWriter, base.Options.GetWriterOptions());
		try
		{
			bool isFinalBlock;
			do
			{
				state.FlushThreshold = (int)((float)bufferWriter.Capacity * 0.9f);
				try
				{
					isFinalBlock = EffectiveConverter.WriteCore(writer, in rootValue, base.Options, ref state);
					writer.Flush();
					if (state.SuppressFlush)
					{
						state.SuppressFlush = false;
						continue;
					}
					await bufferWriter.WriteToStreamAsync(utf8Json, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
					bufferWriter.Clear();
				}
				finally
				{
					if (state.PendingTask != null)
					{
						await state.PendingTask.ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
					}
					List<IAsyncDisposable> completedAsyncDisposables = state.CompletedAsyncDisposables;
					if (completedAsyncDisposables != null && completedAsyncDisposables.Count > 0)
					{
						await state.DisposeCompletedAsyncDisposables().ConfigureAwait(continueOnCapturedContext: false);
					}
				}
			}
			while (!isFinalBlock);
		}
		catch
		{
			await state.DisposePendingDisposablesOnExceptionAsync().ConfigureAwait(continueOnCapturedContext: false);
			throw;
		}
		if (base.CanUseSerializeHandler)
		{
			OnRootLevelAsyncSerializationCompleted(writer.BytesCommitted);
		}
	}

	internal void Serialize(Stream utf8Json, in T rootValue, object rootValueBoxed = null)
	{
		if (CanUseSerializeHandlerInStreaming)
		{
			PooledByteBufferWriter bufferWriter;
			Utf8JsonWriter utf8JsonWriter = Utf8JsonWriterCache.RentWriterAndBuffer(base.Options, out bufferWriter);
			try
			{
				SerializeHandler(utf8JsonWriter, rootValue);
				utf8JsonWriter.Flush();
				bufferWriter.WriteToStream(utf8Json);
				return;
			}
			finally
			{
				OnRootLevelAsyncSerializationCompleted(utf8JsonWriter.BytesCommitted + utf8JsonWriter.BytesPending);
				Utf8JsonWriterCache.ReturnWriterAndBuffer(utf8JsonWriter, bufferWriter);
			}
		}
		if (!typeof(T).IsValueType && base.Converter.CanBePolymorphic && rootValue != null && base.Options.TryGetPolymorphicTypeInfoForRootType(rootValue, out var polymorphicTypeInfo))
		{
			polymorphicTypeInfo.SerializeAsObject(utf8Json, rootValue);
			return;
		}
		WriteStack state = default(WriteStack);
		state.Initialize(this, rootValueBoxed, supportContinuation: true);
		using PooledByteBufferWriter pooledByteBufferWriter = new PooledByteBufferWriter(base.Options.DefaultBufferSize);
		using Utf8JsonWriter utf8JsonWriter2 = new Utf8JsonWriter(pooledByteBufferWriter, base.Options.GetWriterOptions());
		bool flag;
		do
		{
			state.FlushThreshold = (int)((float)pooledByteBufferWriter.Capacity * 0.9f);
			flag = EffectiveConverter.WriteCore(utf8JsonWriter2, in rootValue, base.Options, ref state);
			utf8JsonWriter2.Flush();
			pooledByteBufferWriter.WriteToStream(utf8Json);
			pooledByteBufferWriter.Clear();
		}
		while (!flag);
		if (base.CanUseSerializeHandler)
		{
			OnRootLevelAsyncSerializationCompleted(utf8JsonWriter2.BytesCommitted);
		}
	}

	internal sealed override void SerializeAsObject(Utf8JsonWriter writer, object rootValue)
	{
		T rootValue2 = JsonSerializer.UnboxOnWrite<T>(rootValue);
		Serialize(writer, in rootValue2, rootValue);
	}

	internal sealed override Task SerializeAsObjectAsync(Stream utf8Json, object rootValue, CancellationToken cancellationToken)
	{
		return SerializeAsync(utf8Json, JsonSerializer.UnboxOnWrite<T>(rootValue), cancellationToken, rootValue);
	}

	internal sealed override void SerializeAsObject(Stream utf8Json, object rootValue)
	{
		T rootValue2 = JsonSerializer.UnboxOnWrite<T>(rootValue);
		Serialize(utf8Json, in rootValue2, rootValue);
	}

	private void OnRootLevelAsyncSerializationCompleted(long serializationSize)
	{
		if (_canUseSerializeHandlerInStreamingState != 2)
		{
			if ((ulong)serializationSize > (ulong)(base.Options.DefaultBufferSize / 2))
			{
				_canUseSerializeHandlerInStreamingState = 2;
			}
			else if ((uint)_serializationCount < 10u && Interlocked.Increment(ref _serializationCount) == 10)
			{
				Interlocked.CompareExchange(ref _canUseSerializeHandlerInStreamingState, 1, 0);
			}
		}
	}

	internal JsonTypeInfo(JsonConverter converter, JsonSerializerOptions options)
		: base(typeof(T), converter, options)
	{
		EffectiveConverter = converter.CreateCastingConverter<T>();
	}

	private protected override void SetCreateObject(Delegate createObject)
	{
		VerifyMutable();
		if (base.Kind == JsonTypeInfoKind.None)
		{
			ThrowHelper.ThrowInvalidOperationException_JsonTypeInfoOperationNotPossibleForKind(base.Kind);
		}
		if (!base.Converter.SupportsCreateObjectDelegate)
		{
			ThrowHelper.ThrowInvalidOperationException_CreateObjectConverterNotCompatible(base.Type);
		}
		Func<object> untypedCreateObject;
		Func<T> typedCreateObject;
		if ((object)createObject == null)
		{
			untypedCreateObject = null;
			typedCreateObject = null;
		}
		else
		{
			Func<T> typedDelegate = createObject as Func<T>;
			if (typedDelegate != null)
			{
				typedCreateObject = typedDelegate;
				untypedCreateObject = ((createObject is Func<object> func) ? func : ((Func<object>)(() => typedDelegate())));
			}
			else
			{
				untypedCreateObject = (Func<object>)createObject;
				typedCreateObject = () => (T)untypedCreateObject();
			}
		}
		_createObject = untypedCreateObject;
		_typedCreateObject = typedCreateObject;
	}

	private protected override JsonPropertyInfo CreatePropertyInfoForTypeInfo()
	{
		return new JsonPropertyInfo<T>(typeof(T), this, base.Options)
		{
			JsonTypeInfo = this,
			IsForTypeInfo = true
		};
	}

	private protected override JsonPropertyInfo CreateJsonPropertyInfo(JsonTypeInfo declaringTypeInfo, Type declaringType, JsonSerializerOptions options)
	{
		return new JsonPropertyInfo<T>(declaringType ?? declaringTypeInfo.Type, declaringTypeInfo, options)
		{
			JsonTypeInfo = this
		};
	}
}
[DebuggerDisplay("{DebuggerDisplay,nq}")]
public abstract class JsonTypeInfo
{
	internal delegate T ParameterizedConstructorDelegate<T, TArg0, TArg1, TArg2, TArg3>(TArg0 arg0, TArg1 arg1, TArg2 arg2, TArg3 arg3);

	private enum ConfigurationState : byte
	{
		NotConfigured,
		Configuring,
		Configured
	}

	internal ref struct PropertyHierarchyResolutionState
	{
		public Dictionary<string, (JsonPropertyInfo, int index)> AddedProperties;

		public Dictionary<string, JsonPropertyInfo> IgnoredProperties;

		public bool IsPropertyOrderSpecified;

		public PropertyHierarchyResolutionState(JsonSerializerOptions options)
		{
			IgnoredProperties = null;
			IsPropertyOrderSpecified = false;
			AddedProperties = new Dictionary<string, (JsonPropertyInfo, int)>(options.PropertyNameCaseInsensitive ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal);
		}
	}

	private sealed class ParameterLookupKey
	{
		public string Name { get; }

		public Type Type { get; }

		public ParameterLookupKey(string name, Type type)
		{
			Name = name;
			Type = type;
		}

		public override int GetHashCode()
		{
			return StringComparer.OrdinalIgnoreCase.GetHashCode(Name);
		}

		public override bool Equals([NotNullWhen(true)] object obj)
		{
			ParameterLookupKey parameterLookupKey = (ParameterLookupKey)obj;
			if (Type == parameterLookupKey.Type)
			{
				return string.Equals(Name, parameterLookupKey.Name, StringComparison.OrdinalIgnoreCase);
			}
			return false;
		}
	}

	private sealed class ParameterLookupValue
	{
		public string DuplicateName { get; set; }

		public JsonPropertyInfo JsonPropertyInfo { get; }

		public ParameterLookupValue(JsonPropertyInfo jsonPropertyInfo)
		{
			JsonPropertyInfo = jsonPropertyInfo;
		}
	}

	internal sealed class JsonPropertyInfoList : ConfigurationList<JsonPropertyInfo>
	{
		private readonly JsonTypeInfo _jsonTypeInfo;

		public override bool IsReadOnly
		{
			get
			{
				if (_jsonTypeInfo._properties != this || !_jsonTypeInfo.IsReadOnly)
				{
					return _jsonTypeInfo.Kind != JsonTypeInfoKind.Object;
				}
				return true;
			}
		}

		public JsonPropertyInfoList(JsonTypeInfo jsonTypeInfo)
			: base((IEnumerable<JsonPropertyInfo>)null)
		{
			_jsonTypeInfo = jsonTypeInfo;
		}

		protected override void OnCollectionModifying()
		{
			if (_jsonTypeInfo._properties == this)
			{
				_jsonTypeInfo.VerifyMutable();
			}
			if (_jsonTypeInfo.Kind != JsonTypeInfoKind.Object)
			{
				ThrowHelper.ThrowInvalidOperationException_JsonTypeInfoOperationNotPossibleForKind(_jsonTypeInfo.Kind);
			}
		}

		protected override void ValidateAddedValue(JsonPropertyInfo item)
		{
			item.EnsureChildOf(_jsonTypeInfo);
		}

		public void SortProperties()
		{
			_list.StableSortByKey((JsonPropertyInfo propInfo) => propInfo.Order);
		}

		public void AddPropertyWithConflictResolution(JsonPropertyInfo jsonPropertyInfo, ref PropertyHierarchyResolutionState state)
		{
			string memberName = jsonPropertyInfo.MemberName;
			if (state.AddedProperties.TryAdd(jsonPropertyInfo.Name, (jsonPropertyInfo, base.Count)))
			{
				Add(jsonPropertyInfo);
				state.IsPropertyOrderSpecified |= jsonPropertyInfo.Order != 0;
			}
			else
			{
				var (jsonPropertyInfo2, num) = state.AddedProperties[jsonPropertyInfo.Name];
				if (jsonPropertyInfo2.IsIgnored)
				{
					state.AddedProperties[jsonPropertyInfo.Name] = (jsonPropertyInfo, num);
					base[num] = jsonPropertyInfo;
					state.IsPropertyOrderSpecified |= jsonPropertyInfo.Order != 0;
				}
				else if (!jsonPropertyInfo.IsIgnored && !jsonPropertyInfo.IsOverriddenOrShadowedBy(jsonPropertyInfo2))
				{
					Dictionary<string, JsonPropertyInfo> ignoredProperties = state.IgnoredProperties;
					if (ignoredProperties == null || !ignoredProperties.TryGetValue(memberName, out var value) || !jsonPropertyInfo.IsOverriddenOrShadowedBy(value))
					{
						ThrowHelper.ThrowInvalidOperationException_SerializerPropertyNameConflict(_jsonTypeInfo.Type, jsonPropertyInfo.Name);
					}
				}
			}
			if (jsonPropertyInfo.IsIgnored)
			{
				ref Dictionary<string, JsonPropertyInfo> ignoredProperties2 = ref state.IgnoredProperties;
				(ignoredProperties2 ?? (ignoredProperties2 = new Dictionary<string, JsonPropertyInfo>()))[memberName] = jsonPropertyInfo;
			}
		}
	}

	internal static readonly Type ObjectType = typeof(object);

	private volatile ParameterRef[] _parameterRefsSorted;

	private volatile PropertyRef[] _propertyRefsSorted;

	private Action<object> _onSerializing;

	private Action<object> _onSerialized;

	private Action<object> _onDeserializing;

	private Action<object> _onDeserialized;

	private protected Func<object> _createObject;

	private Func<JsonSerializerContext, JsonPropertyInfo[]> _sourceGenDelayedPropertyInitializer;

	private JsonPropertyInfoList _properties;

	private protected JsonPolymorphismOptions _polymorphismOptions;

	private JsonTypeInfo _elementTypeInfo;

	private JsonTypeInfo _keyTypeInfo;

	private JsonNumberHandling? _numberHandling;

	private JsonUnmappedMemberHandling? _unmappedMemberHandling;

	private JsonObjectCreationHandling? _preferredPropertyObjectCreationHandling;

	private IJsonTypeInfoResolver _originatingResolver;

	private volatile ConfigurationState _configurationState;

	private ExceptionDispatchInfo _cachedConfigureError;

	private JsonTypeInfo _ancestorPolymorhicType;

	private volatile bool _isAncestorPolymorphicTypeResolved;

	internal int ParameterCount { get; private set; }

	internal JsonPropertyDictionary<JsonParameterInfo>? ParameterCache { get; private set; }

	internal bool UsesParameterizedConstructor => ParameterCache != null;

	internal JsonPropertyDictionary<JsonPropertyInfo>? PropertyCache { get; private set; }

	internal int NumberOfRequiredProperties { get; private set; }

	public Func<object>? CreateObject
	{
		get
		{
			return _createObject;
		}
		set
		{
			SetCreateObject(value);
		}
	}

	internal Func<object>? CreateObjectForExtensionDataProperty { get; set; }

	public Action<object>? OnSerializing
	{
		get
		{
			return _onSerializing;
		}
		set
		{
			VerifyMutable();
			if (Kind != JsonTypeInfoKind.Object)
			{
				ThrowHelper.ThrowInvalidOperationException_JsonTypeInfoOperationNotPossibleForKind(Kind);
			}
			_onSerializing = value;
		}
	}

	public Action<object>? OnSerialized
	{
		get
		{
			return _onSerialized;
		}
		set
		{
			VerifyMutable();
			if (Kind != JsonTypeInfoKind.Object)
			{
				ThrowHelper.ThrowInvalidOperationException_JsonTypeInfoOperationNotPossibleForKind(Kind);
			}
			_onSerialized = value;
		}
	}

	public Action<object>? OnDeserializing
	{
		get
		{
			return _onDeserializing;
		}
		set
		{
			VerifyMutable();
			if (Kind != JsonTypeInfoKind.Object)
			{
				ThrowHelper.ThrowInvalidOperationException_JsonTypeInfoOperationNotPossibleForKind(Kind);
			}
			_onDeserializing = value;
		}
	}

	public Action<object>? OnDeserialized
	{
		get
		{
			return _onDeserialized;
		}
		set
		{
			VerifyMutable();
			if (Kind != JsonTypeInfoKind.Object)
			{
				ThrowHelper.ThrowInvalidOperationException_JsonTypeInfoOperationNotPossibleForKind(Kind);
			}
			_onDeserialized = value;
		}
	}

	public IList<JsonPropertyInfo> Properties => PropertyList;

	[DebuggerBrowsable(DebuggerBrowsableState.Never)]
	internal JsonPropertyInfoList PropertyList
	{
		get
		{
			return _properties ?? CreatePropertyList();
			JsonPropertyInfoList CreatePropertyList()
			{
				JsonPropertyInfoList jsonPropertyInfoList = new JsonPropertyInfoList(this);
				Func<JsonSerializerContext, JsonPropertyInfo[]> sourceGenDelayedPropertyInitializer = _sourceGenDelayedPropertyInitializer;
				if (sourceGenDelayedPropertyInitializer != null)
				{
					JsonMetadataServices.PopulateProperties(this, jsonPropertyInfoList, sourceGenDelayedPropertyInitializer);
				}
				JsonPropertyInfoList jsonPropertyInfoList2 = Interlocked.CompareExchange(ref _properties, jsonPropertyInfoList, null);
				_sourceGenDelayedPropertyInitializer = null;
				return jsonPropertyInfoList2 ?? jsonPropertyInfoList;
			}
		}
	}

	internal Func<JsonSerializerContext, JsonPropertyInfo[]>? SourceGenDelayedPropertyInitializer
	{
		set
		{
			_sourceGenDelayedPropertyInitializer = value;
		}
	}

	public JsonPolymorphismOptions? PolymorphismOptions
	{
		get
		{
			return _polymorphismOptions;
		}
		set
		{
			VerifyMutable();
			if (value != null)
			{
				if (Kind == JsonTypeInfoKind.None)
				{
					ThrowHelper.ThrowInvalidOperationException_JsonTypeInfoOperationNotPossibleForKind(Kind);
				}
				if (value.DeclaringTypeInfo != null && value.DeclaringTypeInfo != this)
				{
					ThrowHelper.ThrowArgumentException_JsonPolymorphismOptionsAssociatedWithDifferentJsonTypeInfo("value");
				}
				value.DeclaringTypeInfo = this;
			}
			_polymorphismOptions = value;
		}
	}

	public bool IsReadOnly { get; private set; }

	internal object? CreateObjectWithArgs { get; set; }

	internal object? AddMethodDelegate { get; set; }

	internal JsonPropertyInfo? ExtensionDataProperty { get; private set; }

	internal PolymorphicTypeResolver? PolymorphicTypeResolver { get; private set; }

	internal bool HasSerializeHandler { get; private protected set; }

	internal bool CanUseSerializeHandler { get; private set; }

	internal bool PropertyMetadataSerializationNotSupported { get; set; }

	internal Type? ElementType { get; }

	internal Type? KeyType { get; }

	[DebuggerBrowsable(DebuggerBrowsableState.Never)]
	internal JsonTypeInfo? ElementTypeInfo
	{
		get
		{
			JsonTypeInfo elementTypeInfo = _elementTypeInfo;
			elementTypeInfo?.EnsureConfigured();
			return elementTypeInfo;
		}
		set
		{
			_elementTypeInfo = value;
		}
	}

	[DebuggerBrowsable(DebuggerBrowsableState.Never)]
	internal JsonTypeInfo? KeyTypeInfo
	{
		get
		{
			JsonTypeInfo keyTypeInfo = _keyTypeInfo;
			keyTypeInfo?.EnsureConfigured();
			return keyTypeInfo;
		}
		set
		{
			_keyTypeInfo = value;
		}
	}

	public JsonSerializerOptions Options { get; }

	public Type Type { get; }

	public JsonConverter Converter { get; }

	public JsonTypeInfoKind Kind { get; private set; }

	internal JsonPropertyInfo PropertyInfoForTypeInfo { get; }

	public JsonNumberHandling? NumberHandling
	{
		get
		{
			return _numberHandling;
		}
		set
		{
			VerifyMutable();
			if (value.HasValue && !JsonSerializer.IsValidNumberHandlingValue(value.Value))
			{
				throw new ArgumentOutOfRangeException("value");
			}
			_numberHandling = value;
		}
	}

	public JsonUnmappedMemberHandling? UnmappedMemberHandling
	{
		get
		{
			return _unmappedMemberHandling;
		}
		set
		{
			VerifyMutable();
			if (Kind != JsonTypeInfoKind.Object)
			{
				ThrowHelper.ThrowInvalidOperationException_JsonTypeInfoOperationNotPossibleForKind(Kind);
			}
			if (value.HasValue && !JsonSerializer.IsValidUnmappedMemberHandlingValue(value.Value))
			{
				throw new ArgumentOutOfRangeException("value");
			}
			_unmappedMemberHandling = value;
		}
	}

	internal JsonUnmappedMemberHandling EffectiveUnmappedMemberHandling { get; private set; }

	public JsonObjectCreationHandling? PreferredPropertyObjectCreationHandling
	{
		get
		{
			return _preferredPropertyObjectCreationHandling;
		}
		set
		{
			VerifyMutable();
			if (Kind != JsonTypeInfoKind.Object)
			{
				ThrowHelper.ThrowInvalidOperationException_JsonTypeInfoOperationNotPossibleForKind(Kind);
			}
			if (value.HasValue && !JsonSerializer.IsValidCreationHandlingValue(value.Value))
			{
				throw new ArgumentOutOfRangeException("value");
			}
			_preferredPropertyObjectCreationHandling = value;
		}
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	public IJsonTypeInfoResolver? OriginatingResolver
	{
		get
		{
			return _originatingResolver;
		}
		set
		{
			VerifyMutable();
			if (value is JsonSerializerContext)
			{
				IsCustomized = false;
			}
			_originatingResolver = value;
		}
	}

	internal bool IsCustomized { get; set; } = true;


	internal bool IsConfigured => _configurationState == ConfigurationState.Configured;

	[DebuggerBrowsable(DebuggerBrowsableState.Never)]
	internal JsonTypeInfo? AncestorPolymorphicType
	{
		get
		{
			if (!_isAncestorPolymorphicTypeResolved)
			{
				_ancestorPolymorhicType = System.Text.Json.Serialization.Metadata.PolymorphicTypeResolver.FindNearestPolymorphicBaseType(this);
				_isAncestorPolymorphicTypeResolved = true;
			}
			return _ancestorPolymorhicType;
		}
	}

	private bool IsCompatibleWithCurrentOptions { get; set; } = true;


	internal JsonParameterInfoValues[]? ParameterInfoValues { get; set; }

	[DebuggerBrowsable(DebuggerBrowsableState.Never)]
	internal bool SupportsPolymorphicDeserialization => PolymorphicTypeResolver?.UsesTypeDiscriminators ?? false;

	[DebuggerBrowsable(DebuggerBrowsableState.Never)]
	private string DebuggerDisplay => $"Type = {Type.Name}, Kind = {Kind}";

	[RequiresDynamicCode("JSON serialization and deserialization might require types that cannot be statically analyzed and might need runtime code generation. Use System.Text.Json source generation for native AOT applications.")]
	[RequiresUnreferencedCode("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext, or make sure all of the required types are preserved.")]
	internal JsonPropertyInfo CreatePropertyUsingReflection(Type propertyType, Type declaringType)
	{
		if (Options.TryGetTypeInfoCached(propertyType, out var typeInfo))
		{
			return typeInfo.CreateJsonPropertyInfo(this, declaringType, Options);
		}
		Type type = typeof(JsonPropertyInfo<>).MakeGenericType(propertyType);
		return (JsonPropertyInfo)type.CreateInstanceNoWrapExceptions(new Type[3]
		{
			typeof(Type),
			typeof(JsonTypeInfo),
			typeof(JsonSerializerOptions)
		}, new object[3]
		{
			declaringType ?? Type,
			this,
			Options
		});
	}

	private protected abstract JsonPropertyInfo CreateJsonPropertyInfo(JsonTypeInfo declaringTypeInfo, Type declaringType, JsonSerializerOptions options);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal JsonPropertyInfo GetProperty(ReadOnlySpan<byte> propertyName, ref ReadStackFrame frame, out byte[] utf8PropertyName)
	{
		ValidateCanBeUsedForPropertyMetadataSerialization();
		ulong key = GetKey(propertyName);
		PropertyRef[] propertyRefsSorted = _propertyRefsSorted;
		if (propertyRefsSorted != null)
		{
			int propertyIndex = frame.PropertyIndex;
			int num = propertyRefsSorted.Length;
			int num2 = Math.Min(propertyIndex, num);
			int num3 = num2 - 1;
			while (true)
			{
				if (num2 < num)
				{
					PropertyRef propertyRef = propertyRefsSorted[num2];
					if (IsPropertyRefEqual(in propertyRef, propertyName, key))
					{
						utf8PropertyName = propertyRef.NameFromJson;
						return propertyRef.Info;
					}
					num2++;
					if (num3 >= 0)
					{
						propertyRef = propertyRefsSorted[num3];
						if (IsPropertyRefEqual(in propertyRef, propertyName, key))
						{
							utf8PropertyName = propertyRef.NameFromJson;
							return propertyRef.Info;
						}
						num3--;
					}
				}
				else
				{
					if (num3 < 0)
					{
						break;
					}
					PropertyRef propertyRef = propertyRefsSorted[num3];
					if (IsPropertyRefEqual(in propertyRef, propertyName, key))
					{
						utf8PropertyName = propertyRef.NameFromJson;
						return propertyRef.Info;
					}
					num3--;
				}
			}
		}
		if (PropertyCache.TryGetValue(JsonHelpers.Utf8GetString(propertyName), out JsonPropertyInfo value))
		{
			if (Options.PropertyNameCaseInsensitive)
			{
				if (propertyName.SequenceEqual(value.NameAsUtf8Bytes))
				{
					utf8PropertyName = value.NameAsUtf8Bytes;
				}
				else
				{
					utf8PropertyName = propertyName.ToArray();
				}
			}
			else
			{
				utf8PropertyName = value.NameAsUtf8Bytes;
			}
		}
		else
		{
			value = JsonPropertyInfo.s_missingProperty;
			utf8PropertyName = propertyName.ToArray();
		}
		int num4 = 0;
		if (propertyRefsSorted != null)
		{
			num4 = propertyRefsSorted.Length;
		}
		if (num4 < 64)
		{
			if (frame.PropertyRefCache != null)
			{
				num4 += frame.PropertyRefCache.Count;
			}
			if (num4 < 64)
			{
				ref List<PropertyRef> propertyRefCache = ref frame.PropertyRefCache;
				if (propertyRefCache == null)
				{
					propertyRefCache = new List<PropertyRef>();
				}
				PropertyRef propertyRef = new PropertyRef(key, value, utf8PropertyName);
				frame.PropertyRefCache.Add(propertyRef);
			}
		}
		return value;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal JsonParameterInfo GetParameter(ReadOnlySpan<byte> propertyName, ref ReadStackFrame frame, out byte[] utf8PropertyName)
	{
		ulong key = GetKey(propertyName);
		ParameterRef[] parameterRefsSorted = _parameterRefsSorted;
		if (parameterRefsSorted != null)
		{
			int parameterIndex = frame.CtorArgumentState.ParameterIndex;
			int num = parameterRefsSorted.Length;
			int num2 = Math.Min(parameterIndex, num);
			int num3 = num2 - 1;
			while (true)
			{
				if (num2 < num)
				{
					ParameterRef parameterRef = parameterRefsSorted[num2];
					if (IsParameterRefEqual(in parameterRef, propertyName, key))
					{
						utf8PropertyName = parameterRef.NameFromJson;
						return parameterRef.Info;
					}
					num2++;
					if (num3 >= 0)
					{
						parameterRef = parameterRefsSorted[num3];
						if (IsParameterRefEqual(in parameterRef, propertyName, key))
						{
							utf8PropertyName = parameterRef.NameFromJson;
							return parameterRef.Info;
						}
						num3--;
					}
				}
				else
				{
					if (num3 < 0)
					{
						break;
					}
					ParameterRef parameterRef = parameterRefsSorted[num3];
					if (IsParameterRefEqual(in parameterRef, propertyName, key))
					{
						utf8PropertyName = parameterRef.NameFromJson;
						return parameterRef.Info;
					}
					num3--;
				}
			}
		}
		if (ParameterCache.TryGetValue(JsonHelpers.Utf8GetString(propertyName), out JsonParameterInfo value))
		{
			if (Options.PropertyNameCaseInsensitive)
			{
				if (propertyName.SequenceEqual(value.NameAsUtf8Bytes))
				{
					utf8PropertyName = value.NameAsUtf8Bytes;
				}
				else
				{
					utf8PropertyName = propertyName.ToArray();
				}
			}
			else
			{
				utf8PropertyName = value.NameAsUtf8Bytes;
			}
		}
		else
		{
			utf8PropertyName = propertyName.ToArray();
		}
		int num4 = 0;
		if (parameterRefsSorted != null)
		{
			num4 = parameterRefsSorted.Length;
		}
		if (num4 < 32)
		{
			if (frame.CtorArgumentState.ParameterRefCache != null)
			{
				num4 += frame.CtorArgumentState.ParameterRefCache.Count;
			}
			if (num4 < 32)
			{
				ArgumentState ctorArgumentState = frame.CtorArgumentState;
				if (ctorArgumentState.ParameterRefCache == null)
				{
					ctorArgumentState.ParameterRefCache = new List<ParameterRef>();
				}
				ParameterRef parameterRef = new ParameterRef(key, value, utf8PropertyName);
				frame.CtorArgumentState.ParameterRefCache.Add(parameterRef);
			}
		}
		return value;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static bool IsPropertyRefEqual(in PropertyRef propertyRef, ReadOnlySpan<byte> propertyName, ulong key)
	{
		if (key == propertyRef.Key && (propertyName.Length <= 7 || propertyName.SequenceEqual(propertyRef.NameFromJson)))
		{
			return true;
		}
		return false;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static bool IsParameterRefEqual(in ParameterRef parameterRef, ReadOnlySpan<byte> parameterName, ulong key)
	{
		if (key == parameterRef.Key && (parameterName.Length <= 7 || parameterName.SequenceEqual(parameterRef.NameFromJson)))
		{
			return true;
		}
		return false;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static ulong GetKey(ReadOnlySpan<byte> name)
	{
		ref byte reference = ref MemoryMarshal.GetReference(name);
		int length = name.Length;
		ulong num;
		if (length > 7)
		{
			num = Unsafe.ReadUnaligned<ulong>(ref reference) & 0xFFFFFFFFFFFFFFuL;
			num |= (ulong)((long)Math.Min(length, 255) << 56);
		}
		else
		{
			num = ((length > 5) ? (Unsafe.ReadUnaligned<uint>(ref reference) | ((ulong)Unsafe.ReadUnaligned<ushort>(ref Unsafe.Add(ref reference, 4)) << 32)) : ((length > 3) ? ((ulong)Unsafe.ReadUnaligned<uint>(ref reference)) : ((ulong)((length > 1) ? Unsafe.ReadUnaligned<ushort>(ref reference) : 0))));
			num |= (ulong)((long)length << 56);
			if (((uint)length & (true ? 1u : 0u)) != 0)
			{
				int num2 = length - 1;
				num |= (ulong)Unsafe.Add(ref reference, num2) << num2 * 8;
			}
		}
		return num;
	}

	internal void UpdateSortedPropertyCache(ref ReadStackFrame frame)
	{
		List<PropertyRef> propertyRefCache = frame.PropertyRefCache;
		if (_propertyRefsSorted != null)
		{
			List<PropertyRef> list = new List<PropertyRef>(_propertyRefsSorted);
			while (list.Count + propertyRefCache.Count > 64)
			{
				propertyRefCache.RemoveAt(propertyRefCache.Count - 1);
			}
			list.AddRange(propertyRefCache);
			_propertyRefsSorted = list.ToArray();
		}
		else
		{
			_propertyRefsSorted = propertyRefCache.ToArray();
		}
		frame.PropertyRefCache = null;
	}

	internal void UpdateSortedParameterCache(ref ReadStackFrame frame)
	{
		List<ParameterRef> parameterRefCache = frame.CtorArgumentState.ParameterRefCache;
		if (_parameterRefsSorted != null)
		{
			List<ParameterRef> list = new List<ParameterRef>(_parameterRefsSorted);
			while (list.Count + parameterRefCache.Count > 32)
			{
				parameterRefCache.RemoveAt(parameterRefCache.Count - 1);
			}
			list.AddRange(parameterRefCache);
			_parameterRefsSorted = list.ToArray();
		}
		else
		{
			_parameterRefsSorted = parameterRefCache.ToArray();
		}
		frame.CtorArgumentState.ParameterRefCache = null;
	}

	internal JsonTypeInfo(Type type, JsonConverter converter, JsonSerializerOptions options)
	{
		Type = type;
		Options = options;
		Converter = converter;
		Kind = GetTypeInfoKind(type, converter);
		PropertyInfoForTypeInfo = CreatePropertyInfoForTypeInfo();
		ElementType = converter.ElementType;
		KeyType = converter.KeyType;
	}

	private protected abstract void SetCreateObject(Delegate createObject);

	public void MakeReadOnly()
	{
		IsReadOnly = true;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal void ValidateCanBeUsedForPropertyMetadataSerialization()
	{
		if (PropertyMetadataSerializationNotSupported)
		{
			ThrowHelper.ThrowInvalidOperationException_NoMetadataForTypeProperties(Options.TypeInfoResolver, Type);
		}
	}

	private protected abstract JsonPropertyInfo CreatePropertyInfoForTypeInfo();

	internal void VerifyMutable()
	{
		if (IsReadOnly)
		{
			ThrowHelper.ThrowInvalidOperationException_TypeInfoImmutable();
		}
		IsCustomized = true;
	}

	internal void EnsureConfigured()
	{
		if (!IsConfigured)
		{
			ConfigureSynchronized();
		}
		void ConfigureSynchronized()
		{
			Options.MakeReadOnly();
			MakeReadOnly();
			_cachedConfigureError?.Throw();
			lock (Options.CacheContext)
			{
				if (_configurationState != 0)
				{
					return;
				}
				_cachedConfigureError?.Throw();
				try
				{
					_configurationState = ConfigurationState.Configuring;
					Configure();
					_configurationState = ConfigurationState.Configured;
				}
				catch (Exception source)
				{
					_cachedConfigureError = ExceptionDispatchInfo.Capture(source);
					_configurationState = ConfigurationState.NotConfigured;
					throw;
				}
			}
		}
	}

	private void Configure()
	{
		PropertyInfoForTypeInfo.Configure();
		if (PolymorphismOptions != null)
		{
			PolymorphicTypeResolver = new PolymorphicTypeResolver(Options, PolymorphismOptions, Type, Converter.CanHaveMetadata);
		}
		if (Kind == JsonTypeInfoKind.Object)
		{
			ConfigureProperties();
			if (DetermineUsesParameterizedConstructor())
			{
				ConfigureConstructorParameters();
			}
		}
		if (ElementType != null)
		{
			if (_elementTypeInfo == null)
			{
				_elementTypeInfo = Options.GetTypeInfoInternal(ElementType, ensureConfigured: true, true);
			}
			_elementTypeInfo.EnsureConfigured();
		}
		if (KeyType != null)
		{
			if (_keyTypeInfo == null)
			{
				_keyTypeInfo = Options.GetTypeInfoInternal(KeyType, ensureConfigured: true, true);
			}
			_keyTypeInfo.EnsureConfigured();
		}
		DetermineIsCompatibleWithCurrentOptions();
		CanUseSerializeHandler = HasSerializeHandler && IsCompatibleWithCurrentOptions;
	}

	private void DetermineIsCompatibleWithCurrentOptions()
	{
		if (!IsCurrentNodeCompatible())
		{
			IsCompatibleWithCurrentOptions = false;
			return;
		}
		if (_properties != null)
		{
			foreach (JsonPropertyInfo property in _properties)
			{
				if (property.IsPropertyTypeInfoConfigured && !property.JsonTypeInfo.IsCompatibleWithCurrentOptions)
				{
					IsCompatibleWithCurrentOptions = false;
					return;
				}
			}
		}
		JsonTypeInfo elementTypeInfo = _elementTypeInfo;
		if (elementTypeInfo == null || elementTypeInfo.IsCompatibleWithCurrentOptions)
		{
			JsonTypeInfo keyTypeInfo = _keyTypeInfo;
			if (keyTypeInfo == null || keyTypeInfo.IsCompatibleWithCurrentOptions)
			{
				return;
			}
		}
		IsCompatibleWithCurrentOptions = false;
		bool IsCurrentNodeCompatible()
		{
			if (Options.CanUseFastPathSerializationLogic)
			{
				return true;
			}
			if (IsCustomized)
			{
				return false;
			}
			return OriginatingResolver.IsCompatibleWithOptions(Options);
		}
	}

	internal bool DetermineUsesParameterizedConstructor()
	{
		if (Converter.ConstructorIsParameterized)
		{
			return CreateObject == null;
		}
		return false;
	}

	[RequiresUnreferencedCode("JSON serialization and deserialization might require types that cannot be statically analyzed and might need runtime code generation. Use System.Text.Json source generation for native AOT applications.")]
	[RequiresDynamicCode("JSON serialization and deserialization might require types that cannot be statically analyzed and might need runtime code generation. Use System.Text.Json source generation for native AOT applications.")]
	public static JsonTypeInfo<T> CreateJsonTypeInfo<T>(JsonSerializerOptions options)
	{
		if (options == null)
		{
			ThrowHelper.ThrowArgumentNullException("options");
		}
		JsonConverter converterForType = DefaultJsonTypeInfoResolver.GetConverterForType(typeof(T), options, resolveJsonConverterAttribute: false);
		return new JsonTypeInfo<T>(converterForType, options);
	}

	[RequiresUnreferencedCode("JSON serialization and deserialization might require types that cannot be statically analyzed and might need runtime code generation. Use System.Text.Json source generation for native AOT applications.")]
	[RequiresDynamicCode("JSON serialization and deserialization might require types that cannot be statically analyzed and might need runtime code generation. Use System.Text.Json source generation for native AOT applications.")]
	public static JsonTypeInfo CreateJsonTypeInfo(Type type, JsonSerializerOptions options)
	{
		if (type == null)
		{
			ThrowHelper.ThrowArgumentNullException("type");
		}
		if (options == null)
		{
			ThrowHelper.ThrowArgumentNullException("options");
		}
		if (IsInvalidForSerialization(type))
		{
			ThrowHelper.ThrowArgumentException_CannotSerializeInvalidType("type", type, null, null);
		}
		JsonConverter converterForType = DefaultJsonTypeInfoResolver.GetConverterForType(type, options, resolveJsonConverterAttribute: false);
		return CreateJsonTypeInfo(type, converterForType, options);
	}

	[RequiresUnreferencedCode("JSON serialization and deserialization might require types that cannot be statically analyzed and might need runtime code generation. Use System.Text.Json source generation for native AOT applications.")]
	[RequiresDynamicCode("JSON serialization and deserialization might require types that cannot be statically analyzed and might need runtime code generation. Use System.Text.Json source generation for native AOT applications.")]
	internal static JsonTypeInfo CreateJsonTypeInfo(Type type, JsonConverter converter, JsonSerializerOptions options)
	{
		if (converter.Type == type)
		{
			return converter.CreateJsonTypeInfo(options);
		}
		Type type2 = typeof(JsonTypeInfo<>).MakeGenericType(type);
		return (JsonTypeInfo)type2.CreateInstanceNoWrapExceptions(new Type[2]
		{
			typeof(JsonConverter),
			typeof(JsonSerializerOptions)
		}, new object[2] { converter, options });
	}

	[RequiresUnreferencedCode("JSON serialization and deserialization might require types that cannot be statically analyzed and might need runtime code generation. Use System.Text.Json source generation for native AOT applications.")]
	[RequiresDynamicCode("JSON serialization and deserialization might require types that cannot be statically analyzed and might need runtime code generation. Use System.Text.Json source generation for native AOT applications.")]
	public JsonPropertyInfo CreateJsonPropertyInfo(Type propertyType, string name)
	{
		if (propertyType == null)
		{
			ThrowHelper.ThrowArgumentNullException("propertyType");
		}
		if (name == null)
		{
			ThrowHelper.ThrowArgumentNullException("name");
		}
		if (IsInvalidForSerialization(propertyType))
		{
			ThrowHelper.ThrowArgumentException_CannotSerializeInvalidType("propertyType", propertyType, Type, name);
		}
		VerifyMutable();
		JsonPropertyInfo jsonPropertyInfo = CreatePropertyUsingReflection(propertyType, null);
		jsonPropertyInfo.Name = name;
		return jsonPropertyInfo;
	}

	internal abstract void SerializeAsObject(Utf8JsonWriter writer, object rootValue);

	internal abstract Task SerializeAsObjectAsync(Stream utf8Json, object rootValue, CancellationToken cancellationToken);

	internal abstract void SerializeAsObject(Stream utf8Json, object rootValue);

	internal abstract object DeserializeAsObject(ref Utf8JsonReader reader, ref ReadStack state);

	internal abstract ValueTask<object> DeserializeAsObjectAsync(Stream utf8Json, CancellationToken cancellationToken);

	internal abstract object DeserializeAsObject(Stream utf8Json);

	internal void ConfigureProperties()
	{
		JsonPropertyInfoList propertyList = PropertyList;
		JsonPropertyDictionary<JsonPropertyInfo> jsonPropertyDictionary = CreatePropertyCache(propertyList.Count);
		int numberOfRequiredProperties = 0;
		bool flag = true;
		int num = int.MinValue;
		foreach (JsonPropertyInfo item in propertyList)
		{
			if (item.IsExtensionData)
			{
				JsonUnmappedMemberHandling? unmappedMemberHandling = UnmappedMemberHandling;
				if (unmappedMemberHandling.HasValue && unmappedMemberHandling.GetValueOrDefault() == JsonUnmappedMemberHandling.Disallow)
				{
					ThrowHelper.ThrowInvalidOperationException_ExtensionDataConflictsWithUnmappedMemberHandling(Type, item);
				}
				if (ExtensionDataProperty != null)
				{
					ThrowHelper.ThrowInvalidOperationException_SerializationDuplicateTypeAttribute(Type, typeof(JsonExtensionDataAttribute));
				}
				ExtensionDataProperty = item;
			}
			else
			{
				if (item.IsRequired)
				{
					item.RequiredPropertyIndex = numberOfRequiredProperties++;
				}
				if (flag)
				{
					flag = num <= item.Order;
					num = item.Order;
				}
				if (!jsonPropertyDictionary.TryAddValue(item.Name, item))
				{
					ThrowHelper.ThrowInvalidOperationException_SerializerPropertyNameConflict(Type, item.Name);
				}
			}
			item.Configure();
		}
		if (!flag)
		{
			propertyList.SortProperties();
			jsonPropertyDictionary.List.StableSortByKey((KeyValuePair<string, JsonPropertyInfo> propInfo) => propInfo.Value.Order);
		}
		NumberOfRequiredProperties = numberOfRequiredProperties;
		PropertyCache = jsonPropertyDictionary;
		EffectiveUnmappedMemberHandling = UnmappedMemberHandling ?? ((ExtensionDataProperty == null) ? Options.UnmappedMemberHandling : JsonUnmappedMemberHandling.Skip);
	}

	internal void ConfigureConstructorParameters()
	{
		JsonParameterInfoValues[] array = ParameterInfoValues ?? Array.Empty<JsonParameterInfoValues>();
		JsonPropertyDictionary<JsonParameterInfo> jsonPropertyDictionary = new JsonPropertyDictionary<JsonParameterInfo>(Options.PropertyNameCaseInsensitive, array.Length);
		Dictionary<ParameterLookupKey, ParameterLookupValue> dictionary = new Dictionary<ParameterLookupKey, ParameterLookupValue>(PropertyCache.Count);
		foreach (KeyValuePair<string, JsonPropertyInfo> item in PropertyCache.List)
		{
			JsonPropertyInfo value = item.Value;
			string text = value.MemberName ?? value.Name;
			ParameterLookupKey key = new ParameterLookupKey(text, value.PropertyType);
			ParameterLookupValue value2 = new ParameterLookupValue(value);
			if (!dictionary.TryAdd(key, value2))
			{
				ParameterLookupValue parameterLookupValue = dictionary[key];
				parameterLookupValue.DuplicateName = text;
			}
		}
		JsonParameterInfoValues[] array2 = array;
		foreach (JsonParameterInfoValues jsonParameterInfoValues in array2)
		{
			ParameterLookupKey parameterLookupKey = new ParameterLookupKey(jsonParameterInfoValues.Name, jsonParameterInfoValues.ParameterType);
			if (dictionary.TryGetValue(parameterLookupKey, out var value3))
			{
				if (value3.DuplicateName != null)
				{
					ThrowHelper.ThrowInvalidOperationException_MultiplePropertiesBindToConstructorParameters(Type, jsonParameterInfoValues.Name, value3.JsonPropertyInfo.Name, value3.DuplicateName);
				}
				JsonPropertyInfo jsonPropertyInfo = value3.JsonPropertyInfo;
				JsonParameterInfo value4 = jsonPropertyInfo.CreateJsonParameterInfo(jsonParameterInfoValues);
				jsonPropertyDictionary.Add(jsonPropertyInfo.Name, value4);
			}
			else if (ExtensionDataProperty != null && StringComparer.OrdinalIgnoreCase.Equals(parameterLookupKey.Name, ExtensionDataProperty.Name))
			{
				ThrowHelper.ThrowInvalidOperationException_ExtensionDataCannotBindToCtorParam(ExtensionDataProperty.MemberName, ExtensionDataProperty);
			}
		}
		ParameterCount = array.Length;
		ParameterCache = jsonPropertyDictionary;
		ParameterInfoValues = null;
	}

	internal static void ValidateType(Type type)
	{
		if (IsInvalidForSerialization(type))
		{
			ThrowHelper.ThrowInvalidOperationException_CannotSerializeInvalidType(type, null, null);
		}
	}

	internal static bool IsInvalidForSerialization(Type type)
	{
		if (!(type == typeof(void)) && !type.IsPointer && !type.IsByRef && !IsByRefLike(type))
		{
			return type.ContainsGenericParameters;
		}
		return true;
	}

	internal void PopulatePolymorphismMetadata()
	{
		JsonPolymorphismOptions jsonPolymorphismOptions = JsonPolymorphismOptions.CreateFromAttributeDeclarations(Type);
		if (jsonPolymorphismOptions != null)
		{
			jsonPolymorphismOptions.DeclaringTypeInfo = this;
			_polymorphismOptions = jsonPolymorphismOptions;
		}
	}

	internal void MapInterfaceTypesToCallbacks()
	{
		if (Kind != JsonTypeInfoKind.Object)
		{
			return;
		}
		if (typeof(IJsonOnSerializing).IsAssignableFrom(Type))
		{
			OnSerializing = delegate(object obj)
			{
				((IJsonOnSerializing)obj).OnSerializing();
			};
		}
		if (typeof(IJsonOnSerialized).IsAssignableFrom(Type))
		{
			OnSerialized = delegate(object obj)
			{
				((IJsonOnSerialized)obj).OnSerialized();
			};
		}
		if (typeof(IJsonOnDeserializing).IsAssignableFrom(Type))
		{
			OnDeserializing = delegate(object obj)
			{
				((IJsonOnDeserializing)obj).OnDeserializing();
			};
		}
		if (typeof(IJsonOnDeserialized).IsAssignableFrom(Type))
		{
			OnDeserialized = delegate(object obj)
			{
				((IJsonOnDeserialized)obj).OnDeserialized();
			};
		}
	}

	internal void SetCreateObjectIfCompatible(Delegate createObject)
	{
		if (Converter.SupportsCreateObjectDelegate && !Converter.ConstructorIsParameterized)
		{
			SetCreateObject(createObject);
		}
	}

	private static bool IsByRefLike(Type type)
	{
		return type.IsByRefLike;
	}

	internal static bool IsValidExtensionDataProperty(Type propertyType)
	{
		if (!typeof(IDictionary<string, object>).IsAssignableFrom(propertyType) && !typeof(IDictionary<string, JsonElement>).IsAssignableFrom(propertyType))
		{
			if (propertyType.FullName == "System.Text.Json.Nodes.JsonObject")
			{
				return (object)propertyType.Assembly == typeof(JsonTypeInfo).Assembly;
			}
			return false;
		}
		return true;
	}

	internal JsonPropertyDictionary<JsonPropertyInfo> CreatePropertyCache(int capacity)
	{
		return new JsonPropertyDictionary<JsonPropertyInfo>(Options.PropertyNameCaseInsensitive, capacity);
	}

	private static JsonTypeInfoKind GetTypeInfoKind(Type type, JsonConverter converter)
	{
		if (type == typeof(object) && converter.CanBePolymorphic)
		{
			return JsonTypeInfoKind.None;
		}
		switch (converter.ConverterStrategy)
		{
		case ConverterStrategy.Value:
			return JsonTypeInfoKind.None;
		case ConverterStrategy.Object:
			return JsonTypeInfoKind.Object;
		case ConverterStrategy.Enumerable:
			return JsonTypeInfoKind.Enumerable;
		case ConverterStrategy.Dictionary:
			return JsonTypeInfoKind.Dictionary;
		case ConverterStrategy.None:
			ThrowHelper.ThrowNotSupportedException_SerializationNotSupported(type);
			return JsonTypeInfoKind.None;
		default:
			throw new InvalidOperationException();
		}
	}
}
