using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization.DataContracts;
using System.Text;
using System.Xml;

namespace System.Runtime.Serialization;

public abstract class XmlObjectSerializer
{
	private static IFormatterConverter s_formatterConverter;

	internal virtual Dictionary<XmlQualifiedName, DataContract>? KnownDataContracts
	{
		[RequiresDynamicCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.")]
		[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
		get
		{
			return null;
		}
	}

	internal static IFormatterConverter FormatterConverter => s_formatterConverter ?? (s_formatterConverter = new FormatterConverter());

	[RequiresDynamicCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.")]
	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	public abstract void WriteStartObject(XmlDictionaryWriter writer, object? graph);

	[RequiresDynamicCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.")]
	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	public abstract void WriteObjectContent(XmlDictionaryWriter writer, object? graph);

	[RequiresDynamicCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.")]
	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	public abstract void WriteEndObject(XmlDictionaryWriter writer);

	[RequiresDynamicCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.")]
	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	public virtual void WriteObject(Stream stream, object? graph)
	{
		ArgumentNullException.ThrowIfNull(stream, "stream");
		XmlDictionaryWriter xmlDictionaryWriter = XmlDictionaryWriter.CreateTextWriter(stream, Encoding.UTF8, ownsStream: false);
		WriteObject(xmlDictionaryWriter, graph);
		xmlDictionaryWriter.Flush();
	}

	[RequiresDynamicCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.")]
	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	public virtual void WriteObject(XmlWriter writer, object? graph)
	{
		ArgumentNullException.ThrowIfNull(writer, "writer");
		WriteObject(XmlDictionaryWriter.CreateDictionaryWriter(writer), graph);
	}

	[RequiresDynamicCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.")]
	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	public virtual void WriteStartObject(XmlWriter writer, object? graph)
	{
		ArgumentNullException.ThrowIfNull(writer, "writer");
		WriteStartObject(XmlDictionaryWriter.CreateDictionaryWriter(writer), graph);
	}

	[RequiresDynamicCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.")]
	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	public virtual void WriteObjectContent(XmlWriter writer, object? graph)
	{
		ArgumentNullException.ThrowIfNull(writer, "writer");
		WriteObjectContent(XmlDictionaryWriter.CreateDictionaryWriter(writer), graph);
	}

	[RequiresDynamicCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.")]
	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	public virtual void WriteEndObject(XmlWriter writer)
	{
		ArgumentNullException.ThrowIfNull(writer, "writer");
		WriteEndObject(XmlDictionaryWriter.CreateDictionaryWriter(writer));
	}

	[RequiresDynamicCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.")]
	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	public virtual void WriteObject(XmlDictionaryWriter writer, object? graph)
	{
		WriteObjectHandleExceptions(new XmlWriterDelegator(writer), graph);
	}

	[RequiresDynamicCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.")]
	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	internal void WriteObjectHandleExceptions(XmlWriterDelegator writer, object graph)
	{
		WriteObjectHandleExceptions(writer, graph, null);
	}

	[RequiresDynamicCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.")]
	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	internal void WriteObjectHandleExceptions(XmlWriterDelegator writer, object graph, DataContractResolver dataContractResolver)
	{
		ArgumentNullException.ThrowIfNull(writer, "writer");
		try
		{
			InternalWriteObject(writer, graph, dataContractResolver);
		}
		catch (XmlException innerException)
		{
			throw CreateSerializationException(GetTypeInfoError(System.SR.ErrorSerializing, GetSerializeType(graph), innerException), innerException);
		}
		catch (FormatException innerException2)
		{
			throw CreateSerializationException(GetTypeInfoError(System.SR.ErrorSerializing, GetSerializeType(graph), innerException2), innerException2);
		}
	}

	[RequiresDynamicCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.")]
	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	internal virtual void InternalWriteObject(XmlWriterDelegator writer, object graph)
	{
		WriteStartObject(writer.Writer, graph);
		WriteObjectContent(writer.Writer, graph);
		WriteEndObject(writer.Writer);
	}

	[RequiresDynamicCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.")]
	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	internal virtual void InternalWriteObject(XmlWriterDelegator writer, object graph, DataContractResolver dataContractResolver)
	{
		InternalWriteObject(writer, graph);
	}

	[RequiresDynamicCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.")]
	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	internal virtual void InternalWriteStartObject(XmlWriterDelegator writer, object graph)
	{
		throw new NotSupportedException();
	}

	[RequiresDynamicCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.")]
	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	internal virtual void InternalWriteObjectContent(XmlWriterDelegator writer, object graph)
	{
		throw new NotSupportedException();
	}

	[RequiresDynamicCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.")]
	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	internal virtual void InternalWriteEndObject(XmlWriterDelegator writer)
	{
		throw new NotSupportedException();
	}

	[RequiresDynamicCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.")]
	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	internal void WriteStartObjectHandleExceptions(XmlWriterDelegator writer, object graph)
	{
		ArgumentNullException.ThrowIfNull(writer, "writer");
		try
		{
			InternalWriteStartObject(writer, graph);
		}
		catch (XmlException innerException)
		{
			throw CreateSerializationException(GetTypeInfoError(System.SR.ErrorWriteStartObject, GetSerializeType(graph), innerException), innerException);
		}
		catch (FormatException innerException2)
		{
			throw CreateSerializationException(GetTypeInfoError(System.SR.ErrorWriteStartObject, GetSerializeType(graph), innerException2), innerException2);
		}
	}

	[RequiresDynamicCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.")]
	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	internal void WriteObjectContentHandleExceptions(XmlWriterDelegator writer, object graph)
	{
		ArgumentNullException.ThrowIfNull(writer, "writer");
		try
		{
			if (writer.WriteState != WriteState.Element)
			{
				throw CreateSerializationException(System.SR.Format(System.SR.XmlWriterMustBeInElement, writer.WriteState));
			}
			InternalWriteObjectContent(writer, graph);
		}
		catch (XmlException innerException)
		{
			throw CreateSerializationException(GetTypeInfoError(System.SR.ErrorSerializing, GetSerializeType(graph), innerException), innerException);
		}
		catch (FormatException innerException2)
		{
			throw CreateSerializationException(GetTypeInfoError(System.SR.ErrorSerializing, GetSerializeType(graph), innerException2), innerException2);
		}
	}

	[RequiresDynamicCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.")]
	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	internal void WriteEndObjectHandleExceptions(XmlWriterDelegator writer)
	{
		ArgumentNullException.ThrowIfNull(writer, "writer");
		try
		{
			InternalWriteEndObject(writer);
		}
		catch (XmlException innerException)
		{
			throw CreateSerializationException(GetTypeInfoError(System.SR.ErrorWriteEndObject, null, innerException), innerException);
		}
		catch (FormatException innerException2)
		{
			throw CreateSerializationException(GetTypeInfoError(System.SR.ErrorWriteEndObject, null, innerException2), innerException2);
		}
	}

	internal static void WriteRootElement(XmlWriterDelegator writer, DataContract contract, XmlDictionaryString name, XmlDictionaryString ns, bool needsContractNsAtRoot)
	{
		if (name == null)
		{
			if (contract.HasRoot)
			{
				contract.WriteRootElement(writer, contract.TopLevelElementName, contract.TopLevelElementNamespace);
			}
			return;
		}
		contract.WriteRootElement(writer, name, ns);
		if (needsContractNsAtRoot)
		{
			writer.WriteNamespaceDecl(contract.Namespace);
		}
	}

	internal static bool CheckIfNeedsContractNsAtRoot(XmlDictionaryString name, XmlDictionaryString ns, DataContract contract)
	{
		if (name == null)
		{
			return false;
		}
		if (contract.IsBuiltInDataContract || !contract.CanContainReferences || contract.IsISerializable)
		{
			return false;
		}
		string @string = XmlDictionaryString.GetString(contract.Namespace);
		if (string.IsNullOrEmpty(@string) || @string == XmlDictionaryString.GetString(ns))
		{
			return false;
		}
		return true;
	}

	internal static void WriteNull(XmlWriterDelegator writer)
	{
		writer.WriteAttributeBool("i", DictionaryGlobals.XsiNilLocalName, DictionaryGlobals.SchemaInstanceNamespace, value: true);
	}

	internal static bool IsContractDeclared(DataContract contract, DataContract declaredContract)
	{
		if (contract.Name != declaredContract.Name || contract.Namespace != declaredContract.Namespace)
		{
			if (contract.Name.Value == declaredContract.Name.Value)
			{
				return contract.Namespace.Value == declaredContract.Namespace.Value;
			}
			return false;
		}
		return true;
	}

	[RequiresDynamicCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.")]
	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	public virtual object? ReadObject(Stream stream)
	{
		ArgumentNullException.ThrowIfNull(stream, "stream");
		return ReadObject(XmlDictionaryReader.CreateTextReader(stream, XmlDictionaryReaderQuotas.Max));
	}

	[RequiresDynamicCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.")]
	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	public virtual object? ReadObject(XmlReader reader)
	{
		ArgumentNullException.ThrowIfNull(reader, "reader");
		return ReadObject(XmlDictionaryReader.CreateDictionaryReader(reader));
	}

	[RequiresDynamicCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.")]
	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	public virtual object? ReadObject(XmlDictionaryReader reader)
	{
		return ReadObjectHandleExceptions(new XmlReaderDelegator(reader), verifyObjectName: true);
	}

	[RequiresDynamicCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.")]
	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	public virtual object? ReadObject(XmlReader reader, bool verifyObjectName)
	{
		ArgumentNullException.ThrowIfNull(reader, "reader");
		return ReadObject(XmlDictionaryReader.CreateDictionaryReader(reader), verifyObjectName);
	}

	[RequiresDynamicCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.")]
	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	public abstract object? ReadObject(XmlDictionaryReader reader, bool verifyObjectName);

	[RequiresDynamicCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.")]
	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	public virtual bool IsStartObject(XmlReader reader)
	{
		ArgumentNullException.ThrowIfNull(reader, "reader");
		return IsStartObject(XmlDictionaryReader.CreateDictionaryReader(reader));
	}

	[RequiresDynamicCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.")]
	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	public abstract bool IsStartObject(XmlDictionaryReader reader);

	[RequiresDynamicCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.")]
	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	internal virtual object InternalReadObject(XmlReaderDelegator reader, bool verifyObjectName)
	{
		return ReadObject(reader.UnderlyingReader, verifyObjectName);
	}

	[RequiresDynamicCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.")]
	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	internal virtual object InternalReadObject(XmlReaderDelegator reader, bool verifyObjectName, DataContractResolver dataContractResolver)
	{
		return InternalReadObject(reader, verifyObjectName);
	}

	[RequiresDynamicCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.")]
	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	internal virtual bool InternalIsStartObject(XmlReaderDelegator reader)
	{
		throw new NotSupportedException();
	}

	[RequiresDynamicCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.")]
	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	internal object ReadObjectHandleExceptions(XmlReaderDelegator reader, bool verifyObjectName)
	{
		return ReadObjectHandleExceptions(reader, verifyObjectName, null);
	}

	[RequiresDynamicCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.")]
	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	internal object ReadObjectHandleExceptions(XmlReaderDelegator reader, bool verifyObjectName, DataContractResolver dataContractResolver)
	{
		ArgumentNullException.ThrowIfNull(reader, "reader");
		try
		{
			return InternalReadObject(reader, verifyObjectName, dataContractResolver);
		}
		catch (XmlException innerException)
		{
			throw CreateSerializationException(GetTypeInfoError(System.SR.ErrorDeserializing, GetDeserializeType(), innerException), innerException);
		}
		catch (FormatException innerException2)
		{
			throw CreateSerializationException(GetTypeInfoError(System.SR.ErrorDeserializing, GetDeserializeType(), innerException2), innerException2);
		}
	}

	[RequiresDynamicCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.")]
	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	internal bool IsStartObjectHandleExceptions(XmlReaderDelegator reader)
	{
		ArgumentNullException.ThrowIfNull(reader, "reader");
		try
		{
			return InternalIsStartObject(reader);
		}
		catch (XmlException innerException)
		{
			throw CreateSerializationException(GetTypeInfoError(System.SR.ErrorIsStartObject, GetDeserializeType(), innerException), innerException);
		}
		catch (FormatException innerException2)
		{
			throw CreateSerializationException(GetTypeInfoError(System.SR.ErrorIsStartObject, GetDeserializeType(), innerException2), innerException2);
		}
	}

	internal static bool IsRootXmlAny(XmlDictionaryString rootName, DataContract contract)
	{
		if (rootName == null)
		{
			return !contract.HasRoot;
		}
		return false;
	}

	internal static bool IsStartElement(XmlReaderDelegator reader)
	{
		if (!reader.MoveToElement())
		{
			return reader.IsStartElement();
		}
		return true;
	}

	[RequiresDynamicCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.")]
	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	internal static bool IsRootElement(XmlReaderDelegator reader, DataContract contract, XmlDictionaryString name, XmlDictionaryString ns)
	{
		reader.MoveToElement();
		if (name != null)
		{
			return reader.IsStartElement(name, ns ?? XmlDictionaryString.Empty);
		}
		if (!contract.HasRoot)
		{
			return reader.IsStartElement();
		}
		if (reader.IsStartElement(contract.TopLevelElementName, contract.TopLevelElementNamespace))
		{
			return true;
		}
		ClassDataContract classDataContract = contract as ClassDataContract;
		if (classDataContract != null)
		{
			classDataContract = classDataContract.BaseClassContract;
		}
		while (classDataContract != null)
		{
			if (reader.IsStartElement(classDataContract.TopLevelElementName, classDataContract.TopLevelElementNamespace))
			{
				return true;
			}
			classDataContract = classDataContract.BaseClassContract;
		}
		if (classDataContract == null)
		{
			DataContract primitiveDataContract = PrimitiveDataContract.GetPrimitiveDataContract(Globals.TypeOfObject);
			if (reader.IsStartElement(primitiveDataContract.TopLevelElementName, primitiveDataContract.TopLevelElementNamespace))
			{
				return true;
			}
		}
		return false;
	}

	internal static string TryAddLineInfo(XmlReaderDelegator reader, string errorMessage)
	{
		if (reader.HasLineInfo())
		{
			IFormatProvider invariantCulture = CultureInfo.InvariantCulture;
			DefaultInterpolatedStringHandler handler = new DefaultInterpolatedStringHandler(1, 2, invariantCulture);
			handler.AppendFormatted(System.SR.Format(System.SR.ErrorInLine, reader.LineNumber, reader.LinePosition));
			handler.AppendLiteral(" ");
			handler.AppendFormatted(errorMessage);
			return string.Create(invariantCulture, ref handler);
		}
		return errorMessage;
	}

	internal static Exception CreateSerializationExceptionWithReaderDetails(string errorMessage, XmlReaderDelegator reader)
	{
		return CreateSerializationException(TryAddLineInfo(reader, System.SR.Format(System.SR.EncounteredWithNameNamespace, errorMessage, reader.NodeType, reader.LocalName, reader.NamespaceURI)));
	}

	internal static SerializationException CreateSerializationException(string errorMessage)
	{
		return CreateSerializationException(errorMessage, null);
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static SerializationException CreateSerializationException(string errorMessage, Exception innerException)
	{
		return new SerializationException(errorMessage, innerException);
	}

	internal static string GetTypeInfoError(string errorMessage, Type type, Exception innerException)
	{
		string p = ((type == null) ? string.Empty : System.SR.Format(System.SR.ErrorTypeInfo, DataContract.GetClrTypeFullName(type)));
		string p2 = ((innerException == null) ? string.Empty : innerException.Message);
		return System.SR.Format(errorMessage, p, p2);
	}

	internal virtual Type GetSerializeType(object graph)
	{
		return graph?.GetType();
	}

	internal virtual Type GetDeserializeType()
	{
		return null;
	}
}
