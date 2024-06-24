using System.Xml.Serialization;

namespace System.Xml.Schema;

public class XmlSchemaSequence : XmlSchemaGroupBase
{
	private XmlSchemaObjectCollection _items = new XmlSchemaObjectCollection();

	[XmlElement("element", typeof(XmlSchemaElement))]
	[XmlElement("group", typeof(XmlSchemaGroupRef))]
	[XmlElement("choice", typeof(XmlSchemaChoice))]
	[XmlElement("sequence", typeof(XmlSchemaSequence))]
	[XmlElement("any", typeof(XmlSchemaAny))]
	public override XmlSchemaObjectCollection Items => _items;

	internal override bool IsEmpty
	{
		get
		{
			if (!base.IsEmpty)
			{
				return _items.Count == 0;
			}
			return true;
		}
	}

	internal override void SetItems(XmlSchemaObjectCollection newItems)
	{
		_items = newItems;
	}
}
