using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Xml.Xsl.Runtime;

namespace System.Xml;

public sealed class XmlWriterSettings
{
	internal static readonly XmlWriterSettings s_defaultWriterSettings = new XmlWriterSettings
	{
		ReadOnly = true
	};

	private bool _useAsync;

	private Encoding _encoding;

	private bool _omitXmlDecl;

	private NewLineHandling _newLineHandling;

	private string _newLineChars;

	private string _indentChars;

	private bool _newLineOnAttributes;

	private bool _closeOutput;

	private NamespaceHandling _namespaceHandling;

	private ConformanceLevel _conformanceLevel;

	private bool _checkCharacters;

	private bool _writeEndDocumentOnClose;

	private bool _doNotEscapeUriAttributes;

	private bool _mergeCDataSections;

	private string _mediaType;

	private string _docTypeSystem;

	private string _docTypePublic;

	private XmlStandalone _standalone;

	private bool _autoXmlDecl;

	public bool Async
	{
		get
		{
			return _useAsync;
		}
		set
		{
			CheckReadOnly("Async");
			_useAsync = value;
		}
	}

	public Encoding Encoding
	{
		get
		{
			return _encoding;
		}
		[MemberNotNull("_encoding")]
		set
		{
			CheckReadOnly("Encoding");
			_encoding = value;
		}
	}

	public bool OmitXmlDeclaration
	{
		get
		{
			return _omitXmlDecl;
		}
		set
		{
			CheckReadOnly("OmitXmlDeclaration");
			_omitXmlDecl = value;
		}
	}

	public NewLineHandling NewLineHandling
	{
		get
		{
			return _newLineHandling;
		}
		set
		{
			CheckReadOnly("NewLineHandling");
			if ((uint)value > 2u)
			{
				ThrowArgumentOutOfRangeException("value");
			}
			_newLineHandling = value;
		}
	}

	public string NewLineChars
	{
		get
		{
			return _newLineChars;
		}
		[MemberNotNull("_newLineChars")]
		set
		{
			CheckReadOnly("NewLineChars");
			ArgumentNullException.ThrowIfNull(value, "value");
			_newLineChars = value;
		}
	}

	public bool Indent
	{
		get
		{
			return IndentInternal == TriState.True;
		}
		set
		{
			CheckReadOnly("Indent");
			IndentInternal = (value ? TriState.True : TriState.False);
		}
	}

	public string IndentChars
	{
		get
		{
			return _indentChars;
		}
		[MemberNotNull("_indentChars")]
		set
		{
			CheckReadOnly("IndentChars");
			ArgumentNullException.ThrowIfNull(value, "value");
			_indentChars = value;
		}
	}

	public bool NewLineOnAttributes
	{
		get
		{
			return _newLineOnAttributes;
		}
		set
		{
			CheckReadOnly("NewLineOnAttributes");
			_newLineOnAttributes = value;
		}
	}

	public bool CloseOutput
	{
		get
		{
			return _closeOutput;
		}
		set
		{
			CheckReadOnly("CloseOutput");
			_closeOutput = value;
		}
	}

	public ConformanceLevel ConformanceLevel
	{
		get
		{
			return _conformanceLevel;
		}
		set
		{
			CheckReadOnly("ConformanceLevel");
			if ((uint)value > 2u)
			{
				ThrowArgumentOutOfRangeException("value");
			}
			_conformanceLevel = value;
		}
	}

	public bool CheckCharacters
	{
		get
		{
			return _checkCharacters;
		}
		set
		{
			CheckReadOnly("CheckCharacters");
			_checkCharacters = value;
		}
	}

	public NamespaceHandling NamespaceHandling
	{
		get
		{
			return _namespaceHandling;
		}
		set
		{
			CheckReadOnly("NamespaceHandling");
			ArgumentOutOfRangeException.ThrowIfGreaterThan((uint)value, 1u, "value");
			_namespaceHandling = value;
		}
	}

	public bool WriteEndDocumentOnClose
	{
		get
		{
			return _writeEndDocumentOnClose;
		}
		set
		{
			CheckReadOnly("WriteEndDocumentOnClose");
			_writeEndDocumentOnClose = value;
		}
	}

	public XmlOutputMethod OutputMethod { get; internal set; }

