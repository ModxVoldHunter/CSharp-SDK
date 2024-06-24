using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System.Buffers;

internal sealed class Any3CharSearchValues<TShouldUsePacked> : SearchValues<char> where TShouldUsePacked : struct, SearchValues.IRuntimeConst
{
	private char _e0;

	private char _e1;

	private char _e2;

	public Any3CharSearchValues(char value0, char value1, char value2)
	{
		char e = value0;
		char e2 = value1;
		char e3 = value2;
		_e0 = e;
		_e1 = e2;
		_e2 = e3;
	}

	internal override char[] GetValues()
	{
		return new char[3] { _e0, _e1, _e2 };
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal override bool ContainsCore(char value)
	{
		if (value != _e0 && value != _e1)
		{
			return value == _e2;
		}
		return true;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal override int IndexOfAny(ReadOnlySpan<char> span)
	{
		if (!PackedSpanHelpers.PackedIndexOfIsSupported || !TShouldUsePacked.Value)
		{
			return SpanHelpers.NonPackedIndexOfAnyValueType<short, SpanHelpers.DontNegate<short>>(ref Unsafe.As<char, short>(ref MemoryMarshal.GetReference(span)), Unsafe.As<char, short>(ref _e0), Unsafe.As<char, short>(ref _e1), Unsafe.As<char, short>(ref _e2), span.Length);
		}
		return PackedSpanHelpers.IndexOfAny(ref MemoryMarshal.GetReference(span), _e0, _e1, _e2, span.Length);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal override int IndexOfAnyExcept(ReadOnlySpan<char> span)
	{
		if (!PackedSpanHelpers.PackedIndexOfIsSupported || !TShouldUsePacked.Value)
		{
			return SpanHelpers.NonPackedIndexOfAnyValueType<short, SpanHelpers.Negate<short>>(ref Unsafe.As<char, short>(ref MemoryMarshal.GetReference(span)), Unsafe.As<char, short>(ref _e0), Unsafe.As<char, short>(ref _e1), Unsafe.As<char, short>(ref _e2), span.Length);
		}
		return PackedSpanHelpers.IndexOfAnyExcept(ref MemoryMarshal.GetReference(span), _e0, _e1, _e2, span.Length);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal override int LastIndexOfAny(ReadOnlySpan<char> span)
	{
		return span.LastIndexOfAny(_e0, _e1, _e2);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal override int LastIndexOfAnyExcept(ReadOnlySpan<char> span)
	{
		return span.LastIndexOfAnyExcept(_e0, _e1, _e2);
	}
}
