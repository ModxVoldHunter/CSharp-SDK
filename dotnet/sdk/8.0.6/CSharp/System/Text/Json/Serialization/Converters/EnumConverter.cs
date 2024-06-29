using System.Buffers;
using System.Collections.Concurrent;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text.Encodings.Web;

namespace System.Text.Json.Serialization.Converters;

internal sealed class EnumConverter<T> : JsonPrimitiveConverter<T> where T : struct, Enum
{
	private static readonly TypeCode s_enumTypeCode = System.Type.GetTypeCode(typeof(T));

	private static readonly bool s_isSignedEnum = (int)s_enumTypeCode % 2 == 1;

	private readonly EnumConverterOptions _converterOptions;

	private readonly JsonNamingPolicy _namingPolicy;

	private readonly ConcurrentDictionary<ulong, JsonEncodedText> _nameCacheForWriting;

	private readonly ConcurrentDictionary<string, T> _nameCacheForReading;

	public override bool CanConvert(Type type)
	{
		return type.IsEnum;
	}

	public EnumConverter(EnumConverterOptions converterOptions, JsonSerializerOptions serializerOptions)
		: this(converterOptions, (JsonNamingPolicy)null, serializerOptions)
	{
	}

	public EnumConverter(EnumConverterOptions converterOptions, JsonNamingPolicy namingPolicy, JsonSerializerOptions serializerOptions)
	{
		_converterOptions = converterOptions;
		_namingPolicy = namingPolicy;
		_nameCacheForWriting = new ConcurrentDictionary<ulong, JsonEncodedText>();
		if (namingPolicy != null)
		{
			_nameCacheForReading = new ConcurrentDictionary<string, T>();
		}
		string[] names = Enum.GetNames<T>();
		T[] values = Enum.GetValues<T>();
		JavaScriptEncoder encoder = serializerOptions.Encoder;
		for (int i = 0; i < names.Length; i++)
		{
			T val = values[i];
			ulong key = ConvertToUInt64(val);
			string text = names[i];
			string text2 = FormatJsonName(text, namingPolicy);
			_nameCacheForWriting.TryAdd(key, JsonEncodedText.Encode(text2, encoder));
			_nameCacheForReading?.TryAdd(text2, val);
			if (text.AsSpan().IndexOfAny(',', ' ') >= 0)
			{
				ThrowHelper.ThrowInvalidOperationException_InvalidEnumTypeWithSpecialChar(typeof(T), text);
			}
		}
	}

