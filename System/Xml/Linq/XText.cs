using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace System.Xml.Linq;

public class XText : XNode
{
	internal string text;

	public override XmlNodeType NodeType => XmlNodeType.Text;

	public string Value
	{
		get
		{
			return text;
		}
		set
		{
			ArgumentNullException.ThrowIfNull(value, "value");
			bool flag = NotifyChanging(this, XObjectChangeEventArgs.Value);
			text = value;
			if (flag)
			{
				NotifyChanged(this, XObjectChangeEventArgs.Value);
			}
		}
	}

	public XText(string value)
	{
		ArgumentNullException.ThrowIfNull(value, "value");
		text = value;
	}

	public XText(XText other)
	{
		ArgumentNullException.ThrowIfNull(other, "other");
		text = other.text;
	}

	internal XText(XmlReader r)
	{
		text = r.Value;
		r.Read();
	}

	public override void WriteTo(XmlWriter writer)
	{
		ArgumentNullException.ThrowIfNull(writer, "writer");
		if (parent is XDocument)
		{
			writer.WriteWhitespace(text);
		}
		else
		{
			writer.WriteString(text);
		}
	}

	public override Task WriteToAsync(XmlWriter writer, CancellationToken cancellationToken)
	{
		ArgumentNullException.ThrowIfNull(writer, "writer");
		if (cancellationToken.IsCancellationRequested)
		{
			return Task.FromCanceled(cancellationToken);
		}
		if (!(parent is XDocument))
		{
			return writer.WriteStringAsync(text);
		}
		return writer.WriteWhitespaceAsync(text);
	}

	internal override void AppendText(StringBuilder sb)
	{
		sb.Append(text);
	}

	internal override XNode CloneNode()
	{
		return new XText(this);
	}

	internal override bool DeepEquals(XNode node)
	{
		if (node != null && NodeType == node.NodeType)
		{
			return text == ((XText)node).text;
		}
		return false;
	}

	internal override int GetDeepHashCode()
	{
		return text.GetHashCode();
	}
}
