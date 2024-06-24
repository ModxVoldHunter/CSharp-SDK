using System.Collections.Generic;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace System;

internal static class DateTimeFormat
{
	internal static readonly DateTimeFormatInfo InvariantFormatInfo = CultureInfo.InvariantCulture.DateTimeFormat;

	private static readonly string[] s_invariantAbbreviatedMonthNames = InvariantFormatInfo.AbbreviatedMonthNames;

	private static readonly string[] s_invariantAbbreviatedDayNames = InvariantFormatInfo.AbbreviatedDayNames;

	internal static string[] fixedNumberFormats = new string[7] { "0", "00", "000", "0000", "00000", "000000", "0000000" };

	internal unsafe static void FormatDigits<TChar>(ref ValueListBuilder<TChar> outputBuffer, int value, int minimumLength) where TChar : unmanaged, IUtfChar<TChar>
	{
		switch (minimumLength)
		{
		case 1:
			if (value < 10)
			{
				outputBuffer.Append(TChar.CreateTruncating(value + 48));
				return;
			}
			break;
		case 2:
			if (value < 100)
			{
				fixed (TChar* ptr2 = &MemoryMarshal.GetReference(outputBuffer.AppendSpan(2)))
				{
					Number.WriteTwoDigits((uint)value, ptr2);
				}
				return;
			}
			break;
		case 4:
			if (value < 10000)
			{
				fixed (TChar* ptr = &MemoryMarshal.GetReference(outputBuffer.AppendSpan(4)))
				{
					Number.WriteFourDigits((uint)value, ptr);
				}
				return;
			}
			break;
		}
		TChar* ptr3 = stackalloc TChar[16];
		TChar* ptr4 = Number.UInt32ToDecChars(ptr3 + 16, (uint)value, minimumLength);
		outputBuffer.Append(new ReadOnlySpan<TChar>(ptr4, (int)(ptr3 + 16 - ptr4)));
	}

	internal static int ParseRepeatPattern(ReadOnlySpan<char> format, int pos, char patternChar)
	{
		int i;
		for (i = pos + 1; (uint)i < (uint)format.Length && format[i] == patternChar; i++)
		{
		}
		return i - pos;
	}

	private static string FormatDayOfWeek(int dayOfWeek, int repeat, DateTimeFormatInfo dtfi)
	{
		if (repeat == 3)
		{
			return dtfi.GetAbbreviatedDayName((DayOfWeek)dayOfWeek);
		}
		return dtfi.GetDayName((DayOfWeek)dayOfWeek);
	}

	private static string FormatMonth(int month, int repeatCount, DateTimeFormatInfo dtfi)
	{
		if (repeatCount == 3)
		{
			return dtfi.GetAbbreviatedMonthName(month);
		}
		return dtfi.GetMonthName(month);
	}

	private static string FormatHebrewMonthName(DateTime time, int month, int repeatCount, DateTimeFormatInfo dtfi)
	{
		if (dtfi.Calendar.IsLeapYear(dtfi.Calendar.GetYear(time)))
		{
			return dtfi.InternalGetMonthName(month, MonthNameStyles.LeapYear, repeatCount == 3);
		}
		if (month >= 7)
		{
			month++;
		}
		if (repeatCount == 3)
		{
			return dtfi.GetAbbreviatedMonthName(month);
		}
		return dtfi.GetMonthName(month);
	}

	internal static int ParseQuoteString<TChar>(scoped ReadOnlySpan<char> format, int pos, ref ValueListBuilder<TChar> result) where TChar : unmanaged, IUtfChar<TChar>
	{
		int length = format.Length;
		int num = pos;
		char c = format[pos++];
		bool flag = false;
		while (pos < length)
		{
			char c2 = format[pos++];
			if (c2 == c)
			{
				flag = true;
				break;
			}
			if (c2 == '\\')
			{
				if (pos >= length)
				{
					throw new FormatException(SR.Format_InvalidString);
				}
				result.Append(TChar.CastFrom(format[pos++]));
			}
			else
			{
				AppendChar(ref result, c2);
			}
		}
		if (!flag)
		{
			throw new FormatException(SR.Format(SR.Format_BadQuote, c));
		}
		return pos - num;
	}

	internal static int ParseNextChar(ReadOnlySpan<char> format, int pos)
	{
		if ((uint)(pos + 1) >= (uint)format.Length)
		{
			return -1;
		}
		return format[pos + 1];
	}

	private static bool IsUseGenitiveForm(ReadOnlySpan<char> format, int index, int tokenLen, char patternToMatch)
	{
		int num = 0;
		int num2 = index - 1;
		while (num2 >= 0 && format[num2] != patternToMatch)
		{
			num2--;
		}
		if (num2 >= 0)
		{
			while (--num2 >= 0 && format[num2] == patternToMatch)
			{
				num++;
			}
			if (num <= 1)
			{
				return true;
			}
		}
		for (num2 = index + tokenLen; num2 < format.Length && format[num2] != patternToMatch; num2++)
		{
		}
		if (num2 < format.Length)
		{
			num = 0;
			while (++num2 < format.Length && format[num2] == patternToMatch)
			{
				num++;
			}
			if (num <= 1)
			{
				return true;
			}
		}
		return false;
	}

