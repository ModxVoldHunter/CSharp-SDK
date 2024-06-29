using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.Serialization.DataContracts;
using System.Xml;

namespace System.Runtime.Serialization;

internal sealed class ExtensionDataReader : XmlReader
{
	private enum ExtensionDataNodeType
	{
		None,
		Element,
		EndElement,
		Text,
		Xml,
		ReferencedElement,
		NullElement
	}

	private ElementData[] _elements;

	private ElementData _element;

	private ElementData _nextElement;

	private ReadState _readState;

	private ExtensionDataNodeType _internalNodeType;

	private XmlNodeType _nodeType;

	private int _depth;

	private string _localName;

	private string _ns;

	private string _prefix;

	private string _value;

	private int _attributeCount;

	private int _attributeIndex;

	private readonly Hashtable _cache = new Hashtable();

	private XmlNodeReader _xmlNodeReader;

	private Queue<IDataNode> _deserializedDataNodes;

	private static readonly object s_prefixLock;

	private readonly XmlObjectSerializerReadContext _context;

	private static readonly Hashtable s_nsToPrefixTable;

	private static readonly Hashtable s_prefixToNsTable;

	[MemberNotNullWhen(true, "_xmlNodeReader")]
	[MemberNotNullWhen(false, "_element")]
	private bool IsXmlDataNode
	{
		[MemberNotNullWhen(true, "_xmlNodeReader")]
		[MemberNotNullWhen(false, "_element")]
		get
		{
			return _internalNodeType == ExtensionDataNodeType.Xml;
		}
	}

	public override XmlNodeType NodeType
	{
		get
		{
			if (!IsXmlDataNode)
			{
				return _nodeType;
			}
			return _xmlNodeReader.NodeType;
		}
	}

	public override string LocalName
	{
		get
		{
			if (!IsXmlDataNode)
			{
				return _localName;
			}
			return _xmlNodeReader.LocalName;
		}
	}

	public override string NamespaceURI
	{
		get
		{
			if (!IsXmlDataNode)
			{
				return _ns;
			}
			return _xmlNodeReader.NamespaceURI;
		}
	}

	public override string Prefix
	{
		get
		{
			if (!IsXmlDataNode)
			{
				return _prefix;
			}
			return _xmlNodeReader.Prefix;
		}
	}

	public override string Value
	{
		get
		{
			if (!IsXmlDataNode)
			{
				return _value;
			}
			return _xmlNodeReader.Value;
		}
	}

	public override int Depth
	{
		get
		{
			if (!IsXmlDataNode)
			{
				return _depth;
			}
			return _xmlNodeReader.Depth;
		}
	}

	public override int AttributeCount
	{
		get
		{
			if (!IsXmlDataNode)
			{
				return _attributeCount;
			}
			return _xmlNodeReader.AttributeCount;
		}
	}

	public override bool EOF
	{
		get
		{
			if (!IsXmlDataNode)
			{
				return _readState == ReadState.EndOfFile;
			}
			return _xmlNodeReader.EOF;
		}
	}

	public override ReadState ReadState
	{
		get
		{
			if (!IsXmlDataNode)
			{
				return _readState;
			}
			return _xmlNodeReader.ReadState;
		}
	}

	public override bool IsEmptyElement
	{
		get
		{
			if (!IsXmlDataNode)
			{
				return false;
			}
			return _xmlNodeReader.IsEmptyElement;
		}
	}

	public override bool IsDefault
	{
		get
		{
			if (!IsXmlDataNode)
			{
				return base.IsDefault;
			}
			return _xmlNodeReader.IsDefault;
		}
	}

	public override XmlSpace XmlSpace
	{
		get
		{
			if (!IsXmlDataNode)
			{
				return base.XmlSpace;
			}
			return _xmlNodeReader.XmlSpace;
		}
	}

	public override string XmlLang
	{
		get
		{
			if (!IsXmlDataNode)
			{
				return base.XmlLang;
			}
			return _xmlNodeReader.XmlLang;
		}
	}

	public override string this[int i]
	{
		get
		{
			if (!IsXmlDataNode)
			{
				return GetAttribute(i);
			}
			return _xmlNodeReader[i];
		}
	}

	public override string this[string name]
	{
		get
		{
			if (!IsXmlDataNode)
			{
				return GetAttribute(name);
			}
			return _xmlNodeReader[name];
		}
	}

	public override string this[string name, string namespaceURI]
	{
		get
		{
			if (!IsXmlDataNode)
			{
				return GetAttribute(name, namespaceURI);
			}
			return _xmlNodeReader[name, namespaceURI];
		}
	}

