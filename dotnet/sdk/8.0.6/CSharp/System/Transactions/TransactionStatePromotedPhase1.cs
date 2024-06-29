using System.Threading;

namespace System.Transactions;

internal sealed class TransactionStatePromotedPhase1 : TransactionStatePromotedCommitting
{
	internal override void EnterState(InternalTransaction tx)
	{
		CommonEnterState(tx);
		if (tx._committableTransaction != null)
		{
			tx._committableTransaction._complete = true;
		}
		if (tx._phase1Volatiles._dependentClones != 0)
		{
			tx.State.ChangeStateTransactionAborted(tx, null);
			return;
		}
		int volatileEnlistmentCount = tx._phase1Volatiles._volatileEnlistmentCount;
		if (tx._phase1Volatiles._preparedVolatileEnlistments < volatileEnlistmentCount)
		{
			for (int i = 0; i < volatileEnlistmentCount; i++)
			{
				tx._phase1Volatiles._volatileEnlistments[i]._twoPhaseState.ChangeStatePreparing(tx._phase1Volatiles._volatileEnlistments[i]);
				if (!tx.State.ContinuePhase1Prepares())
				{
					break;
				}
			}
		}
		else
		{
			Phase1VolatilePrepareDone(tx);
		}
	}

	internal override void CreateBlockingClone(InternalTransaction tx)
	{
		throw TransactionException.CreateTransactionStateException(tx._innerException, tx.DistributedTxId);
	}

	internal override void CreateAbortingClone(InternalTransaction tx)
	{
		throw TransactionException.CreateTransactionStateException(tx._innerException, tx.DistributedTxId);
	}

	internal override void ChangeStateTransactionAborted(InternalTransaction tx, Exception e)
	{
		if (tx._innerException == null)
		{
			tx._innerException = e;
		}
		TransactionState.TransactionStatePromotedP1Aborting.EnterState(tx);
	}

	internal override void Phase1VolatilePrepareDone(InternalTransaction tx)
	{
		Monitor.Exit(tx);
		try
		{
			tx._phase1Volatiles.VolatileDemux._promotedEnlistment.Prepared();
		}
		finally
		{
			Monitor.Enter(tx);
		}
	}

	internal override bool ContinuePhase1Prepares()
	{
		return true;
	}

	internal override Enlistment EnlistVolatile(InternalTransaction tx, IEnlistmentNotification enlistmentNotification, EnlistmentOptions enlistmentOptions, Transaction atomicTransaction)
	{
		throw TransactionException.Create(System.SR.TooLate, tx?.DistributedTxId ?? Guid.Empty);
	}

	internal override Enlistment EnlistVolatile(InternalTransaction tx, ISinglePhaseNotification enlistmentNotification, EnlistmentOptions enlistmentOptions, Transaction atomicTransaction)
	{
		throw TransactionException.Create(System.SR.TooLate, tx?.DistributedTxId ?? Guid.Empty);
	}

	internal override Enlistment EnlistDurable(InternalTransaction tx, Guid resourceManagerIdentifier, IEnlistmentNotification enlistmentNotification, EnlistmentOptions enlistmentOptions, Transaction atomicTransaction)
	{
		throw TransactionException.Create(System.SR.TooLate, tx?.DistributedTxId ?? Guid.Empty);
	}

	internal override Enlistment EnlistDurable(InternalTransaction tx, Guid resourceManagerIdentifier, ISinglePhaseNotification enlistmentNotification, EnlistmentOptions enlistmentOptions, Transaction atomicTransaction)
	{
		throw TransactionException.Create(System.SR.TooLate, tx?.DistributedTxId ?? Guid.Empty);
	}
}
