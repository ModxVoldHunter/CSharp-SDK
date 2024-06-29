using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;

namespace System.Text.RegularExpressions.Symbolic;

internal sealed class SymbolicRegexNode<TSet> where TSet : IComparable<TSet>, IEquatable<TSet>
{
	internal readonly SymbolicRegexNodeKind _kind;

	internal readonly int _lower;

	internal readonly int _upper;

	internal readonly TSet _set;

	internal readonly SymbolicRegexNode<TSet> _left;

	internal readonly SymbolicRegexNode<TSet> _right;

	internal readonly SymbolicRegexInfo _info;

	private readonly byte[] _nullabilityCache;

	internal bool IsLazy => _info.IsLazyLoop;

	internal bool IsNullable => _info.IsNullable;

	internal bool CanBeNullable => _info.CanBeNullable;

	public bool IsStar
	{
		get
		{
			if (_lower == 0)
			{
				return _upper == int.MaxValue;
			}
			return false;
		}
	}

	public bool IsEpsilon => _kind == SymbolicRegexNodeKind.Epsilon;

	internal SymbolicRegexNodeKind Kind => _kind;

	private SymbolicRegexNode(SymbolicRegexBuilder<TSet> builder, SymbolicRegexNodeKind kind, SymbolicRegexNode<TSet> left, SymbolicRegexNode<TSet> right, int lower, int upper, TSet set, SymbolicRegexInfo info)
	{
		_kind = kind;
		_left = left;
		_right = right;
		_lower = lower;
		_upper = upper;
		_set = set;
		_info = info;
		_nullabilityCache = ((info.StartsWithSomeAnchor && info.CanBeNullable) ? new byte[64] : null);
	}

	private static SymbolicRegexNode<TSet> Create(SymbolicRegexBuilder<TSet> builder, SymbolicRegexNodeKind kind, SymbolicRegexNode<TSet> left, SymbolicRegexNode<TSet> right, int lower, int upper, TSet set, SymbolicRegexInfo info)
	{
		TSet set2 = ((kind == SymbolicRegexNodeKind.Singleton) ? set : ComputeStartSet(builder, kind, left, right));
		SymbolicRegexBuilder<TSet>.NodeCacheKey key = new SymbolicRegexBuilder<TSet>.NodeCacheKey(kind, left, right, lower, upper, set2, info);
		if (!builder._nodeCache.TryGetValue(key, out var value))
		{
			value = new SymbolicRegexNode<TSet>(builder, kind, left, right, lower, upper, set2, info);
			builder._nodeCache[key] = value;
		}
		return value;
	}

	internal bool IsHighPriorityNullableFor(uint context)
	{
		if (_info.CanBeNullable)
		{
			return IsHighPriorityNullableFor(this, context);
		}
		return false;
	}

	private static bool IsHighPriorityNullableFor(SymbolicRegexNode<TSet> node, uint context)
	{
		if (!StackHelper.TryEnsureSufficientExecutionStack())
		{
			return StackHelper.CallOnEmptyStack((Func<SymbolicRegexNode<TSet>, uint, bool>)IsHighPriorityNullableFor, node, context);
		}
		while (!node._info.IsHighPriorityNullable && node._info.ContainsSomeAnchor)
		{
			switch (node._kind)
			{
			case SymbolicRegexNodeKind.Loop:
				if (node._info.IsLazyLoop)
				{
					return node._lower == 0;
				}
				return false;
			case SymbolicRegexNodeKind.Concat:
				if (!IsHighPriorityNullableFor(node._left, context))
				{
					return false;
				}
				node = node._right;
				break;
			case SymbolicRegexNodeKind.Alternate:
			case SymbolicRegexNodeKind.Effect:
			case SymbolicRegexNodeKind.DisableBacktrackingSimulation:
				if (!node._left._info.CanBeNullable)
				{
					return false;
				}
				node = node._left;
				break;
			default:
				return node.IsNullableFor(context);
			}
		}
		return node._info.IsHighPriorityNullable;
	}

	public List<SymbolicRegexNode<TSet>> ToList(List<SymbolicRegexNode<TSet>> list = null, SymbolicRegexNodeKind listKind = SymbolicRegexNodeKind.Concat)
	{
		if (list == null)
		{
			list = new List<SymbolicRegexNode<TSet>>();
		}
		AppendToList(this, list, listKind);
		return list;
		static void AppendToList(SymbolicRegexNode<TSet> concat, List<SymbolicRegexNode<TSet>> list, SymbolicRegexNodeKind listKind)
		{
			if (!StackHelper.TryEnsureSufficientExecutionStack())
			{
				StackHelper.CallOnEmptyStack(AppendToList, concat, list, listKind);
			}
			else
			{
				SymbolicRegexNode<TSet> symbolicRegexNode = concat;
				while (symbolicRegexNode._kind == listKind)
				{
					if (symbolicRegexNode._left._kind == listKind)
					{
						AppendToList(symbolicRegexNode._left, list, listKind);
					}
					else
					{
						list.Add(symbolicRegexNode._left);
					}
					symbolicRegexNode = symbolicRegexNode._right;
				}
				list.Add(symbolicRegexNode);
			}
		}
	}

	internal bool IsNullableFor(uint context)
	{
		if (_nullabilityCache != null)
		{
			return WithCache(context);
		}
		return _info.IsNullable;
		bool WithCache(uint context)
		{
			if (!StackHelper.TryEnsureSufficientExecutionStack())
			{
				return StackHelper.CallOnEmptyStack(IsNullableFor, context);
			}
			byte b = Volatile.Read(ref _nullabilityCache[context]);
			if (b != 0)
			{
				return b == 1;
			}
			bool flag;
			switch (_kind)
			{
			case SymbolicRegexNodeKind.Loop:
				flag = _lower == 0 || _left.IsNullableFor(context);
				break;
			case SymbolicRegexNodeKind.Concat:
				flag = _left.IsNullableFor(context) && _right.IsNullableFor(context);
				break;
			case SymbolicRegexNodeKind.Alternate:
				flag = _left.IsNullableFor(context) || _right.IsNullableFor(context);
				break;
			case SymbolicRegexNodeKind.BeginningAnchor:
				flag = CharKind.Prev(context) == 1;
				break;
			case SymbolicRegexNodeKind.EndAnchor:
				flag = CharKind.Next(context) == 1;
				break;
			case SymbolicRegexNodeKind.BOLAnchor:
				flag = (CharKind.Prev(context) & 3) != 0;
				break;
			case SymbolicRegexNodeKind.EOLAnchor:
				flag = (CharKind.Next(context) & 3) != 0;
				break;
			case SymbolicRegexNodeKind.BoundaryAnchor:
				flag = ((CharKind.Prev(context) & 4) ^ (CharKind.Next(context) & 4)) != 0;
				break;
			case SymbolicRegexNodeKind.NonBoundaryAnchor:
				flag = ((CharKind.Prev(context) & 4) ^ (CharKind.Next(context) & 4)) == 0;
				break;
			case SymbolicRegexNodeKind.EndAnchorZ:
				flag = (CharKind.Next(context) & 1) != 0;
				break;
			case SymbolicRegexNodeKind.CaptureStart:
			case SymbolicRegexNodeKind.CaptureEnd:
				flag = true;
				break;
			case SymbolicRegexNodeKind.Effect:
			case SymbolicRegexNodeKind.DisableBacktrackingSimulation:
				flag = _left.IsNullableFor(context);
				break;
			default:
				flag = (CharKind.Prev(context) & 1) != 0;
				break;
			}
			Volatile.Write(ref _nullabilityCache[context], (byte)(flag ? 1 : 2));
			return flag;
		}
	}

