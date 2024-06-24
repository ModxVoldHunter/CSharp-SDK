using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace System.Transactions.DtcProxyShim.DtcInterfaces;

[GeneratedComInterface]
[Guid("80c7bfd0-87ee-11ce-8081-0080c758527e")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
[IUnknownDerived<_003CSystem_Transactions_DtcProxyShim_DtcInterfaces_IPrepareInfo_003EF59E9ACF0C07032088BD2F8F7B1A3FC4090F871804A14E706885EDEAD02218519__InterfaceInformation, _003CSystem_Transactions_DtcProxyShim_DtcInterfaces_IPrepareInfo_003EF59E9ACF0C07032088BD2F8F7B1A3FC4090F871804A14E706885EDEAD02218519__InterfaceImplementation>]
internal interface IPrepareInfo
{
	void GetPrepareInfoSize(out uint pcbPrepInfo);

	void GetPrepareInfo([MarshalAs(UnmanagedType.LPArray)] byte[] pPrepInfo);
}
