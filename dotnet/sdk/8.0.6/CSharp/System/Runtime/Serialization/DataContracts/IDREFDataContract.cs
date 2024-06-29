namespace System.Runtime.Serialization.DataContracts;

internal sealed class IDREFDataContract : StringDataContract
{
	internal IDREFDataContract()
		: base(DictionaryGlobals.IDREFLocalName, DictionaryGlobals.SchemaNamespace)
	{
	}
}
