using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;

namespace System.Text;

internal static class EncodingTable
{
	private static readonly Dictionary<string, int> s_nameToCodePageCache = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

	private static readonly Dictionary<int, string> s_codePageToWebNameCache = new Dictionary<int, string>();

	private static readonly Dictionary<int, string> s_codePageToEnglishNameCache = new Dictionary<int, string>();

	private static readonly ReaderWriterLockSlim s_cacheLock = new ReaderWriterLockSlim();

	private static ReadOnlySpan<int> EncodingNameIndices => RuntimeHelpers.CreateSpan<int>((RuntimeFieldHandle)/*OpCode not supported: LdMemberToken*/);

	private static ReadOnlySpan<ushort> CodePagesByName => RuntimeHelpers.CreateSpan<ushort>((RuntimeFieldHandle)/*OpCode not supported: LdMemberToken*/);

	private static ReadOnlySpan<ushort> MappedCodePages => RuntimeHelpers.CreateSpan<ushort>((RuntimeFieldHandle)/*OpCode not supported: LdMemberToken*/);

	private static ReadOnlySpan<int> WebNameIndices => RuntimeHelpers.CreateSpan<int>((RuntimeFieldHandle)/*OpCode not supported: LdMemberToken*/);

	private static ReadOnlySpan<int> EnglishNameIndices => RuntimeHelpers.CreateSpan<int>((RuntimeFieldHandle)/*OpCode not supported: LdMemberToken*/);

	internal static int GetCodePageFromName(string name)
	{
		if (name == null)
		{
			return 0;
		}
		s_cacheLock.EnterUpgradeableReadLock();
		try
		{
			if (s_nameToCodePageCache.TryGetValue(name, out var value))
			{
				return value;
			}
			value = InternalGetCodePageFromName(name);
			if (value == 0)
			{
				return 0;
			}
			s_cacheLock.EnterWriteLock();
			try
			{
				if (s_nameToCodePageCache.TryGetValue(name, out var value2))
				{
					return value2;
				}
				s_nameToCodePageCache.Add(name, value);
				return value;
			}
			finally
			{
				s_cacheLock.ExitWriteLock();
			}
		}
		finally
		{
			s_cacheLock.ExitUpgradeableReadLock();
		}
	}

