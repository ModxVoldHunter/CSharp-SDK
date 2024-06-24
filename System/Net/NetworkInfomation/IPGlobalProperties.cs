using System.Runtime.Versioning;
using System.Threading.Tasks;

namespace System.Net.NetworkInformation;

public abstract class IPGlobalProperties
{
	[UnsupportedOSPlatform("android")]
	public abstract string DhcpScopeName { get; }

	public abstract string DomainName { get; }

	public abstract string HostName { get; }

	[UnsupportedOSPlatform("android")]
	public abstract bool IsWinsProxy { get; }

	public abstract NetBiosNodeType NodeType { get; }

	[UnsupportedOSPlatform("illumos")]
	[UnsupportedOSPlatform("solaris")]
	public static IPGlobalProperties GetIPGlobalProperties()
	{
		return IPGlobalPropertiesPal.GetIPGlobalProperties();
	}

	[UnsupportedOSPlatform("android")]
	public abstract IPEndPoint[] GetActiveUdpListeners();

	[UnsupportedOSPlatform("android")]
	public abstract IPEndPoint[] GetActiveTcpListeners();

	[UnsupportedOSPlatform("android")]
	public abstract TcpConnectionInformation[] GetActiveTcpConnections();

	public virtual IAsyncResult BeginGetUnicastAddresses(AsyncCallback? callback, object? state)
	{
		throw System.NotImplemented.ByDesignWithMessage(System.SR.net_MethodNotImplementedException);
	}

	public virtual UnicastIPAddressInformationCollection EndGetUnicastAddresses(IAsyncResult asyncResult)
	{
		throw System.NotImplemented.ByDesignWithMessage(System.SR.net_MethodNotImplementedException);
	}

	[UnsupportedOSPlatform("android")]
	public abstract TcpStatistics GetTcpIPv4Statistics();

	[UnsupportedOSPlatform("android")]
	public abstract TcpStatistics GetTcpIPv6Statistics();

	[UnsupportedOSPlatform("android")]
	public abstract UdpStatistics GetUdpIPv4Statistics();

	[UnsupportedOSPlatform("android")]
	public abstract UdpStatistics GetUdpIPv6Statistics();

	[UnsupportedOSPlatform("android")]
	public abstract IcmpV4Statistics GetIcmpV4Statistics();

	[UnsupportedOSPlatform("android")]
	public abstract IcmpV6Statistics GetIcmpV6Statistics();

	public abstract IPGlobalStatistics GetIPv4GlobalStatistics();

	[UnsupportedOSPlatform("osx")]
	[UnsupportedOSPlatform("ios")]
	[UnsupportedOSPlatform("tvos")]
	[UnsupportedOSPlatform("freebsd")]
	public abstract IPGlobalStatistics GetIPv6GlobalStatistics();

	public virtual UnicastIPAddressInformationCollection GetUnicastAddresses()
	{
		throw System.NotImplemented.ByDesignWithMessage(System.SR.net_MethodNotImplementedException);
	}

	public virtual Task<UnicastIPAddressInformationCollection> GetUnicastAddressesAsync()
	{
		throw System.NotImplemented.ByDesignWithMessage(System.SR.net_MethodNotImplementedException);
	}
}
