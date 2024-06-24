using System.Buffers.Text;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace System.Globalization;

internal static class TimeSpanFormat
{
	internal enum StandardFormat
	{
		C,
		G,
		g
	}

	internal struct FormatLiterals
	{
		internal string AppCompatLiteral;

		internal int dd;

		internal int hh;

		internal int mm;

		internal int ss;

		internal int ff;

		private string[] _literals;

		internal string Start => _literals[0];

		internal string DayHourSep => _literals[1];

		internal string HourMinuteSep => _literals[2];

		internal string MinuteSecondSep => _literals[3];

		internal string SecondFractionSep => _literals[4];

		internal string End => _literals[5];

		internal static FormatLiterals InitInvariant(bool isNegative)
		{
			FormatLiterals result = default(FormatLiterals);
			result._literals = new string[6];
			result._literals[0] = (isNegative ? "-" : string.Empty);
			result._literals[1] = ".";
			result._literals[2] = ":";
			result._literals[3] = ":";
			result._literals[4] = ".";
			result._literals[5] = string.Empty;
			result.AppCompatLiteral = ":.";
			result.dd = 2;
			result.hh = 2;
			result.mm = 2;
			result.ss = 2;
			result.ff = 7;
			return result;
		}

		internal void Init(ReadOnlySpan<char> format, bool useInvariantFieldLengths)
		{
			dd = (hh = (mm = (ss = (ff = 0))));
			_literals = new string[6];
			for (int i = 0; i < _literals.Length; i++)
			{
				_literals[i] = string.Empty;
			}
			Span<char> initialBuffer = stackalloc char[256];
			ValueStringBuilder valueStringBuilder = new ValueStringBuilder(initialBuffer);
			bool flag = false;
			char c = '\'';
			int num = 0;
			for (int j = 0; j < format.Length; j++)
			{
				switch (format[j])
				{
				case '"':
				case '\'':
					if (flag && c == format[j])
					{
						_literals[num] = valueStringBuilder.AsSpan().ToString();
						valueStringBuilder.Length = 0;
						flag = false;
					}
					else if (!flag)
					{
						c = format[j];
						flag = true;
					}
					continue;
				case '\\':
					if (!flag)
					{
						j++;
						continue;
					}
					break;
				case 'd':
					if (!flag)
					{
						num = 1;
						dd++;
					}
					continue;
				case 'h':
					if (!flag)
					{
						num = 2;
						hh++;
					}
					continue;
				case 'm':
					if (!flag)
					{
						num = 3;
						mm++;
					}
					continue;
				case 's':
					if (!flag)
					{
						num = 4;
						ss++;
					}
					continue;
				case 'F':
				case 'f':
					if (!flag)
					{
						num = 5;
						ff++;
					}
					continue;
				}
				valueStringBuilder.Append(format[j]);
			}
			valueStringBuilder.Dispose();
			AppCompatLiteral = MinuteSecondSep + SecondFractionSep;
			if (useInvariantFieldLengths)
			{
				dd = 2;
				hh = 2;
				mm = 2;
				ss = 2;
				ff = 7;
				return;
			}
			if (dd < 1 || dd > 2)
			{
				dd = 2;
			}
			if (hh < 1 || hh > 2)
			{
				hh = 2;
			}
			if (mm < 1 || mm > 2)
			{
				mm = 2;
			}
			if (ss < 1 || ss > 2)
			{
				ss = 2;
			}
			if (ff < 1 || ff > 7)
			{
				ff = 7;
			}
		}
	}

	internal static readonly FormatLiterals PositiveInvariantFormatLiterals = FormatLiterals.InitInvariant(isNegative: false);

	internal static readonly FormatLiterals NegativeInvariantFormatLiterals = FormatLiterals.InitInvariant(isNegative: true);

	internal static string Format(TimeSpan value, string format, IFormatProvider formatProvider)
	{
		if (string.IsNullOrEmpty(format))
		{
			return FormatC(value);
		}
		if (format.Length == 1)
		{
			char c = format[0];
			if (c == 'c' || (c | 0x20) == 116)
			{
				return FormatC(value);
			}
			if ((c | 0x20) == 103)
			{
				return FormatG(value, DateTimeFormatInfo.GetInstance(formatProvider), (c == 'G') ? StandardFormat.G : StandardFormat.g);
			}
			throw new FormatException(SR.Format_InvalidString);
		}
		Span<char> initialSpan = stackalloc char[256];
		ValueListBuilder<char> result = new ValueListBuilder<char>(initialSpan);
		FormatCustomized(value, format, DateTimeFormatInfo.GetInstance(formatProvider), ref result);
		string result2 = result.AsSpan().ToString();
		result.Dispose();
		return result2;
	}

