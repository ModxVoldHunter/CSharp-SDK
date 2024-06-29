namespace System.Transactions.Oletx;

[Serializable]
internal sealed class OletxCommittableTransaction : OletxTransaction
{
	private bool _commitCalled;

	internal bool CommitCalled => _commitCalled;

	internal OletxCommittableTransaction(RealOletxTransaction realOletxTransaction)
		: base(realOletxTransaction)
	{
		realOletxTransaction.CommittableTransaction = this;
	}

	internal void BeginCommit(InternalTransaction internalTransaction)
	{
		TransactionsEtwProvider log = TransactionsEtwProvider.Log;
		if (log.IsEnabled())
		{
			log.MethodEnter(TraceSourceType.TraceSourceOleTx, this, "BeginCommit");
			log.TransactionCommit(TraceSourceType.TraceSourceOleTx, base.TransactionTraceId, "CommittableTransaction");
		}
		RealOletxTransaction.InternalTransaction = internalTransaction;
		_commitCalled = true;
		RealOletxTransaction.Commit();
		if (log.IsEnabled())
		{
			log.MethodExit(TraceSourceType.TraceSourceOleTx, this, "OletxCommittableTransaction.BeginCommit");
		}
	}
}
