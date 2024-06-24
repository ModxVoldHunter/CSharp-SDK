using System.Diagnostics.CodeAnalysis;
using System.Xml;

namespace System.Runtime.Serialization.DataContracts;

internal sealed class SignedByteDataContract : PrimitiveDataContract
{
	internal override string WriteMethodName => "WriteSignedByte";

	internal override string ReadMethodName => "ReadElementContentAsSignedByte";

	public SignedByteDataContract()
		: base(typeof(sbyte), DictionaryGlobals.SignedByteLocalName, DictionaryGlobals.SchemaNamespace)
	{
	}

	[RequiresDynamicCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.")]
	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	internal override void WriteXmlValue(XmlWriterDelegator writer, object obj, XmlObjectSerializerWriteContext context)
	{
		writer.WriteSignedByte((sbyte)obj);
	}

	[RequiresDynamicCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.")]
	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	internal override object ReadXmlValue(XmlReaderDelegator reader, XmlObjectSerializerReadContext context)
	{
		if (context != null)
		{
			return PrimitiveDataContract.HandleReadValue(reader.ReadElementContentAsSignedByte(), context);
		}
		return reader.ReadElementContentAsSignedByte();
	}

	[RequiresDynamicCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.")]
	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	internal override void WriteXmlElement(XmlWriterDelegator xmlWriter, object obj, XmlObjectSerializerWriteContext context, XmlDictionaryString name, XmlDictionaryString ns)
	{
		xmlWriter.WriteSignedByte((sbyte)obj, name, ns);
	}
}
