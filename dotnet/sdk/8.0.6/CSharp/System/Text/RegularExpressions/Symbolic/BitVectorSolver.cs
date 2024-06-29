namespace System.Text.RegularExpressions.Symbolic;

internal sealed class BitVectorSolver : ISolver<BitVector>
{
	private readonly BDD[] _minterms;

	internal readonly MintermClassifier _classifier;

	private readonly BitVector[] _mintermVectors;

	public BitVector Empty { get; }

	public BitVector Full { get; }

	public BitVectorSolver(BDD[] minterms, CharSetSolver solver)
	{
		_minterms = minterms;
		_classifier = new MintermClassifier(minterms, solver);
		BitVector[] array = new BitVector[minterms.Length];
		for (int i = 0; i < array.Length; i++)
		{
			array[i] = BitVector.CreateSingleBit(minterms.Length, i);
		}
		_mintermVectors = array;
		Empty = BitVector.CreateFalse(minterms.Length);
		Full = BitVector.CreateTrue(minterms.Length);
	}

	public bool IsFull(BitVector set)
	{
		return set.Equals(Full);
	}

	public bool IsEmpty(BitVector set)
	{
		return set.Equals(Empty);
	}

	public BitVector And(BitVector set1, BitVector set2)
	{
		return BitVector.And(set1, set2);
	}

	public BitVector Not(BitVector set)
	{
		return BitVector.Not(set);
	}

	public BitVector Or(ReadOnlySpan<BitVector> sets)
	{
		return BitVector.Or(sets);
	}

	public BitVector Or(BitVector set1, BitVector set2)
	{
		return BitVector.Or(set1, set2);
	}

	public BitVector ConvertFromBDD(BDD set, CharSetSolver solver)
	{
		BDD[] minterms = _minterms;
		BitVector bitVector = Empty;
		for (int i = 0; i < minterms.Length; i++)
		{
			if (!solver.IsEmpty(solver.And(minterms[i], set)))
			{
				bitVector = BitVector.Or(bitVector, _mintermVectors[i]);
			}
		}
		return bitVector;
	}

	public BitVector[] GetMinterms()
	{
		return _mintermVectors;
	}
}
