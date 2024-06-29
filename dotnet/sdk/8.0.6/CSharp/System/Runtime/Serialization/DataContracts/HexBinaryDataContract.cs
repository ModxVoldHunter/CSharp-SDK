namespace System.Runtime.Serialization.DataContracts;

internal sealed class HexBinaryDataContract : StringDataContract
{
	internal HexBinaryDataContract()
		: base(DictionaryGlobals.hexBinaryLocalName, DictionaryGlobals.SchemaNamespace)
	{
	}
}
