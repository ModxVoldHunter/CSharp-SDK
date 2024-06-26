using System.Globalization;

namespace System;

internal struct ParsingInfo
{
	internal Calendar calendar;

	internal int dayOfWeek;

	internal DateTimeParse.TM timeMark;

	internal bool fUseHour12;

	internal bool fUseTwoDigitYear;

	internal bool fAllowInnerWhite;

	internal bool fAllowTrailingWhite;

	internal bool fUseHebrewNumberParser;

	public ParsingInfo(Calendar calendar)
	{
		fUseHour12 = false;
		fUseTwoDigitYear = false;
		fAllowInnerWhite = false;
		fAllowTrailingWhite = false;
		fUseHebrewNumberParser = false;
		this.calendar = calendar;
		dayOfWeek = -1;
		timeMark = DateTimeParse.TM.NotSet;
	}
}
