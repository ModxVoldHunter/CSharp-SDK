using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.Arm;
using System.Runtime.Intrinsics.Wasm;
using System.Runtime.Intrinsics.X86;

namespace System.Buffers;

internal readonly struct ProbabilisticMap
{
	private readonly uint _e0;

	private readonly uint _e1;

	private readonly uint _e2;

	private readonly uint _e3;

	private readonly uint _e4;

	private readonly uint _e5;

	private readonly uint _e6;

	private readonly uint _e7;

	public ProbabilisticMap(ReadOnlySpan<char> values)
	{
		_e0 = 0u;
		_e1 = 0u;
		_e2 = 0u;
		_e3 = 0u;
		_e4 = 0u;
		_e5 = 0u;
		_e6 = 0u;
		_e7 = 0u;
		bool flag = false;
		ref readonly uint e = ref _e0;
		for (int i = 0; i < values.Length; i++)
		{
			int num = values[i];
			SetCharBit(ref e, (byte)num);
			num >>= 8;
			if (num == 0)
			{
				flag = true;
			}
			else
			{
				SetCharBit(ref e, (byte)num);
			}
		}
		if (flag)
		{
			SetCharBit(ref e, 0);
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static void SetCharBit(ref uint charMap, byte value)
	{
		if (Sse41.IsSupported ? true : false)
		{
			Unsafe.Add(ref Unsafe.As<uint, byte>(ref charMap), value & 0x1Fu) |= (byte)(1 << (value >> 5));
		}
		else
		{
			Unsafe.Add(ref charMap, value & 7u) |= (uint)(1 << (value >> 3));
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static bool IsCharBitSet(ref uint charMap, byte value)
	{
		if (!Sse41.IsSupported)
		{
			_ = 0;
			return (Unsafe.Add(ref charMap, value & 7u) & (uint)(1 << (value >> 3))) != 0;
		}
		return (Unsafe.Add(ref Unsafe.As<uint, byte>(ref charMap), value & 0x1Fu) & (1 << (value >> 5))) != 0;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static bool Contains(ref uint charMap, ReadOnlySpan<char> values, int ch)
	{
		if (IsCharBitSet(ref charMap, (byte)ch) && IsCharBitSet(ref charMap, (byte)(ch >> 8)))
		{
			return Contains(values, (char)ch);
		}
		return false;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static bool Contains(ReadOnlySpan<char> values, char ch)
	{
		return SpanHelpers.NonPackedContainsValueType(ref Unsafe.As<char, short>(ref MemoryMarshal.GetReference(values)), (short)ch, values.Length);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[CompExactlyDependsOn(typeof(Avx2))]
	private static Vector256<byte> ContainsMask32CharsAvx2(Vector256<byte> charMapLower, Vector256<byte> charMapUpper, ref char searchSpace)
	{
		Vector256<ushort> vector = Vector256.LoadUnsafe(ref searchSpace);
		Vector256<ushort> vector2 = Vector256.LoadUnsafe(ref searchSpace, (nuint)Vector256<ushort>.Count);
		Vector256<byte> values = Avx2.PackUnsignedSaturate((vector & Vector256.Create((ushort)255)).AsInt16(), (vector2 & Vector256.Create((ushort)255)).AsInt16());
		Vector256<byte> values2 = Avx2.PackUnsignedSaturate((vector >>> 8).AsInt16(), (vector2 >>> 8).AsInt16());
		Vector256<byte> vector3 = IsCharBitSetAvx2(charMapLower, charMapUpper, values);
		Vector256<byte> vector4 = IsCharBitSetAvx2(charMapLower, charMapUpper, values2);
		return vector3 & vector4;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[CompExactlyDependsOn(typeof(Avx2))]
	private static Vector256<byte> IsCharBitSetAvx2(Vector256<byte> charMapLower, Vector256<byte> charMapUpper, Vector256<byte> values)
	{
		Vector256<byte> mask = values >>> 5;
		Vector256<byte> vector = Avx2.Shuffle(Vector256.Create(9241421688590303745uL).AsByte(), mask);
		Vector256<byte> vector2 = values & Vector256.Create((byte)31);
		Vector256<byte> right = Avx2.Shuffle(charMapLower, vector2);
		Vector256<byte> left = Avx2.Shuffle(charMapUpper, vector2 - Vector256.Create((byte)16));
		Vector256<byte> condition = Vector256.GreaterThan(vector2, Vector256.Create((byte)15));
		Vector256<byte> vector3 = Vector256.ConditionalSelect(condition, left, right);
		return ~Vector256.Equals(vector3 & vector, Vector256<byte>.Zero);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[CompExactlyDependsOn(typeof(AdvSimd.Arm64))]
	[CompExactlyDependsOn(typeof(Sse2))]
	private static Vector128<byte> ContainsMask16Chars(Vector128<byte> charMapLower, Vector128<byte> charMapUpper, ref char searchSpace)
	{
		Vector128<ushort> vector = Vector128.LoadUnsafe(ref searchSpace);
		Vector128<ushort> vector2 = Vector128.LoadUnsafe(ref searchSpace, (nuint)Vector128<ushort>.Count);
		Vector128<byte> values = (Sse2.IsSupported ? Sse2.PackUnsignedSaturate((vector & Vector128.Create((ushort)255)).AsInt16(), (vector2 & Vector128.Create((ushort)255)).AsInt16()) : AdvSimd.Arm64.UnzipEven(vector.AsByte(), vector2.AsByte()));
		Vector128<byte> values2 = (Sse2.IsSupported ? Sse2.PackUnsignedSaturate((vector >>> 8).AsInt16(), (vector2 >>> 8).AsInt16()) : AdvSimd.Arm64.UnzipOdd(vector.AsByte(), vector2.AsByte()));
		Vector128<byte> vector3 = IsCharBitSet(charMapLower, charMapUpper, values);
		Vector128<byte> vector4 = IsCharBitSet(charMapLower, charMapUpper, values2);
		return vector3 & vector4;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[CompExactlyDependsOn(typeof(Sse2))]
	[CompExactlyDependsOn(typeof(Ssse3))]
	[CompExactlyDependsOn(typeof(AdvSimd))]
	[CompExactlyDependsOn(typeof(AdvSimd.Arm64))]
	[CompExactlyDependsOn(typeof(PackedSimd))]
	private static Vector128<byte> IsCharBitSet(Vector128<byte> charMapLower, Vector128<byte> charMapUpper, Vector128<byte> values)
	{
		Vector128<byte> indices = values >>> 5;
		Vector128<byte> vector = Vector128.ShuffleUnsafe(Vector128.Create(9241421688590303745uL).AsByte(), indices);
		Vector128<byte> vector2 = values & Vector128.Create((byte)31);
		if (false)
		{
		}
		Vector128<byte> right = Vector128.ShuffleUnsafe(charMapLower, vector2);
		Vector128<byte> left = Vector128.ShuffleUnsafe(charMapUpper, vector2 - Vector128.Create((byte)16));
		Vector128<byte> condition = Vector128.GreaterThan(vector2, Vector128.Create((byte)15));
		Vector128<byte> vector3 = Vector128.ConditionalSelect(condition, left, right);
		return ~Vector128.Equals(vector3 & vector, Vector128<byte>.Zero);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static bool ShouldUseSimpleLoop(int searchSpaceLength, int valuesLength)
	{
		if (searchSpaceLength >= Vector128<short>.Count)
		{
			if (searchSpaceLength < 20)
			{
				return searchSpaceLength < valuesLength >> 1;
			}
			return false;
		}
		return true;
	}

	public static int IndexOfAny(ref char searchSpace, int searchSpaceLength, ref char values, int valuesLength)
	{
		ReadOnlySpan<char> readOnlySpan = new ReadOnlySpan<char>(ref values, valuesLength);
		if (ShouldUseSimpleLoop(searchSpaceLength, valuesLength))
		{
			return IndexOfAnySimpleLoop<IndexOfAnyAsciiSearcher.DontNegate>(ref searchSpace, searchSpaceLength, readOnlySpan);
		}
		if (IndexOfAnyAsciiSearcher.TryIndexOfAny(ref searchSpace, searchSpaceLength, readOnlySpan, out var index))
		{
			return index;
		}
		return ProbabilisticIndexOfAny(ref searchSpace, searchSpaceLength, ref values, valuesLength);
	}

	public static int IndexOfAnyExcept(ref char searchSpace, int searchSpaceLength, ref char values, int valuesLength)
	{
		ReadOnlySpan<char> readOnlySpan = new ReadOnlySpan<char>(ref values, valuesLength);
		if (IndexOfAnyAsciiSearcher.IsVectorizationSupported && !ShouldUseSimpleLoop(searchSpaceLength, valuesLength) && IndexOfAnyAsciiSearcher.TryIndexOfAnyExcept(ref searchSpace, searchSpaceLength, readOnlySpan, out var index))
		{
			return index;
		}
		return IndexOfAnySimpleLoop<IndexOfAnyAsciiSearcher.Negate>(ref searchSpace, searchSpaceLength, readOnlySpan);
	}

	public static int LastIndexOfAny(ref char searchSpace, int searchSpaceLength, ref char values, int valuesLength)
	{
		ReadOnlySpan<char> readOnlySpan = new ReadOnlySpan<char>(ref values, valuesLength);
		if (ShouldUseSimpleLoop(searchSpaceLength, valuesLength))
		{
			return LastIndexOfAnySimpleLoop<IndexOfAnyAsciiSearcher.DontNegate>(ref searchSpace, searchSpaceLength, readOnlySpan);
		}
		if (IndexOfAnyAsciiSearcher.TryLastIndexOfAny(ref searchSpace, searchSpaceLength, readOnlySpan, out var index))
		{
			return index;
		}
		return ProbabilisticLastIndexOfAny(ref searchSpace, searchSpaceLength, ref values, valuesLength);
	}

	public static int LastIndexOfAnyExcept(ref char searchSpace, int searchSpaceLength, ref char values, int valuesLength)
	{
		ReadOnlySpan<char> readOnlySpan = new ReadOnlySpan<char>(ref values, valuesLength);
		if (IndexOfAnyAsciiSearcher.IsVectorizationSupported && !ShouldUseSimpleLoop(searchSpaceLength, valuesLength) && IndexOfAnyAsciiSearcher.TryLastIndexOfAnyExcept(ref searchSpace, searchSpaceLength, readOnlySpan, out var index))
		{
			return index;
		}
		return LastIndexOfAnySimpleLoop<IndexOfAnyAsciiSearcher.Negate>(ref searchSpace, searchSpaceLength, readOnlySpan);
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private static int ProbabilisticIndexOfAny(ref char searchSpace, int searchSpaceLength, ref char values, int valuesLength)
	{
		ReadOnlySpan<char> values2 = new ReadOnlySpan<char>(ref values, valuesLength);
		ProbabilisticMap source = new ProbabilisticMap(values2);
		return IndexOfAny(ref Unsafe.As<ProbabilisticMap, uint>(ref source), ref searchSpace, searchSpaceLength, values2);
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private static int ProbabilisticLastIndexOfAny(ref char searchSpace, int searchSpaceLength, ref char values, int valuesLength)
	{
		ReadOnlySpan<char> values2 = new ReadOnlySpan<char>(ref values, valuesLength);
		ProbabilisticMap source = new ProbabilisticMap(values2);
		return LastIndexOfAny(ref Unsafe.As<ProbabilisticMap, uint>(ref source), ref searchSpace, searchSpaceLength, values2);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static int IndexOfAny(ref uint charMap, ref char searchSpace, int searchSpaceLength, ReadOnlySpan<char> values)
	{
		if ((Sse41.IsSupported ? true : false) && searchSpaceLength >= 16)
		{
			return IndexOfAnyVectorized(ref charMap, ref searchSpace, searchSpaceLength, values);
		}
		ref char right = ref Unsafe.Add(ref searchSpace, searchSpaceLength);
		ref char reference = ref searchSpace;
		while (!Unsafe.AreSame(ref reference, ref right))
		{
			int ch = reference;
			if (Contains(ref charMap, values, ch))
			{
				return (int)((nuint)Unsafe.ByteOffset(ref searchSpace, ref reference) / (nuint)2u);
			}
			reference = ref Unsafe.Add(ref reference, 1);
		}
		return -1;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static int LastIndexOfAny(ref uint charMap, ref char searchSpace, int searchSpaceLength, ReadOnlySpan<char> values)
	{
		for (int num = searchSpaceLength - 1; num >= 0; num--)
		{
			int ch = Unsafe.Add(ref searchSpace, num);
			if (Contains(ref charMap, values, ch))
			{
				return num;
			}
		}
		return -1;
	}

	[CompExactlyDependsOn(typeof(AdvSimd.Arm64))]
	[CompExactlyDependsOn(typeof(Sse41))]
	private static int IndexOfAnyVectorized(ref uint charMap, ref char searchSpace, int searchSpaceLength, ReadOnlySpan<char> values)
	{
		ref char reference = ref Unsafe.Add(ref searchSpace, searchSpaceLength);
		ref char reference2 = ref searchSpace;
		Vector128<byte> vector = Vector128.LoadUnsafe(ref Unsafe.As<uint, byte>(ref charMap));
		Vector128<byte> vector2 = Vector128.LoadUnsafe(ref Unsafe.As<uint, byte>(ref charMap), (nuint)Vector128<byte>.Count);
		if (Avx2.IsSupported && searchSpaceLength >= 32)
		{
			Vector256<byte> charMapLower = Vector256.Create(vector, vector);
			Vector256<byte> charMapUpper = Vector256.Create(vector2, vector2);
			ref char reference3 = ref Unsafe.Subtract(ref reference, 32);
			while (true)
			{
				Vector256<byte> vector3 = ContainsMask32CharsAvx2(charMapLower, charMapUpper, ref reference2);
				if (vector3 != Vector256<byte>.Zero)
				{
					vector3 = Avx2.Permute4x64(vector3.AsInt64(), 216).AsByte();
					uint num = vector3.ExtractMostSignificantBits();
					do
					{
						ref char reference4 = ref Unsafe.Add(ref reference2, BitOperations.TrailingZeroCount(num));
						if (Contains(values, reference4))
						{
							return (int)((nuint)Unsafe.ByteOffset(ref searchSpace, ref reference4) / (nuint)2u);
						}
						num = BitOperations.ResetLowestSetBit(num);
					}
					while (num != 0);
				}
				reference2 = ref Unsafe.Add(ref reference2, 32);
				if (Unsafe.IsAddressGreaterThan(ref reference2, ref reference3))
				{
					if (Unsafe.AreSame(ref reference2, ref reference))
					{
						return -1;
					}
					if (Unsafe.ByteOffset(ref reference2, ref reference) <= 32)
					{
						break;
					}
					reference2 = ref reference3;
				}
			}
			reference2 = ref Unsafe.Subtract(ref reference, 16);
		}
		ref char reference5 = ref Unsafe.Subtract(ref reference, 16);
		while (true)
		{
			Vector128<byte> vector4 = ContainsMask16Chars(vector, vector2, ref reference2);
			if (vector4 != Vector128<byte>.Zero)
			{
				uint num2 = vector4.ExtractMostSignificantBits();
				do
				{
					ref char reference6 = ref Unsafe.Add(ref reference2, BitOperations.TrailingZeroCount(num2));
					if (Contains(values, reference6))
					{
						return (int)((nuint)Unsafe.ByteOffset(ref searchSpace, ref reference6) / (nuint)2u);
					}
					num2 = BitOperations.ResetLowestSetBit(num2);
				}
				while (num2 != 0);
			}
			reference2 = ref Unsafe.Add(ref reference2, 16);
			if (Unsafe.IsAddressGreaterThan(ref reference2, ref reference5))
			{
				if (Unsafe.AreSame(ref reference2, ref reference))
				{
					break;
				}
				reference2 = ref reference5;
			}
		}
		return -1;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static int IndexOfAnySimpleLoop<TNegator>(ref char searchSpace, int searchSpaceLength, ReadOnlySpan<char> values) where TNegator : struct, IndexOfAnyAsciiSearcher.INegator
	{
		ref char right = ref Unsafe.Add(ref searchSpace, searchSpaceLength);
		ref char reference = ref searchSpace;
		while (!Unsafe.AreSame(ref reference, ref right))
		{
			char ch = reference;
			if (TNegator.NegateIfNeeded(Contains(values, ch)))
			{
				return (int)((nuint)Unsafe.ByteOffset(ref searchSpace, ref reference) / (nuint)2u);
			}
			reference = ref Unsafe.Add(ref reference, 1);
		}
		return -1;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static int LastIndexOfAnySimpleLoop<TNegator>(ref char searchSpace, int searchSpaceLength, ReadOnlySpan<char> values) where TNegator : struct, IndexOfAnyAsciiSearcher.INegator
	{
		for (int num = searchSpaceLength - 1; num >= 0; num--)
		{
			char ch = Unsafe.Add(ref searchSpace, num);
			if (TNegator.NegateIfNeeded(Contains(values, ch)))
			{
				return num;
			}
		}
		return -1;
	}
}
