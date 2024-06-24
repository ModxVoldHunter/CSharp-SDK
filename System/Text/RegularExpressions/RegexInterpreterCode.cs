namespace System.Text.RegularExpressions;

internal sealed class RegexInterpreterCode
{
	public readonly RegexFindOptimizations FindOptimizations;

	public readonly RegexOptions Options;

	public readonly int[] Codes;

	public readonly string[] Strings;

	public readonly uint[][] StringsAsciiLookup;

	public readonly int TrackCount;

	public RegexInterpreterCode(RegexFindOptimizations findOptimizations, RegexOptions options, int[] codes, string[] strings, int trackcount)
	{
		FindOptimizations = findOptimizations;
		Options = options;
		Codes = codes;
		Strings = strings;
		StringsAsciiLookup = new uint[strings.Length][];
		TrackCount = trackcount;
	}

	public static bool OpcodeBacktracks(RegexOpcode opcode)
	{
		opcode &= RegexOpcode.OperatorMask;
		switch (opcode)
		{
		case RegexOpcode.Oneloop:
		case RegexOpcode.Notoneloop:
		case RegexOpcode.Setloop:
		case RegexOpcode.Onelazy:
		case RegexOpcode.Notonelazy:
		case RegexOpcode.Setlazy:
		case RegexOpcode.Lazybranch:
		case RegexOpcode.Branchmark:
		case RegexOpcode.Lazybranchmark:
		case RegexOpcode.Nullcount:
		case RegexOpcode.Setcount:
		case RegexOpcode.Branchcount:
		case RegexOpcode.Lazybranchcount:
		case RegexOpcode.Setmark:
		case RegexOpcode.Capturemark:
		case RegexOpcode.Getmark:
		case RegexOpcode.Setjump:
		case RegexOpcode.Backjump:
		case RegexOpcode.Forejump:
		case RegexOpcode.Goto:
			return true;
		default:
			return false;
		}
	}
}