	public bool IsAnyStar(ISolver<TSet> solver)
	{
		if (IsStar && _left._kind == SymbolicRegexNodeKind.Singleton)
		{
			if (!IsLazy)
			{
				return solver.Full.Equals(_left._set);
			}
			return false;
		}
		return false;
	}

	public bool IsNothing(ISolver<TSet> solver)
	{
		if (_kind == SymbolicRegexNodeKind.Singleton)
		{
			return solver.IsEmpty(_set);
		}
		return false;
	}

	internal static SymbolicRegexNode<TSet> CreateFalse(SymbolicRegexBuilder<TSet> builder)
	{
		return Create(builder, SymbolicRegexNodeKind.Singleton, null, null, -1, -1, builder._solver.Empty, default(SymbolicRegexInfo));
	}

	internal static SymbolicRegexNode<TSet> CreateTrue(SymbolicRegexBuilder<TSet> builder)
	{
		return Create(builder, SymbolicRegexNodeKind.Singleton, null, null, -1, -1, builder._solver.Full, default(SymbolicRegexInfo));
	}

	internal static SymbolicRegexNode<TSet> CreateFixedLengthMarker(SymbolicRegexBuilder<TSet> builder, int length)
	{
		return Create(builder, SymbolicRegexNodeKind.FixedLengthMarker, null, null, length, -1, default(TSet), SymbolicRegexInfo.Epsilon());
	}

	internal static SymbolicRegexNode<TSet> CreateEpsilon(SymbolicRegexBuilder<TSet> builder)
	{
		return Create(builder, SymbolicRegexNodeKind.Epsilon, null, null, -1, -1, default(TSet), SymbolicRegexInfo.Epsilon());
	}

	internal static SymbolicRegexNode<TSet> CreateAnchor(SymbolicRegexBuilder<TSet> builder, SymbolicRegexNodeKind kind)
	{
		TSet set = default(TSet);
		bool isLineAnchor = (uint)(kind - 7) <= 3u;
		return Create(builder, kind, null, null, -1, -1, set, SymbolicRegexInfo.Anchor(isLineAnchor));
	}

	internal static SymbolicRegexNode<TSet> CreateSingleton(SymbolicRegexBuilder<TSet> builder, TSet set)
	{
		return Create(builder, SymbolicRegexNodeKind.Singleton, null, null, -1, -1, set, default(SymbolicRegexInfo));
	}

	internal static SymbolicRegexNode<TSet> CreateLoop(SymbolicRegexBuilder<TSet> builder, SymbolicRegexNode<TSet> body, int lower, int upper, bool isLazy)
	{
		if (lower == 0 && upper == 1 && body._kind == SymbolicRegexNodeKind.Loop && body._lower == 0 && body._upper == 1)
		{
			return CreateLoop(builder, body._left, 0, 1, isLazy || body.IsLazy);
		}
		return Create(builder, SymbolicRegexNodeKind.Loop, body, null, lower, upper, default(TSet), SymbolicRegexInfo.Loop(body._info, lower, isLazy));
	}

	internal static SymbolicRegexNode<TSet> CreateEffect(SymbolicRegexBuilder<TSet> builder, SymbolicRegexNode<TSet> node, SymbolicRegexNode<TSet> effectNode)
	{
		if (effectNode == builder.Epsilon)
		{
			return node;
		}
		if (node == builder._nothing)
		{
			return builder._nothing;
		}
		if (node._kind == SymbolicRegexNodeKind.Effect)
		{
			return CreateEffect(builder, node._left, CreateConcat(builder, effectNode, node._right));
		}
		return Create(builder, SymbolicRegexNodeKind.Effect, node, effectNode, -1, -1, default(TSet), SymbolicRegexInfo.Effect(node._info));
	}

	internal static SymbolicRegexNode<TSet> CreateCaptureStart(SymbolicRegexBuilder<TSet> builder, int captureNum)
	{
		return Create(builder, SymbolicRegexNodeKind.CaptureStart, null, null, captureNum, -1, default(TSet), SymbolicRegexInfo.Epsilon());
	}

	internal static SymbolicRegexNode<TSet> CreateCaptureEnd(SymbolicRegexBuilder<TSet> builder, int captureNum)
	{
		return Create(builder, SymbolicRegexNodeKind.CaptureEnd, null, null, captureNum, -1, default(TSet), SymbolicRegexInfo.Epsilon());
	}

	internal static SymbolicRegexNode<TSet> CreateDisableBacktrackingSimulation(SymbolicRegexBuilder<TSet> builder, SymbolicRegexNode<TSet> child)
	{
		return Create(builder, SymbolicRegexNodeKind.DisableBacktrackingSimulation, child, null, -1, -1, default(TSet), child._info);
	}

	internal static SymbolicRegexNode<TSet> CreateConcat(SymbolicRegexBuilder<TSet> builder, SymbolicRegexNode<TSet> left, SymbolicRegexNode<TSet> right)
	{
		if (left == builder._nothing || right == builder._nothing)
		{
			return builder._nothing;
		}
		if (left.IsEpsilon)
		{
			return right;
		}
		if (right.IsEpsilon)
		{
			return left;
		}
		if (left._kind == SymbolicRegexNodeKind.Effect)
		{
			return CreateEffect(builder, CreateConcat(builder, left._left, right), left._right);
		}
		return Create(builder, SymbolicRegexNodeKind.Concat, left, right, -1, -1, default(TSet), SymbolicRegexInfo.Concat(left._info, right._info));
	}

