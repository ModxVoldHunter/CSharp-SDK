using System.Threading;

namespace System.Transactions.Oletx;

[Serializable]
internal sealed class OletxDependentTransaction : OletxTransaction
{
	private readonly OletxVolatileEnlistmentContainer _volatileEnlistmentContainer;

	private int _completed;

	internal OletxDependentTransaction(RealOletxTransaction realTransaction, bool delayCommit)
		: base(realTransaction)
	{
		ArgumentNullException.ThrowIfNull(realTransaction, "realTransaction");
		_volatileEnlistmentContainer = RealOletxTransaction.AddDependentClone(delayCommit);
		TransactionsEtwProvider log = TransactionsEtwProvider.Log;
		if (log.IsEnabled())
		{
			log.TransactionDependentCloneCreate(TraceSourceType.TraceSourceOleTx, base.TransactionTraceId, (!delayCommit) ? DependentCloneOption.RollbackIfNotComplete : DependentCloneOption.BlockCommitUntilComplete);
		}
	}

	public void Complete()
	{
		TransactionsEtwProvider log = TransactionsEtwProvider.Log;
		if (log.IsEnabled())
		{
			log.MethodEnter(TraceSourceType.TraceSourceOleTx, this, "DependentTransaction.Complete");
		}
		int num = Interlocked.Exchange(ref _completed, 1);
		if (num == 1)
		{
			throw TransactionException.CreateTransactionCompletedException(base.DistributedTxId);
		}
		if (log.IsEnabled())
		{
			log.TransactionDependentCloneComplete(TraceSourceType.TraceSourceOleTx, base.TransactionTraceId, "DependentTransaction");
		}
		_volatileEnlistmentContainer.DependentCloneCompleted();
		if (log.IsEnabled())
		{
			log.MethodExit(TraceSourceType.TraceSourceOleTx, this, "DependentTransaction.Complete");
		}
	}
}
