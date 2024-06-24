using System.Runtime.CompilerServices;

namespace System.Buffers;

internal sealed class RangeByteSearchValues : SearchValues<byte>
{
	private readonly byte _lowInclusive;

	private readonly byte _highInclusive;

	private readonly uint _lowUint;

	private readonly uint _highMinusLow;

	public RangeByteSearchValues(byte lowInclusive, byte highInclusive)
	{
		byte lowInclusive2 = lowInclusive;
		byte highInclusive2 = highInclusive;
		_lowInclusive = lowInclusive2;
		_highInclusive = highInclusive2;
		_lowUint = lowInclusive;
		_highMinusLow = (uint)(highInclusive - lowInclusive);
	}

	internal override byte[] GetValues()
	{
		byte[] array = new byte[_highMinusLow + 1];
		int lowInclusive = _lowInclusive;
		for (int i = 0; i < array.Length; i++)
		{
			array[i] = (byte)(lowInclusive + i);
		}
		return array;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal override bool ContainsCore(byte value)
	{
		return value - _lowUint <= _highMinusLow;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal override int IndexOfAny(ReadOnlySpan<byte> span)
	{
		return span.IndexOfAnyInRange(_lowInclusive, _highInclusive);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal override int IndexOfAnyExcept(ReadOnlySpan<byte> span)
	{
		return span.IndexOfAnyExceptInRange(_lowInclusive, _highInclusive);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal override int LastIndexOfAny(ReadOnlySpan<byte> span)
	{
		return span.LastIndexOfAnyInRange(_lowInclusive, _highInclusive);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal override int LastIndexOfAnyExcept(ReadOnlySpan<byte> span)
	{
		return span.LastIndexOfAnyExceptInRange(_lowInclusive, _highInclusive);
	}
}
