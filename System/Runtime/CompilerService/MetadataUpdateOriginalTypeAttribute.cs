namespace System.Runtime.CompilerServices;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false, Inherited = false)]
public class MetadataUpdateOriginalTypeAttribute : Attribute
{
	public Type OriginalType { get; }

	public MetadataUpdateOriginalTypeAttribute(Type originalType)
	{
		OriginalType = originalType;
	}
}
