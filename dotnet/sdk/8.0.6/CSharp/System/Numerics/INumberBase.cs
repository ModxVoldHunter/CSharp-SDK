using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Unicode;

namespace System.Numerics;

public interface INumberBase<TSelf> : IAdditionOperators<TSelf, TSelf, TSelf>, IAdditiveIdentity<TSelf, TSelf>, IDecrementOperators<TSelf>, IDivisionOperators<TSelf, TSelf, TSelf>, IEquatable<TSelf>, IEqualityOperators<TSelf, TSelf, bool>, IIncrementOperators<TSelf>, IMultiplicativeIdentity<TSelf, TSelf>, IMultiplyOperators<TSelf, TSelf, TSelf>, ISpanFormattable, IFormattable, ISpanParsable<TSelf>, IParsable<TSelf>, ISubtractionOperators<TSelf, TSelf, TSelf>, IUnaryPlusOperators<TSelf, TSelf>, IUnaryNegationOperators<TSelf, TSelf>, IUtf8SpanFormattable, IUtf8SpanParsable<TSelf> where TSelf : INumberBase<TSelf>?
{
	static abstract TSelf One { get; }

	static abstract int Radix { get; }

	static abstract TSelf Zero { get; }

	static abstract TSelf Abs(TSelf value);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	static virtual TSelf CreateChecked<TOther>(TOther value) where TOther : INumberBase<TOther>
	{
		if (typeof(TOther) == typeof(TSelf))
		{
			return (TSelf)(object)value;
		}
		if (!TryConvertFromChecked(value, out var result) && !TOther.TryConvertToChecked(value, out result))
		{
			ThrowHelper.ThrowNotSupportedException();
		}
		return result;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	static virtual TSelf CreateSaturating<TOther>(TOther value) where TOther : INumberBase<TOther>
	{
		if (typeof(TOther) == typeof(TSelf))
		{
			return (TSelf)(object)value;
		}
		if (!TryConvertFromSaturating(value, out var result) && !TOther.TryConvertToSaturating(value, out result))
		{
			ThrowHelper.ThrowNotSupportedException();
		}
		return result;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	static virtual TSelf CreateTruncating<TOther>(TOther value) where TOther : INumberBase<TOther>
	{
		if (typeof(TOther) == typeof(TSelf))
		{
			return (TSelf)(object)value;
		}
		if (!TryConvertFromTruncating(value, out var result) && !TOther.TryConvertToTruncating(value, out result))
		{
			ThrowHelper.ThrowNotSupportedException();
		}
		return result;
	}

	static abstract bool IsCanonical(TSelf value);

	static abstract bool IsComplexNumber(TSelf value);

	static abstract bool IsEvenInteger(TSelf value);

	static abstract bool IsFinite(TSelf value);

	static abstract bool IsImaginaryNumber(TSelf value);

	static abstract bool IsInfinity(TSelf value);

	static abstract bool IsInteger(TSelf value);

	static abstract bool IsNaN(TSelf value);

	static abstract bool IsNegative(TSelf value);

	static abstract bool IsNegativeInfinity(TSelf value);

	static abstract bool IsNormal(TSelf value);

	static abstract bool IsOddInteger(TSelf value);

	static abstract bool IsPositive(TSelf value);

	static abstract bool IsPositiveInfinity(TSelf value);

	static abstract bool IsRealNumber(TSelf value);

	static abstract bool IsSubnormal(TSelf value);

	static abstract bool IsZero(TSelf value);

	static abstract TSelf MaxMagnitude(TSelf x, TSelf y);

	static abstract TSelf MaxMagnitudeNumber(TSelf x, TSelf y);

	static abstract TSelf MinMagnitude(TSelf x, TSelf y);

	static abstract TSelf MinMagnitudeNumber(TSelf x, TSelf y);

	static abstract TSelf Parse(string s, NumberStyles style, IFormatProvider? provider);

	static abstract TSelf Parse(ReadOnlySpan<char> s, NumberStyles style, IFormatProvider? provider);

	static virtual TSelf Parse(ReadOnlySpan<byte> utf8Text, NumberStyles style, IFormatProvider? provider)
	{
		int maxCharCount = Encoding.UTF8.GetMaxCharCount(utf8Text.Length);
		char[] array;
		Span<char> span;
		if (maxCharCount < 256)
		{
			array = null;
			span = stackalloc char[256];
		}
		else
		{
			array = ArrayPool<char>.Shared.Rent(maxCharCount);
			span = array.AsSpan(0, maxCharCount);
		}
		if (Utf8.ToUtf16(utf8Text, span, out var _, out var charsWritten, replaceInvalidSequences: false) != 0)
		{
			ThrowHelper.ThrowFormatInvalidString();
		}
		span = span.Slice(0, charsWritten);
		TSelf result = Parse(span, style, provider);
		if (array != null)
		{
			ArrayPool<char>.Shared.Return(array);
		}
		return result;
	}

	protected static abstract bool TryConvertFromChecked<TOther>(TOther value, [MaybeNullWhen(false)] out TSelf result) where TOther : INumberBase<TOther>;

	protected static abstract bool TryConvertFromSaturating<TOther>(TOther value, [MaybeNullWhen(false)] out TSelf result) where TOther : INumberBase<TOther>;

	protected static abstract bool TryConvertFromTruncating<TOther>(TOther value, [MaybeNullWhen(false)] out TSelf result) where TOther : INumberBase<TOther>;

	protected static abstract bool TryConvertToChecked<TOther>(TSelf value, [MaybeNullWhen(false)] out TOther result) where TOther : INumberBase<TOther>;

	protected static abstract bool TryConvertToSaturating<TOther>(TSelf value, [MaybeNullWhen(false)] out TOther result) where TOther : INumberBase<TOther>;

	protected static abstract bool TryConvertToTruncating<TOther>(TSelf value, [MaybeNullWhen(false)] out TOther result) where TOther : INumberBase<TOther>;

	static abstract bool TryParse([NotNullWhen(true)] string? s, NumberStyles style, IFormatProvider? provider, [MaybeNullWhen(false)] out TSelf result);

	static abstract bool TryParse(ReadOnlySpan<char> s, NumberStyles style, IFormatProvider? provider, [MaybeNullWhen(false)] out TSelf result);

	static virtual bool TryParse(ReadOnlySpan<byte> utf8Text, NumberStyles style, IFormatProvider? provider, [MaybeNullWhen(false)] out TSelf result)
	{
		int maxCharCount = Encoding.UTF8.GetMaxCharCount(utf8Text.Length);
		char[] array;
		Span<char> span;
		if (maxCharCount < 256)
		{
			array = null;
			span = stackalloc char[256];
		}
		else
		{
			array = ArrayPool<char>.Shared.Rent(maxCharCount);
			span = array.AsSpan(0, maxCharCount);
		}
		if (Utf8.ToUtf16(utf8Text, span, out var _, out var charsWritten, replaceInvalidSequences: false) != 0)
		{
			result = default(TSelf);
			return false;
		}
		span = span.Slice(0, charsWritten);
		bool result2 = TryParse(span, style, provider, out result);
		if (array != null)
		{
			ArrayPool<char>.Shared.Return(array);
		}
		return result2;
	}

	bool IUtf8SpanFormattable.TryFormat(Span<byte> utf8Destination, out int bytesWritten, ReadOnlySpan<char> format, IFormatProvider provider)
	{
		int maxCharCount = Encoding.UTF8.GetMaxCharCount(utf8Destination.Length);
		char[] array;
		Span<char> span;
		if (maxCharCount < 256)
		{
			array = null;
			span = stackalloc char[256];
		}
		else
		{
			array = ArrayPool<char>.Shared.Rent(maxCharCount);
			span = array.AsSpan(0, maxCharCount);
		}
		if (!TryFormat(span, out var charsWritten, format, provider))
		{
			if (array != null)
			{
				ArrayPool<char>.Shared.Return(array);
			}
			bytesWritten = 0;
			return false;
		}
		span = span.Slice(0, charsWritten);
		int charsRead;
		OperationStatus operationStatus = Utf8.FromUtf16(span, utf8Destination, out charsRead, out bytesWritten, replaceInvalidSequences: false);
		if (array != null)
		{
			ArrayPool<char>.Shared.Return(array);
		}
		switch (operationStatus)
		{
		case OperationStatus.Done:
			return true;
		default:
			ThrowHelper.ThrowInvalidOperationException_InvalidUtf8();
			break;
		case OperationStatus.DestinationTooSmall:
			break;
		}
		bytesWritten = 0;
		return false;
	}

	static TSelf IUtf8SpanParsable<TSelf>.Parse(ReadOnlySpan<byte> utf8Text, IFormatProvider provider)
	{
		int maxCharCount = Encoding.UTF8.GetMaxCharCount(utf8Text.Length);
		char[] array;
		Span<char> span;
		if (maxCharCount < 256)
		{
			array = null;
			span = stackalloc char[256];
		}
		else
		{
			array = ArrayPool<char>.Shared.Rent(maxCharCount);
			span = array.AsSpan(0, maxCharCount);
		}
		if (Utf8.ToUtf16(utf8Text, span, out var _, out var charsWritten, replaceInvalidSequences: false) != 0)
		{
			ThrowHelper.ThrowFormatInvalidString();
		}
		span = span.Slice(0, charsWritten);
		TSelf result = TSelf.Parse(span, provider);
		if (array != null)
		{
			ArrayPool<char>.Shared.Return(array);
		}
		return result;
	}

	static bool IUtf8SpanParsable<TSelf>.TryParse(ReadOnlySpan<byte> utf8Text, IFormatProvider provider, [MaybeNullWhen(false)] out TSelf result)
	{
		int maxCharCount = Encoding.UTF8.GetMaxCharCount(utf8Text.Length);
		char[] array;
		Span<char> span;
		if (maxCharCount < 256)
		{
			array = null;
			span = stackalloc char[256];
		}
		else
		{
			array = ArrayPool<char>.Shared.Rent(maxCharCount);
			span = array.AsSpan(0, maxCharCount);
		}
		if (Utf8.ToUtf16(utf8Text, span, out var _, out var charsWritten, replaceInvalidSequences: false) != 0)
		{
			result = default(TSelf);
			return false;
		}
		span = span.Slice(0, charsWritten);
		bool result2 = TSelf.TryParse(span, provider, out result);
		if (array != null)
		{
			ArrayPool<char>.Shared.Return(array);
		}
		return result2;
	}
}
