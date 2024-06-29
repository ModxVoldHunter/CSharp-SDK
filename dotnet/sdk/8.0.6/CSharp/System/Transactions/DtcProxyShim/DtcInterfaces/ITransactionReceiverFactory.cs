using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace System.Transactions.DtcProxyShim.DtcInterfaces;

[GeneratedComInterface]
[Guid("59313E02-B36C-11cf-A539-00AA006887C3")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
[IUnknownDerived<_003CSystem_Transactions_DtcProxyShim_DtcInterfaces_ITransactionReceiverFactory_003EF71EF4C78F324525CFE1FEEB5E2F7C71F6378DFDB1FB4F09B03C42F92F6A7DFC1__InterfaceInformation, _003CSystem_Transactions_DtcProxyShim_DtcInterfaces_ITransactionReceiverFactory_003EF71EF4C78F324525CFE1FEEB5E2F7C71F6378DFDB1FB4F09B03C42F92F6A7DFC1__InterfaceImplementation>]
internal interface ITransactionReceiverFactory
{
	void Create([MarshalAs(UnmanagedType.Interface)] out ITransactionReceiver pTxReceiver);
}
