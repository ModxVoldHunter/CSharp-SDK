using System.Runtime.InteropServices.Marshalling;
using System.Transactions.DtcProxyShim.DtcInterfaces;

namespace System.Transactions.DtcProxyShim;

[GeneratedComClass]
[ComExposedClass<_003CSystem_Transactions_DtcProxyShim_ResourceManagerNotifyShim_003EF990DC11AC9744B4347D18EF731153511E65928FE9B96F33F5B44914674040C91__ComClassInformation>]
internal sealed class ResourceManagerNotifyShim : NotificationShimBase, IResourceManagerSink
{
	internal ResourceManagerNotifyShim(DtcProxyShimFactory shimFactory, object enlistmentIdentifier)
		: base(shimFactory, enlistmentIdentifier)
	{
	}

	public void TMDown()
	{
		NotificationType = ShimNotificationType.ResourceManagerTmDownNotify;
		ShimFactory.NewNotification(this);
	}
}
