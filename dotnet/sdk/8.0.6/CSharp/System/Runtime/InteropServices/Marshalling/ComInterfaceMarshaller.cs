using System.Runtime.Versioning;

namespace System.Runtime.InteropServices.Marshalling;

[UnsupportedOSPlatform("android")]
[UnsupportedOSPlatform("browser")]
[UnsupportedOSPlatform("ios")]
[UnsupportedOSPlatform("tvos")]
[CLSCompliant(false)]
[CustomMarshaller(typeof(CustomMarshallerAttribute.GenericPlaceholder), MarshalMode.Default, typeof(ComInterfaceMarshaller<>))]
public static class ComInterfaceMarshaller<T>
{
	private static readonly Guid? TargetInterfaceIID = StrategyBasedComWrappers.DefaultIUnknownInterfaceDetailsStrategy.GetIUnknownDerivedDetails(typeof(T).TypeHandle)?.Iid;

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
		return CastIUnknownToInterfaceType(unknown);
	}

	public unsafe static T? ConvertToManaged(void* unmanaged)
	{
		if (unmanaged == null)
		{
			return default(T);
		}
		return (T)StrategyBasedComWrappers.DefaultMarshallingInstance.GetOrCreateObjectForComInstance((nint)unmanaged, CreateObjectFlags.Unwrap);
	}

	public unsafe static void Free(void* unmanaged)
	{
		if (unmanaged != null)
		{
			Marshal.Release((nint)unmanaged);
		}
	}

	internal unsafe static void* CastIUnknownToInterfaceType(nint unknown)
	{
		Guid? targetInterfaceIID = TargetInterfaceIID;
		if (!targetInterfaceIID.HasValue)
		{
			return (void*)unknown;
		}
		if (Marshal.QueryInterface(unknown, ref Nullable.GetValueRefOrDefaultRef(ref TargetInterfaceIID), out var ppv) != 0)
		{
			Marshal.Release(unknown);
			throw new InvalidCastException($"Unable to cast the provided managed object to a COM interface with ID '{TargetInterfaceIID.GetValueOrDefault():B}'");
		}
		Marshal.Release(unknown);
		return (void*)ppv;
	}
}
