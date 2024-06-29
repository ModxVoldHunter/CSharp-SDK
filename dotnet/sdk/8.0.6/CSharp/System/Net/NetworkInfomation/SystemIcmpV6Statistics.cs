using System.Net.Sockets;

namespace System.Net.NetworkInformation;

internal sealed class SystemIcmpV6Statistics : IcmpV6Statistics
{
	private readonly global::Interop.IpHlpApi.MibIcmpInfoEx _stats;

	public override long MessagesSent => _stats.outStats.dwMsgs;

	public override long MessagesReceived => _stats.inStats.dwMsgs;

	public override long ErrorsSent => _stats.outStats.dwErrors;

	public override long ErrorsReceived => _stats.inStats.dwErrors;

	public unsafe override long DestinationUnreachableMessagesSent => _stats.outStats.rgdwTypeCount[1];

	public unsafe override long DestinationUnreachableMessagesReceived => _stats.inStats.rgdwTypeCount[1];

	public unsafe override long PacketTooBigMessagesSent => _stats.outStats.rgdwTypeCount[2];

	public unsafe override long PacketTooBigMessagesReceived => _stats.inStats.rgdwTypeCount[2];

	public unsafe override long TimeExceededMessagesSent => _stats.outStats.rgdwTypeCount[3];

	public unsafe override long TimeExceededMessagesReceived => _stats.inStats.rgdwTypeCount[3];

	public unsafe override long ParameterProblemsSent => _stats.outStats.rgdwTypeCount[4];

	public unsafe override long ParameterProblemsReceived => _stats.inStats.rgdwTypeCount[4];

	public unsafe override long EchoRequestsSent => _stats.outStats.rgdwTypeCount[128];

	public unsafe override long EchoRequestsReceived => _stats.inStats.rgdwTypeCount[128];

	public unsafe override long EchoRepliesSent => _stats.outStats.rgdwTypeCount[129];

	public unsafe override long EchoRepliesReceived => _stats.inStats.rgdwTypeCount[129];

	public unsafe override long MembershipQueriesSent => _stats.outStats.rgdwTypeCount[130];

	public unsafe override long MembershipQueriesReceived => _stats.inStats.rgdwTypeCount[130];

	public unsafe override long MembershipReportsSent => _stats.outStats.rgdwTypeCount[131];

	public unsafe override long MembershipReportsReceived => _stats.inStats.rgdwTypeCount[131];

	public unsafe override long MembershipReductionsSent => _stats.outStats.rgdwTypeCount[132];

	public unsafe override long MembershipReductionsReceived => _stats.inStats.rgdwTypeCount[132];

	public unsafe override long RouterAdvertisementsSent => _stats.outStats.rgdwTypeCount[134];

	public unsafe override long RouterAdvertisementsReceived => _stats.inStats.rgdwTypeCount[134];

	public unsafe override long RouterSolicitsSent => _stats.outStats.rgdwTypeCount[133];

	public unsafe override long RouterSolicitsReceived => _stats.inStats.rgdwTypeCount[133];

	public unsafe override long NeighborAdvertisementsSent => _stats.outStats.rgdwTypeCount[136];

	public unsafe override long NeighborAdvertisementsReceived => _stats.inStats.rgdwTypeCount[136];

	public unsafe override long NeighborSolicitsSent => _stats.outStats.rgdwTypeCount[135];

	public unsafe override long NeighborSolicitsReceived => _stats.inStats.rgdwTypeCount[135];

	public unsafe override long RedirectsSent => _stats.outStats.rgdwTypeCount[137];

	public unsafe override long RedirectsReceived => _stats.inStats.rgdwTypeCount[137];

	internal SystemIcmpV6Statistics()
	{
		uint icmpStatisticsEx = global::Interop.IpHlpApi.GetIcmpStatisticsEx(out _stats, AddressFamily.InterNetworkV6);
		if (icmpStatisticsEx != 0)
		{
			throw new NetworkInformationException((int)icmpStatisticsEx);
		}
	}
}
