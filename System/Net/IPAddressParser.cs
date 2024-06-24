using System.Globalization;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System.Net;

internal static class IPAddressParser
{
	internal static IPAddress Parse(ReadOnlySpan<char> ipSpan, bool tryParse)
	{
		long address;
		if (ipSpan.Contains(':'))
		{
			Span<ushort> span = stackalloc ushort[8];
			span.Clear();
			if (TryParseIPv6(ipSpan, span, 8, out var scope))
			{
				return new IPAddress(span, scope);
			}
		}
		else if (TryParseIpv4(ipSpan, out address))
		{
			return new IPAddress(address);
		}
		if (tryParse)
		{
			return null;
		}
		throw new FormatException(System.SR.dns_bad_ip_address, new SocketException(SocketError.InvalidArgument));
	}

	private unsafe static bool TryParseIpv4(ReadOnlySpan<char> ipSpan, out long address)
	{
		int end = ipSpan.Length;
		long num;
		fixed (char* name = &MemoryMarshal.GetReference(ipSpan))
		{
			num = System.IPv4AddressHelper.ParseNonCanonical(name, 0, ref end, notImplicitFile: true);
		}
		if (num != -1 && end == ipSpan.Length)
		{
			address = (uint)IPAddress.HostToNetworkOrder((int)num);
			return true;
		}
		address = 0L;
		return false;
	}

	private unsafe static bool TryParseIPv6(ReadOnlySpan<char> ipSpan, Span<ushort> numbers, int numbersLength, out uint scope)
	{
		int end = ipSpan.Length;
		bool flag = false;
		fixed (char* name = &MemoryMarshal.GetReference(ipSpan))
		{
			flag = System.IPv6AddressHelper.IsValidStrict(name, 0, ref end);
		}
		if (flag || end != ipSpan.Length)
		{
			string scopeId = null;
			System.IPv6AddressHelper.Parse(ipSpan, numbers, 0, ref scopeId);
			if (scopeId != null && scopeId.Length > 1)
			{
				if (uint.TryParse(scopeId.AsSpan(1), NumberStyles.None, CultureInfo.InvariantCulture, out scope))
				{
					return true;
				}
				uint num = InterfaceInfoPal.InterfaceNameToIndex(scopeId);
				if (num != 0)
				{
					scope = num;
					return true;
				}
			}
			scope = 0u;
			return true;
		}
		scope = 0u;
		return false;
	}

	internal static int FormatIPv4Address<TChar>(uint address, Span<TChar> addressString) where TChar : unmanaged, IBinaryInteger<TChar>
	{
		address = (uint)IPAddress.NetworkToHostOrder((int)address);
		int num = FormatByte(address >> 24, addressString);
		addressString[num++] = TChar.CreateTruncating('.');
		num += FormatByte(address >> 16, addressString.Slice(num));
		addressString[num++] = TChar.CreateTruncating('.');
		num += FormatByte(address >> 8, addressString.Slice(num));
		addressString[num++] = TChar.CreateTruncating('.');
		return num + FormatByte(address, addressString.Slice(num));
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		static int FormatByte(uint number, Span<TChar> addressString)
		{
			number &= 0xFFu;
			switch (number)
			{
			default:
			{
				uint left;
				(left, number) = Math.DivRem(number, 10u);
				var (num3, num2) = Math.DivRem(left, 10u);
				addressString[2] = TChar.CreateTruncating(48 + number);
				addressString[1] = TChar.CreateTruncating(48 + num2);
				addressString[0] = TChar.CreateTruncating(48 + num3);
				return 3;
			}
			case 10u:
			case 11u:
			case 12u:
			case 13u:
			case 14u:
			case 15u:
			case 16u:
			case 17u:
			case 18u:
			case 19u:
			case 20u:
			case 21u:
			case 22u:
			case 23u:
			case 24u:
			case 25u:
			case 26u:
			case 27u:
			case 28u:
			case 29u:
			case 30u:
			case 31u:
			case 32u:
			case 33u:
			case 34u:
			case 35u:
			case 36u:
			case 37u:
			case 38u:
			case 39u:
			case 40u:
			case 41u:
			case 42u:
			case 43u:
			case 44u:
			case 45u:
			case 46u:
			case 47u:
			case 48u:
			case 49u:
			case 50u:
			case 51u:
			case 52u:
			case 53u:
			case 54u:
			case 55u:
			case 56u:
			case 57u:
			case 58u:
			case 59u:
			case 60u:
			case 61u:
			case 62u:
			case 63u:
			case 64u:
			case 65u:
			case 66u:
			case 67u:
			case 68u:
			case 69u:
			case 70u:
			case 71u:
			case 72u:
			case 73u:
			case 74u:
			case 75u:
			case 76u:
			case 77u:
			case 78u:
			case 79u:
			case 80u:
			case 81u:
			case 82u:
			case 83u:
			case 84u:
			case 85u:
			case 86u:
			case 87u:
			case 88u:
			case 89u:
			case 90u:
			case 91u:
			case 92u:
			case 93u:
			case 94u:
			case 95u:
			case 96u:
			case 97u:
			case 98u:
			case 99u:
			{
				uint num2;
				(num2, number) = Math.DivRem(number, 10u);
				addressString[1] = TChar.CreateTruncating(48 + number);
				addressString[0] = TChar.CreateTruncating(48 + num2);
				return 2;
			}
			case 0u:
			case 1u:
			case 2u:
			case 3u:
			case 4u:
			case 5u:
			case 6u:
			case 7u:
			case 8u:
			case 9u:
				addressString[0] = TChar.CreateTruncating(48 + number);
				return 1;
			}
		}
	}

