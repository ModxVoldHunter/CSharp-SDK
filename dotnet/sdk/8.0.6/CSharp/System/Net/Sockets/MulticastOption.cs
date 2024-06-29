namespace System.Net.Sockets;

public class MulticastOption
{
	private IPAddress _group;

	private IPAddress _localAddress;

	private int _ifIndex;

	public IPAddress Group
	{
		get
		{
			return _group;
		}
		set
		{
			ArgumentNullException.ThrowIfNull(value, "value");
			_group = value;
		}
	}

	public IPAddress? LocalAddress
	{
		get
		{
			return _localAddress;
		}
		set
		{
			_ifIndex = 0;
			_localAddress = value;
		}
	}

	public int InterfaceIndex
	{
		get
		{
			return _ifIndex;
		}
		set
		{
			ArgumentOutOfRangeException.ThrowIfNegative(value, "value");
			ArgumentOutOfRangeException.ThrowIfGreaterThan(value, 16777215, "value");
			_localAddress = null;
			_ifIndex = value;
		}
	}

	public MulticastOption(IPAddress group, IPAddress mcint)
	{
		ArgumentNullException.ThrowIfNull(group, "group");
		ArgumentNullException.ThrowIfNull(mcint, "mcint");
		_group = group;
		LocalAddress = mcint;
	}

	public MulticastOption(IPAddress group, int interfaceIndex)
	{
		ArgumentNullException.ThrowIfNull(group, "group");
		ArgumentOutOfRangeException.ThrowIfNegative(interfaceIndex, "interfaceIndex");
		ArgumentOutOfRangeException.ThrowIfGreaterThan(interfaceIndex, 16777215, "interfaceIndex");
		_group = group;
		_ifIndex = interfaceIndex;
	}

	public MulticastOption(IPAddress group)
	{
		ArgumentNullException.ThrowIfNull(group, "group");
		_group = group;
		LocalAddress = IPAddress.Any;
	}
}
