using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Threading;

namespace System.Text.RegularExpressions.Symbolic;

internal sealed class SymbolicRegexBuilder<TSet> where TSet : IComparable<TSet>, IEquatable<TSet>
{
	internal readonly struct NodeCacheKey : IEquatable<NodeCacheKey>
	{
		public readonly SymbolicRegexNodeKind Kind;

		public readonly SymbolicRegexNode<TSet> Left;

		public readonly SymbolicRegexNode<TSet> Right;

		public readonly int Lower;

		public readonly int Upper;

		public readonly TSet Set;

		public readonly SymbolicRegexInfo Info;

		public NodeCacheKey(SymbolicRegexNodeKind kind, SymbolicRegexNode<TSet> left, SymbolicRegexNode<TSet> right, int lower, int upper, TSet set, SymbolicRegexInfo info)
		{
			Kind = kind;
			Left = left;
			Right = right;
			Lower = lower;
			Upper = upper;
			Set = set;
			Info = info;
		}

		public override int GetHashCode()
		{
			return HashCode.Combine((int)Kind, Left, Right, Lower, Upper, Set, Info);
		}

		public override bool Equals([NotNullWhen(true)] object obj)
		{
			if (obj is NodeCacheKey other)
			{
				return Equals(other);
			}
			return false;
		}

		public bool Equals(NodeCacheKey other)
		{
			if (Kind == other.Kind && Left == other.Left && Right == other.Right && Lower == other.Lower && Upper == other.Upper && EqualityComparer<TSet>.Default.Equals(Set, other.Set))
			{
				return EqualityComparer<SymbolicRegexInfo>.Default.Equals(Info, other.Info);
			}
			return false;
		}
	}

	internal readonly CharSetSolver _charSetSolver;

	internal readonly ISolver<TSet> _solver;

	internal readonly SymbolicRegexNode<TSet> _nothing;

	internal readonly SymbolicRegexNode<TSet> _anyChar;

	internal readonly SymbolicRegexNode<TSet> _anyStar;

	internal readonly SymbolicRegexNode<TSet> _anyStarLazy;

	private SymbolicRegexNode<TSet> _epsilon;

	private SymbolicRegexNode<TSet> _beginningAnchor;

	private SymbolicRegexNode<TSet> _endAnchor;

	private SymbolicRegexNode<TSet> _endAnchorZ;

	private SymbolicRegexNode<TSet> _endAnchorZReverse;

	private SymbolicRegexNode<TSet> _bolAnchor;

	private SymbolicRegexNode<TSet> _eolAnchor;

	private SymbolicRegexNode<TSet> _wbAnchor;

	private SymbolicRegexNode<TSet> _nwbAnchor;

	internal TSet _wordLetterForBoundariesSet;

	internal TSet _newLineSet;

	private readonly Dictionary<TSet, SymbolicRegexNode<TSet>> _singletonCache = new Dictionary<TSet, SymbolicRegexNode<TSet>>();

	internal readonly Dictionary<NodeCacheKey, SymbolicRegexNode<TSet>> _nodeCache = new Dictionary<NodeCacheKey, SymbolicRegexNode<TSet>>();

	internal readonly Dictionary<(SymbolicRegexNode<TSet>, TSet elem, uint context), SymbolicRegexNode<TSet>> _derivativeCache = new Dictionary<(SymbolicRegexNode<TSet>, TSet, uint), SymbolicRegexNode<TSet>>();

	internal readonly Dictionary<(SymbolicRegexNode<TSet>, uint), SymbolicRegexNode<TSet>> _pruneLowerPriorityThanNullabilityCache = new Dictionary<(SymbolicRegexNode<TSet>, uint), SymbolicRegexNode<TSet>>();

	internal readonly Dictionary<(SymbolicRegexNode<TSet>, SymbolicRegexNode<TSet>), bool> _subsumptionCache = new Dictionary<(SymbolicRegexNode<TSet>, SymbolicRegexNode<TSet>), bool>();

	internal SymbolicRegexNode<TSet> Epsilon => _epsilon ?? (_epsilon = SymbolicRegexNode<TSet>.CreateEpsilon(this));

	internal SymbolicRegexNode<TSet> BeginningAnchor => _beginningAnchor ?? (_beginningAnchor = SymbolicRegexNode<TSet>.CreateAnchor(this, SymbolicRegexNodeKind.BeginningAnchor));

	internal SymbolicRegexNode<TSet> EndAnchor => _endAnchor ?? (_endAnchor = SymbolicRegexNode<TSet>.CreateAnchor(this, SymbolicRegexNodeKind.EndAnchor));

	internal SymbolicRegexNode<TSet> EndAnchorZ => _endAnchorZ ?? (_endAnchorZ = SymbolicRegexNode<TSet>.CreateAnchor(this, SymbolicRegexNodeKind.EndAnchorZ));

