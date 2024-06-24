using System.Buffers;
using System.IO.Compression;

namespace System.Net.WebSockets.Compression;

internal sealed class WebSocketDeflater : IDisposable
{
	private readonly int _windowBits;

	private ZLibNative.ZLibStreamHandle _stream;

	private readonly bool _persisted;

	private byte[] _buffer;

	internal WebSocketDeflater(int windowBits, bool persisted)
	{
		_windowBits = -windowBits;
		_persisted = persisted;
	}

	public void Dispose()
	{
		_stream?.Dispose();
	}

	public void ReleaseBuffer()
	{
		byte[] buffer = _buffer;
		if (buffer != null)
		{
			_buffer = null;
			ArrayPool<byte>.Shared.Return(buffer);
		}
	}

	public ReadOnlySpan<byte> Deflate(ReadOnlySpan<byte> payload, bool endOfMessage)
	{
		_buffer = ArrayPool<byte>.Shared.Rent(Math.Max(payload.Length, 4096));
		int num = 0;
		while (true)
		{
			DeflatePrivate(payload, _buffer.AsSpan(num), endOfMessage, out var consumed, out var written, out var needsMoreOutput);
			num += written;
			if (!needsMoreOutput)
			{
				break;
			}
			payload = payload.Slice(consumed);
			byte[] array = ArrayPool<byte>.Shared.Rent((int)((double)_buffer.Length * 1.3));
			_buffer.AsSpan(0, num).CopyTo(array);
			byte[] buffer = _buffer;
			_buffer = array;
			ArrayPool<byte>.Shared.Return(buffer);
		}
		return new ReadOnlySpan<byte>(_buffer, 0, num);
	}

	private void DeflatePrivate(ReadOnlySpan<byte> payload, Span<byte> output, bool endOfMessage, out int consumed, out int written, out bool needsMoreOutput)
	{
		if (_stream == null)
		{
			_stream = CreateDeflater();
		}
		if (payload.Length == 0)
		{
			consumed = 0;
			written = 0;
		}
		else
		{
			UnsafeDeflate(payload, output, out consumed, out written, out needsMoreOutput);
			if (needsMoreOutput)
			{
				return;
			}
		}
		written += UnsafeFlush(output.Slice(written), out needsMoreOutput);
		if (!needsMoreOutput)
		{
			if (endOfMessage)
			{
				written -= 4;
			}
			if (endOfMessage && !_persisted)
			{
				_stream.Dispose();
				_stream = null;
			}
		}
	}

	private unsafe void UnsafeDeflate(ReadOnlySpan<byte> input, Span<byte> output, out int consumed, out int written, out bool needsMoreBuffer)
	{
		fixed (byte* nextIn = input)
		{
			fixed (byte* nextOut = output)
			{
				_stream.NextIn = (nint)nextIn;
				_stream.AvailIn = (uint)input.Length;
				_stream.NextOut = (nint)nextOut;
				_stream.AvailOut = (uint)output.Length;
				ZLibNative.ErrorCode errorCode = Deflate(_stream, ZLibNative.FlushCode.NoFlush);
				consumed = input.Length - (int)_stream.AvailIn;
				written = output.Length - (int)_stream.AvailOut;
				needsMoreBuffer = errorCode == ZLibNative.ErrorCode.BufError || _stream.AvailIn != 0 || written == output.Length;
			}
		}
	}

	private unsafe int UnsafeFlush(Span<byte> output, out bool needsMoreBuffer)
	{
		fixed (byte* nextOut = output)
		{
			_stream.NextIn = IntPtr.Zero;
			_stream.AvailIn = 0u;
			_stream.NextOut = (nint)nextOut;
			_stream.AvailOut = (uint)output.Length;
			ZLibNative.ErrorCode errorCode = Deflate(_stream, ZLibNative.FlushCode.Block);
			needsMoreBuffer = _stream.AvailOut < 6;
			if (!needsMoreBuffer)
			{
				errorCode = Deflate(_stream, ZLibNative.FlushCode.SyncFlush);
			}
			return output.Length - (int)_stream.AvailOut;
		}
	}

	private static ZLibNative.ErrorCode Deflate(ZLibNative.ZLibStreamHandle stream, ZLibNative.FlushCode flushCode)
	{
		ZLibNative.ErrorCode errorCode = stream.Deflate(flushCode);
		if ((errorCode == ZLibNative.ErrorCode.BufError || (uint)errorCode <= 1u) ? true : false)
		{
			return errorCode;
		}
		string message = ((errorCode == ZLibNative.ErrorCode.StreamError) ? System.SR.ZLibErrorInconsistentStream : System.SR.Format(System.SR.ZLibErrorUnexpected, (int)errorCode));
		throw new WebSocketException(message);
	}

	private ZLibNative.ZLibStreamHandle CreateDeflater()
	{
		ZLibNative.ZLibStreamHandle zLibStreamHandle = null;
		ZLibNative.ErrorCode errorCode;
		try
		{
			errorCode = ZLibNative.CreateZLibStreamForDeflate(out zLibStreamHandle, ZLibNative.CompressionLevel.DefaultCompression, _windowBits, 8, ZLibNative.CompressionStrategy.DefaultStrategy);
		}
		catch (Exception innerException)
		{
			zLibStreamHandle?.Dispose();
			throw new WebSocketException(System.SR.ZLibErrorDLLLoadError, innerException);
		}
		if (errorCode == ZLibNative.ErrorCode.Ok)
		{
			return zLibStreamHandle;
		}
		zLibStreamHandle.Dispose();
		string message = ((errorCode == ZLibNative.ErrorCode.MemError) ? System.SR.ZLibErrorNotEnoughMemory : System.SR.Format(System.SR.ZLibErrorUnexpected, (int)errorCode));
		throw new WebSocketException(message);
	}
}
