using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace System.Xml;

public class XmlBinaryReaderSession : IXmlDictionary
{
	private const int MaxArrayEntries = 2048;

	private XmlDictionaryString[] _strings;

	private Dictionary<int, XmlDictionaryString> _stringDict;

	public XmlDictionaryString Add(int id, string value)
	{
		if (id < 0)
		{
			throw new ArgumentOutOfRangeException("id", System.SR.XmlInvalidID);
		}
		ArgumentNullException.ThrowIfNull(value, "value");
		if (TryLookup(id, out XmlDictionaryString _))
		{
			throw new InvalidOperationException(System.SR.XmlIDDefined);
		}
		XmlDictionaryString xmlDictionaryString = new XmlDictionaryString(this, value, id);
		if (id >= 2048)
		{
			if (_stringDict == null)
			{
				_stringDict = new Dictionary<int, XmlDictionaryString>();
			}
			_stringDict.Add(id, xmlDictionaryString);
		}
		else
		{
			if (_strings == null)
			{
				_strings = new XmlDictionaryString[Math.Max(id + 1, 16)];
			}
			else if (id >= _strings.Length)
			{
				XmlDictionaryString[] array = new XmlDictionaryString[Math.Min(Math.Max(id + 1, _strings.Length * 2), 2048)];
				Array.Copy(_strings, array, _strings.Length);
				_strings = array;
			}
			_strings[id] = xmlDictionaryString;
		}
		return xmlDictionaryString;
	}

	public bool TryLookup(int key, [NotNullWhen(true)] out XmlDictionaryString? result)
	{
		if (_strings != null && key >= 0 && key < _strings.Length)
		{
			result = _strings[key];
			return result != null;
		}
		if (key >= 2048 && _stringDict != null)
		{
			return _stringDict.TryGetValue(key, out result);
		}
		result = null;
		return false;
	}

	public bool TryLookup(string value, [NotNullWhen(true)] out XmlDictionaryString? result)
	{
		ArgumentNullException.ThrowIfNull(value, "value");
		if (_strings != null)
		{
			for (int i = 0; i < _strings.Length; i++)
			{
				XmlDictionaryString xmlDictionaryString = _strings[i];
				if (xmlDictionaryString != null && xmlDictionaryString.Value == value)
				{
					result = xmlDictionaryString;
					return true;
				}
			}
		}
		if (_stringDict != null)
		{
			foreach (XmlDictionaryString value2 in _stringDict.Values)
			{
				if (value2.Value == value)
				{
					result = value2;
					return true;
				}
			}
		}
		result = null;
		return false;
	}

	public bool TryLookup(XmlDictionaryString value, [NotNullWhen(true)] out XmlDictionaryString? result)
	{
		ArgumentNullException.ThrowIfNull(value, "value");
		if (value.Dictionary != this)
		{
			result = null;
			return false;
		}
		result = value;
		return true;
	}

	public void Clear()
	{
		if (_strings != null)
		{
			Array.Clear(_strings);
		}
		_stringDict?.Clear();
	}
}
