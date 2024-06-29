using System.ComponentModel;

namespace System.Runtime.InteropServices.ComTypes;

[EditorBrowsable(EditorBrowsableState.Never)]
public struct STGMEDIUM
{
	public TYMED tymed;

	public nint unionmember;

	[MarshalAs(UnmanagedType.IUnknown)]
	public object? pUnkForRelease;
}
