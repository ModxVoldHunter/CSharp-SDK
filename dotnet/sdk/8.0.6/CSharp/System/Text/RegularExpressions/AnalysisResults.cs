using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace System.Text.RegularExpressions;

internal sealed class AnalysisResults
{
	internal bool _complete;

	internal readonly HashSet<RegexNode> _isAtomicByAncestor = new HashSet<RegexNode>();

	internal readonly HashSet<RegexNode> _containsCapture = new HashSet<RegexNode>();

	internal HashSet<RegexNode> _mayBacktrack;

	internal HashSet<RegexNode> _inLoops;

	internal bool _hasIgnoreCase;

	internal bool _hasRightToLeft;

	[CompilerGenerated]
	private readonly RegexTree _003CRegexTree_003Ek__BackingField;

	public bool HasRightToLeft
	{
		get
		{
			if (_complete)
			{
				return _hasRightToLeft;
			}
			return true;
		}
	}

	internal AnalysisResults(RegexTree regexTree)
	{
		_003CRegexTree_003Ek__BackingField = regexTree;
	}

	public bool IsAtomicByAncestor(RegexNode node)
	{
		return _isAtomicByAncestor.Contains(node);
	}

	public bool MayContainCapture(RegexNode node)
	{
		if (_complete)
		{
			return _containsCapture.Contains(node);
		}
		return true;
	}

	public bool MayBacktrack(RegexNode node)
	{
		if (_complete)
		{
			return _mayBacktrack?.Contains(node) ?? false;
		}
		return true;
	}

	public bool IsInLoop(RegexNode node)
	{
		if (_complete)
		{
			return _inLoops?.Contains(node) ?? false;
		}
		return true;
	}
}
