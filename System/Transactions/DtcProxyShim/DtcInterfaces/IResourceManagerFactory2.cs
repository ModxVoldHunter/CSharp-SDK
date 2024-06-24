using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace System.Transactions.DtcProxyShim.DtcInterfaces;

[GeneratedComInterface]
[Guid("6B369C21-FBD2-11d1-8F47-00C04F8EE57D")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
[IUnknownDerived<_003CSystem_Transactions_DtcProxyShim_DtcInterfaces_IResourceManagerFactory2_003EF080D80D3DD035592B76E05B961A86C61CB7B5342BF3FBD5761E8EFB56142987A__InterfaceInformation, _003CSystem_Transactions_DtcProxyShim_DtcInterfaces_IResourceManagerFactory2_003EF080D80D3DD035592B76E05B961A86C61CB7B5342BF3FBD5761E8EFB56142987A__InterfaceImplementation>]
internal interface IResourceManagerFactory2
{
	internal void Create(in Guid pguidRM, [MarshalAs(UnmanagedType.LPStr)] string pszRMName, [MarshalAs(UnmanagedType.Interface)] IResourceManagerSink pIResMgrSink, [MarshalAs(UnmanagedType.Interface)] out IResourceManager rm);

	internal void CreateEx(in Guid pguidRM, [MarshalAs(UnmanagedType.LPStr)] string pszRMName, [MarshalAs(UnmanagedType.Interface)] IResourceManagerSink pIResMgrSink, in Guid riidRequested, [MarshalAs(UnmanagedType.Interface)] out object rm);
}
