using System.Diagnostics.CodeAnalysis;

namespace System.Xml.Serialization;

internal sealed class ConstantMapping : Mapping
{
	private string _xmlName;

	private string _name;

	private long _value;

	internal string XmlName
	{
		get
		{
			return _xmlName ?? string.Empty;
		}
		[param: AllowNull]
		set
		{
			_xmlName = value;
		}
	}

	internal string Name
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

	internal long Value
	{
		get
		{
			return _value;
		}
		set
		{
			_value = value;
		}
	}
}
