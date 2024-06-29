using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System.Buffers;

internal sealed class Any2CharSearchValues<TShouldUsePacked> : SearchValues<char> where TShouldUsePacked : struct, SearchValues.IRuntimeConst
{
	private char _e0;

	private char _e1;

	public Any2CharSearchValues(char value0, char value1)
	{
		char e = value0;
		char e2 = value1;
		_e0 = e;
		_e1 = e2;
	}

	internal override char[] GetValues()
	{
		return new char[2] { _e0, _e1 };
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal override bool ContainsCore(char value)
	{
		if (value != _e0)
		{
			return value == _e1;
		}
		return true;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal override int IndexOfAny(ReadOnlySpan<char> span)
	{
		if (!PackedSpanHelpers.PackedIndexOfIsSupported || !TShouldUsePacked.Value)
		{
			return SpanHelpers.NonPackedIndexOfAnyValueType<short, SpanHelpers.DontNegate<short>>(ref Unsafe.As<char, short>(ref MemoryMarshal.GetReference(span)), Unsafe.As<char, short>(ref _e0), Unsafe.As<char, short>(ref _e1), span.Length);
		}
		return PackedSpanHelpers.IndexOfAny(ref MemoryMarshal.GetReference(span), _e0, _e1, span.Length);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal override int IndexOfAnyExcept(ReadOnlySpan<char> span)
	{
		if (!PackedSpanHelpers.PackedIndexOfIsSupported || !TShouldUsePacked.Value)
		{
			return SpanHelpers.NonPackedIndexOfAnyValueType<short, SpanHelpers.Negate<short>>(ref Unsafe.As<char, short>(ref MemoryMarshal.GetReference(span)), Unsafe.As<char, short>(ref _e0), Unsafe.As<char, short>(ref _e1), span.Length);
		}
		return PackedSpanHelpers.IndexOfAnyExcept(ref MemoryMarshal.GetReference(span), _e0, _e1, span.Length);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal override int LastIndexOfAny(ReadOnlySpan<char> span)
	{
		return span.LastIndexOfAny(_e0, _e1);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal override int LastIndexOfAnyExcept(ReadOnlySpan<char> span)
	{
		return span.LastIndexOfAnyExcept(_e0, _e1);
	}
}
