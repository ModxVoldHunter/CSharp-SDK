namespace System.Text.Json.Serialization.Metadata;

public readonly struct JsonDerivedType
{
	public Type DerivedType { get; }

	public object? TypeDiscriminator { get; }

	public JsonDerivedType(Type derivedType)
	{
		DerivedType = derivedType;
		TypeDiscriminator = null;
	}

	public JsonDerivedType(Type derivedType, int typeDiscriminator)
	{
		DerivedType = derivedType;
		TypeDiscriminator = typeDiscriminator;
	}

	public JsonDerivedType(Type derivedType, string typeDiscriminator)
	{
		DerivedType = derivedType;
		TypeDiscriminator = typeDiscriminator;
	}

	internal JsonDerivedType(Type derivedType, object typeDiscriminator)
	{
		DerivedType = derivedType;
		TypeDiscriminator = typeDiscriminator;
	}

	internal void Deconstruct(out Type derivedType, out object typeDiscriminator)
	{
		derivedType = DerivedType;
		typeDiscriminator = TypeDiscriminator;
	}
}
