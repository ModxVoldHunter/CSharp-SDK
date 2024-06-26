using System.ComponentModel.Design.Serialization;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace System.ComponentModel;

public class DateOnlyConverter : TypeConverter
{
	public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType)
	{
		if (!(sourceType == typeof(string)))
		{
			return base.CanConvertFrom(context, sourceType);
		}
		return true;
	}

	public override bool CanConvertTo(ITypeDescriptorContext? context, [NotNullWhen(true)] Type? destinationType)
	{
		if (!(destinationType == typeof(InstanceDescriptor)))
		{
			return base.CanConvertTo(context, destinationType);
		}
		return true;
	}

	public override object? ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value)
	{
		if (value is string text)
		{
			string text2 = text.Trim();
			if (text2.Length == 0)
			{
				return DateOnly.MinValue;
			}
			try
			{
				DateTimeFormatInfo dateTimeFormatInfo = null;
				if (culture != null)
				{
					dateTimeFormatInfo = (DateTimeFormatInfo)culture.GetFormat(typeof(DateTimeFormatInfo));
				}
				if (dateTimeFormatInfo != null)
				{
					return DateOnly.Parse(text2, dateTimeFormatInfo);
				}
				return DateOnly.Parse(text2, culture);
			}
			catch (FormatException innerException)
			{
				throw new FormatException(System.SR.Format(System.SR.ConvertInvalidPrimitive, (string)value, "DateOnly"), innerException);
			}
		}
		return base.ConvertFrom(context, culture, value);
	}

	public override object? ConvertTo(ITypeDescriptorContext? context, CultureInfo? culture, object? value, Type destinationType)
	{
		if (destinationType == typeof(string) && value is DateOnly dateOnly)
		{
			if (dateOnly == DateOnly.MinValue)
			{
				return string.Empty;
			}
			if (culture == null)
			{
				culture = CultureInfo.CurrentCulture;
			}
			if (culture == CultureInfo.InvariantCulture)
			{
				return dateOnly.ToString("yyyy-MM-dd", culture);
			}
			return dateOnly.ToString(culture.DateTimeFormat.ShortDatePattern, culture);
		}
		if (destinationType == typeof(InstanceDescriptor) && value is DateOnly dateOnly2)
		{
			return new InstanceDescriptor(typeof(DateOnly).GetConstructor(new Type[3]
			{
				typeof(int),
				typeof(int),
				typeof(int)
			}), new object[3] { dateOnly2.Year, dateOnly2.Month, dateOnly2.Day });
		}
		return base.ConvertTo(context, culture, value, destinationType);
	}
}