	private static void FormatCustomized<TChar>(DateTime dateTime, scoped ReadOnlySpan<char> format, DateTimeFormatInfo dtfi, TimeSpan offset, ref ValueListBuilder<TChar> result) where TChar : unmanaged, IUtfChar<TChar>
	{
		Calendar calendar = dtfi.Calendar;
		bool flag = calendar.ID == CalendarId.HEBREW;
		bool flag2 = calendar.ID == CalendarId.JAPAN;
		bool timeOnly = true;
		int num;
		for (int i = 0; i < format.Length; i += num)
		{
			char c = format[i];
			switch (c)
			{
			case 'g':
				num = ParseRepeatPattern(format, i, c);
				AppendString(ref result, dtfi.GetEraName(calendar.GetEra(dateTime)));
				break;
			case 'h':
			{
				num = ParseRepeatPattern(format, i, c);
				int num5 = dateTime.Hour;
				if (num5 > 12)
				{
					num5 -= 12;
				}
				else if (num5 == 0)
				{
					num5 = 12;
				}
				FormatDigits(ref result, num5, Math.Min(num, 2));
				break;
			}
			case 'H':
				num = ParseRepeatPattern(format, i, c);
				FormatDigits(ref result, dateTime.Hour, Math.Min(num, 2));
				break;
			case 'm':
				num = ParseRepeatPattern(format, i, c);
				FormatDigits(ref result, dateTime.Minute, Math.Min(num, 2));
				break;
			case 's':
				num = ParseRepeatPattern(format, i, c);
				FormatDigits(ref result, dateTime.Second, Math.Min(num, 2));
				break;
			case 'F':
			case 'f':
				num = ParseRepeatPattern(format, i, c);
				if (num <= 7)
				{
					long num3 = dateTime.Ticks % 10000000;
					num3 /= (long)Math.Pow(10.0, 7 - num);
					if (c == 'f')
					{
						FormatFraction(ref result, (int)num3, fixedNumberFormats[num - 1]);
						break;
					}
					int num4 = num;
					while (num4 > 0 && num3 % 10 == 0L)
					{
						num3 /= 10;
						num4--;
					}
					if (num4 > 0)
					{
						FormatFraction(ref result, (int)num3, fixedNumberFormats[num4 - 1]);
					}
					else if (result.Length > 0)
					{
						if (result[result.Length - 1] == TChar.CastFrom('.'))
						{
							result.Length--;
						}
					}
					break;
				}
				throw new FormatException(SR.Format_InvalidString);
			case 't':
				num = ParseRepeatPattern(format, i, c);
				if (num == 1)
				{
					string text = ((dateTime.Hour < 12) ? dtfi.AMDesignator : dtfi.PMDesignator);
					if (text.Length >= 1)
					{
						AppendChar(ref result, text[0]);
					}
				}
				else
				{
					result.Append((dateTime.Hour < 12) ? dtfi.AMDesignatorTChar<TChar>() : dtfi.PMDesignatorTChar<TChar>());
				}
				break;
			case 'd':
				num = ParseRepeatPattern(format, i, c);
				if (num <= 2)
				{
					int dayOfMonth = calendar.GetDayOfMonth(dateTime);
					if (flag)
					{
						HebrewNumber.Append(ref result, dayOfMonth);
					}
					else
					{
						FormatDigits(ref result, dayOfMonth, num);
					}
				}
				else
				{
					int dayOfWeek = (int)calendar.GetDayOfWeek(dateTime);
					AppendString(ref result, FormatDayOfWeek(dayOfWeek, num, dtfi));
				}
				timeOnly = false;
				break;
			case 'M':
			{
				num = ParseRepeatPattern(format, i, c);
				int month = calendar.GetMonth(dateTime);
				if (num <= 2)
				{
					if (flag)
					{
						HebrewNumber.Append(ref result, month);
					}
					else
					{
						FormatDigits(ref result, month, num);
					}
				}
				else if (flag)
				{
					AppendString(ref result, FormatHebrewMonthName(dateTime, month, num, dtfi));
				}
				else if ((dtfi.FormatFlags & DateTimeFormatFlags.UseGenitiveMonth) != 0)
				{
					AppendString(ref result, dtfi.InternalGetMonthName(month, IsUseGenitiveForm(format, i, num, 'd') ? MonthNameStyles.Genitive : MonthNameStyles.Regular, num == 3));
				}
				else
				{
					AppendString(ref result, FormatMonth(month, num, dtfi));
				}
				timeOnly = false;
				break;
			}
			case 'y':
			{
				int year = calendar.GetYear(dateTime);
				num = ParseRepeatPattern(format, i, c);
				if (flag2 && !LocalAppContextSwitches.FormatJapaneseFirstYearAsANumber && year == 1 && ((i + num < format.Length && format[i + num] == '年') || (i + num < format.Length - 1 && format[i + num] == '\'' && format[i + num + 1] == '年')))
				{
					AppendChar(ref result, "元"[0]);
				}
				else if (dtfi.HasForceTwoDigitYears)
				{
					FormatDigits(ref result, year, Math.Min(num, 2));
				}
				else if (calendar.ID == CalendarId.HEBREW)
				{
					HebrewNumber.Append(ref result, year);
				}
				else if (num <= 2)
				{
					FormatDigits(ref result, year % 100, num);
				}
				else if (num <= 16)
				{
					FormatDigits(ref result, year, num);
				}
				else
				{
					AppendString(ref result, year.ToString("D" + num.ToString(CultureInfo.InvariantCulture), CultureInfo.InvariantCulture));
				}
				timeOnly = false;
				break;
			}
			case 'z':
				num = ParseRepeatPattern(format, i, c);
				FormatCustomizedTimeZone(dateTime, offset, num, timeOnly, ref result);
				break;
			case 'K':
				num = 1;
				FormatCustomizedRoundripTimeZone(dateTime, offset, ref result);
				break;
			case ':':
				result.Append(dtfi.TimeSeparatorTChar<TChar>());
				num = 1;
				break;
			case '/':
				result.Append(dtfi.DateSeparatorTChar<TChar>());
				num = 1;
				break;
			case '"':
			case '\'':
				num = ParseQuoteString(format, i, ref result);
				break;
			case '%':
			{
				int num2 = ParseNextChar(format, i);
				if (num2 >= 0 && num2 != 37)
				{
					char reference = (char)num2;
					FormatCustomized(dateTime, new ReadOnlySpan<char>(ref reference), dtfi, offset, ref result);
					num = 2;
					break;
				}
				throw new FormatException(SR.Format_InvalidString);
			}
			case '\\':
			{
				int num2 = ParseNextChar(format, i);
				if (num2 >= 0)
				{
					result.Append(TChar.CastFrom(num2));
					num = 2;
					break;
				}
				throw new FormatException(SR.Format_InvalidString);
			}
			default:
				AppendChar(ref result, c);
				num = 1;
				break;
			}
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static void AppendChar<TChar>(ref ValueListBuilder<TChar> result, char ch) where TChar : unmanaged, IUtfChar<TChar>
	{
		if (typeof(TChar) == typeof(char) || char.IsAscii(ch))
		{
			result.Append(TChar.CastFrom(ch));
			return;
		}
		Rune rune = new Rune(ch);
		rune.EncodeToUtf8(MemoryMarshal.AsBytes(result.AppendSpan(rune.Utf8SequenceLength)));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static void AppendString<TChar>(ref ValueListBuilder<TChar> result, scoped ReadOnlySpan<char> s) where TChar : unmanaged, IUtfChar<TChar>
	{
		if (typeof(TChar) == typeof(char))
		{
			result.Append(MemoryMarshal.Cast<char, TChar>(s));
		}
		else
		{
			Encoding.UTF8.GetBytes(s, MemoryMarshal.Cast<TChar, byte>(result.AppendSpan(Encoding.UTF8.GetByteCount(s))));
		}
	}

	internal static void FormatFraction<TChar>(ref ValueListBuilder<TChar> result, int fraction, ReadOnlySpan<char> fractionFormat) where TChar : unmanaged, IUtfChar<TChar>
	{
		Span<TChar> span = stackalloc TChar[11];
		int charsWritten;
		bool flag = ((typeof(TChar) == typeof(char)) ? fraction.TryFormat(MemoryMarshal.Cast<TChar, char>(span), out charsWritten, fractionFormat, CultureInfo.InvariantCulture) : fraction.TryFormat(MemoryMarshal.Cast<TChar, byte>(span), out charsWritten, fractionFormat, CultureInfo.InvariantCulture));
		result.Append(span.Slice(0, charsWritten));
	}

	private unsafe static void FormatCustomizedTimeZone<TChar>(DateTime dateTime, TimeSpan offset, int tokenLen, bool timeOnly, ref ValueListBuilder<TChar> result) where TChar : unmanaged, IUtfChar<TChar>
	{
		if (offset.Ticks == long.MinValue)
		{
			offset = ((timeOnly && dateTime.Ticks < 864000000000L) ? TimeZoneInfo.GetLocalUtcOffset(DateTime.Now, TimeZoneInfoOptions.NoThrowOnInvalidTime) : ((dateTime.Kind != DateTimeKind.Utc) ? TimeZoneInfo.GetLocalUtcOffset(dateTime, TimeZoneInfoOptions.NoThrowOnInvalidTime) : default(TimeSpan)));
		}
		if (offset.Ticks >= 0)
		{
			result.Append(TChar.CastFrom('+'));
		}
		else
		{
			result.Append(TChar.CastFrom('-'));
			offset = offset.Negate();
		}
		if (tokenLen <= 1)
		{
			var (num, num2) = Math.DivRem(offset.Hours, 10);
			if (num != 0)
			{
				result.Append(TChar.CastFrom(48 + num));
			}
			result.Append(TChar.CastFrom(48 + num2));
		}
		else if (tokenLen == 2)
		{
			fixed (TChar* ptr = &MemoryMarshal.GetReference(result.AppendSpan(2)))
			{
				Number.WriteTwoDigits((uint)offset.Hours, ptr);
			}
		}
		else
		{
			fixed (TChar* ptr2 = &MemoryMarshal.GetReference(result.AppendSpan(5)))
			{
				Number.WriteTwoDigits((uint)offset.Hours, ptr2);
				ptr2[2] = TChar.CastFrom(':');
				Number.WriteTwoDigits((uint)offset.Minutes, ptr2 + 3);
			}
		}
	}

	private unsafe static void FormatCustomizedRoundripTimeZone<TChar>(DateTime dateTime, TimeSpan offset, ref ValueListBuilder<TChar> result) where TChar : unmanaged, IUtfChar<TChar>
	{
		if (offset.Ticks == long.MinValue)
		{
			switch (dateTime.Kind)
			{
			case DateTimeKind.Local:
				break;
			case DateTimeKind.Utc:
				result.Append(TChar.CastFrom('Z'));
				return;
			default:
				return;
			}
			offset = TimeZoneInfo.GetLocalUtcOffset(dateTime, TimeZoneInfoOptions.NoThrowOnInvalidTime);
		}
		if (offset.Ticks >= 0)
		{
			result.Append(TChar.CastFrom('+'));
		}
		else
		{
			result.Append(TChar.CastFrom('-'));
			offset = offset.Negate();
		}
		fixed (TChar* ptr = &MemoryMarshal.GetReference(result.AppendSpan(5)))
		{
			Number.WriteTwoDigits((uint)offset.Hours, ptr);
			ptr[2] = TChar.CastFrom(':');
			Number.WriteTwoDigits((uint)offset.Minutes, ptr + 3);
		}
	}

	internal static string ExpandStandardFormatToCustomPattern(char format, DateTimeFormatInfo dtfi)
	{
		switch (format)
		{
		case 'd':
			return dtfi.ShortDatePattern;
		case 'D':
			return dtfi.LongDatePattern;
		case 'f':
			return dtfi.LongDatePattern + " " + dtfi.ShortTimePattern;
		case 'F':
			return dtfi.FullDateTimePattern;
		case 'g':
			return dtfi.GeneralShortTimePattern;
		case 'G':
			return dtfi.GeneralLongTimePattern;
		case 'M':
		case 'm':
			return dtfi.MonthDayPattern;
		case 'O':
		case 'o':
			return "yyyy'-'MM'-'dd'T'HH':'mm':'ss.fffffffK";
		case 'R':
		case 'r':
			return dtfi.RFC1123Pattern;
		case 's':
			return dtfi.SortableDateTimePattern;
		case 't':
			return dtfi.ShortTimePattern;
		case 'T':
			return dtfi.LongTimePattern;
		case 'u':
			return dtfi.UniversalSortableDateTimePattern;
		case 'U':
			return dtfi.FullDateTimePattern;
		case 'Y':
		case 'y':
			return dtfi.YearMonthPattern;
		default:
			throw new FormatException(SR.Format_InvalidString);
		}
	}

	internal static string Format(DateTime dateTime, string format, IFormatProvider provider)
	{
		return Format(dateTime, format, provider, new TimeSpan(long.MinValue));
	}

	internal static string Format(DateTime dateTime, string format, IFormatProvider provider, TimeSpan offset)
	{
		DateTimeFormatInfo dtfi;
		if (string.IsNullOrEmpty(format))
		{
			dtfi = DateTimeFormatInfo.GetInstance(provider);
			if (offset.Ticks == long.MinValue)
			{
				if (IsTimeOnlySpecialCase(dateTime, dtfi))
				{
					string text = string.FastAllocateString(19);
					TryFormatS(dateTime, new Span<char>(ref text.GetRawStringData(), text.Length), out var _);
					return text;
				}
				if (dtfi == DateTimeFormatInfo.InvariantInfo)
				{
					string text2 = string.FastAllocateString(19);
					TryFormatInvariantG(dateTime, offset, new Span<char>(ref text2.GetRawStringData(), text2.Length), out var _);
					return text2;
				}
				format = dtfi.GeneralLongTimePattern;
			}
			else if (IsTimeOnlySpecialCase(dateTime, dtfi))
			{
				format = "yyyy'-'MM'-'ddTHH':'mm':'ss zzz";
				dtfi = DateTimeFormatInfo.InvariantInfo;
			}
			else
			{
				if (dtfi == DateTimeFormatInfo.InvariantInfo)
				{
					string text3 = string.FastAllocateString(26);
					TryFormatInvariantG(dateTime, offset, new Span<char>(ref text3.GetRawStringData(), text3.Length), out var _);
					return text3;
				}
				format = dtfi.DateTimeOffsetPattern;
			}
		}
		else if (format.Length == 1)
		{
			int charsWritten2;
			switch (format[0])
			{
			case 'O':
			case 'o':
			{
				Span<char> destination = stackalloc char[33];
				TryFormatO(dateTime, offset, destination, out charsWritten2);
				return destination.Slice(0, charsWritten2).ToString();
			}
			case 'R':
			case 'r':
			{
				string text4 = string.FastAllocateString(29);
				TryFormatR(dateTime, offset, new Span<char>(ref text4.GetRawStringData(), text4.Length), out charsWritten2);
				return text4;
			}
			case 's':
			{
				string text4 = string.FastAllocateString(19);
				TryFormatS(dateTime, new Span<char>(ref text4.GetRawStringData(), text4.Length), out charsWritten2);
				return text4;
			}
			case 'u':
			{
				string text4 = string.FastAllocateString(20);
				TryFormatu(dateTime, offset, new Span<char>(ref text4.GetRawStringData(), text4.Length), out charsWritten2);
				return text4;
			}
			case 'U':
				dtfi = DateTimeFormatInfo.GetInstance(provider);
				PrepareFormatU(ref dateTime, ref dtfi, offset);
				format = dtfi.FullDateTimePattern;
				break;
			default:
				dtfi = DateTimeFormatInfo.GetInstance(provider);
				format = ExpandStandardFormatToCustomPattern(format[0], dtfi);
				break;
			}
		}
		else
		{
			dtfi = DateTimeFormatInfo.GetInstance(provider);
		}
		Span<char> initialSpan = stackalloc char[256];
		ValueListBuilder<char> result = new ValueListBuilder<char>(initialSpan);
		FormatCustomized(dateTime, format, dtfi, offset, ref result);
		string result2 = result.AsSpan().ToString();
		result.Dispose();
		return result2;
	}

	internal static bool TryFormat<TChar>(DateTime dateTime, Span<TChar> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider provider) where TChar : unmanaged, IUtfChar<TChar>
	{
		return TryFormat(dateTime, destination, out charsWritten, format, provider, new TimeSpan(long.MinValue));
	}

	internal static bool TryFormat<TChar>(DateTime dateTime, Span<TChar> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider provider, TimeSpan offset) where TChar : unmanaged, IUtfChar<TChar>
	{
		DateTimeFormatInfo dtfi;
		if (format.IsEmpty)
		{
			dtfi = DateTimeFormatInfo.GetInstance(provider);
			if (offset.Ticks == long.MinValue)
			{
				if (IsTimeOnlySpecialCase(dateTime, dtfi))
				{
					return TryFormatS(dateTime, destination, out charsWritten);
				}
				if (dtfi == DateTimeFormatInfo.InvariantInfo)
				{
					return TryFormatInvariantG(dateTime, offset, destination, out charsWritten);
				}
				format = dtfi.GeneralLongTimePattern;
			}
			else if (IsTimeOnlySpecialCase(dateTime, dtfi))
			{
				format = "yyyy'-'MM'-'ddTHH':'mm':'ss zzz";
				dtfi = DateTimeFormatInfo.InvariantInfo;
			}
			else
			{
				if (dtfi == DateTimeFormatInfo.InvariantInfo)
				{
					return TryFormatInvariantG(dateTime, offset, destination, out charsWritten);
				}
				format = dtfi.DateTimeOffsetPattern;
			}
		}
		else if (format.Length == 1)
		{
			switch (format[0])
			{
			case 'O':
			case 'o':
				return TryFormatO(dateTime, offset, destination, out charsWritten);
			case 'R':
			case 'r':
				return TryFormatR(dateTime, offset, destination, out charsWritten);
			case 's':
				return TryFormatS(dateTime, destination, out charsWritten);
			case 'u':
				return TryFormatu(dateTime, offset, destination, out charsWritten);
			case 'U':
				dtfi = DateTimeFormatInfo.GetInstance(provider);
				PrepareFormatU(ref dateTime, ref dtfi, offset);
				format = dtfi.FullDateTimePattern;
				break;
			default:
				dtfi = DateTimeFormatInfo.GetInstance(provider);
				format = ExpandStandardFormatToCustomPattern(format[0], dtfi);
				break;
			}
		}
		else
		{
			dtfi = DateTimeFormatInfo.GetInstance(provider);
		}
		ValueListBuilder<TChar> result = new ValueListBuilder<TChar>(destination);
		FormatCustomized(dateTime, format, dtfi, offset, ref result);
		bool flag = Unsafe.AreSame(ref MemoryMarshal.GetReference(destination), ref MemoryMarshal.GetReference(result.AsSpan()));
		if (flag)
		{
			charsWritten = result.Length;
		}
		else
		{
			flag = result.TryCopyTo(destination, out charsWritten);
		}
		result.Dispose();
		return flag;
	}

	private static bool IsTimeOnlySpecialCase(DateTime dateTime, DateTimeFormatInfo dtfi)
	{
		bool flag = dateTime.Ticks < 864000000000L;
		bool flag2 = flag;
		if (flag2)
		{
			bool flag3;
			switch (dtfi.Calendar.ID)
			{
			case CalendarId.JAPAN:
			case CalendarId.TAIWAN:
			case CalendarId.HIJRI:
			case CalendarId.HEBREW:
			case CalendarId.JULIAN:
			case CalendarId.PERSIAN:
			case CalendarId.UMALQURA:
				flag3 = true;
				break;
			default:
				flag3 = false;
				break;
			}
			flag2 = flag3;
		}
		return flag2;
	}

	private static void PrepareFormatU(ref DateTime dateTime, ref DateTimeFormatInfo dtfi, TimeSpan offset)
	{
		if (offset.Ticks != long.MinValue)
		{
			throw new FormatException(SR.Format_InvalidString);
		}
		if (dtfi.Calendar.GetType() != typeof(GregorianCalendar))
		{
			dtfi = (DateTimeFormatInfo)dtfi.Clone();
			dtfi.Calendar = GregorianCalendar.GetDefaultInstance();
		}
		dateTime = dateTime.ToUniversalTime();
	}

	internal static bool IsValidCustomDateOnlyFormat(ReadOnlySpan<char> format, bool throwOnError)
	{
		int i = 0;
		while (i < format.Length)
		{
			switch (format[i])
			{
			case '\\':
				if (i == format.Length - 1)
				{
					if (throwOnError)
					{
						throw new FormatException(SR.Format_InvalidString);
					}
					return false;
				}
				i += 2;
				break;
			case '"':
			case '\'':
			{
				char c;
				for (c = format[i++]; i < format.Length && format[i] != c; i++)
				{
				}
				if (i >= format.Length)
				{
					if (throwOnError)
					{
						throw new FormatException(SR.Format(SR.Format_BadQuote, c));
					}
					return false;
				}
				i++;
				break;
			}
			case ':':
			case 'F':
			case 'H':
			case 'K':
			case 'f':
			case 'h':
			case 'm':
			case 's':
			case 't':
			case 'z':
				if (throwOnError)
				{
					throw new FormatException(SR.Format_InvalidString);
				}
				return false;
			default:
				i++;
				break;
			}
		}
		return true;
	}

	internal static bool IsValidCustomTimeOnlyFormat(ReadOnlySpan<char> format, bool throwOnError)
	{
		int length = format.Length;
		int i = 0;
		while (i < length)
		{
			switch (format[i])
			{
			case '\\':
				if (i == length - 1)
				{
					if (throwOnError)
					{
						throw new FormatException(SR.Format_InvalidString);
					}
					return false;
				}
				i += 2;
				break;
			case '"':
			case '\'':
			{
				char c;
				for (c = format[i++]; i < length && format[i] != c; i++)
				{
				}
				if (i >= length)
				{
					if (throwOnError)
					{
						throw new FormatException(SR.Format(SR.Format_BadQuote, c));
					}
					return false;
				}
				i++;
				break;
			}
			case '/':
			case 'M':
			case 'd':
			case 'k':
			case 'y':
			case 'z':
				if (throwOnError)
				{
					throw new FormatException(SR.Format_InvalidString);
				}
				return false;
			default:
				i++;
				break;
			}
		}
		return true;
	}

	internal unsafe static bool TryFormatTimeOnlyO<TChar>(int hour, int minute, int second, long fraction, Span<TChar> destination, out int charsWritten) where TChar : unmanaged, IUtfChar<TChar>
	{
		if (destination.Length < 16)
		{
			charsWritten = 0;
			return false;
		}
		charsWritten = 16;
		fixed (TChar* ptr = &MemoryMarshal.GetReference(destination))
		{
			Number.WriteTwoDigits((uint)hour, ptr);
			ptr[2] = TChar.CastFrom(':');
			Number.WriteTwoDigits((uint)minute, ptr + 3);
			ptr[5] = TChar.CastFrom(':');
			Number.WriteTwoDigits((uint)second, ptr + 6);
			ptr[8] = TChar.CastFrom('.');
			Number.WriteDigits((uint)fraction, ptr + 9, 7);
		}
		return true;
	}

	internal unsafe static bool TryFormatTimeOnlyR<TChar>(int hour, int minute, int second, Span<TChar> destination, out int charsWritten) where TChar : unmanaged, IUtfChar<TChar>
	{
		if (destination.Length < 8)
		{
			charsWritten = 0;
			return false;
		}
		charsWritten = 8;
		fixed (TChar* ptr = &MemoryMarshal.GetReference(destination))
		{
			Number.WriteTwoDigits((uint)hour, ptr);
			ptr[2] = TChar.CastFrom(':');
			Number.WriteTwoDigits((uint)minute, ptr + 3);
			ptr[5] = TChar.CastFrom(':');
			Number.WriteTwoDigits((uint)second, ptr + 6);
		}
		return true;
	}

	internal unsafe static bool TryFormatDateOnlyO<TChar>(int year, int month, int day, Span<TChar> destination, out int charsWritten) where TChar : unmanaged, IUtfChar<TChar>
	{
		if (destination.Length < 10)
		{
			charsWritten = 0;
			return false;
		}
		charsWritten = 10;
		fixed (TChar* ptr = &MemoryMarshal.GetReference(destination))
		{
			Number.WriteFourDigits((uint)year, ptr);
			ptr[4] = TChar.CastFrom('-');
			Number.WriteTwoDigits((uint)month, ptr + 5);
			ptr[7] = TChar.CastFrom('-');
			Number.WriteTwoDigits((uint)day, ptr + 8);
		}
		return true;
	}

	internal unsafe static bool TryFormatDateOnlyR<TChar>(DayOfWeek dayOfWeek, int year, int month, int day, Span<TChar> destination, out int charsWritten) where TChar : unmanaged, IUtfChar<TChar>
	{
		if (destination.Length < 16)
		{
			charsWritten = 0;
			return false;
		}
		charsWritten = 16;
		string text = s_invariantAbbreviatedDayNames[(int)dayOfWeek];
		string text2 = s_invariantAbbreviatedMonthNames[month - 1];
		fixed (TChar* ptr = &MemoryMarshal.GetReference(destination))
		{
			char value = text[2];
			*ptr = TChar.CastFrom(text[0]);
			ptr[1] = TChar.CastFrom(text[1]);
			ptr[2] = TChar.CastFrom(value);
			ptr[3] = TChar.CastFrom(',');
			ptr[4] = TChar.CastFrom(' ');
			Number.WriteTwoDigits((uint)day, ptr + 5);
			ptr[7] = TChar.CastFrom(' ');
			value = text2[2];
			ptr[8] = TChar.CastFrom(text2[0]);
			ptr[9] = TChar.CastFrom(text2[1]);
			ptr[10] = TChar.CastFrom(value);
			ptr[11] = TChar.CastFrom(' ');
			Number.WriteFourDigits((uint)year, ptr + 12);
		}
		return true;
	}

	internal unsafe static bool TryFormatO<TChar>(DateTime dateTime, TimeSpan offset, Span<TChar> destination, out int charsWritten) where TChar : unmanaged, IUtfChar<TChar>
	{
		int num = 27;
		DateTimeKind dateTimeKind = DateTimeKind.Local;
		if (offset.Ticks == long.MinValue)
		{
			dateTimeKind = dateTime.Kind;
			switch (dateTimeKind)
			{
			case DateTimeKind.Local:
				offset = TimeZoneInfo.Local.GetUtcOffset(dateTime);
				num += 6;
				break;
			case DateTimeKind.Utc:
				num++;
				break;
			}
		}
		else
		{
			num += 6;
		}
		if (destination.Length < num)
		{
			charsWritten = 0;
			return false;
		}
		charsWritten = num;
		dateTime.GetDate(out var year, out var month, out var day);
		dateTime.GetTimePrecise(out var hour, out var minute, out var second, out var tick);
		fixed (TChar* ptr = &MemoryMarshal.GetReference(destination))
		{
			Number.WriteFourDigits((uint)year, ptr);
			ptr[4] = TChar.CastFrom('-');
			Number.WriteTwoDigits((uint)month, ptr + 5);
			ptr[7] = TChar.CastFrom('-');
			Number.WriteTwoDigits((uint)day, ptr + 8);
			ptr[10] = TChar.CastFrom('T');
			Number.WriteTwoDigits((uint)hour, ptr + 11);
			ptr[13] = TChar.CastFrom(':');
			Number.WriteTwoDigits((uint)minute, ptr + 14);
			ptr[16] = TChar.CastFrom(':');
			Number.WriteTwoDigits((uint)second, ptr + 17);
			ptr[19] = TChar.CastFrom('.');
			Number.WriteDigits((uint)tick, ptr + 20, 7);
			switch (dateTimeKind)
			{
			case DateTimeKind.Local:
			{
				int num2 = (int)(offset.Ticks / 600000000);
				char value = '+';
				if (num2 < 0)
				{
					value = '-';
					num2 = -num2;
				}
				var (value2, value3) = Math.DivRem(num2, 60);
				ptr[27] = TChar.CastFrom(value);
				Number.WriteTwoDigits((uint)value2, ptr + 28);
				ptr[30] = TChar.CastFrom(':');
				Number.WriteTwoDigits((uint)value3, ptr + 31);
				break;
			}
			case DateTimeKind.Utc:
				ptr[27] = TChar.CastFrom('Z');
				break;
			}
		}
		return true;
	}

	internal unsafe static bool TryFormatS<TChar>(DateTime dateTime, Span<TChar> destination, out int charsWritten) where TChar : unmanaged, IUtfChar<TChar>
	{
		if (destination.Length < 19)
		{
			charsWritten = 0;
			return false;
		}
		charsWritten = 19;
		dateTime.GetDate(out var year, out var month, out var day);
		dateTime.GetTime(out var hour, out var minute, out var second);
		fixed (TChar* ptr = &MemoryMarshal.GetReference(destination))
		{
			Number.WriteFourDigits((uint)year, ptr);
			ptr[4] = TChar.CastFrom('-');
			Number.WriteTwoDigits((uint)month, ptr + 5);
			ptr[7] = TChar.CastFrom('-');
			Number.WriteTwoDigits((uint)day, ptr + 8);
			ptr[10] = TChar.CastFrom('T');
			Number.WriteTwoDigits((uint)hour, ptr + 11);
			ptr[13] = TChar.CastFrom(':');
			Number.WriteTwoDigits((uint)minute, ptr + 14);
			ptr[16] = TChar.CastFrom(':');
			Number.WriteTwoDigits((uint)second, ptr + 17);
		}
		return true;
	}

	internal unsafe static bool TryFormatu<TChar>(DateTime dateTime, TimeSpan offset, Span<TChar> destination, out int charsWritten) where TChar : unmanaged, IUtfChar<TChar>
	{
		if (destination.Length < 20)
		{
			charsWritten = 0;
			return false;
		}
		charsWritten = 20;
		if (offset.Ticks != long.MinValue)
		{
			dateTime -= offset;
		}
		dateTime.GetDate(out var year, out var month, out var day);
		dateTime.GetTime(out var hour, out var minute, out var second);
		fixed (TChar* ptr = &MemoryMarshal.GetReference(destination))
		{
			Number.WriteFourDigits((uint)year, ptr);
			ptr[4] = TChar.CastFrom('-');
			Number.WriteTwoDigits((uint)month, ptr + 5);
			ptr[7] = TChar.CastFrom('-');
			Number.WriteTwoDigits((uint)day, ptr + 8);
			ptr[10] = TChar.CastFrom(' ');
			Number.WriteTwoDigits((uint)hour, ptr + 11);
			ptr[13] = TChar.CastFrom(':');
			Number.WriteTwoDigits((uint)minute, ptr + 14);
			ptr[16] = TChar.CastFrom(':');
			Number.WriteTwoDigits((uint)second, ptr + 17);
			ptr[19] = TChar.CastFrom('Z');
		}
		return true;
	}

	internal unsafe static bool TryFormatR<TChar>(DateTime dateTime, TimeSpan offset, Span<TChar> destination, out int charsWritten) where TChar : unmanaged, IUtfChar<TChar>
	{
		if (destination.Length < 29)
		{
			charsWritten = 0;
			return false;
		}
		charsWritten = 29;
		if (offset.Ticks != long.MinValue)
		{
			dateTime -= offset;
		}
		dateTime.GetDate(out var year, out var month, out var day);
		dateTime.GetTime(out var hour, out var minute, out var second);
		string text = s_invariantAbbreviatedDayNames[(int)dateTime.DayOfWeek];
		string text2 = s_invariantAbbreviatedMonthNames[month - 1];
		fixed (TChar* ptr = &MemoryMarshal.GetReference(destination))
		{
			char value = text[2];
			*ptr = TChar.CastFrom(text[0]);
			ptr[1] = TChar.CastFrom(text[1]);
			ptr[2] = TChar.CastFrom(value);
			ptr[3] = TChar.CastFrom(',');
			ptr[4] = TChar.CastFrom(' ');
			Number.WriteTwoDigits((uint)day, ptr + 5);
			ptr[7] = TChar.CastFrom(' ');
			value = text2[2];
			ptr[8] = TChar.CastFrom(text2[0]);
			ptr[9] = TChar.CastFrom(text2[1]);
			ptr[10] = TChar.CastFrom(value);
			ptr[11] = TChar.CastFrom(' ');
			Number.WriteFourDigits((uint)year, ptr + 12);
			ptr[16] = TChar.CastFrom(' ');
			Number.WriteTwoDigits((uint)hour, ptr + 17);
			ptr[19] = TChar.CastFrom(':');
			Number.WriteTwoDigits((uint)minute, ptr + 20);
			ptr[22] = TChar.CastFrom(':');
			Number.WriteTwoDigits((uint)second, ptr + 23);
			ptr[25] = TChar.CastFrom(' ');
			ptr[26] = TChar.CastFrom('G');
			ptr[27] = TChar.CastFrom('M');
			ptr[28] = TChar.CastFrom('T');
		}
		return true;
	}

	internal unsafe static bool TryFormatInvariantG<TChar>(DateTime value, TimeSpan offset, Span<TChar> destination, out int bytesWritten) where TChar : unmanaged, IUtfChar<TChar>
	{
		int num = 19;
		if (offset.Ticks != long.MinValue)
		{
			num += 7;
		}
		if (destination.Length < num)
		{
			bytesWritten = 0;
			return false;
		}
		bytesWritten = num;
		value.GetDate(out var year, out var month, out var day);
		value.GetTime(out var hour, out var minute, out var second);
		fixed (TChar* ptr = &MemoryMarshal.GetReference(destination))
		{
			Number.WriteTwoDigits((uint)month, ptr);
			ptr[2] = TChar.CastFrom('/');
			Number.WriteTwoDigits((uint)day, ptr + 3);
			ptr[5] = TChar.CastFrom('/');
			Number.WriteFourDigits((uint)year, ptr + 6);
			ptr[10] = TChar.CastFrom(' ');
			Number.WriteTwoDigits((uint)hour, ptr + 11);
			ptr[13] = TChar.CastFrom(':');
			Number.WriteTwoDigits((uint)minute, ptr + 14);
			ptr[16] = TChar.CastFrom(':');
			Number.WriteTwoDigits((uint)second, ptr + 17);
			if (offset.Ticks != long.MinValue)
			{
				int num2 = (int)(offset.Ticks / 600000000);
				TChar val = TChar.CastFrom('+');
				if (num2 < 0)
				{
					val = TChar.CastFrom('-');
					num2 = -num2;
				}
				int value2;
				(value2, num2) = Math.DivRem(num2, 60);
				ptr[19] = TChar.CastFrom(' ');
				ptr[20] = val;
				Number.WriteTwoDigits((uint)value2, ptr + 21);
				ptr[23] = TChar.CastFrom(':');
				Number.WriteTwoDigits((uint)num2, ptr + 24);
			}
		}
		return true;
	}

	internal static string[] GetAllDateTimes(DateTime dateTime, char format, DateTimeFormatInfo dtfi)
	{
		string[] array;
		switch (format)
		{
		case 'D':
		case 'F':
		case 'G':
		case 'M':
		case 'T':
		case 'Y':
		case 'd':
		case 'f':
		case 'g':
		case 'm':
		case 't':
		case 'y':
		{
			string[] allDateTimePatterns = dtfi.GetAllDateTimePatterns(format);
			array = new string[allDateTimePatterns.Length];
			for (int j = 0; j < allDateTimePatterns.Length; j++)
			{
				array[j] = Format(dateTime, allDateTimePatterns[j], dtfi);
			}
			break;
		}
		case 'U':
		{
			DateTime dateTime2 = dateTime.ToUniversalTime();
			string[] allDateTimePatterns = dtfi.GetAllDateTimePatterns(format);
			array = new string[allDateTimePatterns.Length];
			for (int i = 0; i < allDateTimePatterns.Length; i++)
			{
				array[i] = Format(dateTime2, allDateTimePatterns[i], dtfi);
			}
			break;
		}
		case 'O':
		case 'R':
		case 'o':
		case 'r':
		case 's':
		case 'u':
			array = new string[1] { Format(dateTime, char.ToString(format), dtfi) };
			break;
		default:
			throw new FormatException(SR.Format_InvalidString);
		}
		return array;
	}

	internal static string[] GetAllDateTimes(DateTime dateTime, DateTimeFormatInfo dtfi)
	{
		List<string> list = new List<string>(132);
		string text = "dDfFgGmMoOrRstTuUyY";
		foreach (char format in text)
		{
			string[] allDateTimes = GetAllDateTimes(dateTime, format, dtfi);
			foreach (string item in allDateTimes)
			{
				list.Add(item);
			}
		}
		return list.ToArray();
	}
}
