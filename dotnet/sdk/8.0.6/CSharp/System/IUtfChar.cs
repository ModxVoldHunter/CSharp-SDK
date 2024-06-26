using System.Numerics;

namespace System;

internal interface IUtfChar<TSelf> : IBinaryInteger<TSelf>, IBinaryNumber<TSelf>, IBitwiseOperators<TSelf, TSelf, TSelf>, INumber<TSelf>, IComparable, IComparable<TSelf>, IComparisonOperators<TSelf, TSelf, bool>, IEqualityOperators<TSelf, TSelf, bool>, IModulusOperators<TSelf, TSelf, TSelf>, INumberBase<TSelf>, IAdditionOperators<TSelf, TSelf, TSelf>, IAdditiveIdentity<TSelf, TSelf>, IDecrementOperators<TSelf>, IDivisionOperators<TSelf, TSelf, TSelf>, IEquatable<TSelf>, IIncrementOperators<TSelf>, IMultiplicativeIdentity<TSelf, TSelf>, IMultiplyOperators<TSelf, TSelf, TSelf>, ISpanFormattable, IFormattable, ISpanParsable<TSelf>, IParsable<TSelf>, ISubtractionOperators<TSelf, TSelf, TSelf>, IUnaryPlusOperators<TSelf, TSelf>, IUnaryNegationOperators<TSelf, TSelf>, IUtf8SpanFormattable, IUtf8SpanParsable<TSelf>, IShiftOperators<TSelf, int, TSelf> where TSelf : unmanaged, IUtfChar<TSelf>
{
	static abstract TSelf CastFrom(byte value);

	static abstract TSelf CastFrom(char value);

	static abstract TSelf CastFrom(int value);

	static abstract TSelf CastFrom(uint value);

	static abstract TSelf CastFrom(ulong value);

	static abstract uint CastToUInt32(TSelf value);
}
