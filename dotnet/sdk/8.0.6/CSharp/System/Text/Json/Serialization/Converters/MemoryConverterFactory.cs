using System.Diagnostics.CodeAnalysis;

namespace System.Text.Json.Serialization.Converters;

[RequiresDynamicCode("JSON serialization and deserialization might require types that cannot be statically analyzed and might need runtime code generation. Use System.Text.Json source generation for native AOT applications.")]
internal sealed class MemoryConverterFactory : JsonConverterFactory
{
	public override bool CanConvert(Type typeToConvert)
	{
		if (!typeToConvert.IsGenericType || !typeToConvert.IsValueType)
		{
			return false;
		}
		Type genericTypeDefinition = typeToConvert.GetGenericTypeDefinition();
		if (!(genericTypeDefinition == typeof(Memory<>)))
		{
			return genericTypeDefinition == typeof(ReadOnlyMemory<>);
		}
		return true;
	}

	public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
	{
		Type type = ((typeToConvert.GetGenericTypeDefinition() == typeof(Memory<>)) ? typeof(MemoryConverter<>) : typeof(ReadOnlyMemoryConverter<>));
		Type type2 = typeToConvert.GetGenericArguments()[0];
		return (JsonConverter)Activator.CreateInstance(type.MakeGenericType(type2));
	}
}
