using System.Buffers;
using System.Collections.Generic;
using System.Text;

namespace System.Collections.Frozen;

internal static class KeyAnalyzer
{
	private delegate ReadOnlySpan<char> GetSpan(string s, int index, int count);

	internal readonly struct AnalysisResults
	{
		public bool IgnoreCase { get; }

		public bool AllAsciiIfIgnoreCase { get; }

		public int HashIndex { get; }

		public int HashCount { get; }

		public int MinimumLength { get; }

		public int MaximumLengthDiff { get; }

		public bool SubstringHashing => HashCount != 0;

		public bool RightJustifiedSubstring => HashIndex < 0;

		public AnalysisResults(bool ignoreCase, bool allAsciiIfIgnoreCase, int hashIndex, int hashCount, int minLength, int maxLength)
		{
			IgnoreCase = ignoreCase;
			AllAsciiIfIgnoreCase = allAsciiIfIgnoreCase;
			HashIndex = hashIndex;
			HashCount = hashCount;
			MinimumLength = minLength;
			MaximumLengthDiff = maxLength - minLength;
		}
	}

	private abstract class SubstringComparer : IEqualityComparer<string>
	{
		public int Index;

		public int Count;

		public bool IsLeft;

		public abstract bool Equals(string x, string y);

		public abstract int GetHashCode(string s);
	}

	private sealed class JustifiedSubstringComparer : SubstringComparer
	{
		public override bool Equals(string x, string y)
		{
			return x.AsSpan(IsLeft ? Index : (x.Length + Index), Count).SequenceEqual(y.AsSpan(IsLeft ? Index : (y.Length + Index), Count));
		}

		public override int GetHashCode(string s)
		{
			return Hashing.GetHashCodeOrdinal(s.AsSpan(IsLeft ? Index : (s.Length + Index), Count));
		}
	}

	private sealed class JustifiedCaseInsensitiveSubstringComparer : SubstringComparer
	{
		public override bool Equals(string x, string y)
		{
			return MemoryExtensions.Equals(x.AsSpan(IsLeft ? Index : (x.Length + Index), Count), y.AsSpan(IsLeft ? Index : (y.Length + Index), Count), StringComparison.OrdinalIgnoreCase);
		}

		public override int GetHashCode(string s)
		{
			return Hashing.GetHashCodeOrdinalIgnoreCase(s.AsSpan(IsLeft ? Index : (s.Length + Index), Count));
		}
	}

	private static readonly SearchValues<char> s_asciiLetters = SearchValues.Create("ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz");

	public static AnalysisResults Analyze(ReadOnlySpan<string> uniqueStrings, bool ignoreCase, int minLength, int maxLength)
	{
		if (minLength == 0 || !TryUseSubstring(uniqueStrings, ignoreCase, minLength, maxLength, out var results))
		{
			return CreateAnalysisResults(uniqueStrings, ignoreCase, minLength, maxLength, 0, 0, isSubstring: false, (string s, int _, int _) => s.AsSpan());
		}
		return results;
	}

	private static bool TryUseSubstring(ReadOnlySpan<string> uniqueStrings, bool ignoreCase, int minLength, int maxLength, out AnalysisResults results)
	{
		SubstringComparer substringComparer = (ignoreCase ? ((SubstringComparer)new JustifiedCaseInsensitiveSubstringComparer()) : ((SubstringComparer)new JustifiedSubstringComparer()));
		HashSet<string> set = new HashSet<string>(uniqueStrings.Length, substringComparer);
		int num = Math.Min(minLength, 8);
		for (int i = 1; i <= num; i++)
		{
			substringComparer.IsLeft = true;
			substringComparer.Count = i;
			for (int j = 0; j <= minLength - i; j++)
			{
				substringComparer.Index = j;
				if (HasSufficientUniquenessFactor(set, uniqueStrings))
				{
					results = CreateAnalysisResults(uniqueStrings, ignoreCase, minLength, maxLength, j, i, isSubstring: true, (string s, int index, int count) => s.AsSpan(index, count));
					return true;
				}
			}
			if (minLength == maxLength)
			{
				continue;
			}
			substringComparer.IsLeft = false;
			for (int k = 0; k <= minLength - i; k++)
			{
				substringComparer.Index = -k - i;
				if (HasSufficientUniquenessFactor(set, uniqueStrings))
				{
					results = CreateAnalysisResults(uniqueStrings, ignoreCase, minLength, maxLength, substringComparer.Index, i, isSubstring: true, (string s, int index, int count) => s.AsSpan(s.Length + index, count));
					return true;
				}
			}
		}
		results = default(AnalysisResults);
		return false;
	}

	private static AnalysisResults CreateAnalysisResults(ReadOnlySpan<string> uniqueStrings, bool ignoreCase, int minLength, int maxLength, int index, int count, bool isSubstring, GetSpan getSubstringSpan)
	{
		bool allAsciiIfIgnoreCase = true;
		if (ignoreCase)
		{
			bool flag = !isSubstring;
			ReadOnlySpan<string> readOnlySpan = uniqueStrings;
			for (int i = 0; i < readOnlySpan.Length; i++)
			{
				string s = readOnlySpan[i];
				ReadOnlySpan<char> s2 = getSubstringSpan(s, index, count);
				if (!IsAllAscii(s2))
				{
					allAsciiIfIgnoreCase = false;
					flag = false;
					break;
				}
				if (flag && ContainsAnyLetters(s2))
				{
					flag = false;
				}
			}
			if (flag)
			{
				ignoreCase = false;
			}
		}
		return new AnalysisResults(ignoreCase, allAsciiIfIgnoreCase, index, count, minLength, maxLength);
	}

	internal static bool IsAllAscii(ReadOnlySpan<char> s)
	{
		return Ascii.IsValid(s);
	}

	private static bool ContainsAnyLetters(ReadOnlySpan<char> s)
	{
		return s.ContainsAny(s_asciiLetters);
	}

	private static bool HasSufficientUniquenessFactor(HashSet<string> set, ReadOnlySpan<string> uniqueStrings)
	{
		set.Clear();
		int num = uniqueStrings.Length / 20;
		ReadOnlySpan<string> readOnlySpan = uniqueStrings;
		for (int i = 0; i < readOnlySpan.Length; i++)
		{
			string item = readOnlySpan[i];
			if (!set.Add(item) && --num < 0)
			{
				return false;
			}
		}
		return true;
	}
}
