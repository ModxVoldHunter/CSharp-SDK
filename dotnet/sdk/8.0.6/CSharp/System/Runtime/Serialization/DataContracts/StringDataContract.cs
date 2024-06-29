using System.Diagnostics.CodeAnalysis;
using System.Xml;

namespace System.Runtime.Serialization.DataContracts;

internal class StringDataContract : PrimitiveDataContract
{
	internal override string WriteMethodName => "WriteString";

	internal override string ReadMethodName => "ReadElementContentAsString";

	public StringDataContract()
		: this(DictionaryGlobals.StringLocalName, DictionaryGlobals.SchemaNamespace)
	{
	}

	internal StringDataContract(XmlDictionaryString name, XmlDictionaryString ns)
		: base(typeof(string), name, ns)
	{
	}

	[RequiresDynamicCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.")]
	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	internal override void WriteXmlValue(XmlWriterDelegator writer, object obj, XmlObjectSerializerWriteContext context)
	{
		writer.WriteString((string)obj);
	}

	[RequiresDynamicCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.")]
	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	internal override object ReadXmlValue(XmlReaderDelegator reader, XmlObjectSerializerReadContext context)
	{
		if (context == null)
		{
			if (!PrimitiveDataContract.TryReadNullAtTopLevel(reader))
			{
				return reader.ReadElementContentAsString();
			}
			return null;
		}
		return PrimitiveDataContract.HandleReadValue(reader.ReadElementContentAsString(), context);
	}

	[RequiresDynamicCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.")]
	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	internal override void WriteXmlElement(XmlWriterDelegator xmlWriter, object obj, XmlObjectSerializerWriteContext context, XmlDictionaryString name, XmlDictionaryString ns)
	{
		context.WriteString(xmlWriter, (string)obj, name, ns);
	}
}
