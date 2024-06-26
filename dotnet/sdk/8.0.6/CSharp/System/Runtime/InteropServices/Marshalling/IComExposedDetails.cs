using System.Reflection;

namespace System.Runtime.InteropServices.Marshalling;

[CLSCompliant(false)]
public interface IComExposedDetails
{
	unsafe ComWrappers.ComInterfaceEntry* GetComInterfaceEntries(out int count);

	internal static IComExposedDetails GetFromAttribute(RuntimeTypeHandle handle)
	{
		Type typeFromHandle = Type.GetTypeFromHandle(handle);
		if ((object)typeFromHandle == null)
		{
			return null;
		}
		return (IComExposedDetails)typeFromHandle.GetCustomAttribute(typeof(ComExposedClassAttribute<>));
	}
}
