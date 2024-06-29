namespace System.Numerics;

public interface IBinaryInteger<TSelf> : IBinaryNumber<TSelf>, IBitwiseOperators<TSelf, TSelf, TSelf>, INumber<TSelf>, IComparable, IComparable<TSelf>, IComparisonOperators<TSelf, TSelf, bool>, IEqualityOperators<TSelf, TSelf, bool>, IModulusOperators<TSelf, TSelf, TSelf>, INumberBase<TSelf>, IAdditionOperators<TSelf, TSelf, TSelf>, IAdditiveIdentity<TSelf, TSelf>, IDecrementOperators<TSelf>, IDivisionOperators<TSelf, TSelf, TSelf>, IEquatable<TSelf>, IIncrementOperators<TSelf>, IMultiplicativeIdentity<TSelf, TSelf>, IMultiplyOperators<TSelf, TSelf, TSelf>, ISpanFormattable, IFormattable, ISpanParsable<TSelf>, IParsable<TSelf>, ISubtractionOperators<TSelf, TSelf, TSelf>, IUnaryPlusOperators<TSelf, TSelf>, IUnaryNegationOperators<TSelf, TSelf>, IUtf8SpanFormattable, IUtf8SpanParsable<TSelf>, IShiftOperators<TSelf, int, TSelf> where TSelf : IBinaryInteger<TSelf>?
{
	static virtual (TSelf Quotient, TSelf Remainder) DivRem(TSelf left, TSelf right)
	{
		TSelf val = left / right;
		return (Quotient: val, Remainder: left - val * right);
	}

	static virtual TSelf LeadingZeroCount(TSelf value)
	{
		if (!typeof(TSelf).IsValueType)
		{
			ArgumentNullException.ThrowIfNull(value, "value");
		}
		TSelf val = TSelf.CreateChecked((long)value.GetByteCount() * 8L);
		if (value == TSelf.Zero)
		{
			return TSelf.CreateChecked(val);
		}
		return (val - TSelf.One) ^ TSelf.Log2(value);
	}

	static abstract TSelf PopCount(TSelf value);

	static virtual TSelf ReadBigEndian(byte[] source, bool isUnsigned)
	{
		if (!TryReadBigEndian(source, isUnsigned, out var value))
		{
			ThrowHelper.ThrowOverflowException();
		}
		return value;
	}

	static virtual TSelf ReadBigEndian(byte[] source, int startIndex, bool isUnsigned)
	{
		if (!TryReadBigEndian(source.AsSpan(startIndex), isUnsigned, out var value))
		{
			ThrowHelper.ThrowOverflowException();
		}
		return value;
	}

	static virtual TSelf ReadBigEndian(ReadOnlySpan<byte> source, bool isUnsigned)
	{
		if (!TryReadBigEndian(source, isUnsigned, out var value))
		{
			ThrowHelper.ThrowOverflowException();
		}
		return value;
	}

	static virtual TSelf ReadLittleEndian(byte[] source, bool isUnsigned)
	{
		if (!TryReadLittleEndian(source, isUnsigned, out var value))
		{
			ThrowHelper.ThrowOverflowException();
		}
		return value;
	}

	static virtual TSelf ReadLittleEndian(byte[] source, int startIndex, bool isUnsigned)
	{
		if (!TryReadLittleEndian(source.AsSpan(startIndex), isUnsigned, out var value))
		{
			ThrowHelper.ThrowOverflowException();
		}
		return value;
	}

	static virtual TSelf ReadLittleEndian(ReadOnlySpan<byte> source, bool isUnsigned)
	{
		if (!TryReadLittleEndian(source, isUnsigned, out var value))
		{
			ThrowHelper.ThrowOverflowException();
		}
		return value;
	}

	static virtual TSelf RotateLeft(TSelf value, int rotateAmount)
	{
		if (!typeof(TSelf).IsValueType)
		{
			ArgumentNullException.ThrowIfNull(value, "value");
		}
		int num = checked(value.GetByteCount() * 8);
		return (value << rotateAmount) | (value >> num - rotateAmount);
	}

	static virtual TSelf RotateRight(TSelf value, int rotateAmount)
	{
		if (!typeof(TSelf).IsValueType)
		{
			ArgumentNullException.ThrowIfNull(value, "value");
		}
		int num = checked(value.GetByteCount() * 8);
		return (value >> rotateAmount) | (value << num - rotateAmount);
	}

	static abstract TSelf TrailingZeroCount(TSelf value);

	static abstract bool TryReadBigEndian(ReadOnlySpan<byte> source, bool isUnsigned, out TSelf value);

	static abstract bool TryReadLittleEndian(ReadOnlySpan<byte> source, bool isUnsigned, out TSelf value);

	int GetByteCount();

	int GetShortestBitLength();

	bool TryWriteBigEndian(Span<byte> destination, out int bytesWritten);

	bool TryWriteLittleEndian(Span<byte> destination, out int bytesWritten);

	int WriteBigEndian(byte[] destination)
	{
		if (!TryWriteBigEndian(destination, out var bytesWritten))
		{
			ThrowHelper.ThrowArgumentException_DestinationTooShort();
		}
		return bytesWritten;
	}

	int WriteBigEndian(byte[] destination, int startIndex)
	{
		if (!TryWriteBigEndian(destination.AsSpan(startIndex), out var bytesWritten))
		{
			ThrowHelper.ThrowArgumentException_DestinationTooShort();
		}
		return bytesWritten;
	}

	int WriteBigEndian(Span<byte> destination)
	{
		if (!TryWriteBigEndian(destination, out var bytesWritten))
		{
			ThrowHelper.ThrowArgumentException_DestinationTooShort();
		}
		return bytesWritten;
	}

	int WriteLittleEndian(byte[] destination)
	{
		if (!TryWriteLittleEndian(destination, out var bytesWritten))
		{
			ThrowHelper.ThrowArgumentException_DestinationTooShort();
		}
		return bytesWritten;
	}

	int WriteLittleEndian(byte[] destination, int startIndex)
	{
		if (!TryWriteLittleEndian(destination.AsSpan(startIndex), out var bytesWritten))
		{
			ThrowHelper.ThrowArgumentException_DestinationTooShort();
		}
		return bytesWritten;
	}

	int WriteLittleEndian(Span<byte> destination)
	{
		if (!TryWriteLittleEndian(destination, out var bytesWritten))
		{
			ThrowHelper.ThrowArgumentException_DestinationTooShort();
		}
		return bytesWritten;
	}
}
