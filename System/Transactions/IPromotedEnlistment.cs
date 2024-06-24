namespace System.Transactions;

internal interface IPromotedEnlistment
{
	InternalEnlistment InternalEnlistment { get; set; }

	void EnlistmentDone();

	void Prepared();

	void ForceRollback();

	void ForceRollback(Exception e);

	void Committed();

	void Aborted();

	void Aborted(Exception e);

	void InDoubt();

	void InDoubt(Exception e);

	byte[] GetRecoveryInformation();
}
