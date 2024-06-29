using System.ComponentModel.Design.Serialization;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace System.ComponentModel;

public class TimeOnlyConverter : TypeConverter
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
				return TimeOnly.MinValue;
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
					return TimeOnly.Parse(text2, dateTimeFormatInfo);
				}
				return TimeOnly.Parse(text2, culture);
			}
			catch (FormatException innerException)
			{
				throw new FormatException(System.SR.Format(System.SR.ConvertInvalidPrimitive, (string)value, "TimeOnly"), innerException);
			}
		}
		return base.ConvertFrom(context, culture, value);
	}

	public override object? ConvertTo(ITypeDescriptorContext? context, CultureInfo? culture, object? value, Type destinationType)
	{
		if (destinationType == typeof(string) && value is TimeOnly timeOnly)
		{
			if (timeOnly == TimeOnly.MinValue)
			{
				return string.Empty;
			}
			if (culture == null)
			{
				culture = CultureInfo.CurrentCulture;
			}
			return timeOnly.ToString(culture.DateTimeFormat.ShortTimePattern, culture);
		}
		if (destinationType == typeof(InstanceDescriptor) && value is TimeOnly timeOnly2)
		{
			if (timeOnly2.Ticks == 0L)
			{
				return new InstanceDescriptor(typeof(TimeOnly).GetConstructor(new Type[1] { typeof(long) }), new object[1] { timeOnly2.Ticks });
			}
			return new InstanceDescriptor(typeof(TimeOnly).GetConstructor(new Type[5]
			{
				typeof(int),
				typeof(int),
				typeof(int),
				typeof(int),
				typeof(int)
			}), new object[5] { timeOnly2.Hour, timeOnly2.Minute, timeOnly2.Second, timeOnly2.Millisecond, timeOnly2.Microsecond });
		}
		return base.ConvertTo(context, culture, value, destinationType);
	}
}
