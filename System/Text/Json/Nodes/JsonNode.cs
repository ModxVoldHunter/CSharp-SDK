using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text.Json.Serialization.Converters;
using System.Text.Json.Serialization.Metadata;
using System.Threading;
using System.Threading.Tasks;

namespace System.Text.Json.Nodes;

public abstract class JsonNode
{
	private JsonNode _parent;

	private JsonNodeOptions? _options;

	public JsonNodeOptions? Options
	{
		get
		{
			if (!_options.HasValue && Parent != null)
			{
				_options = Parent.Options;
			}
			return _options;
		}
	}

	public JsonNode? Parent
	{
		get
		{
			return _parent;
		}
		internal set
		{
			_parent = value;
		}
	}

	public JsonNode Root
	{
		get
		{
			JsonNode parent = Parent;
			if (parent == null)
			{
				return this;
			}
			while (parent.Parent != null)
			{
				parent = parent.Parent;
			}
			return parent;
		}
	}

	public JsonNode? this[int index]
	{
		get
		{
			return AsArray().GetItem(index);
		}
		set
		{
			AsArray().SetItem(index, value);
		}
	}

	public JsonNode? this[string propertyName]
	{
		get
		{
			return AsObject().GetItem(propertyName);
		}
		set
		{
			AsObject().SetItem(propertyName, value);
		}
	}

	internal JsonNode(JsonNodeOptions? options = null)
	{
		_options = options;
	}

	public JsonArray AsArray()
	{
		JsonArray jsonArray = this as JsonArray;
		if (jsonArray == null)
		{
			ThrowHelper.ThrowInvalidOperationException_NodeWrongType("JsonArray");
		}
		return jsonArray;
	}

	public JsonObject AsObject()
	{
		JsonObject jsonObject = this as JsonObject;
		if (jsonObject == null)
		{
			ThrowHelper.ThrowInvalidOperationException_NodeWrongType("JsonObject");
		}
		return jsonObject;
	}

	public JsonValue AsValue()
	{
		JsonValue jsonValue = this as JsonValue;
		if (jsonValue == null)
		{
			ThrowHelper.ThrowInvalidOperationException_NodeWrongType("JsonValue");
		}
		return jsonValue;
	}

	public string GetPath()
	{
		if (Parent == null)
		{
			return "$";
		}
		List<string> list = new List<string>();
		GetPath(list, null);
		StringBuilder stringBuilder = new StringBuilder("$");
		for (int num = list.Count - 1; num >= 0; num--)
		{
			stringBuilder.Append(list[num]);
		}
		return stringBuilder.ToString();
	}

	internal abstract void GetPath(List<string> path, JsonNode child);

	public virtual T GetValue<T>()
	{
		throw new InvalidOperationException(System.SR.Format(System.SR.NodeWrongType, "JsonValue"));
	}

	public JsonNode DeepClone()
	{
		return DeepCloneCore();
	}

	internal abstract JsonNode DeepCloneCore();

	public JsonValueKind GetValueKind()
	{
		return GetValueKindCore();
	}

	internal abstract JsonValueKind GetValueKindCore();

	public string GetPropertyName()
	{
		JsonObject jsonObject = _parent as JsonObject;
		if (jsonObject == null)
		{
			ThrowHelper.ThrowInvalidOperationException_NodeParentWrongType("JsonObject");
		}
		return jsonObject.GetPropertyName(this);
	}

	public int GetElementIndex()
	{
		JsonArray jsonArray = _parent as JsonArray;
		if (jsonArray == null)
		{
			ThrowHelper.ThrowInvalidOperationException_NodeParentWrongType("JsonArray");
		}
		return jsonArray.GetElementIndex(this);
	}

	public static bool DeepEquals(JsonNode? node1, JsonNode? node2)
	{
		return node1?.DeepEqualsCore(node2) ?? (node2 == null);
	}

	internal abstract bool DeepEqualsCore(JsonNode node);

