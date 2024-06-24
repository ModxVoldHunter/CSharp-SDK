using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization.DataContracts;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace System.Runtime.Serialization;

internal sealed class SchemaExporter
{
	private readonly XmlSchemaSet _schemas;

	private XmlDocument _xmlDoc;

	private DataContractSet _dataContractSet;

	private static XmlQualifiedName s_anytypeQualifiedName;

	private static XmlQualifiedName s_stringQualifiedName;

	private static XmlQualifiedName s_defaultEnumBaseTypeName;

	private static XmlQualifiedName s_enumerationValueAnnotationName;

	private static XmlQualifiedName s_surrogateDataAnnotationName;

	private static XmlQualifiedName s_defaultValueAnnotation;

	private static XmlQualifiedName s_actualTypeAnnotationName;

	private static XmlQualifiedName s_isDictionaryAnnotationName;

	private static XmlQualifiedName s_isValueTypeName;

	private XmlSchemaSet Schemas => _schemas;

	private XmlDocument XmlDoc => _xmlDoc ?? (_xmlDoc = new XmlDocument());

	internal static XmlSchemaSequence ISerializableSequence
	{
		get
		{
			XmlSchemaSequence xmlSchemaSequence = new XmlSchemaSequence();
			xmlSchemaSequence.Items.Add(ISerializableWildcardElement);
			return xmlSchemaSequence;
		}
	}

	internal static XmlSchemaAny ISerializableWildcardElement
	{
		get
		{
			XmlSchemaAny xmlSchemaAny = new XmlSchemaAny();
			xmlSchemaAny.MinOccurs = 0m;
			xmlSchemaAny.MaxOccursString = "unbounded";
			xmlSchemaAny.Namespace = "##local";
			xmlSchemaAny.ProcessContents = XmlSchemaContentProcessing.Skip;
			return xmlSchemaAny;
		}
	}

	internal static XmlQualifiedName AnytypeQualifiedName => s_anytypeQualifiedName ?? (s_anytypeQualifiedName = new XmlQualifiedName("anyType", "http://www.w3.org/2001/XMLSchema"));

	internal static XmlQualifiedName StringQualifiedName => s_stringQualifiedName ?? (s_stringQualifiedName = new XmlQualifiedName("string", "http://www.w3.org/2001/XMLSchema"));

	internal static XmlQualifiedName DefaultEnumBaseTypeName => s_defaultEnumBaseTypeName ?? (s_defaultEnumBaseTypeName = new XmlQualifiedName("int", "http://www.w3.org/2001/XMLSchema"));

	internal static XmlQualifiedName EnumerationValueAnnotationName => s_enumerationValueAnnotationName ?? (s_enumerationValueAnnotationName = new XmlQualifiedName("EnumerationValue", "http://schemas.microsoft.com/2003/10/Serialization/"));

	internal static XmlQualifiedName SurrogateDataAnnotationName => s_surrogateDataAnnotationName ?? (s_surrogateDataAnnotationName = new XmlQualifiedName("Surrogate", "http://schemas.microsoft.com/2003/10/Serialization/"));

	internal static XmlQualifiedName DefaultValueAnnotation => s_defaultValueAnnotation ?? (s_defaultValueAnnotation = new XmlQualifiedName("DefaultValue", "http://schemas.microsoft.com/2003/10/Serialization/"));

	internal static XmlQualifiedName ActualTypeAnnotationName => s_actualTypeAnnotationName ?? (s_actualTypeAnnotationName = new XmlQualifiedName("ActualType", "http://schemas.microsoft.com/2003/10/Serialization/"));

	internal static XmlQualifiedName IsDictionaryAnnotationName => s_isDictionaryAnnotationName ?? (s_isDictionaryAnnotationName = new XmlQualifiedName("IsDictionary", "http://schemas.microsoft.com/2003/10/Serialization/"));

	internal static XmlQualifiedName IsValueTypeName => s_isValueTypeName ?? (s_isValueTypeName = new XmlQualifiedName("IsValueType", "http://schemas.microsoft.com/2003/10/Serialization/"));

	internal static XmlSchemaAttribute ISerializableFactoryTypeAttribute
	{
		get
		{
			XmlSchemaAttribute xmlSchemaAttribute = new XmlSchemaAttribute();
			xmlSchemaAttribute.RefName = new XmlQualifiedName("FactoryType", "http://schemas.microsoft.com/2003/10/Serialization/");
			return xmlSchemaAttribute;
		}
	}

	internal static XmlSchemaAttribute RefAttribute
	{
		get
		{
			XmlSchemaAttribute xmlSchemaAttribute = new XmlSchemaAttribute();
			xmlSchemaAttribute.RefName = Globals.RefQualifiedName;
			return xmlSchemaAttribute;
		}
	}

	internal static XmlSchemaAttribute IdAttribute
	{
		get
		{
			XmlSchemaAttribute xmlSchemaAttribute = new XmlSchemaAttribute();
			xmlSchemaAttribute.RefName = Globals.IdQualifiedName;
			return xmlSchemaAttribute;
		}
	}

	internal SchemaExporter(XmlSchemaSet schemas, DataContractSet dataContractSet)
	{
		_schemas = schemas;
		_dataContractSet = dataContractSet;
	}