	internal List<XmlQualifiedName> CDataSectionElements { get; private set; } = new List<XmlQualifiedName>();


	public bool DoNotEscapeUriAttributes
	{
		get
		{
			return _doNotEscapeUriAttributes;
		}
		set
		{
			CheckReadOnly("DoNotEscapeUriAttributes");
			_doNotEscapeUriAttributes = value;
		}
	}

	internal bool MergeCDataSections
	{
		get
		{
			return _mergeCDataSections;
		}
		set
		{
			CheckReadOnly("MergeCDataSections");
			_mergeCDataSections = value;
		}
	}

	internal string? MediaType
	{
		get
		{
			return _mediaType;
		}
		set
		{
			CheckReadOnly("MediaType");
			_mediaType = value;
		}
	}

	internal string? DocTypeSystem
	{
		get
		{
			return _docTypeSystem;
		}
		set
		{
			CheckReadOnly("DocTypeSystem");
			_docTypeSystem = value;
		}
	}

	internal string? DocTypePublic
	{
		get
		{
			return _docTypePublic;
		}
		set
		{
			CheckReadOnly("DocTypePublic");
			_docTypePublic = value;
		}
	}

	internal XmlStandalone Standalone
	{
		get
		{
			return _standalone;
		}
		set
		{
			CheckReadOnly("Standalone");
			_standalone = value;
		}
	}

	internal bool AutoXmlDeclaration
	{
		get
		{
			return _autoXmlDecl;
		}
		set
		{
			CheckReadOnly("AutoXmlDeclaration");
			_autoXmlDecl = value;
		}
	}

	internal TriState IndentInternal { get; set; }

	private bool IsQuerySpecific
	{
		get
		{
			if (CDataSectionElements.Count == 0 && _docTypePublic == null && _docTypeSystem == null)
			{
				return _standalone == XmlStandalone.Yes;
			}
			return true;
		}
	}

	internal bool ReadOnly { get; set; }

	public XmlWriterSettings()
	{
		Initialize();
	}

	public void Reset()
	{
		CheckReadOnly("Reset");
		Initialize();
	}

	public XmlWriterSettings Clone()
	{
		XmlWriterSettings xmlWriterSettings = (XmlWriterSettings)MemberwiseClone();
		xmlWriterSettings.CDataSectionElements = new List<XmlQualifiedName>(CDataSectionElements);
		xmlWriterSettings.ReadOnly = false;
		return xmlWriterSettings;
	}

	internal XmlWriter CreateWriter(string outputFileName)
	{
		ArgumentNullException.ThrowIfNull(outputFileName, "outputFileName");
		XmlWriterSettings xmlWriterSettings = this;
		if (!xmlWriterSettings.CloseOutput)
		{
			xmlWriterSettings = xmlWriterSettings.Clone();
			xmlWriterSettings.CloseOutput = true;
		}
		FileStream fileStream = null;
		try
		{
			fileStream = new FileStream(outputFileName, FileMode.Create, FileAccess.Write, FileShare.Read, 4096, _useAsync);
			return xmlWriterSettings.CreateWriter(fileStream);
		}
		catch
		{
			fileStream?.Dispose();
			throw;
		}
	}

	internal XmlWriter CreateWriter(Stream output)
	{
		ArgumentNullException.ThrowIfNull(output, "output");
		XmlWriter xmlWriter;
		if (Encoding.WebName == "utf-8")
		{
			switch (OutputMethod)
			{
			case XmlOutputMethod.Xml:
				xmlWriter = (Indent ? new XmlUtf8RawTextWriterIndent(output, this) : new XmlUtf8RawTextWriter(output, this));
				break;
			case XmlOutputMethod.Html:
				xmlWriter = (Indent ? new HtmlUtf8RawTextWriterIndent(output, this) : new HtmlUtf8RawTextWriter(output, this));
				break;
			case XmlOutputMethod.Text:
				xmlWriter = new TextUtf8RawTextWriter(output, this);
				break;
			case XmlOutputMethod.AutoDetect:
				xmlWriter = new XmlAutoDetectWriter(output, this);
				break;
			default:
				return null;
			}
		}
		else
		{
			switch (OutputMethod)
			{
			case XmlOutputMethod.Xml:
				xmlWriter = (Indent ? new XmlEncodedRawTextWriterIndent(output, this) : new XmlEncodedRawTextWriter(output, this));
				break;
			case XmlOutputMethod.Html:
				xmlWriter = (Indent ? new HtmlEncodedRawTextWriterIndent(output, this) : new HtmlEncodedRawTextWriter(output, this));
				break;
			case XmlOutputMethod.Text:
				xmlWriter = new TextEncodedRawTextWriter(output, this);
				break;
			case XmlOutputMethod.AutoDetect:
				xmlWriter = new XmlAutoDetectWriter(output, this);
				break;
			default:
				return null;
			}
		}
		if (OutputMethod != XmlOutputMethod.AutoDetect && IsQuerySpecific)
		{
			xmlWriter = new QueryOutputWriter((XmlRawWriter)xmlWriter, this);
		}
		xmlWriter = new XmlWellFormedWriter(xmlWriter, this);
		if (_useAsync)
		{
			xmlWriter = new XmlAsyncCheckWriter(xmlWriter);
		}
		return xmlWriter;
	}

