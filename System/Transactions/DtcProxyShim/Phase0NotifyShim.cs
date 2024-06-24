using System.Runtime.InteropServices.Marshalling;
using System.Transactions.DtcProxyShim.DtcInterfaces;

namespace System.Transactions.DtcProxyShim;

[GeneratedComClass]
[ComExposedClass<_003CSystem_Transactions_DtcProxyShim_Phase0NotifyShim_003EFE0CC9B93C5D58ACE719E1ADA36E97AE5191DCD41F7D6059570542DE8DE9F09D4__ComClassInformation>]
internal sealed class Phase0NotifyShim : NotificationShimBase, ITransactionPhase0NotifyAsync
{
	internal Phase0NotifyShim(DtcProxyShimFactory shimFactory, object enlistmentIdentifier)
		: base(shimFactory, enlistmentIdentifier)
	{
	}

	public void Phase0Request(bool fAbortHint)
	{
		AbortingHint = fAbortHint;
		NotificationType = ShimNotificationType.Phase0RequestNotify;
		ShimFactory.NewNotification(this);
	}

	public void EnlistCompleted(int status)
	{
	}
}
