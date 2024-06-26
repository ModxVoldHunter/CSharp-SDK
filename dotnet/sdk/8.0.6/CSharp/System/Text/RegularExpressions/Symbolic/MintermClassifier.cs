using System.Runtime.CompilerServices;

namespace System.Text.RegularExpressions.Symbolic;

internal sealed class MintermClassifier
{
	private static readonly int[] AllAsciiIsZeroMintermArray = new int[128];

	private readonly int[] _ascii;

	private readonly BDD _nonAscii;

	public MintermClassifier(BDD[] minterms, CharSetSolver solver)
	{
		if (minterms.Length == 1)
		{
			_ascii = AllAsciiIsZeroMintermArray;
			_nonAscii = solver.ReplaceTrue(BDD.True, 0);
			return;
		}
		BDD bDD = BDD.False;
		for (int i = 0; i < minterms.Length; i++)
		{
			BDD set = solver.ReplaceTrue(minterms[i], i);
			bDD = solver.Or(bDD, set);
		}
		int[] array = new int[128];
		for (int j = 0; j < array.Length; j++)
		{
			array[j] = bDD.Find(j);
		}
		_ascii = array;
		BDD bDD2 = solver.And(bDD, solver.NonAscii);
		_nonAscii = (bDD2.IsEssentiallyBoolean(out var terminalActingAsTrue) ? terminalActingAsTrue : bDD2);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public int GetMintermID(int c)
	{
		int[] ascii = _ascii;
		if ((uint)c >= (uint)ascii.Length)
		{
			return _nonAscii.Find(c);
		}
		return ascii[c];
	}
}