	internal SymbolicRegexNode<TSet> EndAnchorZReverse => _endAnchorZReverse ?? (_endAnchorZReverse = SymbolicRegexNode<TSet>.CreateAnchor(this, SymbolicRegexNodeKind.EndAnchorZReverse));

	internal SymbolicRegexNode<TSet> BolAnchor => _bolAnchor ?? (_bolAnchor = SymbolicRegexNode<TSet>.CreateAnchor(this, SymbolicRegexNodeKind.BOLAnchor));

	internal SymbolicRegexNode<TSet> EolAnchor => _eolAnchor ?? (_eolAnchor = SymbolicRegexNode<TSet>.CreateAnchor(this, SymbolicRegexNodeKind.EOLAnchor));

	internal SymbolicRegexNode<TSet> BoundaryAnchor => _wbAnchor ?? (_wbAnchor = SymbolicRegexNode<TSet>.CreateAnchor(this, SymbolicRegexNodeKind.BoundaryAnchor));

	internal SymbolicRegexNode<TSet> NonBoundaryAnchor => _nwbAnchor ?? (_nwbAnchor = SymbolicRegexNode<TSet>.CreateAnchor(this, SymbolicRegexNodeKind.NonBoundaryAnchor));

	internal SymbolicRegexBuilder(ISolver<TSet> solver, CharSetSolver charSetSolver)
	{
		_charSetSolver = charSetSolver;
		_solver = solver;
		_wordLetterForBoundariesSet = solver.Empty;
		_newLineSet = solver.Empty;
		_nothing = SymbolicRegexNode<TSet>.CreateFalse(this);
		_anyChar = SymbolicRegexNode<TSet>.CreateTrue(this);
		_anyStar = SymbolicRegexNode<TSet>.CreateLoop(this, _anyChar, 0, int.MaxValue, isLazy: false);
		_anyStarLazy = SymbolicRegexNode<TSet>.CreateLoop(this, _anyChar, 0, int.MaxValue, isLazy: true);
		_singletonCache[_solver.Empty] = _nothing;
		_singletonCache[_solver.Full] = _anyChar;
	}

	internal SymbolicRegexNode<TSet> Alternate(List<SymbolicRegexNode<TSet>> nodes)
	{
		HashSet<SymbolicRegexNode<TSet>> hashSet = new HashSet<SymbolicRegexNode<TSet>>();
		for (int i = 0; i < nodes.Count; i++)
		{
			if (!hashSet.Add(nodes[i]))
			{
				nodes[i] = _nothing;
			}
		}
		SymbolicRegexNode<TSet> symbolicRegexNode = _nothing;
		for (int num = nodes.Count - 1; num >= 0; num--)
		{
			symbolicRegexNode = SymbolicRegexNode<TSet>.CreateAlternate(this, nodes[num], symbolicRegexNode, deduplicated: true);
		}
		return symbolicRegexNode;
	}

	internal SymbolicRegexNode<TSet> CreateConcatAlreadyReversed(IEnumerable<SymbolicRegexNode<TSet>> nodes)
	{
		SymbolicRegexNode<TSet> symbolicRegexNode = Epsilon;
		foreach (SymbolicRegexNode<TSet> node in nodes)
		{
			if (node == _nothing)
			{
				return _nothing;
			}
			symbolicRegexNode = SymbolicRegexNode<TSet>.CreateConcat(this, node, symbolicRegexNode);
		}
		return symbolicRegexNode;
	}

	internal SymbolicRegexNode<TSet> CreateConcat(SymbolicRegexNode<TSet> left, SymbolicRegexNode<TSet> right)
	{
		return SymbolicRegexNode<TSet>.CreateConcat(this, left, right);
	}

	internal SymbolicRegexNode<TSet> CreateLoop(SymbolicRegexNode<TSet> node, bool isLazy, int lower = 0, int upper = int.MaxValue)
	{
		if (lower == 1 && upper == 1)
		{
			return node;
		}
		if (lower == 0 && upper == 0)
		{
			return Epsilon;
		}
		if (!isLazy && lower == 0 && upper == int.MaxValue && node._kind == SymbolicRegexNodeKind.Singleton && _solver.IsFull(node._set))
		{
			return _anyStar;
		}
		if (node.Kind == SymbolicRegexNodeKind.Loop && node._lower == 0 && node._upper == 1 && lower == 0 && upper == 1)
		{
			if (node.IsLazy != isLazy)
			{
				return SymbolicRegexNode<TSet>.CreateLoop(this, node._left, 0, 1, isLazy);
			}
			return node;
		}
		return SymbolicRegexNode<TSet>.CreateLoop(this, node, lower, upper, isLazy);
	}