	internal static SymbolicRegexNode<TSet> CreateAlternate(SymbolicRegexBuilder<TSet> builder, SymbolicRegexNode<TSet> left, SymbolicRegexNode<TSet> right, bool deduplicated = false, bool hintRightLikelySubsumes = false)
	{
		if (left.IsAnyStar(builder._solver) || right.IsNothing(builder._solver) || left == right || (left.IsNullable && right.IsEpsilon))
		{
			return left;
		}
		if (left == builder._nothing)
		{
			return right;
		}
		SymbolicRegexNode<TSet> symbolicRegexNode = ((right._kind == SymbolicRegexNodeKind.Alternate) ? right._left : right);
		SymbolicRegexNode<TSet> right2 = ((right._kind == SymbolicRegexNodeKind.Alternate) ? right._right : builder._nothing);
		if (!hintRightLikelySubsumes && left.Subsumes(builder, symbolicRegexNode))
		{
			return CreateAlternate(builder, left, right2);
		}
		if (symbolicRegexNode.Subsumes(builder, left) && TryFoldAlternation(builder, left, symbolicRegexNode, out var result))
		{
			return CreateAlternate(builder, result, right2);
		}
		if (hintRightLikelySubsumes && left.Subsumes(builder, symbolicRegexNode))
		{
			return CreateAlternate(builder, left, right2);
		}
		if (!deduplicated && left._kind != SymbolicRegexNodeKind.Alternate)
		{
			SymbolicRegexNode<TSet> symbolicRegexNode2 = right;
			deduplicated = true;
			while (symbolicRegexNode2._kind == SymbolicRegexNodeKind.Alternate)
			{
				if (symbolicRegexNode2._left == left)
				{
					deduplicated = false;
					break;
				}
				symbolicRegexNode2 = symbolicRegexNode2._right;
			}
			if (deduplicated)
			{
				deduplicated = symbolicRegexNode2 != left;
			}
		}
		if (!deduplicated || left._kind == SymbolicRegexNodeKind.Alternate)
		{
			List<SymbolicRegexNode<TSet>> list = left.ToList(null, SymbolicRegexNodeKind.Alternate);
			int count = list.Count;
			right.ToList(list, SymbolicRegexNodeKind.Alternate);
			HashSet<SymbolicRegexNode<TSet>> hashSet = new HashSet<SymbolicRegexNode<TSet>>();
			bool flag = false;
			for (int i = 0; i < list.Count; i++)
			{
				if (!hashSet.Contains(list[i]))
				{
					hashSet.Add(list[i]);
					continue;
				}
				list[i] = builder._nothing;
				flag = flag || i >= count;
			}
			if (flag)
			{
				SymbolicRegexNode<TSet> symbolicRegexNode3 = builder._nothing;
				for (int num = list.Count - 1; num >= 0; num--)
				{
					symbolicRegexNode3 = CreateAlternate(builder, list[num], symbolicRegexNode3, deduplicated: true);
				}
				return symbolicRegexNode3;
			}
			SymbolicRegexNode<TSet> symbolicRegexNode4 = right;
			for (int num2 = count - 1; num2 >= 0; num2--)
			{
				symbolicRegexNode4 = CreateAlternate(builder, list[num2], symbolicRegexNode4, deduplicated: true);
			}
			return symbolicRegexNode4;
		}
		return Create(builder, SymbolicRegexNodeKind.Alternate, left, right, -1, -1, default(TSet), SymbolicRegexInfo.Alternate(left._info, right._info));
	}

	internal bool Subsumes(SymbolicRegexBuilder<TSet> builder, SymbolicRegexNode<TSet> other, int depth = 0)
	{
		if (this == other)
		{
			return true;
		}
		if (other.IsNothing(builder._solver))
		{
			return true;
		}
		if (depth >= 50)
		{
			return false;
		}
		if (builder._subsumptionCache.TryGetValue((this, other), out var value))
		{
			return value;
		}
		if (!StackHelper.TryEnsureSufficientExecutionStack())
		{
			return StackHelper.CallOnEmptyStack((Func<SymbolicRegexBuilder<TSet>, SymbolicRegexNode<TSet>, int, bool>)Subsumes, builder, other, depth);
		}
		bool? flag = ApplySubsumptionRules(builder, this, other, depth + 1);
		if (flag.HasValue)
		{
			return builder._subsumptionCache[(this, other)] = flag.Value;
		}
		return false;
		static bool? ApplySubsumptionRules(SymbolicRegexBuilder<TSet> builder, SymbolicRegexNode<TSet> left, SymbolicRegexNode<TSet> right, int depth)
		{
			if (left._kind == SymbolicRegexNodeKind.Effect)
			{
				return left._left.Subsumes(builder, right, depth);
			}
			if (right._kind == SymbolicRegexNodeKind.Effect)
			{
				return left.Subsumes(builder, right._left, depth);
			}
			if (left._kind == SymbolicRegexNodeKind.Concat && right._kind == SymbolicRegexNodeKind.Concat)
			{
				SymbolicRegexNode<TSet> left2 = right._left;
				if (left._left.IsNullable && left2._kind == SymbolicRegexNodeKind.Loop && left2._lower == 0 && left2._upper == 1 && left2.IsLazy && TrySkipPrefix(left, left2._left, out var tail2))
				{
					return tail2.Subsumes(builder, right._right, depth);
				}
			}
			if (left._kind == SymbolicRegexNodeKind.Concat && right._kind == SymbolicRegexNodeKind.Concat)
			{
				SymbolicRegexNode<TSet> left3 = left._left;
				if (left3._kind == SymbolicRegexNodeKind.Loop && left3._lower == 0 && left3._upper == 1 && left3.IsLazy && TrySkipPrefix(right, left3._left, out var tail3))
				{
					return left._right.Subsumes(builder, tail3, depth);
				}
			}
			if (left._kind == SymbolicRegexNodeKind.Concat && left._left.IsNullable)
			{
				return left._right.Subsumes(builder, right, depth);
			}
			return null;
		}
		static bool TrySkipPrefix(SymbolicRegexNode<TSet> node, SymbolicRegexNode<TSet> prefix, [NotNullWhen(true)] out SymbolicRegexNode<TSet> tail)
		{
			tail = null;
			while (prefix._kind == SymbolicRegexNodeKind.Concat)
			{
				if (node._kind != SymbolicRegexNodeKind.Concat)
				{
					return false;
				}
				if (node._left != prefix._left)
				{
					return false;
				}
				node = node._right;
				prefix = prefix._right;
			}
			if (node._kind != SymbolicRegexNodeKind.Concat)
			{
				return false;
			}
			if (node._left == prefix)
			{
				tail = node._right;
				return true;
			}
			return false;
		}
	}

	private SymbolicRegexNode<TSet> UnwrapEffects()
	{
		SymbolicRegexNode<TSet> symbolicRegexNode = this;
		while (symbolicRegexNode._kind == SymbolicRegexNodeKind.Effect)
		{
			symbolicRegexNode = symbolicRegexNode._left;
		}
		return symbolicRegexNode;
	}