	private static int InternalGetCodePageFromName(string name)
	{
		ReadOnlySpan<int> encodingNameIndices = EncodingNameIndices;
		int i = 0;
		int num = encodingNameIndices.Length - 2;
		name = name.ToLowerInvariant();
		while (num - i > 3)
		{
			int num2 = (num - i) / 2 + i;
			int num3 = CompareOrdinal(name, "437arabicasmo-708big5big5-hkscsccsid00858ccsid00924ccsid01140ccsid01141ccsid01142ccsid01143ccsid01144ccsid01145ccsid01146ccsid01147ccsid01148ccsid01149chinesecn-big5cn-gbcp00858cp00924cp01140cp01141cp01142cp01143cp01144cp01145cp01146cp01147cp01148cp01149cp037cp1025cp1026cp1252cp1256cp273cp278cp280cp284cp285cp290cp297cp420cp423cp424cp437cp500cp50227cp850cp852cp855cp857cp858cp860cp861cp862cp863cp864cp865cp866cp869cp870cp871cp875cp880cp905csbig5cseuckrcseucpkdfmtjapanesecsgb2312csgb231280csibm037csibm1026csibm273csibm277csibm278csibm280csibm284csibm285csibm290csibm297csibm420csibm423csibm424csibm500csibm870csibm871csibm880csibm905csibmthaicsiso2022jpcsiso2022krcsiso58gb231280csisolatin2csisolatin3csisolatin4csisolatin5csisolatin9csisolatinarabiccsisolatincyrilliccsisolatingreekcsisolatinhebrewcskoi8rcsksc56011987cspc8codepage437csshiftjiscswindows31jcyrillicdin_66003dos-720dos-862dos-874ebcdic-cp-ar1ebcdic-cp-beebcdic-cp-caebcdic-cp-chebcdic-cp-dkebcdic-cp-esebcdic-cp-fiebcdic-cp-frebcdic-cp-gbebcdic-cp-grebcdic-cp-heebcdic-cp-isebcdic-cp-itebcdic-cp-nlebcdic-cp-noebcdic-cp-roeceebcdic-cp-seebcdic-cp-trebcdic-cp-usebcdic-cp-wtebcdic-cp-yuebcdic-cyrillicebcdic-de-273+euroebcdic-dk-277+euroebcdic-es-284+euroebcdic-fi-278+euroebcdic-fr-297+euroebcdic-gb-285+euroebcdic-international-500+euroebcdic-is-871+euroebcdic-it-280+euroebcdic-jp-kanaebcdic-latin9--euroebcdic-no-277+euroebcdic-se-278+euroebcdic-us-37+euroecma-114ecma-118elot_928euc-cneuc-jpeuc-krextended_unix_code_packed_format_for_japanesegb18030gb2312gb2312-80gb231280gb_2312-80gbkgermangreekgreek8hebrewhz-gb-2312ibm-thaiibm00858ibm00924ibm01047ibm01140ibm01141ibm01142ibm01143ibm01144ibm01145ibm01146ibm01147ibm01148ibm01149ibm037ibm1026ibm273ibm277ibm278ibm280ibm284ibm285ibm290ibm297ibm420ibm423ibm424ibm437ibm500ibm737ibm775ibm850ibm852ibm855ibm857ibm860ibm861ibm862ibm863ibm864ibm865ibm866ibm869ibm870ibm871ibm880ibm905irviso-2022-jpiso-2022-jpeuciso-2022-kriso-2022-kr-7iso-2022-kr-7bitiso-2022-kr-8iso-2022-kr-8bitiso-8859-11iso-8859-13iso-8859-15iso-8859-2iso-8859-3iso-8859-4iso-8859-5iso-8859-6iso-8859-7iso-8859-8iso-8859-8 visualiso-8859-8-iiso-8859-9iso-ir-101iso-ir-109iso-ir-110iso-ir-126iso-ir-127iso-ir-138iso-ir-144iso-ir-148iso-ir-149iso-ir-58iso8859-2iso_8859-15iso_8859-2iso_8859-2:1987iso_8859-3iso_8859-3:1988iso_8859-4iso_8859-4:1988iso_8859-5iso_8859-5:1988iso_8859-6iso_8859-6:1987iso_8859-7iso_8859-7:1987iso_8859-8iso_8859-8:1988iso_8859-9iso_8859-9:1989johabkoikoi8koi8-rkoi8-rukoi8-ukoi8rkoreanks-c-5601ks-c5601ks_c_5601ks_c_5601-1987ks_c_5601-1989ks_c_5601_1987ksc5601ksc_5601l2l3l4l5l9latin2latin3latin4latin5latin9logicalmacintoshms_kanjinorwegianns_4551-1pc-multilingual-850+eurosen_850200_bshift-jisshift_jissjisswedishtis-620visualwindows-1250windows-1251windows-1252windows-1253windows-1254windows-1255windows-1256windows-1257windows-1258windows-874x-ansix-chinese-cnsx-chinese-etenx-cp1250x-cp1251x-cp20001x-cp20003x-cp20004x-cp20005x-cp20261x-cp20269x-cp20936x-cp20949x-cp50227x-ebcdic-koreanextendedx-eucx-euc-cnx-euc-jpx-europax-ia5x-ia5-germanx-ia5-norwegianx-ia5-swedishx-iscii-asx-iscii-bex-iscii-dex-iscii-gux-iscii-kax-iscii-max-iscii-orx-iscii-pax-iscii-tax-iscii-tex-mac-arabicx-mac-cex-mac-chinesesimpx-mac-chinesetradx-mac-croatianx-mac-cyrillicx-mac-greekx-mac-hebrewx-mac-icelandicx-mac-japanesex-mac-koreanx-mac-romanianx-mac-thaix-mac-turkishx-mac-ukrainianx-ms-cp932x-sjisx-x-big5", encodingNameIndices[num2], encodingNameIndices[num2 + 1] - encodingNameIndices[num2]);
			if (num3 == 0)
			{
				return CodePagesByName[num2];
			}
			if (num3 < 0)
			{
				num = num2;
			}
			else
			{
				i = num2;
			}
		}
		for (; i <= num; i++)
		{
			if (CompareOrdinal(name, "437arabicasmo-708big5big5-hkscsccsid00858ccsid00924ccsid01140ccsid01141ccsid01142ccsid01143ccsid01144ccsid01145ccsid01146ccsid01147ccsid01148ccsid01149chinesecn-big5cn-gbcp00858cp00924cp01140cp01141cp01142cp01143cp01144cp01145cp01146cp01147cp01148cp01149cp037cp1025cp1026cp1252cp1256cp273cp278cp280cp284cp285cp290cp297cp420cp423cp424cp437cp500cp50227cp850cp852cp855cp857cp858cp860cp861cp862cp863cp864cp865cp866cp869cp870cp871cp875cp880cp905csbig5cseuckrcseucpkdfmtjapanesecsgb2312csgb231280csibm037csibm1026csibm273csibm277csibm278csibm280csibm284csibm285csibm290csibm297csibm420csibm423csibm424csibm500csibm870csibm871csibm880csibm905csibmthaicsiso2022jpcsiso2022krcsiso58gb231280csisolatin2csisolatin3csisolatin4csisolatin5csisolatin9csisolatinarabiccsisolatincyrilliccsisolatingreekcsisolatinhebrewcskoi8rcsksc56011987cspc8codepage437csshiftjiscswindows31jcyrillicdin_66003dos-720dos-862dos-874ebcdic-cp-ar1ebcdic-cp-beebcdic-cp-caebcdic-cp-chebcdic-cp-dkebcdic-cp-esebcdic-cp-fiebcdic-cp-frebcdic-cp-gbebcdic-cp-grebcdic-cp-heebcdic-cp-isebcdic-cp-itebcdic-cp-nlebcdic-cp-noebcdic-cp-roeceebcdic-cp-seebcdic-cp-trebcdic-cp-usebcdic-cp-wtebcdic-cp-yuebcdic-cyrillicebcdic-de-273+euroebcdic-dk-277+euroebcdic-es-284+euroebcdic-fi-278+euroebcdic-fr-297+euroebcdic-gb-285+euroebcdic-international-500+euroebcdic-is-871+euroebcdic-it-280+euroebcdic-jp-kanaebcdic-latin9--euroebcdic-no-277+euroebcdic-se-278+euroebcdic-us-37+euroecma-114ecma-118elot_928euc-cneuc-jpeuc-krextended_unix_code_packed_format_for_japanesegb18030gb2312gb2312-80gb231280gb_2312-80gbkgermangreekgreek8hebrewhz-gb-2312ibm-thaiibm00858ibm00924ibm01047ibm01140ibm01141ibm01142ibm01143ibm01144ibm01145ibm01146ibm01147ibm01148ibm01149ibm037ibm1026ibm273ibm277ibm278ibm280ibm284ibm285ibm290ibm297ibm420ibm423ibm424ibm437ibm500ibm737ibm775ibm850ibm852ibm855ibm857ibm860ibm861ibm862ibm863ibm864ibm865ibm866ibm869ibm870ibm871ibm880ibm905irviso-2022-jpiso-2022-jpeuciso-2022-kriso-2022-kr-7iso-2022-kr-7bitiso-2022-kr-8iso-2022-kr-8bitiso-8859-11iso-8859-13iso-8859-15iso-8859-2iso-8859-3iso-8859-4iso-8859-5iso-8859-6iso-8859-7iso-8859-8iso-8859-8 visualiso-8859-8-iiso-8859-9iso-ir-101iso-ir-109iso-ir-110iso-ir-126iso-ir-127iso-ir-138iso-ir-144iso-ir-148iso-ir-149iso-ir-58iso8859-2iso_8859-15iso_8859-2iso_8859-2:1987iso_8859-3iso_8859-3:1988iso_8859-4iso_8859-4:1988iso_8859-5iso_8859-5:1988iso_8859-6iso_8859-6:1987iso_8859-7iso_8859-7:1987iso_8859-8iso_8859-8:1988iso_8859-9iso_8859-9:1989johabkoikoi8koi8-rkoi8-rukoi8-ukoi8rkoreanks-c-5601ks-c5601ks_c_5601ks_c_5601-1987ks_c_5601-1989ks_c_5601_1987ksc5601ksc_5601l2l3l4l5l9latin2latin3latin4latin5latin9logicalmacintoshms_kanjinorwegianns_4551-1pc-multilingual-850+eurosen_850200_bshift-jisshift_jissjisswedishtis-620visualwindows-1250windows-1251windows-1252windows-1253windows-1254windows-1255windows-1256windows-1257windows-1258windows-874x-ansix-chinese-cnsx-chinese-etenx-cp1250x-cp1251x-cp20001x-cp20003x-cp20004x-cp20005x-cp20261x-cp20269x-cp20936x-cp20949x-cp50227x-ebcdic-koreanextendedx-eucx-euc-cnx-euc-jpx-europax-ia5x-ia5-germanx-ia5-norwegianx-ia5-swedishx-iscii-asx-iscii-bex-iscii-dex-iscii-gux-iscii-kax-iscii-max-iscii-orx-iscii-pax-iscii-tax-iscii-tex-mac-arabicx-mac-cex-mac-chinesesimpx-mac-chinesetradx-mac-croatianx-mac-cyrillicx-mac-greekx-mac-hebrewx-mac-icelandicx-mac-japanesex-mac-koreanx-mac-romanianx-mac-thaix-mac-turkishx-mac-ukrainianx-ms-cp932x-sjisx-x-big5", encodingNameIndices[i], encodingNameIndices[i + 1] - encodingNameIndices[i]) == 0)
			{
				return CodePagesByName[i];
			}
		}
		return 0;
	}

