using System.Collections.Generic;

namespace System.Collections.Frozen;

internal sealed class OrdinalStringFrozenSet_FullCaseInsensitive : OrdinalStringFrozenSet
{
	internal OrdinalStringFrozenSet_FullCaseInsensitive(string[] entries, IEqualityComparer<string> comparer, int minimumLength, int maximumLengthDiff)
		: base(entries, comparer, minimumLength, maximumLengthDiff)
	{
	}

	private protected override int FindItemIndex(string item)
	{
		return base.FindItemIndex(item);
	}

	private protected override bool Equals(string? x, string? y)
	{
		return StringComparer.OrdinalIgnoreCase.Equals(x, y);
	}

	private protected override int GetHashCode(string s)
	{
		return Hashing.GetHashCodeOrdinalIgnoreCase(s.AsSpan());
	}
}
