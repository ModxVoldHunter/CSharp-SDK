using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;

namespace System.Xml.Schema;

internal struct XsdDateTime
{
	private enum DateTimeTypeCode
	{
		DateTime,
		Time,
		Date,
		GYearMonth,
		GYear,
		GMonthDay,
		GDay,
		GMonth,
		XdrDateTime
	}

	private enum XsdDateTimeKind
	{
		Unspecified,
		Zulu,
		LocalWestOfZulu,
		LocalEastOfZulu
	}

	private struct Parser
	{
		public DateTimeTypeCode typeCode;

		public int year;

		public int month;

		public int day;

		public int hour;

		public int minute;

		public int second;

		public int fraction;

		public XsdDateTimeKind kind;

		public int zoneHour;

		public int zoneMinute;

		private string _text;

		private int _length;

		private static ReadOnlySpan<int> Power10 => RuntimeHelpers.CreateSpan<int>((RuntimeFieldHandle)/*OpCode not supported: LdMemberToken*/);

		public bool Parse(string text, XsdDateTimeFlags kinds)
		{
			_text = text;
			_length = text.Length;
			int i;
			for (i = 0; i < _length && char.IsWhiteSpace(text[i]); i++)
			{
			}
			if (Test(kinds, XsdDateTimeFlags.DateTime | XsdDateTimeFlags.Date | XsdDateTimeFlags.XdrDateTimeNoTz | XsdDateTimeFlags.XdrDateTime) && ParseDate(i))
			{
				if (Test(kinds, XsdDateTimeFlags.DateTime) && ParseChar(i + s_lzyyyy_MM_dd, 'T') && ParseTimeAndZoneAndWhitespace(i + s_lzyyyy_MM_ddT))
				{
					typeCode = DateTimeTypeCode.DateTime;
					return true;
				}
				if (Test(kinds, XsdDateTimeFlags.Date) && ParseZoneAndWhitespace(i + s_lzyyyy_MM_dd))
				{
					typeCode = DateTimeTypeCode.Date;
					return true;
				}
				if (Test(kinds, XsdDateTimeFlags.XdrDateTime) && (ParseZoneAndWhitespace(i + s_lzyyyy_MM_dd) || (ParseChar(i + s_lzyyyy_MM_dd, 'T') && ParseTimeAndZoneAndWhitespace(i + s_lzyyyy_MM_ddT))))
				{
					typeCode = DateTimeTypeCode.XdrDateTime;
					return true;
				}
				if (Test(kinds, XsdDateTimeFlags.XdrDateTimeNoTz))
				{
					if (!ParseChar(i + s_lzyyyy_MM_dd, 'T'))
					{
						typeCode = DateTimeTypeCode.XdrDateTime;
						return true;
					}
					if (ParseTimeAndWhitespace(i + s_lzyyyy_MM_ddT))
					{
						typeCode = DateTimeTypeCode.XdrDateTime;
						return true;
					}
				}
			}
			if (Test(kinds, XsdDateTimeFlags.Time) && ParseTimeAndZoneAndWhitespace(i))
			{
				year = 1904;
				month = 1;
				day = 1;
				typeCode = DateTimeTypeCode.Time;
				return true;
			}
			if (Test(kinds, XsdDateTimeFlags.XdrTimeNoTz) && ParseTimeAndWhitespace(i))
			{
				year = 1904;
				month = 1;
				day = 1;
				typeCode = DateTimeTypeCode.Time;
				return true;
			}
			if (Test(kinds, XsdDateTimeFlags.GYearMonth | XsdDateTimeFlags.GYear) && Parse4Dig(i, ref year) && 1 <= year)
			{
				if (Test(kinds, XsdDateTimeFlags.GYearMonth) && ParseChar(i + s_lzyyyy, '-') && Parse2Dig(i + s_lzyyyy_, ref month) && 1 <= month && month <= 12 && ParseZoneAndWhitespace(i + s_lzyyyy_MM))
				{
					day = 1;
					typeCode = DateTimeTypeCode.GYearMonth;
					return true;
				}
				if (Test(kinds, XsdDateTimeFlags.GYear) && ParseZoneAndWhitespace(i + s_lzyyyy))
				{
					month = 1;
					day = 1;
					typeCode = DateTimeTypeCode.GYear;
					return true;
				}
			}
			if (Test(kinds, XsdDateTimeFlags.GMonthDay | XsdDateTimeFlags.GMonth) && ParseChar(i, '-') && ParseChar(i + s_Lz_, '-') && Parse2Dig(i + s_Lz__, ref month) && 1 <= month && month <= 12)
			{
				if (Test(kinds, XsdDateTimeFlags.GMonthDay) && ParseChar(i + s_lz__mm, '-') && Parse2Dig(i + s_lz__mm_, ref day) && 1 <= day && day <= DateTime.DaysInMonth(1904, month) && ParseZoneAndWhitespace(i + s_lz__mm_dd))
				{
					year = 1904;
					typeCode = DateTimeTypeCode.GMonthDay;
					return true;
				}
				if (Test(kinds, XsdDateTimeFlags.GMonth) && (ParseZoneAndWhitespace(i + s_lz__mm) || (ParseChar(i + s_lz__mm, '-') && ParseChar(i + s_lz__mm_, '-') && ParseZoneAndWhitespace(i + s_lz__mm__))))
				{
					year = 1904;
					day = 1;
					typeCode = DateTimeTypeCode.GMonth;
					return true;
				}
			}
			if (Test(kinds, XsdDateTimeFlags.GDay) && ParseChar(i, '-') && ParseChar(i + s_Lz_, '-') && ParseChar(i + s_Lz__, '-') && Parse2Dig(i + s_Lz___, ref day) && 1 <= day && day <= DateTime.DaysInMonth(1904, 1) && ParseZoneAndWhitespace(i + s_lz___dd))
			{
				year = 1904;
				month = 1;
				typeCode = DateTimeTypeCode.GDay;
				return true;
			}
			return false;
		}

