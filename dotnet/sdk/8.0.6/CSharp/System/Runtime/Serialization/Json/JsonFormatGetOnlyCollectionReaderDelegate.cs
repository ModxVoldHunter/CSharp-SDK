using System.Runtime.Serialization.DataContracts;
using System.Xml;

namespace System.Runtime.Serialization.Json;

internal delegate void JsonFormatGetOnlyCollectionReaderDelegate(XmlReaderDelegator xmlReader, XmlObjectSerializerReadContextComplexJson context, XmlDictionaryString emptyDictionaryString, XmlDictionaryString itemName, CollectionDataContract collectionContract);
