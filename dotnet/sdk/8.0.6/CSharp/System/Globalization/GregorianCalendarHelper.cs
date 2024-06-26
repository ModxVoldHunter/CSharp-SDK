namespace System.Globalization;

internal sealed class GregorianCalendarHelper
{
	private readonly int m_maxYear;

	private readonly int m_minYear;

	private readonly Calendar m_Cal;

	private readonly EraInfo[] m_EraInfo;

	internal int MaxYear => m_maxYear;

	public int[] Eras
	{
		get
		{
			EraInfo[] eraInfo = m_EraInfo;
			int[] array = new int[eraInfo.Length];
			for (int i = 0; i < eraInfo.Length; i++)
			{
				array[i] = eraInfo[i].era;
			}
			return array;
		}
	}

	internal GregorianCalendarHelper(Calendar cal, EraInfo[] eraInfo)
	{
		m_Cal = cal;
		m_EraInfo = eraInfo;
		m_maxYear = eraInfo[0].maxEraYear;
		m_minYear = eraInfo[0].minEraYear;
	}

	private int GetYearOffset(int year, int era, bool throwOnError)
	{
		if (year < 0)
		{
			if (throwOnError)
			{
				throw new ArgumentOutOfRangeException("year", SR.ArgumentOutOfRange_NeedNonNegNum);
			}
			return -1;
		}
		if (era == 0)
		{
			era = m_Cal.CurrentEraValue;
		}
		EraInfo[] eraInfo = m_EraInfo;
		for (int i = 0; i < eraInfo.Length; i++)
		{
			EraInfo eraInfo2 = eraInfo[i];
			if (era != eraInfo2.era)
			{
				continue;
			}
			if (year >= eraInfo2.minEraYear)
			{
				if (year <= eraInfo2.maxEraYear)
				{
					return eraInfo2.yearOffset;
				}
				if (!LocalAppContextSwitches.EnforceJapaneseEraYearRanges)
				{
					int num = year - eraInfo2.maxEraYear;
					for (int num2 = i - 1; num2 >= 0; num2--)
					{
						if (num <= eraInfo[num2].maxEraYear)
						{
							return eraInfo2.yearOffset;
						}
						num -= eraInfo[num2].maxEraYear;
					}
				}
			}
			if (!throwOnError)
			{
				break;
			}
			throw new ArgumentOutOfRangeException("year", SR.Format(SR.ArgumentOutOfRange_Range, eraInfo2.minEraYear, eraInfo2.maxEraYear));
		}
		if (throwOnError)
		{
			throw new ArgumentOutOfRangeException("era", SR.ArgumentOutOfRange_InvalidEraValue);
		}
		return -1;
	}

	internal int GetGregorianYear(int year, int era)
	{
		return GetYearOffset(year, era, throwOnError: true) + year;
	}

	internal bool IsValidYear(int year, int era)
	{
		return GetYearOffset(year, era, throwOnError: false) >= 0;
	}

	internal static long GetAbsoluteDate(int year, int month, int day)
	{
		if (year >= 1 && year <= 9999 && month >= 1 && month <= 12)
		{
			ReadOnlySpan<int> readOnlySpan = ((year % 4 == 0 && (year % 100 != 0 || year % 400 == 0)) ? GregorianCalendar.DaysToMonth366 : GregorianCalendar.DaysToMonth365);
			if (day >= 1 && day <= readOnlySpan[month] - readOnlySpan[month - 1])
			{
				int num = year - 1;
				int num2 = num * 365 + num / 4 - num / 100 + num / 400 + readOnlySpan[month - 1] + day - 1;
				return num2;
			}
		}
		throw new ArgumentOutOfRangeException(null, SR.ArgumentOutOfRange_BadYearMonthDay);
	}

	internal static long DateToTicks(int year, int month, int day)
	{
		return GetAbsoluteDate(year, month, day) * 864000000000L;
	}

	internal void CheckTicksRange(long ticks)
	{
		if (ticks < m_Cal.MinSupportedDateTime.Ticks || ticks > m_Cal.MaxSupportedDateTime.Ticks)
		{
			throw new ArgumentOutOfRangeException("time", SR.Format(CultureInfo.InvariantCulture, SR.ArgumentOutOfRange_CalendarRange, m_Cal.MinSupportedDateTime, m_Cal.MaxSupportedDateTime));
		}
	}

	public DateTime AddMonths(DateTime time, int months)
	{
		if (months < -120000 || months > 120000)
		{
			throw new ArgumentOutOfRangeException("months", SR.Format(SR.ArgumentOutOfRange_Range, -120000, 120000));
		}
		CheckTicksRange(time.Ticks);
		time.GetDate(out var year, out var month, out var day);
		int num = month - 1 + months;
		if (num >= 0)
		{
			month = num % 12 + 1;
			year += num / 12;
		}
		else
		{
			month = 12 + (num + 1) % 12;
			year += (num - 11) / 12;
		}
		ReadOnlySpan<int> readOnlySpan = ((year % 4 == 0 && (year % 100 != 0 || year % 400 == 0)) ? GregorianCalendar.DaysToMonth366 : GregorianCalendar.DaysToMonth365);
		int num2 = readOnlySpan[month] - readOnlySpan[month - 1];
		if (day > num2)
		{
			day = num2;
		}
		long ticks = DateToTicks(year, month, day) + time.TimeOfDay.Ticks;
		Calendar.CheckAddResult(ticks, m_Cal.MinSupportedDateTime, m_Cal.MaxSupportedDateTime);
		return new DateTime(ticks);
	}

