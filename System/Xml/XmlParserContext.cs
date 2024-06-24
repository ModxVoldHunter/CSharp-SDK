using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace System.Xml;

public class XmlParserContext
{
	private XmlNameTable _nt;

	private XmlNamespaceManager _nsMgr;

	private string _docTypeName = string.Empty;

	private string _pubId = string.Empty;

	private string _sysId = string.Empty;

	private string _internalSubset = string.Empty;

	private string _xmlLang = string.Empty;

	private XmlSpace _xmlSpace;

	private string _baseURI = string.Empty;

	private Encoding _encoding;

	public XmlNameTable? NameTable
	{
		get
		{
			return _nt;
		}
		set
		{
			_nt = value;
		}
	}

	public XmlNamespaceManager? NamespaceManager
	{
		get
		{
			return _nsMgr;
		}
		set
		{
			_nsMgr = value;
		}
	}

	public string DocTypeName
	{
		get
		{
			return _docTypeName;
		}
		[param: AllowNull]
		set
		{
			_docTypeName = value ?? string.Empty;
		}
	}

	public string PublicId
	{
		get
		{
			return _pubId;
		}
		[param: AllowNull]
		set
		{
			_pubId = value ?? string.Empty;
		}
	}

	public string SystemId
	{
		get
		{
			return _sysId;
		}
		[param: AllowNull]
		set
		{
			_sysId = value ?? string.Empty;
		}
	}

	public string BaseURI
	{
		get
		{
			return _baseURI;
		}
		[param: AllowNull]
		set
		{
			_baseURI = value ?? string.Empty;
		}
	}

	public string InternalSubset
	{
		get
		{
			return _internalSubset;
		}
		[param: AllowNull]
		set
		{
			_internalSubset = value ?? string.Empty;
		}
	}

	public string XmlLang
	{
		get
		{
			return _xmlLang;
		}
		[param: AllowNull]
		set
		{
			_xmlLang = value ?? string.Empty;
		}
	}

	public XmlSpace XmlSpace
	{
		get
		{
			return _xmlSpace;
		}
		set
		{
			_xmlSpace = value;
		}
	}

	public Encoding? Encoding
	{
		get
		{
			return _encoding;
		}
		set
		{
			_encoding = value;
		}
	}

	internal bool HasDtdInfo
	{
		get
		{
			if (!(_internalSubset != string.Empty) && !(_pubId != string.Empty))
			{
				return _sysId != string.Empty;
			}
			return true;
		}
	}

	public XmlParserContext(XmlNameTable? nt, XmlNamespaceManager? nsMgr, string? xmlLang, XmlSpace xmlSpace)
		: this(nt, nsMgr, null, null, null, null, string.Empty, xmlLang, xmlSpace)
	{
	}

	public XmlParserContext(XmlNameTable? nt, XmlNamespaceManager? nsMgr, string? xmlLang, XmlSpace xmlSpace, Encoding? enc)
		: this(nt, nsMgr, null, null, null, null, string.Empty, xmlLang, xmlSpace, enc)
	{
	}

	public XmlParserContext(XmlNameTable? nt, XmlNamespaceManager? nsMgr, string? docTypeName, string? pubId, string? sysId, string? internalSubset, string? baseURI, string? xmlLang, XmlSpace xmlSpace)
		: this(nt, nsMgr, docTypeName, pubId, sysId, internalSubset, baseURI, xmlLang, xmlSpace, null)
	{
	}

	public XmlParserContext(XmlNameTable? nt, XmlNamespaceManager? nsMgr, string? docTypeName, string? pubId, string? sysId, string? internalSubset, string? baseURI, string? xmlLang, XmlSpace xmlSpace, Encoding? enc)
	{
		if (nsMgr != null)
		{
			if (nt == null)
			{
				_nt = nsMgr.NameTable;
			}
			else
			{
				if (nt != nsMgr.NameTable)
				{
					throw new XmlException(System.SR.Xml_NotSameNametable, string.Empty);
				}
				_nt = nt;
			}
		}
		else
		{
			_nt = nt;
		}
		_nsMgr = nsMgr;
		_docTypeName = docTypeName ?? string.Empty;
		_pubId = pubId ?? string.Empty;
		_sysId = sysId ?? string.Empty;
		_internalSubset = internalSubset ?? string.Empty;
		_baseURI = baseURI ?? string.Empty;
		_xmlLang = xmlLang ?? string.Empty;
		_xmlSpace = xmlSpace;
		_encoding = enc;
	}
}