	internal static bool TryFormat<TChar>(TimeSpan value, Span<TChar> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider formatProvider) where TChar : unmanaged, IUtfChar<TChar>
	{
		if (format.Length == 0)
		{
			return TryFormatStandard(value, StandardFormat.C, null, destination, out charsWritten);
		}
		if (format.Length == 1)
		{
			char c = format[0];
			if (c == 'c' || (c | 0x20) == 116)
			{
				return TryFormatStandard(value, StandardFormat.C, null, destination, out charsWritten);
			}
			return TryFormatStandard(value, c switch
			{
				'G' => StandardFormat.G, 
				'g' => StandardFormat.g, 
				_ => throw new FormatException(SR.Format_InvalidString), 
			}, DateTimeFormatInfo.GetInstance(formatProvider).DecimalSeparatorTChar<TChar>(), destination, out charsWritten);
		}
		Span<TChar> initialSpan = stackalloc TChar[256];
		ValueListBuilder<TChar> result = new ValueListBuilder<TChar>(initialSpan);
		FormatCustomized(value, format, DateTimeFormatInfo.GetInstance(formatProvider), ref result);
		bool result2 = result.TryCopyTo(destination, out charsWritten);
		result.Dispose();
		return result2;
	}

	internal static string FormatC(TimeSpan value)
	{
		Span<char> destination = stackalloc char[26];
		TryFormatStandard(value, StandardFormat.C, null, destination, out var written);
		return new string(destination.Slice(0, written));
	}

	private static string FormatG(TimeSpan value, DateTimeFormatInfo dtfi, StandardFormat format)
	{
		string decimalSeparator = dtfi.DecimalSeparator;
		int num = 25 + decimalSeparator.Length;
		Span<char> span = ((num >= 128) ? ((Span<char>)new char[num]) : stackalloc char[num]);
		Span<char> destination = span;
		TryFormatStandard(value, format, decimalSeparator, destination, out var written);
		return new string(destination.Slice(0, written));
	}

	internal unsafe static bool TryFormatStandard<TChar>(TimeSpan value, StandardFormat format, ReadOnlySpan<TChar> decimalSeparator, Span<TChar> destination, out int written) where TChar : unmanaged, IUtfChar<TChar>
	{
		int num = 8;
		long num2 = value.Ticks;
		uint valueWithoutTrailingZeros;
		ulong num3;
		if (num2 < 0)
		{
			num = 9;
			num2 = -num2;
			if (num2 < 0)
			{
				valueWithoutTrailingZeros = 4775808u;
				num3 = 922337203685uL;
				goto IL_0050;
			}
		}
		(ulong Quotient, ulong Remainder) tuple = Math.DivRem((ulong)num2, 10000000uL);
		num3 = tuple.Quotient;
		ulong item = tuple.Remainder;
		valueWithoutTrailingZeros = (uint)item;
		goto IL_0050;
		IL_0050:
		int num4 = 0;
		switch (format)
		{
		case StandardFormat.C:
			if (valueWithoutTrailingZeros != 0)
			{
				num4 = 7;
				num += num4 + 1;
			}
			break;
		case StandardFormat.G:
			num4 = 7;
			num += num4;
			num += decimalSeparator.Length;
			break;
		default:
			if (valueWithoutTrailingZeros != 0)
			{
				num4 = 7 - FormattingHelpers.CountDecimalTrailingZeros(valueWithoutTrailingZeros, out valueWithoutTrailingZeros);
				num += num4;
				num += decimalSeparator.Length;
			}
			break;
		}
		ulong num5 = 0uL;
		ulong num6 = 0uL;
		if (num3 != 0)
		{
			(num5, num6) = Math.DivRem(num3, 60uL);
		}
		ulong num7 = 0uL;
		ulong num8 = 0uL;
		if (num5 != 0)
		{
			(num7, num8) = Math.DivRem(num5, 60uL);
		}
		uint num9 = 0u;
		uint num10 = 0u;
		if (num7 != 0)
		{
			(num9, num10) = Math.DivRem((uint)num7, 24u);
		}
		int num11 = 2;
		if (format == StandardFormat.g && num10 < 10)
		{
			num11 = 1;
			num--;
		}
		int num12 = 0;
		if (num9 != 0)
		{
			num12 = FormattingHelpers.CountDigits(num9);
			num += num12 + 1;
		}
		else if (format == StandardFormat.G)
		{
			num += 2;
			num12 = 1;
		}
		if (destination.Length < num)
		{
			written = 0;
			return false;
		}
		fixed (TChar* ptr = &MemoryMarshal.GetReference(destination))
		{
			TChar* ptr2 = ptr;
			if (value.Ticks < 0)
			{
				*(ptr2++) = TChar.CastFrom('-');
			}
			if (num12 != 0)
			{
				Number.WriteDigits(num9, ptr2, num12);
				ptr2 += num12;
				*(ptr2++) = TChar.CastFrom((format == StandardFormat.C) ? '.' : ':');
			}
			if (num11 == 2)
			{
				Number.WriteTwoDigits(num10, ptr2);
				ptr2 += 2;
			}
			else
			{
				*(ptr2++) = TChar.CastFrom(48 + num10);
			}
			*(ptr2++) = TChar.CastFrom(':');
			Number.WriteTwoDigits((uint)num8, ptr2);
			ptr2 += 2;
			*(ptr2++) = TChar.CastFrom(':');
			Number.WriteTwoDigits((uint)num6, ptr2);
			ptr2 += 2;
			if (num4 != 0)
			{
				if (format == StandardFormat.C)
				{
					*(ptr2++) = TChar.CastFrom('.');
				}
				else if (decimalSeparator.Length == 1)
				{
					*(ptr2++) = decimalSeparator[0];
				}
				else
				{
					decimalSeparator.CopyTo(new Span<TChar>(ptr2, decimalSeparator.Length));
					ptr2 += decimalSeparator.Length;
				}
				Number.WriteDigits(valueWithoutTrailingZeros, ptr2, num4);
				ptr2 += num4;
			}
		}
		written = num;
		return true;
	}

