namespace System;

internal enum ParseFailureKind
{
	None,
	ArgumentNull_String,
	Format_BadDatePattern,
	Format_BadDateTime,
	Format_BadDateTimeCalendar,
	Format_BadDayOfWeek,
	Format_BadFormatSpecifier,
	Format_BadQuote,
	Format_DateOutOfRange,
	Format_MissingIncompleteDate,
	Format_NoFormatSpecifier,
	Format_OffsetOutOfRange,
	Format_RepeatDateTimePattern,
	Format_UnknownDateTimeWord,
	Format_UTCOutOfRange,
	Argument_InvalidDateStyles,
	Argument_BadFormatSpecifier,
	Format_BadDateOnly,
	Format_BadTimeOnly,
	Format_DateTimeOnlyContainsNoneDateParts
}
