using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace System.Text.RegularExpressions.Symbolic;

internal sealed class MatchingState<TSet> where TSet : IComparable<TSet>, IEquatable<TSet>
{
	internal SymbolicRegexNode<TSet> Node { get; }

	internal uint PrevCharKind { get; }

	internal int Id { get; set; }

	internal bool StartsWithLineAnchor => Node._info.StartsWithLineAnchor;

	internal MatchingState(SymbolicRegexNode<TSet> node, uint prevCharKind)
	{
		Node = node;
		PrevCharKind = prevCharKind;
	}

	internal bool IsDeadend(ISolver<TSet> solver)
	{
		return Node.IsNothing(solver);
	}

	internal int FixedLength(uint nextCharKind)
	{
		uint context = CharKind.Context(PrevCharKind, nextCharKind);
		return Node.ResolveFixedLength(context);
	}

	internal SymbolicRegexNode<TSet> Next(SymbolicRegexBuilder<TSet> builder, TSet minterm, uint nextCharKind)
	{
		uint context = CharKind.Context(PrevCharKind, nextCharKind);
		return Node.CreateDerivativeWithoutEffects(builder, minterm, context);
	}

	internal List<(SymbolicRegexNode<TSet> Node, DerivativeEffect[] Effects)> NfaNextWithEffects(SymbolicRegexBuilder<TSet> builder, TSet minterm, uint nextCharKind)
	{
		uint context = CharKind.Context(PrevCharKind, nextCharKind);
		return Node.CreateNfaDerivativeWithEffects(builder, minterm, context);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal bool IsNullableFor(uint nextCharKind)
	{
		uint context = CharKind.Context(PrevCharKind, nextCharKind);
		return Node.IsNullableFor(context);
	}

	internal StateFlags BuildStateFlags(ISolver<TSet> solver, bool isInitial)
	{
		StateFlags stateFlags = (StateFlags)0;
		if (isInitial)
		{
			stateFlags |= StateFlags.IsInitialFlag;
		}
		if (IsDeadend(solver))
		{
			stateFlags |= StateFlags.IsDeadendFlag;
		}
		if (Node.CanBeNullable)
		{
			stateFlags |= StateFlags.CanBeNullableFlag;
			if (Node.IsNullable)
			{
				stateFlags |= StateFlags.IsNullableFlag;
			}
		}
		if (Node.Kind != SymbolicRegexNodeKind.DisableBacktrackingSimulation)
		{
			stateFlags |= StateFlags.SimulatesBacktrackingFlag;
		}
		return stateFlags;
	}

	public override bool Equals(object obj)
	{
		if (obj is MatchingState<TSet> matchingState && PrevCharKind == matchingState.PrevCharKind)
		{
			return Node.Equals(matchingState.Node);
		}
		return false;
	}

	public override int GetHashCode()
	{
		return HashCode.Combine(PrevCharKind, Node);
	}
}
