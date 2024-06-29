namespace System.IO.Compression;

internal sealed class HuffmanTree
{
	private readonly int _tableBits;

	private readonly short[] _table;

	private readonly short[] _left;

	private readonly short[] _right;

	private readonly byte[] _codeLengthArray;

	private readonly int _tableMask;

	public static HuffmanTree StaticLiteralLengthTree { get; } = new HuffmanTree(GetStaticLiteralTreeLength());


	public static HuffmanTree StaticDistanceTree { get; } = new HuffmanTree(GetStaticDistanceTreeLength());


	public HuffmanTree(byte[] codeLengths)
	{
		_codeLengthArray = codeLengths;
		if (_codeLengthArray.Length == 288)
		{
			_tableBits = 9;
		}
		else
		{
			_tableBits = 7;
		}
		_tableMask = (1 << _tableBits) - 1;
		_table = new short[1 << _tableBits];
		_left = new short[2 * _codeLengthArray.Length];
		_right = new short[2 * _codeLengthArray.Length];
		CreateTable();
	}

	private static byte[] GetStaticLiteralTreeLength()
	{
		byte[] array = new byte[288];
		array.AsSpan(0, 144).Fill(8);
		array.AsSpan(144, 112).Fill(9);
		array.AsSpan(256, 24).Fill(7);
		array.AsSpan(280, 8).Fill(8);
		return array;
	}

	private static byte[] GetStaticDistanceTreeLength()
	{
		byte[] array = new byte[32];
		Array.Fill(array, (byte)5);
		return array;
	}

	private static uint BitReverse(uint code, int length)
	{
		uint num = 0u;
		do
		{
			num |= code & 1u;
			num <<= 1;
			code >>= 1;
		}
		while (--length > 0);
		return num >> 1;
	}

	private uint[] CalculateHuffmanCode()
	{
		Span<uint> span = stackalloc uint[17];
		span.Clear();
		byte[] codeLengthArray = _codeLengthArray;
		for (int i = 0; i < codeLengthArray.Length; i++)
		{
			span[codeLengthArray[i]]++;
		}
		span[0] = 0u;
		Span<uint> span2 = stackalloc uint[17];
		span2.Clear();
		uint num = 0u;
		for (int j = 1; j <= 16; j++)
		{
			num = num + span[j - 1] << 1;
			span2[j] = num;
		}
		uint[] array = new uint[288];
		for (int k = 0; k < _codeLengthArray.Length; k++)
		{
			int num2 = _codeLengthArray[k];
			if (num2 > 0)
			{
				array[k] = BitReverse(span2[num2], num2);
				span2[num2]++;
			}
		}
		return array;
	}

	private void CreateTable()
	{
		uint[] array = CalculateHuffmanCode();
		short num = (short)_codeLengthArray.Length;
		for (int i = 0; i < _codeLengthArray.Length; i++)
		{
			int num2 = _codeLengthArray[i];
			if (num2 <= 0)
			{
				continue;
			}
			int num3 = (int)array[i];
			if (num2 <= _tableBits)
			{
				int num4 = 1 << num2;
				if (num3 >= num4)
				{
					throw new InvalidDataException(System.SR.InvalidHuffmanData);
				}
				int num5 = 1 << _tableBits - num2;
				for (int j = 0; j < num5; j++)
				{
					_table[num3] = (short)i;
					num3 += num4;
				}
				continue;
			}
			int num6 = num2 - _tableBits;
			int num7 = 1 << _tableBits;
			int num8 = num3 & ((1 << _tableBits) - 1);
			short[] array2 = _table;
			do
			{
				short num9 = array2[num8];
				if (num9 == 0)
				{
					array2[num8] = (short)(-num);
					num9 = (short)(-num);
					num++;
				}
				if (num9 > 0)
				{
					throw new InvalidDataException(System.SR.InvalidHuffmanData);
				}
				array2 = (((num3 & num7) != 0) ? _right : _left);
				num8 = -num9;
				num7 <<= 1;
				num6--;
			}
			while (num6 != 0);
			array2[num8] = (short)i;
		}
	}

	public int GetNextSymbol(InputBuffer input)
	{
		uint num = input.TryLoad16Bits();
		if (input.AvailableBits == 0)
		{
			return -1;
		}
		int num2 = _table[num & _tableMask];
		if (num2 < 0)
		{
			uint num3 = (uint)(1 << _tableBits);
			do
			{
				num2 = -num2;
				num2 = (((num & num3) != 0) ? _right[num2] : _left[num2]);
				num3 <<= 1;
			}
			while (num2 < 0);
		}
		int num4 = _codeLengthArray[num2];
		if (num4 <= 0)
		{
			throw new InvalidDataException(System.SR.InvalidHuffmanData);
		}
		if (num4 > input.AvailableBits)
		{
			return -1;
		}
		input.SkipBits(num4);
		return num2;
	}
}
