namespace System.Text.RegularExpressions.Symbolic;

internal sealed class SymbolicRegexRunnerFactory : RegexRunnerFactory
{
	private sealed class Runner<TSet> : RegexRunner where TSet : IComparable<TSet>, IEquatable<TSet>
	{
		private readonly SymbolicRegexMatcher<TSet> _matcher;

		private readonly SymbolicRegexMatcher<TSet>.PerThreadData _perThreadData;

		internal Runner(SymbolicRegexMatcher<TSet> matcher)
		{
			_matcher = matcher;
			_perThreadData = matcher.CreatePerThreadData();
		}

		protected internal override void Scan(ReadOnlySpan<char> text)
		{
			SymbolicMatch symbolicMatch = _matcher.FindMatch(_mode, text, runtextpos, _perThreadData);
			if (symbolicMatch.Success)
			{
				int index = symbolicMatch.Index;
				int end = index + symbolicMatch.Length;
				if (_mode == RegexRunnerMode.FullMatchRequired && symbolicMatch.CaptureStarts != null)
				{
					for (int i = 0; i < symbolicMatch.CaptureStarts.Length; i++)
					{
						if (symbolicMatch.CaptureStarts[i] >= 0)
						{
							Capture(i, symbolicMatch.CaptureStarts[i], symbolicMatch.CaptureEnds[i]);
						}
					}
				}
				else
				{
					Capture(0, index, end);
				}
				runtextpos = end;
			}
			else
			{
				runtextpos = text.Length;
			}
		}
	}

	internal readonly SymbolicRegexMatcher _matcher;

	public SymbolicRegexRunnerFactory(RegexTree regexTree, RegexOptions options, TimeSpan matchTimeout)
	{
		CharSetSolver charSetSolver = new CharSetSolver();
		SymbolicRegexBuilder<BDD> symbolicRegexBuilder = new SymbolicRegexBuilder<BDD>(charSetSolver, charSetSolver);
		RegexNodeConverter regexNodeConverter = new RegexNodeConverter(symbolicRegexBuilder, regexTree.CaptureNumberSparseMapping);
		SymbolicRegexNode<BDD> symbolicRegexNode = regexNodeConverter.ConvertToSymbolicRegexNode(regexTree.Root);
		int symbolicRegexSafeSizeThreshold = SymbolicRegexThresholds.GetSymbolicRegexSafeSizeThreshold();
		if (symbolicRegexSafeSizeThreshold != int.MaxValue)
		{
			int num = symbolicRegexNode.EstimateNfaSize();
			if (num > symbolicRegexSafeSizeThreshold)
			{
				throw new NotSupportedException(System.SR.Format(System.SR.NotSupported_NonBacktrackingUnsafeSize, num, symbolicRegexSafeSizeThreshold));
			}
		}
		symbolicRegexNode = symbolicRegexNode.AddFixedLengthMarkers(symbolicRegexBuilder);
		BDD[] array = symbolicRegexNode.ComputeMinterms(symbolicRegexBuilder);
		_matcher = ((array.Length > 64) ? ((SymbolicRegexMatcher)SymbolicRegexMatcher<BitVector>.Create(regexTree.CaptureCount, regexTree.FindOptimizations, symbolicRegexBuilder, symbolicRegexNode, new BitVectorSolver(array, charSetSolver), matchTimeout)) : ((SymbolicRegexMatcher)SymbolicRegexMatcher<ulong>.Create(regexTree.CaptureCount, regexTree.FindOptimizations, symbolicRegexBuilder, symbolicRegexNode, new UInt64Solver(array, charSetSolver), matchTimeout)));
	}

	protected internal override RegexRunner CreateInstance()
	{
		if (!(_matcher is SymbolicRegexMatcher<ulong> matcher))
		{
			return new Runner<BitVector>((SymbolicRegexMatcher<BitVector>)_matcher);
		}
		return new Runner<ulong>(matcher);
	}
}
