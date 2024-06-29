using System.Runtime.CompilerServices;

namespace System.StubHelpers;

internal static class MngdNativeArrayMarshaler
{
	internal struct MarshalerState
	{
		private nint m_pElementMT;

		private nint m_Array;

		private nint m_pManagedNativeArrayMarshaler;

		private int m_NativeDataValid;

		private int m_BestFitMap;

		private int m_ThrowOnUnmappableChar;

		private short m_vt;
	}

	[MethodImpl(MethodImplOptions.InternalCall)]
	internal static extern void CreateMarshaler(nint pMarshalState, nint pMT, int dwFlags, nint pManagedMarshaler);

	[MethodImpl(MethodImplOptions.InternalCall)]
	internal static extern void ConvertSpaceToNative(nint pMarshalState, ref object pManagedHome, nint pNativeHome);

	[MethodImpl(MethodImplOptions.InternalCall)]
	internal static extern void ConvertContentsToNative(nint pMarshalState, ref object pManagedHome, nint pNativeHome);

	[MethodImpl(MethodImplOptions.InternalCall)]
	internal static extern void ConvertSpaceToManaged(nint pMarshalState, ref object pManagedHome, nint pNativeHome, int cElements);

	[MethodImpl(MethodImplOptions.InternalCall)]
	internal static extern void ConvertContentsToManaged(nint pMarshalState, ref object pManagedHome, nint pNativeHome);

	[MethodImpl(MethodImplOptions.InternalCall)]
	internal static extern void ClearNative(nint pMarshalState, ref object pManagedHome, nint pNativeHome, int cElements);

	[MethodImpl(MethodImplOptions.InternalCall)]
	internal static extern void ClearNativeContents(nint pMarshalState, ref object pManagedHome, nint pNativeHome, int cElements);
}
