using System.Transactions.DtcProxyShim.DtcInterfaces;

namespace System.Transactions.DtcProxyShim;

internal sealed class TransactionNotifyShim : NotificationShimBase, ITransactionOutcomeEvents
{
	internal TransactionNotifyShim(DtcProxyShimFactory shimFactory, object enlistmentIdentifier)
		: base(shimFactory, enlistmentIdentifier)
	{
	}

	public void Committed(bool fRetaining, nint pNewUOW, int hresult)
	{
		NotificationType = ShimNotificationType.CommittedNotify;
		ShimFactory.NewNotification(this);
	}

	public void Aborted(nint pboidReason, bool fRetaining, nint pNewUOW, int hresult)
	{
		NotificationType = ShimNotificationType.AbortedNotify;
		ShimFactory.NewNotification(this);
	}

	public void HeuristicDecision(OletxTransactionHeuristic dwDecision, nint pboidReason, int hresult)
	{
		NotificationType = dwDecision switch
		{
			OletxTransactionHeuristic.XACTHEURISTIC_ABORT => ShimNotificationType.AbortedNotify, 
			OletxTransactionHeuristic.XACTHEURISTIC_COMMIT => ShimNotificationType.CommittedNotify, 
			_ => ShimNotificationType.InDoubtNotify, 
		};
		ShimFactory.NewNotification(this);
	}

	public void Indoubt()
	{
		NotificationType = ShimNotificationType.InDoubtNotify;
		ShimFactory.NewNotification(this);
	}
}
