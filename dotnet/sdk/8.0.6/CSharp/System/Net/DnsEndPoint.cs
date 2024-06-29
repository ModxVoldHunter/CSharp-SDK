using System.Diagnostics.CodeAnalysis;
using System.Net.Sockets;

namespace System.Net;

public class DnsEndPoint : EndPoint
{
	private readonly string _host;

	private readonly int _port;

	private readonly AddressFamily _family;

	public string Host => _host;

	public override AddressFamily AddressFamily => _family;

	public int Port => _port;

	public DnsEndPoint(string host, int port)
		: this(host, port, AddressFamily.Unspecified)
	{
	}

	public DnsEndPoint(string host, int port, AddressFamily addressFamily)
	{
		ArgumentException.ThrowIfNullOrEmpty(host, "host");
		ArgumentOutOfRangeException.ThrowIfLessThan(port, 0, "port");
		ArgumentOutOfRangeException.ThrowIfGreaterThan(port, 65535, "port");
		if (addressFamily != AddressFamily.InterNetwork && addressFamily != AddressFamily.InterNetworkV6 && addressFamily != 0)
		{
			throw new ArgumentException(System.SR.net_sockets_invalid_optionValue_all, "addressFamily");
		}
		_host = host;
		_port = port;
		_family = addressFamily;
	}

	public override bool Equals([NotNullWhen(true)] object? comparand)
	{
		if (comparand is DnsEndPoint dnsEndPoint && _family == dnsEndPoint._family && _port == dnsEndPoint._port)
		{
			return StringComparer.OrdinalIgnoreCase.Equals(_host, dnsEndPoint._host);
		}
		return false;
	}

	public override int GetHashCode()
	{
		return HashCode.Combine((int)_family, _port, StringComparer.OrdinalIgnoreCase.GetHashCode(_host));
	}

	public override string ToString()
	{
		return $"{_family}/{_host}:{_port}";
	}
}
