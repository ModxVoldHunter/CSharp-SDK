using System.Reflection;

namespace System.Runtime.InteropServices.Marshalling;

[CLSCompliant(false)]
public interface IIUnknownDerivedDetails
{
	Guid Iid { get; }

	Type Implementation { get; }

	unsafe void** ManagedVirtualMethodTable { get; }

	internal static IIUnknownDerivedDetails GetFromAttribute(RuntimeTypeHandle handle)
	{
		Type typeFromHandle = Type.GetTypeFromHandle(handle);
		if ((object)typeFromHandle == null)
		{
			return null;
		}
		return (IIUnknownDerivedDetails)typeFromHandle.GetCustomAttribute(typeof(IUnknownDerivedAttribute<, >));
	}
}
