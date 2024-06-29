namespace System.Runtime.Serialization.DataContracts;

internal sealed class TimeDataContract : StringDataContract
{
	internal TimeDataContract()
		: base(DictionaryGlobals.timeLocalName, DictionaryGlobals.SchemaNamespace)
	{
	}
}
