using System.Diagnostics.CodeAnalysis;
using System.Xml;

namespace System.Runtime.Serialization.DataContracts;

internal sealed class ByteArrayDataContract : PrimitiveDataContract
{
	internal override string WriteMethodName => "WriteBase64";

	internal override string ReadMethodName => "ReadElementContentAsBase64";

	public ByteArrayDataContract()
		: base(typeof(byte[]), DictionaryGlobals.ByteArrayLocalName, DictionaryGlobals.SchemaNamespace)
	{
	}

	[RequiresDynamicCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.")]
	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	internal override void WriteXmlValue(XmlWriterDelegator writer, object obj, XmlObjectSerializerWriteContext context)
	{
		writer.WriteBase64((byte[])obj);
	}

	[RequiresDynamicCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.")]
	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	internal override object ReadXmlValue(XmlReaderDelegator reader, XmlObjectSerializerReadContext context)
	{
		if (context == null)
		{
			if (!PrimitiveDataContract.TryReadNullAtTopLevel(reader))
			{
				return reader.ReadElementContentAsBase64();
			}
			return null;
		}
		return PrimitiveDataContract.HandleReadValue(reader.ReadElementContentAsBase64(), context);
	}

	[RequiresDynamicCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.")]
	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	internal override void WriteXmlElement(XmlWriterDelegator xmlWriter, object obj, XmlObjectSerializerWriteContext context, XmlDictionaryString name, XmlDictionaryString ns)
	{
		xmlWriter.WriteStartElement(name, ns);
		xmlWriter.WriteBase64((byte[])obj);
		xmlWriter.WriteEndElement();
	}
}
