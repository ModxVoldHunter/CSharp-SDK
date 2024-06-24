using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization.DataContracts;
using System.Xml;

namespace System.Runtime.Serialization;

internal class XmlObjectSerializerContext
{
	protected XmlObjectSerializer serializer;

	protected DataContract rootTypeDataContract;

	internal ScopedKnownTypes scopedKnownTypes;

	protected Dictionary<XmlQualifiedName, DataContract> serializerKnownDataContracts;

	private bool _isSerializerKnownDataContractsSetExplicit;

	protected IList<Type> serializerKnownTypeList;

	private int _itemCount;

	private readonly int _maxItemsInObjectGraph;

	private readonly StreamingContext _streamingContext;

	private readonly bool _ignoreExtensionDataObject;

	private readonly DataContractResolver _dataContractResolver;

	private KnownTypeDataContractResolver _knownTypeResolver;

	internal virtual bool IsGetOnlyCollection
	{
		get
		{
			return false;
		}
		set
		{
		}
	}

	internal int RemainingItemCount => _maxItemsInObjectGraph - _itemCount;

	internal bool IgnoreExtensionDataObject => _ignoreExtensionDataObject;

	protected DataContractResolver DataContractResolver => _dataContractResolver;

	protected KnownTypeDataContractResolver KnownTypeResolver => _knownTypeResolver ?? (_knownTypeResolver = new KnownTypeDataContractResolver(this));

