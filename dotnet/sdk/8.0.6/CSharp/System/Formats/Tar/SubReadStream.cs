using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace System.Formats.Tar;

internal class SubReadStream : Stream
{
	protected bool _hasReachedEnd;

	protected readonly long _startInSuperStream;

	protected long _positionInSuperStream;

	protected readonly long _endInSuperStream;

	protected readonly Stream _superStream;

	protected bool _isDisposed;

	public override long Length
	{
		get
		{
			ThrowIfDisposed();
			return _endInSuperStream - _startInSuperStream;
		}
	}

	public override long Position
	{
		get
		{
			ThrowIfDisposed();
			return _positionInSuperStream - _startInSuperStream;
		}
		set
		{
			ThrowIfDisposed();
			throw new InvalidOperationException(System.SR.IO_NotSupported_UnseekableStream);
		}
	}

	public override bool CanRead => !_isDisposed;

	public override bool CanSeek => false;

	public override bool CanWrite => false;

	internal bool HasReachedEnd
	{
		get
		{
			if (!_hasReachedEnd && _positionInSuperStream > _endInSuperStream)
			{
				_hasReachedEnd = true;
			}
			return _hasReachedEnd;
		}
		set
		{
			if (value)
			{
				_hasReachedEnd = true;
			}
		}
	}

	public SubReadStream(Stream superStream, long startPosition, long maxLength)
	{
		if (!superStream.CanRead)
		{
			throw new ArgumentException(System.SR.IO_NotSupported_UnreadableStream, "superStream");
		}
		_startInSuperStream = startPosition;
		_positionInSuperStream = startPosition;
		_endInSuperStream = startPosition + maxLength;
		_superStream = superStream;
		_isDisposed = false;
		_hasReachedEnd = false;
	}

	protected void ThrowIfDisposed()
	{
		ObjectDisposedException.ThrowIf(_isDisposed, this);
	}

	private void ThrowIfBeyondEndOfStream()
	{
		if (HasReachedEnd)
		{
			throw new EndOfStreamException();
		}
	}

	public override int Read(byte[] buffer, int offset, int count)
	{
		Stream.ValidateBufferArguments(buffer, offset, count);
		return Read(buffer.AsSpan(offset, count));
	}

	public override int Read(Span<byte> destination)
	{
		ThrowIfDisposed();
		ThrowIfBeyondEndOfStream();
		int length = destination.Length;
		int num = destination.Length;
		if (_positionInSuperStream + num > _endInSuperStream)
		{
			num = (int)(_endInSuperStream - _positionInSuperStream);
		}
		int num2 = _superStream.Read(destination.Slice(0, num));
		_positionInSuperStream += num2;
		return num2;
	}

	public override int ReadByte()
	{
		byte reference = 0;
		if (Read(new Span<byte>(ref reference)) != 1)
		{
			return -1;
		}
		return reference;
	}

	public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
	{
		if (cancellationToken.IsCancellationRequested)
		{
			return Task.FromCanceled<int>(cancellationToken);
		}
		Stream.ValidateBufferArguments(buffer, offset, count);
		return ReadAsync(new Memory<byte>(buffer, offset, count), cancellationToken).AsTask();
	}

	public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default(CancellationToken))
	{
		if (cancellationToken.IsCancellationRequested)
		{
			return ValueTask.FromCanceled<int>(cancellationToken);
		}
		ThrowIfDisposed();
		ThrowIfBeyondEndOfStream();
		return ReadAsyncCore(buffer, cancellationToken);
	}

	protected async ValueTask<int> ReadAsyncCore(Memory<byte> buffer, CancellationToken cancellationToken)
	{
		cancellationToken.ThrowIfCancellationRequested();
		if (_positionInSuperStream > _endInSuperStream - buffer.Length)
		{
			buffer = buffer.Slice(0, (int)(_endInSuperStream - _positionInSuperStream));
		}
		int num = await _superStream.ReadAsync(buffer, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
		_positionInSuperStream += num;
		return num;
	}

	public override long Seek(long offset, SeekOrigin origin)
	{
		throw new NotSupportedException(System.SR.IO_NotSupported_UnseekableStream);
	}

	public override void SetLength(long value)
	{
		throw new NotSupportedException(System.SR.IO_NotSupported_UnseekableStream);
	}

	public override void Write(byte[] buffer, int offset, int count)
	{
		throw new NotSupportedException(System.SR.IO_NotSupported_UnwritableStream);
	}

	public override void Flush()
	{
	}

	public override Task FlushAsync(CancellationToken cancellationToken)
	{
		if (!cancellationToken.IsCancellationRequested)
		{
			return Task.CompletedTask;
		}
		return Task.FromCanceled(cancellationToken);
	}

	protected override void Dispose(bool disposing)
	{
		_isDisposed = true;
		base.Dispose(disposing);
	}
}
