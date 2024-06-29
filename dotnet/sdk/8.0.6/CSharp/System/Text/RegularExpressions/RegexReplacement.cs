using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System.Text.RegularExpressions;

internal sealed class RegexReplacement
{
	private struct FourStackStrings
	{
		public string Item1;

		public string Item2;

		public string Item3;

		public string Item4;
	}

	private readonly string[] _strings;

	private readonly int[] _rules;

	private readonly bool _hasBackreferences;

	public string Pattern { get; }

	public RegexReplacement(string rep, RegexNode concat, Hashtable _caps)
	{
		Span<char> initialBuffer = stackalloc char[256];
		System.Text.ValueStringBuilder valueStringBuilder = new System.Text.ValueStringBuilder(initialBuffer);
		FourStackStrings fourStackStrings = default(FourStackStrings);
		System.Collections.Generic.ValueListBuilder<string> valueListBuilder = new System.Collections.Generic.ValueListBuilder<string>(MemoryMarshal.CreateSpan(ref fourStackStrings.Item1, 4));
		Span<int> initialSpan = stackalloc int[64];
		System.Collections.Generic.ValueListBuilder<int> valueListBuilder2 = new System.Collections.Generic.ValueListBuilder<int>(initialSpan);
		int num = concat.ChildCount();
		for (int i = 0; i < num; i++)
		{
			RegexNode regexNode = concat.Child(i);
			switch (regexNode.Kind)
			{
			case RegexNodeKind.Multi:
				valueStringBuilder.Append(regexNode.Str);
				break;
			case RegexNodeKind.One:
				valueStringBuilder.Append(regexNode.Ch);
				break;
			case RegexNodeKind.Backreference:
			{
				if (valueStringBuilder.Length > 0)
				{
					valueListBuilder2.Append(valueListBuilder.Length);
					valueListBuilder.Append(valueStringBuilder.AsSpan().ToString());
					valueStringBuilder.Length = 0;
				}
				int num2 = regexNode.M;
				if (_caps != null && num2 >= 0)
				{
					num2 = (int)_caps[num2];
				}
				valueListBuilder2.Append(-5 - num2);
				_hasBackreferences = true;
				break;
			}
			}
		}
		if (valueStringBuilder.Length > 0)
		{
			valueListBuilder2.Append(valueListBuilder.Length);
			valueListBuilder.Append(valueStringBuilder.ToString());
		}
		valueStringBuilder.Dispose();
		Pattern = rep;
		_strings = valueListBuilder.AsSpan().ToArray();
		_rules = valueListBuilder2.AsSpan().ToArray();
		valueListBuilder2.Dispose();
	}

	public static RegexReplacement GetOrCreate(WeakReference<RegexReplacement> replRef, string replacement, Hashtable caps, int capsize, Hashtable capnames, RegexOptions roptions)
	{
		if (!replRef.TryGetTarget(out var target) || !target.Pattern.Equals(replacement))
		{
			target = RegexParser.ParseReplacement(replacement, roptions, caps, capsize, capnames);
			replRef.SetTarget(target);
		}
		return target;
	}

	public void ReplacementImpl(ref StructListBuilder<ReadOnlyMemory<char>> segments, Match match)
	{
		int[] rules = _rules;
		foreach (int num in rules)
		{
			ReadOnlyMemory<char> readOnlyMemory;
			if (num >= 0)
			{
				readOnlyMemory = _strings[num].AsMemory();
			}
			else
			{
				ReadOnlyMemory<char> readOnlyMemory2 = ((num >= -4) ? ((-5 - num) switch
				{
					-1 => match.GetLeftSubstring(), 
					-2 => match.GetRightSubstring(), 
					-3 => match.LastGroupToStringImpl(), 
					-4 => match.Text.AsMemory(), 
					_ => default(ReadOnlyMemory<char>), 
				}) : match.GroupToStringImpl(-5 - num));
				readOnlyMemory = readOnlyMemory2;
			}
			ReadOnlyMemory<char> item = readOnlyMemory;
			if (item.Length != 0)
			{
				segments.Add(item);
			}
		}
	}

	public void ReplacementImplRTL(ref StructListBuilder<ReadOnlyMemory<char>> segments, Match match)
	{
		for (int num = _rules.Length - 1; num >= 0; num--)
		{
			int num2 = _rules[num];
			ReadOnlyMemory<char> readOnlyMemory;
			if (num2 >= 0)
			{
				readOnlyMemory = _strings[num2].AsMemory();
			}
			else
			{
				ReadOnlyMemory<char> readOnlyMemory2 = ((num2 >= -4) ? ((-5 - num2) switch
				{
					-1 => match.GetLeftSubstring(), 
					-2 => match.GetRightSubstring(), 
					-3 => match.LastGroupToStringImpl(), 
					-4 => match.Text.AsMemory(), 
					_ => default(ReadOnlyMemory<char>), 
				}) : match.GroupToStringImpl(-5 - num2));
				readOnlyMemory = readOnlyMemory2;
			}
			ReadOnlyMemory<char> item = readOnlyMemory;
			if (item.Length != 0)
			{
				segments.Add(item);
			}
		}
	}

