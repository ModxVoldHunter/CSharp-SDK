using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.Arm;
using System.Runtime.Intrinsics.Wasm;
using System.Runtime.Intrinsics.X86;

namespace System.Buffers;

internal static class IndexOfAnyAsciiSearcher
{
	internal interface INegator
	{
		static abstract bool NegateIfNeeded(bool result);

		static abstract Vector128<byte> NegateIfNeeded(Vector128<byte> result);

		static abstract Vector256<byte> NegateIfNeeded(Vector256<byte> result);

		static abstract uint ExtractMask(Vector128<byte> result);

		static abstract uint ExtractMask(Vector256<byte> result);
	}

	[StructLayout(LayoutKind.Sequential, Size = 1)]
	internal readonly struct DontNegate : INegator
	{
		public static bool NegateIfNeeded(bool result)
		{
			return result;
		}

		public static Vector128<byte> NegateIfNeeded(Vector128<byte> result)
		{
			return result;
		}

		public static Vector256<byte> NegateIfNeeded(Vector256<byte> result)
		{
			return result;
		}

		public static uint ExtractMask(Vector128<byte> result)
		{
			return ~Vector128.Equals(result, Vector128<byte>.Zero).ExtractMostSignificantBits();
		}

		public static uint ExtractMask(Vector256<byte> result)
		{
			return ~Vector256.Equals(result, Vector256<byte>.Zero).ExtractMostSignificantBits();
		}
	}

	[StructLayout(LayoutKind.Sequential, Size = 1)]
	internal readonly struct Negate : INegator
	{
		public static bool NegateIfNeeded(bool result)
		{
			return !result;
		}

		public static Vector128<byte> NegateIfNeeded(Vector128<byte> result)
		{
			return Vector128.Equals(result, Vector128<byte>.Zero);
		}

		public static Vector256<byte> NegateIfNeeded(Vector256<byte> result)
		{
			return Vector256.Equals(result, Vector256<byte>.Zero);
		}

		public static uint ExtractMask(Vector128<byte> result)
		{
			return result.ExtractMostSignificantBits();
		}

		public static uint ExtractMask(Vector256<byte> result)
		{
			return result.ExtractMostSignificantBits();
		}
	}

	internal interface IOptimizations
	{
		static abstract Vector128<byte> PackSources(Vector128<ushort> lower, Vector128<ushort> upper);

		static abstract Vector256<byte> PackSources(Vector256<ushort> lower, Vector256<ushort> upper);
	}

	[StructLayout(LayoutKind.Sequential, Size = 1)]
	internal readonly struct Ssse3AndWasmHandleZeroInNeedle : IOptimizations
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		[CompExactlyDependsOn(typeof(Sse2))]
		[CompExactlyDependsOn(typeof(PackedSimd))]
		public static Vector128<byte> PackSources(Vector128<ushort> lower, Vector128<ushort> upper)
		{
			Vector128<short> vector = Vector128.Min(lower, Vector128.Create((ushort)255)).AsInt16();
			Vector128<short> vector2 = Vector128.Min(upper, Vector128.Create((ushort)255)).AsInt16();
			if (!Sse2.IsSupported)
			{
				return PackedSimd.ConvertNarrowingSaturateUnsigned(vector, vector2);
			}
			return Sse2.PackUnsignedSaturate(vector, vector2);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		[CompExactlyDependsOn(typeof(Avx2))]
		public static Vector256<byte> PackSources(Vector256<ushort> lower, Vector256<ushort> upper)
		{
			return Avx2.PackUnsignedSaturate(Vector256.Min(lower, Vector256.Create((ushort)255)).AsInt16(), Vector256.Min(upper, Vector256.Create((ushort)255)).AsInt16());
		}
	}

	[StructLayout(LayoutKind.Sequential, Size = 1)]
	internal readonly struct Default : IOptimizations
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		[CompExactlyDependsOn(typeof(Sse2))]
		[CompExactlyDependsOn(typeof(AdvSimd))]
		[CompExactlyDependsOn(typeof(PackedSimd))]
		public static Vector128<byte> PackSources(Vector128<ushort> lower, Vector128<ushort> upper)
		{
			if (!Sse2.IsSupported)
			{
				_ = 0;
				return PackedSimd.ConvertNarrowingSaturateUnsigned(lower.AsInt16(), upper.AsInt16());
			}
			return Sse2.PackUnsignedSaturate(lower.AsInt16(), upper.AsInt16());
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		[CompExactlyDependsOn(typeof(Avx2))]
		public static Vector256<byte> PackSources(Vector256<ushort> lower, Vector256<ushort> upper)
		{
			return Avx2.PackUnsignedSaturate(lower.AsInt16(), upper.AsInt16());
		}
	}

	internal static bool IsVectorizationSupported
	{
		get
		{
			if (!Ssse3.IsSupported)
			{
				_ = 0;
				return PackedSimd.IsSupported;
			}
			return true;
		}
	}

	internal unsafe static void ComputeBitmap256(ReadOnlySpan<byte> values, out Vector256<byte> bitmap0, out Vector256<byte> bitmap1, out BitVector256 lookup)
	{
		Vector128<byte> vector = default(Vector128<byte>);
		Vector128<byte> vector2 = default(Vector128<byte>);
		byte* ptr = (byte*)(&vector);
		byte* ptr2 = (byte*)(&vector2);
		BitVector256 bitVector = default(BitVector256);
		ReadOnlySpan<byte> readOnlySpan = values;
		for (int i = 0; i < readOnlySpan.Length; i++)
		{
			byte b = readOnlySpan[i];
			bitVector.Set(b);
			int num = b >> 4;
			int num2 = b & 0xF;
			if (num < 8)
			{
				byte* num3 = ptr + (uint)num2;
				*num3 |= (byte)(1 << num);
			}
			else
			{
				byte* num4 = ptr2 + (uint)num2;
				*num4 |= (byte)(1 << num - 8);
			}
		}
		bitmap0 = Vector256.Create(vector, vector);
		bitmap1 = Vector256.Create(vector2, vector2);
		lookup = bitVector;
	}

