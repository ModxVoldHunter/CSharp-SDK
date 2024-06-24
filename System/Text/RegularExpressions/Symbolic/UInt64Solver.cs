namespace System.Text.RegularExpressions.Symbolic;

internal sealed class UInt64Solver : ISolver<ulong>
{
	private readonly BDD[] _minterms;

	internal readonly MintermClassifier _classifier;

	public ulong Empty => 0uL;

	public ulong Full { get; }

	public UInt64Solver(BDD[] minterms, CharSetSolver solver)
	{
		_minterms = minterms;
		_classifier = new MintermClassifier(minterms, solver);
		Full = ((minterms.Length == 64) ? ulong.MaxValue : (ulong.MaxValue >> 64 - minterms.Length));
	}

	public bool IsFull(ulong set)
	{
		return set == Full;
	}

	public bool IsEmpty(ulong set)
	{
		return set == 0;
	}

	public ulong And(ulong set1, ulong set2)
	{
		return set1 & set2;
	}

	public ulong Not(ulong set)
	{
		return Full & ~set;
	}

	public ulong Or(ReadOnlySpan<ulong> sets)
	{
		ulong num = 0uL;
		ReadOnlySpan<ulong> readOnlySpan = sets;
		for (int i = 0; i < readOnlySpan.Length; i++)
		{
			ulong num2 = readOnlySpan[i];
			num |= num2;
			if (num == Full)
			{
				break;
			}
		}
		return num;
	}

	public ulong Or(ulong set1, ulong set2)
	{
		return set1 | set2;
	}

	public ulong ConvertFromBDD(BDD set, CharSetSolver solver)
	{
		BDD[] minterms = _minterms;
		ulong num = 0uL;
		for (int i = 0; i < minterms.Length; i++)
		{
			if (!solver.IsEmpty(solver.And(minterms[i], set)))
			{
				num |= (ulong)(1L << i);
			}
		}
		return num;
	}

	public ulong[] GetMinterms()
	{
		ulong[] array = new ulong[_minterms.Length];
		for (int i = 0; i < array.Length; i++)
		{
			array[i] = (ulong)(1L << i);
		}
		return array;
	}
}
