namespace System.ComponentModel.DataAnnotations;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter, AllowMultiple = false)]
public sealed class PhoneAttribute : DataTypeAttribute
{
	public PhoneAttribute()
		: base(DataType.PhoneNumber)
	{
		base.DefaultErrorMessage = System.SR.PhoneAttribute_Invalid;
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
		ReadOnlySpan<char> potentialPhoneNumber = text.Replace("+", string.Empty).AsSpan().TrimEnd();
		potentialPhoneNumber = RemoveExtension(potentialPhoneNumber);
		bool flag = false;
		ReadOnlySpan<char> readOnlySpan = potentialPhoneNumber;
		for (int i = 0; i < readOnlySpan.Length; i++)
		{
			char c = readOnlySpan[i];
			if (char.IsDigit(c))
			{
				flag = true;
				break;
			}
		}
		if (!flag)
		{
			return false;
		}
		ReadOnlySpan<char> readOnlySpan2 = potentialPhoneNumber;
		for (int j = 0; j < readOnlySpan2.Length; j++)
		{
			char c2 = readOnlySpan2[j];
			if (!char.IsDigit(c2) && !char.IsWhiteSpace(c2) && !"-.()".Contains(c2))
			{
				return false;
			}
		}
		return true;
	}

	private static ReadOnlySpan<char> RemoveExtension(ReadOnlySpan<char> potentialPhoneNumber)
	{
		int num = potentialPhoneNumber.LastIndexOf("ext.", StringComparison.OrdinalIgnoreCase);
		if (num >= 0)
		{
			ReadOnlySpan<char> potentialExtension = potentialPhoneNumber.Slice(num + "ext.".Length);
			if (MatchesExtension(potentialExtension))
			{
				return potentialPhoneNumber.Slice(0, num);
			}
		}
		num = potentialPhoneNumber.LastIndexOf("ext", StringComparison.OrdinalIgnoreCase);
		if (num >= 0)
		{
			ReadOnlySpan<char> potentialExtension2 = potentialPhoneNumber.Slice(num + "ext".Length);
			if (MatchesExtension(potentialExtension2))
			{
				return potentialPhoneNumber.Slice(0, num);
			}
		}
		num = potentialPhoneNumber.LastIndexOf("x", StringComparison.OrdinalIgnoreCase);
		if (num >= 0)
		{
			ReadOnlySpan<char> potentialExtension3 = potentialPhoneNumber.Slice(num + "x".Length);
			if (MatchesExtension(potentialExtension3))
			{
				return potentialPhoneNumber.Slice(0, num);
			}
		}
		return potentialPhoneNumber;
	}

	private static bool MatchesExtension(ReadOnlySpan<char> potentialExtension)
	{
		potentialExtension = potentialExtension.TrimStart();
		if (potentialExtension.Length == 0)
		{
			return false;
		}
		ReadOnlySpan<char> readOnlySpan = potentialExtension;
		for (int i = 0; i < readOnlySpan.Length; i++)
		{
			char c = readOnlySpan[i];
			if (!char.IsDigit(c))
			{
				return false;
			}
		}
		return true;
	}
}
