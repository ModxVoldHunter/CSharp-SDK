using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace System.Transactions.DtcProxyShim.DtcInterfaces;

[GeneratedComInterface]
[Guid("0D563181-DEFB-11CE-AED1-00AA0051E2C4")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
[IUnknownDerived<_003CSystem_Transactions_DtcProxyShim_DtcInterfaces_IResourceManagerSink_003EF208849E591366B7D76D6EEFF9ABA315AA2D3410ABAB759EDDE4ECF11EC8CFF37__InterfaceInformation, _003CSystem_Transactions_DtcProxyShim_DtcInterfaces_IResourceManagerSink_003EF208849E591366B7D76D6EEFF9ABA315AA2D3410ABAB759EDDE4ECF11EC8CFF37__InterfaceImplementation>]
internal interface IResourceManagerSink
{
	void TMDown();
}
