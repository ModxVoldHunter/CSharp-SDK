using System.Threading;

namespace System.Transactions;

internal sealed class TransactionStateDelegatedCommitting : TransactionStatePromotedCommitting
{
	internal override void EnterState(InternalTransaction tx)
	{
		CommonEnterState(tx);
		Monitor.Exit(tx);
		TransactionsEtwProvider log = TransactionsEtwProvider.Log;
		if (log.IsEnabled())
		{
			log.EnlistmentStatus(TraceSourceType.TraceSourceLtm, tx._durableEnlistment.EnlistmentTraceId, NotificationCall.SinglePhaseCommit);
		}
		try
		{
			tx._durableEnlistment.PromotableSinglePhaseNotification.SinglePhaseCommit(tx._durableEnlistment.SinglePhaseEnlistment);
		}
		finally
		{
			Monitor.Enter(tx);
		}
	}
}