	public override string Name
	{
		get
		{
			if (IsXmlDataNode)
			{
				return _xmlNodeReader.Name;
			}
			return string.Empty;
		}
	}

	public override bool HasValue
	{
		get
		{
			if (IsXmlDataNode)
			{
				return _xmlNodeReader.HasValue;
			}
			return false;
		}
	}

	public override string BaseURI
	{
		get
		{
			if (IsXmlDataNode)
			{
				return _xmlNodeReader.BaseURI;
			}
			return string.Empty;
		}
	}

	public override XmlNameTable NameTable
	{
		get
		{
			if (IsXmlDataNode)
			{
				return _xmlNodeReader.NameTable;
			}
			return null;
		}
	}

	static ExtensionDataReader()
	{
		s_prefixLock = new object();
		s_nsToPrefixTable = new Hashtable();
		s_prefixToNsTable = new Hashtable();
		AddPrefix("i", "http://www.w3.org/2001/XMLSchema-instance");
		AddPrefix("z", "http://schemas.microsoft.com/2003/10/Serialization/");
		AddPrefix(string.Empty, string.Empty);
	}

	internal ExtensionDataReader(XmlObjectSerializerReadContext context)
	{
		_attributeIndex = -1;
		_context = context;
	}

	internal void SetDeserializedValue(object obj)
	{
		IDataNode dataNode = ((_deserializedDataNodes == null || _deserializedDataNodes.Count == 0) ? null : _deserializedDataNodes.Dequeue());
		if (dataNode != null && !(obj is IDataNode))
		{
			dataNode.Value = obj;
			dataNode.IsFinalValue = true;
		}
	}

	internal IDataNode GetCurrentNode()
	{
		IDataNode dataNode = _element.dataNode;
		Skip();
		return dataNode;
	}

	internal void SetDataNode(IDataNode dataNode, string name, string ns)
	{
		SetNextElement(dataNode, name, ns, null);
		_element = _nextElement;
		_nextElement = null;
		SetElement();
	}

	internal void Reset()
	{
		_localName = null;
		_ns = null;
		_prefix = null;
		_value = null;
		_attributeCount = 0;
		_attributeIndex = -1;
		_depth = 0;
		_element = null;
		_nextElement = null;
		_elements = null;
	}

	public override bool MoveToFirstAttribute()
	{
		if (IsXmlDataNode)
		{
			return _xmlNodeReader.MoveToFirstAttribute();
		}
		if (_attributeCount == 0)
		{
			return false;
		}
		MoveToAttribute(0);
		return true;
	}

	public override bool MoveToNextAttribute()
	{
		if (IsXmlDataNode)
		{
			return _xmlNodeReader.MoveToNextAttribute();
		}
		if (_attributeIndex + 1 >= _attributeCount)
		{
			return false;
		}
		MoveToAttribute(_attributeIndex + 1);
		return true;
	}

	public override void MoveToAttribute(int index)
	{
		if (IsXmlDataNode)
		{
			_xmlNodeReader.MoveToAttribute(index);
			return;
		}
		if (index < 0 || index >= _attributeCount)
		{
			throw new XmlException(System.SR.InvalidXmlDeserializingExtensionData);
		}
		_nodeType = XmlNodeType.Attribute;
		AttributeData attributeData = _element.attributes[index];
		_localName = attributeData.localName;
		_ns = attributeData.ns;
		_prefix = attributeData.prefix;
		_value = attributeData.value;
		_attributeIndex = index;
	}

	public override string GetAttribute(string name, string namespaceURI)
	{
		if (IsXmlDataNode)
		{
			return _xmlNodeReader.GetAttribute(name, namespaceURI);
		}
		for (int i = 0; i < _element.attributeCount; i++)
		{
			AttributeData attributeData = _element.attributes[i];
			if (attributeData.localName == name && attributeData.ns == namespaceURI)
			{
				return attributeData.value;
			}
		}
		return null;
	}

	public override bool MoveToAttribute(string name, string namespaceURI)
	{
		if (IsXmlDataNode)
		{
			return _xmlNodeReader.MoveToAttribute(name, _ns);
		}
		for (int i = 0; i < _element.attributeCount; i++)
		{
			AttributeData attributeData = _element.attributes[i];
			if (attributeData.localName == name && attributeData.ns == namespaceURI)
			{
				MoveToAttribute(i);
				return true;
			}
		}
		return false;
	}