	private static bool TryFoldAlternation(SymbolicRegexBuilder<TSet> builder, SymbolicRegexNode<TSet> left, SymbolicRegexNode<TSet> right, [NotNullWhen(true)] out SymbolicRegexNode<TSet> result, SymbolicRegexNode<TSet> rightEffects = null)
	{
		if (rightEffects == null)
		{
			rightEffects = builder.Epsilon;
		}
		if (left.UnwrapEffects() == right.UnwrapEffects())
		{
			result = left;
			return true;
		}
		if (left._kind == SymbolicRegexNodeKind.Effect)
		{
			if (rightEffects != builder.Epsilon)
			{
				result = null;
				return false;
			}
			if (TryFoldAlternation(builder, left._left, right, out var result2, rightEffects))
			{
				result = CreateEffect(builder, result2, left._right);
				return true;
			}
		}
		if (right._kind == SymbolicRegexNodeKind.Effect)
		{
			rightEffects = CreateConcat(builder, right._right, rightEffects);
			return TryFoldAlternation(builder, left, right._left, out result, rightEffects);
		}
		if (right._kind == SymbolicRegexNodeKind.Concat && right._left.IsNullable && TrySplitConcatSubsumption(builder, left, right, out var prefix2))
		{
			prefix2 = CreateEffect(builder, prefix2, rightEffects);
			result = builder.CreateConcat(CreateLoop(builder, prefix2, 0, 1, isLazy: true), left);
			return true;
		}
		result = null;
		return false;
		static bool TrySplitConcatSubsumption(SymbolicRegexBuilder<TSet> builder, SymbolicRegexNode<TSet> left, SymbolicRegexNode<TSet> right, [NotNullWhen(true)] out SymbolicRegexNode<TSet> prefix)
		{
			List<SymbolicRegexNode<TSet>> list = new List<SymbolicRegexNode<TSet>>();
			SymbolicRegexNode<TSet> symbolicRegexNode = right;
			while (symbolicRegexNode._kind == SymbolicRegexNodeKind.Concat)
			{
				if (symbolicRegexNode == left)
				{
					list.Reverse();
					prefix = builder.CreateConcatAlreadyReversed(list);
					return true;
				}
				if (!symbolicRegexNode._right.Subsumes(builder, left))
				{
					if (!left.Subsumes(builder, symbolicRegexNode))
					{
						break;
					}
					list.Reverse();
					prefix = builder.CreateConcatAlreadyReversed(list);
					return true;
				}
				list.Add(symbolicRegexNode._left);
				symbolicRegexNode = symbolicRegexNode._right;
			}
			prefix = null;
			return false;
		}
	}

	public int GetFixedLength()
	{
		if (!StackHelper.TryEnsureSufficientExecutionStack())
		{
			return -1;
		}
		switch (_kind)
		{
		case SymbolicRegexNodeKind.Epsilon:
		case SymbolicRegexNodeKind.BeginningAnchor:
		case SymbolicRegexNodeKind.EndAnchor:
		case SymbolicRegexNodeKind.EndAnchorZ:
		case SymbolicRegexNodeKind.EndAnchorZReverse:
		case SymbolicRegexNodeKind.BOLAnchor:
		case SymbolicRegexNodeKind.EOLAnchor:
		case SymbolicRegexNodeKind.BoundaryAnchor:
		case SymbolicRegexNodeKind.NonBoundaryAnchor:
		case SymbolicRegexNodeKind.FixedLengthMarker:
		case SymbolicRegexNodeKind.CaptureStart:
		case SymbolicRegexNodeKind.CaptureEnd:
			return 0;
		case SymbolicRegexNodeKind.Singleton:
			return 1;
		case SymbolicRegexNodeKind.Loop:
		{
			if (_lower != _upper)
			{
				break;
			}
			long num2 = _left.GetFixedLength();
			if (num2 >= 0)
			{
				num2 *= _lower;
				if (num2 <= int.MaxValue)
				{
					return (int)num2;
				}
			}
			break;
		}
		case SymbolicRegexNodeKind.Concat:
		{
			int fixedLength2 = _left.GetFixedLength();
			if (fixedLength2 < 0)
			{
				break;
			}
			int fixedLength3 = _right.GetFixedLength();
			if (fixedLength3 >= 0)
			{
				long num = (long)fixedLength2 + (long)fixedLength3;
				if (num <= int.MaxValue)
				{
					return (int)num;
				}
			}
			break;
		}
		case SymbolicRegexNodeKind.Alternate:
		{
			int fixedLength = _left.GetFixedLength();
			if (fixedLength >= 0 && _right.GetFixedLength() == fixedLength)
			{
				return fixedLength;
			}
			break;
		}
		case SymbolicRegexNodeKind.Effect:
		case SymbolicRegexNodeKind.DisableBacktrackingSimulation:
			return _left.GetFixedLength();
		}
		return -1;
	}

	public SymbolicRegexNode<TSet> AddFixedLengthMarkers(SymbolicRegexBuilder<TSet> builder, int lengthSoFar = 0)
	{
		if (!StackHelper.TryEnsureSufficientExecutionStack())
		{
			return this;
		}
		switch (_kind)
		{
		case SymbolicRegexNodeKind.Alternate:
			return CreateAlternate(builder, _left.AddFixedLengthMarkers(builder, lengthSoFar), _right.AddFixedLengthMarkers(builder, lengthSoFar), deduplicated: true);
		case SymbolicRegexNodeKind.Concat:
		{
			int fixedLength = _left.GetFixedLength();
			if (fixedLength >= 0)
			{
				return CreateConcat(builder, _left, _right.AddFixedLengthMarkers(builder, lengthSoFar + fixedLength));
			}
			if (_right.GetFixedLength() == 0)
			{
				return CreateConcat(builder, _left.AddFixedLengthMarkers(builder, lengthSoFar), _right);
			}
			break;
		}
		case SymbolicRegexNodeKind.FixedLengthMarker:
			return this;
		}
		int fixedLength2 = GetFixedLength();
		if (fixedLength2 >= 0)
		{
			return CreateConcat(builder, this, CreateFixedLengthMarker(builder, lengthSoFar + fixedLength2));
		}
		return this;
	}

	internal SymbolicRegexNode<TSet> CreateDerivativeWithoutEffects(SymbolicRegexBuilder<TSet> builder, TSet elem, uint context)
	{
		return CreateDerivativeWrapper(builder, elem, context).StripEffects(builder);
	}

	internal List<(SymbolicRegexNode<TSet>, DerivativeEffect[])> CreateNfaDerivativeWithEffects(SymbolicRegexBuilder<TSet> builder, TSet elem, uint context)
	{
		List<(SymbolicRegexNode<TSet>, DerivativeEffect[])> list = new List<(SymbolicRegexNode<TSet>, DerivativeEffect[])>();
		CreateDerivativeWrapper(builder, elem, context).StripAndMapEffects(builder, context, list);
		return list;
	}

