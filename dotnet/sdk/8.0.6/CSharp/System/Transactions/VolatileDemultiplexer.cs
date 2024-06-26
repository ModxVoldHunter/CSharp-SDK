using System.Threading;

namespace System.Transactions;

internal abstract class VolatileDemultiplexer : IEnlistmentNotificationInternal
{
	protected InternalTransaction _transaction;

	internal IPromotedEnlistment _promotedEnlistment;

	internal IPromotedEnlistment _preparingEnlistment;

	private static object s_classSyncObject;

	private static WaitCallback s_prepareCallback;

	private static WaitCallback s_commitCallback;

	private static WaitCallback s_rollbackCallback;

	private static WaitCallback s_inDoubtCallback;

	private static WaitCallback PrepareCallback => LazyInitializer.EnsureInitialized<WaitCallback>(ref s_prepareCallback, ref s_classSyncObject, () => PoolablePrepare);

	private static WaitCallback CommitCallback => LazyInitializer.EnsureInitialized<WaitCallback>(ref s_commitCallback, ref s_classSyncObject, () => PoolableCommit);

	private static WaitCallback RollbackCallback => LazyInitializer.EnsureInitialized<WaitCallback>(ref s_rollbackCallback, ref s_classSyncObject, () => PoolableRollback);

	private static WaitCallback InDoubtCallback => LazyInitializer.EnsureInitialized<WaitCallback>(ref s_inDoubtCallback, ref s_classSyncObject, () => PoolableInDoubt);

	public VolatileDemultiplexer(InternalTransaction transaction)
	{
		_transaction = transaction;
	}

	internal static void BroadcastCommitted(ref VolatileEnlistmentSet volatiles)
	{
		for (int i = 0; i < volatiles._volatileEnlistmentCount; i++)
		{
			volatiles._volatileEnlistments[i]._twoPhaseState.InternalCommitted(volatiles._volatileEnlistments[i]);
		}
	}

	internal static void BroadcastRollback(ref VolatileEnlistmentSet volatiles)
	{
		for (int i = 0; i < volatiles._volatileEnlistmentCount; i++)
		{
			volatiles._volatileEnlistments[i]._twoPhaseState.InternalAborted(volatiles._volatileEnlistments[i]);
		}
	}

	internal static void BroadcastInDoubt(ref VolatileEnlistmentSet volatiles)
	{
		for (int i = 0; i < volatiles._volatileEnlistmentCount; i++)
		{
			volatiles._volatileEnlistments[i]._twoPhaseState.InternalIndoubt(volatiles._volatileEnlistments[i]);
		}
	}

	protected static void PoolablePrepare(object state)
	{
		VolatileDemultiplexer volatileDemultiplexer = (VolatileDemultiplexer)state;
		bool lockTaken = false;
		try
		{
			Monitor.TryEnter(volatileDemultiplexer._transaction, 250, ref lockTaken);
			if (lockTaken)
			{
				volatileDemultiplexer.InternalPrepare();
			}
			else if (!ThreadPool.QueueUserWorkItem(PrepareCallback, volatileDemultiplexer))
			{
				throw TransactionException.CreateInvalidOperationException(TraceSourceType.TraceSourceLtm, System.SR.UnexpectedFailureOfThreadPool, null);
			}
		}
		finally
		{
			if (lockTaken)
			{
				Monitor.Exit(volatileDemultiplexer._transaction);
			}
		}
	}

	protected static void PoolableCommit(object state)
	{
		VolatileDemultiplexer volatileDemultiplexer = (VolatileDemultiplexer)state;
		bool lockTaken = false;
		try
		{
			Monitor.TryEnter(volatileDemultiplexer._transaction, 250, ref lockTaken);
			if (lockTaken)
			{
				volatileDemultiplexer.InternalCommit();
			}
			else if (!ThreadPool.QueueUserWorkItem(CommitCallback, volatileDemultiplexer))
			{
				throw TransactionException.CreateInvalidOperationException(TraceSourceType.TraceSourceLtm, System.SR.UnexpectedFailureOfThreadPool, null);
			}
		}
		finally
		{
			if (lockTaken)
			{
				Monitor.Exit(volatileDemultiplexer._transaction);
			}
		}
	}

	protected static void PoolableRollback(object state)
	{
		VolatileDemultiplexer volatileDemultiplexer = (VolatileDemultiplexer)state;
		bool lockTaken = false;
		try
		{
			Monitor.TryEnter(volatileDemultiplexer._transaction, 250, ref lockTaken);
			if (lockTaken)
			{
				volatileDemultiplexer.InternalRollback();
			}
			else if (!ThreadPool.QueueUserWorkItem(RollbackCallback, volatileDemultiplexer))
			{
				throw TransactionException.CreateInvalidOperationException(TraceSourceType.TraceSourceLtm, System.SR.UnexpectedFailureOfThreadPool, null);
			}
		}
		finally
		{
			if (lockTaken)
			{
				Monitor.Exit(volatileDemultiplexer._transaction);
			}
		}
	}

	protected static void PoolableInDoubt(object state)
	{
		VolatileDemultiplexer volatileDemultiplexer = (VolatileDemultiplexer)state;
		bool lockTaken = false;
		try
		{
			Monitor.TryEnter(volatileDemultiplexer._transaction, 250, ref lockTaken);
			if (lockTaken)
			{
				volatileDemultiplexer.InternalInDoubt();
			}
			else if (!ThreadPool.QueueUserWorkItem(InDoubtCallback, volatileDemultiplexer))
			{
				throw TransactionException.CreateInvalidOperationException(TraceSourceType.TraceSourceLtm, System.SR.UnexpectedFailureOfThreadPool, null);
			}
		}
		finally
		{
			if (lockTaken)
			{
				Monitor.Exit(volatileDemultiplexer._transaction);
			}
		}
	}

	protected abstract void InternalPrepare();

	protected abstract void InternalCommit();

	protected abstract void InternalRollback();

	protected abstract void InternalInDoubt();

	public abstract void Prepare(IPromotedEnlistment en);

	public abstract void Commit(IPromotedEnlistment en);

	public abstract void Rollback(IPromotedEnlistment en);

	public abstract void InDoubt(IPromotedEnlistment en);
}
