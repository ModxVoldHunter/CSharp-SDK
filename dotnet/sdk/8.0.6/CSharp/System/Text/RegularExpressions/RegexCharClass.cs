using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Threading;

namespace System.Text.RegularExpressions;

internal sealed class RegexCharClass
{
	internal struct CharClassAnalysisResults
	{
		public bool OnlyRanges;

		public bool ContainsOnlyAscii;

		public bool ContainsNoAscii;

		public bool AllAsciiContained;

		public bool AllNonAsciiContained;

		public int LowerBoundInclusiveIfOnlyRanges;

		public int UpperBoundExclusiveIfOnlyRanges;
	}

	private static readonly Dictionary<string, string> s_definedCategories = new Dictionary<string, string>(38)
	{
		{ "Cc", "\u000f" },
		{ "Cf", "\u0010" },
		{ "Cn", "\u001e" },
		{ "Co", "\u0012" },
		{ "Cs", "\u0011" },
		{ "C", "\0\u000f\u0010\u001e\u0012\u0011\0" },
		{ "Ll", "\u0002" },
		{ "Lm", "\u0004" },
		{ "Lo", "\u0005" },
		{ "Lt", "\u0003" },
		{ "Lu", "\u0001" },
		{ "L", "\0\u0002\u0004\u0005\u0003\u0001\0" },
		{ "__InternalRegexIgnoreCase__", "\0\u0002\u0003\u0001\0" },
		{ "Mc", "\a" },
		{ "Me", "\b" },
		{ "Mn", "\u0006" },
		{ "M", "\0\a\b\u0006\0" },
		{ "Nd", "\t" },
		{ "Nl", "\n" },
		{ "No", "\v" },
		{ "N", "\0\t\n\v\0" },
		{ "Pc", "\u0013" },
		{ "Pd", "\u0014" },
		{ "Pe", "\u0016" },
		{ "Po", "\u0019" },
		{ "Ps", "\u0015" },
		{ "Pf", "\u0018" },
		{ "Pi", "\u0017" },
		{ "P", "\0\u0013\u0014\u0016\u0019\u0015\u0018\u0017\0" },
		{ "Sc", "\u001b" },
		{ "Sk", "\u001c" },
		{ "Sm", "\u001a" },
		{ "So", "\u001d" },
		{ "S", "\0\u001b\u001c\u001a\u001d\0" },
		{ "Zl", "\r" },
		{ "Zp", "\u000e" },
		{ "Zs", "\f" },
		{ "Z", "\0\r\u000e\f\0" }
	};

