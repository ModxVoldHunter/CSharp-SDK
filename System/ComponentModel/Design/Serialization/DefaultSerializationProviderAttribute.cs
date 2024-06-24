namespace System.ComponentModel.Design.Serialization;

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class DefaultSerializationProviderAttribute : Attribute
{
	public string ProviderTypeName { get; }

	public DefaultSerializationProviderAttribute(Type providerType)
	{
		ArgumentNullException.ThrowIfNull(providerType, "providerType");
		ProviderTypeName = providerType.AssemblyQualifiedName;
	}

	public DefaultSerializationProviderAttribute(string providerTypeName)
	{
		ArgumentNullException.ThrowIfNull(providerTypeName, "providerTypeName");
		ProviderTypeName = providerTypeName;
	}
}
