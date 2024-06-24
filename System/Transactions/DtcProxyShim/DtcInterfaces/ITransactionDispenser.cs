using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace System.Transactions.DtcProxyShim.DtcInterfaces;

[GeneratedComInterface]
[Guid("3A6AD9E1-23B9-11cf-AD60-00AA00A74CCD")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
[IUnknownDerived<_003CSystem_Transactions_DtcProxyShim_DtcInterfaces_ITransactionDispenser_003EF2B57004AEDA33D5278488E75DE994EC1DCD8DB1DECF128CF776E2AAC841B0325__InterfaceInformation, _003CSystem_Transactions_DtcProxyShim_DtcInterfaces_ITransactionDispenser_003EF2B57004AEDA33D5278488E75DE994EC1DCD8DB1DECF128CF776E2AAC841B0325__InterfaceImplementation>]
internal interface ITransactionDispenser
{
	void GetOptionsObject([MarshalAs(UnmanagedType.Interface)] out ITransactionOptions ppOptions);

	void BeginTransaction(nint punkOuter, OletxTransactionIsolationLevel isoLevel, OletxTransactionIsoFlags isoFlags, [MarshalAs(UnmanagedType.Interface)] ITransactionOptions pOptions, [MarshalAs(UnmanagedType.Interface)] out ITransaction ppTransaction);
}
