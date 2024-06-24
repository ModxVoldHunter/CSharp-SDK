namespace System.Security.Cryptography;

internal static class KdfWorkLimiter
{
	private sealed class State
	{
		internal ulong RemainingAllowedWork;

		internal bool WorkLimitWasExceeded;
	}

	[ThreadStatic]
	private static State t_state;

	internal static void SetIterationLimit(ulong workLimit)
	{
		State state = new State();
		state.RemainingAllowedWork = workLimit;
		t_state = state;
	}

	internal static bool WasWorkLimitExceeded()
	{
		return t_state.WorkLimitWasExceeded;
	}

	internal static void ResetIterationLimit()
	{
		t_state = null;
	}

	internal static void RecordIterations(int workCount)
	{
		RecordIterations((long)workCount);
	}

	internal static void RecordIterations(long workCount)
	{
		State state = t_state;
		if (state == null)
		{
			return;
		}
		bool flag = false;
		if (workCount < 0)
		{
			throw new CryptographicException();
		}
		checked
		{
			try
			{
				if (!state.WorkLimitWasExceeded)
				{
					state.RemainingAllowedWork -= (ulong)workCount;
					flag = true;
				}
			}
			finally
			{
				if (!flag)
				{
					state.RemainingAllowedWork = 0uL;
					state.WorkLimitWasExceeded = true;
					throw new CryptographicException();
				}
			}
		}
	}
}
