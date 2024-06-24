using System.Runtime.CompilerServices;

namespace System.StubHelpers;

internal static class MngdRefCustomMarshaler
{
	[MethodImpl(MethodImplOptions.InternalCall)]
	internal static extern void CreateMarshaler(nint pMarshalState, nint pCMHelper);

	[MethodImpl(MethodImplOptions.InternalCall)]
	internal static extern void ConvertContentsToNative(nint pMarshalState, ref object pManagedHome, nint pNativeHome);

	[MethodImpl(MethodImplOptions.InternalCall)]
	internal static extern void ConvertContentsToManaged(nint pMarshalState, ref object pManagedHome, nint pNativeHome);

	[MethodImpl(MethodImplOptions.InternalCall)]
	internal static extern void ClearNative(nint pMarshalState, ref object pManagedHome, nint pNativeHome);

	[MethodImpl(MethodImplOptions.InternalCall)]
	internal static extern void ClearManaged(nint pMarshalState, ref object pManagedHome, nint pNativeHome);
}
