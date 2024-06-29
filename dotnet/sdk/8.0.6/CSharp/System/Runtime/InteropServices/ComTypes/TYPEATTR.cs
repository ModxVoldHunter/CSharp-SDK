using System.ComponentModel;

namespace System.Runtime.InteropServices.ComTypes;

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
[EditorBrowsable(EditorBrowsableState.Never)]
public struct TYPEATTR
{
	public const int MEMBER_ID_NIL = -1;

	public Guid guid;

	public int lcid;

	public int dwReserved;

	public int memidConstructor;

	public int memidDestructor;

	public nint lpstrSchema;

	public int cbSizeInstance;

	public TYPEKIND typekind;

	public short cFuncs;

	public short cVars;

	public short cImplTypes;

	public short cbSizeVft;

	public short cbAlignment;

	public TYPEFLAGS wTypeFlags;

	public short wMajorVerNum;

	public short wMinorVerNum;

	public TYPEDESC tdescAlias;

	public IDLDESC idldescType;
}