	private static readonly string[][] s_propTable = new string[112][]
	{
		new string[2] { "IsAlphabeticPresentationForms", "ﬀﭐ" },
		new string[2] { "IsArabic", "\u0600܀" },
		new string[2] { "IsArabicPresentationForms-A", "ﭐ\ufe00" },
		new string[2] { "IsArabicPresentationForms-B", "ﹰ\uff00" },
		new string[2] { "IsArmenian", "\u0530\u0590" },
		new string[2] { "IsArrows", "←∀" },
		new string[2] { "IsBasicLatin", "\0\u0080" },
		new string[2] { "IsBengali", "ঀ\u0a00" },
		new string[2] { "IsBlockElements", "▀■" },
		new string[2] { "IsBopomofo", "\u3100\u3130" },
		new string[2] { "IsBopomofoExtended", "ㆠ㇀" },
		new string[2] { "IsBoxDrawing", "─▀" },
		new string[2] { "IsBraillePatterns", "⠀⤀" },
		new string[2] { "IsBuhid", "ᝀᝠ" },
		new string[2] { "IsCJKCompatibility", "㌀㐀" },
		new string[2] { "IsCJKCompatibilityForms", "︰﹐" },
		new string[2] { "IsCJKCompatibilityIdeographs", "豈ﬀ" },
		new string[2] { "IsCJKRadicalsSupplement", "⺀⼀" },
		new string[2] { "IsCJKSymbolsandPunctuation", "\u3000\u3040" },
		new string[2] { "IsCJKUnifiedIdeographs", "一ꀀ" },
		new string[2] { "IsCJKUnifiedIdeographsExtensionA", "㐀䷀" },
		new string[2] { "IsCherokee", "Ꭰ᐀" },
		new string[2] { "IsCombiningDiacriticalMarks", "\u0300Ͱ" },
		new string[2] { "IsCombiningDiacriticalMarksforSymbols", "\u20d0℀" },
		new string[2] { "IsCombiningHalfMarks", "\ufe20︰" },
		new string[2] { "IsCombiningMarksforSymbols", "\u20d0℀" },
		new string[2] { "IsControlPictures", "␀⑀" },
		new string[2] { "IsCurrencySymbols", "₠\u20d0" },
		new string[2] { "IsCyrillic", "ЀԀ" },
		new string[2] { "IsCyrillicSupplement", "Ԁ\u0530" },
		new string[2] { "IsDevanagari", "\u0900ঀ" },
		new string[2] { "IsDingbats", "✀⟀" },
		new string[2] { "IsEnclosedAlphanumerics", "①─" },
		new string[2] { "IsEnclosedCJKLettersandMonths", "㈀㌀" },
		new string[2] { "IsEthiopic", "ሀᎀ" },
		new string[2] { "IsGeneralPunctuation", "\u2000⁰" },
		new string[2] { "IsGeometricShapes", "■☀" },
		new string[2] { "IsGeorgian", "Ⴀᄀ" },
		new string[2] { "IsGreek", "ͰЀ" },
		new string[2] { "IsGreekExtended", "ἀ\u2000" },
		new string[2] { "IsGreekandCoptic", "ͰЀ" },
		new string[2] { "IsGujarati", "\u0a80\u0b00" },
		new string[2] { "IsGurmukhi", "\u0a00\u0a80" },
		new string[2] { "IsHalfwidthandFullwidthForms", "\uff00\ufff0" },
		new string[2] { "IsHangulCompatibilityJamo", "\u3130㆐" },
		new string[2] { "IsHangulJamo", "ᄀሀ" },
		new string[2] { "IsHangulSyllables", "가ힰ" },
		new string[2] { "IsHanunoo", "ᜠᝀ" },
		new string[2] { "IsHebrew", "\u0590\u0600" },
		new string[2] { "IsHighPrivateUseSurrogates", "\udb80\udc00" },
		new string[2] { "IsHighSurrogates", "\ud800\udb80" },
		new string[2] { "IsHiragana", "\u3040゠" },
		new string[2] { "IsIPAExtensions", "ɐʰ" },
		new string[2] { "IsIdeographicDescriptionCharacters", "⿰\u3000" },
		new string[2] { "IsKanbun", "㆐ㆠ" },
		new string[2] { "IsKangxiRadicals", "⼀\u2fe0" },
		new string[2] { "IsKannada", "ಀ\u0d00" },
		new string[2] { "IsKatakana", "゠\u3100" },
		new string[2] { "IsKatakanaPhoneticExtensions", "ㇰ㈀" },
		new string[2] { "IsKhmer", "ក᠀" },
		new string[2] { "IsKhmerSymbols", "᧠ᨀ" },
		new string[2] { "IsLao", "\u0e80ༀ" },
		new string[2] { "IsLatin-1Supplement", "\u0080Ā" },
		new string[2] { "IsLatinExtended-A", "Āƀ" },
		new string[2] { "IsLatinExtended-B", "ƀɐ" },
		new string[2] { "IsLatinExtendedAdditional", "Ḁἀ" },
		new string[2] { "IsLetterlikeSymbols", "℀⅐" },
		new string[2] { "IsLimbu", "ᤀᥐ" },
		new string[2] { "IsLowSurrogates", "\udc00\ue000" },
		new string[2] { "IsMalayalam", "\u0d00\u0d80" },
		new string[2] { "IsMathematicalOperators", "∀⌀" },
		new string[2] { "IsMiscellaneousMathematicalSymbols-A", "⟀⟰" },
		new string[2] { "IsMiscellaneousMathematicalSymbols-B", "⦀⨀" },
		new string[2] { "IsMiscellaneousSymbols", "☀✀" },
		new string[2] { "IsMiscellaneousSymbolsandArrows", "⬀Ⰰ" },
		new string[2] { "IsMiscellaneousTechnical", "⌀␀" },
		new string[2] { "IsMongolian", "᠀ᢰ" },
		new string[2] { "IsMyanmar", "ကႠ" },
		new string[2] { "IsNumberForms", "⅐←" },
		new string[2] { "IsOgham", "\u1680ᚠ" },
		new string[2] { "IsOpticalCharacterRecognition", "⑀①" },
		new string[2] { "IsOriya", "\u0b00\u0b80" },
		new string[2] { "IsPhoneticExtensions", "ᴀᶀ" },
		new string[2] { "IsPrivateUse", "\ue000豈" },
		new string[2] { "IsPrivateUseArea", "\ue000豈" },
		new string[2] { "IsRunic", "ᚠᜀ" },
		new string[2] { "IsSinhala", "\u0d80\u0e00" },
		new string[2] { "IsSmallFormVariants", "﹐ﹰ" },
		new string[2] { "IsSpacingModifierLetters", "ʰ\u0300" },
		new string[2] { "IsSpecials", "\ufff0" },
		new string[2] { "IsSuperscriptsandSubscripts", "⁰₠" },
		new string[2] { "IsSupplementalArrows-A", "⟰⠀" },
		new string[2] { "IsSupplementalArrows-B", "⤀⦀" },
		new string[2] { "IsSupplementalMathematicalOperators", "⨀⬀" },
		new string[2] { "IsSyriac", "܀ݐ" },
		new string[2] { "IsTagalog", "ᜀᜠ" },
		new string[2] { "IsTagbanwa", "ᝠក" },
		new string[2] { "IsTaiLe", "ᥐᦀ" },
		new string[2] { "IsTamil", "\u0b80\u0c00" },
		new string[2] { "IsTelugu", "\u0c00ಀ" },
		new string[2] { "IsThaana", "ހ߀" },
		new string[2] { "IsThai", "\u0e00\u0e80" },
		new string[2] { "IsTibetan", "ༀက" },
		new string[2] { "IsUnifiedCanadianAboriginalSyllabics", "᐀\u1680" },
		new string[2] { "IsVariationSelectors", "\ufe00︐" },
		new string[2] { "IsYiRadicals", "꒐ꓐ" },
		new string[2] { "IsYiSyllables", "ꀀ꒐" },
		new string[2] { "IsYijingHexagramSymbols", "䷀一" },
		new string[2] { "_xmlC", "-/0;A[_`a{·\u00b8À×Ø÷øĲĴĿŁŉŊſƀǄǍǱǴǶǺȘɐʩʻ\u02c2ː\u02d2\u0300\u0346\u0360\u0362Ά\u038bΌ\u038dΎ\u03a2ΣϏϐϗϚϛϜϝϞϟϠϡϢϴЁЍЎѐёѝў҂\u0483\u0487ҐӅӇӉӋӍӐӬӮӶӸӺԱ\u0557ՙ՚աև\u0591\u05a2\u05a3\u05ba\u05bb־\u05bf׀\u05c1׃\u05c4\u05c5א\u05ebװ׳ءػـ\u0653٠٪\u0670ڸںڿۀۏې۔ە۩\u06eaۮ۰ۺ\u0901ऄअ\u093a\u093c\u094e\u0951\u0955क़।०॰\u0981\u0984অ\u098dএ\u0991ও\u09a9প\u09b1ল\u09b3শ\u09ba\u09bcঽ\u09be\u09c5\u09c7\u09c9\u09cbৎ\u09d7\u09d8ড়\u09deয়\u09e4০৲\u0a02\u0a03ਅ\u0a0bਏ\u0a11ਓ\u0a29ਪ\u0a31ਲ\u0a34ਵ\u0a37ਸ\u0a3a\u0a3c\u0a3d\u0a3e\u0a43\u0a47\u0a49\u0a4b\u0a4eਖ਼\u0a5dਫ਼\u0a5f੦\u0a75\u0a81\u0a84અઌઍ\u0a8eએ\u0a92ઓ\u0aa9પ\u0ab1લ\u0ab4વ\u0aba\u0abc\u0ac6\u0ac7\u0aca\u0acb\u0aceૠૡ૦૰\u0b01\u0b04ଅ\u0b0dଏ\u0b11ଓ\u0b29ପ\u0b31ଲ\u0b34ଶ\u0b3a\u0b3c\u0b44\u0b47\u0b49\u0b4b\u0b4e\u0b56\u0b58ଡ଼\u0b5eୟ\u0b62୦୰\u0b82\u0b84அ\u0b8bஎ\u0b91ஒ\u0b96ங\u0b9bஜ\u0b9dஞ\u0ba0ண\u0ba5ந\u0babமஶஷ\u0bba\u0bbe\u0bc3\u0bc6\u0bc9\u0bca\u0bce\u0bd7\u0bd8௧௰\u0c01\u0c04అ\u0c0dఎ\u0c11ఒ\u0c29పఴవ\u0c3a\u0c3e\u0c45\u0c46\u0c49\u0c4a\u0c4e\u0c55\u0c57ౠ\u0c62౦\u0c70\u0c82಄ಅ\u0c8dಎ\u0c91ಒ\u0ca9ಪ\u0cb4ವ\u0cba\u0cbe\u0cc5\u0cc6\u0cc9\u0cca\u0cce\u0cd5\u0cd7ೞ\u0cdfೠ\u0ce2೦\u0cf0\u0d02ഄഅ\u0d0dഎ\u0d11ഒഩപഺ\u0d3e\u0d44\u0d46\u0d49\u0d4aൎ\u0d57൘ൠ\u0d62൦൰กฯะ\u0e3bเ๏๐๚ກ\u0e83ຄ\u0e85ງຉຊ\u0e8bຍຎດຘນຠມ\u0ea4ລ\u0ea6ວຨສຬອຯະ\u0eba\u0ebb\u0ebeເ\u0ec5ໆ\u0ec7\u0ec8\u0ece໐\u0eda\u0f18༚༠༪\u0f35༶\u0f37༸\u0f39༺\u0f3e\u0f48ཉཪ\u0f71྅\u0f86ྌ\u0f90\u0f96\u0f97\u0f98\u0f99\u0fae\u0fb1\u0fb8\u0fb9\u0fbaႠ\u10c6აჷᄀᄁᄂᄄᄅᄈᄉᄊᄋᄍᄎᄓᄼᄽᄾᄿᅀᅁᅌᅍᅎᅏᅐᅑᅔᅖᅙᅚᅟᅢᅣᅤᅥᅦᅧᅨᅩᅪᅭᅯᅲᅴᅵᅶᆞᆟᆨᆩᆫᆬᆮᆰᆷᆹᆺᆻᆼᇃᇫᇬᇰᇱᇹᇺḀẜẠỺἀ\u1f16Ἐ\u1f1eἠ\u1f46Ὀ\u1f4eὐ\u1f58Ὑ\u1f5aὛ\u1f5cὝ\u1f5eὟ\u1f7eᾀ\u1fb5ᾶ\u1fbdι\u1fbfῂ\u1fc5ῆ\u1fcdῐ\u1fd4ῖ\u1fdcῠ\u1fedῲ\u1ff5ῶ\u1ffd\u20d0\u20dd\u20e1\u20e2Ω℧Kℬ℮ℯↀↃ々〆〇〈〡〰〱〶ぁゕ\u3099\u309bゝゟァ・ーヿㄅㄭ一龦가\ud7a4" },
		new string[2] { "_xmlD", "0:٠٪۰ۺ०॰০ৰ੦\u0a70૦૰୦୰௧௰౦\u0c70೦\u0cf0൦൰๐๚໐\u0eda༠༪၀၊፩፲០\u17ea᠐\u181a０：" },
		new string[2] { "_xmlI", ":;A[_`a{À×Ø÷øĲĴĿŁŉŊſƀǄǍǱǴǶǺȘɐʩʻ\u02c2Ά·Έ\u038bΌ\u038dΎ\u03a2ΣϏϐϗϚϛϜϝϞϟϠϡϢϴЁЍЎѐёѝў҂ҐӅӇӉӋӍӐӬӮӶӸӺԱ\u0557ՙ՚աևא\u05ebװ׳ءػف\u064bٱڸںڿۀۏې۔ە\u06d6ۥ\u06e7अ\u093aऽ\u093eक़\u0962অ\u098dএ\u0991ও\u09a9প\u09b1ল\u09b3শ\u09baড়\u09deয়\u09e2ৰ৲ਅ\u0a0bਏ\u0a11ਓ\u0a29ਪ\u0a31ਲ\u0a34ਵ\u0a37ਸ\u0a3aਖ਼\u0a5dਫ਼\u0a5fੲ\u0a75અઌઍ\u0a8eએ\u0a92ઓ\u0aa9પ\u0ab1લ\u0ab4વ\u0abaઽ\u0abeૠૡଅ\u0b0dଏ\u0b11ଓ\u0b29ପ\u0b31ଲ\u0b34ଶ\u0b3aଽ\u0b3eଡ଼\u0b5eୟ\u0b62அ\u0b8bஎ\u0b91ஒ\u0b96ங\u0b9bஜ\u0b9dஞ\u0ba0ண\u0ba5ந\u0babமஶஷ\u0bbaఅ\u0c0dఎ\u0c11ఒ\u0c29పఴవ\u0c3aౠ\u0c62ಅ\u0c8dಎ\u0c91ಒ\u0ca9ಪ\u0cb4ವ\u0cbaೞ\u0cdfೠ\u0ce2അ\u0d0dഎ\u0d11ഒഩപഺൠ\u0d62กฯะ\u0e31า\u0e34เๆກ\u0e83ຄ\u0e85ງຉຊ\u0e8bຍຎດຘນຠມ\u0ea4ລ\u0ea6ວຨສຬອຯະ\u0eb1າ\u0eb4ຽ\u0ebeເ\u0ec5ཀ\u0f48ཉཪႠ\u10c6აჷᄀᄁᄂᄄᄅᄈᄉᄊᄋᄍᄎᄓᄼᄽᄾᄿᅀᅁᅌᅍᅎᅏᅐᅑᅔᅖᅙᅚᅟᅢᅣᅤᅥᅦᅧᅨᅩᅪᅭᅯᅲᅴᅵᅶᆞᆟᆨᆩᆫᆬᆮᆰᆷᆹᆺᆻᆼᇃᇫᇬᇰᇱᇹᇺḀẜẠỺἀ\u1f16Ἐ\u1f1eἠ\u1f46Ὀ\u1f4eὐ\u1f58Ὑ\u1f5aὛ\u1f5cὝ\u1f5eὟ\u1f7eᾀ\u1fb5ᾶ\u1fbdι\u1fbfῂ\u1fc5ῆ\u1fcdῐ\u1fd4ῖ\u1fdcῠ\u1fedῲ\u1ff5ῶ\u1ffdΩ℧Kℬ℮ℯↀↃ〇〈〡\u302aぁゕァ・ㄅㄭ一龦가\ud7a4" },
		new string[2] { "_xmlW", "$%+,0:<?A[^_`{|}~\u007f¢«¬\u00ad®·\u00b8»¼¿ÀȡȢȴɐʮʰ\u02ef\u0300\u0350\u0360ͰʹͶͺͻ\u0384·Έ\u038bΌ\u038dΎ\u03a2ΣϏϐϷЀ\u0487\u0488ӏӐӶӸӺԀԐԱ\u0557ՙ՚աֈ\u0591\u05a2\u05a3\u05ba\u05bb־\u05bf׀\u05c1׃\u05c4\u05c5א\u05ebװ׳ءػـ\u0656٠٪ٮ۔ە\u06dd۞ۮ۰ۿܐܭ\u0730\u074bހ\u07b2\u0901ऄअ\u093a\u093c\u094eॐ\u0955क़।०॰\u0981\u0984অ\u098dএ\u0991ও\u09a9প\u09b1ল\u09b3শ\u09ba\u09bcঽ\u09be\u09c5\u09c7\u09c9\u09cbৎ\u09d7\u09d8ড়\u09deয়\u09e4০৻\u0a02\u0a03ਅ\u0a0bਏ\u0a11ਓ\u0a29ਪ\u0a31ਲ\u0a34ਵ\u0a37ਸ\u0a3a\u0a3c\u0a3d\u0a3e\u0a43\u0a47\u0a49\u0a4b\u0a4eਖ਼\u0a5dਫ਼\u0a5f੦\u0a75\u0a81\u0a84અઌઍ\u0a8eએ\u0a92ઓ\u0aa9પ\u0ab1લ\u0ab4વ\u0aba\u0abc\u0ac6\u0ac7\u0aca\u0acb\u0aceૐ\u0ad1ૠૡ૦૰\u0b01\u0b04ଅ\u0b0dଏ\u0b11ଓ\u0b29ପ\u0b31ଲ\u0b34ଶ\u0b3a\u0b3c\u0b44\u0b47\u0b49\u0b4b\u0b4e\u0b56\u0b58ଡ଼\u0b5eୟ\u0b62୦ୱ\u0b82\u0b84அ\u0b8bஎ\u0b91ஒ\u0b96ங\u0b9bஜ\u0b9dஞ\u0ba0ண\u0ba5ந\u0babமஶஷ\u0bba\u0bbe\u0bc3\u0bc6\u0bc9\u0bca\u0bce\u0bd7\u0bd8௧௳\u0c01\u0c04అ\u0c0dఎ\u0c11ఒ\u0c29పఴవ\u0c3a\u0c3e\u0c45\u0c46\u0c49\u0c4a\u0c4e\u0c55\u0c57ౠ\u0c62౦\u0c70\u0c82಄ಅ\u0c8dಎ\u0c91ಒ\u0ca9ಪ\u0cb4ವ\u0cba\u0cbe\u0cc5\u0cc6\u0cc9\u0cca\u0cce\u0cd5\u0cd7ೞ\u0cdfೠ\u0ce2೦\u0cf0\u0d02ഄഅ\u0d0dഎ\u0d11ഒഩപഺ\u0d3e\u0d44\u0d46\u0d49\u0d4aൎ\u0d57൘ൠ\u0d62൦൰\u0d82\u0d84අ\u0d97ක\u0db2ඳ\u0dbcල\u0dbeව\u0dc7\u0dca\u0dcb\u0dcf\u0dd5\u0dd6\u0dd7\u0dd8\u0de0\u0df2෴ก\u0e3b฿๏๐๚ກ\u0e83ຄ\u0e85ງຉຊ\u0e8bຍຎດຘນຠມ\u0ea4ລ\u0ea6ວຨສຬອ\u0eba\u0ebb\u0ebeເ\u0ec5ໆ\u0ec7\u0ec8\u0ece໐\u0edaໜໞༀ༄༓༺\u0f3e\u0f48ཉཫ\u0f71྅\u0f86ྌ\u0f90\u0f98\u0f99\u0fbd྾\u0fcd࿏࿐ကဢဣဨဩ\u102b\u102c\u1033\u1036\u103a၀၊ၐၚႠ\u10c6აჹᄀᅚᅟᆣᆨᇺሀሇለቇቈ\u1249ቊ\u124eቐ\u1257ቘ\u1259ቚ\u125eበኇኈ\u1289ኊ\u128eነኯኰ\u12b1ኲ\u12b6ኸ\u12bfዀ\u12c1ዂ\u12c6ወዏዐ\u12d7ዘዯደጏጐ\u1311ጒ\u1316ጘጟጠፇፈ\u135b፩\u137dᎠᏵᐁ᙭ᙯᙷᚁ᚛ᚠ᛫ᛮᛱᜀᜍᜎ\u1715ᜠ᜵ᝀ\u1754ᝠ\u176dᝮ\u1771\u1772\u1774ក។ៗ៘៛\u17dd០\u17ea\u180b\u180e᠐\u181aᠠᡸᢀᢪḀẜẠỺἀ\u1f16Ἐ\u1f1eἠ\u1f46Ὀ\u1f4eὐ\u1f58Ὑ\u1f5aὛ\u1f5cὝ\u1f5eὟ\u1f7eᾀ\u1fb5ᾶ\u1fc5ῆ\u1fd4ῖ\u1fdc\u1fdd\u1ff0ῲ\u1ff5ῶ\u1fff⁄⁅⁒⁓⁰\u2072⁴⁽ⁿ₍₠₲\u20d0\u20eb℀℻ℽ⅌⅓ↄ←〈⌫⎴⎷⏏␀\u2427⑀\u244b①⓿─☔☖☘☙♾⚀⚊✁✅✆✊✌✨✩❌❍❎❏❓❖❗❘❟❡❨❶➕➘➰➱➿⟐⟦⟰⦃⦙⧘⧜⧼⧾⬀⺀\u2e9a⺛\u2ef4⼀\u2fd6⿰\u2ffc〄〈〒〔〠〰〱〽〾\u3040ぁ\u3097\u3099゠ァ・ー\u3100ㄅㄭㄱ\u318f㆐ㆸㇰ㈝㈠㉄㉑㉼㉿㋌㋐㋿㌀㍷㍻㏞㏠㏿㐀䶶一龦ꀀ\ua48d꒐\ua4c7가\ud7a4豈郞侮恵ﬀ\ufb07ﬓ\ufb18יִ\ufb37טּ\ufb3dמּ\ufb3fנּ\ufb42ףּ\ufb45צּ\ufbb2ﯓ﴾ﵐ\ufd90ﶒ\ufdc8ﷰ﷽\ufe00︐\ufe20\ufe24﹢﹣﹤\ufe67﹩﹪ﹰ\ufe75ﹶ\ufefd＄％＋，０：＜？Ａ［\uff3e\uff3f\uff40｛｜｝～｟ｦ\uffbfￂ\uffc8ￊ\uffd0ￒ\uffd8ￚ\uffdd￠\uffe7￨\uffef￼\ufffe" }
	};

