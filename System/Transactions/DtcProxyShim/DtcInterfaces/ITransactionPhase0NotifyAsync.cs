using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace System.Transactions.DtcProxyShim.DtcInterfaces;

[GeneratedComInterface]
[Guid("EF081809-0C76-11d2-87A6-00C04F990F34")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
[IUnknownDerived<_003CSystem_Transactions_DtcProxyShim_DtcInterfaces_ITransactionPhase0NotifyAsync_003EF20E524DD0787EB4E8D29935FABE09B3F7FD9EF7663055BB9E8DC8CD397ACA00B__InterfaceInformation, _003CSystem_Transactions_DtcProxyShim_DtcInterfaces_ITransactionPhase0NotifyAsync_003EF20E524DD0787EB4E8D29935FABE09B3F7FD9EF7663055BB9E8DC8CD397ACA00B__InterfaceImplementation>]
internal interface ITransactionPhase0NotifyAsync
{
	void Phase0Request([MarshalAs(UnmanagedType.Bool)] bool fAbortHint);

	void EnlistCompleted(int status);
}
