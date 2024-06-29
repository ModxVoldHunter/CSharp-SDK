using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace System.IO;

public class UnmanagedMemoryStream : Stream
{
	private SafeBuffer _buffer;

	private unsafe byte* _mem;

	private nuint _capacity;

	private nuint _offset;

	private nuint _length;

	private long _position;

	private FileAccess _access;

	private bool _isOpen;

	private CachedCompletedInt32Task _lastReadTask;

	public override bool CanRead
	{
		get
		{
			if (_isOpen)
			{
				return (_access & FileAccess.Read) != 0;
			}
			return false;
		}
	}

	public override bool CanSeek => _isOpen;

	public override bool CanWrite
	{
		get
		{
			if (_isOpen)
			{
				return (_access & FileAccess.Write) != 0;
			}
			return false;
		}
	}

	public override long Length
	{
		get
		{
			EnsureNotClosed();
			return (long)_length;
		}
	}

	public long Capacity
	{
		get
		{
			EnsureNotClosed();
			return (long)_capacity;
		}
	}

	public override long Position
	{
		get
		{
			if (!CanSeek)
			{
				ThrowHelper.ThrowObjectDisposedException_StreamClosed(null);
			}
			return _position;
		}
		set
		{
			ArgumentOutOfRangeException.ThrowIfNegative(value, "value");
			if (!CanSeek)
			{
				ThrowHelper.ThrowObjectDisposedException_StreamClosed(null);
			}
			_position = value;
		}
	}

	[CLSCompliant(false)]
	public unsafe byte* PositionPointer
	{
		get
		{
			if (_buffer != null)
			{
				throw new NotSupportedException(SR.NotSupported_UmsSafeBuffer);
			}
			EnsureNotClosed();
			long position = _position;
			if (position > (long)_capacity)
			{
				throw new IndexOutOfRangeException(SR.IndexOutOfRange_UMSPosition);
			}
			return _mem + position;
		}
		set
		{
			if (_buffer != null)
			{
				throw new NotSupportedException(SR.NotSupported_UmsSafeBuffer);
			}
			EnsureNotClosed();
			if (value < _mem)
			{
				throw new IOException(SR.IO_SeekBeforeBegin);
			}
			long num = (long)value - (long)_mem;
			if (num < 0)
			{
				throw new ArgumentOutOfRangeException("value", SR.ArgumentOutOfRange_UnmanagedMemStreamLength);
			}
			_position = num;
		}
	}

	protected UnmanagedMemoryStream()
	{
	}

	public UnmanagedMemoryStream(SafeBuffer buffer, long offset, long length)
	{
		Initialize(buffer, offset, length, FileAccess.Read);
	}

	public UnmanagedMemoryStream(SafeBuffer buffer, long offset, long length, FileAccess access)
	{
		Initialize(buffer, offset, length, access);
	}

	protected unsafe void Initialize(SafeBuffer buffer, long offset, long length, FileAccess access)
	{
		ArgumentNullException.ThrowIfNull(buffer, "buffer");
		ArgumentOutOfRangeException.ThrowIfNegative(offset, "offset");
		ArgumentOutOfRangeException.ThrowIfNegative(length, "length");
		if (buffer.ByteLength < (ulong)(offset + length))
		{
			throw new ArgumentException(SR.Argument_InvalidSafeBufferOffLen);
		}
		if (access < FileAccess.Read || access > FileAccess.ReadWrite)
		{
			throw new ArgumentOutOfRangeException("access");
		}
		if (_isOpen)
		{
			throw new InvalidOperationException(SR.InvalidOperation_CalledTwice);
		}
		byte* pointer = null;
		try
		{
			buffer.AcquirePointer(ref pointer);
			if (pointer + offset + length < pointer)
			{
				throw new ArgumentException(SR.ArgumentOutOfRange_UnmanagedMemStreamWrapAround);
			}
		}
		finally
		{
			if (pointer != null)
			{
				buffer.ReleasePointer();
			}
		}
		_offset = (nuint)offset;
		_buffer = buffer;
		_length = (nuint)length;
		_capacity = (nuint)length;
		_access = access;
		_isOpen = true;
	}

	[CLSCompliant(false)]
	public unsafe UnmanagedMemoryStream(byte* pointer, long length)
	{
		Initialize(pointer, length, length, FileAccess.Read);
	}

	[CLSCompliant(false)]
	public unsafe UnmanagedMemoryStream(byte* pointer, long length, long capacity, FileAccess access)
	{
		Initialize(pointer, length, capacity, access);
	}

