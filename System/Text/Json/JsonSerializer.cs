using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Converters;
using System.Text.Json.Serialization.Metadata;
using System.Threading;
using System.Threading.Tasks;

namespace System.Text.Json;

public static class JsonSerializer
{
	private static readonly byte[] s_idPropertyName = "$id"u8.ToArray();

	private static readonly byte[] s_refPropertyName = "$ref"u8.ToArray();

	private static readonly byte[] s_typePropertyName = "$type"u8.ToArray();

	private static readonly byte[] s_valuesPropertyName = "$values"u8.ToArray();

	internal static readonly JsonEncodedText s_metadataId = JsonEncodedText.Encode("$id");

	internal static readonly JsonEncodedText s_metadataRef = JsonEncodedText.Encode("$ref");

	internal static readonly JsonEncodedText s_metadataType = JsonEncodedText.Encode("$type");

	internal static readonly JsonEncodedText s_metadataValues = JsonEncodedText.Encode("$values");

	public static bool IsReflectionEnabledByDefault { get; } = !AppContext.TryGetSwitch("System.Text.Json.JsonSerializer.IsReflectionEnabledByDefault", out var isEnabled) || isEnabled;


	[RequiresUnreferencedCode("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext, or make sure all of the required types are preserved.")]
	[RequiresDynamicCode("JSON serialization and deserialization might require types that cannot be statically analyzed and might need runtime code generation. Use System.Text.Json source generation for native AOT applications.")]
	public static TValue? Deserialize<TValue>(this JsonDocument document, JsonSerializerOptions? options = null)
	{
		if (document == null)
		{
			ThrowHelper.ThrowArgumentNullException("document");
		}
		JsonTypeInfo<TValue> typeInfo = GetTypeInfo<TValue>(options);
		ReadOnlySpan<byte> span = document.GetRootRawValue().Span;
		return ReadFromSpan(span, typeInfo);
	}

	[RequiresUnreferencedCode("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext, or make sure all of the required types are preserved.")]
	[RequiresDynamicCode("JSON serialization and deserialization might require types that cannot be statically analyzed and might need runtime code generation. Use System.Text.Json source generation for native AOT applications.")]
	public static object? Deserialize(this JsonDocument document, Type returnType, JsonSerializerOptions? options = null)
	{
		if (document == null)
		{
			ThrowHelper.ThrowArgumentNullException("document");
		}
		if ((object)returnType == null)
		{
			ThrowHelper.ThrowArgumentNullException("returnType");
		}
		JsonTypeInfo typeInfo = GetTypeInfo(options, returnType);
		ReadOnlySpan<byte> span = document.GetRootRawValue().Span;
		return ReadFromSpanAsObject(span, typeInfo);
	}

	public static TValue? Deserialize<TValue>(this JsonDocument document, JsonTypeInfo<TValue> jsonTypeInfo)
	{
		if (document == null)
		{
			ThrowHelper.ThrowArgumentNullException("document");
		}
		if (jsonTypeInfo == null)
		{
			ThrowHelper.ThrowArgumentNullException("jsonTypeInfo");
		}
		jsonTypeInfo.EnsureConfigured();
		ReadOnlySpan<byte> span = document.GetRootRawValue().Span;
		return ReadFromSpan(span, jsonTypeInfo);
	}

	public static object? Deserialize(this JsonDocument document, JsonTypeInfo jsonTypeInfo)
	{
		if (document == null)
		{
			ThrowHelper.ThrowArgumentNullException("document");
		}
		if (jsonTypeInfo == null)
		{
			ThrowHelper.ThrowArgumentNullException("jsonTypeInfo");
		}
		jsonTypeInfo.EnsureConfigured();
		ReadOnlySpan<byte> span = document.GetRootRawValue().Span;
		return ReadFromSpanAsObject(span, jsonTypeInfo);
	}

	public static object? Deserialize(this JsonDocument document, Type returnType, JsonSerializerContext context)
	{
		if (document == null)
		{
			ThrowHelper.ThrowArgumentNullException("document");
		}
		if ((object)returnType == null)
		{
			ThrowHelper.ThrowArgumentNullException("returnType");
		}
		if (context == null)
		{
			ThrowHelper.ThrowArgumentNullException("context");
		}
		JsonTypeInfo typeInfo = GetTypeInfo(context, returnType);
		ReadOnlySpan<byte> span = document.GetRootRawValue().Span;
		return ReadFromSpanAsObject(span, typeInfo);
	}

	[RequiresUnreferencedCode("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext, or make sure all of the required types are preserved.")]
	[RequiresDynamicCode("JSON serialization and deserialization might require types that cannot be statically analyzed and might need runtime code generation. Use System.Text.Json source generation for native AOT applications.")]
	public static TValue? Deserialize<TValue>(this JsonElement element, JsonSerializerOptions? options = null)
	{
		JsonTypeInfo<TValue> typeInfo = GetTypeInfo<TValue>(options);
		ReadOnlySpan<byte> span = element.GetRawValue().Span;
		return ReadFromSpan(span, typeInfo);
	}

	[RequiresUnreferencedCode("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext, or make sure all of the required types are preserved.")]
	[RequiresDynamicCode("JSON serialization and deserialization might require types that cannot be statically analyzed and might need runtime code generation. Use System.Text.Json source generation for native AOT applications.")]
	public static object? Deserialize(this JsonElement element, Type returnType, JsonSerializerOptions? options = null)
	{
		if ((object)returnType == null)
		{
			ThrowHelper.ThrowArgumentNullException("returnType");
		}
		JsonTypeInfo typeInfo = GetTypeInfo(options, returnType);
		ReadOnlySpan<byte> span = element.GetRawValue().Span;
		return ReadFromSpanAsObject(span, typeInfo);
	}

	public static TValue? Deserialize<TValue>(this JsonElement element, JsonTypeInfo<TValue> jsonTypeInfo)
	{
		if (jsonTypeInfo == null)
		{
			ThrowHelper.ThrowArgumentNullException("jsonTypeInfo");
		}
		jsonTypeInfo.EnsureConfigured();
		ReadOnlySpan<byte> span = element.GetRawValue().Span;
		return ReadFromSpan(span, jsonTypeInfo);
	}

	public static object? Deserialize(this JsonElement element, JsonTypeInfo jsonTypeInfo)
	{
		if (jsonTypeInfo == null)
		{
			ThrowHelper.ThrowArgumentNullException("jsonTypeInfo");
		}
		jsonTypeInfo.EnsureConfigured();
		ReadOnlySpan<byte> span = element.GetRawValue().Span;
		return ReadFromSpanAsObject(span, jsonTypeInfo);
	}

	public static object? Deserialize(this JsonElement element, Type returnType, JsonSerializerContext context)
	{
		if ((object)returnType == null)
		{
			ThrowHelper.ThrowArgumentNullException("returnType");
		}
		if (context == null)
		{
			ThrowHelper.ThrowArgumentNullException("context");
		}
		JsonTypeInfo typeInfo = GetTypeInfo(context, returnType);
		ReadOnlySpan<byte> span = element.GetRawValue().Span;
		return ReadFromSpanAsObject(span, typeInfo);
	}

	[RequiresUnreferencedCode("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext, or make sure all of the required types are preserved.")]
	[RequiresDynamicCode("JSON serialization and deserialization might require types that cannot be statically analyzed and might need runtime code generation. Use System.Text.Json source generation for native AOT applications.")]
	public static TValue? Deserialize<TValue>(this JsonNode? node, JsonSerializerOptions? options = null)
	{
		JsonTypeInfo<TValue> typeInfo = GetTypeInfo<TValue>(options);
		return ReadFromNode(node, typeInfo);
	}

	[RequiresUnreferencedCode("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext, or make sure all of the required types are preserved.")]
	[RequiresDynamicCode("JSON serialization and deserialization might require types that cannot be statically analyzed and might need runtime code generation. Use System.Text.Json source generation for native AOT applications.")]
	public static object? Deserialize(this JsonNode? node, Type returnType, JsonSerializerOptions? options = null)
	{
		if ((object)returnType == null)
		{
			ThrowHelper.ThrowArgumentNullException("returnType");
		}
		JsonTypeInfo typeInfo = GetTypeInfo(options, returnType);
		return ReadFromNodeAsObject(node, typeInfo);
	}

	public static TValue? Deserialize<TValue>(this JsonNode? node, JsonTypeInfo<TValue> jsonTypeInfo)
	{
		if (jsonTypeInfo == null)
		{
			ThrowHelper.ThrowArgumentNullException("jsonTypeInfo");
		}
		jsonTypeInfo.EnsureConfigured();
		return ReadFromNode(node, jsonTypeInfo);
	}

	public static object? Deserialize(this JsonNode? node, JsonTypeInfo jsonTypeInfo)
	{
		if (jsonTypeInfo == null)
		{
			ThrowHelper.ThrowArgumentNullException("jsonTypeInfo");
		}
		jsonTypeInfo.EnsureConfigured();
		return ReadFromNodeAsObject(node, jsonTypeInfo);
	}

	public static object? Deserialize(this JsonNode? node, Type returnType, JsonSerializerContext context)
	{
		if ((object)returnType == null)
		{
			ThrowHelper.ThrowArgumentNullException("returnType");
		}
		if (context == null)
		{
			ThrowHelper.ThrowArgumentNullException("context");
		}
		JsonTypeInfo typeInfo = GetTypeInfo(context, returnType);
		return ReadFromNodeAsObject(node, typeInfo);
	}

	private static TValue ReadFromNode<TValue>(JsonNode node, JsonTypeInfo<TValue> jsonTypeInfo)
	{
		JsonSerializerOptions options = jsonTypeInfo.Options;
		using PooledByteBufferWriter pooledByteBufferWriter = new PooledByteBufferWriter(options.DefaultBufferSize);
		using (Utf8JsonWriter utf8JsonWriter = new Utf8JsonWriter(pooledByteBufferWriter, options.GetWriterOptions()))
		{
			if (node == null)
			{
				utf8JsonWriter.WriteNullValue();
			}
			else
			{
				node.WriteTo(utf8JsonWriter, options);
			}
		}
		return ReadFromSpan(pooledByteBufferWriter.WrittenMemory.Span, jsonTypeInfo);
	}

	private static object ReadFromNodeAsObject(JsonNode node, JsonTypeInfo jsonTypeInfo)
	{
		JsonSerializerOptions options = jsonTypeInfo.Options;
		using PooledByteBufferWriter pooledByteBufferWriter = new PooledByteBufferWriter(options.DefaultBufferSize);
		using (Utf8JsonWriter utf8JsonWriter = new Utf8JsonWriter(pooledByteBufferWriter, options.GetWriterOptions()))
		{
			if (node == null)
			{
				utf8JsonWriter.WriteNullValue();
			}
			else
			{
				node.WriteTo(utf8JsonWriter, options);
			}
		}
		return ReadFromSpanAsObject(pooledByteBufferWriter.WrittenMemory.Span, jsonTypeInfo);
	}

