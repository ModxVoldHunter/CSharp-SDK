using System.Numerics;

namespace System.Reflection.Internal;

internal static class BitArithmetic
{
	internal static int CountBits(int v)
	{
		return CountBits((uint)v);
	}

	internal static int CountBits(uint v)
	{
		return BitOperations.PopCount(v);
	}

	internal static int CountBits(ulong v)
	{
		return BitOperations.PopCount(v);
	}

	internal static uint Align(uint position, uint alignment)
	{
		uint num = position & ~(alignment - 1);
		if (num == position)
		{
			return num;
		}
		return num + alignment;
	}

	internal static int Align(int position, int alignment)
	{
		int num = position & ~(alignment - 1);
		if (num == position)
		{
			return num;
		}
		return num + alignment;
	}
}
