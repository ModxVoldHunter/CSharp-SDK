using System.Xml;

namespace System.Runtime.Serialization;

internal sealed class SchemaDefinedType
{
	internal XmlQualifiedName _xmlName;

	public SchemaDefinedType(XmlQualifiedName xmlName)
	{
		_xmlName = xmlName;
	}
}
