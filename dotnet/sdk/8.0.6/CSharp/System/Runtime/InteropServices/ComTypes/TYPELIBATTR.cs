using System.ComponentModel;

namespace System.Runtime.InteropServices.ComTypes;

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
[EditorBrowsable(EditorBrowsableState.Never)]
public struct TYPELIBATTR
{
	public Guid guid;

	public int lcid;

	public SYSKIND syskind;

	public short wMajorVerNum;

	public short wMinorVerNum;

	public LIBFLAGS wLibFlags;
}
