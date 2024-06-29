namespace System.Runtime.Serialization.DataContracts;

internal sealed class GMonthDataContract : StringDataContract
{
	internal GMonthDataContract()
		: base(DictionaryGlobals.gMonthLocalName, DictionaryGlobals.SchemaNamespace)
	{
	}
}
