using System.Transactions.Oletx;

namespace System.Transactions;

internal class TransactionStatePromotedCommitting : TransactionStatePromotedBase
{
	internal override void EnterState(InternalTransaction tx)
	{
		CommonEnterState(tx);
		OletxCommittableTransaction oletxCommittableTransaction = (OletxCommittableTransaction)tx.PromotedTransaction;
		oletxCommittableTransaction.BeginCommit(tx);
	}

	internal override void BeginCommit(InternalTransaction tx, bool asyncCommit, AsyncCallback asyncCallback, object asyncState)
	{
		throw TransactionException.CreateTransactionStateException(tx._innerException, tx.DistributedTxId);
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
