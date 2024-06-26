using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace System.Transactions.DtcProxyShim.DtcInterfaces;

[GeneratedComInterface]
[Guid("0141fda5-8fc0-11ce-bd18-204c4f4f5020")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
[IUnknownDerived<_003CSystem_Transactions_DtcProxyShim_DtcInterfaces_ITransactionExport_003EF9C5B814B7162C590559407924B657F0932CDD922998759D4A68F881CE7950308__InterfaceInformation, _003CSystem_Transactions_DtcProxyShim_DtcInterfaces_ITransactionExport_003EF9C5B814B7162C590559407924B657F0932CDD922998759D4A68F881CE7950308__InterfaceImplementation>]
internal interface ITransactionExport
{
	void Export([MarshalAs(UnmanagedType.Interface)] ITransaction punkTransaction, out uint pcbTransactionCookie);

	void GetTransactionCookie([MarshalAs(UnmanagedType.Interface)] ITransaction pITransaction, uint cbTransactionCookie, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] byte[] rgbTransactionCookie, out uint pcbUsed);
}
