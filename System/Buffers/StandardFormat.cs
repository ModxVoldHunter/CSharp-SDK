using System.Diagnostics.CodeAnalysis;

namespace System.Buffers;

public readonly struct StandardFormat : IEquatable<StandardFormat>
{
	public const byte NoPrecision = byte.MaxValue;

	public const byte MaxPrecision = 99;

	private readonly byte _format;

	private readonly byte _precision;

	public char Symbol => (char)_format;

	public byte Precision => _precision;

	public bool HasPrecision => _precision != byte.MaxValue;

	internal byte PrecisionOrZero
	{
		get
		{
			if (_precision == byte.MaxValue)
			{
				return 0;
			}
			return _precision;
		}
	}

	public bool IsDefault => (_format | _precision) == 0;

	public StandardFormat(char symbol, byte precision = byte.MaxValue)
	{
		if (precision != byte.MaxValue && precision > 99)
		{
			ThrowHelper.ThrowArgumentOutOfRangeException_PrecisionTooLarge();
		}
		if (symbol != (byte)symbol)
		{
			ThrowHelper.ThrowArgumentOutOfRangeException_SymbolDoesNotFit();
		}
		_format = (byte)symbol;
		_precision = precision;
	}

	public static implicit operator StandardFormat(char symbol)
	{
		return new StandardFormat(symbol);
	}

	public static StandardFormat Parse([StringSyntax("NumericFormat")] ReadOnlySpan<char> format)
	{
		ParseHelper(format, out var standardFormat, throws: true);
		return standardFormat;
	}

	public static StandardFormat Parse([StringSyntax("NumericFormat")] string? format)
	{
		if (format != null)
		{
			return Parse(format.AsSpan());
		}
		return default(StandardFormat);
	}

	public static bool TryParse([StringSyntax("NumericFormat")] ReadOnlySpan<char> format, out StandardFormat result)
	{
		return ParseHelper(format, out result);
	}

	private static bool ParseHelper(ReadOnlySpan<char> format, out StandardFormat standardFormat, bool throws = false)
	{
		standardFormat = default(StandardFormat);
		if (format.Length == 0)
		{
			return true;
		}
		char symbol = format[0];
		byte precision;
		if (format.Length == 1)
		{
			precision = byte.MaxValue;
		}
		else
		{
			uint num = 0u;
			for (int i = 1; i < format.Length; i++)
			{
				uint num2 = (uint)(format[i] - 48);
				if (num2 > 9)
				{
					if (!throws)
					{
						return false;
					}
					throw new FormatException(SR.Format(SR.Argument_CannotParsePrecision, (byte)99));
				}
				num = num * 10 + num2;
				if (num > 99)
				{
					if (!throws)
					{
						return false;
					}
					throw new FormatException(SR.Format(SR.Argument_PrecisionTooLarge, (byte)99));
				}
			}
			precision = (byte)num;
		}
		standardFormat = new StandardFormat(symbol, precision);
		return true;
	}

	public override bool Equals([NotNullWhen(true)] object? obj)
	{
		if (obj is StandardFormat other)
		{
			return Equals(other);
		}
		return false;
	}

	public override int GetHashCode()
	{
		return _format.GetHashCode() ^ _precision.GetHashCode();
	}

	public bool Equals(StandardFormat other)
	{
		if (_format == other._format)
		{
			return _precision == other._precision;
		}
		return false;
	}

	public override string ToString()
	{
		Span<char> destination = stackalloc char[3];
		return new string(Format(destination));
	}

	internal Span<char> Format(Span<char> destination)
	{
		char symbol = Symbol;
		if (symbol != 0 && destination.Length == 3)
		{
			destination[0] = symbol;
			uint precision = Precision;
			switch (precision)
			{
			default:
			{
				uint num;
				(num, precision) = Math.DivRem(precision, 10u);
				destination[1] = (char)(48 + num % 10);
				destination[2] = (char)(48 + precision);
				return destination;
			}
			case 0u:
			case 1u:
			case 2u:
			case 3u:
			case 4u:
			case 5u:
			case 6u:
			case 7u:
			case 8u:
			case 9u:
				destination[1] = (char)(48 + precision);
				return destination.Slice(0, 2);
			case 255u:
				return destination.Slice(0, 1);
			}
		}
		return default(Span<char>);
	}

	public static bool operator ==(StandardFormat left, StandardFormat right)
	{
		return left.Equals(right);
	}

	public static bool operator !=(StandardFormat left, StandardFormat right)
	{
		return !left.Equals(right);
	}
}