	private List<(char First, char Last)> _rangelist;

	private StringBuilder _categories;

	private RegexCharClass _subtractor;

	private bool _negate;

	private RegexCaseBehavior _caseBehavior;

	private static readonly string[] s_asciiStrings = new string[128];

	private static readonly string[] s_asciiLetterPairStrings = new string[26];

	public bool CanMerge
	{
		get
		{
			if (!_negate)
			{
				return _subtractor == null;
			}
			return false;
		}
	}

	public bool Negate
	{
		set
		{
			_negate = value;
		}
	}

	private static ReadOnlySpan<byte> WordCharAsciiLookup => new byte[16]
	{
		0, 0, 0, 0, 0, 0, 255, 3, 254, 255,
		255, 135, 254, 255, 255, 7
	};

	public RegexCharClass()
	{
	}

	private RegexCharClass(bool negate, List<(char First, char Last)> ranges, StringBuilder categories, RegexCharClass subtraction)
	{
		_rangelist = ranges;
		_categories = categories;
		_negate = negate;
		_subtractor = subtraction;
	}

	public void AddChar(char c)
	{
		AddRange(c, c);
	}

	public void AddCharClass(RegexCharClass cc)
	{
		List<(char First, char Last)> rangelist = cc._rangelist;
		if (rangelist != null && rangelist.Count != 0)
		{
			EnsureRangeList().AddRange(cc._rangelist);
		}
		if (cc._categories != null)
		{
			EnsureCategories().Append(cc._categories);
		}
	}

