using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization.Metadata;

namespace System.Text.Json.Nodes;

public abstract class JsonValue : JsonNode
{
	internal const string CreateUnreferencedCodeMessage = "Creating JsonValue instances with non-primitive types is not compatible with trimming. It can result in non-primitive types being serialized, which may have their members trimmed.";

	internal const string CreateDynamicCodeMessage = "Creating JsonValue instances with non-primitive types requires generating code at runtime.";

	public static JsonValue Create(bool value, JsonNodeOptions? options = null)
	{
		return new JsonValuePrimitive<bool>(value, JsonMetadataServices.BooleanConverter);
	}

	public static JsonValue? Create(bool? value, JsonNodeOptions? options = null)
	{
		if (!value.HasValue)
		{
			return null;
		}
		return new JsonValuePrimitive<bool>(value.Value, JsonMetadataServices.BooleanConverter);
	}

	public static JsonValue Create(byte value, JsonNodeOptions? options = null)
	{
		return new JsonValuePrimitive<byte>(value, JsonMetadataServices.ByteConverter);
	}

	public static JsonValue? Create(byte? value, JsonNodeOptions? options = null)
	{
		if (!value.HasValue)
		{
			return null;
		}
		return new JsonValuePrimitive<byte>(value.Value, JsonMetadataServices.ByteConverter);
	}

	public static JsonValue Create(char value, JsonNodeOptions? options = null)
	{
		return new JsonValuePrimitive<char>(value, JsonMetadataServices.CharConverter);
	}

	public static JsonValue? Create(char? value, JsonNodeOptions? options = null)
	{
		if (!value.HasValue)
		{
			return null;
		}
		return new JsonValuePrimitive<char>(value.Value, JsonMetadataServices.CharConverter);
	}

	public static JsonValue Create(DateTime value, JsonNodeOptions? options = null)
	{
		return new JsonValuePrimitive<DateTime>(value, JsonMetadataServices.DateTimeConverter);
	}

	public static JsonValue? Create(DateTime? value, JsonNodeOptions? options = null)
	{
		if (!value.HasValue)
		{
			return null;
		}
		return new JsonValuePrimitive<DateTime>(value.Value, JsonMetadataServices.DateTimeConverter);
	}

	public static JsonValue Create(DateTimeOffset value, JsonNodeOptions? options = null)
	{
		return new JsonValuePrimitive<DateTimeOffset>(value, JsonMetadataServices.DateTimeOffsetConverter);
	}

	public static JsonValue? Create(DateTimeOffset? value, JsonNodeOptions? options = null)
	{
		if (!value.HasValue)
		{
			return null;
		}
		return new JsonValuePrimitive<DateTimeOffset>(value.Value, JsonMetadataServices.DateTimeOffsetConverter);
	}

	public static JsonValue Create(decimal value, JsonNodeOptions? options = null)
	{
		return new JsonValuePrimitive<decimal>(value, JsonMetadataServices.DecimalConverter);
	}

	public static JsonValue? Create(decimal? value, JsonNodeOptions? options = null)
	{
		if (!value.HasValue)
		{
			return null;
		}
		return new JsonValuePrimitive<decimal>(value.Value, JsonMetadataServices.DecimalConverter);
	}

	public static JsonValue Create(double value, JsonNodeOptions? options = null)
	{
		return new JsonValuePrimitive<double>(value, JsonMetadataServices.DoubleConverter);
	}

	public static JsonValue? Create(double? value, JsonNodeOptions? options = null)
	{
		if (!value.HasValue)
		{
			return null;
		}
		return new JsonValuePrimitive<double>(value.Value, JsonMetadataServices.DoubleConverter);
	}

	public static JsonValue Create(Guid value, JsonNodeOptions? options = null)
	{
		return new JsonValuePrimitive<Guid>(value, JsonMetadataServices.GuidConverter);
	}

	public static JsonValue? Create(Guid? value, JsonNodeOptions? options = null)
	{
		if (!value.HasValue)
		{
			return null;
		}
		return new JsonValuePrimitive<Guid>(value.Value, JsonMetadataServices.GuidConverter);
	}

