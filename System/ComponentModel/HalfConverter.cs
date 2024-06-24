using System.Globalization;

namespace System.ComponentModel;

public class HalfConverter : BaseNumberConverter
{
	internal override bool AllowHex => false;

	internal override Type TargetType => typeof(Half);

	internal override object FromString(string value, int radix)
	{
		throw new NotImplementedException();
	}

	internal override object FromString(string value, NumberFormatInfo formatInfo)
	{
		return Half.Parse(value, formatInfo);
	}

	internal override string ToString(object value, NumberFormatInfo formatInfo)
	{
		return ((Half)value).ToString(formatInfo);
	}
}
