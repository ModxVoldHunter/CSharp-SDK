namespace System.Text.RegularExpressions.Symbolic;

internal readonly struct SymbolicMatch
{
	internal static SymbolicMatch NoMatch => new SymbolicMatch(-1, -1);

	internal static SymbolicMatch MatchExists => new SymbolicMatch(0, 0);

	public int Index { get; }

	public int Length { get; }

	public bool Success => Index >= 0;

	public int[] CaptureStarts { get; }

	public int[] CaptureEnds { get; }

	public SymbolicMatch(int index, int length, int[] captureStarts = null, int[] captureEnds = null)
	{
		Index = index;
		Length = length;
		CaptureStarts = captureStarts;
		CaptureEnds = captureEnds;
	}
}
