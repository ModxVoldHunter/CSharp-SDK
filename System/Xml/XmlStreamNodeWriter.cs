using System.Buffers.Binary;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace System.Xml;

internal abstract class XmlStreamNodeWriter : XmlNodeWriter
{
	private readonly byte[] _buffer;

	private int _offset;

	private bool _ownsStream;

	private const int bufferLength = 512;

	private const int maxBytesPerChar = 3;

	private Encoding _encoding;

	public Stream OutputStream { get; set; }

	public byte[] StreamBuffer => _buffer;

	public int BufferOffset => _offset;

	public int Position => (int)OutputStream.Position + _offset;

	protected XmlStreamNodeWriter()
	{
		_buffer = new byte[512];
		OutputStream = null;
	}

	protected void SetOutput(Stream stream, bool ownsStream, Encoding encoding)
	{
		OutputStream = stream;
		_ownsStream = ownsStream;
		_offset = 0;
		if (encoding != null)
		{
			_encoding = encoding;
		}
	}

	protected byte[] GetBuffer(int count, out int offset)
	{
		int offset2 = _offset;
		if (offset2 + count <= 512)
		{
			offset = offset2;
		}
		else
		{
			FlushBuffer();
			offset = 0;
		}
		return _buffer;
	}

	protected async Task<BytesWithOffset> GetBufferAsync(int count)
	{
		int offset = _offset;
		int offset2;
		if (offset + count <= 512)
		{
			offset2 = offset;
		}
		else
		{
			await FlushBufferAsync().ConfigureAwait(continueOnCapturedContext: false);
			offset2 = 0;
		}
		return new BytesWithOffset(_buffer, offset2);
	}

	protected void Advance(int count)
	{
		_offset += count;
	}

	private void EnsureByte()
	{
		if (_offset >= 512)
		{
			FlushBuffer();
		}
	}

	protected void WriteByte(byte b)
	{
		EnsureByte();
		_buffer[_offset++] = b;
	}

	protected Task WriteByteAsync(byte b)
	{
		if (_offset >= 512)
		{
			return FlushBufferAndWriteByteAsync(b);
		}
		_buffer[_offset++] = b;
		return Task.CompletedTask;
	}

	private async Task FlushBufferAndWriteByteAsync(byte b)
	{
		await FlushBufferAsync().ConfigureAwait(continueOnCapturedContext: false);
		_buffer[_offset++] = b;
	}

	protected void WriteByte(char ch)
	{
		WriteByte((byte)ch);
	}

	protected Task WriteByteAsync(char ch)
	{
		return WriteByteAsync((byte)ch);
	}

	protected void WriteBytes(byte b1, byte b2)
	{
		byte[] buffer = _buffer;
		int num = _offset;
		if (num + 1 >= 512)
		{
			FlushBuffer();
			num = 0;
		}
		buffer[num] = b1;
		buffer[num + 1] = b2;
		_offset += 2;
	}

	protected Task WriteBytesAsync(byte b1, byte b2)
	{
		if (_offset + 1 >= 512)
		{
			return FlushAndWriteBytesAsync(b1, b2);
		}
		_buffer[_offset++] = b1;
		_buffer[_offset++] = b2;
		return Task.CompletedTask;
	}

	private async Task FlushAndWriteBytesAsync(byte b1, byte b2)
	{
		await FlushBufferAsync().ConfigureAwait(continueOnCapturedContext: false);
		_buffer[_offset++] = b1;
		_buffer[_offset++] = b2;
	}

	protected void WriteBytes(char ch1, char ch2)
	{
		WriteBytes((byte)ch1, (byte)ch2);
	}

	protected Task WriteBytesAsync(char ch1, char ch2)
	{
		return WriteBytesAsync((byte)ch1, (byte)ch2);
	}

	public void WriteBytes(byte[] byteBuffer, int byteOffset, int byteCount)
	{
		if (byteCount < 512)
		{
			int offset;
			byte[] buffer = GetBuffer(byteCount, out offset);
			Buffer.BlockCopy(byteBuffer, byteOffset, buffer, offset, byteCount);
			Advance(byteCount);
		}
		else
		{
			FlushBuffer();
			OutputStream.Write(byteBuffer, byteOffset, byteCount);
		}
	}

	protected void WriteBytes(ReadOnlySpan<byte> bytes)
	{
		if (bytes.Length < 512)
		{
			int offset;
			Span<byte> destination = GetBuffer(bytes.Length, out offset).AsSpan(offset, bytes.Length);
			bytes.CopyTo(destination);
			Advance(bytes.Length);
		}
		else
		{
			FlushBuffer();
			OutputStream.Write(bytes);
		}
	}

	protected unsafe void WriteUTF8Char(int ch)
	{
		if (ch < 128)
		{
			WriteByte((byte)ch);
		}
		else if (ch <= 65535)
		{
			char* ptr = stackalloc char[1];
			*ptr = (char)ch;
			UnsafeWriteUTF8Chars(ptr, 1);
		}
		else
		{
			SurrogateChar surrogateChar = new SurrogateChar(ch);
			char* ptr2 = stackalloc char[2];
			*ptr2 = surrogateChar.HighChar;
			ptr2[1] = surrogateChar.LowChar;
			UnsafeWriteUTF8Chars(ptr2, 2);
		}
	}

