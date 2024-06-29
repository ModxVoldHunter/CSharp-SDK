using System.Collections.Generic;
using System.Xml.Schema;

namespace System.Runtime.Serialization;

internal sealed class SchemaObjectInfo
{
	internal XmlSchemaType _type;

	internal XmlSchemaElement _element;

	internal XmlSchema _schema;

	internal List<XmlSchemaType> _knownTypes;

	internal SchemaObjectInfo(XmlSchemaType type, XmlSchemaElement element, XmlSchema schema, List<XmlSchemaType> knownTypes)
	{
		_type = type;
		_element = element;
		_schema = schema;
		_knownTypes = knownTypes;
	}
}
