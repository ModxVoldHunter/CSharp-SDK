namespace System.Xml;

internal sealed class Base64Decoder : IncrementalReadDecoder
{
	private byte[] _buffer;

	private int _startIndex;

	private int _curIndex;

	private int _endIndex;

	private int _bits;

	private int _bitsFilled;

	internal override int DecodedCount => _curIndex - _startIndex;

	internal override bool IsFull => _curIndex == _endIndex;

	internal override int Decode(char[] chars, int startPos, int len)
	{
		ArgumentNullException.ThrowIfNull(chars, "chars");
		ArgumentOutOfRangeException.ThrowIfNegative(len, "len");
		ArgumentOutOfRangeException.ThrowIfNegative(startPos, "startPos");
		ArgumentOutOfRangeException.ThrowIfGreaterThan(len, chars.Length - startPos, "len");
		if (len == 0)
		{
			return 0;
		}
		Decode(chars.AsSpan(startPos, len), _buffer.AsSpan(_curIndex, _endIndex - _curIndex), out var charsDecoded, out var bytesDecoded);
		_curIndex += bytesDecoded;
		return charsDecoded;
	}

	internal override int Decode(string str, int startPos, int len)
	{
		ArgumentNullException.ThrowIfNull(str, "str");
		ArgumentOutOfRangeException.ThrowIfNegative(len, "len");
		ArgumentOutOfRangeException.ThrowIfNegative(startPos, "startPos");
		ArgumentOutOfRangeException.ThrowIfGreaterThan(len, str.Length - startPos, "len");
		if (len == 0)
		{
			return 0;
		}
		Decode(str.AsSpan(startPos, len), _buffer.AsSpan(_curIndex, _endIndex - _curIndex), out var charsDecoded, out var bytesDecoded);
		_curIndex += bytesDecoded;
		return charsDecoded;
	}

	internal override void Reset()
	{
		_bitsFilled = 0;
		_bits = 0;
	}

	internal override void SetNextOutputBuffer(Array buffer, int index, int count)
	{
		_buffer = (byte[])buffer;
		_startIndex = index;
		_curIndex = index;
		_endIndex = index + count;
	}

	private void Decode(ReadOnlySpan<char> chars, Span<byte> bytes, out int charsDecoded, out int bytesDecoded)
	{
		int num = 0;
		int num2 = 0;
		int num3 = _bits;
		int num4 = _bitsFilled;
		ReadOnlySpan<byte> readOnlySpan = new byte[123]
		{
			255, 255, 255, 255, 255, 255, 255, 255, 255, 255,
			255, 255, 255, 255, 255, 255, 255, 255, 255, 255,
			255, 255, 255, 255, 255, 255, 255, 255, 255, 255,
			255, 255, 255, 255, 255, 255, 255, 255, 255, 255,
			255, 255, 255, 62, 255, 255, 255, 63, 52, 53,
			54, 55, 56, 57, 58, 59, 60, 61, 255, 255,
			255, 255, 255, 255, 255, 0, 1, 2, 3, 4,
			5, 6, 7, 8, 9, 10, 11, 12, 13, 14,
			15, 16, 17, 18, 19, 20, 21, 22, 23, 24,
			25, 255, 255, 255, 255, 255, 255, 26, 27, 28,
			29, 30, 31, 32, 33, 34, 35, 36, 37, 38,
			39, 40, 41, 42, 43, 44, 45, 46, 47, 48,
			49, 50, 51
		};
		while (true)
		{
			if ((uint)num2 < (uint)chars.Length && (uint)num < (uint)bytes.Length)
			{
				char c = chars[num2];
				if (c != '=')
				{
					num2++;
					if (XmlCharType.IsWhiteSpace(c))
					{
						continue;
					}
					int num5;
					if (c >= readOnlySpan.Length || (num5 = readOnlySpan[c]) == 255)
					{
						throw new XmlException(System.SR.Xml_InvalidBase64Value, chars.ToString());
					}
					num3 = (num3 << 6) | num5;
					num4 += 6;
					if (num4 >= 8)
					{
						bytes[num++] = (byte)((uint)(num3 >> num4 - 8) & 0xFFu);
						num4 -= 8;
						if (num == bytes.Length)
						{
							break;
						}
					}
					continue;
				}
			}
			if ((uint)num2 >= (uint)chars.Length || chars[num2] != '=')
			{
				break;
			}
			num4 = 0;
			do
			{
				num2++;
			}
			while ((uint)num2 < (uint)chars.Length && chars[num2] == '=');
			while ((uint)num2 < (uint)chars.Length)
			{
				if (!XmlCharType.IsWhiteSpace(chars[num2++]))
				{
					throw new XmlException(System.SR.Xml_InvalidBase64Value, chars.ToString());
				}
			}
			break;
		}
		_bits = num3;
		_bitsFilled = num4;
		bytesDecoded = num;
		charsDecoded = num2;
	}
}
