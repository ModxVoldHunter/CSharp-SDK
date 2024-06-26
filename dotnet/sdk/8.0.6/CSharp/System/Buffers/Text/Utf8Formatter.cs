using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;

namespace System.Buffers.Text;

public static class Utf8Formatter
{
	private static TimeSpan NullOffset => new TimeSpan(long.MinValue);

	public static bool TryFormat(bool value, Span<byte> destination, out int bytesWritten, StandardFormat format = default(StandardFormat))
	{
		char symbolOrDefault = FormattingHelpers.GetSymbolOrDefault(in format, 'G');
		if (value)
		{
			if (symbolOrDefault == 'G')
			{
				if ("True"u8.TryCopyTo(destination))
				{
					goto IL_0045;
				}
			}
			else
			{
				if (symbolOrDefault != 'l')
				{
					goto IL_0087;
				}
				if ("true"u8.TryCopyTo(destination))
				{
					goto IL_0045;
				}
			}
		}
		else if (symbolOrDefault == 'G')
		{
			if ("False"u8.TryCopyTo(destination))
			{
				goto IL_0082;
			}
		}
		else
		{
			if (symbolOrDefault != 'l')
			{
				goto IL_0087;
			}
			if ("false"u8.TryCopyTo(destination))
			{
				goto IL_0082;
			}
		}
		goto IL_008c;
		IL_0082:
		bytesWritten = 5;
		return true;
		IL_0045:
		bytesWritten = 4;
		return true;
		IL_008c:
		bytesWritten = 0;
		return false;
		IL_0087:
		ThrowHelper.ThrowFormatException_BadFormatSpecifier();
		goto IL_008c;
	}

	public static bool TryFormat(DateTimeOffset value, Span<byte> destination, out int bytesWritten, StandardFormat format = default(StandardFormat))
	{
		if (format.IsDefault)
		{
			return DateTimeFormat.TryFormatInvariantG(value.DateTime, value.Offset, destination, out bytesWritten);
		}
		char symbol = format.Symbol;
		if ((uint)symbol <= 79u)
		{
			switch (symbol)
			{
			case 'O':
				return DateTimeFormat.TryFormatO(value.DateTime, value.Offset, destination, out bytesWritten);
			case 'G':
				return DateTimeFormat.TryFormatInvariantG(value.DateTime, NullOffset, destination, out bytesWritten);
			}
		}
		else
		{
			if (symbol == 'R')
			{
				goto IL_0044;
			}
			if (symbol == 'l')
			{
				return TryFormatDateTimeL(value.UtcDateTime, destination, out bytesWritten);
			}
		}
		ThrowHelper.ThrowFormatException_BadFormatSpecifier();
		goto IL_0044;
		IL_0044:
		return DateTimeFormat.TryFormatR(value.UtcDateTime, NullOffset, destination, out bytesWritten);
	}

	public static bool TryFormat(DateTime value, Span<byte> destination, out int bytesWritten, StandardFormat format = default(StandardFormat))
	{
		char symbolOrDefault = FormattingHelpers.GetSymbolOrDefault(in format, 'G');
		if ((uint)symbolOrDefault <= 79u)
		{
			switch (symbolOrDefault)
			{
			case 'O':
				return DateTimeFormat.TryFormatO(value, NullOffset, destination, out bytesWritten);
			case 'G':
				return DateTimeFormat.TryFormatInvariantG(value, NullOffset, destination, out bytesWritten);
			}
		}
		else
		{
			if (symbolOrDefault == 'R')
			{
				goto IL_0027;
			}
			if (symbolOrDefault == 'l')
			{
				return TryFormatDateTimeL(value, destination, out bytesWritten);
			}
		}
		ThrowHelper.ThrowFormatException_BadFormatSpecifier();
		goto IL_0027;
		IL_0027:
		return DateTimeFormat.TryFormatR(value, NullOffset, destination, out bytesWritten);
	}

	private static bool TryFormatDateTimeL(DateTime value, Span<byte> destination, out int bytesWritten)
	{
		if (DateTimeFormat.TryFormatR(value, NullOffset, destination, out bytesWritten))
		{
			Ascii.ToLowerInPlace(destination.Slice(0, bytesWritten), out bytesWritten);
			return true;
		}
		return false;
	}

	public static bool TryFormat(decimal value, Span<byte> destination, out int bytesWritten, StandardFormat format = default(StandardFormat))
	{
		return FormattingHelpers.TryFormat(value, destination, out bytesWritten, format);
	}

	public static bool TryFormat(double value, Span<byte> destination, out int bytesWritten, StandardFormat format = default(StandardFormat))
	{
		return FormattingHelpers.TryFormat(value, destination, out bytesWritten, format);
	}

