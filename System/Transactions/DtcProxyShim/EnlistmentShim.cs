using System.Transactions.DtcProxyShim.DtcInterfaces;

namespace System.Transactions.DtcProxyShim;

internal sealed class EnlistmentShim
{
	private readonly EnlistmentNotifyShim _enlistmentNotifyShim;

	internal ITransactionEnlistmentAsync EnlistmentAsync { get; set; }

	internal EnlistmentShim(EnlistmentNotifyShim notifyShim)
	{
		_enlistmentNotifyShim = notifyShim;
	}

	public void PrepareRequestDone(OletxPrepareVoteType voteType)
	{
		int hr = OletxHelper.S_OK;
		bool flag = false;
		switch (voteType)
		{
		case OletxPrepareVoteType.ReadOnly:
			_enlistmentNotifyShim.SetIgnoreSpuriousProxyNotifications();
			hr = OletxHelper.XACT_S_READONLY;
			break;
		case OletxPrepareVoteType.SinglePhase:
			_enlistmentNotifyShim.SetIgnoreSpuriousProxyNotifications();
			hr = OletxHelper.XACT_S_SINGLEPHASE;
			break;
		case OletxPrepareVoteType.Prepared:
			hr = OletxHelper.S_OK;
			break;
		case OletxPrepareVoteType.Failed:
			_enlistmentNotifyShim.SetIgnoreSpuriousProxyNotifications();
			hr = OletxHelper.E_FAIL;
			break;
		case OletxPrepareVoteType.InDoubt:
			flag = true;
			break;
		default:
			hr = OletxHelper.E_FAIL;
			break;
		}
		if (!flag)
		{
			EnlistmentAsync.PrepareRequestDone(hr, IntPtr.Zero, IntPtr.Zero);
		}
	}

	public void CommitRequestDone()
	{
		EnlistmentAsync.CommitRequestDone(OletxHelper.S_OK);
	}

	public void AbortRequestDone()
	{
		EnlistmentAsync.AbortRequestDone(OletxHelper.S_OK);
	}
}
