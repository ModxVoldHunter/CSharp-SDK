using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Runtime.Serialization.DataContracts;
using System.Xml;
using System.Xml.Schema;

namespace System.Runtime.Serialization;

internal sealed class SchemaImporter
{
	private readonly DataContractSet _dataContractSet;

	private readonly XmlSchemaSet _schemaSet;

	private readonly IEnumerable<XmlQualifiedName> _typeNames;

	private readonly IEnumerable<XmlSchemaElement> _elements;

	private readonly bool _importXmlDataType;

	private Dictionary<XmlQualifiedName, SchemaObjectInfo> _schemaObjects;

	private List<XmlSchemaRedefine> _redefineList;

	private bool _needToImportKnownTypesForObject;

	private static Hashtable s_serializationSchemaElements;

	private Dictionary<XmlQualifiedName, SchemaObjectInfo> SchemaObjects => _schemaObjects ?? (_schemaObjects = CreateSchemaObjects());

	private List<XmlSchemaRedefine> RedefineList => _redefineList ?? (_redefineList = CreateRedefineList());

	internal SchemaImporter(XmlSchemaSet schemas, IEnumerable<XmlQualifiedName> typeNames, IEnumerable<XmlSchemaElement> elements, DataContractSet dataContractSet, bool importXmlDataType)
	{
		_dataContractSet = dataContractSet;
		_schemaSet = schemas;
		_typeNames = typeNames;
		_elements = elements;
		_importXmlDataType = importXmlDataType;
	}

