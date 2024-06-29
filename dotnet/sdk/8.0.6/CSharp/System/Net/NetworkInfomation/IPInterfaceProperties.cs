using System.Runtime.Versioning;

namespace System.Net.NetworkInformation;

public abstract class IPInterfaceProperties
{
	[UnsupportedOSPlatform("android")]
	[UnsupportedOSPlatform("osx")]
	[UnsupportedOSPlatform("ios")]
	[UnsupportedOSPlatform("tvos")]
	[UnsupportedOSPlatform("freebsd")]
	public abstract bool IsDnsEnabled { get; }

	[UnsupportedOSPlatform("android")]
	public abstract string DnsSuffix { get; }

	[SupportedOSPlatform("windows")]
	public abstract bool IsDynamicDnsEnabled { get; }

	public abstract UnicastIPAddressInformationCollection UnicastAddresses { get; }

	public abstract MulticastIPAddressInformationCollection MulticastAddresses { get; }

	[SupportedOSPlatform("windows")]
	public abstract IPAddressInformationCollection AnycastAddresses { get; }

	[UnsupportedOSPlatform("android")]
	public abstract IPAddressCollection DnsAddresses { get; }

	[UnsupportedOSPlatform("android")]
	public abstract GatewayIPAddressInformationCollection GatewayAddresses { get; }

	[UnsupportedOSPlatform("android")]
	[UnsupportedOSPlatform("osx")]
	[UnsupportedOSPlatform("ios")]
	[UnsupportedOSPlatform("tvos")]
	[UnsupportedOSPlatform("freebsd")]
	public abstract IPAddressCollection DhcpServerAddresses { get; }

	[UnsupportedOSPlatform("android")]
	[UnsupportedOSPlatform("osx")]
	[UnsupportedOSPlatform("ios")]
	[UnsupportedOSPlatform("tvos")]
	[UnsupportedOSPlatform("freebsd")]
	public abstract IPAddressCollection WinsServersAddresses { get; }

	public abstract IPv4InterfaceProperties GetIPv4Properties();

	public abstract IPv6InterfaceProperties GetIPv6Properties();
}
