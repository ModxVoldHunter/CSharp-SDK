using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;

namespace System.Buffers;

internal sealed class AsciiCharSearchValues<TOptimizations> : SearchValues<char> where TOptimizations : struct, IndexOfAnyAsciiSearcher.IOptimizations
{
	private Vector256<byte> _bitmap;

	private readonly BitVector256 _lookup;

	public AsciiCharSearchValues(Vector256<byte> bitmap, BitVector256 lookup)
	{
		_bitmap = bitmap;
		_lookup = lookup;
	}

	internal override char[] GetValues()
	{
		return _lookup.GetCharValues();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal override bool ContainsCore(char value)
	{
		return _lookup.Contains128(value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal override int IndexOfAny(ReadOnlySpan<char> span)
	{
		return IndexOfAny<IndexOfAnyAsciiSearcher.DontNegate>(ref MemoryMarshal.GetReference(span), span.Length);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal override int IndexOfAnyExcept(ReadOnlySpan<char> span)
	{
		return IndexOfAny<IndexOfAnyAsciiSearcher.Negate>(ref MemoryMarshal.GetReference(span), span.Length);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal override int LastIndexOfAny(ReadOnlySpan<char> span)
	{
		return LastIndexOfAny<IndexOfAnyAsciiSearcher.DontNegate>(ref MemoryMarshal.GetReference(span), span.Length);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal override int LastIndexOfAnyExcept(ReadOnlySpan<char> span)
	{
		return LastIndexOfAny<IndexOfAnyAsciiSearcher.Negate>(ref MemoryMarshal.GetReference(span), span.Length);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private int IndexOfAny<TNegator>(ref char searchSpace, int searchSpaceLength) where TNegator : struct, IndexOfAnyAsciiSearcher.INegator
	{
		if (!IndexOfAnyAsciiSearcher.IsVectorizationSupported || searchSpaceLength < Vector128<short>.Count)
		{
			return IndexOfAnyScalar<TNegator>(ref searchSpace, searchSpaceLength);
		}
		return IndexOfAnyAsciiSearcher.IndexOfAnyVectorized<TNegator, TOptimizations>(ref Unsafe.As<char, short>(ref searchSpace), searchSpaceLength, ref _bitmap);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private int LastIndexOfAny<TNegator>(ref char searchSpace, int searchSpaceLength) where TNegator : struct, IndexOfAnyAsciiSearcher.INegator
	{
		if (!IndexOfAnyAsciiSearcher.IsVectorizationSupported || searchSpaceLength < Vector128<short>.Count)
		{
			return LastIndexOfAnyScalar<TNegator>(ref searchSpace, searchSpaceLength);
		}
		return IndexOfAnyAsciiSearcher.LastIndexOfAnyVectorized<TNegator, TOptimizations>(ref Unsafe.As<char, short>(ref searchSpace), searchSpaceLength, ref _bitmap);
	}

	private int IndexOfAnyScalar<TNegator>(ref char searchSpace, int searchSpaceLength) where TNegator : struct, IndexOfAnyAsciiSearcher.INegator
	{
		ref char right = ref Unsafe.Add(ref searchSpace, searchSpaceLength);
		ref char reference = ref searchSpace;
		while (!Unsafe.AreSame(ref reference, ref right))
		{
			char c = reference;
			if (TNegator.NegateIfNeeded(_lookup.Contains128(c)))
			{
				return (int)((nuint)Unsafe.ByteOffset(ref searchSpace, ref reference) / (nuint)2u);
			}
			reference = ref Unsafe.Add(ref reference, 1);
		}
		return -1;
	}

	private int LastIndexOfAnyScalar<TNegator>(ref char searchSpace, int searchSpaceLength) where TNegator : struct, IndexOfAnyAsciiSearcher.INegator
	{
		for (int num = searchSpaceLength - 1; num >= 0; num--)
		{
			char c = Unsafe.Add(ref searchSpace, num);
			if (TNegator.NegateIfNeeded(_lookup.Contains128(c)))
			{
				return num;
			}
		}
		return -1;
	}
}
