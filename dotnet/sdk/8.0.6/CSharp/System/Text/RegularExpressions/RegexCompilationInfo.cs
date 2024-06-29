using System.Diagnostics.CodeAnalysis;

namespace System.Text.RegularExpressions;

[Obsolete("Regex.CompileToAssembly is obsolete and not supported. Use the GeneratedRegexAttribute with the regular expression source generator instead.", DiagnosticId = "SYSLIB0036", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
public class RegexCompilationInfo
{
	private string _pattern;

	private string _name;

	private string _nspace;

	private TimeSpan _matchTimeout;

	public bool IsPublic { get; set; }

	public TimeSpan MatchTimeout
	{
		get
		{
			return _matchTimeout;
		}
		set
		{
			Regex.ValidateMatchTimeout(value);
			_matchTimeout = value;
		}
	}

	public string Name
	{
		get
		{
			return _name;
		}
		[MemberNotNull("_name")]
		set
		{
			ArgumentException.ThrowIfNullOrEmpty(value, "Name");
			_name = value;
		}
	}

	public string Namespace
	{
		get
		{
			return _nspace;
		}
		[MemberNotNull("_nspace")]
		set
		{
			ArgumentNullException.ThrowIfNull(value, "Namespace");
			_nspace = value;
		}
	}

	public RegexOptions Options { get; set; }

	public string Pattern
	{
		get
		{
			return _pattern;
		}
		[MemberNotNull("_pattern")]
		set
		{
			ArgumentNullException.ThrowIfNull(value, "Pattern");
			_pattern = value;
		}
	}

	public RegexCompilationInfo(string pattern, RegexOptions options, string name, string fullnamespace, bool ispublic)
		: this(pattern, options, name, fullnamespace, ispublic, Regex.s_defaultMatchTimeout)
	{
	}

	public RegexCompilationInfo(string pattern, RegexOptions options, string name, string fullnamespace, bool ispublic, TimeSpan matchTimeout)
	{
		Pattern = pattern;
		Name = name;
		Namespace = fullnamespace;
		Options = options;
		IsPublic = ispublic;
		MatchTimeout = matchTimeout;
	}
}