		private bool ParseDate(int start)
		{
			if (Parse4Dig(start, ref year) && 1 <= year && ParseChar(start + s_lzyyyy, '-') && Parse2Dig(start + s_lzyyyy_, ref month) && 1 <= month && month <= 12 && ParseChar(start + s_lzyyyy_MM, '-') && Parse2Dig(start + s_lzyyyy_MM_, ref day) && 1 <= day)
			{
				return day <= DateTime.DaysInMonth(year, month);
			}
			return false;
		}

		private bool ParseTimeAndZoneAndWhitespace(int start)
		{
			if (ParseTime(ref start) && ParseZoneAndWhitespace(start))
			{
				return true;
			}
			return false;
		}

		private bool ParseTimeAndWhitespace(int start)
		{
			if (ParseTime(ref start))
			{
				while (start < _length)
				{
					start++;
				}
				return start == _length;
			}
			return false;
		}

		private bool ParseTime(ref int start)
		{
			if (Parse2Dig(start, ref hour) && hour < 24 && ParseChar(start + s_lzHH, ':') && Parse2Dig(start + s_lzHH_, ref minute) && minute < 60 && ParseChar(start + s_lzHH_mm, ':') && Parse2Dig(start + s_lzHH_mm_, ref second) && second < 60)
			{
				start += s_lzHH_mm_ss;
				if (ParseChar(start, '.'))
				{
					fraction = 0;
					int num = 0;
					int num2 = 0;
					while (++start < _length)
					{
						int num3 = _text[start] - 48;
						if (9u < (uint)num3)
						{
							break;
						}
						if (num < 7)
						{
							fraction = fraction * 10 + num3;
						}
						else if (num == 7)
						{
							if (5 < num3)
							{
								num2 = 1;
							}
							else if (num3 == 5)
							{
								num2 = -1;
							}
						}
						else if (num2 < 0 && num3 != 0)
						{
							num2 = 1;
						}
						num++;
					}
					if (num < 7)
					{
						if (num == 0)
						{
							return false;
						}
						fraction *= Power10[7 - num];
					}
					else
					{
						if (num2 < 0)
						{
							num2 = fraction & 1;
						}
						fraction += num2;
					}
				}
				return true;
			}
			hour = 0;
			return false;
		}

