namespace System.Runtime.Serialization.Formatters.Binary;

internal sealed class BinaryCrossAppDomainAssembly : IStreamable
{
	internal int _assemId;

	internal int _assemblyIndex;

	internal BinaryCrossAppDomainAssembly()
	{
	}

	public void Write(BinaryFormatterWriter output)
	{
		output.WriteByte(20);
		output.WriteInt32(_assemId);
		output.WriteInt32(_assemblyIndex);
	}

	public void Read(BinaryParser input)
	{
		_assemId = input.ReadInt32();
		_assemblyIndex = input.ReadInt32();
	}
}
