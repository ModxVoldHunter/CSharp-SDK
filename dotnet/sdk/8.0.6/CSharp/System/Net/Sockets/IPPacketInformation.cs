using System.Diagnostics.CodeAnalysis;

namespace System.Net.Sockets;

public struct IPPacketInformation : IEquatable<IPPacketInformation>
{
	private readonly IPAddress _address;

	private readonly int _networkInterface;

	public IPAddress Address => _address;

	public int Interface => _networkInterface;

	internal IPPacketInformation(IPAddress address, int networkInterface)
	{
		_address = address;
		_networkInterface = networkInterface;
	}

	public static bool operator ==(IPPacketInformation packetInformation1, IPPacketInformation packetInformation2)
	{
		return packetInformation1.Equals(packetInformation2);
	}

	public static bool operator !=(IPPacketInformation packetInformation1, IPPacketInformation packetInformation2)
	{
		return !packetInformation1.Equals(packetInformation2);
	}

	public override bool Equals([NotNullWhen(true)] object? comparand)
	{
		if (comparand is IPPacketInformation other)
		{
			return Equals(other);
		}
		return false;
	}

	public bool Equals(IPPacketInformation other)
	{
		if (_networkInterface == other._networkInterface)
		{
			if (_address != null)
			{
				return _address.Equals(other._address);
			}
			return other._address == null;
		}
		return false;
	}

	public override int GetHashCode()
	{
		return _networkInterface.GetHashCode() * -1521134295 + (_address?.GetHashCode() ?? 0);
	}
}
