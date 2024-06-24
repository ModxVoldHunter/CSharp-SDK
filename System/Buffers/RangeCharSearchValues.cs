using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System.Buffers;

internal sealed class RangeCharSearchValues<TShouldUsePacked> : SearchValues<char> where TShouldUsePacked : struct, SearchValues.IRuntimeConst
{
	private readonly char _rangeInclusive;

	private char _lowInclusive;

	private char _highInclusive;

	private readonly uint _lowUint;

	private readonly uint _highMinusLow;

	public RangeCharSearchValues(char lowInclusive, char highInclusive)
	{
		char lowInclusive2 = lowInclusive;
		char rangeInclusive = (char)(highInclusive - lowInclusive);
		char highInclusive2 = highInclusive;
		_lowInclusive = lowInclusive2;
		_rangeInclusive = rangeInclusive;
		_highInclusive = highInclusive2;
		_lowUint = lowInclusive;
		_highMinusLow = (uint)(highInclusive - lowInclusive);
	}

	internal override char[] GetValues()
	{
		char[] array = new char[_rangeInclusive + 1];
		int lowInclusive = _lowInclusive;
		for (int i = 0; i < array.Length; i++)
		{
			array[i] = (char)(lowInclusive + i);
		}
		return array;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal override bool ContainsCore(char value)
	{
		return value - _lowUint <= _highMinusLow;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal override int IndexOfAny(ReadOnlySpan<char> span)
	{
		if (!PackedSpanHelpers.PackedIndexOfIsSupported || !TShouldUsePacked.Value)
		{
			return SpanHelpers.NonPackedIndexOfAnyInRangeUnsignedNumber<ushort, SpanHelpers.DontNegate<ushort>>(ref Unsafe.As<char, ushort>(ref MemoryMarshal.GetReference(span)), Unsafe.As<char, ushort>(ref _lowInclusive), Unsafe.As<char, ushort>(ref _highInclusive), span.Length);
		}
		return PackedSpanHelpers.IndexOfAnyInRange(ref MemoryMarshal.GetReference(span), _lowInclusive, _rangeInclusive, span.Length);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal override int IndexOfAnyExcept(ReadOnlySpan<char> span)
	{
		if (!PackedSpanHelpers.PackedIndexOfIsSupported || !TShouldUsePacked.Value)
		{
			return SpanHelpers.NonPackedIndexOfAnyInRangeUnsignedNumber<ushort, SpanHelpers.Negate<ushort>>(ref Unsafe.As<char, ushort>(ref MemoryMarshal.GetReference(span)), Unsafe.As<char, ushort>(ref _lowInclusive), Unsafe.As<char, ushort>(ref _highInclusive), span.Length);
		}
		return PackedSpanHelpers.IndexOfAnyExceptInRange(ref MemoryMarshal.GetReference(span), _lowInclusive, _rangeInclusive, span.Length);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal override int LastIndexOfAny(ReadOnlySpan<char> span)
	{
		return span.LastIndexOfAnyInRange(_lowInclusive, _highInclusive);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal override int LastIndexOfAnyExcept(ReadOnlySpan<char> span)
	{
		return span.LastIndexOfAnyExceptInRange(_lowInclusive, _highInclusive);
	}
}