	public bool TryAddCharClass(RegexCharClass cc)
	{
		if (cc.CanMerge && CanMerge)
		{
			AddCharClass(cc);
			return true;
		}
		return false;
	}

	private StringBuilder EnsureCategories()
	{
		return _categories ?? (_categories = new StringBuilder());
	}

	private List<(char First, char Last)> EnsureRangeList()
	{
		return _rangelist ?? (_rangelist = new List<(char, char)>(6));
	}

	private void AddRanges(ReadOnlySpan<char> set)
	{
		List<(char, char)> list = EnsureRangeList();
		int i;
		for (i = 0; i < set.Length - 1; i += 2)
		{
			list.Add((set[i], (char)(set[i + 1] - 1)));
		}
		if (i < set.Length)
		{
			list.Add((set[i], '\uffff'));
		}
	}

	public void AddSubtraction(RegexCharClass sub)
	{
		_subtractor = sub;
	}

	public void AddRange(char first, char last)
	{
		EnsureRangeList().Add((first, last));
	}

	public void AddCategoryFromName(string categoryName, bool invert, bool caseInsensitive, string pattern, int currentPos)
	{
		if (s_definedCategories.TryGetValue(categoryName, out var value) && !categoryName.Equals("__InternalRegexIgnoreCase__"))
		{
			if (caseInsensitive && (categoryName.Equals("Ll") || categoryName.Equals("Lu") || categoryName.Equals("Lt")))
			{
				value = s_definedCategories["__InternalRegexIgnoreCase__"];
			}
			StringBuilder stringBuilder = EnsureCategories();
			if (invert)
			{
				for (int i = 0; i < value.Length; i++)
				{
					short num = (short)value[i];
					stringBuilder.Append((char)(-num));
				}
			}
			else
			{
				stringBuilder.Append(value);
			}
		}
		else
		{
			AddRanges(RangesFromProperty(categoryName, invert, pattern, currentPos));
		}
	}

