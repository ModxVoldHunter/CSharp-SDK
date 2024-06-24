using System.Runtime.CompilerServices;

namespace System.Collections;

internal static class HashHelper
{
	public const uint HashCollisionThreshold = 100u;

	public const int MaxPrimeArrayLength = 2147483587;

	public const int HashPrime = 101;

	internal static ReadOnlySpan<int> Primes => RuntimeHelpers.CreateSpan<int>((RuntimeFieldHandle)/*OpCode not supported: LdMemberToken*/);

	public static bool IsPrime(int candidate)
	{
		if (((uint)candidate & (true ? 1u : 0u)) != 0)
		{
			int num = (int)Math.Sqrt(candidate);
			for (int i = 3; i <= num; i += 2)
			{
				if (candidate % i == 0)
				{
					return false;
				}
			}
			return true;
		}
		return candidate == 2;
	}

	public static int GetPrime(int min)
	{
		if (min < 0)
		{
			throw new ArgumentException(System.SR.Arg_HTCapacityOverflow);
		}
		ReadOnlySpan<int> primes = Primes;
		for (int i = 0; i < primes.Length; i++)
		{
			int num = primes[i];
			if (num >= min)
			{
				return num;
			}
		}
		for (int j = min | 1; j < int.MaxValue; j += 2)
		{
			if (IsPrime(j) && (j - 1) % 101 != 0)
			{
				return j;
			}
		}
		return min;
	}

	public static int ExpandPrime(int oldSize)
	{
		int num = 2 * oldSize;
		if ((uint)num > 2147483587u && 2147483587 > oldSize)
		{
			return 2147483587;
		}
		return GetPrime(num);
	}

	public static ulong GetFastModMultiplier(uint divisor)
	{
		return ulong.MaxValue / (ulong)divisor + 1;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static uint FastMod(uint value, uint divisor, ulong multiplier)
	{
		return (uint)(((multiplier * value >> 32) + 1) * divisor >> 32);
	}
}
