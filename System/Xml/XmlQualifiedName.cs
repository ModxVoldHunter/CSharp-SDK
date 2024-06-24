using System.Diagnostics.CodeAnalysis;

namespace System.Xml;

public class XmlQualifiedName
{
	private int _hash;

	public static readonly XmlQualifiedName Empty = new XmlQualifiedName(string.Empty);

	public string Namespace { get; private set; }

	public string Name { get; private set; }

	public bool IsEmpty
	{
		get
		{
			if (Name.Length == 0)
			{
				return Namespace.Length == 0;
			}
			return false;
		}
	}

	public XmlQualifiedName()
		: this(string.Empty, string.Empty)
	{
	}

	public XmlQualifiedName(string? name)
		: this(name, string.Empty)
	{
	}

	public XmlQualifiedName(string? name, string? ns)
	{
		Namespace = ns ?? string.Empty;
		Name = name ?? string.Empty;
	}

	public override int GetHashCode()
	{
		if (_hash == 0)
		{
			_hash = Name.GetHashCode();
		}
		return _hash;
	}

	public override string ToString()
	{
		if (Namespace.Length != 0)
		{
			return Namespace + ":" + Name;
		}
		return Name;
	}

	public override bool Equals([NotNullWhen(true)] object? other)
	{
		if (this == other)
		{
			return true;
		}
		if (other is XmlQualifiedName xmlQualifiedName)
		{
			return Equals(xmlQualifiedName.Name, xmlQualifiedName.Namespace);
		}
		return false;
	}

	internal bool Equals(string name, string ns)
	{
		if (Name == name)
		{
			return Namespace == ns;
		}
		return false;
	}

	public static bool operator ==(XmlQualifiedName? a, XmlQualifiedName? b)
	{
		if ((object)a == b)
		{
			return true;
		}
		if ((object)a == null || (object)b == null)
		{
			return false;
		}
		if (a.Name == b.Name)
		{
			return a.Namespace == b.Namespace;
		}
		return false;
	}

	public static bool operator !=(XmlQualifiedName? a, XmlQualifiedName? b)
	{
		return !(a == b);
	}

	public static string ToString(string name, string? ns)
	{
		if (!string.IsNullOrEmpty(ns))
		{
			return ns + ":" + name;
		}
		return name;
	}

	internal void Init(string name, string ns)
	{
		Name = name ?? string.Empty;
		Namespace = ns ?? string.Empty;
		_hash = 0;
	}

	internal void SetNamespace(string ns)
	{
		Namespace = ns ?? string.Empty;
	}

	internal void Verify()
	{
		XmlConvert.VerifyNCName(Name);
		if (Namespace.Length != 0)
		{
			XmlConvert.ToUri(Namespace);
		}
	}

	internal void Atomize(XmlNameTable nameTable)
	{
		Name = nameTable.Add(Name);
		Namespace = nameTable.Add(Namespace);
	}

	internal static XmlQualifiedName Parse(string s, IXmlNamespaceResolver nsmgr, out string prefix)
	{
		ValidateNames.ParseQNameThrow(s, out prefix, out var localName);
		string text = nsmgr.LookupNamespace(prefix);
		if (text == null)
		{
			if (prefix.Length != 0)
			{
				throw new XmlException(System.SR.Xml_UnknownNs, prefix);
			}
			text = string.Empty;
		}
		return new XmlQualifiedName(localName, text);
	}

	internal XmlQualifiedName Clone()
	{
		return (XmlQualifiedName)MemberwiseClone();
	}
}
