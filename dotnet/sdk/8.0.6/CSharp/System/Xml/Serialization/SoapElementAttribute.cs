using System.Diagnostics.CodeAnalysis;

namespace System.Xml.Serialization;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter | AttributeTargets.ReturnValue)]
public class SoapElementAttribute : Attribute
{
	private string _elementName;

	private string _dataType;

	private bool _nullable;

	public string ElementName
	{
		get
		{
			return _elementName ?? string.Empty;
		}
		[param: AllowNull]
		set
		{
			_elementName = value;
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
		}
	}

	public SoapElementAttribute()
	{
	}

	public SoapElementAttribute(string? elementName)
	{
		_elementName = elementName;
	}
}
