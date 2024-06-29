using System.Collections.Generic;
using System.Threading;

namespace System.Text.RegularExpressions;

internal static class RegexTreeAnalyzer
{
	public static AnalysisResults Analyze(RegexTree regexTree)
	{
		AnalysisResults analysisResults = new AnalysisResults(regexTree);
		analysisResults._complete = TryAnalyze(regexTree.Root, analysisResults, isAtomicByAncestor: true, isInLoop: false);
		return analysisResults;
		static bool TryAnalyze(RegexNode node, AnalysisResults results, bool isAtomicByAncestor, bool isInLoop)
		{
			if (!StackHelper.TryEnsureSufficientExecutionStack())
			{
				return false;
			}
			results._hasIgnoreCase |= (node.Options & RegexOptions.IgnoreCase) != 0;
			results._hasRightToLeft |= (node.Options & RegexOptions.RightToLeft) != 0;
			AnalysisResults analysisResults2;
			if (isInLoop)
			{
				analysisResults2 = results;
				(analysisResults2._inLoops ?? (analysisResults2._inLoops = new HashSet<RegexNode>())).Add(node);
			}
			if (isAtomicByAncestor)
			{
				results._isAtomicByAncestor.Add(node);
			}
			else
			{
				RegexNodeKind kind = node.Kind;
				if (kind - 3 > RegexNodeKind.Setloop)
				{
					if (kind == RegexNodeKind.Alternate || (kind - 26 <= (RegexNodeKind)1 && node.M != node.N))
					{
						goto IL_00ab;
					}
				}
				else if (node.M != node.N)
				{
					goto IL_00ab;
				}
			}
			goto IL_00cc;
			IL_00ab:
			analysisResults2 = results;
			(analysisResults2._mayBacktrack ?? (analysisResults2._mayBacktrack = new HashSet<RegexNode>())).Add(node);
			goto IL_00cc;
			IL_00cc:
			bool flag = false;
			switch (node.Kind)
			{
			case RegexNodeKind.PositiveLookaround:
			case RegexNodeKind.NegativeLookaround:
			case RegexNodeKind.Atomic:
				flag = true;
				break;
			case RegexNodeKind.Capture:
				results._containsCapture.Add(node);
				break;
			case RegexNodeKind.Loop:
			case RegexNodeKind.Lazyloop:
				isInLoop = true;
				break;
			}
			int num = node.ChildCount();
			for (int i = 0; i < num; i++)
			{
				RegexNode regexNode = node.Child(i);
				bool flag2 = isAtomicByAncestor || flag;
				bool flag3 = flag2;
				if (flag3)
				{
					bool flag4;
					switch (node.Kind)
					{
					case RegexNodeKind.PositiveLookaround:
					case RegexNodeKind.NegativeLookaround:
					case RegexNodeKind.Atomic:
						flag4 = true;
						break;
					case RegexNodeKind.Alternate:
					case RegexNodeKind.BackreferenceConditional:
					case RegexNodeKind.ExpressionConditional:
						flag4 = true;
						break;
					case RegexNodeKind.Capture:
						flag4 = true;
						break;
					case RegexNodeKind.Concatenate:
						flag4 = i == num - 1;
						break;
					case RegexNodeKind.Loop:
					case RegexNodeKind.Lazyloop:
						if (node.N == 1)
						{
							flag4 = true;
							break;
						}
						goto default;
					default:
						flag4 = false;
						break;
					}
					flag3 = flag4;
				}
				bool isAtomicByAncestor2 = flag3;
				if (!TryAnalyze(regexNode, results, isAtomicByAncestor2, isInLoop))
				{
					return false;
				}
				if (results._containsCapture.Contains(regexNode))
				{
					results._containsCapture.Add(node);
				}
				if (!flag)
				{
					HashSet<RegexNode> mayBacktrack = results._mayBacktrack;
					if (mayBacktrack != null && mayBacktrack.Contains(regexNode))
					{
						analysisResults2 = results;
						(analysisResults2._mayBacktrack ?? (analysisResults2._mayBacktrack = new HashSet<RegexNode>())).Add(node);
					}
				}
			}
			return true;
		}
	}
}
