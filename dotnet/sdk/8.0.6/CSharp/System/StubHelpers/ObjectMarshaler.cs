using System.Runtime.CompilerServices;

namespace System.StubHelpers;

internal static class ObjectMarshaler
{
	[MethodImpl(MethodImplOptions.InternalCall)]
	internal static extern void ConvertToNative(object objSrc, nint pDstVariant);

	[MethodImpl(MethodImplOptions.InternalCall)]
	internal static extern object ConvertToManaged(nint pSrcVariant);

	[MethodImpl(MethodImplOptions.InternalCall)]
	internal static extern void ClearNative(nint pVariant);
}
