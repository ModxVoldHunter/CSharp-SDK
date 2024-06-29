using System.Threading;

namespace System.Transactions;

internal sealed class TransactionStatePromotedPhase0 : TransactionStatePromotedCommitting
{
	internal override void EnterState(InternalTransaction tx)
	{
		CommonEnterState(tx);
		int volatileEnlistmentCount = tx._phase0Volatiles._volatileEnlistmentCount;
		int dependentClones = tx._phase0Volatiles._dependentClones;
		tx._phase0VolatileWaveCount = volatileEnlistmentCount;
		if (tx._phase0Volatiles._preparedVolatileEnlistments < volatileEnlistmentCount + dependentClones)
		{
			for (int i = 0; i < volatileEnlistmentCount; i++)
			{
				tx._phase0Volatiles._volatileEnlistments[i]._twoPhaseState.ChangeStatePreparing(tx._phase0Volatiles._volatileEnlistments[i]);
				if (!tx.State.ContinuePhase0Prepares())
				{
					break;
				}
			}
		}
		else
		{
			Phase0VolatilePrepareDone(tx);
		}
	}

	internal override void Phase0VolatilePrepareDone(InternalTransaction tx)
	{
		Monitor.Exit(tx);
		try
		{
			tx._phase0Volatiles.VolatileDemux._promotedEnlistment.Prepared();
		}
		finally
		{
			Monitor.Enter(tx);
		}
	}

	internal override bool ContinuePhase0Prepares()
	{
		return true;
	}

	internal override void ChangeStateTransactionAborted(InternalTransaction tx, Exception e)
	{
		if (tx._innerException == null)
		{
			tx._innerException = e;
		}
		TransactionState.TransactionStatePromotedP0Aborting.EnterState(tx);
	}
}
