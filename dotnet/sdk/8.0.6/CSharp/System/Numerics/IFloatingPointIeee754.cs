namespace System.Numerics;

public interface IFloatingPointIeee754<TSelf> : IExponentialFunctions<TSelf>, IFloatingPointConstants<TSelf>, INumberBase<TSelf>, IAdditionOperators<TSelf, TSelf, TSelf>, IAdditiveIdentity<TSelf, TSelf>, IDecrementOperators<TSelf>, IDivisionOperators<TSelf, TSelf, TSelf>, IEquatable<TSelf>, IEqualityOperators<TSelf, TSelf, bool>, IIncrementOperators<TSelf>, IMultiplicativeIdentity<TSelf, TSelf>, IMultiplyOperators<TSelf, TSelf, TSelf>, ISpanFormattable, IFormattable, ISpanParsable<TSelf>, IParsable<TSelf>, ISubtractionOperators<TSelf, TSelf, TSelf>, IUnaryPlusOperators<TSelf, TSelf>, IUnaryNegationOperators<TSelf, TSelf>, IUtf8SpanFormattable, IUtf8SpanParsable<TSelf>, IFloatingPoint<TSelf>, INumber<TSelf>, IComparable, IComparable<TSelf>, IComparisonOperators<TSelf, TSelf, bool>, IModulusOperators<TSelf, TSelf, TSelf>, ISignedNumber<TSelf>, IHyperbolicFunctions<TSelf>, ILogarithmicFunctions<TSelf>, IPowerFunctions<TSelf>, IRootFunctions<TSelf>, ITrigonometricFunctions<TSelf> where TSelf : IFloatingPointIeee754<TSelf>?
{
	static abstract TSelf Epsilon { get; }

	static abstract TSelf NaN { get; }

	static abstract TSelf NegativeInfinity { get; }

	static abstract TSelf NegativeZero { get; }

	static abstract TSelf PositiveInfinity { get; }

	static abstract TSelf Atan2(TSelf y, TSelf x);

	static abstract TSelf Atan2Pi(TSelf y, TSelf x);

	static abstract TSelf BitDecrement(TSelf x);

	static abstract TSelf BitIncrement(TSelf x);

	static abstract TSelf FusedMultiplyAdd(TSelf left, TSelf right, TSelf addend);

	static abstract TSelf Ieee754Remainder(TSelf left, TSelf right);

	static abstract int ILogB(TSelf x);

	static virtual TSelf Lerp(TSelf value1, TSelf value2, TSelf amount)
	{
		return value1 * (TSelf.One - amount) + value2 * amount;
	}

	static virtual TSelf ReciprocalEstimate(TSelf x)
	{
		return TSelf.One / x;
	}

	static virtual TSelf ReciprocalSqrtEstimate(TSelf x)
	{
		return TSelf.One / TSelf.Sqrt(x);
	}

	static abstract TSelf ScaleB(TSelf x, int n);
}
