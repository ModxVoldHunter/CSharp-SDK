using System.Net.Sockets;
using System.Runtime.Versioning;

namespace System.Net.NetworkInformation;

public abstract class UnicastIPAddressInformation : IPAddressInformation
{
	[SupportedOSPlatform("windows")]
	public abstract long AddressPreferredLifetime { get; }

	[SupportedOSPlatform("windows")]
	public abstract long AddressValidLifetime { get; }

	[SupportedOSPlatform("windows")]
	public abstract long DhcpLeaseLifetime { get; }

	[SupportedOSPlatform("windows")]
	public abstract DuplicateAddressDetectionState DuplicateAddressDetectionState { get; }

	[SupportedOSPlatform("windows")]
	public abstract PrefixOrigin PrefixOrigin { get; }

	[SupportedOSPlatform("windows")]
	public abstract SuffixOrigin SuffixOrigin { get; }

	public abstract IPAddress IPv4Mask { get; }

	public virtual int PrefixLength
	{
		get
		{
			throw System.NotImplemented.ByDesignWithMessage(System.SR.net_PropertyNotImplementedException);
		}
	}

	internal static IPAddress PrefixLengthToSubnetMask(byte prefixLength, AddressFamily family)
	{
		Span<byte> span = ((family != AddressFamily.InterNetwork) ? stackalloc byte[16] : stackalloc byte[4]);
		Span<byte> span2 = span;
		span2.Clear();
		for (int i = 0; i < prefixLength; i++)
		{
			span2[i / 8] |= (byte)(128 >> i % 8);
		}
		return new IPAddress(span2);
	}
}
