namespace System.ComponentModel.DataAnnotations;

[CLSCompliant(false)]
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter, AllowMultiple = false)]
public class DeniedValuesAttribute : ValidationAttribute
{
	public object?[] Values { get; }

	public DeniedValuesAttribute(params object?[] values)
	{
		ArgumentNullException.ThrowIfNull(values, "values");
		Values = values;
		base.DefaultErrorMessage = System.SR.DeniedValuesAttribute_Invalid;
	}

	public override bool IsValid(object? value)
	{
		object[] values = Values;
		for (int i = 0; i < values.Length; i++)
		{
			if (values[i]?.Equals(value) ?? (value == null))
			{
				return false;
			}
		}
		return true;
	}
}
