using System.Buffers;
using System.Globalization;

namespace System.Text.RegularExpressions;

internal sealed class CompiledRegexRunner : RegexRunner
{
	internal delegate void ScanDelegate(RegexRunner runner, ReadOnlySpan<char> text);

	private readonly ScanDelegate _scanMethod;

	private readonly SearchValues<char>[] _searchValues;

	private readonly CultureInfo _culture;

	private RegexCaseBehavior _caseBehavior;

	public CompiledRegexRunner(ScanDelegate scan, SearchValues<char>[] searchValues, CultureInfo culture)
	{
		_scanMethod = scan;
		_searchValues = searchValues;
		_culture = culture;
	}

	protected internal override void Scan(ReadOnlySpan<char> text)
	{
		_scanMethod(this, text);
	}
}
