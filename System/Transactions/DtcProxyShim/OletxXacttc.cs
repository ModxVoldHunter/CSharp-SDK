namespace System.Transactions.DtcProxyShim;

internal enum OletxXacttc : uint
{
	XACTTC_NONE = 0u,
	XACTTC_SYNC_PHASEONE = 1u,
	XACTTC_SYNC_PHASETWO = 2u,
	XACTTC_SYNC = 2u,
	XACTTC_ASYNC_PHASEONE = 4u,
	XACTTC_ASYNC = 4u
}
