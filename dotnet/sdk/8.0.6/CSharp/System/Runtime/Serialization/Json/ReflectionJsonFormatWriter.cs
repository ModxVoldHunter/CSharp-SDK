using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization.DataContracts;
using System.Xml;

namespace System.Runtime.Serialization.Json;

internal sealed class ReflectionJsonFormatWriter
{
	private readonly ReflectionJsonClassWriter _reflectionClassWriter = new ReflectionJsonClassWriter();

	[RequiresDynamicCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.")]
	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	public void ReflectionWriteClass(XmlWriterDelegator xmlWriter, object obj, XmlObjectSerializerWriteContextComplexJson context, ClassDataContract classContract, XmlDictionaryString[] memberNames)
	{
		_reflectionClassWriter.ReflectionWriteClass(xmlWriter, obj, context, classContract, memberNames);
	}

	[RequiresDynamicCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.")]
	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	public static void ReflectionWriteCollection(XmlWriterDelegator xmlWriter, object obj, XmlObjectSerializerWriteContextComplexJson context, CollectionDataContract collectionContract)
	{
		if (!(xmlWriter is JsonWriterDelegator jsonWriterDelegator))
		{
			throw new ArgumentException("xmlWriter");
		}
		XmlDictionaryString collectionItemName = XmlObjectSerializerWriteContextComplexJson.CollectionItemName;
		if (collectionContract.Kind == CollectionKind.Array)
		{
			context.IncrementArrayCount(jsonWriterDelegator, (Array)obj);
			Type itemType = collectionContract.ItemType;
			if (!ReflectionTryWritePrimitiveArray(jsonWriterDelegator, obj, itemType, collectionItemName))
			{
				ReflectionWriteArrayAttribute(jsonWriterDelegator);
				Array array = (Array)obj;
				PrimitiveDataContract primitiveDataContract = PrimitiveDataContract.GetPrimitiveDataContract(itemType);
				for (int i = 0; i < array.Length; i++)
				{
					ReflectionJsonClassWriter.ReflectionWriteStartElement(jsonWriterDelegator, collectionItemName);
					ReflectionClassWriter.ReflectionWriteValue(jsonWriterDelegator, context, itemType, array.GetValue(i), writeXsiType: false, primitiveDataContract);
					ReflectionJsonClassWriter.ReflectionWriteEndElement(jsonWriterDelegator);
				}
			}
			return;
		}
		collectionContract.IncrementCollectionCount(jsonWriterDelegator, obj, context);
		IEnumerator enumeratorForCollection = collectionContract.GetEnumeratorForCollection(obj);
		bool flag = collectionContract.Kind == CollectionKind.GenericDictionary || collectionContract.Kind == CollectionKind.Dictionary;
		bool useSimpleDictionaryFormat = context.UseSimpleDictionaryFormat;
		if (flag && useSimpleDictionaryFormat)
		{
			ReflectionWriteObjectAttribute(jsonWriterDelegator);
			Type[] genericArguments = collectionContract.ItemType.GetGenericArguments();
			Type type = ((genericArguments.Length == 2) ? genericArguments[1] : null);
			while (enumeratorForCollection.MoveNext())
			{
				object current = enumeratorForCollection.Current;
				object key = ((IKeyValue)current).Key;
				object value = ((IKeyValue)current).Value;
				ReflectionJsonClassWriter.ReflectionWriteStartElement(jsonWriterDelegator, key.ToString());
				ReflectionClassWriter.ReflectionWriteValue(jsonWriterDelegator, context, type ?? value.GetType(), value, writeXsiType: false, null);
				ReflectionJsonClassWriter.ReflectionWriteEndElement(jsonWriterDelegator);
			}
			return;
		}
		ReflectionWriteArrayAttribute(jsonWriterDelegator);
		PrimitiveDataContract primitiveDataContract2 = PrimitiveDataContract.GetPrimitiveDataContract(collectionContract.UnderlyingType);
		if (primitiveDataContract2 != null && primitiveDataContract2.UnderlyingType != Globals.TypeOfObject)
		{
			while (enumeratorForCollection.MoveNext())
			{
				object current2 = enumeratorForCollection.Current;
				context.IncrementItemCount(1);
				primitiveDataContract2.WriteXmlElement(jsonWriterDelegator, current2, context, collectionItemName, null);
			}
			return;
		}
		Type collectionElementType = collectionContract.GetCollectionElementType();
		bool flag2 = collectionContract.Kind == CollectionKind.Dictionary || collectionContract.Kind == CollectionKind.GenericDictionary;
		JsonDataContract jsonDataContract = null;
		if (flag2)
		{
			DataContract revisedItemContract = XmlObjectSerializerWriteContextComplexJson.GetRevisedItemContract(collectionContract.ItemContract);
			jsonDataContract = JsonDataContract.GetJsonDataContract(revisedItemContract);
		}
		while (enumeratorForCollection.MoveNext())
		{
			object current3 = enumeratorForCollection.Current;
			context.IncrementItemCount(1);
			ReflectionJsonClassWriter.ReflectionWriteStartElement(jsonWriterDelegator, collectionItemName);
			if (flag2)
			{
				jsonDataContract.WriteJsonValue(jsonWriterDelegator, current3, context, collectionContract.ItemType.TypeHandle);
			}
			else
			{
				ReflectionClassWriter.ReflectionWriteValue(jsonWriterDelegator, context, collectionElementType, current3, writeXsiType: false, null);
			}
			ReflectionJsonClassWriter.ReflectionWriteEndElement(jsonWriterDelegator);
		}
	}

	private static void ReflectionWriteObjectAttribute(XmlWriterDelegator xmlWriter)
	{
		xmlWriter.WriteAttributeString(null, "type", null, "object");
	}

	[RequiresDynamicCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed.")]
	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	private static bool ReflectionTryWritePrimitiveArray(JsonWriterDelegator jsonWriter, object obj, Type itemType, XmlDictionaryString collectionItemName)
	{
		PrimitiveDataContract primitiveDataContract = PrimitiveDataContract.GetPrimitiveDataContract(itemType);
		if (primitiveDataContract == null)
		{
			return false;
		}
		XmlDictionaryString itemNamespace = null;
		switch (Type.GetTypeCode(itemType))
		{
		case TypeCode.Boolean:
			ReflectionWriteArrayAttribute(jsonWriter);
			jsonWriter.WriteJsonBooleanArray((bool[])obj, collectionItemName, itemNamespace);
			break;
		case TypeCode.DateTime:
			ReflectionWriteArrayAttribute(jsonWriter);
			jsonWriter.WriteJsonDateTimeArray((DateTime[])obj, collectionItemName, itemNamespace);
			break;
		case TypeCode.Decimal:
			ReflectionWriteArrayAttribute(jsonWriter);
			jsonWriter.WriteJsonDecimalArray((decimal[])obj, collectionItemName, itemNamespace);
			break;
		case TypeCode.Int32:
			ReflectionWriteArrayAttribute(jsonWriter);
			jsonWriter.WriteJsonInt32Array((int[])obj, collectionItemName, itemNamespace);
			break;
		case TypeCode.Int64:
			ReflectionWriteArrayAttribute(jsonWriter);
			jsonWriter.WriteJsonInt64Array((long[])obj, collectionItemName, itemNamespace);
			break;
		case TypeCode.Single:
			ReflectionWriteArrayAttribute(jsonWriter);
			jsonWriter.WriteJsonSingleArray((float[])obj, collectionItemName, itemNamespace);
			break;
		case TypeCode.Double:
			ReflectionWriteArrayAttribute(jsonWriter);
			jsonWriter.WriteJsonDoubleArray((double[])obj, collectionItemName, itemNamespace);
			break;
		default:
			return false;
		}
		return true;
	}

	private static void ReflectionWriteArrayAttribute(XmlWriterDelegator xmlWriter)
	{
		xmlWriter.WriteAttributeString(null, "type", string.Empty, "array");
	}
}
