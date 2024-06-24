using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Reflection;
using System.Text.Json.Serialization.Converters;

namespace System.Text.Json.Serialization;

[RequiresDynamicCode("JSON serialization and deserialization might require types that cannot be statically analyzed and might need runtime code generation. Use System.Text.Json source generation for native AOT applications.")]
internal sealed class IAsyncEnumerableConverterFactory : JsonConverterFactory
{
	public override bool CanConvert(Type typeToConvert)
	{
		return (object)GetAsyncEnumerableInterface(typeToConvert) != null;
	}

	public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
	{
		Type asyncEnumerableInterface = GetAsyncEnumerableInterface(typeToConvert);
		Type type = asyncEnumerableInterface.GetGenericArguments()[0];
		Type type2 = typeof(IAsyncEnumerableOfTConverter<, >).MakeGenericType(typeToConvert, type);
		return (JsonConverter)Activator.CreateInstance(type2);
	}

	private static Type GetAsyncEnumerableInterface(Type type)
	{
		return type.GetCompatibleGenericInterface(typeof(IAsyncEnumerable<>));
	}
}
