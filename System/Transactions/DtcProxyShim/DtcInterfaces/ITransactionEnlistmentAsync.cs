using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace System.Transactions.DtcProxyShim.DtcInterfaces;

[GeneratedComInterface]
[Guid("0fb15081-af41-11ce-bd2b-204c4f4f5020")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
[IUnknownDerived<_003CSystem_Transactions_DtcProxyShim_DtcInterfaces_ITransactionEnlistmentAsync_003EF0A570C27F4F25EB5132D2B3CAE79E81CDA3880E098CA5CDEB2D6F094E39197C7__InterfaceInformation, _003CSystem_Transactions_DtcProxyShim_DtcInterfaces_ITransactionEnlistmentAsync_003EF0A570C27F4F25EB5132D2B3CAE79E81CDA3880E098CA5CDEB2D6F094E39197C7__InterfaceImplementation>]
internal interface ITransactionEnlistmentAsync
{
	void PrepareRequestDone(int hr, nint pmk, nint pboidReason);

	void CommitRequestDone(int hr);

	void AbortRequestDone(int hr);
}
