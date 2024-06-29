using System.Data.Common;
using System.IO;

namespace System.Data.SqlTypes;

internal sealed class SqlXmlStreamWrapper : Stream
{
	private readonly Stream _stream;

	private long _lPosition;

	private bool _isClosed;

	public override bool CanRead
	{
		get
		{
			if (IsStreamClosed())
			{
				return false;
			}
			return _stream.CanRead;
		}
	}

	public override bool CanSeek
	{
		get
		{
			if (IsStreamClosed())
			{
				return false;
			}
			return _stream.CanSeek;
		}
	}

	public override bool CanWrite
	{
		get
		{
			if (IsStreamClosed())
			{
				return false;
			}
			return _stream.CanWrite;
		}
	}

	public override long Length
	{
		get
		{
			ThrowIfStreamClosed();
			ThrowIfStreamCannotSeek("get_Length");
			return _stream.Length;
		}
	}

	public override long Position
	{
		get
		{
			ThrowIfStreamClosed();
			ThrowIfStreamCannotSeek("get_Position");
			return _lPosition;
		}
		set
		{
			ThrowIfStreamClosed();
			ThrowIfStreamCannotSeek("set_Position");
			ArgumentOutOfRangeException.ThrowIfNegative(value, "value");
			ArgumentOutOfRangeException.ThrowIfGreaterThan(value, _stream.Length, "value");
			_lPosition = value;
		}
	}

	internal SqlXmlStreamWrapper(Stream stream)
	{
		_stream = stream;
		_lPosition = 0L;
		_isClosed = false;
	}

	public override long Seek(long offset, SeekOrigin origin)
	{
		long num = 0L;
		ThrowIfStreamClosed();
		ThrowIfStreamCannotSeek("Seek");
		switch (origin)
		{
		case SeekOrigin.Begin:
			ArgumentOutOfRangeException.ThrowIfNegative(offset, "offset");
			ArgumentOutOfRangeException.ThrowIfGreaterThan(offset, _stream.Length, "offset");
			_lPosition = offset;
			break;
		case SeekOrigin.Current:
			num = _lPosition + offset;
			ArgumentOutOfRangeException.ThrowIfNegative(num, "offset");
			ArgumentOutOfRangeException.ThrowIfGreaterThan(num, _stream.Length, "offset");
			_lPosition = num;
			break;
		case SeekOrigin.End:
			num = _stream.Length + offset;
			ArgumentOutOfRangeException.ThrowIfNegative(num, "offset");
			ArgumentOutOfRangeException.ThrowIfGreaterThan(num, _stream.Length, "offset");
			_lPosition = num;
			break;
		default:
			throw ADP.InvalidSeekOrigin("offset");
		}
		return _lPosition;
	}

	public override int Read(byte[] buffer, int offset, int count)
	{
		ThrowIfStreamClosed();
		ThrowIfStreamCannotRead("Read");
		ArgumentNullException.ThrowIfNull(buffer, "buffer");
		ArgumentOutOfRangeException.ThrowIfNegative(offset, "offset");
		ArgumentOutOfRangeException.ThrowIfGreaterThan(offset, buffer.Length, "offset");
		ArgumentOutOfRangeException.ThrowIfNegative(count, "count");
		ArgumentOutOfRangeException.ThrowIfGreaterThan(count, buffer.Length - offset, "count");
		if (_stream.CanSeek && _stream.Position != _lPosition)
		{
			_stream.Seek(_lPosition, SeekOrigin.Begin);
		}
		int num = _stream.Read(buffer, offset, count);
		_lPosition += num;
		return num;
	}

	public override int Read(Span<byte> buffer)
	{
		ThrowIfStreamClosed();
		ThrowIfStreamCannotRead("Read");
		if (_stream.CanSeek && _stream.Position != _lPosition)
		{
			_stream.Seek(_lPosition, SeekOrigin.Begin);
		}
		int num = _stream.Read(buffer);
		_lPosition += num;
		return num;
	}

	public override void Write(byte[] buffer, int offset, int count)
	{
		ThrowIfStreamClosed();
		ThrowIfStreamCannotWrite("Write");
		ArgumentNullException.ThrowIfNull(buffer, "buffer");
		ArgumentOutOfRangeException.ThrowIfNegative(offset, "offset");
		ArgumentOutOfRangeException.ThrowIfGreaterThan(offset, buffer.Length, "offset");
		ArgumentOutOfRangeException.ThrowIfNegative(count, "count");
		ArgumentOutOfRangeException.ThrowIfGreaterThan(count, buffer.Length - offset, "count");
		if (_stream.CanSeek && _stream.Position != _lPosition)
		{
			_stream.Seek(_lPosition, SeekOrigin.Begin);
		}
		_stream.Write(buffer, offset, count);
		_lPosition += count;
	}

	public override int ReadByte()
	{
		ThrowIfStreamClosed();
		ThrowIfStreamCannotRead("ReadByte");
		if (_stream.CanSeek && _lPosition >= _stream.Length)
		{
			return -1;
		}
		if (_stream.CanSeek && _stream.Position != _lPosition)
		{
			_stream.Seek(_lPosition, SeekOrigin.Begin);
		}
		int result = _stream.ReadByte();
		_lPosition++;
		return result;
	}

	public override void WriteByte(byte value)
	{
		ThrowIfStreamClosed();
		ThrowIfStreamCannotWrite("WriteByte");
		if (_stream.CanSeek && _stream.Position != _lPosition)
		{
			_stream.Seek(_lPosition, SeekOrigin.Begin);
		}
		_stream.WriteByte(value);
		_lPosition++;
	}

	public override void SetLength(long value)
	{
		ThrowIfStreamClosed();
		ThrowIfStreamCannotSeek("SetLength");
		_stream.SetLength(value);
		if (_lPosition > value)
		{
			_lPosition = value;
		}
	}

	public override void Flush()
	{
		_stream?.Flush();
	}

	protected override void Dispose(bool disposing)
	{
		try
		{
			_isClosed = true;
		}
		finally
		{
			base.Dispose(disposing);
		}
	}

	private void ThrowIfStreamCannotSeek(string method)
	{
		if (!_stream.CanSeek)
		{
			throw new NotSupportedException(SQLResource.InvalidOpStreamNonSeekable(method));
		}
	}

	private void ThrowIfStreamCannotRead(string method)
	{
		if (!_stream.CanRead)
		{
			throw new NotSupportedException(SQLResource.InvalidOpStreamNonReadable(method));
		}
	}

	private void ThrowIfStreamCannotWrite(string method)
	{
		if (!_stream.CanWrite)
		{
			throw new NotSupportedException(SQLResource.InvalidOpStreamNonWritable(method));
		}
	}

	private void ThrowIfStreamClosed()
	{
		ObjectDisposedException.ThrowIf(IsStreamClosed(), this);
	}

	private bool IsStreamClosed()
	{
		if (_isClosed || _stream == null || (!_stream.CanRead && !_stream.CanWrite && !_stream.CanSeek))
		{
			return true;
		}
		return false;
	}
}