	private void AddCategory(string category)
	{
		EnsureCategories().Append(category);
	}

	public void AddCaseEquivalences(CultureInfo culture)
	{
		List<(char, char)> rangelist = _rangelist;
		if (rangelist == null)
		{
			return;
		}
		int count = rangelist.Count;
		for (int i = 0; i < count; i++)
		{
			(char, char) tuple = rangelist[i];
			if (tuple.Item1 == tuple.Item2)
			{
				if (RegexCaseEquivalences.TryFindCaseEquivalencesForCharWithIBehavior(tuple.Item1, culture, ref _caseBehavior, out var equivalences))
				{
					ReadOnlySpan<char> readOnlySpan = equivalences;
					for (int j = 0; j < readOnlySpan.Length; j++)
					{
						char c = readOnlySpan[j];
						AddChar(c);
					}
				}
			}
			else
			{
				AddCaseEquivalenceRange(tuple.Item1, tuple.Item2, culture);
			}
		}
	}

	private void AddCaseEquivalenceRange(char chMin, char chMax, CultureInfo culture)
	{
		for (int i = chMin; i <= chMax; i++)
		{
			if (RegexCaseEquivalences.TryFindCaseEquivalencesForCharWithIBehavior((char)i, culture, ref _caseBehavior, out var equivalences))
			{
				ReadOnlySpan<char> readOnlySpan = equivalences;
				for (int j = 0; j < readOnlySpan.Length; j++)
				{
					char c = readOnlySpan[j];
					AddChar(c);
				}
			}
		}
	}

	public void AddWord(bool ecma, bool negate)
	{
		if (ecma)
		{
			AddRanges((negate ? "\00:A[_`a{İı" : "0:A[_`a{İı").AsSpan());
		}
		else
		{
			AddCategory(negate ? "\0\ufffe￼\ufffb\ufffd\uffff\ufffa\ufff7￭\0" : "\0\u0002\u0004\u0005\u0003\u0001\u0006\t\u0013\0");
		}
	}

	public void AddSpace(bool ecma, bool negate)
	{
		if (ecma)
		{
			AddRanges((negate ? "\0\t\u000e !" : "\t\u000e !").AsSpan());
		}
		else
		{
			AddCategory(negate ? "ﾜ" : "d");
		}
	}

	public void AddDigit(bool ecma, bool negate, string pattern, int currentPos)
	{
		if (ecma)
		{
			AddRanges((negate ? "\00:" : "0:").AsSpan());
		}
		else
		{
			AddCategoryFromName("Nd", negate, caseInsensitive: false, pattern, currentPos);
		}
	}

	public static string ConvertOldStringsToClass(string set, string category)
	{
		bool flag = set.Length >= 2 && set[0] == '\0' && set[1] == '\0';
		int num = 3 + set.Length + category.Length;
		if (flag)
		{
			num -= 2;
		}
		return string.Create(num, (set, category, flag), delegate(Span<char> span, (string set, string category, bool startsWithNulls) state)
		{
			int start;
			if (state.startsWithNulls)
			{
				span[0] = '\u0001';
				span[1] = (char)(state.set.Length - 2);
				span[2] = (char)state.category.Length;
				state.set.AsSpan(2).CopyTo(span.Slice(3));
				start = 3 + state.set.Length - 2;
			}
			else
			{
				span[0] = '\0';
				span[1] = (char)state.set.Length;
				span[2] = (char)state.category.Length;
				state.set.AsSpan().CopyTo(span.Slice(3));
				start = 3 + state.set.Length;
			}
			state.category.AsSpan().CopyTo(span.Slice(start));
		});
	}

	public static char SingletonChar(string set)
	{
		return set[3];
	}

	public static bool IsMergeable(string charClass)
	{
		if (charClass != null && !IsNegated(charClass))
		{
			return !IsSubtraction(charClass);
		}
		return false;
	}

	public static bool IsEmpty(string charClass)
	{
		if (charClass[2] == '\0' && charClass[1] == '\0' && !IsNegated(charClass))
		{
			return !IsSubtraction(charClass);
		}
		return false;
	}

	public static bool IsSingleton(string set)
	{
		if (set[2] == '\0' && set[1] == '\u0002' && !IsNegated(set) && !IsSubtraction(set))
		{
			if (set[3] != '\uffff')
			{
				return set[3] + 1 == set[4];
			}
			return true;
		}
		return false;
	}

	public static bool IsSingletonInverse(string set)
	{
		if (set[2] == '\0' && set[1] == '\u0002' && IsNegated(set) && !IsSubtraction(set))
		{
			if (set[3] != '\uffff')
			{
				return set[3] + 1 == set[4];
			}
			return true;
		}
		return false;
	}

	public static bool TryGetOnlyCategories(string set, Span<UnicodeCategory> categories, out int numCategories, out bool negated)
	{
		negated = false;
		numCategories = 0;
		bool flag = false;
		if (set[2] == '\0' || set[1] != 0 || IsSubtraction(set))
		{
			return false;
		}
		int num = 3 + set[2];
		for (int i = 3; i < num; i++)
		{
			short num2 = (short)set[i];
			if (num2 > 0)
			{
				if ((flag & negated) || num2 == 100 || numCategories == categories.Length)
				{
					return false;
				}
				flag = true;
				categories[numCategories++] = (UnicodeCategory)(num2 - 1);
				continue;
			}
			if (num2 < 0)
			{
				if ((flag && !negated) || num2 == -100 || numCategories == categories.Length)
				{
					return false;
				}
				flag = true;
				negated = true;
				categories[numCategories++] = (UnicodeCategory)(-1 - num2);
				continue;
			}
			num2 = (short)set[++i];
			if (num2 > 0)
			{
				if (flag & negated)
				{
					return false;
				}
				flag = true;
				do
				{
					if (numCategories == categories.Length)
					{
						return false;
					}
					categories[numCategories++] = (UnicodeCategory)(num2 - 1);
					num2 = (short)set[++i];
				}
				while (num2 != 0);
				continue;
			}
			if (flag && !negated)
			{
				return false;
			}
			negated = true;
			flag = true;
			do
			{
				if (numCategories == categories.Length)
				{
					return false;
				}
				categories[numCategories++] = (UnicodeCategory)(-1 - num2);
				num2 = (short)set[++i];
			}
			while (num2 != 0);
		}
		negated ^= IsNegated(set);
		return true;
	}

	public static bool TryGetSingleRange(string set, out char lowInclusive, out char highInclusive)
	{
		if (set[2] == '\0' && set.Length == 3 + set[1])
		{
			switch (set[1])
			{
			case '\u0001':
				lowInclusive = set[3];
				highInclusive = '\uffff';
				return true;
			case '\u0002':
				lowInclusive = set[3];
				highInclusive = (char)(set[4] - 1);
				return true;
			}
		}
		lowInclusive = (highInclusive = '\0');
		return false;
	}

