namespace System.ComponentModel.DataAnnotations;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter, AllowMultiple = false)]
public sealed class CreditCardAttribute : DataTypeAttribute
{
	public CreditCardAttribute()
		: base(DataType.CreditCard)
	{
		base.DefaultErrorMessage = System.SR.CreditCardAttribute_Invalid;
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
		int num = 0;
		bool flag = false;
		for (int num2 = text.Length - 1; num2 >= 0; num2--)
		{
			char c = text[num2];
			if (!char.IsAsciiDigit(c))
			{
				if ((c != ' ' && c != '-') || 1 == 0)
				{
					return false;
				}
			}
			else
			{
				int num3 = (c - 48) * ((!flag) ? 1 : 2);
				flag = !flag;
				while (num3 > 0)
				{
					num += num3 % 10;
					num3 /= 10;
				}
			}
		}
		return num % 10 == 0;
	}
}
