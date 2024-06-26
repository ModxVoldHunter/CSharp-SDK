using System.Collections.Generic;
using System.ComponentModel;
using System.Xml.Schema;
using System.Xml.XPath;

namespace System.Xml.Xsl.Runtime;

[EditorBrowsable(EditorBrowsableState.Never)]
public static class XsltConvert
{
	public static bool ToBoolean(XPathItem item)
	{
		if (item.IsNode)
		{
			return true;
		}
		Type valueType = item.ValueType;
		if (valueType == typeof(string))
		{
			return item.Value.Length != 0;
		}
		if (valueType == typeof(double))
		{
			double valueAsDouble = item.ValueAsDouble;
			if (!(valueAsDouble < 0.0))
			{
				return 0.0 < valueAsDouble;
			}
			return true;
		}
		return item.ValueAsBoolean;
	}

	public static bool ToBoolean(IList<XPathItem> listItems)
	{
		if (listItems.Count == 0)
		{
			return false;
		}
		return ToBoolean(listItems[0]);
	}

	public static double ToDouble(string value)
	{
		return XPathConvert.StringToDouble(value);
	}

	public static double ToDouble(XPathItem item)
	{
		if (item.IsNode)
		{
			return XPathConvert.StringToDouble(item.Value);
		}
		Type valueType = item.ValueType;
		if (valueType == typeof(string))
		{
			return XPathConvert.StringToDouble(item.Value);
		}
		if (valueType == typeof(double))
		{
			return item.ValueAsDouble;
		}
		if (!item.ValueAsBoolean)
		{
			return 0.0;
		}
		return 1.0;
	}

	public static double ToDouble(IList<XPathItem> listItems)
	{
		if (listItems.Count == 0)
		{
			return double.NaN;
		}
		return ToDouble(listItems[0]);
	}

	public static XPathNavigator ToNode(XPathItem item)
	{
		if (!item.IsNode)
		{
			XPathDocument xPathDocument = new XPathDocument();
			XmlRawWriter xmlRawWriter = xPathDocument.LoadFromWriter(XPathDocument.LoadFlags.AtomizeNames, string.Empty);
			xmlRawWriter.WriteString(ToString(item));
			xmlRawWriter.Close();
			return xPathDocument.CreateNavigator();
		}
		if (item is RtfNavigator rtfNavigator)
		{
			return rtfNavigator.ToNavigator();
		}
		return (XPathNavigator)item;
	}

	public static XPathNavigator ToNode(IList<XPathItem> listItems)
	{
		if (listItems.Count == 1)
		{
			return ToNode(listItems[0]);
		}
		throw new XslTransformException(System.SR.Xslt_NodeSetNotNode, string.Empty);
	}

	public static IList<XPathNavigator> ToNodeSet(XPathItem item)
	{
		return new XmlQueryNodeSequence(ToNode(item));
	}

	public static IList<XPathNavigator> ToNodeSet(IList<XPathItem> listItems)
	{
		if (listItems.Count == 1)
		{
			return new XmlQueryNodeSequence(ToNode(listItems[0]));
		}
		return XmlILStorageConverter.ItemsToNavigators(listItems);
	}

	public static string ToString(double value)
	{
		return XPathConvert.DoubleToString(value);
	}

	public static string ToString(XPathItem item)
	{
		if (!item.IsNode && item.ValueType == typeof(double))
		{
			return XPathConvert.DoubleToString(item.ValueAsDouble);
		}
		return item.Value;
	}

	public static string ToString(IList<XPathItem> listItems)
	{
		if (listItems.Count == 0)
		{
			return string.Empty;
		}
		return ToString(listItems[0]);
	}

	public static string ToString(DateTime value)
	{
		return new XsdDateTime(value, XsdDateTimeFlags.DateTime).ToString();
	}

	public static double ToDouble(decimal value)
	{
		return (double)value;
	}

	public static double ToDouble(int value)
	{
		return value;
	}

	public static double ToDouble(long value)
	{
		return value;
	}

	public static decimal ToDecimal(double value)
	{
		return (decimal)value;
	}

	public static int ToInt(double value)
	{
		return checked((int)value);
	}

	public static long ToLong(double value)
	{
		return checked((long)value);
	}

	public static DateTime ToDateTime(string value)
	{
		return new XsdDateTime(value, XsdDateTimeFlags.AllXsd);
	}

