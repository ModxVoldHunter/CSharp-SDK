using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace System.ComponentModel.DataAnnotations;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter, AllowMultiple = false)]
public class LengthAttribute : ValidationAttribute
{
	public int MinimumLength { get; }

	public int MaximumLength { get; }

	[RequiresUnreferencedCode("Uses reflection to get the 'Count' property on types that don't implement ICollection. This 'Count' property may be trimmed. Ensure it is preserved.")]
	public LengthAttribute(int minimumLength, int maximumLength)
		: base(System.SR.LengthAttribute_ValidationError)
	{
		MinimumLength = minimumLength;
		MaximumLength = maximumLength;
	}

	[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode", Justification = "The ctor is marked with RequiresUnreferencedCode.")]
	public override bool IsValid(object? value)
	{
		EnsureLegalLengths();
		if (value == null)
		{
			return true;
		}
		int count;
		if (value is string text)
		{
			count = text.Length;
		}
		else if (!CountPropertyHelper.TryGetCount(value, out count))
		{
			throw new InvalidCastException(System.SR.Format(System.SR.LengthAttribute_InvalidValueType, value.GetType()));
		}
		return (uint)(count - MinimumLength) <= (uint)(MaximumLength - MinimumLength);
	}

	public override string FormatErrorMessage(string name)
	{
		return string.Format(CultureInfo.CurrentCulture, base.ErrorMessageString, name, MinimumLength, MaximumLength);
	}

	private void EnsureLegalLengths()
	{
		if (MinimumLength < 0)
		{
			throw new InvalidOperationException(System.SR.LengthAttribute_InvalidMinLength);
		}
		if (MaximumLength < MinimumLength)
		{
			throw new InvalidOperationException(System.SR.LengthAttribute_InvalidMaxLength);
		}
	}
}
