using System.Numerics;

namespace System;

internal interface IBinaryIntegerParseAndFormatInfo<TSelf> : IBinaryInteger<TSelf>, IBinaryNumber<TSelf>, IBitwiseOperators<TSelf, TSelf, TSelf>, INumber<TSelf>, IComparable, IComparable<TSelf>, IComparisonOperators<TSelf, TSelf, bool>, IEqualityOperators<TSelf, TSelf, bool>, IModulusOperators<TSelf, TSelf, TSelf>, INumberBase<TSelf>, IAdditionOperators<TSelf, TSelf, TSelf>, IAdditiveIdentity<TSelf, TSelf>, IDecrementOperators<TSelf>, IDivisionOperators<TSelf, TSelf, TSelf>, IEquatable<TSelf>, IIncrementOperators<TSelf>, IMultiplicativeIdentity<TSelf, TSelf>, IMultiplyOperators<TSelf, TSelf, TSelf>, ISpanFormattable, IFormattable, ISpanParsable<TSelf>, IParsable<TSelf>, ISubtractionOperators<TSelf, TSelf, TSelf>, IUnaryPlusOperators<TSelf, TSelf>, IUnaryNegationOperators<TSelf, TSelf>, IUtf8SpanFormattable, IUtf8SpanParsable<TSelf>, IShiftOperators<TSelf, int, TSelf>, IMinMaxValue<TSelf> where TSelf : unmanaged, IBinaryIntegerParseAndFormatInfo<TSelf>
{
	static abstract bool IsSigned { get; }

	static abstract int MaxDigitCount { get; }

	static abstract int MaxHexDigitCount { get; }

	static abstract TSelf MaxValueDiv10 { get; }

	static abstract string OverflowMessage { get; }

	static abstract bool IsGreaterThanAsUnsigned(TSelf left, TSelf right);

	static abstract TSelf MultiplyBy10(TSelf value);

	static abstract TSelf MultiplyBy16(TSelf value);
}
