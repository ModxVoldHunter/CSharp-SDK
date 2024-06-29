using System.IO;
using System.Text;

namespace System.Xml;

internal sealed class XmlUTF8TextWriter : XmlBaseWriter, IXmlTextWriterInitializer
{
	private XmlUTF8NodeWriter _writer;

	public override bool CanFragment => _writer.Encoding == null;

	public void SetOutput(Stream stream, Encoding encoding, bool ownsStream)
	{
		ArgumentNullException.ThrowIfNull(stream, "stream");
		ArgumentNullException.ThrowIfNull(encoding, "encoding");
		if (encoding.WebName != Encoding.UTF8.WebName)
		{
			stream = new EncodingStreamWrapper(stream, encoding, emitBOM: true);
		}
		if (_writer == null)
		{
			_writer = new XmlUTF8NodeWriter();
		}
		_writer.SetOutput(stream, ownsStream, encoding);
		SetOutput(_writer);
	}

	protected override XmlSigningNodeWriter CreateSigningNodeWriter()
	{
		return new XmlSigningNodeWriter(text: true);
	}
}
