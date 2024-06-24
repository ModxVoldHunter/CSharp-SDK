using System.Diagnostics.CodeAnalysis;
using System.Xml;

namespace System.Runtime.Serialization.DataContracts;

internal class CharDataContract : PrimitiveDataContract
{
	internal override string WriteMethodName => "WriteChar";

	internal override string ReadMethodName => "ReadElementContentAsChar";

	public CharDataContract()
		: this(DictionaryGlobals.CharLocalName, DictionaryGlobals.SerializationNamespace)
	{
	}

	internal CharDataContract(XmlDictionaryString name, XmlDictionaryString ns)
		: base(typeof(char), name, ns)
	{
	}

	[RequiresDynamicCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.")]
	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	internal override void WriteXmlValue(XmlWriterDelegator writer, object obj, XmlObjectSerializerWriteContext context)
	{
		writer.WriteChar((char)obj);
	}

	[RequiresDynamicCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.")]
	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	internal override object ReadXmlValue(XmlReaderDelegator reader, XmlObjectSerializerReadContext context)
	{
		if (context != null)
		{
			return PrimitiveDataContract.HandleReadValue(reader.ReadElementContentAsChar(), context);
		}
		return reader.ReadElementContentAsChar();
	}

	[RequiresDynamicCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.")]
	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	internal override void WriteXmlElement(XmlWriterDelegator xmlWriter, object obj, XmlObjectSerializerWriteContext context, XmlDictionaryString name, XmlDictionaryString ns)
	{
		xmlWriter.WriteChar((char)obj, name, ns);
	}
}
