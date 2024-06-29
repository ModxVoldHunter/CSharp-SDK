using System.Diagnostics.CodeAnalysis;

namespace System.Xml.Serialization;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Enum | AttributeTargets.Interface | AttributeTargets.ReturnValue)]
public class XmlRootAttribute : Attribute
{
	private string _elementName;

	private string _ns;

	private string _dataType;

	private bool _nullable = true;

	private bool _nullableSpecified;

	public string ElementName
	{
		get
		{
			return _elementName ?? string.Empty;
		}
		set
		{
			_elementName = value;
		}
	}

	public string? Namespace
	{
		get
		{
			return _ns;
		}
		set
		{
			_ns = value;
		}
	}

	public string DataType
	{
		get
		{
			return _dataType ?? string.Empty;
		}
		[param: AllowNull]
		set
		{
			_dataType = value;
		}
	}

	public bool IsNullable
	{
		get
		{
			return _nullable;
		}
		set
		{
			_nullable = value;
			_nullableSpecified = true;
		}
	}

	internal bool IsNullableSpecified => _nullableSpecified;

	internal string Key => $"{_ns ?? string.Empty}:{ElementName}:{_nullable}";

	public XmlRootAttribute()
	{
	}

	public XmlRootAttribute(string elementName)
	{
		_elementName = elementName;
	}

	internal bool GetIsNullableSpecified()
	{
		return IsNullableSpecified;
	}

	internal string GetKey()
	{
		return Key;
	}
}
