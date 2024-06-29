namespace System.Text.Json.Serialization;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface, AllowMultiple = true, Inherited = false)]
public class JsonDerivedTypeAttribute : JsonAttribute
{
	public Type DerivedType { get; }

	public object? TypeDiscriminator { get; }

	public JsonDerivedTypeAttribute(Type derivedType)
	{
		DerivedType = derivedType;
	}

	public JsonDerivedTypeAttribute(Type derivedType, string typeDiscriminator)
	{
		DerivedType = derivedType;
		TypeDiscriminator = typeDiscriminator;
	}

	public JsonDerivedTypeAttribute(Type derivedType, int typeDiscriminator)
	{
		DerivedType = derivedType;
		TypeDiscriminator = typeDiscriminator;
	}
}
