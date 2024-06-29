namespace System.Runtime.Serialization.DataContracts;

internal sealed class IDREFSDataContract : StringDataContract
{
	internal IDREFSDataContract()
		: base(DictionaryGlobals.IDREFSLocalName, DictionaryGlobals.SchemaNamespace)
	{
	}
}
