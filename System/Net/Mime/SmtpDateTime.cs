using System.Collections.Generic;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace System.Net.Mime;

internal sealed class SmtpDateTime
{
	internal static readonly string[] s_validDateTimeFormats = new string[4] { "ddd, dd MMM yyyy HH:mm:ss", "dd MMM yyyy HH:mm:ss", "ddd, dd MMM yyyy HH:mm", "dd MMM yyyy HH:mm" };

	internal static readonly Dictionary<string, TimeSpan> s_timeZoneOffsetLookup = new Dictionary<string, TimeSpan>
	{
		{
			"UT",
			TimeSpan.Zero
		},
		{
			"GMT",
			TimeSpan.Zero
		},
		{
			"EDT",
			new TimeSpan(-4, 0, 0)
		},
		{
			"EST",
			new TimeSpan(-5, 0, 0)
		},
		{
			"CDT",
			new TimeSpan(-5, 0, 0)
		},
		{
			"CST",
			new TimeSpan(-6, 0, 0)
		},
		{
			"MDT",
			new TimeSpan(-6, 0, 0)
		},
		{
			"MST",
			new TimeSpan(-7, 0, 0)
		},
		{
			"PDT",
			new TimeSpan(-7, 0, 0)
		},
		{
			"PST",
			new TimeSpan(-8, 0, 0)
		}
	};

	private readonly DateTime _date;

	private readonly TimeSpan _timeZone;

	private readonly bool _unknownTimeZone;

	internal DateTime Date
	{
		get
		{
			if (_unknownTimeZone)
			{
				return DateTime.SpecifyKind(_date, DateTimeKind.Unspecified);
			}
			return new DateTimeOffset(_date, _timeZone).LocalDateTime;
		}
	}

	internal SmtpDateTime(DateTime value)
	{
		_date = value;
		switch (value.Kind)
		{
		case DateTimeKind.Local:
		{
			TimeSpan utcOffset = TimeZoneInfo.Local.GetUtcOffset(value);
			_timeZone = ValidateAndGetSanitizedTimeSpan(utcOffset);
			break;
		}
		case DateTimeKind.Unspecified:
			_unknownTimeZone = true;
			break;
		case DateTimeKind.Utc:
			_timeZone = TimeSpan.Zero;
			break;
		}
	}

	internal SmtpDateTime(string value)
	{
		_date = ParseValue(value, out var timeZone);
		if (!TryParseTimeZoneString(timeZone, out _timeZone))
		{
			_unknownTimeZone = true;
		}
	}

	public override string ToString()
	{
		TimeSpan timeZone = _timeZone;
		IFormatProvider invariantCulture;
		if (!_unknownTimeZone && timeZone.Ticks != 0L)
		{
			invariantCulture = CultureInfo.InvariantCulture;
			IFormatProvider provider = invariantCulture;
			DefaultInterpolatedStringHandler handler = new DefaultInterpolatedStringHandler(1, 3, invariantCulture);
			handler.AppendFormatted(_date, "ddd, dd MMM yyyy HH:mm:ss");
			handler.AppendLiteral(" ");
			handler.AppendFormatted((timeZone.Ticks > 0) ? '+' : '-');
			handler.AppendFormatted(timeZone, "hhmm");
			return string.Create(provider, ref handler);
		}
		invariantCulture = CultureInfo.InvariantCulture;
		IFormatProvider provider2 = invariantCulture;
		DefaultInterpolatedStringHandler handler2 = new DefaultInterpolatedStringHandler(1, 2, invariantCulture);
		handler2.AppendFormatted(_date, "ddd, dd MMM yyyy HH:mm:ss");
		handler2.AppendLiteral(" ");
		handler2.AppendFormatted(_unknownTimeZone ? "-0000" : "+0000");
		return string.Create(provider2, ref handler2);
	}

	internal static void ValidateAndGetTimeZoneOffsetValues(string offset, out bool positive, out int hours, out int minutes)
	{
		if (offset.Length != 5)
		{
			throw new FormatException(System.SR.MailDateInvalidFormat);
		}
		positive = offset.StartsWith('+');
		if (!int.TryParse(offset.AsSpan(1, 2), NumberStyles.None, CultureInfo.InvariantCulture, out hours))
		{
			throw new FormatException(System.SR.MailDateInvalidFormat);
		}
		if (!int.TryParse(offset.AsSpan(3, 2), NumberStyles.None, CultureInfo.InvariantCulture, out minutes))
		{
			throw new FormatException(System.SR.MailDateInvalidFormat);
		}
		if (minutes > 59)
		{
			throw new FormatException(System.SR.MailDateInvalidFormat);
		}
	}

	internal static void ValidateTimeZoneShortHandValue(string value)
	{
		for (int i = 0; i < value.Length; i++)
		{
			if (!char.IsLetter(value, i))
			{
				throw new FormatException(System.SR.Format(System.SR.MailHeaderFieldInvalidCharacter, value));
			}
		}
	}

	internal static DateTime ParseValue(string data, out string timeZone)
	{
		if (string.IsNullOrEmpty(data))
		{
			throw new FormatException(System.SR.MailDateInvalidFormat);
		}
		int num = data.IndexOf(':');
		if (num == -1)
		{
			throw new FormatException(System.SR.Format(System.SR.MailHeaderFieldInvalidCharacter, data));
		}
		int num2 = data.AsSpan(num).IndexOfAny(' ', '\t');
		if (num2 < 0)
		{
			throw new FormatException(System.SR.Format(System.SR.MailHeaderFieldInvalidCharacter, data));
		}
		num2 += num;
		ReadOnlySpan<char> s = data.AsSpan(0, num2).Trim();
		if (!DateTime.TryParseExact(s, s_validDateTimeFormats, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces, out var result))
		{
			throw new FormatException(System.SR.MailDateInvalidFormat);
		}
		ReadOnlySpan<char> span = data.AsSpan(num2).Trim();
		int num3 = span.IndexOfAny(' ', '\t');
		if (num3 >= 0)
		{
			span = span.Slice(0, num3);
		}
		if (span.IsEmpty)
		{
			throw new FormatException(System.SR.MailDateInvalidFormat);
		}
		timeZone = span.ToString();
		return result;
	}

	internal static bool TryParseTimeZoneString(string timeZoneString, out TimeSpan timeZone)
	{
		if (timeZoneString == "-0000")
		{
			timeZone = TimeSpan.Zero;
			return false;
		}
		if (timeZoneString[0] == '+' || timeZoneString[0] == '-')
		{
			ValidateAndGetTimeZoneOffsetValues(timeZoneString, out var positive, out var hours, out var minutes);
			if (!positive)
			{
				if (hours != 0)
				{
					hours *= -1;
				}
				else if (minutes != 0)
				{
					minutes *= -1;
				}
			}
			timeZone = new TimeSpan(hours, minutes, 0);
			return true;
		}
		ValidateTimeZoneShortHandValue(timeZoneString);
		return s_timeZoneOffsetLookup.TryGetValue(timeZoneString, out timeZone);
	}

	internal static TimeSpan ValidateAndGetSanitizedTimeSpan(TimeSpan span)
	{
		TimeSpan result = new TimeSpan(span.Days, span.Hours, span.Minutes, 0, 0);
		if (Math.Abs(result.Ticks) > 3599400000000L)
		{
			throw new FormatException(System.SR.MailDateInvalidFormat);
		}
		return result;
	}
}