	[CLSCompliant(false)]
	protected unsafe void Initialize(byte* pointer, long length, long capacity, FileAccess access)
	{
		ArgumentNullException.ThrowIfNull(pointer, "pointer");
		ArgumentOutOfRangeException.ThrowIfNegative(length, "length");
		ArgumentOutOfRangeException.ThrowIfNegative(capacity, "capacity");
		if (length > capacity)
		{
			throw new ArgumentOutOfRangeException("length", SR.ArgumentOutOfRange_LengthGreaterThanCapacity);
		}
		if ((nuint)((long)pointer + capacity) < (nuint)pointer)
		{
			throw new ArgumentOutOfRangeException("capacity", SR.ArgumentOutOfRange_UnmanagedMemStreamWrapAround);
		}
		if (access < FileAccess.Read || access > FileAccess.ReadWrite)
		{
			throw new ArgumentOutOfRangeException("access", SR.ArgumentOutOfRange_Enum);
		}
		if (_isOpen)
		{
			throw new InvalidOperationException(SR.InvalidOperation_CalledTwice);
		}
		_mem = pointer;
		_offset = 0u;
		_length = (nuint)length;
		_capacity = (nuint)capacity;
		_access = access;
		_isOpen = true;
	}

	protected unsafe override void Dispose(bool disposing)
	{
		_isOpen = false;
		_mem = null;
		base.Dispose(disposing);
	}

	private void EnsureNotClosed()
	{
		if (!_isOpen)
		{
			ThrowHelper.ThrowObjectDisposedException_StreamClosed(null);
		}
	}

	private void EnsureReadable()
	{
		if (!CanRead)
		{
			ThrowHelper.ThrowNotSupportedException_UnreadableStream();
		}
	}

	private void EnsureWriteable()
	{
		if (!CanWrite)
		{
			ThrowHelper.ThrowNotSupportedException_UnwritableStream();
		}
	}

	public override void Flush()
	{
		EnsureNotClosed();
	}

	public override Task FlushAsync(CancellationToken cancellationToken)
	{
		if (cancellationToken.IsCancellationRequested)
		{
			return Task.FromCanceled(cancellationToken);
		}
		try
		{
			Flush();
			return Task.CompletedTask;
		}
		catch (Exception exception)
		{
			return Task.FromException(exception);
		}
	}

	public override int Read(byte[] buffer, int offset, int count)
	{
		Stream.ValidateBufferArguments(buffer, offset, count);
		return ReadCore(new Span<byte>(buffer, offset, count));
	}

	public override int Read(Span<byte> buffer)
	{
		if (GetType() == typeof(UnmanagedMemoryStream))
		{
			return ReadCore(buffer);
		}
		return base.Read(buffer);
	}

	internal unsafe int ReadCore(Span<byte> buffer)
	{
		EnsureNotClosed();
		EnsureReadable();
		long position = _position;
		long num = (long)Volatile.Read(ref _length);
		long num2 = Math.Min(num - position, buffer.Length);
		if (num2 <= 0)
		{
			return 0;
		}
		int num3 = (int)num2;
		if (num3 < 0)
		{
			return 0;
		}
		if (_buffer != null)
		{
			byte* pointer = null;
			try
			{
				_buffer.AcquirePointer(ref pointer);
				Buffer.Memmove(ref MemoryMarshal.GetReference(buffer), ref (pointer + position)[_offset], (nuint)num3);
			}
			finally
			{
				if (pointer != null)
				{
					_buffer.ReleasePointer();
				}
			}
		}
		else
		{
			Buffer.Memmove(ref MemoryMarshal.GetReference(buffer), ref _mem[position], (nuint)num3);
		}
		_position = position + num2;
		return num3;
	}

	public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
	{
		Stream.ValidateBufferArguments(buffer, offset, count);
		if (cancellationToken.IsCancellationRequested)
		{
			return Task.FromCanceled<int>(cancellationToken);
		}
		try
		{
			int result = Read(buffer, offset, count);
			return _lastReadTask.GetTask(result);
		}
		catch (Exception exception)
		{
			return Task.FromException<int>(exception);
		}
	}