	public static bool TryGetDoubleRange(string set, out (char LowInclusive, char HighInclusive) range0, out (char LowInclusive, char HighInclusive) range1)
	{
		if (set[2] == '\0' && set.Length == 3 + set[1])
		{
			int num = set[1];
			if ((uint)(num - 3) <= 1u)
			{
				range0 = (LowInclusive: set[3], HighInclusive: (char)(set[4] - 1));
				range1 = (LowInclusive: set[5], HighInclusive: (num == 3) ? '\uffff' : ((char)(set[6] - 1)));
				return true;
			}
		}
		range0 = (range1 = (LowInclusive: '\0', HighInclusive: '\0'));
		return false;
	}

	public static int GetSetChars(string set, Span<char> chars)
	{
		if (!CanEasilyEnumerateSetContents(set))
		{
			return 0;
		}
		int num = set[1];
		int num2 = 0;
		for (int i = 3; i < 3 + num; i += 2)
		{
			int num3 = set[i + 1];
			for (int j = set[i]; j < num3; j++)
			{
				if (num2 >= chars.Length)
				{
					return 0;
				}
				chars[num2++] = (char)j;
			}
		}
		return num2;
	}

	public static bool TryGetAsciiSetChars(string set, [NotNullWhen(true)] out char[] asciiChars)
	{
		Span<char> span = stackalloc char[128];
		span = span.Slice(0, GetSetChars(set, span));
		if (span.IsEmpty || !IsAscii(span))
		{
			asciiChars = null;
			return false;
		}
		asciiChars = span.ToArray();
		return true;
	}

	public static bool MayOverlap(string set1, string set2)
	{
		if (set1 == set2)
		{
			return true;
		}
		if (set1 == "\0\u0001\0\0" || set2 == "\0\u0001\0\0")
		{
			return true;
		}
		bool flag = IsNegated(set1);
		bool flag2 = IsNegated(set2);
		if (flag != flag2)
		{
			return !set1.AsSpan(1).SequenceEqual(set2.AsSpan(1));
		}
		if (flag)
		{
			return true;
		}
		if (KnownDistinctSets(set1, set2) || KnownDistinctSets(set2, set1))
		{
			return false;
		}
		if (CanEasilyEnumerateSetContents(set2))
		{
			return MayOverlapByEnumeration(set1, set2);
		}
		if (CanEasilyEnumerateSetContents(set1))
		{
			return MayOverlapByEnumeration(set2, set1);
		}
		return true;
		static bool KnownDistinctSets(string set1, string set2)
		{
			if (set1 == "\0\0\u0001d" || set1 == "\0\u0004\0\t\u000e !")
			{
				switch (set2)
				{
				default:
					return set2 == "\0\n\00:A[_`a{İı";
				case "\0\0\u0001\t":
				case "\0\0\n\0\u0002\u0004\u0005\u0003\u0001\u0006\t\u0013\0":
				case "\0\u0002\00:":
					return true;
				}
			}
			return false;
		}
		static bool MayOverlapByEnumeration(string set1, string set2)
		{
			for (int i = 3; i < 3 + set2[1]; i += 2)
			{
				int num = set2[i + 1];
				for (int j = set2[i]; j < num; j++)
				{
					if (CharInClass((char)j, set1))
					{
						return true;
					}
				}
			}
			return false;
		}
	}

	public static bool ParticipatesInCaseConversion(int comparison)
	{
		switch (char.GetUnicodeCategory((char)comparison))
		{
		case UnicodeCategory.DecimalDigitNumber:
		case UnicodeCategory.OtherNumber:
		case UnicodeCategory.SpaceSeparator:
		case UnicodeCategory.LineSeparator:
		case UnicodeCategory.ParagraphSeparator:
		case UnicodeCategory.Control:
		case UnicodeCategory.ConnectorPunctuation:
		case UnicodeCategory.DashPunctuation:
		case UnicodeCategory.OpenPunctuation:
		case UnicodeCategory.ClosePunctuation:
		case UnicodeCategory.InitialQuotePunctuation:
		case UnicodeCategory.FinalQuotePunctuation:
		case UnicodeCategory.OtherPunctuation:
			return false;
		default:
			return true;
		}
	}

	public static bool ParticipatesInCaseConversion(ReadOnlySpan<char> s)
	{
		ReadOnlySpan<char> readOnlySpan = s;
		for (int i = 0; i < readOnlySpan.Length; i++)
		{
			char comparison = readOnlySpan[i];
			if (ParticipatesInCaseConversion(comparison))
			{
				return true;
			}
		}
		return false;
	}

	public static bool IsAscii(ReadOnlySpan<char> s)
	{
		return Ascii.IsValid(s);
	}

	private static bool CanEasilyEnumerateSetContents(string set)
	{
		if (CanEasilyEnumerateSetContents(set, out var hasSubtraction))
		{
			return !hasSubtraction;
		}
		return false;
	}

	private static bool CanEasilyEnumerateSetContents(string set, out bool hasSubtraction)
	{
		hasSubtraction = IsSubtraction(set);
		if (set.Length > 3 && set[1] > '\0' && set[1] % 2 == 0)
		{
			return set[2] == '\0';
		}
		return false;
	}

	internal static CharClassAnalysisResults Analyze(string set)
	{
		bool flag = IsNegated(set);
		if (!CanEasilyEnumerateSetContents(set, out var hasSubtraction) || (flag && hasSubtraction))
		{
			return default(CharClassAnalysisResults);
		}
		char c = set[3];
		char c2 = set[3 + set[1] - 1];
		CharClassAnalysisResults result;
		if (flag)
		{
			result = default(CharClassAnalysisResults);
			result.OnlyRanges = true;
			result.AllNonAsciiContained = c2 <= '\u0080';
			result.AllAsciiContained = c >= '\u0080';
			result.ContainsNoAscii = c == '\0' && set[4] >= '\u0080';
			result.ContainsOnlyAscii = false;
			result.LowerBoundInclusiveIfOnlyRanges = c;
			result.UpperBoundExclusiveIfOnlyRanges = c2;
			return result;
		}
		result = default(CharClassAnalysisResults);
		result.OnlyRanges = true;
		result.AllNonAsciiContained = false;
		result.AllAsciiContained = c == '\0' && set[4] >= '\u0080' && !hasSubtraction;
		result.ContainsOnlyAscii = c2 <= '\u0080';
		result.ContainsNoAscii = c >= '\u0080';
		result.LowerBoundInclusiveIfOnlyRanges = c;
		result.UpperBoundExclusiveIfOnlyRanges = c2;
		return result;
	}

	internal static bool IsSubtraction(string charClass)
	{
		return charClass.Length > 3 + charClass[2] + charClass[1];
	}

	internal static bool IsNegated(string set)
	{
		return set[0] == '\u0001';
	}

	internal static bool IsNegated(string set, int setOffset)
	{
		return set[setOffset] == '\u0001';
	}

	public static bool IsECMAWordChar(char ch)
	{
		if (((uint)(ch - 65) & -33) >= 26 && (uint)(ch - 48) >= 10u && ch != '_')
		{
			return ch == 'İ';
		}
		return true;
	}

