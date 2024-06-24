using System.Runtime.InteropServices;

namespace System.Net.NetworkInformation;

internal sealed class SystemIPv4InterfaceProperties : IPv4InterfaceProperties
{
	private readonly bool _haveWins;

	private readonly bool _dhcpEnabled;

	private readonly bool _routingEnabled;

	private readonly uint _index;

	private readonly uint _mtu;

	private bool _autoConfigEnabled;

	private bool _autoConfigActive;

	public override bool UsesWins => _haveWins;

	public override bool IsDhcpEnabled => _dhcpEnabled;

	public override bool IsForwardingEnabled => _routingEnabled;

	public override bool IsAutomaticPrivateAddressingEnabled => _autoConfigEnabled;

	public override bool IsAutomaticPrivateAddressingActive => _autoConfigActive;

	public override int Mtu => (int)_mtu;

	public override int Index => (int)_index;

	internal SystemIPv4InterfaceProperties(in global::Interop.IpHlpApi.IpAdapterAddresses ipAdapterAddresses)
	{
		_index = ipAdapterAddresses.index;
		_routingEnabled = System.Net.NetworkInformation.HostInformationPal.GetEnableRouting();
		_dhcpEnabled = (ipAdapterAddresses.flags & global::Interop.IpHlpApi.AdapterFlags.DhcpEnabled) != 0;
		_haveWins = ipAdapterAddresses.firstWinsServerAddress != IntPtr.Zero;
		_mtu = ipAdapterAddresses.mtu;
		GetPerAdapterInfo(ipAdapterAddresses.index);
	}

	private unsafe void GetPerAdapterInfo(uint index)
	{
		if (index == 0)
		{
			return;
		}
		uint cb = 0u;
		uint perAdapterInfo = global::Interop.IpHlpApi.GetPerAdapterInfo(index, IntPtr.Zero, &cb);
		while (true)
		{
			switch (perAdapterInfo)
			{
			case 111u:
			{
				nint num = Marshal.AllocHGlobal((int)cb);
				try
				{
					perAdapterInfo = global::Interop.IpHlpApi.GetPerAdapterInfo(index, num, &cb);
					if (perAdapterInfo == 0)
					{
						global::Interop.IpHlpApi.IpPerAdapterInfo* ptr = (global::Interop.IpHlpApi.IpPerAdapterInfo*)num;
						_autoConfigEnabled = ptr->autoconfigEnabled != 0;
						_autoConfigActive = ptr->autoconfigActive != 0;
					}
				}
				finally
				{
					Marshal.FreeHGlobal(num);
				}
				break;
			}
			default:
				throw new NetworkInformationException((int)perAdapterInfo);
			case 0u:
				return;
			}
		}
	}
}
