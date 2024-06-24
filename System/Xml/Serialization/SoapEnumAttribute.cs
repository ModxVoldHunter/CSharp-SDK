using System.Diagnostics.CodeAnalysis;

namespace System.Xml.Serialization;

[AttributeUsage(AttributeTargets.Field)]
public class SoapEnumAttribute : Attribute
{
	private string _name;

	public string Name
	{
		get
		{
			return _name ?? string.Empty;
		}
		[param: AllowNull]
		set
		{
			_name = value;
		}
	}

	public SoapEnumAttribute()
	{
	}

	public SoapEnumAttribute(string name)
	{
		_name = name;
	}
}
