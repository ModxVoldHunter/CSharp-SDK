using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;

namespace System.Text.RegularExpressions;

internal static class RegexPrefixAnalyzer
{
	private static ReadOnlySpan<float> Frequency => RuntimeHelpers.CreateSpan<float>((RuntimeFieldHandle)/*OpCode not supported: LdMemberToken*/);

	public static string FindPrefix(RegexNode node)
	{
		Span<char> initialBuffer = stackalloc char[64];
		System.Text.ValueStringBuilder vsb2 = new System.Text.ValueStringBuilder(initialBuffer);
		Process(node, ref vsb2);
		return vsb2.ToString();
		static bool Process(RegexNode node, ref System.Text.ValueStringBuilder vsb)
		{
			if (!StackHelper.TryEnsureSufficientExecutionStack())
			{
				return false;
			}
			bool flag = (node.Options & RegexOptions.RightToLeft) != 0;
			switch (node.Kind)
			{
			case RegexNodeKind.Concatenate:
			{
				int num5 = node.ChildCount();
				for (int k = 0; k < num5; k++)
				{
					if (!Process(node.Child(k), ref vsb))
					{
						return false;
					}
				}
				return !flag;
			}
			case RegexNodeKind.Alternate:
			{
				int num3 = node.ChildCount();
				int length = vsb.Length;
				Process(node.Child(0), ref vsb);
				int num4 = vsb.Length - length;
				if (num4 != 0)
				{
					System.Text.ValueStringBuilder vsb3 = new System.Text.ValueStringBuilder(64);
					for (int j = 1; j < num3; j++)
					{
						if (num4 == 0)
						{
							break;
						}
						vsb3.Length = 0;
						Process(node.Child(j), ref vsb3);
						num4 = vsb.AsSpan(length, num4).CommonPrefixLength(vsb3.AsSpan());
					}
					vsb3.Dispose();
					vsb.Length = length + num4;
				}
				return false;
			}
			case RegexNodeKind.One:
				vsb.Append(node.Ch);
				return !flag;
			case RegexNodeKind.Multi:
				vsb.Append(node.Str);
				return !flag;
			case RegexNodeKind.Oneloop:
			case RegexNodeKind.Onelazy:
			case RegexNodeKind.Oneloopatomic:
				if (node.M > 0)
				{
					int num2 = Math.Min(node.M, 32);
					vsb.Append(node.Ch, num2);
					if (num2 == node.N)
					{
						return !flag;
					}
					return false;
				}
				break;
			case RegexNodeKind.Loop:
			case RegexNodeKind.Lazyloop:
				if (node.M > 0)
				{
					int num = Math.Min(node.M, 4);
					for (int i = 0; i < num; i++)
					{
						if (!Process(node.Child(0), ref vsb))
						{
							return false;
						}
					}
					if (num == node.N)
					{
						return !flag;
					}
					return false;
				}
				break;
			case RegexNodeKind.Capture:
			case RegexNodeKind.Atomic:
				return Process(node.Child(0), ref vsb);
			case RegexNodeKind.Bol:
			case RegexNodeKind.Eol:
			case RegexNodeKind.Boundary:
			case RegexNodeKind.NonBoundary:
			case RegexNodeKind.Beginning:
			case RegexNodeKind.Start:
			case RegexNodeKind.EndZ:
			case RegexNodeKind.End:
			case RegexNodeKind.Empty:
			case RegexNodeKind.PositiveLookaround:
			case RegexNodeKind.NegativeLookaround:
			case RegexNodeKind.ECMABoundary:
			case RegexNodeKind.NonECMABoundary:
			case RegexNodeKind.UpdateBumpalong:
				return true;
			}
			return false;
		}
	}

	public static string FindPrefixOrdinalCaseInsensitive(RegexNode node)
	{
		while (true)
		{
			switch (node.Kind)
			{
			case RegexNodeKind.Loop:
			case RegexNodeKind.Lazyloop:
				if (node.M > 0)
				{
					goto IL_003b;
				}
				break;
			case RegexNodeKind.Capture:
			case RegexNodeKind.Atomic:
				goto IL_003b;
			case RegexNodeKind.Concatenate:
			{
				node.TryGetOrdinalCaseInsensitiveString(0, node.ChildCount(), out var _, out var caseInsensitiveString, consumeZeroWidthNodes: true);
				return caseInsensitiveString;
			}
			}
			break;
			IL_003b:
			node = node.Child(0);
		}
		return null;
	}

