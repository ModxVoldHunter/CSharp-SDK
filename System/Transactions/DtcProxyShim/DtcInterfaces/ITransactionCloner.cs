using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace System.Transactions.DtcProxyShim.DtcInterfaces;

[GeneratedComInterface]
[Guid("02656950-2152-11d0-944C-00A0C905416E")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
[IUnknownDerived<_003CSystem_Transactions_DtcProxyShim_DtcInterfaces_ITransactionCloner_003EFA62063ED372AFDDBA875D74F06F51218C1ECB42AD0F6910CC288C2B06B257A27__InterfaceInformation, _003CSystem_Transactions_DtcProxyShim_DtcInterfaces_ITransactionCloner_003EFA62063ED372AFDDBA875D74F06F51218C1ECB42AD0F6910CC288C2B06B257A27__InterfaceImplementation>]
internal interface ITransactionCloner
{
	void Commit([MarshalAs(UnmanagedType.Bool)] bool fRetainingt, OletxXacttc grfTC, uint grfRM);

	void Abort(nint reason, [MarshalAs(UnmanagedType.Bool)] bool retaining, [MarshalAs(UnmanagedType.Bool)] bool async);

	void GetTransactionInfo(out OletxXactTransInfo xactInfo);

	void CloneWithCommitDisabled([MarshalAs(UnmanagedType.Interface)] out ITransaction ppITransaction);
}
