using System.Diagnostics.CodeAnalysis;

namespace System.Xml.Serialization;

public class SoapSchemaMember
{
	private string _memberName;

	private XmlQualifiedName _type = XmlQualifiedName.Empty;

	public XmlQualifiedName? MemberType
	{
		get
		{
			return _type;
		}
		set
		{
			_type = value;
		}
	}

	public string MemberName
	{
		get
		{
			return _memberName ?? string.Empty;
		}
		[param: AllowNull]
		set
		{
			_memberName = value;
		}
	}
}
