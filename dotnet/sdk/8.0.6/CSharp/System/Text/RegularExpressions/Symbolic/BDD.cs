using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace System.Text.RegularExpressions.Symbolic;

internal sealed class BDD : IComparable<BDD>, IEquatable<BDD>
{
	public static readonly BDD True = new BDD(-2, null, null);

	public static readonly BDD False = new BDD(-1, null, null);

	public readonly BDD One;

	public readonly BDD Zero;

	public readonly int Ordinal;

	private readonly int _hashcode;

	[MemberNotNullWhen(false, "One")]
	[MemberNotNullWhen(false, "Zero")]
	public bool IsLeaf
	{
		[MemberNotNullWhen(false, "One")]
		[MemberNotNullWhen(false, "Zero")]
		get
		{
			return One == null;
		}
	}

	public bool IsFull => this == True;

	public bool IsEmpty => this == False;

	internal BDD(int ordinal, BDD one, BDD zero)
	{
		One = one;
		Zero = zero;
		Ordinal = ordinal;
		_hashcode = HashCode.Combine(ordinal, one, zero);
	}

	public ulong GetMin()
	{
		BDD bDD = this;
		if (bDD.IsFull)
		{
			return 0uL;
		}
		ulong num = 0uL;
		while (!bDD.IsLeaf)
		{
			if (bDD.Zero.IsEmpty)
			{
				num |= (ulong)(1L << bDD.Ordinal);
				bDD = bDD.One;
			}
			else
			{
				bDD = bDD.Zero;
			}
		}
		return num;
	}

	public override int GetHashCode()
	{
		return _hashcode;
	}

	public override bool Equals(object obj)
	{
		return Equals(obj as BDD);
	}

	public bool Equals(BDD bdd)
	{
		if (bdd != null)
		{
			if (this != bdd)
			{
				if (Ordinal == bdd.Ordinal && One == bdd.One)
				{
					return Zero == bdd.Zero;
				}
				return false;
			}
			return true;
		}
		return false;
	}

	public static BDD Deserialize(ReadOnlySpan<byte> bytes)
	{
		int num = bytes[0];
		int num2 = (bytes.Length - 1) / num;
		int num3 = (int)Get(num, bytes, 0);
		int num4 = (int)Get(num, bytes, 1);
		long num5 = (1 << num3) - 1;
		long num6 = (1 << num4) - 1;
		BitLayout(num3, num4, out var zero_node_shift, out var one_node_shift, out var ordinal_shift);
		BDD[] array = new BDD[num2];
		array[0] = False;
		array[1] = True;
		for (int j = 2; j < num2; j++)
		{
			long num7 = Get(num, bytes, j);
			int ordinal = (int)((num7 >> ordinal_shift) & num5);
			int num8 = (int)((num7 >> one_node_shift) & num6);
			int num9 = (int)((num7 >> zero_node_shift) & num6);
			array[j] = new BDD(ordinal, array[num8], array[num9]);
		}
		return array[num2 - 1];
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		static long Get(int bytesPerLong, ReadOnlySpan<byte> bytes, int i)
		{
			ulong num10 = 0uL;
			for (int num11 = bytesPerLong; num11 > 0; num11--)
			{
				num10 = (num10 << 8) | bytes[bytesPerLong * i + num11];
			}
			return (long)num10;
		}
	}

	private static void BitLayout(int ordinal_bits, int node_bits, out int zero_node_shift, out int one_node_shift, out int ordinal_shift)
	{
		zero_node_shift = ordinal_bits + node_bits;
		one_node_shift = ordinal_bits;
		ordinal_shift = 0;
	}

	public int Find(int input)
	{
		BDD bDD = this;
		while (!bDD.IsLeaf)
		{
			bDD = (((input & (1 << bDD.Ordinal)) == 0) ? bDD.Zero : bDD.One);
		}
		return bDD.Ordinal;
	}

	public bool IsEssentiallyBoolean([NotNullWhen(true)] out BDD terminalActingAsTrue)
	{
		if (IsFull || IsEmpty)
		{
			terminalActingAsTrue = null;
			return false;
		}
		if (IsLeaf)
		{
			terminalActingAsTrue = this;
			return true;
		}
		Stack<BDD> stack = new Stack<BDD>();
		HashSet<BDD> hashSet = new HashSet<BDD>();
		stack.Push(this);
		BDD bDD = null;
		while (stack.Count > 0)
		{
			BDD bDD2 = stack.Pop();
			if (bDD2.IsEmpty)
			{
				continue;
			}
			if (bDD2.IsFull)
			{
				terminalActingAsTrue = null;
				return false;
			}
			if (bDD2.IsLeaf)
			{
				if (bDD == null)
				{
					bDD = bDD2;
				}
				else if (bDD != bDD2)
				{
					terminalActingAsTrue = null;
					return false;
				}
			}
			else
			{
				if (hashSet.Add(bDD2.Zero))
				{
					stack.Push(bDD2.Zero);
				}
				if (hashSet.Add(bDD2.One))
				{
					stack.Push(bDD2.One);
				}
			}
		}
		terminalActingAsTrue = bDD;
		return true;
	}

	public int CompareTo(BDD other)
	{
		if (other == null)
		{
			return -1;
		}
		if (IsLeaf)
		{
			if (other.IsLeaf && Ordinal >= other.Ordinal)
			{
				return (Ordinal != other.Ordinal) ? 1 : 0;
			}
			return -1;
		}
		if (other.IsLeaf)
		{
			return 1;
		}
		ulong min = GetMin();
		ulong min2 = other.GetMin();
		if (min >= min2)
		{
			if (min2 >= min)
			{
				return Ordinal.CompareTo(other.Ordinal);
			}
			return 1;
		}
		return -1;
	}
}