	public override bool MoveToElement()
	{
		if (IsXmlDataNode)
		{
			return _xmlNodeReader.MoveToElement();
		}
		if (_nodeType != XmlNodeType.Attribute)
		{
			return false;
		}
		SetElement();
		return true;
	}

	private void SetElement()
	{
		_nodeType = XmlNodeType.Element;
		_localName = _element.localName;
		_ns = _element.ns;
		_prefix = _element.prefix;
		_value = string.Empty;
		_attributeCount = _element.attributeCount;
		_attributeIndex = -1;
	}

	public override string LookupNamespace(string prefix)
	{
		if (IsXmlDataNode)
		{
			return _xmlNodeReader.LookupNamespace(prefix);
		}
		return (string)s_prefixToNsTable[prefix];
	}

	public override void Skip()
	{
		if (IsXmlDataNode)
		{
			_xmlNodeReader.Skip();
		}
		else
		{
			if (ReadState != ReadState.Interactive)
			{
				return;
			}
			MoveToElement();
			if (IsElementNode(_internalNodeType))
			{
				int num = 1;
				while (num != 0)
				{
					if (!Read())
					{
						throw new XmlException(System.SR.InvalidXmlDeserializingExtensionData);
					}
					if (IsElementNode(_internalNodeType))
					{
						num++;
					}
					else if (_internalNodeType == ExtensionDataNodeType.EndElement)
					{
						ReadEndElement();
						num--;
					}
				}
			}
			else
			{
				Read();
			}
		}
	}

	private static bool IsElementNode(ExtensionDataNodeType nodeType)
	{
		if (nodeType != ExtensionDataNodeType.Element && nodeType != ExtensionDataNodeType.ReferencedElement)
		{
			return nodeType == ExtensionDataNodeType.NullElement;
		}
		return true;
	}

	protected override void Dispose(bool disposing)
	{
		if (IsXmlDataNode)
		{
			_xmlNodeReader.Dispose();
		}
		else
		{
			Reset();
			_readState = ReadState.Closed;
		}
		base.Dispose(disposing);
	}

	public override bool Read()
	{
		if (_nodeType == XmlNodeType.Attribute && MoveToNextAttribute())
		{
			return true;
		}
		MoveNext(_element.dataNode);
		switch (_internalNodeType)
		{
		case ExtensionDataNodeType.Element:
		case ExtensionDataNodeType.ReferencedElement:
		case ExtensionDataNodeType.NullElement:
			PushElement();
			SetElement();
			break;
		case ExtensionDataNodeType.Text:
			_nodeType = XmlNodeType.Text;
			_prefix = string.Empty;
			_ns = string.Empty;
			_localName = string.Empty;
			_attributeCount = 0;
			_attributeIndex = -1;
			break;
		case ExtensionDataNodeType.EndElement:
			_nodeType = XmlNodeType.EndElement;
			_prefix = string.Empty;
			_ns = string.Empty;
			_localName = string.Empty;
			_value = string.Empty;
			_attributeCount = 0;
			_attributeIndex = -1;
			PopElement();
			break;
		case ExtensionDataNodeType.None:
			if (_depth != 0)
			{
				throw new XmlException(System.SR.InvalidXmlDeserializingExtensionData);
			}
			_nodeType = XmlNodeType.None;
			_prefix = string.Empty;
			_ns = string.Empty;
			_localName = string.Empty;
			_value = string.Empty;
			_attributeCount = 0;
			_readState = ReadState.EndOfFile;
			return false;
		default:
			throw new SerializationException(System.SR.InvalidStateInExtensionDataReader);
		case ExtensionDataNodeType.Xml:
			break;
		}
		_readState = ReadState.Interactive;
		return true;
	}

	public override string GetAttribute(string name)
	{
		if (IsXmlDataNode)
		{
			return _xmlNodeReader.GetAttribute(name);
		}
		return null;
	}

	public override string GetAttribute(int i)
	{
		if (IsXmlDataNode)
		{
			return _xmlNodeReader.GetAttribute(i);
		}
		return null;
	}

	public override bool MoveToAttribute(string name)
	{
		if (IsXmlDataNode)
		{
			return _xmlNodeReader.MoveToAttribute(name);
		}
		return false;
	}

	public override void ResolveEntity()
	{
		if (IsXmlDataNode)
		{
			_xmlNodeReader.ResolveEntity();
		}
	}

	public override bool ReadAttributeValue()
	{
		if (IsXmlDataNode)
		{
			return _xmlNodeReader.ReadAttributeValue();
		}
		return false;
	}

