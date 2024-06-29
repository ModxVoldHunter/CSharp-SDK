using System.Runtime.Versioning;

namespace System.Runtime.InteropServices.Marshalling;

[UnsupportedOSPlatform("android")]
[UnsupportedOSPlatform("browser")]
[UnsupportedOSPlatform("ios")]
[UnsupportedOSPlatform("tvos")]
[CLSCompliant(false)]
[CustomMarshaller(typeof(CustomMarshallerAttribute.GenericPlaceholder), MarshalMode.Default, typeof(UniqueComInterfaceMarshaller<>))]
public static class UniqueComInterfaceMarshaller<T>
{
	public unsafe static void* ConvertToUnmanaged(T? managed)
	{
		if (managed == null)
		{
			return null;
		}
		if (!ComWrappers.TryGetComInstance(managed, out var unknown))
		{
			unknown = StrategyBasedComWrappers.DefaultMarshallingInstance.GetOrCreateComInterfaceForObject(managed, CreateComInterfaceFlags.None);
		}
		return ComInterfaceMarshaller<T>.CastIUnknownToInterfaceType(unknown);
	}

	public unsafe static T? ConvertToManaged(void* unmanaged)
	{
		if (unmanaged == null)
		{
			return default(T);
		}
		return (T)StrategyBasedComWrappers.DefaultMarshallingInstance.GetOrCreateObjectForComInstance((nint)unmanaged, CreateObjectFlags.UniqueInstance | CreateObjectFlags.Unwrap);
	}

	public unsafe static void Free(void* unmanaged)
	{
		if (unmanaged != null)
		{
			Marshal.Release((nint)unmanaged);
		}
	}
}
