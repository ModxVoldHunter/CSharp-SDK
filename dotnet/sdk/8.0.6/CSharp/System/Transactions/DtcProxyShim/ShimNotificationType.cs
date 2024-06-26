namespace System.Transactions.DtcProxyShim;

internal enum ShimNotificationType
{
	None,
	Phase0RequestNotify,
	VoteRequestNotify,
	PrepareRequestNotify,
	CommitRequestNotify,
	AbortRequestNotify,
	CommittedNotify,
	AbortedNotify,
	InDoubtNotify,
	EnlistmentTmDownNotify,
	ResourceManagerTmDownNotify
}
