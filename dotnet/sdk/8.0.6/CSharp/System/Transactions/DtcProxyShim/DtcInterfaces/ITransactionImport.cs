using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace System.Transactions.DtcProxyShim.DtcInterfaces;

[GeneratedComInterface]
[Guid("E1CF9B5A-8745-11ce-A9BA-00AA006C3706")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
[IUnknownDerived<_003CSystem_Transactions_DtcProxyShim_DtcInterfaces_ITransactionImport_003EF12273B61332E88613B7593BF1406DD97D177E1A16C2A739D08AFD6844DE9E498__InterfaceInformation, _003CSystem_Transactions_DtcProxyShim_DtcInterfaces_ITransactionImport_003EF12273B61332E88613B7593BF1406DD97D177E1A16C2A739D08AFD6844DE9E498__InterfaceImplementation>]
internal interface ITransactionImport
{
	void Import(uint cbTransactionCookie, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)] byte[] rgbTransactionCookie, in Guid piid, [MarshalAs(UnmanagedType.Interface)] out object ppvTransaction);
}
