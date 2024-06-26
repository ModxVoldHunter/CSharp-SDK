using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace System.Transactions.DtcProxyShim.DtcInterfaces;

[GeneratedComInterface]
[Guid("0141fda4-8fc0-11ce-bd18-204c4f4f5020")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
[IUnknownDerived<_003CSystem_Transactions_DtcProxyShim_DtcInterfaces_ITransactionImportWhereabouts_003EFA46475FEDDDC7E32749B542A07B93FC0A81442ED7D08EE66F264658312D06EE7__InterfaceInformation, _003CSystem_Transactions_DtcProxyShim_DtcInterfaces_ITransactionImportWhereabouts_003EFA46475FEDDDC7E32749B542A07B93FC0A81442ED7D08EE66F264658312D06EE7__InterfaceImplementation>]
internal interface ITransactionImportWhereabouts
{
	internal void GetWhereaboutsSize(out uint pcbSize);

	internal void GetWhereabouts(uint cbWhereabouts, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)] byte[] rgbWhereabouts, out uint pcbUsed);
}
