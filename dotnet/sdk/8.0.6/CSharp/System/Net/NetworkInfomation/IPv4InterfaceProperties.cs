using System.Runtime.Versioning;

namespace System.Net.NetworkInformation;

public abstract class IPv4InterfaceProperties
{
	[SupportedOSPlatform("windows")]
	[SupportedOSPlatform("linux")]
	public abstract bool UsesWins { get; }

	[SupportedOSPlatform("windows")]
	public abstract bool IsDhcpEnabled { get; }

	[SupportedOSPlatform("windows")]
	public abstract bool IsAutomaticPrivateAddressingActive { get; }

	[SupportedOSPlatform("windows")]
	public abstract bool IsAutomaticPrivateAddressingEnabled { get; }

	public abstract int Index { get; }

	[SupportedOSPlatform("windows")]
	[SupportedOSPlatform("linux")]
	public abstract bool IsForwardingEnabled { get; }

	public abstract int Mtu { get; }
}
