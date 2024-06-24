using System.Threading;

namespace System.Transactions;

internal sealed class TransactionStatePromotedP1Aborting : TransactionStatePromotedAborting
{
	internal override void EnterState(InternalTransaction tx)
	{
		CommonEnterState(tx);
		ChangeStatePromotedAborted(tx);
		Monitor.Exit(tx);
		try
		{
			tx._phase1Volatiles.VolatileDemux._promotedEnlistment.ForceRollback();
		}
		finally
		{
			Monitor.Enter(tx);
		}
	}

	internal override void Phase1VolatilePrepareDone(InternalTransaction tx)
	{
	}
}
