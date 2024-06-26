namespace System.Xml.Schema;

internal sealed class XmlBooleanConverter : XmlBaseConverter
{
	private XmlBooleanConverter(XmlSchemaType schemaType)
		: base(schemaType)
	{
	}

	public static XmlValueConverter Create(XmlSchemaType schemaType)
	{
		return new XmlBooleanConverter(schemaType);
	}

	public override bool ToBoolean(string value)
	{
		ArgumentNullException.ThrowIfNull(value, "value");
		return XmlConvert.ToBoolean(value);
	}

	public override bool ToBoolean(object value)
	{
		ArgumentNullException.ThrowIfNull(value, "value");
		Type type = value.GetType();
		if (type == XmlBaseConverter.BooleanType)
		{
			return (bool)value;
		}
		if (type == XmlBaseConverter.StringType)
		{
			return XmlConvert.ToBoolean((string)value);
		}
		if (type == XmlBaseConverter.XmlAtomicValueType)
		{
			return ((XmlAtomicValue)value).ValueAsBoolean;
		}
		return (bool)ChangeListType(value, XmlBaseConverter.BooleanType, null);
	}

	public override string ToString(bool value)
	{
		return XmlConvert.ToString(value);
	}

	public override string ToString(object value, IXmlNamespaceResolver nsResolver)
	{
		ArgumentNullException.ThrowIfNull(value, "value");
		Type type = value.GetType();
		if (type == XmlBaseConverter.BooleanType)
		{
			return XmlConvert.ToString((bool)value);
		}
		if (type == XmlBaseConverter.StringType)
		{
			return (string)value;
		}
		if (type == XmlBaseConverter.XmlAtomicValueType)
		{
			return ((XmlAtomicValue)value).Value;
		}
		return (string)ChangeListType(value, XmlBaseConverter.StringType, nsResolver);
	}

	public override object ChangeType(bool value, Type destinationType)
	{
		ArgumentNullException.ThrowIfNull(destinationType, "destinationType");
		if (destinationType == XmlBaseConverter.ObjectType)
		{
			destinationType = base.DefaultClrType;
		}
		if (destinationType == XmlBaseConverter.BooleanType)
		{
			return value;
		}
		if (destinationType == XmlBaseConverter.StringType)
		{
			return XmlConvert.ToString(value);
		}
		if (destinationType == XmlBaseConverter.XmlAtomicValueType)
		{
			return new XmlAtomicValue(base.SchemaType, value);
		}
		if (destinationType == XmlBaseConverter.XPathItemType)
		{
			return new XmlAtomicValue(base.SchemaType, value);
		}
		return ChangeListType(value, destinationType, null);
	}

	public override object ChangeType(string value, Type destinationType, IXmlNamespaceResolver nsResolver)
	{
		ArgumentNullException.ThrowIfNull(value, "value");
		ArgumentNullException.ThrowIfNull(destinationType, "destinationType");
		if (destinationType == XmlBaseConverter.ObjectType)
		{
			destinationType = base.DefaultClrType;
		}
		if (destinationType == XmlBaseConverter.BooleanType)
		{
			return XmlConvert.ToBoolean(value);
		}
		if (destinationType == XmlBaseConverter.StringType)
		{
			return value;
		}
		if (destinationType == XmlBaseConverter.XmlAtomicValueType)
		{
			return new XmlAtomicValue(base.SchemaType, value);
		}
		if (destinationType == XmlBaseConverter.XPathItemType)
		{
			return new XmlAtomicValue(base.SchemaType, value);
		}
		return ChangeListType(value, destinationType, nsResolver);
	}

	public override object ChangeType(object value, Type destinationType, IXmlNamespaceResolver nsResolver)
	{
		ArgumentNullException.ThrowIfNull(value, "value");
		ArgumentNullException.ThrowIfNull(destinationType, "destinationType");
		Type type = value.GetType();
		if (destinationType == XmlBaseConverter.ObjectType)
		{
			destinationType = base.DefaultClrType;
		}
		if (destinationType == XmlBaseConverter.BooleanType)
		{
			return ToBoolean(value);
		}
		if (destinationType == XmlBaseConverter.StringType)
		{
			return ToString(value, nsResolver);
		}
		if (destinationType == XmlBaseConverter.XmlAtomicValueType)
		{
			if (type == XmlBaseConverter.BooleanType)
			{
				return new XmlAtomicValue(base.SchemaType, (bool)value);
			}
			if (type == XmlBaseConverter.StringType)
			{
				return new XmlAtomicValue(base.SchemaType, (string)value);
			}
			if (type == XmlBaseConverter.XmlAtomicValueType)
			{
				return (XmlAtomicValue)value;
			}
		}
		if (destinationType == XmlBaseConverter.XPathItemType)
		{
			if (type == XmlBaseConverter.BooleanType)
			{
				return new XmlAtomicValue(base.SchemaType, (bool)value);
			}
			if (type == XmlBaseConverter.StringType)
			{
				return new XmlAtomicValue(base.SchemaType, (string)value);
			}
			if (type == XmlBaseConverter.XmlAtomicValueType)
			{
				return (XmlAtomicValue)value;
			}
		}
		return ChangeListType(value, destinationType, nsResolver);
	}
}
