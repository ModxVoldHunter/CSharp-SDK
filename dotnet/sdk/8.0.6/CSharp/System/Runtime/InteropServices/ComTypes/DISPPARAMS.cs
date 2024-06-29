using System.ComponentModel;

namespace System.Runtime.InteropServices.ComTypes;

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
[EditorBrowsable(EditorBrowsableState.Never)]
public struct DISPPARAMS
{
	public nint rgvarg;

	public nint rgdispidNamedArgs;

	public int cArgs;

	public int cNamedArgs;
}