	[RequiresUnreferencedCode("Creating JsonValue instances with non-primitive types is not compatible with trimming. It can result in non-primitive types being serialized, which may have their members trimmed.")]
	[RequiresDynamicCode("Creating JsonValue instances with non-primitive types requires generating code at runtime.")]
	public void ReplaceWith<T>(T value)
	{
		JsonNode parent = _parent;
		if (!(parent is JsonObject jsonObject))
		{
			if (parent is JsonArray jsonArray)
			{
				JsonNode value2 = ConvertFromValue(value);
				jsonArray.SetItem(GetElementIndex(), value2);
			}
		}
		else
		{
			JsonNode value2 = ConvertFromValue(value);
			jsonObject.SetItem(GetPropertyName(), value2);
		}
	}

	internal void AssignParent(JsonNode parent)
	{
		if (Parent != null)
		{
			ThrowHelper.ThrowInvalidOperationException_NodeAlreadyHasParent();
		}
		for (JsonNode jsonNode = parent; jsonNode != null; jsonNode = jsonNode.Parent)
		{
			if (jsonNode == this)
			{
				ThrowHelper.ThrowInvalidOperationException_NodeCycleDetected();
			}
		}
		Parent = parent;
	}

	[RequiresUnreferencedCode("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext, or make sure all of the required types are preserved.")]
	[RequiresDynamicCode("JSON serialization and deserialization might require types that cannot be statically analyzed and might need runtime code generation. Use System.Text.Json source generation for native AOT applications.")]
	internal static JsonNode ConvertFromValue<T>(T value, JsonNodeOptions? options = null)
	{
		if (value == null)
		{
			return null;
		}
		if (value is JsonNode result)
		{
			return result;
		}
		if (value is JsonElement element)
		{
			return JsonNodeConverter.Create(element, options);
		}
		JsonTypeInfo<T> jsonTypeInfo = (JsonTypeInfo<T>)JsonSerializerOptions.Default.GetTypeInfo(typeof(T));
		return new JsonValueCustomized<T>(value, jsonTypeInfo, options);
	}

	public static implicit operator JsonNode(bool value)
	{
		return JsonValue.Create(value);
	}

	public static implicit operator JsonNode?(bool? value)
	{
		return JsonValue.Create(value);
	}

	public static implicit operator JsonNode(byte value)
	{
		return JsonValue.Create(value);
	}

	public static implicit operator JsonNode?(byte? value)
	{
		return JsonValue.Create(value);
	}

	public static implicit operator JsonNode(char value)
	{
		return JsonValue.Create(value);
	}

	public static implicit operator JsonNode?(char? value)
	{
		return JsonValue.Create(value);
	}

	public static implicit operator JsonNode(DateTime value)
	{
		return JsonValue.Create(value);
	}

	public static implicit operator JsonNode?(DateTime? value)
	{
		return JsonValue.Create(value);
	}

	public static implicit operator JsonNode(DateTimeOffset value)
	{
		return JsonValue.Create(value);
	}

	public static implicit operator JsonNode?(DateTimeOffset? value)
	{
		return JsonValue.Create(value);
	}

	public static implicit operator JsonNode(decimal value)
	{
		return JsonValue.Create(value);
	}

	public static implicit operator JsonNode?(decimal? value)
	{
		return JsonValue.Create(value);
	}

	public static implicit operator JsonNode(double value)
	{
		return JsonValue.Create(value);
	}

	public static implicit operator JsonNode?(double? value)
	{
		return JsonValue.Create(value);
	}

	public static implicit operator JsonNode(Guid value)
	{
		return JsonValue.Create(value);
	}

	public static implicit operator JsonNode?(Guid? value)
	{
		return JsonValue.Create(value);
	}

	public static implicit operator JsonNode(short value)
	{
		return JsonValue.Create(value);
	}

	public static implicit operator JsonNode?(short? value)
	{
		return JsonValue.Create(value);
	}

	public static implicit operator JsonNode(int value)
	{
		return JsonValue.Create(value);
	}

	public static implicit operator JsonNode?(int? value)
	{
		return JsonValue.Create(value);
	}

	public static implicit operator JsonNode(long value)
	{
		return JsonValue.Create(value);
	}

	public static implicit operator JsonNode?(long? value)
	{
		return JsonValue.Create(value);
	}

	[CLSCompliant(false)]
	public static implicit operator JsonNode(sbyte value)
	{
		return JsonValue.Create(value);
	}

	[CLSCompliant(false)]
	public static implicit operator JsonNode?(sbyte? value)
	{
		return JsonValue.Create(value);
	}

	public static implicit operator JsonNode(float value)
	{
		return JsonValue.Create(value);
	}

