namespace System.Runtime.Serialization.Formatters.Binary;

internal interface IStreamable
{
	void Write(BinaryFormatterWriter output);

	void Read(BinaryParser input);
}