	public static bool TryFormat(float value, Span<byte> destination, out int bytesWritten, StandardFormat format = default(StandardFormat))
	{
		return FormattingHelpers.TryFormat(value, destination, out bytesWritten, format);
	}

	public static bool TryFormat(Guid value, Span<byte> destination, out int bytesWritten, StandardFormat format = default(StandardFormat))
	{
		char symbolOrDefault = FormattingHelpers.GetSymbolOrDefault(in format, 'D');
		int flags;
		if ((uint)symbolOrDefault <= 68u)
		{
			if (symbolOrDefault != 'B')
			{
				if (symbolOrDefault == 'D')
				{
					goto IL_0027;
				}
				goto IL_0044;
			}
			flags = -2139260122;
		}
		else if (symbolOrDefault != 'N')
		{
			if (symbolOrDefault != 'P')
			{
				goto IL_0044;
			}
			flags = -2144786394;
		}
		else
		{
			flags = 32;
		}
		goto IL_004b;
		IL_004b:
		return value.TryFormatCore(destination, out bytesWritten, flags);
		IL_0027:
		flags = -2147483612;
		goto IL_004b;
		IL_0044:
		ThrowHelper.ThrowFormatException_BadFormatSpecifier();
		goto IL_0027;
	}

	public static bool TryFormat(byte value, Span<byte> destination, out int bytesWritten, StandardFormat format = default(StandardFormat))
	{
		return TryFormat((uint)value, destination, out bytesWritten, format);
	}

	[CLSCompliant(false)]
	public static bool TryFormat(sbyte value, Span<byte> destination, out int bytesWritten, StandardFormat format = default(StandardFormat))
	{
		return TryFormat(value, 255, destination, out bytesWritten, format);
	}

	[CLSCompliant(false)]
	public static bool TryFormat(ushort value, Span<byte> destination, out int bytesWritten, StandardFormat format = default(StandardFormat))
	{
		return TryFormat((uint)value, destination, out bytesWritten, format);
	}