	public static JsonValue Create(short value, JsonNodeOptions? options = null)
	{
		return new JsonValuePrimitive<short>(value, JsonMetadataServices.Int16Converter);
	}

	public static JsonValue? Create(short? value, JsonNodeOptions? options = null)
	{
		if (!value.HasValue)
		{
			return null;
		}
		return new JsonValuePrimitive<short>(value.Value, JsonMetadataServices.Int16Converter);
	}

	public static JsonValue Create(int value, JsonNodeOptions? options = null)
	{
		return new JsonValuePrimitive<int>(value, JsonMetadataServices.Int32Converter);
	}

	public static JsonValue? Create(int? value, JsonNodeOptions? options = null)
	{
		if (!value.HasValue)
		{
			return null;
		}
		return new JsonValuePrimitive<int>(value.Value, JsonMetadataServices.Int32Converter);
	}

	public static JsonValue Create(long value, JsonNodeOptions? options = null)
	{
		return new JsonValuePrimitive<long>(value, JsonMetadataServices.Int64Converter);
	}

	public static JsonValue? Create(long? value, JsonNodeOptions? options = null)
	{
		if (!value.HasValue)
		{
			return null;
		}
		return new JsonValuePrimitive<long>(value.Value, JsonMetadataServices.Int64Converter);
	}

	[CLSCompliant(false)]
	public static JsonValue Create(sbyte value, JsonNodeOptions? options = null)
	{
		return new JsonValuePrimitive<sbyte>(value, JsonMetadataServices.SByteConverter);
	}

	[CLSCompliant(false)]
	public static JsonValue? Create(sbyte? value, JsonNodeOptions? options = null)
	{
		if (!value.HasValue)
		{
			return null;
		}
		return new JsonValuePrimitive<sbyte>(value.Value, JsonMetadataServices.SByteConverter);
	}

	public static JsonValue Create(float value, JsonNodeOptions? options = null)
	{
		return new JsonValuePrimitive<float>(value, JsonMetadataServices.SingleConverter);
	}

	public static JsonValue? Create(float? value, JsonNodeOptions? options = null)
	{
		if (!value.HasValue)
		{
			return null;
		}
		return new JsonValuePrimitive<float>(value.Value, JsonMetadataServices.SingleConverter);
	}

	[return: NotNullIfNotNull("value")]
	public static JsonValue? Create(string? value, JsonNodeOptions? options = null)
	{
		if (value == null)
		{
			return null;
		}
		return new JsonValuePrimitive<string>(value, JsonMetadataServices.StringConverter);
	}

	[CLSCompliant(false)]
	public static JsonValue Create(ushort value, JsonNodeOptions? options = null)
	{
		return new JsonValuePrimitive<ushort>(value, JsonMetadataServices.UInt16Converter);
	}

	[CLSCompliant(false)]
	public static JsonValue? Create(ushort? value, JsonNodeOptions? options = null)
	{
		if (!value.HasValue)
		{
			return null;
		}
		return new JsonValuePrimitive<ushort>(value.Value, JsonMetadataServices.UInt16Converter);
	}

	[CLSCompliant(false)]
	public static JsonValue Create(uint value, JsonNodeOptions? options = null)
	{
		return new JsonValuePrimitive<uint>(value, JsonMetadataServices.UInt32Converter);
	}

	[CLSCompliant(false)]
	public static JsonValue? Create(uint? value, JsonNodeOptions? options = null)
	{
		if (!value.HasValue)
		{
			return null;
		}
		return new JsonValuePrimitive<uint>(value.Value, JsonMetadataServices.UInt32Converter);
	}

	[CLSCompliant(false)]
	public static JsonValue Create(ulong value, JsonNodeOptions? options = null)
	{
		return new JsonValuePrimitive<ulong>(value, JsonMetadataServices.UInt64Converter);
	}

	[CLSCompliant(false)]
	public static JsonValue? Create(ulong? value, JsonNodeOptions? options = null)
	{
		if (!value.HasValue)
		{
			return null;
		}
		return new JsonValuePrimitive<ulong>(value.Value, JsonMetadataServices.UInt64Converter);
	}

