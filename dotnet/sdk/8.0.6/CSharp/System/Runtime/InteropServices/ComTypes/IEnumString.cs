using System.ComponentModel;

namespace System.Runtime.InteropServices.ComTypes;

[ComImport]
[Guid("00000101-0000-0000-C000-000000000046")]
[EditorBrowsable(EditorBrowsableState.Never)]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
public interface IEnumString
{
	[PreserveSig]
	int Next(int celt, [Out][MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.LPWStr, SizeParamIndex = 0)] string[] rgelt, nint pceltFetched);

	[PreserveSig]
	int Skip(int celt);

	void Reset();

	void Clone(out IEnumString ppenum);
}
