using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization.DataContracts;

namespace System.Runtime.Serialization;

internal class XmlObjectSerializerReadContextComplex : XmlObjectSerializerReadContext
{
	private readonly bool _preserveObjectReferences;

	private readonly ISerializationSurrogateProvider _serializationSurrogateProvider;

	internal XmlObjectSerializerReadContextComplex(DataContractSerializer serializer, DataContract rootTypeDataContract, DataContractResolver dataContractResolver)
		: base(serializer, rootTypeDataContract, dataContractResolver)
	{
		_preserveObjectReferences = serializer.PreserveObjectReferences;
		_serializationSurrogateProvider = serializer.SerializationSurrogateProvider;
	}

	internal XmlObjectSerializerReadContextComplex(XmlObjectSerializer serializer, int maxItemsInObjectGraph, StreamingContext streamingContext, bool ignoreExtensionDataObject)
		: base(serializer, maxItemsInObjectGraph, streamingContext, ignoreExtensionDataObject)
	{
	}

	[RequiresDynamicCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.")]
	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	internal override object InternalDeserialize(XmlReaderDelegator xmlReader, int declaredTypeID, RuntimeTypeHandle declaredTypeHandle, string name, string ns)
	{
		if (_serializationSurrogateProvider == null)
		{
			return base.InternalDeserialize(xmlReader, declaredTypeID, declaredTypeHandle, name, ns);
		}
		return InternalDeserializeWithSurrogate(xmlReader, Type.GetTypeFromHandle(declaredTypeHandle), null, name, ns);
	}

	[RequiresDynamicCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.")]
	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	internal override object InternalDeserialize(XmlReaderDelegator xmlReader, Type declaredType, string name, string ns)
	{
		if (_serializationSurrogateProvider == null)
		{
			return base.InternalDeserialize(xmlReader, declaredType, name, ns);
		}
		return InternalDeserializeWithSurrogate(xmlReader, declaredType, null, name, ns);
	}

	[RequiresDynamicCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.")]
	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	internal override object InternalDeserialize(XmlReaderDelegator xmlReader, Type declaredType, DataContract dataContract, string name, string ns)
	{
		if (_serializationSurrogateProvider == null)
		{
			return base.InternalDeserialize(xmlReader, declaredType, dataContract, name, ns);
		}
		return InternalDeserializeWithSurrogate(xmlReader, declaredType, dataContract, name, ns);
	}

	[RequiresDynamicCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.")]
	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	private object InternalDeserializeWithSurrogate(XmlReaderDelegator xmlReader, Type declaredType, DataContract surrogateDataContract, string name, string ns)
	{
		DataContract dataContract = surrogateDataContract ?? GetDataContract(DataContractSurrogateCaller.GetDataContractType(_serializationSurrogateProvider, declaredType));
		if (IsGetOnlyCollection && dataContract.UnderlyingType != declaredType)
		{
			throw new InvalidDataContractException(System.SR.Format(System.SR.SurrogatesWithGetOnlyCollectionsNotSupportedSerDeser, DataContract.GetClrTypeFullName(declaredType)));
		}
		ReadAttributes(xmlReader);
		string objectId = GetObjectId();
		object obj = InternalDeserialize(xmlReader, name, ns, declaredType, ref dataContract);
		object deserializedObject = DataContractSurrogateCaller.GetDeserializedObject(_serializationSurrogateProvider, obj, dataContract.UnderlyingType, declaredType);
		ReplaceDeserializedObject(objectId, obj, deserializedObject);
		return deserializedObject;
	}

	[RequiresDynamicCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.")]
	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	internal override void CheckIfTypeSerializable(Type memberType, bool isMemberTypeSerializable)
	{
		if (_serializationSurrogateProvider != null)
		{
			while (memberType.IsArray)
			{
				memberType = memberType.GetElementType();
			}
			memberType = DataContractSurrogateCaller.GetDataContractType(_serializationSurrogateProvider, memberType);
			if (!DataContract.IsTypeSerializable(memberType))
			{
				throw new InvalidDataContractException(System.SR.Format(System.SR.TypeNotSerializable, memberType));
			}
		}
		else
		{
			base.CheckIfTypeSerializable(memberType, isMemberTypeSerializable);
		}
	}

	[RequiresDynamicCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.")]
	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	internal override Type GetSurrogatedType(Type type)
	{
		if (_serializationSurrogateProvider == null)
		{
			return base.GetSurrogatedType(type);
		}
		type = DataContract.UnwrapNullableType(type);
		Type surrogatedType = DataContractSerializer.GetSurrogatedType(_serializationSurrogateProvider, type);
		if (IsGetOnlyCollection && surrogatedType != type)
		{
			throw new InvalidDataContractException(System.SR.Format(System.SR.SurrogatesWithGetOnlyCollectionsNotSupportedSerDeser, DataContract.GetClrTypeFullName(type)));
		}
		return surrogatedType;
	}

	internal override int GetArraySize()
	{
		if (!_preserveObjectReferences)
		{
			return -1;
		}
		return attributes.ArraySZSize;
	}
}