	public static JsonValue? Create(JsonElement value, JsonNodeOptions? options = null)
	{
		if (value.ValueKind == JsonValueKind.Null)
		{
			return null;
		}
		VerifyJsonElementIsNotArrayOrObject(ref value);
		return new JsonValuePrimitive<JsonElement>(value, JsonMetadataServices.JsonElementConverter);
	}

	public static JsonValue? Create(JsonElement? value, JsonNodeOptions? options = null)
	{
		if (!value.HasValue)
		{
			return null;
		}
		JsonElement element = value.Value;
		if (element.ValueKind == JsonValueKind.Null)
		{
			return null;
		}
		VerifyJsonElementIsNotArrayOrObject(ref element);
		return new JsonValuePrimitive<JsonElement>(element, JsonMetadataServices.JsonElementConverter);
	}

	private protected JsonValue(JsonNodeOptions? options = null)
		: base(options)
	{
	}

	[RequiresUnreferencedCode("Creating JsonValue instances with non-primitive types is not compatible with trimming. It can result in non-primitive types being serialized, which may have their members trimmed. Use the overload that takes a JsonTypeInfo, or make sure all of the required types are preserved.")]
	[RequiresDynamicCode("Creating JsonValue instances with non-primitive types requires generating code at runtime.")]
	public static JsonValue? Create<T>(T? value, JsonNodeOptions? options = null)
	{
		if (value == null)
		{
			return null;
		}
		if (value is JsonElement)
		{
			object obj = value;
			JsonElement element = (JsonElement)((obj is JsonElement) ? obj : null);
			if (element.ValueKind == JsonValueKind.Null)
			{
				return null;
			}
			VerifyJsonElementIsNotArrayOrObject(ref element);
			return new JsonValuePrimitive<JsonElement>(element, JsonMetadataServices.JsonElementConverter, options);
		}
		JsonTypeInfo<T> jsonTypeInfo = (JsonTypeInfo<T>)JsonSerializerOptions.Default.GetTypeInfo(typeof(T));
		return new JsonValueCustomized<T>(value, jsonTypeInfo, options);
	}

	public static JsonValue? Create<T>(T? value, JsonTypeInfo<T> jsonTypeInfo, JsonNodeOptions? options = null)
	{
		if (jsonTypeInfo == null)
		{
			ThrowHelper.ThrowArgumentNullException("jsonTypeInfo");
		}
		if (value == null)
		{
			return null;
		}
		if (value is JsonElement)
		{
			object obj = value;
			JsonElement element = (JsonElement)((obj is JsonElement) ? obj : null);
			if (element.ValueKind == JsonValueKind.Null)
			{
				return null;
			}
			VerifyJsonElementIsNotArrayOrObject(ref element);
		}
		jsonTypeInfo.EnsureConfigured();
		return new JsonValueCustomized<T>(value, jsonTypeInfo, options);
	}

	internal override void GetPath(List<string> path, JsonNode child)
	{
		base.Parent?.GetPath(path, this);
	}

	public abstract bool TryGetValue<T>([NotNullWhen(true)] out T? value);

	private static void VerifyJsonElementIsNotArrayOrObject(ref JsonElement element)
	{
		JsonValueKind valueKind = element.ValueKind;
		if (valueKind - 1 <= JsonValueKind.Object)
		{
			ThrowHelper.ThrowInvalidOperationException_NodeElementCannotBeObjectOrArray();
		}
	}
}
[DebuggerDisplay("{ToJsonString(),nq}")]
[DebuggerTypeProxy(typeof(JsonValue<>.DebugView))]
internal abstract class JsonValue<TValue> : JsonValue
{
	[ExcludeFromCodeCoverage]
	[DebuggerDisplay("{Json,nq}")]
	private sealed class DebugView
	{
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public JsonValue<TValue> _node;

		public string Json => _node.ToJsonString();

		public string Path => _node.GetPath();

		public TValue Value => _node.Value;

