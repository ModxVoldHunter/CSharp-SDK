using System.Globalization;

namespace System.Text.RegularExpressions;

internal sealed class RegexInterpreterFactory : RegexRunnerFactory
{
	private readonly RegexInterpreterCode _code;

	private readonly CultureInfo _culture;

	public RegexInterpreterFactory(RegexTree tree)
	{
		_culture = tree.Culture;
		_code = RegexWriter.Write(tree);
	}

	protected internal override RegexRunner CreateInstance()
	{
		return new RegexInterpreter(_code, _culture);
	}
}
