namespace System.Numerics;

public interface ILogarithmicFunctions<TSelf> : IFloatingPointConstants<TSelf>, INumberBase<TSelf>, IAdditionOperators<TSelf, TSelf, TSelf>, IAdditiveIdentity<TSelf, TSelf>, IDecrementOperators<TSelf>, IDivisionOperators<TSelf, TSelf, TSelf>, IEquatable<TSelf>, IEqualityOperators<TSelf, TSelf, bool>, IIncrementOperators<TSelf>, IMultiplicativeIdentity<TSelf, TSelf>, IMultiplyOperators<TSelf, TSelf, TSelf>, ISpanFormattable, IFormattable, ISpanParsable<TSelf>, IParsable<TSelf>, ISubtractionOperators<TSelf, TSelf, TSelf>, IUnaryPlusOperators<TSelf, TSelf>, IUnaryNegationOperators<TSelf, TSelf>, IUtf8SpanFormattable, IUtf8SpanParsable<TSelf> where TSelf : ILogarithmicFunctions<TSelf>?
{
	static abstract TSelf Log(TSelf x);

	static abstract TSelf Log(TSelf x, TSelf newBase);

	static virtual TSelf LogP1(TSelf x)
	{
		return Log(x + TSelf.One);
	}

	static abstract TSelf Log2(TSelf x);

	static virtual TSelf Log2P1(TSelf x)
	{
		return Log2(x + TSelf.One);
	}

	static abstract TSelf Log10(TSelf x);

	static virtual TSelf Log10P1(TSelf x)
	{
		return Log10(x + TSelf.One);
	}
}