	public static List<RegexFindOptimizations.FixedDistanceSet> FindFixedDistanceSets(RegexNode root, bool thorough)
	{
		List<RegexFindOptimizations.FixedDistanceSet> list = new List<RegexFindOptimizations.FixedDistanceSet>();
		int distance2 = 0;
		TryFindRawFixedSets(root, list, ref distance2, thorough);
		for (int i = 0; i < list.Count; i++)
		{
			if (list[i].Set == "\0\u0001\0\0")
			{
				list.RemoveAll((RegexFindOptimizations.FixedDistanceSet s) => s.Set == "\0\u0001\0\0");
				break;
			}
		}
		if (list.Count == 0)
		{
			string text = FindFirstCharClass(root);
			if (text == null || text == "\0\u0001\0\0")
			{
				return null;
			}
			list.Add(new RegexFindOptimizations.FixedDistanceSet(null, text, 0));
		}
		Span<char> chars = stackalloc char[128];
		for (int j = 0; j < list.Count; j++)
		{
			RegexFindOptimizations.FixedDistanceSet value = list[j];
			value.Negated = RegexCharClass.IsNegated(value.Set);
			int setChars = RegexCharClass.GetSetChars(value.Set, chars);
			if (setChars > 0)
			{
				value.Chars = chars.Slice(0, setChars).ToArray();
			}
			if (thorough && (value.Chars == null || value.Chars.Length > 2) && RegexCharClass.TryGetSingleRange(value.Set, out var lowInclusive, out var highInclusive))
			{
				value.Chars = null;
				value.Range = (lowInclusive, highInclusive);
			}
			list[j] = value;
		}
		return list;
		static bool TryFindRawFixedSets(RegexNode node, List<RegexFindOptimizations.FixedDistanceSet> results, ref int distance, bool thorough)
		{
			if (!StackHelper.TryEnsureSufficientExecutionStack())
			{
				return false;
			}
			if ((node.Options & RegexOptions.RightToLeft) != 0)
			{
				return false;
			}
			switch (node.Kind)
			{
			case RegexNodeKind.One:
				if (results.Count < 50)
				{
					string set = RegexCharClass.OneToStringClass(node.Ch);
					results.Add(new RegexFindOptimizations.FixedDistanceSet(null, set, distance++));
					return true;
				}
				return false;
			case RegexNodeKind.Oneloop:
			case RegexNodeKind.Onelazy:
			case RegexNodeKind.Oneloopatomic:
				if (node.M > 0)
				{
					string set3 = RegexCharClass.OneToStringClass(node.Ch);
					int num5 = Math.Min(node.M, 20);
					int num6;
					for (num6 = 0; num6 < num5; num6++)
					{
						if (results.Count >= 50)
						{
							break;
						}
						results.Add(new RegexFindOptimizations.FixedDistanceSet(null, set3, distance++));
					}
					if (num6 == node.M)
					{
						return num6 == node.N;
					}
					return false;
				}
				break;
			case RegexNodeKind.Multi:
			{
				string str = node.Str;
				int n;
				for (n = 0; n < str.Length; n++)
				{
					if (results.Count >= 50)
					{
						break;
					}
					string set2 = RegexCharClass.OneToStringClass(str[n]);
					results.Add(new RegexFindOptimizations.FixedDistanceSet(null, set2, distance++));
				}
				return n == str.Length;
			}
			case RegexNodeKind.Set:
				if (results.Count < 50)
				{
					results.Add(new RegexFindOptimizations.FixedDistanceSet(null, node.Str, distance++));
					return true;
				}
				return false;
			case RegexNodeKind.Setloop:
			case RegexNodeKind.Setlazy:
			case RegexNodeKind.Setloopatomic:
				if (node.M > 0)
				{
					int num3 = Math.Min(node.M, 20);
					int l;
					for (l = 0; l < num3; l++)
					{
						if (results.Count >= 50)
						{
							break;
						}
						results.Add(new RegexFindOptimizations.FixedDistanceSet(null, node.Str, distance++));
					}
					if (l == node.M)
					{
						return l == node.N;
					}
					return false;
				}
				break;
			case RegexNodeKind.Notone:
				distance++;
				return true;
			case RegexNodeKind.Notoneloop:
			case RegexNodeKind.Notonelazy:
			case RegexNodeKind.Notoneloopatomic:
				if (node.M == node.N)
				{
					distance += node.M;
					return true;
				}
				break;
			case RegexNodeKind.Bol:
			case RegexNodeKind.Eol:
			case RegexNodeKind.Boundary:
			case RegexNodeKind.NonBoundary:
			case RegexNodeKind.Beginning:
			case RegexNodeKind.Start:
			case RegexNodeKind.EndZ:
			case RegexNodeKind.End:
			case RegexNodeKind.Empty:
			case RegexNodeKind.PositiveLookaround:
			case RegexNodeKind.NegativeLookaround:
			case RegexNodeKind.ECMABoundary:
			case RegexNodeKind.NonECMABoundary:
			case RegexNodeKind.UpdateBumpalong:
				return true;
			case RegexNodeKind.Capture:
			case RegexNodeKind.Group:
			case RegexNodeKind.Atomic:
				return TryFindRawFixedSets(node.Child(0), results, ref distance, thorough);
			case RegexNodeKind.Loop:
			case RegexNodeKind.Lazyloop:
				if (node.M > 0)
				{
					TryFindRawFixedSets(node.Child(0), results, ref distance, thorough);
					return false;
				}
				break;
			case RegexNodeKind.Concatenate:
			{
				int num4 = node.ChildCount();
				for (int m = 0; m < num4; m++)
				{
					if (!TryFindRawFixedSets(node.Child(m), results, ref distance, thorough))
					{
						return false;
					}
				}
				return true;
			}
			case RegexNodeKind.Alternate:
				if (thorough)
				{
					int num = node.ChildCount();
					bool flag = true;
					int? num2 = null;
					Dictionary<int, (RegexCharClass, int)> dictionary = new Dictionary<int, (RegexCharClass, int)>();
					List<RegexFindOptimizations.FixedDistanceSet> list2 = new List<RegexFindOptimizations.FixedDistanceSet>();
					for (int k = 0; k < num; k++)
					{
						list2.Clear();
						int distance3 = 0;
						flag &= TryFindRawFixedSets(node.Child(k), list2, ref distance3, thorough);
						if (list2.Count == 0)
						{
							return false;
						}
						if (flag)
						{
							if (!num2.HasValue)
							{
								num2 = distance3;
							}
							else if (num2.Value != distance3)
							{
								flag = false;
							}
						}
						foreach (RegexFindOptimizations.FixedDistanceSet item in list2)
						{
							if (dictionary.TryGetValue(item.Distance, out var value2))
							{
								if (value2.Item1.TryAddCharClass(RegexCharClass.Parse(item.Set)))
								{
									value2.Item2++;
									dictionary[item.Distance] = value2;
								}
							}
							else
							{
								dictionary[item.Distance] = (RegexCharClass.Parse(item.Set), 1);
							}
						}
					}
					foreach (KeyValuePair<int, (RegexCharClass, int)> item2 in dictionary)
					{
						if (results.Count >= 50)
						{
							flag = false;
							break;
						}
						if (item2.Value.Item2 == num)
						{
							results.Add(new RegexFindOptimizations.FixedDistanceSet(null, item2.Value.Item1.ToStringClass(), item2.Key + distance));
						}
					}
					if (flag)
					{
						distance += num2.Value;
						return true;
					}
					return false;
				}
				break;
			}
			return false;
		}
	}