	[RequiresDynamicCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.")]
	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	internal void Import([NotNullIfNotNull("_elements")] out List<XmlQualifiedName> elementTypeNames)
	{
		elementTypeNames = null;
		if (!_schemaSet.Contains("http://schemas.microsoft.com/2003/10/Serialization/"))
		{
			StringReader input = new StringReader("<?xml version='1.0' encoding='utf-8'?>\r\n<xs:schema elementFormDefault='qualified' attributeFormDefault='qualified' xmlns:tns='http://schemas.microsoft.com/2003/10/Serialization/' targetNamespace='http://schemas.microsoft.com/2003/10/Serialization/' xmlns:xs='http://www.w3.org/2001/XMLSchema'>\r\n  <xs:element name='anyType' nillable='true' type='xs:anyType' />\r\n  <xs:element name='anyURI' nillable='true' type='xs:anyURI' />\r\n  <xs:element name='base64Binary' nillable='true' type='xs:base64Binary' />\r\n  <xs:element name='boolean' nillable='true' type='xs:boolean' />\r\n  <xs:element name='byte' nillable='true' type='xs:byte' />\r\n  <xs:element name='dateTime' nillable='true' type='xs:dateTime' />\r\n  <xs:element name='decimal' nillable='true' type='xs:decimal' />\r\n  <xs:element name='double' nillable='true' type='xs:double' />\r\n  <xs:element name='float' nillable='true' type='xs:float' />\r\n  <xs:element name='int' nillable='true' type='xs:int' />\r\n  <xs:element name='long' nillable='true' type='xs:long' />\r\n  <xs:element name='QName' nillable='true' type='xs:QName' />\r\n  <xs:element name='short' nillable='true' type='xs:short' />\r\n  <xs:element name='string' nillable='true' type='xs:string' />\r\n  <xs:element name='unsignedByte' nillable='true' type='xs:unsignedByte' />\r\n  <xs:element name='unsignedInt' nillable='true' type='xs:unsignedInt' />\r\n  <xs:element name='unsignedLong' nillable='true' type='xs:unsignedLong' />\r\n  <xs:element name='unsignedShort' nillable='true' type='xs:unsignedShort' />\r\n  <xs:element name='char' nillable='true' type='tns:char' />\r\n  <xs:simpleType name='char'>\r\n    <xs:restriction base='xs:int'/>\r\n  </xs:simpleType>\r\n  <xs:element name='duration' nillable='true' type='tns:duration' />\r\n  <xs:simpleType name='duration'>\r\n    <xs:restriction base='xs:duration'>\r\n      <xs:pattern value='\\-?P(\\d*D)?(T(\\d*H)?(\\d*M)?(\\d*(\\.\\d*)?S)?)?' />\r\n      <xs:minInclusive value='-P10675199DT2H48M5.4775808S' />\r\n      <xs:maxInclusive value='P10675199DT2H48M5.4775807S' />\r\n    </xs:restriction>\r\n  </xs:simpleType>\r\n  <xs:element name='guid' nillable='true' type='tns:guid' />\r\n  <xs:simpleType name='guid'>\r\n    <xs:restriction base='xs:string'>\r\n      <xs:pattern value='[\\da-fA-F]{8}-[\\da-fA-F]{4}-[\\da-fA-F]{4}-[\\da-fA-F]{4}-[\\da-fA-F]{12}' />\r\n    </xs:restriction>\r\n  </xs:simpleType>\r\n  <xs:attribute name='FactoryType' type='xs:QName' />\r\n  <xs:attribute name='Id' type='xs:ID' />\r\n  <xs:attribute name='Ref' type='xs:IDREF' />\r\n</xs:schema>\r\n");
			XmlSchema xmlSchema = XmlSchema.Read(new XmlTextReader(input)
			{
				DtdProcessing = DtdProcessing.Prohibit
			}, null);
			if (xmlSchema == null)
			{
				throw new InvalidOperationException(System.SR.Format(System.SR.CouldNotReadSerializationSchema, "http://schemas.microsoft.com/2003/10/Serialization/"));
			}
			_schemaSet.Add(xmlSchema);
		}
		try
		{
			CompileSchemaSet(_schemaSet);
		}
		catch (Exception ex) when (!ExceptionUtility.IsFatal(ex))
		{
			throw new ArgumentException(System.SR.Format(System.SR.CannotImportInvalidSchemas), ex);
		}
		if (_typeNames == null)
		{
			ICollection collection = _schemaSet.Schemas();
			foreach (object item in collection)
			{
				if (item == null)
				{
					throw new ArgumentException(System.SR.Format(System.SR.CannotImportNullSchema));
				}
				XmlSchema xmlSchema2 = (XmlSchema)item;
				if (!(xmlSchema2.TargetNamespace != "http://schemas.microsoft.com/2003/10/Serialization/") || !(xmlSchema2.TargetNamespace != "http://www.w3.org/2001/XMLSchema"))
				{
					continue;
				}
				foreach (XmlSchemaObject value in xmlSchema2.SchemaTypes.Values)
				{
					ImportType((XmlSchemaType)value);
				}
				foreach (XmlSchemaElement value2 in xmlSchema2.Elements.Values)
				{
					if (value2.SchemaType != null)
					{
						ImportAnonymousGlobalElement(value2, value2.QualifiedName, xmlSchema2.TargetNamespace);
					}
				}
			}
		}
		else
		{
			foreach (XmlQualifiedName typeName in _typeNames)
			{
				if (typeName == null)
				{
					throw new ArgumentException(System.SR.Format(System.SR.CannotImportNullDataContractName));
				}
				ImportType(typeName);
			}
			if (_elements != null)
			{
				List<XmlQualifiedName> list = new List<XmlQualifiedName>();
				foreach (XmlSchemaElement element in _elements)
				{
					XmlQualifiedName schemaTypeName = element.SchemaTypeName;
					if (schemaTypeName != null && schemaTypeName.Name.Length > 0)
					{
						list.Add(ImportType(schemaTypeName).XmlName);
						continue;
					}
					XmlSchema schemaWithGlobalElementDeclaration = SchemaHelper.GetSchemaWithGlobalElementDeclaration(element, _schemaSet);
					if (schemaWithGlobalElementDeclaration == null)
					{
						list.Add(ImportAnonymousElement(element, element.QualifiedName).XmlName);
					}
					else
					{
						list.Add(ImportAnonymousGlobalElement(element, element.QualifiedName, schemaWithGlobalElementDeclaration.TargetNamespace).XmlName);
					}
				}
				if (list.Count > 0)
				{
					elementTypeNames = list;
				}
			}
		}
		ImportKnownTypesForObject();
	}

	internal static void CompileSchemaSet(XmlSchemaSet schemaSet)
	{
		if (schemaSet.Contains("http://www.w3.org/2001/XMLSchema"))
		{
			schemaSet.Compile();
			return;
		}
		XmlSchema xmlSchema = new XmlSchema();
		xmlSchema.TargetNamespace = "http://www.w3.org/2001/XMLSchema";
		XmlSchemaElement xmlSchemaElement = new XmlSchemaElement();
		xmlSchemaElement.Name = "schema";
		xmlSchemaElement.SchemaType = new XmlSchemaComplexType();
		xmlSchema.Items.Add(xmlSchemaElement);
		schemaSet.Add(xmlSchema);
		schemaSet.Compile();
	}

	[RequiresDynamicCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.")]
	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	private void ImportKnownTypes(XmlQualifiedName typeName)
	{
		if (!SchemaObjects.TryGetValue(typeName, out var value))
		{
			return;
		}
		List<XmlSchemaType> knownTypes = value._knownTypes;
		if (knownTypes == null)
		{
			return;
		}
		foreach (XmlSchemaType item in knownTypes)
		{
			ImportType(item);
		}
	}

	[RequiresDynamicCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.")]
	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	internal static bool IsObjectContract(DataContract dataContract)
	{
		Dictionary<Type, object> dictionary = new Dictionary<Type, object>();
		while (dataContract is CollectionDataContract)
		{
			if (dataContract.OriginalUnderlyingType == null)
			{
				dataContract = ((CollectionDataContract)dataContract).ItemContract;
				continue;
			}
			if (dictionary.ContainsKey(dataContract.OriginalUnderlyingType))
			{
				break;
			}
			dictionary.Add(dataContract.OriginalUnderlyingType, dataContract.OriginalUnderlyingType);
			dataContract = ((CollectionDataContract)dataContract).ItemContract;
		}
		if (dataContract is PrimitiveDataContract)
		{
			return ((PrimitiveDataContract)dataContract).UnderlyingType == Globals.TypeOfObject;
		}
		return false;
	}

	[RequiresDynamicCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.")]
	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	private void ImportKnownTypesForObject()
	{
		if (!_needToImportKnownTypesForObject)
		{
			return;
		}
		_needToImportKnownTypesForObject = false;
		if (_dataContractSet.KnownTypesForObject != null || !SchemaObjects.TryGetValue(SchemaExporter.AnytypeQualifiedName, out var value))
		{
			return;
		}
		List<XmlSchemaType> knownTypes = value._knownTypes;
		if (knownTypes == null)
		{
			return;
		}
		Dictionary<XmlQualifiedName, DataContract> dictionary = new Dictionary<XmlQualifiedName, DataContract>();
		foreach (XmlSchemaType item in knownTypes)
		{
			DataContract dataContract = ImportType(item);
			if (!dictionary.TryGetValue(dataContract.XmlName, out var _))
			{
				dictionary.Add(dataContract.XmlName, dataContract);
			}
		}
		_dataContractSet.KnownTypesForObject = dictionary;
	}

	internal Dictionary<XmlQualifiedName, SchemaObjectInfo> CreateSchemaObjects()
	{
		Dictionary<XmlQualifiedName, SchemaObjectInfo> dictionary = new Dictionary<XmlQualifiedName, SchemaObjectInfo>();
		ICollection collection = _schemaSet.Schemas();
		List<XmlSchemaType> list = new List<XmlSchemaType>();
		dictionary.Add(SchemaExporter.AnytypeQualifiedName, new SchemaObjectInfo(null, null, null, list));
		foreach (XmlSchema item in collection)
		{
			if (!(item.TargetNamespace != "http://schemas.microsoft.com/2003/10/Serialization/"))
			{
				continue;
			}
			foreach (XmlSchemaObject value4 in item.SchemaTypes.Values)
			{
				if (!(value4 is XmlSchemaType xmlSchemaType))
				{
					continue;
				}
				list.Add(xmlSchemaType);
				XmlQualifiedName key = new XmlQualifiedName(xmlSchemaType.Name, item.TargetNamespace);
				if (dictionary.TryGetValue(key, out var value))
				{
					value._type = xmlSchemaType;
					value._schema = item;
				}
				else
				{
					dictionary.Add(key, new SchemaObjectInfo(xmlSchemaType, null, item, null));
				}
				XmlQualifiedName baseTypeName = GetBaseTypeName(xmlSchemaType);
				if (!(baseTypeName != null))
				{
					continue;
				}
				if (dictionary.TryGetValue(baseTypeName, out var value2))
				{
					SchemaObjectInfo schemaObjectInfo = value2;
					if (schemaObjectInfo._knownTypes == null)
					{
						schemaObjectInfo._knownTypes = new List<XmlSchemaType>();
					}
				}
				else
				{
					value2 = new SchemaObjectInfo(null, null, null, new List<XmlSchemaType>());
					dictionary.Add(baseTypeName, value2);
				}
				value2._knownTypes.Add(xmlSchemaType);
			}
			foreach (XmlSchemaObject value5 in item.Elements.Values)
			{
				if (value5 is XmlSchemaElement xmlSchemaElement)
				{
					XmlQualifiedName key2 = new XmlQualifiedName(xmlSchemaElement.Name, item.TargetNamespace);
					if (dictionary.TryGetValue(key2, out var value3))
					{
						value3._element = xmlSchemaElement;
						value3._schema = item;
					}
					else
					{
						dictionary.Add(key2, new SchemaObjectInfo(null, xmlSchemaElement, item, null));
					}
				}
			}
		}
		return dictionary;
	}

	private static XmlQualifiedName GetBaseTypeName(XmlSchemaType type)
	{
		XmlQualifiedName result = null;
		if (type is XmlSchemaComplexType { ContentModel: not null, ContentModel: XmlSchemaComplexContent { Content: XmlSchemaComplexContentExtension content } })
		{
			result = content.BaseTypeName;
		}
		return result;
	}

	private List<XmlSchemaRedefine> CreateRedefineList()
	{
		List<XmlSchemaRedefine> list = new List<XmlSchemaRedefine>();
		ICollection collection = _schemaSet.Schemas();
		foreach (object item2 in collection)
		{
			if (!(item2 is XmlSchema xmlSchema))
			{
				continue;
			}
			foreach (XmlSchemaExternal include in xmlSchema.Includes)
			{
				if (include is XmlSchemaRedefine item)
				{
					list.Add(item);
				}
			}
		}
		return list;
	}

	[RequiresDynamicCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.")]
	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	private DataContract ImportAnonymousGlobalElement(XmlSchemaElement element, XmlQualifiedName typeQName, string ns)
	{
		DataContract dataContract = ImportAnonymousElement(element, typeQName);
		if (dataContract is XmlDataContract xmlDataContract)
		{
			xmlDataContract.SetTopLevelElementName(new XmlQualifiedName(element.Name, ns));
			xmlDataContract.IsTopLevelElementNullable = element.IsNillable;
		}
		return dataContract;
	}

	[RequiresDynamicCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.")]
	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	private DataContract ImportAnonymousElement(XmlSchemaElement element, XmlQualifiedName typeQName)
	{
		if (SchemaHelper.GetSchemaType(SchemaObjects, typeQName) != null)
		{
			int num = 1;
			while (true)
			{
				typeQName = new XmlQualifiedName(typeQName.Name + num.ToString(NumberFormatInfo.InvariantInfo), typeQName.Namespace);
				if (SchemaHelper.GetSchemaType(SchemaObjects, typeQName) == null)
				{
					break;
				}
				if (num == int.MaxValue)
				{
					throw new InvalidDataContractException(System.SR.Format(System.SR.CannotComputeUniqueName, element.Name));
				}
				num++;
			}
		}
		if (element.SchemaType == null)
		{
			return ImportType(SchemaExporter.AnytypeQualifiedName);
		}
		return ImportType(element.SchemaType, typeQName, isAnonymous: true);
	}

	[RequiresDynamicCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.")]
	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	private DataContract ImportType(XmlQualifiedName typeName)
	{
		DataContract dataContract = DataContract.GetBuiltInDataContract(typeName.Name, typeName.Namespace);
		if (dataContract == null)
		{
			XmlSchemaType schemaType = SchemaHelper.GetSchemaType(SchemaObjects, typeName);
			if (schemaType == null)
			{
				throw new InvalidDataContractException(System.SR.Format(System.SR.SpecifiedTypeNotFoundInSchema, typeName.Name, typeName.Namespace));
			}
			dataContract = ImportType(schemaType);
		}
		if (IsObjectContract(dataContract))
		{
			_needToImportKnownTypesForObject = true;
		}
		return dataContract;
	}

	[RequiresDynamicCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.")]
	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	private DataContract ImportType(XmlSchemaType type)
	{
		return ImportType(type, type.QualifiedName, isAnonymous: false);
	}

	[RequiresDynamicCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.")]
	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	private DataContract ImportType(XmlSchemaType type, XmlQualifiedName typeName, bool isAnonymous)
	{
		DataContract dataContract = _dataContractSet.GetDataContract(typeName);
		if (dataContract != null)
		{
			return dataContract;
		}
		InvalidDataContractException ex2;
		try
		{
			foreach (XmlSchemaRedefine redefine in RedefineList)
			{
				if (redefine.SchemaTypes[typeName] != null)
				{
					ThrowTypeCannotBeImportedException(typeName.Name, typeName.Namespace, System.SR.Format(System.SR.RedefineNotSupported));
				}
			}
			if (type is XmlSchemaSimpleType xmlSchemaSimpleType)
			{
				XmlSchemaSimpleTypeContent content = xmlSchemaSimpleType.Content;
				if (content is XmlSchemaSimpleTypeUnion)
				{
					ThrowTypeCannotBeImportedException(typeName.Name, typeName.Namespace, System.SR.Format(System.SR.SimpleTypeUnionNotSupported));
				}
				else if (content is XmlSchemaSimpleTypeList)
				{
					dataContract = ImportFlagsEnum(typeName, (XmlSchemaSimpleTypeList)content, xmlSchemaSimpleType.Annotation);
				}
				else if (content is XmlSchemaSimpleTypeRestriction restriction)
				{
					if (CheckIfEnum(restriction))
					{
						dataContract = ImportEnum(typeName, restriction, isFlags: false, xmlSchemaSimpleType.Annotation);
					}
					else
					{
						dataContract = ImportSimpleTypeRestriction(typeName, restriction);
						if (dataContract.IsBuiltInDataContract && !isAnonymous)
						{
							_dataContractSet.InternalAdd(typeName, dataContract);
						}
					}
				}
			}
			else if (type is XmlSchemaComplexType xmlSchemaComplexType)
			{
				if (xmlSchemaComplexType.ContentModel == null)
				{
					CheckComplexType(typeName, xmlSchemaComplexType);
					dataContract = ImportType(typeName, xmlSchemaComplexType.Particle, xmlSchemaComplexType.Attributes, xmlSchemaComplexType.AnyAttribute, null, xmlSchemaComplexType.Annotation);
				}
				else
				{
					XmlSchemaContentModel contentModel = xmlSchemaComplexType.ContentModel;
					if (contentModel is XmlSchemaSimpleContent)
					{
						ThrowTypeCannotBeImportedException(typeName.Name, typeName.Namespace, System.SR.Format(System.SR.SimpleContentNotSupported));
					}
					else if (contentModel is XmlSchemaComplexContent xmlSchemaComplexContent)
					{
						if (xmlSchemaComplexContent.IsMixed)
						{
							ThrowTypeCannotBeImportedException(typeName.Name, typeName.Namespace, System.SR.Format(System.SR.MixedContentNotSupported));
						}
						if (xmlSchemaComplexContent.Content is XmlSchemaComplexContentExtension xmlSchemaComplexContentExtension)
						{
							dataContract = ImportType(typeName, xmlSchemaComplexContentExtension.Particle, xmlSchemaComplexContentExtension.Attributes, xmlSchemaComplexContentExtension.AnyAttribute, xmlSchemaComplexContentExtension.BaseTypeName, xmlSchemaComplexType.Annotation);
						}
						else if (xmlSchemaComplexContent.Content is XmlSchemaComplexContentRestriction xmlSchemaComplexContentRestriction)
						{
							XmlQualifiedName baseTypeName = xmlSchemaComplexContentRestriction.BaseTypeName;
							if (baseTypeName == SchemaExporter.AnytypeQualifiedName)
							{
								dataContract = ImportType(typeName, xmlSchemaComplexContentRestriction.Particle, xmlSchemaComplexContentRestriction.Attributes, xmlSchemaComplexContentRestriction.AnyAttribute, null, xmlSchemaComplexType.Annotation);
							}
							else
							{
								ThrowTypeCannotBeImportedException(typeName.Name, typeName.Namespace, System.SR.Format(System.SR.ComplexTypeRestrictionNotSupported));
							}
						}
					}
				}
			}
			if (dataContract == null)
			{
				ThrowTypeCannotBeImportedException(typeName.Name, typeName.Namespace, string.Empty);
			}
			if (type.QualifiedName != XmlQualifiedName.Empty)
			{
				ImportTopLevelElement(typeName);
			}
			ImportDataContractExtension(type, dataContract);
			ImportGenericInfo(type, dataContract);
			ImportKnownTypes(typeName);
			return dataContract;
		}
		catch (InvalidDataContractException ex)
		{
			ex2 = ex;
		}
		if (_importXmlDataType)
		{
			RemoveFailedContract(typeName);
			return ImportXmlDataType(typeName, type, isAnonymous);
		}
		if ((_dataContractSet.TryGetReferencedType(typeName, dataContract, out var type2) || (string.IsNullOrEmpty(type.Name) && _dataContractSet.TryGetReferencedType(ImportActualType(type.Annotation, typeName, typeName), dataContract, out type2))) && Globals.TypeOfIXmlSerializable.IsAssignableFrom(type2))
		{
			RemoveFailedContract(typeName);
			return ImportXmlDataType(typeName, type, isAnonymous);
		}
		XmlDataContract xmlDataContract = ImportSpecialXmlDataType(type, isAnonymous);
		if (xmlDataContract != null)
		{
			_dataContractSet.Remove(typeName);
			return xmlDataContract;
		}
		throw ex2;
	}

	[RequiresDynamicCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.")]
	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	private void RemoveFailedContract(XmlQualifiedName typeName)
	{
		ClassDataContract classDataContract = _dataContractSet.GetDataContract(typeName) as ClassDataContract;
		_dataContractSet.Remove(typeName);
		if (classDataContract != null)
		{
			for (ClassDataContract baseClassContract = classDataContract.BaseClassContract; baseClassContract != null; baseClassContract = baseClassContract.BaseClassContract)
			{
				baseClassContract.KnownDataContracts?.Remove(typeName);
			}
			_dataContractSet.KnownTypesForObject?.Remove(typeName);
		}
	}

	[RequiresDynamicCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.")]
	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	private bool CheckIfEnum(XmlSchemaSimpleTypeRestriction restriction)
	{
		foreach (XmlSchemaFacet facet in restriction.Facets)
		{
			if (!(facet is XmlSchemaEnumerationFacet))
			{
				return false;
			}
		}
		XmlQualifiedName stringQualifiedName = SchemaExporter.StringQualifiedName;
		if (restriction.BaseTypeName != XmlQualifiedName.Empty)
		{
			if (!(restriction.BaseTypeName == stringQualifiedName) || restriction.Facets.Count <= 0)
			{
				return ImportType(restriction.BaseTypeName) is EnumDataContract;
			}
			return true;
		}
		if (restriction.BaseType != null)
		{
			DataContract dataContract = ImportType(restriction.BaseType);
			if (!(dataContract.XmlName == stringQualifiedName))
			{
				return dataContract is EnumDataContract;
			}
			return true;
		}
		return false;
	}

	private static bool CheckIfCollection(XmlSchemaSequence rootSequence)
	{
		if (rootSequence.Items == null || rootSequence.Items.Count == 0)
		{
			return false;
		}
		RemoveOptionalUnknownSerializationElements(rootSequence.Items);
		if (rootSequence.Items.Count != 1)
		{
			return false;
		}
		XmlSchemaObject xmlSchemaObject = rootSequence.Items[0];
		if (xmlSchemaObject is XmlSchemaElement xmlSchemaElement)
		{
			if (!(xmlSchemaElement.MaxOccursString == "unbounded"))
			{
				return xmlSchemaElement.MaxOccurs > 1m;
			}
			return true;
		}
		return false;
	}

	private static bool CheckIfISerializable(XmlSchemaSequence rootSequence, XmlSchemaObjectCollection attributes)
	{
		if (rootSequence.Items == null || rootSequence.Items.Count == 0)
		{
			return false;
		}
		RemoveOptionalUnknownSerializationElements(rootSequence.Items);
		if (attributes == null || attributes.Count == 0)
		{
			return false;
		}
		if (rootSequence.Items.Count == 1)
		{
			return rootSequence.Items[0] is XmlSchemaAny;
		}
		return false;
	}

	private static void RemoveOptionalUnknownSerializationElements(XmlSchemaObjectCollection items)
	{
		for (int i = 0; i < items.Count; i++)
		{
			if (!(items[i] is XmlSchemaElement xmlSchemaElement) || !(xmlSchemaElement.RefName != null) || !(xmlSchemaElement.RefName.Namespace == "http://schemas.microsoft.com/2003/10/Serialization/") || !(xmlSchemaElement.MinOccurs == 0m))
			{
				continue;
			}
			if (s_serializationSchemaElements == null)
			{
				XmlSchema xmlSchema = XmlSchema.Read(XmlReader.Create(new StringReader("<?xml version='1.0' encoding='utf-8'?>\r\n<xs:schema elementFormDefault='qualified' attributeFormDefault='qualified' xmlns:tns='http://schemas.microsoft.com/2003/10/Serialization/' targetNamespace='http://schemas.microsoft.com/2003/10/Serialization/' xmlns:xs='http://www.w3.org/2001/XMLSchema'>\r\n  <xs:element name='anyType' nillable='true' type='xs:anyType' />\r\n  <xs:element name='anyURI' nillable='true' type='xs:anyURI' />\r\n  <xs:element name='base64Binary' nillable='true' type='xs:base64Binary' />\r\n  <xs:element name='boolean' nillable='true' type='xs:boolean' />\r\n  <xs:element name='byte' nillable='true' type='xs:byte' />\r\n  <xs:element name='dateTime' nillable='true' type='xs:dateTime' />\r\n  <xs:element name='decimal' nillable='true' type='xs:decimal' />\r\n  <xs:element name='double' nillable='true' type='xs:double' />\r\n  <xs:element name='float' nillable='true' type='xs:float' />\r\n  <xs:element name='int' nillable='true' type='xs:int' />\r\n  <xs:element name='long' nillable='true' type='xs:long' />\r\n  <xs:element name='QName' nillable='true' type='xs:QName' />\r\n  <xs:element name='short' nillable='true' type='xs:short' />\r\n  <xs:element name='string' nillable='true' type='xs:string' />\r\n  <xs:element name='unsignedByte' nillable='true' type='xs:unsignedByte' />\r\n  <xs:element name='unsignedInt' nillable='true' type='xs:unsignedInt' />\r\n  <xs:element name='unsignedLong' nillable='true' type='xs:unsignedLong' />\r\n  <xs:element name='unsignedShort' nillable='true' type='xs:unsignedShort' />\r\n  <xs:element name='char' nillable='true' type='tns:char' />\r\n  <xs:simpleType name='char'>\r\n    <xs:restriction base='xs:int'/>\r\n  </xs:simpleType>\r\n  <xs:element name='duration' nillable='true' type='tns:duration' />\r\n  <xs:simpleType name='duration'>\r\n    <xs:restriction base='xs:duration'>\r\n      <xs:pattern value='\\-?P(\\d*D)?(T(\\d*H)?(\\d*M)?(\\d*(\\.\\d*)?S)?)?' />\r\n      <xs:minInclusive value='-P10675199DT2H48M5.4775808S' />\r\n      <xs:maxInclusive value='P10675199DT2H48M5.4775807S' />\r\n    </xs:restriction>\r\n  </xs:simpleType>\r\n  <xs:element name='guid' nillable='true' type='tns:guid' />\r\n  <xs:simpleType name='guid'>\r\n    <xs:restriction base='xs:string'>\r\n      <xs:pattern value='[\\da-fA-F]{8}-[\\da-fA-F]{4}-[\\da-fA-F]{4}-[\\da-fA-F]{4}-[\\da-fA-F]{12}' />\r\n    </xs:restriction>\r\n  </xs:simpleType>\r\n  <xs:attribute name='FactoryType' type='xs:QName' />\r\n  <xs:attribute name='Id' type='xs:ID' />\r\n  <xs:attribute name='Ref' type='xs:IDREF' />\r\n</xs:schema>\r\n")), null);
				s_serializationSchemaElements = new Hashtable();
				foreach (XmlSchemaObject item in xmlSchema.Items)
				{
					if (item is XmlSchemaElement { Name: not null } xmlSchemaElement2)
					{
						s_serializationSchemaElements.Add(xmlSchemaElement2.Name, xmlSchemaElement2);
					}
				}
			}
			if (!s_serializationSchemaElements.ContainsKey(xmlSchemaElement.RefName.Name))
			{
				items.RemoveAt(i);
				i--;
			}
		}
	}

	[RequiresDynamicCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.")]
	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	private DataContract ImportType(XmlQualifiedName typeName, XmlSchemaParticle rootParticle, XmlSchemaObjectCollection attributes, XmlSchemaAnyAttribute anyAttribute, XmlQualifiedName baseTypeName, XmlSchemaAnnotation annotation)
	{
		DataContract result = null;
		bool flag = baseTypeName != null;
		ImportAttributes(typeName, attributes, anyAttribute, out var isReference);
		if (rootParticle == null)
		{
			result = ImportClass(typeName, new XmlSchemaSequence(), baseTypeName, annotation, isReference);
		}
		else if (rootParticle is XmlSchemaSequence xmlSchemaSequence)
		{
			if (xmlSchemaSequence.MinOccurs != 1m)
			{
				ThrowTypeCannotBeImportedException(typeName.Name, typeName.Namespace, System.SR.Format(System.SR.RootSequenceMustBeRequired));
			}
			if (xmlSchemaSequence.MaxOccurs != 1m)
			{
				ThrowTypeCannotBeImportedException(typeName.Name, typeName.Namespace, System.SR.Format(System.SR.RootSequenceMaxOccursMustBe));
			}
			result = ((!flag && CheckIfCollection(xmlSchemaSequence)) ? ((DataContract)ImportCollection(typeName, xmlSchemaSequence, annotation, isReference)) : ((DataContract)((!CheckIfISerializable(xmlSchemaSequence, attributes)) ? ImportClass(typeName, xmlSchemaSequence, baseTypeName, annotation, isReference) : ImportISerializable(typeName, xmlSchemaSequence, baseTypeName, attributes, annotation))));
		}
		else
		{
			ThrowTypeCannotBeImportedException(typeName.Name, typeName.Namespace, System.SR.Format(System.SR.RootParticleMustBeSequence));
		}
		return result;
	}

	[RequiresDynamicCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.")]
	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	private ClassDataContract ImportClass(XmlQualifiedName typeName, XmlSchemaSequence rootSequence, XmlQualifiedName baseTypeName, XmlSchemaAnnotation annotation, bool isReference)
	{
		ClassDataContract classDataContract = new ClassDataContract(Globals.TypeOfSchemaDefinedType);
		classDataContract.XmlName = typeName;
		AddDataContract(classDataContract);
		classDataContract.IsValueType = IsValueType(typeName, annotation);
		classDataContract.IsReference = isReference;
		if (baseTypeName != null)
		{
			ImportBaseContract(baseTypeName, classDataContract);
			if (classDataContract.BaseClassContract.IsISerializable)
			{
				if (IsISerializableDerived(rootSequence))
				{
					classDataContract.IsISerializable = true;
				}
				else
				{
					ThrowTypeCannotBeImportedException(classDataContract.XmlName.Name, classDataContract.XmlName.Namespace, System.SR.Format(System.SR.DerivedTypeNotISerializable, baseTypeName.Name, baseTypeName.Namespace));
				}
			}
			if (classDataContract.BaseClassContract.IsReference)
			{
				classDataContract.IsReference = true;
			}
		}
		if (!classDataContract.IsISerializable)
		{
			classDataContract.Members = new List<DataMember>();
			RemoveOptionalUnknownSerializationElements(rootSequence.Items);
			for (int i = 0; i < rootSequence.Items.Count; i++)
			{
				XmlSchemaElement xmlSchemaElement = rootSequence.Items[i] as XmlSchemaElement;
				if (xmlSchemaElement == null)
				{
					ThrowTypeCannotBeImportedException(typeName.Name, typeName.Namespace, System.SR.Format(System.SR.MustContainOnlyLocalElements));
				}
				ImportClassMember(xmlSchemaElement, classDataContract);
			}
		}
		return classDataContract;
	}

	[RequiresDynamicCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.")]
	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	private DataContract ImportXmlDataType(XmlQualifiedName typeName, XmlSchemaType xsdType, bool isAnonymous)
	{
		DataContract dataContract = _dataContractSet.GetDataContract(typeName);
		if (dataContract != null)
		{
			return dataContract;
		}
		XmlDataContract xmlDataContract = ImportSpecialXmlDataType(xsdType, isAnonymous);
		if (xmlDataContract != null)
		{
			return xmlDataContract;
		}
		xmlDataContract = new XmlDataContract(Globals.TypeOfSchemaDefinedType);
		xmlDataContract.XmlName = typeName;
		xmlDataContract.IsValueType = false;
		AddDataContract(xmlDataContract);
		if (xsdType != null)
		{
			ImportDataContractExtension(xsdType, xmlDataContract);
			xmlDataContract.IsValueType = IsValueType(typeName, xsdType.Annotation);
			xmlDataContract.IsTypeDefinedOnImport = true;
			xmlDataContract.XsdType = (isAnonymous ? xsdType : null);
			xmlDataContract.HasRoot = !IsXmlAnyElementType(xsdType as XmlSchemaComplexType);
		}
		if (!isAnonymous)
		{
			xmlDataContract.SetTopLevelElementName(SchemaHelper.GetGlobalElementDeclaration(_schemaSet, typeName, out var isNullable));
			xmlDataContract.IsTopLevelElementNullable = isNullable;
		}
		return xmlDataContract;
	}

	[RequiresDynamicCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.")]
	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	private XmlDataContract ImportSpecialXmlDataType(XmlSchemaType xsdType, bool isAnonymous)
	{
		if (!isAnonymous)
		{
			return null;
		}
		if (!(xsdType is XmlSchemaComplexType xsdType2))
		{
			return null;
		}
		if (IsXmlAnyElementType(xsdType2))
		{
			XmlQualifiedName xmlName = new XmlQualifiedName("XElement", "http://schemas.datacontract.org/2004/07/System.Xml.Linq");
			if (_dataContractSet.TryGetReferencedType(xmlName, null, out var type) && Globals.TypeOfIXmlSerializable.IsAssignableFrom(type))
			{
				XmlDataContract xmlDataContract = new XmlDataContract(type);
				AddDataContract(xmlDataContract);
				return xmlDataContract;
			}
			return (XmlDataContract)DataContract.GetBuiltInDataContract(Globals.TypeOfXmlElement);
		}
		if (IsXmlAnyType(xsdType2))
		{
			return (XmlDataContract)DataContract.GetBuiltInDataContract(Globals.TypeOfXmlNodeArray);
		}
		return null;
	}

	private static bool IsXmlAnyElementType(XmlSchemaComplexType xsdType)
	{
		if (xsdType == null)
		{
			return false;
		}
		if (xsdType.Particle is XmlSchemaSequence xmlSchemaSequence)
		{
			if (xmlSchemaSequence.Items == null || xmlSchemaSequence.Items.Count != 1)
			{
				return false;
			}
			if (!(xmlSchemaSequence.Items[0] is XmlSchemaAny { Namespace: null }))
			{
				return false;
			}
			if (xsdType.AnyAttribute != null || (xsdType.Attributes != null && xsdType.Attributes.Count > 0))
			{
				return false;
			}
			return true;
		}
		return false;
	}

	private static bool IsXmlAnyType(XmlSchemaComplexType xsdType)
	{
		if (xsdType == null)
		{
			return false;
		}
		if (xsdType.Particle is XmlSchemaSequence xmlSchemaSequence)
		{
			if (xmlSchemaSequence.Items == null || xmlSchemaSequence.Items.Count != 1)
			{
				return false;
			}
			if (!(xmlSchemaSequence.Items[0] is XmlSchemaAny { Namespace: null } xmlSchemaAny))
			{
				return false;
			}
			if (xmlSchemaAny.MaxOccurs != decimal.MaxValue)
			{
				return false;
			}
			if (xsdType.AnyAttribute == null || xsdType.Attributes.Count > 0)
			{
				return false;
			}
			return true;
		}
		return false;
	}

	private static bool IsValueType(XmlQualifiedName typeName, XmlSchemaAnnotation annotation)
	{
		string innerText = GetInnerText(typeName, ImportAnnotation(annotation, SchemaExporter.IsValueTypeName));
		if (innerText != null)
		{
			try
			{
				return XmlConvert.ToBoolean(innerText);
			}
			catch (FormatException ex)
			{
				ThrowTypeCannotBeImportedException(typeName.Name, typeName.Namespace, System.SR.Format(System.SR.IsValueTypeFormattedIncorrectly, innerText, ex.Message));
			}
		}
		return false;
	}

	[RequiresDynamicCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.")]
	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	private ClassDataContract ImportISerializable(XmlQualifiedName typeName, XmlSchemaSequence rootSequence, XmlQualifiedName baseTypeName, XmlSchemaObjectCollection attributes, XmlSchemaAnnotation annotation)
	{
		ClassDataContract classDataContract = new ClassDataContract(Globals.TypeOfSchemaDefinedType);
		classDataContract.XmlName = typeName;
		classDataContract.IsISerializable = true;
		AddDataContract(classDataContract);
		classDataContract.IsValueType = IsValueType(typeName, annotation);
		if (baseTypeName == null)
		{
			CheckISerializableBase(typeName, rootSequence, attributes);
		}
		else
		{
			ImportBaseContract(baseTypeName, classDataContract);
			if (!classDataContract.BaseClassContract.IsISerializable)
			{
				ThrowISerializableTypeCannotBeImportedException(classDataContract.XmlName.Name, classDataContract.XmlName.Namespace, System.SR.Format(System.SR.BaseTypeNotISerializable, baseTypeName.Name, baseTypeName.Namespace));
			}
			if (!IsISerializableDerived(rootSequence))
			{
				ThrowISerializableTypeCannotBeImportedException(typeName.Name, typeName.Namespace, System.SR.Format(System.SR.ISerializableDerivedContainsOneOrMoreItems));
			}
		}
		return classDataContract;
	}

	private static void CheckISerializableBase(XmlQualifiedName typeName, XmlSchemaSequence rootSequence, XmlSchemaObjectCollection attributes)
	{
		if (rootSequence == null)
		{
			ThrowISerializableTypeCannotBeImportedException(typeName.Name, typeName.Namespace, System.SR.Format(System.SR.ISerializableDoesNotContainAny));
		}
		if (rootSequence.Items == null || rootSequence.Items.Count < 1)
		{
			ThrowISerializableTypeCannotBeImportedException(typeName.Name, typeName.Namespace, System.SR.Format(System.SR.ISerializableDoesNotContainAny));
		}
		else if (rootSequence.Items.Count > 1)
		{
			ThrowISerializableTypeCannotBeImportedException(typeName.Name, typeName.Namespace, System.SR.Format(System.SR.ISerializableContainsMoreThanOneItems));
		}
		XmlSchemaObject xmlSchemaObject = rootSequence.Items[0];
		if (!(xmlSchemaObject is XmlSchemaAny))
		{
			ThrowISerializableTypeCannotBeImportedException(typeName.Name, typeName.Namespace, System.SR.Format(System.SR.ISerializableDoesNotContainAny));
		}
		XmlSchemaAny xmlSchemaAny = (XmlSchemaAny)xmlSchemaObject;
		XmlSchemaAny iSerializableWildcardElement = SchemaExporter.ISerializableWildcardElement;
		if (xmlSchemaAny.MinOccurs != iSerializableWildcardElement.MinOccurs)
		{
			ThrowISerializableTypeCannotBeImportedException(typeName.Name, typeName.Namespace, System.SR.Format(System.SR.ISerializableWildcardMinOccursMustBe, iSerializableWildcardElement.MinOccurs));
		}
		if (xmlSchemaAny.MaxOccursString != iSerializableWildcardElement.MaxOccursString)
		{
			ThrowISerializableTypeCannotBeImportedException(typeName.Name, typeName.Namespace, System.SR.Format(System.SR.ISerializableWildcardMaxOccursMustBe, iSerializableWildcardElement.MaxOccursString));
		}
		if (xmlSchemaAny.Namespace != iSerializableWildcardElement.Namespace)
		{
			ThrowISerializableTypeCannotBeImportedException(typeName.Name, typeName.Namespace, System.SR.Format(System.SR.ISerializableWildcardNamespaceInvalid, iSerializableWildcardElement.Namespace));
		}
		if (xmlSchemaAny.ProcessContents != iSerializableWildcardElement.ProcessContents)
		{
			ThrowISerializableTypeCannotBeImportedException(typeName.Name, typeName.Namespace, System.SR.Format(System.SR.ISerializableWildcardProcessContentsInvalid, iSerializableWildcardElement.ProcessContents));
		}
		XmlQualifiedName refName = SchemaExporter.ISerializableFactoryTypeAttribute.RefName;
		bool flag = false;
		if (attributes != null)
		{
			for (int i = 0; i < attributes.Count; i++)
			{
				xmlSchemaObject = attributes[i];
				if (xmlSchemaObject is XmlSchemaAttribute && ((XmlSchemaAttribute)xmlSchemaObject).RefName == refName)
				{
					flag = true;
					break;
				}
			}
		}
		if (!flag)
		{
			ThrowISerializableTypeCannotBeImportedException(typeName.Name, typeName.Namespace, System.SR.Format(System.SR.ISerializableMustRefFactoryTypeAttribute, refName.Name, refName.Namespace));
		}
	}

	private static bool IsISerializableDerived(XmlSchemaSequence rootSequence)
	{
		if (rootSequence != null && rootSequence.Items != null)
		{
			return rootSequence.Items.Count == 0;
		}
		return true;
	}

	[RequiresDynamicCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.")]
	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	private void ImportBaseContract(XmlQualifiedName baseTypeName, ClassDataContract dataContract)
	{
		ClassDataContract classDataContract = ImportType(baseTypeName) as ClassDataContract;
		if (classDataContract == null)
		{
			ThrowTypeCannotBeImportedException(dataContract.XmlName.Name, dataContract.XmlName.Namespace, System.SR.Format(dataContract.IsISerializable ? System.SR.InvalidISerializableDerivation : System.SR.InvalidClassDerivation, baseTypeName.Name, baseTypeName.Namespace));
		}
		if (classDataContract.IsValueType)
		{
			classDataContract.IsValueType = false;
		}
		for (ClassDataContract classDataContract2 = classDataContract; classDataContract2 != null; classDataContract2 = classDataContract2.BaseClassContract)
		{
			Dictionary<XmlQualifiedName, DataContract> knownDataContracts = classDataContract2.KnownDataContracts;
			knownDataContracts.Add(dataContract.XmlName, dataContract);
		}
		dataContract.BaseClassContract = classDataContract;
	}

	private void ImportTopLevelElement(XmlQualifiedName typeName)
	{
		XmlSchemaElement schemaElement = SchemaHelper.GetSchemaElement(SchemaObjects, typeName);
		if (schemaElement == null)
		{
			return;
		}
		XmlQualifiedName xmlQualifiedName = schemaElement.SchemaTypeName;
		if (xmlQualifiedName.IsEmpty)
		{
			if (schemaElement.SchemaType != null)
			{
				ThrowTypeCannotBeImportedException(typeName.Name, typeName.Namespace, System.SR.Format(System.SR.AnonymousTypeNotSupported, typeName.Name, typeName.Namespace));
			}
			else
			{
				xmlQualifiedName = SchemaExporter.AnytypeQualifiedName;
			}
		}
		if (xmlQualifiedName != typeName)
		{
			ThrowTypeCannotBeImportedException(typeName.Name, typeName.Namespace, System.SR.Format(System.SR.TopLevelElementRepresentsDifferentType, schemaElement.SchemaTypeName.Name, schemaElement.SchemaTypeName.Namespace));
		}
		CheckIfElementUsesUnsupportedConstructs(typeName, schemaElement);
	}

	[RequiresDynamicCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.")]
	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	private void ImportClassMember(XmlSchemaElement element, ClassDataContract dataContract)
	{
		XmlQualifiedName xmlName = dataContract.XmlName;
		if (element.MinOccurs > 1m)
		{
			ThrowTypeCannotBeImportedException(xmlName.Name, xmlName.Namespace, System.SR.Format(System.SR.ElementMinOccursMustBe, element.Name));
		}
		if (element.MaxOccurs != 1m)
		{
			ThrowTypeCannotBeImportedException(xmlName.Name, xmlName.Namespace, System.SR.Format(System.SR.ElementMaxOccursMustBe, element.Name));
		}
		DataContract dataContract2 = null;
		string name = element.Name;
		bool isRequired = element.MinOccurs > 0m;
		bool isNillable = element.IsNillable;
		int num = 0;
		XmlSchemaForm xmlSchemaForm = element.Form;
		if (xmlSchemaForm == XmlSchemaForm.None)
		{
			XmlSchema schemaWithType = SchemaHelper.GetSchemaWithType(SchemaObjects, _schemaSet, xmlName);
			if (schemaWithType != null)
			{
				xmlSchemaForm = schemaWithType.ElementFormDefault;
			}
		}
		if (xmlSchemaForm != XmlSchemaForm.Qualified)
		{
			ThrowTypeCannotBeImportedException(xmlName.Name, xmlName.Namespace, System.SR.Format(System.SR.FormMustBeQualified, element.Name));
		}
		CheckIfElementUsesUnsupportedConstructs(xmlName, element);
		if (element.SchemaTypeName.IsEmpty)
		{
			if (element.SchemaType != null)
			{
				dataContract2 = ImportAnonymousElement(element, new XmlQualifiedName(string.Format(CultureInfo.InvariantCulture, "{0}.{1}Type", xmlName.Name, element.Name), xmlName.Namespace));
			}
			else if (!element.RefName.IsEmpty)
			{
				ThrowTypeCannotBeImportedException(xmlName.Name, xmlName.Namespace, System.SR.Format(System.SR.ElementRefOnLocalElementNotSupported, element.RefName.Name, element.RefName.Namespace));
			}
			else
			{
				dataContract2 = ImportType(SchemaExporter.AnytypeQualifiedName);
			}
		}
		else
		{
			XmlQualifiedName typeName = ImportActualType(element.Annotation, element.SchemaTypeName, xmlName);
			dataContract2 = ImportType(typeName);
			if (IsObjectContract(dataContract2))
			{
				_needToImportKnownTypesForObject = true;
			}
		}
		bool? flag = ImportEmitDefaultValue(element.Annotation, xmlName);
		bool emitDefaultValue;
		if (!dataContract2.IsValueType && !isNillable)
		{
			if (flag.HasValue && flag.Value)
			{
				throw new InvalidDataContractException(System.SR.Format(System.SR.InvalidEmitDefaultAnnotation, name, xmlName.Name, xmlName.Namespace));
			}
			emitDefaultValue = false;
		}
		else
		{
			emitDefaultValue = !flag.HasValue || flag.Value;
		}
		int num2 = dataContract.Members.Count - 1;
		if (num2 >= 0)
		{
			DataMember dataMember = dataContract.Members[num2];
			if (dataMember.Order > 0)
			{
				num = dataContract.Members.Count;
			}
			DataMember y = new DataMember(dataContract2, name, isNillable, isRequired, emitDefaultValue, num);
			int num3 = ClassDataContract.DataMemberComparer.Singleton.Compare(dataMember, y);
			if (num3 == 0)
			{
				ThrowTypeCannotBeImportedException(xmlName.Name, xmlName.Namespace, System.SR.Format(System.SR.CannotHaveDuplicateElementNames, name));
			}
			else if (num3 > 0)
			{
				num = dataContract.Members.Count;
			}
		}
		DataMember dataMember2 = new DataMember(dataContract2, name, isNillable, isRequired, emitDefaultValue, num);
		XmlQualifiedName surrogateDataAnnotationName = SchemaExporter.SurrogateDataAnnotationName;
		_dataContractSet.SetSurrogateData(dataMember2, ImportSurrogateData(ImportAnnotation(element.Annotation, surrogateDataAnnotationName), surrogateDataAnnotationName.Name, surrogateDataAnnotationName.Namespace));
		dataContract.Members.Add(dataMember2);
	}

	private static bool? ImportEmitDefaultValue(XmlSchemaAnnotation annotation, XmlQualifiedName typeName)
	{
		XmlElement xmlElement = ImportAnnotation(annotation, SchemaExporter.DefaultValueAnnotation);
		if (xmlElement == null)
		{
			return null;
		}
		XmlNode namedItem = xmlElement.Attributes.GetNamedItem("EmitDefaultValue");
		if (namedItem == null || namedItem.Value == null)
		{
			throw new InvalidDataContractException(System.SR.Format(System.SR.AnnotationAttributeNotFound, SchemaExporter.DefaultValueAnnotation.Name, typeName.Name, typeName.Namespace, "EmitDefaultValue"));
		}
		return XmlConvert.ToBoolean(namedItem.Value);
	}

	internal static XmlQualifiedName ImportActualType(XmlSchemaAnnotation annotation, XmlQualifiedName defaultTypeName, XmlQualifiedName typeName)
	{
		XmlElement xmlElement = ImportAnnotation(annotation, SchemaExporter.ActualTypeAnnotationName);
		if (xmlElement == null)
		{
			return defaultTypeName;
		}
		XmlNode namedItem = xmlElement.Attributes.GetNamedItem("Name");
		if (namedItem == null || namedItem.Value == null)
		{
			throw new InvalidDataContractException(System.SR.Format(System.SR.AnnotationAttributeNotFound, SchemaExporter.ActualTypeAnnotationName.Name, typeName.Name, typeName.Namespace, "Name"));
		}
		XmlNode namedItem2 = xmlElement.Attributes.GetNamedItem("Namespace");
		if (namedItem2 == null || namedItem2.Value == null)
		{
			throw new InvalidDataContractException(System.SR.Format(System.SR.AnnotationAttributeNotFound, SchemaExporter.ActualTypeAnnotationName.Name, typeName.Name, typeName.Namespace, "Namespace"));
		}
		return new XmlQualifiedName(namedItem.Value, namedItem2.Value);
	}

	[RequiresDynamicCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.")]
	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	private CollectionDataContract ImportCollection(XmlQualifiedName typeName, XmlSchemaSequence rootSequence, XmlSchemaAnnotation annotation, bool isReference)
	{
		CollectionDataContract collectionDataContract = new CollectionDataContract(Globals.TypeOfSchemaDefinedType, CollectionKind.Array);
		collectionDataContract.XmlName = typeName;
		AddDataContract(collectionDataContract);
		collectionDataContract.IsReference = isReference;
		XmlSchemaElement xmlSchemaElement = (XmlSchemaElement)rootSequence.Items[0];
		collectionDataContract.IsItemTypeNullable = xmlSchemaElement.IsNillable;
		collectionDataContract.ItemName = xmlSchemaElement.Name;
		XmlSchemaForm xmlSchemaForm = xmlSchemaElement.Form;
		if (xmlSchemaForm == XmlSchemaForm.None)
		{
			XmlSchema schemaWithType = SchemaHelper.GetSchemaWithType(SchemaObjects, _schemaSet, typeName);
			if (schemaWithType != null)
			{
				xmlSchemaForm = schemaWithType.ElementFormDefault;
			}
		}
		if (xmlSchemaForm != XmlSchemaForm.Qualified)
		{
			ThrowArrayTypeCannotBeImportedException(typeName.Name, typeName.Namespace, System.SR.Format(System.SR.ArrayItemFormMustBe, xmlSchemaElement.Name));
		}
		CheckIfElementUsesUnsupportedConstructs(typeName, xmlSchemaElement);
		if (xmlSchemaElement.SchemaTypeName.IsEmpty)
		{
			if (xmlSchemaElement.SchemaType != null)
			{
				XmlQualifiedName xmlQualifiedName = new XmlQualifiedName(xmlSchemaElement.Name, typeName.Namespace);
				DataContract dataContract = _dataContractSet.GetDataContract(xmlQualifiedName);
				if (dataContract == null)
				{
					collectionDataContract.ItemContract = ImportAnonymousElement(xmlSchemaElement, xmlQualifiedName);
				}
				else
				{
					XmlQualifiedName typeQName = new XmlQualifiedName(string.Format(CultureInfo.InvariantCulture, "{0}.{1}Type", typeName.Name, xmlSchemaElement.Name), typeName.Namespace);
					collectionDataContract.ItemContract = ImportAnonymousElement(xmlSchemaElement, typeQName);
				}
			}
			else if (!xmlSchemaElement.RefName.IsEmpty)
			{
				ThrowArrayTypeCannotBeImportedException(typeName.Name, typeName.Namespace, System.SR.Format(System.SR.ElementRefOnLocalElementNotSupported, xmlSchemaElement.RefName.Name, xmlSchemaElement.RefName.Namespace));
			}
			else
			{
				collectionDataContract.ItemContract = ImportType(SchemaExporter.AnytypeQualifiedName);
			}
		}
		else
		{
			collectionDataContract.ItemContract = ImportType(xmlSchemaElement.SchemaTypeName);
		}
		if (IsDictionary(typeName, annotation))
		{
			ClassDataContract classDataContract = collectionDataContract.ItemContract as ClassDataContract;
			DataMember dataMember = null;
			DataMember dataMember2 = null;
			if (classDataContract == null || classDataContract.Members == null || classDataContract.Members.Count != 2 || !(dataMember = classDataContract.Members[0]).IsRequired || !(dataMember2 = classDataContract.Members[1]).IsRequired)
			{
				ThrowArrayTypeCannotBeImportedException(typeName.Name, typeName.Namespace, System.SR.Format(System.SR.InvalidKeyValueType, xmlSchemaElement.Name));
			}
			if (classDataContract.Namespace != collectionDataContract.Namespace)
			{
				ThrowArrayTypeCannotBeImportedException(typeName.Name, typeName.Namespace, System.SR.Format(System.SR.InvalidKeyValueTypeNamespace, xmlSchemaElement.Name, classDataContract.Namespace));
			}
			classDataContract.IsValueType = true;
			collectionDataContract.KeyName = dataMember.Name;
			collectionDataContract.ValueName = dataMember2.Name;
			if (xmlSchemaElement.SchemaType != null)
			{
				_dataContractSet.Remove(classDataContract.XmlName);
				GenericInfo genericInfo = new GenericInfo(DataContract.GetXmlName(Globals.TypeOfKeyValue), Globals.TypeOfKeyValue.FullName);
				genericInfo.Add(GetGenericInfoForDataMember(dataMember));
				genericInfo.Add(GetGenericInfoForDataMember(dataMember2));
				genericInfo.AddToLevel(0, 2);
				collectionDataContract.ItemContract.XmlName = new XmlQualifiedName(genericInfo.GetExpandedXmlName().Name, typeName.Namespace);
			}
		}
		return collectionDataContract;
	}

	[RequiresDynamicCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.")]
	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	private static GenericInfo GetGenericInfoForDataMember(DataMember dataMember)
	{
		GenericInfo genericInfo;
		if (dataMember.MemberTypeContract.IsValueType && dataMember.IsNullable)
		{
			genericInfo = new GenericInfo(DataContract.GetXmlName(Globals.TypeOfNullable), Globals.TypeOfNullable.FullName);
			genericInfo.Add(new GenericInfo(dataMember.MemberTypeContract.XmlName, null));
		}
		else
		{
			genericInfo = new GenericInfo(dataMember.MemberTypeContract.XmlName, null);
		}
		return genericInfo;
	}

	private static bool IsDictionary(XmlQualifiedName typeName, XmlSchemaAnnotation annotation)
	{
		string innerText = GetInnerText(typeName, ImportAnnotation(annotation, SchemaExporter.IsDictionaryAnnotationName));
		if (innerText != null)
		{
			try
			{
				return XmlConvert.ToBoolean(innerText);
			}
			catch (FormatException ex)
			{
				ThrowTypeCannotBeImportedException(typeName.Name, typeName.Namespace, System.SR.Format(System.SR.IsDictionaryFormattedIncorrectly, innerText, ex.Message));
			}
		}
		return false;
	}

	[RequiresDynamicCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.")]
	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	private EnumDataContract ImportFlagsEnum(XmlQualifiedName typeName, XmlSchemaSimpleTypeList list, XmlSchemaAnnotation annotation)
	{
		XmlSchemaSimpleType itemType = list.ItemType;
		if (itemType == null)
		{
			ThrowEnumTypeCannotBeImportedException(typeName.Name, typeName.Namespace, System.SR.Format(System.SR.EnumListMustContainAnonymousType));
		}
		XmlSchemaSimpleTypeContent content = itemType.Content;
		if (content is XmlSchemaSimpleTypeUnion)
		{
			ThrowEnumTypeCannotBeImportedException(typeName.Name, typeName.Namespace, System.SR.Format(System.SR.EnumUnionInAnonymousTypeNotSupported));
		}
		else if (content is XmlSchemaSimpleTypeList)
		{
			ThrowEnumTypeCannotBeImportedException(typeName.Name, typeName.Namespace, System.SR.Format(System.SR.EnumListInAnonymousTypeNotSupported));
		}
		else if (content is XmlSchemaSimpleTypeRestriction)
		{
			if (content is XmlSchemaSimpleTypeRestriction restriction && CheckIfEnum(restriction))
			{
				return ImportEnum(typeName, restriction, isFlags: true, annotation);
			}
			ThrowEnumTypeCannotBeImportedException(typeName.Name, typeName.Namespace, System.SR.Format(System.SR.EnumRestrictionInvalid));
		}
		return null;
	}

	[RequiresDynamicCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.")]
	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	private EnumDataContract ImportEnum(XmlQualifiedName typeName, XmlSchemaSimpleTypeRestriction restriction, bool isFlags, XmlSchemaAnnotation annotation)
	{
		EnumDataContract enumDataContract = new EnumDataContract(Globals.TypeOfSchemaDefinedEnum);
		enumDataContract.XmlName = typeName;
		enumDataContract.BaseContractName = ImportActualType(annotation, SchemaExporter.DefaultEnumBaseTypeName, typeName);
		enumDataContract.IsFlags = isFlags;
		AddDataContract(enumDataContract);
		enumDataContract.Values = new List<long>();
		enumDataContract.Members = new List<DataMember>();
		foreach (XmlSchemaFacet facet in restriction.Facets)
		{
			XmlSchemaEnumerationFacet xmlSchemaEnumerationFacet = facet as XmlSchemaEnumerationFacet;
			if (xmlSchemaEnumerationFacet == null)
			{
				ThrowEnumTypeCannotBeImportedException(typeName.Name, typeName.Namespace, System.SR.Format(System.SR.EnumOnlyEnumerationFacetsSupported));
			}
			if (xmlSchemaEnumerationFacet.Value == null)
			{
				ThrowEnumTypeCannotBeImportedException(typeName.Name, typeName.Namespace, System.SR.Format(System.SR.EnumEnumerationFacetsMustHaveValue));
			}
			string innerText = GetInnerText(typeName, ImportAnnotation(xmlSchemaEnumerationFacet.Annotation, SchemaExporter.EnumerationValueAnnotationName));
			long num = ((innerText == null) ? SchemaExporter.GetDefaultEnumValue(isFlags, enumDataContract.Members.Count) : enumDataContract.GetEnumValueFromString(innerText));
			enumDataContract.Values.Add(num);
			DataMember item = new DataMember(Globals.SchemaMemberInfoPlaceholder)
			{
				Name = xmlSchemaEnumerationFacet.Value,
				Order = num
			};
			enumDataContract.Members.Add(item);
		}
		return enumDataContract;
	}

	[RequiresDynamicCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.")]
	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	private DataContract ImportSimpleTypeRestriction(XmlQualifiedName typeName, XmlSchemaSimpleTypeRestriction restriction)
	{
		DataContract result = null;
		if (!restriction.BaseTypeName.IsEmpty)
		{
			result = ImportType(restriction.BaseTypeName);
		}
		else if (restriction.BaseType != null)
		{
			result = ImportType(restriction.BaseType);
		}
		else
		{
			ThrowTypeCannotBeImportedException(typeName.Name, typeName.Namespace, System.SR.Format(System.SR.SimpleTypeRestrictionDoesNotSpecifyBase));
		}
		return result;
	}

	[RequiresDynamicCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.")]
	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	private void ImportDataContractExtension(XmlSchemaType type, DataContract dataContract)
	{
		if (type.Annotation == null || type.Annotation.Items == null)
		{
			return;
		}
		foreach (XmlSchemaObject item in type.Annotation.Items)
		{
			if (!(item is XmlSchemaAppInfo { Markup: not null } xmlSchemaAppInfo))
			{
				continue;
			}
			XmlNode[] markup = xmlSchemaAppInfo.Markup;
			foreach (XmlNode xmlNode in markup)
			{
				XmlElement xmlElement = xmlNode as XmlElement;
				XmlQualifiedName surrogateDataAnnotationName = SchemaExporter.SurrogateDataAnnotationName;
				if (xmlElement != null && xmlElement.NamespaceURI == surrogateDataAnnotationName.Namespace && xmlElement.LocalName == surrogateDataAnnotationName.Name)
				{
					object surrogateData = ImportSurrogateData(xmlElement, surrogateDataAnnotationName.Name, surrogateDataAnnotationName.Namespace);
					_dataContractSet.SetSurrogateData(dataContract, surrogateData);
				}
			}
		}
	}

	private static void ImportGenericInfo(XmlSchemaType type, DataContract dataContract)
	{
		if (type.Annotation == null || type.Annotation.Items == null)
		{
			return;
		}
		foreach (XmlSchemaObject item in type.Annotation.Items)
		{
			if (!(item is XmlSchemaAppInfo { Markup: not null } xmlSchemaAppInfo))
			{
				continue;
			}
			XmlNode[] markup = xmlSchemaAppInfo.Markup;
			foreach (XmlNode xmlNode in markup)
			{
				if (xmlNode is XmlElement { NamespaceURI: "http://schemas.microsoft.com/2003/10/Serialization/", LocalName: "GenericType" } xmlElement)
				{
					dataContract.GenericInfo = ImportGenericInfo(xmlElement, type);
				}
			}
		}
	}

	private static GenericInfo ImportGenericInfo(XmlElement typeElement, XmlSchemaType type)
	{
		string text = typeElement.Attributes.GetNamedItem("Name")?.Value;
		if (text == null)
		{
			throw new InvalidDataContractException(System.SR.Format(System.SR.GenericAnnotationAttributeNotFound, type.Name, "Name"));
		}
		string text2 = typeElement.Attributes.GetNamedItem("Namespace")?.Value;
		if (text2 == null)
		{
			throw new InvalidDataContractException(System.SR.Format(System.SR.GenericAnnotationAttributeNotFound, type.Name, "Namespace"));
		}
		if (typeElement.ChildNodes.Count > 0)
		{
			text = DataContract.EncodeLocalName(text);
		}
		int num = 0;
		GenericInfo genericInfo = new GenericInfo(new XmlQualifiedName(text, text2), type.Name);
		foreach (XmlNode childNode in typeElement.ChildNodes)
		{
			if (childNode is XmlElement xmlElement)
			{
				if (xmlElement.LocalName != "GenericParameter" || xmlElement.NamespaceURI != "http://schemas.microsoft.com/2003/10/Serialization/")
				{
					throw new InvalidDataContractException(System.SR.Format(System.SR.GenericAnnotationHasInvalidElement, xmlElement.LocalName, xmlElement.NamespaceURI, type.Name));
				}
				XmlNode namedItem = xmlElement.Attributes.GetNamedItem("NestedLevel");
				int result = 0;
				if (namedItem != null && !int.TryParse(namedItem.Value, out result))
				{
					throw new InvalidDataContractException(System.SR.Format(System.SR.GenericAnnotationHasInvalidAttributeValue, xmlElement.LocalName, xmlElement.NamespaceURI, type.Name, namedItem.Value, namedItem.LocalName, Globals.TypeOfInt.Name));
				}
				if (result < num)
				{
					throw new InvalidDataContractException(System.SR.Format(System.SR.GenericAnnotationForNestedLevelMustBeIncreasing, xmlElement.LocalName, xmlElement.NamespaceURI, type.Name));
				}
				genericInfo.Add(ImportGenericInfo(xmlElement, type));
				genericInfo.AddToLevel(result, 1);
				num = result;
			}
		}
		XmlNode namedItem2 = typeElement.Attributes.GetNamedItem("NestedLevel");
		if (namedItem2 != null)
		{
			if (!int.TryParse(namedItem2.Value, out var result2))
			{
				throw new InvalidDataContractException(System.SR.Format(System.SR.GenericAnnotationHasInvalidAttributeValue, typeElement.LocalName, typeElement.NamespaceURI, type.Name, namedItem2.Value, namedItem2.LocalName, Globals.TypeOfInt.Name));
			}
			if (result2 - 1 > num)
			{
				genericInfo.AddToLevel(result2 - 1, 0);
			}
		}
		return genericInfo;
	}

	[RequiresDynamicCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.")]
	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	private object ImportSurrogateData(XmlElement typeElement, string name, string ns)
	{
		if (_dataContractSet.SerializationExtendedSurrogateProvider != null && typeElement != null)
		{
			Collection<Type> collection = new Collection<Type>();
			DataContractSurrogateCaller.GetKnownCustomDataTypes(_dataContractSet.SerializationExtendedSurrogateProvider, collection);
			DataContractSerializer dataContractSerializer = new DataContractSerializer(Globals.TypeOfObject, name, ns, collection, ignoreExtensionDataObject: false, preserveObjectReferences: true);
			return dataContractSerializer.ReadObject(new XmlNodeReader(typeElement));
		}
		return null;
	}

	private static void CheckComplexType(XmlQualifiedName typeName, XmlSchemaComplexType type)
	{
		if (type.IsAbstract)
		{
			ThrowTypeCannotBeImportedException(typeName.Name, typeName.Namespace, System.SR.Format(System.SR.AbstractTypeNotSupported));
		}
		if (type.IsMixed)
		{
			ThrowTypeCannotBeImportedException(typeName.Name, typeName.Namespace, System.SR.Format(System.SR.MixedContentNotSupported));
		}
	}

	private static void CheckIfElementUsesUnsupportedConstructs(XmlQualifiedName typeName, XmlSchemaElement element)
	{
		if (element.IsAbstract)
		{
			ThrowTypeCannotBeImportedException(typeName.Name, typeName.Namespace, System.SR.Format(System.SR.AbstractElementNotSupported, element.Name));
		}
		if (element.DefaultValue != null)
		{
			ThrowTypeCannotBeImportedException(typeName.Name, typeName.Namespace, System.SR.Format(System.SR.DefaultOnElementNotSupported, element.Name));
		}
		if (element.FixedValue != null)
		{
			ThrowTypeCannotBeImportedException(typeName.Name, typeName.Namespace, System.SR.Format(System.SR.FixedOnElementNotSupported, element.Name));
		}
		if (!element.SubstitutionGroup.IsEmpty)
		{
			ThrowTypeCannotBeImportedException(typeName.Name, typeName.Namespace, System.SR.Format(System.SR.SubstitutionGroupOnElementNotSupported, element.Name));
		}
	}

	private static void ImportAttributes(XmlQualifiedName typeName, XmlSchemaObjectCollection attributes, XmlSchemaAnyAttribute anyAttribute, out bool isReference)
	{
		if (anyAttribute != null)
		{
			ThrowTypeCannotBeImportedException(typeName.Name, typeName.Namespace, System.SR.Format(System.SR.AnyAttributeNotSupported));
		}
		isReference = false;
		if (attributes == null)
		{
			return;
		}
		bool foundAttribute = false;
		bool foundAttribute2 = false;
		for (int i = 0; i < attributes.Count; i++)
		{
			XmlSchemaObject xmlSchemaObject = attributes[i];
			if (xmlSchemaObject is XmlSchemaAttribute { Use: not XmlSchemaUse.Prohibited } xmlSchemaAttribute && !TryCheckIfAttribute(typeName, xmlSchemaAttribute, Globals.IdQualifiedName, ref foundAttribute) && !TryCheckIfAttribute(typeName, xmlSchemaAttribute, Globals.RefQualifiedName, ref foundAttribute2) && (xmlSchemaAttribute.RefName.IsEmpty || xmlSchemaAttribute.RefName.Namespace != "http://schemas.microsoft.com/2003/10/Serialization/" || xmlSchemaAttribute.Use == XmlSchemaUse.Required))
			{
				ThrowTypeCannotBeImportedException(typeName.Name, typeName.Namespace, System.SR.Format(System.SR.TypeShouldNotContainAttributes, "http://schemas.microsoft.com/2003/10/Serialization/"));
			}
		}
		isReference = foundAttribute && foundAttribute2;
	}

	private static bool TryCheckIfAttribute(XmlQualifiedName typeName, XmlSchemaAttribute attribute, XmlQualifiedName refName, ref bool foundAttribute)
	{
		if (attribute.RefName != refName)
		{
			return false;
		}
		if (foundAttribute)
		{
			ThrowTypeCannotBeImportedException(typeName.Name, typeName.Namespace, System.SR.Format(System.SR.CannotHaveDuplicateAttributeNames, refName.Name));
		}
		foundAttribute = true;
		return true;
	}

	[RequiresDynamicCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.")]
	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	private void AddDataContract(DataContract dataContract)
	{
		_dataContractSet.Add(dataContract.XmlName, dataContract);
	}

	private static string GetInnerText(XmlQualifiedName typeName, XmlElement xmlElement)
	{
		if (xmlElement != null)
		{
			for (XmlNode xmlNode = xmlElement.FirstChild; xmlNode != null; xmlNode = xmlNode.NextSibling)
			{
				if (xmlNode.NodeType == XmlNodeType.Element)
				{
					ThrowTypeCannotBeImportedException(typeName.Name, typeName.Namespace, System.SR.Format(System.SR.InvalidAnnotationExpectingText, xmlElement.LocalName, xmlElement.NamespaceURI, xmlNode.LocalName, xmlNode.NamespaceURI));
				}
			}
			return xmlElement.InnerText;
		}
		return null;
	}

	private static XmlElement ImportAnnotation(XmlSchemaAnnotation annotation, XmlQualifiedName annotationQualifiedName)
	{
		if (annotation != null && annotation.Items != null && annotation.Items.Count > 0 && annotation.Items[0] is XmlSchemaAppInfo)
		{
			XmlSchemaAppInfo xmlSchemaAppInfo = (XmlSchemaAppInfo)annotation.Items[0];
			XmlNode[] markup = xmlSchemaAppInfo.Markup;
			if (markup != null)
			{
				for (int i = 0; i < markup.Length; i++)
				{
					if (markup[i] is XmlElement xmlElement && xmlElement.LocalName == annotationQualifiedName.Name && xmlElement.NamespaceURI == annotationQualifiedName.Namespace)
					{
						return xmlElement;
					}
				}
			}
		}
		return null;
	}

	[DoesNotReturn]
	private static void ThrowTypeCannotBeImportedException(string name, string ns, string message)
	{
		ThrowTypeCannotBeImportedException(System.SR.Format(System.SR.TypeCannotBeImported, name, ns, message));
	}

	[DoesNotReturn]
	private static void ThrowArrayTypeCannotBeImportedException(string name, string ns, string message)
	{
		ThrowTypeCannotBeImportedException(System.SR.Format(System.SR.ArrayTypeCannotBeImported, name, ns, message));
	}

	[DoesNotReturn]
	private static void ThrowEnumTypeCannotBeImportedException(string name, string ns, string message)
	{
		ThrowTypeCannotBeImportedException(System.SR.Format(System.SR.EnumTypeCannotBeImported, name, ns, message));
	}

	[DoesNotReturn]
	private static void ThrowISerializableTypeCannotBeImportedException(string name, string ns, string message)
	{
		ThrowTypeCannotBeImportedException(System.SR.Format(System.SR.ISerializableTypeCannotBeImported, name, ns, message));
	}

	[DoesNotReturn]
	private static void ThrowTypeCannotBeImportedException(string message)
	{
		throw new InvalidDataContractException(System.SR.Format(System.SR.TypeCannotBeImportedHowToFix, message));
	}
}
