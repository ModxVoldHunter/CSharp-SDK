using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace System.Transactions.DtcProxyShim.DtcInterfaces;

[GeneratedComInterface]
[Guid("30274F88-6EE4-474e-9B95-7807BC9EF8CF")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
[IUnknownDerived<_003CSystem_Transactions_DtcProxyShim_DtcInterfaces_ITmNodeName_003EF6F9699CE0A397769E7C642475511667D12758E535EC02BB20C801306EC381A7B__InterfaceInformation, _003CSystem_Transactions_DtcProxyShim_DtcInterfaces_ITmNodeName_003EF6F9699CE0A397769E7C642475511667D12758E535EC02BB20C801306EC381A7B__InterfaceImplementation>]
internal interface ITmNodeName
{
	internal void GetNodeNameSize(out uint pcbNodeNameSize);

	internal void GetNodeName(uint cbNodeNameBufferSize, [MarshalAs(UnmanagedType.LPWStr)] out string pcbNodeSize);
}
