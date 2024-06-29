using System.CodeDom.Compiler;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization.DataContracts;
using System.Text.RegularExpressions;
using System.Text.RegularExpressions.Generated;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace System.Runtime.Serialization;

internal static class Globals
{
	internal const BindingFlags ScanAllMembers = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

	private static XmlQualifiedName s_idQualifiedName;

	private static XmlQualifiedName s_refQualifiedName;

	private static Type s_typeOfObject;

	private static Type s_typeOfValueType;

	private static Type s_typeOfArray;

	private static Type s_typeOfString;

	private static Type s_typeOfInt;

	private static Type s_typeOfULong;

	private static Type s_typeOfVoid;

	private static Type s_typeOfByteArray;

	private static Type s_typeOfTimeSpan;

	private static Type s_typeOfGuid;

	private static Type s_typeOfDateTimeOffset;

	private static Type s_typeOfDateTimeOffsetAdapter;

	private static Type s_typeOfMemoryStream;

	private static Type s_typeOfMemoryStreamAdapter;

	private static Type s_typeOfUri;

	private static Type s_typeOfTypeEnumerable;

	private static Type s_typeOfStreamingContext;

	private static Type s_typeOfISerializable;

	private static Type s_typeOfIDeserializationCallback;

	private static Type s_typeOfIObjectReference;

	private static Type s_typeOfXmlFormatClassWriterDelegate;

	private static Type s_typeOfXmlFormatCollectionWriterDelegate;

	private static Type s_typeOfXmlFormatClassReaderDelegate;

	private static Type s_typeOfXmlFormatCollectionReaderDelegate;

	private static Type s_typeOfXmlFormatGetOnlyCollectionReaderDelegate;

	private static Type s_typeOfKnownTypeAttribute;

	private static Type s_typeOfDataContractAttribute;

	private static Type s_typeOfDataMemberAttribute;

	private static Type s_typeOfEnumMemberAttribute;

	private static Type s_typeOfCollectionDataContractAttribute;

	private static Type s_typeOfOptionalFieldAttribute;

	private static Type s_typeOfObjectArray;

	private static Type s_typeOfOnSerializingAttribute;

	private static Type s_typeOfOnSerializedAttribute;

	private static Type s_typeOfOnDeserializingAttribute;

	private static Type s_typeOfOnDeserializedAttribute;

	private static Type s_typeOfFlagsAttribute;

	private static Type s_typeOfIXmlSerializable;

	private static Type s_typeOfXmlSchemaProviderAttribute;

	private static Type s_typeOfXmlRootAttribute;

	private static Type s_typeOfXmlQualifiedName;

	private static Type s_typeOfXmlSchemaType;

	private static Type s_typeOfIExtensibleDataObject;

	private static Type s_typeOfExtensionDataObject;

	private static Type s_typeOfISerializableDataNode;

	private static Type s_typeOfClassDataNode;

	private static Type s_typeOfCollectionDataNode;

	private static Type s_typeOfXmlDataNode;

	private static Type s_typeOfNullable;

	private static Type s_typeOfReflectionPointer;

	private static Type s_typeOfIDictionaryGeneric;

	private static Type s_typeOfIDictionary;

	private static Type s_typeOfIListGeneric;

	private static Type s_typeOfIList;

	private static Type s_typeOfICollectionGeneric;

	private static Type s_typeOfICollection;

	private static Type s_typeOfIEnumerableGeneric;

	private static Type s_typeOfIEnumerable;

	private static Type s_typeOfIEnumeratorGeneric;

	private static Type s_typeOfIEnumerator;

	private static Type s_typeOfKeyValuePair;

	private static Type s_typeOfKeyValue;

	private static Type s_typeOfIDictionaryEnumerator;

	private static Type s_typeOfDictionaryEnumerator;

	private static Type s_typeOfGenericDictionaryEnumerator;

	private static Type s_typeOfDictionaryGeneric;

	private static Type s_typeOfHashtable;

	private static Type s_typeOfXmlElement;

	private static Type s_typeOfXmlNodeArray;