	internal static XmlAtomicValue ConvertToType(XmlAtomicValue value, XmlQueryType destinationType)
	{
		switch (destinationType.TypeCode)
		{
		case XmlTypeCode.Boolean:
		{
			XmlTypeCode typeCode = value.XmlType.TypeCode;
			if ((uint)(typeCode - 12) <= 1u || typeCode == XmlTypeCode.Double)
			{
				return new XmlAtomicValue(destinationType.SchemaType, ToBoolean(value));
			}
			break;
		}
		case XmlTypeCode.DateTime:
			if (value.XmlType.TypeCode == XmlTypeCode.String)
			{
				return new XmlAtomicValue(destinationType.SchemaType, ToDateTime(value.Value));
			}
			break;
		case XmlTypeCode.Decimal:
			if (value.XmlType.TypeCode == XmlTypeCode.Double)
			{
				return new XmlAtomicValue(destinationType.SchemaType, ToDecimal(value.ValueAsDouble));
			}
			break;
		case XmlTypeCode.Double:
			switch (value.XmlType.TypeCode)
			{
			case XmlTypeCode.String:
			case XmlTypeCode.Boolean:
			case XmlTypeCode.Double:
				return new XmlAtomicValue(destinationType.SchemaType, ToDouble(value));
			case XmlTypeCode.Decimal:
				return new XmlAtomicValue(destinationType.SchemaType, ToDouble((decimal)value.ValueAs(typeof(decimal), null)));
			case XmlTypeCode.Long:
			case XmlTypeCode.Int:
				return new XmlAtomicValue(destinationType.SchemaType, ToDouble(value.ValueAsLong));
			}
			break;
		case XmlTypeCode.Long:
		case XmlTypeCode.Int:
			if (value.XmlType.TypeCode == XmlTypeCode.Double)
			{
				return new XmlAtomicValue(destinationType.SchemaType, ToLong(value.ValueAsDouble));
			}
			break;
		case XmlTypeCode.String:
			switch (value.XmlType.TypeCode)
			{
			case XmlTypeCode.String:
			case XmlTypeCode.Boolean:
			case XmlTypeCode.Double:
				return new XmlAtomicValue(destinationType.SchemaType, ToString(value));
			case XmlTypeCode.DateTime:
				return new XmlAtomicValue(destinationType.SchemaType, ToString(value.ValueAsDateTime));
			}
			break;
		}
		return value;
	}

	public static IList<XPathNavigator> EnsureNodeSet(IList<XPathItem> listItems)
	{
		if (listItems.Count == 1)
		{
			XPathItem xPathItem = listItems[0];
			if (!xPathItem.IsNode)
			{
				throw new XslTransformException(System.SR.XPath_NodeSetExpected, string.Empty);
			}
			if (xPathItem is RtfNavigator)
			{
				throw new XslTransformException(System.SR.XPath_RtfInPathExpr, string.Empty);
			}
		}
		return XmlILStorageConverter.ItemsToNavigators(listItems);
	}

	internal static XmlQueryType InferXsltType(Type clrType)
	{
		if (clrType == typeof(bool))
		{
			return XmlQueryTypeFactory.BooleanX;
		}
		if (clrType == typeof(byte))
		{
			return XmlQueryTypeFactory.DoubleX;
		}
		if (clrType == typeof(decimal))
		{
			return XmlQueryTypeFactory.DoubleX;
		}
		if (clrType == typeof(DateTime))
		{
			return XmlQueryTypeFactory.StringX;
		}
		if (clrType == typeof(double))
		{
			return XmlQueryTypeFactory.DoubleX;
		}
		if (clrType == typeof(short))
		{
			return XmlQueryTypeFactory.DoubleX;
		}
		if (clrType == typeof(int))
		{
			return XmlQueryTypeFactory.DoubleX;
		}
		if (clrType == typeof(long))
		{
			return XmlQueryTypeFactory.DoubleX;
		}
		if (clrType == typeof(IXPathNavigable))
		{
			return XmlQueryTypeFactory.NodeNotRtf;
		}
		if (clrType == typeof(sbyte))
		{
			return XmlQueryTypeFactory.DoubleX;
		}
		if (clrType == typeof(float))
		{
			return XmlQueryTypeFactory.DoubleX;
		}
		if (clrType == typeof(string))
		{
			return XmlQueryTypeFactory.StringX;
		}
		if (clrType == typeof(ushort))
		{
			return XmlQueryTypeFactory.DoubleX;
		}
		if (clrType == typeof(uint))
		{
			return XmlQueryTypeFactory.DoubleX;
		}
		if (clrType == typeof(ulong))
		{
			return XmlQueryTypeFactory.DoubleX;
		}
		if (clrType == typeof(XPathNavigator[]))
		{
			return XmlQueryTypeFactory.NodeSDod;
		}
		if (clrType == typeof(XPathNavigator))
		{
			return XmlQueryTypeFactory.NodeNotRtf;
		}
		if (clrType == typeof(XPathNodeIterator))
		{
			return XmlQueryTypeFactory.NodeSDod;
		}
		if (clrType.IsEnum)
		{
			return XmlQueryTypeFactory.DoubleX;
		}
		if (clrType == typeof(void))
		{
			return XmlQueryTypeFactory.Empty;
		}
		return XmlQueryTypeFactory.ItemS;
	}
}
