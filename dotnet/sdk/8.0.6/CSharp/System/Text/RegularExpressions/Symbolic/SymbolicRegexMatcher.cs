using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;

namespace System.Text.RegularExpressions.Symbolic;

internal sealed class SymbolicRegexMatcher<TSet> : SymbolicRegexMatcher where TSet : IComparable<TSet>, IEquatable<TSet>
{
	internal struct Registers
	{
		public int[] CaptureStarts { get; set; }

		public int[] CaptureEnds { get; set; }

		public Registers(int[] captureStarts, int[] captureEnds)
		{
			CaptureStarts = captureStarts;
			CaptureEnds = captureEnds;
		}

		public void ApplyEffects(DerivativeEffect[] effects, int pos)
		{
			foreach (DerivativeEffect effect in effects)
			{
				ApplyEffect(effect, pos);
			}
		}

		public void ApplyEffect(DerivativeEffect effect, int pos)
		{
			switch (effect.Kind)
			{
			case DerivativeEffectKind.CaptureStart:
				CaptureStarts[effect.CaptureNumber] = pos;
				break;
			case DerivativeEffectKind.CaptureEnd:
				CaptureEnds[effect.CaptureNumber] = pos;
				break;
			}
		}

		public Registers Clone()
		{
			return new Registers((int[])CaptureStarts.Clone(), (int[])CaptureEnds.Clone());
		}
	}

	internal sealed class PerThreadData
	{
		public readonly NfaMatchingState NfaState;

		public readonly SparseIntMap<Registers> Current;

		public readonly SparseIntMap<Registers> Next;

		public readonly Registers InitialRegisters;

		public PerThreadData(int capsize)
		{
			NfaState = new NfaMatchingState();
			if (capsize > 1)
			{
				Current = new SparseIntMap<Registers>();
				Next = new SparseIntMap<Registers>();
				InitialRegisters = new Registers(new int[capsize], new int[capsize]);
			}
		}
	}

	internal sealed class NfaMatchingState
	{
		public SparseIntMap<int> NfaStateSet = new SparseIntMap<int>();

		public SparseIntMap<int> NfaStateSetScratch = new SparseIntMap<int>();

		public void InitializeFrom(SymbolicRegexMatcher<TSet> matcher, MatchingState<TSet> dfaMatchingState)
		{
			NfaStateSet.Clear();
			matcher.ForEachNfaState(dfaMatchingState.Node, dfaMatchingState.PrevCharKind, NfaStateSet, delegate(int nfaId, SparseIntMap<int> nfaStateSet)
			{
				nfaStateSet.Add(nfaId, out var _);
			});
		}
	}

	private struct CurrentState
	{
		public int DfaStateId;

		public NfaMatchingState NfaState;

		public CurrentState(MatchingState<TSet> dfaState)
		{
			DfaStateId = dfaState.Id;
			NfaState = null;
		}

		public CurrentState(NfaMatchingState nfaState)
		{
			DfaStateId = -1;
			NfaState = nfaState;
		}
	}

	private interface IStateHandler
	{
		static abstract bool StartsWithLineAnchor(SymbolicRegexMatcher<TSet> matcher, in CurrentState state);

		static abstract bool IsNullableFor(SymbolicRegexMatcher<TSet> matcher, in CurrentState state, uint nextCharKind);

		static abstract int ExtractNullableCoreStateId(SymbolicRegexMatcher<TSet> matcher, in CurrentState state, ReadOnlySpan<char> input, int pos);

		static abstract int FixedLength(SymbolicRegexMatcher<TSet> matcher, in CurrentState state, uint nextCharKind);

		static abstract bool TryTakeTransition(SymbolicRegexMatcher<TSet> matcher, ref CurrentState state, int mintermId);

		static abstract StateFlags GetStateFlags(SymbolicRegexMatcher<TSet> matcher, in CurrentState state);
	}