		private bool ParseZoneAndWhitespace(int start)
		{
			if (start < _length)
			{
				char c = _text[start];
				if (c == 'Z' || c == 'z')
				{
					kind = XsdDateTimeKind.Zulu;
					start++;
				}
				else if (start + 5 < _length && Parse2Dig(start + s_Lz_, ref zoneHour) && zoneHour <= 99 && ParseChar(start + s_lz_zz, ':') && Parse2Dig(start + s_lz_zz_, ref zoneMinute) && zoneMinute <= 99)
				{
					switch (c)
					{
					case '-':
						kind = XsdDateTimeKind.LocalWestOfZulu;
						start += s_lz_zz_zz;
						break;
					case '+':
						kind = XsdDateTimeKind.LocalEastOfZulu;
						start += s_lz_zz_zz;
						break;
					}
				}
			}
			while (start < _length && char.IsWhiteSpace(_text[start]))
			{
				start++;
			}
			return start == _length;
		}

		private bool Parse4Dig(int start, ref int num)
		{
			if (start + 3 < _length)
			{
				int num2 = _text[start] - 48;
				int num3 = _text[start + 1] - 48;
				int num4 = _text[start + 2] - 48;
				int num5 = _text[start + 3] - 48;
				if (0 <= num2 && num2 < 10 && 0 <= num3 && num3 < 10 && 0 <= num4 && num4 < 10 && 0 <= num5 && num5 < 10)
				{
					num = ((num2 * 10 + num3) * 10 + num4) * 10 + num5;
					return true;
				}
			}
			return false;
		}

		private bool Parse2Dig(int start, ref int num)
		{
			if (start + 1 < _length)
			{
				int num2 = _text[start] - 48;
				int num3 = _text[start + 1] - 48;
				if (0 <= num2 && num2 < 10 && 0 <= num3 && num3 < 10)
				{
					num = num2 * 10 + num3;
					return true;
				}
			}
			return false;
		}

		private bool ParseChar(int start, char ch)
		{
			if (start < _length)
			{
				return _text[start] == ch;
			}
			return false;
		}

		private static bool Test(XsdDateTimeFlags left, XsdDateTimeFlags right)
		{
			return (left & right) != 0;
		}
	}

	private DateTime _dt;

	private uint _extra;

	private static readonly int s_lzyyyy = "yyyy".Length;

	private static readonly int s_lzyyyy_ = "yyyy-".Length;

	private static readonly int s_lzyyyy_MM = "yyyy-MM".Length;

	private static readonly int s_lzyyyy_MM_ = "yyyy-MM-".Length;

	private static readonly int s_lzyyyy_MM_dd = "yyyy-MM-dd".Length;

	private static readonly int s_lzyyyy_MM_ddT = "yyyy-MM-ddT".Length;

	private static readonly int s_lzHH = "HH".Length;

	private static readonly int s_lzHH_ = "HH:".Length;

	private static readonly int s_lzHH_mm = "HH:mm".Length;

	private static readonly int s_lzHH_mm_ = "HH:mm:".Length;

	private static readonly int s_lzHH_mm_ss = "HH:mm:ss".Length;

	private static readonly int s_Lz_ = "-".Length;

	private static readonly int s_lz_zz = "-zz".Length;

	private static readonly int s_lz_zz_ = "-zz:".Length;

	private static readonly int s_lz_zz_zz = "-zz:zz".Length;

