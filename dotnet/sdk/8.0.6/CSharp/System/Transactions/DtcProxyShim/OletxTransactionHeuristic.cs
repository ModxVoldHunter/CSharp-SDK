namespace System.Transactions.DtcProxyShim;

internal enum OletxTransactionHeuristic : uint
{
	XACTHEURISTIC_ABORT = 1u,
	XACTHEURISTIC_COMMIT,
	XACTHEURISTIC_DAMAGE,
	XACTHEURISTIC_DANGER
}
