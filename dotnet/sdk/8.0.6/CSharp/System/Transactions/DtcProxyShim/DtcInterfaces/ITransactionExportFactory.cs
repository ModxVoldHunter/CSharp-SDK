using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace System.Transactions.DtcProxyShim.DtcInterfaces;

[GeneratedComInterface]
[Guid("E1CF9B53-8745-11ce-A9BA-00AA006C3706")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
[IUnknownDerived<_003CSystem_Transactions_DtcProxyShim_DtcInterfaces_ITransactionExportFactory_003EF8A70EC5F19458FF8EDC54A5FF76F3931A57F8C0BAC30B0309B9947CF0AC215A8__InterfaceInformation, _003CSystem_Transactions_DtcProxyShim_DtcInterfaces_ITransactionExportFactory_003EF8A70EC5F19458FF8EDC54A5FF76F3931A57F8C0BAC30B0309B9947CF0AC215A8__InterfaceImplementation>]
internal interface ITransactionExportFactory
{
	void GetRemoteClassId(out Guid pclsid);

	void Create(uint cbWhereabouts, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)] byte[] rgbWhereabouts, [MarshalAs(UnmanagedType.Interface)] out ITransactionExport ppExport);
}
