namespace System.Text;

internal sealed class BinHexEncoding : Encoding
{
	public override int GetMaxByteCount(int charCount)
	{
		ArgumentOutOfRangeException.ThrowIfNegative(charCount, "charCount");
		if (charCount % 2 != 0)
		{
			throw new FormatException(System.SR.Format(System.SR.XmlInvalidBinHexLength, charCount.ToString()));
		}
		return charCount / 2;
	}

	public override int GetByteCount(char[] chars, int index, int count)
	{
		return GetMaxByteCount(count);
	}

	public override int GetBytes(char[] chars, int charIndex, int charCount, byte[] bytes, int byteIndex)
	{
		ArgumentNullException.ThrowIfNull(chars, "chars");
		ArgumentOutOfRangeException.ThrowIfNegative(charIndex, "charIndex");
		if (charIndex > chars.Length)
		{
			throw new ArgumentOutOfRangeException("charIndex", System.SR.Format(System.SR.OffsetExceedsBufferSize, chars.Length));
		}
		ArgumentOutOfRangeException.ThrowIfNegative(charCount, "charCount");
		if (charCount > chars.Length - charIndex)
		{
			throw new ArgumentOutOfRangeException("charCount", System.SR.Format(System.SR.SizeExceedsRemainingBufferSpace, chars.Length - charIndex));
		}
		ArgumentNullException.ThrowIfNull(bytes, "bytes");
		ArgumentOutOfRangeException.ThrowIfNegative(byteIndex, "byteIndex");
		if (byteIndex > bytes.Length)
		{
			throw new ArgumentOutOfRangeException("byteIndex", System.SR.Format(System.SR.OffsetExceedsBufferSize, bytes.Length));
		}
		int byteCount = GetByteCount(chars, charIndex, charCount);
		if (byteCount < 0 || byteCount > bytes.Length - byteIndex)
		{
			throw new ArgumentException(System.SR.XmlArrayTooSmall, "bytes");
		}
		if (charCount > 0 && !System.HexConverter.TryDecodeFromUtf16(chars.AsSpan(charIndex, charCount), bytes.AsSpan(byteIndex, byteCount), out var charsProcessed))
		{
			int num = charsProcessed + charIndex;
			throw new FormatException(System.SR.Format(System.SR.XmlInvalidBinHexSequence, new string(chars, num, 2), num));
		}
		return byteCount;
	}

	public override int GetMaxCharCount(int byteCount)
	{
		if (byteCount < 0 || byteCount > 1073741823)
		{
			throw new ArgumentOutOfRangeException("byteCount", System.SR.Format(System.SR.ValueMustBeInRange, 0, 1073741823));
		}
		return byteCount * 2;
	}

	public override int GetCharCount(byte[] bytes, int index, int count)
	{
		return GetMaxCharCount(count);
	}

	public override int GetChars(byte[] bytes, int byteIndex, int byteCount, char[] chars, int charIndex)
	{
		ArgumentNullException.ThrowIfNull(bytes, "bytes");
		ArgumentOutOfRangeException.ThrowIfNegative(byteIndex, "byteIndex");
		if (byteIndex > bytes.Length)
		{
			throw new ArgumentOutOfRangeException("byteIndex", System.SR.Format(System.SR.OffsetExceedsBufferSize, bytes.Length));
		}
		ArgumentOutOfRangeException.ThrowIfNegative(byteCount, "byteCount");
		if (byteCount > bytes.Length - byteIndex)
		{
			throw new ArgumentOutOfRangeException("byteCount", System.SR.Format(System.SR.SizeExceedsRemainingBufferSpace, bytes.Length - byteIndex));
		}
		int charCount = GetCharCount(bytes, byteIndex, byteCount);
		ArgumentNullException.ThrowIfNull(chars, "chars");
		ArgumentOutOfRangeException.ThrowIfNegative(charIndex, "charIndex");
		if (charIndex > chars.Length)
		{
			throw new ArgumentOutOfRangeException("charIndex", System.SR.Format(System.SR.OffsetExceedsBufferSize, chars.Length));
		}
		if (charCount < 0 || charCount > chars.Length - charIndex)
		{
			throw new ArgumentException(System.SR.XmlArrayTooSmall, "chars");
		}
		if (byteCount > 0)
		{
			System.HexConverter.EncodeToUtf16(bytes.AsSpan(byteIndex, byteCount), chars.AsSpan(charIndex));
		}
		return charCount;
	}
}
