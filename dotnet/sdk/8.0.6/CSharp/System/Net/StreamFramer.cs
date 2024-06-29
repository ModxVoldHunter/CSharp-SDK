using System.Globalization;
using System.IO;
using System.Net.Security;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net;

internal sealed class StreamFramer
{
	private readonly FrameHeader _writeHeader = new FrameHeader();

	private readonly FrameHeader _curReadHeader = new FrameHeader();

	private readonly byte[] _readHeaderBuffer = new byte[5];

	private readonly byte[] _writeHeaderBuffer = new byte[5];

	private bool _eof;

	public FrameHeader ReadHeader => _curReadHeader;

	public FrameHeader WriteHeader => _writeHeader;

	public async ValueTask<byte[]> ReadMessageAsync<TAdapter>(Stream stream, CancellationToken cancellationToken) where TAdapter : IReadWriteAdapter
	{
		if (_eof)
		{
			return null;
		}
		byte[] buffer = _readHeaderBuffer;
		int num = await TAdapter.ReadAtLeastAsync(stream, buffer, buffer.Length, throwOnEndOfStream: false, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
		if (num < buffer.Length)
		{
			if (num == 0)
			{
				_eof = true;
				return null;
			}
			throw new IOException(System.SR.Format(System.SR.net_io_readfailure, System.SR.net_io_connectionclosed));
		}
		_curReadHeader.CopyFrom(buffer, 0);
		if (_curReadHeader.PayloadSize > 65535)
		{
			throw new InvalidOperationException(System.SR.Format(System.SR.net_frame_size, 65535, _curReadHeader.PayloadSize.ToString(NumberFormatInfo.InvariantInfo)));
		}
		buffer = new byte[_curReadHeader.PayloadSize];
		if (buffer.Length != 0 && await TAdapter.ReadAtLeastAsync(stream, buffer, buffer.Length, throwOnEndOfStream: false, cancellationToken).ConfigureAwait(continueOnCapturedContext: false) < buffer.Length)
		{
			throw new IOException(System.SR.Format(System.SR.net_io_readfailure, System.SR.net_io_connectionclosed));
		}
		return buffer;
	}

	public async Task WriteMessageAsync<TAdapter>(Stream stream, byte[] message, CancellationToken cancellationToken) where TAdapter : IReadWriteAdapter
	{
		_writeHeader.PayloadSize = message.Length;
		_writeHeader.CopyTo(_writeHeaderBuffer, 0);
		await TAdapter.WriteAsync(stream, _writeHeaderBuffer, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
		if (message.Length != 0)
		{
			await TAdapter.WriteAsync(stream, message, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
		}
	}
}
