using System.Threading;
using System.Threading.Tasks;

namespace System.Xml.Linq;

public class XProcessingInstruction : XNode
{
	internal string target;

	internal string data;

	public string Data
	{
		get
		{
			return data;
		}
		set
		{
			ArgumentNullException.ThrowIfNull(value, "value");
			bool flag = NotifyChanging(this, XObjectChangeEventArgs.Value);
			data = value;
			if (flag)
			{
				NotifyChanged(this, XObjectChangeEventArgs.Value);
			}
		}
	}

	public override XmlNodeType NodeType => XmlNodeType.ProcessingInstruction;

	public string Target
	{
		get
		{
			return target;
		}
		set
		{
			ValidateName(value);
			bool flag = NotifyChanging(this, XObjectChangeEventArgs.Name);
			target = value;
			if (flag)
			{
				NotifyChanged(this, XObjectChangeEventArgs.Name);
			}
		}
	}

	public XProcessingInstruction(string target, string data)
	{
		ArgumentNullException.ThrowIfNull(data, "data");
		ValidateName(target);
		this.target = target;
		this.data = data;
	}

	public XProcessingInstruction(XProcessingInstruction other)
	{
		ArgumentNullException.ThrowIfNull(other, "other");
		target = other.target;
		data = other.data;
	}

	internal XProcessingInstruction(XmlReader r)
	{
		target = r.Name;
		data = r.Value;
		r.Read();
	}

	public override void WriteTo(XmlWriter writer)
	{
		ArgumentNullException.ThrowIfNull(writer, "writer");
		writer.WriteProcessingInstruction(target, data);
	}

	public override Task WriteToAsync(XmlWriter writer, CancellationToken cancellationToken)
	{
		ArgumentNullException.ThrowIfNull(writer, "writer");
		if (cancellationToken.IsCancellationRequested)
		{
			return Task.FromCanceled(cancellationToken);
		}
		return writer.WriteProcessingInstructionAsync(target, data);
	}

	internal override XNode CloneNode()
	{
		return new XProcessingInstruction(this);
	}

	internal override bool DeepEquals(XNode node)
	{
		if (node is XProcessingInstruction xProcessingInstruction && target == xProcessingInstruction.target)
		{
			return data == xProcessingInstruction.data;
		}
		return false;
	}

	internal override int GetDeepHashCode()
	{
		return target.GetHashCode() ^ data.GetHashCode();
	}

	private static void ValidateName(string name)
	{
		XmlConvert.VerifyNCName(name);
		if (string.Equals(name, "xml", StringComparison.OrdinalIgnoreCase))
		{
			throw new ArgumentException(System.SR.Format(System.SR.Argument_InvalidPIName, name));
		}
	}
}
