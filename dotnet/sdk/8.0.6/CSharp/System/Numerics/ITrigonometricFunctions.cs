namespace System.Numerics;

public interface ITrigonometricFunctions<TSelf> : IFloatingPointConstants<TSelf>, INumberBase<TSelf>, IAdditionOperators<TSelf, TSelf, TSelf>, IAdditiveIdentity<TSelf, TSelf>, IDecrementOperators<TSelf>, IDivisionOperators<TSelf, TSelf, TSelf>, IEquatable<TSelf>, IEqualityOperators<TSelf, TSelf, bool>, IIncrementOperators<TSelf>, IMultiplicativeIdentity<TSelf, TSelf>, IMultiplyOperators<TSelf, TSelf, TSelf>, ISpanFormattable, IFormattable, ISpanParsable<TSelf>, IParsable<TSelf>, ISubtractionOperators<TSelf, TSelf, TSelf>, IUnaryPlusOperators<TSelf, TSelf>, IUnaryNegationOperators<TSelf, TSelf>, IUtf8SpanFormattable, IUtf8SpanParsable<TSelf> where TSelf : ITrigonometricFunctions<TSelf>?
{
	static abstract TSelf Acos(TSelf x);

	static abstract TSelf AcosPi(TSelf x);

	static abstract TSelf Asin(TSelf x);

	static abstract TSelf AsinPi(TSelf x);

	static abstract TSelf Atan(TSelf x);

	static abstract TSelf AtanPi(TSelf x);

	static abstract TSelf Cos(TSelf x);

	static abstract TSelf CosPi(TSelf x);

	static virtual TSelf DegreesToRadians(TSelf degrees)
	{
		return degrees * TSelf.Pi / TSelf.CreateChecked(180);
	}

	static virtual TSelf RadiansToDegrees(TSelf radians)
	{
		return radians * TSelf.CreateChecked(180) / TSelf.Pi;
	}

	static abstract TSelf Sin(TSelf x);

	static abstract (TSelf Sin, TSelf Cos) SinCos(TSelf x);

	static abstract (TSelf SinPi, TSelf CosPi) SinCosPi(TSelf x);

	static abstract TSelf SinPi(TSelf x);

	static abstract TSelf Tan(TSelf x);

	static abstract TSelf TanPi(TSelf x);
}