	private static readonly int s_Lz__ = "--".Length;

	private static readonly int s_lz__mm = "--MM".Length;

	private static readonly int s_lz__mm_ = "--MM-".Length;

	private static readonly int s_lz__mm__ = "--MM--".Length;

	private static readonly int s_lz__mm_dd = "--MM-dd".Length;

	private static readonly int s_Lz___ = "---".Length;

	private static readonly int s_lz___dd = "---dd".Length;

	private static readonly XmlTypeCode[] s_typeCodes = new XmlTypeCode[8]
	{
		XmlTypeCode.DateTime,
		XmlTypeCode.Time,
		XmlTypeCode.Date,
		XmlTypeCode.GYearMonth,
		XmlTypeCode.GYear,
		XmlTypeCode.GMonthDay,
		XmlTypeCode.GDay,
		XmlTypeCode.GMonth
	};

	private static ReadOnlySpan<int> DaysToMonth365 => RuntimeHelpers.CreateSpan<int>((RuntimeFieldHandle)/*OpCode not supported: LdMemberToken*/);

	private static ReadOnlySpan<int> DaysToMonth366 => RuntimeHelpers.CreateSpan<int>((RuntimeFieldHandle)/*OpCode not supported: LdMemberToken*/);

	private DateTimeTypeCode InternalTypeCode => (DateTimeTypeCode)((_extra & 0xFF000000u) >> 24);

	private XsdDateTimeKind InternalKind => (XsdDateTimeKind)((_extra & 0xFF0000) >> 16);

	public XmlTypeCode TypeCode => s_typeCodes[(int)InternalTypeCode];

	public int Year => _dt.Year;

	public int Month => _dt.Month;

	public int Day => _dt.Day;

	public int Hour => _dt.Hour;

	public int Minute => _dt.Minute;

	public int Second => _dt.Second;

	public int Fraction => (int)(_dt.Ticks % 10000000);

	public int ZoneHour => (int)((_extra & 0xFF00) >> 8);

	public int ZoneMinute => (int)(_extra & 0xFF);

	public XsdDateTime(string text, XsdDateTimeFlags kinds)
	{
		this = default(XsdDateTime);
		Parser parser = default(Parser);
		if (!parser.Parse(text, kinds))
		{
			throw new FormatException(System.SR.Format(System.SR.XmlConvert_BadFormat, text, kinds));
		}
		InitiateXsdDateTime(parser);
	}

	private XsdDateTime(Parser parser)
	{
		this = default(XsdDateTime);
		InitiateXsdDateTime(parser);
	}

	private void InitiateXsdDateTime(Parser parser)
	{
		_dt = new DateTime(parser.year, parser.month, parser.day, parser.hour, parser.minute, parser.second);
		if (parser.fraction != 0)
		{
			_dt = _dt.AddTicks(parser.fraction);
		}
		_extra = (uint)(((int)parser.typeCode << 24) | ((int)parser.kind << 16) | (parser.zoneHour << 8) | parser.zoneMinute);
	}

	internal static bool TryParse(string text, XsdDateTimeFlags kinds, out XsdDateTime result)
	{
		Parser parser = default(Parser);
		if (!parser.Parse(text, kinds))
		{
			result = default(XsdDateTime);
			return false;
		}
		result = new XsdDateTime(parser);
		return true;
	}

