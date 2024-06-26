using System.Runtime.CompilerServices;

namespace System.Net.Http.HPack;

internal static class Huffman
{
	private static readonly ushort[] s_decodingTree = GenerateDecodingLookupTree();

	private static ReadOnlySpan<uint> EncodingTableCodes => RuntimeHelpers.CreateSpan<uint>((RuntimeFieldHandle)/*OpCode not supported: LdMemberToken*/);

	private static ReadOnlySpan<byte> EncodingTableBitLengths => new byte[257]
	{
		13, 23, 28, 28, 28, 28, 28, 28, 28, 24,
		30, 28, 28, 30, 28, 28, 28, 28, 28, 28,
		28, 28, 30, 28, 28, 28, 28, 28, 28, 28,
		28, 28, 6, 10, 10, 12, 13, 6, 8, 11,
		10, 10, 8, 11, 8, 6, 6, 6, 5, 5,
		5, 6, 6, 6, 6, 6, 6, 6, 7, 8,
		15, 6, 12, 10, 13, 6, 7, 7, 7, 7,
		7, 7, 7, 7, 7, 7, 7, 7, 7, 7,
		7, 7, 7, 7, 7, 7, 7, 7, 8, 7,
		8, 13, 19, 13, 14, 6, 15, 5, 6, 5,
		6, 5, 6, 6, 6, 5, 7, 7, 6, 6,
		6, 5, 6, 7, 6, 5, 5, 6, 7, 7,
		7, 7, 7, 15, 11, 14, 13, 28, 20, 22,
		20, 20, 22, 22, 22, 23, 22, 23, 23, 23,
		23, 23, 24, 23, 24, 24, 22, 23, 24, 23,
		23, 23, 23, 21, 22, 23, 22, 23, 23, 24,
		22, 21, 20, 22, 22, 23, 23, 21, 23, 22,
		22, 24, 21, 22, 23, 23, 21, 21, 22, 21,
		23, 22, 23, 23, 20, 22, 22, 22, 23, 22,
		22, 23, 26, 26, 20, 19, 22, 23, 22, 25,
		26, 26, 26, 27, 27, 26, 24, 25, 19, 21,
		26, 27, 27, 26, 27, 24, 21, 21, 26, 26,
		28, 27, 27, 27, 20, 24, 20, 21, 22, 21,
		21, 23, 22, 22, 25, 25, 24, 24, 26, 23,
		26, 27, 26, 26, 27, 27, 27, 27, 27, 28,
		27, 27, 27, 27, 27, 26, 30
	};

	private static ushort[] GenerateDecodingLookupTree()
	{
		ushort[] array = new ushort[3840];
		ReadOnlySpan<uint> encodingTableCodes = EncodingTableCodes;
		ReadOnlySpan<byte> encodingTableBitLengths = EncodingTableBitLengths;
		int num = 0;
		for (int i = 0; i <= 256; i++)
		{
			uint num2 = encodingTableCodes[i];
			int num3 = encodingTableBitLengths[i];
			int num4 = 0;
			int num5 = num3;
			while (num5 > 0)
			{
				int num6 = (int)(num2 >> 24);
				if (num5 <= 8)
				{
					int num7 = 1 << 8 - num5;
					for (int j = 0; j < num7; j++)
					{
						if (i == 256)
						{
							array[(num4 << 8) + (num6 | j)] = 33023;
						}
						else
						{
							array[(num4 << 8) + (num6 | j)] = (ushort)((num5 << 8) | i);
						}
					}
				}
				else
				{
					ushort num8 = array[(num4 << 8) + num6];
					if (num8 == 0)
					{
						num++;
						array[(num4 << 8) + num6] = (ushort)((0x80 | num) << 8);
						num4 = num;
					}
					else
					{
						num4 = (num8 & 0x7F00) >> 8;
					}
				}
				num5 -= 8;
				num2 <<= 8;
			}
		}
		return array;
	}

	public static int Decode(ReadOnlySpan<byte> src, ref byte[] dstArray)
	{
		Span<byte> span = dstArray;
		ushort[] array = s_decodingTree;
		int num = 0;
		uint num2 = 0u;
		int num3 = 0;
		int num4 = 0;
		int num5 = 0;
		while (num4 < src.Length)
		{
			num2 <<= 8;
			num2 |= src[num4++];
			num3 += 8;
			do
			{
				int num6 = (byte)(num2 >> num3 - 8);
				int num7 = array[(num << 8) + num6];
				if (num7 < 32768)
				{
					if (num5 == span.Length)
					{
						Array.Resize(ref dstArray, span.Length * 2);
						span = dstArray;
					}
					span[num5++] = (byte)num7;
					num = 0;
					num3 -= num7 >> 8;
				}
				else
				{
					num = (num7 & 0x7F00) >> 8;
					if (num == 0)
					{
						throw new HuffmanDecodingException(System.SR.net_http_hpack_huffman_decode_failed);
					}
					num3 -= 8;
				}
			}
			while (num3 >= 8);
		}
		while (num3 > 0)
		{
			if (num == 0)
			{
				uint num8 = uint.MaxValue >> 32 - num3;
				if ((num2 & num8) == num8)
				{
					break;
				}
			}
			int num6 = (byte)(num2 << 8 - num3);
			int num9 = array[(num << 8) + num6];
			if (num9 < 32768)
			{
				num3 -= num9 >> 8;
				if (num3 < 0)
				{
					throw new HuffmanDecodingException(System.SR.net_http_hpack_huffman_decode_failed);
				}
				if (num5 == span.Length)
				{
					Array.Resize(ref dstArray, span.Length * 2);
					span = dstArray;
				}
				span[num5++] = (byte)num9;
				num = 0;
				continue;
			}
			throw new HuffmanDecodingException(System.SR.net_http_hpack_huffman_decode_failed);
		}
		if (num != 0)
		{
			throw new HuffmanDecodingException(System.SR.net_http_hpack_huffman_decode_failed);
		}
		return num5;
	}
}
