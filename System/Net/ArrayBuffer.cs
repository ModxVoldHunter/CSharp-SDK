using System.Buffers;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System.Net;

[StructLayout(LayoutKind.Auto)]
internal struct ArrayBuffer : IDisposable
{
	private readonly bool _usePool;

	private byte[] _bytes;

	private int _activeStart;

	private int _availableStart;

	public int ActiveLength => _availableStart - _activeStart;

	public Span<byte> ActiveSpan => new Span<byte>(_bytes, _activeStart, _availableStart - _activeStart);

	public int AvailableLength => _bytes.Length - _availableStart;

	public Memory<byte> AvailableMemory => _bytes.AsMemory(_availableStart);

	public ArrayBuffer(int initialSize, bool usePool = false)
	{
		_usePool = usePool;
		_bytes = ((initialSize == 0) ? Array.Empty<byte>() : (usePool ? ArrayPool<byte>.Shared.Rent(initialSize) : new byte[initialSize]));
		_activeStart = 0;
		_availableStart = 0;
	}

	public void Dispose()
	{
		_activeStart = 0;
		_availableStart = 0;
		byte[] bytes = _bytes;
		_bytes = null;
		if (bytes != null)
		{
			ReturnBufferIfPooled(bytes);
		}
	}

	public void ClearAndReturnBuffer()
	{
		_activeStart = 0;
		_availableStart = 0;
		byte[] bytes = _bytes;
		_bytes = Array.Empty<byte>();
		ReturnBufferIfPooled(bytes);
	}

	public void Discard(int byteCount)
	{
		_activeStart += byteCount;
		if (_activeStart == _availableStart)
		{
			_activeStart = 0;
			_availableStart = 0;
		}
	}

	public void Commit(int byteCount)
	{
		_availableStart += byteCount;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void EnsureAvailableSpace(int byteCount)
	{
		if (byteCount > AvailableLength)
		{
			EnsureAvailableSpaceCore(byteCount);
		}
	}

	private void EnsureAvailableSpaceCore(int byteCount)
	{
		if (_bytes.Length == 0)
		{
			_bytes = ArrayPool<byte>.Shared.Rent(byteCount);
			return;
		}
		int num = _activeStart + AvailableLength;
		if (byteCount <= num)
		{
			Buffer.BlockCopy(_bytes, _activeStart, _bytes, 0, ActiveLength);
			_availableStart = ActiveLength;
			_activeStart = 0;
			return;
		}
		int num2 = ActiveLength + byteCount;
		int num3 = _bytes.Length;
		do
		{
			num3 *= 2;
		}
		while (num3 < num2);
		byte[] array = (_usePool ? ArrayPool<byte>.Shared.Rent(num3) : new byte[num3]);
		byte[] bytes = _bytes;
		if (ActiveLength != 0)
		{
			Buffer.BlockCopy(bytes, _activeStart, array, 0, ActiveLength);
		}
		_availableStart = ActiveLength;
		_activeStart = 0;
		_bytes = array;
		ReturnBufferIfPooled(bytes);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private void ReturnBufferIfPooled(byte[] buffer)
	{
		if (_usePool && buffer.Length != 0)
		{
			ArrayPool<byte>.Shared.Return(buffer);
		}
	}
}
