using System.Globalization;

namespace System;

internal static class AppContextConfigHelper
{
	internal static bool GetBooleanConfig(string configName, bool defaultValue)
	{
		if (!AppContext.TryGetSwitch(configName, out var isEnabled))
		{
			return defaultValue;
		}
		return isEnabled;
	}

	internal static bool GetBooleanConfig(string switchName, string envVariable, bool defaultValue = false)
	{
		if (!AppContext.TryGetSwitch(switchName, out var isEnabled))
		{
			string environmentVariable = Environment.GetEnvironmentVariable(envVariable);
			return (environmentVariable == null) ? defaultValue : (bool.IsTrueStringIgnoreCase(environmentVariable) || environmentVariable.Equals("1"));
		}
		return isEnabled;
	}

	internal static int GetInt32Config(string configName, int defaultValue, bool allowNegative = true)
	{
		try
		{
			object data = AppContext.GetData(configName);
			int num = defaultValue;
			if (!(data is uint num2))
			{
				if (data is string text)
				{
					num = ((!text.StartsWith('0')) ? int.Parse(text, NumberStyles.AllowLeadingSign, NumberFormatInfo.InvariantInfo) : ((text.Length < 2 || text[1] != 'x') ? Convert.ToInt32(text, 8) : Convert.ToInt32(text, 16)));
				}
				else if (data is IConvertible convertible)
				{
					num = convertible.ToInt32(NumberFormatInfo.InvariantInfo);
				}
			}
			else
			{
				num = (int)num2;
			}
			return (!allowNegative && num < 0) ? defaultValue : num;
		}
		catch (FormatException)
		{
			return defaultValue;
		}
		catch (OverflowException)
		{
			return defaultValue;
		}
	}

	internal static int GetInt32Config(string configName, string envVariable, int defaultValue, bool allowNegative = true)
	{
		string environmentVariable = Environment.GetEnvironmentVariable(envVariable);
		if (environmentVariable != null)
		{
			try
			{
				int num = ((!environmentVariable.StartsWith('0')) ? int.Parse(environmentVariable, NumberStyles.AllowLeadingSign, NumberFormatInfo.InvariantInfo) : ((environmentVariable.Length < 2 || environmentVariable[1] != 'x') ? Convert.ToInt32(environmentVariable, 8) : Convert.ToInt32(environmentVariable, 16)));
				if (allowNegative || num >= 0)
				{
					return num;
				}
			}
			catch (FormatException)
			{
			}
			catch (OverflowException)
			{
			}
		}
		return GetInt32Config(configName, defaultValue, allowNegative);
	}

	internal static short GetInt16Config(string configName, short defaultValue, bool allowNegative = true)
	{
		try
		{
			object data = AppContext.GetData(configName);
			short num = defaultValue;
			if (!(data is uint num2))
			{
				if (data is string text)
				{
					num = (text.StartsWith("0x") ? Convert.ToInt16(text, 16) : ((!text.StartsWith('0')) ? short.Parse(text, NumberStyles.AllowLeadingSign, NumberFormatInfo.InvariantInfo) : Convert.ToInt16(text, 8)));
				}
				else if (data is IConvertible convertible)
				{
					num = convertible.ToInt16(NumberFormatInfo.InvariantInfo);
				}
			}
			else
			{
				num = (short)num2;
				if ((uint)num != num2)
				{
					return defaultValue;
				}
			}
			return (!allowNegative && num < 0) ? defaultValue : num;
		}
		catch (FormatException)
		{
			return defaultValue;
		}
		catch (OverflowException)
		{
			return defaultValue;
		}
	}

	internal static short GetInt16Config(string configName, string envVariable, short defaultValue, bool allowNegative = true)
	{
		string environmentVariable = Environment.GetEnvironmentVariable(envVariable);
		if (environmentVariable != null)
		{
			try
			{
				short num = ((!environmentVariable.StartsWith('0')) ? short.Parse(environmentVariable, NumberStyles.AllowLeadingSign, NumberFormatInfo.InvariantInfo) : ((environmentVariable.Length < 2 || environmentVariable[1] != 'x') ? Convert.ToInt16(environmentVariable, 8) : Convert.ToInt16(environmentVariable, 16)));
				if (allowNegative || num >= 0)
				{
					return num;
				}
			}
			catch (FormatException)
			{
			}
			catch (OverflowException)
			{
			}
		}
		return GetInt16Config(configName, defaultValue, allowNegative);
	}
}
