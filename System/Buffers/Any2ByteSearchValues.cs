using System.Runtime.CompilerServices;

namespace System.Buffers;

internal sealed class Any2ByteSearchValues : SearchValues<byte>
{
	private readonly byte _e0;

	private readonly byte _e1;

	public Any2ByteSearchValues(ReadOnlySpan<byte> values)
	{
		byte e = values[0];
		byte e2 = values[1];
		_e0 = e;
		_e1 = e2;
	}

	internal override byte[] GetValues()
	{
		return new byte[2] { _e0, _e1 };
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal override bool ContainsCore(byte value)
	{
		if (value != _e0)
		{
			return value == _e1;
		}
		return true;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal override int IndexOfAny(ReadOnlySpan<byte> span)
	{
		return span.IndexOfAny(_e0, _e1);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal override int IndexOfAnyExcept(ReadOnlySpan<byte> span)
	{
		return span.IndexOfAnyExcept(_e0, _e1);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal override int LastIndexOfAny(ReadOnlySpan<byte> span)
	{
		return span.LastIndexOfAny(_e0, _e1);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal override int LastIndexOfAnyExcept(ReadOnlySpan<byte> span)
	{
		return span.LastIndexOfAnyExcept(_e0, _e1);
	}
}
