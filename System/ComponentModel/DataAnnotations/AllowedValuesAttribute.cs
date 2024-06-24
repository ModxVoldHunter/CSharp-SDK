namespace System.ComponentModel.DataAnnotations;

[CLSCompliant(false)]
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter, AllowMultiple = false)]
public class AllowedValuesAttribute : ValidationAttribute
{
	public object?[] Values { get; }

	public AllowedValuesAttribute(params object?[] values)
	{
		ArgumentNullException.ThrowIfNull(values, "values");
		Values = values;
		base.DefaultErrorMessage = System.SR.AllowedValuesAttribute_Invalid;
	}

	public override bool IsValid(object? value)
	{
		object[] values = Values;
		for (int i = 0; i < values.Length; i++)
		{
			if (values[i]?.Equals(value) ?? (value == null))
			{
				return true;
			}
		}
		return false;
	}
}
