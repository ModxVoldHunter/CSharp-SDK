using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace System.Transactions.DtcProxyShim.DtcInterfaces;

[GeneratedComInterface]
[Guid("5433376B-414D-11d3-B206-00C04FC2F3EF")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
[IUnknownDerived<_003CSystem_Transactions_DtcProxyShim_DtcInterfaces_ITransactionVoterNotifyAsync2_003EF6D25C67B099C923F5B3B81D2426221BA44AC041A3EB43020F9DFF37D2A260614__InterfaceInformation, _003CSystem_Transactions_DtcProxyShim_DtcInterfaces_ITransactionVoterNotifyAsync2_003EF6D25C67B099C923F5B3B81D2426221BA44AC041A3EB43020F9DFF37D2A260614__InterfaceImplementation>]
internal interface ITransactionVoterNotifyAsync2
{
	void Committed([MarshalAs(UnmanagedType.Bool)] bool fRetaining, nint pNewUOW, uint hresult);

	void Aborted(nint pboidReason, [MarshalAs(UnmanagedType.Bool)] bool fRetaining, nint pNewUOW, uint hresult);

	void HeuristicDecision(OletxTransactionHeuristic dwDecision, nint pboidReason, uint hresult);

	void Indoubt();

	void VoteRequest();
}
