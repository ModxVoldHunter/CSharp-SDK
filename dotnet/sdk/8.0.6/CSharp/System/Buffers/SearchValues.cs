using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.Wasm;
using System.Runtime.Intrinsics.X86;

namespace System.Buffers;

public static class SearchValues
{
	internal interface IRuntimeConst
	{
		static abstract bool Value { get; }
	}

	[StructLayout(LayoutKind.Sequential, Size = 1)]
	private readonly struct TrueConst : IRuntimeConst
	{
		public static bool Value => true;
	}

	[StructLayout(LayoutKind.Sequential, Size = 1)]
	private readonly struct FalseConst : IRuntimeConst
	{
		public static bool Value => false;
	}

	public static SearchValues<byte> Create(ReadOnlySpan<byte> values)
	{
		if (values.IsEmpty)
		{
			return new EmptySearchValues<byte>();
		}
		if (values.Length == 1)
		{
			return new SingleByteSearchValues(values);
		}
		if (TryGetSingleRange(values, out var minInclusive, out var maxInclusive))
		{
			return new RangeByteSearchValues(minInclusive, maxInclusive);
		}
		if (values.Length <= 5)
		{
			return values.Length switch
			{
				2 => new Any2ByteSearchValues(values), 
				3 => new Any3ByteSearchValues(values), 
				4 => new Any4SearchValues<byte, byte>(values), 
				_ => new Any5SearchValues<byte, byte>(values), 
			};
		}
		if (IndexOfAnyAsciiSearcher.IsVectorizationSupported && maxInclusive < 128)
		{
			return new AsciiByteSearchValues(values);
		}
		return new AnyByteSearchValues(values);
	}

	public static SearchValues<char> Create(ReadOnlySpan<char> values)
	{
		if (values.IsEmpty)
		{
			return new EmptySearchValues<char>();
		}
		if (values.Length == 1)
		{
			char value = values[0];
			if (!PackedSpanHelpers.PackedIndexOfIsSupported || !PackedSpanHelpers.CanUsePackedIndexOf(value))
			{
				return new SingleCharSearchValues<FalseConst>(value);
			}
			return new SingleCharSearchValues<TrueConst>(value);
		}
		if (TryGetSingleRange(values, out var minInclusive, out var maxInclusive))
		{
			if (!PackedSpanHelpers.PackedIndexOfIsSupported || !PackedSpanHelpers.CanUsePackedIndexOf(minInclusive) || !PackedSpanHelpers.CanUsePackedIndexOf(maxInclusive))
			{
				return new RangeCharSearchValues<FalseConst>(minInclusive, maxInclusive);
			}
			return new RangeCharSearchValues<TrueConst>(minInclusive, maxInclusive);
		}
		if (values.Length == 2)
		{
			char c = values[0];
			char c2 = values[1];
			if (!PackedSpanHelpers.PackedIndexOfIsSupported || !PackedSpanHelpers.CanUsePackedIndexOf(c) || !PackedSpanHelpers.CanUsePackedIndexOf(c2))
			{
				return new Any2CharSearchValues<FalseConst>(c, c2);
			}
			return new Any2CharSearchValues<TrueConst>(c, c2);
		}
		if (values.Length == 3)
		{
			char c3 = values[0];
			char c4 = values[1];
			char c5 = values[2];
			if (!PackedSpanHelpers.PackedIndexOfIsSupported || !PackedSpanHelpers.CanUsePackedIndexOf(c3) || !PackedSpanHelpers.CanUsePackedIndexOf(c4) || !PackedSpanHelpers.CanUsePackedIndexOf(c5))
			{
				return new Any3CharSearchValues<FalseConst>(c3, c4, c5);
			}
			return new Any3CharSearchValues<TrueConst>(c3, c4, c5);
		}
		if (IndexOfAnyAsciiSearcher.IsVectorizationSupported && maxInclusive < '\u0080')
		{
			IndexOfAnyAsciiSearcher.ComputeBitmap(values, out var bitmap, out var lookup);
			if ((!Ssse3.IsSupported && !PackedSimd.IsSupported) || !lookup.Contains(0))
			{
				return new AsciiCharSearchValues<IndexOfAnyAsciiSearcher.Default>(bitmap, lookup);
			}
			return new AsciiCharSearchValues<IndexOfAnyAsciiSearcher.Ssse3AndWasmHandleZeroInNeedle>(bitmap, lookup);
		}
		ReadOnlySpan<short> values2 = MemoryMarshal.CreateReadOnlySpan(ref Unsafe.As<char, short>(ref MemoryMarshal.GetReference(values)), values.Length);
		if (values.Length == 4)
		{
			return new Any4SearchValues<char, short>(values2);
		}
		if (values.Length == 5)
		{
			return new Any5SearchValues<char, short>(values2);
		}
		ReadOnlySpan<char> readOnlySpan = values;
		if (Vector128.IsHardwareAccelerated && values.Length < 8)
		{
			Span<char> span = stackalloc char[8];
			span.Fill(values[0]);
			values.CopyTo(span);
			readOnlySpan = span;
		}
		if (IndexOfAnyAsciiSearcher.IsVectorizationSupported && minInclusive < '\u0080')
		{
			if ((!Ssse3.IsSupported && !PackedSimd.IsSupported) || !readOnlySpan.Contains('\0'))
			{
				return new ProbabilisticWithAsciiCharSearchValues<IndexOfAnyAsciiSearcher.Default>(readOnlySpan);
			}
			return new ProbabilisticWithAsciiCharSearchValues<IndexOfAnyAsciiSearcher.Ssse3AndWasmHandleZeroInNeedle>(readOnlySpan);
		}
		if (!Sse41.IsSupported)
		{
			_ = 0;
			if (maxInclusive < 'Ā')
			{
				return new Latin1CharSearchValues(values);
			}
		}
		return new ProbabilisticCharSearchValues(readOnlySpan);
	}

