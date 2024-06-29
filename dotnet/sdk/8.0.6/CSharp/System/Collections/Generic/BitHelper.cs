namespace System.Collections.Generic;

internal ref struct BitHelper
{
	private readonly Span<int> _span;

	internal BitHelper(Span<int> span, bool clear)
	{
		if (clear)
		{
			span.Clear();
		}
		_span = span;
	}

	internal void MarkBit(int bitPosition)
	{
		uint num = (uint)bitPosition / 32u;
		Span<int> span = _span;
		if (num < (uint)span.Length)
		{
			span[(int)num] |= 1 << (int)((uint)bitPosition % 32u);
		}
	}

	internal bool IsMarked(int bitPosition)
	{
		uint num = (uint)bitPosition / 32u;
		Span<int> span = _span;
		if (num < (uint)span.Length)
		{
			return (span[(int)num] & (1 << (int)((uint)bitPosition % 32u))) != 0;
		}
		return false;
	}

	internal static int ToIntArrayLength(int n)
	{
		if (n <= 0)
		{
			return 0;
		}
		return (n - 1) / 32 + 1;
	}
}
