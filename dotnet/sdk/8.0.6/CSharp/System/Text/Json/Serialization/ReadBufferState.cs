using System.Buffers;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace System.Text.Json.Serialization;

[StructLayout(LayoutKind.Auto)]
internal struct ReadBufferState : IDisposable
{
	private byte[] _buffer;

	private byte _offset;

	private int _count;

	private int _maxCount;

	private bool _isFirstBlock;

	private bool _isFinalBlock;

	public bool IsFinalBlock => _isFinalBlock;

	public ReadOnlySpan<byte> Bytes => _buffer.AsSpan(_offset, _count);

	public ReadBufferState(int initialBufferSize)
	{
		_buffer = ArrayPool<byte>.Shared.Rent(Math.Max(initialBufferSize, JsonConstants.Utf8Bom.Length));
		_maxCount = (_count = (_offset = 0));
		_isFirstBlock = true;
		_isFinalBlock = false;
	}

	public readonly async ValueTask<ReadBufferState> ReadFromStreamAsync(Stream utf8Json, CancellationToken cancellationToken, bool fillBuffer = true)
	{
		ReadBufferState bufferState = this;
		do
		{
			int num = await utf8Json.ReadAsync(bufferState._buffer.AsMemory(bufferState._count), cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
			if (num == 0)
			{
				bufferState._isFinalBlock = true;
				break;
			}
			bufferState._count += num;
		}
		while (fillBuffer && bufferState._count < bufferState._buffer.Length);
		bufferState.ProcessReadBytes();
		return bufferState;
	}

	public void ReadFromStream(Stream utf8Json)
	{
		do
		{
			int num = utf8Json.Read(_buffer.AsSpan(_count));
			if (num == 0)
			{
				_isFinalBlock = true;
				break;
			}
			_count += num;
		}
		while (_count < _buffer.Length);
		ProcessReadBytes();
	}

	public void AdvanceBuffer(int bytesConsumed)
	{
		_count -= bytesConsumed;
		if (!_isFinalBlock)
		{
			if ((uint)_count > (uint)_buffer.Length / 2u)
			{
				byte[] buffer = _buffer;
				int maxCount = _maxCount;
				byte[] array = ArrayPool<byte>.Shared.Rent((_buffer.Length < 1073741823) ? (_buffer.Length * 2) : int.MaxValue);
				Buffer.BlockCopy(buffer, _offset + bytesConsumed, array, 0, _count);
				_buffer = array;
				_maxCount = _count;
				new Span<byte>(buffer, 0, maxCount).Clear();
				ArrayPool<byte>.Shared.Return(buffer);
			}
			else if (_count != 0)
			{
				Buffer.BlockCopy(_buffer, _offset + bytesConsumed, _buffer, 0, _count);
			}
		}
		_offset = 0;
	}

	private void ProcessReadBytes()
	{
		if (_count > _maxCount)
		{
			_maxCount = _count;
		}
		if (_isFirstBlock)
		{
			_isFirstBlock = false;
			if (_buffer.AsSpan(0, _count).StartsWith(JsonConstants.Utf8Bom))
			{
				_offset = (byte)JsonConstants.Utf8Bom.Length;
				_count -= JsonConstants.Utf8Bom.Length;
			}
		}
	}

	public void Dispose()
	{
		new Span<byte>(_buffer, 0, _maxCount).Clear();
		byte[] buffer = _buffer;
		_buffer = null;
		ArrayPool<byte>.Shared.Return(buffer);
	}
}
