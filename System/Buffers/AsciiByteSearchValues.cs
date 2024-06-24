using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;

namespace System.Buffers;

internal sealed class AsciiByteSearchValues : SearchValues<byte>
{
	private Vector256<byte> _bitmap;

	private readonly BitVector256 _lookup;

	public AsciiByteSearchValues(ReadOnlySpan<byte> values)
	{
		IndexOfAnyAsciiSearcher.ComputeBitmap(values, out _bitmap, out _lookup);
	}

	internal override byte[] GetValues()
	{
		return _lookup.GetByteValues();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal override bool ContainsCore(byte value)
	{
		return _lookup.Contains(value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal override int IndexOfAny(ReadOnlySpan<byte> span)
	{
		return IndexOfAny<IndexOfAnyAsciiSearcher.DontNegate>(ref MemoryMarshal.GetReference(span), span.Length);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal override int IndexOfAnyExcept(ReadOnlySpan<byte> span)
	{
		return IndexOfAny<IndexOfAnyAsciiSearcher.Negate>(ref MemoryMarshal.GetReference(span), span.Length);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal override int LastIndexOfAny(ReadOnlySpan<byte> span)
	{
		return LastIndexOfAny<IndexOfAnyAsciiSearcher.DontNegate>(ref MemoryMarshal.GetReference(span), span.Length);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal override int LastIndexOfAnyExcept(ReadOnlySpan<byte> span)
	{
		return LastIndexOfAny<IndexOfAnyAsciiSearcher.Negate>(ref MemoryMarshal.GetReference(span), span.Length);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private int IndexOfAny<TNegator>(ref byte searchSpace, int searchSpaceLength) where TNegator : struct, IndexOfAnyAsciiSearcher.INegator
	{
		if (!IndexOfAnyAsciiSearcher.IsVectorizationSupported || searchSpaceLength < 8)
		{
			return IndexOfAnyScalar<TNegator>(ref searchSpace, searchSpaceLength);
		}
		return IndexOfAnyAsciiSearcher.IndexOfAnyVectorized<TNegator>(ref searchSpace, searchSpaceLength, ref _bitmap);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private int LastIndexOfAny<TNegator>(ref byte searchSpace, int searchSpaceLength) where TNegator : struct, IndexOfAnyAsciiSearcher.INegator
	{
		if (!IndexOfAnyAsciiSearcher.IsVectorizationSupported || searchSpaceLength < 8)
		{
			return LastIndexOfAnyScalar<TNegator>(ref searchSpace, searchSpaceLength);
		}
		return IndexOfAnyAsciiSearcher.LastIndexOfAnyVectorized<TNegator>(ref searchSpace, searchSpaceLength, ref _bitmap);
	}

	private int IndexOfAnyScalar<TNegator>(ref byte searchSpace, int searchSpaceLength) where TNegator : struct, IndexOfAnyAsciiSearcher.INegator
	{
		ref byte right = ref Unsafe.Add(ref searchSpace, searchSpaceLength);
		ref byte reference = ref searchSpace;
		while (!Unsafe.AreSame(ref reference, ref right))
		{
			byte b = reference;
			if (TNegator.NegateIfNeeded(_lookup.Contains(b)))
			{
				return (int)Unsafe.ByteOffset(ref searchSpace, ref reference);
			}
			reference = ref Unsafe.Add(ref reference, 1);
		}
		return -1;
	}

	private int LastIndexOfAnyScalar<TNegator>(ref byte searchSpace, int searchSpaceLength) where TNegator : struct, IndexOfAnyAsciiSearcher.INegator
	{
		for (int num = searchSpaceLength - 1; num >= 0; num--)
		{
			byte b = Unsafe.Add(ref searchSpace, num);
			if (TNegator.NegateIfNeeded(_lookup.Contains(b)))
			{
				return num;
			}
		}
		return -1;
	}
}