	protected unsafe void WriteUTF8Chars(string value)
	{
		int length = value.Length;
		if (length > 0)
		{
			fixed (char* chars = value)
			{
				UnsafeWriteUTF8Chars(chars, length);
			}
		}
	}

	protected void WriteUTF8Bytes(ReadOnlySpan<byte> value)
	{
		if (value.Length < 512)
		{
			int offset;
			byte[] buffer = GetBuffer(value.Length, out offset);
			value.CopyTo(buffer.AsSpan(offset));
			Advance(value.Length);
		}
		else
		{
			FlushBuffer();
			OutputStream.Write(value);
		}
	}

	protected unsafe void UnsafeWriteUTF8Chars(char* chars, int charCount)
	{
		while (charCount > 170)
		{
			int num = 170;
			if ((chars[num - 1] & 0xFC00) == 55296)
			{
				num--;
			}
			int offset;
			byte[] buffer = GetBuffer(num * 3, out offset);
			Advance(UnsafeGetUTF8Chars(chars, num, buffer, offset));
			charCount -= num;
			chars += num;
		}
		if (charCount > 0)
		{
			int offset2;
			byte[] buffer2 = GetBuffer(charCount * 3, out offset2);
			Advance(UnsafeGetUTF8Chars(chars, charCount, buffer2, offset2));
		}
	}

	protected unsafe void UnsafeWriteUnicodeChars(char* chars, int charCount)
	{
		while (charCount > 256)
		{
			int num = 256;
			if ((chars[num - 1] & 0xFC00) == 55296)
			{
				num--;
			}
			int offset;
			byte[] buffer = GetBuffer(num * 2, out offset);
			Advance(UnsafeGetUnicodeChars(chars, num, buffer, offset));
			charCount -= num;
			chars += num;
		}
		if (charCount > 0)
		{
			int offset2;
			byte[] buffer2 = GetBuffer(charCount * 2, out offset2);
			Advance(UnsafeGetUnicodeChars(chars, charCount, buffer2, offset2));
		}
	}

	protected unsafe static int UnsafeGetUnicodeChars(char* chars, int charCount, byte[] buffer, int offset)
	{
		if (BitConverter.IsLittleEndian)
		{
			new ReadOnlySpan<char>(chars, charCount).CopyTo(MemoryMarshal.Cast<byte, char>(buffer.AsSpan(offset)));
		}
		else
		{
			BinaryPrimitives.ReverseEndianness(new ReadOnlySpan<short>(chars, charCount), MemoryMarshal.Cast<byte, short>(buffer.AsSpan(offset)));
		}
		return charCount * 2;
	}

	protected unsafe int UnsafeGetUTF8Length(char* chars, int charCount)
	{
		return (_encoding ?? DataContractSerializer.ValidatingUTF8).GetByteCount(chars, charCount);
	}

	protected unsafe int UnsafeGetUTF8Chars(char* chars, int charCount, byte[] buffer, int offset)
	{
		if (charCount > 0)
		{
			fixed (byte* ptr = &buffer[offset])
			{
				if (!Vector128.IsHardwareAccelerated || (uint)charCount < 32u)
				{
					byte* ptr2 = ptr;
					char* ptr3 = chars + charCount;
					while (true)
					{
						if (chars >= ptr3)
						{
							return charCount;
						}
						char c = *chars;
						if (c >= '\u0080')
						{
							break;
						}
						*ptr2 = (byte)c;
						ptr2++;
						chars++;
					}
					byte* ptr4 = ptr + buffer.Length - offset;
					return (int)(ptr2 - ptr) + (_encoding ?? DataContractSerializer.ValidatingUTF8).GetBytes(chars, (int)(ptr3 - chars), ptr2, (int)(ptr4 - ptr2));
				}
				return (_encoding ?? DataContractSerializer.ValidatingUTF8).GetBytes(chars, charCount, ptr, buffer.Length - offset);
			}
		}
		return 0;
	}

	protected virtual void FlushBuffer()
	{
		if (_offset != 0)
		{
			OutputStream.Write(_buffer, 0, _offset);
			_offset = 0;
		}
	}

	protected virtual Task FlushBufferAsync()
	{
		if (_offset != 0)
		{
			Task result = OutputStream.WriteAsync(_buffer, 0, _offset);
			_offset = 0;
			return result;
		}
		return Task.CompletedTask;
	}

	public override void Flush()
	{
		FlushBuffer();
		OutputStream.Flush();
	}

	public override async Task FlushAsync()
	{
		await FlushBufferAsync().ConfigureAwait(continueOnCapturedContext: false);
		await OutputStream.FlushAsync().ConfigureAwait(continueOnCapturedContext: false);
	}

	public override void Close()
	{
		if (OutputStream != null)
		{
			if (_ownsStream)
			{
				OutputStream.Dispose();
			}
			OutputStream = null;
		}
	}
}
