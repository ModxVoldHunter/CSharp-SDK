using System.Net.Sockets;

namespace System.Net;

public class SocketAddress : IEquatable<SocketAddress>
{
	internal static readonly int IPv6AddressSize = 28;

	internal static readonly int IPv4AddressSize = 16;

	internal static readonly int UdsAddressSize = 110;

	internal static readonly int MaxAddressSize = 128;

	private int _size;

	private byte[] _buffer;

	public AddressFamily Family => SocketAddressPal.GetAddressFamily(_buffer);

	public int Size
	{
		get
		{
			return _size;
		}
		set
		{
			ArgumentOutOfRangeException.ThrowIfGreaterThan(value, _buffer.Length, "value");
			ArgumentOutOfRangeException.ThrowIfLessThan(value, 0, "value");
			_size = value;
		}
	}

	public byte this[int offset]
	{
		get
		{
			if ((uint)offset >= (uint)Size)
			{
				throw new IndexOutOfRangeException();
			}
			return _buffer[offset];
		}
		set
		{
			if ((uint)offset >= (uint)Size)
			{
				throw new IndexOutOfRangeException();
			}
			_buffer[offset] = value;
		}
	}

	public Memory<byte> Buffer => new Memory<byte>(_buffer);

	public static int GetMaximumAddressSize(AddressFamily addressFamily)
	{
		return addressFamily switch
		{
			AddressFamily.InterNetwork => IPv4AddressSize, 
			AddressFamily.InterNetworkV6 => IPv6AddressSize, 
			AddressFamily.Unix => UdsAddressSize, 
			_ => MaxAddressSize, 
		};
	}

	public SocketAddress(AddressFamily family)
		: this(family, GetMaximumAddressSize(family))
	{
	}

	public SocketAddress(AddressFamily family, int size)
	{
		ArgumentOutOfRangeException.ThrowIfLessThan(size, 2, "size");
		_size = size;
		_buffer = new byte[size];
		_buffer[0] = (byte)_size;
		SocketAddressPal.SetAddressFamily(_buffer, family);
	}

	internal SocketAddress(IPAddress ipAddress)
		: this(ipAddress.AddressFamily, (ipAddress.AddressFamily == AddressFamily.InterNetwork) ? IPv4AddressSize : IPv6AddressSize)
	{
		SocketAddressPal.SetPort(_buffer, 0);
		if (ipAddress.AddressFamily == AddressFamily.InterNetworkV6)
		{
			Span<byte> span = stackalloc byte[16];
			ipAddress.TryWriteBytes(span, out var _);
			SocketAddressPal.SetIPv6Address(_buffer, span, (uint)ipAddress.ScopeId);
		}
		else
		{
			uint address = (uint)ipAddress.Address;
			SocketAddressPal.SetIPv4Address(_buffer, address);
		}
	}

	internal SocketAddress(IPAddress ipaddress, int port)
		: this(ipaddress)
	{
		SocketAddressPal.SetPort(_buffer, (ushort)port);
	}

	public override bool Equals(object? comparand)
	{
		if (comparand is SocketAddress comparand2)
		{
			return Equals(comparand2);
		}
		return false;
	}

	public bool Equals(SocketAddress? comparand)
	{
		if (comparand != null)
		{
			Memory<byte> buffer = Buffer;
			Span<byte> span = buffer.Span;
			buffer = comparand.Buffer;
			return span.SequenceEqual(buffer.Span);
		}
		return false;
	}

	public override int GetHashCode()
	{
		HashCode hashCode = default(HashCode);
		hashCode.AddBytes(new ReadOnlySpan<byte>(_buffer, 0, _size));
		return hashCode.ToHashCode();
	}

	public override string ToString()
	{
		string text = Family.ToString();
		int num = text.Length + 1 + 10 + 2 + (Size - 2) * 4 + 1;
		Span<char> span = ((num > 256) ? ((Span<char>)new char[num]) : stackalloc char[256]);
		Span<char> destination = span;
		text.CopyTo(destination);
		int length = text.Length;
		destination[length++] = ':';
		bool flag = Size.TryFormat(destination.Slice(length), out var charsWritten);
		length += charsWritten;
		destination[length++] = ':';
		destination[length++] = '{';
		byte[] buffer = _buffer;
		for (int i = 2; i < Size; i++)
		{
			if (i > 2)
			{
				destination[length++] = ',';
			}
			flag = buffer[i].TryFormat(destination.Slice(length), out charsWritten);
			length += charsWritten;
		}
		destination[length++] = '}';
		return destination.Slice(0, length).ToString();
	}
}