	[RequiresUnreferencedCode("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext, or make sure all of the required types are preserved.")]
	[RequiresDynamicCode("JSON serialization and deserialization might require types that cannot be statically analyzed and might need runtime code generation. Use System.Text.Json source generation for native AOT applications.")]
	public static JsonDocument SerializeToDocument<TValue>(TValue value, JsonSerializerOptions? options = null)
	{
		JsonTypeInfo<TValue> typeInfo = GetTypeInfo<TValue>(options);
		return WriteDocument<TValue>(in value, typeInfo);
	}

	[RequiresUnreferencedCode("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext, or make sure all of the required types are preserved.")]
	[RequiresDynamicCode("JSON serialization and deserialization might require types that cannot be statically analyzed and might need runtime code generation. Use System.Text.Json source generation for native AOT applications.")]
	public static JsonDocument SerializeToDocument(object? value, Type inputType, JsonSerializerOptions? options = null)
	{
		ValidateInputType(value, inputType);
		JsonTypeInfo typeInfo = GetTypeInfo(options, inputType);
		return WriteDocumentAsObject(value, typeInfo);
	}

	public static JsonDocument SerializeToDocument<TValue>(TValue value, JsonTypeInfo<TValue> jsonTypeInfo)
	{
		if (jsonTypeInfo == null)
		{
			ThrowHelper.ThrowArgumentNullException("jsonTypeInfo");
		}
		jsonTypeInfo.EnsureConfigured();
		return WriteDocument(in value, jsonTypeInfo);
	}

	public static JsonDocument SerializeToDocument(object? value, JsonTypeInfo jsonTypeInfo)
	{
		if (jsonTypeInfo == null)
		{
			ThrowHelper.ThrowArgumentNullException("jsonTypeInfo");
		}
		jsonTypeInfo.EnsureConfigured();
		return WriteDocumentAsObject(value, jsonTypeInfo);
	}

	public static JsonDocument SerializeToDocument(object? value, Type inputType, JsonSerializerContext context)
	{
		if (context == null)
		{
			ThrowHelper.ThrowArgumentNullException("context");
		}
		ValidateInputType(value, inputType);
		return WriteDocumentAsObject(value, GetTypeInfo(context, inputType));
	}

	private static JsonDocument WriteDocument<TValue>(in TValue value, JsonTypeInfo<TValue> jsonTypeInfo)
	{
		JsonSerializerOptions options = jsonTypeInfo.Options;
		PooledByteBufferWriter pooledByteBufferWriter = new PooledByteBufferWriter(options.DefaultBufferSize);
		Utf8JsonWriter writer = Utf8JsonWriterCache.RentWriter(options, pooledByteBufferWriter);
		try
		{
			jsonTypeInfo.Serialize(writer, in value);
			return JsonDocument.ParseRented(pooledByteBufferWriter, options.GetDocumentOptions());
		}
		finally
		{
			Utf8JsonWriterCache.ReturnWriter(writer);
		}
	}

	private static JsonDocument WriteDocumentAsObject(object value, JsonTypeInfo jsonTypeInfo)
	{
		JsonSerializerOptions options = jsonTypeInfo.Options;
		PooledByteBufferWriter pooledByteBufferWriter = new PooledByteBufferWriter(options.DefaultBufferSize);
		Utf8JsonWriter writer = Utf8JsonWriterCache.RentWriter(options, pooledByteBufferWriter);
		try
		{
			jsonTypeInfo.SerializeAsObject(writer, value);
			return JsonDocument.ParseRented(pooledByteBufferWriter, options.GetDocumentOptions());
		}
		finally
		{
			Utf8JsonWriterCache.ReturnWriter(writer);
		}
	}

	[RequiresUnreferencedCode("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext, or make sure all of the required types are preserved.")]
	[RequiresDynamicCode("JSON serialization and deserialization might require types that cannot be statically analyzed and might need runtime code generation. Use System.Text.Json source generation for native AOT applications.")]
	public static JsonElement SerializeToElement<TValue>(TValue value, JsonSerializerOptions? options = null)
	{
		JsonTypeInfo<TValue> typeInfo = GetTypeInfo<TValue>(options);
		return WriteElement<TValue>(in value, typeInfo);
	}

	[RequiresUnreferencedCode("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext, or make sure all of the required types are preserved.")]
	[RequiresDynamicCode("JSON serialization and deserialization might require types that cannot be statically analyzed and might need runtime code generation. Use System.Text.Json source generation for native AOT applications.")]
	public static JsonElement SerializeToElement(object? value, Type inputType, JsonSerializerOptions? options = null)
	{
		ValidateInputType(value, inputType);
		JsonTypeInfo typeInfo = GetTypeInfo(options, inputType);
		return WriteElementAsObject(value, typeInfo);
	}

	public static JsonElement SerializeToElement<TValue>(TValue value, JsonTypeInfo<TValue> jsonTypeInfo)
	{
		if (jsonTypeInfo == null)
		{
			ThrowHelper.ThrowArgumentNullException("jsonTypeInfo");
		}
		jsonTypeInfo.EnsureConfigured();
		return WriteElement(in value, jsonTypeInfo);
	}

	public static JsonElement SerializeToElement(object? value, JsonTypeInfo jsonTypeInfo)
	{
		if (jsonTypeInfo == null)
		{
			ThrowHelper.ThrowArgumentNullException("jsonTypeInfo");
		}
		jsonTypeInfo.EnsureConfigured();
		return WriteElementAsObject(value, jsonTypeInfo);
	}

	public static JsonElement SerializeToElement(object? value, Type inputType, JsonSerializerContext context)
	{
		if (context == null)
		{
			ThrowHelper.ThrowArgumentNullException("context");
		}
		ValidateInputType(value, inputType);
		JsonTypeInfo typeInfo = GetTypeInfo(context, inputType);
		return WriteElementAsObject(value, typeInfo);
	}

	private static JsonElement WriteElement<TValue>(in TValue value, JsonTypeInfo<TValue> jsonTypeInfo)
	{
		JsonSerializerOptions options = jsonTypeInfo.Options;
		PooledByteBufferWriter bufferWriter;
		Utf8JsonWriter writer = Utf8JsonWriterCache.RentWriterAndBuffer(jsonTypeInfo.Options, out bufferWriter);
		try
		{
			jsonTypeInfo.Serialize(writer, in value);
			return JsonElement.ParseValue(bufferWriter.WrittenMemory.Span, options.GetDocumentOptions());
		}
		finally
		{
			Utf8JsonWriterCache.ReturnWriterAndBuffer(writer, bufferWriter);
		}
	}

	private static JsonElement WriteElementAsObject(object value, JsonTypeInfo jsonTypeInfo)
	{
		JsonSerializerOptions options = jsonTypeInfo.Options;
		PooledByteBufferWriter bufferWriter;
		Utf8JsonWriter writer = Utf8JsonWriterCache.RentWriterAndBuffer(jsonTypeInfo.Options, out bufferWriter);
		try
		{
			jsonTypeInfo.SerializeAsObject(writer, value);
			return JsonElement.ParseValue(bufferWriter.WrittenMemory.Span, options.GetDocumentOptions());
		}
		finally
		{
			Utf8JsonWriterCache.ReturnWriterAndBuffer(writer, bufferWriter);
		}
	}

	[RequiresUnreferencedCode("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext, or make sure all of the required types are preserved.")]
	[RequiresDynamicCode("JSON serialization and deserialization might require types that cannot be statically analyzed and might need runtime code generation. Use System.Text.Json source generation for native AOT applications.")]
	public static JsonNode? SerializeToNode<TValue>(TValue value, JsonSerializerOptions? options = null)
	{
		JsonTypeInfo<TValue> typeInfo = GetTypeInfo<TValue>(options);
		return WriteNode<TValue>(in value, typeInfo);
	}

	[RequiresUnreferencedCode("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext, or make sure all of the required types are preserved.")]
	[RequiresDynamicCode("JSON serialization and deserialization might require types that cannot be statically analyzed and might need runtime code generation. Use System.Text.Json source generation for native AOT applications.")]
	public static JsonNode? SerializeToNode(object? value, Type inputType, JsonSerializerOptions? options = null)
	{
		ValidateInputType(value, inputType);
		JsonTypeInfo typeInfo = GetTypeInfo(options, inputType);
		return WriteNodeAsObject(value, typeInfo);
	}

	public static JsonNode? SerializeToNode<TValue>(TValue value, JsonTypeInfo<TValue> jsonTypeInfo)
	{
		if (jsonTypeInfo == null)
		{
			ThrowHelper.ThrowArgumentNullException("jsonTypeInfo");
		}
		jsonTypeInfo.EnsureConfigured();
		return WriteNode(in value, jsonTypeInfo);
	}

	public static JsonNode? SerializeToNode(object? value, JsonTypeInfo jsonTypeInfo)
	{
		if (jsonTypeInfo == null)
		{
			ThrowHelper.ThrowArgumentNullException("jsonTypeInfo");
		}
		jsonTypeInfo.EnsureConfigured();
		return WriteNodeAsObject(value, jsonTypeInfo);
	}

	public static JsonNode? SerializeToNode(object? value, Type inputType, JsonSerializerContext context)
	{
		if (context == null)
		{
			ThrowHelper.ThrowArgumentNullException("context");
		}
		ValidateInputType(value, inputType);
		JsonTypeInfo typeInfo = GetTypeInfo(context, inputType);
		return WriteNodeAsObject(value, typeInfo);
	}

	private static JsonNode WriteNode<TValue>(in TValue value, JsonTypeInfo<TValue> jsonTypeInfo)
	{
		JsonSerializerOptions options = jsonTypeInfo.Options;
		PooledByteBufferWriter bufferWriter;
		Utf8JsonWriter writer = Utf8JsonWriterCache.RentWriterAndBuffer(jsonTypeInfo.Options, out bufferWriter);
		try
		{
			jsonTypeInfo.Serialize(writer, in value);
			return JsonNode.Parse(bufferWriter.WrittenMemory.Span, options.GetNodeOptions(), options.GetDocumentOptions());
		}
		finally
		{
			Utf8JsonWriterCache.ReturnWriterAndBuffer(writer, bufferWriter);
		}
	}

	private static JsonNode WriteNodeAsObject(object value, JsonTypeInfo jsonTypeInfo)
	{
		JsonSerializerOptions options = jsonTypeInfo.Options;
		PooledByteBufferWriter bufferWriter;
		Utf8JsonWriter writer = Utf8JsonWriterCache.RentWriterAndBuffer(jsonTypeInfo.Options, out bufferWriter);
		try
		{
			jsonTypeInfo.SerializeAsObject(writer, value);
			return JsonNode.Parse(bufferWriter.WrittenMemory.Span, options.GetNodeOptions(), options.GetDocumentOptions());
		}
		finally
		{
			Utf8JsonWriterCache.ReturnWriterAndBuffer(writer, bufferWriter);
		}
	}

	[RequiresUnreferencedCode("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext, or make sure all of the required types are preserved.")]
	[RequiresDynamicCode("JSON serialization and deserialization might require types that cannot be statically analyzed and might need runtime code generation. Use System.Text.Json source generation for native AOT applications.")]
	private static JsonTypeInfo GetTypeInfo(JsonSerializerOptions options, Type inputType)
	{
		if (options == null)
		{
			options = JsonSerializerOptions.Default;
		}
		options.MakeReadOnly(populateMissingResolver: true);
		if (!(inputType == JsonTypeInfo.ObjectType))
		{
			return options.GetTypeInfoForRootType(inputType);
		}
		return options.ObjectTypeInfo;
	}

