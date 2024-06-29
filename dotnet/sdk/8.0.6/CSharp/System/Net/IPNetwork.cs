using System.Buffers.Binary;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.Unicode;

namespace System.Net;

public readonly struct IPNetwork : IEquatable<IPNetwork>, ISpanFormattable, IFormattable, ISpanParsable<IPNetwork>, IParsable<IPNetwork>, IUtf8SpanFormattable
{
	private readonly IPAddress _baseAddress;

	public IPAddress BaseAddress => _baseAddress ?? IPAddress.Any;

	public int PrefixLength { get; }

	public IPNetwork(IPAddress baseAddress, int prefixLength)
	{
		ArgumentNullException.ThrowIfNull(baseAddress, "baseAddress");
		if (prefixLength < 0 || prefixLength > GetMaxPrefixLength(baseAddress))
		{
			ThrowArgumentOutOfRangeException();
		}
		if (HasNonZeroBitsAfterNetworkPrefix(baseAddress, prefixLength))
		{
			ThrowInvalidBaseAddressException();
		}
		_baseAddress = baseAddress;
		PrefixLength = prefixLength;
		[DoesNotReturn]
		static void ThrowArgumentOutOfRangeException()
		{
			throw new ArgumentOutOfRangeException("prefixLength");
		}
		[DoesNotReturn]
		static void ThrowInvalidBaseAddressException()
		{
			throw new ArgumentException(System.SR.net_bad_ip_network_invalid_baseaddress, "baseAddress");
		}
	}

	private IPNetwork(IPAddress baseAddress, int prefixLength, bool _)
	{
		_baseAddress = baseAddress;
		PrefixLength = prefixLength;
	}

	public bool Contains(IPAddress address)
	{
		ArgumentNullException.ThrowIfNull(address, "address");
		if (address.AddressFamily != BaseAddress.AddressFamily)
		{
			return false;
		}
		if (PrefixLength == 0)
		{
			return true;
		}
		if (address.AddressFamily == AddressFamily.InterNetwork)
		{
			uint num = (uint)(-1 << 32 - PrefixLength);
			if (BitConverter.IsLittleEndian)
			{
				num = BinaryPrimitives.ReverseEndianness(num);
			}
			return BaseAddress.PrivateAddress == (address.PrivateAddress & num);
		}
		UInt128 reference = default(UInt128);
		UInt128 reference2 = default(UInt128);
		BaseAddress.TryWriteBytes(MemoryMarshal.AsBytes(new Span<UInt128>(ref reference)), out var bytesWritten);
		address.TryWriteBytes(MemoryMarshal.AsBytes(new Span<UInt128>(ref reference2)), out bytesWritten);
		UInt128 uInt = UInt128.MaxValue << 128 - PrefixLength;
		if (BitConverter.IsLittleEndian)
		{
			uInt = BinaryPrimitives.ReverseEndianness(uInt);
		}
		return reference == (reference2 & uInt);
	}

	public static IPNetwork Parse(string s)
	{
		ArgumentNullException.ThrowIfNull(s, "s");
		return Parse(s.AsSpan());
	}

	public static IPNetwork Parse(ReadOnlySpan<char> s)
	{
		if (!TryParse(s, out var result))
		{
			throw new FormatException(System.SR.net_bad_ip_network);
		}
		return result;
	}

	public static bool TryParse(string? s, out IPNetwork result)
	{
		if (s == null)
		{
			result = default(IPNetwork);
			return false;
		}
		return TryParse(s.AsSpan(), out result);
	}

	public static bool TryParse(ReadOnlySpan<char> s, out IPNetwork result)
	{
		int num = s.LastIndexOf('/');
		if (num >= 0)
		{
			ReadOnlySpan<char> ipSpan = s.Slice(0, num);
			ReadOnlySpan<char> s2 = s.Slice(num + 1);
			if (IPAddress.TryParse(ipSpan, out IPAddress address) && int.TryParse(s2, NumberStyles.None, CultureInfo.InvariantCulture, out var result2) && result2 <= GetMaxPrefixLength(address) && !HasNonZeroBitsAfterNetworkPrefix(address, result2))
			{
				result = new IPNetwork(address, result2, _: false);
				return true;
			}
		}
		result = default(IPNetwork);
		return false;
	}

	private static int GetMaxPrefixLength(IPAddress baseAddress)
	{
		if (baseAddress.AddressFamily != AddressFamily.InterNetwork)
		{
			return 128;
		}
		return 32;
	}

	private static bool HasNonZeroBitsAfterNetworkPrefix(IPAddress baseAddress, int prefixLength)
	{
		if (baseAddress.AddressFamily == AddressFamily.InterNetwork)
		{
			uint num = (uint)(4294967295uL << 32 - prefixLength);
			if (BitConverter.IsLittleEndian)
			{
				num = BinaryPrimitives.ReverseEndianness(num);
			}
			return (baseAddress.PrivateAddress & num) != baseAddress.PrivateAddress;
		}
		UInt128 reference = default(UInt128);
		baseAddress.TryWriteBytes(MemoryMarshal.AsBytes(new Span<UInt128>(ref reference)), out var _);
		if (prefixLength == 0)
		{
			return reference != UInt128.Zero;
		}
		UInt128 uInt = UInt128.MaxValue << 128 - prefixLength;
		if (BitConverter.IsLittleEndian)
		{
			uInt = BinaryPrimitives.ReverseEndianness(uInt);
		}
		return (reference & uInt) != reference;
	}

	public override string ToString()
	{
		IFormatProvider invariantCulture = CultureInfo.InvariantCulture;
		IFormatProvider provider = invariantCulture;
		Span<char> initialBuffer = stackalloc char[128];
		DefaultInterpolatedStringHandler handler = new DefaultInterpolatedStringHandler(1, 2, invariantCulture, initialBuffer);
		handler.AppendFormatted(BaseAddress);
		handler.AppendLiteral("/");
		handler.AppendFormatted((uint)PrefixLength);
		return string.Create(provider, initialBuffer, ref handler);
	}

	public bool TryFormat(Span<char> destination, out int charsWritten)
	{
		IFormatProvider invariantCulture = CultureInfo.InvariantCulture;
		bool shouldAppend;
		MemoryExtensions.TryWriteInterpolatedStringHandler handler = new MemoryExtensions.TryWriteInterpolatedStringHandler(1, 2, destination, invariantCulture, out shouldAppend);
		if (shouldAppend && handler.AppendFormatted(BaseAddress) && handler.AppendLiteral("/"))
		{
			handler.AppendFormatted((uint)PrefixLength);
		}
		else
			_ = 0;
		return destination.TryWrite(invariantCulture, ref handler, out charsWritten);
	}

	public bool TryFormat(Span<byte> utf8Destination, out int bytesWritten)
	{
		IFormatProvider invariantCulture = CultureInfo.InvariantCulture;
		bool shouldAppend;
		Utf8.TryWriteInterpolatedStringHandler handler = new Utf8.TryWriteInterpolatedStringHandler(1, 2, utf8Destination, invariantCulture, out shouldAppend);
		if (shouldAppend && handler.AppendFormatted(BaseAddress) && handler.AppendLiteral("/"))
		{
			handler.AppendFormatted((uint)PrefixLength);
		}
		else
			_ = 0;
		return Utf8.TryWrite(utf8Destination, invariantCulture, ref handler, out bytesWritten);
	}

	public bool Equals(IPNetwork other)
	{
		if (PrefixLength == other.PrefixLength)
		{
			return BaseAddress.Equals(other.BaseAddress);
		}
		return false;
	}

	public override bool Equals([NotNullWhen(true)] object? obj)
	{
		if (obj is IPNetwork other)
		{
			return Equals(other);
		}
		return false;
	}

	public static bool operator ==(IPNetwork left, IPNetwork right)
	{
		return left.Equals(right);
	}

	public static bool operator !=(IPNetwork left, IPNetwork right)
	{
		return !(left == right);
	}

	public override int GetHashCode()
	{
		return HashCode.Combine(BaseAddress, PrefixLength);
	}

	string IFormattable.ToString(string format, IFormatProvider provider)
	{
		return ToString();
	}

	bool ISpanFormattable.TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider provider)
	{
		return TryFormat(destination, out charsWritten);
	}

	bool IUtf8SpanFormattable.TryFormat(Span<byte> utf8Destination, out int bytesWritten, ReadOnlySpan<char> format, IFormatProvider provider)
	{
		return TryFormat(utf8Destination, out bytesWritten);
	}

	static IPNetwork IParsable<IPNetwork>.Parse([NotNull] string s, IFormatProvider provider)
	{
		return Parse(s);
	}

	static bool IParsable<IPNetwork>.TryParse([NotNullWhen(true)] string s, IFormatProvider provider, out IPNetwork result)
	{
		return TryParse(s, out result);
	}

	static IPNetwork ISpanParsable<IPNetwork>.Parse(ReadOnlySpan<char> s, IFormatProvider provider)
	{
		return Parse(s);
	}

	static bool ISpanParsable<IPNetwork>.TryParse(ReadOnlySpan<char> s, IFormatProvider provider, out IPNetwork result)
	{
		return TryParse(s, out result);
	}
}
