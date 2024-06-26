using System.ComponentModel;

namespace System.Runtime.InteropServices.ComTypes;

[StructLayout(LayoutKind.Explicit, CharSet = CharSet.Unicode)]
[EditorBrowsable(EditorBrowsableState.Never)]
public struct BINDPTR
{
	[FieldOffset(0)]
	public nint lpfuncdesc;

	[FieldOffset(0)]
	public nint lpvardesc;

	[FieldOffset(0)]
	public nint lptcomp;
}