	public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default(CancellationToken))
	{
		if (cancellationToken.IsCancellationRequested)
		{
			return ValueTask.FromCanceled<int>(cancellationToken);
		}
		try
		{
			ArraySegment<byte> segment;
			return new ValueTask<int>(MemoryMarshal.TryGetArray((ReadOnlyMemory<byte>)buffer, out segment) ? Read(segment.Array, segment.Offset, segment.Count) : Read(buffer.Span));
		}
		catch (Exception exception)
		{
			return ValueTask.FromException<int>(exception);
		}
	}

	public unsafe override int ReadByte()
	{
		EnsureNotClosed();
		EnsureReadable();
		long position = _position;
		long num = (long)Volatile.Read(ref _length);
		if (position >= num)
		{
			return -1;
		}
		_position = position + 1;
		if (_buffer != null)
		{
			byte* pointer = null;
			try
			{
				_buffer.AcquirePointer(ref pointer);
				return (pointer + position)[_offset];
			}
			finally
			{
				if (pointer != null)
				{
					_buffer.ReleasePointer();
				}
			}
		}
		return _mem[position];
	}

	public override long Seek(long offset, SeekOrigin loc)
	{
		EnsureNotClosed();
		long num;
		switch (loc)
		{
		case SeekOrigin.Begin:
			num = offset;
			if (num < 0)
			{
				throw new IOException(SR.IO_SeekBeforeBegin);
			}
			break;
		case SeekOrigin.Current:
			num = _position + offset;
			if (num < 0)
			{
				throw new IOException(SR.IO_SeekBeforeBegin);
			}
			break;
		case SeekOrigin.End:
			num = (long)_length + offset;
			if (num < 0)
			{
				throw new IOException(SR.IO_SeekBeforeBegin);
			}
			break;
		default:
			throw new ArgumentException(SR.Argument_InvalidSeekOrigin);
		}
		_position = num;
		return num;
	}

	public unsafe override void SetLength(long value)
	{
		ArgumentOutOfRangeException.ThrowIfNegative(value, "value");
		if (_buffer != null)
		{
			throw new NotSupportedException(SR.NotSupported_UmsSafeBuffer);
		}
		EnsureNotClosed();
		EnsureWriteable();
		if (value > (long)_capacity)
		{
			throw new IOException(SR.IO_FixedCapacity);
		}
		long num = (long)_length;
		if (value > num)
		{
			NativeMemory.Clear(_mem + num, (nuint)(value - num));
		}
		Volatile.Write(ref _length, (nuint)value);
		if (_position > value)
		{
			_position = value;
		}
	}

	public override void Write(byte[] buffer, int offset, int count)
	{
		Stream.ValidateBufferArguments(buffer, offset, count);
		WriteCore(new ReadOnlySpan<byte>(buffer, offset, count));
	}

	public override void Write(ReadOnlySpan<byte> buffer)
	{
		if (GetType() == typeof(UnmanagedMemoryStream))
		{
			WriteCore(buffer);
		}
		else
		{
			base.Write(buffer);
		}
	}

	internal unsafe void WriteCore(ReadOnlySpan<byte> buffer)
	{
		EnsureNotClosed();
		EnsureWriteable();
		long position = _position;
		long num = (long)_length;
		long num2 = position + buffer.Length;
		if (num2 < 0)
		{
			throw new IOException(SR.IO_StreamTooLong);
		}
		if (num2 > (long)_capacity)
		{
			throw new NotSupportedException(SR.IO_FixedCapacity);
		}
		if (_buffer == null)
		{
			if (position > num)
			{
				NativeMemory.Clear(_mem + num, (nuint)(position - num));
			}
			if (num2 > num)
			{
				Volatile.Write(ref _length, (nuint)num2);
			}
		}
		if (_buffer != null)
		{
			long num3 = (long)_capacity - position;
			if (num3 < buffer.Length)
			{
				throw new ArgumentException(SR.Arg_BufferTooSmall);
			}
			byte* pointer = null;
			try
			{
				_buffer.AcquirePointer(ref pointer);
				Buffer.Memmove(ref (pointer + position)[_offset], ref MemoryMarshal.GetReference(buffer), (nuint)buffer.Length);
			}
			finally
			{
				if (pointer != null)
				{
					_buffer.ReleasePointer();
				}
			}
		}
		else
		{
			Buffer.Memmove(ref _mem[position], ref MemoryMarshal.GetReference(buffer), (nuint)buffer.Length);
		}
		_position = num2;
	}

	public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
	{
		Stream.ValidateBufferArguments(buffer, offset, count);
		if (cancellationToken.IsCancellationRequested)
		{
			return Task.FromCanceled(cancellationToken);
		}
		try
		{
			Write(buffer, offset, count);
			return Task.CompletedTask;
		}
		catch (Exception exception)
		{
			return Task.FromException(exception);
		}
	}

	public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default(CancellationToken))
	{
		if (cancellationToken.IsCancellationRequested)
		{
			return ValueTask.FromCanceled(cancellationToken);
		}
		try
		{
			if (MemoryMarshal.TryGetArray(buffer, out var segment))
			{
				Write(segment.Array, segment.Offset, segment.Count);
			}
			else
			{
				Write(buffer.Span);
			}
			return default(ValueTask);
		}
		catch (Exception exception)
		{
			return ValueTask.FromException(exception);
		}
	}

	public unsafe override void WriteByte(byte value)
	{
		EnsureNotClosed();
		EnsureWriteable();
		long position = _position;
		long num = (long)_length;
		long num2 = position + 1;
		if (position >= num)
		{
			if (num2 < 0)
			{
				throw new IOException(SR.IO_StreamTooLong);
			}
			if (num2 > (long)_capacity)
			{
				throw new NotSupportedException(SR.IO_FixedCapacity);
			}
			if (_buffer == null)
			{
				if (position > num)
				{
					NativeMemory.Clear(_mem + num, (nuint)(position - num));
				}
				Volatile.Write(ref _length, (nuint)num2);
			}
		}
		if (_buffer != null)
		{
			byte* pointer = null;
			try
			{
				_buffer.AcquirePointer(ref pointer);
				(pointer + position)[_offset] = value;
			}
			finally
			{
				if (pointer != null)
				{
					_buffer.ReleasePointer();
				}
			}
		}
		else
		{
			_mem[position] = value;
		}
		_position = num2;
	}
}
