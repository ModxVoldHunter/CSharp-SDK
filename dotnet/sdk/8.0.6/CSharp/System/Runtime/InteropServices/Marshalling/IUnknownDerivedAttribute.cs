namespace System.Runtime.InteropServices.Marshalling;

[AttributeUsage(AttributeTargets.Interface, Inherited = false)]
[CLSCompliant(false)]
public class IUnknownDerivedAttribute<T, TImpl> : Attribute, IIUnknownDerivedDetails where T : IIUnknownInterfaceType
{
	public Guid Iid => T.Iid;

	public Type Implementation => typeof(TImpl);

	public unsafe void** ManagedVirtualMethodTable => T.ManagedVirtualMethodTable;
}
