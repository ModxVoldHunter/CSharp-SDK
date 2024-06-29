using System.Runtime.Versioning;

namespace System.Net.NetworkInformation;

public abstract class IPv6InterfaceProperties
{
	public abstract int Index { get; }

	public abstract int Mtu { get; }

	[UnsupportedOSPlatform("osx")]
	[UnsupportedOSPlatform("ios")]
	[UnsupportedOSPlatform("tvos")]
	[UnsupportedOSPlatform("freebsd")]
	public virtual long GetScopeId(ScopeLevel scopeLevel)
	{
		throw System.NotImplemented.ByDesignWithMessage(System.SR.net_MethodNotImplementedException);
	}
}