	public static implicit operator JsonNode?(float? value)
	{
		return JsonValue.Create(value);
	}

	[return: NotNullIfNotNull("value")]
	public static implicit operator JsonNode?(string? value)
	{
		return JsonValue.Create(value);
	}

	[CLSCompliant(false)]
	public static implicit operator JsonNode(ushort value)
	{
		return JsonValue.Create(value);
	}

	[CLSCompliant(false)]
	public static implicit operator JsonNode?(ushort? value)
	{
		return JsonValue.Create(value);
	}

	[CLSCompliant(false)]
	public static implicit operator JsonNode(uint value)
	{
		return JsonValue.Create(value);
	}

	[CLSCompliant(false)]
	public static implicit operator JsonNode?(uint? value)
	{
		return JsonValue.Create(value);
	}

	[CLSCompliant(false)]
	public static implicit operator JsonNode(ulong value)
	{
		return JsonValue.Create(value);
	}

	[CLSCompliant(false)]
	public static implicit operator JsonNode?(ulong? value)
	{
		return JsonValue.Create(value);
	}

	public static explicit operator bool(JsonNode value)
	{
		return value.GetValue<bool>();
	}

	public static explicit operator bool?(JsonNode? value)
	{
		return value?.GetValue<bool>();
	}

	public static explicit operator byte(JsonNode value)
	{
		return value.GetValue<byte>();
	}

	public static explicit operator byte?(JsonNode? value)
	{
		return value?.GetValue<byte>();
	}

	public static explicit operator char(JsonNode value)
	{
		return value.GetValue<char>();
	}

	public static explicit operator char?(JsonNode? value)
	{
		return value?.GetValue<char>();
	}

	public static explicit operator DateTime(JsonNode value)
	{
		return value.GetValue<DateTime>();
	}

	public static explicit operator DateTime?(JsonNode? value)
	{
		return value?.GetValue<DateTime>();
	}

	public static explicit operator DateTimeOffset(JsonNode value)
	{
		return value.GetValue<DateTimeOffset>();
	}

	public static explicit operator DateTimeOffset?(JsonNode? value)
	{
		return value?.GetValue<DateTimeOffset>();
	}

	public static explicit operator decimal(JsonNode value)
	{
		return value.GetValue<decimal>();
	}

	public static explicit operator decimal?(JsonNode? value)
	{
		return value?.GetValue<decimal>();
	}

	public static explicit operator double(JsonNode value)
	{
		return value.GetValue<double>();
	}

	public static explicit operator double?(JsonNode? value)
	{
		return value?.GetValue<double>();
	}

	public static explicit operator Guid(JsonNode value)
	{
		return value.GetValue<Guid>();
	}

	public static explicit operator Guid?(JsonNode? value)
	{
		return value?.GetValue<Guid>();
	}

	public static explicit operator short(JsonNode value)
	{
		return value.GetValue<short>();
	}

	public static explicit operator short?(JsonNode? value)
	{
		return value?.GetValue<short>();
	}

	public static explicit operator int(JsonNode value)
	{
		return value.GetValue<int>();
	}

	public static explicit operator int?(JsonNode? value)
	{
		return value?.GetValue<int>();
	}

	public static explicit operator long(JsonNode value)
	{
		return value.GetValue<long>();
	}

	public static explicit operator long?(JsonNode? value)
	{
		return value?.GetValue<long>();
	}

	[CLSCompliant(false)]
	public static explicit operator sbyte(JsonNode value)
	{
		return value.GetValue<sbyte>();
	}

	[CLSCompliant(false)]
	public static explicit operator sbyte?(JsonNode? value)
	{
		return value?.GetValue<sbyte>();
	}

	public static explicit operator float(JsonNode value)
	{
		return value.GetValue<float>();
	}

	public static explicit operator float?(JsonNode? value)
	{
		return value?.GetValue<float>();
	}

	public static explicit operator string?(JsonNode? value)
	{
		return value?.GetValue<string>();
	}

	[CLSCompliant(false)]
	public static explicit operator ushort(JsonNode value)
	{
		return value.GetValue<ushort>();
	}

	[CLSCompliant(false)]
	public static explicit operator ushort?(JsonNode? value)
	{
		return value?.GetValue<ushort>();
	}

