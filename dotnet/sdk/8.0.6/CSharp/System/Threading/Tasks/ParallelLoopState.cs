using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace System.Threading.Tasks;

[DebuggerDisplay("ShouldExitCurrentIteration = {ShouldExitCurrentIteration}")]
public class ParallelLoopState
{
	private readonly ParallelLoopStateFlags _flagsBase;

	internal virtual bool InternalShouldExitCurrentIteration
	{
		get
		{
			throw new NotSupportedException(System.SR.ParallelState_NotSupportedException_UnsupportedMethod);
		}
	}

	public bool ShouldExitCurrentIteration => InternalShouldExitCurrentIteration;

	public bool IsStopped => (_flagsBase.LoopStateFlags & 4) != 0;

	public bool IsExceptional => (_flagsBase.LoopStateFlags & 1) != 0;

	internal virtual long? InternalLowestBreakIteration
	{
		get
		{
			throw new NotSupportedException(System.SR.ParallelState_NotSupportedException_UnsupportedMethod);
		}
	}

	public long? LowestBreakIteration => InternalLowestBreakIteration;

	internal ParallelLoopState(ParallelLoopStateFlags fbase)
	{
		_flagsBase = fbase;
	}

	public void Stop()
	{
		_flagsBase.Stop();
	}

	internal virtual void InternalBreak()
	{
		throw new NotSupportedException(System.SR.ParallelState_NotSupportedException_UnsupportedMethod);
	}

	public void Break()
	{
		InternalBreak();
	}

	internal static void Break<TInt>(TInt iteration, ParallelLoopStateFlags<TInt> pflags) where TInt : struct, IBinaryInteger<TInt>, IMinMaxValue<TInt>
	{
		int oldState = 0;
		if (!pflags.AtomicLoopStateUpdate(2, 13, ref oldState))
		{
			if (((uint)oldState & 4u) != 0)
			{
				throw new InvalidOperationException(System.SR.ParallelState_Break_InvalidOperationException_BreakAfterStop);
			}
			return;
		}
		TInt source = pflags.LowestBreakIteration;
		if (!(iteration < source))
		{
			return;
		}
		SpinWait spinWait = default(SpinWait);
		while ((typeof(TInt) == typeof(int)) ? (Interlocked.CompareExchange(ref Unsafe.As<TInt, int>(ref pflags._lowestBreakIteration), Unsafe.As<TInt, int>(ref iteration), Unsafe.As<TInt, int>(ref source)) != Unsafe.As<TInt, int>(ref source)) : (Interlocked.CompareExchange(ref Unsafe.As<TInt, long>(ref pflags._lowestBreakIteration), Unsafe.As<TInt, long>(ref iteration), Unsafe.As<TInt, long>(ref source)) != Unsafe.As<TInt, long>(ref source)))
		{
			spinWait.SpinOnce();
			source = pflags.LowestBreakIteration;
			if (iteration > source)
			{
				break;
			}
		}
	}
}
internal sealed class ParallelLoopState<TInt> : ParallelLoopState where TInt : struct, IBinaryInteger<TInt>, IMinMaxValue<TInt>
{
	private readonly ParallelLoopStateFlags<TInt> _sharedParallelStateFlags;

	private TInt _currentIteration;

	internal TInt CurrentIteration
	{
		get
		{
			return _currentIteration;
		}
		set
		{
			_currentIteration = value;
		}
	}

	internal override bool InternalShouldExitCurrentIteration => _sharedParallelStateFlags.ShouldExitLoop(CurrentIteration);

	internal override long? InternalLowestBreakIteration => _sharedParallelStateFlags.NullableLowestBreakIteration;

	internal ParallelLoopState(ParallelLoopStateFlags<TInt> sharedParallelStateFlags)
		: base(sharedParallelStateFlags)
	{
		_sharedParallelStateFlags = sharedParallelStateFlags;
	}

	internal override void InternalBreak()
	{
		ParallelLoopState.Break(CurrentIteration, _sharedParallelStateFlags);
	}
}
