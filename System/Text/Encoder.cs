using System.Runtime.InteropServices;

namespace System.Text;

public abstract class Encoder
{
	internal EncoderFallback _fallback;

	internal EncoderFallbackBuffer _fallbackBuffer;

	public EncoderFallback? Fallback
	{
		get
		{
			return _fallback;
		}
		set
		{
			ArgumentNullException.ThrowIfNull(value, "value");
			if (_fallbackBuffer != null && _fallbackBuffer.Remaining > 0)
			{
				throw new ArgumentException(SR.Argument_FallbackBufferNotEmpty, "value");
			}
			_fallback = value;
			_fallbackBuffer = null;
		}
	}

	public EncoderFallbackBuffer FallbackBuffer
	{
		get
		{
			if (_fallbackBuffer == null)
			{
				_fallbackBuffer = ((_fallback != null) ? _fallback.CreateFallbackBuffer() : EncoderFallback.ReplacementFallback.CreateFallbackBuffer());
			}
			return _fallbackBuffer;
		}
	}

	internal bool InternalHasFallbackBuffer => _fallbackBuffer != null;

	public virtual void Reset()
	{
		char[] chars = Array.Empty<char>();
		byte[] bytes = new byte[GetByteCount(chars, 0, 0, flush: true)];
		GetBytes(chars, 0, 0, bytes, 0, flush: true);
		_fallbackBuffer?.Reset();
	}

	public abstract int GetByteCount(char[] chars, int index, int count, bool flush);

	[CLSCompliant(false)]
	public unsafe virtual int GetByteCount(char* chars, int count, bool flush)
	{
		ArgumentNullException.ThrowIfNull(chars, "chars");
		ArgumentOutOfRangeException.ThrowIfNegative(count, "count");
		char[] array = new char[count];
		for (int i = 0; i < count; i++)
		{
			array[i] = chars[i];
		}
		return GetByteCount(array, 0, count, flush);
	}

	public unsafe virtual int GetByteCount(ReadOnlySpan<char> chars, bool flush)
	{
		fixed (char* chars2 = &MemoryMarshal.GetNonNullPinnableReference(chars))
		{
			return GetByteCount(chars2, chars.Length, flush);
		}
	}

	public abstract int GetBytes(char[] chars, int charIndex, int charCount, byte[] bytes, int byteIndex, bool flush);

	[CLSCompliant(false)]
	public unsafe virtual int GetBytes(char* chars, int charCount, byte* bytes, int byteCount, bool flush)
	{
		ArgumentNullException.ThrowIfNull(chars, "chars");
		ArgumentNullException.ThrowIfNull(bytes, "bytes");
		ArgumentOutOfRangeException.ThrowIfNegative(charCount, "charCount");
		ArgumentOutOfRangeException.ThrowIfNegative(byteCount, "byteCount");
		char[] array = new char[charCount];
		for (int i = 0; i < charCount; i++)
		{
			array[i] = chars[i];
		}
		byte[] array2 = new byte[byteCount];
		int bytes2 = GetBytes(array, 0, charCount, array2, 0, flush);
		if (bytes2 < byteCount)
		{
			byteCount = bytes2;
		}
		for (int j = 0; j < byteCount; j++)
		{
			bytes[j] = array2[j];
		}
		return byteCount;
	}

	public unsafe virtual int GetBytes(ReadOnlySpan<char> chars, Span<byte> bytes, bool flush)
	{
		fixed (char* chars2 = &MemoryMarshal.GetNonNullPinnableReference(chars))
		{
			fixed (byte* bytes2 = &MemoryMarshal.GetNonNullPinnableReference(bytes))
			{
				return GetBytes(chars2, chars.Length, bytes2, bytes.Length, flush);
			}
		}
	}

	public virtual void Convert(char[] chars, int charIndex, int charCount, byte[] bytes, int byteIndex, int byteCount, bool flush, out int charsUsed, out int bytesUsed, out bool completed)
	{
		ArgumentNullException.ThrowIfNull(chars, "chars");
		ArgumentNullException.ThrowIfNull(bytes, "bytes");
		ArgumentOutOfRangeException.ThrowIfNegative(charIndex, "charIndex");
		ArgumentOutOfRangeException.ThrowIfNegative(charCount, "charCount");
		ArgumentOutOfRangeException.ThrowIfNegative(byteIndex, "byteIndex");
		ArgumentOutOfRangeException.ThrowIfNegative(byteCount, "byteCount");
		if (chars.Length - charIndex < charCount)
		{
			throw new ArgumentOutOfRangeException("chars", SR.ArgumentOutOfRange_IndexCountBuffer);
		}
		if (bytes.Length - byteIndex < byteCount)
		{
			throw new ArgumentOutOfRangeException("bytes", SR.ArgumentOutOfRange_IndexCountBuffer);
		}
		for (charsUsed = charCount; charsUsed > 0; charsUsed /= 2)
		{
			if (GetByteCount(chars, charIndex, charsUsed, flush) <= byteCount)
			{
				bytesUsed = GetBytes(chars, charIndex, charsUsed, bytes, byteIndex, flush);
				completed = charsUsed == charCount && (_fallbackBuffer == null || _fallbackBuffer.Remaining == 0);
				return;
			}
			flush = false;
		}
		throw new ArgumentException(SR.Argument_ConversionOverflow);
	}

	[CLSCompliant(false)]
	public unsafe virtual void Convert(char* chars, int charCount, byte* bytes, int byteCount, bool flush, out int charsUsed, out int bytesUsed, out bool completed)
	{
		ArgumentNullException.ThrowIfNull(chars, "chars");
		ArgumentNullException.ThrowIfNull(bytes, "bytes");
		ArgumentOutOfRangeException.ThrowIfNegative(charCount, "charCount");
		ArgumentOutOfRangeException.ThrowIfNegative(byteCount, "byteCount");
		for (charsUsed = charCount; charsUsed > 0; charsUsed /= 2)
		{
			if (GetByteCount(chars, charsUsed, flush) <= byteCount)
			{
				bytesUsed = GetBytes(chars, charsUsed, bytes, byteCount, flush);
				completed = charsUsed == charCount && (_fallbackBuffer == null || _fallbackBuffer.Remaining == 0);
				return;
			}
			flush = false;
		}
		throw new ArgumentException(SR.Argument_ConversionOverflow);
	}

	public unsafe virtual void Convert(ReadOnlySpan<char> chars, Span<byte> bytes, bool flush, out int charsUsed, out int bytesUsed, out bool completed)
	{
		fixed (char* chars2 = &MemoryMarshal.GetNonNullPinnableReference(chars))
		{
			fixed (byte* bytes2 = &MemoryMarshal.GetNonNullPinnableReference(bytes))
			{
				Convert(chars2, chars.Length, bytes2, bytes.Length, flush, out charsUsed, out bytesUsed, out completed);
			}
		}
	}
}
