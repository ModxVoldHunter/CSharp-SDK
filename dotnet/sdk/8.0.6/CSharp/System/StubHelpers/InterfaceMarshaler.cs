using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System.StubHelpers;

internal static class InterfaceMarshaler
{
	[MethodImpl(MethodImplOptions.InternalCall)]
	internal static extern nint ConvertToNative(object objSrc, nint itfMT, nint classMT, int flags);

	[MethodImpl(MethodImplOptions.InternalCall)]
	internal static extern object ConvertToManaged(ref nint ppUnk, nint itfMT, nint classMT, int flags);

	[DllImport("QCall", EntryPoint = "InterfaceMarshaler__ClearNative", ExactSpelling = true)]
	[LibraryImport("QCall", EntryPoint = "InterfaceMarshaler__ClearNative")]
	internal static extern void ClearNative(nint pUnk);
}