	[CLSCompliant(false)]
	public static explicit operator uint(JsonNode value)
	{
		return value.GetValue<uint>();
	}

	[CLSCompliant(false)]
	public static explicit operator uint?(JsonNode? value)
	{
		return value?.GetValue<uint>();
	}

	[CLSCompliant(false)]
	public static explicit operator ulong(JsonNode value)
	{
		return value.GetValue<ulong>();
	}

	[CLSCompliant(false)]
	public static explicit operator ulong?(JsonNode? value)
	{
		return value?.GetValue<ulong>();
	}

	public static JsonNode? Parse(ref Utf8JsonReader reader, JsonNodeOptions? nodeOptions = null)
	{
		JsonElement element = JsonElement.ParseValue(ref reader);
		return JsonNodeConverter.Create(element, nodeOptions);
	}

	public static JsonNode? Parse([StringSyntax("Json")] string json, JsonNodeOptions? nodeOptions = null, JsonDocumentOptions documentOptions = default(JsonDocumentOptions))
	{
		if (json == null)
		{
			ThrowHelper.ThrowArgumentNullException("json");
		}
		JsonElement element = JsonElement.ParseValue(json, documentOptions);
		return JsonNodeConverter.Create(element, nodeOptions);
	}

	public static JsonNode? Parse(ReadOnlySpan<byte> utf8Json, JsonNodeOptions? nodeOptions = null, JsonDocumentOptions documentOptions = default(JsonDocumentOptions))
	{
		JsonElement element = JsonElement.ParseValue(utf8Json, documentOptions);
		return JsonNodeConverter.Create(element, nodeOptions);
	}

	public static JsonNode? Parse(Stream utf8Json, JsonNodeOptions? nodeOptions = null, JsonDocumentOptions documentOptions = default(JsonDocumentOptions))
	{
		if (utf8Json == null)
		{
			ThrowHelper.ThrowArgumentNullException("utf8Json");
		}
		JsonElement element = JsonElement.ParseValue(utf8Json, documentOptions);
		return JsonNodeConverter.Create(element, nodeOptions);
	}

	public static async Task<JsonNode?> ParseAsync(Stream utf8Json, JsonNodeOptions? nodeOptions = null, JsonDocumentOptions documentOptions = default(JsonDocumentOptions), CancellationToken cancellationToken = default(CancellationToken))
	{
		if (utf8Json == null)
		{
			ThrowHelper.ThrowArgumentNullException("utf8Json");
		}
		return JsonNodeConverter.Create((await JsonDocument.ParseAsyncCoreUnrented(utf8Json, documentOptions, cancellationToken).ConfigureAwait(continueOnCapturedContext: false)).RootElement, nodeOptions);
	}

	public string ToJsonString(JsonSerializerOptions? options = null)
	{
		using PooledByteBufferWriter pooledByteBufferWriter = WriteToPooledBuffer(options, options?.GetWriterOptions() ?? default(JsonWriterOptions));
		return JsonHelpers.Utf8GetString(pooledByteBufferWriter.WrittenMemory.Span);
	}

	public override string ToString()
	{
		if (this is JsonValue)
		{
			if (this is JsonValue<string> jsonValue)
			{
				return jsonValue.Value;
			}
			if (this is JsonValue<JsonElement> jsonValue2 && jsonValue2.Value.ValueKind == JsonValueKind.String)
			{
				return jsonValue2.Value.GetString();
			}
		}
		using PooledByteBufferWriter pooledByteBufferWriter = WriteToPooledBuffer(null, new JsonWriterOptions
		{
			Indented = true
		});
		return JsonHelpers.Utf8GetString(pooledByteBufferWriter.WrittenMemory.Span);
	}

	public abstract void WriteTo(Utf8JsonWriter writer, JsonSerializerOptions? options = null);

	internal PooledByteBufferWriter WriteToPooledBuffer(JsonSerializerOptions options = null, JsonWriterOptions writerOptions = default(JsonWriterOptions), int bufferSize = 16384)
	{
		PooledByteBufferWriter pooledByteBufferWriter = new PooledByteBufferWriter(bufferSize);
		using Utf8JsonWriter writer = new Utf8JsonWriter(pooledByteBufferWriter, writerOptions);
		WriteTo(writer, options);
		return pooledByteBufferWriter;
	}
}
