namespace System.Transactions;

internal enum EnlistmentCallback
{
	Done,
	Prepared,
	ForceRollback,
	Committed,
	Aborted,
	InDoubt
}