	internal XmlWriter CreateWriter(TextWriter output)
	{
		ArgumentNullException.ThrowIfNull(output, "output");
		XmlWriter xmlWriter;
		switch (OutputMethod)
		{
		case XmlOutputMethod.Xml:
			xmlWriter = (Indent ? new XmlEncodedRawTextWriterIndent(output, this) : new XmlEncodedRawTextWriter(output, this));
			break;
		case XmlOutputMethod.Html:
			xmlWriter = (Indent ? new HtmlEncodedRawTextWriterIndent(output, this) : new HtmlEncodedRawTextWriter(output, this));
			break;
		case XmlOutputMethod.Text:
			xmlWriter = new TextEncodedRawTextWriter(output, this);
			break;
		case XmlOutputMethod.AutoDetect:
			xmlWriter = new XmlAutoDetectWriter(output, this);
			break;
		default:
			return null;
		}
		if (OutputMethod != XmlOutputMethod.AutoDetect && IsQuerySpecific)
		{
			xmlWriter = new QueryOutputWriter((XmlRawWriter)xmlWriter, this);
		}
		xmlWriter = new XmlWellFormedWriter(xmlWriter, this);
		if (_useAsync)
		{
			xmlWriter = new XmlAsyncCheckWriter(xmlWriter);
		}
		return xmlWriter;
	}

	internal XmlWriter CreateWriter(XmlWriter output)
	{
		ArgumentNullException.ThrowIfNull(output, "output");
		return AddConformanceWrapper(output);
	}

	private void CheckReadOnly([CallerMemberName] string propertyName = null)
	{
		if (ReadOnly)
		{
			throw new XmlException(System.SR.Xml_ReadOnlyProperty, GetType().Name + "." + propertyName);
		}
	}

	[MemberNotNull("_encoding")]
	[MemberNotNull("_newLineChars")]
	[MemberNotNull("_indentChars")]
	private void Initialize()
	{
		_encoding = System.Text.Encoding.UTF8;
		_omitXmlDecl = false;
		_newLineHandling = NewLineHandling.Replace;
		_newLineChars = Environment.NewLine;
		IndentInternal = TriState.Unknown;
		_indentChars = "  ";
		_newLineOnAttributes = false;
		_closeOutput = false;
		_namespaceHandling = NamespaceHandling.Default;
		_conformanceLevel = ConformanceLevel.Document;
		_checkCharacters = true;
		_writeEndDocumentOnClose = true;
		OutputMethod = XmlOutputMethod.Xml;
		CDataSectionElements.Clear();
		_mergeCDataSections = false;
		_mediaType = null;
		_docTypeSystem = null;
		_docTypePublic = null;
		_standalone = XmlStandalone.Omit;
		_doNotEscapeUriAttributes = false;
		_useAsync = false;
		ReadOnly = false;
	}