	public XsdDateTime(DateTime dateTime, XsdDateTimeFlags kinds)
	{
		_dt = dateTime;
		DateTimeTypeCode dateTimeTypeCode = (DateTimeTypeCode)BitOperations.TrailingZeroCount((uint)kinds);
		int num = 0;
		int num2 = 0;
		XsdDateTimeKind xsdDateTimeKind;
		switch (dateTime.Kind)
		{
		case DateTimeKind.Unspecified:
			xsdDateTimeKind = XsdDateTimeKind.Unspecified;
			break;
		case DateTimeKind.Utc:
			xsdDateTimeKind = XsdDateTimeKind.Zulu;
			break;
		default:
		{
			TimeSpan utcOffset = TimeZoneInfo.Local.GetUtcOffset(dateTime);
			if (utcOffset.Ticks < 0)
			{
				xsdDateTimeKind = XsdDateTimeKind.LocalWestOfZulu;
				num = -utcOffset.Hours;
				num2 = -utcOffset.Minutes;
			}
			else
			{
				xsdDateTimeKind = XsdDateTimeKind.LocalEastOfZulu;
				num = utcOffset.Hours;
				num2 = utcOffset.Minutes;
			}
			break;
		}
		}
		_extra = (uint)(((int)dateTimeTypeCode << 24) | ((int)xsdDateTimeKind << 16) | (num << 8) | num2);
	}

	public XsdDateTime(DateTimeOffset dateTimeOffset)
		: this(dateTimeOffset, XsdDateTimeFlags.DateTime)
	{
	}

	public XsdDateTime(DateTimeOffset dateTimeOffset, XsdDateTimeFlags kinds)
	{
		_dt = dateTimeOffset.DateTime;
		TimeSpan timeSpan = dateTimeOffset.Offset;
		DateTimeTypeCode dateTimeTypeCode = (DateTimeTypeCode)BitOperations.TrailingZeroCount((uint)kinds);
		XsdDateTimeKind xsdDateTimeKind;
		if (!(timeSpan.TotalMinutes < 0.0))
		{
			xsdDateTimeKind = ((!(timeSpan.TotalMinutes > 0.0)) ? XsdDateTimeKind.Zulu : XsdDateTimeKind.LocalEastOfZulu);
		}
		else
		{
			timeSpan = timeSpan.Negate();
			xsdDateTimeKind = XsdDateTimeKind.LocalWestOfZulu;
		}
		_extra = (uint)(((int)dateTimeTypeCode << 24) | ((int)xsdDateTimeKind << 16) | (timeSpan.Hours << 8) | timeSpan.Minutes);
	}

	public DateTime ToZulu()
	{
		return InternalKind switch
		{
			XsdDateTimeKind.Zulu => new DateTime(_dt.Ticks, DateTimeKind.Utc), 
			XsdDateTimeKind.LocalEastOfZulu => new DateTime(_dt.Subtract(new TimeSpan(ZoneHour, ZoneMinute, 0)).Ticks, DateTimeKind.Utc), 
			XsdDateTimeKind.LocalWestOfZulu => new DateTime(_dt.Add(new TimeSpan(ZoneHour, ZoneMinute, 0)).Ticks, DateTimeKind.Utc), 
			_ => _dt, 
		};
	}

	public static implicit operator DateTime(XsdDateTime xdt)
	{
		DateTime dateTime;
		switch (xdt.InternalTypeCode)
		{
		case DateTimeTypeCode.GDay:
		case DateTimeTypeCode.GMonth:
			dateTime = new DateTime(DateTime.Now.Year, xdt.Month, xdt.Day);
			break;
		case DateTimeTypeCode.Time:
		{
			DateTime now = DateTime.Now;
			TimeSpan value = new DateTime(now.Year, now.Month, now.Day) - new DateTime(xdt.Year, xdt.Month, xdt.Day);
			dateTime = xdt._dt.Add(value);
			break;
		}
		default:
			dateTime = xdt._dt;
			break;
		}
		switch (xdt.InternalKind)
		{
		case XsdDateTimeKind.Zulu:
			dateTime = new DateTime(dateTime.Ticks, DateTimeKind.Utc);
			break;
		case XsdDateTimeKind.LocalEastOfZulu:
		{
			long num = dateTime.Ticks - new TimeSpan(xdt.ZoneHour, xdt.ZoneMinute, 0).Ticks;
			if (num < DateTime.MinValue.Ticks)
			{
				num += TimeZoneInfo.Local.GetUtcOffset(dateTime).Ticks;
				if (num < DateTime.MinValue.Ticks)
				{
					num = DateTime.MinValue.Ticks;
				}
				return new DateTime(num, DateTimeKind.Local);
			}
			dateTime = new DateTime(num, DateTimeKind.Utc).ToLocalTime();
			break;
		}
		case XsdDateTimeKind.LocalWestOfZulu:
		{
			long num = dateTime.Ticks + new TimeSpan(xdt.ZoneHour, xdt.ZoneMinute, 0).Ticks;
			if (num > DateTime.MaxValue.Ticks)
			{
				num += TimeZoneInfo.Local.GetUtcOffset(dateTime).Ticks;
				if (num > DateTime.MaxValue.Ticks)
				{
					num = DateTime.MaxValue.Ticks;
				}
				return new DateTime(num, DateTimeKind.Local);
			}
			dateTime = new DateTime(num, DateTimeKind.Utc).ToLocalTime();
			break;
		}
		}
		return dateTime;
	}

