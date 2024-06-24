using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace System.Transactions.DtcProxyShim.DtcInterfaces;

[GeneratedComInterface]
[Guid("0fb15084-af41-11ce-bd2b-204c4f4f5020")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
[IUnknownDerived<_003CSystem_Transactions_DtcProxyShim_DtcInterfaces_ITransaction_003EF6BCA9EB471494E7FD8AF56CC8961A010187AB58347F3F5DD98806230A28C4E2E__InterfaceInformation, _003CSystem_Transactions_DtcProxyShim_DtcInterfaces_ITransaction_003EF6BCA9EB471494E7FD8AF56CC8961A010187AB58347F3F5DD98806230A28C4E2E__InterfaceImplementation>]
internal interface ITransaction
{
	void Commit([MarshalAs(UnmanagedType.Bool)] bool fRetaining, OletxXacttc grfTC, uint grfRM);

	void Abort(nint reason, [MarshalAs(UnmanagedType.Bool)] bool retaining, [MarshalAs(UnmanagedType.Bool)] bool async);

	void GetTransactionInfo(out OletxXactTransInfo xactInfo);
}