	public string Replace(Regex regex, string input, int count, int startat)
	{
		if (count < -1)
		{
			ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.count, ExceptionResource.CountTooSmall);
		}
		if ((uint)startat > (uint)input.Length)
		{
			ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.startat, ExceptionResource.BeginIndexNotNegative);
		}
		if (count == 0)
		{
			return input;
		}
		if (!regex.RightToLeft && !_hasBackreferences)
		{
			return ReplaceSimpleText(regex, input, (_rules.Length != 0) ? _strings[0] : "", count, startat);
		}
		return ReplaceNonSimpleText(regex, input, count, startat);
	}

	private unsafe static string ReplaceSimpleText(Regex regex, string input, string replacement, int count, int startat)
	{
		(string, string, StructListBuilder<int>, ReadOnlyMemory<char>, int, int) state2 = (input, replacement, new StructListBuilder<int>(), input.AsMemory(), 0, count);
		string result = input;
		regex.RunAllMatchesWithCallback<(string, string, StructListBuilder<int>, ReadOnlyMemory<char>, int, int)>(input, startat, ref state2, delegate(ref (string input, string replacement, StructListBuilder<int> segments, ReadOnlyMemory<char> inputMemory, int prevat, int count) state, Match match)
		{
			state.segments.Add(state.prevat);
			state.segments.Add(match.Index - state.prevat);
			state.prevat = match.Index + match.Length;
			return --state.count != 0;
		}, RegexRunnerMode.BoundsRequired, reuseMatchObject: true);
		if (state2.Item3.Count != 0)
		{
			state2.Item3.Add(state2.Item5);
			state2.Item3.Add(input.Length - state2.Item5);
			Span<int> span = state2.Item3.AsSpan();
			int num = (span.Length / 2 - 1) * replacement.Length;
			for (int i = 1; i < span.Length; i += 2)
			{
				num += span[i];
			}
			ReadOnlySpan<int> readOnlySpan = span;
			result = string.Create(num, ((nint)(&readOnlySpan), input, replacement), delegate(Span<char> dest, (nint, string input, string replacement) state)
			{
				Span<int> item = Unsafe.Read<Span<int>>((void*)state.Item1);
				for (int j = 0; j < item.Length; j += 2)
				{
					if (j != 0)
					{
						state.replacement.CopyTo(dest);
						dest = dest.Slice(state.replacement.Length);
					}
					int num2 = item[j];
					int num3 = item[j + 1];
					int start = num2;
					state.input.AsSpan(start, num3).CopyTo(dest);
					dest = dest.Slice(num3);
				}
			});
		}
		state2.Item3.Dispose();
		return result;
	}

	private string ReplaceNonSimpleText(Regex regex, string input, int count, int startat)
	{
		(RegexReplacement, StructListBuilder<ReadOnlyMemory<char>>, ReadOnlyMemory<char>, int, int) state2 = (this, new StructListBuilder<ReadOnlyMemory<char>>(), input.AsMemory(), 0, count);
		if (!regex.RightToLeft)
		{
			regex.RunAllMatchesWithCallback<(RegexReplacement, StructListBuilder<ReadOnlyMemory<char>>, ReadOnlyMemory<char>, int, int)>(input, startat, ref state2, delegate(ref (RegexReplacement thisRef, StructListBuilder<ReadOnlyMemory<char>> segments, ReadOnlyMemory<char> inputMemory, int prevat, int count) state, Match match)
			{
				state.segments.Add(state.inputMemory.Slice(state.prevat, match.Index - state.prevat));
				state.prevat = match.Index + match.Length;
				state.thisRef.ReplacementImpl(ref state.segments, match);
				return --state.count != 0;
			}, (!_hasBackreferences) ? RegexRunnerMode.BoundsRequired : RegexRunnerMode.FullMatchRequired, reuseMatchObject: true);
			if (state2.Item2.Count == 0)
			{
				return input;
			}
			state2.Item2.Add(state2.Item3.Slice(state2.Item4));
		}
		else
		{
			state2.Item4 = input.Length;
			regex.RunAllMatchesWithCallback<(RegexReplacement, StructListBuilder<ReadOnlyMemory<char>>, ReadOnlyMemory<char>, int, int)>(input, startat, ref state2, delegate(ref (RegexReplacement thisRef, StructListBuilder<ReadOnlyMemory<char>> segments, ReadOnlyMemory<char> inputMemory, int prevat, int count) state, Match match)
			{
				state.segments.Add(state.inputMemory.Slice(match.Index + match.Length, state.prevat - match.Index - match.Length));
				state.prevat = match.Index;
				state.thisRef.ReplacementImplRTL(ref state.segments, match);
				return --state.count != 0;
			}, (!_hasBackreferences) ? RegexRunnerMode.BoundsRequired : RegexRunnerMode.FullMatchRequired, reuseMatchObject: true);
			if (state2.Item2.Count == 0)
			{
				return input;
			}
			state2.Item2.Add(state2.Item3.Slice(0, state2.Item4));
			state2.Item2.AsSpan().Reverse();
		}
		return Regex.SegmentsToStringAndDispose(ref state2.Item2);
	}
}
