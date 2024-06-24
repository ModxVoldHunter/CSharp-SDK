using System.Runtime.InteropServices;
using System.Transactions.DtcProxyShim.DtcInterfaces;
using System.Transactions.Oletx;

namespace System.Transactions.DtcProxyShim;

internal sealed class ResourceManagerShim
{
	private readonly DtcProxyShimFactory _shimFactory;

	public IResourceManager ResourceManager { get; set; }

	internal ResourceManagerShim(DtcProxyShimFactory shimFactory)
	{
		_shimFactory = shimFactory;
	}

	public void Enlist(TransactionShim transactionShim, OletxEnlistment managedIdentifier, out EnlistmentShim enlistmentShim)
	{
		EnlistmentNotifyShim enlistmentNotifyShim = new EnlistmentNotifyShim(_shimFactory, managedIdentifier);
		EnlistmentShim enlistmentShim2 = new EnlistmentShim(enlistmentNotifyShim);
		ITransaction transaction = transactionShim.Transaction;
		ResourceManager.Enlist(transaction, enlistmentNotifyShim, out var _, out var _, out var ppEnlist);
		enlistmentNotifyShim.EnlistmentAsync = ppEnlist;
		enlistmentShim2.EnlistmentAsync = ppEnlist;
		enlistmentShim = enlistmentShim2;
	}

	public void Reenlist(byte[] prepareInfo, out OletxTransactionOutcome outcome)
	{
		try
		{
			ResourceManager.Reenlist(prepareInfo, (uint)prepareInfo.Length, 5u, out var pXactStat);
			outcome = pXactStat switch
			{
				OletxXactStat.XACTSTAT_ABORTED => OletxTransactionOutcome.Aborted, 
				OletxXactStat.XACTSTAT_COMMITTED => OletxTransactionOutcome.Committed, 
				_ => OletxTransactionOutcome.Aborted, 
			};
		}
		catch (COMException ex) when (ex.ErrorCode == OletxHelper.XACT_E_REENLISTTIMEOUT)
		{
			outcome = OletxTransactionOutcome.NotKnownYet;
		}
	}

	public void ReenlistComplete()
	{
		ResourceManager.ReenlistmentComplete();
	}
}
