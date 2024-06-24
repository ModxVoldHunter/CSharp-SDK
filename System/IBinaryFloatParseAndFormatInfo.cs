using System.Numerics;

namespace System;

internal interface IBinaryFloatParseAndFormatInfo<TSelf> : IBinaryFloatingPointIeee754<TSelf>, IBinaryNumber<TSelf>, IBitwiseOperators<TSelf, TSelf, TSelf>, INumber<TSelf>, IComparable, IComparable<TSelf>, IComparisonOperators<TSelf, TSelf, bool>, IEqualityOperators<TSelf, TSelf, bool>, IModulusOperators<TSelf, TSelf, TSelf>, INumberBase<TSelf>, IAdditionOperators<TSelf, TSelf, TSelf>, IAdditiveIdentity<TSelf, TSelf>, IDecrementOperators<TSelf>, IDivisionOperators<TSelf, TSelf, TSelf>, IEquatable<TSelf>, IIncrementOperators<TSelf>, IMultiplicativeIdentity<TSelf, TSelf>, IMultiplyOperators<TSelf, TSelf, TSelf>, ISpanFormattable, IFormattable, ISpanParsable<TSelf>, IParsable<TSelf>, ISubtractionOperators<TSelf, TSelf, TSelf>, IUnaryPlusOperators<TSelf, TSelf>, IUnaryNegationOperators<TSelf, TSelf>, IUtf8SpanFormattable, IUtf8SpanParsable<TSelf>, IFloatingPointIeee754<TSelf>, IExponentialFunctions<TSelf>, IFloatingPointConstants<TSelf>, IFloatingPoint<TSelf>, ISignedNumber<TSelf>, IHyperbolicFunctions<TSelf>, ILogarithmicFunctions<TSelf>, IPowerFunctions<TSelf>, IRootFunctions<TSelf>, ITrigonometricFunctions<TSelf>, IMinMaxValue<TSelf> where TSelf : unmanaged, IBinaryFloatParseAndFormatInfo<TSelf>
{
	static abstract int NumberBufferLength { get; }

	static abstract ulong ZeroBits { get; }

	static abstract ulong InfinityBits { get; }

	static abstract ulong NormalMantissaMask { get; }

	static abstract ulong DenormalMantissaMask { get; }

	static abstract int MinBinaryExponent { get; }

	static abstract int MaxBinaryExponent { get; }

	static abstract int MinDecimalExponent { get; }

	static abstract int MaxDecimalExponent { get; }

	static abstract int ExponentBias { get; }

	static abstract ushort ExponentBits { get; }

	static abstract int OverflowDecimalExponent { get; }

	static abstract int InfinityExponent { get; }

	static abstract ushort NormalMantissaBits { get; }

	static abstract ushort DenormalMantissaBits { get; }

	static abstract int MinFastFloatDecimalExponent { get; }

	static abstract int MaxFastFloatDecimalExponent { get; }

	static abstract int MinExponentRoundToEven { get; }

	static abstract int MaxExponentRoundToEven { get; }

	static abstract int MaxExponentFastPath { get; }

	static abstract ulong MaxMantissaFastPath { get; }

	static abstract TSelf BitsToFloat(ulong bits);

	static abstract ulong FloatToBits(TSelf value);
}
