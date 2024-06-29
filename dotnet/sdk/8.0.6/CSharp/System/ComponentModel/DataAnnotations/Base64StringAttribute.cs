using System.Buffers.Text;

namespace System.ComponentModel.DataAnnotations;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter, AllowMultiple = false)]
public class Base64StringAttribute : ValidationAttribute
{
	public Base64StringAttribute()
	{
		base.DefaultErrorMessage = System.SR.Base64StringAttribute_Invalid;
	}

	public override bool IsValid(object? value)
	{
		if (value == null)
		{
			return true;
		}
		if (!(value is string text))
		{
			return false;
		}
		return Base64.IsValid(text);
	}
}