	private static void FormatCustomized<TChar>(TimeSpan value, scoped ReadOnlySpan<char> format, DateTimeFormatInfo dtfi, ref ValueListBuilder<TChar> result) where TChar : unmanaged, IUtfChar<TChar>
	{
		int num = (int)(value.Ticks / 864000000000L);
		long num2 = value.Ticks % 864000000000L;
		if (value.Ticks < 0)
		{
			num = -num;
			num2 = -num2;
		}
		int value2 = (int)(num2 / 36000000000L % 24);
		int value3 = (int)(num2 / 600000000 % 60);
		int value4 = (int)(num2 / 10000000 % 60);
		int num3 = (int)(num2 % 10000000);
		int num5;
		for (int i = 0; i < format.Length; i += num5)
		{
			char c = format[i];
			switch (c)
			{
			case 'h':
				num5 = DateTimeFormat.ParseRepeatPattern(format, i, c);
				if (num5 <= 2)
				{
					DateTimeFormat.FormatDigits(ref result, value2, num5);
					continue;
				}
				break;
			case 'm':
				num5 = DateTimeFormat.ParseRepeatPattern(format, i, c);
				if (num5 <= 2)
				{
					DateTimeFormat.FormatDigits(ref result, value3, num5);
					continue;
				}
				break;
			case 's':
				num5 = DateTimeFormat.ParseRepeatPattern(format, i, c);
				if (num5 <= 2)
				{
					DateTimeFormat.FormatDigits(ref result, value4, num5);
					continue;
				}
				break;
			case 'f':
				num5 = DateTimeFormat.ParseRepeatPattern(format, i, c);
				if (num5 <= 7)
				{
					long num6 = num3;
					num6 /= TimeSpanParse.Pow10(7 - num5);
					DateTimeFormat.FormatFraction(ref result, (int)num6, DateTimeFormat.fixedNumberFormats[num5 - 1]);
					continue;
				}
				break;
			case 'F':
				num5 = DateTimeFormat.ParseRepeatPattern(format, i, c);
				if (num5 <= 7)
				{
					long num6 = num3;
					num6 /= TimeSpanParse.Pow10(7 - num5);
					int num7 = num5;
					while (num7 > 0 && num6 % 10 == 0L)
					{
						num6 /= 10;
						num7--;
					}
					if (num7 > 0)
					{
						DateTimeFormat.FormatFraction(ref result, (int)num6, DateTimeFormat.fixedNumberFormats[num7 - 1]);
					}
					continue;
				}
				break;
			case 'd':
				num5 = DateTimeFormat.ParseRepeatPattern(format, i, c);
				if (num5 <= 8)
				{
					DateTimeFormat.FormatDigits(ref result, num, num5);
					continue;
				}
				break;
			case '"':
			case '\'':
				num5 = DateTimeFormat.ParseQuoteString(format, i, ref result);
				continue;
			case '%':
			{
				int num4 = DateTimeFormat.ParseNextChar(format, i);
				if (num4 >= 0 && num4 != 37)
				{
					char reference = (char)num4;
					FormatCustomized(value, new ReadOnlySpan<char>(ref reference), dtfi, ref result);
					num5 = 2;
					continue;
				}
				break;
			}
			case '\\':
			{
				int num4 = DateTimeFormat.ParseNextChar(format, i);
				if (num4 >= 0)
				{
					result.Append(TChar.CastFrom(num4));
					num5 = 2;
					continue;
				}
				break;
			}
			}
			throw new FormatException(SR.Format_InvalidString);
		}
	}
}
