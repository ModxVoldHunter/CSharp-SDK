using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text.RegularExpressions;

namespace System.ComponentModel.DataAnnotations;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter, AllowMultiple = false)]
public class RegularExpressionAttribute : ValidationAttribute
{
	public int MatchTimeoutInMilliseconds { get; set; }

	public TimeSpan MatchTimeout => TimeSpan.FromMilliseconds(MatchTimeoutInMilliseconds);

	public string Pattern { get; }

	private Regex? Regex { get; set; }

	public RegularExpressionAttribute([StringSyntax("Regex")] string pattern)
		: base(() => System.SR.RegexAttribute_ValidationError)
	{
		Pattern = pattern;
		MatchTimeoutInMilliseconds = 2000;
	}

	public override bool IsValid(object? value)
	{
		SetupRegex();
		string text = Convert.ToString(value, CultureInfo.CurrentCulture);
		if (string.IsNullOrEmpty(text))
		{
			return true;
		}
		Regex.ValueMatchEnumerator enumerator = Regex.EnumerateMatches(text).GetEnumerator();
		if (enumerator.MoveNext())
		{
			ValueMatch current = enumerator.Current;
			if (current.Index == 0)
			{
				return current.Length == text.Length;
			}
			return false;
		}
		return false;
	}

	public override string FormatErrorMessage(string name)
	{
		SetupRegex();
		return string.Format(CultureInfo.CurrentCulture, base.ErrorMessageString, name, Pattern);
	}

	private void SetupRegex()
	{
		if (Regex == null)
		{
			if (string.IsNullOrEmpty(Pattern))
			{
				throw new InvalidOperationException(System.SR.RegularExpressionAttribute_Empty_Pattern);
			}
			Regex = ((MatchTimeoutInMilliseconds == -1) ? new Regex(Pattern) : new Regex(Pattern, RegexOptions.None, TimeSpan.FromMilliseconds(MatchTimeoutInMilliseconds)));
		}
	}
}