	public static bool IsWordChar(char ch)
	{
		ReadOnlySpan<byte> wordCharAsciiLookup = WordCharAsciiLookup;
		int num = (int)ch >> 3;
		if ((uint)num >= (uint)wordCharAsciiLookup.Length)
		{
			return (0x4013F & (1 << (int)CharUnicodeInfo.GetUnicodeCategory(ch))) != 0;
		}
		return (wordCharAsciiLookup[num] & (1 << (ch & 7))) != 0;
	}

	public static bool IsBoundaryWordChar(char ch)
	{
		ReadOnlySpan<byte> wordCharAsciiLookup = WordCharAsciiLookup;
		int num = (int)ch >> 3;
		if ((uint)num >= (uint)wordCharAsciiLookup.Length)
		{
			if ((0x4013F & (1 << (int)CharUnicodeInfo.GetUnicodeCategory(ch))) == 0)
			{
				return ch == '\u200d' || ch == '\u200c';
			}
			return true;
		}
		return (wordCharAsciiLookup[num] & (1 << (ch & 7))) != 0;
	}

	public static bool DifferByOneBit(char a, char b, out int mask)
	{
		mask = a ^ b;
		return BitOperations.IsPow2(mask);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool CharInClass(char ch, string set, ref uint[] asciiLazyCache)
	{
		uint[] array = asciiLazyCache;
		if (array != null)
		{
			int num = (int)ch >> 4;
			if ((uint)num < (uint)array.Length)
			{
				uint num2 = array[num];
				uint num3 = (uint)(1 << ((ch & 0xF) << 1));
				if ((num2 & num3) != 0)
				{
					return (num2 & (num3 << 1)) != 0;
				}
			}
		}
		if (ch >= '\u0080')
		{
			return CharInClassRecursive(ch, set, 0);
		}
		return InitializeValue(ch, set, ref asciiLazyCache);
		static bool InitializeValue(char ch, string set, ref uint[] asciiLazyCache)
		{
			bool flag = CharInClass(ch, set);
			uint num4 = (uint)(1 << ((ch & 0xF) << 1));
			if (flag)
			{
				num4 |= num4 << 1;
			}
			uint[] array2 = asciiLazyCache ?? Interlocked.CompareExchange(ref asciiLazyCache, new uint[8], null) ?? asciiLazyCache;
			Interlocked.Or(ref array2[(int)ch >> 4], num4);
			return flag;
		}
	}

	public static bool CharInClass(char ch, string set)
	{
		return CharInClassRecursive(ch, set, 0);
	}

	private static bool CharInClassRecursive(char ch, string set, int start)
	{
		int num = set[start + 1];
		int num2 = set[start + 2];
		int num3 = start + 3 + num + num2;
		bool flag = CharInClassInternal(ch, set, start, num, num2);
		if (IsNegated(set, start))
		{
			flag = !flag;
		}
		if (flag && set.Length > num3)
		{
			flag = !CharInClassRecursive(ch, set, num3);
		}
		return flag;
	}

	private static bool CharInClassInternal(char ch, string set, int start, int setLength, int categoryLength)
	{
		int num = start + 3;
		int num2 = num + setLength;
		while (num != num2)
		{
			int num3 = num + num2 >> 1;
			if (ch < set[num3])
			{
				num2 = num3;
			}
			else
			{
				num = num3 + 1;
			}
		}
		if ((num & 1) == (start & 1))
		{
			return true;
		}
		if (categoryLength == 0)
		{
			return false;
		}
		return CharInCategory(ch, set.AsSpan(3 + start + setLength, categoryLength));
	}

	private static bool CharInCategory(char ch, ReadOnlySpan<char> categorySetSegment)
	{
		UnicodeCategory unicodeCategory = char.GetUnicodeCategory(ch);
		for (int i = 0; i < categorySetSegment.Length; i++)
		{
			int num = (short)categorySetSegment[i];
			if (num == 0)
			{
				if (CharInCategoryGroup(unicodeCategory, categorySetSegment, ref i))
				{
					return true;
				}
			}
			else if (num > 0)
			{
				if (num == 100)
				{
					if (char.IsWhiteSpace(ch))
					{
						return true;
					}
				}
				else if (unicodeCategory == (UnicodeCategory)(num - 1))
				{
					return true;
				}
			}
			else if (num == -100)
			{
				if (!char.IsWhiteSpace(ch))
				{
					return true;
				}
			}
			else if (unicodeCategory != (UnicodeCategory)(-1 - num))
			{
				return true;
			}
		}
		return false;
	}

	private static bool CharInCategoryGroup(UnicodeCategory chcategory, ReadOnlySpan<char> category, ref int i)
	{
		int num = i + 1;
		int num2 = (short)category[num];
		bool flag;
		if (num2 > 0)
		{
			flag = false;
			do
			{
				flag = flag || chcategory == (UnicodeCategory)(num2 - 1);
				num2 = (short)category[++num];
			}
			while (num2 != 0);
		}
		else
		{
			flag = true;
			do
			{
				flag = flag && chcategory != (UnicodeCategory)(-1 - num2);
				num2 = (short)category[++num];
			}
			while (num2 != 0);
		}
		i = num;
		return flag;
	}

	public static RegexCharClass Parse(string charClass)
	{
		return ParseRecursive(charClass, 0);
	}

	private static RegexCharClass ParseRecursive(string charClass, int start)
	{
		int num = charClass[start + 1];
		int num2 = charClass[start + 2];
		int num3 = start + 3 + num + num2;
		int num4 = start + 3;
		int start2 = num4 + num;
		List<(char, char)> ranges = ComputeRanges(charClass.AsSpan(start));
		RegexCharClass subtraction = null;
		if (charClass.Length > num3)
		{
			subtraction = ParseRecursive(charClass, num3);
		}
		StringBuilder categories = null;
		if (num2 > 0)
		{
			categories = new StringBuilder().Append(charClass.AsSpan(start2, num2));
		}
		return new RegexCharClass(IsNegated(charClass, start), ranges, categories, subtraction);
	}

	public static List<(char First, char Last)> ComputeRanges(ReadOnlySpan<char> set)
	{
		int num = set[1];
		int num2 = 3;
		int num3 = num2 + num;
		List<(char, char)> list = null;
		if (num > 0)
		{
			list = new List<(char, char)>(num);
			while (num2 < num3)
			{
				char item = set[num2];
				num2++;
				char item2 = ((num2 < num3) ? ((char)(set[num2] - 1)) : '\uffff');
				num2++;
				list.Add((item, item2));
			}
		}
		return list;
	}

	public static string OneToStringClass(char c)
	{
		Span<char> span = stackalloc char[1] { c };
		return CharsToStringClass(span);
	}

	internal unsafe static string CharsToStringClass(ReadOnlySpan<char> chars)
	{
		switch (chars.Length)
		{
		case 0:
			return "\0\0\0";
		case 1:
			if (chars[0] < '\u0080')
			{
				string[] array = s_asciiStrings;
				if (chars[0] < array.Length)
				{
					ref string reference = ref array[(uint)chars[0]];
					return reference ?? (reference = $"\0\u0002\0{chars[0]}{(ushort)(chars[0] + 1)}");
				}
			}
			break;
		case 2:
		{
			int num = chars[0] | 0x20;
			if ((uint)(num - 97) <= 25u && num == (chars[1] | 0x20))
			{
				ref string reference = ref s_asciiLetterPairStrings[num - 97];
				return reference ?? (reference = $"\0\u0004\0{(ushort)(num & -33)}{(ushort)((num & -33) + 1)}{(ushort)num}{(ushort)(num + 1)}");
			}
			break;
		}
		}
		int num2 = chars.Length * 2;
		if (chars[chars.Length - 1] == '\uffff')
		{
			num2--;
		}
		ReadOnlySpan<char> readOnlySpan = chars;
		return string.Create(3 + num2, (nint)(&readOnlySpan), delegate(Span<char> span, nint charsPtr)
		{
			span[0] = '\0';
			span[1] = (char)(span.Length - 3);
			span[2] = '\0';
			int num3 = 3;
			ReadOnlySpan<char> readOnlySpan2 = Unsafe.Read<ReadOnlySpan<char>>((void*)charsPtr);
			for (int i = 0; i < readOnlySpan2.Length; i++)
			{
				char c = readOnlySpan2[i];
				span[num3++] = c;
				if (c != '\uffff')
				{
					span[num3++] = (char)(c + 1);
				}
			}
		});
	}

	public string ToStringClass()
	{
		Span<char> initialBuffer = stackalloc char[256];
		System.Text.ValueStringBuilder vsb = new System.Text.ValueStringBuilder(initialBuffer);
		ToStringClass(ref vsb);
		return vsb.ToString();
	}

	private void ToStringClass(ref System.Text.ValueStringBuilder vsb)
	{
		Canonicalize();
		int length = vsb.Length;
		int num = _categories?.Length ?? 0;
		Span<char> span = vsb.AppendSpan(3);
		span[0] = (_negate ? '\u0001' : '\0');
		span[1] = '\0';
		span[2] = (char)num;
		List<(char, char)> rangelist = _rangelist;
		if (rangelist != null)
		{
			for (int i = 0; i < rangelist.Count; i++)
			{
				(char, char) tuple = rangelist[i];
				vsb.Append(tuple.Item1);
				if (tuple.Item2 != '\uffff')
				{
					vsb.Append((char)(tuple.Item2 + 1));
				}
			}
		}
		vsb[length + 1] = (char)(vsb.Length - length - 3);
		if (num != 0)
		{
			StringBuilder.ChunkEnumerator enumerator = _categories.GetChunks().GetEnumerator();
			while (enumerator.MoveNext())
			{
				vsb.Append(enumerator.Current.Span);
			}
		}
		_subtractor?.ToStringClass(ref vsb);
	}

	private void Canonicalize()
	{
		List<(char, char)> rangelist = _rangelist;
		if (rangelist == null)
		{
			return;
		}
		if (rangelist.Count > 1)
		{
			rangelist.Sort(((char First, char Last) x, (char First, char Last) y) => x.First.CompareTo(y.First));
			bool flag = false;
			int num = 0;
			int num2 = 1;
			while (true)
			{
				char item = rangelist[num].Item2;
				while (true)
				{
					if (num2 == rangelist.Count || item == '\uffff')
					{
						flag = true;
						break;
					}
					(char, char) tuple;
					(char, char) tuple2 = (tuple = rangelist[num2]);
					if (tuple2.Item1 > item + 1)
					{
						break;
					}
					if (item < tuple.Item2)
					{
						item = tuple.Item2;
					}
					num2++;
				}
				rangelist[num] = (rangelist[num].Item1, item);
				num++;
				if (flag)
				{
					break;
				}
				if (num < num2)
				{
					rangelist[num] = rangelist[num2];
				}
				num2++;
			}
			rangelist.RemoveRange(num, rangelist.Count - num);
		}
		if (!_negate && _subtractor == null && (_categories == null || _categories.Length == 0))
		{
			if (rangelist.Count == 2)
			{
				if (rangelist[0].Item1 == '\0' && rangelist[1].Item2 == '\uffff' && rangelist[0].Item2 < rangelist[1].Item1 - 1)
				{
					rangelist[0] = ((char)(rangelist[0].Item2 + 1), (char)(rangelist[1].Item1 - 1));
					rangelist.RemoveAt(1);
					_negate = true;
				}
			}
			else if (rangelist.Count == 1)
			{
				if (rangelist[0].Item1 == '\0')
				{
					if (rangelist[0].Item2 == '\ufffe')
					{
						rangelist[0] = ('\uffff', '\uffff');
						_negate = true;
					}
				}
				else if (rangelist[0].Item1 == '\u0001' && rangelist[0].Item2 == '\uffff')
				{
					rangelist[0] = ('\0', '\0');
					_negate = true;
				}
			}
		}
		if (!_negate && _subtractor == null)
		{
			StringBuilder categories = _categories;
			if (categories != null && categories.Length > 0 && rangelist.Count == 1 && rangelist[0].Item1 == '\0' && rangelist[0].Item2 == '\uffff')
			{
				_categories.Clear();
			}
		}
		if (_negate || _subtractor != null)
		{
			return;
		}
		StringBuilder categories2 = _categories;
		if (categories2 != null && categories2.Length > 0 && rangelist.Count == 2 && rangelist[0].Item1 == '\0' && rangelist[0].Item2 + 2 == rangelist[1].Item1 && rangelist[1].Item2 == '\uffff')
		{
			Span<char> initialBuffer = stackalloc char[256];
			System.Text.ValueStringBuilder valueStringBuilder = new System.Text.ValueStringBuilder(initialBuffer);
			StringBuilder.ChunkEnumerator enumerator = _categories.GetChunks().GetEnumerator();
			while (enumerator.MoveNext())
			{
				valueStringBuilder.Append(enumerator.Current.Span);
			}
			if (CharInCategory((char)(rangelist[0].Item2 + 1), valueStringBuilder.AsSpan()))
			{
				rangelist.RemoveAt(1);
				rangelist[0] = ('\0', '\uffff');
			}
			else
			{
				_negate = true;
				rangelist.RemoveAt(1);
				char c = (char)(rangelist[0].Item2 + 1);
				rangelist[0] = (c, c);
			}
			_categories.Clear();
			valueStringBuilder.Dispose();
		}
	}

	private static ReadOnlySpan<char> RangesFromProperty(string capname, bool invert, string pattern, int currentPos)
	{
		int num = 0;
		int num2 = s_propTable.Length;
		while (num != num2)
		{
			int num3 = (num + num2) / 2;
			int num4 = string.Compare(capname, s_propTable[num3][0], StringComparison.Ordinal);
			if (num4 < 0)
			{
				num2 = num3;
				continue;
			}
			if (num4 > 0)
			{
				num = num3 + 1;
				continue;
			}
			string text = s_propTable[num3][1];
			if (invert)
			{
				if (text[0] != 0)
				{
					return ("\0" + text).AsSpan();
				}
				return text.AsSpan(1);
			}
			return text.AsSpan();
		}
		throw new RegexParseException(RegexParseError.UnrecognizedUnicodeProperty, currentPos, System.SR.Format(System.SR.MakeException, pattern, currentPos, System.SR.Format(System.SR.UnrecognizedUnicodeProperty, capname)));
	}
}
