using System.ComponentModel;

namespace System.Runtime.InteropServices.ComTypes;

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
[EditorBrowsable(EditorBrowsableState.Never)]
public struct CONNECTDATA
{
	[MarshalAs(UnmanagedType.Interface)]
	public object pUnk;

	public int dwCookie;
}
