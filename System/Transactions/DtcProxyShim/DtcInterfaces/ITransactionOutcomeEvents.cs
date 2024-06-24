using System.Runtime.InteropServices;

namespace System.Transactions.DtcProxyShim.DtcInterfaces;

[ComImport]
[Guid("3A6AD9E2-23B9-11cf-AD60-00AA00A74CCD")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface ITransactionOutcomeEvents
{
	void Committed([MarshalAs(UnmanagedType.Bool)] bool fRetaining, nint pNewUOW, int hresult);

	void Aborted(nint pboidReason, [MarshalAs(UnmanagedType.Bool)] bool fRetaining, nint pNewUOW, int hresult);

	void HeuristicDecision(OletxTransactionHeuristic dwDecision, nint pboidReason, int hresult);

	void Indoubt();
}
