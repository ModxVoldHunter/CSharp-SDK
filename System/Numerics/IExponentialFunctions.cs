namespace System.Numerics;

public interface IExponentialFunctions<TSelf> : IFloatingPointConstants<TSelf>, INumberBase<TSelf>, IAdditionOperators<TSelf, TSelf, TSelf>, IAdditiveIdentity<TSelf, TSelf>, IDecrementOperators<TSelf>, IDivisionOperators<TSelf, TSelf, TSelf>, IEquatable<TSelf>, IEqualityOperators<TSelf, TSelf, bool>, IIncrementOperators<TSelf>, IMultiplicativeIdentity<TSelf, TSelf>, IMultiplyOperators<TSelf, TSelf, TSelf>, ISpanFormattable, IFormattable, ISpanParsable<TSelf>, IParsable<TSelf>, ISubtractionOperators<TSelf, TSelf, TSelf>, IUnaryPlusOperators<TSelf, TSelf>, IUnaryNegationOperators<TSelf, TSelf>, IUtf8SpanFormattable, IUtf8SpanParsable<TSelf> where TSelf : IExponentialFunctions<TSelf>?
{
	static abstract TSelf Exp(TSelf x);

	static virtual TSelf ExpM1(TSelf x)
	{
		return Exp(x) - TSelf.One;
	}

	static abstract TSelf Exp2(TSelf x);

	static virtual TSelf Exp2M1(TSelf x)
	{
		return Exp2(x) - TSelf.One;
	}

	static abstract TSelf Exp10(TSelf x);

	static virtual TSelf Exp10M1(TSelf x)
	{
		return Exp10(x) - TSelf.One;
	}
}
