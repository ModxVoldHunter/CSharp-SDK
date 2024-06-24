using System.Text.Json.Serialization.Converters;

namespace System.Text.Json.Serialization;

public sealed class JsonNumberEnumConverter<TEnum> : JsonConverterFactory where TEnum : struct, Enum
{
	public override bool CanConvert(Type typeToConvert)
	{
		return typeToConvert == typeof(TEnum);
	}

	public override JsonConverter? CreateConverter(Type typeToConvert, JsonSerializerOptions options)
	{
		if (typeToConvert != typeof(TEnum))
		{
			ThrowHelper.ThrowArgumentOutOfRangeException_JsonConverterFactory_TypeNotSupported(typeToConvert);
		}
		return new EnumConverter<TEnum>(EnumConverterOptions.AllowNumbers, options);
	}
}
