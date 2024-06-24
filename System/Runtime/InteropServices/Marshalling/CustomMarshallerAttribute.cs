namespace System.Runtime.InteropServices.Marshalling;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = true)]
public sealed class CustomMarshallerAttribute : Attribute
{
	[StructLayout(LayoutKind.Sequential, Size = 1)]
	public struct GenericPlaceholder
	{
	}

	public Type ManagedType { get; }

	public MarshalMode MarshalMode { get; }

	public Type MarshallerType { get; }

	public CustomMarshallerAttribute(Type managedType, MarshalMode marshalMode, Type marshallerType)
	{
		ManagedType = managedType;
		MarshalMode = marshalMode;
		MarshallerType = marshallerType;
	}
}
