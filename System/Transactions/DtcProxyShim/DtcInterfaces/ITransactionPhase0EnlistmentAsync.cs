using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace System.Transactions.DtcProxyShim.DtcInterfaces;

[GeneratedComInterface]
[Guid("82DC88E1-A954-11d1-8F88-00600895E7D5")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
[IUnknownDerived<_003CSystem_Transactions_DtcProxyShim_DtcInterfaces_ITransactionPhase0EnlistmentAsync_003EF1EB6AC66EE59E7D8D5419EA35370EF80C9D0326AA49E65BF7C16C62469B20CEF__InterfaceInformation, _003CSystem_Transactions_DtcProxyShim_DtcInterfaces_ITransactionPhase0EnlistmentAsync_003EF1EB6AC66EE59E7D8D5419EA35370EF80C9D0326AA49E65BF7C16C62469B20CEF__InterfaceImplementation>]
internal interface ITransactionPhase0EnlistmentAsync
{
	void Enable();

	void WaitForEnlistment();

	void Phase0Done();

	void Unenlist();

	void GetTransaction([MarshalAs(UnmanagedType.Interface)] out ITransaction ppITransaction);
}