	private void MoveNext(IDataNode dataNode)
	{
		ExtensionDataNodeType internalNodeType = _internalNodeType;
		if (internalNodeType == ExtensionDataNodeType.Text || (uint)(internalNodeType - 5) <= 1u)
		{
			_internalNodeType = ExtensionDataNodeType.EndElement;
			return;
		}
		Type type = dataNode?.DataType;
		if (type == Globals.TypeOfClassDataNode)
		{
			MoveNextInClass((ClassDataNode)dataNode);
			return;
		}
		if (type == Globals.TypeOfCollectionDataNode)
		{
			MoveNextInCollection((CollectionDataNode)dataNode);
			return;
		}
		if (type == Globals.TypeOfISerializableDataNode)
		{
			MoveNextInISerializable((ISerializableDataNode)dataNode);
			return;
		}
		if (type == Globals.TypeOfXmlDataNode)
		{
			MoveNextInXml((XmlDataNode)dataNode);
			return;
		}
		if (dataNode != null && dataNode.Value != null)
		{
			MoveToDeserializedObject(dataNode);
			return;
		}
		throw new SerializationException(System.SR.Format(System.SR.InvalidStateInExtensionDataReader));
	}

	private void SetNextElement(IDataNode node, string name, string ns, string prefix)
	{
		_internalNodeType = ExtensionDataNodeType.Element;
		_nextElement = GetNextElement();
		_nextElement.localName = name;
		_nextElement.ns = ns;
		_nextElement.prefix = prefix;
		if (node == null)
		{
			_nextElement.attributeCount = 0;
			_nextElement.AddAttribute("i", "http://www.w3.org/2001/XMLSchema-instance", "nil", "true");
			_internalNodeType = ExtensionDataNodeType.NullElement;
		}
		else if (!CheckIfNodeHandled(node))
		{
			AddDeserializedDataNode(node);
			node.GetData(_nextElement);
			if (node is XmlDataNode dataNode)
			{
				MoveNextInXml(dataNode);
			}
		}
	}

	private void AddDeserializedDataNode(IDataNode node)
	{
		if (node.Id != Globals.NewObjectId && (node.Value == null || !node.IsFinalValue))
		{
			if (_deserializedDataNodes == null)
			{
				_deserializedDataNodes = new Queue<IDataNode>();
			}
			_deserializedDataNodes.Enqueue(node);
		}
	}

	private bool CheckIfNodeHandled(IDataNode node)
	{
		bool flag = false;
		if (node.Id != Globals.NewObjectId)
		{
			flag = _cache[node] != null;
			if (flag)
			{
				if (_nextElement == null)
				{
					_nextElement = GetNextElement();
				}
				_nextElement.attributeCount = 0;
				_nextElement.AddAttribute("z", "http://schemas.microsoft.com/2003/10/Serialization/", "Ref", node.Id.ToString(NumberFormatInfo.InvariantInfo));
				_nextElement.AddAttribute("i", "http://www.w3.org/2001/XMLSchema-instance", "nil", "true");
				_internalNodeType = ExtensionDataNodeType.ReferencedElement;
			}
			else
			{
				_cache.Add(node, node);
			}
		}
		return flag;
	}

	private void MoveNextInClass(ClassDataNode dataNode)
	{
		if (dataNode.Members != null && _element.childElementIndex < dataNode.Members.Count)
		{
			if (_element.childElementIndex == 0)
			{
				_context.IncrementItemCount(-dataNode.Members.Count);
			}
			ExtensionDataMember extensionDataMember = dataNode.Members[_element.childElementIndex++];
			SetNextElement(extensionDataMember.Value, extensionDataMember.Name, extensionDataMember.Namespace, GetPrefix(extensionDataMember.Namespace));
		}
		else
		{
			_internalNodeType = ExtensionDataNodeType.EndElement;
			_element.childElementIndex = 0;
		}
	}

	private void MoveNextInCollection(CollectionDataNode dataNode)
	{
		if (dataNode.Items != null && _element.childElementIndex < dataNode.Items.Count)
		{
			if (_element.childElementIndex == 0)
			{
				_context.IncrementItemCount(-dataNode.Items.Count);
			}
			IDataNode node = dataNode.Items[_element.childElementIndex++];
			SetNextElement(node, dataNode.ItemName, dataNode.ItemNamespace, GetPrefix(dataNode.ItemNamespace));
		}
		else
		{
			_internalNodeType = ExtensionDataNodeType.EndElement;
			_element.childElementIndex = 0;
		}
	}

