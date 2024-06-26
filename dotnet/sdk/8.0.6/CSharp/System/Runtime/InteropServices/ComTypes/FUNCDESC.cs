using System.ComponentModel;

namespace System.Runtime.InteropServices.ComTypes;

[EditorBrowsable(EditorBrowsableState.Never)]
public struct FUNCDESC
{
	public int memid;

	public nint lprgscode;

	public nint lprgelemdescParam;

	public FUNCKIND funckind;

	public INVOKEKIND invkind;

	public CALLCONV callconv;

	public short cParams;

	public short cParamsOpt;

	public short oVft;

	public short cScodes;

	public ELEMDESC elemdescFunc;

	public short wFuncFlags;
}
