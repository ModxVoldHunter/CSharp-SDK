using System.Diagnostics.CodeAnalysis;

namespace System.Text.Json.Serialization.Converters;

[RequiresDynamicCode("JSON serialization and deserialization might require types that cannot be statically analyzed and might need runtime code generation. Use System.Text.Json source generation for native AOT applications.")]
internal sealed class EnumConverterFactory : JsonConverterFactory
{
	public override bool CanConvert(Type type)
	{
		return type.IsEnum;
	}

	public override JsonConverter CreateConverter(Type type, JsonSerializerOptions options)
	{
		return Create(type, EnumConverterOptions.AllowNumbers, null, options);
	}

	internal static JsonConverter Create(Type enumType, EnumConverterOptions converterOptions, JsonNamingPolicy namingPolicy, JsonSerializerOptions options)
	{
		return (JsonConverter)Activator.CreateInstance(GetEnumConverterType(enumType), converterOptions, namingPolicy, options);
	}

	[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2070:UnrecognizedReflectionPattern", Justification = "'EnumConverter<T> where T : struct' implies 'T : new()', so the trimmer is warning calling MakeGenericType here because enumType's constructors are not annotated. But EnumConverter doesn't call new T(), so this is safe.")]
	[return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
	private static Type GetEnumConverterType(Type enumType)
	{
		return typeof(EnumConverter<>).MakeGenericType(enumType);
	}
}
