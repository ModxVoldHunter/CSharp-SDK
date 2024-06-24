namespace System.Numerics;

public interface INumber<TSelf> : IComparable, IComparable<TSelf>, IComparisonOperators<TSelf, TSelf, bool>, IEqualityOperators<TSelf, TSelf, bool>, IModulusOperators<TSelf, TSelf, TSelf>, INumberBase<TSelf>, IAdditionOperators<TSelf, TSelf, TSelf>, IAdditiveIdentity<TSelf, TSelf>, IDecrementOperators<TSelf>, IDivisionOperators<TSelf, TSelf, TSelf>, IEquatable<TSelf>, IIncrementOperators<TSelf>, IMultiplicativeIdentity<TSelf, TSelf>, IMultiplyOperators<TSelf, TSelf, TSelf>, ISpanFormattable, IFormattable, ISpanParsable<TSelf>, IParsable<TSelf>, ISubtractionOperators<TSelf, TSelf, TSelf>, IUnaryPlusOperators<TSelf, TSelf>, IUnaryNegationOperators<TSelf, TSelf>, IUtf8SpanFormattable, IUtf8SpanParsable<TSelf> where TSelf : INumber<TSelf>?
{
	static virtual TSelf Clamp(TSelf value, TSelf min, TSelf max)
	{
		if (min > max)
		{
			Math.ThrowMinMaxException<TSelf>(min, max);
		}
		TSelf x = value;
		x = Max(x, min);
		return Min(x, max);
	}

	static virtual TSelf CopySign(TSelf value, TSelf sign)
	{
		TSelf val = value;
		if (TSelf.IsNegative(value) != TSelf.IsNegative(sign))
		{
			val = checked(-val);
		}
		return val;
	}

	static virtual TSelf Max(TSelf x, TSelf y)
	{
		if (x != y)
		{
			if (!TSelf.IsNaN(x))
			{
				if (!(y < x))
				{
					return y;
				}
				return x;
			}
			return x;
		}
		if (!TSelf.IsNegative(y))
		{
			return y;
		}
		return x;
	}

	static virtual TSelf MaxNumber(TSelf x, TSelf y)
	{
		if (x != y)
		{
			if (!TSelf.IsNaN(y))
			{
				if (!(y < x))
				{
					return y;
				}
				return x;
			}
			return x;
		}
		if (!TSelf.IsNegative(y))
		{
			return y;
		}
		return x;
	}

	static virtual TSelf Min(TSelf x, TSelf y)
	{
		if (x != y && !TSelf.IsNaN(x))
		{
			if (!(x < y))
			{
				return y;
			}
			return x;
		}
		if (!TSelf.IsNegative(x))
		{
			return y;
		}
		return x;
	}

	static virtual TSelf MinNumber(TSelf x, TSelf y)
	{
		if (x != y)
		{
			if (!TSelf.IsNaN(y))
			{
				if (!(x < y))
				{
					return y;
				}
				return x;
			}
			return x;
		}
		if (!TSelf.IsNegative(x))
		{
			return y;
		}
		return x;
	}

	static virtual int Sign(TSelf value)
	{
		if (value != TSelf.Zero)
		{
			if (!TSelf.IsNegative(value))
			{
				return 1;
			}
			return -1;
		}
		return 0;
	}
}
