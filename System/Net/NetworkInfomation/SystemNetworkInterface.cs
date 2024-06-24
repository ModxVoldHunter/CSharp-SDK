using System.Collections.Generic;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System.Net.NetworkInformation;

internal sealed class SystemNetworkInterface : NetworkInterface
{
	private readonly string _name;

	private readonly string _id;

	private readonly string _description;

	private readonly byte[] _physicalAddress;

	private readonly NetworkInterfaceType _type;

	private readonly OperationalStatus _operStatus;

	private readonly long _speed;

	private readonly uint _index;

	private readonly uint _ipv6Index;

	private readonly global::Interop.IpHlpApi.AdapterFlags _adapterFlags;

	private readonly SystemIPInterfaceProperties _interfaceProperties;

	internal static int InternalLoopbackInterfaceIndex => GetBestInterfaceForAddress(IPAddress.Loopback);

	internal static int InternalIPv6LoopbackInterfaceIndex => GetBestInterfaceForAddress(IPAddress.IPv6Loopback);

	public override string Id => _id;

	public override string Name => _name;

	public override string Description => _description;

	public override NetworkInterfaceType NetworkInterfaceType => _type;

	public override OperationalStatus OperationalStatus => _operStatus;

	public override long Speed => _speed;

	public override bool IsReceiveOnly => (_adapterFlags & global::Interop.IpHlpApi.AdapterFlags.ReceiveOnly) > (global::Interop.IpHlpApi.AdapterFlags)0;

	public override bool SupportsMulticast => (_adapterFlags & global::Interop.IpHlpApi.AdapterFlags.NoMulticast) == 0;

	private unsafe static int GetBestInterfaceForAddress(IPAddress addr)
	{
		Span<byte> span = stackalloc byte[28];
		System.Net.Sockets.IPEndPointExtensions.SetIPAddress(span, addr);
		Unsafe.SkipInit(out int result);
		int bestInterfaceEx = (int)global::Interop.IpHlpApi.GetBestInterfaceEx(span, &result);
		if (bestInterfaceEx != 0)
		{
			throw new NetworkInformationException(bestInterfaceEx);
		}
		return result;
	}

	internal static bool InternalGetIsNetworkAvailable()
	{
		try
		{
			NetworkInterface[] networkInterfaces = GetNetworkInterfaces();
			NetworkInterface[] array = networkInterfaces;
			foreach (NetworkInterface networkInterface in array)
			{
				if (networkInterface.OperationalStatus == OperationalStatus.Up && networkInterface.NetworkInterfaceType != NetworkInterfaceType.Tunnel && networkInterface.NetworkInterfaceType != NetworkInterfaceType.Loopback)
				{
					return true;
				}
			}
		}
		catch (NetworkInformationException exception)
		{
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				System.Net.NetEventSource.Error(exception, "InternalGetIsNetworkAvailable");
			}
		}
		return false;
	}

	internal unsafe static NetworkInterface[] GetNetworkInterfaces()
	{
		AddressFamily family = AddressFamily.Unspecified;
		uint cb = 0u;
		List<SystemNetworkInterface> list = new List<SystemNetworkInterface>();
		global::Interop.IpHlpApi.GetAdaptersAddressesFlags flags = global::Interop.IpHlpApi.GetAdaptersAddressesFlags.IncludeWins | global::Interop.IpHlpApi.GetAdaptersAddressesFlags.IncludeGateways;
		uint adaptersAddresses = global::Interop.IpHlpApi.GetAdaptersAddresses(family, (uint)flags, IntPtr.Zero, IntPtr.Zero, &cb);
		while (true)
		{
			switch (adaptersAddresses)
			{
			case 111u:
			{
				nint num = Marshal.AllocHGlobal((int)cb);
				try
				{
					adaptersAddresses = global::Interop.IpHlpApi.GetAdaptersAddresses(family, (uint)flags, IntPtr.Zero, num, &cb);
					if (adaptersAddresses == 0)
					{
						for (global::Interop.IpHlpApi.IpAdapterAddresses* ptr = (global::Interop.IpHlpApi.IpAdapterAddresses*)num; ptr != null; ptr = ptr->next)
						{
							list.Add(new SystemNetworkInterface(in *ptr));
						}
					}
				}
				finally
				{
					Marshal.FreeHGlobal(num);
				}
				break;
			}
			case 87u:
			case 232u:
				return Array.Empty<SystemNetworkInterface>();
			default:
				throw new NetworkInformationException((int)adaptersAddresses);
			case 0u:
				return list.ToArray();
			}
		}
	}

	internal SystemNetworkInterface(in global::Interop.IpHlpApi.IpAdapterAddresses ipAdapterAddresses)
	{
		_id = ipAdapterAddresses.AdapterName;
		_name = ipAdapterAddresses.FriendlyName;
		_description = ipAdapterAddresses.Description;
		_index = ipAdapterAddresses.index;
		_physicalAddress = ipAdapterAddresses.Address;
		_type = ipAdapterAddresses.type;
		_operStatus = ipAdapterAddresses.operStatus;
		_speed = (long)ipAdapterAddresses.receiveLinkSpeed;
		_ipv6Index = ipAdapterAddresses.ipv6Index;
		_adapterFlags = ipAdapterAddresses.flags;
		_interfaceProperties = new SystemIPInterfaceProperties(in ipAdapterAddresses);
	}

	public override PhysicalAddress GetPhysicalAddress()
	{
		return new PhysicalAddress(_physicalAddress);
	}

	public override IPInterfaceProperties GetIPProperties()
	{
		return _interfaceProperties;
	}

	public override IPv4InterfaceStatistics GetIPv4Statistics()
	{
		return new SystemIPv4InterfaceStatistics(_index);
	}

	public override IPInterfaceStatistics GetIPStatistics()
	{
		return new SystemIPInterfaceStatistics(_index);
	}

	public override bool Supports(NetworkInterfaceComponent networkInterfaceComponent)
	{
		if (networkInterfaceComponent == NetworkInterfaceComponent.IPv6 && (_adapterFlags & global::Interop.IpHlpApi.AdapterFlags.IPv6Enabled) != 0)
		{
			return true;
		}
		if (networkInterfaceComponent == NetworkInterfaceComponent.IPv4 && (_adapterFlags & global::Interop.IpHlpApi.AdapterFlags.IPv4Enabled) != 0)
		{
			return true;
		}
		return false;
	}
}
