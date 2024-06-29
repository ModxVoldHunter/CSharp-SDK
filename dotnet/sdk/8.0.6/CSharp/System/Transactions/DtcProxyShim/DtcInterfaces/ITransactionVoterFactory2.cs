using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace System.Transactions.DtcProxyShim.DtcInterfaces;

[GeneratedComInterface]
[Guid("5433376A-414D-11d3-B206-00C04FC2F3EF")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
[IUnknownDerived<_003CSystem_Transactions_DtcProxyShim_DtcInterfaces_ITransactionVoterFactory2_003EF8502B5C41F7422DE681FC2FE7BBC009B35ECBEE7471E6BCEE19421C3E027377C__InterfaceInformation, _003CSystem_Transactions_DtcProxyShim_DtcInterfaces_ITransactionVoterFactory2_003EF8502B5C41F7422DE681FC2FE7BBC009B35ECBEE7471E6BCEE19421C3E027377C__InterfaceImplementation>]
internal interface ITransactionVoterFactory2
{
	void Create([MarshalAs(UnmanagedType.Interface)] ITransaction pITransaction, [MarshalAs(UnmanagedType.Interface)] ITransactionVoterNotifyAsync2 pVoterNotify, [MarshalAs(UnmanagedType.Interface)] out ITransactionVoterBallotAsync2 ppVoterBallot);
}