	internal static int FormatIPv6Address<TChar>(ushort[] address, uint scopeId, Span<TChar> destination) where TChar : unmanaged, IBinaryInteger<TChar>
	{
		int offset2 = 0;
		if (System.IPv6AddressHelper.ShouldHaveIpv4Embedded(address))
		{
			AppendSections(address.AsSpan(0, 6), destination, ref offset2);
			if (destination[offset2 - 1] != TChar.CreateTruncating(':'))
			{
				destination[offset2++] = TChar.CreateTruncating(':');
			}
			offset2 += FormatIPv4Address(ExtractIPv4Address(address), destination.Slice(offset2));
		}
		else
		{
			AppendSections(address.AsSpan(0, 8), destination, ref offset2);
		}
		if (scopeId != 0)
		{
			destination[offset2++] = TChar.CreateTruncating('%');
			Span<TChar> span = stackalloc TChar[10];
			int num = 10;
			do
			{
				uint num2;
				(scopeId, num2) = Math.DivRem(scopeId, 10u);
				span[--num] = TChar.CreateTruncating(48 + num2);
			}
			while (scopeId != 0);
			Span<TChar> span2 = span.Slice(num);
			span2.CopyTo(destination.Slice(offset2));
			offset2 += span2.Length;
		}
		return offset2;
		static void AppendHex(ushort value, Span<TChar> destination, ref int offset)
		{
			if ((value & 0xFFF0u) != 0)
			{
				if ((value & 0xFF00u) != 0)
				{
					if ((value & 0xF000u) != 0)
					{
						destination[offset++] = TChar.CreateTruncating(System.HexConverter.ToCharLower(value >> 12));
					}
					destination[offset++] = TChar.CreateTruncating(System.HexConverter.ToCharLower(value >> 8));
				}
				destination[offset++] = TChar.CreateTruncating(System.HexConverter.ToCharLower(value >> 4));
			}
			destination[offset++] = TChar.CreateTruncating(System.HexConverter.ToCharLower(value));
		}
		static void AppendSections(ReadOnlySpan<ushort> address, Span<TChar> destination, ref int offset)
		{
			(int longestSequenceStart, int longestSequenceLength) tuple2 = System.IPv6AddressHelper.FindCompressionRange(address);
			int item = tuple2.longestSequenceStart;
			int item2 = tuple2.longestSequenceLength;
			bool flag = false;
			if (item >= 0)
			{
				for (int i = 0; i < item; i++)
				{
					if (flag)
					{
						destination[offset++] = TChar.CreateTruncating(':');
					}
					flag = true;
					AppendHex(address[i], destination, ref offset);
				}
				destination[offset++] = TChar.CreateTruncating(':');
				destination[offset++] = TChar.CreateTruncating(':');
				flag = false;
			}
			for (int j = item2; j < address.Length; j++)
			{
				if (flag)
				{
					destination[offset++] = TChar.CreateTruncating(':');
				}
				flag = true;
				AppendHex(address[j], destination, ref offset);
			}
		}
	}

	private static uint ExtractIPv4Address(ushort[] address)
	{
		uint host = (uint)((address[6] << 16) | address[7]);
		return (uint)IPAddress.HostToNetworkOrder((int)host);
	}
}
