using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.Serialization;

namespace System.Text.Json.Serialization.Converters;

[RequiresDynamicCode("JSON serialization and deserialization might require types that cannot be statically analyzed and might need runtime code generation. Use System.Text.Json source generation for native AOT applications.")]
internal sealed class UnsupportedTypeConverterFactory : JsonConverterFactory
{
	public override bool CanConvert(Type type)
	{
		if (!typeof(MemberInfo).IsAssignableFrom(type) && !(type == typeof(SerializationInfo)) && !(type == typeof(nint)) && !(type == typeof(nuint)))
		{
			return typeof(Delegate).IsAssignableFrom(type);
		}
		return true;
	}

	public override JsonConverter CreateConverter(Type type, JsonSerializerOptions options)
	{
		return CreateUnsupportedConverterForType(type);
	}

	internal static JsonConverter CreateUnsupportedConverterForType(Type type, string errorMessage = null)
	{
		return (JsonConverter)Activator.CreateInstance(typeof(UnsupportedTypeConverter<>).MakeGenericType(type), BindingFlags.Instance | BindingFlags.Public, null, new object[1] { errorMessage }, null);
	}
}
