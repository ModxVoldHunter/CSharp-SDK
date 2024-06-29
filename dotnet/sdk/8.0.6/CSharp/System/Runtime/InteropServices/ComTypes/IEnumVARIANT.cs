using System.ComponentModel;

namespace System.Runtime.InteropServices.ComTypes;

[ComImport]
[Guid("00020404-0000-0000-C000-000000000046")]
[EditorBrowsable(EditorBrowsableState.Never)]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
public interface IEnumVARIANT
{
	[PreserveSig]
	int Next(int celt, [Out][MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)] object?[] rgVar, nint pceltFetched);

	[PreserveSig]
	int Skip(int celt);

	[PreserveSig]
	int Reset();

	IEnumVARIANT Clone();
}
