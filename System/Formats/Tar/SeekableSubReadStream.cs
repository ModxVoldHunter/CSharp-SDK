using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace System.Formats.Tar;

internal sealed class SeekableSubReadStream : SubReadStream
{
	public override bool CanSeek => !_isDisposed;

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
			ArgumentOutOfRangeException.ThrowIfNegative(value, "value");
			ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(value, _endInSuperStream, "value");
			_positionInSuperStream = _startInSuperStream + value;
		}
	}

	public SeekableSubReadStream(Stream superStream, long startPosition, long maxLength)
		: base(superStream, startPosition, maxLength)
	{
		if (!superStream.CanSeek)
		{
			throw new ArgumentException(System.SR.IO_NotSupported_UnseekableStream, "superStream");
		}
	}

	public override int Read(Span<byte> destination)
	{
		ThrowIfDisposed();
		VerifyPositionInSuperStream();
		int length = destination.Length;
		int num = destination.Length;
		if ((ulong)(_positionInSuperStream + num) > (ulong)_endInSuperStream)
		{
			num = Math.Max(0, (int)(_endInSuperStream - _positionInSuperStream));
		}
		if (num > 0)
		{
			int num2 = _superStream.Read(destination.Slice(0, num));
			_positionInSuperStream += num2;
			return num2;
		}
		return 0;
	}

	public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default(CancellationToken))
	{
		if (cancellationToken.IsCancellationRequested)
		{
			return ValueTask.FromCanceled<int>(cancellationToken);
		}
		ThrowIfDisposed();
		VerifyPositionInSuperStream();
		return ReadAsyncCore(buffer, cancellationToken);
	}

	public override long Seek(long offset, SeekOrigin origin)
	{
		ThrowIfDisposed();
		long num = origin switch
		{
			SeekOrigin.Begin => _startInSuperStream + offset, 
			SeekOrigin.Current => _positionInSuperStream + offset, 
			SeekOrigin.End => _endInSuperStream + offset, 
			_ => throw new ArgumentOutOfRangeException("origin"), 
		};
		if (num < _startInSuperStream)
		{
			throw new IOException(System.SR.IO_SeekBeforeBegin);
		}
		_positionInSuperStream = num;
		return _positionInSuperStream - _startInSuperStream;
	}

	private void VerifyPositionInSuperStream()
	{
		if (_positionInSuperStream != _superStream.Position)
		{
			_superStream.Seek(_positionInSuperStream, SeekOrigin.Begin);
		}
	}
}
