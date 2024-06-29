using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Net.Sockets;
using System.Runtime.CompilerServices;

namespace System.Net;

public class IPEndPoint : EndPoint
{
	public const int MinPort = 0;

	public const int MaxPort = 65535;

	private IPAddress _address;

	private int _port;

	public override AddressFamily AddressFamily => _address.AddressFamily;

	public IPAddress Address
	{
		get
		{
			return _address;
		}
		set
		{
			ArgumentNullException.ThrowIfNull(value, "value");
			_address = value;
		}
	}

	public int Port
	{
		get
		{
			return _port;
		}
		set
		{
			if (!TcpValidationHelpers.ValidatePortNumber(value))
			{
				throw new ArgumentOutOfRangeException("value");
			}
			_port = value;
		}
	}

	public IPEndPoint(long address, int port)
	{
		if (!TcpValidationHelpers.ValidatePortNumber(port))
		{
			throw new ArgumentOutOfRangeException("port");
		}
		_port = port;
		_address = new IPAddress(address);
	}

	public IPEndPoint(IPAddress address, int port)
	{
		ArgumentNullException.ThrowIfNull(address, "address");
		if (!TcpValidationHelpers.ValidatePortNumber(port))
		{
			throw new ArgumentOutOfRangeException("port");
		}
		_port = port;
		_address = address;
	}

	public static bool TryParse(string s, [NotNullWhen(true)] out IPEndPoint? result)
	{
		return TryParse(s.AsSpan(), out result);
	}

	public static bool TryParse(ReadOnlySpan<char> s, [NotNullWhen(true)] out IPEndPoint? result)
	{
		int num = s.Length;
		int num2 = s.LastIndexOf(':');
		if (num2 > 0)
		{
			if (s[num2 - 1] == ']')
			{
				num = num2;
			}
			else if (s.Slice(0, num2).LastIndexOf(':') == -1)
			{
				num = num2;
			}
		}
		if (IPAddress.TryParse(s.Slice(0, num), out IPAddress address))
		{
			uint result2 = 0u;
			if (num == s.Length || (uint.TryParse(s.Slice(num + 1), NumberStyles.None, CultureInfo.InvariantCulture, out result2) && result2 <= 65535))
			{
				result = new IPEndPoint(address, (int)result2);
				return true;
			}
		}
		result = null;
		return false;
	}

	public static IPEndPoint Parse(string s)
	{
		ArgumentNullException.ThrowIfNull(s, "s");
		return Parse(s.AsSpan());
	}

	public static IPEndPoint Parse(ReadOnlySpan<char> s)
	{
		if (TryParse(s, out IPEndPoint result))
		{
			return result;
		}
		throw new FormatException(System.SR.bad_endpoint_string);
	}

	public override string ToString()
	{
		IFormatProvider invariantInfo;
		if (_address.AddressFamily != AddressFamily.InterNetworkV6)
		{
			invariantInfo = NumberFormatInfo.InvariantInfo;
			IFormatProvider provider = invariantInfo;
			DefaultInterpolatedStringHandler handler = new DefaultInterpolatedStringHandler(1, 2, invariantInfo);
			handler.AppendFormatted(_address);
			handler.AppendLiteral(":");
			handler.AppendFormatted(_port);
			return string.Create(provider, ref handler);
		}
		invariantInfo = NumberFormatInfo.InvariantInfo;
		IFormatProvider provider2 = invariantInfo;
		DefaultInterpolatedStringHandler handler2 = new DefaultInterpolatedStringHandler(3, 2, invariantInfo);
		handler2.AppendLiteral("[");
		handler2.AppendFormatted(_address);
		handler2.AppendLiteral("]:");
		handler2.AppendFormatted(_port);
		return string.Create(provider2, ref handler2);
	}

	public override SocketAddress Serialize()
	{
		return new SocketAddress(Address, Port);
	}

	public override EndPoint Create(SocketAddress socketAddress)
	{
		ArgumentNullException.ThrowIfNull(socketAddress, "socketAddress");
		AddressFamily family = socketAddress.Family;
		if ((family != AddressFamily.InterNetwork && family != AddressFamily.InterNetworkV6) || 1 == 0)
		{
			throw new ArgumentException(System.SR.Format(System.SR.net_InvalidAddressFamily, socketAddress.Family.ToString(), GetType().FullName), "socketAddress");
		}
		int num = ((AddressFamily == AddressFamily.InterNetworkV6) ? SocketAddress.IPv6AddressSize : SocketAddress.IPv4AddressSize);
		if (socketAddress.Size < num)
		{
			throw new ArgumentException(System.SR.Format(System.SR.net_InvalidSocketAddressSize, socketAddress.GetType().FullName, GetType().FullName), "socketAddress");
		}
		return socketAddress.GetIPEndPoint();
	}

	public override bool Equals([NotNullWhen(true)] object? comparand)
	{
		if (comparand is IPEndPoint iPEndPoint && iPEndPoint._address.Equals(_address))
		{
			return iPEndPoint._port == _port;
		}
		return false;
	}

	public override int GetHashCode()
	{
		return _address.GetHashCode() ^ _port;
	}
}