	internal SymbolicRegexNode<TSet> CreateSingleton(TSet set)
	{
		bool exists;
		ref SymbolicRegexNode<TSet> valueRefOrAddDefault = ref CollectionsMarshal.GetValueRefOrAddDefault(_singletonCache, set, out exists);
		return valueRefOrAddDefault ?? (valueRefOrAddDefault = SymbolicRegexNode<TSet>.CreateSingleton(this, set));
	}

	internal SymbolicRegexNode<TSet> CreateFixedLengthMarker(int length)
	{
		return SymbolicRegexNode<TSet>.CreateFixedLengthMarker(this, length);
	}

	internal SymbolicRegexNode<TSet> CreateEffect(SymbolicRegexNode<TSet> node, SymbolicRegexNode<TSet> effectNode)
	{
		return SymbolicRegexNode<TSet>.CreateEffect(this, node, effectNode);
	}

	internal SymbolicRegexNode<TSet> CreateCaptureStart(int captureNum)
	{
		return SymbolicRegexNode<TSet>.CreateCaptureStart(this, captureNum);
	}

	internal SymbolicRegexNode<TSet> CreateCaptureEnd(int captureNum)
	{
		return SymbolicRegexNode<TSet>.CreateCaptureEnd(this, captureNum);
	}

	internal SymbolicRegexNode<TSet> CreateDisableBacktrackingSimulation(SymbolicRegexNode<TSet> child)
	{
		if (child != _nothing)
		{
			return SymbolicRegexNode<TSet>.CreateDisableBacktrackingSimulation(this, child);
		}
		return _nothing;
	}

	internal SymbolicRegexNode<TNewSet> Transform<TNewSet>(SymbolicRegexNode<TSet> node, SymbolicRegexBuilder<TNewSet> builder, Func<SymbolicRegexBuilder<TNewSet>, TSet, TNewSet> setTransformer) where TNewSet : IComparable<TNewSet>, IEquatable<TNewSet>
	{
		if (!StackHelper.TryEnsureSufficientExecutionStack())
		{
			return StackHelper.CallOnEmptyStack((Func<SymbolicRegexNode<TSet>, SymbolicRegexBuilder<TNewSet>, Func<SymbolicRegexBuilder<TNewSet>, TSet, TNewSet>, SymbolicRegexNode<TNewSet>>)Transform, node, builder, setTransformer);
		}
		switch (node._kind)
		{
		case SymbolicRegexNodeKind.BeginningAnchor:
			return builder.BeginningAnchor;
		case SymbolicRegexNodeKind.EndAnchor:
			return builder.EndAnchor;
		case SymbolicRegexNodeKind.EndAnchorZ:
			return builder.EndAnchorZ;
		case SymbolicRegexNodeKind.EndAnchorZReverse:
			return builder.EndAnchorZReverse;
		case SymbolicRegexNodeKind.BOLAnchor:
			return builder.BolAnchor;
		case SymbolicRegexNodeKind.EOLAnchor:
			return builder.EolAnchor;
		case SymbolicRegexNodeKind.BoundaryAnchor:
			return builder.BoundaryAnchor;
		case SymbolicRegexNodeKind.NonBoundaryAnchor:
			return builder.NonBoundaryAnchor;
		case SymbolicRegexNodeKind.FixedLengthMarker:
			return builder.CreateFixedLengthMarker(node._lower);
		case SymbolicRegexNodeKind.Epsilon:
			return builder.Epsilon;
		case SymbolicRegexNodeKind.Singleton:
			return builder.CreateSingleton(setTransformer(builder, node._set));
		case SymbolicRegexNodeKind.Loop:
			return builder.CreateLoop(Transform(node._left, builder, setTransformer), node.IsLazy, node._lower, node._upper);
		case SymbolicRegexNodeKind.Alternate:
			return SymbolicRegexNode<TNewSet>.CreateAlternate(builder, Transform(node._left, builder, setTransformer), Transform(node._right, builder, setTransformer), deduplicated: true);
		case SymbolicRegexNodeKind.CaptureStart:
			return builder.CreateCaptureStart(node._lower);
		case SymbolicRegexNodeKind.CaptureEnd:
			return builder.CreateCaptureEnd(node._lower);
		case SymbolicRegexNodeKind.Concat:
		{
			List<SymbolicRegexNode<TSet>> list = node.ToList();
			SymbolicRegexNode<TNewSet>[] array = new SymbolicRegexNode<TNewSet>[list.Count];
			for (int i = 0; i < array.Length; i++)
			{
				int num = i;
				int num2 = i + 1;
				array[num] = Transform(list[list.Count - num2], builder, setTransformer);
			}
			return builder.CreateConcatAlreadyReversed(array);
		}
		case SymbolicRegexNodeKind.DisableBacktrackingSimulation:
			return builder.CreateDisableBacktrackingSimulation(Transform(node._left, builder, setTransformer));
		default:
			return null;
		}
	}
}
