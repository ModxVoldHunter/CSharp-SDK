using System.ComponentModel;

namespace System.Runtime.InteropServices.ComTypes;

[ComImport]
[Guid("B196B284-BAB4-101A-B69C-00AA00341D07")]
[EditorBrowsable(EditorBrowsableState.Never)]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
public interface IConnectionPointContainer
{
	void EnumConnectionPoints(out IEnumConnectionPoints ppEnum);

	void FindConnectionPoint([In] ref Guid riid, out IConnectionPoint? ppCP);
}
