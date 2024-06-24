using System.Collections.Generic;
using System.Xml;
using System.Xml.Schema;

namespace System.Runtime.Serialization;

public static class XmlSerializableServices
{
	internal static string AddDefaultSchemaMethodName = "AddDefaultSchema";

	public static XmlNode[] ReadNodes(XmlReader xmlReader)
	{
		ArgumentNullException.ThrowIfNull(xmlReader, "xmlReader");
		XmlDocument xmlDocument = new XmlDocument();
		List<XmlNode> list = new List<XmlNode>();
		if (xmlReader.MoveToFirstAttribute())
		{
			do
			{
				if (IsValidAttribute(xmlReader))
				{
					XmlNode xmlNode = xmlDocument.ReadNode(xmlReader);
					if (xmlNode == null)
					{
						throw XmlObjectSerializer.CreateSerializationException(System.SR.UnexpectedEndOfFile);
					}
					list.Add(xmlNode);
				}
			}
			while (xmlReader.MoveToNextAttribute());
		}
		xmlReader.MoveToElement();
		if (!xmlReader.IsEmptyElement)
		{
			int depth = xmlReader.Depth;
			xmlReader.Read();
			while (xmlReader.Depth > depth && xmlReader.NodeType != XmlNodeType.EndElement)
			{
				XmlNode xmlNode2 = xmlDocument.ReadNode(xmlReader);
				if (xmlNode2 == null)
				{
					throw XmlObjectSerializer.CreateSerializationException(System.SR.UnexpectedEndOfFile);
				}
				list.Add(xmlNode2);
			}
		}
		return list.ToArray();
	}

	private static bool IsValidAttribute(XmlReader xmlReader)
	{
		if (xmlReader.NamespaceURI != "http://schemas.microsoft.com/2003/10/Serialization/" && xmlReader.NamespaceURI != "http://www.w3.org/2001/XMLSchema-instance" && xmlReader.Prefix != "xmlns")
		{
			return xmlReader.LocalName != "xmlns";
		}
		return false;
	}

	public static void WriteNodes(XmlWriter xmlWriter, XmlNode?[]? nodes)
	{
		ArgumentNullException.ThrowIfNull(xmlWriter, "xmlWriter");
		if (nodes == null)
		{
			return;
		}
		for (int i = 0; i < nodes.Length; i++)
		{
			if (nodes[i] != null)
			{
				nodes[i].WriteTo(xmlWriter);
			}
		}
	}

	public static void AddDefaultSchema(XmlSchemaSet schemas, XmlQualifiedName typeQName)
	{
		ArgumentNullException.ThrowIfNull(schemas, "schemas");
		ArgumentNullException.ThrowIfNull(typeQName, "typeQName");
		SchemaExporter.AddDefaultXmlType(schemas, typeQName.Name, typeQName.Namespace);
	}
}