	public static implicit operator DateTimeOffset(XsdDateTime xdt)
	{
		DateTime dateTime;
		switch (xdt.InternalTypeCode)
		{
		case DateTimeTypeCode.GDay:
		case DateTimeTypeCode.GMonth:
			dateTime = new DateTime(DateTime.Now.Year, xdt.Month, xdt.Day);
			break;
		case DateTimeTypeCode.Time:
		{
			DateTime now = DateTime.Now;
			TimeSpan value = new DateTime(now.Year, now.Month, now.Day) - new DateTime(xdt.Year, xdt.Month, xdt.Day);
			dateTime = xdt._dt.Add(value);
			break;
		}
		default:
			dateTime = xdt._dt;
			break;
		}
		return xdt.InternalKind switch
		{
			XsdDateTimeKind.LocalEastOfZulu => new DateTimeOffset(dateTime, new TimeSpan(xdt.ZoneHour, xdt.ZoneMinute, 0)), 
			XsdDateTimeKind.LocalWestOfZulu => new DateTimeOffset(dateTime, new TimeSpan(-xdt.ZoneHour, -xdt.ZoneMinute, 0)), 
			XsdDateTimeKind.Zulu => new DateTimeOffset(dateTime, new TimeSpan(0L)), 
			_ => new DateTimeOffset(dateTime, TimeZoneInfo.Local.GetUtcOffset(dateTime)), 
		};
	}

	public override string ToString()
	{
		Span<char> destination = stackalloc char[64];
		int charsWritten;
		bool flag = TryFormat(destination, out charsWritten);
		return destination.Slice(0, charsWritten).ToString();
	}

	public bool TryFormat(Span<char> destination, out int charsWritten)
	{
		System.Text.ValueStringBuilder vsb = new System.Text.ValueStringBuilder(destination);
		switch (InternalTypeCode)
		{
		case DateTimeTypeCode.DateTime:
			PrintDate(ref vsb);
			vsb.Append('T');
			PrintTime(ref vsb);
			break;
		case DateTimeTypeCode.Time:
			PrintTime(ref vsb);
			break;
		case DateTimeTypeCode.Date:
			PrintDate(ref vsb);
			break;
		case DateTimeTypeCode.GYearMonth:
			vsb.AppendSpanFormattable(Year, "D4");
			vsb.Append('-');
			vsb.AppendSpanFormattable(Month, "D2");
			break;
		case DateTimeTypeCode.GYear:
			vsb.AppendSpanFormattable(Year, "D4");
			break;
		case DateTimeTypeCode.GMonthDay:
			vsb.Append("--");
			vsb.AppendSpanFormattable(Month, "D2");
			vsb.Append('-');
			vsb.AppendSpanFormattable(Day, "D2");
			break;
		case DateTimeTypeCode.GDay:
			vsb.Append("---");
			vsb.AppendSpanFormattable(Day, "D2");
			break;
		case DateTimeTypeCode.GMonth:
			vsb.Append("--");
			vsb.AppendSpanFormattable(Month, "D2");
			vsb.Append("--");
			break;
		}
		PrintZone(ref vsb);
		charsWritten = vsb.Length;
		return destination.Length >= vsb.Length;
	}