	public static void SortFixedDistanceSetsByQuality(List<RegexFindOptimizations.FixedDistanceSet> results)
	{
		results.Sort(delegate(RegexFindOptimizations.FixedDistanceSet s1, RegexFindOptimizations.FixedDistanceSet s2)
		{
			char[] chars2 = s1.Chars;
			char[] chars3 = s2.Chars;
			int num3 = ((chars2 != null) ? chars2.Length : 0);
			int num4 = ((chars3 != null) ? chars3.Length : 0);
			bool negated2 = s1.Negated;
			bool negated3 = s2.Negated;
			(char, char)? range2 = s1.Range;
			int num5 = (range2.HasValue ? GetRangeLength(s1.Range.Value, negated2) : 0);
			range2 = s2.Range;
			int num6 = (range2.HasValue ? GetRangeLength(s2.Range.Value, negated3) : 0);
			if (negated2 != negated3)
			{
				if (!negated3)
				{
					return 1;
				}
				return -1;
			}
			if (!negated2)
			{
				if (chars2 != null && chars3 != null)
				{
					float num7 = SumFrequencies(chars2);
					float num8 = SumFrequencies(chars3);
					if (num7 != num8)
					{
						return num7.CompareTo(num8);
					}
					if (!RegexCharClass.IsAscii(chars2) && !RegexCharClass.IsAscii(chars3))
					{
						return num3.CompareTo(num4);
					}
				}
				if ((num3 > 0 && num6 > 0) || (num5 > 0 && num4 > 0))
				{
					int num9 = Math.Max(num3, num5).CompareTo(Math.Max(num4, num6));
					if (num9 != 0)
					{
						return num9;
					}
					if (num3 <= 0)
					{
						return 1;
					}
					return -1;
				}
				if (num3 > 0 != num4 > 0)
				{
					if (num3 <= 0)
					{
						return 1;
					}
					return -1;
				}
			}
			if (num5 > 0 != num6 > 0)
			{
				if (num5 <= 0)
				{
					return 1;
				}
				return -1;
			}
			return (num5 > 0) ? num5.CompareTo(num6) : s1.Distance.CompareTo(s2.Distance);
		});
		static int GetRangeLength((char LowInclusive, char HighInclusive) range, bool negated)
		{
			int num2 = range.HighInclusive - range.LowInclusive + 1;
			if (!negated)
			{
				return num2;
			}
			return 65536 - num2;
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		static float SumFrequencies(char[] chars)
		{
			float num = 0f;
			foreach (char c in chars)
			{
				if (c < '\u0080')
				{
					num += Frequency[c];
				}
			}
			return num;
		}
	}

	public static string FindFirstCharClass(RegexNode root)
	{
		RegexCharClass cc2 = null;
		if (TryFindFirstCharClass(root, ref cc2) != true)
		{
			return null;
		}
		return cc2.ToStringClass();
		static bool? TryFindFirstCharClass(RegexNode node, ref RegexCharClass cc)
		{
			if (!StackHelper.TryEnsureSufficientExecutionStack())
			{
				return false;
			}
			switch (node.Kind)
			{
			case RegexNodeKind.Oneloop:
			case RegexNodeKind.Onelazy:
			case RegexNodeKind.One:
			case RegexNodeKind.Oneloopatomic:
				if (cc == null || cc.CanMerge)
				{
					if (cc == null)
					{
						cc = new RegexCharClass();
					}
					cc.AddChar(node.Ch);
					if (node.Kind != RegexNodeKind.One && node.M <= 0)
					{
						return null;
					}
					return true;
				}
				return false;
			case RegexNodeKind.Notoneloop:
			case RegexNodeKind.Notonelazy:
			case RegexNodeKind.Notone:
			case RegexNodeKind.Notoneloopatomic:
				if (cc == null || cc.CanMerge)
				{
					if (cc == null)
					{
						cc = new RegexCharClass();
					}
					if (node.Ch > '\0')
					{
						cc.AddRange('\0', (char)(node.Ch - 1));
					}
					if (node.Ch < '\uffff')
					{
						cc.AddRange((char)(node.Ch + 1), '\uffff');
					}
					if (node.Kind != RegexNodeKind.Notone && node.M <= 0)
					{
						return null;
					}
					return true;
				}
				return false;
			case RegexNodeKind.Setloop:
			case RegexNodeKind.Setlazy:
			case RegexNodeKind.Set:
			case RegexNodeKind.Setloopatomic:
			{
				bool flag3 = false;
				if (cc == null)
				{
					cc = RegexCharClass.Parse(node.Str);
					flag3 = true;
				}
				else if (cc.CanMerge)
				{
					RegexCharClass regexCharClass = RegexCharClass.Parse(node.Str);
					if (regexCharClass != null && regexCharClass.CanMerge)
					{
						cc.AddCharClass(regexCharClass);
						flag3 = true;
					}
				}
				if (flag3)
				{
					if (node.Kind != RegexNodeKind.Set && node.M <= 0)
					{
						return null;
					}
					return true;
				}
				return false;
			}
			case RegexNodeKind.Multi:
				if (cc == null || cc.CanMerge)
				{
					if (cc == null)
					{
						cc = new RegexCharClass();
					}
					cc.AddChar(node.Str[((node.Options & RegexOptions.RightToLeft) != 0) ? (node.Str.Length - 1) : 0]);
					return true;
				}
				return false;
			case RegexNodeKind.Bol:
			case RegexNodeKind.Eol:
			case RegexNodeKind.Boundary:
			case RegexNodeKind.NonBoundary:
			case RegexNodeKind.Beginning:
			case RegexNodeKind.Start:
			case RegexNodeKind.EndZ:
			case RegexNodeKind.End:
			case RegexNodeKind.Nothing:
			case RegexNodeKind.Empty:
			case RegexNodeKind.PositiveLookaround:
			case RegexNodeKind.NegativeLookaround:
			case RegexNodeKind.ECMABoundary:
			case RegexNodeKind.NonECMABoundary:
			case RegexNodeKind.UpdateBumpalong:
				return null;
			case RegexNodeKind.Capture:
			case RegexNodeKind.Atomic:
				return TryFindFirstCharClass(node.Child(0), ref cc);
			case RegexNodeKind.Loop:
			case RegexNodeKind.Lazyloop:
			{
				bool? flag4 = TryFindFirstCharClass(node.Child(0), ref cc);
				if (!flag4.HasValue)
				{
					return null;
				}
				if (!flag4.GetValueOrDefault())
				{
					return false;
				}
				return (node.M == 0) ? null : new bool?(true);
			}
			case RegexNodeKind.Concatenate:
			{
				int num2 = node.ChildCount();
				for (int i = 0; i < num2; i++)
				{
					bool? result = TryFindFirstCharClass(node.Child(i), ref cc);
					if (result.HasValue)
					{
						return result;
					}
				}
				return null;
			}
			case RegexNodeKind.Alternate:
			{
				int num3 = node.ChildCount();
				bool flag5 = false;
				for (int j = 0; j < num3; j++)
				{
					bool? flag6 = TryFindFirstCharClass(node.Child(j), ref cc);
					if (!flag6.HasValue)
					{
						flag5 = true;
					}
					else if (flag6 == false)
					{
						return false;
					}
				}
				if (!flag5)
				{
					return true;
				}
				return null;
			}
			case RegexNodeKind.BackreferenceConditional:
			case RegexNodeKind.ExpressionConditional:
			{
				int num = ((node.Kind != RegexNodeKind.BackreferenceConditional) ? 1 : 0);
				bool? flag = TryFindFirstCharClass(node.Child(num), ref cc);
				bool? flag2 = TryFindFirstCharClass(node.Child(num + 1), ref cc);
				if (flag.HasValue)
				{
					if (flag.GetValueOrDefault())
					{
						if (!flag2.HasValue)
						{
							goto IL_0402;
						}
						if (flag2.GetValueOrDefault())
						{
							return true;
						}
					}
				}
				else if (!((!flag2) ?? false))
				{
					goto IL_0402;
				}
				return false;
			}
			case RegexNodeKind.Backreference:
				return false;
			default:
				{
					return false;
				}
				IL_0402:
				return null;
			}
		}
	}

	public static (RegexNode LoopNode, (char Char, string String, char[] Chars) Literal)? FindLiteralFollowingLeadingLoop(RegexNode node)
	{
		if ((node.Options & RegexOptions.RightToLeft) != 0)
		{
			return null;
		}
		RegexNodeKind kind;
		while (true)
		{
			kind = node.Kind;
			if ((kind != RegexNodeKind.Capture && kind != RegexNodeKind.Atomic) || 1 == 0)
			{
				break;
			}
			node = node.Child(0);
		}
		if (node.Kind != RegexNodeKind.Concatenate)
		{
			return null;
		}
		RegexNode regexNode = node.Child(0);
		kind = regexNode.Kind;
		bool flag = ((kind == RegexNodeKind.Setloop || kind == RegexNodeKind.Setlazy || kind == RegexNodeKind.Setloopatomic) ? true : false);
		if (!flag || regexNode.N != int.MaxValue)
		{
			return null;
		}
		RegexNode regexNode2 = node.Child(1);
		if (regexNode2.Kind == RegexNodeKind.UpdateBumpalong)
		{
			if (node.ChildCount() == 2)
			{
				return null;
			}
			regexNode2 = node.Child(2);
		}
		switch (regexNode2.Kind)
		{
		case RegexNodeKind.One:
			if (!RegexCharClass.CharInClass(regexNode2.Ch, regexNode.Str))
			{
				return (regexNode, (regexNode2.Ch, null, null));
			}
			break;
		case RegexNodeKind.Multi:
			if (!RegexCharClass.CharInClass(regexNode2.Str[0], regexNode.Str))
			{
				return (regexNode, ('\0', regexNode2.Str, null));
			}
			break;
		case RegexNodeKind.Set:
		{
			if (RegexCharClass.IsNegated(regexNode2.Str))
			{
				break;
			}
			Span<char> span = stackalloc char[5];
			span = span.Slice(0, RegexCharClass.GetSetChars(regexNode2.Str, span));
			if (span.IsEmpty)
			{
				break;
			}
			Span<char> span2 = span;
			for (int i = 0; i < span2.Length; i++)
			{
				char ch = span2[i];
				if (RegexCharClass.CharInClass(ch, regexNode.Str))
				{
					return null;
				}
			}
			return (regexNode, ('\0', null, span.ToArray()));
		}
		}
		return null;
	}

	public static RegexNodeKind FindLeadingAnchor(RegexNode node)
	{
		return FindLeadingOrTrailingAnchor(node, leading: true);
	}

	public static RegexNodeKind FindTrailingAnchor(RegexNode node)
	{
		return FindLeadingOrTrailingAnchor(node, leading: false);
	}

	private static RegexNodeKind FindLeadingOrTrailingAnchor(RegexNode node, bool leading)
	{
		if (!StackHelper.TryEnsureSufficientExecutionStack())
		{
			return RegexNodeKind.Unknown;
		}
		while (true)
		{
			switch (node.Kind)
			{
			case RegexNodeKind.Bol:
			case RegexNodeKind.Eol:
			case RegexNodeKind.Boundary:
			case RegexNodeKind.Beginning:
			case RegexNodeKind.Start:
			case RegexNodeKind.EndZ:
			case RegexNodeKind.End:
			case RegexNodeKind.ECMABoundary:
				return node.Kind;
			case RegexNodeKind.Capture:
			case RegexNodeKind.Atomic:
				node = node.Child(0);
				continue;
			case RegexNodeKind.Concatenate:
			{
				int num2 = node.ChildCount();
				RegexNode regexNode = null;
				if (leading)
				{
					for (int j = 0; j < num2; j++)
					{
						RegexNodeKind kind = node.Child(j).Kind;
						if ((kind != RegexNodeKind.Empty && kind - 30 > (RegexNodeKind)1) || 1 == 0)
						{
							regexNode = node.Child(j);
							break;
						}
					}
				}
				else
				{
					for (int num3 = num2 - 1; num3 >= 0; num3--)
					{
						RegexNodeKind kind = node.Child(num3).Kind;
						if ((kind != RegexNodeKind.Empty && kind - 30 > (RegexNodeKind)1) || 1 == 0)
						{
							regexNode = node.Child(num3);
							break;
						}
					}
				}
				if (regexNode != null)
				{
					node = regexNode;
					continue;
				}
				break;
			}
			case RegexNodeKind.Alternate:
			{
				RegexNodeKind regexNodeKind = FindLeadingOrTrailingAnchor(node.Child(0), leading);
				if (regexNodeKind == RegexNodeKind.Unknown)
				{
					return RegexNodeKind.Unknown;
				}
				int num = node.ChildCount();
				for (int i = 1; i < num; i++)
				{
					if (FindLeadingOrTrailingAnchor(node.Child(i), leading) != regexNodeKind)
					{
						return RegexNodeKind.Unknown;
					}
				}
				return regexNodeKind;
			}
			}
			break;
		}
		return RegexNodeKind.Unknown;
	}
}
