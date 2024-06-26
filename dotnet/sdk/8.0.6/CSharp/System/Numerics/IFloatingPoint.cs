namespace System.Numerics;

public interface IFloatingPoint<TSelf> : IFloatingPointConstants<TSelf>, INumberBase<TSelf>, IAdditionOperators<TSelf, TSelf, TSelf>, IAdditiveIdentity<TSelf, TSelf>, IDecrementOperators<TSelf>, IDivisionOperators<TSelf, TSelf, TSelf>, IEquatable<TSelf>, IEqualityOperators<TSelf, TSelf, bool>, IIncrementOperators<TSelf>, IMultiplicativeIdentity<TSelf, TSelf>, IMultiplyOperators<TSelf, TSelf, TSelf>, ISpanFormattable, IFormattable, ISpanParsable<TSelf>, IParsable<TSelf>, ISubtractionOperators<TSelf, TSelf, TSelf>, IUnaryPlusOperators<TSelf, TSelf>, IUnaryNegationOperators<TSelf, TSelf>, IUtf8SpanFormattable, IUtf8SpanParsable<TSelf>, INumber<TSelf>, IComparable, IComparable<TSelf>, IComparisonOperators<TSelf, TSelf, bool>, IModulusOperators<TSelf, TSelf, TSelf>, ISignedNumber<TSelf> where TSelf : IFloatingPoint<TSelf>?
{
	static virtual TSelf Ceiling(TSelf x)
	{
		return Round(x, 0, MidpointRounding.ToPositiveInfinity);
	}

	static virtual TSelf Floor(TSelf x)
	{
		return Round(x, 0, MidpointRounding.ToNegativeInfinity);
	}

	static virtual TSelf Round(TSelf x)
	{
		return Round(x, 0, MidpointRounding.ToEven);
	}

	static virtual TSelf Round(TSelf x, int digits)
	{
		return Round(x, digits, MidpointRounding.ToEven);
	}

	static virtual TSelf Round(TSelf x, MidpointRounding mode)
	{
		return Round(x, 0, mode);
	}

	static abstract TSelf Round(TSelf x, int digits, MidpointRounding mode);

	static virtual TSelf Truncate(TSelf x)
	{
		return Round(x, 0, MidpointRounding.ToZero);
	}

	int GetExponentByteCount();

	int GetExponentShortestBitLength();

	int GetSignificandBitLength();

	int GetSignificandByteCount();

	bool TryWriteExponentBigEndian(Span<byte> destination, out int bytesWritten);

	bool TryWriteExponentLittleEndian(Span<byte> destination, out int bytesWritten);

	bool TryWriteSignificandBigEndian(Span<byte> destination, out int bytesWritten);

	bool TryWriteSignificandLittleEndian(Span<byte> destination, out int bytesWritten);

	int WriteExponentBigEndian(byte[] destination)
	{
		if (!TryWriteExponentBigEndian(destination, out var bytesWritten))
		{
			ThrowHelper.ThrowArgumentException_DestinationTooShort();
		}
		return bytesWritten;
	}

	int WriteExponentBigEndian(byte[] destination, int startIndex)
	{
		if (!TryWriteExponentBigEndian(destination.AsSpan(startIndex), out var bytesWritten))
		{
			ThrowHelper.ThrowArgumentException_DestinationTooShort();
		}
		return bytesWritten;
	}

	int WriteExponentBigEndian(Span<byte> destination)
	{
		if (!TryWriteExponentBigEndian(destination, out var bytesWritten))
		{
			ThrowHelper.ThrowArgumentException_DestinationTooShort();
		}
		return bytesWritten;
	}

	int WriteExponentLittleEndian(byte[] destination)
	{
		if (!TryWriteExponentLittleEndian(destination, out var bytesWritten))
		{
			ThrowHelper.ThrowArgumentException_DestinationTooShort();
		}
		return bytesWritten;
	}

	int WriteExponentLittleEndian(byte[] destination, int startIndex)
	{
		if (!TryWriteExponentLittleEndian(destination.AsSpan(startIndex), out var bytesWritten))
		{
			ThrowHelper.ThrowArgumentException_DestinationTooShort();
		}
		return bytesWritten;
	}

	int WriteExponentLittleEndian(Span<byte> destination)
	{
		if (!TryWriteExponentLittleEndian(destination, out var bytesWritten))
		{
			ThrowHelper.ThrowArgumentException_DestinationTooShort();
		}
		return bytesWritten;
	}

	int WriteSignificandBigEndian(byte[] destination)
	{
		if (!TryWriteSignificandBigEndian(destination, out var bytesWritten))
		{
			ThrowHelper.ThrowArgumentException_DestinationTooShort();
		}
		return bytesWritten;
	}

	int WriteSignificandBigEndian(byte[] destination, int startIndex)
	{
		if (!TryWriteSignificandBigEndian(destination.AsSpan(startIndex), out var bytesWritten))
		{
			ThrowHelper.ThrowArgumentException_DestinationTooShort();
		}
		return bytesWritten;
	}

	int WriteSignificandBigEndian(Span<byte> destination)
	{
		if (!TryWriteSignificandBigEndian(destination, out var bytesWritten))
		{
			ThrowHelper.ThrowArgumentException_DestinationTooShort();
		}
		return bytesWritten;
	}

	int WriteSignificandLittleEndian(byte[] destination)
	{
		if (!TryWriteSignificandLittleEndian(destination, out var bytesWritten))
		{
			ThrowHelper.ThrowArgumentException_DestinationTooShort();
		}
		return bytesWritten;
	}

	int WriteSignificandLittleEndian(byte[] destination, int startIndex)
	{
		if (!TryWriteSignificandLittleEndian(destination.AsSpan(startIndex), out var bytesWritten))
		{
			ThrowHelper.ThrowArgumentException_DestinationTooShort();
		}
		return bytesWritten;
	}

	int WriteSignificandLittleEndian(Span<byte> destination)
	{
		if (!TryWriteSignificandLittleEndian(destination, out var bytesWritten))
		{
			ThrowHelper.ThrowArgumentException_DestinationTooShort();
		}
		return bytesWritten;
	}
}