	private static int CompareOrdinal(string s1, string s2, int index, int length)
	{
		int num = s1.Length;
		if (num > length)
		{
			num = length;
		}
		int i;
		for (i = 0; i < num && s1[i] == s2[index + i]; i++)
		{
		}
		if (i < num)
		{
			return s1[i] - s2[index + i];
		}
		return s1.Length - length;
	}

	internal static string GetWebNameFromCodePage(int codePage)
	{
		return GetNameFromCodePage(codePage, "ibm037ibm437ibm500asmo-708dos-720ibm737ibm775ibm850ibm852ibm855ibm857ibm00858ibm860ibm861dos-862ibm863ibm864ibm865cp866ibm869ibm870windows-874cp875shift_jisgb2312ks_c_5601-1987big5ibm1026ibm01047ibm01140ibm01141ibm01142ibm01143ibm01144ibm01145ibm01146ibm01147ibm01148ibm01149windows-1250windows-1251windows-1252windows-1253windows-1254windows-1255windows-1256windows-1257windows-1258johabmacintoshx-mac-japanesex-mac-chinesetradx-mac-koreanx-mac-arabicx-mac-hebrewx-mac-greekx-mac-cyrillicx-mac-chinesesimpx-mac-romanianx-mac-ukrainianx-mac-thaix-mac-cex-mac-icelandicx-mac-turkishx-mac-croatianx-chinese-cnsx-cp20001x-chinese-etenx-cp20003x-cp20004x-cp20005x-ia5x-ia5-germanx-ia5-swedishx-ia5-norwegianx-cp20261x-cp20269ibm273ibm277ibm278ibm280ibm284ibm285ibm290ibm297ibm420ibm423ibm424x-ebcdic-koreanextendedibm-thaikoi8-ribm871ibm880ibm905ibm00924euc-jpx-cp20936x-cp20949cp1025koi8-uiso-8859-2iso-8859-3iso-8859-4iso-8859-5iso-8859-6iso-8859-7iso-8859-8iso-8859-9iso-8859-13iso-8859-15x-europaiso-8859-8-iiso-2022-jpcsiso2022jpiso-2022-jpiso-2022-krx-cp50227euc-jpeuc-cneuc-krhz-gb-2312gb18030x-iscii-dex-iscii-bex-iscii-tax-iscii-tex-iscii-asx-iscii-orx-iscii-kax-iscii-max-iscii-gux-iscii-pa", WebNameIndices, s_codePageToWebNameCache);
	}

