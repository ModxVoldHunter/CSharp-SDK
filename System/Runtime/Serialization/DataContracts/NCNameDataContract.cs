namespace System.Runtime.Serialization.DataContracts;

internal sealed class NCNameDataContract : StringDataContract
{
	internal NCNameDataContract()
		: base(DictionaryGlobals.NCNameLocalName, DictionaryGlobals.SchemaNamespace)
	{
	}
}
