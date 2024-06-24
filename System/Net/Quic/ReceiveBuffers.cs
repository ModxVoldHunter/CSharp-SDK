using Microsoft.Quic;

namespace System.Net.Quic;

internal struct ReceiveBuffers
{
	private readonly object _syncRoot;

	private MultiArrayBuffer _buffer;

	private bool _final;

	public ReceiveBuffers()
	{
		_syncRoot = new object();
		_buffer = default(MultiArrayBuffer);
		_final = false;
	}

	public void SetFinal()
	{
		lock (_syncRoot)
		{
			_final = true;
		}
	}

	public bool HasCapacity()
	{
		lock (_syncRoot)
		{
			return _buffer.ActiveMemory.Length < 65536;
		}
	}

	public int CopyFrom(ReadOnlySpan<QUIC_BUFFER> quicBuffers, int totalLength, bool final)
	{
		lock (_syncRoot)
		{
			if (_buffer.ActiveMemory.Length > 65536 - totalLength)
			{
				totalLength = 65536 - _buffer.ActiveMemory.Length;
				final = false;
			}
			_final = final;
			_buffer.EnsureAvailableSpace(totalLength);
			int num = 0;
			for (int i = 0; i < quicBuffers.Length; i++)
			{
				Span<byte> span = quicBuffers[i].Span;
				if (totalLength < span.Length)
				{
					span = span.Slice(0, totalLength);
				}
				_buffer.AvailableMemory.CopyFrom(span);
				_buffer.Commit(span.Length);
				num += span.Length;
				totalLength -= span.Length;
			}
			return num;
		}
	}

	public int CopyTo(Memory<byte> buffer, out bool isCompleted, out bool isEmpty)
	{
		lock (_syncRoot)
		{
			int num = 0;
			if (!_buffer.IsEmpty)
			{
				MultiMemory activeMemory = _buffer.ActiveMemory;
				num = Math.Min(buffer.Length, activeMemory.Length);
				activeMemory.Slice(0, num).CopyTo(buffer.Span);
				_buffer.Discard(num);
			}
			isCompleted = _buffer.IsEmpty && _final;
			isEmpty = _buffer.IsEmpty;
			return num;
		}
	}
}