	private void MoveNextInISerializable(ISerializableDataNode dataNode)
	{
		if (dataNode.Members != null && _element.childElementIndex < dataNode.Members.Count)
		{
			if (_element.childElementIndex == 0)
			{
				_context.IncrementItemCount(-dataNode.Members.Count);
			}
			ISerializableDataMember serializableDataMember = dataNode.Members[_element.childElementIndex++];
			SetNextElement(serializableDataMember.Value, serializableDataMember.Name, string.Empty, string.Empty);
		}
		else
		{
			_internalNodeType = ExtensionDataNodeType.EndElement;
			_element.childElementIndex = 0;
		}
	}

	private void MoveNextInXml(XmlDataNode dataNode)
	{
		if (IsXmlDataNode)
		{
			_xmlNodeReader.Read();
			if (_xmlNodeReader.Depth == 0)
			{
				_internalNodeType = ExtensionDataNodeType.EndElement;
				_xmlNodeReader = null;
			}
			return;
		}
		_internalNodeType = ExtensionDataNodeType.Xml;
		if (_element == null)
		{
			_element = _nextElement;
		}
		else
		{
			PushElement();
		}
		XmlElement xmlElement = XmlObjectSerializerReadContext.CreateWrapperXmlElement(dataNode.OwnerDocument, dataNode.XmlAttributes, dataNode.XmlChildNodes, _element?.prefix, _element?.localName, _element?.ns);
		if (_element != null)
		{
			for (int i = 0; i < _element.attributeCount; i++)
			{
				AttributeData attributeData = _element.attributes[i];
				XmlAttribute xmlAttribute = dataNode.OwnerDocument.CreateAttribute(attributeData.prefix, attributeData.localName, attributeData.ns);
				xmlAttribute.Value = attributeData.value;
				xmlElement.Attributes.Append(xmlAttribute);
			}
		}
		_xmlNodeReader = new XmlNodeReader(xmlElement);
		_xmlNodeReader.Read();
	}

	private void MoveToDeserializedObject(IDataNode dataNode)
	{
		Type type = dataNode.DataType;
		bool isTypedNode = true;
		if (type == Globals.TypeOfObject && dataNode.Value != null)
		{
			type = dataNode.Value.GetType();
			if (type == Globals.TypeOfObject)
			{
				_internalNodeType = ExtensionDataNodeType.EndElement;
				return;
			}
			isTypedNode = false;
		}
		if (!MoveToText(type, dataNode, isTypedNode))
		{
			if (!dataNode.IsFinalValue)
			{
				throw new XmlException(System.SR.Format(System.SR.InvalidDataNode, DataContract.GetClrTypeFullName(type)));
			}
			_internalNodeType = ExtensionDataNodeType.EndElement;
		}
	}

