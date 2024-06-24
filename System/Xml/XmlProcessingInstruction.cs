using System.Diagnostics.CodeAnalysis;
using System.Xml.XPath;

namespace System.Xml;

public class XmlProcessingInstruction : XmlLinkedNode
{
	private readonly string _target;

	private string _data;

	public override string Name => _target;

	public override string LocalName => Name;

	public override string Value
	{
		get
		{
			return _data;
		}
		[param: AllowNull]
		set
		{
			Data = value;
		}
	}

	public string Target => _target;

	public string Data
	{
		get
		{
			return _data;
		}
		[param: AllowNull]
		set
		{
			XmlNode xmlNode = ParentNode;
			string text = value ?? string.Empty;
			XmlNodeChangedEventArgs eventArgs = GetEventArgs(this, xmlNode, xmlNode, _data, text, XmlNodeChangedAction.Change);
			if (eventArgs != null)
			{
				BeforeEvent(eventArgs);
			}
			_data = text;
			if (eventArgs != null)
			{
				AfterEvent(eventArgs);
			}
		}
	}

	public override string InnerText
	{
		get
		{
			return _data;
		}
		[param: AllowNull]
		set
		{
			Data = value;
		}
	}

	public override XmlNodeType NodeType => XmlNodeType.ProcessingInstruction;

	internal override string XPLocalName => Name;

	internal override XPathNodeType XPNodeType => XPathNodeType.ProcessingInstruction;

	protected internal XmlProcessingInstruction(string target, string? data, XmlDocument doc)
		: base(doc)
	{
		ArgumentException.ThrowIfNullOrEmpty(target, "target");
		_target = target;
		_data = data ?? string.Empty;
	}

	public override XmlNode CloneNode(bool deep)
	{
		return OwnerDocument.CreateProcessingInstruction(_target, _data);
	}

	public override void WriteTo(XmlWriter w)
	{
		w.WriteProcessingInstruction(_target, _data);
	}

	public override void WriteContentTo(XmlWriter w)
	{
	}
}
