using System.Diagnostics.CodeAnalysis;

namespace System.Text.RegularExpressions;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
public sealed class GeneratedRegexAttribute : Attribute
{
	public string Pattern { get; }

	public RegexOptions Options { get; }

	public int MatchTimeoutMilliseconds { get; }

	public string CultureName { get; }

	public GeneratedRegexAttribute([StringSyntax("Regex")] string pattern)
		: this(pattern, RegexOptions.None)
	{
	}

	public GeneratedRegexAttribute([StringSyntax("Regex", new object[] { "options" })] string pattern, RegexOptions options)
		: this(pattern, options, -1)
	{
	}

	public GeneratedRegexAttribute([StringSyntax("Regex", new object[] { "options" })] string pattern, RegexOptions options, string cultureName)
		: this(pattern, options, -1, cultureName)
	{
	}

	public GeneratedRegexAttribute([StringSyntax("Regex", new object[] { "options" })] string pattern, RegexOptions options, int matchTimeoutMilliseconds)
		: this(pattern, options, matchTimeoutMilliseconds, string.Empty)
	{
	}

	public GeneratedRegexAttribute([StringSyntax("Regex", new object[] { "options" })] string pattern, RegexOptions options, int matchTimeoutMilliseconds, string cultureName)
	{
		Pattern = pattern;
		Options = options;
		MatchTimeoutMilliseconds = matchTimeoutMilliseconds;
		CultureName = cultureName;
	}
}
