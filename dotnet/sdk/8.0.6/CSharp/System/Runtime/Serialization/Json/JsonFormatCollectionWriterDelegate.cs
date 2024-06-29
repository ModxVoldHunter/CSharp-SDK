using System.Runtime.Serialization.DataContracts;

namespace System.Runtime.Serialization.Json;

internal delegate void JsonFormatCollectionWriterDelegate(XmlWriterDelegator xmlWriter, object obj, XmlObjectSerializerWriteContextComplexJson context, CollectionDataContract dataContract);
