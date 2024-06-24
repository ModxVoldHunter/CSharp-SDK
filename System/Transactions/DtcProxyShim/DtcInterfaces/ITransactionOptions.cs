using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace System.Transactions.DtcProxyShim.DtcInterfaces;

[GeneratedComInterface]
[Guid("3A6AD9E0-23B9-11cf-AD60-00AA00A74CCD")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
[IUnknownDerived<_003CSystem_Transactions_DtcProxyShim_DtcInterfaces_ITransactionOptions_003EF9B4761294E8E3D52CB3E53258E2E9FF6E70A9845EB4EC9C13D4CFEA3889EB8D5__InterfaceInformation, _003CSystem_Transactions_DtcProxyShim_DtcInterfaces_ITransactionOptions_003EF9B4761294E8E3D52CB3E53258E2E9FF6E70A9845EB4EC9C13D4CFEA3889EB8D5__InterfaceImplementation>]
internal interface ITransactionOptions
{
	void SetOptions(Xactopt pOptions);

	void GetOptions();
}