		public DebugView(JsonValue<TValue> node)
		{
			_node = node;
		}
	}

	internal readonly TValue Value;

	protected JsonValue(TValue value, JsonNodeOptions? options = null)
		: base(options)
	{
		if (value is JsonNode)
		{
			ThrowHelper.ThrowArgumentException_NodeValueNotAllowed("value");
		}
		Value = value;
	}

	public override T GetValue<T>()
	{
		TValue value = Value;
		if (value is T)
		{
			object obj = value;
			return (T)((obj is T) ? obj : null);
		}
		if (Value is JsonElement)
		{
			return ConvertJsonElement<T>();
		}
		throw new InvalidOperationException(System.SR.Format(System.SR.NodeUnableToConvert, Value.GetType(), typeof(T)));
	}

	public override bool TryGetValue<T>([NotNullWhen(true)] out T value)
	{
		TValue value2 = Value;
		if (value2 is T val)
		{
			value = val;
			return true;
		}
		if (Value is JsonElement)
		{
			return TryConvertJsonElement<T>(out value);
		}
		value = default(T);
		return false;
	}

	internal sealed override JsonValueKind GetValueKindCore()
	{
		TValue value = Value;
		if (value is JsonElement jsonElement)
		{
			return jsonElement.ValueKind;
		}
		using PooledByteBufferWriter pooledByteBufferWriter = WriteToPooledBuffer();
		return JsonElement.ParseValue(pooledByteBufferWriter.WrittenMemory.Span, default(JsonDocumentOptions)).ValueKind;
	}

	internal sealed override bool DeepEqualsCore(JsonNode otherNode)
	{
		if (otherNode == null)
		{
			return false;
		}
		TValue value = Value;
		ReadOnlyMemory<byte> readOnlyMemory;
		if (value is JsonElement jsonElement && otherNode is JsonValue<JsonElement> { Value: var value2 })
		{
			if (jsonElement.ValueKind != value2.ValueKind)
			{
				return false;
			}
			switch (jsonElement.ValueKind)
			{
			case JsonValueKind.True:
			case JsonValueKind.False:
			case JsonValueKind.Null:
				return true;
			case JsonValueKind.String:
				return jsonElement.ValueEquals(value2.GetString());
			case JsonValueKind.Number:
			{
				readOnlyMemory = jsonElement.GetRawValue();
				ReadOnlySpan<byte> span = readOnlyMemory.Span;
				readOnlyMemory = value2.GetRawValue();
				return span.SequenceEqual(readOnlyMemory.Span);
			}
			default:
				return false;
			}
		}
		using PooledByteBufferWriter pooledByteBufferWriter = WriteToPooledBuffer();
		using PooledByteBufferWriter pooledByteBufferWriter2 = otherNode.WriteToPooledBuffer();
		readOnlyMemory = pooledByteBufferWriter.WrittenMemory;
		ReadOnlySpan<byte> span2 = readOnlyMemory.Span;
		readOnlyMemory = pooledByteBufferWriter2.WrittenMemory;
		return span2.SequenceEqual(readOnlyMemory.Span);
	}