	private SymbolicRegexNode<TSet> CreateDerivativeWrapper(SymbolicRegexBuilder<TSet> builder, TSet elem, uint context)
	{
		if (_kind == SymbolicRegexNodeKind.DisableBacktrackingSimulation)
		{
			SymbolicRegexNode<TSet> child = _left.CreateDerivative(builder, elem, context);
			return builder.CreateDisableBacktrackingSimulation(child);
		}
		SymbolicRegexNode<TSet> symbolicRegexNode = PruneLowerPriorityThanNullability(builder, context);
		return symbolicRegexNode.CreateDerivative(builder, elem, context);
	}

	private SymbolicRegexNode<TSet> PruneLowerPriorityThanNullability(SymbolicRegexBuilder<TSet> builder, uint context)
	{
		if (!IsNullableFor(context))
		{
			return this;
		}
		(SymbolicRegexNode<TSet>, uint) key = (this, context);
		if (builder._pruneLowerPriorityThanNullabilityCache.TryGetValue(key, out var value))
		{
			return value;
		}
		if (!StackHelper.TryEnsureSufficientExecutionStack())
		{
			return StackHelper.CallOnEmptyStack((Func<SymbolicRegexBuilder<TSet>, uint, SymbolicRegexNode<TSet>>)PruneLowerPriorityThanNullability, builder, context);
		}
		value = _kind switch
		{
			SymbolicRegexNodeKind.Alternate => _left.IsNullableFor(context) ? _left.PruneLowerPriorityThanNullability(builder, context) : CreateAlternate(builder, _left, _right.PruneLowerPriorityThanNullability(builder, context), deduplicated: true), 
			SymbolicRegexNodeKind.Concat => _left._kind switch
			{
				SymbolicRegexNodeKind.Concat => CreateConcat(builder, _left._left, CreateConcat(builder, _left._right, _right)).PruneLowerPriorityThanNullability(builder, context), 
				SymbolicRegexNodeKind.Alternate => _left._left.IsNullableFor(context) ? CreateConcat(builder, _left._left, _right).PruneLowerPriorityThanNullability(builder, context) : CreateAlternate(builder, CreateConcat(builder, _left._left, _right), CreateConcat(builder, _left._right, _right).PruneLowerPriorityThanNullability(builder, context), deduplicated: true), 
				SymbolicRegexNodeKind.Loop => PruneLoop(builder, context, _left, _right), 
				_ => CreateConcat(builder, _left, _right.PruneLowerPriorityThanNullability(builder, context)), 
			}, 
			SymbolicRegexNodeKind.Loop => PruneLoop(builder, context, this, builder.Epsilon), 
			SymbolicRegexNodeKind.Effect => CreateEffect(builder, _left.PruneLowerPriorityThanNullability(builder, context), _right), 
			_ => this, 
		};
		builder._pruneLowerPriorityThanNullabilityCache[key] = value;
		return value;
		static SymbolicRegexNode<TSet> PruneLoop(SymbolicRegexBuilder<TSet> builder, uint context, SymbolicRegexNode<TSet> loop, SymbolicRegexNode<TSet> tail)
		{
			if (loop._lower == 0)
			{
				if (loop.IsLazy)
				{
					return tail.PruneLowerPriorityThanNullability(builder, context);
				}
				if (!loop._left.IsNullableFor(context))
				{
					return CreateAlternate(builder, CreateConcat(builder, CreateLoop(builder, loop._left, 1, loop._upper, loop.IsLazy), tail), tail.PruneLowerPriorityThanNullability(builder, context));
				}
				if (loop._upper == int.MaxValue)
				{
					SymbolicRegexNode<TSet> symbolicRegexNode = CreateConcat(builder, loop._left.PruneLowerPriorityThanNullability(builder, context), tail.PruneLowerPriorityThanNullability(builder, context));
					if (!loop._left.IsHighPriorityNullableFor(context))
					{
						return CreateAlternate(builder, CreateConcat(builder, loop._left.PruneLowerPriorityThanNullability(builder, context), CreateConcat(builder, loop, tail)), symbolicRegexNode);
					}
					return symbolicRegexNode;
				}
			}
			return CreateConcat(builder, loop._left, CreateConcat(builder, loop.CreateLoopContinuation(builder), tail)).PruneLowerPriorityThanNullability(builder, context);
		}
	}

	private SymbolicRegexNode<TSet> CreateLoopContinuation(SymbolicRegexBuilder<TSet> builder)
	{
		int upper = ((_upper == int.MaxValue) ? int.MaxValue : (_upper - 1));
		int lower = ((_lower == 0 || _lower == int.MaxValue) ? _lower : (_lower - 1));
		return builder.CreateLoop(_left, IsLazy, lower, upper);
	}

	private SymbolicRegexNode<TSet> CreateDerivative(SymbolicRegexBuilder<TSet> builder, TSet elem, uint context)
	{
		if (!StackHelper.TryEnsureSufficientExecutionStack())
		{
			return StackHelper.CallOnEmptyStack((Func<SymbolicRegexBuilder<TSet>, TSet, uint, SymbolicRegexNode<TSet>>)CreateDerivative, builder, elem, context);
		}
		(SymbolicRegexNode<TSet>, TSet, uint) key = (this, elem, context);
		if (builder._derivativeCache.TryGetValue(key, out var value))
		{
			return value;
		}
		switch (_kind)
		{
		case SymbolicRegexNodeKind.Singleton:
			value = (builder._solver.IsEmpty(builder._solver.And(elem, _set)) ? builder._nothing : builder.Epsilon);
			break;
		case SymbolicRegexNodeKind.Concat:
		{
			if (!_left.IsNullableFor(context))
			{
				value = builder.CreateConcat(_left.CreateDerivative(builder, elem, context), _right);
				break;
			}
			SymbolicRegexNode<TSet> symbolicRegexNode = builder.CreateConcat(_left.CreateDerivative(builder, elem, context), _right);
			SymbolicRegexNode<TSet> symbolicRegexNode2 = builder.CreateEffect(_right.CreateDerivative(builder, elem, context), _left);
			value = (_left.IsHighPriorityNullableFor(context) ? CreateAlternate(builder, symbolicRegexNode2, symbolicRegexNode, deduplicated: false, hintRightLikelySubsumes: true) : CreateAlternate(builder, symbolicRegexNode, symbolicRegexNode2));
			break;
		}
		case SymbolicRegexNodeKind.Loop:
			value = ((_lower != 0 && !_left.IsNullable && _left.IsNullableFor(context)) ? builder.CreateConcat(_left, CreateLoopContinuation(builder)).CreateDerivative(builder, elem, context) : builder.CreateConcat(_left.CreateDerivative(builder, elem, context), CreateLoopContinuation(builder)));
			break;
		case SymbolicRegexNodeKind.Alternate:
			value = CreateAlternate(builder, _left.CreateDerivative(builder, elem, context), _right.CreateDerivative(builder, elem, context));
			break;
		default:
			value = builder._nothing;
			break;
		case SymbolicRegexNodeKind.Effect:
			break;
		}
		builder._derivativeCache[key] = value;
		return value;
	}

