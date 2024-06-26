using System.Transactions.DtcProxyShim.DtcInterfaces;

namespace System.Transactions.DtcProxyShim;

internal sealed class VoterBallotShim
{
	private readonly VoterNotifyShim _voterNotifyShim;

	internal ITransactionVoterBallotAsync2 VoterBallotAsync2 { get; set; }

	internal VoterBallotShim(VoterNotifyShim notifyShim)
	{
		_voterNotifyShim = notifyShim;
	}

	public void Vote(bool voteYes)
	{
		int hr = OletxHelper.S_OK;
		if (!voteYes)
		{
			hr = OletxHelper.E_FAIL;
		}
		VoterBallotAsync2.VoteRequestDone(hr, IntPtr.Zero);
	}
}