	public static bool TryFormat(short value, Span<byte> destination, out int bytesWritten, StandardFormat format = default(StandardFormat))
	{
		return TryFormat(value, 65535, destination, out bytesWritten, format);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[CLSCompliant(false)]
	public static bool TryFormat(uint value, Span<byte> destination, out int bytesWritten, StandardFormat format = default(StandardFormat))
	{
		if (format.IsDefault)
		{
			return Number.TryUInt32ToDecStr(value, destination, out bytesWritten);
		}
		int num = format.Symbol | 0x20;
		if (num <= 103)
		{
			if (num == 100)
			{
				goto IL_003f;
			}
			if (num == 103)
			{
				goto IL_0075;
			}
		}
		else
		{
			if (num == 110)
			{
				return FormattingHelpers.TryFormat(value, destination, out bytesWritten, format);
			}
			if (num == 114)
			{
				goto IL_0075;
			}
			if (num == 120)
			{
				return Number.TryInt32ToHexStr((int)value, Number.GetHexBase(format.Symbol), format.PrecisionOrZero, destination, out bytesWritten);
			}
		}
		ThrowHelper.ThrowFormatException_BadFormatSpecifier();
		goto IL_003f;
		IL_0075:
		if (format.HasPrecision)
		{
			ThrowGWithPrecisionNotSupported();
		}
		goto IL_003f;
		IL_003f:
		return Number.TryUInt32ToDecStr(value, format.PrecisionOrZero, destination, out bytesWritten);
	}

	public static bool TryFormat(int value, Span<byte> destination, out int bytesWritten, StandardFormat format = default(StandardFormat))
	{
		return TryFormat(value, -1, destination, out bytesWritten, format);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static bool TryFormat(int value, int hexMask, Span<byte> destination, out int bytesWritten, StandardFormat format = default(StandardFormat))
	{
		if (format.IsDefault)
		{
			if (value < 0)
			{
				return Number.TryNegativeInt32ToDecStr(value, format.PrecisionOrZero, "-"u8, destination, out bytesWritten);
			}
			return Number.TryUInt32ToDecStr((uint)value, destination, out bytesWritten);
		}
		int num = format.Symbol | 0x20;
		if (num <= 103)
		{
			if (num == 100)
			{
				goto IL_005e;
			}
			if (num == 103)
			{
				goto IL_00b6;
			}
		}
		else
		{
			if (num == 110)
			{
				return FormattingHelpers.TryFormat(value, destination, out bytesWritten, format);
			}
			if (num == 114)
			{
				goto IL_00b6;
			}
			if (num == 120)
			{
				return Number.TryInt32ToHexStr(value & hexMask, Number.GetHexBase(format.Symbol), format.PrecisionOrZero, destination, out bytesWritten);
			}
		}
		ThrowHelper.ThrowFormatException_BadFormatSpecifier();
		goto IL_005e;
		IL_005e:
		if (value < 0)
		{
			return Number.TryNegativeInt32ToDecStr(value, format.PrecisionOrZero, "-"u8, destination, out bytesWritten);
		}
		return Number.TryUInt32ToDecStr((uint)value, format.PrecisionOrZero, destination, out bytesWritten);
		IL_00b6:
		if (format.HasPrecision)
		{
			ThrowGWithPrecisionNotSupported();
		}
		goto IL_005e;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[CLSCompliant(false)]
	public static bool TryFormat(ulong value, Span<byte> destination, out int bytesWritten, StandardFormat format = default(StandardFormat))
	{
		if (format.IsDefault)
		{
			return Number.TryUInt64ToDecStr(value, destination, out bytesWritten);
		}
		int num = format.Symbol | 0x20;
		if (num <= 103)
		{
			if (num == 100)
			{
				goto IL_003f;
			}
			if (num == 103)
			{
				goto IL_0075;
			}
		}
		else
		{
			if (num == 110)
			{
				return FormattingHelpers.TryFormat(value, destination, out bytesWritten, format);
			}
			if (num == 114)
			{
				goto IL_0075;
			}
			if (num == 120)
			{
				return Number.TryInt64ToHexStr((long)value, Number.GetHexBase(format.Symbol), format.PrecisionOrZero, destination, out bytesWritten);
			}
		}
		ThrowHelper.ThrowFormatException_BadFormatSpecifier();
		goto IL_003f;
		IL_0075:
		if (format.HasPrecision)
		{
			ThrowGWithPrecisionNotSupported();
		}
		goto IL_003f;
		IL_003f:
		return Number.TryUInt64ToDecStr(value, format.PrecisionOrZero, destination, out bytesWritten);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool TryFormat(long value, Span<byte> destination, out int bytesWritten, StandardFormat format = default(StandardFormat))
	{
		if (format.IsDefault)
		{
			if (value < 0)
			{
				return Number.TryNegativeInt64ToDecStr(value, format.PrecisionOrZero, "-"u8, destination, out bytesWritten);
			}
			return Number.TryUInt64ToDecStr((ulong)value, destination, out bytesWritten);
		}
		int num = format.Symbol | 0x20;
		if (num <= 103)
		{
			if (num == 100)
			{
				goto IL_005f;
			}
			if (num == 103)
			{
				goto IL_00b5;
			}
		}
		else
		{
			if (num == 110)
			{
				return FormattingHelpers.TryFormat(value, destination, out bytesWritten, format);
			}
			if (num == 114)
			{
				goto IL_00b5;
			}
			if (num == 120)
			{
				return Number.TryInt64ToHexStr(value, Number.GetHexBase(format.Symbol), format.PrecisionOrZero, destination, out bytesWritten);
			}
		}
		ThrowHelper.ThrowFormatException_BadFormatSpecifier();
		goto IL_005f;
		IL_005f:
		if (value < 0)
		{
			return Number.TryNegativeInt64ToDecStr(value, format.PrecisionOrZero, "-"u8, destination, out bytesWritten);
		}
		return Number.TryUInt64ToDecStr((ulong)value, format.PrecisionOrZero, destination, out bytesWritten);
		IL_00b5:
		if (format.HasPrecision)
		{
			ThrowGWithPrecisionNotSupported();
		}
		goto IL_005f;
	}

	private static void ThrowGWithPrecisionNotSupported()
	{
		throw new NotSupportedException(SR.Argument_GWithPrecisionNotSupported);
	}

	public static bool TryFormat(TimeSpan value, Span<byte> destination, out int bytesWritten, StandardFormat format = default(StandardFormat))
	{
		TimeSpanFormat.StandardFormat format2 = TimeSpanFormat.StandardFormat.C;
		ReadOnlySpan<byte> decimalSeparator = default(ReadOnlySpan<byte>);
		char symbolOrDefault = FormattingHelpers.GetSymbolOrDefault(in format, 'c');
		if (symbolOrDefault != 'c' && (symbolOrDefault | 0x20) != 116)
		{
			decimalSeparator = DateTimeFormatInfo.InvariantInfo.DecimalSeparatorTChar<byte>();
			if (symbolOrDefault == 'g')
			{
				format2 = TimeSpanFormat.StandardFormat.g;
			}
			else
			{
				format2 = TimeSpanFormat.StandardFormat.G;
				if (symbolOrDefault != 'G')
				{
					ThrowHelper.ThrowFormatException_BadFormatSpecifier();
				}
			}
		}
		return TimeSpanFormat.TryFormatStandard(value, format2, decimalSeparator, destination, out bytesWritten);
	}
}
