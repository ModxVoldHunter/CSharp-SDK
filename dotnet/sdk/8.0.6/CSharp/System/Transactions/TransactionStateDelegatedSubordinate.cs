namespace System.Transactions;

internal sealed class TransactionStateDelegatedSubordinate : TransactionStateDelegatedBase
{
	internal override bool PromoteDurable(InternalTransaction tx)
	{
		return true;
	}

	internal override void Rollback(InternalTransaction tx, Exception e)
	{
		if (tx._innerException == null)
		{
			tx._innerException = e;
		}
		tx.PromotedTransaction.Rollback();
		TransactionState.TransactionStatePromotedAborted.EnterState(tx);
	}

	internal override void ChangeStatePromotedPhase0(InternalTransaction tx)
	{
		TransactionState.TransactionStatePromotedPhase0.EnterState(tx);
	}

	internal override void ChangeStatePromotedPhase1(InternalTransaction tx)
	{
		TransactionState.TransactionStatePromotedPhase1.EnterState(tx);
	}
}
