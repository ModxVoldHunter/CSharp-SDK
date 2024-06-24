using System.Runtime.InteropServices.Marshalling;
using System.Threading;
using System.Transactions.DtcProxyShim.DtcInterfaces;
using System.Transactions.Oletx;

namespace System.Transactions.DtcProxyShim;

[GeneratedComClass]
[ComExposedClass<_003CSystem_Transactions_DtcProxyShim_EnlistmentNotifyShim_003EF58AD71DA60274E1D32B200CDD7E2C0578A1C364166AE7580E62D502C7CAE20CC__ComClassInformation>]
internal sealed class EnlistmentNotifyShim : NotificationShimBase, ITransactionResourceAsync
{
	internal ITransactionEnlistmentAsync EnlistmentAsync;

	private bool _ignoreSpuriousProxyNotifications;

	internal EnlistmentNotifyShim(DtcProxyShimFactory shimFactory, OletxEnlistment enlistmentIdentifier)
		: base(shimFactory, enlistmentIdentifier)
	{
		_ignoreSpuriousProxyNotifications = false;
	}

	internal void SetIgnoreSpuriousProxyNotifications()
	{
		_ignoreSpuriousProxyNotifications = true;
	}

	public void PrepareRequest(bool fRetaining, OletxXactRm grfRM, bool fWantMoniker, bool fSinglePhase)
	{
		ITransactionEnlistmentAsync transactionEnlistmentAsync = Interlocked.Exchange(ref EnlistmentAsync, null);
		if (transactionEnlistmentAsync == null)
		{
			throw new InvalidOperationException("Unexpected null in pEnlistmentAsync");
		}
		IPrepareInfo prepareInfo = (IPrepareInfo)transactionEnlistmentAsync;
		prepareInfo.GetPrepareInfoSize(out var pcbPrepInfo);
		byte[] array = new byte[pcbPrepInfo];
		prepareInfo.GetPrepareInfo(array);
		PrepareInfo = array;
		IsSinglePhase = fSinglePhase;
		NotificationType = ShimNotificationType.PrepareRequestNotify;
		ShimFactory.NewNotification(this);
	}

	public void CommitRequest(OletxXactRm grfRM, nint pNewUOW)
	{
		NotificationType = ShimNotificationType.CommitRequestNotify;
		ShimFactory.NewNotification(this);
	}

	public void AbortRequest(nint pboidReason, bool fRetaining, nint pNewUOW)
	{
		if (!_ignoreSpuriousProxyNotifications)
		{
			NotificationType = ShimNotificationType.AbortRequestNotify;
			ShimFactory.NewNotification(this);
		}
	}

	public void TMDown()
	{
		NotificationType = ShimNotificationType.ResourceManagerTmDownNotify;
		ShimFactory.NewNotification(this);
	}
}
