using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text.Json.Reflection;
using System.Text.Json.Serialization.Metadata;

namespace System.Text.Json.Serialization.Converters;

[RequiresDynamicCode("JSON serialization and deserialization might require types that cannot be statically analyzed and might need runtime code generation. Use System.Text.Json source generation for native AOT applications.")]
internal sealed class ObjectConverterFactory : JsonConverterFactory
{
	private readonly bool _useDefaultConstructorInUnannotatedStructs;

	[RequiresUnreferencedCode("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext, or make sure all of the required types are preserved.")]
	public ObjectConverterFactory(bool useDefaultConstructorInUnannotatedStructs = true)
	{
		_useDefaultConstructorInUnannotatedStructs = useDefaultConstructorInUnannotatedStructs;
	}

	public override bool CanConvert(Type typeToConvert)
	{
		return true;
	}

	[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode", Justification = "The ctor is marked RequiresUnreferencedCode.")]
	[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2067:UnrecognizedReflectionPattern", Justification = "The ctor is marked RequiresUnreferencedCode.")]
	public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
	{
		bool useDefaultCtorInAnnotatedStructs = _useDefaultConstructorInUnannotatedStructs && !typeToConvert.IsKeyValuePair();
		if (!typeToConvert.TryGetDeserializationConstructor(useDefaultCtorInAnnotatedStructs, out var deserializationCtor))
		{
			ThrowHelper.ThrowInvalidOperationException_SerializationDuplicateTypeAttribute<JsonConstructorAttribute>(typeToConvert);
		}
		ParameterInfo[] array = deserializationCtor?.GetParameters();
		Type type;
		if (deserializationCtor == null || typeToConvert.IsAbstract || array.Length == 0)
		{
			type = typeof(ObjectDefaultConverter<>).MakeGenericType(typeToConvert);
		}
		else
		{
			int num = array.Length;
			if (num <= 4)
			{
				Type objectType = JsonTypeInfo.ObjectType;
				Type[] array2 = new Type[5] { typeToConvert, null, null, null, null };
				for (int i = 0; i < 4; i++)
				{
					if (i < num)
					{
						array2[i + 1] = array[i].ParameterType;
					}
					else
					{
						array2[i + 1] = objectType;
					}
				}
				type = typeof(SmallObjectWithParameterizedConstructorConverter<, , , , >).MakeGenericType(array2);
			}
			else
			{
				type = typeof(LargeObjectWithParameterizedConstructorConverterWithReflection<>).MakeGenericType(typeToConvert);
			}
		}
		JsonConverter jsonConverter = (JsonConverter)Activator.CreateInstance(type, BindingFlags.Instance | BindingFlags.Public, null, null, null);
		jsonConverter.ConstructorInfo = deserializationCtor;
		return jsonConverter;
	}
}
