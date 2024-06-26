using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System.Buffers;

internal sealed class Latin1CharSearchValues : SearchValues<char>
{
	private readonly BitVector256 _lookup;

	public Latin1CharSearchValues(ReadOnlySpan<char> values)
	{
		ReadOnlySpan<char> readOnlySpan = values;
		for (int i = 0; i < readOnlySpan.Length; i++)
		{
			char c = readOnlySpan[i];
			if (c > 'ÿ')
			{
				ThrowHelper.ThrowInvalidOperationException_InvalidOperation_EnumFailedVersion();
			}
			_lookup.Set(c);
		}
	}

	internal override char[] GetValues()
	{
		return _lookup.GetCharValues();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal override bool ContainsCore(char value)
	{
		return _lookup.Contains256(value);
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

	private int IndexOfAny<TNegator>(ref char searchSpace, int searchSpaceLength) where TNegator : struct, IndexOfAnyAsciiSearcher.INegator
	{
		ref char right = ref Unsafe.Add(ref searchSpace, searchSpaceLength);
		ref char reference = ref searchSpace;
		while (!Unsafe.AreSame(ref reference, ref right))
		{
			char c = reference;
			if (TNegator.NegateIfNeeded(_lookup.Contains256(c)))
			{
				return (int)((nuint)Unsafe.ByteOffset(ref searchSpace, ref reference) / (nuint)2u);
			}
			reference = ref Unsafe.Add(ref reference, 1);
		}
		return -1;
	}

	private int LastIndexOfAny<TNegator>(ref char searchSpace, int searchSpaceLength) where TNegator : struct, IndexOfAnyAsciiSearcher.INegator
	{
		for (int num = searchSpaceLength - 1; num >= 0; num--)
		{
			char c = Unsafe.Add(ref searchSpace, num);
			if (TNegator.NegateIfNeeded(_lookup.Contains256(c)))
			{
				return num;
			}
		}
		return -1;
	}
}
