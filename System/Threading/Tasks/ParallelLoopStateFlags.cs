using System.Numerics;
using System.Runtime.CompilerServices;

namespace System.Threading.Tasks;

internal class ParallelLoopStateFlags
{
	private volatile int _loopStateFlags;

	internal int LoopStateFlags => _loopStateFlags;

	internal bool AtomicLoopStateUpdate(int newState, int illegalStates)
	{
		int oldState = 0;
		return AtomicLoopStateUpdate(newState, illegalStates, ref oldState);
	}

	internal bool AtomicLoopStateUpdate(int newState, int illegalStates, ref int oldState)
	{
		SpinWait spinWait = default(SpinWait);
		while (true)
		{
			oldState = _loopStateFlags;
			if ((oldState & illegalStates) != 0)
			{
				return false;
			}
			if (Interlocked.CompareExchange(ref _loopStateFlags, oldState | newState, oldState) == oldState)
			{
				break;
			}
			spinWait.SpinOnce();
		}
		return true;
	}

	internal void SetExceptional()
	{
		AtomicLoopStateUpdate(1, 0);
	}

	internal void Stop()
	{
		if (!AtomicLoopStateUpdate(4, 2))
		{
			throw new InvalidOperationException(System.SR.ParallelState_Stop_InvalidOperationException_StopAfterBreak);
		}
	}

	internal bool Cancel()
	{
		return AtomicLoopStateUpdate(8, 0);
	}
}
internal sealed class ParallelLoopStateFlags<TInt> : ParallelLoopStateFlags where TInt : struct, IBinaryInteger<TInt>, IMinMaxValue<TInt>
{
	internal TInt _lowestBreakIteration = TInt.MaxValue;

	internal TInt LowestBreakIteration
	{
		get
		{
			if (typeof(TInt) == typeof(int))
			{
				return Unsafe.BitCast<int, TInt>(Volatile.Read(ref Unsafe.As<TInt, int>(ref _lowestBreakIteration)));
			}
			return Unsafe.BitCast<long, TInt>(Volatile.Read(ref Unsafe.As<TInt, long>(ref _lowestBreakIteration)));
		}
	}

	internal long? NullableLowestBreakIteration
	{
		get
		{
			TInt lowestBreakIteration = LowestBreakIteration;
			if (!(lowestBreakIteration == TInt.MaxValue))
			{
				return long.CreateTruncating(lowestBreakIteration);
			}
			return null;
		}
	}

	internal bool ShouldExitLoop(TInt CallerIteration)
	{
		int loopStateFlags = base.LoopStateFlags;
		if (loopStateFlags != 0)
		{
			if ((loopStateFlags & 0xD) == 0)
			{
				if (((uint)loopStateFlags & 2u) != 0)
				{
					return CallerIteration > LowestBreakIteration;
				}
				return false;
			}
			return true;
		}
		return false;
	}

	internal bool ShouldExitLoop()
	{
		int loopStateFlags = base.LoopStateFlags;
		if (loopStateFlags != 0)
		{
			return (loopStateFlags & 9) != 0;
		}
		return false;
	}
}
