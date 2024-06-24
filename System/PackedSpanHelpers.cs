using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace System;

internal static class PackedSpanHelpers
{
	public static bool PackedIndexOfIsSupported => Sse2.IsSupported;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public unsafe static bool CanUsePackedIndexOf<T>(T value)
	{
		return (uint)(*(ushort*)(&value) - 1) < 254u;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[CompExactlyDependsOn(typeof(Sse2))]
	public static int IndexOf(ref char searchSpace, char value, int length)
	{
		return IndexOf<SpanHelpers.DontNegate<short>>(ref Unsafe.As<char, short>(ref searchSpace), (short)value, length);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[CompExactlyDependsOn(typeof(Sse2))]
	public static int IndexOfAnyExcept(ref char searchSpace, char value, int length)
	{
		return IndexOf<SpanHelpers.Negate<short>>(ref Unsafe.As<char, short>(ref searchSpace), (short)value, length);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[CompExactlyDependsOn(typeof(Sse2))]
	public static int IndexOfAny(ref char searchSpace, char value0, char value1, int length)
	{
		return IndexOfAny<SpanHelpers.DontNegate<short>>(ref Unsafe.As<char, short>(ref searchSpace), (short)value0, (short)value1, length);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[CompExactlyDependsOn(typeof(Sse2))]
	public static int IndexOfAnyExcept(ref char searchSpace, char value0, char value1, int length)
	{
		return IndexOfAny<SpanHelpers.Negate<short>>(ref Unsafe.As<char, short>(ref searchSpace), (short)value0, (short)value1, length);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[CompExactlyDependsOn(typeof(Sse2))]
	public static int IndexOfAny(ref char searchSpace, char value0, char value1, char value2, int length)
	{
		return IndexOfAny<SpanHelpers.DontNegate<short>>(ref Unsafe.As<char, short>(ref searchSpace), (short)value0, (short)value1, (short)value2, length);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[CompExactlyDependsOn(typeof(Sse2))]
	public static int IndexOfAnyExcept(ref char searchSpace, char value0, char value1, char value2, int length)
	{
		return IndexOfAny<SpanHelpers.Negate<short>>(ref Unsafe.As<char, short>(ref searchSpace), (short)value0, (short)value1, (short)value2, length);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[CompExactlyDependsOn(typeof(Sse2))]
	public static int IndexOfAnyInRange(ref char searchSpace, char lowInclusive, char rangeInclusive, int length)
	{
		return IndexOfAnyInRange<SpanHelpers.DontNegate<short>>(ref Unsafe.As<char, short>(ref searchSpace), (short)lowInclusive, (short)rangeInclusive, length);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[CompExactlyDependsOn(typeof(Sse2))]
	public static int IndexOfAnyExceptInRange(ref char searchSpace, char lowInclusive, char rangeInclusive, int length)
	{
		return IndexOfAnyInRange<SpanHelpers.Negate<short>>(ref Unsafe.As<char, short>(ref searchSpace), (short)lowInclusive, (short)rangeInclusive, length);
	}

	[CompExactlyDependsOn(typeof(Sse2))]
	public static bool Contains(ref short searchSpace, short value, int length)
	{
		if (length < Vector128<short>.Count)
		{
			nuint num = 0u;
			if (length >= 4)
			{
				length -= 4;
				if (searchSpace == value || Unsafe.Add(ref searchSpace, 1) == value || Unsafe.Add(ref searchSpace, 2) == value || Unsafe.Add(ref searchSpace, 3) == value)
				{
					return true;
				}
				num = 4u;
			}
			while (length > 0)
			{
				length--;
				if (Unsafe.Add(ref searchSpace, num) == value)
				{
					return true;
				}
				num++;
			}
		}
		else
		{
			ref short reference = ref searchSpace;
			if (Avx512BW.IsSupported && Vector512.IsHardwareAccelerated && length > Vector512<short>.Count)
			{
				Vector512<byte> left = Vector512.Create((byte)value);
				if (length > 2 * Vector512<short>.Count)
				{
					ref short right = ref Unsafe.Add(ref searchSpace, length - 2 * Vector512<short>.Count);
					do
					{
						Vector512<short> source = Vector512.LoadUnsafe(ref reference);
						Vector512<short> source2 = Vector512.LoadUnsafe(ref reference, (nuint)Vector512<short>.Count);
						Vector512<byte> right2 = PackSources(source, source2);
						if (Vector512.EqualsAny(left, right2))
						{
							return true;
						}
						reference = ref Unsafe.Add(ref reference, 2 * Vector512<short>.Count);
					}
					while (Unsafe.IsAddressLessThan(ref reference, ref right));
				}
				ref short reference2 = ref Unsafe.Add(ref searchSpace, length - Vector512<short>.Count);
				Vector512<short> source3 = Vector512.LoadUnsafe(ref Unsafe.IsAddressGreaterThan(ref reference, ref reference2) ? ref reference2 : ref reference);
				Vector512<short> source4 = Vector512.LoadUnsafe(ref reference2);
				Vector512<byte> right3 = PackSources(source3, source4);
				if (Vector512.EqualsAny(left, right3))
				{
					return true;
				}
			}
			else if (Avx2.IsSupported && length > Vector256<short>.Count)
			{
				Vector256<byte> left2 = Vector256.Create((byte)value);
				if (length > 2 * Vector256<short>.Count)
				{
					ref short right4 = ref Unsafe.Add(ref searchSpace, length - 2 * Vector256<short>.Count);
					do
					{
						Vector256<short> source5 = Vector256.LoadUnsafe(ref reference);
						Vector256<short> source6 = Vector256.LoadUnsafe(ref reference, (nuint)Vector256<short>.Count);
						Vector256<byte> right5 = PackSources(source5, source6);
						Vector256<byte> vector = Vector256.Equals(left2, right5);
						if (vector != Vector256<byte>.Zero)
						{
							return true;
						}
						reference = ref Unsafe.Add(ref reference, 2 * Vector256<short>.Count);
					}
					while (Unsafe.IsAddressLessThan(ref reference, ref right4));
				}
				ref short reference3 = ref Unsafe.Add(ref searchSpace, length - Vector256<short>.Count);
				Vector256<short> source7 = Vector256.LoadUnsafe(ref Unsafe.IsAddressGreaterThan(ref reference, ref reference3) ? ref reference3 : ref reference);
				Vector256<short> source8 = Vector256.LoadUnsafe(ref reference3);
				Vector256<byte> right6 = PackSources(source7, source8);
				Vector256<byte> vector2 = Vector256.Equals(left2, right6);
				if (vector2 != Vector256<byte>.Zero)
				{
					return true;
				}
			}
			else
			{
				Vector128<byte> left3 = Vector128.Create((byte)value);
				if (!Avx2.IsSupported && length > 2 * Vector128<short>.Count)
				{
					ref short right7 = ref Unsafe.Add(ref searchSpace, length - 2 * Vector128<short>.Count);
					do
					{
						Vector128<short> source9 = Vector128.LoadUnsafe(ref reference);
						Vector128<short> source10 = Vector128.LoadUnsafe(ref reference, (nuint)Vector128<short>.Count);
						Vector128<byte> right8 = PackSources(source9, source10);
						Vector128<byte> vector3 = Vector128.Equals(left3, right8);
						if (vector3 != Vector128<byte>.Zero)
						{
							return true;
						}
						reference = ref Unsafe.Add(ref reference, 2 * Vector128<short>.Count);
					}
					while (Unsafe.IsAddressLessThan(ref reference, ref right7));
				}
				ref short reference4 = ref Unsafe.Add(ref searchSpace, length - Vector128<short>.Count);
				Vector128<short> source11 = Vector128.LoadUnsafe(ref Unsafe.IsAddressGreaterThan(ref reference, ref reference4) ? ref reference4 : ref reference);
				Vector128<short> source12 = Vector128.LoadUnsafe(ref reference4);
				Vector128<byte> right9 = PackSources(source11, source12);
				Vector128<byte> vector4 = Vector128.Equals(left3, right9);
				if (vector4 != Vector128<byte>.Zero)
				{
					return true;
				}
			}
		}
		return false;
	}

	[CompExactlyDependsOn(typeof(Sse2))]
	private static int IndexOf<TNegator>(ref short searchSpace, short value, int length) where TNegator : struct, SpanHelpers.INegator<short>
	{
		if (length < Vector128<short>.Count)
		{
			nuint num = 0u;
			if (length >= 4)
			{
				length -= 4;
				if (TNegator.NegateIfNeeded(searchSpace == value))
				{
					return 0;
				}
				if (TNegator.NegateIfNeeded(Unsafe.Add(ref searchSpace, 1) == value))
				{
					return 1;
				}
				if (TNegator.NegateIfNeeded(Unsafe.Add(ref searchSpace, 2) == value))
				{
					return 2;
				}
				if (TNegator.NegateIfNeeded(Unsafe.Add(ref searchSpace, 3) == value))
				{
					return 3;
				}
				num = 4u;
			}
			while (length > 0)
			{
				length--;
				if (TNegator.NegateIfNeeded(Unsafe.Add(ref searchSpace, num) == value))
				{
					return (int)num;
				}
				num++;
			}
		}
		else
		{
			ref short reference = ref searchSpace;
			if (Avx512BW.IsSupported && Vector512.IsHardwareAccelerated && length > Vector512<short>.Count)
			{
				Vector512<byte> left = Vector512.Create((byte)value);
				if (length > 2 * Vector512<short>.Count)
				{
					ref short right = ref Unsafe.Add(ref searchSpace, length - 2 * Vector512<short>.Count);
					do
					{
						Vector512<short> source = Vector512.LoadUnsafe(ref reference);
						Vector512<short> source2 = Vector512.LoadUnsafe(ref reference, (nuint)Vector512<short>.Count);
						Vector512<byte> right2 = PackSources(source, source2);
						if (HasMatch<TNegator>(left, right2))
						{
							return ComputeFirstIndex(ref searchSpace, ref reference, GetMatchMask<TNegator>(left, right2));
						}
						reference = ref Unsafe.Add(ref reference, 2 * Vector512<short>.Count);
					}
					while (Unsafe.IsAddressLessThan(ref reference, ref right));
				}
				ref short reference2 = ref Unsafe.Add(ref searchSpace, length - Vector512<short>.Count);
				ref short reference3 = ref Unsafe.IsAddressGreaterThan(ref reference, ref reference2) ? ref reference2 : ref reference;
				Vector512<short> source3 = Vector512.LoadUnsafe(ref reference3);
				Vector512<short> source4 = Vector512.LoadUnsafe(ref reference2);
				Vector512<byte> right3 = PackSources(source3, source4);
				if (HasMatch<TNegator>(left, right3))
				{
					return ComputeFirstIndexOverlapped(ref searchSpace, ref reference3, ref reference2, GetMatchMask<TNegator>(left, right3));
				}
			}
			else if (Avx2.IsSupported && length > Vector256<short>.Count)
			{
				Vector256<byte> left2 = Vector256.Create((byte)value);
				if (length > 2 * Vector256<short>.Count)
				{
					ref short right4 = ref Unsafe.Add(ref searchSpace, length - 2 * Vector256<short>.Count);
					do
					{
						Vector256<short> source5 = Vector256.LoadUnsafe(ref reference);
						Vector256<short> source6 = Vector256.LoadUnsafe(ref reference, (nuint)Vector256<short>.Count);
						Vector256<byte> right5 = PackSources(source5, source6);
						Vector256<byte> result = Vector256.Equals(left2, right5);
						result = NegateIfNeeded<TNegator>(result);
						if (result != Vector256<byte>.Zero)
						{
							return ComputeFirstIndex(ref searchSpace, ref reference, result);
						}
						reference = ref Unsafe.Add(ref reference, 2 * Vector256<short>.Count);
					}
					while (Unsafe.IsAddressLessThan(ref reference, ref right4));
				}
				ref short reference4 = ref Unsafe.Add(ref searchSpace, length - Vector256<short>.Count);
				ref short reference5 = ref Unsafe.IsAddressGreaterThan(ref reference, ref reference4) ? ref reference4 : ref reference;
				Vector256<short> source7 = Vector256.LoadUnsafe(ref reference5);
				Vector256<short> source8 = Vector256.LoadUnsafe(ref reference4);
				Vector256<byte> right6 = PackSources(source7, source8);
				Vector256<byte> result2 = Vector256.Equals(left2, right6);
				result2 = NegateIfNeeded<TNegator>(result2);
				if (result2 != Vector256<byte>.Zero)
				{
					return ComputeFirstIndexOverlapped(ref searchSpace, ref reference5, ref reference4, result2);
				}
			}
			else
			{
				Vector128<byte> left3 = Vector128.Create((byte)value);
				if (!Avx2.IsSupported && length > 2 * Vector128<short>.Count)
				{
					ref short right7 = ref Unsafe.Add(ref searchSpace, length - 2 * Vector128<short>.Count);
					do
					{
						Vector128<short> source9 = Vector128.LoadUnsafe(ref reference);
						Vector128<short> source10 = Vector128.LoadUnsafe(ref reference, (nuint)Vector128<short>.Count);
						Vector128<byte> right8 = PackSources(source9, source10);
						Vector128<byte> result3 = Vector128.Equals(left3, right8);
						result3 = NegateIfNeeded<TNegator>(result3);
						if (result3 != Vector128<byte>.Zero)
						{
							return ComputeFirstIndex(ref searchSpace, ref reference, result3);
						}
						reference = ref Unsafe.Add(ref reference, 2 * Vector128<short>.Count);
					}
					while (Unsafe.IsAddressLessThan(ref reference, ref right7));
				}
				ref short reference6 = ref Unsafe.Add(ref searchSpace, length - Vector128<short>.Count);
				ref short reference7 = ref Unsafe.IsAddressGreaterThan(ref reference, ref reference6) ? ref reference6 : ref reference;
				Vector128<short> source11 = Vector128.LoadUnsafe(ref reference7);
				Vector128<short> source12 = Vector128.LoadUnsafe(ref reference6);
				Vector128<byte> right9 = PackSources(source11, source12);
				Vector128<byte> result4 = Vector128.Equals(left3, right9);
				result4 = NegateIfNeeded<TNegator>(result4);
				if (result4 != Vector128<byte>.Zero)
				{
					return ComputeFirstIndexOverlapped(ref searchSpace, ref reference7, ref reference6, result4);
				}
			}
		}
		return -1;
	}

	[CompExactlyDependsOn(typeof(Sse2))]
	private static int IndexOfAny<TNegator>(ref short searchSpace, short value0, short value1, int length) where TNegator : struct, SpanHelpers.INegator<short>
	{
		if (length < Vector128<short>.Count)
		{
			nuint num = 0u;
			if (length >= 4)
			{
				length -= 4;
				short num2 = searchSpace;
				if (TNegator.NegateIfNeeded(num2 == value0 || num2 == value1))
				{
					return 0;
				}
				num2 = Unsafe.Add(ref searchSpace, 1);
				if (TNegator.NegateIfNeeded(num2 == value0 || num2 == value1))
				{
					return 1;
				}
				num2 = Unsafe.Add(ref searchSpace, 2);
				if (TNegator.NegateIfNeeded(num2 == value0 || num2 == value1))
				{
					return 2;
				}
				num2 = Unsafe.Add(ref searchSpace, 3);
				if (TNegator.NegateIfNeeded(num2 == value0 || num2 == value1))
				{
					return 3;
				}
				num = 4u;
			}
			while (length > 0)
			{
				length--;
				short num2 = Unsafe.Add(ref searchSpace, num);
				if (TNegator.NegateIfNeeded(num2 == value0 || num2 == value1))
				{
					return (int)num;
				}
				num++;
			}
		}
		else
		{
			ref short reference = ref searchSpace;
			if (Avx512BW.IsSupported && Vector512.IsHardwareAccelerated && length > Vector512<short>.Count)
			{
				Vector512<byte> left = Vector512.Create((byte)value0);
				Vector512<byte> left2 = Vector512.Create((byte)value1);
				if (length > 2 * Vector512<short>.Count)
				{
					ref short right = ref Unsafe.Add(ref searchSpace, length - 2 * Vector512<short>.Count);
					do
					{
						Vector512<short> source = Vector512.LoadUnsafe(ref reference);
						Vector512<short> source2 = Vector512.LoadUnsafe(ref reference, (nuint)Vector512<short>.Count);
						Vector512<byte> right2 = PackSources(source, source2);
						Vector512<byte> vector = NegateIfNeeded<TNegator>(Vector512.Equals(left, right2) | Vector512.Equals(left2, right2));
						if (vector != Vector512<byte>.Zero)
						{
							return ComputeFirstIndex(ref searchSpace, ref reference, vector);
						}
						reference = ref Unsafe.Add(ref reference, 2 * Vector512<short>.Count);
					}
					while (Unsafe.IsAddressLessThan(ref reference, ref right));
				}
				ref short reference2 = ref Unsafe.Add(ref searchSpace, length - Vector512<short>.Count);
				ref short reference3 = ref Unsafe.IsAddressGreaterThan(ref reference, ref reference2) ? ref reference2 : ref reference;
				Vector512<short> source3 = Vector512.LoadUnsafe(ref reference3);
				Vector512<short> source4 = Vector512.LoadUnsafe(ref reference2);
				Vector512<byte> right3 = PackSources(source3, source4);
				Vector512<byte> vector2 = NegateIfNeeded<TNegator>(Vector512.Equals(left, right3) | Vector512.Equals(left2, right3));
				if (vector2 != Vector512<byte>.Zero)
				{
					return ComputeFirstIndexOverlapped(ref searchSpace, ref reference3, ref reference2, vector2);
				}
			}
			else if (Avx2.IsSupported && length > Vector256<short>.Count)
			{
				Vector256<byte> left3 = Vector256.Create((byte)value0);
				Vector256<byte> left4 = Vector256.Create((byte)value1);
				if (length > 2 * Vector256<short>.Count)
				{
					ref short right4 = ref Unsafe.Add(ref searchSpace, length - 2 * Vector256<short>.Count);
					do
					{
						Vector256<short> source5 = Vector256.LoadUnsafe(ref reference);
						Vector256<short> source6 = Vector256.LoadUnsafe(ref reference, (nuint)Vector256<short>.Count);
						Vector256<byte> right5 = PackSources(source5, source6);
						Vector256<byte> result = Vector256.Equals(left3, right5) | Vector256.Equals(left4, right5);
						result = NegateIfNeeded<TNegator>(result);
						if (result != Vector256<byte>.Zero)
						{
							return ComputeFirstIndex(ref searchSpace, ref reference, result);
						}
						reference = ref Unsafe.Add(ref reference, 2 * Vector256<short>.Count);
					}
					while (Unsafe.IsAddressLessThan(ref reference, ref right4));
				}
				ref short reference4 = ref Unsafe.Add(ref searchSpace, length - Vector256<short>.Count);
				ref short reference5 = ref Unsafe.IsAddressGreaterThan(ref reference, ref reference4) ? ref reference4 : ref reference;
				Vector256<short> source7 = Vector256.LoadUnsafe(ref reference5);
				Vector256<short> source8 = Vector256.LoadUnsafe(ref reference4);
				Vector256<byte> right6 = PackSources(source7, source8);
				Vector256<byte> result2 = Vector256.Equals(left3, right6) | Vector256.Equals(left4, right6);
				result2 = NegateIfNeeded<TNegator>(result2);
				if (result2 != Vector256<byte>.Zero)
				{
					return ComputeFirstIndexOverlapped(ref searchSpace, ref reference5, ref reference4, result2);
				}
			}
			else
			{
				Vector128<byte> left5 = Vector128.Create((byte)value0);
				Vector128<byte> left6 = Vector128.Create((byte)value1);
				if (!Avx2.IsSupported && length > 2 * Vector128<short>.Count)
				{
					ref short right7 = ref Unsafe.Add(ref searchSpace, length - 2 * Vector128<short>.Count);
					do
					{
						Vector128<short> source9 = Vector128.LoadUnsafe(ref reference);
						Vector128<short> source10 = Vector128.LoadUnsafe(ref reference, (nuint)Vector128<short>.Count);
						Vector128<byte> right8 = PackSources(source9, source10);
						Vector128<byte> result3 = Vector128.Equals(left5, right8) | Vector128.Equals(left6, right8);
						result3 = NegateIfNeeded<TNegator>(result3);
						if (result3 != Vector128<byte>.Zero)
						{
							return ComputeFirstIndex(ref searchSpace, ref reference, result3);
						}
						reference = ref Unsafe.Add(ref reference, 2 * Vector128<short>.Count);
					}
					while (Unsafe.IsAddressLessThan(ref reference, ref right7));
				}
				ref short reference6 = ref Unsafe.Add(ref searchSpace, length - Vector128<short>.Count);
				ref short reference7 = ref Unsafe.IsAddressGreaterThan(ref reference, ref reference6) ? ref reference6 : ref reference;
				Vector128<short> source11 = Vector128.LoadUnsafe(ref reference7);
				Vector128<short> source12 = Vector128.LoadUnsafe(ref reference6);
				Vector128<byte> right9 = PackSources(source11, source12);
				Vector128<byte> result4 = Vector128.Equals(left5, right9) | Vector128.Equals(left6, right9);
				result4 = NegateIfNeeded<TNegator>(result4);
				if (result4 != Vector128<byte>.Zero)
				{
					return ComputeFirstIndexOverlapped(ref searchSpace, ref reference7, ref reference6, result4);
				}
			}
		}
		return -1;
	}

	[CompExactlyDependsOn(typeof(Sse2))]
	private static int IndexOfAny<TNegator>(ref short searchSpace, short value0, short value1, short value2, int length) where TNegator : struct, SpanHelpers.INegator<short>
	{
		if (length < Vector128<short>.Count)
		{
			nuint num = 0u;
			if (length >= 4)
			{
				length -= 4;
				short num2 = searchSpace;
				if (TNegator.NegateIfNeeded(num2 == value0 || num2 == value1 || num2 == value2))
				{
					return 0;
				}
				num2 = Unsafe.Add(ref searchSpace, 1);
				if (TNegator.NegateIfNeeded(num2 == value0 || num2 == value1 || num2 == value2))
				{
					return 1;
				}
				num2 = Unsafe.Add(ref searchSpace, 2);
				if (TNegator.NegateIfNeeded(num2 == value0 || num2 == value1 || num2 == value2))
				{
					return 2;
				}
				num2 = Unsafe.Add(ref searchSpace, 3);
				if (TNegator.NegateIfNeeded(num2 == value0 || num2 == value1 || num2 == value2))
				{
					return 3;
				}
				num = 4u;
			}
			while (length > 0)
			{
				length--;
				short num2 = Unsafe.Add(ref searchSpace, num);
				if (TNegator.NegateIfNeeded(num2 == value0 || num2 == value1 || num2 == value2))
				{
					return (int)num;
				}
				num++;
			}
		}
		else
		{
			ref short reference = ref searchSpace;
			if (Avx512BW.IsSupported && Vector512.IsHardwareAccelerated && length > Vector512<short>.Count)
			{
				Vector512<byte> left = Vector512.Create((byte)value0);
				Vector512<byte> left2 = Vector512.Create((byte)value1);
				Vector512<byte> left3 = Vector512.Create((byte)value2);
				if (length > 2 * Vector512<short>.Count)
				{
					ref short right = ref Unsafe.Add(ref searchSpace, length - 2 * Vector512<short>.Count);
					do
					{
						Vector512<short> source = Vector512.LoadUnsafe(ref reference);
						Vector512<short> source2 = Vector512.LoadUnsafe(ref reference, (nuint)Vector512<short>.Count);
						Vector512<byte> right2 = PackSources(source, source2);
						Vector512<byte> vector = NegateIfNeeded<TNegator>(Vector512.Equals(left, right2) | Vector512.Equals(left2, right2) | Vector512.Equals(left3, right2));
						if (vector != Vector512<byte>.Zero)
						{
							return ComputeFirstIndex(ref searchSpace, ref reference, vector);
						}
						reference = ref Unsafe.Add(ref reference, 2 * Vector512<short>.Count);
					}
					while (Unsafe.IsAddressLessThan(ref reference, ref right));
				}
				ref short reference2 = ref Unsafe.Add(ref searchSpace, length - Vector512<short>.Count);
				ref short reference3 = ref Unsafe.IsAddressGreaterThan(ref reference, ref reference2) ? ref reference2 : ref reference;
				Vector512<short> source3 = Vector512.LoadUnsafe(ref reference3);
				Vector512<short> source4 = Vector512.LoadUnsafe(ref reference2);
				Vector512<byte> right3 = PackSources(source3, source4);
				Vector512<byte> vector2 = NegateIfNeeded<TNegator>(Vector512.Equals(left, right3) | Vector512.Equals(left2, right3) | Vector512.Equals(left3, right3));
				if (vector2 != Vector512<byte>.Zero)
				{
					return ComputeFirstIndexOverlapped(ref searchSpace, ref reference3, ref reference2, vector2);
				}
			}
			else if (Avx2.IsSupported && length > Vector256<short>.Count)
			{
				Vector256<byte> left4 = Vector256.Create((byte)value0);
				Vector256<byte> left5 = Vector256.Create((byte)value1);
				Vector256<byte> left6 = Vector256.Create((byte)value2);
				if (length > 2 * Vector256<short>.Count)
				{
					ref short right4 = ref Unsafe.Add(ref searchSpace, length - 2 * Vector256<short>.Count);
					do
					{
						Vector256<short> source5 = Vector256.LoadUnsafe(ref reference);
						Vector256<short> source6 = Vector256.LoadUnsafe(ref reference, (nuint)Vector256<short>.Count);
						Vector256<byte> right5 = PackSources(source5, source6);
						Vector256<byte> result = Vector256.Equals(left4, right5) | Vector256.Equals(left5, right5) | Vector256.Equals(left6, right5);
						result = NegateIfNeeded<TNegator>(result);
						if (result != Vector256<byte>.Zero)
						{
							return ComputeFirstIndex(ref searchSpace, ref reference, result);
						}
						reference = ref Unsafe.Add(ref reference, 2 * Vector256<short>.Count);
					}
					while (Unsafe.IsAddressLessThan(ref reference, ref right4));
				}
				ref short reference4 = ref Unsafe.Add(ref searchSpace, length - Vector256<short>.Count);
				ref short reference5 = ref Unsafe.IsAddressGreaterThan(ref reference, ref reference4) ? ref reference4 : ref reference;
				Vector256<short> source7 = Vector256.LoadUnsafe(ref reference5);
				Vector256<short> source8 = Vector256.LoadUnsafe(ref reference4);
				Vector256<byte> right6 = PackSources(source7, source8);
				Vector256<byte> result2 = Vector256.Equals(left4, right6) | Vector256.Equals(left5, right6) | Vector256.Equals(left6, right6);
				result2 = NegateIfNeeded<TNegator>(result2);
				if (result2 != Vector256<byte>.Zero)
				{
					return ComputeFirstIndexOverlapped(ref searchSpace, ref reference5, ref reference4, result2);
				}
			}
			else
			{
				Vector128<byte> left7 = Vector128.Create((byte)value0);
				Vector128<byte> left8 = Vector128.Create((byte)value1);
				Vector128<byte> left9 = Vector128.Create((byte)value2);
				if (!Avx2.IsSupported && length > 2 * Vector128<short>.Count)
				{
					ref short right7 = ref Unsafe.Add(ref searchSpace, length - 2 * Vector128<short>.Count);
					do
					{
						Vector128<short> source9 = Vector128.LoadUnsafe(ref reference);
						Vector128<short> source10 = Vector128.LoadUnsafe(ref reference, (nuint)Vector128<short>.Count);
						Vector128<byte> right8 = PackSources(source9, source10);
						Vector128<byte> result3 = Vector128.Equals(left7, right8) | Vector128.Equals(left8, right8) | Vector128.Equals(left9, right8);
						result3 = NegateIfNeeded<TNegator>(result3);
						if (result3 != Vector128<byte>.Zero)
						{
							return ComputeFirstIndex(ref searchSpace, ref reference, result3);
						}
						reference = ref Unsafe.Add(ref reference, 2 * Vector128<short>.Count);
					}
					while (Unsafe.IsAddressLessThan(ref reference, ref right7));
				}
				ref short reference6 = ref Unsafe.Add(ref searchSpace, length - Vector128<short>.Count);
				ref short reference7 = ref Unsafe.IsAddressGreaterThan(ref reference, ref reference6) ? ref reference6 : ref reference;
				Vector128<short> source11 = Vector128.LoadUnsafe(ref reference7);
				Vector128<short> source12 = Vector128.LoadUnsafe(ref reference6);
				Vector128<byte> right9 = PackSources(source11, source12);
				Vector128<byte> result4 = Vector128.Equals(left7, right9) | Vector128.Equals(left8, right9) | Vector128.Equals(left9, right9);
				result4 = NegateIfNeeded<TNegator>(result4);
				if (result4 != Vector128<byte>.Zero)
				{
					return ComputeFirstIndexOverlapped(ref searchSpace, ref reference7, ref reference6, result4);
				}
			}
		}
		return -1;
	}

	[CompExactlyDependsOn(typeof(Sse2))]
	private static int IndexOfAnyInRange<TNegator>(ref short searchSpace, short lowInclusive, short rangeInclusive, int length) where TNegator : struct, SpanHelpers.INegator<short>
	{
		if (length < Vector128<short>.Count)
		{
			for (int i = 0; i < length; i++)
			{
				uint num = (uint)Unsafe.Add(ref searchSpace, i);
				if (TNegator.NegateIfNeeded((uint)((int)num - (int)lowInclusive) <= (uint)rangeInclusive))
				{
					return i;
				}
			}
		}
		else
		{
			ref short reference = ref searchSpace;
			if (Avx512BW.IsSupported && Vector512.IsHardwareAccelerated && length > Vector512<short>.Count)
			{
				Vector512<byte> vector = Vector512.Create((byte)lowInclusive);
				Vector512<byte> right = Vector512.Create((byte)rangeInclusive);
				if (length > 2 * Vector512<short>.Count)
				{
					ref short right2 = ref Unsafe.Add(ref searchSpace, length - 2 * Vector512<short>.Count);
					do
					{
						Vector512<short> source = Vector512.LoadUnsafe(ref reference);
						Vector512<short> source2 = Vector512.LoadUnsafe(ref reference, (nuint)Vector512<short>.Count);
						Vector512<byte> left = PackSources(source, source2) - vector;
						if (HasMatchInRange<TNegator>(left, right))
						{
							return ComputeFirstIndex(ref searchSpace, ref reference, GetMatchInRangeMask<TNegator>(left, right));
						}
						reference = ref Unsafe.Add(ref reference, 2 * Vector512<short>.Count);
					}
					while (Unsafe.IsAddressLessThan(ref reference, ref right2));
				}
				ref short reference2 = ref Unsafe.Add(ref searchSpace, length - Vector512<short>.Count);
				ref short reference3 = ref Unsafe.IsAddressGreaterThan(ref reference, ref reference2) ? ref reference2 : ref reference;
				Vector512<short> source3 = Vector512.LoadUnsafe(ref reference3);
				Vector512<short> source4 = Vector512.LoadUnsafe(ref reference2);
				Vector512<byte> left2 = PackSources(source3, source4) - vector;
				if (HasMatchInRange<TNegator>(left2, right))
				{
					return ComputeFirstIndexOverlapped(ref searchSpace, ref reference3, ref reference2, GetMatchInRangeMask<TNegator>(left2, right));
				}
			}
			else if (Avx2.IsSupported && length > Vector256<short>.Count)
			{
				Vector256<byte> vector2 = Vector256.Create((byte)lowInclusive);
				Vector256<byte> right3 = Vector256.Create((byte)rangeInclusive);
				if (length > 2 * Vector256<short>.Count)
				{
					ref short right4 = ref Unsafe.Add(ref searchSpace, length - 2 * Vector256<short>.Count);
					do
					{
						Vector256<short> source5 = Vector256.LoadUnsafe(ref reference);
						Vector256<short> source6 = Vector256.LoadUnsafe(ref reference, (nuint)Vector256<short>.Count);
						Vector256<byte> vector3 = PackSources(source5, source6);
						Vector256<byte> result = Vector256.LessThanOrEqual(vector3 - vector2, right3);
						result = NegateIfNeeded<TNegator>(result);
						if (result != Vector256<byte>.Zero)
						{
							return ComputeFirstIndex(ref searchSpace, ref reference, result);
						}
						reference = ref Unsafe.Add(ref reference, 2 * Vector256<short>.Count);
					}
					while (Unsafe.IsAddressLessThan(ref reference, ref right4));
				}
				ref short reference4 = ref Unsafe.Add(ref searchSpace, length - Vector256<short>.Count);
				ref short reference5 = ref Unsafe.IsAddressGreaterThan(ref reference, ref reference4) ? ref reference4 : ref reference;
				Vector256<short> source7 = Vector256.LoadUnsafe(ref reference5);
				Vector256<short> source8 = Vector256.LoadUnsafe(ref reference4);
				Vector256<byte> vector4 = PackSources(source7, source8);
				Vector256<byte> result2 = Vector256.LessThanOrEqual(vector4 - vector2, right3);
				result2 = NegateIfNeeded<TNegator>(result2);
				if (result2 != Vector256<byte>.Zero)
				{
					return ComputeFirstIndexOverlapped(ref searchSpace, ref reference5, ref reference4, result2);
				}
			}
			else
			{
				Vector128<byte> vector5 = Vector128.Create((byte)lowInclusive);
				Vector128<byte> right5 = Vector128.Create((byte)rangeInclusive);
				if (!Avx2.IsSupported && length > 2 * Vector128<short>.Count)
				{
					ref short right6 = ref Unsafe.Add(ref searchSpace, length - 2 * Vector128<short>.Count);
					do
					{
						Vector128<short> source9 = Vector128.LoadUnsafe(ref reference);
						Vector128<short> source10 = Vector128.LoadUnsafe(ref reference, (nuint)Vector128<short>.Count);
						Vector128<byte> vector6 = PackSources(source9, source10);
						Vector128<byte> result3 = Vector128.LessThanOrEqual(vector6 - vector5, right5);
						result3 = NegateIfNeeded<TNegator>(result3);
						if (result3 != Vector128<byte>.Zero)
						{
							return ComputeFirstIndex(ref searchSpace, ref reference, result3);
						}
						reference = ref Unsafe.Add(ref reference, 2 * Vector128<short>.Count);
					}
					while (Unsafe.IsAddressLessThan(ref reference, ref right6));
				}
				ref short reference6 = ref Unsafe.Add(ref searchSpace, length - Vector128<short>.Count);
				ref short reference7 = ref Unsafe.IsAddressGreaterThan(ref reference, ref reference6) ? ref reference6 : ref reference;
				Vector128<short> source11 = Vector128.LoadUnsafe(ref reference7);
				Vector128<short> source12 = Vector128.LoadUnsafe(ref reference6);
				Vector128<byte> vector7 = PackSources(source11, source12);
				Vector128<byte> result4 = Vector128.LessThanOrEqual(vector7 - vector5, right5);
				result4 = NegateIfNeeded<TNegator>(result4);
				if (result4 != Vector128<byte>.Zero)
				{
					return ComputeFirstIndexOverlapped(ref searchSpace, ref reference7, ref reference6, result4);
				}
			}
		}
		return -1;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[CompExactlyDependsOn(typeof(Avx512BW))]
	private static Vector512<byte> PackSources(Vector512<short> source0, Vector512<short> source1)
	{
		return Avx512BW.PackUnsignedSaturate(source0, source1).AsByte();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[CompExactlyDependsOn(typeof(Avx2))]
	private static Vector256<byte> PackSources(Vector256<short> source0, Vector256<short> source1)
	{
		return Avx2.PackUnsignedSaturate(source0, source1).AsByte();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[CompExactlyDependsOn(typeof(Sse2))]
	private static Vector128<byte> PackSources(Vector128<short> source0, Vector128<short> source1)
	{
		return Sse2.PackUnsignedSaturate(source0, source1).AsByte();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static Vector128<byte> NegateIfNeeded<TNegator>(Vector128<byte> result) where TNegator : struct, SpanHelpers.INegator<short>
	{
		if (!(typeof(TNegator) == typeof(SpanHelpers.DontNegate<short>)))
		{
			return ~result;
		}
		return result;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static Vector256<byte> NegateIfNeeded<TNegator>(Vector256<byte> result) where TNegator : struct, SpanHelpers.INegator<short>
	{
		if (!(typeof(TNegator) == typeof(SpanHelpers.DontNegate<short>)))
		{
			return ~result;
		}
		return result;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static Vector512<byte> NegateIfNeeded<TNegator>(Vector512<byte> result) where TNegator : struct, SpanHelpers.INegator<short>
	{
		if (!(typeof(TNegator) == typeof(SpanHelpers.DontNegate<short>)))
		{
			return ~result;
		}
		return result;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static bool HasMatch<TNegator>(Vector512<byte> left, Vector512<byte> right) where TNegator : struct, SpanHelpers.INegator<short>
	{
		if (!(typeof(TNegator) == typeof(SpanHelpers.DontNegate<short>)))
		{
			return !Vector512.EqualsAll(left, right);
		}
		return Vector512.EqualsAny(left, right);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static Vector512<byte> GetMatchMask<TNegator>(Vector512<byte> left, Vector512<byte> right) where TNegator : struct, SpanHelpers.INegator<short>
	{
		if (!(typeof(TNegator) == typeof(SpanHelpers.DontNegate<short>)))
		{
			return ~Vector512.Equals(left, right);
		}
		return Vector512.Equals(left, right);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static bool HasMatchInRange<TNegator>(Vector512<byte> left, Vector512<byte> right) where TNegator : struct, SpanHelpers.INegator<short>
	{
		if (!(typeof(TNegator) == typeof(SpanHelpers.DontNegate<short>)))
		{
			return !Vector512.LessThanOrEqualAll(left, right);
		}
		return Vector512.LessThanOrEqualAny(left, right);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static Vector512<byte> GetMatchInRangeMask<TNegator>(Vector512<byte> left, Vector512<byte> right) where TNegator : struct, SpanHelpers.INegator<short>
	{
		if (!(typeof(TNegator) == typeof(SpanHelpers.DontNegate<short>)))
		{
			return ~Vector512.LessThanOrEqual(left, right);
		}
		return Vector512.LessThanOrEqual(left, right);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static int ComputeFirstIndex(ref short searchSpace, ref short current, Vector128<byte> equals)
	{
		uint value = equals.ExtractMostSignificantBits();
		int num = BitOperations.TrailingZeroCount(value);
		return num + (int)((nuint)Unsafe.ByteOffset(ref searchSpace, ref current) / (nuint)2u);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[CompExactlyDependsOn(typeof(Avx2))]
	private static int ComputeFirstIndex(ref short searchSpace, ref short current, Vector256<byte> equals)
	{
		uint value = FixUpPackedVector256Result(equals).ExtractMostSignificantBits();
		int num = BitOperations.TrailingZeroCount(value);
		return num + (int)((nuint)Unsafe.ByteOffset(ref searchSpace, ref current) / (nuint)2u);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[CompExactlyDependsOn(typeof(Avx512F))]
	private static int ComputeFirstIndex(ref short searchSpace, ref short current, Vector512<byte> equals)
	{
		ulong value = FixUpPackedVector512Result(equals).ExtractMostSignificantBits();
		int num = BitOperations.TrailingZeroCount(value);
		return num + (int)((nuint)Unsafe.ByteOffset(ref searchSpace, ref current) / (nuint)2u);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static int ComputeFirstIndexOverlapped(ref short searchSpace, ref short current0, ref short current1, Vector128<byte> equals)
	{
		uint value = equals.ExtractMostSignificantBits();
		int num = BitOperations.TrailingZeroCount(value);
		if (num >= Vector128<short>.Count)
		{
			current0 = ref current1;
			num -= Vector128<short>.Count;
		}
		return num + (int)((nuint)Unsafe.ByteOffset(ref searchSpace, ref current0) / (nuint)2u);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[CompExactlyDependsOn(typeof(Avx2))]
	private static int ComputeFirstIndexOverlapped(ref short searchSpace, ref short current0, ref short current1, Vector256<byte> equals)
	{
		uint value = FixUpPackedVector256Result(equals).ExtractMostSignificantBits();
		int num = BitOperations.TrailingZeroCount(value);
		if (num >= Vector256<short>.Count)
		{
			current0 = ref current1;
			num -= Vector256<short>.Count;
		}
		return num + (int)((nuint)Unsafe.ByteOffset(ref searchSpace, ref current0) / (nuint)2u);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[CompExactlyDependsOn(typeof(Avx512F))]
	private static int ComputeFirstIndexOverlapped(ref short searchSpace, ref short current0, ref short current1, Vector512<byte> equals)
	{
		ulong value = FixUpPackedVector512Result(equals).ExtractMostSignificantBits();
		int num = BitOperations.TrailingZeroCount(value);
		if (num >= Vector512<short>.Count)
		{
			current0 = ref current1;
			num -= Vector512<short>.Count;
		}
		return num + (int)((nuint)Unsafe.ByteOffset(ref searchSpace, ref current0) / (nuint)2u);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[CompExactlyDependsOn(typeof(Avx2))]
	private static Vector256<byte> FixUpPackedVector256Result(Vector256<byte> result)
	{
		return Avx2.Permute4x64(result.AsInt64(), 216).AsByte();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[CompExactlyDependsOn(typeof(Avx512F))]
	private static Vector512<byte> FixUpPackedVector512Result(Vector512<byte> result)
	{
		return Avx512F.PermuteVar8x64(result.AsInt64(), Vector512.Create(0L, 2L, 4L, 6L, 1L, 3L, 5L, 7L)).AsByte();
	}
}
