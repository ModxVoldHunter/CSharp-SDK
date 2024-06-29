using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace System.Xml;

public class XmlDictionaryString
{
	private sealed class EmptyStringDictionary : IXmlDictionary
	{
		private readonly XmlDictionaryString _empty;

		public XmlDictionaryString EmptyString => _empty;

		public EmptyStringDictionary()
		{
			_empty = new XmlDictionaryString(this, string.Empty, 0);
		}

		public bool TryLookup(string value, [NotNullWhen(true)] out XmlDictionaryString result)
		{
			ArgumentNullException.ThrowIfNull(value, "value");
			if (value.Length == 0)
			{
				result = _empty;
				return true;
			}
			result = null;
			return false;
		}

		public bool TryLookup(int key, [NotNullWhen(true)] out XmlDictionaryString result)
		{
			if (key == 0)
			{
				result = _empty;
				return true;
			}
			result = null;
			return false;
		}

		public bool TryLookup(XmlDictionaryString value, [NotNullWhen(true)] out XmlDictionaryString result)
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
	}

	internal const int MinKey = 0;

	internal const int MaxKey = 536870911;

	private readonly IXmlDictionary _dictionary;

	private readonly string _value;

	private readonly int _key;

	private byte[] _buffer;

	private static readonly EmptyStringDictionary s_emptyStringDictionary = new EmptyStringDictionary();

	public static XmlDictionaryString Empty => s_emptyStringDictionary.EmptyString;

	public IXmlDictionary Dictionary => _dictionary;

	public int Key => _key;

	public string Value => _value;

	public XmlDictionaryString(IXmlDictionary dictionary, string value, int key)
	{
		ArgumentNullException.ThrowIfNull(dictionary, "dictionary");
		ArgumentNullException.ThrowIfNull(value, "value");
		if (key < 0 || key > 536870911)
		{
			throw new ArgumentOutOfRangeException("key", System.SR.Format(System.SR.ValueMustBeInRange, 0, 536870911));
		}
		_dictionary = dictionary;
		_value = value;
		_key = key;
	}

	[return: NotNullIfNotNull("s")]
	internal static string GetString(XmlDictionaryString s)
	{
		return s?.Value;
	}

	internal byte[] ToUTF8()
	{
		return _buffer ?? (_buffer = Encoding.UTF8.GetBytes(_value));
	}

	public override string ToString()
	{
		return _value;
	}
}
