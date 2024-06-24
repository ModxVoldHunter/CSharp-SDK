namespace System.Text.RegularExpressions;

internal enum FindNextStartingPositionMode
{
	LeadingAnchor_LeftToRight_Beginning,
	LeadingAnchor_LeftToRight_Start,
	LeadingAnchor_LeftToRight_EndZ,
	LeadingAnchor_LeftToRight_End,
	LeadingAnchor_RightToLeft_Beginning,
	LeadingAnchor_RightToLeft_Start,
	LeadingAnchor_RightToLeft_EndZ,
	LeadingAnchor_RightToLeft_End,
	TrailingAnchor_FixedLength_LeftToRight_End,
	TrailingAnchor_FixedLength_LeftToRight_EndZ,
	LeadingString_LeftToRight,
	LeadingString_RightToLeft,
	LeadingString_OrdinalIgnoreCase_LeftToRight,
	LeadingSet_LeftToRight,
	LeadingSet_RightToLeft,
	LeadingChar_RightToLeft,
	FixedDistanceChar_LeftToRight,
	FixedDistanceString_LeftToRight,
	FixedDistanceSets_LeftToRight,
	LiteralAfterLoop_LeftToRight,
	NoSearch
}