	private XmlWriter AddConformanceWrapper(XmlWriter baseWriter)
	{
		ConformanceLevel conformanceLevel = ConformanceLevel.Auto;
		XmlWriterSettings settings = baseWriter.Settings;
		bool flag = false;
		bool checkNames = false;
		bool flag2 = false;
		bool flag3 = false;
		if (settings == null)
		{
			if (_newLineHandling == NewLineHandling.Replace)
			{
				flag2 = true;
				flag3 = true;
			}
			if (_checkCharacters)
			{
				flag = true;
				flag3 = true;
			}
		}
		else
		{
			if (_conformanceLevel != settings.ConformanceLevel)
			{
				conformanceLevel = ConformanceLevel;
				flag3 = true;
			}
			if (_checkCharacters && !settings.CheckCharacters)
			{
				flag = true;
				checkNames = conformanceLevel == ConformanceLevel.Auto;
				flag3 = true;
			}
			if (_newLineHandling == NewLineHandling.Replace && settings.NewLineHandling == NewLineHandling.None)
			{
				flag2 = true;
				flag3 = true;
			}
		}
		XmlWriter xmlWriter = baseWriter;
		if (flag3)
		{
			if (conformanceLevel != 0)
			{
				xmlWriter = new XmlWellFormedWriter(xmlWriter, this);
			}
			if (flag || flag2)
			{
				xmlWriter = new XmlCharCheckingWriter(xmlWriter, flag, checkNames, flag2, NewLineChars);
			}
		}
		if (IsQuerySpecific && (settings == null || !settings.IsQuerySpecific))
		{
			xmlWriter = new QueryOutputWriterV1(xmlWriter, this);
		}
		return xmlWriter;
	}

	internal void GetObjectData(XmlQueryDataWriter writer)
	{
		writer.Write(Encoding.CodePage);
		writer.Write(OmitXmlDeclaration);
		writer.Write((sbyte)NewLineHandling);
		writer.WriteStringQ(NewLineChars);
		writer.Write((sbyte)IndentInternal);
		writer.WriteStringQ(IndentChars);
		writer.Write(NewLineOnAttributes);
		writer.Write(CloseOutput);
		writer.Write((sbyte)ConformanceLevel);
		writer.Write(CheckCharacters);
		writer.Write((sbyte)OutputMethod);
		writer.Write(CDataSectionElements.Count);
		foreach (XmlQualifiedName cDataSectionElement in CDataSectionElements)
		{
			writer.Write(cDataSectionElement.Name);
			writer.Write(cDataSectionElement.Namespace);
		}
		writer.Write(_mergeCDataSections);
		writer.WriteStringQ(_mediaType);
		writer.WriteStringQ(_docTypeSystem);
		writer.WriteStringQ(_docTypePublic);
		writer.Write((sbyte)_standalone);
		writer.Write(_autoXmlDecl);
		writer.Write(ReadOnly);
	}

	internal XmlWriterSettings(XmlQueryDataReader reader)
	{
		Encoding = System.Text.Encoding.GetEncoding(reader.ReadInt32());
		OmitXmlDeclaration = reader.ReadBoolean();
		NewLineHandling = (NewLineHandling)reader.ReadSByte(0, 2);
		NewLineChars = reader.ReadStringQ();
		IndentInternal = (TriState)reader.ReadSByte(-1, 1);
		IndentChars = reader.ReadStringQ();
		NewLineOnAttributes = reader.ReadBoolean();
		CloseOutput = reader.ReadBoolean();
		ConformanceLevel = (ConformanceLevel)reader.ReadSByte(0, 2);
		CheckCharacters = reader.ReadBoolean();
		OutputMethod = (XmlOutputMethod)reader.ReadSByte(0, 3);
		int num = reader.ReadInt32();
		CDataSectionElements = new List<XmlQualifiedName>(num);
		for (int i = 0; i < num; i++)
		{
			CDataSectionElements.Add(new XmlQualifiedName(reader.ReadString(), reader.ReadString()));
		}
		_mergeCDataSections = reader.ReadBoolean();
		_mediaType = reader.ReadStringQ();
		_docTypeSystem = reader.ReadStringQ();
		_docTypePublic = reader.ReadStringQ();
		Standalone = (XmlStandalone)reader.ReadSByte(0, 2);
		_autoXmlDecl = reader.ReadBoolean();
		ReadOnly = reader.ReadBoolean();
	}

	[DoesNotReturn]
	[StackTraceHidden]
	private static void ThrowArgumentOutOfRangeException(string paramName)
	{
		throw new ArgumentOutOfRangeException(paramName);
	}
}
