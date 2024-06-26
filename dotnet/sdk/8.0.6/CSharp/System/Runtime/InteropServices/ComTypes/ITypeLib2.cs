using System.ComponentModel;

namespace System.Runtime.InteropServices.ComTypes;

[ComImport]
[Guid("00020411-0000-0000-C000-000000000046")]
[EditorBrowsable(EditorBrowsableState.Never)]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
public interface ITypeLib2 : ITypeLib
{
	[PreserveSig]
	new int GetTypeInfoCount();

	new void GetTypeInfo(int index, out ITypeInfo ppTI);

	new void GetTypeInfoType(int index, out TYPEKIND pTKind);

	new void GetTypeInfoOfGuid(ref Guid guid, out ITypeInfo ppTInfo);

	new void GetLibAttr(out nint ppTLibAttr);

	new void GetTypeComp(out ITypeComp ppTComp);

	new void GetDocumentation(int index, out string strName, out string strDocString, out int dwHelpContext, out string strHelpFile);

	[return: MarshalAs(UnmanagedType.Bool)]
	new bool IsName([MarshalAs(UnmanagedType.LPWStr)] string szNameBuf, int lHashVal);

	new void FindName([MarshalAs(UnmanagedType.LPWStr)] string szNameBuf, int lHashVal, [Out][MarshalAs(UnmanagedType.LPArray)] ITypeInfo[] ppTInfo, [Out][MarshalAs(UnmanagedType.LPArray)] int[] rgMemId, ref short pcFound);

	[PreserveSig]
	new void ReleaseTLibAttr(nint pTLibAttr);

	void GetCustData(ref Guid guid, out object pVarVal);

	[LCIDConversion(1)]
	void GetDocumentation2(int index, out string pbstrHelpString, out int pdwHelpStringContext, out string pbstrHelpStringDll);

	void GetLibStatistics(nint pcUniqueNames, out int pcchUniqueNames);

	void GetAllCustData(nint pCustData);
}
