namespace System.Text.RegularExpressions.Symbolic;

internal static class SymbolicRegexThresholds
{
	internal static int GetSymbolicRegexSafeSizeThreshold()
	{
		if (AppContext.GetData("REGEX_NONBACKTRACKING_MAX_AUTOMATA_SIZE") is int num && num > 0)
		{
			return num;
		}
		return 1000;
	}
}
