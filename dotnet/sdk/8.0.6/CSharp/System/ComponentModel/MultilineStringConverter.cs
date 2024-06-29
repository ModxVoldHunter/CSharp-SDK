using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace System.ComponentModel;

public class MultilineStringConverter : TypeConverter
{
	public override object? ConvertTo(ITypeDescriptorContext? context, CultureInfo? culture, object? value, Type destinationType)
	{
		ArgumentNullException.ThrowIfNull(destinationType, "destinationType");
		if (destinationType == typeof(string) && value is string)
		{
			if (!System.SR.UsingResourceKeys())
			{
				return System.SR.Text;
			}
			return "(Text)";
		}
		return base.ConvertTo(context, culture, value, destinationType);
	}

	[RequiresUnreferencedCode("The Type of value cannot be statically discovered. The public parameterless constructor or the 'Default' static field may be trimmed from the Attribute's Type.")]
	public override PropertyDescriptorCollection? GetProperties(ITypeDescriptorContext? context, object value, Attribute[]? attributes)
	{
		return null;
	}

	public override bool GetPropertiesSupported(ITypeDescriptorContext? context)
	{
		return false;
	}
}
