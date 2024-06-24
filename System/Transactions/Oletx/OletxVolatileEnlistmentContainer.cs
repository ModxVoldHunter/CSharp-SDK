using System.Collections;

namespace System.Transactions.Oletx;

internal abstract class OletxVolatileEnlistmentContainer
{
	protected RealOletxTransaction RealOletxTransaction;

	protected ArrayList EnlistmentList = new ArrayList();

	protected int Phase;

	protected int OutstandingNotifications;

	protected bool CollectedVoteYes;

	protected int IncompleteDependentClones;

	protected bool AlreadyVoted;

	protected OletxVolatileEnlistmentContainer(RealOletxTransaction realOletxTransaction)
	{
		RealOletxTransaction = realOletxTransaction;
	}

	internal abstract void DecrementOutstandingNotifications(bool voteYes);

	internal abstract void AddDependentClone();

	internal abstract void DependentCloneCompleted();

	internal abstract void RollbackFromTransaction();

	internal abstract void OutcomeFromTransaction(TransactionStatus outcome);

	internal abstract void Committed();

	internal abstract void Aborted();

	internal abstract void InDoubt();
}
