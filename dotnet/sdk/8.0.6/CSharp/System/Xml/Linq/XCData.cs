using System.Threading;
using System.Threading.Tasks;

namespace System.Xml.Linq;

public class XCData : XText
{
	public override XmlNodeType NodeType => XmlNodeType.CDATA;

	public XCData(string value)
		: base(value)
	{
	}

	public XCData(XCData other)
		: base(other)
	{
	}

	internal XCData(XmlReader r)
		: base(r)
	{
	}

	public override void WriteTo(XmlWriter writer)
	{
		ArgumentNullException.ThrowIfNull(writer, "writer");
		writer.WriteCData(text);
	}

	public override Task WriteToAsync(XmlWriter writer, CancellationToken cancellationToken)
	{
		ArgumentNullException.ThrowIfNull(writer, "writer");
		if (cancellationToken.IsCancellationRequested)
		{
			return Task.FromCanceled(cancellationToken);
		}
		return writer.WriteCDataAsync(text);
	}

	internal override XNode CloneNode()
	{
		return new XCData(this);
	}
}
