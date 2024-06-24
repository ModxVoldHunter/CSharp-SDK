using System.Globalization;

namespace System.ComponentModel;

public class Int128Converter : BaseNumberConverter
{
	internal override Type TargetType => typeof(Int128);

	internal override object FromString(string value, int radix)
	{
		return Int128.Parse(value, NumberStyles.HexNumber);
	}

	internal override object FromString(string value, NumberFormatInfo formatInfo)
	{
		return Int128.Parse(value, formatInfo);
	}

	internal override string ToString(object value, NumberFormatInfo formatInfo)
	{
		return ((Int128)value).ToString(formatInfo);
	}
}