	internal TypeToConvert ConvertJsonElement<TypeToConvert>()
	{
		JsonElement jsonElement = (JsonElement)(object)Value;
		switch (jsonElement.ValueKind)
		{
		case JsonValueKind.Number:
			if (typeof(TypeToConvert) == typeof(int) || typeof(TypeToConvert) == typeof(int?))
			{
				return (TypeToConvert)(object)jsonElement.GetInt32();
			}
			if (typeof(TypeToConvert) == typeof(long) || typeof(TypeToConvert) == typeof(long?))
			{
				return (TypeToConvert)(object)jsonElement.GetInt64();
			}
			if (typeof(TypeToConvert) == typeof(double) || typeof(TypeToConvert) == typeof(double?))
			{
				return (TypeToConvert)(object)jsonElement.GetDouble();
			}
			if (typeof(TypeToConvert) == typeof(short) || typeof(TypeToConvert) == typeof(short?))
			{
				return (TypeToConvert)(object)jsonElement.GetInt16();
			}
			if (typeof(TypeToConvert) == typeof(decimal) || typeof(TypeToConvert) == typeof(decimal?))
			{
				return (TypeToConvert)(object)jsonElement.GetDecimal();
			}
			if (typeof(TypeToConvert) == typeof(byte) || typeof(TypeToConvert) == typeof(byte?))
			{
				return (TypeToConvert)(object)jsonElement.GetByte();
			}
			if (typeof(TypeToConvert) == typeof(float) || typeof(TypeToConvert) == typeof(float?))
			{
				return (TypeToConvert)(object)jsonElement.GetSingle();
			}
			if (typeof(TypeToConvert) == typeof(uint) || typeof(TypeToConvert) == typeof(uint?))
			{
				return (TypeToConvert)(object)jsonElement.GetUInt32();
			}
			if (typeof(TypeToConvert) == typeof(ushort) || typeof(TypeToConvert) == typeof(ushort?))
			{
				return (TypeToConvert)(object)jsonElement.GetUInt16();
			}
			if (typeof(TypeToConvert) == typeof(ulong) || typeof(TypeToConvert) == typeof(ulong?))
			{
				return (TypeToConvert)(object)jsonElement.GetUInt64();
			}
			if (typeof(TypeToConvert) == typeof(sbyte) || typeof(TypeToConvert) == typeof(sbyte?))
			{
				return (TypeToConvert)(object)jsonElement.GetSByte();
			}
			break;
		case JsonValueKind.String:
			if (typeof(TypeToConvert) == typeof(string))
			{
				return (TypeToConvert)(object)jsonElement.GetString();
			}
			if (typeof(TypeToConvert) == typeof(DateTime) || typeof(TypeToConvert) == typeof(DateTime?))
			{
				return (TypeToConvert)(object)jsonElement.GetDateTime();
			}
			if (typeof(TypeToConvert) == typeof(DateTimeOffset) || typeof(TypeToConvert) == typeof(DateTimeOffset?))
			{
				return (TypeToConvert)(object)jsonElement.GetDateTimeOffset();
			}
			if (typeof(TypeToConvert) == typeof(Guid) || typeof(TypeToConvert) == typeof(Guid?))
			{
				return (TypeToConvert)(object)jsonElement.GetGuid();
			}
			if (typeof(TypeToConvert) == typeof(char) || typeof(TypeToConvert) == typeof(char?))
			{
				string @string = jsonElement.GetString();
				if (@string.Length == 1)
				{
					return (TypeToConvert)(object)@string[0];
				}
			}
			break;
		case JsonValueKind.True:
		case JsonValueKind.False:
			if (typeof(TypeToConvert) == typeof(bool) || typeof(TypeToConvert) == typeof(bool?))
			{
				return (TypeToConvert)(object)jsonElement.GetBoolean();
			}
			break;
		}
		throw new InvalidOperationException(System.SR.Format(System.SR.NodeUnableToConvertElement, jsonElement.ValueKind, typeof(TypeToConvert)));
	}