	[RequiresUnreferencedCode("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext, or make sure all of the required types are preserved.")]
	[RequiresDynamicCode("JSON serialization and deserialization might require types that cannot be statically analyzed and might need runtime code generation. Use System.Text.Json source generation for native AOT applications.")]
	private static JsonTypeInfo<T> GetTypeInfo<T>(JsonSerializerOptions options)
	{
		return (JsonTypeInfo<T>)GetTypeInfo(options, typeof(T));
	}

	private static JsonTypeInfo GetTypeInfo(JsonSerializerContext context, Type inputType)
	{
		JsonTypeInfo typeInfo = context.GetTypeInfo(inputType);
		if (typeInfo == null)
		{
			ThrowHelper.ThrowInvalidOperationException_NoMetadataForType(inputType, context);
		}
		typeInfo.EnsureConfigured();
		return typeInfo;
	}

	private static void ValidateInputType(object value, Type inputType)
	{
		if ((object)inputType == null)
		{
			ThrowHelper.ThrowArgumentNullException("inputType");
		}
		if (value != null)
		{
			Type type = value.GetType();
			if (!inputType.IsAssignableFrom(type))
			{
				ThrowHelper.ThrowArgumentException_DeserializeWrongType(inputType, value);
			}
		}
	}

	internal static bool IsValidNumberHandlingValue(JsonNumberHandling handling)
	{
		return JsonHelpers.IsInRangeInclusive((int)handling, 0, 7);
	}

	internal static bool IsValidCreationHandlingValue(JsonObjectCreationHandling handling)
	{
		if ((uint)handling <= 1u)
		{
			return true;
		}
		return false;
	}

	internal static bool IsValidUnmappedMemberHandlingValue(JsonUnmappedMemberHandling handling)
	{
		if ((uint)handling <= 1u)
		{
			return true;
		}
		return false;
	}

	[return: NotNullIfNotNull("value")]
	internal static T UnboxOnRead<T>(object value)
	{
		if (value == null)
		{
			if (default(T) != null)
			{
				ThrowUnableToCastValue(value);
			}
			return default(T);
		}
		if (value is T)
		{
			return (T)value;
		}
		ThrowUnableToCastValue(value);
		return default(T);
		static void ThrowUnableToCastValue(object value)
		{
			if (value == null)
			{
				ThrowHelper.ThrowInvalidOperationException_DeserializeUnableToAssignNull(typeof(T));
			}
			else
			{
				ThrowHelper.ThrowInvalidCastException_DeserializeUnableToAssignValue(value.GetType(), typeof(T));
			}
		}
	}

	[return: NotNullIfNotNull("value")]
	internal static T UnboxOnWrite<T>(object value)
	{
		if (default(T) != null && value == null)
		{
			ThrowHelper.ThrowJsonException_DeserializeUnableToConvertValue(typeof(T));
		}
		return (T)value;
	}

	internal static bool TryReadMetadata(JsonConverter converter, JsonTypeInfo jsonTypeInfo, ref Utf8JsonReader reader, scoped ref ReadStack state)
	{
		while (true)
		{
			if (state.Current.PropertyState == StackFramePropertyState.None)
			{
				state.Current.PropertyState = StackFramePropertyState.ReadName;
				if (!reader.Read())
				{
					return false;
				}
			}
			if ((int)state.Current.PropertyState < 2)
			{
				if (reader.TokenType == JsonTokenType.EndObject)
				{
					return true;
				}
				if ((state.Current.MetadataPropertyNames & MetadataPropertyName.Ref) != 0)
				{
					ThrowHelper.ThrowJsonException_MetadataReferenceObjectCannotContainOtherProperties(reader.GetSpan(), ref state);
				}
				ReadOnlySpan<byte> span = reader.GetSpan();
				switch (state.Current.LatestMetadataPropertyName = GetMetadataPropertyName(span, jsonTypeInfo.PolymorphicTypeResolver))
				{
				case MetadataPropertyName.Id:
					state.Current.JsonPropertyName = s_idPropertyName;
					if (state.ReferenceResolver == null)
					{
						ThrowHelper.ThrowJsonException_MetadataUnexpectedProperty(span, ref state);
					}
					if ((state.Current.MetadataPropertyNames & (MetadataPropertyName.Id | MetadataPropertyName.Ref)) != 0)
					{
						ThrowHelper.ThrowJsonException_MetadataIdIsNotFirstProperty(span, ref state);
					}
					if (!converter.CanHaveMetadata)
					{
						ThrowHelper.ThrowJsonException_MetadataCannotParsePreservedObjectIntoImmutable(converter.Type);
					}
					break;
				case MetadataPropertyName.Ref:
					state.Current.JsonPropertyName = s_refPropertyName;
					if (state.ReferenceResolver == null)
					{
						ThrowHelper.ThrowJsonException_MetadataUnexpectedProperty(span, ref state);
					}
					if (converter.IsValueType)
					{
						ThrowHelper.ThrowJsonException_MetadataInvalidReferenceToValueType(converter.Type);
					}
					if (state.Current.MetadataPropertyNames != 0)
					{
						ThrowHelper.ThrowJsonException_MetadataReferenceObjectCannotContainOtherProperties(reader.GetSpan(), ref state);
					}
					break;
				case MetadataPropertyName.Type:
					state.Current.JsonPropertyName = jsonTypeInfo.PolymorphicTypeResolver?.TypeDiscriminatorPropertyNameUtf8 ?? s_typePropertyName;
					if (jsonTypeInfo.PolymorphicTypeResolver == null)
					{
						ThrowHelper.ThrowJsonException_MetadataUnexpectedProperty(span, ref state);
					}
					if (state.PolymorphicTypeDiscriminator != null)
					{
						ThrowHelper.ThrowJsonException_MetadataDuplicateTypeProperty();
					}
					break;
				case MetadataPropertyName.Values:
					state.Current.JsonPropertyName = s_valuesPropertyName;
					if (state.Current.MetadataPropertyNames == MetadataPropertyName.None)
					{
						ThrowHelper.ThrowJsonException_MetadataStandaloneValuesProperty(ref state, span);
					}
					break;
				default:
					return true;
				}
				state.Current.PropertyState = StackFramePropertyState.Name;
			}
			if ((int)state.Current.PropertyState < 3)
			{
				state.Current.PropertyState = StackFramePropertyState.ReadValue;
				if (!reader.Read())
				{
					break;
				}
			}
			switch (state.Current.LatestMetadataPropertyName)
			{
			case MetadataPropertyName.Id:
				if (reader.TokenType != JsonTokenType.String)
				{
					ThrowHelper.ThrowJsonException_MetadataValueWasNotString(reader.TokenType);
				}
				if (state.ReferenceId != null)
				{
					ThrowHelper.ThrowNotSupportedException_ObjectWithParameterizedCtorRefMetadataNotSupported(s_refPropertyName, ref reader, ref state);
				}
				state.ReferenceId = reader.GetString();
				break;
			case MetadataPropertyName.Ref:
				if (reader.TokenType != JsonTokenType.String)
				{
					ThrowHelper.ThrowJsonException_MetadataValueWasNotString(reader.TokenType);
				}
				if (state.ReferenceId != null)
				{
					ThrowHelper.ThrowNotSupportedException_ObjectWithParameterizedCtorRefMetadataNotSupported(s_refPropertyName, ref reader, ref state);
				}
				state.ReferenceId = reader.GetString();
				break;
			case MetadataPropertyName.Type:
				switch (reader.TokenType)
				{
				case JsonTokenType.String:
					state.PolymorphicTypeDiscriminator = reader.GetString();
					break;
				case JsonTokenType.Number:
					state.PolymorphicTypeDiscriminator = reader.GetInt32();
					break;
				default:
					ThrowHelper.ThrowJsonException_MetadataValueWasNotString(reader.TokenType);
					break;
				}
				break;
			case MetadataPropertyName.Values:
				if (reader.TokenType != JsonTokenType.StartArray)
				{
					ThrowHelper.ThrowJsonException_MetadataValuesInvalidToken(reader.TokenType);
				}
				state.Current.PropertyState = StackFramePropertyState.None;
				state.Current.MetadataPropertyNames |= state.Current.LatestMetadataPropertyName;
				return true;
			}
			state.Current.MetadataPropertyNames |= state.Current.LatestMetadataPropertyName;
			state.Current.PropertyState = StackFramePropertyState.None;
			state.Current.JsonPropertyName = null;
		}
		return false;
	}

	internal static bool IsMetadataPropertyName(ReadOnlySpan<byte> propertyName, PolymorphicTypeResolver resolver)
	{
		if (propertyName.Length <= 0 || propertyName[0] != 36)
		{
			if (resolver == null)
			{
				return false;
			}
			return resolver.TypeDiscriminatorPropertyNameUtf8?.AsSpan().SequenceEqual(propertyName) == true;
		}
		return true;
	}

	internal static MetadataPropertyName GetMetadataPropertyName(ReadOnlySpan<byte> propertyName, PolymorphicTypeResolver resolver)
	{
		if (propertyName.Length > 0 && propertyName[0] == 36)
		{
			switch (propertyName.Length)
			{
			case 3:
				if (propertyName[1] == 105 && propertyName[2] == 100)
				{
					return MetadataPropertyName.Id;
				}
				break;
			case 4:
				if (propertyName[1] == 114 && propertyName[2] == 101 && propertyName[3] == 102)
				{
					return MetadataPropertyName.Ref;
				}
				break;
			case 5:
				if (resolver?.TypeDiscriminatorPropertyNameUtf8 == null && propertyName[1] == 116 && propertyName[2] == 121 && propertyName[3] == 112 && propertyName[4] == 101)
				{
					return MetadataPropertyName.Type;
				}
				break;
			case 7:
				if (propertyName[1] == 118 && propertyName[2] == 97 && propertyName[3] == 108 && propertyName[4] == 117 && propertyName[5] == 101 && propertyName[6] == 115)
				{
					return MetadataPropertyName.Values;
				}
				break;
			}
		}
		byte[] array = resolver?.TypeDiscriminatorPropertyNameUtf8;
		if (array != null && propertyName.SequenceEqual(array))
		{
			return MetadataPropertyName.Type;
		}
		return MetadataPropertyName.None;
	}

