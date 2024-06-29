using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization.DataContracts;
using System.Xml;
using System.Xml.Serialization;

namespace System.Runtime.Serialization;

internal class XmlObjectSerializerWriteContext : XmlObjectSerializerContext
{
	private ObjectReferenceStack _byValObjectsInScope;

	private XmlSerializableWriter _xmlSerializableWriter;

	private const int depthToCheckCyclicReference = 512;

	private ObjectToIdCache _serializedObjects;

	private bool _isGetOnlyCollection;

	private readonly bool _unsafeTypeForwardingEnabled;

	protected bool serializeReadOnlyTypes;

	protected bool preserveObjectReferences;

	protected ObjectToIdCache SerializedObjects => _serializedObjects ?? (_serializedObjects = new ObjectToIdCache());

	internal override bool IsGetOnlyCollection
	{
		get
		{
			return _isGetOnlyCollection;
		}
		set
		{
			_isGetOnlyCollection = value;
		}
	}

	internal bool SerializeReadOnlyTypes => serializeReadOnlyTypes;

	internal bool UnsafeTypeForwardingEnabled => _unsafeTypeForwardingEnabled;

	internal static XmlObjectSerializerWriteContext CreateContext(DataContractSerializer serializer, DataContract rootTypeDataContract, DataContractResolver dataContractResolver)
	{
		if (!serializer.PreserveObjectReferences && serializer.SerializationSurrogateProvider == null)
		{
			return new XmlObjectSerializerWriteContext(serializer, rootTypeDataContract, dataContractResolver);
		}
		return new XmlObjectSerializerWriteContextComplex(serializer, rootTypeDataContract, dataContractResolver);
	}

	protected XmlObjectSerializerWriteContext(DataContractSerializer serializer, DataContract rootTypeDataContract, DataContractResolver resolver)
		: base(serializer, rootTypeDataContract, resolver)
	{
		serializeReadOnlyTypes = serializer.SerializeReadOnlyTypes;
		_unsafeTypeForwardingEnabled = true;
	}

	internal XmlObjectSerializerWriteContext(XmlObjectSerializer serializer, int maxItemsInObjectGraph, StreamingContext streamingContext, bool ignoreExtensionDataObject)
		: base(serializer, maxItemsInObjectGraph, streamingContext, ignoreExtensionDataObject)
	{
		_unsafeTypeForwardingEnabled = true;
	}

	internal void StoreIsGetOnlyCollection()
	{
		_isGetOnlyCollection = true;
	}

	internal void ResetIsGetOnlyCollection()
	{
		_isGetOnlyCollection = false;
	}

