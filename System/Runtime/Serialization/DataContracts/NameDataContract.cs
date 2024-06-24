namespace System.Runtime.Serialization.DataContracts;

internal sealed class NameDataContract : StringDataContract
{
	internal NameDataContract()
		: base(DictionaryGlobals.NameLocalName, DictionaryGlobals.SchemaNamespace)
	{
	}
}