	internal SymbolicRegexNode<TSet> StripEffects(SymbolicRegexBuilder<TSet> builder)
	{
		if (!StackHelper.TryEnsureSufficientExecutionStack())
		{
			return StackHelper.CallOnEmptyStack(StripEffects, builder);
		}
		if (!_info.ContainsEffect)
		{
			return this;
		}
		switch (_kind)
		{
		case SymbolicRegexNodeKind.Effect:
			return _left.StripEffects(builder);
		case SymbolicRegexNodeKind.Concat:
			return builder.CreateConcat(_left.StripEffects(builder), _right);
		case SymbolicRegexNodeKind.Alternate:
		{
			List<SymbolicRegexNode<TSet>> list = ToList(null, SymbolicRegexNodeKind.Alternate);
			for (int i = 0; i < list.Count; i++)
			{
				list[i] = list[i].StripEffects(builder);
			}
			return builder.Alternate(list);
		}
		case SymbolicRegexNodeKind.DisableBacktrackingSimulation:
			return builder.CreateDisableBacktrackingSimulation(_left.StripEffects(builder));
		case SymbolicRegexNodeKind.Loop:
			return builder.CreateLoop(_left.StripEffects(builder), IsLazy, _lower, _upper);
		default:
			return null;
		}
	}

	internal void StripAndMapEffects(SymbolicRegexBuilder<TSet> builder, uint context, List<(SymbolicRegexNode<TSet>, DerivativeEffect[])> alternativesAndEffects, List<DerivativeEffect> currentEffects = null)
	{
		if (!StackHelper.TryEnsureSufficientExecutionStack())
		{
			StackHelper.CallOnEmptyStack(StripAndMapEffects, builder, context, alternativesAndEffects, currentEffects);
			return;
		}
		if (currentEffects == null)
		{
			currentEffects = new List<DerivativeEffect>();
		}
		if (!_info.ContainsEffect)
		{
			alternativesAndEffects.Add((this, (currentEffects.Count > 0) ? currentEffects.ToArray() : Array.Empty<DerivativeEffect>()));
			return;
		}
		switch (_kind)
		{
		case SymbolicRegexNodeKind.Effect:
		{
			int count3 = currentEffects.Count;
			_right.ApplyEffects(delegate(DerivativeEffect e, List<DerivativeEffect> s)
			{
				s.Add(e);
			}, context, currentEffects);
			_left.StripAndMapEffects(builder, context, alternativesAndEffects, currentEffects);
			currentEffects.RemoveRange(count3, currentEffects.Count - count3);
			break;
		}
		case SymbolicRegexNodeKind.Concat:
		{
			int count2 = alternativesAndEffects.Count;
			_left.StripAndMapEffects(builder, context, alternativesAndEffects, currentEffects);
			for (int j = count2; j < alternativesAndEffects.Count; j++)
			{
				(SymbolicRegexNode<TSet>, DerivativeEffect[]) tuple2 = alternativesAndEffects[j];
				SymbolicRegexNode<TSet> item3 = tuple2.Item1;
				DerivativeEffect[] item4 = tuple2.Item2;
				alternativesAndEffects[j] = (builder.CreateConcat(item3, _right), item4);
			}
			break;
		}
		case SymbolicRegexNodeKind.Alternate:
			_left.StripAndMapEffects(builder, context, alternativesAndEffects, currentEffects);
			_right.StripAndMapEffects(builder, context, alternativesAndEffects, currentEffects);
			break;
		case SymbolicRegexNodeKind.Loop:
			if (_lower == 0 && _upper == 1)
			{
				if (IsLazy)
				{
					alternativesAndEffects.Add((builder.Epsilon, (currentEffects.Count > 0) ? currentEffects.ToArray() : Array.Empty<DerivativeEffect>()));
				}
				_left.StripAndMapEffects(builder, context, alternativesAndEffects, currentEffects);
				if (!IsLazy)
				{
					alternativesAndEffects.Add((builder.Epsilon, (currentEffects.Count > 0) ? currentEffects.ToArray() : Array.Empty<DerivativeEffect>()));
				}
			}
			break;
		case SymbolicRegexNodeKind.DisableBacktrackingSimulation:
		{
			int count = alternativesAndEffects.Count;
			_left.StripAndMapEffects(builder, context, alternativesAndEffects, currentEffects);
			for (int i = count; i < alternativesAndEffects.Count; i++)
			{
				(SymbolicRegexNode<TSet>, DerivativeEffect[]) tuple = alternativesAndEffects[i];
				SymbolicRegexNode<TSet> item = tuple.Item1;
				DerivativeEffect[] item2 = tuple.Item2;
				alternativesAndEffects[i] = (builder.CreateDisableBacktrackingSimulation(item), item2);
			}
			break;
		}
		}
	}

	internal void ApplyEffects<TArg>(Action<DerivativeEffect, TArg> apply, uint context, TArg arg)
	{
		if (!StackHelper.TryEnsureSufficientExecutionStack())
		{
			StackHelper.CallOnEmptyStack(ApplyEffects, apply, context, arg);
			return;
		}
		switch (_kind)
		{
		case SymbolicRegexNodeKind.Concat:
			_left.ApplyEffects(apply, context, arg);
			_right.ApplyEffects(apply, context, arg);
			break;
		case SymbolicRegexNodeKind.Loop:
			if (_lower != 0 || (_upper != 0 && !IsLazy && _left.IsNullableFor(context)))
			{
				_left.ApplyEffects(apply, context, arg);
			}
			break;
		case SymbolicRegexNodeKind.Alternate:
			if (_left.IsNullableFor(context))
			{
				_left.ApplyEffects(apply, context, arg);
			}
			else
			{
				_right.ApplyEffects(apply, context, arg);
			}
			break;
		case SymbolicRegexNodeKind.CaptureStart:
			apply(new DerivativeEffect(DerivativeEffectKind.CaptureStart, _lower), arg);
			break;
		case SymbolicRegexNodeKind.CaptureEnd:
			apply(new DerivativeEffect(DerivativeEffectKind.CaptureEnd, _lower), arg);
			break;
		case SymbolicRegexNodeKind.DisableBacktrackingSimulation:
			_left.ApplyEffects(apply, context, arg);
			break;
		}
	}

	public HashSet<TSet> GetSets(SymbolicRegexBuilder<TSet> builder)
	{
		HashSet<TSet> hashSet = new HashSet<TSet>();
		CollectSets(builder, hashSet);
		return hashSet;
	}

