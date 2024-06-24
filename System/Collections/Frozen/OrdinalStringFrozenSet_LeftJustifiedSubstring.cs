using System.Collections.Generic;

namespace System.Collections.Frozen;

internal sealed class OrdinalStringFrozenSet_LeftJustifiedSubstring : OrdinalStringFrozenSet
{
	internal OrdinalStringFrozenSet_LeftJustifiedSubstring(string[] entries, IEqualityComparer<string> comparer, int minimumLength, int maximumLengthDiff, int hashIndex, int hashCount)
		: base(entries, comparer, minimumLength, maximumLengthDiff, hashIndex, hashCount)
	{
	}

	private protected override int FindItemIndex(string item)
	{
		return base.FindItemIndex(item);
	}

	private protected override bool Equals(string? x, string? y)
	{
		return string.Equals(x, y);
	}

	private protected override int GetHashCode(string s)
	{
		return Hashing.GetHashCodeOrdinal(s.AsSpan(base.HashIndex, base.HashCount));
	}
}
