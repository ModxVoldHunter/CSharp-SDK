using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Http.Json;

internal sealed class LengthLimitReadStream : Stream
{
	private readonly Stream _innerStream;

	private readonly int _lengthLimit;

	private int _remainingLength;

	public override bool CanRead => _innerStream.CanRead;

	public override bool CanSeek => _innerStream.CanSeek;

	public override bool CanWrite => false;

	public override long Length => _innerStream.Length;

	public override long Position
	{
		get
		{
			return _innerStream.Position;
		}
		set
		{
			_innerStream.Position = value;
		}
	}

	public LengthLimitReadStream(Stream innerStream, int lengthLimit)
	{
		_innerStream = innerStream;
		_lengthLimit = (_remainingLength = lengthLimit);
	}

	private void CheckLengthLimit(int read)
	{
		_remainingLength -= read;
		if (_remainingLength < 0)
		{
			ThrowExceededBufferLimit(_lengthLimit);
		}
	}

	internal static void ThrowExceededBufferLimit(int limit)
	{
		throw new HttpRequestException(System.SR.Format(System.SR.net_http_content_buffersize_exceeded, limit));
	}

	public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
	{
		return ReadAsync(new Memory<byte>(buffer, offset, count), cancellationToken).AsTask();
	}

	public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default(CancellationToken))
	{
		ValueTask<int> valueTask = _innerStream.ReadAsync(buffer, cancellationToken);
		if (buffer.IsEmpty)
		{
			return valueTask;
		}
		if (valueTask.IsCompletedSuccessfully)
		{
			int result = valueTask.Result;
			CheckLengthLimit(result);
			return new ValueTask<int>(result);
		}
		return Core(valueTask);
		async ValueTask<int> Core(ValueTask<int> readTask)
		{
			int num = await readTask.ConfigureAwait(continueOnCapturedContext: false);
			CheckLengthLimit(num);
			return num;
		}
	}

	public override int Read(byte[] buffer, int offset, int count)
	{
		int num = _innerStream.Read(buffer, offset, count);
		CheckLengthLimit(num);
		return num;
	}

	public override void Flush()
	{
		_innerStream.Flush();
	}

	public override Task FlushAsync(CancellationToken cancellationToken)
	{
		return _innerStream.FlushAsync(cancellationToken);
	}

	public override long Seek(long offset, SeekOrigin origin)
	{
		return _innerStream.Seek(offset, origin);
	}

	public override void SetLength(long value)
	{
		_innerStream.SetLength(value);
	}

	public override void Write(byte[] buffer, int offset, int count)
	{
		throw new NotSupportedException();
	}
}
