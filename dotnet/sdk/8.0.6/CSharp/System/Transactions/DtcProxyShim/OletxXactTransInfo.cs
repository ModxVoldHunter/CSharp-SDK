using System.Runtime.InteropServices;

namespace System.Transactions.DtcProxyShim;

[ComVisible(false)]
internal struct OletxXactTransInfo
{
	internal Guid Uow;

	internal OletxTransactionIsolationLevel IsoLevel;

	internal OletxTransactionIsoFlags IsoFlags;

	internal int GrfTCSupported;

	internal int GrfRMSupported;

	internal int GrfTCSupportedRetaining;

	internal int GrfRMSupportedRetaining;
}
