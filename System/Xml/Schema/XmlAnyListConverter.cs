using System.Collections;

namespace System.Xml.Schema;

internal sealed class XmlAnyListConverter : XmlListConverter
{
	public static readonly XmlValueConverter ItemList = new XmlAnyListConverter((XmlBaseConverter)XmlAnyConverter.Item);

	public static readonly XmlValueConverter AnyAtomicList = new XmlAnyListConverter((XmlBaseConverter)XmlAnyConverter.AnyAtomic);

	private XmlAnyListConverter(XmlBaseConverter atomicConverter)
		: base(atomicConverter)
	{
	}

	public override object ChangeType(object value, Type destinationType, IXmlNamespaceResolver nsResolver)
	{
		ArgumentNullException.ThrowIfNull(value, "value");
		ArgumentNullException.ThrowIfNull(destinationType, "destinationType");
		if (!(value is IEnumerable) || value.GetType() == XmlBaseConverter.StringType || value.GetType() == XmlBaseConverter.ByteArrayType)
		{
			value = new object[1] { value };
		}
		return ChangeListType(value, destinationType, nsResolver);
	}
}
