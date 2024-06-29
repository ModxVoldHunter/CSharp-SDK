using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Xml;
using System.Xml.Schema;

namespace System.Runtime.Serialization.DataContracts;

public sealed class DataContractSet
{
	private Dictionary<XmlQualifiedName, DataContract> _contracts;

	private Dictionary<DataContract, object> _processedContracts;

	private readonly ISerializationSurrogateProvider _surrogateProvider;

	private readonly ISerializationSurrogateProvider2 _extendedSurrogateProvider;

	private Hashtable _surrogateData;

	private Dictionary<XmlQualifiedName, DataContract> _knownTypesForObject;

	private readonly List<Type> _referencedTypes;

	private readonly List<Type> _referencedCollectionTypes;

	private Dictionary<XmlQualifiedName, object> _referencedTypesDictionary;

	private Dictionary<XmlQualifiedName, object> _referencedCollectionTypesDictionary;

	public Dictionary<XmlQualifiedName, DataContract> Contracts => _contracts ?? (_contracts = new Dictionary<XmlQualifiedName, DataContract>());

	public Dictionary<DataContract, object> ProcessedContracts => _processedContracts ?? (_processedContracts = new Dictionary<DataContract, object>());

	public Hashtable SurrogateData => _surrogateData ?? (_surrogateData = new Hashtable());

	public Dictionary<XmlQualifiedName, DataContract>? KnownTypesForObject
	{
		get
		{
			return _knownTypesForObject;
		}
		internal set
		{
			_knownTypesForObject = value;
		}
	}

	internal ISerializationSurrogateProvider2? SerializationExtendedSurrogateProvider => _extendedSurrogateProvider;

	public DataContractSet(ISerializationSurrogateProvider? dataContractSurrogate, IEnumerable<Type>? referencedTypes, IEnumerable<Type>? referencedCollectionTypes)
	{
		_surrogateProvider = dataContractSurrogate;
		_extendedSurrogateProvider = dataContractSurrogate as ISerializationSurrogateProvider2;
		_referencedTypes = ((referencedTypes != null) ? new List<Type>(referencedTypes) : null);
		_referencedCollectionTypes = ((referencedCollectionTypes != null) ? new List<Type>(referencedCollectionTypes) : null);
	}

