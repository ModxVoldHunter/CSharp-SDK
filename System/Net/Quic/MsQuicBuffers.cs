using System.Collections.Generic;
using System.Runtime.InteropServices;
using Microsoft.Quic;

namespace System.Net.Quic;

internal struct MsQuicBuffers : IDisposable
{
	private unsafe QUIC_BUFFER* _buffers;

	private int _count;

	public unsafe QUIC_BUFFER* Buffers => _buffers;

	public int Count => _count;

	public unsafe MsQuicBuffers()
	{
		_buffers = null;
		_count = 0;
	}

	private unsafe void FreeNativeMemory()
	{
		QUIC_BUFFER* buffers = _buffers;
		_buffers = null;
		NativeMemory.Free(buffers);
		_count = 0;
	}

	private unsafe void Reserve(int count)
	{
		if (count > _count)
		{
			FreeNativeMemory();
			_buffers = (QUIC_BUFFER*)NativeMemory.AllocZeroed((nuint)count, (nuint)sizeof(QUIC_BUFFER));
			_count = count;
		}
	}

	private unsafe void SetBuffer(int index, ReadOnlyMemory<byte> buffer)
	{
		_buffers[index].Buffer = (byte*)NativeMemory.Alloc((nuint)buffer.Length, 1u);
		_buffers[index].Length = (uint)buffer.Length;
		buffer.Span.CopyTo(_buffers[index].Span);
	}

	public void Initialize<T>(IList<T> inputs, Func<T, ReadOnlyMemory<byte>> toBuffer)
	{
		Reserve(inputs.Count);
		for (int i = 0; i < inputs.Count; i++)
		{
			SetBuffer(i, toBuffer(inputs[i]));
		}
	}

	public void Initialize(ReadOnlyMemory<byte> buffer)
	{
		Reserve(1);
		SetBuffer(0, buffer);
	}

	public unsafe void Reset()
	{
		for (int i = 0; i < _count && _buffers[i].Buffer != null; i++)
		{
			byte* buffer = _buffers[i].Buffer;
			_buffers[i].Buffer = null;
			NativeMemory.Free(buffer);
			_buffers[i].Length = 0u;
		}
	}

	public void Dispose()
	{
		Reset();
		FreeNativeMemory();
	}
}
