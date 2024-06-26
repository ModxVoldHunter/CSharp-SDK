using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.InteropServices;

namespace System.Text.RegularExpressions.Symbolic;

[DefaultMember("Item")]
internal struct BitVector : IComparable<BitVector>, IEquatable<BitVector>
{
	private readonly ulong[] _blocks;

	public readonly int Length;

	private int? _hashcode;

	private BitVector(int length)
	{
		Length = length;
		_blocks = new ulong[(length - 1) / 64 + 1];
		_hashcode = null;
	}

	private BitVector(int length, ulong[] blocks)
	{
		Length = length;
		_blocks = blocks;
		_hashcode = null;
	}

	public static BitVector CreateFalse(int length)
	{
		return new BitVector(length);
	}

	public static BitVector CreateTrue(int length)
	{
		BitVector result = new BitVector(length);
		Array.Fill(result._blocks, ulong.MaxValue);
		result.ClearRemainderBits();
		return result;
	}

	public static BitVector CreateSingleBit(int length, int i)
	{
		BitVector result = new BitVector(length);
		result.Set(i);
		return result;
	}

	private void Set(int i)
	{
		var (num, num2) = Math.DivRem(i, 64);
		_blocks[num] |= (ulong)(1L << num2);
	}

	public static BitVector And(BitVector x, BitVector y)
	{
		ulong[] blocks = x._blocks;
		ulong[] blocks2 = y._blocks;
		ulong[] array = new ulong[blocks.Length];
		for (int i = 0; i < array.Length; i++)
		{
			array[i] = blocks[i] & blocks2[i];
		}
		return new BitVector(x.Length, array);
	}

	public static BitVector Or(BitVector x, BitVector y)
	{
		ulong[] blocks = x._blocks;
		ulong[] blocks2 = y._blocks;
		ulong[] array = new ulong[blocks.Length];
		for (int i = 0; i < array.Length; i++)
		{
			array[i] = blocks[i] | blocks2[i];
		}
		return new BitVector(x.Length, array);
	}

	public static BitVector Or(ReadOnlySpan<BitVector> bitVectors)
	{
		BitVector bitVector = bitVectors[0];
		ulong[] array = new ulong[bitVector._blocks.Length];
		ReadOnlySpan<BitVector> readOnlySpan = bitVectors;
		for (int i = 0; i < readOnlySpan.Length; i++)
		{
			BitVector bitVector2 = readOnlySpan[i];
			ulong[] blocks = bitVector2._blocks;
			for (int j = 0; j < array.Length; j++)
			{
				array[j] |= blocks[j];
			}
		}
		return new BitVector(bitVector.Length, array);
	}

	public static BitVector Not(BitVector x)
	{
		ulong[] blocks = x._blocks;
		ulong[] array = new ulong[blocks.Length];
		for (int i = 0; i < array.Length; i++)
		{
			array[i] = ~blocks[i];
		}
		BitVector result = new BitVector(x.Length, array);
		result.ClearRemainderBits();
		return result;
	}

	private void ClearRemainderBits()
	{
		int num = Length % 64;
		if (num != 0)
		{
			int num2 = (Length - 1) / 64;
			_blocks[num2] &= (ulong)((1L << num) - 1);
		}
	}

	public override int GetHashCode()
	{
		if (!_hashcode.HasValue)
		{
			HashCode hashCode = default(HashCode);
			hashCode.AddBytes(MemoryMarshal.AsBytes<ulong>(_blocks));
			hashCode.Add(Length);
			_hashcode = hashCode.ToHashCode();
		}
		return _hashcode.GetValueOrDefault();
	}

	public override bool Equals([NotNullWhen(true)] object obj)
	{
		if (obj is BitVector other)
		{
			return Equals(other);
		}
		return false;
	}

	public bool Equals(BitVector other)
	{
		if (Length == other.Length)
		{
			return new ReadOnlySpan<ulong>(_blocks).SequenceEqual(new ReadOnlySpan<ulong>(other._blocks));
		}
		return false;
	}

	public int CompareTo(BitVector other)
	{
		if (Length == other.Length)
		{
			return new ReadOnlySpan<ulong>(_blocks).SequenceCompareTo(new ReadOnlySpan<ulong>(other._blocks));
		}
		return Length.CompareTo(other.Length);
	}
}
