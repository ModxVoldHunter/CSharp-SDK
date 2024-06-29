using System.Diagnostics.CodeAnalysis;
using System.Xml;

namespace System.Runtime.Serialization.DataContracts;

internal sealed class QNameDataContract : PrimitiveDataContract
{
	internal override string WriteMethodName => "WriteQName";

	internal override string ReadMethodName => "ReadElementContentAsQName";

	internal override bool IsPrimitive => false;

	public QNameDataContract()
		: base(typeof(XmlQualifiedName), DictionaryGlobals.QNameLocalName, DictionaryGlobals.SchemaNamespace)
	{
	}

	[RequiresDynamicCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.")]
	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	internal override void WriteXmlValue(XmlWriterDelegator writer, object obj, XmlObjectSerializerWriteContext context)
	{
		writer.WriteQName((XmlQualifiedName)obj);
	}

	[RequiresDynamicCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.")]
	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	internal override object ReadXmlValue(XmlReaderDelegator reader, XmlObjectSerializerReadContext context)
	{
		if (context == null)
		{
			if (!PrimitiveDataContract.TryReadNullAtTopLevel(reader))
			{
				return reader.ReadElementContentAsQName();
			}
			return null;
		}
		return PrimitiveDataContract.HandleReadValue(reader.ReadElementContentAsQName(), context);
	}

	[RequiresDynamicCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.")]
	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	internal override void WriteXmlElement(XmlWriterDelegator writer, object obj, XmlObjectSerializerWriteContext context, XmlDictionaryString name, XmlDictionaryString ns)
	{
		context.WriteQName(writer, (XmlQualifiedName)obj, name, ns);
	}

	internal override void WriteRootElement(XmlWriterDelegator writer, XmlDictionaryString name, XmlDictionaryString ns)
	{
		if (ns == DictionaryGlobals.SerializationNamespace)
		{
			writer.WriteStartElement("z", name, ns);
		}
		else if (ns != null && ns.Value != null && ns.Value.Length > 0)
		{
			writer.WriteStartElement("q", name, ns);
		}
		else
		{
			writer.WriteStartElement(name, ns);
		}
	}
}