	[RequiresDynamicCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.")]
	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	public DataContractSet(DataContractSet dataContractSet)
	{
		ArgumentNullException.ThrowIfNull(dataContractSet, "dataContractSet");
		_referencedTypes = dataContractSet._referencedTypes;
		_referencedCollectionTypes = dataContractSet._referencedCollectionTypes;
		_extendedSurrogateProvider = dataContractSet._extendedSurrogateProvider;
		foreach (KeyValuePair<XmlQualifiedName, DataContract> contract in dataContractSet.Contracts)
		{
			InternalAdd(contract.Key, contract.Value);
		}
		if (dataContractSet._processedContracts == null)
		{
			return;
		}
		foreach (KeyValuePair<DataContract, object> processedContract in dataContractSet._processedContracts)
		{
			ProcessedContracts.Add(processedContract.Key, processedContract.Value);
		}
	}

	internal static void EnsureTypeNotGeneric(Type type)
	{
		if (type.ContainsGenericParameters)
		{
			throw new InvalidDataContractException(System.SR.Format(System.SR.GenericTypeNotExportable, type));
		}
	}

	[RequiresDynamicCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.")]
	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	internal void Add(Type type)
	{
		DataContract dataContract = GetDataContract(type);
		EnsureTypeNotGeneric(dataContract.UnderlyingType);
		Add(dataContract);
	}

	[RequiresDynamicCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.")]
	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	private void Add(DataContract dataContract)
	{
		Add(dataContract.XmlName, dataContract);
	}

	[RequiresDynamicCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.")]
	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	internal void Add(XmlQualifiedName name, DataContract dataContract)
	{
		if (!dataContract.IsBuiltInDataContract)
		{
			InternalAdd(name, dataContract);
		}
	}

	[RequiresDynamicCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.")]
	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	internal void InternalAdd(XmlQualifiedName name, DataContract dataContract)
	{
		if (Contracts.TryGetValue(name, out DataContract value))
		{
			if (!value.Equals(dataContract))
			{
				if (dataContract.UnderlyingType == null || value.UnderlyingType == null)
				{
					throw new InvalidOperationException(System.SR.Format(System.SR.DupContractInDataContractSet, dataContract.XmlName.Name, dataContract.XmlName.Namespace));
				}
				bool flag = DataContract.GetClrTypeFullName(dataContract.UnderlyingType) == DataContract.GetClrTypeFullName(value.UnderlyingType);
				throw new InvalidOperationException(System.SR.Format(System.SR.DupTypeContractInDataContractSet, flag ? dataContract.UnderlyingType.AssemblyQualifiedName : DataContract.GetClrTypeFullName(dataContract.UnderlyingType), flag ? value.UnderlyingType.AssemblyQualifiedName : DataContract.GetClrTypeFullName(value.UnderlyingType), dataContract.XmlName.Name, dataContract.XmlName.Namespace));
			}
		}
		else
		{
			Contracts.Add(name, dataContract);
			if (dataContract is ClassDataContract classDataContract)
			{
				AddClassDataContract(classDataContract);
			}
			else if (dataContract is CollectionDataContract collectionDataContract)
			{
				AddCollectionDataContract(collectionDataContract);
			}
			else if (dataContract is XmlDataContract xmlDataContract)
			{
				AddXmlDataContract(xmlDataContract);
			}
		}
	}

	[RequiresDynamicCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.")]
	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	private void AddClassDataContract(ClassDataContract classDataContract)
	{
		if (classDataContract.BaseClassContract != null)
		{
			Add(classDataContract.BaseClassContract.XmlName, classDataContract.BaseClassContract);
		}
		if (!classDataContract.IsISerializable && classDataContract.Members != null)
		{
			for (int i = 0; i < classDataContract.Members.Count; i++)
			{
				DataMember dataMember = classDataContract.Members[i];
				DataContract memberTypeDataContract = GetMemberTypeDataContract(dataMember);
				if (_extendedSurrogateProvider != null && dataMember.MemberInfo != null)
				{
					object customDataToExport = DataContractSurrogateCaller.GetCustomDataToExport(_extendedSurrogateProvider, dataMember.MemberInfo, memberTypeDataContract.UnderlyingType);
					if (customDataToExport != null)
					{
						SurrogateData.Add(dataMember, customDataToExport);
					}
				}
				Add(memberTypeDataContract.XmlName, memberTypeDataContract);
			}
		}
		AddKnownDataContracts(classDataContract.KnownDataContracts);
	}

	[RequiresDynamicCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.")]
	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	private void AddCollectionDataContract(CollectionDataContract collectionDataContract)
	{
		if (collectionDataContract.UnderlyingType != Globals.TypeOfSchemaDefinedType)
		{
			if (collectionDataContract.IsDictionary)
			{
				ClassDataContract classDataContract = collectionDataContract.ItemContract as ClassDataContract;
				AddClassDataContract(classDataContract);
			}
			else
			{
				DataContract itemTypeDataContract = GetItemTypeDataContract(collectionDataContract);
				if (itemTypeDataContract != null)
				{
					Add(itemTypeDataContract.XmlName, itemTypeDataContract);
				}
			}
		}
		AddKnownDataContracts(collectionDataContract.KnownDataContracts);
	}

	[RequiresDynamicCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.")]
	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	private void AddXmlDataContract(XmlDataContract xmlDataContract)
	{
		AddKnownDataContracts(xmlDataContract.KnownDataContracts);
	}

	[RequiresDynamicCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.")]
	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	private void AddKnownDataContracts(Dictionary<XmlQualifiedName, DataContract> knownDataContracts)
	{
		if (knownDataContracts == null || knownDataContracts.Count <= 0)
		{
			return;
		}
		foreach (DataContract value in knownDataContracts.Values)
		{
			Add(value);
		}
	}

	[RequiresDynamicCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.")]
	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	internal XmlQualifiedName GetXmlName(Type clrType)
	{
		if (_surrogateProvider != null)
		{
			Type dataContractType = DataContractSurrogateCaller.GetDataContractType(_surrogateProvider, clrType);
			return DataContract.GetXmlName(dataContractType);
		}
		return DataContract.GetXmlName(clrType);
	}

	[RequiresDynamicCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.")]
	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	public DataContract GetDataContract(Type type)
	{
		if (_surrogateProvider == null)
		{
			return DataContract.GetDataContract(type);
		}
		DataContract builtInDataContract = DataContract.GetBuiltInDataContract(type);
		if (builtInDataContract != null)
		{
			return builtInDataContract;
		}
		Type dataContractType = DataContractSurrogateCaller.GetDataContractType(_surrogateProvider, type);
		builtInDataContract = DataContract.GetDataContract(dataContractType);
		if (_extendedSurrogateProvider != null && !SurrogateData.Contains(builtInDataContract))
		{
			object customDataToExport = DataContractSurrogateCaller.GetCustomDataToExport(_extendedSurrogateProvider, type, dataContractType);
			if (customDataToExport != null)
			{
				SurrogateData.Add(builtInDataContract, customDataToExport);
			}
		}
		return builtInDataContract;
	}

	[RequiresDynamicCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.")]
	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	public DataContract? GetDataContract(XmlQualifiedName key)
	{
		DataContract value = DataContract.GetBuiltInDataContract(key.Name, key.Namespace);
		if (value == null)
		{
			Contracts.TryGetValue(key, out value);
		}
		return value;
	}

	[RequiresDynamicCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.")]
	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	internal DataContract GetMemberTypeDataContract(DataMember dataMember)
	{
		if (!(dataMember.MemberInfo is Type))
		{
			Type memberType = dataMember.MemberType;
			if (dataMember.IsGetOnlyCollection)
			{
				if (_surrogateProvider != null)
				{
					Type dataContractType = DataContractSurrogateCaller.GetDataContractType(_surrogateProvider, memberType);
					if (dataContractType != memberType)
					{
						throw new InvalidDataContractException(System.SR.Format(System.SR.SurrogatesWithGetOnlyCollectionsNotSupported, DataContract.GetClrTypeFullName(memberType), (dataMember.MemberInfo.DeclaringType != null) ? ((object)DataContract.GetClrTypeFullName(dataMember.MemberInfo.DeclaringType)) : ((object)dataMember.MemberInfo.DeclaringType), dataMember.MemberInfo.Name));
					}
				}
				return DataContract.GetGetOnlyCollectionDataContract(DataContract.GetId(memberType.TypeHandle), memberType.TypeHandle, memberType);
			}
			return GetDataContract(memberType);
		}
		return dataMember.MemberTypeContract;
	}

	[RequiresDynamicCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.")]
	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	internal DataContract GetItemTypeDataContract(CollectionDataContract collectionContract)
	{
		if (collectionContract.ItemType != null)
		{
			return GetDataContract(collectionContract.ItemType);
		}
		return collectionContract.ItemContract;
	}

	[RequiresDynamicCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.")]
	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	internal bool Remove(XmlQualifiedName key)
	{
		if (DataContract.GetBuiltInDataContract(key.Name, key.Namespace) != null)
		{
			return false;
		}
		return Contracts.Remove(key);
	}

	[RequiresDynamicCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.")]
	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	private Dictionary<XmlQualifiedName, object> GetReferencedTypes()
	{
		if (_referencedTypesDictionary == null)
		{
			_referencedTypesDictionary = new Dictionary<XmlQualifiedName, object>();
			_referencedTypesDictionary.Add(DataContract.GetXmlName(Globals.TypeOfNullable), Globals.TypeOfNullable);
			if (_referencedTypes != null)
			{
				foreach (Type referencedType in _referencedTypes)
				{
					if (referencedType == null)
					{
						throw new InvalidOperationException(System.SR.Format(System.SR.ReferencedTypesCannotContainNull));
					}
					AddReferencedType(_referencedTypesDictionary, referencedType);
				}
			}
		}
		return _referencedTypesDictionary;
	}

	[RequiresDynamicCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.")]
	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	private Dictionary<XmlQualifiedName, object> GetReferencedCollectionTypes()
	{
		if (_referencedCollectionTypesDictionary == null)
		{
			_referencedCollectionTypesDictionary = new Dictionary<XmlQualifiedName, object>();
			if (_referencedCollectionTypes != null)
			{
				foreach (Type referencedCollectionType in _referencedCollectionTypes)
				{
					if (referencedCollectionType == null)
					{
						throw new InvalidOperationException(System.SR.Format(System.SR.ReferencedCollectionTypesCannotContainNull));
					}
					AddReferencedType(_referencedCollectionTypesDictionary, referencedCollectionType);
				}
			}
			XmlQualifiedName xmlName = DataContract.GetXmlName(Globals.TypeOfDictionaryGeneric);
			if (!_referencedCollectionTypesDictionary.ContainsKey(xmlName) && GetReferencedTypes().ContainsKey(xmlName))
			{
				AddReferencedType(_referencedCollectionTypesDictionary, Globals.TypeOfDictionaryGeneric);
			}
		}
		return _referencedCollectionTypesDictionary;
	}

	[RequiresDynamicCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.")]
	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	private void AddReferencedType(Dictionary<XmlQualifiedName, object> referencedTypes, Type type)
	{
		if (!IsTypeReferenceable(type))
		{
			return;
		}
		XmlQualifiedName xmlName;
		try
		{
			xmlName = GetXmlName(type);
		}
		catch (InvalidDataContractException)
		{
			return;
		}
		catch (InvalidOperationException)
		{
			return;
		}
		if (referencedTypes.TryGetValue(xmlName, out var value))
		{
			if (value is Type type2)
			{
				if (type2 != type)
				{
					referencedTypes.Remove(xmlName);
					List<Type> list = new List<Type>();
					list.Add(type2);
					list.Add(type);
					referencedTypes.Add(xmlName, list);
				}
			}
			else
			{
				List<Type> list2 = (List<Type>)value;
				if (!list2.Contains(type))
				{
					list2.Add(type);
				}
			}
		}
		else
		{
			referencedTypes.Add(xmlName, type);
		}
	}

	[RequiresDynamicCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.")]
	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	private static bool IsTypeReferenceable(Type type)
	{
		try
		{
			Type itemType;
			return type.IsSerializable || type.IsDefined(Globals.TypeOfDataContractAttribute, inherit: false) || (Globals.TypeOfIXmlSerializable.IsAssignableFrom(type) && !type.IsGenericTypeDefinition) || CollectionDataContract.IsCollection(type, out itemType) || ClassDataContract.IsNonAttributedTypeValidForSerialization(type);
		}
		catch (Exception exception) when (!ExceptionUtility.IsFatal(exception))
		{
		}
		return false;
	}

	[RequiresDynamicCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.")]
	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	public Type? GetReferencedType(XmlQualifiedName xmlName, DataContract dataContract, out DataContract? referencedContract, out object[]? genericParameters, bool? supportGenericTypes = null)
	{
		Type referencedTypeInternal = GetReferencedTypeInternal(xmlName, dataContract);
		referencedContract = null;
		genericParameters = null;
		if (!supportGenericTypes.HasValue)
		{
			return referencedTypeInternal;
		}
		if (referencedTypeInternal != null && !referencedTypeInternal.IsGenericTypeDefinition && !referencedTypeInternal.ContainsGenericParameters)
		{
			return referencedTypeInternal;
		}
		if (dataContract.GenericInfo == null)
		{
			return null;
		}
		XmlQualifiedName expandedXmlName = dataContract.GenericInfo.GetExpandedXmlName();
		if (expandedXmlName != dataContract.XmlName)
		{
			throw new InvalidDataContractException(System.SR.Format(System.SR.GenericTypeNameMismatch, dataContract.XmlName.Name, dataContract.XmlName.Namespace, expandedXmlName.Name, expandedXmlName.Namespace));
		}
		if (!supportGenericTypes.Value)
		{
			return null;
		}
		return GetReferencedGenericTypeInternal(dataContract.GenericInfo, out referencedContract, out genericParameters);
	}

	[RequiresDynamicCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.")]
	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	private Type GetReferencedGenericTypeInternal(GenericInfo genInfo, out DataContract referencedContract, out object[] genericParameters)
	{
		genericParameters = null;
		referencedContract = null;
		Type referencedTypeInternal = GetReferencedTypeInternal(genInfo.XmlName, null);
		if (referencedTypeInternal == null)
		{
			if (genInfo.Parameters != null)
			{
				return null;
			}
			referencedContract = GetDataContract(genInfo.XmlName);
			if (referencedContract != null && referencedContract.GenericInfo != null)
			{
				referencedContract = null;
			}
			return null;
		}
		if (genInfo.Parameters != null)
		{
			bool flag = referencedTypeInternal != Globals.TypeOfNullable;
			genericParameters = new object[genInfo.Parameters.Count];
			DataContract[] array = new DataContract[genInfo.Parameters.Count];
			for (int i = 0; i < genInfo.Parameters.Count; i++)
			{
				GenericInfo genericInfo = genInfo.Parameters[i];
				XmlQualifiedName expandedXmlName = genericInfo.GetExpandedXmlName();
				DataContract referencedContract2 = GetDataContract(expandedXmlName);
				if (referencedContract2 != null)
				{
					genericParameters[i] = referencedContract2;
				}
				else
				{
					object[] genericParameters2;
					Type referencedGenericTypeInternal = GetReferencedGenericTypeInternal(genericInfo, out referencedContract2, out genericParameters2);
					if (referencedGenericTypeInternal != null)
					{
						genericParameters[i] = new Tuple<Type, object[]>(referencedGenericTypeInternal, genericParameters2);
					}
					else
					{
						genericParameters[i] = referencedContract2;
					}
				}
				array[i] = referencedContract2;
				if (referencedContract2 == null)
				{
					flag = false;
				}
			}
			if (flag)
			{
				referencedContract = DataContract.GetDataContract(referencedTypeInternal).BindGenericParameters(array);
			}
		}
		return referencedTypeInternal;
	}

	[RequiresDynamicCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.")]
	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	private Type GetReferencedTypeInternal(XmlQualifiedName xmlName, DataContract dataContract)
	{
		Type type;
		if (dataContract == null)
		{
			if (TryGetReferencedCollectionType(xmlName, null, out type))
			{
				return type;
			}
			if (TryGetReferencedType(xmlName, null, out type))
			{
				if (CollectionDataContract.IsCollection(type))
				{
					return null;
				}
				return type;
			}
		}
		else if (dataContract is CollectionDataContract)
		{
			if (TryGetReferencedCollectionType(xmlName, dataContract, out type))
			{
				return type;
			}
		}
		else
		{
			if (dataContract is XmlDataContract { IsAnonymous: not false } xmlDataContract)
			{
				xmlName = SchemaImporter.ImportActualType(xmlDataContract.XsdType?.Annotation, xmlName, dataContract.XmlName);
			}
			if (TryGetReferencedType(xmlName, dataContract, out type))
			{
				return type;
			}
		}
		return null;
	}

	[RequiresDynamicCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.")]
	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	internal bool TryGetReferencedType(XmlQualifiedName xmlName, DataContract dataContract, [NotNullWhen(true)] out Type type)
	{
		return TryGetReferencedType(xmlName, dataContract, useReferencedCollectionTypes: false, out type);
	}

	[RequiresDynamicCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.")]
	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	internal bool TryGetReferencedCollectionType(XmlQualifiedName xmlName, DataContract dataContract, [NotNullWhen(true)] out Type type)
	{
		return TryGetReferencedType(xmlName, dataContract, useReferencedCollectionTypes: true, out type);
	}

	[RequiresDynamicCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.")]
	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	private bool TryGetReferencedType(XmlQualifiedName xmlName, DataContract dataContract, bool useReferencedCollectionTypes, [NotNullWhen(true)] out Type type)
	{
		Dictionary<XmlQualifiedName, object> dictionary = (useReferencedCollectionTypes ? GetReferencedCollectionTypes() : GetReferencedTypes());
		if (dictionary.TryGetValue(xmlName, out var value))
		{
			type = value as Type;
			if (type != null)
			{
				return true;
			}
			List<Type> list = (List<Type>)value;
			StringBuilder stringBuilder = new StringBuilder();
			bool flag = false;
			for (int i = 0; i < list.Count; i++)
			{
				Type type2 = list[i];
				if (!flag)
				{
					flag = type2.IsGenericTypeDefinition;
				}
				stringBuilder.AppendFormat("{0}\"{1}\" ", Environment.NewLine, type2.AssemblyQualifiedName);
				if (dataContract != null)
				{
					DataContract dataContract2 = GetDataContract(type2);
					stringBuilder.Append(System.SR.Format((dataContract2 != null && dataContract2.Equals(dataContract)) ? System.SR.ReferencedTypeMatchingMessage : System.SR.ReferencedTypeNotMatchingMessage));
				}
			}
			if (flag)
			{
				throw new InvalidOperationException(System.SR.Format(useReferencedCollectionTypes ? System.SR.AmbiguousReferencedCollectionTypes1 : System.SR.AmbiguousReferencedTypes1, stringBuilder.ToString()));
			}
			throw new InvalidOperationException(System.SR.Format(useReferencedCollectionTypes ? System.SR.AmbiguousReferencedCollectionTypes3 : System.SR.AmbiguousReferencedTypes3, XmlConvert.DecodeName(xmlName.Name), xmlName.Namespace, stringBuilder.ToString()));
		}
		type = null;
		return false;
	}

	internal object GetSurrogateData(object key)
	{
		return SurrogateData[key];
	}

	internal void SetSurrogateData(object key, object surrogateData)
	{
		SurrogateData[key] = surrogateData;
	}

	internal bool IsContractProcessed(DataContract dataContract)
	{
		return ProcessedContracts.ContainsKey(dataContract);
	}

	internal void SetContractProcessed(DataContract dataContract)
	{
		ProcessedContracts.Add(dataContract, dataContract);
	}

	internal IEnumerator<KeyValuePair<XmlQualifiedName, DataContract>> GetEnumerator()
	{
		return Contracts.GetEnumerator();
	}

	[RequiresDynamicCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.")]
	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	public void ImportSchemaSet(XmlSchemaSet schemaSet, IEnumerable<XmlQualifiedName>? typeNames, bool importXmlDataType)
	{
		SchemaImporter schemaImporter = new SchemaImporter(schemaSet, typeNames, null, this, importXmlDataType);
		schemaImporter.Import(out var _);
	}

	[RequiresDynamicCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.")]
	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	public List<XmlQualifiedName> ImportSchemaSet(XmlSchemaSet schemaSet, IEnumerable<XmlSchemaElement> elements, bool importXmlDataType)
	{
		SchemaImporter schemaImporter = new SchemaImporter(schemaSet, Array.Empty<XmlQualifiedName>(), elements, this, importXmlDataType);
		schemaImporter.Import(out var elementTypeNames);
		return elementTypeNames;
	}
}
