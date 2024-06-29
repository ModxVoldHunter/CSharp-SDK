using System.Diagnostics.CodeAnalysis;
using System.Xml;

namespace System.Runtime.Serialization.DataContracts;

internal class TimeSpanDataContract : PrimitiveDataContract
{
	internal override string WriteMethodName => "WriteTimeSpan";

	internal override string ReadMethodName => "ReadElementContentAsTimeSpan";

	public TimeSpanDataContract()
		: this(DictionaryGlobals.TimeSpanLocalName, DictionaryGlobals.SerializationNamespace)
	{
	}

	internal TimeSpanDataContract(XmlDictionaryString name, XmlDictionaryString ns)
		: base(typeof(TimeSpan), name, ns)
	{
	}

	[RequiresDynamicCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.")]
	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	internal override void WriteXmlValue(XmlWriterDelegator writer, object obj, XmlObjectSerializerWriteContext context)
	{
		writer.WriteTimeSpan((TimeSpan)obj);
	}

	[RequiresDynamicCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.")]
	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	internal override object ReadXmlValue(XmlReaderDelegator reader, XmlObjectSerializerReadContext context)
	{
		if (context != null)
		{
			return PrimitiveDataContract.HandleReadValue(reader.ReadElementContentAsTimeSpan(), context);
		}
		return reader.ReadElementContentAsTimeSpan();
	}

	[RequiresDynamicCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.")]
	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	internal override void WriteXmlElement(XmlWriterDelegator writer, object obj, XmlObjectSerializerWriteContext context, XmlDictionaryString name, XmlDictionaryString ns)
	{
		writer.WriteTimeSpan((TimeSpan)obj, name, ns);
	}
}