	[RequiresDynamicCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.")]
	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	internal void Export()
	{
		try
		{
			ExportSerializationSchema();
			foreach (KeyValuePair<XmlQualifiedName, DataContract> contract in _dataContractSet.Contracts)
			{
				DataContract value = contract.Value;
				if (!_dataContractSet.IsContractProcessed(value))
				{
					ExportDataContract(value);
					_dataContractSet.SetContractProcessed(value);
				}
			}
		}
		finally
		{
			_xmlDoc = null;
			_dataContractSet = null;
		}
	}

	private void ExportSerializationSchema()
	{
		if (!Schemas.Contains("http://schemas.microsoft.com/2003/10/Serialization/"))
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
			Schemas.Add(xmlSchema);
		}
	}

	[RequiresDynamicCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.")]
	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	private void ExportDataContract(DataContract dataContract)
	{
		if (dataContract.IsBuiltInDataContract)
		{
			return;
		}
		if (dataContract is XmlDataContract)
		{
			ExportXmlDataContract((XmlDataContract)dataContract);
			return;
		}
		XmlSchema schema = GetSchema(dataContract.XmlName.Namespace);
		if (dataContract is ClassDataContract classDataContract)
		{
			if (classDataContract.IsISerializable)
			{
				ExportISerializableDataContract(classDataContract, schema);
			}
			else
			{
				ExportClassDataContract(classDataContract, schema);
			}
		}
		else if (dataContract is CollectionDataContract)
		{
			ExportCollectionDataContract((CollectionDataContract)dataContract, schema);
		}
		else if (dataContract is EnumDataContract)
		{
			ExportEnumDataContract((EnumDataContract)dataContract, schema);
		}
		ExportTopLevelElement(dataContract, schema);
		Schemas.Reprocess(schema);
	}

	private XmlSchemaElement ExportTopLevelElement(DataContract dataContract, XmlSchema schema)
	{
		if (schema == null || dataContract.XmlName.Namespace != dataContract.TopLevelElementNamespace.Value)
		{
			schema = GetSchema(dataContract.TopLevelElementNamespace.Value);
		}
		XmlSchemaElement xmlSchemaElement = new XmlSchemaElement();
		xmlSchemaElement.Name = dataContract.TopLevelElementName.Value;
		SetElementType(xmlSchemaElement, dataContract, schema);
		xmlSchemaElement.IsNillable = true;
		schema.Items.Add(xmlSchemaElement);
		return xmlSchemaElement;
	}

	[RequiresDynamicCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.")]
	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	private void ExportClassDataContract(ClassDataContract classDataContract, XmlSchema schema)
	{
		XmlSchemaComplexType xmlSchemaComplexType = new XmlSchemaComplexType();
		xmlSchemaComplexType.Name = classDataContract.XmlName.Name;
		schema.Items.Add(xmlSchemaComplexType);
		XmlElement xmlElement = null;
		if (classDataContract.UnderlyingType.IsGenericType)
		{
			xmlElement = ExportGenericInfo(classDataContract.UnderlyingType, "GenericType", "http://schemas.microsoft.com/2003/10/Serialization/");
		}
		XmlSchemaSequence xmlSchemaSequence = new XmlSchemaSequence();
		for (int i = 0; i < classDataContract.Members.Count; i++)
		{
			DataMember dataMember = classDataContract.Members[i];
			XmlSchemaElement xmlSchemaElement = new XmlSchemaElement();
			xmlSchemaElement.Name = dataMember.Name;
			XmlElement xmlElement2 = null;
			DataContract memberTypeDataContract = _dataContractSet.GetMemberTypeDataContract(dataMember);
			if (CheckIfMemberHasConflict(dataMember))
			{
				xmlSchemaElement.SchemaTypeName = AnytypeQualifiedName;
				xmlElement2 = ExportActualType(memberTypeDataContract.XmlName);
				SchemaHelper.AddSchemaImport(memberTypeDataContract.XmlName.Namespace, schema);
			}
			else
			{
				SetElementType(xmlSchemaElement, memberTypeDataContract, schema);
			}
			SchemaHelper.AddElementForm(xmlSchemaElement, schema);
			if (dataMember.IsNullable)
			{
				xmlSchemaElement.IsNillable = true;
			}
			if (!dataMember.IsRequired)
			{
				xmlSchemaElement.MinOccurs = 0m;
			}
			xmlSchemaElement.Annotation = GetSchemaAnnotation(xmlElement2, ExportSurrogateData(dataMember), ExportEmitDefaultValue(dataMember));
			xmlSchemaSequence.Items.Add(xmlSchemaElement);
		}
		XmlElement xmlElement3 = null;
		if (classDataContract.BaseClassContract != null)
		{
			XmlSchemaComplexContentExtension xmlSchemaComplexContentExtension = CreateTypeContent(xmlSchemaComplexType, classDataContract.BaseClassContract.XmlName, schema);
			xmlSchemaComplexContentExtension.Particle = xmlSchemaSequence;
			if (classDataContract.IsReference && !classDataContract.BaseClassContract.IsReference)
			{
				AddReferenceAttributes(xmlSchemaComplexContentExtension.Attributes, schema);
			}
		}
		else
		{
			xmlSchemaComplexType.Particle = xmlSchemaSequence;
			if (classDataContract.IsValueType)
			{
				xmlElement3 = GetAnnotationMarkup(IsValueTypeName, XmlConvert.ToString(classDataContract.IsValueType), schema);
			}
			if (classDataContract.IsReference)
			{
				AddReferenceAttributes(xmlSchemaComplexType.Attributes, schema);
			}
		}
		xmlSchemaComplexType.Annotation = GetSchemaAnnotation(xmlElement, ExportSurrogateData(classDataContract), xmlElement3);
	}

	private static void AddReferenceAttributes(XmlSchemaObjectCollection attributes, XmlSchema schema)
	{
		SchemaHelper.AddSchemaImport("http://schemas.microsoft.com/2003/10/Serialization/", schema);
		schema.Namespaces.Add("ser", "http://schemas.microsoft.com/2003/10/Serialization/");
		attributes.Add(IdAttribute);
		attributes.Add(RefAttribute);
	}

	private static void SetElementType(XmlSchemaElement element, DataContract dataContract, XmlSchema schema)
	{
		if (dataContract is XmlDataContract { IsAnonymous: not false } xmlDataContract)
		{
			element.SchemaType = xmlDataContract.XsdType;
			return;
		}
		element.SchemaTypeName = dataContract.XmlName;
		if (element.SchemaTypeName.Namespace.Equals("http://schemas.microsoft.com/2003/10/Serialization/"))
		{
			schema.Namespaces.Add("ser", "http://schemas.microsoft.com/2003/10/Serialization/");
		}
		SchemaHelper.AddSchemaImport(dataContract.XmlName.Namespace, schema);
	}

	private static bool CheckIfMemberHasConflict(DataMember dataMember)
	{
		if (dataMember.HasConflictingNameAndType)
		{
			return true;
		}
		for (DataMember conflictingMember = dataMember.ConflictingMember; conflictingMember != null; conflictingMember = conflictingMember.ConflictingMember)
		{
			if (conflictingMember.HasConflictingNameAndType)
			{
				return true;
			}
		}
		return false;
	}

	private XmlElement ExportEmitDefaultValue(DataMember dataMember)
	{
		if (dataMember.EmitDefaultValue)
		{
			return null;
		}
		XmlElement xmlElement = XmlDoc.CreateElement(DefaultValueAnnotation.Name, DefaultValueAnnotation.Namespace);
		XmlAttribute xmlAttribute = XmlDoc.CreateAttribute("EmitDefaultValue");
		xmlAttribute.Value = "false";
		xmlElement.Attributes.Append(xmlAttribute);
		return xmlElement;
	}

	private XmlElement ExportActualType(XmlQualifiedName typeName)
	{
		return ExportActualType(typeName, XmlDoc);
	}

	private static XmlElement ExportActualType(XmlQualifiedName typeName, XmlDocument xmlDoc)
	{
		XmlElement xmlElement = xmlDoc.CreateElement(ActualTypeAnnotationName.Name, ActualTypeAnnotationName.Namespace);
		XmlAttribute xmlAttribute = xmlDoc.CreateAttribute("Name");
		xmlAttribute.Value = typeName.Name;
		xmlElement.Attributes.Append(xmlAttribute);
		XmlAttribute xmlAttribute2 = xmlDoc.CreateAttribute("Namespace");
		xmlAttribute2.Value = typeName.Namespace;
		xmlElement.Attributes.Append(xmlAttribute2);
		return xmlElement;
	}

	[RequiresDynamicCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.")]
	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	private XmlElement ExportGenericInfo(Type clrType, string elementName, string elementNs)
	{
		int num = 0;
		Type itemType;
		while (CollectionDataContract.IsCollection(clrType, out itemType) && DataContract.GetBuiltInDataContract(clrType) == null && !CollectionDataContract.IsCollectionDataContract(clrType))
		{
			clrType = itemType;
			num++;
		}
		Type[] array = null;
		IList<int> list = null;
		if (clrType.IsGenericType)
		{
			array = clrType.GetGenericArguments();
			string text;
			if (clrType.DeclaringType == null)
			{
				text = clrType.Name;
			}
			else
			{
				int num2 = ((clrType.Namespace != null) ? clrType.Namespace.Length : 0);
				if (num2 > 0)
				{
					num2++;
				}
				text = DataContract.GetClrTypeFullName(clrType).Substring(num2).Replace('+', '.');
			}
			int num3 = text.IndexOf('[');
			if (num3 >= 0)
			{
				text = text.Substring(0, num3);
			}
			list = DataContract.GetDataContractNameForGenericName(text, null);
			clrType = clrType.GetGenericTypeDefinition();
		}
		XmlQualifiedName xmlQualifiedName = DataContract.GetXmlName(clrType);
		if (num > 0)
		{
			string text2 = xmlQualifiedName.Name;
			for (int i = 0; i < num; i++)
			{
				text2 = "ArrayOf" + text2;
			}
			xmlQualifiedName = new XmlQualifiedName(text2, DataContract.GetCollectionNamespace(xmlQualifiedName.Namespace));
		}
		XmlElement xmlElement = XmlDoc.CreateElement(elementName, elementNs);
		XmlAttribute xmlAttribute = XmlDoc.CreateAttribute("Name");
		xmlAttribute.Value = ((array != null) ? XmlConvert.DecodeName(xmlQualifiedName.Name) : xmlQualifiedName.Name);
		xmlElement.Attributes.Append(xmlAttribute);
		XmlAttribute xmlAttribute2 = XmlDoc.CreateAttribute("Namespace");
		xmlAttribute2.Value = xmlQualifiedName.Namespace;
		xmlElement.Attributes.Append(xmlAttribute2);
		if (array != null)
		{
			int num4 = 0;
			int num5 = 0;
			foreach (int item in list)
			{
				int num6 = 0;
				while (num6 < item)
				{
					XmlElement xmlElement2 = ExportGenericInfo(array[num4], "GenericParameter", "http://schemas.microsoft.com/2003/10/Serialization/");
					if (num5 > 0)
					{
						XmlAttribute xmlAttribute3 = XmlDoc.CreateAttribute("NestedLevel");
						xmlAttribute3.Value = num5.ToString(CultureInfo.InvariantCulture);
						xmlElement2.Attributes.Append(xmlAttribute3);
					}
					xmlElement.AppendChild(xmlElement2);
					num6++;
					num4++;
				}
				num5++;
			}
			if (list[num5 - 1] == 0)
			{
				XmlAttribute xmlAttribute4 = XmlDoc.CreateAttribute("NestedLevel");
				xmlAttribute4.Value = list.Count.ToString(CultureInfo.InvariantCulture);
				xmlElement.Attributes.Append(xmlAttribute4);
			}
		}
		return xmlElement;
	}

	[RequiresDynamicCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.")]
	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	private XmlElement ExportSurrogateData(object key)
	{
		object surrogateData = _dataContractSet.GetSurrogateData(key);
		if (surrogateData == null)
		{
			return null;
		}
		StringWriter stringWriter = new StringWriter(CultureInfo.InvariantCulture);
		XmlWriterSettings xmlWriterSettings = new XmlWriterSettings();
		xmlWriterSettings.OmitXmlDeclaration = true;
		XmlWriter xmlWriter = XmlWriter.Create(stringWriter, xmlWriterSettings);
		Collection<Type> collection = new Collection<Type>();
		if (_dataContractSet.SerializationExtendedSurrogateProvider != null)
		{
			DataContractSurrogateCaller.GetKnownCustomDataTypes(_dataContractSet.SerializationExtendedSurrogateProvider, collection);
		}
		DataContractSerializer dataContractSerializer = new DataContractSerializer(Globals.TypeOfObject, SurrogateDataAnnotationName.Name, SurrogateDataAnnotationName.Namespace, collection, ignoreExtensionDataObject: false, preserveObjectReferences: true);
		dataContractSerializer.WriteObject(xmlWriter, surrogateData);
		xmlWriter.Flush();
		return (XmlElement)XmlDoc.ReadNode(XmlReader.Create(new StringReader(stringWriter.ToString())));
	}

	[RequiresDynamicCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.")]
	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	private void ExportCollectionDataContract(CollectionDataContract collectionDataContract, XmlSchema schema)
	{
		XmlSchemaComplexType xmlSchemaComplexType = new XmlSchemaComplexType();
		xmlSchemaComplexType.Name = collectionDataContract.XmlName.Name;
		schema.Items.Add(xmlSchemaComplexType);
		XmlElement xmlElement = null;
		XmlElement xmlElement2 = null;
		if (collectionDataContract.UnderlyingType.IsGenericType && CollectionDataContract.IsCollectionDataContract(collectionDataContract.UnderlyingType))
		{
			xmlElement = ExportGenericInfo(collectionDataContract.UnderlyingType, "GenericType", "http://schemas.microsoft.com/2003/10/Serialization/");
		}
		if (collectionDataContract.IsDictionary)
		{
			xmlElement2 = ExportIsDictionary();
		}
		xmlSchemaComplexType.Annotation = GetSchemaAnnotation(xmlElement2, xmlElement, ExportSurrogateData(collectionDataContract));
		XmlSchemaSequence xmlSchemaSequence = new XmlSchemaSequence();
		XmlSchemaElement xmlSchemaElement = new XmlSchemaElement();
		xmlSchemaElement.Name = collectionDataContract.ItemName;
		xmlSchemaElement.MinOccurs = 0m;
		xmlSchemaElement.MaxOccursString = "unbounded";
		if (collectionDataContract.IsDictionary)
		{
			ClassDataContract classDataContract = collectionDataContract.ItemContract as ClassDataContract;
			XmlSchemaComplexType xmlSchemaComplexType2 = new XmlSchemaComplexType();
			XmlSchemaSequence xmlSchemaSequence2 = new XmlSchemaSequence();
			foreach (DataMember member in classDataContract.Members)
			{
				XmlSchemaElement xmlSchemaElement2 = new XmlSchemaElement();
				xmlSchemaElement2.Name = member.Name;
				SetElementType(xmlSchemaElement2, _dataContractSet.GetMemberTypeDataContract(member), schema);
				SchemaHelper.AddElementForm(xmlSchemaElement2, schema);
				if (member.IsNullable)
				{
					xmlSchemaElement2.IsNillable = true;
				}
				xmlSchemaElement2.Annotation = GetSchemaAnnotation(ExportSurrogateData(member));
				xmlSchemaSequence2.Items.Add(xmlSchemaElement2);
			}
			xmlSchemaComplexType2.Particle = xmlSchemaSequence2;
			xmlSchemaElement.SchemaType = xmlSchemaComplexType2;
		}
		else
		{
			if (collectionDataContract.IsItemTypeNullable)
			{
				xmlSchemaElement.IsNillable = true;
			}
			DataContract itemTypeDataContract = _dataContractSet.GetItemTypeDataContract(collectionDataContract);
			SetElementType(xmlSchemaElement, itemTypeDataContract, schema);
		}
		SchemaHelper.AddElementForm(xmlSchemaElement, schema);
		xmlSchemaSequence.Items.Add(xmlSchemaElement);
		xmlSchemaComplexType.Particle = xmlSchemaSequence;
		if (collectionDataContract.IsReference)
		{
			AddReferenceAttributes(xmlSchemaComplexType.Attributes, schema);
		}
	}

	private XmlElement ExportIsDictionary()
	{
		XmlElement xmlElement = XmlDoc.CreateElement(IsDictionaryAnnotationName.Name, IsDictionaryAnnotationName.Namespace);
		xmlElement.InnerText = "true";
		return xmlElement;
	}

	[RequiresDynamicCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.")]
	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	private void ExportEnumDataContract(EnumDataContract enumDataContract, XmlSchema schema)
	{
		XmlSchemaSimpleType xmlSchemaSimpleType = new XmlSchemaSimpleType();
		xmlSchemaSimpleType.Name = enumDataContract.XmlName.Name;
		XmlElement xmlElement = ((enumDataContract.BaseContractName == DefaultEnumBaseTypeName) ? null : ExportActualType(enumDataContract.BaseContractName));
		xmlSchemaSimpleType.Annotation = GetSchemaAnnotation(xmlElement, ExportSurrogateData(enumDataContract));
		schema.Items.Add(xmlSchemaSimpleType);
		XmlSchemaSimpleTypeRestriction xmlSchemaSimpleTypeRestriction = new XmlSchemaSimpleTypeRestriction();
		xmlSchemaSimpleTypeRestriction.BaseTypeName = StringQualifiedName;
		SchemaHelper.AddSchemaImport(enumDataContract.BaseContractName.Namespace, schema);
		if (enumDataContract.Values != null)
		{
			for (int i = 0; i < enumDataContract.Values.Count; i++)
			{
				XmlSchemaEnumerationFacet xmlSchemaEnumerationFacet = new XmlSchemaEnumerationFacet();
				xmlSchemaEnumerationFacet.Value = enumDataContract.Members[i].Name;
				if (enumDataContract.Values[i] != GetDefaultEnumValue(enumDataContract.IsFlags, i))
				{
					xmlSchemaEnumerationFacet.Annotation = GetSchemaAnnotation(EnumerationValueAnnotationName, enumDataContract.GetStringFromEnumValue(enumDataContract.Values[i]), schema);
				}
				xmlSchemaSimpleTypeRestriction.Facets.Add(xmlSchemaEnumerationFacet);
			}
		}
		if (enumDataContract.IsFlags)
		{
			XmlSchemaSimpleTypeList xmlSchemaSimpleTypeList = new XmlSchemaSimpleTypeList();
			XmlSchemaSimpleType xmlSchemaSimpleType2 = new XmlSchemaSimpleType();
			xmlSchemaSimpleType2.Content = xmlSchemaSimpleTypeRestriction;
			xmlSchemaSimpleTypeList.ItemType = xmlSchemaSimpleType2;
			xmlSchemaSimpleType.Content = xmlSchemaSimpleTypeList;
		}
		else
		{
			xmlSchemaSimpleType.Content = xmlSchemaSimpleTypeRestriction;
		}
	}

	internal static long GetDefaultEnumValue(bool isFlags, int index)
	{
		if (!isFlags)
		{
			return index;
		}
		return (long)Math.Pow(2.0, index);
	}

	[RequiresDynamicCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.")]
	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	private void ExportISerializableDataContract(ClassDataContract dataContract, XmlSchema schema)
	{
		XmlSchemaComplexType xmlSchemaComplexType = new XmlSchemaComplexType();
		xmlSchemaComplexType.Name = dataContract.XmlName.Name;
		schema.Items.Add(xmlSchemaComplexType);
		XmlElement xmlElement = null;
		if (dataContract.UnderlyingType.IsGenericType)
		{
			xmlElement = ExportGenericInfo(dataContract.UnderlyingType, "GenericType", "http://schemas.microsoft.com/2003/10/Serialization/");
		}
		XmlElement xmlElement2 = null;
		if (dataContract.BaseClassContract != null)
		{
			CreateTypeContent(xmlSchemaComplexType, dataContract.BaseClassContract.XmlName, schema);
		}
		else
		{
			schema.Namespaces.Add("ser", "http://schemas.microsoft.com/2003/10/Serialization/");
			xmlSchemaComplexType.Particle = ISerializableSequence;
			XmlSchemaAttribute iSerializableFactoryTypeAttribute = ISerializableFactoryTypeAttribute;
			xmlSchemaComplexType.Attributes.Add(iSerializableFactoryTypeAttribute);
			SchemaHelper.AddSchemaImport(ISerializableFactoryTypeAttribute.RefName.Namespace, schema);
			if (dataContract.IsValueType)
			{
				xmlElement2 = GetAnnotationMarkup(IsValueTypeName, XmlConvert.ToString(dataContract.IsValueType), schema);
			}
		}
		xmlSchemaComplexType.Annotation = GetSchemaAnnotation(xmlElement, ExportSurrogateData(dataContract), xmlElement2);
	}

	private static XmlSchemaComplexContentExtension CreateTypeContent(XmlSchemaComplexType type, XmlQualifiedName baseTypeName, XmlSchema schema)
	{
		SchemaHelper.AddSchemaImport(baseTypeName.Namespace, schema);
		XmlSchemaComplexContentExtension xmlSchemaComplexContentExtension = new XmlSchemaComplexContentExtension();
		xmlSchemaComplexContentExtension.BaseTypeName = baseTypeName;
		type.ContentModel = new XmlSchemaComplexContent();
		type.ContentModel.Content = xmlSchemaComplexContentExtension;
		return xmlSchemaComplexContentExtension;
	}

	[RequiresDynamicCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.")]
	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	private void ExportXmlDataContract(XmlDataContract dataContract)
	{
		Type underlyingType = dataContract.UnderlyingType;
		if (!IsSpecialXmlType(underlyingType, out var typeName, out var xsdType, out var hasRoot) && !InvokeSchemaProviderMethod(underlyingType, _schemas, out typeName, out xsdType, out hasRoot))
		{
			InvokeGetSchemaMethod(underlyingType, _schemas, typeName);
		}
		if (hasRoot)
		{
			typeName.Equals(dataContract.XmlName);
			if (SchemaHelper.GetSchemaElement(Schemas, new XmlQualifiedName(dataContract.TopLevelElementName.Value, dataContract.TopLevelElementNamespace.Value), out var outSchema) == null)
			{
				XmlSchemaElement xmlSchemaElement = ExportTopLevelElement(dataContract, outSchema);
				xmlSchemaElement.IsNillable = dataContract.IsTopLevelElementNullable;
				ReprocessAll(_schemas);
			}
			XmlSchemaType xmlSchemaType = xsdType;
			xsdType = SchemaHelper.GetSchemaType(_schemas, typeName, out outSchema);
			if (xmlSchemaType == null && xsdType == null && typeName.Namespace != "http://www.w3.org/2001/XMLSchema")
			{
				throw new InvalidDataContractException(System.SR.Format(System.SR.MissingSchemaType, typeName, DataContract.GetClrTypeFullName(underlyingType)));
			}
			if (xsdType != null)
			{
				xsdType.Annotation = GetSchemaAnnotation(ExportSurrogateData(dataContract), dataContract.IsValueType ? GetAnnotationMarkup(IsValueTypeName, XmlConvert.ToString(dataContract.IsValueType), outSchema) : null);
			}
		}
	}

	private static void ReprocessAll(XmlSchemaSet schemas)
	{
		Hashtable hashtable = new Hashtable();
		Hashtable hashtable2 = new Hashtable();
		XmlSchema[] array = new XmlSchema[schemas.Count];
		schemas.CopyTo(array, 0);
		foreach (XmlSchema xmlSchema in array)
		{
			XmlSchemaObject[] array2 = new XmlSchemaObject[xmlSchema.Items.Count];
			xmlSchema.Items.CopyTo(array2, 0);
			foreach (XmlSchemaObject xmlSchemaObject in array2)
			{
				Hashtable hashtable3;
				XmlQualifiedName key;
				if (xmlSchemaObject is XmlSchemaElement)
				{
					hashtable3 = hashtable;
					key = new XmlQualifiedName(((XmlSchemaElement)xmlSchemaObject).Name, xmlSchema.TargetNamespace);
				}
				else
				{
					if (!(xmlSchemaObject is XmlSchemaType))
					{
						continue;
					}
					hashtable3 = hashtable2;
					key = new XmlQualifiedName(((XmlSchemaType)xmlSchemaObject).Name, xmlSchema.TargetNamespace);
				}
				object obj = hashtable3[key];
				if (obj != null)
				{
					xmlSchema.Items.Remove(xmlSchemaObject);
				}
				else
				{
					hashtable3.Add(key, xmlSchemaObject);
				}
			}
			schemas.Reprocess(xmlSchema);
		}
	}

	[RequiresDynamicCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.")]
	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	internal static void GetXmlTypeInfo(Type type, out XmlQualifiedName xmlName, out XmlSchemaType xsdType, out bool hasRoot)
	{
		if (!IsSpecialXmlType(type, out xmlName, out xsdType, out hasRoot))
		{
			XmlSchemaSet xmlSchemaSet = new XmlSchemaSet();
			xmlSchemaSet.XmlResolver = null;
			InvokeSchemaProviderMethod(type, xmlSchemaSet, out xmlName, out xsdType, out hasRoot);
			if (string.IsNullOrEmpty(xmlName.Name))
			{
				throw new InvalidDataContractException(System.SR.Format(System.SR.InvalidXmlDataContractName, DataContract.GetClrTypeFullName(type)));
			}
		}
	}

	[RequiresDynamicCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.")]
	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	private static bool InvokeSchemaProviderMethod(Type clrType, XmlSchemaSet schemas, out XmlQualifiedName xmlName, out XmlSchemaType xsdType, out bool hasRoot)
	{
		xsdType = null;
		hasRoot = true;
		object[] customAttributes = clrType.GetCustomAttributes(Globals.TypeOfXmlSchemaProviderAttribute, inherit: false);
		if (customAttributes == null || customAttributes.Length == 0)
		{
			xmlName = DataContract.GetDefaultXmlName(clrType);
			return false;
		}
		XmlSchemaProviderAttribute xmlSchemaProviderAttribute = (XmlSchemaProviderAttribute)customAttributes[0];
		if (xmlSchemaProviderAttribute.IsAny)
		{
			xsdType = CreateAnyElementType();
			hasRoot = false;
		}
		string methodName = xmlSchemaProviderAttribute.MethodName;
		if (string.IsNullOrEmpty(methodName))
		{
			if (!xmlSchemaProviderAttribute.IsAny)
			{
				throw new InvalidDataContractException(System.SR.Format(System.SR.InvalidGetSchemaMethod, DataContract.GetClrTypeFullName(clrType)));
			}
			xmlName = DataContract.GetDefaultXmlName(clrType);
		}
		else
		{
			MethodInfo method = clrType.GetMethod(methodName, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, new Type[1] { typeof(XmlSchemaSet) });
			if (method == null)
			{
				throw new InvalidDataContractException(System.SR.Format(System.SR.MissingGetSchemaMethod, DataContract.GetClrTypeFullName(clrType), methodName));
			}
			if (!Globals.TypeOfXmlQualifiedName.IsAssignableFrom(method.ReturnType) && !Globals.TypeOfXmlSchemaType.IsAssignableFrom(method.ReturnType))
			{
				throw new InvalidDataContractException(System.SR.Format(System.SR.InvalidReturnTypeOnGetSchemaMethod, DataContract.GetClrTypeFullName(clrType), methodName, DataContract.GetClrTypeFullName(method.ReturnType), DataContract.GetClrTypeFullName(Globals.TypeOfXmlQualifiedName), typeof(XmlSchemaType)));
			}
			object obj = method.Invoke(null, new object[1] { schemas });
			if (xmlSchemaProviderAttribute.IsAny)
			{
				if (obj != null)
				{
					throw new InvalidDataContractException(System.SR.Format(System.SR.InvalidNonNullReturnValueByIsAny, DataContract.GetClrTypeFullName(clrType), methodName));
				}
				xmlName = DataContract.GetDefaultXmlName(clrType);
			}
			else if (obj == null)
			{
				xsdType = CreateAnyElementType();
				hasRoot = false;
				xmlName = DataContract.GetDefaultXmlName(clrType);
			}
			else if (obj is XmlSchemaType xmlSchemaType)
			{
				string localName = xmlSchemaType.Name;
				string ns = null;
				if (string.IsNullOrEmpty(localName))
				{
					DataContract.GetDefaultXmlName(DataContract.GetClrTypeFullName(clrType), out localName, out ns);
					xmlName = new XmlQualifiedName(localName, ns);
					xmlSchemaType.Annotation = GetSchemaAnnotation(ExportActualType(xmlName, new XmlDocument()));
					xsdType = xmlSchemaType;
				}
				else
				{
					foreach (XmlSchema item in schemas.Schemas())
					{
						foreach (XmlSchemaObject item2 in item.Items)
						{
							if (item2 == xmlSchemaType)
							{
								ns = item.TargetNamespace ?? string.Empty;
								break;
							}
						}
						if (ns != null)
						{
							break;
						}
					}
					if (ns == null)
					{
						throw new InvalidDataContractException(System.SR.Format(System.SR.MissingSchemaType, localName, DataContract.GetClrTypeFullName(clrType)));
					}
					xmlName = new XmlQualifiedName(localName, ns);
				}
			}
			else
			{
				xmlName = (XmlQualifiedName)obj;
			}
		}
		return true;
	}

	private static void InvokeGetSchemaMethod([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] Type clrType, XmlSchemaSet schemas, XmlQualifiedName xmlName)
	{
		IXmlSerializable xmlSerializable = (IXmlSerializable)Activator.CreateInstance(clrType);
		XmlSchema schema = xmlSerializable.GetSchema();
		if (schema == null)
		{
			AddDefaultDatasetType(schemas, xmlName.Name, xmlName.Namespace);
			return;
		}
		if (string.IsNullOrEmpty(schema.Id))
		{
			throw new InvalidDataContractException(System.SR.Format(System.SR.InvalidReturnSchemaOnGetSchemaMethod, DataContract.GetClrTypeFullName(clrType)));
		}
		AddDefaultTypedDatasetType(schemas, schema, xmlName.Name, xmlName.Namespace);
	}

	internal static void AddDefaultXmlType(XmlSchemaSet schemas, string localName, string ns)
	{
		XmlSchemaComplexType xmlSchemaComplexType = CreateAnyType();
		xmlSchemaComplexType.Name = localName;
		XmlSchema schema = SchemaHelper.GetSchema(ns, schemas);
		schema.Items.Add(xmlSchemaComplexType);
		schemas.Reprocess(schema);
	}

	private static XmlSchemaComplexType CreateAnyType()
	{
		XmlSchemaComplexType xmlSchemaComplexType = new XmlSchemaComplexType();
		xmlSchemaComplexType.IsMixed = true;
		xmlSchemaComplexType.Particle = new XmlSchemaSequence();
		XmlSchemaAny xmlSchemaAny = new XmlSchemaAny();
		xmlSchemaAny.MinOccurs = 0m;
		xmlSchemaAny.MaxOccurs = decimal.MaxValue;
		xmlSchemaAny.ProcessContents = XmlSchemaContentProcessing.Lax;
		((XmlSchemaSequence)xmlSchemaComplexType.Particle).Items.Add(xmlSchemaAny);
		xmlSchemaComplexType.AnyAttribute = new XmlSchemaAnyAttribute();
		return xmlSchemaComplexType;
	}

	private static XmlSchemaComplexType CreateAnyElementType()
	{
		XmlSchemaComplexType xmlSchemaComplexType = new XmlSchemaComplexType();
		xmlSchemaComplexType.IsMixed = false;
		xmlSchemaComplexType.Particle = new XmlSchemaSequence();
		XmlSchemaAny xmlSchemaAny = new XmlSchemaAny();
		xmlSchemaAny.MinOccurs = 0m;
		xmlSchemaAny.ProcessContents = XmlSchemaContentProcessing.Lax;
		((XmlSchemaSequence)xmlSchemaComplexType.Particle).Items.Add(xmlSchemaAny);
		return xmlSchemaComplexType;
	}

	internal static bool IsSpecialXmlType(Type type, [NotNullWhen(true)] out XmlQualifiedName typeName, [NotNullWhen(true)] out XmlSchemaType xsdType, out bool hasRoot)
	{
		xsdType = null;
		hasRoot = true;
		if (type == Globals.TypeOfXmlElement || type == Globals.TypeOfXmlNodeArray)
		{
			string name;
			if (type == Globals.TypeOfXmlElement)
			{
				xsdType = CreateAnyElementType();
				name = "XmlElement";
				hasRoot = false;
			}
			else
			{
				xsdType = CreateAnyType();
				name = "ArrayOfXmlNode";
				hasRoot = true;
			}
			typeName = new XmlQualifiedName(name, DataContract.GetDefaultXmlNamespace(type));
			return true;
		}
		typeName = null;
		return false;
	}

	private static void AddDefaultDatasetType(XmlSchemaSet schemas, string localName, string ns)
	{
		XmlSchemaComplexType xmlSchemaComplexType = new XmlSchemaComplexType();
		xmlSchemaComplexType.Name = localName;
		xmlSchemaComplexType.Particle = new XmlSchemaSequence();
		XmlSchemaElement xmlSchemaElement = new XmlSchemaElement();
		xmlSchemaElement.RefName = new XmlQualifiedName("schema", "http://www.w3.org/2001/XMLSchema");
		((XmlSchemaSequence)xmlSchemaComplexType.Particle).Items.Add(xmlSchemaElement);
		XmlSchemaAny item = new XmlSchemaAny();
		((XmlSchemaSequence)xmlSchemaComplexType.Particle).Items.Add(item);
		XmlSchema schema = SchemaHelper.GetSchema(ns, schemas);
		schema.Items.Add(xmlSchemaComplexType);
		schemas.Reprocess(schema);
	}

	private static void AddDefaultTypedDatasetType(XmlSchemaSet schemas, XmlSchema datasetSchema, string localName, string ns)
	{
		XmlSchemaComplexType xmlSchemaComplexType = new XmlSchemaComplexType();
		xmlSchemaComplexType.Name = localName;
		xmlSchemaComplexType.Particle = new XmlSchemaSequence();
		XmlSchemaAny xmlSchemaAny = new XmlSchemaAny();
		xmlSchemaAny.Namespace = datasetSchema.TargetNamespace ?? string.Empty;
		((XmlSchemaSequence)xmlSchemaComplexType.Particle).Items.Add(xmlSchemaAny);
		schemas.Add(datasetSchema);
		XmlSchema schema = SchemaHelper.GetSchema(ns, schemas);
		schema.Items.Add(xmlSchemaComplexType);
		schemas.Reprocess(datasetSchema);
		schemas.Reprocess(schema);
	}

	private XmlSchemaAnnotation GetSchemaAnnotation(XmlQualifiedName annotationQualifiedName, string innerText, XmlSchema schema)
	{
		XmlSchemaAnnotation xmlSchemaAnnotation = new XmlSchemaAnnotation();
		XmlSchemaAppInfo xmlSchemaAppInfo = new XmlSchemaAppInfo();
		XmlElement annotationMarkup = GetAnnotationMarkup(annotationQualifiedName, innerText, schema);
		xmlSchemaAppInfo.Markup = new XmlNode[1] { annotationMarkup };
		xmlSchemaAnnotation.Items.Add(xmlSchemaAppInfo);
		return xmlSchemaAnnotation;
	}

	private static XmlSchemaAnnotation GetSchemaAnnotation(params XmlNode[] nodes)
	{
		if (nodes == null || nodes.Length == 0)
		{
			return null;
		}
		bool flag = false;
		for (int i = 0; i < nodes.Length; i++)
		{
			if (nodes[i] != null)
			{
				flag = true;
				break;
			}
		}
		if (!flag)
		{
			return null;
		}
		XmlSchemaAnnotation xmlSchemaAnnotation = new XmlSchemaAnnotation();
		XmlSchemaAppInfo xmlSchemaAppInfo = new XmlSchemaAppInfo();
		xmlSchemaAnnotation.Items.Add(xmlSchemaAppInfo);
		xmlSchemaAppInfo.Markup = nodes;
		return xmlSchemaAnnotation;
	}

	private XmlElement GetAnnotationMarkup(XmlQualifiedName annotationQualifiedName, string innerText, XmlSchema schema)
	{
		XmlElement xmlElement = XmlDoc.CreateElement(annotationQualifiedName.Name, annotationQualifiedName.Namespace);
		SchemaHelper.AddSchemaImport(annotationQualifiedName.Namespace, schema);
		xmlElement.InnerText = innerText;
		return xmlElement;
	}

	private XmlSchema GetSchema(string ns)
	{
		return SchemaHelper.GetSchema(ns, Schemas);
	}
}
