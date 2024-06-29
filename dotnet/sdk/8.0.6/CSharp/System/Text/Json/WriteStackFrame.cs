using System.Collections;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace System.Text.Json;

[StructLayout(LayoutKind.Auto)]
[DebuggerDisplay("{DebuggerDisplay,nq}")]
internal struct WriteStackFrame
{
	public IEnumerator CollectionEnumerator;

	public IAsyncDisposable AsyncDisposable;

	public bool AsyncEnumeratorIsPendingCompletion;

	public JsonPropertyInfo JsonPropertyInfo;

	public bool IsWritingExtensionDataProperty;

	public JsonTypeInfo JsonTypeInfo;

	public int OriginalDepth;

	public bool ProcessedStartToken;

	public bool ProcessedEndToken;

	public StackFramePropertyState PropertyState;

	public int EnumeratorIndex;

	public string JsonPropertyNameAsString;

	public MetadataPropertyName MetadataPropertyName;

	public PolymorphicSerializationState PolymorphicSerializationState;

	public JsonTypeInfo PolymorphicTypeInfo;

	public JsonNumberHandling? NumberHandling;

	public bool IsPushedReferenceForCycleDetection;

	[DebuggerBrowsable(DebuggerBrowsableState.Never)]
	private readonly string DebuggerDisplay => $"ConverterStrategy.{JsonTypeInfo?.Converter.ConverterStrategy}, {JsonTypeInfo?.Type.Name}";

	public void EndCollectionElement()
	{
		PolymorphicSerializationState = PolymorphicSerializationState.None;
	}

	public void EndDictionaryEntry()
	{
		PropertyState = StackFramePropertyState.None;
		PolymorphicSerializationState = PolymorphicSerializationState.None;
	}

	public void EndProperty()
	{
		JsonPropertyInfo = null;
		JsonPropertyNameAsString = null;
		PropertyState = StackFramePropertyState.None;
		PolymorphicSerializationState = PolymorphicSerializationState.None;
	}

	public readonly JsonTypeInfo GetNestedJsonTypeInfo()
	{
		if (PolymorphicSerializationState != PolymorphicSerializationState.PolymorphicReEntryStarted)
		{
			return JsonPropertyInfo.JsonTypeInfo;
		}
		return PolymorphicTypeInfo;
	}

	public JsonTypeInfo InitializePolymorphicReEntry(Type runtimeType, JsonSerializerOptions options)
	{
		if (PolymorphicTypeInfo?.Type != runtimeType)
		{
			JsonTypeInfo typeInfoInternal = options.GetTypeInfoInternal(runtimeType, ensureConfigured: true, true, resolveIfMutable: false, fallBackToNearestAncestorType: true);
			PolymorphicTypeInfo = typeInfoInternal.AncestorPolymorphicType ?? typeInfoInternal;
		}
		PolymorphicSerializationState = PolymorphicSerializationState.PolymorphicReEntryStarted;
		return PolymorphicTypeInfo;
	}

	public JsonConverter InitializePolymorphicReEntry(JsonTypeInfo derivedJsonTypeInfo)
	{
		PolymorphicTypeInfo = derivedJsonTypeInfo;
		PolymorphicSerializationState = PolymorphicSerializationState.PolymorphicReEntryStarted;
		return derivedJsonTypeInfo.Converter;
	}

	public JsonConverter ResumePolymorphicReEntry()
	{
		PolymorphicSerializationState = PolymorphicSerializationState.PolymorphicReEntryStarted;
		return PolymorphicTypeInfo.Converter;
	}

	public void ExitPolymorphicConverter(bool success)
	{
		PolymorphicSerializationState = ((!success) ? PolymorphicSerializationState.PolymorphicReEntrySuspended : PolymorphicSerializationState.None);
	}
}