	internal bool TryConvertJsonElement<TypeToConvert>([NotNullWhen(true)] out TypeToConvert result)
	{
		JsonElement jsonElement = (JsonElement)(object)Value;
		switch (jsonElement.ValueKind)
		{
		case JsonValueKind.Number:
			if (typeof(TypeToConvert) == typeof(int) || typeof(TypeToConvert) == typeof(int?))
			{
				int value;
				bool result2 = jsonElement.TryGetInt32(out value);
				result = (TypeToConvert)(object)value;
				return result2;
			}
			if (typeof(TypeToConvert) == typeof(long) || typeof(TypeToConvert) == typeof(long?))
			{
				long value2;
				bool result2 = jsonElement.TryGetInt64(out value2);
				result = (TypeToConvert)(object)value2;
				return result2;
			}
			if (typeof(TypeToConvert) == typeof(double) || typeof(TypeToConvert) == typeof(double?))
			{
				double value3;
				bool result2 = jsonElement.TryGetDouble(out value3);
				result = (TypeToConvert)(object)value3;
				return result2;
			}
			if (typeof(TypeToConvert) == typeof(short) || typeof(TypeToConvert) == typeof(short?))
			{
				short value4;
				bool result2 = jsonElement.TryGetInt16(out value4);
				result = (TypeToConvert)(object)value4;
				return result2;
			}
			if (typeof(TypeToConvert) == typeof(decimal) || typeof(TypeToConvert) == typeof(decimal?))
			{
				decimal value5;
				bool result2 = jsonElement.TryGetDecimal(out value5);
				result = (TypeToConvert)(object)value5;
				return result2;
			}
			if (typeof(TypeToConvert) == typeof(byte) || typeof(TypeToConvert) == typeof(byte?))
			{
				byte value6;
				bool result2 = jsonElement.TryGetByte(out value6);
				result = (TypeToConvert)(object)value6;
				return result2;
			}
			if (typeof(TypeToConvert) == typeof(float) || typeof(TypeToConvert) == typeof(float?))
			{
				float value7;
				bool result2 = jsonElement.TryGetSingle(out value7);
				result = (TypeToConvert)(object)value7;
				return result2;
			}
			if (typeof(TypeToConvert) == typeof(uint) || typeof(TypeToConvert) == typeof(uint?))
			{
				uint value8;
				bool result2 = jsonElement.TryGetUInt32(out value8);
				result = (TypeToConvert)(object)value8;
				return result2;
			}
			if (typeof(TypeToConvert) == typeof(ushort) || typeof(TypeToConvert) == typeof(ushort?))
			{
				ushort value9;
				bool result2 = jsonElement.TryGetUInt16(out value9);
				result = (TypeToConvert)(object)value9;
				return result2;
			}
			if (typeof(TypeToConvert) == typeof(ulong) || typeof(TypeToConvert) == typeof(ulong?))
			{
				ulong value10;
				bool result2 = jsonElement.TryGetUInt64(out value10);
				result = (TypeToConvert)(object)value10;
				return result2;
			}
			if (typeof(TypeToConvert) == typeof(sbyte) || typeof(TypeToConvert) == typeof(sbyte?))
			{
				sbyte value11;
				bool result2 = jsonElement.TryGetSByte(out value11);
				result = (TypeToConvert)(object)value11;
				return result2;
			}
			break;
		case JsonValueKind.String:
			if (typeof(TypeToConvert) == typeof(string))
			{
				string @string = jsonElement.GetString();
				result = (TypeToConvert)(object)@string;
				return true;
			}
			if (typeof(TypeToConvert) == typeof(DateTime) || typeof(TypeToConvert) == typeof(DateTime?))
			{
				DateTime value12;
				bool result2 = jsonElement.TryGetDateTime(out value12);
				result = (TypeToConvert)(object)value12;
				return result2;
			}
			if (typeof(TypeToConvert) == typeof(DateTimeOffset) || typeof(TypeToConvert) == typeof(DateTimeOffset?))
			{
				DateTimeOffset value13;
				bool result2 = jsonElement.TryGetDateTimeOffset(out value13);
				result = (TypeToConvert)(object)value13;
				return result2;
			}
			if (typeof(TypeToConvert) == typeof(Guid) || typeof(TypeToConvert) == typeof(Guid?))
			{
				Guid value14;
				bool result2 = jsonElement.TryGetGuid(out value14);
				result = (TypeToConvert)(object)value14;
				return result2;
			}
			if (typeof(TypeToConvert) == typeof(char) || typeof(TypeToConvert) == typeof(char?))
			{
				string string2 = jsonElement.GetString();
				if (string2.Length == 1)
				{
					result = (TypeToConvert)(object)string2[0];
					return true;
				}
			}
			break;
		case JsonValueKind.True:
		case JsonValueKind.False:
			if (typeof(TypeToConvert) == typeof(bool) || typeof(TypeToConvert) == typeof(bool?))
			{
				result = (TypeToConvert)(object)jsonElement.GetBoolean();
				return true;
			}
			break;
		}
		result = default(TypeToConvert);
		return false;
	}
}
