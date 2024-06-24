using System.Transactions.DtcProxyShim.DtcInterfaces;
using System.Transactions.Oletx;

namespace System.Transactions.DtcProxyShim;

internal sealed class TransactionShim
{
	private readonly DtcProxyShimFactory _shimFactory;

	private readonly TransactionNotifyShim _transactionNotifyShim;

	internal ITransaction Transaction { get; set; }

	internal TransactionShim(DtcProxyShimFactory shimFactory, TransactionNotifyShim notifyShim, ITransaction transaction)
	{
		_shimFactory = shimFactory;
		_transactionNotifyShim = notifyShim;
		Transaction = transaction;
	}

	public void Commit()
	{
		Transaction.Commit(fRetaining: false, OletxXacttc.XACTTC_ASYNC_PHASEONE, 0u);
	}

	public void Abort()
	{
		Transaction.Abort(IntPtr.Zero, retaining: false, async: false);
	}

	public void CreateVoter(OletxPhase1VolatileEnlistmentContainer managedIdentifier, out VoterBallotShim voterBallotShim)
	{
		VoterNotifyShim voterNotifyShim = new VoterNotifyShim(_shimFactory, managedIdentifier);
		VoterBallotShim voterBallotShim2 = new VoterBallotShim(voterNotifyShim);
		_shimFactory.VoterFactory.Create(Transaction, voterNotifyShim, out var ppVoterBallot);
		voterBallotShim2.VoterBallotAsync2 = ppVoterBallot;
		voterBallotShim = voterBallotShim2;
	}

	public void Export(byte[] whereabouts, out byte[] cookieBuffer)
	{
		_shimFactory.ExportFactory.Create((uint)whereabouts.Length, whereabouts, out var export);
		uint cookieSizeULong = 0u;
		OletxHelper.Retry(delegate
		{
			export.Export(Transaction, out cookieSizeULong);
		});
		uint cookieSize = cookieSizeULong;
		byte[] buffer = new byte[cookieSize];
		uint bytesUsed = 0u;
		OletxHelper.Retry(delegate
		{
			export.GetTransactionCookie(Transaction, cookieSize, buffer, out bytesUsed);
		});
		cookieBuffer = buffer;
	}

	public void GetITransactionNative(out ITransaction transactionNative)
	{
		ITransactionCloner transactionCloner = (ITransactionCloner)Transaction;
		transactionCloner.CloneWithCommitDisabled(out var ppITransaction);
		transactionNative = ppITransaction;
	}

	public byte[] GetPropagationToken()
	{
		ITransactionTransmitter cachedTransmitter = _shimFactory.GetCachedTransmitter(Transaction);
		try
		{
			cachedTransmitter.GetPropagationTokenSize(out var pcbToken);
			int num = (int)pcbToken;
			byte[] array = new byte[num];
			cachedTransmitter.MarshalPropagationToken((uint)num, array, out var _);
			return array;
		}
		finally
		{
			_shimFactory.ReturnCachedTransmitter(cachedTransmitter);
		}
	}

	public void Phase0Enlist(object managedIdentifier, out Phase0EnlistmentShim phase0EnlistmentShim)
	{
		ITransactionPhase0Factory transactionPhase0Factory = (ITransactionPhase0Factory)Transaction;
		Phase0NotifyShim phase0NotifyShim = new Phase0NotifyShim(_shimFactory, managedIdentifier);
		Phase0EnlistmentShim phase0EnlistmentShim2 = new Phase0EnlistmentShim(phase0NotifyShim);
		transactionPhase0Factory.Create(phase0NotifyShim, out var ppITransactionPhase0Enlistment);
		phase0EnlistmentShim2.Phase0EnlistmentAsync = ppITransactionPhase0Enlistment;
		ppITransactionPhase0Enlistment.Enable();
		ppITransactionPhase0Enlistment.WaitForEnlistment();
		phase0EnlistmentShim = phase0EnlistmentShim2;
	}
}
