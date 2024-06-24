using System.IO;

namespace System.Xml.Xsl.Runtime;

internal sealed class XmlQueryDataReader : BinaryReader
{
	public XmlQueryDataReader(Stream input)
		: base(input)
	{
	}

	public string ReadStringQ()
	{
		if (!ReadBoolean())
		{
			return null;
		}
		return ReadString();
	}

	public sbyte ReadSByte(sbyte minValue, sbyte maxValue)
	{
		sbyte b = ReadSByte();
		ArgumentOutOfRangeException.ThrowIfGreaterThan(minValue, b, "minValue");
		ArgumentOutOfRangeException.ThrowIfLessThan(maxValue, b, "maxValue");
		return b;
	}
}