	internal static bool TryHandleReferenceFromJsonElement(ref Utf8JsonReader reader, scoped ref ReadStack state, JsonElement element, [NotNullWhen(true)] out object referenceValue)
	{
		bool flag = false;
		referenceValue = null;
		if (element.ValueKind == JsonValueKind.Object)
		{
			int num = 0;
			foreach (JsonProperty item in element.EnumerateObject())
			{
				num++;
				if (flag)
				{
					ThrowHelper.ThrowJsonException_MetadataReferenceObjectCannotContainOtherProperties();
					continue;
				}
				if (item.EscapedNameEquals(s_idPropertyName))
				{
					if (state.ReferenceId != null)
					{
						ThrowHelper.ThrowNotSupportedException_ObjectWithParameterizedCtorRefMetadataNotSupported(s_refPropertyName, ref reader, ref state);
					}
					if (item.Value.ValueKind != JsonValueKind.String)
					{
						ThrowHelper.ThrowJsonException_MetadataValueWasNotString(item.Value.ValueKind);
					}
					object obj = element;
					state.ReferenceResolver.AddReference(item.Value.GetString(), obj);
					referenceValue = obj;
					return true;
				}
				if (item.EscapedNameEquals(s_refPropertyName))
				{
					if (state.ReferenceId != null)
					{
						ThrowHelper.ThrowNotSupportedException_ObjectWithParameterizedCtorRefMetadataNotSupported(s_refPropertyName, ref reader, ref state);
					}
					if (num > 1)
					{
						ThrowHelper.ThrowJsonException_MetadataReferenceObjectCannotContainOtherProperties();
					}
					if (item.Value.ValueKind != JsonValueKind.String)
					{
						ThrowHelper.ThrowJsonException_MetadataValueWasNotString(item.Value.ValueKind);
					}
					referenceValue = state.ReferenceResolver.ResolveReference(item.Value.GetString());
					flag = true;
				}
			}
		}
		return flag;
	}

	internal static bool TryHandleReferenceFromJsonNode(ref Utf8JsonReader reader, scoped ref ReadStack state, JsonNode jsonNode, [NotNullWhen(true)] out object referenceValue)
	{
		bool flag = false;
		referenceValue = null;
		if (jsonNode is JsonObject jsonObject)
		{
			int num = 0;
			foreach (KeyValuePair<string, JsonNode> item in jsonObject)
			{
				num++;
				if (flag)
				{
					ThrowHelper.ThrowJsonException_MetadataReferenceObjectCannotContainOtherProperties();
					continue;
				}
				if (item.Key == "$id")
				{
					if (state.ReferenceId != null)
					{
						ThrowHelper.ThrowNotSupportedException_ObjectWithParameterizedCtorRefMetadataNotSupported(s_refPropertyName, ref reader, ref state);
					}
					string referenceId = ReadAsStringMetadataValue(item.Value);
					state.ReferenceResolver.AddReference(referenceId, jsonNode);
					referenceValue = jsonNode;
					return true;
				}
				if (item.Key == "$ref")
				{
					if (state.ReferenceId != null)
					{
						ThrowHelper.ThrowNotSupportedException_ObjectWithParameterizedCtorRefMetadataNotSupported(s_refPropertyName, ref reader, ref state);
					}
					if (num > 1)
					{
						ThrowHelper.ThrowJsonException_MetadataReferenceObjectCannotContainOtherProperties();
					}
					string referenceId2 = ReadAsStringMetadataValue(item.Value);
					referenceValue = state.ReferenceResolver.ResolveReference(referenceId2);
					flag = true;
				}
			}
		}
		return flag;
		static string ReadAsStringMetadataValue(JsonNode jsonNode)
		{
			if (jsonNode is JsonValue jsonValue && jsonValue.TryGetValue<string>(out string value) && value != null)
			{
				return value;
			}
			JsonValueKind jsonValueKind = ((jsonNode == null) ? JsonValueKind.Null : ((jsonNode is JsonObject) ? JsonValueKind.Object : ((jsonNode is JsonArray) ? JsonValueKind.Array : ((jsonNode is JsonValue<JsonElement> jsonValue2) ? jsonValue2.Value.ValueKind : JsonValueKind.Undefined))));
			JsonValueKind valueKind = jsonValueKind;
			ThrowHelper.ThrowJsonException_MetadataValueWasNotString(valueKind);
			return null;
		}
	}

	internal static void ValidateMetadataForObjectConverter(ref ReadStack state)
	{
		if ((state.Current.MetadataPropertyNames & MetadataPropertyName.Values) != 0)
		{
			ThrowHelper.ThrowJsonException_MetadataUnexpectedProperty(s_valuesPropertyName, ref state);
		}
	}

	internal static void ValidateMetadataForArrayConverter(JsonConverter converter, ref Utf8JsonReader reader, scoped ref ReadStack state)
	{
		switch (reader.TokenType)
		{
		case JsonTokenType.EndObject:
			if (state.Current.MetadataPropertyNames != MetadataPropertyName.Ref)
			{
				ThrowHelper.ThrowJsonException_MetadataPreservedArrayValuesNotFound(ref state, converter.Type);
			}
			break;
		default:
			ThrowHelper.ThrowJsonException_MetadataInvalidPropertyInArrayMetadata(ref state, converter.Type, in reader);
			break;
		case JsonTokenType.StartArray:
			break;
		}
	}

	internal static T ResolveReferenceId<T>(ref ReadStack state)
	{
		string referenceId = state.ReferenceId;
		object obj = state.ReferenceResolver.ResolveReference(referenceId);
		state.ReferenceId = null;
		try
		{
			return (T)obj;
		}
		catch (InvalidCastException)
		{
			ThrowHelper.ThrowInvalidOperationException_MetadataReferenceOfTypeCannotBeAssignedToType(referenceId, obj.GetType(), typeof(T));
			return default(T);
		}
	}