	internal virtual Dictionary<XmlQualifiedName, DataContract> SerializerKnownDataContracts
	{
		[RequiresDynamicCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.")]
		[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
		get
		{
			if (!_isSerializerKnownDataContractsSetExplicit)
			{
				serializerKnownDataContracts = serializer.KnownDataContracts;
				_isSerializerKnownDataContractsSetExplicit = true;
			}
			return serializerKnownDataContracts;
		}
	}

	internal XmlObjectSerializerContext(XmlObjectSerializer serializer, int maxItemsInObjectGraph, StreamingContext streamingContext, bool ignoreExtensionDataObject, DataContractResolver dataContractResolver)
	{
		this.serializer = serializer;
		_itemCount = 1;
		_maxItemsInObjectGraph = maxItemsInObjectGraph;
		_streamingContext = streamingContext;
		_ignoreExtensionDataObject = ignoreExtensionDataObject;
		_dataContractResolver = dataContractResolver;
	}

	internal XmlObjectSerializerContext(XmlObjectSerializer serializer, int maxItemsInObjectGraph, StreamingContext streamingContext, bool ignoreExtensionDataObject)
		: this(serializer, maxItemsInObjectGraph, streamingContext, ignoreExtensionDataObject, null)
	{
	}

	internal XmlObjectSerializerContext(DataContractSerializer serializer, DataContract rootTypeDataContract, DataContractResolver dataContractResolver)
		: this(serializer, serializer.MaxItemsInObjectGraph, new StreamingContext(StreamingContextStates.All), serializer.IgnoreExtensionDataObject, dataContractResolver)
	{
		this.rootTypeDataContract = rootTypeDataContract;
		serializerKnownTypeList = serializer._knownTypeList;
	}

	internal StreamingContext GetStreamingContext()
	{
		return _streamingContext;
	}

	internal void IncrementItemCount(int count)
	{
		if (count > _maxItemsInObjectGraph - _itemCount)
		{
			throw XmlObjectSerializer.CreateSerializationException(System.SR.Format(System.SR.ExceededMaxItemsQuota, _maxItemsInObjectGraph));
		}
		_itemCount += count;
	}

	[RequiresDynamicCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.")]
	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	internal DataContract GetDataContract(Type type)
	{
		return GetDataContract(type.TypeHandle, type);
	}

	[RequiresDynamicCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.")]
	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	internal virtual DataContract GetDataContract(RuntimeTypeHandle typeHandle, Type type)
	{
		if (IsGetOnlyCollection)
		{
			return DataContract.GetGetOnlyCollectionDataContract(DataContract.GetId(typeHandle), typeHandle, type);
		}
		return DataContract.GetDataContract(typeHandle);
	}

	[RequiresDynamicCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.")]
	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	internal virtual DataContract GetDataContractSkipValidation(int typeId, RuntimeTypeHandle typeHandle, Type type)
	{
		if (IsGetOnlyCollection)
		{
			return DataContract.GetGetOnlyCollectionDataContractSkipValidation(typeId, typeHandle, type);
		}
		return DataContract.GetDataContractSkipValidation(typeId, typeHandle, type);
	}

	[RequiresDynamicCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.")]
	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	internal virtual DataContract GetDataContract(int id, RuntimeTypeHandle typeHandle)
	{
		if (IsGetOnlyCollection)
		{
			return DataContract.GetGetOnlyCollectionDataContract(id, typeHandle, null);
		}
		return DataContract.GetDataContract(id, typeHandle);
	}

	[RequiresDynamicCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.")]
	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	internal virtual void CheckIfTypeSerializable(Type memberType, bool isMemberTypeSerializable)
	{
		if (!isMemberTypeSerializable)
		{
			throw new InvalidDataContractException(System.SR.Format(System.SR.TypeNotSerializable, memberType));
		}
	}

	[RequiresDynamicCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.")]
	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	internal virtual Type GetSurrogatedType(Type type)
	{
		return type;
	}

	[RequiresDynamicCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.")]
	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	private DataContract GetDataContractFromSerializerKnownTypes(XmlQualifiedName qname)
	{
		Dictionary<XmlQualifiedName, DataContract> dictionary = SerializerKnownDataContracts;
		if (dictionary == null)
		{
			return null;
		}
		if (!dictionary.TryGetValue(qname, out var value))
		{
			return null;
		}
		return value;
	}

	[RequiresDynamicCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.")]
	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	internal static Dictionary<XmlQualifiedName, DataContract> GetDataContractsForKnownTypes(IList<Type> knownTypeList)
	{
		if (knownTypeList == null)
		{
			return null;
		}
		Dictionary<XmlQualifiedName, DataContract> nameToDataContractTable = new Dictionary<XmlQualifiedName, DataContract>();
		Dictionary<Type, Type> typesChecked = new Dictionary<Type, Type>();
		for (int i = 0; i < knownTypeList.Count; i++)
		{
			Type type = knownTypeList[i];
			if (type == null)
			{
				throw new ArgumentException(System.SR.Format(System.SR.NullKnownType, "knownTypes"));
			}
			DataContract.CheckAndAdd(type, typesChecked, ref nameToDataContractTable);
		}
		return nameToDataContractTable;
	}

	[RequiresDynamicCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.")]
	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	internal bool IsKnownType(DataContract dataContract, Dictionary<XmlQualifiedName, DataContract> knownDataContracts, Type declaredType)
	{
		bool flag = false;
		if (knownDataContracts != null && knownDataContracts.Count > 0)
		{
			scopedKnownTypes.Push(knownDataContracts);
			flag = true;
		}
		bool result = IsKnownType(dataContract, declaredType);
		if (flag)
		{
			scopedKnownTypes.Pop();
		}
		return result;
	}

	[RequiresDynamicCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.")]
	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	internal bool IsKnownType(DataContract dataContract, Type declaredType)
	{
		DataContract dataContract2 = ResolveDataContractFromKnownTypes(dataContract.XmlName.Name, dataContract.XmlName.Namespace, null, declaredType);
		if (dataContract2 != null)
		{
			return dataContract2.UnderlyingType == dataContract.UnderlyingType;
		}
		return false;
	}

	[RequiresDynamicCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.")]
	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	internal Type ResolveNameFromKnownTypes(XmlQualifiedName typeName)
	{
		return ResolveDataContractFromKnownTypes(typeName)?.OriginalUnderlyingType;
	}

	[RequiresDynamicCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.")]
	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	private DataContract ResolveDataContractFromKnownTypes(XmlQualifiedName typeName)
	{
		return PrimitiveDataContract.GetPrimitiveDataContract(typeName.Name, typeName.Namespace) ?? scopedKnownTypes.GetDataContract(typeName) ?? GetDataContractFromSerializerKnownTypes(typeName);
	}

	[RequiresDynamicCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.")]
	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	protected DataContract ResolveDataContractFromKnownTypes(string typeName, string typeNs, DataContract memberTypeContract, Type declaredType)
	{
		XmlQualifiedName xmlQualifiedName = new XmlQualifiedName(typeName, typeNs);
		DataContract dataContract;
		if (_dataContractResolver == null)
		{
			dataContract = ResolveDataContractFromKnownTypes(xmlQualifiedName);
		}
		else
		{
			Type type = _dataContractResolver.ResolveName(typeName, typeNs, declaredType, KnownTypeResolver);
			dataContract = ((type == null) ? null : GetDataContract(type));
		}
		if (dataContract == null)
		{
			if (memberTypeContract != null && !memberTypeContract.UnderlyingType.IsInterface && memberTypeContract.XmlName == xmlQualifiedName)
			{
				dataContract = memberTypeContract;
			}
			if (dataContract == null && rootTypeDataContract != null)
			{
				dataContract = ((!(rootTypeDataContract.XmlName == xmlQualifiedName)) ? ResolveDataContractFromRootDataContract(xmlQualifiedName) : rootTypeDataContract);
			}
		}
		return dataContract;
	}

	[RequiresDynamicCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.")]
	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	protected virtual DataContract ResolveDataContractFromRootDataContract(XmlQualifiedName typeQName)
	{
		CollectionDataContract collectionDataContract = rootTypeDataContract as CollectionDataContract;
		while (collectionDataContract != null)
		{
			DataContract dataContract = GetDataContract(GetSurrogatedType(collectionDataContract.ItemType));
			if (dataContract.XmlName == typeQName)
			{
				return dataContract;
			}
			collectionDataContract = dataContract as CollectionDataContract;
		}
		return null;
	}
}