	[RequiresDynamicCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.")]
	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	internal void InternalSerializeReference(XmlWriterDelegator xmlWriter, object obj, bool isDeclaredType, bool writeXsiType, int declaredTypeID, RuntimeTypeHandle declaredTypeHandle)
	{
		if (!OnHandleReference(xmlWriter, obj, canContainCyclicReference: true))
		{
			InternalSerialize(xmlWriter, obj, isDeclaredType, writeXsiType, declaredTypeID, declaredTypeHandle);
		}
		OnEndHandleReference(xmlWriter, obj, canContainCyclicReference: true);
	}

	[RequiresDynamicCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.")]
	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	internal virtual void InternalSerialize(XmlWriterDelegator xmlWriter, object obj, bool isDeclaredType, bool writeXsiType, int declaredTypeID, RuntimeTypeHandle declaredTypeHandle)
	{
		if (writeXsiType)
		{
			Type typeOfObject = Globals.TypeOfObject;
			SerializeWithXsiType(xmlWriter, obj, obj.GetType().TypeHandle, null, -1, typeOfObject.TypeHandle, typeOfObject);
			return;
		}
		if (isDeclaredType)
		{
			DataContract dataContract = GetDataContract(declaredTypeID, declaredTypeHandle);
			SerializeWithoutXsiType(dataContract, xmlWriter, obj, declaredTypeHandle);
			return;
		}
		RuntimeTypeHandle typeHandle = obj.GetType().TypeHandle;
		if (declaredTypeHandle.GetHashCode() == typeHandle.GetHashCode())
		{
			DataContract dataContract2 = ((declaredTypeID >= 0) ? GetDataContract(declaredTypeID, declaredTypeHandle) : GetDataContract(declaredTypeHandle, null));
			SerializeWithoutXsiType(dataContract2, xmlWriter, obj, declaredTypeHandle);
		}
		else
		{
			SerializeWithXsiType(xmlWriter, obj, typeHandle, null, declaredTypeID, declaredTypeHandle, Type.GetTypeFromHandle(declaredTypeHandle));
		}
	}

	[RequiresDynamicCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.")]
	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	internal void SerializeWithoutXsiType(DataContract dataContract, XmlWriterDelegator xmlWriter, object obj, RuntimeTypeHandle declaredTypeHandle)
	{
		if (!OnHandleIsReference(xmlWriter, dataContract, obj))
		{
			Dictionary<XmlQualifiedName, DataContract>? knownDataContracts = dataContract.KnownDataContracts;
			if (knownDataContracts != null && knownDataContracts.Count > 0)
			{
				scopedKnownTypes.Push(dataContract.KnownDataContracts);
				WriteDataContractValue(dataContract, xmlWriter, obj, declaredTypeHandle);
				scopedKnownTypes.Pop();
			}
			else
			{
				WriteDataContractValue(dataContract, xmlWriter, obj, declaredTypeHandle);
			}
		}
	}

	[RequiresDynamicCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.")]
	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	internal virtual void SerializeWithXsiTypeAtTopLevel(DataContract dataContract, XmlWriterDelegator xmlWriter, object obj, RuntimeTypeHandle originalDeclaredTypeHandle, Type graphType)
	{
		bool verifyKnownType = false;
		Type originalUnderlyingType = rootTypeDataContract.OriginalUnderlyingType;
		if (originalUnderlyingType.IsInterface && CollectionDataContract.IsCollectionInterface(originalUnderlyingType))
		{
			if (base.DataContractResolver != null)
			{
				WriteResolvedTypeInfo(xmlWriter, graphType, originalUnderlyingType);
			}
		}
		else if (!originalUnderlyingType.IsArray)
		{
			verifyKnownType = WriteTypeInfo(xmlWriter, dataContract, rootTypeDataContract);
		}
		SerializeAndVerifyType(dataContract, xmlWriter, obj, verifyKnownType, originalDeclaredTypeHandle, originalUnderlyingType);
	}

	[RequiresDynamicCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.")]
	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	protected virtual void SerializeWithXsiType(XmlWriterDelegator xmlWriter, object obj, RuntimeTypeHandle objectTypeHandle, Type objectType, int declaredTypeID, RuntimeTypeHandle declaredTypeHandle, Type declaredType)
	{
		bool verifyKnownType = false;
		DataContract dataContractSkipValidation;
		if (declaredType.IsInterface && CollectionDataContract.IsCollectionInterface(declaredType))
		{
			dataContractSkipValidation = GetDataContractSkipValidation(DataContract.GetId(objectTypeHandle), objectTypeHandle, objectType);
			if (OnHandleIsReference(xmlWriter, dataContractSkipValidation, obj))
			{
				return;
			}
			dataContractSkipValidation = GetDataContract(declaredTypeHandle, declaredType);
			if (!WriteClrTypeInfo(xmlWriter, dataContractSkipValidation) && base.DataContractResolver != null)
			{
				if ((object)objectType == null)
				{
					objectType = Type.GetTypeFromHandle(objectTypeHandle);
				}
				WriteResolvedTypeInfo(xmlWriter, objectType, declaredType);
			}
		}
		else if (declaredType.IsArray)
		{
			dataContractSkipValidation = GetDataContract(objectTypeHandle, objectType);
			WriteClrTypeInfo(xmlWriter, dataContractSkipValidation);
			dataContractSkipValidation = GetDataContract(declaredTypeHandle, declaredType);
		}
		else
		{
			dataContractSkipValidation = GetDataContract(objectTypeHandle, objectType);
			if (OnHandleIsReference(xmlWriter, dataContractSkipValidation, obj))
			{
				return;
			}
			if (!WriteClrTypeInfo(xmlWriter, dataContractSkipValidation))
			{
				DataContract declaredContract = ((declaredTypeID >= 0) ? GetDataContract(declaredTypeID, declaredTypeHandle) : GetDataContract(declaredTypeHandle, declaredType));
				verifyKnownType = WriteTypeInfo(xmlWriter, dataContractSkipValidation, declaredContract);
			}
		}
		SerializeAndVerifyType(dataContractSkipValidation, xmlWriter, obj, verifyKnownType, declaredTypeHandle, declaredType);
	}

	internal bool OnHandleIsReference(XmlWriterDelegator xmlWriter, DataContract contract, object obj)
	{
		if (preserveObjectReferences || !contract.IsReference || _isGetOnlyCollection)
		{
			return false;
		}
		bool newId = true;
		int id = SerializedObjects.GetId(obj, ref newId);
		_byValObjectsInScope.EnsureSetAsIsReference(obj);
		DefaultInterpolatedStringHandler handler;
		IFormatProvider invariantCulture;
		if (newId)
		{
			XmlDictionaryString idLocalName = DictionaryGlobals.IdLocalName;
			XmlDictionaryString serializationNamespace = DictionaryGlobals.SerializationNamespace;
			invariantCulture = CultureInfo.InvariantCulture;
			IFormatProvider provider = invariantCulture;
			handler = new DefaultInterpolatedStringHandler(1, 1, invariantCulture);
			handler.AppendLiteral("i");
			handler.AppendFormatted(id);
			xmlWriter.WriteAttributeString("z", idLocalName, serializationNamespace, string.Create(provider, ref handler));
			return false;
		}
		XmlDictionaryString refLocalName = DictionaryGlobals.RefLocalName;
		XmlDictionaryString serializationNamespace2 = DictionaryGlobals.SerializationNamespace;
		invariantCulture = CultureInfo.InvariantCulture;
		IFormatProvider provider2 = invariantCulture;
		handler = new DefaultInterpolatedStringHandler(1, 1, invariantCulture);
		handler.AppendLiteral("i");
		handler.AppendFormatted(id);
		xmlWriter.WriteAttributeString("z", refLocalName, serializationNamespace2, string.Create(provider2, ref handler));
		return true;
	}

	[RequiresDynamicCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.")]
	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	protected void SerializeAndVerifyType(DataContract dataContract, XmlWriterDelegator xmlWriter, object obj, bool verifyKnownType, RuntimeTypeHandle declaredTypeHandle, Type declaredType)
	{
		bool flag = false;
		Dictionary<XmlQualifiedName, DataContract>? knownDataContracts = dataContract.KnownDataContracts;
		if (knownDataContracts != null && knownDataContracts.Count > 0)
		{
			scopedKnownTypes.Push(dataContract.KnownDataContracts);
			flag = true;
		}
		if (verifyKnownType && !IsKnownType(dataContract, declaredType))
		{
			DataContract dataContract2 = ResolveDataContractFromKnownTypes(dataContract.XmlName.Name, dataContract.XmlName.Namespace, null, declaredType);
			if (dataContract2 == null || dataContract2.UnderlyingType != dataContract.UnderlyingType)
			{
				throw XmlObjectSerializer.CreateSerializationException(System.SR.Format(System.SR.DcTypeNotFoundOnSerialize, DataContract.GetClrTypeFullName(dataContract.UnderlyingType), dataContract.XmlName.Name, dataContract.XmlName.Namespace));
			}
		}
		WriteDataContractValue(dataContract, xmlWriter, obj, declaredTypeHandle);
		if (flag)
		{
			scopedKnownTypes.Pop();
		}
	}

	internal virtual bool WriteClrTypeInfo(XmlWriterDelegator xmlWriter, DataContract dataContract)
	{
		return false;
	}

	internal virtual bool WriteClrTypeInfo(XmlWriterDelegator xmlWriter, string clrTypeName, string clrAssemblyName)
	{
		return false;
	}

	internal virtual bool WriteClrTypeInfo(XmlWriterDelegator xmlWriter, Type dataContractType, string clrTypeName, string clrAssemblyName)
	{
		return false;
	}

	internal virtual bool WriteClrTypeInfo(XmlWriterDelegator xmlWriter, Type dataContractType, SerializationInfo serInfo)
	{
		return false;
	}

	internal virtual void WriteAnyType(XmlWriterDelegator xmlWriter, object value)
	{
		xmlWriter.WriteAnyType(value);
	}

	internal virtual void WriteString(XmlWriterDelegator xmlWriter, string value)
	{
		xmlWriter.WriteString(value);
	}

	[RequiresDynamicCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.")]
	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	internal virtual void WriteString(XmlWriterDelegator xmlWriter, string value, XmlDictionaryString name, XmlDictionaryString ns)
	{
		if (value == null)
		{
			WriteNull(xmlWriter, typeof(string), isMemberTypeSerializable: true, name, ns);
			return;
		}
		xmlWriter.WriteStartElementPrimitive(name, ns);
		xmlWriter.WriteString(value);
		xmlWriter.WriteEndElementPrimitive();
	}

	internal virtual void WriteBase64(XmlWriterDelegator xmlWriter, byte[] value)
	{
		xmlWriter.WriteBase64(value);
	}

	[RequiresDynamicCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.")]
	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	internal virtual void WriteBase64(XmlWriterDelegator xmlWriter, byte[] value, XmlDictionaryString name, XmlDictionaryString ns)
	{
		if (value == null)
		{
			WriteNull(xmlWriter, typeof(byte[]), isMemberTypeSerializable: true, name, ns);
			return;
		}
		xmlWriter.WriteStartElementPrimitive(name, ns);
		xmlWriter.WriteBase64(value);
		xmlWriter.WriteEndElementPrimitive();
	}

	internal virtual void WriteUri(XmlWriterDelegator xmlWriter, Uri value)
	{
		xmlWriter.WriteUri(value);
	}

	[RequiresDynamicCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.")]
	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	internal virtual void WriteUri(XmlWriterDelegator xmlWriter, Uri value, XmlDictionaryString name, XmlDictionaryString ns)
	{
		if (value == null)
		{
			WriteNull(xmlWriter, typeof(Uri), isMemberTypeSerializable: true, name, ns);
			return;
		}
		xmlWriter.WriteStartElementPrimitive(name, ns);
		xmlWriter.WriteUri(value);
		xmlWriter.WriteEndElementPrimitive();
	}

	internal virtual void WriteQName(XmlWriterDelegator xmlWriter, XmlQualifiedName value)
	{
		xmlWriter.WriteQName(value);
	}

	[RequiresDynamicCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.")]
	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	internal virtual void WriteQName(XmlWriterDelegator xmlWriter, XmlQualifiedName value, XmlDictionaryString name, XmlDictionaryString ns)
	{
		if (value == null)
		{
			WriteNull(xmlWriter, typeof(XmlQualifiedName), isMemberTypeSerializable: true, name, ns);
			return;
		}
		if (ns != null && ns.Value != null && ns.Value.Length > 0)
		{
			xmlWriter.WriteStartElement("q", name, ns);
		}
		else
		{
			xmlWriter.WriteStartElement(name, ns);
		}
		xmlWriter.WriteQName(value);
		xmlWriter.WriteEndElement();
	}

	internal void HandleGraphAtTopLevel(XmlWriterDelegator writer, object obj, DataContract contract)
	{
		writer.WriteXmlnsAttribute("i", DictionaryGlobals.SchemaInstanceNamespace);
		if (contract.IsISerializable)
		{
			writer.WriteXmlnsAttribute("x", DictionaryGlobals.SchemaNamespace);
		}
		OnHandleReference(writer, obj, canContainCyclicReference: true);
	}

	internal virtual bool OnHandleReference(XmlWriterDelegator xmlWriter, object obj, bool canContainCyclicReference)
	{
		if (xmlWriter.depth < 512)
		{
			return false;
		}
		if (canContainCyclicReference)
		{
			if (_byValObjectsInScope.Contains(obj))
			{
				throw XmlObjectSerializer.CreateSerializationException(System.SR.Format(System.SR.CannotSerializeObjectWithCycles, DataContract.GetClrTypeFullName(obj.GetType())));
			}
			_byValObjectsInScope.Push(obj);
		}
		return false;
	}

	internal virtual void OnEndHandleReference(XmlWriterDelegator xmlWriter, object obj, bool canContainCyclicReference)
	{
		if (xmlWriter.depth >= 512 && canContainCyclicReference)
		{
			_byValObjectsInScope.Pop(obj);
		}
	}

	[RequiresDynamicCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.")]
	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	internal void WriteNull(XmlWriterDelegator xmlWriter, Type memberType, bool isMemberTypeSerializable)
	{
		CheckIfTypeSerializable(memberType, isMemberTypeSerializable);
		WriteNull(xmlWriter);
	}

	[RequiresDynamicCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.")]
	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	internal void WriteNull(XmlWriterDelegator xmlWriter, Type memberType, bool isMemberTypeSerializable, XmlDictionaryString name, XmlDictionaryString ns)
	{
		xmlWriter.WriteStartElement(name, ns);
		WriteNull(xmlWriter, memberType, isMemberTypeSerializable);
		xmlWriter.WriteEndElement();
	}

	internal void IncrementArrayCount(XmlWriterDelegator xmlWriter, Array array)
	{
		IncrementCollectionCount(xmlWriter, array.GetLength(0));
	}

	internal void IncrementCollectionCount(XmlWriterDelegator xmlWriter, ICollection collection)
	{
		IncrementCollectionCount(xmlWriter, collection.Count);
	}

	internal void IncrementCollectionCountGeneric<T>(XmlWriterDelegator xmlWriter, ICollection<T> collection)
	{
		IncrementCollectionCount(xmlWriter, collection.Count);
	}

	private void IncrementCollectionCount(XmlWriterDelegator xmlWriter, int size)
	{
		IncrementItemCount(size);
		WriteArraySize(xmlWriter, size);
	}

	internal virtual void WriteArraySize(XmlWriterDelegator xmlWriter, int size)
	{
	}

	internal static bool IsMemberTypeSameAsMemberValue(object obj, Type memberType)
	{
		if (obj == null || memberType == null)
		{
			return false;
		}
		return obj.GetType().TypeHandle.Equals(memberType.TypeHandle);
	}

	internal static T GetDefaultValue<T>()
	{
		return default(T);
	}

	internal static T GetNullableValue<T>(T? value) where T : struct
	{
		return value.Value;
	}

	internal static void ThrowRequiredMemberMustBeEmitted(string memberName, Type type)
	{
		throw new SerializationException(System.SR.Format(System.SR.RequiredMemberMustBeEmitted, memberName, type.FullName));
	}

	internal static bool GetHasValue<T>(T? value) where T : struct
	{
		return value.HasValue;
	}

	internal void WriteIXmlSerializable(XmlWriterDelegator xmlWriter, object obj)
	{
		if (_xmlSerializableWriter == null)
		{
			_xmlSerializableWriter = new XmlSerializableWriter();
		}
		WriteIXmlSerializable(xmlWriter, obj, _xmlSerializableWriter);
	}

	internal static void WriteRootIXmlSerializable(XmlWriterDelegator xmlWriter, object obj)
	{
		WriteIXmlSerializable(xmlWriter, obj, new XmlSerializableWriter());
	}

	private static void WriteIXmlSerializable(XmlWriterDelegator xmlWriter, object obj, XmlSerializableWriter xmlSerializableWriter)
	{
		xmlSerializableWriter.BeginWrite(xmlWriter.Writer, obj);
		if (obj is IXmlSerializable xmlSerializable)
		{
			xmlSerializable.WriteXml(xmlSerializableWriter);
		}
		else if (obj is XmlElement xmlElement)
		{
			xmlElement.WriteTo(xmlSerializableWriter);
		}
		else
		{
			if (!(obj is XmlNode[] array))
			{
				throw XmlObjectSerializer.CreateSerializationException(System.SR.Format(System.SR.UnknownXmlType, DataContract.GetClrTypeFullName(obj.GetType())));
			}
			XmlNode[] array2 = array;
			foreach (XmlNode xmlNode in array2)
			{
				xmlNode.WriteTo(xmlSerializableWriter);
			}
		}
		xmlSerializableWriter.EndWrite();
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static void GetObjectData(ISerializable obj, SerializationInfo serInfo, StreamingContext context)
	{
		obj.GetObjectData(serInfo, context);
	}

	[RequiresDynamicCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.")]
	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	public void WriteISerializable(XmlWriterDelegator xmlWriter, ISerializable obj)
	{
		Type type = obj.GetType();
		SerializationInfo serInfo = new SerializationInfo(type, XmlObjectSerializer.FormatterConverter);
		GetObjectData(obj, serInfo, GetStreamingContext());
		WriteSerializationInfo(xmlWriter, type, serInfo);
	}

	[RequiresDynamicCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.")]
	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	internal void WriteSerializationInfo(XmlWriterDelegator xmlWriter, Type objType, SerializationInfo serInfo)
	{
		if (DataContract.GetClrTypeFullName(objType) != serInfo.FullTypeName)
		{
			if (base.DataContractResolver != null)
			{
				if (ResolveType(serInfo.ObjectType, objType, out var typeName, out var typeNamespace))
				{
					xmlWriter.WriteAttributeQualifiedName("z", DictionaryGlobals.ISerializableFactoryTypeLocalName, DictionaryGlobals.SerializationNamespace, typeName, typeNamespace);
				}
			}
			else
			{
				DataContract.GetDefaultXmlName(serInfo.FullTypeName, out var localName, out var ns);
				xmlWriter.WriteAttributeQualifiedName("z", DictionaryGlobals.ISerializableFactoryTypeLocalName, DictionaryGlobals.SerializationNamespace, DataContract.GetClrTypeString(localName), DataContract.GetClrTypeString(ns));
			}
		}
		WriteClrTypeInfo(xmlWriter, objType, serInfo);
		IncrementItemCount(serInfo.MemberCount);
		SerializationInfoEnumerator enumerator = serInfo.GetEnumerator();
		while (enumerator.MoveNext())
		{
			SerializationEntry current = enumerator.Current;
			XmlDictionaryString clrTypeString = DataContract.GetClrTypeString(DataContract.EncodeLocalName(current.Name));
			xmlWriter.WriteStartElement(clrTypeString, DictionaryGlobals.EmptyString);
			object value = current.Value;
			if (value == null)
			{
				WriteNull(xmlWriter);
			}
			else
			{
				InternalSerializeReference(xmlWriter, value, isDeclaredType: false, writeXsiType: false, -1, Globals.TypeOfObject.TypeHandle);
			}
			xmlWriter.WriteEndElement();
		}
	}

	[RequiresDynamicCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.")]
	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	protected virtual void WriteDataContractValue(DataContract dataContract, XmlWriterDelegator xmlWriter, object obj, RuntimeTypeHandle declaredTypeHandle)
	{
		dataContract.WriteXmlValue(xmlWriter, obj, this);
	}

	protected virtual void WriteNull(XmlWriterDelegator xmlWriter)
	{
		XmlObjectSerializer.WriteNull(xmlWriter);
	}

	[RequiresDynamicCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.")]
	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	private void WriteResolvedTypeInfo(XmlWriterDelegator writer, Type objectType, Type declaredType)
	{
		if (ResolveType(objectType, declaredType, out var typeName, out var typeNamespace))
		{
			WriteTypeInfo(writer, typeName, typeNamespace);
		}
	}

	[RequiresDynamicCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.")]
	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	private bool ResolveType(Type objectType, Type declaredType, [NotNullWhen(true)] out XmlDictionaryString typeName, [NotNullWhen(true)] out XmlDictionaryString typeNamespace)
	{
		if (!base.DataContractResolver.TryResolveType(objectType, declaredType, base.KnownTypeResolver, out typeName, out typeNamespace))
		{
			throw XmlObjectSerializer.CreateSerializationException(System.SR.Format(System.SR.ResolveTypeReturnedFalse, DataContract.GetClrTypeFullName(base.DataContractResolver.GetType()), DataContract.GetClrTypeFullName(objectType)));
		}
		if (typeName == null)
		{
			if (typeNamespace == null)
			{
				return false;
			}
			throw XmlObjectSerializer.CreateSerializationException(System.SR.Format(System.SR.ResolveTypeReturnedNull, DataContract.GetClrTypeFullName(base.DataContractResolver.GetType()), DataContract.GetClrTypeFullName(objectType)));
		}
		if (typeNamespace == null)
		{
			throw XmlObjectSerializer.CreateSerializationException(System.SR.Format(System.SR.ResolveTypeReturnedNull, DataContract.GetClrTypeFullName(base.DataContractResolver.GetType()), DataContract.GetClrTypeFullName(objectType)));
		}
		return true;
	}

	[RequiresDynamicCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.")]
	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	protected virtual bool WriteTypeInfo(XmlWriterDelegator writer, DataContract contract, DataContract declaredContract)
	{
		if (!XmlObjectSerializer.IsContractDeclared(contract, declaredContract))
		{
			if (base.DataContractResolver == null)
			{
				WriteTypeInfo(writer, contract.Name, contract.Namespace);
				return true;
			}
			WriteResolvedTypeInfo(writer, contract.OriginalUnderlyingType, declaredContract.OriginalUnderlyingType);
			return false;
		}
		return false;
	}

	protected virtual void WriteTypeInfo(XmlWriterDelegator writer, string dataContractName, string dataContractNamespace)
	{
		writer.WriteAttributeQualifiedName("i", DictionaryGlobals.XsiTypeLocalName, DictionaryGlobals.SchemaInstanceNamespace, dataContractName, dataContractNamespace);
	}

	protected virtual void WriteTypeInfo(XmlWriterDelegator writer, XmlDictionaryString dataContractName, XmlDictionaryString dataContractNamespace)
	{
		writer.WriteAttributeQualifiedName("i", DictionaryGlobals.XsiTypeLocalName, DictionaryGlobals.SchemaInstanceNamespace, dataContractName, dataContractNamespace);
	}

	[RequiresDynamicCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.")]
	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	public void WriteExtensionData(XmlWriterDelegator xmlWriter, ExtensionDataObject extensionData, int memberIndex)
	{
		if (base.IgnoreExtensionDataObject || extensionData == null)
		{
			return;
		}
		IList<ExtensionDataMember> members = extensionData.Members;
		if (members == null)
		{
			return;
		}
		for (int i = 0; i < members.Count; i++)
		{
			ExtensionDataMember extensionDataMember = members[i];
			if (extensionDataMember.MemberIndex == memberIndex)
			{
				WriteExtensionDataMember(xmlWriter, extensionDataMember);
			}
		}
	}

	[RequiresDynamicCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.")]
	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	private void WriteExtensionDataMember(XmlWriterDelegator xmlWriter, ExtensionDataMember member)
	{
		xmlWriter.WriteStartElement(member.Name, member.Namespace);
		IDataNode value = member.Value;
		WriteExtensionDataValue(xmlWriter, value);
		xmlWriter.WriteEndElement();
	}

	[RequiresDynamicCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.")]
	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	internal virtual void WriteExtensionDataTypeInfo(XmlWriterDelegator xmlWriter, IDataNode dataNode)
	{
		if (dataNode.DataContractName != null)
		{
			WriteTypeInfo(xmlWriter, dataNode.DataContractName, dataNode.DataContractNamespace);
		}
		WriteClrTypeInfo(xmlWriter, dataNode.DataType, dataNode.ClrTypeName, dataNode.ClrAssemblyName);
	}

	[RequiresDynamicCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.")]
	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	internal void WriteExtensionDataValue(XmlWriterDelegator xmlWriter, IDataNode dataNode)
	{
		IncrementItemCount(1);
		if (dataNode == null)
		{
			WriteNull(xmlWriter);
		}
		else
		{
			if (dataNode.PreservesReferences && OnHandleReference(xmlWriter, dataNode.Value ?? dataNode, canContainCyclicReference: true))
			{
				return;
			}
			Type dataType = dataNode.DataType;
			if (dataType == Globals.TypeOfClassDataNode)
			{
				WriteExtensionClassData(xmlWriter, (ClassDataNode)dataNode);
			}
			else if (dataType == Globals.TypeOfCollectionDataNode)
			{
				WriteExtensionCollectionData(xmlWriter, (CollectionDataNode)dataNode);
			}
			else if (dataType == Globals.TypeOfXmlDataNode)
			{
				WriteExtensionXmlData(xmlWriter, (XmlDataNode)dataNode);
			}
			else if (dataType == Globals.TypeOfISerializableDataNode)
			{
				WriteExtensionISerializableData(xmlWriter, (ISerializableDataNode)dataNode);
			}
			else
			{
				WriteExtensionDataTypeInfo(xmlWriter, dataNode);
				if (dataType == Globals.TypeOfObject)
				{
					object value = dataNode.Value;
					if (value != null)
					{
						InternalSerialize(xmlWriter, value, isDeclaredType: false, writeXsiType: false, -1, value.GetType().TypeHandle);
					}
				}
				else
				{
					xmlWriter.WriteExtensionData(dataNode);
				}
			}
			if (dataNode.PreservesReferences)
			{
				OnEndHandleReference(xmlWriter, dataNode.Value ?? dataNode, canContainCyclicReference: true);
			}
		}
	}

	[RequiresDynamicCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.")]
	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	internal bool TryWriteDeserializedExtensionData(XmlWriterDelegator xmlWriter, IDataNode dataNode)
	{
		object value = dataNode.Value;
		if (value == null)
		{
			return false;
		}
		Type type = ((dataNode.DataContractName == null) ? value.GetType() : Globals.TypeOfObject);
		InternalSerialize(xmlWriter, value, isDeclaredType: false, writeXsiType: false, -1, type.TypeHandle);
		return true;
	}

	[RequiresDynamicCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.")]
	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	private void WriteExtensionClassData(XmlWriterDelegator xmlWriter, ClassDataNode dataNode)
	{
		if (TryWriteDeserializedExtensionData(xmlWriter, dataNode))
		{
			return;
		}
		WriteExtensionDataTypeInfo(xmlWriter, dataNode);
		IList<ExtensionDataMember> members = dataNode.Members;
		if (members != null)
		{
			for (int i = 0; i < members.Count; i++)
			{
				WriteExtensionDataMember(xmlWriter, members[i]);
			}
		}
	}

	[RequiresDynamicCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.")]
	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	private void WriteExtensionCollectionData(XmlWriterDelegator xmlWriter, CollectionDataNode dataNode)
	{
		if (TryWriteDeserializedExtensionData(xmlWriter, dataNode))
		{
			return;
		}
		WriteExtensionDataTypeInfo(xmlWriter, dataNode);
		WriteArraySize(xmlWriter, dataNode.Size);
		IList<IDataNode> items = dataNode.Items;
		if (items != null)
		{
			for (int i = 0; i < items.Count; i++)
			{
				xmlWriter.WriteStartElement(dataNode.ItemName, dataNode.ItemNamespace);
				WriteExtensionDataValue(xmlWriter, items[i]);
				xmlWriter.WriteEndElement();
			}
		}
	}

	[RequiresDynamicCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.")]
	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	private void WriteExtensionISerializableData(XmlWriterDelegator xmlWriter, ISerializableDataNode dataNode)
	{
		if (TryWriteDeserializedExtensionData(xmlWriter, dataNode))
		{
			return;
		}
		WriteExtensionDataTypeInfo(xmlWriter, dataNode);
		if (dataNode.FactoryTypeName != null)
		{
			xmlWriter.WriteAttributeQualifiedName("z", DictionaryGlobals.ISerializableFactoryTypeLocalName, DictionaryGlobals.SerializationNamespace, dataNode.FactoryTypeName, dataNode.FactoryTypeNamespace);
		}
		IList<ISerializableDataMember> members = dataNode.Members;
		if (members != null)
		{
			for (int i = 0; i < members.Count; i++)
			{
				ISerializableDataMember serializableDataMember = members[i];
				xmlWriter.WriteStartElement(serializableDataMember.Name, string.Empty);
				WriteExtensionDataValue(xmlWriter, serializableDataMember.Value);
				xmlWriter.WriteEndElement();
			}
		}
	}

	[RequiresDynamicCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.")]
	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	private void WriteExtensionXmlData(XmlWriterDelegator xmlWriter, XmlDataNode dataNode)
	{
		if (TryWriteDeserializedExtensionData(xmlWriter, dataNode))
		{
			return;
		}
		IList<XmlAttribute> xmlAttributes = dataNode.XmlAttributes;
		if (xmlAttributes != null)
		{
			foreach (XmlAttribute item in xmlAttributes)
			{
				item.WriteTo(xmlWriter.Writer);
			}
		}
		WriteExtensionDataTypeInfo(xmlWriter, dataNode);
		IList<XmlNode> xmlChildNodes = dataNode.XmlChildNodes;
		if (xmlChildNodes == null)
		{
			return;
		}
		foreach (XmlNode item2 in xmlChildNodes)
		{
			item2.WriteTo(xmlWriter.Writer);
		}
	}
}