	internal unsafe static void ComputeBitmap<T>(ReadOnlySpan<T> values, out Vector256<byte> bitmap, out BitVector256 lookup) where T : struct, IUnsignedNumber<T>
	{
		Vector128<byte> vector = default(Vector128<byte>);
		byte* ptr = (byte*)(&vector);
		BitVector256 bitVector = default(BitVector256);
		ReadOnlySpan<T> readOnlySpan = values;
		for (int i = 0; i < readOnlySpan.Length; i++)
		{
			T value = readOnlySpan[i];
			int num = int.CreateChecked(value);
			if (num <= 127)
			{
				bitVector.Set(num);
				int num2 = num >> 4;
				int num3 = num & 0xF;
				byte* num4 = ptr + (uint)num3;
				*num4 |= (byte)(1 << num2);
			}
		}
		bitmap = Vector256.Create(vector, vector);
		lookup = bitVector;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private unsafe static bool TryComputeBitmap(ReadOnlySpan<char> values, byte* bitmap, out bool needleContainsZero)
	{
		ReadOnlySpan<char> readOnlySpan = values;
		for (int i = 0; i < readOnlySpan.Length; i++)
		{
			char c = readOnlySpan[i];
			if (c > '\u007f')
			{
				needleContainsZero = false;
				return false;
			}
			int num = (int)c >> 4;
			int num2 = c & 0xF;
			byte* num3 = bitmap + (uint)num2;
			*num3 |= (byte)(1 << num);
		}
		needleContainsZero = (*bitmap & 1) != 0;
		return true;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool TryIndexOfAny(ref char searchSpace, int searchSpaceLength, ReadOnlySpan<char> asciiValues, out int index)
	{
		return TryIndexOfAny<DontNegate>(ref Unsafe.As<char, short>(ref searchSpace), searchSpaceLength, asciiValues, out index);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool TryIndexOfAnyExcept(ref char searchSpace, int searchSpaceLength, ReadOnlySpan<char> asciiValues, out int index)
	{
		return TryIndexOfAny<Negate>(ref Unsafe.As<char, short>(ref searchSpace), searchSpaceLength, asciiValues, out index);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool TryLastIndexOfAny(ref char searchSpace, int searchSpaceLength, ReadOnlySpan<char> asciiValues, out int index)
	{
		return TryLastIndexOfAny<DontNegate>(ref Unsafe.As<char, short>(ref searchSpace), searchSpaceLength, asciiValues, out index);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool TryLastIndexOfAnyExcept(ref char searchSpace, int searchSpaceLength, ReadOnlySpan<char> asciiValues, out int index)
	{
		return TryLastIndexOfAny<Negate>(ref Unsafe.As<char, short>(ref searchSpace), searchSpaceLength, asciiValues, out index);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private unsafe static bool TryIndexOfAny<TNegator>(ref short searchSpace, int searchSpaceLength, ReadOnlySpan<char> asciiValues, out int index) where TNegator : struct, INegator
	{
		if (IsVectorizationSupported)
		{
			Vector128<byte> vector = default(Vector128<byte>);
			if (TryComputeBitmap(asciiValues, (byte*)(&vector), out var needleContainsZero))
			{
				Vector256<byte> bitmapRef = Vector256.Create(vector, vector);
				index = (((Ssse3.IsSupported || PackedSimd.IsSupported) && needleContainsZero) ? IndexOfAnyVectorized<TNegator, Ssse3AndWasmHandleZeroInNeedle>(ref searchSpace, searchSpaceLength, ref bitmapRef) : IndexOfAnyVectorized<TNegator, Default>(ref searchSpace, searchSpaceLength, ref bitmapRef));
				return true;
			}
		}
		index = 0;
		return false;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private unsafe static bool TryLastIndexOfAny<TNegator>(ref short searchSpace, int searchSpaceLength, ReadOnlySpan<char> asciiValues, out int index) where TNegator : struct, INegator
	{
		if (IsVectorizationSupported)
		{
			Vector128<byte> vector = default(Vector128<byte>);
			if (TryComputeBitmap(asciiValues, (byte*)(&vector), out var needleContainsZero))
			{
				Vector256<byte> bitmapRef = Vector256.Create(vector, vector);
				index = (((Ssse3.IsSupported || PackedSimd.IsSupported) && needleContainsZero) ? LastIndexOfAnyVectorized<TNegator, Ssse3AndWasmHandleZeroInNeedle>(ref searchSpace, searchSpaceLength, ref bitmapRef) : LastIndexOfAnyVectorized<TNegator, Default>(ref searchSpace, searchSpaceLength, ref bitmapRef));
				return true;
			}
		}
		index = 0;
		return false;
	}

	[CompExactlyDependsOn(typeof(Ssse3))]
	[CompExactlyDependsOn(typeof(AdvSimd))]
	[CompExactlyDependsOn(typeof(PackedSimd))]
	internal static int IndexOfAnyVectorized<TNegator, TOptimizations>(ref short searchSpace, int searchSpaceLength, ref Vector256<byte> bitmapRef) where TNegator : struct, INegator where TOptimizations : struct, IOptimizations
	{
		ref short reference = ref searchSpace;
		if (Avx2.IsSupported && searchSpaceLength > 2 * Vector128<short>.Count)
		{
			Vector256<byte> bitmapLookup = bitmapRef;
			if (searchSpaceLength > 2 * Vector256<short>.Count)
			{
				ref short right = ref Unsafe.Add(ref searchSpace, searchSpaceLength - 2 * Vector256<short>.Count);
				do
				{
					Vector256<short> source = Vector256.LoadUnsafe(ref reference);
					Vector256<short> source2 = Vector256.LoadUnsafe(ref reference, (nuint)Vector256<short>.Count);
					Vector256<byte> vector = IndexOfAnyLookup<TNegator, TOptimizations>(source, source2, bitmapLookup);
					if (vector != Vector256<byte>.Zero)
					{
						return ComputeFirstIndex<short, TNegator>(ref searchSpace, ref reference, vector);
					}
					reference = ref Unsafe.Add(ref reference, 2 * Vector256<short>.Count);
				}
				while (Unsafe.IsAddressLessThan(ref reference, ref right));
			}
			ref short reference2 = ref Unsafe.Add(ref searchSpace, searchSpaceLength - Vector256<short>.Count);
			ref short reference3 = ref Unsafe.IsAddressGreaterThan(ref reference, ref reference2) ? ref reference2 : ref reference;
			Vector256<short> source3 = Vector256.LoadUnsafe(ref reference3);
			Vector256<short> source4 = Vector256.LoadUnsafe(ref reference2);
			Vector256<byte> vector2 = IndexOfAnyLookup<TNegator, TOptimizations>(source3, source4, bitmapLookup);
			if (vector2 != Vector256<byte>.Zero)
			{
				return ComputeFirstIndexOverlapped<short, TNegator>(ref searchSpace, ref reference3, ref reference2, vector2);
			}
			return -1;
		}
		Vector128<byte> lower = bitmapRef._lower;
		if (!Avx2.IsSupported && searchSpaceLength > 2 * Vector128<short>.Count)
		{
			ref short right2 = ref Unsafe.Add(ref searchSpace, searchSpaceLength - 2 * Vector128<short>.Count);
			do
			{
				Vector128<short> source5 = Vector128.LoadUnsafe(ref reference);
				Vector128<short> source6 = Vector128.LoadUnsafe(ref reference, (nuint)Vector128<short>.Count);
				Vector128<byte> vector3 = IndexOfAnyLookup<TNegator, TOptimizations>(source5, source6, lower);
				if (vector3 != Vector128<byte>.Zero)
				{
					return ComputeFirstIndex<short, TNegator>(ref searchSpace, ref reference, vector3);
				}
				reference = ref Unsafe.Add(ref reference, 2 * Vector128<short>.Count);
			}
			while (Unsafe.IsAddressLessThan(ref reference, ref right2));
		}
		ref short reference4 = ref Unsafe.Add(ref searchSpace, searchSpaceLength - Vector128<short>.Count);
		ref short reference5 = ref Unsafe.IsAddressGreaterThan(ref reference, ref reference4) ? ref reference4 : ref reference;
		Vector128<short> source7 = Vector128.LoadUnsafe(ref reference5);
		Vector128<short> source8 = Vector128.LoadUnsafe(ref reference4);
		Vector128<byte> vector4 = IndexOfAnyLookup<TNegator, TOptimizations>(source7, source8, lower);
		if (vector4 != Vector128<byte>.Zero)
		{
			return ComputeFirstIndexOverlapped<short, TNegator>(ref searchSpace, ref reference5, ref reference4, vector4);
		}
		return -1;
	}

	[CompExactlyDependsOn(typeof(Ssse3))]
	[CompExactlyDependsOn(typeof(AdvSimd))]
	[CompExactlyDependsOn(typeof(PackedSimd))]
	internal static int LastIndexOfAnyVectorized<TNegator, TOptimizations>(ref short searchSpace, int searchSpaceLength, ref Vector256<byte> bitmapRef) where TNegator : struct, INegator where TOptimizations : struct, IOptimizations
	{
		ref short reference = ref Unsafe.Add(ref searchSpace, searchSpaceLength);
		if (Avx2.IsSupported && searchSpaceLength > 2 * Vector128<short>.Count)
		{
			Vector256<byte> bitmapLookup = bitmapRef;
			if (searchSpaceLength > 2 * Vector256<short>.Count)
			{
				ref short right = ref Unsafe.Add(ref searchSpace, 2 * Vector256<short>.Count);
				do
				{
					reference = ref Unsafe.Subtract(ref reference, 2 * Vector256<short>.Count);
					Vector256<short> source = Vector256.LoadUnsafe(ref reference);
					Vector256<short> source2 = Vector256.LoadUnsafe(ref reference, (nuint)Vector256<short>.Count);
					Vector256<byte> vector = IndexOfAnyLookup<TNegator, TOptimizations>(source, source2, bitmapLookup);
					if (vector != Vector256<byte>.Zero)
					{
						return ComputeLastIndex<short, TNegator>(ref searchSpace, ref reference, vector);
					}
				}
				while (Unsafe.IsAddressGreaterThan(ref reference, ref right));
			}
			ref short reference2 = ref Unsafe.IsAddressGreaterThan(ref reference, ref Unsafe.Add(ref searchSpace, Vector256<short>.Count)) ? ref Unsafe.Subtract(ref reference, Vector256<short>.Count) : ref searchSpace;
			Vector256<short> source3 = Vector256.LoadUnsafe(ref searchSpace);
			Vector256<short> source4 = Vector256.LoadUnsafe(ref reference2);
			Vector256<byte> vector2 = IndexOfAnyLookup<TNegator, TOptimizations>(source3, source4, bitmapLookup);
			if (vector2 != Vector256<byte>.Zero)
			{
				return ComputeLastIndexOverlapped<short, TNegator>(ref searchSpace, ref reference2, vector2);
			}
			return -1;
		}
		Vector128<byte> lower = bitmapRef._lower;
		if (!Avx2.IsSupported && searchSpaceLength > 2 * Vector128<short>.Count)
		{
			ref short right2 = ref Unsafe.Add(ref searchSpace, 2 * Vector128<short>.Count);
			do
			{
				reference = ref Unsafe.Subtract(ref reference, 2 * Vector128<short>.Count);
				Vector128<short> source5 = Vector128.LoadUnsafe(ref reference);
				Vector128<short> source6 = Vector128.LoadUnsafe(ref reference, (nuint)Vector128<short>.Count);
				Vector128<byte> vector3 = IndexOfAnyLookup<TNegator, TOptimizations>(source5, source6, lower);
				if (vector3 != Vector128<byte>.Zero)
				{
					return ComputeLastIndex<short, TNegator>(ref searchSpace, ref reference, vector3);
				}
			}
			while (Unsafe.IsAddressGreaterThan(ref reference, ref right2));
		}
		ref short reference3 = ref Unsafe.IsAddressGreaterThan(ref reference, ref Unsafe.Add(ref searchSpace, Vector128<short>.Count)) ? ref Unsafe.Subtract(ref reference, Vector128<short>.Count) : ref searchSpace;
		Vector128<short> source7 = Vector128.LoadUnsafe(ref searchSpace);
		Vector128<short> source8 = Vector128.LoadUnsafe(ref reference3);
		Vector128<byte> vector4 = IndexOfAnyLookup<TNegator, TOptimizations>(source7, source8, lower);
		if (vector4 != Vector128<byte>.Zero)
		{
			return ComputeLastIndexOverlapped<short, TNegator>(ref searchSpace, ref reference3, vector4);
		}
		return -1;
	}

	[CompExactlyDependsOn(typeof(Ssse3))]
	[CompExactlyDependsOn(typeof(AdvSimd))]
	[CompExactlyDependsOn(typeof(PackedSimd))]
	internal static int IndexOfAnyVectorized<TNegator>(ref byte searchSpace, int searchSpaceLength, ref Vector256<byte> bitmapRef) where TNegator : struct, INegator
	{
		ref byte reference = ref searchSpace;
		if (Avx2.IsSupported && searchSpaceLength > Vector128<byte>.Count)
		{
			Vector256<byte> bitmapLookup = bitmapRef;
			if (searchSpaceLength > Vector256<byte>.Count)
			{
				ref byte right = ref Unsafe.Add(ref searchSpace, searchSpaceLength - Vector256<byte>.Count);
				do
				{
					Vector256<byte> source = Vector256.LoadUnsafe(ref reference);
					Vector256<byte> vector = TNegator.NegateIfNeeded(IndexOfAnyLookupCore(source, bitmapLookup));
					if (vector != Vector256<byte>.Zero)
					{
						return ComputeFirstIndex<byte, TNegator>(ref searchSpace, ref reference, vector);
					}
					reference = ref Unsafe.Add(ref reference, Vector256<byte>.Count);
				}
				while (Unsafe.IsAddressLessThan(ref reference, ref right));
			}
			ref byte reference2 = ref Unsafe.Add(ref searchSpace, searchSpaceLength - Vector128<byte>.Count);
			ref byte reference3 = ref Unsafe.IsAddressGreaterThan(ref reference, ref reference2) ? ref reference2 : ref reference;
			Vector128<byte> lower = Vector128.LoadUnsafe(ref reference3);
			Vector128<byte> upper = Vector128.LoadUnsafe(ref reference2);
			Vector256<byte> source2 = Vector256.Create(lower, upper);
			Vector256<byte> vector2 = TNegator.NegateIfNeeded(IndexOfAnyLookupCore(source2, bitmapLookup));
			if (vector2 != Vector256<byte>.Zero)
			{
				return ComputeFirstIndexOverlapped<byte, TNegator>(ref searchSpace, ref reference3, ref reference2, vector2);
			}
			return -1;
		}
		Vector128<byte> lower2 = bitmapRef._lower;
		if (!Avx2.IsSupported && searchSpaceLength > Vector128<byte>.Count)
		{
			ref byte right2 = ref Unsafe.Add(ref searchSpace, searchSpaceLength - Vector128<byte>.Count);
			do
			{
				Vector128<byte> source3 = Vector128.LoadUnsafe(ref reference);
				Vector128<byte> vector3 = TNegator.NegateIfNeeded(IndexOfAnyLookupCore(source3, lower2));
				if (vector3 != Vector128<byte>.Zero)
				{
					return ComputeFirstIndex<byte, TNegator>(ref searchSpace, ref reference, vector3);
				}
				reference = ref Unsafe.Add(ref reference, Vector128<byte>.Count);
			}
			while (Unsafe.IsAddressLessThan(ref reference, ref right2));
		}
		ref byte reference4 = ref Unsafe.Add(ref searchSpace, searchSpaceLength - 8);
		ref byte reference5 = ref Unsafe.IsAddressGreaterThan(ref reference, ref reference4) ? ref reference4 : ref reference;
		ulong e = Unsafe.ReadUnaligned<ulong>(ref reference5);
		ulong e2 = Unsafe.ReadUnaligned<ulong>(ref reference4);
		Vector128<byte> source4 = Vector128.Create(e, e2).AsByte();
		Vector128<byte> vector4 = TNegator.NegateIfNeeded(IndexOfAnyLookupCore(source4, lower2));
		if (vector4 != Vector128<byte>.Zero)
		{
			return ComputeFirstIndexOverlapped<byte, TNegator>(ref searchSpace, ref reference5, ref reference4, vector4);
		}
		return -1;
	}

	[CompExactlyDependsOn(typeof(Ssse3))]
	[CompExactlyDependsOn(typeof(AdvSimd))]
	[CompExactlyDependsOn(typeof(PackedSimd))]
	internal static int LastIndexOfAnyVectorized<TNegator>(ref byte searchSpace, int searchSpaceLength, ref Vector256<byte> bitmapRef) where TNegator : struct, INegator
	{
		ref byte reference = ref Unsafe.Add(ref searchSpace, searchSpaceLength);
		if (Avx2.IsSupported && searchSpaceLength > Vector128<byte>.Count)
		{
			Vector256<byte> bitmapLookup = bitmapRef;
			if (searchSpaceLength > Vector256<byte>.Count)
			{
				ref byte right = ref Unsafe.Add(ref searchSpace, Vector256<byte>.Count);
				do
				{
					reference = ref Unsafe.Subtract(ref reference, Vector256<byte>.Count);
					Vector256<byte> source = Vector256.LoadUnsafe(ref reference);
					Vector256<byte> vector = TNegator.NegateIfNeeded(IndexOfAnyLookupCore(source, bitmapLookup));
					if (vector != Vector256<byte>.Zero)
					{
						return ComputeLastIndex<byte, TNegator>(ref searchSpace, ref reference, vector);
					}
				}
				while (Unsafe.IsAddressGreaterThan(ref reference, ref right));
			}
			ref byte reference2 = ref Unsafe.IsAddressGreaterThan(ref reference, ref Unsafe.Add(ref searchSpace, Vector128<byte>.Count)) ? ref Unsafe.Subtract(ref reference, Vector128<byte>.Count) : ref searchSpace;
			Vector128<byte> lower = Vector128.LoadUnsafe(ref searchSpace);
			Vector128<byte> upper = Vector128.LoadUnsafe(ref reference2);
			Vector256<byte> source2 = Vector256.Create(lower, upper);
			Vector256<byte> vector2 = TNegator.NegateIfNeeded(IndexOfAnyLookupCore(source2, bitmapLookup));
			if (vector2 != Vector256<byte>.Zero)
			{
				return ComputeLastIndexOverlapped<byte, TNegator>(ref searchSpace, ref reference2, vector2);
			}
			return -1;
		}
		Vector128<byte> lower2 = bitmapRef._lower;
		if (!Avx2.IsSupported && searchSpaceLength > Vector128<byte>.Count)
		{
			ref byte right2 = ref Unsafe.Add(ref searchSpace, Vector128<byte>.Count);
			do
			{
				reference = ref Unsafe.Subtract(ref reference, Vector128<byte>.Count);
				Vector128<byte> source3 = Vector128.LoadUnsafe(ref reference);
				Vector128<byte> vector3 = TNegator.NegateIfNeeded(IndexOfAnyLookupCore(source3, lower2));
				if (vector3 != Vector128<byte>.Zero)
				{
					return ComputeLastIndex<byte, TNegator>(ref searchSpace, ref reference, vector3);
				}
			}
			while (Unsafe.IsAddressGreaterThan(ref reference, ref right2));
		}
		ref byte reference3 = ref Unsafe.IsAddressGreaterThan(ref reference, ref Unsafe.Add(ref searchSpace, 8)) ? ref Unsafe.Subtract(ref reference, 8) : ref searchSpace;
		ulong e = Unsafe.ReadUnaligned<ulong>(ref searchSpace);
		ulong e2 = Unsafe.ReadUnaligned<ulong>(ref reference3);
		Vector128<byte> source4 = Vector128.Create(e, e2).AsByte();
		Vector128<byte> vector4 = TNegator.NegateIfNeeded(IndexOfAnyLookupCore(source4, lower2));
		if (vector4 != Vector128<byte>.Zero)
		{
			return ComputeLastIndexOverlapped<byte, TNegator>(ref searchSpace, ref reference3, vector4);
		}
		return -1;
	}

	[CompExactlyDependsOn(typeof(Ssse3))]
	[CompExactlyDependsOn(typeof(AdvSimd))]
	[CompExactlyDependsOn(typeof(PackedSimd))]
	internal static int IndexOfAnyVectorizedAnyByte<TNegator>(ref byte searchSpace, int searchSpaceLength, ref Vector512<byte> bitmapsRef) where TNegator : struct, INegator
	{
		ref byte reference = ref searchSpace;
		if (Avx2.IsSupported && searchSpaceLength > Vector128<byte>.Count)
		{
			Vector256<byte> lower = bitmapsRef._lower;
			Vector256<byte> upper = bitmapsRef._upper;
			if (searchSpaceLength > Vector256<byte>.Count)
			{
				ref byte right = ref Unsafe.Add(ref searchSpace, searchSpaceLength - Vector256<byte>.Count);
				do
				{
					Vector256<byte> source = Vector256.LoadUnsafe(ref reference);
					Vector256<byte> vector = IndexOfAnyLookup<TNegator>(source, lower, upper);
					if (vector != Vector256<byte>.Zero)
					{
						return ComputeFirstIndex<byte, TNegator>(ref searchSpace, ref reference, vector);
					}
					reference = ref Unsafe.Add(ref reference, Vector256<byte>.Count);
				}
				while (Unsafe.IsAddressLessThan(ref reference, ref right));
			}
			ref byte reference2 = ref Unsafe.Add(ref searchSpace, searchSpaceLength - Vector128<byte>.Count);
			ref byte reference3 = ref Unsafe.IsAddressGreaterThan(ref reference, ref reference2) ? ref reference2 : ref reference;
			Vector128<byte> lower2 = Vector128.LoadUnsafe(ref reference3);
			Vector128<byte> upper2 = Vector128.LoadUnsafe(ref reference2);
			Vector256<byte> source2 = Vector256.Create(lower2, upper2);
			Vector256<byte> vector2 = IndexOfAnyLookup<TNegator>(source2, lower, upper);
			if (vector2 != Vector256<byte>.Zero)
			{
				return ComputeFirstIndexOverlapped<byte, TNegator>(ref searchSpace, ref reference3, ref reference2, vector2);
			}
			return -1;
		}
		Vector128<byte> lower3 = bitmapsRef._lower._lower;
		Vector128<byte> lower4 = bitmapsRef._upper._lower;
		if (!Avx2.IsSupported && searchSpaceLength > Vector128<byte>.Count)
		{
			ref byte right2 = ref Unsafe.Add(ref searchSpace, searchSpaceLength - Vector128<byte>.Count);
			do
			{
				Vector128<byte> source3 = Vector128.LoadUnsafe(ref reference);
				Vector128<byte> vector3 = IndexOfAnyLookup<TNegator>(source3, lower3, lower4);
				if (vector3 != Vector128<byte>.Zero)
				{
					return ComputeFirstIndex<byte, TNegator>(ref searchSpace, ref reference, vector3);
				}
				reference = ref Unsafe.Add(ref reference, Vector128<byte>.Count);
			}
			while (Unsafe.IsAddressLessThan(ref reference, ref right2));
		}
		ref byte reference4 = ref Unsafe.Add(ref searchSpace, searchSpaceLength - 8);
		ref byte reference5 = ref Unsafe.IsAddressGreaterThan(ref reference, ref reference4) ? ref reference4 : ref reference;
		ulong e = Unsafe.ReadUnaligned<ulong>(ref reference5);
		ulong e2 = Unsafe.ReadUnaligned<ulong>(ref reference4);
		Vector128<byte> source4 = Vector128.Create(e, e2).AsByte();
		Vector128<byte> vector4 = IndexOfAnyLookup<TNegator>(source4, lower3, lower4);
		if (vector4 != Vector128<byte>.Zero)
		{
			return ComputeFirstIndexOverlapped<byte, TNegator>(ref searchSpace, ref reference5, ref reference4, vector4);
		}
		return -1;
	}

	[CompExactlyDependsOn(typeof(Ssse3))]
	[CompExactlyDependsOn(typeof(AdvSimd))]
	[CompExactlyDependsOn(typeof(PackedSimd))]
	internal static int LastIndexOfAnyVectorizedAnyByte<TNegator>(ref byte searchSpace, int searchSpaceLength, ref Vector512<byte> bitmapsRef) where TNegator : struct, INegator
	{
		ref byte reference = ref Unsafe.Add(ref searchSpace, searchSpaceLength);
		if (Avx2.IsSupported && searchSpaceLength > Vector128<byte>.Count)
		{
			Vector256<byte> lower = bitmapsRef._lower;
			Vector256<byte> upper = bitmapsRef._upper;
			if (searchSpaceLength > Vector256<byte>.Count)
			{
				ref byte right = ref Unsafe.Add(ref searchSpace, Vector256<byte>.Count);
				do
				{
					reference = ref Unsafe.Subtract(ref reference, Vector256<byte>.Count);
					Vector256<byte> source = Vector256.LoadUnsafe(ref reference);
					Vector256<byte> vector = IndexOfAnyLookup<TNegator>(source, lower, upper);
					if (vector != Vector256<byte>.Zero)
					{
						return ComputeLastIndex<byte, TNegator>(ref searchSpace, ref reference, vector);
					}
				}
				while (Unsafe.IsAddressGreaterThan(ref reference, ref right));
			}
			ref byte reference2 = ref Unsafe.IsAddressGreaterThan(ref reference, ref Unsafe.Add(ref searchSpace, Vector128<byte>.Count)) ? ref Unsafe.Subtract(ref reference, Vector128<byte>.Count) : ref searchSpace;
			Vector128<byte> lower2 = Vector128.LoadUnsafe(ref searchSpace);
			Vector128<byte> upper2 = Vector128.LoadUnsafe(ref reference2);
			Vector256<byte> source2 = Vector256.Create(lower2, upper2);
			Vector256<byte> vector2 = IndexOfAnyLookup<TNegator>(source2, lower, upper);
			if (vector2 != Vector256<byte>.Zero)
			{
				return ComputeLastIndexOverlapped<byte, TNegator>(ref searchSpace, ref reference2, vector2);
			}
			return -1;
		}
		Vector128<byte> lower3 = bitmapsRef._lower._lower;
		Vector128<byte> lower4 = bitmapsRef._upper._lower;
		if (!Avx2.IsSupported && searchSpaceLength > Vector128<byte>.Count)
		{
			ref byte right2 = ref Unsafe.Add(ref searchSpace, Vector128<byte>.Count);
			do
			{
				reference = ref Unsafe.Subtract(ref reference, Vector128<byte>.Count);
				Vector128<byte> source3 = Vector128.LoadUnsafe(ref reference);
				Vector128<byte> vector3 = IndexOfAnyLookup<TNegator>(source3, lower3, lower4);
				if (vector3 != Vector128<byte>.Zero)
				{
					return ComputeLastIndex<byte, TNegator>(ref searchSpace, ref reference, vector3);
				}
			}
			while (Unsafe.IsAddressGreaterThan(ref reference, ref right2));
		}
		ref byte reference3 = ref Unsafe.IsAddressGreaterThan(ref reference, ref Unsafe.Add(ref searchSpace, 8)) ? ref Unsafe.Subtract(ref reference, 8) : ref searchSpace;
		ulong e = Unsafe.ReadUnaligned<ulong>(ref searchSpace);
		ulong e2 = Unsafe.ReadUnaligned<ulong>(ref reference3);
		Vector128<byte> source4 = Vector128.Create(e, e2).AsByte();
		Vector128<byte> vector4 = IndexOfAnyLookup<TNegator>(source4, lower3, lower4);
		if (vector4 != Vector128<byte>.Zero)
		{
			return ComputeLastIndexOverlapped<byte, TNegator>(ref searchSpace, ref reference3, vector4);
		}
		return -1;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[CompExactlyDependsOn(typeof(Sse2))]
	[CompExactlyDependsOn(typeof(AdvSimd))]
	[CompExactlyDependsOn(typeof(PackedSimd))]
	private static Vector128<byte> IndexOfAnyLookup<TNegator, TOptimizations>(Vector128<short> source0, Vector128<short> source1, Vector128<byte> bitmapLookup) where TNegator : struct, INegator where TOptimizations : struct, IOptimizations
	{
		Vector128<byte> source2 = TOptimizations.PackSources(source0.AsUInt16(), source1.AsUInt16());
		Vector128<byte> result = IndexOfAnyLookupCore(source2, bitmapLookup);
		return TNegator.NegateIfNeeded(result);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[CompExactlyDependsOn(typeof(Ssse3))]
	[CompExactlyDependsOn(typeof(AdvSimd))]
	[CompExactlyDependsOn(typeof(PackedSimd))]
	private static Vector128<byte> IndexOfAnyLookupCore(Vector128<byte> source, Vector128<byte> bitmapLookup)
	{
		Vector128<byte> indices = (Ssse3.IsSupported ? source : (source & Vector128.Create((byte)15)));
		_ = 0;
		Vector128<byte> indices2 = source >>> 4;
		Vector128<byte> vector = Vector128.ShuffleUnsafe(bitmapLookup, indices);
		Vector128<byte> vector2 = Vector128.ShuffleUnsafe(Vector128.Create(9241421688590303745uL, 0uL).AsByte(), indices2);
		return vector & vector2;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[CompExactlyDependsOn(typeof(Avx2))]
	private static Vector256<byte> IndexOfAnyLookup<TNegator, TOptimizations>(Vector256<short> source0, Vector256<short> source1, Vector256<byte> bitmapLookup) where TNegator : struct, INegator where TOptimizations : struct, IOptimizations
	{
		Vector256<byte> source2 = TOptimizations.PackSources(source0.AsUInt16(), source1.AsUInt16());
		Vector256<byte> result = IndexOfAnyLookupCore(source2, bitmapLookup);
		return TNegator.NegateIfNeeded(result);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[CompExactlyDependsOn(typeof(Avx2))]
	private static Vector256<byte> IndexOfAnyLookupCore(Vector256<byte> source, Vector256<byte> bitmapLookup)
	{
		Vector256<byte> mask = source >>> 4;
		Vector256<byte> vector = Avx2.Shuffle(bitmapLookup, source);
		Vector256<byte> vector2 = Avx2.Shuffle(Vector256.Create(9241421688590303745uL).AsByte(), mask);
		return vector & vector2;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[CompExactlyDependsOn(typeof(Ssse3))]
	[CompExactlyDependsOn(typeof(AdvSimd))]
	[CompExactlyDependsOn(typeof(PackedSimd))]
	private static Vector128<byte> IndexOfAnyLookup<TNegator>(Vector128<byte> source, Vector128<byte> bitmapLookup0, Vector128<byte> bitmapLookup1) where TNegator : struct, INegator
	{
		Vector128<byte> indices = source & Vector128.Create((byte)15);
		Vector128<byte> vector = Vector128.ShiftRightLogical(source.AsInt32(), 4).AsByte() & Vector128.Create((byte)15);
		Vector128<byte> right = Vector128.ShuffleUnsafe(bitmapLookup0, indices);
		Vector128<byte> left = Vector128.ShuffleUnsafe(bitmapLookup1, indices);
		Vector128<byte> vector2 = Vector128.ShuffleUnsafe(Vector128.Create(9241421688590303745uL).AsByte(), vector);
		Vector128<byte> condition = Vector128.GreaterThan(vector.AsSByte(), Vector128.Create((sbyte)7)).AsByte();
		Vector128<byte> vector3 = Vector128.ConditionalSelect(condition, left, right);
		Vector128<byte> result = Vector128.Equals(vector3 & vector2, vector2);
		return TNegator.NegateIfNeeded(result);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[CompExactlyDependsOn(typeof(Avx2))]
	private static Vector256<byte> IndexOfAnyLookup<TNegator>(Vector256<byte> source, Vector256<byte> bitmapLookup0, Vector256<byte> bitmapLookup1) where TNegator : struct, INegator
	{
		Vector256<byte> mask = source & Vector256.Create((byte)15);
		Vector256<byte> vector = Vector256.ShiftRightLogical(source.AsInt32(), 4).AsByte() & Vector256.Create((byte)15);
		Vector256<byte> right = Avx2.Shuffle(bitmapLookup0, mask);
		Vector256<byte> left = Avx2.Shuffle(bitmapLookup1, mask);
		Vector256<byte> vector2 = Avx2.Shuffle(Vector256.Create(9241421688590303745uL).AsByte(), vector);
		Vector256<byte> condition = Vector256.GreaterThan(vector.AsSByte(), Vector256.Create((sbyte)7)).AsByte();
		Vector256<byte> vector3 = Vector256.ConditionalSelect(condition, left, right);
		Vector256<byte> result = Vector256.Equals(vector3 & vector2, vector2);
		return TNegator.NegateIfNeeded(result);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static int ComputeFirstIndex<T, TNegator>(ref T searchSpace, ref T current, Vector128<byte> result) where TNegator : struct, INegator
	{
		uint value = TNegator.ExtractMask(result);
		int num = BitOperations.TrailingZeroCount(value);
		return num + (int)((nuint)Unsafe.ByteOffset(ref searchSpace, ref current) / (nuint)Unsafe.SizeOf<T>());
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static int ComputeFirstIndexOverlapped<T, TNegator>(ref T searchSpace, ref T current0, ref T current1, Vector128<byte> result) where TNegator : struct, INegator
	{
		uint value = TNegator.ExtractMask(result);
		int num = BitOperations.TrailingZeroCount(value);
		if (num >= Vector128<short>.Count)
		{
			current0 = ref current1;
			num -= Vector128<short>.Count;
		}
		return num + (int)((nuint)Unsafe.ByteOffset(ref searchSpace, ref current0) / (nuint)Unsafe.SizeOf<T>());
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static int ComputeLastIndex<T, TNegator>(ref T searchSpace, ref T current, Vector128<byte> result) where TNegator : struct, INegator
	{
		uint value = TNegator.ExtractMask(result) & 0xFFFFu;
		int num = 31 - BitOperations.LeadingZeroCount(value);
		return num + (int)((nuint)Unsafe.ByteOffset(ref searchSpace, ref current) / (nuint)Unsafe.SizeOf<T>());
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static int ComputeLastIndexOverlapped<T, TNegator>(ref T searchSpace, ref T secondVector, Vector128<byte> result) where TNegator : struct, INegator
	{
		uint value = TNegator.ExtractMask(result) & 0xFFFFu;
		int num = 31 - BitOperations.LeadingZeroCount(value);
		if (num < Vector128<short>.Count)
		{
			return num;
		}
		return num - Vector128<short>.Count + (int)((nuint)Unsafe.ByteOffset(ref searchSpace, ref secondVector) / (nuint)Unsafe.SizeOf<T>());
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[CompExactlyDependsOn(typeof(Avx2))]
	private static int ComputeFirstIndex<T, TNegator>(ref T searchSpace, ref T current, Vector256<byte> result) where TNegator : struct, INegator
	{
		if (typeof(T) == typeof(short))
		{
			result = FixUpPackedVector256Result(result);
		}
		uint value = TNegator.ExtractMask(result);
		int num = BitOperations.TrailingZeroCount(value);
		return num + (int)((nuint)Unsafe.ByteOffset(ref searchSpace, ref current) / (nuint)Unsafe.SizeOf<T>());
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[CompExactlyDependsOn(typeof(Avx2))]
	private static int ComputeFirstIndexOverlapped<T, TNegator>(ref T searchSpace, ref T current0, ref T current1, Vector256<byte> result) where TNegator : struct, INegator
	{
		if (typeof(T) == typeof(short))
		{
			result = FixUpPackedVector256Result(result);
		}
		uint value = TNegator.ExtractMask(result);
		int num = BitOperations.TrailingZeroCount(value);
		if (num >= Vector256<short>.Count)
		{
			current0 = ref current1;
			num -= Vector256<short>.Count;
		}
		return num + (int)((nuint)Unsafe.ByteOffset(ref searchSpace, ref current0) / (nuint)Unsafe.SizeOf<T>());
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[CompExactlyDependsOn(typeof(Avx2))]
	private static int ComputeLastIndex<T, TNegator>(ref T searchSpace, ref T current, Vector256<byte> result) where TNegator : struct, INegator
	{
		if (typeof(T) == typeof(short))
		{
			result = FixUpPackedVector256Result(result);
		}
		uint value = TNegator.ExtractMask(result);
		int num = 31 - BitOperations.LeadingZeroCount(value);
		return num + (int)((nuint)Unsafe.ByteOffset(ref searchSpace, ref current) / (nuint)Unsafe.SizeOf<T>());
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[CompExactlyDependsOn(typeof(Avx2))]
	private static int ComputeLastIndexOverlapped<T, TNegator>(ref T searchSpace, ref T secondVector, Vector256<byte> result) where TNegator : struct, INegator
	{
		if (typeof(T) == typeof(short))
		{
			result = FixUpPackedVector256Result(result);
		}
		uint value = TNegator.ExtractMask(result);
		int num = 31 - BitOperations.LeadingZeroCount(value);
		if (num < Vector256<short>.Count)
		{
			return num;
		}
		return num - Vector256<short>.Count + (int)((nuint)Unsafe.ByteOffset(ref searchSpace, ref secondVector) / (nuint)Unsafe.SizeOf<T>());
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[CompExactlyDependsOn(typeof(Avx2))]
	private static Vector256<byte> FixUpPackedVector256Result(Vector256<byte> result)
	{
		return Avx2.Permute4x64(result.AsInt64(), 216).AsByte();
	}
}