	[StructLayout(LayoutKind.Sequential, Size = 1)]
	private readonly struct DfaStateHandler : IStateHandler
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool StartsWithLineAnchor(SymbolicRegexMatcher<TSet> matcher, in CurrentState state)
		{
			return matcher.GetState(state.DfaStateId).StartsWithLineAnchor;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool IsNullableFor(SymbolicRegexMatcher<TSet> matcher, in CurrentState state, uint nextCharKind)
		{
			return matcher.GetState(state.DfaStateId).IsNullableFor(nextCharKind);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int ExtractNullableCoreStateId(SymbolicRegexMatcher<TSet> matcher, in CurrentState state, ReadOnlySpan<char> input, int pos)
		{
			return state.DfaStateId;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int FixedLength(SymbolicRegexMatcher<TSet> matcher, in CurrentState state, uint nextCharKind)
		{
			return matcher.GetState(state.DfaStateId).FixedLength(nextCharKind);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool TryTakeTransition(SymbolicRegexMatcher<TSet> matcher, ref CurrentState state, int mintermId)
		{
			int num = matcher.DeltaOffset(state.DfaStateId, mintermId);
			int num2 = matcher._dfaDelta[num];
			if (num2 > 0)
			{
				state.DfaStateId = num2;
				return true;
			}
			if (matcher.TryCreateNewTransition(matcher.GetState(state.DfaStateId), mintermId, num, checkThreshold: true, out var nextState))
			{
				state.DfaStateId = nextState.Id;
				return true;
			}
			return false;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static StateFlags GetStateFlags(SymbolicRegexMatcher<TSet> matcher, in CurrentState state)
		{
			return matcher._stateFlagsArray[state.DfaStateId];
		}

		static bool IStateHandler.StartsWithLineAnchor(SymbolicRegexMatcher<TSet> matcher, in CurrentState state)
		{
			return StartsWithLineAnchor(matcher, in state);
		}

		static bool IStateHandler.IsNullableFor(SymbolicRegexMatcher<TSet> matcher, in CurrentState state, uint nextCharKind)
		{
			return IsNullableFor(matcher, in state, nextCharKind);
		}

		static int IStateHandler.ExtractNullableCoreStateId(SymbolicRegexMatcher<TSet> matcher, in CurrentState state, ReadOnlySpan<char> input, int pos)
		{
			return ExtractNullableCoreStateId(matcher, in state, input, pos);
		}

		static int IStateHandler.FixedLength(SymbolicRegexMatcher<TSet> matcher, in CurrentState state, uint nextCharKind)
		{
			return FixedLength(matcher, in state, nextCharKind);
		}

		static StateFlags IStateHandler.GetStateFlags(SymbolicRegexMatcher<TSet> matcher, in CurrentState state)
		{
			return GetStateFlags(matcher, in state);
		}
	}

	[StructLayout(LayoutKind.Sequential, Size = 1)]
	private readonly struct NfaStateHandler : IStateHandler
	{
		public static bool StartsWithLineAnchor(SymbolicRegexMatcher<TSet> matcher, in CurrentState state)
		{
			Span<KeyValuePair<int, int>> span = CollectionsMarshal.AsSpan(state.NfaState.NfaStateSet.Values);
			for (int i = 0; i < span.Length; i++)
			{
				if (matcher.GetState(matcher.GetCoreStateId(span[i].Key)).StartsWithLineAnchor)
				{
					return true;
				}
			}
			return false;
		}

		public static bool IsNullableFor(SymbolicRegexMatcher<TSet> matcher, in CurrentState state, uint nextCharKind)
		{
			Span<KeyValuePair<int, int>> span = CollectionsMarshal.AsSpan(state.NfaState.NfaStateSet.Values);
			for (int i = 0; i < span.Length; i++)
			{
				if (matcher.GetState(matcher.GetCoreStateId(span[i].Key)).IsNullableFor(nextCharKind))
				{
					return true;
				}
			}
			return false;
		}

		public static int ExtractNullableCoreStateId(SymbolicRegexMatcher<TSet> matcher, in CurrentState state, ReadOnlySpan<char> input, int pos)
		{
			uint charKind = matcher.GetCharKind<FullInputReader>(input, pos);
			Span<KeyValuePair<int, int>> span = CollectionsMarshal.AsSpan(state.NfaState.NfaStateSet.Values);
			for (int i = 0; i < span.Length; i++)
			{
				MatchingState<TSet> state2 = matcher.GetState(matcher.GetCoreStateId(span[i].Key));
				if (state2.IsNullableFor(charKind))
				{
					return state2.Id;
				}
			}
			return -1;
		}

		public static int FixedLength(SymbolicRegexMatcher<TSet> matcher, in CurrentState state, uint nextCharKind)
		{
			Span<KeyValuePair<int, int>> span = CollectionsMarshal.AsSpan(state.NfaState.NfaStateSet.Values);
			for (int i = 0; i < span.Length; i++)
			{
				MatchingState<TSet> state2 = matcher.GetState(matcher.GetCoreStateId(span[i].Key));
				if (state2.IsNullableFor(nextCharKind))
				{
					return state2.FixedLength(nextCharKind);
				}
			}
			return -1;
		}

		public static bool TryTakeTransition(SymbolicRegexMatcher<TSet> matcher, ref CurrentState state, int mintermId)
		{
			NfaMatchingState nfaState = state.NfaState;
			SparseIntMap<int> nfaStateSetScratch = nfaState.NfaStateSetScratch;
			SparseIntMap<int> nfaStateSet = nfaState.NfaStateSet;
			nfaState.NfaStateSet = nfaStateSetScratch;
			nfaState.NfaStateSetScratch = nfaStateSet;
			nfaStateSetScratch.Clear();
			int index;
			if (nfaStateSet.Count == 1)
			{
				int[] array = GetNextStates(nfaStateSet.Values[0].Key, mintermId, matcher);
				foreach (int key in array)
				{
					nfaStateSetScratch.Add(key, out index);
				}
			}
			else
			{
				uint positionKind = matcher.GetPositionKind(mintermId);
				Span<KeyValuePair<int, int>> span = CollectionsMarshal.AsSpan(nfaStateSet.Values);
				for (int j = 0; j < span.Length; j++)
				{
					ref KeyValuePair<int, int> reference = ref span[j];
					int[] array2 = GetNextStates(reference.Key, mintermId, matcher);
					foreach (int key2 in array2)
					{
						nfaStateSetScratch.Add(key2, out index);
					}
					int coreStateId = matcher.GetCoreStateId(reference.Key);
					StateFlags info = matcher._stateFlagsArray[coreStateId];
					if (info.SimulatesBacktracking() && (info.IsNullable() || (info.CanBeNullable() && matcher.GetState(coreStateId).IsNullableFor(positionKind))))
					{
						break;
					}
				}
			}
			return true;
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			static int[] GetNextStates(int sourceState, int mintermId, SymbolicRegexMatcher<TSet> matcher)
			{
				int num = matcher.DeltaOffset(sourceState, mintermId);
				return matcher._nfaDelta[num] ?? matcher.CreateNewNfaTransition(sourceState, mintermId, num);
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static StateFlags GetStateFlags(SymbolicRegexMatcher<TSet> matcher, in CurrentState state)
		{
			SparseIntMap<int> nfaStateSet = state.NfaState.NfaStateSet;
			if (nfaStateSet.Count == 0)
			{
				return StateFlags.IsDeadendFlag;
			}
			StateFlags stateFlags = (StateFlags)0;
			Span<KeyValuePair<int, int>> span = CollectionsMarshal.AsSpan(nfaStateSet.Values);
			for (int i = 0; i < span.Length; i++)
			{
				ref KeyValuePair<int, int> reference = ref span[i];
				stateFlags |= matcher._stateFlagsArray[matcher.GetCoreStateId(reference.Key)];
			}
			return stateFlags & (StateFlags.IsNullableFlag | StateFlags.CanBeNullableFlag | StateFlags.SimulatesBacktrackingFlag);
		}

		static bool IStateHandler.StartsWithLineAnchor(SymbolicRegexMatcher<TSet> matcher, in CurrentState state)
		{
			return StartsWithLineAnchor(matcher, in state);
		}

		static bool IStateHandler.IsNullableFor(SymbolicRegexMatcher<TSet> matcher, in CurrentState state, uint nextCharKind)
		{
			return IsNullableFor(matcher, in state, nextCharKind);
		}

		static int IStateHandler.ExtractNullableCoreStateId(SymbolicRegexMatcher<TSet> matcher, in CurrentState state, ReadOnlySpan<char> input, int pos)
		{
			return ExtractNullableCoreStateId(matcher, in state, input, pos);
		}

		static int IStateHandler.FixedLength(SymbolicRegexMatcher<TSet> matcher, in CurrentState state, uint nextCharKind)
		{
			return FixedLength(matcher, in state, nextCharKind);
		}

		static StateFlags IStateHandler.GetStateFlags(SymbolicRegexMatcher<TSet> matcher, in CurrentState state)
		{
			return GetStateFlags(matcher, in state);
		}
	}

	private interface IInputReader
	{
		static abstract int GetPositionId(SymbolicRegexMatcher<TSet> matcher, ReadOnlySpan<char> input, int pos);
	}

	[StructLayout(LayoutKind.Sequential, Size = 1)]
	private readonly struct NoZAnchorInputReader : IInputReader
	{
		public static int GetPositionId(SymbolicRegexMatcher<TSet> matcher, ReadOnlySpan<char> input, int pos)
		{
			if ((uint)pos < (uint)input.Length)
			{
				return matcher._mintermClassifier.GetMintermID(input[pos]);
			}
			return -1;
		}
	}

	[StructLayout(LayoutKind.Sequential, Size = 1)]
	private readonly struct FullInputReader : IInputReader
	{
		public static int GetPositionId(SymbolicRegexMatcher<TSet> matcher, ReadOnlySpan<char> input, int pos)
		{
			if ((uint)pos >= (uint)input.Length)
			{
				return -1;
			}
			int num = input[pos];
			if (num != 10 || pos != input.Length - 1)
			{
				return matcher._mintermClassifier.GetMintermID(num);
			}
			return matcher._minterms.Length;
		}
	}

	private interface IInitialStateHandler
	{
		static abstract bool TryFindNextStartingPosition<TInputReader>(SymbolicRegexMatcher<TSet> matcher, ReadOnlySpan<char> input, ref CurrentState state, ref int pos) where TInputReader : struct, IInputReader;
	}

	[StructLayout(LayoutKind.Sequential, Size = 1)]
	private readonly struct NoOptimizationsInitialStateHandler : IInitialStateHandler
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool TryFindNextStartingPosition<TInputReader>(SymbolicRegexMatcher<TSet> matcher, ReadOnlySpan<char> input, ref CurrentState state, ref int pos) where TInputReader : struct, IInputReader
		{
			return true;
		}
	}

	[StructLayout(LayoutKind.Sequential, Size = 1)]
	private readonly struct InitialStateFindOptimizationsHandler : IInitialStateHandler
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool TryFindNextStartingPosition<TInputReader>(SymbolicRegexMatcher<TSet> matcher, ReadOnlySpan<char> input, ref CurrentState state, ref int pos) where TInputReader : struct, IInputReader
		{
			if (!matcher._findOpts.TryFindNextStartingPositionLeftToRight(input, ref pos, 0))
			{
				return false;
			}
			state = new CurrentState(matcher._dotstarredInitialStates[matcher.GetCharKind<TInputReader>(input, pos - 1)]);
			return true;
		}
	}

	private interface INullabilityHandler
	{
		static abstract bool IsNullableAt<TStateHandler>(SymbolicRegexMatcher<TSet> matcher, in CurrentState state, int positionId, StateFlags flags) where TStateHandler : struct, IStateHandler;
	}

	[StructLayout(LayoutKind.Sequential, Size = 1)]
	private readonly struct NoAnchorsNullabilityHandler : INullabilityHandler
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool IsNullableAt<TStateHandler>(SymbolicRegexMatcher<TSet> matcher, in CurrentState state, int positionId, StateFlags flags) where TStateHandler : struct, IStateHandler
		{
			return flags.IsNullable();
		}

		static bool INullabilityHandler.IsNullableAt<TStateHandler>(SymbolicRegexMatcher<TSet> matcher, in CurrentState state, int positionId, StateFlags flags)
		{
			return IsNullableAt<TStateHandler>(matcher, in state, positionId, flags);
		}
	}

	[StructLayout(LayoutKind.Sequential, Size = 1)]
	private readonly struct FullNullabilityHandler : INullabilityHandler
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool IsNullableAt<TStateHandler>(SymbolicRegexMatcher<TSet> matcher, in CurrentState state, int positionId, StateFlags flags) where TStateHandler : struct, IStateHandler
		{
			if (!flags.IsNullable())
			{
				if (flags.CanBeNullable())
				{
					return TStateHandler.IsNullableFor(matcher, in state, matcher.GetPositionKind(positionId));
				}
				return false;
			}
			return true;
		}

		static bool INullabilityHandler.IsNullableAt<TStateHandler>(SymbolicRegexMatcher<TSet> matcher, in CurrentState state, int positionId, StateFlags flags)
		{
			return IsNullableAt<TStateHandler>(matcher, in state, positionId, flags);
		}
	}

	private readonly Dictionary<(SymbolicRegexNode<TSet> Node, uint PrevCharKind), MatchingState<TSet>> _stateCache = new Dictionary<(SymbolicRegexNode<TSet>, uint), MatchingState<TSet>>();

	private MatchingState<TSet>[] _stateArray;

	private StateFlags[] _stateFlagsArray;

	private int[] _dfaDelta;

	private int[] _nfaCoreIdArray = Array.Empty<int>();

	private readonly Dictionary<int, int> _nfaIdByCoreId = new Dictionary<int, int>();

	private int[][] _nfaDelta = Array.Empty<int[]>();

	private (int, DerivativeEffect[])[][] _capturingNfaDelta = Array.Empty<(int, DerivativeEffect[])[]>();

	internal readonly SymbolicRegexBuilder<TSet> _builder;

	private readonly MintermClassifier _mintermClassifier;

	internal readonly SymbolicRegexNode<TSet> _dotStarredPattern;

	internal readonly SymbolicRegexNode<TSet> _pattern;

	internal readonly SymbolicRegexNode<TSet> _reversePattern;

	private readonly bool _checkTimeout;

	private readonly int _timeout;

	private readonly RegexFindOptimizations _findOpts;

	private readonly MatchingState<TSet>[] _initialStates;

	private readonly MatchingState<TSet>[] _dotstarredInitialStates;

	private readonly MatchingState<TSet>[] _reverseInitialStates;

	private readonly TSet[] _minterms;

	private readonly uint[] _positionKinds;

	private readonly int _mintermsLog;

	private readonly int _capsize;

	internal bool HasSubcaptures => _capsize > 1;

	private ISolver<TSet> Solver => _builder._solver;

	private static void ArrayResizeAndVolatilePublish<T>(ref T[] array, int newSize)
	{
		T[] array2 = new T[newSize];
		Array.Copy(array, array2, array.Length);
		Volatile.Write(ref array, array2);
	}

	private int DeltaOffset(int stateId, int mintermId)
	{
		return (stateId << _mintermsLog) | mintermId;
	}

	private MatchingState<TSet> GetOrCreateState(SymbolicRegexNode<TSet> node, uint prevCharKind)
	{
		return GetOrCreateState_NoLock(node, prevCharKind);
	}

	private MatchingState<TSet> GetOrCreateState_NoLock(SymbolicRegexNode<TSet> node, uint prevCharKind, bool isInitialState = false)
	{
		SymbolicRegexNode<TSet> item = node.PruneAnchors(_builder, prevCharKind);
		(SymbolicRegexNode<TSet>, uint) key = (item, prevCharKind);
		if (!_stateCache.TryGetValue(key, out var value))
		{
			value = new MatchingState<TSet>(key.Item1, key.Item2);
			_stateCache.Add(key, value);
			value.Id = _stateCache.Count;
			if (value.Id == _stateArray.Length)
			{
				int num = _stateArray.Length * 2;
				ArrayResizeAndVolatilePublish(ref _stateArray, num);
				ArrayResizeAndVolatilePublish(ref _dfaDelta, num << _mintermsLog);
				ArrayResizeAndVolatilePublish(ref _stateFlagsArray, num);
			}
			_stateArray[value.Id] = value;
			_stateFlagsArray[value.Id] = value.BuildStateFlags(Solver, isInitialState);
		}
		return value;
	}

	private int? CreateNfaState(SymbolicRegexNode<TSet> node, uint prevCharKind)
	{
		MatchingState<TSet> orCreateState = GetOrCreateState(node, prevCharKind);
		if (orCreateState.IsDeadend(Solver))
		{
			return null;
		}
		if (!_nfaIdByCoreId.TryGetValue(orCreateState.Id, out var value))
		{
			value = _nfaIdByCoreId.Count;
			if (value == _nfaCoreIdArray.Length)
			{
				int num = Math.Max(_nfaCoreIdArray.Length * 2, 64);
				ArrayResizeAndVolatilePublish(ref _nfaCoreIdArray, num);
				ArrayResizeAndVolatilePublish(ref _nfaDelta, num << _mintermsLog);
				ArrayResizeAndVolatilePublish(ref _capturingNfaDelta, num << _mintermsLog);
			}
			_nfaCoreIdArray[value] = orCreateState.Id;
			_nfaIdByCoreId.Add(orCreateState.Id, value);
		}
		return value;
	}

	private MatchingState<TSet> GetState(int stateId)
	{
		return _stateArray[stateId];
	}

	private int GetCoreStateId(int nfaStateId)
	{
		return _nfaCoreIdArray[nfaStateId];
	}

	private bool TryCreateNewTransition(MatchingState<TSet> sourceState, int mintermId, int offset, bool checkThreshold, [NotNullWhen(true)] out MatchingState<TSet> nextState)
	{
		lock (this)
		{
			MatchingState<TSet> matchingState = _stateArray[_dfaDelta[offset]];
			if (matchingState == null)
			{
				if (checkThreshold && _stateCache.Count >= 10000)
				{
					nextState = null;
					return false;
				}
				TSet mintermFromId = GetMintermFromId(mintermId);
				uint positionKind = GetPositionKind(mintermId);
				matchingState = GetOrCreateState(sourceState.Next(_builder, mintermFromId, positionKind), positionKind);
				Volatile.Write(ref _dfaDelta[offset], matchingState.Id);
			}
			nextState = matchingState;
			return true;
		}
	}

	private int[] CreateNewNfaTransition(int nfaStateId, int mintermId, int nfaOffset)
	{
		lock (this)
		{
			int[] array = _nfaDelta[nfaOffset];
			if (array == null)
			{
				int coreStateId = GetCoreStateId(nfaStateId);
				int num = (coreStateId << _mintermsLog) | mintermId;
				int num2 = _dfaDelta[num];
				MatchingState<TSet> state = GetState(coreStateId);
				TSet mintermFromId = GetMintermFromId(mintermId);
				uint positionKind = GetPositionKind(mintermId);
				SymbolicRegexNode<TSet> node = ((num2 > 0) ? GetState(num2).Node : state.Next(_builder, mintermFromId, positionKind));
				List<int> list = new List<int>();
				ForEachNfaState(node, positionKind, list, delegate(int nfaId, List<int> targetsList)
				{
					targetsList.Add(nfaId);
				});
				array = list.ToArray();
				Volatile.Write(ref _nfaDelta[nfaOffset], array);
			}
			return array;
		}
	}

	private (int, DerivativeEffect[])[] CreateNewCapturingTransition(int nfaStateId, int mintermId, int offset)
	{
		lock (this)
		{
			(int, DerivativeEffect[])[] array = _capturingNfaDelta[offset];
			if (array == null)
			{
				MatchingState<TSet> state = GetState(GetCoreStateId(nfaStateId));
				TSet mintermFromId = GetMintermFromId(mintermId);
				uint positionKind = GetPositionKind(mintermId);
				List<(SymbolicRegexNode<TSet>, DerivativeEffect[])> list = state.NfaNextWithEffects(_builder, mintermFromId, positionKind);
				List<(int, DerivativeEffect[])> list2 = new List<(int, DerivativeEffect[])>();
				foreach (var item in list)
				{
					ForEachNfaState(item.Item1, positionKind, (list2, item.Item2), delegate(int nfaId, (List<(int, DerivativeEffect[])> Targets, DerivativeEffect[] Effects) args)
					{
						args.Targets.Add((nfaId, args.Effects));
					});
				}
				array = list2.ToArray();
				Volatile.Write(ref _capturingNfaDelta[offset], array);
			}
			return array;
		}
	}

	private void ForEachNfaState<T>(SymbolicRegexNode<TSet> node, uint prevCharKind, T arg, Action<int, T> action)
	{
		lock (this)
		{
			foreach (SymbolicRegexNode<TSet> item in node.EnumerateAlternationBranches(_builder))
			{
				int? num = CreateNfaState(item, prevCharKind);
				if (num.HasValue)
				{
					int valueOrDefault = num.GetValueOrDefault();
					action(valueOrDefault, arg);
				}
			}
		}
	}

	public static SymbolicRegexMatcher<TSet> Create(int captureCount, RegexFindOptimizations findOptimizations, SymbolicRegexBuilder<BDD> bddBuilder, SymbolicRegexNode<BDD> rootBddNode, ISolver<TSet> solver, TimeSpan matchTimeout)
	{
		CharSetSolver charSetSolver = (CharSetSolver)bddBuilder._solver;
		SymbolicRegexBuilder<TSet> builder2 = new SymbolicRegexBuilder<TSet>(solver, charSetSolver)
		{
			_wordLetterForBoundariesSet = solver.ConvertFromBDD(bddBuilder._wordLetterForBoundariesSet, charSetSolver),
			_newLineSet = solver.ConvertFromBDD(bddBuilder._newLineSet, charSetSolver)
		};
		SymbolicRegexNode<TSet> rootNode = bddBuilder.Transform(rootBddNode, builder2, (SymbolicRegexBuilder<TSet> builder, BDD bdd) => builder._solver.ConvertFromBDD(bdd, charSetSolver));
		return new SymbolicRegexMatcher<TSet>(builder2, rootNode, captureCount, findOptimizations, matchTimeout);
	}

	private SymbolicRegexMatcher(SymbolicRegexBuilder<TSet> builder, SymbolicRegexNode<TSet> rootNode, int captureCount, RegexFindOptimizations findOptimizations, TimeSpan matchTimeout)
	{
		_pattern = rootNode;
		_builder = builder;
		_checkTimeout = Regex.InfiniteMatchTimeout != matchTimeout;
		_timeout = (int)(matchTimeout.TotalMilliseconds + 0.5);
		TSet[] minterms = builder._solver.GetMinterms();
		_minterms = minterms;
		_mintermsLog = BitOperations.Log2((uint)_minterms.Length) + 1;
		_mintermClassifier = ((builder._solver is UInt64Solver uInt64Solver) ? uInt64Solver._classifier : ((BitVectorSolver)(object)builder._solver)._classifier);
		_capsize = captureCount;
		_stateArray = new MatchingState<TSet>[1024];
		_stateFlagsArray = new StateFlags[1024];
		_dfaDelta = new int[1024 << _mintermsLog];
		_positionKinds = new uint[_minterms.Length + 2];
		for (int i = -1; i < _positionKinds.Length - 1; i++)
		{
			_positionKinds[i + 1] = CalculateMintermIdKind(i);
			uint CalculateMintermIdKind(int mintermId)
			{
				if (_pattern._info.ContainsSomeAnchor)
				{
					if (mintermId == -1)
					{
						return 1u;
					}
					if (mintermId == _minterms.Length)
					{
						return 3u;
					}
					TSet val = _minterms[mintermId];
					if (_builder._newLineSet.Equals(val))
					{
						return 2u;
					}
					if (!Solver.IsEmpty(Solver.And(_builder._wordLetterForBoundariesSet, val)))
					{
						return 4u;
					}
				}
				return 0u;
			}
		}
		if (findOptimizations.IsUseful && findOptimizations.LeadingAnchor != RegexNodeKind.Beginning)
		{
			_findOpts = findOptimizations;
		}
		int num = ((!_pattern._info.ContainsSomeAnchor) ? 1 : 5);
		MatchingState<TSet>[] array = new MatchingState<TSet>[num];
		for (uint num2 = 0u; num2 < array.Length; num2++)
		{
			array[num2] = GetOrCreateState_NoLock(_pattern, num2);
		}
		_initialStates = array;
		_dotStarredPattern = builder.CreateConcat(builder._anyStarLazy, _pattern);
		MatchingState<TSet>[] array2 = new MatchingState<TSet>[num];
		for (uint num3 = 0u; num3 < array2.Length; num3++)
		{
			array2[num3] = GetOrCreateState_NoLock(_dotStarredPattern, num3, isInitialState: true);
		}
		_dotstarredInitialStates = array2;
		_reversePattern = builder.CreateDisableBacktrackingSimulation(_pattern.Reverse(builder));
		MatchingState<TSet>[] array3 = new MatchingState<TSet>[num];
		for (uint num4 = 0u; num4 < array3.Length; num4++)
		{
			array3[num4] = GetOrCreateState_NoLock(_reversePattern, num4);
		}
		_reverseInitialStates = array3;
	}

	internal PerThreadData CreatePerThreadData()
	{
		return new PerThreadData(_capsize);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private uint GetPositionKind(int positionId)
	{
		return _positionKinds[positionId + 1];
	}

	internal TSet GetMintermFromId(int mintermId)
	{
		TSet[] minterms = _minterms;
		if ((uint)mintermId >= (uint)minterms.Length)
		{
			return _builder._newLineSet;
		}
		return minterms[mintermId];
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private uint GetCharKind<TInputReader>(ReadOnlySpan<char> input, int i) where TInputReader : struct, IInputReader
	{
		if (_pattern._info.ContainsSomeAnchor)
		{
			return GetPositionKind(TInputReader.GetPositionId(this, input, i));
		}
		return 0u;
	}

	private void CheckTimeout(long timeoutOccursAt)
	{
		if (Environment.TickCount64 >= timeoutOccursAt)
		{
			throw new RegexMatchTimeoutException(string.Empty, string.Empty, TimeSpan.FromMilliseconds(_timeout));
		}
	}

	public SymbolicMatch FindMatch(RegexRunnerMode mode, ReadOnlySpan<char> input, int startat, PerThreadData perThreadData)
	{
		long timeoutOccursAt = 0L;
		if (_checkTimeout)
		{
			timeoutOccursAt = Environment.TickCount64 + _timeout;
		}
		bool containsLineAnchor = _pattern._info.ContainsLineAnchor;
		bool flag = _findOpts != null;
		bool containsSomeAnchor = _pattern._info.ContainsSomeAnchor;
		int num = (containsLineAnchor ? (flag ? ((!containsSomeAnchor) ? FindEndPosition<FullInputReader, InitialStateFindOptimizationsHandler, NoAnchorsNullabilityHandler>(input, startat, timeoutOccursAt, mode, out var initialStatePos, out var matchLength, perThreadData) : FindEndPosition<FullInputReader, InitialStateFindOptimizationsHandler, FullNullabilityHandler>(input, startat, timeoutOccursAt, mode, out initialStatePos, out matchLength, perThreadData)) : ((!containsSomeAnchor) ? FindEndPosition<FullInputReader, NoOptimizationsInitialStateHandler, NoAnchorsNullabilityHandler>(input, startat, timeoutOccursAt, mode, out initialStatePos, out matchLength, perThreadData) : FindEndPosition<FullInputReader, NoOptimizationsInitialStateHandler, FullNullabilityHandler>(input, startat, timeoutOccursAt, mode, out initialStatePos, out matchLength, perThreadData))) : (flag ? ((!containsSomeAnchor) ? FindEndPosition<NoZAnchorInputReader, InitialStateFindOptimizationsHandler, NoAnchorsNullabilityHandler>(input, startat, timeoutOccursAt, mode, out initialStatePos, out matchLength, perThreadData) : FindEndPosition<NoZAnchorInputReader, InitialStateFindOptimizationsHandler, FullNullabilityHandler>(input, startat, timeoutOccursAt, mode, out initialStatePos, out matchLength, perThreadData)) : ((!containsSomeAnchor) ? FindEndPosition<NoZAnchorInputReader, NoOptimizationsInitialStateHandler, NoAnchorsNullabilityHandler>(input, startat, timeoutOccursAt, mode, out initialStatePos, out matchLength, perThreadData) : FindEndPosition<NoZAnchorInputReader, NoOptimizationsInitialStateHandler, FullNullabilityHandler>(input, startat, timeoutOccursAt, mode, out initialStatePos, out matchLength, perThreadData))));
		int num2 = num;
		if (num2 == -2)
		{
			return SymbolicMatch.NoMatch;
		}
		if (mode == RegexRunnerMode.ExistenceRequired)
		{
			return SymbolicMatch.MatchExists;
		}
		int num3;
		if (matchLength >= 0)
		{
			num3 = num2 - matchLength;
		}
		else
		{
			int num4;
			if (num2 < startat)
			{
				num4 = startat;
			}
			else
			{
				bool containsLineAnchor2 = _pattern._info.ContainsLineAnchor;
				bool containsSomeAnchor2 = _pattern._info.ContainsSomeAnchor;
				num = (containsLineAnchor2 ? ((!containsSomeAnchor2) ? FindStartPosition<FullInputReader, NoAnchorsNullabilityHandler>(input, num2, initialStatePos, perThreadData) : FindStartPosition<FullInputReader, FullNullabilityHandler>(input, num2, initialStatePos, perThreadData)) : ((!containsSomeAnchor2) ? FindStartPosition<NoZAnchorInputReader, NoAnchorsNullabilityHandler>(input, num2, initialStatePos, perThreadData) : FindStartPosition<NoZAnchorInputReader, FullNullabilityHandler>(input, num2, initialStatePos, perThreadData)));
				num4 = num;
			}
			num3 = num4;
		}
		if (!HasSubcaptures || mode < RegexRunnerMode.FullMatchRequired)
		{
			return new SymbolicMatch(num3, num2 - num3);
		}
		Registers registers = (_pattern._info.ContainsLineAnchor ? FindSubcaptures<FullInputReader>(input, num3, num2, perThreadData) : FindSubcaptures<NoZAnchorInputReader>(input, num3, num2, perThreadData));
		return new SymbolicMatch(num3, num2 - num3, registers.CaptureStarts, registers.CaptureEnds);
	}

	private int FindEndPosition<TInputReader, TFindOptimizationsHandler, TNullabilityHandler>(ReadOnlySpan<char> input, int pos, long timeoutOccursAt, RegexRunnerMode mode, out int initialStatePos, out int matchLength, PerThreadData perThreadData) where TInputReader : struct, IInputReader where TFindOptimizationsHandler : struct, IInitialStateHandler where TNullabilityHandler : struct, INullabilityHandler
	{
		initialStatePos = pos;
		int initialStatePosCandidateRef = pos;
		CurrentState state = new CurrentState(_dotstarredInitialStates[GetCharKind<TInputReader>(input, pos - 1)]);
		int endPosRef = -2;
		int endStateIdRef = -1;
		while (true)
		{
			int num = ((_checkTimeout && input.Length - pos > 1000) ? (pos + 1000) : input.Length);
			if (((state.NfaState != null) ? FindEndPositionDeltas<NfaStateHandler, TInputReader, TFindOptimizationsHandler, TNullabilityHandler>(input, num, mode, ref pos, ref state, ref endPosRef, ref endStateIdRef, ref initialStatePos, ref initialStatePosCandidateRef) : FindEndPositionDeltas<DfaStateHandler, TInputReader, TFindOptimizationsHandler, TNullabilityHandler>(input, num, mode, ref pos, ref state, ref endPosRef, ref endStateIdRef, ref initialStatePos, ref initialStatePosCandidateRef)) || pos >= input.Length)
			{
				break;
			}
			if (pos < num)
			{
				NfaMatchingState nfaState = perThreadData.NfaState;
				nfaState.InitializeFrom(this, GetState(state.DfaStateId));
				state = new CurrentState(nfaState);
			}
			if (_checkTimeout)
			{
				CheckTimeout(timeoutOccursAt);
			}
		}
		matchLength = ((endStateIdRef > 0) ? GetState(endStateIdRef).FixedLength(GetCharKind<TInputReader>(input, endPosRef)) : (-1));
		return endPosRef;
	}

	private bool FindEndPositionDeltas<TStateHandler, TInputReader, TFindOptimizationsHandler, TNullabilityHandler>(ReadOnlySpan<char> input, int length, RegexRunnerMode mode, ref int posRef, ref CurrentState state, ref int endPosRef, ref int endStateIdRef, ref int initialStatePosRef, ref int initialStatePosCandidateRef) where TStateHandler : struct, IStateHandler where TInputReader : struct, IInputReader where TFindOptimizationsHandler : struct, IInitialStateHandler where TNullabilityHandler : struct, INullabilityHandler
	{
		int pos = posRef;
		int num = endPosRef;
		int num2 = endStateIdRef;
		int num3 = initialStatePosRef;
		int num4 = initialStatePosCandidateRef;
		try
		{
			while (true)
			{
				StateFlags stateFlags = TStateHandler.GetStateFlags(this, in state);
				if (stateFlags.IsInitial())
				{
					if (!TFindOptimizationsHandler.TryFindNextStartingPosition<TInputReader>(this, input, ref state, ref pos))
					{
						return true;
					}
					num4 = pos;
				}
				if (stateFlags.IsDeadend())
				{
					return true;
				}
				int positionId = TInputReader.GetPositionId(this, input, pos);
				if (TNullabilityHandler.IsNullableAt<TStateHandler>(this, in state, positionId, stateFlags))
				{
					num = pos;
					num2 = TStateHandler.ExtractNullableCoreStateId(this, in state, input, pos);
					num3 = num4;
					if (mode == RegexRunnerMode.ExistenceRequired)
					{
						return true;
					}
				}
				if (pos >= length || !TStateHandler.TryTakeTransition(this, ref state, positionId))
				{
					break;
				}
				pos++;
			}
			return false;
		}
		finally
		{
			posRef = pos;
			endPosRef = num;
			endStateIdRef = num2;
			initialStatePosRef = num3;
			initialStatePosCandidateRef = num4;
		}
	}

	private int FindStartPosition<TInputReader, TNullabilityHandler>(ReadOnlySpan<char> input, int i, int matchStartBoundary, PerThreadData perThreadData) where TInputReader : struct, IInputReader where TNullabilityHandler : struct, INullabilityHandler
	{
		CurrentState state = new CurrentState(_reverseInitialStates[GetCharKind<TInputReader>(input, i)]);
		int lastStart = -1;
		while (!((state.NfaState != null) ? FindStartPositionDeltas<NfaStateHandler, TInputReader, TNullabilityHandler>(input, ref i, matchStartBoundary, ref state, ref lastStart) : FindStartPositionDeltas<DfaStateHandler, TInputReader, TNullabilityHandler>(input, ref i, matchStartBoundary, ref state, ref lastStart)))
		{
			NfaMatchingState nfaState = perThreadData.NfaState;
			nfaState.InitializeFrom(this, GetState(state.DfaStateId));
			state = new CurrentState(nfaState);
		}
		return lastStart;
	}

	private bool FindStartPositionDeltas<TStateHandler, TInputReader, TNullabilityHandler>(ReadOnlySpan<char> input, ref int i, int startThreshold, ref CurrentState state, ref int lastStart) where TStateHandler : struct, IStateHandler where TInputReader : struct, IInputReader where TNullabilityHandler : struct, INullabilityHandler
	{
		int num = i;
		try
		{
			while (true)
			{
				StateFlags stateFlags = TStateHandler.GetStateFlags(this, in state);
				int positionId = TInputReader.GetPositionId(this, input, num - 1);
				if (TNullabilityHandler.IsNullableAt<TStateHandler>(this, in state, positionId, stateFlags))
				{
					lastStart = num;
				}
				if (num <= startThreshold || stateFlags.IsDeadend())
				{
					return true;
				}
				if (!TStateHandler.TryTakeTransition(this, ref state, positionId))
				{
					break;
				}
				num--;
			}
			return false;
		}
		finally
		{
			i = num;
		}
	}

	private Registers FindSubcaptures<TInputReader>(ReadOnlySpan<char> input, int i, int iEnd, PerThreadData perThreadData) where TInputReader : struct, IInputReader
	{
		MatchingState<TSet> matchingState = _initialStates[GetCharKind<TInputReader>(input, i - 1)];
		Registers initialRegisters = perThreadData.InitialRegisters;
		Array.Fill(initialRegisters.CaptureStarts, -1);
		Array.Fill(initialRegisters.CaptureEnds, -1);
		SparseIntMap<Registers> sparseIntMap = perThreadData.Current;
		SparseIntMap<Registers> sparseIntMap2 = perThreadData.Next;
		sparseIntMap.Clear();
		sparseIntMap2.Clear();
		ForEachNfaState(matchingState.Node, matchingState.PrevCharKind, (sparseIntMap, initialRegisters), delegate(int nfaId, (SparseIntMap<Registers> Current, Registers InitialRegisters) args)
		{
			args.Current.Add(nfaId, args.InitialRegisters.Clone());
		});
		int key;
		Registers value;
		while ((uint)i < (uint)iEnd)
		{
			int positionId = TInputReader.GetPositionId(this, input, i);
			foreach (KeyValuePair<int, Registers> value3 in sparseIntMap.Values)
			{
				value3.Deconstruct(out key, out value);
				int num = key;
				Registers registers = value;
				int num2 = DeltaOffset(num, positionId);
				(int, DerivativeEffect[])[] array = _capturingNfaDelta[num2] ?? CreateNewCapturingTransition(num, positionId, num2);
				for (int j = 0; j < array.Length; j++)
				{
					var (num3, effects) = array[j];
					if (sparseIntMap2.Add(num3, out var index))
					{
						Registers value2 = ((j != array.Length - 1) ? registers.Clone() : registers);
						value2.ApplyEffects(effects, i);
						sparseIntMap2.Update(index, num3, value2);
						int coreStateId = GetCoreStateId(num3);
						StateFlags info = _stateFlagsArray[coreStateId];
						if (info.IsNullable() || (info.CanBeNullable() && GetState(coreStateId).IsNullableFor(GetCharKind<TInputReader>(input, i + 1))))
						{
							goto end_IL_019e;
						}
					}
				}
				continue;
				end_IL_019e:
				break;
			}
			SparseIntMap<Registers> sparseIntMap3 = sparseIntMap;
			sparseIntMap = sparseIntMap2;
			sparseIntMap2 = sparseIntMap3;
			sparseIntMap2.Clear();
			i++;
		}
		foreach (KeyValuePair<int, Registers> value4 in sparseIntMap.Values)
		{
			value4.Deconstruct(out key, out value);
			int nfaStateId = key;
			Registers registers2 = value;
			MatchingState<TSet> state = GetState(GetCoreStateId(nfaStateId));
			if (state.IsNullableFor(GetCharKind<TInputReader>(input, iEnd)))
			{
				state.Node.ApplyEffects(delegate(DerivativeEffect effect, (Registers Registers, int Pos) args)
				{
					args.Registers.ApplyEffect(effect, args.Pos);
				}, CharKind.Context(state.PrevCharKind, GetCharKind<TInputReader>(input, iEnd)), (registers2, iEnd));
				return registers2;
			}
		}
		return default(Registers);
	}
}
internal abstract class SymbolicRegexMatcher
{
}
