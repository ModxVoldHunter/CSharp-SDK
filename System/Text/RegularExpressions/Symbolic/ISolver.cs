namespace System.Text.RegularExpressions.Symbolic;

internal interface ISolver<TSet>
{
	TSet Full { get; }

	TSet Empty { get; }

	TSet ConvertFromBDD(BDD set, CharSetSolver solver);

	TSet[] GetMinterms();

	TSet And(TSet set1, TSet set2);

	TSet Or(TSet set1, TSet set2);

	TSet Or(ReadOnlySpan<TSet> sets);

	TSet Not(TSet set);

	bool IsEmpty(TSet set);

	bool IsFull(TSet set);
}
