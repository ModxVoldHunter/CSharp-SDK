using System.Globalization;
using System.Threading;

namespace System.Text.RegularExpressions.Symbolic;

internal static class UnicodeCategoryConditions
{
	private static readonly BDD[] s_categories = new BDD[30];

	private static volatile BDD s_whiteSpace;

	private static volatile BDD s_wordLetter;

	private static volatile BDD s_wordLetterForAnchors;

	public static BDD WhiteSpace => s_whiteSpace ?? Interlocked.CompareExchange(ref s_whiteSpace, BDD.Deserialize(UnicodeCategoryRanges.SerializedWhitespaceBDD), null) ?? s_whiteSpace;

	public static BDD GetCategory(UnicodeCategory category)
	{
		return Volatile.Read(ref s_categories[(int)category]) ?? Interlocked.CompareExchange(ref s_categories[(int)category], BDD.Deserialize(UnicodeCategoryRanges.GetSerializedCategory(category)), null) ?? s_categories[(int)category];
	}

	public static BDD WordLetter(CharSetSolver solver)
	{
		return s_wordLetter ?? Interlocked.CompareExchange(ref s_wordLetter, solver.Or(new BDD[8]
		{
			GetCategory(UnicodeCategory.UppercaseLetter),
			GetCategory(UnicodeCategory.LowercaseLetter),
			GetCategory(UnicodeCategory.TitlecaseLetter),
			GetCategory(UnicodeCategory.ModifierLetter),
			GetCategory(UnicodeCategory.OtherLetter),
			GetCategory(UnicodeCategory.NonSpacingMark),
			GetCategory(UnicodeCategory.DecimalDigitNumber),
			GetCategory(UnicodeCategory.ConnectorPunctuation)
		}), null) ?? s_wordLetter;
	}

	public static BDD WordLetterForAnchors(CharSetSolver solver)
	{
		return s_wordLetterForAnchors ?? Interlocked.CompareExchange(ref s_wordLetterForAnchors, solver.Or(WordLetter(solver), solver.CreateBDDFromRange('\u200c', '\u200d')), null) ?? s_wordLetterForAnchors;
	}
}
