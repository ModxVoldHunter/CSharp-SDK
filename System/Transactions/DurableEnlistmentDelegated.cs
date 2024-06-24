namespace System.Transactions;

internal sealed class DurableEnlistmentDelegated : DurableEnlistmentState
{
	internal override void EnterState(InternalEnlistment enlistment)
	{
		enlistment.State = this;
	}

	internal override void Committed(InternalEnlistment enlistment)
	{
		DurableEnlistmentState.DurableEnlistmentEnded.EnterState(enlistment);
		enlistment.Transaction.State.ChangeStatePromotedCommitted(enlistment.Transaction);
	}

	internal override void Aborted(InternalEnlistment enlistment, Exception e)
	{
		DurableEnlistmentState.DurableEnlistmentEnded.EnterState(enlistment);
		InternalTransaction transaction = enlistment.Transaction;
		if (transaction._innerException == null)
		{
			transaction._innerException = e;
		}
		enlistment.Transaction.State.ChangeStatePromotedAborted(enlistment.Transaction);
	}

	internal override void InDoubt(InternalEnlistment enlistment, Exception e)
	{
		DurableEnlistmentState.DurableEnlistmentEnded.EnterState(enlistment);
		InternalTransaction transaction = enlistment.Transaction;
		if (transaction._innerException == null)
		{
			transaction._innerException = e;
		}
		enlistment.Transaction.State.InDoubtFromEnlistment(enlistment.Transaction);
	}
}