	private void PrintDate(ref System.Text.ValueStringBuilder vsb)
	{
		Span<char> text = vsb.AppendSpan(s_lzyyyy_MM_dd);
		GetYearMonthDay(out var year, out var month, out var day);
		WriteXDigits(text, 0, year, 4);
		text[s_lzyyyy] = '-';
		Write2Digits(text, s_lzyyyy_, month);
		text[s_lzyyyy_MM] = '-';
		Write2Digits(text, s_lzyyyy_MM_, day);
	}

	private void GetYearMonthDay(out int year, out int month, out int day)
	{
		long ticks = _dt.Ticks;
		int num = (int)(ticks / 864000000000L);
		int num2 = num / 146097;
		num -= num2 * 146097;
		int num3 = num / 36524;
		if (num3 == 4)
		{
			num3 = 3;
		}
		num -= num3 * 36524;
		int num4 = num / 1461;
		num -= num4 * 1461;
		int num5 = num / 365;
		if (num5 == 4)
		{
			num5 = 3;
		}
		year = num2 * 400 + num3 * 100 + num4 * 4 + num5 + 1;
		num -= num5 * 365;
		ReadOnlySpan<int> readOnlySpan = ((num5 == 3 && (num4 != 24 || num3 == 3)) ? DaysToMonth366 : DaysToMonth365);
		month = (num >> 5) + 1;
		while (num >= readOnlySpan[month])
		{
			month++;
		}
		day = num - readOnlySpan[month - 1] + 1;
	}

	private void PrintTime(ref System.Text.ValueStringBuilder vsb)
	{
		Span<char> text = vsb.AppendSpan(s_lzHH_mm_ss);
		Write2Digits(text, 0, Hour);
		text[s_lzHH] = ':';
		Write2Digits(text, s_lzHH_, Minute);
		text[s_lzHH_mm] = ':';
		Write2Digits(text, s_lzHH_mm_, Second);
		int num = Fraction;
		if (num != 0)
		{
			int num2 = 7;
			while (num % 10 == 0)
			{
				num2--;
				num /= 10;
			}
			text = vsb.AppendSpan(num2 + 1);
			text[0] = '.';
			WriteXDigits(text, 1, num, num2);
		}
	}

	private void PrintZone(ref System.Text.ValueStringBuilder vsb)
	{
		switch (InternalKind)
		{
		case XsdDateTimeKind.Zulu:
			vsb.Append('Z');
			break;
		case XsdDateTimeKind.LocalWestOfZulu:
		{
			Span<char> text = vsb.AppendSpan(s_lz_zz_zz);
			text[0] = '-';
			Write2Digits(text, s_Lz_, ZoneHour);
			text[s_lz_zz] = ':';
			Write2Digits(text, s_lz_zz_, ZoneMinute);
			break;
		}
		case XsdDateTimeKind.LocalEastOfZulu:
		{
			Span<char> text = vsb.AppendSpan(s_lz_zz_zz);
			text[0] = '+';
			Write2Digits(text, s_Lz_, ZoneHour);
			text[s_lz_zz] = ':';
			Write2Digits(text, s_lz_zz_, ZoneMinute);
			break;
		}
		}
	}

	private static void WriteXDigits(Span<char> text, int start, int value, int digits)
	{
		while (digits-- != 0)
		{
			text[start + digits] = (char)(value % 10 + 48);
			value /= 10;
		}
	}

	private static void Write2Digits(Span<char> text, int start, int value)
	{
		text[start] = (char)(value / 10 + 48);
		text[start + 1] = (char)(value % 10 + 48);
	}
}
