using System.Globalization;

namespace System.ComponentModel;

public class UInt128Converter : BaseNumberConverter
{
	internal override Type TargetType => typeof(UInt128);

	internal override object FromString(string value, int radix)
	{
		return UInt128.Parse(value, NumberStyles.HexNumber);
	}

	internal override object FromString(string value, NumberFormatInfo formatInfo)
	{
		return UInt128.Parse(value, formatInfo);
	}

	internal override string ToString(object value, NumberFormatInfo formatInfo)
	{
		return ((UInt128)value).ToString(formatInfo);
	}
}