	public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		switch (reader.TokenType)
		{
		case JsonTokenType.String:
		{
			if ((_converterOptions & EnumConverterOptions.AllowStrings) == 0)
			{
				ThrowHelper.ThrowJsonException();
				return default(T);
			}
			if (TryParseEnumCore(ref reader, options, out var value9))
			{
				return value9;
			}
			return ReadEnumUsingNamingPolicy(reader.GetString());
		}
		case JsonTokenType.Number:
			if ((_converterOptions & EnumConverterOptions.AllowNumbers) != 0)
			{
				switch (s_enumTypeCode)
				{
				case TypeCode.Int32:
				{
					if (reader.TryGetInt32(out var value8))
					{
						return Unsafe.As<int, T>(ref value8);
					}
					break;
				}
				case TypeCode.UInt32:
				{
					if (reader.TryGetUInt32(out var value4))
					{
						return Unsafe.As<uint, T>(ref value4);
					}
					break;
				}
				case TypeCode.UInt64:
				{
					if (reader.TryGetUInt64(out var value6))
					{
						return Unsafe.As<ulong, T>(ref value6);
					}
					break;
				}
				case TypeCode.Int64:
				{
					if (reader.TryGetInt64(out var value2))
					{
						return Unsafe.As<long, T>(ref value2);
					}
					break;
				}
				case TypeCode.SByte:
				{
					if (reader.TryGetSByte(out var value7))
					{
						return Unsafe.As<sbyte, T>(ref value7);
					}
					break;
				}
				case TypeCode.Byte:
				{
					if (reader.TryGetByte(out var value5))
					{
						return Unsafe.As<byte, T>(ref value5);
					}
					break;
				}
				case TypeCode.Int16:
				{
					if (reader.TryGetInt16(out var value3))
					{
						return Unsafe.As<short, T>(ref value3);
					}
					break;
				}
				case TypeCode.UInt16:
				{
					if (reader.TryGetUInt16(out var value))
					{
						return Unsafe.As<ushort, T>(ref value);
					}
					break;
				}
				}
				ThrowHelper.ThrowJsonException();
				return default(T);
			}
			goto default;
		default:
			ThrowHelper.ThrowJsonException();
			return default(T);
		}
	}

	public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
	{
		if ((_converterOptions & EnumConverterOptions.AllowStrings) != 0)
		{
			ulong key = ConvertToUInt64(value);
			if (_nameCacheForWriting.TryGetValue(key, out var value2))
			{
				writer.WriteStringValue(value2);
				return;
			}
			string value3 = value.ToString();
			if (IsValidIdentifier(value3))
			{
				value3 = FormatJsonName(value3, _namingPolicy);
				if (_nameCacheForWriting.Count < 64)
				{
					value2 = JsonEncodedText.Encode(value3, options.Encoder);
					writer.WriteStringValue(value2);
					_nameCacheForWriting.TryAdd(key, value2);
				}
				else
				{
					writer.WriteStringValue(value3);
				}
				return;
			}
		}
		if ((_converterOptions & EnumConverterOptions.AllowNumbers) == 0)
		{
			ThrowHelper.ThrowJsonException();
		}
		switch (s_enumTypeCode)
		{
		case TypeCode.Int32:
			writer.WriteNumberValue(Unsafe.As<T, int>(ref value));
			break;
		case TypeCode.UInt32:
			writer.WriteNumberValue(Unsafe.As<T, uint>(ref value));
			break;
		case TypeCode.UInt64:
			writer.WriteNumberValue(Unsafe.As<T, ulong>(ref value));
			break;
		case TypeCode.Int64:
			writer.WriteNumberValue(Unsafe.As<T, long>(ref value));
			break;
		case TypeCode.Int16:
			writer.WriteNumberValue(Unsafe.As<T, short>(ref value));
			break;
		case TypeCode.UInt16:
			writer.WriteNumberValue(Unsafe.As<T, ushort>(ref value));
			break;
		case TypeCode.Byte:
			writer.WriteNumberValue(Unsafe.As<T, byte>(ref value));
			break;
		case TypeCode.SByte:
			writer.WriteNumberValue(Unsafe.As<T, sbyte>(ref value));
			break;
		default:
			ThrowHelper.ThrowJsonException();
			break;
		}
	}

	internal override T ReadAsPropertyNameCore(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		if (!TryParseEnumCore(ref reader, options, out var value))
		{
			ThrowHelper.ThrowJsonException();
		}
		return value;
	}

	internal unsafe override void WriteAsPropertyNameCore(Utf8JsonWriter writer, T value, JsonSerializerOptions options, bool isWritingExtensionDataProperty)
	{
		ulong key = ConvertToUInt64(value);
		if (options.DictionaryKeyPolicy == null && _nameCacheForWriting.TryGetValue(key, out var value2))
		{
			writer.WritePropertyName(value2);
			return;
		}
		string value3 = value.ToString();
		if (IsValidIdentifier(value3))
		{
			if (options.DictionaryKeyPolicy != null)
			{
				value3 = FormatJsonName(value3, options.DictionaryKeyPolicy);
				writer.WritePropertyName(value3);
				return;
			}
			value3 = FormatJsonName(value3, _namingPolicy);
			if (_nameCacheForWriting.Count < 64)
			{
				value2 = JsonEncodedText.Encode(value3, options.Encoder);
				writer.WritePropertyName(value2);
				_nameCacheForWriting.TryAdd(key, value2);
			}
			else
			{
				writer.WritePropertyName(value3);
			}
			return;
		}
		switch (s_enumTypeCode)
		{
		case TypeCode.Int32:
			writer.WritePropertyName(*(int*)(&value));
			break;
		case TypeCode.UInt32:
			writer.WritePropertyName(*(uint*)(&value));
			break;
		case TypeCode.UInt64:
			writer.WritePropertyName(*(ulong*)(&value));
			break;
		case TypeCode.Int64:
			writer.WritePropertyName(*(long*)(&value));
			break;
		case TypeCode.Int16:
			writer.WritePropertyName(*(short*)(&value));
			break;
		case TypeCode.UInt16:
			writer.WritePropertyName(*(ushort*)(&value));
			break;
		case TypeCode.Byte:
			writer.WritePropertyName(*(byte*)(&value));
			break;
		case TypeCode.SByte:
			writer.WritePropertyName(*(sbyte*)(&value));
			break;
		default:
			ThrowHelper.ThrowJsonException();
			break;
		}
	}

	private static bool TryParseEnumCore(ref Utf8JsonReader reader, JsonSerializerOptions options, out T value)
	{
		char[] array = null;
		int valueLength = reader.ValueLength;
		Span<char> span = ((valueLength > 128) ? ((Span<char>)(array = ArrayPool<char>.Shared.Rent(valueLength))) : stackalloc char[128]);
		Span<char> destination = span;
		int length = reader.CopyString(destination);
		ReadOnlySpan<char> value2 = destination.Slice(0, length);
		T result;
		bool result2 = Enum.TryParse<T>(value2, out result) || Enum.TryParse<T>(value2, ignoreCase: true, out result);
		if (array != null)
		{
			destination.Slice(0, length).Clear();
			ArrayPool<char>.Shared.Return(array);
		}
		value = result;
		return result2;
	}

	private T ReadEnumUsingNamingPolicy(string enumString)
	{
		if (_namingPolicy == null)
		{
			ThrowHelper.ThrowJsonException();
		}
		if (enumString == null)
		{
			ThrowHelper.ThrowJsonException();
		}
		bool flag;
		if (!(flag = _nameCacheForReading.TryGetValue(enumString, out var value)) && enumString.Contains(", "))
		{
			string[] array = SplitFlagsEnum(enumString);
			ulong num = 0uL;
			for (int i = 0; i < array.Length; i++)
			{
				flag = _nameCacheForReading.TryGetValue(array[i], out value);
				if (!flag)
				{
					break;
				}
				num |= ConvertToUInt64(value);
			}
			value = (T)Enum.ToObject(typeof(T), num);
			if (flag && _nameCacheForReading.Count < 64)
			{
				_nameCacheForReading[enumString] = value;
			}
		}
		if (!flag)
		{
			ThrowHelper.ThrowJsonException();
		}
		return value;
	}

	private static ulong ConvertToUInt64(object value)
	{
		return s_enumTypeCode switch
		{
			TypeCode.Int32 => (ulong)(int)value, 
			TypeCode.UInt32 => (uint)value, 
			TypeCode.UInt64 => (ulong)value, 
			TypeCode.Int64 => (ulong)(long)value, 
			TypeCode.SByte => (ulong)(sbyte)value, 
			TypeCode.Byte => (byte)value, 
			TypeCode.Int16 => (ulong)(short)value, 
			TypeCode.UInt16 => (ushort)value, 
			_ => throw new InvalidOperationException(), 
		};
	}

	private static bool IsValidIdentifier(string value)
	{
		if (value[0] >= 'A')
		{
			if (s_isSignedEnum)
			{
				return !value.StartsWith(NumberFormatInfo.CurrentInfo.NegativeSign);
			}
			return true;
		}
		return false;
	}

	private static string FormatJsonName(string value, JsonNamingPolicy namingPolicy)
	{
		if (namingPolicy == null)
		{
			return value;
		}
		string text;
		if (!value.Contains(", "))
		{
			text = namingPolicy.ConvertName(value);
			if (text == null)
			{
				ThrowHelper.ThrowInvalidOperationException_NamingPolicyReturnNull(namingPolicy);
			}
		}
		else
		{
			string[] array = SplitFlagsEnum(value);
			for (int i = 0; i < array.Length; i++)
			{
				string text2 = namingPolicy.ConvertName(array[i]);
				if (text2 == null)
				{
					ThrowHelper.ThrowInvalidOperationException_NamingPolicyReturnNull(namingPolicy);
				}
				array[i] = text2;
			}
			text = string.Join(", ", array);
		}
		return text;
	}

	private static string[] SplitFlagsEnum(string value)
	{
		return value.Split(", ");
	}
}
