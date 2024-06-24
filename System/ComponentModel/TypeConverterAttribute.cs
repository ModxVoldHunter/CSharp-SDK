using System.Diagnostics.CodeAnalysis;

namespace System.ComponentModel;

[AttributeUsage(AttributeTargets.All)]
public sealed class TypeConverterAttribute : Attribute
{
	public static readonly TypeConverterAttribute Default = new TypeConverterAttribute();

	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
	public string ConverterTypeName { get; }

	public TypeConverterAttribute()
	{
		ConverterTypeName = string.Empty;
	}

	public TypeConverterAttribute([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type type)
	{
		ArgumentNullException.ThrowIfNull(type, "type");
		ConverterTypeName = type.AssemblyQualifiedName;
	}

	public TypeConverterAttribute([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] string typeName)
	{
		ArgumentNullException.ThrowIfNull(typeName, "typeName");
		ConverterTypeName = typeName;
	}

	public override bool Equals([NotNullWhen(true)] object? obj)
	{
		if (obj is TypeConverterAttribute typeConverterAttribute)
		{
			return typeConverterAttribute.ConverterTypeName == ConverterTypeName;
		}
		return false;
	}

	public override int GetHashCode()
	{
		return ConverterTypeName.GetHashCode();
	}
}