	private bool MoveToText(Type type, IDataNode dataNode, bool isTypedNode)
	{
		bool flag = true;
		switch (Type.GetTypeCode(type))
		{
		case TypeCode.Boolean:
			_value = XmlConvert.ToString(isTypedNode ? ((DataNode<bool>)dataNode).GetValue() : ((bool)dataNode.Value));
			break;
		case TypeCode.Char:
			_value = XmlConvert.ToString((int)(isTypedNode ? ((DataNode<char>)dataNode).GetValue() : ((char)dataNode.Value)));
			break;
		case TypeCode.Byte:
			_value = XmlConvert.ToString(isTypedNode ? ((DataNode<byte>)dataNode).GetValue() : ((byte)dataNode.Value));
			break;
		case TypeCode.Int16:
			_value = XmlConvert.ToString(isTypedNode ? ((DataNode<short>)dataNode).GetValue() : ((short)dataNode.Value));
			break;
		case TypeCode.Int32:
			_value = XmlConvert.ToString(isTypedNode ? ((DataNode<int>)dataNode).GetValue() : ((int)dataNode.Value));
			break;
		case TypeCode.Int64:
			_value = XmlConvert.ToString(isTypedNode ? ((DataNode<long>)dataNode).GetValue() : ((long)dataNode.Value));
			break;
		case TypeCode.Single:
			_value = XmlConvert.ToString(isTypedNode ? ((DataNode<float>)dataNode).GetValue() : ((float)dataNode.Value));
			break;
		case TypeCode.Double:
			_value = XmlConvert.ToString(isTypedNode ? ((DataNode<double>)dataNode).GetValue() : ((double)dataNode.Value));
			break;
		case TypeCode.Decimal:
			_value = XmlConvert.ToString(isTypedNode ? ((DataNode<decimal>)dataNode).GetValue() : ((decimal)dataNode.Value));
			break;
		case TypeCode.DateTime:
			_value = (isTypedNode ? ((DataNode<DateTime>)dataNode).GetValue() : ((DateTime)dataNode.Value)).ToString("yyyy-MM-ddTHH:mm:ss.fffffffK", DateTimeFormatInfo.InvariantInfo);
			break;
		case TypeCode.String:
			_value = (isTypedNode ? ((DataNode<string>)dataNode).GetValue() : ((string)dataNode.Value));
			break;
		case TypeCode.SByte:
			_value = XmlConvert.ToString(isTypedNode ? ((DataNode<sbyte>)dataNode).GetValue() : ((sbyte)dataNode.Value));
			break;
		case TypeCode.UInt16:
			_value = XmlConvert.ToString(isTypedNode ? ((DataNode<ushort>)dataNode).GetValue() : ((ushort)dataNode.Value));
			break;
		case TypeCode.UInt32:
			_value = XmlConvert.ToString(isTypedNode ? ((DataNode<uint>)dataNode).GetValue() : ((uint)dataNode.Value));
			break;
		case TypeCode.UInt64:
			_value = XmlConvert.ToString(isTypedNode ? ((DataNode<ulong>)dataNode).GetValue() : ((ulong)dataNode.Value));
			break;
		default:
			if (type == Globals.TypeOfByteArray)
			{
				byte[] array = (isTypedNode ? ((DataNode<byte[]>)dataNode).GetValue() : ((byte[])dataNode.Value));
				_value = ((array == null) ? string.Empty : Convert.ToBase64String(array));
			}
			else if (type == Globals.TypeOfTimeSpan)
			{
				_value = XmlConvert.ToString(isTypedNode ? ((DataNode<TimeSpan>)dataNode).GetValue() : ((TimeSpan)dataNode.Value));
			}
			else if (type == Globals.TypeOfGuid)
			{
				_value = (isTypedNode ? ((DataNode<Guid>)dataNode).GetValue() : ((Guid)dataNode.Value)).ToString();
			}
			else if (type == Globals.TypeOfUri)
			{
				Uri uri = (isTypedNode ? ((DataNode<Uri>)dataNode).GetValue() : ((Uri)dataNode.Value));
				_value = uri.GetComponents(UriComponents.SerializationInfoString, UriFormat.UriEscaped);
			}
			else
			{
				flag = false;
			}
			break;
		}
		if (flag)
		{
			_internalNodeType = ExtensionDataNodeType.Text;
		}
		return flag;
	}

	private void PushElement()
	{
		GrowElementsIfNeeded();
		_elements[_depth++] = _element;
		if (_nextElement == null)
		{
			_element = GetNextElement();
			return;
		}
		_element = _nextElement;
		_nextElement = null;
	}

	private void PopElement()
	{
		_prefix = _element.prefix;
		_localName = _element.localName;
		_ns = _element.ns;
		if (_depth != 0)
		{
			_depth--;
			if (_elements != null)
			{
				_element = _elements[_depth];
			}
		}
	}

	[MemberNotNull("_elements")]
	private void GrowElementsIfNeeded()
	{
		if (_elements == null)
		{
			_elements = new ElementData[8];
		}
		else if (_elements.Length == _depth)
		{
			ElementData[] array = new ElementData[_elements.Length * 2];
			Array.Copy(_elements, array, _elements.Length);
			_elements = array;
		}
	}

	private ElementData GetNextElement()
	{
		int num = _depth + 1;
		if (_elements != null && _elements.Length > num && _elements[num] != null)
		{
			return _elements[num];
		}
		return new ElementData();
	}

	internal static string GetPrefix(string ns)
	{
		if (ns == null)
		{
			ns = string.Empty;
		}
		string text = (string)s_nsToPrefixTable[ns];
		if (text == null)
		{
			lock (s_prefixLock)
			{
				text = (string)s_nsToPrefixTable[ns];
				if (text == null)
				{
					text = ((ns.Length == 0) ? string.Empty : ("p" + s_nsToPrefixTable.Count));
					AddPrefix(text, ns);
				}
			}
		}
		return text;
	}

	private static void AddPrefix(string prefix, string ns)
	{
		s_nsToPrefixTable.Add(ns, prefix);
		s_prefixToNsTable.Add(prefix, ns);
	}
}
