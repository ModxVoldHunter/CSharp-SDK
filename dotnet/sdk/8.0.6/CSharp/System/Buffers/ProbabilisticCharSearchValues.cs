using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System.Buffers;

internal sealed class ProbabilisticCharSearchValues : SearchValues<char>
{
	private ProbabilisticMap _map;

	private readonly string _values;

	public ProbabilisticCharSearchValues(scoped ReadOnlySpan<char> values)
	{
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
		return ProbabilisticMap.IndexOfAny(ref Unsafe.As<ProbabilisticMap, uint>(ref _map), ref MemoryMarshal.GetReference(span), span.Length, _values);
	}

	internal override int IndexOfAnyExcept(ReadOnlySpan<char> span)
	{
		return ProbabilisticMap.IndexOfAnySimpleLoop<IndexOfAnyAsciiSearcher.Negate>(ref MemoryMarshal.GetReference(span), span.Length, _values);
	}

	internal override int LastIndexOfAny(ReadOnlySpan<char> span)
	{
		return ProbabilisticMap.LastIndexOfAny(ref Unsafe.As<ProbabilisticMap, uint>(ref _map), ref MemoryMarshal.GetReference(span), span.Length, _values);
	}

	internal override int LastIndexOfAnyExcept(ReadOnlySpan<char> span)
	{
		return ProbabilisticMap.LastIndexOfAnySimpleLoop<IndexOfAnyAsciiSearcher.Negate>(ref MemoryMarshal.GetReference(span), span.Length, _values);
	}
}
