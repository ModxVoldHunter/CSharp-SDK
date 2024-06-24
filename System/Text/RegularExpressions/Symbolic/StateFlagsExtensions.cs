namespace System.Text.RegularExpressions.Symbolic;

internal static class StateFlagsExtensions
{
	internal static bool IsInitial(this StateFlags info)
	{
		return (info & StateFlags.IsInitialFlag) != 0;
	}

	internal static bool IsDeadend(this StateFlags info)
	{
		return (info & StateFlags.IsDeadendFlag) != 0;
	}

	internal static bool IsNullable(this StateFlags info)
	{
		return (info & StateFlags.IsNullableFlag) != 0;
	}

	internal static bool CanBeNullable(this StateFlags info)
	{
		return (info & StateFlags.CanBeNullableFlag) != 0;
	}

	internal static bool SimulatesBacktracking(this StateFlags info)
	{
		return (info & StateFlags.SimulatesBacktrackingFlag) != 0;
	}
}
