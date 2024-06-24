namespace System.Runtime.Serialization.DataContracts;

internal sealed class DateDataContract : StringDataContract
{
	internal DateDataContract()
		: base(DictionaryGlobals.dateLocalName, DictionaryGlobals.SchemaNamespace)
	{
	}
}
