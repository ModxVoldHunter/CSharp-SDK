using System.Runtime.CompilerServices;

namespace System.StubHelpers;

internal static class MngdFixedArrayMarshaler
{
	[MethodImpl(MethodImplOptions.InternalCall)]
	internal static extern void CreateMarshaler(nint pMarshalState, nint pMT, int dwFlags, int cElements, nint pManagedMarshaler);

	[MethodImpl(MethodImplOptions.InternalCall)]
	internal static extern void ConvertSpaceToNative(nint pMarshalState, ref object pManagedHome, nint pNativeHome);

	[MethodImpl(MethodImplOptions.InternalCall)]
	internal static extern void ConvertContentsToNative(nint pMarshalState, ref object pManagedHome, nint pNativeHome);

	[MethodImpl(MethodImplOptions.InternalCall)]
	internal static extern void ConvertSpaceToManaged(nint pMarshalState, ref object pManagedHome, nint pNativeHome);

	[MethodImpl(MethodImplOptions.InternalCall)]
	internal static extern void ConvertContentsToManaged(nint pMarshalState, ref object pManagedHome, nint pNativeHome);

	[MethodImpl(MethodImplOptions.InternalCall)]
	internal static extern void ClearNativeContents(nint pMarshalState, ref object pManagedHome, nint pNativeHome);
}