	internal static string GetEnglishNameFromCodePage(int codePage)
	{
		return GetNameFromCodePage(codePage, "IBM EBCDIC (US-Canada)OEM United StatesIBM EBCDIC (International)Arabic (ASMO 708)Arabic (DOS)Greek (DOS)Baltic (DOS)Western European (DOS)Central European (DOS)OEM CyrillicTurkish (DOS)OEM Multilingual Latin IPortuguese (DOS)Icelandic (DOS)Hebrew (DOS)French Canadian (DOS)Arabic (864)Nordic (DOS)Cyrillic (DOS)Greek, Modern (DOS)IBM EBCDIC (Multilingual Latin-2)Thai (Windows)IBM EBCDIC (Greek Modern)Japanese (Shift-JIS)Chinese Simplified (GB2312)KoreanChinese Traditional (Big5)IBM EBCDIC (Turkish Latin-5)IBM Latin-1IBM EBCDIC (US-Canada-Euro)IBM EBCDIC (Germany-Euro)IBM EBCDIC (Denmark-Norway-Euro)IBM EBCDIC (Finland-Sweden-Euro)IBM EBCDIC (Italy-Euro)IBM EBCDIC (Spain-Euro)IBM EBCDIC (UK-Euro)IBM EBCDIC (France-Euro)IBM EBCDIC (International-Euro)IBM EBCDIC (Icelandic-Euro)Central European (Windows)Cyrillic (Windows)Western European (Windows)Greek (Windows)Turkish (Windows)Hebrew (Windows)Arabic (Windows)Baltic (Windows)Vietnamese (Windows)Korean (Johab)Western European (Mac)Japanese (Mac)Chinese Traditional (Mac)Korean (Mac)Arabic (Mac)Hebrew (Mac)Greek (Mac)Cyrillic (Mac)Chinese Simplified (Mac)Romanian (Mac)Ukrainian (Mac)Thai (Mac)Central European (Mac)Icelandic (Mac)Turkish (Mac)Croatian (Mac)Chinese Traditional (CNS)TCA TaiwanChinese Traditional (Eten)IBM5550 TaiwanTeleText TaiwanWang TaiwanWestern European (IA5)German (IA5)Swedish (IA5)Norwegian (IA5)T.61ISO-6937IBM EBCDIC (Germany)IBM EBCDIC (Denmark-Norway)IBM EBCDIC (Finland-Sweden)IBM EBCDIC (Italy)IBM EBCDIC (Spain)IBM EBCDIC (UK)IBM EBCDIC (Japanese katakana)IBM EBCDIC (France)IBM EBCDIC (Arabic)IBM EBCDIC (Greek)IBM EBCDIC (Hebrew)IBM EBCDIC (Korean Extended)IBM EBCDIC (Thai)Cyrillic (KOI8-R)IBM EBCDIC (Icelandic)IBM EBCDIC (Cyrillic Russian)IBM EBCDIC (Turkish)IBM Latin-1Japanese (JIS 0208-1990 and 0212-1990)Chinese Simplified (GB2312-80)Korean WansungIBM EBCDIC (Cyrillic Serbian-Bulgarian)Cyrillic (KOI8-U)Central European (ISO)Latin 3 (ISO)Baltic (ISO)Cyrillic (ISO)Arabic (ISO)Greek (ISO)Hebrew (ISO-Visual)Turkish (ISO)Estonian (ISO)Latin 9 (ISO)EuropaHebrew (ISO-Logical)Japanese (JIS)Japanese (JIS-Allow 1 byte Kana)Japanese (JIS-Allow 1 byte Kana - SO/SI)Korean (ISO)Chinese Simplified (ISO-2022)Japanese (EUC)Chinese Simplified (EUC)Korean (EUC)Chinese Simplified (HZ)Chinese Simplified (GB18030)ISCII DevanagariISCII BengaliISCII TamilISCII TeluguISCII AssameseISCII OriyaISCII KannadaISCII MalayalamISCII GujaratiISCII Punjabi", EnglishNameIndices, s_codePageToEnglishNameCache);
	}

	private static string GetNameFromCodePage(int codePage, string names, ReadOnlySpan<int> indices, Dictionary<int, string> cache)
	{
		if ((uint)codePage > 65535u)
		{
			return null;
		}
		int num = MappedCodePages.IndexOf((ushort)codePage);
		if (num < 0)
		{
			return null;
		}
		s_cacheLock.EnterUpgradeableReadLock();
		try
		{
			if (cache.TryGetValue(codePage, out var value))
			{
				return value;
			}
			value = names.Substring(indices[num], indices[num + 1] - indices[num]);
			s_cacheLock.EnterWriteLock();
			try
			{
				if (cache.TryGetValue(codePage, out var value2))
				{
					return value2;
				}
				cache.Add(codePage, value);
				return value;
			}
			finally
			{
				s_cacheLock.ExitWriteLock();
			}
		}
		finally
		{
			s_cacheLock.ExitUpgradeableReadLock();
		}
	}
}
