using System.Globalization;
using System.Runtime.CompilerServices;

namespace System.Text.RegularExpressions;

internal static class RegexCaseEquivalences
{
	private static ReadOnlySpan<char> EquivalenceCasingValues => RuntimeHelpers.CreateSpan<char>((RuntimeFieldHandle)/*OpCode not supported: LdMemberToken*/);

	private static ReadOnlySpan<ushort> EquivalenceFirstLevelLookup => RuntimeHelpers.CreateSpan<ushort>((RuntimeFieldHandle)/*OpCode not supported: LdMemberToken*/);

	private static ReadOnlySpan<ushort> EquivalenceCasingMap => RuntimeHelpers.CreateSpan<ushort>((RuntimeFieldHandle)/*OpCode not supported: LdMemberToken*/);

	public static bool TryFindCaseEquivalencesForCharWithIBehavior(char c, CultureInfo culture, scoped ref RegexCaseBehavior mappingBehavior, out ReadOnlySpan<char> equivalences)
	{
		int num;
		ReadOnlySpan<char> readOnlySpan;
		if ((c | 0x20) == 105 || (c | 1) == 305)
		{
			if (mappingBehavior == RegexCaseBehavior.NotSet)
			{
				mappingBehavior = GetRegexBehavior(culture);
			}
			char c2 = c;
			if ((uint)c2 <= 105u)
			{
				if (c2 != 'I')
				{
					if (c2 != 'i')
					{
						goto IL_00a9;
					}
					num = 0;
				}
				else
				{
					num = 1;
				}
				if (mappingBehavior != RegexCaseBehavior.Invariant)
				{
					if (num == 0)
					{
						goto IL_0069;
					}
					if (num == 1)
					{
						num = 2;
						goto IL_006b;
					}
				}
				readOnlySpan = "Ii".AsSpan();
				goto IL_00b3;
			}
			if (c2 == 'İ')
			{
				goto IL_0069;
			}
			if (c2 == 'ı')
			{
				goto IL_0085;
			}
			goto IL_00a9;
		}
		return TryFindCaseEquivalencesForChar(c, out equivalences);
		IL_006b:
		if (mappingBehavior != RegexCaseBehavior.NonTurkish)
		{
			if (num == 2)
			{
				goto IL_0085;
			}
			if (num == 3)
			{
				if (mappingBehavior != RegexCaseBehavior.Turkish)
				{
					goto IL_00a9;
				}
				readOnlySpan = "iİ".AsSpan();
				goto IL_00b3;
			}
		}
		readOnlySpan = "Iiİ".AsSpan();
		goto IL_00b3;
		IL_00b3:
		equivalences = readOnlySpan;
		return equivalences != default(ReadOnlySpan<char>);
		IL_00a9:
		readOnlySpan = default(ReadOnlySpan<char>);
		goto IL_00b3;
		IL_0069:
		num = 3;
		goto IL_006b;
		IL_0085:
		if (mappingBehavior != RegexCaseBehavior.Turkish)
		{
			goto IL_00a9;
		}
		readOnlySpan = "Iı".AsSpan();
		goto IL_00b3;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static RegexCaseBehavior GetRegexBehavior(CultureInfo culture)
	{
		if (culture.Name.Length != 0)
		{
			if (!IsTurkishOrAzeri(culture.Name))
			{
				return RegexCaseBehavior.NonTurkish;
			}
			return RegexCaseBehavior.Turkish;
		}
		return RegexCaseBehavior.Invariant;
		static bool IsTurkishOrAzeri(string cultureName)
		{
			if (cultureName.Length >= 2)
			{
				switch (cultureName[0])
				{
				case 't':
					if (cultureName[1] == 'r')
					{
						if (cultureName.Length != 2)
						{
							return cultureName[2] == '-';
						}
						return true;
					}
					return false;
				case 'a':
					if (cultureName[1] == 'z')
					{
						if (cultureName.Length != 2)
						{
							return cultureName[2] == '-';
						}
						return true;
					}
					return false;
				}
			}
			return false;
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static bool TryFindCaseEquivalencesForChar(char c, out ReadOnlySpan<char> equivalences)
	{
		byte index = (byte)((uint)c / 1024u);
		ushort num = EquivalenceFirstLevelLookup[index];
		if (num == ushort.MaxValue)
		{
			equivalences = default(ReadOnlySpan<char>);
			return false;
		}
		ushort index2 = (ushort)((uint)c % 1024u + num);
		ushort num2 = EquivalenceCasingMap[index2];
		if (num2 == ushort.MaxValue)
		{
			equivalences = default(ReadOnlySpan<char>);
			return false;
		}
		byte length = (byte)((uint)(num2 >> 13) & 7u);
		ushort start = (ushort)(num2 & 0x1FFFu);
		equivalences = EquivalenceCasingValues.Slice(start, length);
		return true;
	}
}
