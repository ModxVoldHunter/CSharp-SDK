using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace System.Transactions.DtcProxyShim.DtcInterfaces;

[GeneratedComInterface]
[Guid("69E971F0-23CE-11cf-AD60-00AA00A74CCD")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
[IUnknownDerived<_003CSystem_Transactions_DtcProxyShim_DtcInterfaces_ITransactionResourceAsync_003EF7C2C4B9D6A24A006C5DAEC87551E1FDD2C9C0D22FF6954ED87ABCFB258CD958F__InterfaceInformation, _003CSystem_Transactions_DtcProxyShim_DtcInterfaces_ITransactionResourceAsync_003EF7C2C4B9D6A24A006C5DAEC87551E1FDD2C9C0D22FF6954ED87ABCFB258CD958F__InterfaceImplementation>]
internal interface ITransactionResourceAsync
{
	void PrepareRequest([MarshalAs(UnmanagedType.Bool)] bool fRetaining, OletxXactRm grfRM, [MarshalAs(UnmanagedType.Bool)] bool fWantMoniker, [MarshalAs(UnmanagedType.Bool)] bool fSinglePhase);

	void CommitRequest(OletxXactRm grfRM, nint pNewUOW);

	void AbortRequest(nint pboidReason, [MarshalAs(UnmanagedType.Bool)] bool fRetaining, nint pNewUOW);

	void TMDown();
}
