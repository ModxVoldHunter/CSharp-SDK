using System.ComponentModel;

namespace System.Runtime.InteropServices.ComTypes;

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
[EditorBrowsable(EditorBrowsableState.Never)]
public struct IDLDESC
{
	public nint dwReserved;

	public IDLFLAG wIDLFlags;
}
