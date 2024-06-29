using System.Threading;

namespace System.Transactions;

internal sealed class DurableEnlistmentAborting : DurableEnlistmentState
{
	internal override void EnterState(InternalEnlistment enlistment)
	{
		enlistment.State = this;
		Monitor.Exit(enlistment.Transaction);
		try
		{
			TransactionsEtwProvider log = TransactionsEtwProvider.Log;
			if (log.IsEnabled())
			{
				log.EnlistmentStatus(TraceSourceType.TraceSourceLtm, enlistment.EnlistmentTraceId, NotificationCall.Rollback);
			}
			if (enlistment.SinglePhaseNotification != null)
			{
				enlistment.SinglePhaseNotification.Rollback(enlistment.SinglePhaseEnlistment);
			}
			else
			{
				enlistment.PromotableSinglePhaseNotification.Rollback(enlistment.SinglePhaseEnlistment);
			}
		}
		finally
		{
			Monitor.Enter(enlistment.Transaction);
		}
	}

	internal override void Aborted(InternalEnlistment enlistment, Exception e)
	{
		InternalTransaction transaction = enlistment.Transaction;
		if (transaction._innerException == null)
		{
			transaction._innerException = e;
		}
		DurableEnlistmentState.DurableEnlistmentEnded.EnterState(enlistment);
	}

	internal override void EnlistmentDone(InternalEnlistment enlistment)
	{
		DurableEnlistmentState.DurableEnlistmentEnded.EnterState(enlistment);
	}
}