	public DateTime AddYears(DateTime time, int years)
	{
		return AddMonths(time, years * 12);
	}

	public int GetDayOfMonth(DateTime time)
	{
		CheckTicksRange(time.Ticks);
		return time.Day;
	}

	public DayOfWeek GetDayOfWeek(DateTime time)
	{
		CheckTicksRange(time.Ticks);
		return time.DayOfWeek;
	}

	public int GetDayOfYear(DateTime time)
	{
		CheckTicksRange(time.Ticks);
		return time.DayOfYear;
	}

	public int GetDaysInMonth(int year, int month, int era)
	{
		year = GetGregorianYear(year, era);
		if (month < 1 || month > 12)
		{
			ThrowHelper.ThrowArgumentOutOfRange_Month(month);
		}
		ReadOnlySpan<int> readOnlySpan = ((year % 4 == 0 && (year % 100 != 0 || year % 400 == 0)) ? GregorianCalendar.DaysToMonth366 : GregorianCalendar.DaysToMonth365);
		return readOnlySpan[month] - readOnlySpan[month - 1];
	}

	public int GetDaysInYear(int year, int era)
	{
		year = GetGregorianYear(year, era);
		if (year % 4 != 0 || (year % 100 == 0 && year % 400 != 0))
		{
			return 365;
		}
		return 366;
	}

	public int GetEra(DateTime time)
	{
		long ticks = time.Ticks;
		EraInfo[] eraInfo = m_EraInfo;
		foreach (EraInfo eraInfo2 in eraInfo)
		{
			if (ticks >= eraInfo2.ticks)
			{
				return eraInfo2.era;
			}
		}
		throw new ArgumentOutOfRangeException("time", SR.ArgumentOutOfRange_Era);
	}

	public int GetMonth(DateTime time)
	{
		CheckTicksRange(time.Ticks);
		return time.Month;
	}

	public int GetMonthsInYear(int year, int era)
	{
		ValidateYearInEra(year, era);
		return 12;
	}

	public int GetYear(DateTime time)
	{
		long ticks = time.Ticks;
		CheckTicksRange(ticks);
		EraInfo[] eraInfo = m_EraInfo;
		foreach (EraInfo eraInfo2 in eraInfo)
		{
			if (ticks >= eraInfo2.ticks)
			{
				return time.Year - eraInfo2.yearOffset;
			}
		}
		throw new ArgumentException(SR.Argument_NoEra);
	}

	public int GetYear(int year, DateTime time)
	{
		long ticks = time.Ticks;
		EraInfo[] eraInfo = m_EraInfo;
		foreach (EraInfo eraInfo2 in eraInfo)
		{
			if (ticks >= eraInfo2.ticks && year > eraInfo2.yearOffset)
			{
				return year - eraInfo2.yearOffset;
			}
		}
		throw new ArgumentException(SR.Argument_NoEra);
	}

	public bool IsLeapDay(int year, int month, int day, int era)
	{
		if (day < 1 || day > GetDaysInMonth(year, month, era))
		{
			throw new ArgumentOutOfRangeException("day", SR.Format(SR.ArgumentOutOfRange_Range, 1, GetDaysInMonth(year, month, era)));
		}
		if (!IsLeapYear(year, era))
		{
			return false;
		}
		if (month == 2 && day == 29)
		{
			return true;
		}
		return false;
	}

	public void ValidateYearInEra(int year, int era)
	{
		GetYearOffset(year, era, throwOnError: true);
	}

	public int GetLeapMonth(int year, int era)
	{
		ValidateYearInEra(year, era);
		return 0;
	}

	public bool IsLeapMonth(int year, int month, int era)
	{
		ValidateYearInEra(year, era);
		if (month < 1 || month > 12)
		{
			throw new ArgumentOutOfRangeException("month", SR.Format(SR.ArgumentOutOfRange_Range, 1, 12));
		}
		return false;
	}

	public bool IsLeapYear(int year, int era)
	{
		year = GetGregorianYear(year, era);
		if (year % 4 == 0)
		{
			if (year % 100 == 0)
			{
				return year % 400 == 0;
			}
			return true;
		}
		return false;
	}

	public DateTime ToDateTime(int year, int month, int day, int hour, int minute, int second, int millisecond, int era)
	{
		year = GetGregorianYear(year, era);
		long ticks = DateToTicks(year, month, day) + Calendar.TimeToTicks(hour, minute, second, millisecond);
		CheckTicksRange(ticks);
		return new DateTime(ticks);
	}

	public int GetWeekOfYear(DateTime time, CalendarWeekRule rule, DayOfWeek firstDayOfWeek)
	{
		CheckTicksRange(time.Ticks);
		return GregorianCalendar.GetDefaultInstance().GetWeekOfYear(time, rule, firstDayOfWeek);
	}

	public int ToFourDigitYear(int year, int twoDigitYearMax)
	{
		ArgumentOutOfRangeException.ThrowIfNegative(year, "year");
		if (year < 100)
		{
			return (twoDigitYearMax / 100 - ((year > twoDigitYearMax % 100) ? 1 : 0)) * 100 + year;
		}
		if (year < m_minYear || year > m_maxYear)
		{
			throw new ArgumentOutOfRangeException("year", SR.Format(SR.ArgumentOutOfRange_Range, m_minYear, m_maxYear));
		}
		return year;
	}
}
