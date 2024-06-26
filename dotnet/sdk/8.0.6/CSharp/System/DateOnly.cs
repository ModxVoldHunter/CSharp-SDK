using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace System;

public readonly struct DateOnly : IComparable, IComparable<DateOnly>, IEquatable<DateOnly>, ISpanFormattable, IFormattable, ISpanParsable<DateOnly>, IParsable<DateOnly>, IUtf8SpanFormattable
{
	private readonly int _dayNumber;

	public static DateOnly MinValue => new DateOnly(0);

	public static DateOnly MaxValue => new DateOnly(3652058);

	public int Year => GetEquivalentDateTime().Year;

	public int Month => GetEquivalentDateTime().Month;

	public int Day => GetEquivalentDateTime().Day;

	public DayOfWeek DayOfWeek => (DayOfWeek)((uint)(_dayNumber + 1) % 7u);

	public int DayOfYear => GetEquivalentDateTime().DayOfYear;

	public int DayNumber => _dayNumber;

	private static int DayNumberFromDateTime(DateTime dt)
	{
		return (int)((ulong)dt.Ticks / 864000000000uL);
	}

	private DateTime GetEquivalentDateTime()
	{
		return DateTime.UnsafeCreate(_dayNumber * 864000000000L);
	}

	private DateOnly(int dayNumber)
	{
		_dayNumber = dayNumber;
	}

	public DateOnly(int year, int month, int day)
	{
		_dayNumber = DayNumberFromDateTime(new DateTime(year, month, day));
	}

	public DateOnly(int year, int month, int day, Calendar calendar)
	{
		_dayNumber = DayNumberFromDateTime(new DateTime(year, month, day, calendar));
	}

	public static DateOnly FromDayNumber(int dayNumber)
	{
		if ((uint)dayNumber > 3652058u)
		{
			ThrowHelper.ThrowArgumentOutOfRange_DayNumber(dayNumber);
		}
		return new DateOnly(dayNumber);
	}

	public DateOnly AddDays(int value)
	{
		int num = _dayNumber + value;
		if ((uint)num > 3652058u)
		{
			ThrowOutOfRange();
		}
		return new DateOnly(num);
		static void ThrowOutOfRange()
		{
			throw new ArgumentOutOfRangeException("value", SR.ArgumentOutOfRange_AddValue);
		}
	}

	public DateOnly AddMonths(int value)
	{
		return new DateOnly(DayNumberFromDateTime(GetEquivalentDateTime().AddMonths(value)));
	}

	public DateOnly AddYears(int value)
	{
		return new DateOnly(DayNumberFromDateTime(GetEquivalentDateTime().AddYears(value)));
	}

	public static bool operator ==(DateOnly left, DateOnly right)
	{
		return left._dayNumber == right._dayNumber;
	}

	public static bool operator !=(DateOnly left, DateOnly right)
	{
		return left._dayNumber != right._dayNumber;
	}

	public static bool operator >(DateOnly left, DateOnly right)
	{
		return left._dayNumber > right._dayNumber;
	}

	public static bool operator >=(DateOnly left, DateOnly right)
	{
		return left._dayNumber >= right._dayNumber;
	}

	public static bool operator <(DateOnly left, DateOnly right)
	{
		return left._dayNumber < right._dayNumber;
	}

	public static bool operator <=(DateOnly left, DateOnly right)
	{
		return left._dayNumber <= right._dayNumber;
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	public void Deconstruct(out int year, out int month, out int day)
	{
		GetEquivalentDateTime().GetDate(out year, out month, out day);
	}

	public DateTime ToDateTime(TimeOnly time)
	{
		return new DateTime(_dayNumber * 864000000000L + time.Ticks);
	}

	public DateTime ToDateTime(TimeOnly time, DateTimeKind kind)
	{
		return new DateTime(_dayNumber * 864000000000L + time.Ticks, kind);
	}

	public static DateOnly FromDateTime(DateTime dateTime)
	{
		return new DateOnly(DayNumberFromDateTime(dateTime));
	}

	public int CompareTo(DateOnly value)
	{
		return _dayNumber.CompareTo(value._dayNumber);
	}

	public int CompareTo(object? value)
	{
		if (value == null)
		{
			return 1;
		}
		if (!(value is DateOnly value2))
		{
			throw new ArgumentException(SR.Arg_MustBeDateOnly);
		}
		return CompareTo(value2);
	}

	public bool Equals(DateOnly value)
	{
		return _dayNumber == value._dayNumber;
	}

	public override bool Equals([NotNullWhen(true)] object? value)
	{
		if (value is DateOnly dateOnly)
		{
			return _dayNumber == dateOnly._dayNumber;
		}
		return false;
	}

	public override int GetHashCode()
	{
		return _dayNumber;
	}

	public static DateOnly Parse(ReadOnlySpan<char> s, IFormatProvider? provider = null, DateTimeStyles style = DateTimeStyles.None)
	{
		DateOnly result;
		ParseFailureKind parseFailureKind = TryParseInternal(s, provider, style, out result);
		if (parseFailureKind != 0)
		{
			ThrowOnError(parseFailureKind, s);
		}
		return result;
	}

	public static DateOnly ParseExact(ReadOnlySpan<char> s, [StringSyntax("DateOnlyFormat")] ReadOnlySpan<char> format, IFormatProvider? provider = null, DateTimeStyles style = DateTimeStyles.None)
	{
		DateOnly result;
		ParseFailureKind parseFailureKind = TryParseExactInternal(s, format, provider, style, out result);
		if (parseFailureKind != 0)
		{
			ThrowOnError(parseFailureKind, s);
		}
		return result;
	}

	public static DateOnly ParseExact(ReadOnlySpan<char> s, [StringSyntax("DateOnlyFormat")] string[] formats)
	{
		return ParseExact(s, formats, null);
	}

	public static DateOnly ParseExact(ReadOnlySpan<char> s, [StringSyntax("DateOnlyFormat")] string[] formats, IFormatProvider? provider, DateTimeStyles style = DateTimeStyles.None)
	{
		DateOnly result;
		ParseFailureKind parseFailureKind = TryParseExactInternal(s, formats, provider, style, out result);
		if (parseFailureKind != 0)
		{
			ThrowOnError(parseFailureKind, s);
		}
		return result;
	}

	public static DateOnly Parse(string s)
	{
		return Parse(s, null, DateTimeStyles.None);
	}

	public static DateOnly Parse(string s, IFormatProvider? provider, DateTimeStyles style = DateTimeStyles.None)
	{
		if (s == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.s);
		}
		return Parse(s.AsSpan(), provider, style);
	}

	public static DateOnly ParseExact(string s, [StringSyntax("DateOnlyFormat")] string format)
	{
		return ParseExact(s, format, null);
	}

	public static DateOnly ParseExact(string s, [StringSyntax("DateOnlyFormat")] string format, IFormatProvider? provider, DateTimeStyles style = DateTimeStyles.None)
	{
		if (s == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.s);
		}
		if (format == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.format);
		}
		return ParseExact(s.AsSpan(), format.AsSpan(), provider, style);
	}

	public static DateOnly ParseExact(string s, [StringSyntax("DateOnlyFormat")] string[] formats)
	{
		return ParseExact(s, formats, null);
	}

	public static DateOnly ParseExact(string s, [StringSyntax("DateOnlyFormat")] string[] formats, IFormatProvider? provider, DateTimeStyles style = DateTimeStyles.None)
	{
		if (s == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.s);
		}
		return ParseExact(s.AsSpan(), formats, provider, style);
	}

	public static bool TryParse(ReadOnlySpan<char> s, out DateOnly result)
	{
		return TryParse(s, null, DateTimeStyles.None, out result);
	}

	public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, DateTimeStyles style, out DateOnly result)
	{
		return TryParseInternal(s, provider, style, out result) == ParseFailureKind.None;
	}

	private static ParseFailureKind TryParseInternal(ReadOnlySpan<char> s, IFormatProvider provider, DateTimeStyles style, out DateOnly result)
	{
		if (((uint)style & 0xFFFFFFF8u) != 0)
		{
			result = default(DateOnly);
			return ParseFailureKind.Argument_InvalidDateStyles;
		}
		DateTimeResult result2 = default(DateTimeResult);
		result2.Init(s);
		if (!DateTimeParse.TryParse(s, DateTimeFormatInfo.GetInstance(provider), style, ref result2))
		{
			result = default(DateOnly);
			return ParseFailureKind.Format_BadDateOnly;
		}
		if ((result2.flags & (ParseFlags.HaveHour | ParseFlags.HaveMinute | ParseFlags.HaveSecond | ParseFlags.HaveTime | ParseFlags.TimeZoneUsed | ParseFlags.TimeZoneUtc | ParseFlags.CaptureOffset | ParseFlags.UtcSortPattern)) != 0)
		{
			result = default(DateOnly);
			return ParseFailureKind.Format_DateTimeOnlyContainsNoneDateParts;
		}
		result = new DateOnly(DayNumberFromDateTime(result2.parsedDate));
		return ParseFailureKind.None;
	}

	public static bool TryParseExact(ReadOnlySpan<char> s, [StringSyntax("DateOnlyFormat")] ReadOnlySpan<char> format, out DateOnly result)
	{
		return TryParseExact(s, format, null, DateTimeStyles.None, out result);
	}

	public static bool TryParseExact(ReadOnlySpan<char> s, [StringSyntax("DateOnlyFormat")] ReadOnlySpan<char> format, IFormatProvider? provider, DateTimeStyles style, out DateOnly result)
	{
		return TryParseExactInternal(s, format, provider, style, out result) == ParseFailureKind.None;
	}

	private static ParseFailureKind TryParseExactInternal(ReadOnlySpan<char> s, ReadOnlySpan<char> format, IFormatProvider provider, DateTimeStyles style, out DateOnly result)
	{
		if (((uint)style & 0xFFFFFFF8u) != 0)
		{
			result = default(DateOnly);
			return ParseFailureKind.Argument_InvalidDateStyles;
		}
		if (format.Length == 1)
		{
			switch (format[0] | 0x20)
			{
			case 111:
				format = "yyyy'-'MM'-'dd";
				provider = CultureInfo.InvariantCulture.DateTimeFormat;
				break;
			case 114:
				format = "ddd, dd MMM yyyy";
				provider = CultureInfo.InvariantCulture.DateTimeFormat;
				break;
			}
		}
		DateTimeResult result2 = default(DateTimeResult);
		result2.Init(s);
		if (!DateTimeParse.TryParseExact(s, format, DateTimeFormatInfo.GetInstance(provider), style, ref result2))
		{
			result = default(DateOnly);
			return ParseFailureKind.Format_BadDateOnly;
		}
		if ((result2.flags & (ParseFlags.HaveHour | ParseFlags.HaveMinute | ParseFlags.HaveSecond | ParseFlags.HaveTime | ParseFlags.TimeZoneUsed | ParseFlags.TimeZoneUtc | ParseFlags.CaptureOffset | ParseFlags.UtcSortPattern)) != 0)
		{
			result = default(DateOnly);
			return ParseFailureKind.Format_DateTimeOnlyContainsNoneDateParts;
		}
		result = new DateOnly(DayNumberFromDateTime(result2.parsedDate));
		return ParseFailureKind.None;
	}

	public static bool TryParseExact(ReadOnlySpan<char> s, [NotNullWhen(true)][StringSyntax("DateOnlyFormat")] string?[]? formats, out DateOnly result)
	{
		return TryParseExact(s, formats, null, DateTimeStyles.None, out result);
	}

	public static bool TryParseExact(ReadOnlySpan<char> s, [NotNullWhen(true)][StringSyntax("DateOnlyFormat")] string?[]? formats, IFormatProvider? provider, DateTimeStyles style, out DateOnly result)
	{
		return TryParseExactInternal(s, formats, provider, style, out result) == ParseFailureKind.None;
	}

	private static ParseFailureKind TryParseExactInternal(ReadOnlySpan<char> s, string[] formats, IFormatProvider provider, DateTimeStyles style, out DateOnly result)
	{
		if (((uint)style & 0xFFFFFFF8u) != 0 || formats == null)
		{
			result = default(DateOnly);
			return ParseFailureKind.Argument_InvalidDateStyles;
		}
		DateTimeFormatInfo instance = DateTimeFormatInfo.GetInstance(provider);
		for (int i = 0; i < formats.Length; i++)
		{
			DateTimeFormatInfo dtfi = instance;
			string text = formats[i];
			if (string.IsNullOrEmpty(text))
			{
				result = default(DateOnly);
				return ParseFailureKind.Argument_BadFormatSpecifier;
			}
			if (text.Length == 1)
			{
				switch (text[0] | 0x20)
				{
				case 111:
					text = "yyyy'-'MM'-'dd";
					dtfi = CultureInfo.InvariantCulture.DateTimeFormat;
					break;
				case 114:
					text = "ddd, dd MMM yyyy";
					dtfi = CultureInfo.InvariantCulture.DateTimeFormat;
					break;
				}
			}
			DateTimeResult result2 = default(DateTimeResult);
			result2.Init(s);
			if (DateTimeParse.TryParseExact(s, text, dtfi, style, ref result2) && (result2.flags & (ParseFlags.HaveHour | ParseFlags.HaveMinute | ParseFlags.HaveSecond | ParseFlags.HaveTime | ParseFlags.TimeZoneUsed | ParseFlags.TimeZoneUtc | ParseFlags.CaptureOffset | ParseFlags.UtcSortPattern)) == 0)
			{
				result = new DateOnly(DayNumberFromDateTime(result2.parsedDate));
				return ParseFailureKind.None;
			}
		}
		result = default(DateOnly);
		return ParseFailureKind.Format_BadDateOnly;
	}

	public static bool TryParse([NotNullWhen(true)] string? s, out DateOnly result)
	{
		return TryParse(s, null, DateTimeStyles.None, out result);
	}

	public static bool TryParse([NotNullWhen(true)] string? s, IFormatProvider? provider, DateTimeStyles style, out DateOnly result)
	{
		if (s == null)
		{
			result = default(DateOnly);
			return false;
		}
		return TryParse(s.AsSpan(), provider, style, out result);
	}

	public static bool TryParseExact([NotNullWhen(true)] string? s, [NotNullWhen(true)][StringSyntax("DateOnlyFormat")] string? format, out DateOnly result)
	{
		return TryParseExact(s, format, null, DateTimeStyles.None, out result);
	}

	public static bool TryParseExact([NotNullWhen(true)] string? s, [NotNullWhen(true)][StringSyntax("DateOnlyFormat")] string? format, IFormatProvider? provider, DateTimeStyles style, out DateOnly result)
	{
		if (s == null || format == null)
		{
			result = default(DateOnly);
			return false;
		}
		return TryParseExact(s.AsSpan(), format.AsSpan(), provider, style, out result);
	}

	public static bool TryParseExact([NotNullWhen(true)] string? s, [NotNullWhen(true)][StringSyntax("DateOnlyFormat")] string?[]? formats, out DateOnly result)
	{
		return TryParseExact(s, formats, null, DateTimeStyles.None, out result);
	}

	public static bool TryParseExact([NotNullWhen(true)] string? s, [NotNullWhen(true)][StringSyntax("DateOnlyFormat")] string?[]? formats, IFormatProvider? provider, DateTimeStyles style, out DateOnly result)
	{
		if (s == null)
		{
			result = default(DateOnly);
			return false;
		}
		return TryParseExact(s.AsSpan(), formats, provider, style, out result);
	}

	private static void ThrowOnError(ParseFailureKind result, ReadOnlySpan<char> s)
	{
		switch (result)
		{
		case ParseFailureKind.Argument_InvalidDateStyles:
			throw new ArgumentException(SR.Argument_InvalidDateStyles, "style");
		case ParseFailureKind.Argument_BadFormatSpecifier:
			throw new FormatException(SR.Argument_BadFormatSpecifier);
		case ParseFailureKind.Format_BadDateOnly:
			throw new FormatException(SR.Format(SR.Format_BadDateOnly, s.ToString()));
		default:
			throw new FormatException(SR.Format(SR.Format_DateTimeOnlyContainsNoneDateParts, s.ToString(), "DateOnly"));
		}
	}

	public string ToLongDateString()
	{
		return ToString("D");
	}

	public string ToShortDateString()
	{
		return ToString();
	}

	public override string ToString()
	{
		return ToString("d");
	}

	public string ToString([StringSyntax("DateOnlyFormat")] string? format)
	{
		return ToString(format, null);
	}

	public string ToString(IFormatProvider? provider)
	{
		return ToString("d", provider);
	}

	public string ToString([StringSyntax("DateOnlyFormat")] string? format, IFormatProvider? provider)
	{
		if (string.IsNullOrEmpty(format))
		{
			format = "d";
		}
		if (format.Length == 1)
		{
			switch (format[0] | 0x20)
			{
			case 111:
				return string.Create(10, this, delegate(Span<char> destination, DateOnly value)
				{
					DateTimeFormat.TryFormatDateOnlyO(value.Year, value.Month, value.Day, destination, out var _);
				});
			case 114:
				return string.Create(16, this, delegate(Span<char> destination, DateOnly value)
				{
					DateTimeFormat.TryFormatDateOnlyR(value.DayOfWeek, value.Year, value.Month, value.Day, destination, out var _);
				});
			case 100:
			case 109:
			case 121:
				return DateTimeFormat.Format(GetEquivalentDateTime(), format, provider);
			default:
				throw new FormatException(SR.Format_InvalidString);
			}
		}
		DateTimeFormat.IsValidCustomDateOnlyFormat(format.AsSpan(), throwOnError: true);
		return DateTimeFormat.Format(GetEquivalentDateTime(), format, provider);
	}

	public bool TryFormat(Span<char> destination, out int charsWritten, [StringSyntax("DateOnlyFormat")] ReadOnlySpan<char> format = default(ReadOnlySpan<char>), IFormatProvider? provider = null)
	{
		return TryFormatCore(destination, out charsWritten, format, provider);
	}

	public bool TryFormat(Span<byte> utf8Destination, out int bytesWritten, [StringSyntax("DateOnlyFormat")] ReadOnlySpan<char> format = default(ReadOnlySpan<char>), IFormatProvider? provider = null)
	{
		return TryFormatCore(utf8Destination, out bytesWritten, format, provider);
	}

	private bool TryFormatCore<TChar>(Span<TChar> destination, out int charsWritten, [StringSyntax("DateOnlyFormat")] ReadOnlySpan<char> format, IFormatProvider provider = null) where TChar : unmanaged, IUtfChar<TChar>
	{
		if (format.Length == 0)
		{
			format = "d";
		}
		if (format.Length == 1)
		{
			switch (format[0] | 0x20)
			{
			case 111:
				return DateTimeFormat.TryFormatDateOnlyO(Year, Month, Day, destination, out charsWritten);
			case 114:
				return DateTimeFormat.TryFormatDateOnlyR(DayOfWeek, Year, Month, Day, destination, out charsWritten);
			case 100:
			case 109:
			case 121:
				return DateTimeFormat.TryFormat(GetEquivalentDateTime(), destination, out charsWritten, format, provider);
			}
			ThrowHelper.ThrowFormatException_BadFormatSpecifier();
		}
		if (!DateTimeFormat.IsValidCustomDateOnlyFormat(format, throwOnError: false))
		{
			throw new FormatException(SR.Format(SR.Format_DateTimeOnlyContainsNoneDateParts, format.ToString(), "DateOnly"));
		}
		return DateTimeFormat.TryFormat(GetEquivalentDateTime(), destination, out charsWritten, format, provider);
	}

	public static DateOnly Parse(string s, IFormatProvider? provider)
	{
		return Parse(s, provider, DateTimeStyles.None);
	}

	public static bool TryParse([NotNullWhen(true)] string? s, IFormatProvider? provider, out DateOnly result)
	{
		return TryParse(s, provider, DateTimeStyles.None, out result);
	}

	public static DateOnly Parse(ReadOnlySpan<char> s, IFormatProvider? provider)
	{
		return Parse(s, provider, DateTimeStyles.None);
	}

	public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, out DateOnly result)
	{
		return TryParse(s, provider, DateTimeStyles.None, out result);
	}
}
