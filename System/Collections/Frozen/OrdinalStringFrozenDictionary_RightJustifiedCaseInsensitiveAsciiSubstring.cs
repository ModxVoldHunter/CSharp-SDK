using System.Collections.Generic;

namespace System.Collections.Frozen;

internal sealed class OrdinalStringFrozenDictionary_RightJustifiedCaseInsensitiveAsciiSubstring<TValue> : OrdinalStringFrozenDictionary<TValue>
{
	internal OrdinalStringFrozenDictionary_RightJustifiedCaseInsensitiveAsciiSubstring(string[] keys, TValue[] values, IEqualityComparer<string> comparer, int minimumLength, int maximumLengthDiff, int hashIndex, int hashCount)
		: base(keys, values, comparer, minimumLength, maximumLengthDiff, hashIndex, hashCount)
	{
	}

	private protected override ref readonly TValue GetValueRefOrNullRefCore(string key)
	{
		return ref base.GetValueRefOrNullRefCore(key);
	}

	private protected override bool Equals(string? x, string? y)
	{
		return StringComparer.OrdinalIgnoreCase.Equals(x, y);
	}

	private protected override int GetHashCode(string s)
	{
		return Hashing.GetHashCodeOrdinalIgnoreCaseAscii(s.AsSpan(s.Length + base.HashIndex, base.HashCount));
	}
}
