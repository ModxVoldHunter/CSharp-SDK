using System.Buffers;
using System.Globalization;
using System.Reflection.Emit;

namespace System.Text.RegularExpressions;

internal sealed class CompiledRegexRunnerFactory : RegexRunnerFactory
{
	private readonly DynamicMethod _scanMethod;

	private readonly SearchValues<char>[] _searchValues;

	private readonly CultureInfo _culture;

	private CompiledRegexRunner.ScanDelegate _scan;

	public CompiledRegexRunnerFactory(DynamicMethod scanMethod, SearchValues<char>[] searchValues, CultureInfo culture)
	{
		_scanMethod = scanMethod;
		_searchValues = searchValues;
		_culture = culture;
	}

	protected internal override RegexRunner CreateInstance()
	{
		return new CompiledRegexRunner(_scan ?? (_scan = _scanMethod.CreateDelegate<CompiledRegexRunner.ScanDelegate>()), _searchValues, _culture);
	}
}
