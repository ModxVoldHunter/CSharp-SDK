namespace System.Transactions;

internal sealed class Phase1VolatileDemultiplexer : VolatileDemultiplexer
{
	public Phase1VolatileDemultiplexer(InternalTransaction transaction)
		: base(transaction)
	{
	}

	protected override void InternalPrepare()
	{
		try
		{
			_transaction.State.ChangeStatePromotedPhase1(_transaction);
		}
		catch (TransactionAbortedException ex)
		{
			_promotedEnlistment.ForceRollback(ex);
			TransactionsEtwProvider log = TransactionsEtwProvider.Log;
			if (log.IsEnabled())
			{
				log.ExceptionConsumed(ex);
			}
		}
		catch (TransactionInDoubtException exception)
		{
			_promotedEnlistment.EnlistmentDone();
			TransactionsEtwProvider log2 = TransactionsEtwProvider.Log;
			if (log2.IsEnabled())
			{
				log2.ExceptionConsumed(exception);
			}
		}
	}

	protected override void InternalCommit()
	{
		_promotedEnlistment.EnlistmentDone();
		_transaction.State.ChangeStatePromotedCommitted(_transaction);
	}

	protected override void InternalRollback()
	{
		_promotedEnlistment.EnlistmentDone();
		_transaction.State.ChangeStatePromotedAborted(_transaction);
	}

	protected override void InternalInDoubt()
	{
		_transaction.State.InDoubtFromDtc(_transaction);
	}

	public override void Prepare(IPromotedEnlistment en)
	{
		_preparingEnlistment = en;
		VolatileDemultiplexer.PoolablePrepare(this);
	}

	public override void Commit(IPromotedEnlistment en)
	{
		_promotedEnlistment = en;
		VolatileDemultiplexer.PoolableCommit(this);
	}

	public override void Rollback(IPromotedEnlistment en)
	{
		_promotedEnlistment = en;
		VolatileDemultiplexer.PoolableRollback(this);
	}

	public override void InDoubt(IPromotedEnlistment en)
	{
		_promotedEnlistment = en;
		VolatileDemultiplexer.PoolableInDoubt(this);
	}
}
