using System.ComponentModel;

namespace System.Runtime.InteropServices.ComTypes;

[EditorBrowsable(EditorBrowsableState.Never)]
public struct FORMATETC
{
	[MarshalAs(UnmanagedType.U2)]
	public short cfFormat;

	public nint ptd;

	[MarshalAs(UnmanagedType.U4)]
	public DVASPECT dwAspect;

	public int lindex;

	[MarshalAs(UnmanagedType.U4)]
	public TYMED tymed;
}
