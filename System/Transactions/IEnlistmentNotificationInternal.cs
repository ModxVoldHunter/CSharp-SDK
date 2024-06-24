namespace System.Transactions;

internal interface IEnlistmentNotificationInternal
{
	void Prepare(IPromotedEnlistment preparingEnlistment);

	void Commit(IPromotedEnlistment enlistment);

	void Rollback(IPromotedEnlistment enlistment);

	void InDoubt(IPromotedEnlistment enlistment);
}
