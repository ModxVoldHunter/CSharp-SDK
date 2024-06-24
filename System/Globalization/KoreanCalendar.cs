namespace System.Globalization;

public class KoreanCalendar : Calendar
{
	public const int KoreanEra = 1;

	private static readonly EraInfo[] s_koreanEraInfo = new EraInfo[1]
	{
		new EraInfo(1, 1, 1, 1, -2333, 2334, 12332)
	};

	private readonly GregorianCalendarHelper _helper;

	public override DateTime MinSupportedDateTime => DateTime.MinValue;

	public override DateTime MaxSupportedDateTime => DateTime.MaxValue;

	public override CalendarAlgorithmType AlgorithmType => CalendarAlgorithmType.SolarCalendar;

	internal override CalendarId ID => CalendarId.KOREA;

	public override int[] Eras => _helper.Eras;

	public override int TwoDigitYearMax
	{
		get
		{
			if (_twoDigitYearMax == -1)
			{
				_twoDigitYearMax = Calendar.GetSystemTwoDigitYearSetting(ID, 4362);
			}
			return _twoDigitYearMax;
		}
		set
		{
			VerifyWritable();
			if (value < 99 || value > _helper.MaxYear)
			{
				throw new ArgumentOutOfRangeException("value", value, SR.Format(SR.ArgumentOutOfRange_Range, 99, _helper.MaxYear));
			}
			_twoDigitYearMax = value;
		}
	}

	public KoreanCalendar()
	{
		try
		{
			new CultureInfo("ko-KR");
		}
		catch (ArgumentException innerException)
		{
			throw new TypeInitializationException(GetType().ToString(), innerException);
		}
		_helper = new GregorianCalendarHelper(this, s_koreanEraInfo);
	}

	public override DateTime AddMonths(DateTime time, int months)
	{
		return _helper.AddMonths(time, months);
	}

	public override DateTime AddYears(DateTime time, int years)
	{
		return _helper.AddYears(time, years);
	}

	public override int GetDaysInMonth(int year, int month, int era)
	{
		return _helper.GetDaysInMonth(year, month, era);
	}

	public override int GetDaysInYear(int year, int era)
	{
		return _helper.GetDaysInYear(year, era);
	}

	public override int GetDayOfMonth(DateTime time)
	{
		return _helper.GetDayOfMonth(time);
	}

	public override DayOfWeek GetDayOfWeek(DateTime time)
	{
		return _helper.GetDayOfWeek(time);
	}

	public override int GetDayOfYear(DateTime time)
	{
		return _helper.GetDayOfYear(time);
	}

	public override int GetMonthsInYear(int year, int era)
	{
		return _helper.GetMonthsInYear(year, era);
	}

	public override int GetWeekOfYear(DateTime time, CalendarWeekRule rule, DayOfWeek firstDayOfWeek)
	{
		return _helper.GetWeekOfYear(time, rule, firstDayOfWeek);
	}

	public override int GetEra(DateTime time)
	{
		return _helper.GetEra(time);
	}

	public override int GetMonth(DateTime time)
	{
		return _helper.GetMonth(time);
	}

	public override int GetYear(DateTime time)
	{
		return _helper.GetYear(time);
	}

	public override bool IsLeapDay(int year, int month, int day, int era)
	{
		return _helper.IsLeapDay(year, month, day, era);
	}

	public override bool IsLeapYear(int year, int era)
	{
		return _helper.IsLeapYear(year, era);
	}

	public override int GetLeapMonth(int year, int era)
	{
		return _helper.GetLeapMonth(year, era);
	}

	public override bool IsLeapMonth(int year, int month, int era)
	{
		return _helper.IsLeapMonth(year, month, era);
	}

	public override DateTime ToDateTime(int year, int month, int day, int hour, int minute, int second, int millisecond, int era)
	{
		return _helper.ToDateTime(year, month, day, hour, minute, second, millisecond, era);
	}

	public override int ToFourDigitYear(int year)
	{
		ArgumentOutOfRangeException.ThrowIfNegative(year, "year");
		return _helper.ToFourDigitYear(year, TwoDigitYearMax);
	}
}
