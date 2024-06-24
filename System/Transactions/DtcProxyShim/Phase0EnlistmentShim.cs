using System.Runtime.InteropServices;
using System.Transactions.DtcProxyShim.DtcInterfaces;

namespace System.Transactions.DtcProxyShim;

internal sealed class Phase0EnlistmentShim
{
	private readonly Phase0NotifyShim _phase0NotifyShim;

	internal ITransactionPhase0EnlistmentAsync Phase0EnlistmentAsync { get; set; }

	internal Phase0EnlistmentShim(Phase0NotifyShim notifyShim)
	{
		_phase0NotifyShim = notifyShim;
	}

	public void Unenlist()
	{
		Phase0EnlistmentAsync?.Unenlist();
	}

	public void Phase0Done(bool voteYes)
	{
		if (voteYes)
		{
			try
			{
				Phase0EnlistmentAsync.Phase0Done();
			}
			catch (COMException ex) when (ex.ErrorCode == OletxHelper.XACT_E_PROTOCOL)
			{
			}
		}
	}
}
