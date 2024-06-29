using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.Wasm;
using System.Runtime.Intrinsics.X86;

namespace System.Buffers;

internal sealed class ProbabilisticWithAsciiCharSearchValues<TOptimizations> : SearchValues<char> where TOptimizations : struct, IndexOfAnyAsciiSearcher.IOptimizations
{
	private Vector256<byte> _asciiBitmap;

	private Vector256<byte> _inverseAsciiBitmap;

	private ProbabilisticMap _map;

	private readonly string _values;

	public ProbabilisticWithAsciiCharSearchValues(scoped ReadOnlySpan<char> values)
	{
		IndexOfAnyAsciiSearcher.ComputeBitmap(values, out _asciiBitmap, out var _);
		_inverseAsciiBitmap = ~_asciiBitmap;
		_values = new string(values);
		_map = new ProbabilisticMap(_values);
	}

	internal override char[] GetValues()
	{
		return _values.ToCharArray();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal override bool ContainsCore(char value)
	{
		return ProbabilisticMap.Contains(ref Unsafe.As<ProbabilisticMap, uint>(ref _map), _values, value);
	}

	internal override int IndexOfAny(ReadOnlySpan<char> span)
	{
		int num = 0;
		if (IndexOfAnyAsciiSearcher.IsVectorizationSupported && span.Length >= Vector128<short>.Count && char.IsAscii(span[0]))
		{
			num = (((!Ssse3.IsSupported && !PackedSimd.IsSupported) || !(typeof(TOptimizations) == typeof(IndexOfAnyAsciiSearcher.Default))) ? IndexOfAnyAsciiSearcher.IndexOfAnyVectorized<IndexOfAnyAsciiSearcher.Negate, IndexOfAnyAsciiSearcher.Default>(ref Unsafe.As<char, short>(ref MemoryMarshal.GetReference(span)), span.Length, ref _inverseAsciiBitmap) : IndexOfAnyAsciiSearcher.IndexOfAnyVectorized<IndexOfAnyAsciiSearcher.Negate, IndexOfAnyAsciiSearcher.Ssse3AndWasmHandleZeroInNeedle>(ref Unsafe.As<char, short>(ref MemoryMarshal.GetReference(span)), span.Length, ref _inverseAsciiBitmap));
			if ((uint)num >= (uint)span.Length || char.IsAscii(span[num]))
			{
				return num;
			}
			span = span.Slice(num);
		}
		int num2 = ProbabilisticMap.IndexOfAny(ref Unsafe.As<ProbabilisticMap, uint>(ref _map), ref MemoryMarshal.GetReference(span), span.Length, _values);
		if (num2 >= 0)
		{
			num2 += num;
		}
		return num2;
	}

	internal override int IndexOfAnyExcept(ReadOnlySpan<char> span)
	{
		int num = 0;
		if (IndexOfAnyAsciiSearcher.IsVectorizationSupported && span.Length >= Vector128<short>.Count && char.IsAscii(span[0]))
		{
			num = IndexOfAnyAsciiSearcher.IndexOfAnyVectorized<IndexOfAnyAsciiSearcher.Negate, TOptimizations>(ref Unsafe.As<char, short>(ref MemoryMarshal.GetReference(span)), span.Length, ref _asciiBitmap);
			if ((uint)num >= (uint)span.Length || char.IsAscii(span[num]))
			{
				return num;
			}
			span = span.Slice(num);
		}
		int num2 = ProbabilisticMap.IndexOfAnySimpleLoop<IndexOfAnyAsciiSearcher.Negate>(ref MemoryMarshal.GetReference(span), span.Length, _values);
		if (num2 >= 0)
		{
			num2 += num;
		}
		return num2;
	}

	internal override int LastIndexOfAny(ReadOnlySpan<char> span)
	{
		if (IndexOfAnyAsciiSearcher.IsVectorizationSupported && span.Length >= Vector128<short>.Count)
		{
			if (char.IsAscii(span[span.Length - 1]))
			{
				int num = (((!Ssse3.IsSupported && !PackedSimd.IsSupported) || !(typeof(TOptimizations) == typeof(IndexOfAnyAsciiSearcher.Default))) ? IndexOfAnyAsciiSearcher.LastIndexOfAnyVectorized<IndexOfAnyAsciiSearcher.Negate, IndexOfAnyAsciiSearcher.Default>(ref Unsafe.As<char, short>(ref MemoryMarshal.GetReference(span)), span.Length, ref _inverseAsciiBitmap) : IndexOfAnyAsciiSearcher.LastIndexOfAnyVectorized<IndexOfAnyAsciiSearcher.Negate, IndexOfAnyAsciiSearcher.Ssse3AndWasmHandleZeroInNeedle>(ref Unsafe.As<char, short>(ref MemoryMarshal.GetReference(span)), span.Length, ref _inverseAsciiBitmap));
				if ((uint)num >= (uint)span.Length || char.IsAscii(span[num]))
				{
					return num;
				}
				span = span.Slice(0, num + 1);
			}
		}
		return ProbabilisticMap.LastIndexOfAny(ref Unsafe.As<ProbabilisticMap, uint>(ref _map), ref MemoryMarshal.GetReference(span), span.Length, _values);
	}

	internal override int LastIndexOfAnyExcept(ReadOnlySpan<char> span)
	{
		if (IndexOfAnyAsciiSearcher.IsVectorizationSupported && span.Length >= Vector128<short>.Count)
		{
			if (char.IsAscii(span[span.Length - 1]))
			{
				int num = IndexOfAnyAsciiSearcher.LastIndexOfAnyVectorized<IndexOfAnyAsciiSearcher.Negate, TOptimizations>(ref Unsafe.As<char, short>(ref MemoryMarshal.GetReference(span)), span.Length, ref _asciiBitmap);
				if ((uint)num >= (uint)span.Length || char.IsAscii(span[num]))
				{
					return num;
				}
				span = span.Slice(0, num + 1);
			}
		}
		return ProbabilisticMap.LastIndexOfAnySimpleLoop<IndexOfAnyAsciiSearcher.Negate>(ref MemoryMarshal.GetReference(span), span.Length, _values);
	}
}
