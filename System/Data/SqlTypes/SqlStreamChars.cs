using System.Data.Common;
using System.IO;
using System.Runtime.CompilerServices;

namespace System.Data.SqlTypes;

internal sealed class SqlStreamChars
{
	private readonly SqlChars _sqlchars;

	private long _lPosition;

	public long Length
	{
		get
		{
			CheckIfStreamClosed("get_Length");
			return _sqlchars.Length;
		}
	}

	public long Position
	{
		get
		{
			CheckIfStreamClosed("get_Position");
			return _lPosition;
		}
	}

	public long Seek(long offset, SeekOrigin origin)
	{
		CheckIfStreamClosed("Seek");
		switch (origin)
		{
		case SeekOrigin.Begin:
			if (offset < 0 || offset > _sqlchars.Length)
			{
				throw ADP.ArgumentOutOfRange("offset");
			}
			_lPosition = offset;
			break;
		case SeekOrigin.Current:
		{
			long num = _lPosition + offset;
			if (num < 0 || num > _sqlchars.Length)
			{
				throw ADP.ArgumentOutOfRange("offset");
			}
			_lPosition = num;
			break;
		}
		case SeekOrigin.End:
		{
			long num = _sqlchars.Length + offset;
			if (num < 0 || num > _sqlchars.Length)
			{
				throw ADP.ArgumentOutOfRange("offset");
			}
			_lPosition = num;
			break;
		}
		default:
			throw ADP.ArgumentOutOfRange("offset");
		}
		return _lPosition;
	}

	public int Read(char[] buffer, int offset, int count)
	{
		CheckIfStreamClosed("Read");
		ArgumentNullException.ThrowIfNull(buffer, "buffer");
		ArgumentOutOfRangeException.ThrowIfNegative(offset, "offset");
		ArgumentOutOfRangeException.ThrowIfGreaterThan(offset, buffer.Length, "offset");
		ArgumentOutOfRangeException.ThrowIfNegative(count, "count");
		ArgumentOutOfRangeException.ThrowIfGreaterThan(count, buffer.Length - offset, "count");
		int num = (int)_sqlchars.Read(_lPosition, buffer, offset, count);
		_lPosition += num;
		return num;
	}

	public void Write(char[] buffer, int offset, int count)
	{
		CheckIfStreamClosed("Write");
		ArgumentNullException.ThrowIfNull(buffer, "buffer");
		ArgumentOutOfRangeException.ThrowIfNegative(offset, "offset");
		ArgumentOutOfRangeException.ThrowIfGreaterThan(offset, buffer.Length, "offset");
		ArgumentOutOfRangeException.ThrowIfNegative(count, "count");
		ArgumentOutOfRangeException.ThrowIfGreaterThan(count, buffer.Length - offset, "count");
		_sqlchars.Write(_lPosition, buffer, offset, count);
		_lPosition += count;
	}

	public void SetLength(long value)
	{
		CheckIfStreamClosed("SetLength");
		_sqlchars.SetLength(value);
		if (_lPosition > value)
		{
			_lPosition = value;
		}
	}

	private bool FClosed()
	{
		return _sqlchars == null;
	}

	private void CheckIfStreamClosed([CallerMemberName] string methodname = "")
	{
		if (FClosed())
		{
			throw ADP.StreamClosed(methodname);
		}
	}
}
