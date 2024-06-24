using System.ComponentModel;

namespace System.Runtime.InteropServices.ComTypes;

[ComImport]
[Guid("00020401-0000-0000-C000-000000000046")]
[EditorBrowsable(EditorBrowsableState.Never)]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
public interface ITypeInfo
{
	void GetTypeAttr(out nint ppTypeAttr);

	void GetTypeComp(out ITypeComp ppTComp);

	void GetFuncDesc(int index, out nint ppFuncDesc);

	void GetVarDesc(int index, out nint ppVarDesc);

	void GetNames(int memid, [Out][MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)] string[] rgBstrNames, int cMaxNames, out int pcNames);

	void GetRefTypeOfImplType(int index, out int href);

	void GetImplTypeFlags(int index, out IMPLTYPEFLAGS pImplTypeFlags);

	void GetIDsOfNames([In][MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.LPWStr, SizeParamIndex = 1)] string[] rgszNames, int cNames, [Out][MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] int[] pMemId);

	void Invoke([MarshalAs(UnmanagedType.IUnknown)] object pvInstance, int memid, short wFlags, ref DISPPARAMS pDispParams, nint pVarResult, nint pExcepInfo, out int puArgErr);

	void GetDocumentation(int index, out string strName, out string strDocString, out int dwHelpContext, out string strHelpFile);

	void GetDllEntry(int memid, INVOKEKIND invKind, nint pBstrDllName, nint pBstrName, nint pwOrdinal);

	void GetRefTypeInfo(int hRef, out ITypeInfo ppTI);

	void AddressOfMember(int memid, INVOKEKIND invKind, out nint ppv);

	void CreateInstance([MarshalAs(UnmanagedType.IUnknown)] object? pUnkOuter, [In] ref Guid riid, [MarshalAs(UnmanagedType.IUnknown)] out object ppvObj);

	void GetMops(int memid, out string? pBstrMops);

	void GetContainingTypeLib(out ITypeLib ppTLB, out int pIndex);

	[PreserveSig]
	void ReleaseTypeAttr(nint pTypeAttr);

	[PreserveSig]
	void ReleaseFuncDesc(nint pFuncDesc);

	[PreserveSig]
	void ReleaseVarDesc(nint pVarDesc);
}
