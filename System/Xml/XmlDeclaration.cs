using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace System.Xml;

public class XmlDeclaration : XmlLinkedNode
{
	private string _version;

	private string _encoding;

	private string _standalone;

	public string Version
	{
		get
		{
			return _version;
		}
		[MemberNotNull("_version")]
		internal set
		{
			_version = value;
		}
	}

	public string Encoding
	{
		get
		{
			return _encoding;
		}
		[MemberNotNull("_encoding")]
		[param: AllowNull]
		set
		{
			_encoding = value ?? string.Empty;
		}
	}

	public string Standalone
	{
		get
		{
			return _standalone;
		}
		[MemberNotNull("_standalone")]
		[param: AllowNull]
		set
		{
			if (value == null)
			{
				_standalone = string.Empty;
				return;
			}
			if (value.Length == 0 || value == "yes" || value == "no")
			{
				_standalone = value;
				return;
			}
			throw new ArgumentException(System.SR.Format(System.SR.Xdom_standalone, value));
		}
	}

	public override string? Value
	{
		get
		{
			return InnerText;
		}
		set
		{
			InnerText = value;
		}
	}

	public override string InnerText
	{
		get
		{
			Span<char> initialBuffer = stackalloc char[256];
			System.Text.ValueStringBuilder valueStringBuilder = new System.Text.ValueStringBuilder(initialBuffer);
			valueStringBuilder.Append("version=\"");
			valueStringBuilder.Append(Version);
			valueStringBuilder.Append('"');
			if (Encoding.Length > 0)
			{
				valueStringBuilder.Append(" encoding=\"");
				valueStringBuilder.Append(Encoding);
				valueStringBuilder.Append('"');
			}
			if (Standalone.Length > 0)
			{
				valueStringBuilder.Append(" standalone=\"");
				valueStringBuilder.Append(Standalone);
				valueStringBuilder.Append('"');
			}
			return valueStringBuilder.ToString();
		}
		set
		{
			string encoding = Encoding;
			string standalone = Standalone;
			string version = Version;
			XmlLoader.ParseXmlDeclarationValue(value, out var version2, out var encoding2, out var standalone2);
			try
			{
				if (version2 != null && !IsValidXmlVersion(version2))
				{
					throw new ArgumentException(System.SR.Xdom_Version);
				}
				Version = version2;
				if (encoding2 != null)
				{
					Encoding = encoding2;
				}
				if (standalone2 != null)
				{
					Standalone = standalone2;
				}
			}
			catch
			{
				Encoding = encoding;
				Standalone = standalone;
				Version = version;
				throw;
			}
		}
	}

	public override string Name => "xml";

	public override string LocalName => Name;

	public override XmlNodeType NodeType => XmlNodeType.XmlDeclaration;

	protected internal XmlDeclaration(string version, string? encoding, string? standalone, XmlDocument doc)
		: base(doc)
	{
		if (!IsValidXmlVersion(version))
		{
			throw new ArgumentException(System.SR.Xdom_Version);
		}
		if (standalone != null && standalone.Length > 0 && standalone != "yes" && standalone != "no")
		{
			throw new ArgumentException(System.SR.Format(System.SR.Xdom_standalone, standalone));
		}
		Encoding = encoding;
		Standalone = standalone;
		Version = version;
	}

	public override XmlNode CloneNode(bool deep)
	{
		return OwnerDocument.CreateXmlDeclaration(Version, Encoding, Standalone);
	}

	public override void WriteTo(XmlWriter w)
	{
		w.WriteProcessingInstruction(Name, InnerText);
	}

	public override void WriteContentTo(XmlWriter w)
	{
	}

	private static bool IsValidXmlVersion(string ver)
	{
		if (ver.Length >= 3 && ver[0] == '1' && ver[1] == '.')
		{
			return XmlCharType.IsOnlyDigits(ver, 2, ver.Length - 2);
		}
		return false;
	}
}
