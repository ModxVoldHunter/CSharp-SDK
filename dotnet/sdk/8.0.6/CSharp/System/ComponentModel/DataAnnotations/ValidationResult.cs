using System.Collections.Generic;

namespace System.ComponentModel.DataAnnotations;

public class ValidationResult
{
	public static readonly ValidationResult? Success;

	public IEnumerable<string> MemberNames { get; }

	public string? ErrorMessage { get; set; }

	public ValidationResult(string? errorMessage)
		: this(errorMessage, null)
	{
	}

	public ValidationResult(string? errorMessage, IEnumerable<string>? memberNames)
	{
		ErrorMessage = errorMessage;
		MemberNames = memberNames ?? Array.Empty<string>();
	}

	protected ValidationResult(ValidationResult validationResult)
	{
		ArgumentNullException.ThrowIfNull(validationResult, "validationResult");
		ErrorMessage = validationResult.ErrorMessage;
		MemberNames = validationResult.MemberNames;
	}

	public override string ToString()
	{
		return ErrorMessage ?? base.ToString();
	}
}
