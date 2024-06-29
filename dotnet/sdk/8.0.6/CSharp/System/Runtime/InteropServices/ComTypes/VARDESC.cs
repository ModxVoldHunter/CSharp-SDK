using System.ComponentModel;

namespace System.Runtime.InteropServices.ComTypes;

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
[EditorBrowsable(EditorBrowsableState.Never)]
public struct VARDESC
{
	[StructLayout(LayoutKind.Explicit, CharSet = CharSet.Unicode)]
	public struct DESCUNION
	{
		[FieldOffset(0)]
		public int oInst;

		[FieldOffset(0)]
		public nint lpvarValue;
	}

	public int memid;

	public string lpstrSchema;

	public DESCUNION desc;

	public ELEMDESC elemdescVar;

	public short wVarFlags;

	public VARKIND varkind;
}