	internal static JsonPropertyInfo LookupProperty(object obj, ReadOnlySpan<byte> unescapedPropertyName, ref ReadStack state, JsonSerializerOptions options, out bool useExtensionProperty, bool createExtensionProperty = true)
	{
		JsonTypeInfo jsonTypeInfo = state.Current.JsonTypeInfo;
		useExtensionProperty = false;
		byte[] utf8PropertyName;
		JsonPropertyInfo jsonPropertyInfo = jsonTypeInfo.GetProperty(unescapedPropertyName, ref state.Current, out utf8PropertyName);
		state.Current.PropertyIndex++;
		state.Current.JsonPropertyName = utf8PropertyName;
		if (jsonPropertyInfo == JsonPropertyInfo.s_missingProperty)
		{
			if (jsonTypeInfo.EffectiveUnmappedMemberHandling == JsonUnmappedMemberHandling.Disallow)
			{
				string unmappedPropertyName = JsonHelpers.Utf8GetString(unescapedPropertyName);
				ThrowHelper.ThrowJsonException_UnmappedJsonProperty(jsonTypeInfo.Type, unmappedPropertyName);
			}
			JsonPropertyInfo extensionDataProperty = jsonTypeInfo.ExtensionDataProperty;
			if (extensionDataProperty != null && extensionDataProperty.HasGetter && extensionDataProperty.HasSetter)
			{
				state.Current.JsonPropertyNameAsString = JsonHelpers.Utf8GetString(unescapedPropertyName);
				if (createExtensionProperty)
				{
					CreateExtensionDataProperty(obj, extensionDataProperty, options);
				}
				jsonPropertyInfo = extensionDataProperty;
				useExtensionProperty = true;
			}
		}
		state.Current.JsonPropertyInfo = jsonPropertyInfo;
		state.Current.NumberHandling = jsonPropertyInfo.EffectiveNumberHandling;
		return jsonPropertyInfo;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static ReadOnlySpan<byte> GetPropertyName(scoped ref ReadStack state, ref Utf8JsonReader reader)
	{
		ReadOnlySpan<byte> span = reader.GetSpan();
		ReadOnlySpan<byte> result = ((!reader.ValueIsEscaped) ? span : JsonReaderHelper.GetUnescapedSpan(span));
		if (state.Current.CanContainMetadata && IsMetadataPropertyName(span, state.Current.BaseJsonTypeInfo.PolymorphicTypeResolver))
		{
			ThrowHelper.ThrowUnexpectedMetadataException(span, ref reader, ref state);
		}
		return result;
	}

	internal static void CreateExtensionDataProperty(object obj, JsonPropertyInfo jsonPropertyInfo, JsonSerializerOptions options)
	{
		object valueAsObject = jsonPropertyInfo.GetValueAsObject(obj);
		if (valueAsObject != null)
		{
			return;
		}
		Func<object> func = jsonPropertyInfo.JsonTypeInfo.CreateObject ?? jsonPropertyInfo.JsonTypeInfo.CreateObjectForExtensionDataProperty;
		if (func == null)
		{
			if (jsonPropertyInfo.PropertyType.FullName == "System.Text.Json.Nodes.JsonObject")
			{
				ThrowHelper.ThrowInvalidOperationException_NodeJsonObjectCustomConverterNotAllowedOnExtensionProperty();
			}
			else
			{
				ThrowHelper.ThrowNotSupportedException_SerializationNotSupported(jsonPropertyInfo.PropertyType);
			}
		}
		valueAsObject = func();
		jsonPropertyInfo.Set(obj, valueAsObject);
	}

	[RequiresUnreferencedCode("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext, or make sure all of the required types are preserved.")]
	[RequiresDynamicCode("JSON serialization and deserialization might require types that cannot be statically analyzed and might need runtime code generation. Use System.Text.Json source generation for native AOT applications.")]
	public static TValue? Deserialize<TValue>(ReadOnlySpan<byte> utf8Json, JsonSerializerOptions? options = null)
	{
		JsonTypeInfo<TValue> typeInfo = GetTypeInfo<TValue>(options);
		return ReadFromSpan(utf8Json, typeInfo);
	}

	[RequiresUnreferencedCode("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext, or make sure all of the required types are preserved.")]
	[RequiresDynamicCode("JSON serialization and deserialization might require types that cannot be statically analyzed and might need runtime code generation. Use System.Text.Json source generation for native AOT applications.")]
	public static object? Deserialize(ReadOnlySpan<byte> utf8Json, Type returnType, JsonSerializerOptions? options = null)
	{
		if ((object)returnType == null)
		{
			ThrowHelper.ThrowArgumentNullException("returnType");
		}
		JsonTypeInfo typeInfo = GetTypeInfo(options, returnType);
		return ReadFromSpanAsObject(utf8Json, typeInfo);
	}

	public static TValue? Deserialize<TValue>(ReadOnlySpan<byte> utf8Json, JsonTypeInfo<TValue> jsonTypeInfo)
	{
		if (jsonTypeInfo == null)
		{
			ThrowHelper.ThrowArgumentNullException("jsonTypeInfo");
		}
		jsonTypeInfo.EnsureConfigured();
		return ReadFromSpan(utf8Json, jsonTypeInfo);
	}

	public static object? Deserialize(ReadOnlySpan<byte> utf8Json, JsonTypeInfo jsonTypeInfo)
	{
		if (jsonTypeInfo == null)
		{
			ThrowHelper.ThrowArgumentNullException("jsonTypeInfo");
		}
		jsonTypeInfo.EnsureConfigured();
		return ReadFromSpanAsObject(utf8Json, jsonTypeInfo);
	}

	public static object? Deserialize(ReadOnlySpan<byte> utf8Json, Type returnType, JsonSerializerContext context)
	{
		if ((object)returnType == null)
		{
			ThrowHelper.ThrowArgumentNullException("returnType");
		}
		if (context == null)
		{
			ThrowHelper.ThrowArgumentNullException("context");
		}
		return ReadFromSpanAsObject(utf8Json, GetTypeInfo(context, returnType));
	}

	private static TValue ReadFromSpan<TValue>(ReadOnlySpan<byte> utf8Json, JsonTypeInfo<TValue> jsonTypeInfo, int? actualByteCount = null)
	{
		JsonReaderState state = new JsonReaderState(jsonTypeInfo.Options.GetReaderOptions());
		Utf8JsonReader reader = new Utf8JsonReader(utf8Json, isFinalBlock: true, state);
		ReadStack state2 = default(ReadStack);
		state2.Initialize(jsonTypeInfo);
		return jsonTypeInfo.Deserialize(ref reader, ref state2);
	}

	private static object ReadFromSpanAsObject(ReadOnlySpan<byte> utf8Json, JsonTypeInfo jsonTypeInfo, int? actualByteCount = null)
	{
		JsonReaderState state = new JsonReaderState(jsonTypeInfo.Options.GetReaderOptions());
		Utf8JsonReader reader = new Utf8JsonReader(utf8Json, isFinalBlock: true, state);
		ReadStack state2 = default(ReadStack);
		state2.Initialize(jsonTypeInfo);
		return jsonTypeInfo.DeserializeAsObject(ref reader, ref state2);
	}

	[RequiresUnreferencedCode("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext, or make sure all of the required types are preserved.")]
	[RequiresDynamicCode("JSON serialization and deserialization might require types that cannot be statically analyzed and might need runtime code generation. Use System.Text.Json source generation for native AOT applications.")]
	public static ValueTask<TValue?> DeserializeAsync<TValue>(Stream utf8Json, JsonSerializerOptions? options = null, CancellationToken cancellationToken = default(CancellationToken))
	{
		if (utf8Json == null)
		{
			ThrowHelper.ThrowArgumentNullException("utf8Json");
		}
		JsonTypeInfo<TValue> typeInfo = GetTypeInfo<TValue>(options);
		return typeInfo.DeserializeAsync(utf8Json, cancellationToken);
	}

	[RequiresUnreferencedCode("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext, or make sure all of the required types are preserved.")]
	[RequiresDynamicCode("JSON serialization and deserialization might require types that cannot be statically analyzed and might need runtime code generation. Use System.Text.Json source generation for native AOT applications.")]
	public static TValue? Deserialize<TValue>(Stream utf8Json, JsonSerializerOptions? options = null)
	{
		if (utf8Json == null)
		{
			ThrowHelper.ThrowArgumentNullException("utf8Json");
		}
		JsonTypeInfo<TValue> typeInfo = GetTypeInfo<TValue>(options);
		return typeInfo.Deserialize(utf8Json);
	}

	[RequiresUnreferencedCode("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext, or make sure all of the required types are preserved.")]
	[RequiresDynamicCode("JSON serialization and deserialization might require types that cannot be statically analyzed and might need runtime code generation. Use System.Text.Json source generation for native AOT applications.")]
	public static ValueTask<object?> DeserializeAsync(Stream utf8Json, Type returnType, JsonSerializerOptions? options = null, CancellationToken cancellationToken = default(CancellationToken))
	{
		if (utf8Json == null)
		{
			ThrowHelper.ThrowArgumentNullException("utf8Json");
		}
		if ((object)returnType == null)
		{
			ThrowHelper.ThrowArgumentNullException("returnType");
		}
		JsonTypeInfo typeInfo = GetTypeInfo(options, returnType);
		return typeInfo.DeserializeAsObjectAsync(utf8Json, cancellationToken);
	}

	[RequiresUnreferencedCode("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext, or make sure all of the required types are preserved.")]
	[RequiresDynamicCode("JSON serialization and deserialization might require types that cannot be statically analyzed and might need runtime code generation. Use System.Text.Json source generation for native AOT applications.")]
	public static object? Deserialize(Stream utf8Json, Type returnType, JsonSerializerOptions? options = null)
	{
		if (utf8Json == null)
		{
			ThrowHelper.ThrowArgumentNullException("utf8Json");
		}
		if ((object)returnType == null)
		{
			ThrowHelper.ThrowArgumentNullException("returnType");
		}
		JsonTypeInfo typeInfo = GetTypeInfo(options, returnType);
		return typeInfo.DeserializeAsObject(utf8Json);
	}

	public static ValueTask<TValue?> DeserializeAsync<TValue>(Stream utf8Json, JsonTypeInfo<TValue> jsonTypeInfo, CancellationToken cancellationToken = default(CancellationToken))
	{
		if (utf8Json == null)
		{
			ThrowHelper.ThrowArgumentNullException("utf8Json");
		}
		if (jsonTypeInfo == null)
		{
			ThrowHelper.ThrowArgumentNullException("jsonTypeInfo");
		}
		jsonTypeInfo.EnsureConfigured();
		return jsonTypeInfo.DeserializeAsync(utf8Json, cancellationToken);
	}

	public static ValueTask<object?> DeserializeAsync(Stream utf8Json, JsonTypeInfo jsonTypeInfo, CancellationToken cancellationToken = default(CancellationToken))
	{
		if (utf8Json == null)
		{
			ThrowHelper.ThrowArgumentNullException("utf8Json");
		}
		if (jsonTypeInfo == null)
		{
			ThrowHelper.ThrowArgumentNullException("jsonTypeInfo");
		}
		jsonTypeInfo.EnsureConfigured();
		return jsonTypeInfo.DeserializeAsObjectAsync(utf8Json, cancellationToken);
	}

	public static TValue? Deserialize<TValue>(Stream utf8Json, JsonTypeInfo<TValue> jsonTypeInfo)
	{
		if (utf8Json == null)
		{
			ThrowHelper.ThrowArgumentNullException("utf8Json");
		}
		if (jsonTypeInfo == null)
		{
			ThrowHelper.ThrowArgumentNullException("jsonTypeInfo");
		}
		jsonTypeInfo.EnsureConfigured();
		return jsonTypeInfo.Deserialize(utf8Json);
	}

	public static object? Deserialize(Stream utf8Json, JsonTypeInfo jsonTypeInfo)
	{
		if (utf8Json == null)
		{
			ThrowHelper.ThrowArgumentNullException("utf8Json");
		}
		if (jsonTypeInfo == null)
		{
			ThrowHelper.ThrowArgumentNullException("jsonTypeInfo");
		}
		jsonTypeInfo.EnsureConfigured();
		return jsonTypeInfo.DeserializeAsObject(utf8Json);
	}

	public static ValueTask<object?> DeserializeAsync(Stream utf8Json, Type returnType, JsonSerializerContext context, CancellationToken cancellationToken = default(CancellationToken))
	{
		if (utf8Json == null)
		{
			ThrowHelper.ThrowArgumentNullException("utf8Json");
		}
		if ((object)returnType == null)
		{
			ThrowHelper.ThrowArgumentNullException("returnType");
		}
		if (context == null)
		{
			ThrowHelper.ThrowArgumentNullException("context");
		}
		JsonTypeInfo typeInfo = GetTypeInfo(context, returnType);
		return typeInfo.DeserializeAsObjectAsync(utf8Json, cancellationToken);
	}

	public static object? Deserialize(Stream utf8Json, Type returnType, JsonSerializerContext context)
	{
		if (utf8Json == null)
		{
			ThrowHelper.ThrowArgumentNullException("utf8Json");
		}
		if ((object)returnType == null)
		{
			ThrowHelper.ThrowArgumentNullException("returnType");
		}
		if (context == null)
		{
			ThrowHelper.ThrowArgumentNullException("context");
		}
		JsonTypeInfo typeInfo = GetTypeInfo(context, returnType);
		return typeInfo.DeserializeAsObject(utf8Json);
	}

	[RequiresUnreferencedCode("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext, or make sure all of the required types are preserved.")]
	[RequiresDynamicCode("JSON serialization and deserialization might require types that cannot be statically analyzed and might need runtime code generation. Use System.Text.Json source generation for native AOT applications.")]
	public static IAsyncEnumerable<TValue?> DeserializeAsyncEnumerable<TValue>(Stream utf8Json, JsonSerializerOptions? options = null, CancellationToken cancellationToken = default(CancellationToken))
	{
		if (utf8Json == null)
		{
			ThrowHelper.ThrowArgumentNullException("utf8Json");
		}
		JsonTypeInfo<TValue> typeInfo = GetTypeInfo<TValue>(options);
		return DeserializeAsyncEnumerableCore(utf8Json, typeInfo, cancellationToken);
	}

	public static IAsyncEnumerable<TValue?> DeserializeAsyncEnumerable<TValue>(Stream utf8Json, JsonTypeInfo<TValue> jsonTypeInfo, CancellationToken cancellationToken = default(CancellationToken))
	{
		if (utf8Json == null)
		{
			ThrowHelper.ThrowArgumentNullException("utf8Json");
		}
		if (jsonTypeInfo == null)
		{
			ThrowHelper.ThrowArgumentNullException("jsonTypeInfo");
		}
		jsonTypeInfo.EnsureConfigured();
		return DeserializeAsyncEnumerableCore(utf8Json, jsonTypeInfo, cancellationToken);
	}

	private static IAsyncEnumerable<T> DeserializeAsyncEnumerableCore<T>(Stream utf8Json, JsonTypeInfo<T> jsonTypeInfo, CancellationToken cancellationToken)
	{
		JsonTypeInfo asyncEnumerableQueueTypeInfo = jsonTypeInfo._asyncEnumerableQueueTypeInfo;
		JsonTypeInfo<Queue<T>> queueTypeInfo2 = ((asyncEnumerableQueueTypeInfo != null) ? ((JsonTypeInfo<Queue<T>>)asyncEnumerableQueueTypeInfo) : CreateQueueTypeInfo(jsonTypeInfo));
		return CreateAsyncEnumerable(utf8Json, queueTypeInfo2, cancellationToken);
		static async IAsyncEnumerable<T> CreateAsyncEnumerable(Stream utf8Json, JsonTypeInfo<Queue<T>> queueTypeInfo, [EnumeratorCancellation] CancellationToken cancellationToken)
		{
			JsonSerializerOptions options = queueTypeInfo.Options;
			ReadBufferState bufferState = new ReadBufferState(options.DefaultBufferSize);
			ReadStack readStack = default(ReadStack);
			readStack.Initialize(queueTypeInfo, supportContinuation: true);
			JsonReaderState jsonReaderState = new JsonReaderState(options.GetReaderOptions());
			try
			{
				do
				{
					bufferState = await bufferState.ReadFromStreamAsync(utf8Json, cancellationToken, fillBuffer: false).ConfigureAwait(continueOnCapturedContext: false);
					queueTypeInfo.ContinueDeserialize(ref bufferState, ref jsonReaderState, ref readStack);
					object returnValue = readStack.Current.ReturnValue;
					if (returnValue != null)
					{
						Queue<T> queue = (Queue<T>)returnValue;
						T result;
						while (queue.TryDequeue(out result))
						{
							yield return result;
						}
					}
				}
				while (!bufferState.IsFinalBlock);
			}
			finally
			{
				bufferState.Dispose();
			}
		}
		static JsonTypeInfo<Queue<T>> CreateQueueTypeInfo(JsonTypeInfo<T> jsonTypeInfo)
		{
			QueueOfTConverter<Queue<T>, T> converter = new QueueOfTConverter<Queue<T>, T>();
			JsonTypeInfo<Queue<T>> jsonTypeInfo2 = new JsonTypeInfo<Queue<T>>(converter, jsonTypeInfo.Options)
			{
				CreateObject = () => new Queue<T>(),
				ElementTypeInfo = jsonTypeInfo,
				NumberHandling = jsonTypeInfo.Options.NumberHandling
			};
			jsonTypeInfo2.EnsureConfigured();
			jsonTypeInfo._asyncEnumerableQueueTypeInfo = jsonTypeInfo2;
			return jsonTypeInfo2;
		}
	}

	[RequiresUnreferencedCode("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext, or make sure all of the required types are preserved.")]
	[RequiresDynamicCode("JSON serialization and deserialization might require types that cannot be statically analyzed and might need runtime code generation. Use System.Text.Json source generation for native AOT applications.")]
	public static TValue? Deserialize<TValue>([StringSyntax("Json")] string json, JsonSerializerOptions? options = null)
	{
		if (json == null)
		{
			ThrowHelper.ThrowArgumentNullException("json");
		}
		JsonTypeInfo<TValue> typeInfo = GetTypeInfo<TValue>(options);
		return ReadFromSpan(json.AsSpan(), typeInfo);
	}

	[RequiresUnreferencedCode("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext, or make sure all of the required types are preserved.")]
	[RequiresDynamicCode("JSON serialization and deserialization might require types that cannot be statically analyzed and might need runtime code generation. Use System.Text.Json source generation for native AOT applications.")]
	public static TValue? Deserialize<TValue>([StringSyntax("Json")] ReadOnlySpan<char> json, JsonSerializerOptions? options = null)
	{
		JsonTypeInfo<TValue> typeInfo = GetTypeInfo<TValue>(options);
		return ReadFromSpan(json, typeInfo);
	}

	[RequiresUnreferencedCode("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext, or make sure all of the required types are preserved.")]
	[RequiresDynamicCode("JSON serialization and deserialization might require types that cannot be statically analyzed and might need runtime code generation. Use System.Text.Json source generation for native AOT applications.")]
	public static object? Deserialize([StringSyntax("Json")] string json, Type returnType, JsonSerializerOptions? options = null)
	{
		if (json == null)
		{
			ThrowHelper.ThrowArgumentNullException("json");
		}
		if ((object)returnType == null)
		{
			ThrowHelper.ThrowArgumentNullException("returnType");
		}
		JsonTypeInfo typeInfo = GetTypeInfo(options, returnType);
		return ReadFromSpanAsObject(json.AsSpan(), typeInfo);
	}

	[RequiresUnreferencedCode("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext, or make sure all of the required types are preserved.")]
	[RequiresDynamicCode("JSON serialization and deserialization might require types that cannot be statically analyzed and might need runtime code generation. Use System.Text.Json source generation for native AOT applications.")]
	public static object? Deserialize([StringSyntax("Json")] ReadOnlySpan<char> json, Type returnType, JsonSerializerOptions? options = null)
	{
		if ((object)returnType == null)
		{
			ThrowHelper.ThrowArgumentNullException("returnType");
		}
		JsonTypeInfo typeInfo = GetTypeInfo(options, returnType);
		return ReadFromSpanAsObject(json, typeInfo);
	}

	public static TValue? Deserialize<TValue>([StringSyntax("Json")] string json, JsonTypeInfo<TValue> jsonTypeInfo)
	{
		if (json == null)
		{
			ThrowHelper.ThrowArgumentNullException("json");
		}
		if (jsonTypeInfo == null)
		{
			ThrowHelper.ThrowArgumentNullException("jsonTypeInfo");
		}
		jsonTypeInfo.EnsureConfigured();
		return ReadFromSpan(json.AsSpan(), jsonTypeInfo);
	}

	public static TValue? Deserialize<TValue>([StringSyntax("Json")] ReadOnlySpan<char> json, JsonTypeInfo<TValue> jsonTypeInfo)
	{
		if (jsonTypeInfo == null)
		{
			ThrowHelper.ThrowArgumentNullException("jsonTypeInfo");
		}
		jsonTypeInfo.EnsureConfigured();
		return ReadFromSpan(json, jsonTypeInfo);
	}

	public static object? Deserialize([StringSyntax("Json")] string json, JsonTypeInfo jsonTypeInfo)
	{
		if (json == null)
		{
			ThrowHelper.ThrowArgumentNullException("json");
		}
		if (jsonTypeInfo == null)
		{
			ThrowHelper.ThrowArgumentNullException("jsonTypeInfo");
		}
		jsonTypeInfo.EnsureConfigured();
		return ReadFromSpanAsObject(json.AsSpan(), jsonTypeInfo);
	}

	public static object? Deserialize([StringSyntax("Json")] ReadOnlySpan<char> json, JsonTypeInfo jsonTypeInfo)
	{
		if (jsonTypeInfo == null)
		{
			ThrowHelper.ThrowArgumentNullException("jsonTypeInfo");
		}
		jsonTypeInfo.EnsureConfigured();
		return ReadFromSpanAsObject(json, jsonTypeInfo);
	}

	public static object? Deserialize([StringSyntax("Json")] string json, Type returnType, JsonSerializerContext context)
	{
		if (json == null)
		{
			ThrowHelper.ThrowArgumentNullException("json");
		}
		if ((object)returnType == null)
		{
			ThrowHelper.ThrowArgumentNullException("returnType");
		}
		if (context == null)
		{
			ThrowHelper.ThrowArgumentNullException("context");
		}
		JsonTypeInfo typeInfo = GetTypeInfo(context, returnType);
		return ReadFromSpanAsObject(json.AsSpan(), typeInfo);
	}

	public static object? Deserialize([StringSyntax("Json")] ReadOnlySpan<char> json, Type returnType, JsonSerializerContext context)
	{
		if ((object)returnType == null)
		{
			ThrowHelper.ThrowArgumentNullException("returnType");
		}
		if (context == null)
		{
			ThrowHelper.ThrowArgumentNullException("context");
		}
		JsonTypeInfo typeInfo = GetTypeInfo(context, returnType);
		return ReadFromSpanAsObject(json, typeInfo);
	}

	private static TValue ReadFromSpan<TValue>(ReadOnlySpan<char> json, JsonTypeInfo<TValue> jsonTypeInfo)
	{
		byte[] array = null;
		Span<byte> span = (((long)json.Length > 349525L) ? new byte[JsonReaderHelper.GetUtf8ByteCount(json)] : (array = ArrayPool<byte>.Shared.Rent(json.Length * 3)));
		try
		{
			int utf8FromText = JsonReaderHelper.GetUtf8FromText(json, span);
			span = span.Slice(0, utf8FromText);
			return ReadFromSpan(span, jsonTypeInfo, utf8FromText);
		}
		finally
		{
			if (array != null)
			{
				span.Clear();
				ArrayPool<byte>.Shared.Return(array);
			}
		}
	}

	private static object ReadFromSpanAsObject(ReadOnlySpan<char> json, JsonTypeInfo jsonTypeInfo)
	{
		byte[] array = null;
		Span<byte> span = (((long)json.Length > 349525L) ? new byte[JsonReaderHelper.GetUtf8ByteCount(json)] : (array = ArrayPool<byte>.Shared.Rent(json.Length * 3)));
		try
		{
			int utf8FromText = JsonReaderHelper.GetUtf8FromText(json, span);
			span = span.Slice(0, utf8FromText);
			return ReadFromSpanAsObject(span, jsonTypeInfo, utf8FromText);
		}
		finally
		{
			if (array != null)
			{
				span.Clear();
				ArrayPool<byte>.Shared.Return(array);
			}
		}
	}

	[RequiresUnreferencedCode("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext, or make sure all of the required types are preserved.")]
	[RequiresDynamicCode("JSON serialization and deserialization might require types that cannot be statically analyzed and might need runtime code generation. Use System.Text.Json source generation for native AOT applications.")]
	public static TValue? Deserialize<TValue>(ref Utf8JsonReader reader, JsonSerializerOptions? options = null)
	{
		JsonTypeInfo<TValue> typeInfo = GetTypeInfo<TValue>(options);
		return Read(ref reader, typeInfo);
	}

	[RequiresUnreferencedCode("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext, or make sure all of the required types are preserved.")]
	[RequiresDynamicCode("JSON serialization and deserialization might require types that cannot be statically analyzed and might need runtime code generation. Use System.Text.Json source generation for native AOT applications.")]
	public static object? Deserialize(ref Utf8JsonReader reader, Type returnType, JsonSerializerOptions? options = null)
	{
		if ((object)returnType == null)
		{
			ThrowHelper.ThrowArgumentNullException("returnType");
		}
		JsonTypeInfo typeInfo = GetTypeInfo(options, returnType);
		return ReadAsObject(ref reader, typeInfo);
	}

	public static TValue? Deserialize<TValue>(ref Utf8JsonReader reader, JsonTypeInfo<TValue> jsonTypeInfo)
	{
		if (jsonTypeInfo == null)
		{
			ThrowHelper.ThrowArgumentNullException("jsonTypeInfo");
		}
		jsonTypeInfo.EnsureConfigured();
		return Read(ref reader, jsonTypeInfo);
	}

	public static object? Deserialize(ref Utf8JsonReader reader, JsonTypeInfo jsonTypeInfo)
	{
		if (jsonTypeInfo == null)
		{
			ThrowHelper.ThrowArgumentNullException("jsonTypeInfo");
		}
		jsonTypeInfo.EnsureConfigured();
		return ReadAsObject(ref reader, jsonTypeInfo);
	}

	public static object? Deserialize(ref Utf8JsonReader reader, Type returnType, JsonSerializerContext context)
	{
		if ((object)returnType == null)
		{
			ThrowHelper.ThrowArgumentNullException("returnType");
		}
		if (context == null)
		{
			ThrowHelper.ThrowArgumentNullException("context");
		}
		return ReadAsObject(ref reader, GetTypeInfo(context, returnType));
	}

	private static TValue Read<TValue>(ref Utf8JsonReader reader, JsonTypeInfo<TValue> jsonTypeInfo)
	{
		if (reader.CurrentState.Options.CommentHandling == JsonCommentHandling.Allow)
		{
			ThrowHelper.ThrowArgumentException_SerializerDoesNotSupportComments("reader");
		}
		ReadStack state = default(ReadStack);
		state.Initialize(jsonTypeInfo);
		Utf8JsonReader utf8JsonReader = reader;
		try
		{
			Utf8JsonReader reader2 = GetReaderScopedToNextValue(ref reader, ref state);
			return jsonTypeInfo.Deserialize(ref reader2, ref state);
		}
		catch (JsonException)
		{
			reader = utf8JsonReader;
			throw;
		}
	}

	private static object ReadAsObject(ref Utf8JsonReader reader, JsonTypeInfo jsonTypeInfo)
	{
		if (reader.CurrentState.Options.CommentHandling == JsonCommentHandling.Allow)
		{
			ThrowHelper.ThrowArgumentException_SerializerDoesNotSupportComments("reader");
		}
		ReadStack state = default(ReadStack);
		state.Initialize(jsonTypeInfo);
		Utf8JsonReader utf8JsonReader = reader;
		try
		{
			Utf8JsonReader reader2 = GetReaderScopedToNextValue(ref reader, ref state);
			return jsonTypeInfo.DeserializeAsObject(ref reader2, ref state);
		}
		catch (JsonException)
		{
			reader = utf8JsonReader;
			throw;
		}
	}

	private static Utf8JsonReader GetReaderScopedToNextValue(ref Utf8JsonReader reader, scoped ref ReadStack state)
	{
		ReadOnlySpan<byte> jsonData = default(ReadOnlySpan<byte>);
		ReadOnlySequence<byte> jsonData2 = default(ReadOnlySequence<byte>);
		try
		{
			JsonTokenType tokenType = reader.TokenType;
			ReadOnlySpan<byte> bytes;
			if ((tokenType == JsonTokenType.None || tokenType == JsonTokenType.PropertyName) && !reader.Read())
			{
				bytes = default(ReadOnlySpan<byte>);
				ThrowHelper.ThrowJsonReaderException(ref reader, ExceptionResource.ExpectedOneCompleteToken, 0, bytes);
			}
			switch (reader.TokenType)
			{
			case JsonTokenType.StartObject:
			case JsonTokenType.StartArray:
			{
				long tokenStartIndex = reader.TokenStartIndex;
				if (!reader.TrySkip())
				{
					bytes = default(ReadOnlySpan<byte>);
					ThrowHelper.ThrowJsonReaderException(ref reader, ExceptionResource.NotEnoughData, 0, bytes);
				}
				long num2 = reader.BytesConsumed - tokenStartIndex;
				ReadOnlySequence<byte> originalSequence = reader.OriginalSequence;
				if (originalSequence.IsEmpty)
				{
					bytes = reader.OriginalSpan;
					jsonData = checked(bytes.Slice((int)tokenStartIndex, (int)num2));
				}
				else
				{
					jsonData2 = originalSequence.Slice(tokenStartIndex, num2);
				}
				break;
			}
			case JsonTokenType.Number:
			case JsonTokenType.True:
			case JsonTokenType.False:
			case JsonTokenType.Null:
				if (reader.HasValueSequence)
				{
					jsonData2 = reader.ValueSequence;
				}
				else
				{
					jsonData = reader.ValueSpan;
				}
				break;
			case JsonTokenType.String:
			{
				ReadOnlySequence<byte> originalSequence2 = reader.OriginalSequence;
				if (originalSequence2.IsEmpty)
				{
					bytes = reader.ValueSpan;
					int length = bytes.Length + 2;
					jsonData = reader.OriginalSpan.Slice((int)reader.TokenStartIndex, length);
					break;
				}
				long num3;
				if (!reader.HasValueSequence)
				{
					bytes = reader.ValueSpan;
					num3 = bytes.Length + 2;
				}
				else
				{
					num3 = reader.ValueSequence.Length + 2;
				}
				long length2 = num3;
				jsonData2 = originalSequence2.Slice(reader.TokenStartIndex, length2);
				break;
			}
			default:
			{
				byte num;
				if (!reader.HasValueSequence)
				{
					bytes = reader.ValueSpan;
					num = bytes[0];
				}
				else
				{
					bytes = reader.ValueSequence.First.Span;
					num = bytes[0];
				}
				byte nextByte = num;
				bytes = default(ReadOnlySpan<byte>);
				ThrowHelper.ThrowJsonReaderException(ref reader, ExceptionResource.ExpectedStartOfValueNotFound, nextByte, bytes);
				break;
			}
			}
		}
		catch (JsonReaderException ex)
		{
			ThrowHelper.ReThrowWithPath(ref state, ex);
		}
		if (!jsonData.IsEmpty)
		{
			return new Utf8JsonReader(jsonData, reader.CurrentState.Options);
		}
		return new Utf8JsonReader(jsonData2, reader.CurrentState.Options);
	}

	[RequiresUnreferencedCode("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext, or make sure all of the required types are preserved.")]
	[RequiresDynamicCode("JSON serialization and deserialization might require types that cannot be statically analyzed and might need runtime code generation. Use System.Text.Json source generation for native AOT applications.")]
	public static byte[] SerializeToUtf8Bytes<TValue>(TValue value, JsonSerializerOptions? options = null)
	{
		JsonTypeInfo<TValue> typeInfo = GetTypeInfo<TValue>(options);
		return WriteBytes<TValue>(in value, typeInfo);
	}

	[RequiresUnreferencedCode("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext, or make sure all of the required types are preserved.")]
	[RequiresDynamicCode("JSON serialization and deserialization might require types that cannot be statically analyzed and might need runtime code generation. Use System.Text.Json source generation for native AOT applications.")]
	public static byte[] SerializeToUtf8Bytes(object? value, Type inputType, JsonSerializerOptions? options = null)
	{
		ValidateInputType(value, inputType);
		JsonTypeInfo typeInfo = GetTypeInfo(options, inputType);
		return WriteBytesAsObject(value, typeInfo);
	}

	public static byte[] SerializeToUtf8Bytes<TValue>(TValue value, JsonTypeInfo<TValue> jsonTypeInfo)
	{
		if (jsonTypeInfo == null)
		{
			ThrowHelper.ThrowArgumentNullException("jsonTypeInfo");
		}
		jsonTypeInfo.EnsureConfigured();
		return WriteBytes(in value, jsonTypeInfo);
	}

	public static byte[] SerializeToUtf8Bytes(object? value, JsonTypeInfo jsonTypeInfo)
	{
		if (jsonTypeInfo == null)
		{
			ThrowHelper.ThrowArgumentNullException("jsonTypeInfo");
		}
		jsonTypeInfo.EnsureConfigured();
		return WriteBytesAsObject(value, jsonTypeInfo);
	}

	public static byte[] SerializeToUtf8Bytes(object? value, Type inputType, JsonSerializerContext context)
	{
		if (context == null)
		{
			ThrowHelper.ThrowArgumentNullException("context");
		}
		ValidateInputType(value, inputType);
		JsonTypeInfo typeInfo = GetTypeInfo(context, inputType);
		return WriteBytesAsObject(value, typeInfo);
	}

	private static byte[] WriteBytes<TValue>(in TValue value, JsonTypeInfo<TValue> jsonTypeInfo)
	{
		PooledByteBufferWriter bufferWriter;
		Utf8JsonWriter writer = Utf8JsonWriterCache.RentWriterAndBuffer(jsonTypeInfo.Options, out bufferWriter);
		try
		{
			jsonTypeInfo.Serialize(writer, in value);
			return bufferWriter.WrittenMemory.ToArray();
		}
		finally
		{
			Utf8JsonWriterCache.ReturnWriterAndBuffer(writer, bufferWriter);
		}
	}

	private static byte[] WriteBytesAsObject(object value, JsonTypeInfo jsonTypeInfo)
	{
		PooledByteBufferWriter bufferWriter;
		Utf8JsonWriter writer = Utf8JsonWriterCache.RentWriterAndBuffer(jsonTypeInfo.Options, out bufferWriter);
		try
		{
			jsonTypeInfo.SerializeAsObject(writer, value);
			return bufferWriter.WrittenMemory.ToArray();
		}
		finally
		{
			Utf8JsonWriterCache.ReturnWriterAndBuffer(writer, bufferWriter);
		}
	}

	internal static MetadataPropertyName WriteMetadataForObject(JsonConverter jsonConverter, ref WriteStack state, Utf8JsonWriter writer)
	{
		MetadataPropertyName metadataPropertyName = MetadataPropertyName.None;
		if (state.NewReferenceId != null)
		{
			writer.WriteString(s_metadataId, state.NewReferenceId);
			metadataPropertyName |= MetadataPropertyName.Id;
			state.NewReferenceId = null;
		}
		object polymorphicTypeDiscriminator = state.PolymorphicTypeDiscriminator;
		if (polymorphicTypeDiscriminator != null)
		{
			JsonEncodedText? customTypeDiscriminatorPropertyNameJsonEncoded = state.PolymorphicTypeResolver.CustomTypeDiscriminatorPropertyNameJsonEncoded;
			JsonEncodedText jsonEncodedText;
			if (customTypeDiscriminatorPropertyNameJsonEncoded.HasValue)
			{
				JsonEncodedText valueOrDefault = customTypeDiscriminatorPropertyNameJsonEncoded.GetValueOrDefault();
				jsonEncodedText = valueOrDefault;
			}
			else
			{
				jsonEncodedText = s_metadataType;
			}
			JsonEncodedText propertyName = jsonEncodedText;
			if (polymorphicTypeDiscriminator is string value)
			{
				writer.WriteString(propertyName, value);
			}
			else
			{
				writer.WriteNumber(propertyName, (int)polymorphicTypeDiscriminator);
			}
			metadataPropertyName |= MetadataPropertyName.Type;
			state.PolymorphicTypeDiscriminator = null;
		}
		return metadataPropertyName;
	}

	internal static MetadataPropertyName WriteMetadataForCollection(JsonConverter jsonConverter, ref WriteStack state, Utf8JsonWriter writer)
	{
		writer.WriteStartObject();
		MetadataPropertyName result = WriteMetadataForObject(jsonConverter, ref state, writer);
		writer.WritePropertyName(s_metadataValues);
		return result;
	}

	internal static bool TryGetReferenceForValue(object currentValue, ref WriteStack state, Utf8JsonWriter writer)
	{
		bool alreadyExists;
		string reference = state.ReferenceResolver.GetReference(currentValue, out alreadyExists);
		if (alreadyExists)
		{
			writer.WriteStartObject();
			writer.WriteString(s_metadataRef, reference);
			writer.WriteEndObject();
			state.PolymorphicTypeDiscriminator = null;
			state.PolymorphicTypeResolver = null;
		}
		else
		{
			state.NewReferenceId = reference;
		}
		return alreadyExists;
	}

	[RequiresUnreferencedCode("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext, or make sure all of the required types are preserved.")]
	[RequiresDynamicCode("JSON serialization and deserialization might require types that cannot be statically analyzed and might need runtime code generation. Use System.Text.Json source generation for native AOT applications.")]
	public static Task SerializeAsync<TValue>(Stream utf8Json, TValue value, JsonSerializerOptions? options = null, CancellationToken cancellationToken = default(CancellationToken))
	{
		if (utf8Json == null)
		{
			ThrowHelper.ThrowArgumentNullException("utf8Json");
		}
		JsonTypeInfo<TValue> typeInfo = GetTypeInfo<TValue>(options);
		return typeInfo.SerializeAsync(utf8Json, value, cancellationToken);
	}

	[RequiresUnreferencedCode("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext, or make sure all of the required types are preserved.")]
	[RequiresDynamicCode("JSON serialization and deserialization might require types that cannot be statically analyzed and might need runtime code generation. Use System.Text.Json source generation for native AOT applications.")]
	public static void Serialize<TValue>(Stream utf8Json, TValue value, JsonSerializerOptions? options = null)
	{
		if (utf8Json == null)
		{
			ThrowHelper.ThrowArgumentNullException("utf8Json");
		}
		JsonTypeInfo<TValue> typeInfo = GetTypeInfo<TValue>(options);
		typeInfo.Serialize(utf8Json, in value);
	}

	[RequiresUnreferencedCode("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext, or make sure all of the required types are preserved.")]
	[RequiresDynamicCode("JSON serialization and deserialization might require types that cannot be statically analyzed and might need runtime code generation. Use System.Text.Json source generation for native AOT applications.")]
	public static Task SerializeAsync(Stream utf8Json, object? value, Type inputType, JsonSerializerOptions? options = null, CancellationToken cancellationToken = default(CancellationToken))
	{
		if (utf8Json == null)
		{
			ThrowHelper.ThrowArgumentNullException("utf8Json");
		}
		ValidateInputType(value, inputType);
		JsonTypeInfo typeInfo = GetTypeInfo(options, inputType);
		return typeInfo.SerializeAsObjectAsync(utf8Json, value, cancellationToken);
	}

	[RequiresUnreferencedCode("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext, or make sure all of the required types are preserved.")]
	[RequiresDynamicCode("JSON serialization and deserialization might require types that cannot be statically analyzed and might need runtime code generation. Use System.Text.Json source generation for native AOT applications.")]
	public static void Serialize(Stream utf8Json, object? value, Type inputType, JsonSerializerOptions? options = null)
	{
		if (utf8Json == null)
		{
			ThrowHelper.ThrowArgumentNullException("utf8Json");
		}
		ValidateInputType(value, inputType);
		JsonTypeInfo typeInfo = GetTypeInfo(options, inputType);
		typeInfo.SerializeAsObject(utf8Json, value);
	}

	public static Task SerializeAsync<TValue>(Stream utf8Json, TValue value, JsonTypeInfo<TValue> jsonTypeInfo, CancellationToken cancellationToken = default(CancellationToken))
	{
		if (utf8Json == null)
		{
			ThrowHelper.ThrowArgumentNullException("utf8Json");
		}
		if (jsonTypeInfo == null)
		{
			ThrowHelper.ThrowArgumentNullException("jsonTypeInfo");
		}
		jsonTypeInfo.EnsureConfigured();
		return jsonTypeInfo.SerializeAsync(utf8Json, value, cancellationToken);
	}

	public static void Serialize<TValue>(Stream utf8Json, TValue value, JsonTypeInfo<TValue> jsonTypeInfo)
	{
		if (utf8Json == null)
		{
			ThrowHelper.ThrowArgumentNullException("utf8Json");
		}
		if (jsonTypeInfo == null)
		{
			ThrowHelper.ThrowArgumentNullException("jsonTypeInfo");
		}
		jsonTypeInfo.EnsureConfigured();
		jsonTypeInfo.Serialize(utf8Json, in value);
	}

	public static Task SerializeAsync(Stream utf8Json, object? value, JsonTypeInfo jsonTypeInfo, CancellationToken cancellationToken = default(CancellationToken))
	{
		if (utf8Json == null)
		{
			ThrowHelper.ThrowArgumentNullException("utf8Json");
		}
		if (jsonTypeInfo == null)
		{
			ThrowHelper.ThrowArgumentNullException("jsonTypeInfo");
		}
		jsonTypeInfo.EnsureConfigured();
		return jsonTypeInfo.SerializeAsObjectAsync(utf8Json, value, cancellationToken);
	}

	public static void Serialize(Stream utf8Json, object? value, JsonTypeInfo jsonTypeInfo)
	{
		if (utf8Json == null)
		{
			ThrowHelper.ThrowArgumentNullException("utf8Json");
		}
		if (jsonTypeInfo == null)
		{
			ThrowHelper.ThrowArgumentNullException("jsonTypeInfo");
		}
		jsonTypeInfo.EnsureConfigured();
		jsonTypeInfo.SerializeAsObject(utf8Json, value);
	}

	public static Task SerializeAsync(Stream utf8Json, object? value, Type inputType, JsonSerializerContext context, CancellationToken cancellationToken = default(CancellationToken))
	{
		if (utf8Json == null)
		{
			ThrowHelper.ThrowArgumentNullException("utf8Json");
		}
		if (context == null)
		{
			ThrowHelper.ThrowArgumentNullException("context");
		}
		ValidateInputType(value, inputType);
		JsonTypeInfo typeInfo = GetTypeInfo(context, inputType);
		return typeInfo.SerializeAsObjectAsync(utf8Json, value, cancellationToken);
	}

	public static void Serialize(Stream utf8Json, object? value, Type inputType, JsonSerializerContext context)
	{
		if (utf8Json == null)
		{
			ThrowHelper.ThrowArgumentNullException("utf8Json");
		}
		if (context == null)
		{
			ThrowHelper.ThrowArgumentNullException("context");
		}
		ValidateInputType(value, inputType);
		JsonTypeInfo typeInfo = GetTypeInfo(context, inputType);
		typeInfo.SerializeAsObject(utf8Json, value);
	}

	[RequiresUnreferencedCode("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext, or make sure all of the required types are preserved.")]
	[RequiresDynamicCode("JSON serialization and deserialization might require types that cannot be statically analyzed and might need runtime code generation. Use System.Text.Json source generation for native AOT applications.")]
	public static string Serialize<TValue>(TValue value, JsonSerializerOptions? options = null)
	{
		JsonTypeInfo<TValue> typeInfo = GetTypeInfo<TValue>(options);
		return WriteString<TValue>(in value, typeInfo);
	}

	[RequiresUnreferencedCode("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext, or make sure all of the required types are preserved.")]
	[RequiresDynamicCode("JSON serialization and deserialization might require types that cannot be statically analyzed and might need runtime code generation. Use System.Text.Json source generation for native AOT applications.")]
	public static string Serialize(object? value, Type inputType, JsonSerializerOptions? options = null)
	{
		ValidateInputType(value, inputType);
		JsonTypeInfo typeInfo = GetTypeInfo(options, inputType);
		return WriteStringAsObject(value, typeInfo);
	}

	public static string Serialize<TValue>(TValue value, JsonTypeInfo<TValue> jsonTypeInfo)
	{
		if (jsonTypeInfo == null)
		{
			ThrowHelper.ThrowArgumentNullException("jsonTypeInfo");
		}
		jsonTypeInfo.EnsureConfigured();
		return WriteString(in value, jsonTypeInfo);
	}

	public static string Serialize(object? value, JsonTypeInfo jsonTypeInfo)
	{
		if (jsonTypeInfo == null)
		{
			ThrowHelper.ThrowArgumentNullException("jsonTypeInfo");
		}
		jsonTypeInfo.EnsureConfigured();
		return WriteStringAsObject(value, jsonTypeInfo);
	}

	public static string Serialize(object? value, Type inputType, JsonSerializerContext context)
	{
		if (context == null)
		{
			ThrowHelper.ThrowArgumentNullException("context");
		}
		ValidateInputType(value, inputType);
		JsonTypeInfo typeInfo = GetTypeInfo(context, inputType);
		return WriteStringAsObject(value, typeInfo);
	}

	private static string WriteString<TValue>(in TValue value, JsonTypeInfo<TValue> jsonTypeInfo)
	{
		PooledByteBufferWriter bufferWriter;
		Utf8JsonWriter writer = Utf8JsonWriterCache.RentWriterAndBuffer(jsonTypeInfo.Options, out bufferWriter);
		try
		{
			jsonTypeInfo.Serialize(writer, in value);
			return JsonReaderHelper.TranscodeHelper(bufferWriter.WrittenMemory.Span);
		}
		finally
		{
			Utf8JsonWriterCache.ReturnWriterAndBuffer(writer, bufferWriter);
		}
	}

	private static string WriteStringAsObject(object value, JsonTypeInfo jsonTypeInfo)
	{
		PooledByteBufferWriter bufferWriter;
		Utf8JsonWriter writer = Utf8JsonWriterCache.RentWriterAndBuffer(jsonTypeInfo.Options, out bufferWriter);
		try
		{
			jsonTypeInfo.SerializeAsObject(writer, value);
			return JsonReaderHelper.TranscodeHelper(bufferWriter.WrittenMemory.Span);
		}
		finally
		{
			Utf8JsonWriterCache.ReturnWriterAndBuffer(writer, bufferWriter);
		}
	}

	[RequiresUnreferencedCode("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext, or make sure all of the required types are preserved.")]
	[RequiresDynamicCode("JSON serialization and deserialization might require types that cannot be statically analyzed and might need runtime code generation. Use System.Text.Json source generation for native AOT applications.")]
	public static void Serialize<TValue>(Utf8JsonWriter writer, TValue value, JsonSerializerOptions? options = null)
	{
		if (writer == null)
		{
			ThrowHelper.ThrowArgumentNullException("writer");
		}
		JsonTypeInfo<TValue> typeInfo = GetTypeInfo<TValue>(options);
		typeInfo.Serialize(writer, in value);
	}

	[RequiresUnreferencedCode("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext, or make sure all of the required types are preserved.")]
	[RequiresDynamicCode("JSON serialization and deserialization might require types that cannot be statically analyzed and might need runtime code generation. Use System.Text.Json source generation for native AOT applications.")]
	public static void Serialize(Utf8JsonWriter writer, object? value, Type inputType, JsonSerializerOptions? options = null)
	{
		if (writer == null)
		{
			ThrowHelper.ThrowArgumentNullException("writer");
		}
		ValidateInputType(value, inputType);
		JsonTypeInfo typeInfo = GetTypeInfo(options, inputType);
		typeInfo.SerializeAsObject(writer, value);
	}

	public static void Serialize<TValue>(Utf8JsonWriter writer, TValue value, JsonTypeInfo<TValue> jsonTypeInfo)
	{
		if (writer == null)
		{
			ThrowHelper.ThrowArgumentNullException("writer");
		}
		if (jsonTypeInfo == null)
		{
			ThrowHelper.ThrowArgumentNullException("jsonTypeInfo");
		}
		jsonTypeInfo.EnsureConfigured();
		jsonTypeInfo.Serialize(writer, in value);
	}

	public static void Serialize(Utf8JsonWriter writer, object? value, JsonTypeInfo jsonTypeInfo)
	{
		if (writer == null)
		{
			ThrowHelper.ThrowArgumentNullException("writer");
		}
		if (jsonTypeInfo == null)
		{
			ThrowHelper.ThrowArgumentNullException("jsonTypeInfo");
		}
		jsonTypeInfo.EnsureConfigured();
		jsonTypeInfo.SerializeAsObject(writer, value);
	}

	public static void Serialize(Utf8JsonWriter writer, object? value, Type inputType, JsonSerializerContext context)
	{
		if (writer == null)
		{
			ThrowHelper.ThrowArgumentNullException("writer");
		}
		if (context == null)
		{
			ThrowHelper.ThrowArgumentNullException("context");
		}
		ValidateInputType(value, inputType);
		JsonTypeInfo typeInfo = GetTypeInfo(context, inputType);
		typeInfo.SerializeAsObject(writer, value);
	}
}