	private static bool TryGetSingleRange<T>(ReadOnlySpan<T> values, out T minInclusive, out T maxInclusive) where T : struct, INumber<T>, IMinMaxValue<T>
	{
		T val = T.MaxValue;
		T val2 = T.MinValue;
		ReadOnlySpan<T> readOnlySpan = values;
		for (int i = 0; i < readOnlySpan.Length; i++)
		{
			T y = readOnlySpan[i];
			val = T.Min(val, y);
			val2 = T.Max(val2, y);
		}
		minInclusive = val;
		maxInclusive = val2;
		uint num = uint.CreateChecked(val2 - val) + 1;
		if (num > values.Length)
		{
			return false;
		}
		Span<bool> span = ((num > 256) ? ((Span<bool>)new bool[num]) : stackalloc bool[256]);
		Span<bool> span2 = span;
		span2 = span2.Slice(0, (int)num);
		span2.Clear();
		ReadOnlySpan<T> readOnlySpan2 = values;
		for (int j = 0; j < readOnlySpan2.Length; j++)
		{
			T val3 = readOnlySpan2[j];
			int index = int.CreateChecked(val3 - val);
			span2[index] = true;
		}
		if (span2.Contains(value: false))
		{
			return false;
		}
		return true;
	}
}
[DebuggerDisplay("{DebuggerDisplay,nq}")]
[DebuggerTypeProxy(typeof(SearchValuesDebugView<>))]
public class SearchValues<T> where T : IEquatable<T>?
{
	private string DebuggerDisplay
	{
		get
		{
			T[] source = GetValues();
			string text = $"{GetType().Name}, Count = {source.Length}";
			if (source.Length != 0)
			{
				text += ", Values = ";
				text += ((typeof(T) == typeof(char)) ? ("\"" + new string(Unsafe.As<T[], char[]>(ref source)) + "\"") : string.Join(",", source));
			}
			return text;
		}
	}

	private protected SearchValues()
	{
	}

	internal virtual T[] GetValues()
	{
		throw new UnreachableException();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool Contains(T value)
	{
		return ContainsCore(value);
	}

	internal virtual bool ContainsCore(T value)
	{
		throw new UnreachableException();
	}

	internal virtual int IndexOfAny(ReadOnlySpan<T> span)
	{
		throw new UnreachableException();
	}

	internal virtual int IndexOfAnyExcept(ReadOnlySpan<T> span)
	{
		throw new UnreachableException();
	}

	internal virtual int LastIndexOfAny(ReadOnlySpan<T> span)
	{
		throw new UnreachableException();
	}

	internal virtual int LastIndexOfAnyExcept(ReadOnlySpan<T> span)
	{
		throw new UnreachableException();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static int IndexOfAny(ReadOnlySpan<T> span, SearchValues<T> values)
	{
		if (values == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.values);
		}
		return values.IndexOfAny(span);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static int IndexOfAnyExcept(ReadOnlySpan<T> span, SearchValues<T> values)
	{
		if (values == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.values);
		}
		return values.IndexOfAnyExcept(span);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static int LastIndexOfAny(ReadOnlySpan<T> span, SearchValues<T> values)
	{
		if (values == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.values);
		}
		return values.LastIndexOfAny(span);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static int LastIndexOfAnyExcept(ReadOnlySpan<T> span, SearchValues<T> values)
	{
		if (values == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.values);
		}
		return values.LastIndexOfAnyExcept(span);
	}
}
