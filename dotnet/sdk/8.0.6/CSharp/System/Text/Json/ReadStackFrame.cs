using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace System.Text.Json;

[StructLayout(LayoutKind.Auto)]
[DebuggerDisplay("{DebuggerDisplay,nq}")]
internal struct ReadStackFrame
{
	public JsonPropertyInfo JsonPropertyInfo;

	public StackFramePropertyState PropertyState;

	public bool UseExtensionProperty;

	public byte[] JsonPropertyName;

	public string JsonPropertyNameAsString;

	public object DictionaryKey;

	public object ReturnValue;

	public JsonTypeInfo JsonTypeInfo;

	public StackFrameObjectState ObjectState;

	public bool CanContainMetadata;

	public MetadataPropertyName LatestMetadataPropertyName;

	public MetadataPropertyName MetadataPropertyNames;

	public PolymorphicSerializationState PolymorphicSerializationState;

	public JsonTypeInfo PolymorphicJsonTypeInfo;

	public int PropertyIndex;

	public List<PropertyRef> PropertyRefCache;

	public ArgumentState CtorArgumentState;

	public JsonNumberHandling? NumberHandling;

	public BitArray RequiredPropertiesSet;

	public bool HasParentObject;

	public bool IsPopulating;

	public JsonTypeInfo BaseJsonTypeInfo
	{
		get
		{
			if (PolymorphicSerializationState != PolymorphicSerializationState.PolymorphicReEntryStarted)
			{
				return JsonTypeInfo;
			}
			return PolymorphicJsonTypeInfo;
		}
	}

	[DebuggerBrowsable(DebuggerBrowsableState.Never)]
	private string DebuggerDisplay => $"ConverterStrategy.{JsonTypeInfo?.Converter.ConverterStrategy}, {JsonTypeInfo?.Type.Name}";

	public void EndConstructorParameter()
	{
		CtorArgumentState.JsonParameterInfo = null;
		JsonPropertyName = null;
		PropertyState = StackFramePropertyState.None;
	}

	public void EndProperty()
	{
		JsonPropertyInfo = null;
		JsonPropertyName = null;
		JsonPropertyNameAsString = null;
		PropertyState = StackFramePropertyState.None;
	}

	public void EndElement()
	{
		JsonPropertyNameAsString = null;
		PropertyState = StackFramePropertyState.None;
	}

	public bool IsProcessingDictionary()
	{
		return JsonTypeInfo.Kind == JsonTypeInfoKind.Dictionary;
	}

	public bool IsProcessingEnumerable()
	{
		return JsonTypeInfo.Kind == JsonTypeInfoKind.Enumerable;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void MarkRequiredPropertyAsRead(JsonPropertyInfo propertyInfo)
	{
		if (propertyInfo.IsRequired)
		{
			RequiredPropertiesSet[propertyInfo.RequiredPropertyIndex] = true;
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal void InitializeRequiredPropertiesValidationState(JsonTypeInfo typeInfo)
	{
		if (typeInfo.NumberOfRequiredProperties > 0)
		{
			RequiredPropertiesSet = new BitArray(typeInfo.NumberOfRequiredProperties);
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal void ValidateAllRequiredPropertiesAreRead(JsonTypeInfo typeInfo)
	{
		if (typeInfo.NumberOfRequiredProperties > 0 && !RequiredPropertiesSet.HasAllSet())
		{
			ThrowHelper.ThrowJsonException_JsonRequiredPropertyMissing(typeInfo, RequiredPropertiesSet);
		}
	}
}
