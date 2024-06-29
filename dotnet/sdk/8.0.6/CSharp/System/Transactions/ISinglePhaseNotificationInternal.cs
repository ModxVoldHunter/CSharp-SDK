namespace System.Transactions;

internal interface ISinglePhaseNotificationInternal : IEnlistmentNotificationInternal
{
	void SinglePhaseCommit(IPromotedEnlistment singlePhaseEnlistment);
}