	private static Type s_typeOfDBNull;

	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.NonPublicFields)]
	private static Type s_typeOfSchemaDefinedType;

	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.NonPublicFields)]
	private static Type s_typeOfSchemaDefinedEnum;

	private static MemberInfo s_schemaMemberInfoPlaceholder;

	private static Uri s_dataContractXsdBaseNamespaceUri;

	public const bool DefaultIsRequired = false;

	public const bool DefaultEmitDefaultValue = true;

	public const int DefaultOrder = 0;

	public const bool DefaultIsReference = false;

	public static readonly string NewObjectId = string.Empty;

	public const string NullObjectId = null;

	public const string FullSRSInternalsVisiblePattern = "^[\\s]*System\\.Runtime\\.Serialization[\\s]*,[\\s]*PublicKey[\\s]*=[\\s]*(?i:00240000048000009400000006020000002400005253413100040000010001008d56c76f9e8649383049f383c44be0ec204181822a6c31cf5eb7ef486944d032188ea1d3920763712ccb12d75fb77e9811149e6148e5d32fbaab37611c1878ddc19e20ef135d0cb2cff2bfec3d115810c3d9069638fe4be215dbf795861920e5ab6f7db2e2ceef136ac23d5dd2bf031700aec232f6c6b1c785b4305c123b37ab)[\\s]*$";

	public const char SpaceChar = ' ';

	public const char OpenBracketChar = '[';

	public const char CloseBracketChar = ']';

	public const char CommaChar = ',';

	public const string Space = " ";

	public const string XsiPrefix = "i";

	public const string XsdPrefix = "x";

	public const string SerPrefix = "z";

	public const string SerPrefixForSchema = "ser";

	public const string ElementPrefix = "q";

	public const string DataContractXsdBaseNamespace = "http://schemas.datacontract.org/2004/07/";

	public const string DataContractXmlNamespace = "http://schemas.datacontract.org/2004/07/System.Xml";

	public const string SchemaInstanceNamespace = "http://www.w3.org/2001/XMLSchema-instance";

	public const string SchemaNamespace = "http://www.w3.org/2001/XMLSchema";

	public const string XsiNilLocalName = "nil";

	public const string XsiTypeLocalName = "type";

	public const string TnsPrefix = "tns";

	public const string OccursUnbounded = "unbounded";

	public const string AnyTypeLocalName = "anyType";

	public const string StringLocalName = "string";

	public const string IntLocalName = "int";

	public const string True = "true";

	public const string False = "false";

	public const string ArrayPrefix = "ArrayOf";

	public const string XmlnsNamespace = "http://www.w3.org/2000/xmlns/";

	public const string XmlnsPrefix = "xmlns";

	public const string SchemaLocalName = "schema";

	public const string CollectionsNamespace = "http://schemas.microsoft.com/2003/10/Serialization/Arrays";

	public const string DefaultClrNamespace = "GeneratedNamespace";

	public const string DefaultTypeName = "GeneratedType";

	public const string DefaultGeneratedMember = "GeneratedMember";

	public const string DefaultFieldSuffix = "Field";

	public const string DefaultPropertySuffix = "Property";

	public const string DefaultMemberSuffix = "Member";

	public const string NameProperty = "Name";

	public const string NamespaceProperty = "Namespace";

	public const string OrderProperty = "Order";

	public const string IsReferenceProperty = "IsReference";

	public const string IsRequiredProperty = "IsRequired";

	public const string EmitDefaultValueProperty = "EmitDefaultValue";

	public const string ClrNamespaceProperty = "ClrNamespace";

	public const string ItemNameProperty = "ItemName";

	public const string KeyNameProperty = "KeyName";

	public const string ValueNameProperty = "ValueName";

	public const string SerializationInfoPropertyName = "SerializationInfo";

	public const string SerializationInfoFieldName = "info";

	public const string NodeArrayPropertyName = "Nodes";

	public const string NodeArrayFieldName = "nodesField";

	public const string ExportSchemaMethod = "ExportSchema";

	public const string IsAnyProperty = "IsAny";

	public const string ContextFieldName = "context";

	public const string GetObjectDataMethodName = "GetObjectData";

	public const string GetEnumeratorMethodName = "GetEnumerator";

	public const string MoveNextMethodName = "MoveNext";

	public const string AddValueMethodName = "AddValue";

	public const string CurrentPropertyName = "Current";

	public const string ValueProperty = "Value";

	public const string EnumeratorFieldName = "enumerator";

	public const string SerializationEntryFieldName = "entry";

	public const string ExtensionDataSetMethod = "set_ExtensionData";

	public const string ExtensionDataSetExplicitMethod = "System.Runtime.Serialization.IExtensibleDataObject.set_ExtensionData";

	public const string ExtensionDataObjectPropertyName = "ExtensionData";

	public const string ExtensionDataObjectFieldName = "extensionDataField";

	public const string AddMethodName = "Add";

	public const string GetCurrentMethodName = "get_Current";

	public const string SerializationNamespace = "http://schemas.microsoft.com/2003/10/Serialization/";

	public const string ClrTypeLocalName = "Type";

	public const string ClrAssemblyLocalName = "Assembly";

	public const string IsValueTypeLocalName = "IsValueType";

	public const string EnumerationValueLocalName = "EnumerationValue";

	public const string SurrogateDataLocalName = "Surrogate";

	public const string GenericTypeLocalName = "GenericType";

	public const string GenericParameterLocalName = "GenericParameter";

	public const string GenericNameAttribute = "Name";

	public const string GenericNamespaceAttribute = "Namespace";

	public const string GenericParameterNestedLevelAttribute = "NestedLevel";

	public const string IsDictionaryLocalName = "IsDictionary";

	public const string ActualTypeLocalName = "ActualType";

	public const string ActualTypeNameAttribute = "Name";

	public const string ActualTypeNamespaceAttribute = "Namespace";

	public const string DefaultValueLocalName = "DefaultValue";

	public const string EmitDefaultValueAttribute = "EmitDefaultValue";

	public const string IdLocalName = "Id";

	public const string RefLocalName = "Ref";

	public const string ArraySizeLocalName = "Size";

	public const string KeyLocalName = "Key";

	public const string ValueLocalName = "Value";

	public const string MscorlibAssemblyName = "0";

	public const string ParseMethodName = "Parse";

	public const string SafeSerializationManagerName = "SafeSerializationManager";

	public const string SafeSerializationManagerNamespace = "http://schemas.datacontract.org/2004/07/System.Runtime.Serialization";

	public const string ISerializableFactoryTypeLocalName = "FactoryType";

	public const string SerializationSchema = "<?xml version='1.0' encoding='utf-8'?>\r\n<xs:schema elementFormDefault='qualified' attributeFormDefault='qualified' xmlns:tns='http://schemas.microsoft.com/2003/10/Serialization/' targetNamespace='http://schemas.microsoft.com/2003/10/Serialization/' xmlns:xs='http://www.w3.org/2001/XMLSchema'>\r\n  <xs:element name='anyType' nillable='true' type='xs:anyType' />\r\n  <xs:element name='anyURI' nillable='true' type='xs:anyURI' />\r\n  <xs:element name='base64Binary' nillable='true' type='xs:base64Binary' />\r\n  <xs:element name='boolean' nillable='true' type='xs:boolean' />\r\n  <xs:element name='byte' nillable='true' type='xs:byte' />\r\n  <xs:element name='dateTime' nillable='true' type='xs:dateTime' />\r\n  <xs:element name='decimal' nillable='true' type='xs:decimal' />\r\n  <xs:element name='double' nillable='true' type='xs:double' />\r\n  <xs:element name='float' nillable='true' type='xs:float' />\r\n  <xs:element name='int' nillable='true' type='xs:int' />\r\n  <xs:element name='long' nillable='true' type='xs:long' />\r\n  <xs:element name='QName' nillable='true' type='xs:QName' />\r\n  <xs:element name='short' nillable='true' type='xs:short' />\r\n  <xs:element name='string' nillable='true' type='xs:string' />\r\n  <xs:element name='unsignedByte' nillable='true' type='xs:unsignedByte' />\r\n  <xs:element name='unsignedInt' nillable='true' type='xs:unsignedInt' />\r\n  <xs:element name='unsignedLong' nillable='true' type='xs:unsignedLong' />\r\n  <xs:element name='unsignedShort' nillable='true' type='xs:unsignedShort' />\r\n  <xs:element name='char' nillable='true' type='tns:char' />\r\n  <xs:simpleType name='char'>\r\n    <xs:restriction base='xs:int'/>\r\n  </xs:simpleType>\r\n  <xs:element name='duration' nillable='true' type='tns:duration' />\r\n  <xs:simpleType name='duration'>\r\n    <xs:restriction base='xs:duration'>\r\n      <xs:pattern value='\\-?P(\\d*D)?(T(\\d*H)?(\\d*M)?(\\d*(\\.\\d*)?S)?)?' />\r\n      <xs:minInclusive value='-P10675199DT2H48M5.4775808S' />\r\n      <xs:maxInclusive value='P10675199DT2H48M5.4775807S' />\r\n    </xs:restriction>\r\n  </xs:simpleType>\r\n  <xs:element name='guid' nillable='true' type='tns:guid' />\r\n  <xs:simpleType name='guid'>\r\n    <xs:restriction base='xs:string'>\r\n      <xs:pattern value='[\\da-fA-F]{8}-[\\da-fA-F]{4}-[\\da-fA-F]{4}-[\\da-fA-F]{4}-[\\da-fA-F]{12}' />\r\n    </xs:restriction>\r\n  </xs:simpleType>\r\n  <xs:attribute name='FactoryType' type='xs:QName' />\r\n  <xs:attribute name='Id' type='xs:ID' />\r\n  <xs:attribute name='Ref' type='xs:IDREF' />\r\n</xs:schema>\r\n";

	internal static XmlQualifiedName IdQualifiedName => s_idQualifiedName ?? (s_idQualifiedName = new XmlQualifiedName("Id", "http://schemas.microsoft.com/2003/10/Serialization/"));

	internal static XmlQualifiedName RefQualifiedName => s_refQualifiedName ?? (s_refQualifiedName = new XmlQualifiedName("Ref", "http://schemas.microsoft.com/2003/10/Serialization/"));

	internal static Type TypeOfObject => s_typeOfObject ?? (s_typeOfObject = typeof(object));

	internal static Type TypeOfValueType => s_typeOfValueType ?? (s_typeOfValueType = typeof(ValueType));

	internal static Type TypeOfArray => s_typeOfArray ?? (s_typeOfArray = typeof(Array));

	internal static Type TypeOfString => s_typeOfString ?? (s_typeOfString = typeof(string));

	internal static Type TypeOfInt => s_typeOfInt ?? (s_typeOfInt = typeof(int));

	internal static Type TypeOfULong => s_typeOfULong ?? (s_typeOfULong = typeof(ulong));

	internal static Type TypeOfVoid => s_typeOfVoid ?? (s_typeOfVoid = typeof(void));

	internal static Type TypeOfByteArray => s_typeOfByteArray ?? (s_typeOfByteArray = typeof(byte[]));

	internal static Type TypeOfTimeSpan => s_typeOfTimeSpan ?? (s_typeOfTimeSpan = typeof(TimeSpan));

	internal static Type TypeOfGuid => s_typeOfGuid ?? (s_typeOfGuid = typeof(Guid));

	internal static Type TypeOfDateTimeOffset => s_typeOfDateTimeOffset ?? (s_typeOfDateTimeOffset = typeof(DateTimeOffset));

	internal static Type TypeOfDateTimeOffsetAdapter => s_typeOfDateTimeOffsetAdapter ?? (s_typeOfDateTimeOffsetAdapter = typeof(DateTimeOffsetAdapter));

	internal static Type TypeOfMemoryStream => s_typeOfMemoryStream ?? (s_typeOfMemoryStream = typeof(MemoryStream));

	internal static Type TypeOfMemoryStreamAdapter => s_typeOfMemoryStreamAdapter ?? (s_typeOfMemoryStreamAdapter = typeof(MemoryStreamAdapter));

	internal static Type TypeOfUri => s_typeOfUri ?? (s_typeOfUri = typeof(Uri));

	internal static Type TypeOfTypeEnumerable => s_typeOfTypeEnumerable ?? (s_typeOfTypeEnumerable = typeof(IEnumerable<Type>));

	internal static Type TypeOfStreamingContext => s_typeOfStreamingContext ?? (s_typeOfStreamingContext = typeof(StreamingContext));

	internal static Type TypeOfISerializable => s_typeOfISerializable ?? (s_typeOfISerializable = typeof(ISerializable));

	internal static Type TypeOfIDeserializationCallback => s_typeOfIDeserializationCallback ?? (s_typeOfIDeserializationCallback = typeof(IDeserializationCallback));

	internal static Type TypeOfIObjectReference => s_typeOfIObjectReference ?? (s_typeOfIObjectReference = typeof(IObjectReference));

	internal static Type TypeOfXmlFormatClassWriterDelegate => s_typeOfXmlFormatClassWriterDelegate ?? (s_typeOfXmlFormatClassWriterDelegate = typeof(XmlFormatClassWriterDelegate));

	internal static Type TypeOfXmlFormatCollectionWriterDelegate => s_typeOfXmlFormatCollectionWriterDelegate ?? (s_typeOfXmlFormatCollectionWriterDelegate = typeof(XmlFormatCollectionWriterDelegate));

	internal static Type TypeOfXmlFormatClassReaderDelegate => s_typeOfXmlFormatClassReaderDelegate ?? (s_typeOfXmlFormatClassReaderDelegate = typeof(XmlFormatClassReaderDelegate));

	internal static Type TypeOfXmlFormatCollectionReaderDelegate => s_typeOfXmlFormatCollectionReaderDelegate ?? (s_typeOfXmlFormatCollectionReaderDelegate = typeof(XmlFormatCollectionReaderDelegate));

	internal static Type TypeOfXmlFormatGetOnlyCollectionReaderDelegate => s_typeOfXmlFormatGetOnlyCollectionReaderDelegate ?? (s_typeOfXmlFormatGetOnlyCollectionReaderDelegate = typeof(XmlFormatGetOnlyCollectionReaderDelegate));

	internal static Type TypeOfKnownTypeAttribute => s_typeOfKnownTypeAttribute ?? (s_typeOfKnownTypeAttribute = typeof(KnownTypeAttribute));

	internal static Type TypeOfDataContractAttribute => s_typeOfDataContractAttribute ?? (s_typeOfDataContractAttribute = typeof(DataContractAttribute));

	internal static Type TypeOfDataMemberAttribute => s_typeOfDataMemberAttribute ?? (s_typeOfDataMemberAttribute = typeof(DataMemberAttribute));

	internal static Type TypeOfEnumMemberAttribute => s_typeOfEnumMemberAttribute ?? (s_typeOfEnumMemberAttribute = typeof(EnumMemberAttribute));

	internal static Type TypeOfCollectionDataContractAttribute => s_typeOfCollectionDataContractAttribute ?? (s_typeOfCollectionDataContractAttribute = typeof(CollectionDataContractAttribute));

	internal static Type TypeOfOptionalFieldAttribute => s_typeOfOptionalFieldAttribute ?? (s_typeOfOptionalFieldAttribute = typeof(OptionalFieldAttribute));

	internal static Type TypeOfObjectArray => s_typeOfObjectArray ?? (s_typeOfObjectArray = typeof(object[]));

	internal static Type TypeOfOnSerializingAttribute => s_typeOfOnSerializingAttribute ?? (s_typeOfOnSerializingAttribute = typeof(OnSerializingAttribute));

	internal static Type TypeOfOnSerializedAttribute => s_typeOfOnSerializedAttribute ?? (s_typeOfOnSerializedAttribute = typeof(OnSerializedAttribute));

	internal static Type TypeOfOnDeserializingAttribute => s_typeOfOnDeserializingAttribute ?? (s_typeOfOnDeserializingAttribute = typeof(OnDeserializingAttribute));

	internal static Type TypeOfOnDeserializedAttribute => s_typeOfOnDeserializedAttribute ?? (s_typeOfOnDeserializedAttribute = typeof(OnDeserializedAttribute));

	internal static Type TypeOfFlagsAttribute => s_typeOfFlagsAttribute ?? (s_typeOfFlagsAttribute = typeof(FlagsAttribute));

	internal static Type TypeOfIXmlSerializable => s_typeOfIXmlSerializable ?? (s_typeOfIXmlSerializable = typeof(IXmlSerializable));

	internal static Type TypeOfXmlSchemaProviderAttribute => s_typeOfXmlSchemaProviderAttribute ?? (s_typeOfXmlSchemaProviderAttribute = typeof(XmlSchemaProviderAttribute));

	internal static Type TypeOfXmlRootAttribute => s_typeOfXmlRootAttribute ?? (s_typeOfXmlRootAttribute = typeof(XmlRootAttribute));

	internal static Type TypeOfXmlQualifiedName => s_typeOfXmlQualifiedName ?? (s_typeOfXmlQualifiedName = typeof(XmlQualifiedName));

	internal static Type TypeOfXmlSchemaType => s_typeOfXmlSchemaType ?? (s_typeOfXmlSchemaType = typeof(XmlSchemaType));

	internal static Type TypeOfIExtensibleDataObject => s_typeOfIExtensibleDataObject ?? (s_typeOfIExtensibleDataObject = typeof(IExtensibleDataObject));

	internal static Type TypeOfExtensionDataObject => s_typeOfExtensionDataObject ?? (s_typeOfExtensionDataObject = typeof(ExtensionDataObject));

	internal static Type TypeOfISerializableDataNode => s_typeOfISerializableDataNode ?? (s_typeOfISerializableDataNode = typeof(ISerializableDataNode));

	internal static Type TypeOfClassDataNode => s_typeOfClassDataNode ?? (s_typeOfClassDataNode = typeof(ClassDataNode));

	internal static Type TypeOfCollectionDataNode => s_typeOfCollectionDataNode ?? (s_typeOfCollectionDataNode = typeof(CollectionDataNode));

	internal static Type TypeOfXmlDataNode => s_typeOfXmlDataNode ?? (s_typeOfXmlDataNode = typeof(XmlDataNode));

	internal static Type TypeOfNullable => s_typeOfNullable ?? (s_typeOfNullable = typeof(Nullable<>));

	internal static Type TypeOfReflectionPointer => s_typeOfReflectionPointer ?? (s_typeOfReflectionPointer = typeof(Pointer));

	internal static Type TypeOfIDictionaryGeneric => s_typeOfIDictionaryGeneric ?? (s_typeOfIDictionaryGeneric = typeof(IDictionary<, >));

	internal static Type TypeOfIDictionary => s_typeOfIDictionary ?? (s_typeOfIDictionary = typeof(IDictionary));

	internal static Type TypeOfIListGeneric => s_typeOfIListGeneric ?? (s_typeOfIListGeneric = typeof(IList<>));

	internal static Type TypeOfIList => s_typeOfIList ?? (s_typeOfIList = typeof(IList));

	internal static Type TypeOfICollectionGeneric => s_typeOfICollectionGeneric ?? (s_typeOfICollectionGeneric = typeof(ICollection<>));

	internal static Type TypeOfICollection => s_typeOfICollection ?? (s_typeOfICollection = typeof(ICollection));

	internal static Type TypeOfIEnumerableGeneric => s_typeOfIEnumerableGeneric ?? (s_typeOfIEnumerableGeneric = typeof(IEnumerable<>));

	internal static Type TypeOfIEnumerable => s_typeOfIEnumerable ?? (s_typeOfIEnumerable = typeof(IEnumerable));

	internal static Type TypeOfIEnumeratorGeneric => s_typeOfIEnumeratorGeneric ?? (s_typeOfIEnumeratorGeneric = typeof(IEnumerator<>));

	internal static Type TypeOfIEnumerator => s_typeOfIEnumerator ?? (s_typeOfIEnumerator = typeof(IEnumerator));

	internal static Type TypeOfKeyValuePair => s_typeOfKeyValuePair ?? (s_typeOfKeyValuePair = typeof(KeyValuePair<, >));

	internal static Type TypeOfKeyValue => s_typeOfKeyValue ?? (s_typeOfKeyValue = typeof(KeyValue<, >));

	internal static Type TypeOfIDictionaryEnumerator => s_typeOfIDictionaryEnumerator ?? (s_typeOfIDictionaryEnumerator = typeof(IDictionaryEnumerator));

	internal static Type TypeOfDictionaryEnumerator => s_typeOfDictionaryEnumerator ?? (s_typeOfDictionaryEnumerator = typeof(CollectionDataContract.DictionaryEnumerator));

	internal static Type TypeOfGenericDictionaryEnumerator => s_typeOfGenericDictionaryEnumerator ?? (s_typeOfGenericDictionaryEnumerator = typeof(CollectionDataContract.GenericDictionaryEnumerator<, >));

	internal static Type TypeOfDictionaryGeneric => s_typeOfDictionaryGeneric ?? (s_typeOfDictionaryGeneric = typeof(Dictionary<, >));

	internal static Type TypeOfHashtable
	{
		[RequiresDynamicCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.")]
		[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
		get
		{
			return s_typeOfHashtable ?? (s_typeOfHashtable = TypeOfDictionaryGeneric.MakeGenericType(TypeOfObject, TypeOfObject));
		}
	}

	internal static Type TypeOfXmlElement => s_typeOfXmlElement ?? (s_typeOfXmlElement = typeof(XmlElement));

	internal static Type TypeOfXmlNodeArray => s_typeOfXmlNodeArray ?? (s_typeOfXmlNodeArray = typeof(XmlNode[]));

	internal static Type TypeOfDBNull => s_typeOfDBNull ?? (s_typeOfDBNull = typeof(DBNull));

	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.NonPublicFields)]
	internal static Type TypeOfSchemaDefinedType => s_typeOfSchemaDefinedType ?? (s_typeOfSchemaDefinedType = typeof(SchemaDefinedType));

	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.NonPublicFields)]
	internal static Type TypeOfSchemaDefinedEnum => s_typeOfSchemaDefinedEnum ?? (s_typeOfSchemaDefinedEnum = typeof(SchemaDefinedEnum));

	internal static MemberInfo SchemaMemberInfoPlaceholder => s_schemaMemberInfoPlaceholder ?? (s_schemaMemberInfoPlaceholder = TypeOfSchemaDefinedType.GetField("_xmlName", BindingFlags.Instance | BindingFlags.NonPublic));

	internal static Uri DataContractXsdBaseNamespaceUri => s_dataContractXsdBaseNamespaceUri ?? (s_dataContractXsdBaseNamespaceUri = new Uri("http://schemas.datacontract.org/2004/07/"));

	[GeneratedRegex("^[\\s]*System\\.Runtime\\.Serialization[\\s]*,[\\s]*PublicKey[\\s]*=[\\s]*(?i:00240000048000009400000006020000002400005253413100040000010001008d56c76f9e8649383049f383c44be0ec204181822a6c31cf5eb7ef486944d032188ea1d3920763712ccb12d75fb77e9811149e6148e5d32fbaab37611c1878ddc19e20ef135d0cb2cff2bfec3d115810c3d9069638fe4be215dbf795861920e5ab6f7db2e2ceef136ac23d5dd2bf031700aec232f6c6b1c785b4305c123b37ab)[\\s]*$")]
	[GeneratedCode("System.Text.RegularExpressions.Generator", "8.0.10.26715")]
	public static Regex FullSRSInternalsVisibleRegex()
	{
		return _003CRegexGenerator_g_003EF74B1AE921BCEFE4BA601AA541C7A23B1CA9711EA81E8FE504B5B6446748E035A__FullSRSInternalsVisibleRegex_0.Instance;
	}
}