	private void CollectSets(SymbolicRegexBuilder<TSet> builder, HashSet<TSet> sets)
	{
		if (!StackHelper.TryEnsureSufficientExecutionStack())
		{
			StackHelper.CallOnEmptyStack(CollectSets, builder, sets);
			return;
		}
		switch (_kind)
		{
		case SymbolicRegexNodeKind.EndAnchorZ:
		case SymbolicRegexNodeKind.EndAnchorZReverse:
		case SymbolicRegexNodeKind.BOLAnchor:
		case SymbolicRegexNodeKind.EOLAnchor:
			sets.Add(builder._newLineSet);
			break;
		case SymbolicRegexNodeKind.Epsilon:
		case SymbolicRegexNodeKind.BeginningAnchor:
		case SymbolicRegexNodeKind.EndAnchor:
		case SymbolicRegexNodeKind.FixedLengthMarker:
		case SymbolicRegexNodeKind.CaptureStart:
		case SymbolicRegexNodeKind.CaptureEnd:
			break;
		case SymbolicRegexNodeKind.Singleton:
			sets.Add(_set);
			break;
		case SymbolicRegexNodeKind.Loop:
			_left.CollectSets(builder, sets);
			break;
		case SymbolicRegexNodeKind.Alternate:
			_left.CollectSets(builder, sets);
			_right.CollectSets(builder, sets);
			break;
		case SymbolicRegexNodeKind.Concat:
		{
			SymbolicRegexNode<TSet> symbolicRegexNode = this;
			while (symbolicRegexNode._kind == SymbolicRegexNodeKind.Concat)
			{
				symbolicRegexNode._left.CollectSets(builder, sets);
				symbolicRegexNode = symbolicRegexNode._right;
			}
			symbolicRegexNode.CollectSets(builder, sets);
			break;
		}
		case SymbolicRegexNodeKind.DisableBacktrackingSimulation:
			_left.CollectSets(builder, sets);
			break;
		case SymbolicRegexNodeKind.BoundaryAnchor:
		case SymbolicRegexNodeKind.NonBoundaryAnchor:
			sets.Add(builder._wordLetterForBoundariesSet);
			break;
		case SymbolicRegexNodeKind.Effect:
			break;
		}
	}

	public TSet[] ComputeMinterms(SymbolicRegexBuilder<TSet> builder)
	{
		HashSet<TSet> sets = GetSets(builder);
		List<TSet> list = MintermGenerator<TSet>.GenerateMinterms(builder._solver, sets);
		return list.ToArray();
	}

	public SymbolicRegexNode<TSet> Reverse(SymbolicRegexBuilder<TSet> builder)
	{
		if (!StackHelper.TryEnsureSufficientExecutionStack())
		{
			return StackHelper.CallOnEmptyStack(Reverse, builder);
		}
		switch (_kind)
		{
		case SymbolicRegexNodeKind.Loop:
			return builder.CreateLoop(_left.Reverse(builder), IsLazy, _lower, _upper);
		case SymbolicRegexNodeKind.Concat:
		{
			SymbolicRegexNode<TSet> right = _left.Reverse(builder);
			SymbolicRegexNode<TSet> right2 = _right;
			while (right2._kind == SymbolicRegexNodeKind.Concat)
			{
				SymbolicRegexNode<TSet> left = right2._left.Reverse(builder);
				right = builder.CreateConcat(left, right);
				right2 = right2._right;
			}
			SymbolicRegexNode<TSet> left2 = right2.Reverse(builder);
			return builder.CreateConcat(left2, right);
		}
		case SymbolicRegexNodeKind.Alternate:
			return CreateAlternate(builder, _left.Reverse(builder), _right.Reverse(builder));
		case SymbolicRegexNodeKind.FixedLengthMarker:
			return builder.Epsilon;
		case SymbolicRegexNodeKind.BeginningAnchor:
			return builder.EndAnchor;
		case SymbolicRegexNodeKind.EndAnchor:
			return builder.BeginningAnchor;
		case SymbolicRegexNodeKind.BOLAnchor:
			return builder.EolAnchor;
		case SymbolicRegexNodeKind.EOLAnchor:
			return builder.BolAnchor;
		case SymbolicRegexNodeKind.EndAnchorZ:
			return builder.EndAnchorZReverse;
		case SymbolicRegexNodeKind.EndAnchorZReverse:
			return builder.EndAnchorZ;
		case SymbolicRegexNodeKind.DisableBacktrackingSimulation:
			return builder.CreateDisableBacktrackingSimulation(_left.Reverse(builder));
		default:
			return this;
		}
	}

	private static TSet ComputeStartSet(SymbolicRegexBuilder<TSet> builder, SymbolicRegexNodeKind kind, SymbolicRegexNode<TSet> left, SymbolicRegexNode<TSet> right)
	{
		switch (kind)
		{
		case SymbolicRegexNodeKind.Epsilon:
		case SymbolicRegexNodeKind.BeginningAnchor:
		case SymbolicRegexNodeKind.EndAnchor:
		case SymbolicRegexNodeKind.EndAnchorZ:
		case SymbolicRegexNodeKind.EndAnchorZReverse:
		case SymbolicRegexNodeKind.BOLAnchor:
		case SymbolicRegexNodeKind.EOLAnchor:
		case SymbolicRegexNodeKind.BoundaryAnchor:
		case SymbolicRegexNodeKind.NonBoundaryAnchor:
		case SymbolicRegexNodeKind.FixedLengthMarker:
		case SymbolicRegexNodeKind.CaptureStart:
		case SymbolicRegexNodeKind.CaptureEnd:
			return builder._solver.Empty;
		case SymbolicRegexNodeKind.Loop:
			return left._set;
		case SymbolicRegexNodeKind.Concat:
			if (!left.CanBeNullable)
			{
				return left._set;
			}
			return builder._solver.Or(left._set, right._set);
		case SymbolicRegexNodeKind.Alternate:
			return builder._solver.Or(left._set, right._set);
		case SymbolicRegexNodeKind.Effect:
		case SymbolicRegexNodeKind.DisableBacktrackingSimulation:
			return left._set;
		default:
			return builder._solver.Full;
		}
	}

	internal SymbolicRegexNode<TSet> PruneAnchors(SymbolicRegexBuilder<TSet> builder, uint prevKind)
	{
		TSet wordLetterForBoundariesSet = builder._wordLetterForBoundariesSet;
		bool contWithWL = CanBeNullable || !builder._solver.IsEmpty(builder._solver.And(wordLetterForBoundariesSet, _set));
		bool contWithNWL = CanBeNullable || !builder._solver.IsEmpty(builder._solver.And(builder._solver.Not(wordLetterForBoundariesSet), _set));
		return PruneAnchorsImpl(builder, prevKind, contWithWL, contWithNWL);
	}

