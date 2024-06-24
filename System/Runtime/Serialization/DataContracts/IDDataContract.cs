namespace System.Runtime.Serialization.DataContracts;

internal sealed class IDDataContract : StringDataContract
{
	internal IDDataContract()
		: base(DictionaryGlobals.XSDIDLocalName, DictionaryGlobals.SchemaNamespace)
	{
	}
}
