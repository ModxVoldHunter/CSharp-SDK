using System.Runtime.CompilerServices;

namespace System.Buffers;

internal sealed class SingleByteSearchValues : SearchValues<byte>
{
	private readonly byte _e0;

	public SingleByteSearchValues(ReadOnlySpan<byte> values)
	{
		_e0 = values[0];
	}

	internal override byte[] GetValues()
	{
		return new byte[1] { _e0 };
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal override bool ContainsCore(byte value)
	{
		return value == _e0;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal override int IndexOfAny(ReadOnlySpan<byte> span)
	{
		return span.IndexOf(_e0);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal override int IndexOfAnyExcept(ReadOnlySpan<byte> span)
	{
		return span.IndexOfAnyExcept(_e0);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal override int LastIndexOfAny(ReadOnlySpan<byte> span)
	{
		return span.LastIndexOf(_e0);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal override int LastIndexOfAnyExcept(ReadOnlySpan<byte> span)
	{
		return span.LastIndexOfAnyExcept(_e0);
	}
}