	private SymbolicRegexNode<TSet> PruneAnchorsImpl(SymbolicRegexBuilder<TSet> builder, uint prevKind, bool contWithWL, bool contWithNWL)
	{
		if (!StackHelper.TryEnsureSufficientExecutionStack())
		{
			return StackHelper.CallOnEmptyStack((Func<SymbolicRegexBuilder<TSet>, uint, bool, bool, SymbolicRegexNode<TSet>>)PruneAnchorsImpl, builder, prevKind, contWithWL, contWithNWL);
		}
		if (!_info.StartsWithSomeAnchor)
		{
			return this;
		}
		switch (_kind)
		{
		case SymbolicRegexNodeKind.BeginningAnchor:
			if (prevKind != 1)
			{
				return builder._nothing;
			}
			return this;
		case SymbolicRegexNodeKind.EndAnchorZReverse:
			if ((prevKind & 1) == 0)
			{
				return builder._nothing;
			}
			return this;
		case SymbolicRegexNodeKind.BoundaryAnchor:
			if (!((prevKind == 4) ? contWithNWL : contWithWL))
			{
				return builder._nothing;
			}
			return this;
		case SymbolicRegexNodeKind.NonBoundaryAnchor:
			if (!((prevKind == 4) ? contWithWL : contWithNWL))
			{
				return builder._nothing;
			}
			return this;
		case SymbolicRegexNodeKind.Loop:
		{
			SymbolicRegexNode<TSet> symbolicRegexNode5 = _left.PruneAnchorsImpl(builder, prevKind, contWithWL, contWithNWL);
			if (symbolicRegexNode5 != _left)
			{
				return CreateLoop(builder, symbolicRegexNode5, _lower, _upper, IsLazy);
			}
			return this;
		}
		case SymbolicRegexNodeKind.Concat:
		{
			SymbolicRegexNode<TSet> symbolicRegexNode2 = _left.PruneAnchorsImpl(builder, prevKind, contWithWL, contWithNWL);
			SymbolicRegexNode<TSet> symbolicRegexNode3 = (_left.IsNullable ? _right.PruneAnchorsImpl(builder, prevKind, contWithWL, contWithNWL) : _right);
			if (symbolicRegexNode2 != _left || symbolicRegexNode3 != _right)
			{
				return CreateConcat(builder, symbolicRegexNode2, symbolicRegexNode3);
			}
			return this;
		}
		case SymbolicRegexNodeKind.Alternate:
		{
			SymbolicRegexNode<TSet> symbolicRegexNode6 = _left.PruneAnchorsImpl(builder, prevKind, contWithWL, contWithNWL);
			SymbolicRegexNode<TSet> symbolicRegexNode7 = _right.PruneAnchorsImpl(builder, prevKind, contWithWL, contWithNWL);
			if (symbolicRegexNode6 != _left || symbolicRegexNode7 != _right)
			{
				return CreateAlternate(builder, symbolicRegexNode6, symbolicRegexNode7);
			}
			return this;
		}
		case SymbolicRegexNodeKind.Effect:
		{
			SymbolicRegexNode<TSet> symbolicRegexNode4 = _left.PruneAnchorsImpl(builder, prevKind, contWithWL, contWithNWL);
			if (symbolicRegexNode4 != _left)
			{
				return CreateEffect(builder, symbolicRegexNode4, _right);
			}
			return this;
		}
		case SymbolicRegexNodeKind.DisableBacktrackingSimulation:
		{
			SymbolicRegexNode<TSet> symbolicRegexNode = _left.PruneAnchorsImpl(builder, prevKind, contWithWL, contWithNWL);
			if (symbolicRegexNode != _left)
			{
				return builder.CreateDisableBacktrackingSimulation(symbolicRegexNode);
			}
			return this;
		}
		default:
			return this;
		}
	}

	internal int ResolveFixedLength(uint context)
	{
		if (!StackHelper.TryEnsureSufficientExecutionStack())
		{
			return StackHelper.CallOnEmptyStack(ResolveFixedLength, context);
		}
		switch (_kind)
		{
		case SymbolicRegexNodeKind.FixedLengthMarker:
			return _lower;
		case SymbolicRegexNodeKind.Alternate:
			if (_left.IsNullableFor(context))
			{
				return _left.ResolveFixedLength(context);
			}
			return _right.ResolveFixedLength(context);
		case SymbolicRegexNodeKind.Concat:
		{
			int num = _left.ResolveFixedLength(context);
			if (num < 0)
			{
				return _right.ResolveFixedLength(context);
			}
			return num;
		}
		default:
			return -1;
		}
	}

	internal IEnumerable<SymbolicRegexNode<TSet>> EnumerateAlternationBranches(SymbolicRegexBuilder<TSet> builder)
	{
		switch (_kind)
		{
		case SymbolicRegexNodeKind.DisableBacktrackingSimulation:
			foreach (SymbolicRegexNode<TSet> item in _left.EnumerateAlternationBranches(builder))
			{
				yield return builder.CreateDisableBacktrackingSimulation(item);
			}
			break;
		case SymbolicRegexNodeKind.Alternate:
		{
			SymbolicRegexNode<TSet> current = this;
			while (current._kind == SymbolicRegexNodeKind.Alternate)
			{
				yield return current._left;
				current = current._right;
			}
			yield return current;
			break;
		}
		default:
			yield return this;
			break;
		}
	}

	internal int EstimateNfaSize()
	{
		return Times((!_info.ContainsSomeAnchor) ? 1 : 5, Sum(1, CountSingletons()));
	}

	internal int CountSingletons()
	{
		if (!StackHelper.TryEnsureSufficientExecutionStack())
		{
			return StackHelper.CallOnEmptyStack(CountSingletons);
		}
		switch (_kind)
		{
		case SymbolicRegexNodeKind.Singleton:
			return 1;
		case SymbolicRegexNodeKind.Concat:
		case SymbolicRegexNodeKind.Alternate:
			return Sum(_left.CountSingletons(), _right.CountSingletons());
		case SymbolicRegexNodeKind.Loop:
			if (_upper == int.MaxValue)
			{
				if (_lower == 0 || _lower == int.MaxValue)
				{
					return _left.CountSingletons();
				}
				return Times(_lower + 1, _left.CountSingletons());
			}
			return Times(_upper, _left.CountSingletons());
		case SymbolicRegexNodeKind.Effect:
		case SymbolicRegexNodeKind.DisableBacktrackingSimulation:
			return _left.CountSingletons();
		default:
			return 0;
		}
	}

	private static int Sum(int m, int n)
	{
		return (int)Math.Min((long)m + (long)n, 2147483647L);
	}

	private static int Times(int m, int n)
	{
		return (int)Math.Min((long)m * (long)n, 2147483647L);
	}
}
