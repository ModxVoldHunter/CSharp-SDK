using System.Threading;
using System.Threading.Tasks;

namespace System.Xml.Linq;

public class XComment : XNode
{
	internal string value;

	public override XmlNodeType NodeType => XmlNodeType.Comment;

	public string Value
	{
		get
		{
			return value;
		}
		set
		{
			ArgumentNullException.ThrowIfNull(value, "value");
			bool flag = NotifyChanging(this, XObjectChangeEventArgs.Value);
			this.value = value;
			if (flag)
			{
				NotifyChanged(this, XObjectChangeEventArgs.Value);
			}
		}
	}

	public XComment(string value)
	{
		ArgumentNullException.ThrowIfNull(value, "value");
		this.value = value;
	}

	public XComment(XComment other)
	{
		ArgumentNullException.ThrowIfNull(other, "other");
		value = other.value;
	}

	internal XComment(XmlReader r)
	{
		value = r.Value;
		r.Read();
	}

	public override void WriteTo(XmlWriter writer)
	{
		ArgumentNullException.ThrowIfNull(writer, "writer");
		writer.WriteComment(value);
	}

	public override Task WriteToAsync(XmlWriter writer, CancellationToken cancellationToken)
	{
		ArgumentNullException.ThrowIfNull(writer, "writer");
		if (cancellationToken.IsCancellationRequested)
		{
			return Task.FromCanceled(cancellationToken);
		}
		return writer.WriteCommentAsync(value);
	}

	internal override XNode CloneNode()
	{
		return new XComment(this);
	}

	internal override bool DeepEquals(XNode node)
	{
		if (node is XComment xComment)
		{
			return value == xComment.value;
		}
		return false;
	}

	internal override int GetDeepHashCode()
	{
		return value.GetHashCode();
	}
}
