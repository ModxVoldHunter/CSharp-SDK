using System.Collections;
using System.Transactions.Oletx;

namespace System.Transactions;

internal abstract class TransactionStateDelegatedBase : TransactionStatePromoted
{
	internal override void EnterState(InternalTransaction tx)
	{
		if (tx._outcomeSource._isoLevel == IsolationLevel.Snapshot)
		{
			throw TransactionException.CreateInvalidOperationException(TraceSourceType.TraceSourceLtm, System.SR.CannotPromoteSnapshot, null, tx.DistributedTxId);
		}
		CommonEnterState(tx);
		OletxTransaction oletxTransaction = null;
		try
		{
			if (tx._durableEnlistment != null)
			{
				TransactionsEtwProvider log = TransactionsEtwProvider.Log;
				if (log.IsEnabled())
				{
					log.EnlistmentStatus(TraceSourceType.TraceSourceLtm, tx._durableEnlistment.EnlistmentTraceId, NotificationCall.Promote);
				}
			}
			oletxTransaction = TransactionState.TransactionStatePSPEOperation.PSPEPromote(tx);
		}
		catch (TransactionPromotionException innerException)
		{
			TransactionPromotionException exception = (TransactionPromotionException)(tx._innerException = innerException);
			TransactionsEtwProvider log2 = TransactionsEtwProvider.Log;
			if (log2.IsEnabled())
			{
				log2.ExceptionConsumed(exception);
			}
		}
		finally
		{
			if (oletxTransaction == null)
			{
				tx.State.ChangeStateAbortedDuringPromotion(tx);
			}
		}
		if (oletxTransaction != null && tx.PromotedTransaction != oletxTransaction)
		{
			tx.PromotedTransaction = oletxTransaction;
			Hashtable promotedTransactionTable = TransactionManager.PromotedTransactionTable;
			lock (promotedTransactionTable)
			{
				tx._finalizedObject = new FinalizedObject(tx, tx.PromotedTransaction.Identifier);
				WeakReference value = new WeakReference(tx._outcomeSource, trackResurrection: false);
				promotedTransactionTable[tx.PromotedTransaction.Identifier] = value;
			}
			TransactionManager.FireDistributedTransactionStarted(tx._outcomeSource);
			TransactionsEtwProvider log3 = TransactionsEtwProvider.Log;
			if (log3.IsEnabled())
			{
				log3.TransactionPromoted(tx.TransactionTraceId, oletxTransaction.TransactionTraceId);
			}
			PromoteEnlistmentsAndOutcome(tx);
		}
	}
}
